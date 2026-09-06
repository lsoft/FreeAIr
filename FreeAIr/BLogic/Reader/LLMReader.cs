using FreeAIr.Helper;
using FreeAIr.Llm;
using FreeAIr.Llm.Streaming;
using Microsoft.VisualStudio.Threading;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FreeAIr.Chat;
using FreeAIr.Chat.Content;

namespace FreeAIr.BLogic.Reader
{
    /// <summary>
    /// Performs one streaming completion request for a chat and pushes the result back into it.
    ///
    /// A reader serves exactly one chat and only one request at a time: <see cref="AsyncStartRead"/>
    /// does nothing while the previous read is still running. The reader never throws to its
    /// caller — every failure is turned into a chat answer plus a `Failed` chat status.
    ///
    /// Use <see cref="LLMReaderPool"/> to get one; do not create readers directly.
    /// </summary>
    public sealed class LLMReader : IDisposable
    {
        /// <summary>Guards <see cref="_task"/> and <see cref="_cancellationTokenSource"/> against concurrent start/stop calls.</summary>
        private readonly object _taskLocker = new();

        /// <summary>The chat this reader streams completions into.</summary>
        private readonly FreeAIr.Chat.Chat _chat;

        /// <summary>
        /// The source the read in flight was started with. Replaced by every
        /// <see cref="StopSafelyAsync"/>, so its identity also tells a read whether it is still the
        /// current one. Guarded by <see cref="_taskLocker"/>.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// The read in flight, or null when the reader is idle. Guarded by <see cref="_taskLocker"/>.
        /// </summary>
        private Task? _task;

        /// <summary>Creates a reader bound to the given chat; obtain one through <see cref="LLMReaderPool"/> instead of calling this directly.</summary>
        public LLMReader(
            FreeAIr.Chat.Chat chat
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            _chat = chat;
        }

        /// <summary>
        /// Starts reading in the background and returns at once.
        /// Silently ignored if a read is already in progress.
        /// </summary>
        public void AsyncStartRead(
            )
        {
            lock (_taskLocker)
            {
                if (_task is not null)
                {
                    return;
                }

                //the source is taken here, under the lock, and not somewhere inside the read:
                //a concurrent StopSafelyAsync replaces the field, and the read which is being
                //started right now must not pick up the source that stop has just cancelled
                _task = ReadSafelyAsync(_cancellationTokenSource);
            }
        }

        /// <summary>Awaits the read currently in flight, if any; returns immediately when the reader is idle.</summary>
        public async Task WaitForTaskAsync(
            )
        {
            Task? task = null;
            lock (_taskLocker)
            {
                task = _task;
            }

            if (task is null)
            {
                return;
            }

            await task;
        }

