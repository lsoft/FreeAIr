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
        /// The text appended is not the text delta but whatever <see cref="AnswerTextAssembler"/>
        /// makes of the event, which is how the reasoning of a thinking model reaches the chat: as
        /// a `think` block the renderer collapses, and only when the agent asks for it.
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

            //outside the try because a turn which fails while the model is still reasoning has to
            //close the block before the exception is appended, or the message lands inside it and
            //the user sees an answer which stops mid-thought
            var answerTextAssembler = new AnswerTextAssembler(
                _chat.Options.ChosenAgent.Technical.ShowReasoning
                );

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

                    //the answer text of this event - the delta itself, plus the think tags opened
                    //and closed around a run of reasoning - and nothing at all for most events
                    var answerPart = answerTextAssembler.Append(streamEvent);
                    if (answerPart.Length > 0)
                    {
                        //for updating UI in real time
                        chatAnswer = await CreateOrAppendAnswerPartAsync(chatAnswer, answerPart);
                    }

                    switch (streamEvent)
                    {
                        case LlmTextDeltaEvent:
                        case LlmReasoningDeltaEvent:
                            if (cancellationToken.IsCancellationRequested)
                            {
                                //stopped while the model was still thinking: the block has to be
                                //closed here too, or what is left of it is sent back as history on
                                //the next turn, where nothing recognises it any more
                                chatAnswer = await CloseReasoningAsync(chatAnswer, answerTextAssembler);

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
                            //sit waiting. It never becomes an exception, so this is the only place
                            //it can reach the log - and a chat which "just stopped" is exactly the
                            //report this has to be diagnosable from
                            ActivityLogHelper.ActivityLogWarning(
                                DescribeTurn() + " failed while streaming: " + fault.Message
                                );

                            chatAnswer = await CreateOrAppendAnswerPartAsync(chatAnswer, fault.Message);
                            _chat.Status = ChatStatusEnum.Failed;
                            return;
                    }
                }

                //a turn which ends in a tool call says nothing after its reasoning, so the end of
                //the stream is the only thing left to close the block
                chatAnswer = await CloseReasoningAsync(chatAnswer, answerTextAssembler);

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
                chatAnswer = await CloseReasoningAsync(chatAnswer, answerTextAssembler);

                _chat.Status = ChatStatusEnum.Ready;
            }
            catch (Exception excp)
            {
                //close whatever the model was still thinking about, so the failure is shown next to
                //the answer and not folded into the collapsed block above it
                chatAnswer = await CloseReasoningAsync(chatAnswer, answerTextAssembler);

                chatAnswer = await CreateOrAppendAnswerPartAsync(chatAnswer, excp);

                _chat.Status = ChatStatusEnum.Failed;

                //the exception alone does not say which agent was answering, and a report of "the
                //chat stopped working" is otherwise indistinguishable between agents
                excp.ActivityLogException(DescribeTurn() + " failed.");

                //the sentence naming what to fix is a property of the transport's exception rather
                //than part of its message, so ActivityLogException would not print it
                var serverMessage = TryReadServerErrorMessage(excp);
                if (!string.IsNullOrWhiteSpace(serverMessage))
                {
                    ActivityLogHelper.ActivityLogError("The endpoint said: " + serverMessage);
                }
            }
        }

        /// <summary>
        /// Names the agent, the protocol, the endpoint and the model of the turn, for the log line
        /// that accompanies a failure. This triple being wrong for itself - an agent pointed at
        /// Anthropic while still set to speak the OpenAI protocol, above all - is the likeliest
        /// cause of a request being refused, and none of it appears in an HTTP status.
        /// </summary>
        private string DescribeTurn()
        {
            try
            {
                var agent = _chat.Options.ChosenAgent;

                return
                    $"Chat {_chat.Id}, agent '{agent.Name}' ({agent.Technical.ApiProtocol} at "
                    + $"{agent.Technical.Endpoint}, model '{agent.Technical.ChosenModel}')";
            }
            catch (Exception)
            {
                //a description is not worth an exception of its own, least of all inside a catch
                return $"Chat {_chat.Id}";
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

        /// <summary>
        /// Closes a reasoning block the turn ended in the middle of, and leaves the answer alone
        /// when there is none.
        ///
        /// Every way out of the loop needs this — the end of the stream, a stop, a cancellation, a
        /// failure — because an answer left holding an unclosed `think` tag hides everything
        /// appended after it, and is no longer recognised as reasoning when the chat is replayed as
        /// history.
        /// </summary>
        private async Task<AnswerChatContent?> CloseReasoningAsync(
            AnswerChatContent? chatAnswer,
            AnswerTextAssembler answerTextAssembler
            )
        {
            var reasoningTail = answerTextAssembler.Flush();
            if (reasoningTail.Length == 0)
            {
                return chatAnswer;
            }

            return await CreateOrAppendAnswerPartAsync(chatAnswer, reasoningTail);
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
