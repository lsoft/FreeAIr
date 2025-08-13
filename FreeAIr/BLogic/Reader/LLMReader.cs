using FreeAIr.BLogic;
using FreeAIr.BLogic.Content;
using FreeAIr.Helper;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.UI.Dialog;
using FreeAIr.UI.Dialog.Content;
using FreeAIr.UI.ViewModels;
using MarkdownParser.Antlr.Answer;
using Microsoft.VisualStudio.Threading;
using OpenAI.Chat;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.UI.BLogic.Reader
{
    public sealed class LLMReader : IDisposable
    {
        private readonly object _taskLocker = new();

        private readonly SemaphoreSlim _semaphore = new (1);

        private readonly FreeAIr.BLogic.Chat _chat;
        private readonly DialogViewModel _dialog;
        private readonly AdditionalCommandContainer _additionalCommandContainer;

        private CancellationTokenSource _cancellationTokenSource = new();

        private AnswerDialogContent? _dialogAnswer;
        private Task? _task;

        public LLMReader(
            FreeAIr.BLogic.Chat chat,
            DialogViewModel dialog,
            AdditionalCommandContainer? additionalCommandContainer
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            if (dialog is null)
            {
                throw new ArgumentNullException(nameof(dialog));
            }

            _chat = chat;
            _dialog = dialog;
            _additionalCommandContainer = additionalCommandContainer;
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
            _cancellationTokenSource.Dispose();
            _semaphore.Dispose();
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

            try
            {
                _chat.Status = ChatStatusEnum.WaitingForAnswer;

                var cancellationToken = _cancellationTokenSource.Token;

                var chatClient = _chat.CreateChatClient();
                var chatCompletionOptions = await _chat.CreateChatCompletionOptionsAsync();




                var completionUpdates = chatClient.CompleteChatStreaming(
                    messages: await _chat.GetMessageListAsync(),
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
                        await AddOrAppendAnswerAsync("Server returns error.");
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
                        await AddOrAppendAnswerAsync(contentPart.Text);

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
                        var toolArguments = ParseToolInvocationArguments(toolCall);

                        var toolResult = await McpServerProxyCollection.CallToolAsync(
                            toolCall.FunctionName,
                            toolArguments,
                            cancellationToken: CancellationToken.None
                            );
                        if (toolResult is null)
                        {
                            //dialog.AppendUnsuccessfulToolCall(
                            //    toolCall
                            //    );

                            throw new InvalidOperationException($"Tool named {toolCall.FunctionName} failed to run.");
                        }

                        //dialog.AppendToolCallResult(
                        //    toolCall,
                        //    toolResult
                        //    );
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
                await AddOrAppendAnswerAsync(excp);

                _chat.Status = ChatStatusEnum.Failed;

                excp.ActivityLogException();
            }
        }

        private async Task AddOrAppendAnswerAsync(Exception excp)
        {
            if (excp is null)
            {
                throw new ArgumentNullException(nameof(excp));
            }

            var answerPart =
                excp.Message
                + Environment.NewLine
                + excp.StackTrace
                ;

            await AddOrAppendAnswerAsync(answerPart);
        }

        //private async Task AddOrAppendAnswerAsync(
        //    List<ChatMessageContentPart> contentParts
        //    )
        //{
        //    if (contentParts is null)
        //    {
        //        throw new ArgumentNullException(nameof(contentParts));
        //    }

        //    if (contentParts.Count == 0)
        //    {
        //        return;
        //    }

        //    var answerPart = string.Join("", contentParts.Select(c => c.Text));
        //    await AddOrAppendAnswerAsync(answerPart);
        //}

        private async Task AddOrAppendAnswerAsync(
            string answerPart
            )
        {
            await _semaphore.WaitAsync();
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_dialogAnswer is null)
                {
                    var chatAnswer = _chat.CreateAnswer();

                    _dialogAnswer = AnswerDialogContent.Create(
                        chatAnswer,
                        _additionalCommandContainer,
                        true
                        );

                    _dialog.AddContent(_dialogAnswer);
                }

                _dialogAnswer.TypedContent.Append(answerPart);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static Dictionary<string, object?> ParseToolInvocationArguments(
            StreamingChatToolCallUpdate toolCall
            )
        {
            var toolArguments = new Dictionary<string, object?>();
            if (toolCall.FunctionArgumentsUpdate.ToMemory().Length > 0)
            {
                using JsonDocument toolArgumentJson = JsonDocument.Parse(
                    toolCall.FunctionArgumentsUpdate
                    );

                foreach (var pair in toolArgumentJson.RootElement.EnumerateObject())
                {
                    toolArguments.Add(pair.Name, pair.Value.DeserializeToObject());
                }
            }

            return toolArguments;
        }

    }
}
