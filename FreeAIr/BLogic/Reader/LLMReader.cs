using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using OpenAI.Chat;
using System.Collections.Generic;
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
        private readonly object _taskLocker = new();

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

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }

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

                var chatClient = _chat.CreateChatClient();
                var chatCompletionOptions = await _chat.CreateChatCompletionOptionsAsync();



                var messages = await _chat.GetMessageListAsync();

                var completionUpdates = chatClient.CompleteChatStreaming(
                    messages: messages,
                    options: chatCompletionOptions,
                    cancellationToken: cancellationToken
                    );

                OpenAI.Chat.ChatFinishReason? chatFinishReason = null;
                var toolCalls = new List<StreamingChatToolCallUpdate>();
                //var contentParts = new List<ChatMessageContentPart>();

                _chat.Status = ChatStatusEnum.ReadingAnswer;

                foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    //completionUpdate.Usage.OutputTokenDetails.

                    //a chunk without a completion id means the endpoint replied with
                    //something which is not a completion at all (an error page, a quota
                    //message, etc.); there is nothing to read further
                    if (completionUpdate.CompletionId is null)
                    {
                        chatAnswer = await CreateOrAppendAnswerPartAsync(chatAnswer, "Server returns error.");
                        _chat.Status = ChatStatusEnum.Failed;
                        return;
                    }

                    chatFinishReason ??= completionUpdate.FinishReason;
                    toolCalls.AddRange(completionUpdate.ToolCallUpdates);

                    //if (completionUpdate.FinishReason != ChatFinishReason.ToolCalls
                    //    || completionUpdate.ToolCallUpdates.Count == 0
                    //    )
                    //{
                    //    if (completionUpdate.ContentUpdate.Count > 0)
                    //    {
                    //        contentParts.AddRange(completionUpdate.ContentUpdate);
                    //    }
                    //}

                    foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                    {
                        if (contentPart.Kind != ChatMessageContentPartKind.Text)
                        {
                            continue;
                        }
                        if (string.IsNullOrEmpty(contentPart.Text))
                        {
                            continue;
                        }

                        //for updating UI in real time
                        chatAnswer = await CreateOrAppendAnswerPartAsync(chatAnswer, contentPart.Text);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            _chat.Status = ChatStatusEnum.Ready;
                            return;
                        }
                    }
                }

                if (chatFinishReason == ChatFinishReason.ToolCalls && toolCalls.Count > 0)
                {
                    foreach (var toolCall in toolCalls)
                    {
                        if (string.IsNullOrEmpty(toolCall.FunctionName))
                        {
                            continue;
                        }

                        var toolCallContent = _chat.CreateToolCall(
                            toolCall
                            );


                        //var toolArguments = ParseToolInvocationArguments(toolCall);

                        //var toolResult = await McpServerProxyCollection.CallToolAsync(
                        //    toolCall.FunctionName,
                        //    toolArguments,
                        //    cancellationToken: CancellationToken.None
                        //    );
                        //if (toolResult is null)
                        //{
                        //    //dialog.AppendUnsuccessfulToolCall(
                        //    //    toolCall
                        //    //    );

                        //    throw new InvalidOperationException($"Tool named {toolCall.FunctionName} failed to run.");
                        //}

                        ////dialog.AppendToolCallResult(
                        ////    toolCall,
                        ////    toolResult
                        ////    );
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

        private async Task<AnswerChatContent> CreateOrAppendAnswerPartAsync(
            AnswerChatContent? chatAnswer,
            Exception excp
            )
        {
            if (excp is null)
            {
                throw new ArgumentNullException(nameof(excp));
            }

            var answerPart =
                Environment.NewLine
                + excp.Message
                + Environment.NewLine
                + excp.StackTrace
                ;

            return await CreateOrAppendAnswerPartAsync(chatAnswer, answerPart);
        }


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
