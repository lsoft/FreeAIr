using FreeAIr.BLogic;
using FreeAIr.BLogic.Content;
using FreeAIr.Helper;
using FreeAIr.MCP.McpServerProxy;
using Microsoft.VisualStudio.Threading;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.UI.BLogic.Reader
{
    public sealed class LLMReader : IDisposable
    {
        private readonly object _taskLocker = new();

        private readonly FreeAIr.BLogic.Chat _chat;

        private CancellationTokenSource _cancellationTokenSource = new();

        private Task? _task;

        public LLMReader(
            FreeAIr.BLogic.Chat chat
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            _chat = chat;
        }

        public void AsyncStartRead(
            )
        {
            lock (_taskLocker)
            {
                if (_task is not null)
                {
                    return;
                }

                _task = ReadSafelyAsync();
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

        public async Task StopSafelyAsync()
        {
            try
            {
                Task? task = null;
                lock (_taskLocker)
                {
                    task = _task;
                    _task = null;
                }

                if (task is null)
                {
                    return;
                }

                _cancellationTokenSource.Cancel();

                await task;

                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
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
            )
        {
            try
            {
                await ReadSafelyPrivateAsync();
            }
            finally
            {
                lock (_taskLocker)
                {
                    _task = null;
                }
            }
        }

        private async Task ReadSafelyPrivateAsync(
            )
        {
            await TaskScheduler.Default;

            AnswerChatContent? chatAnswer = null;

            try
            {
                _chat.Status = ChatStatusEnum.WaitingForAnswer;

                var cancellationToken = _cancellationTokenSource.Token;

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
                var contentParts = new List<ChatMessageContentPart>();

                _chat.Status = ChatStatusEnum.ReadingAnswer;

                foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    //completionUpdate.Usage.OutputTokenDetails.

                    if (completionUpdate.CompletionId is null)
                    {
                        chatAnswer = await CreateOrAppendAnswerPartAsync(chatAnswer, "Server returns error.");
                        _chat.Status = ChatStatusEnum.Failed;
                        return;
                    }

                    chatFinishReason ??= completionUpdate.FinishReason;
                    toolCalls.AddRange(completionUpdate.ToolCallUpdates);

                    if (completionUpdate.FinishReason != ChatFinishReason.ToolCalls
                        || completionUpdate.ToolCallUpdates.Count == 0
                        )
                    {
                        if (completionUpdate.ContentUpdate.Count > 0)
                        {
                            contentParts.AddRange(completionUpdate.ContentUpdate);
                        }
                    }

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