        /// <summary>
        /// Cancels the read in flight and waits for it to unwind. Never throws.
        ///
        /// The fresh cancellation source is armed before the wait, not after it: the reader is
        /// public and a new read may be requested the very moment the old one has been let go, and
        /// that read has to start with a source nobody has cancelled.
        /// </summary>
        public async Task StopSafelyAsync()
        {
            try
            {
                Task? task;
                CancellationTokenSource cancellationTokenSource;
                lock (_taskLocker)
                {
                    task = _task;
                    _task = null;

                    cancellationTokenSource = _cancellationTokenSource;
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                if (task is null)
                {
                    //nothing was reading, so nobody has ever seen the source we have just replaced
                    cancellationTokenSource.Dispose();
                    return;
                }

                cancellationTokenSource.Cancel();

                try
                {
                    await task;
                }
                finally
                {
                    //only now, when the read which held the token has unwound, the source is free
                    cancellationTokenSource.Dispose();
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        /// <summary>Releases the current cancellation token source.</summary>
        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }

        /// <summary>Runs one read and, when it finishes, clears <see cref="_task"/> unless a newer read has already replaced the cancellation source.</summary>
        private async Task ReadSafelyAsync(
            CancellationTokenSource cancellationTokenSource
            )
        {
            try
            {
                await ReadSafelyPrivateAsync(cancellationTokenSource.Token);
            }
            finally
            {
                lock (_taskLocker)
                {
                    //if the reader has been stopped and started again while this read was
                    //unwinding, _task belongs to that newer read and must be left alone
                    if (ReferenceEquals(_cancellationTokenSource, cancellationTokenSource))
                    {
                        _task = null;
                    }
                }
            }
        }

        /// <summary>
        /// The body of a single turn:
        /// build the request from the chat, stream the completion, append the text to the answer
        /// as it arrives (so the UI updates live) and register the tool calls the model asked for.
        ///
        /// The tools are NOT invoked here. Each <see cref="ToolCallChatContent"/> executes itself
        /// (possibly after asking the user for a permission), and the last one to finish restarts
        /// this reader through <see cref="FreeAIr.Chat.Chat.CreateToolCall"/>.
        /// </summary>
        private async Task ReadSafelyPrivateAsync(
            CancellationToken cancellationToken
            )
        {
            //never block the UI thread while streaming
            await TaskScheduler.Default;

            AnswerChatContent? chatAnswer = null;

            try
            {
                _chat.Status = ChatStatusEnum.WaitingForAnswer;

                var transport = _chat.CreateTransport();
                var request = await _chat.BuildRequestAsync();

                var finishReason = LlmFinishReason.Unknown;
                var toolCallAccumulator = new ToolCallAccumulator();

                _chat.Status = ChatStatusEnum.ReadingAnswer;

                await foreach (var streamEvent in transport.StreamAsync(request, cancellationToken))
                {
                    toolCallAccumulator.Append(streamEvent);

                    switch (streamEvent)
                    {
                        case LlmTextDeltaEvent textDelta:
                            //for updating UI in real time
                            chatAnswer = await CreateOrAppendAnswerPartAsync(chatAnswer, textDelta.Text);

                            if (cancellationToken.IsCancellationRequested)
                            {
                                _chat.Status = ChatStatusEnum.Ready;
                                return;
                            }
                            break;

                        case LlmFinishedEvent finished:
                            finishReason = finished.Reason;
                            break;

                        case LlmProtocolFaultEvent fault:
                            //the endpoint answered with something which is not a completion at all;
                            //there is nothing to read further and the chat has to fail rather than
                            //sit waiting
                            chatAnswer = await CreateOrAppendAnswerPartAsync(chatAnswer, fault.Message);
                            _chat.Status = ChatStatusEnum.Failed;
                            return;
                    }
                }

                if (finishReason == LlmFinishReason.ToolCalls)
                {
                    foreach (var toolCall in toolCallAccumulator.Build())
                    {
                        _chat.CreateToolCall(
                            toolCall
                            );
                    }
                }

                _chat.Status = ChatStatusEnum.Ready;
            }
            catch (OperationCanceledException)
            {
                //this is OK
                _chat.Status = ChatStatusEnum.Ready;
            }
            catch (Exception excp)
            {
                chatAnswer = await CreateOrAppendAnswerPartAsync(chatAnswer, excp);

                _chat.Status = ChatStatusEnum.Failed;

                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Formats an exception as answer text and appends it to the chat, creating the answer
        /// content if this is the first piece. An HTTP client's own message is only the status —
        /// vLLM names the actual fault (`max_completion_tokens`, the context window) in the body
        /// and Anthropic the missing `max_tokens`, so the transport digs that sentence out and it
        /// is shown here alongside.
        /// </summary>
        private async Task<AnswerChatContent> CreateOrAppendAnswerPartAsync(
            AnswerChatContent? chatAnswer,
            Exception excp
            )
        {
            if (excp is null)
            {
                throw new ArgumentNullException(nameof(excp));
            }

            var answerPart = new StringBuilder();
            answerPart.AppendLine();
            answerPart.AppendLine(excp.Message);

            var serverMessage = TryReadServerErrorMessage(excp);
            if (!string.IsNullOrWhiteSpace(serverMessage)
                && !string.Equals(serverMessage, excp.Message, StringComparison.Ordinal))
            {
                answerPart.AppendLine();
                answerPart.AppendLine(serverMessage);
            }

            answerPart.AppendLine();
            answerPart.Append(excp.StackTrace);

            return await CreateOrAppendAnswerPartAsync(chatAnswer, answerPart.ToString());
        }

        /// <summary>
        /// The complaint the endpoint actually sent, when the failure came from a transport. The
        /// exception's own message is the status line - `Service request failed. Status: 400` - and
        /// the sentence naming what to fix lives in the body, which the transport has already
        /// unwrapped into <see cref="LlmTransportException.ServerMessage"/>.
        /// </summary>
        private static string? TryReadServerErrorMessage(
            Exception excp
            )
        {
            for (var current = excp; current is not null; current = current.InnerException)
            {
                if (current is LlmTransportException transportException
                    && !string.IsNullOrWhiteSpace(transportException.ServerMessage))
                {
                    return transportException.ServerMessage;
                }
            }

            return null;
        }

        /// <summary>Appends a piece of streamed text to the chat's answer, creating the answer content on the first call so the UI can update live.</summary>
        private async Task<AnswerChatContent> CreateOrAppendAnswerPartAsync(
            AnswerChatContent? chatAnswer,
            string answerPart
            )
        {
            if (chatAnswer is null)
            {
                chatAnswer = _chat.CreateAnswer();
            }

            await chatAnswer.AppendAsync(answerPart);

            return chatAnswer;
        }

    }
}
