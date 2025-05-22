using FreeAIr.BLogic.Context;
using FreeAIr.Helper;
using FreeAIr.MCP.Agent;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    public sealed class Chat : IDisposable
    {
        private readonly ChatClient _chatClient;
        private readonly List<UserPrompt> _prompts = new();
        private readonly ChatContext _chatContext = new();

        private readonly Action<Chat, Answer> _promptAnsweredCallBack;

        private CancellationTokenSource _cancellationTokenSource = new();
        private Task? _task;

        public IChatContext ChatContext => _chatContext;

        private ChatStatusEnum _status;

        public IReadOnlyList<IUserPrompt> Prompts => _prompts;

        public event ChatStatusChangedDelegate ChatStatusChangedEvent;

        public ChatDescription Description
        {
            get;
        }

        public string ResultFilePath
        {
            get;
        }

        public DateTime? Started
        {
            get;
            private set;
        }

        /// <summary>
        /// Status of last prompt in the chat = whole chat status.
        /// </summary>
        public ChatStatusEnum Status
        {
            get => _status;
            private set
            {
                _status = value;

                StatusChanged();
            }
        }

        public Chat(
            ChatDescription description,
            Action<Chat, Answer> promptAnsweredCallBack = null
            )
        {
            if (description is null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            Description = description;
            _promptAnsweredCallBack = promptAnsweredCallBack;

            _status = ChatStatusEnum.NotStarted;

            Started = DateTime.Now;

            ResultFilePath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString() + ".md"
                );

            _chatClient = new ChatClient(
                model: ApiPage.Instance.ChosenModel,
                new ApiKeyCredential(
                    ApiPage.Instance.Token
                    ),
                new OpenAIClientOptions
                {
                    NetworkTimeout = TimeSpan.FromHours(1),
                    Endpoint = ApiPage.Instance.TryBuildEndpointUri(),
                }
                );

            _chatContext.ChatContextChangedEvent += ChatContextChangedRaised;
        }

        public void AddPrompt(
            UserPrompt userPrompt
            )
        {
            if (userPrompt is null)
            {
                throw new ArgumentNullException(nameof(userPrompt));
            }

            _prompts.Add(userPrompt);

            _task = Task.Run(async () => await AskPromptAndReceiveAnswerAsync(userPrompt));
        }

        public Task WaitForPromptResultAsync()
        {
            return WaitForTaskAsync();
        }

        public string ReadResponse()
        {
            if (File.Exists(ResultFilePath))
            {
                return ReadResponseFile();
            }

            return "Chat is not started yet.";
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource?.Cancel();

            await WaitForTaskAsync();
            
            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            DeleteResultFile();

            Description.Dispose();
        }

        private async Task WaitForTaskAsync()
        {
            if (_task is null)
            {
                return;
            }
            if (_task.IsCompleted)
            {
                return;
            }
            await _task;
        }

        private void ChatContextChangedRaised(object sender, ChatContextEventArgs e)
        {
            StatusChanged();
        }

        private async Task AskPromptAndReceiveAnswerAsync(UserPrompt userPrompt)
        {
            var answer = await AskAndWaitForAnswerInternalAsync(userPrompt);

            userPrompt.SetAnswer(answer.GetAnswer());

            var e = _promptAnsweredCallBack;
            if (e is not null)
            {
                e(this, answer);
            }
        }

        private async Task<Answer> AskAndWaitForAnswerInternalAsync(UserPrompt userPrompt)
        {
            var cancellationToken = _cancellationTokenSource.Token;

            var answer = new Answer(ResultFilePath);

            try
            {
                File.AppendAllText(
                    ResultFilePath,
                    Environment.NewLine + Environment.NewLine
                    );
                File.AppendAllText(
                    ResultFilePath,
                    $"> {"UI_Prompt".GetLocalizedResourceByName()}:" + Environment.NewLine
                    );
                File.AppendAllText(
                    ResultFilePath,
                    _prompts.Last().PromptBody + Environment.NewLine + Environment.NewLine
                    );
                File.AppendAllText(
                    ResultFilePath,
                    $"> {"UI_Answer".GetLocalizedResourceByName()}:" + Environment.NewLine
                    );

                Status = ChatStatusEnum.WaitingForAnswer;

                var chatMessages = new List<ChatMessage>(_prompts.Count + 1);
                chatMessages.Add(
                    new SystemChatMessage(userPrompt.BuildRulesSection())
                    );

                foreach (var prompt in _prompts.Take(_prompts.Count - 1))
                {
                    chatMessages.Add(
                        new UserChatMessage(
                            ChatMessageContentPart.CreateTextPart(prompt.PromptBody)
                            )
                        );
                }

                var lastPromptParts = new List<ChatMessageContentPart>(_chatContext.Items.Count + 1);
                foreach (var contextItem in _chatContext.Items)
                {
                    lastPromptParts.Add(
                        ChatMessageContentPart.CreateTextPart(
                            await contextItem.AsContextPromptTextAsync()
                            )
                        );
                }

                var lastUserPrompt = _prompts.Last();
                var lastUserPromptPart = ChatMessageContentPart.CreateTextPart(lastUserPrompt.PromptBody);
                lastPromptParts.Insert(0, lastUserPromptPart);
                chatMessages.Add(
                    new UserChatMessage(
                        lastPromptParts
                        )
                    );

                var toolCollection = AgentCollection.GetTools();
                var activeTools = toolCollection.GetActiveToolList();

                var cco = new ChatCompletionOptions
                {
                    //ResponseFormat = ChatResponseFormat.CreateTextFormat(),
                    MaxOutputTokenCount = 4096,
                };
                foreach (var tool in activeTools)
                {
                    cco.Tools.Add(
                        tool.CreateChatTools()
                        );
                }

                var continueTalk = false;
                do
                {
                    continueTalk = false;

                    var completionUpdates = _chatClient.CompleteChatStreaming(
                        messages: chatMessages,
                        options: cco,
                        cancellationToken: cancellationToken
                        );

                    ChatFinishReason? chatFinishReason = null;
                    var toolCalls = new List<StreamingChatToolCallUpdate>();

                    foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                    {
                        if (completionUpdate.CompletionId is null)
                        {
                            answer.Append("Error reading answer (out of limit for free account?).");
                            Status = ChatStatusEnum.Failed;
                            return answer;
                        }

                        chatFinishReason ??= completionUpdate.FinishReason;
                        toolCalls.AddRange(completionUpdate.ToolCallUpdates);

                        foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                        {
                            if (string.IsNullOrEmpty(contentPart.Text))
                            {
                                continue;
                            }

                            answer.Append(contentPart.Text);

                            Status = ChatStatusEnum.ReadingAnswer;

                            if (cancellationToken.IsCancellationRequested)
                            {
                                Status = ChatStatusEnum.Ready;
                                return answer;
                            }
                        }
                    }

                    if (chatFinishReason == ChatFinishReason.ToolCalls && toolCalls.Count > 0)
                    {
                        foreach (var toolCall in toolCalls)
                        {
                            File.AppendAllText(
                                ResultFilePath,
                                Environment.NewLine
                                + "<ToolCall>"
                                + Environment.NewLine
                                + toolCall.FunctionName
                                + Environment.NewLine
                                + "</ToolCall>"
                                + Environment.NewLine
                                );

                            var toolArguments = ParseToolInvocationArguments(toolCall);

                            var toolResult = await AgentCollection.CallToolAsync(
                                toolCall.FunctionName,
                                toolArguments,
                                cancellationToken: CancellationToken.None
                                );
                            if (toolResult is not null)
                            {
                                chatMessages.Add(
                                    new ToolChatMessage(
                                        toolCall.ToolCallId,
                                        string.Join(
                                            Environment.NewLine,
                                            toolResult
                                            )
                                        )
                                    );
                            }
                        }

                        continueTalk = true;
                    }
                }
                while (continueTalk);

                Status = ChatStatusEnum.Ready;
            }
            catch (OperationCanceledException)
            {
                //this is OK
                Status = ChatStatusEnum.Ready;
            }
            catch (Exception excp)
            {
                answer.Append(excp.Message + Environment.NewLine + "```" + Environment.NewLine + excp.StackTrace + Environment.NewLine + "```");

                Status = ChatStatusEnum.Failed;

                //todo
            }

            return answer;
        }

        private string ReadResponseFile()
        {
            using var fs = new FileStream(
                ResultFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
                );

            using var sr = new StreamReader(fs);

            return sr.ReadToEnd();
        }

        private void DeleteResultFile()
        {
            if (File.Exists(ResultFilePath))
            {
                File.Delete(ResultFilePath);
            }
        }

        private void StatusChanged()
        {
            var e = ChatStatusChangedEvent;
            if (e is not null)
            {
                e(this, new ChatEventArgs(this));
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
                    toolArguments.Add(pair.Name, pair.Value.GetRawText());
                }
            }

            return toolArguments;
        }

    }

    public delegate void ChatStatusChangedDelegate(object sender, ChatEventArgs e);

    public sealed class ChatEventArgs : EventArgs
    {
        public Chat Chat
        {
            get;
        }

        public ChatEventArgs(Chat chat)
        {
            Chat = chat;
        }
    }
}
