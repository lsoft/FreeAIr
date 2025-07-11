using FreeAIr.BLogic.Context;
using FreeAIr.Helper;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using FreeAIr.Shared.Helper;
using OpenAI;
using OpenAI.Chat;
using System;
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
        private readonly List<UserPrompt> _prompts = new();

        private CancellationTokenSource _cancellationTokenSource = new();
        private Task? _task;

        private ChatStatusEnum _status;

        public AvailableToolContainer ChatTools
        {
            get;
        }

        public ChatContext ChatContext
        {
            get;
        }

        public ChatOptions Options
        {
            get;
        }

        public IReadOnlyList<UserPrompt> Prompts => _prompts;

        public event ChatStatusChangedDelegate ChatStatusChangedEvent;

        public TimeoutEventProxy<PromptEventArgs> PromptStateChangedEvent
        {
            get;
        }

        public ChatDescription Description
        {
            get;
        }

        public DateTime? Started
        {
            get;
            private set;
        }

        /// <summary>
        /// Status of the chat.
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

        private Chat(
            ChatContext chatContext,
            ChatDescription description,
            FreeAIr.BLogic.ChatOptions options,
            AvailableToolContainer chatTools
            )
        {
            if (chatContext is null)
            {
                throw new ArgumentNullException(nameof(chatContext));
            }

            if (description is null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ChatContext = chatContext;
            Description = description;
            Options = options;
            ChatTools = chatTools;
            _status = ChatStatusEnum.NotStarted;

            PromptStateChangedEvent = new TimeoutEventProxy<PromptEventArgs>(
                250,
                this,
                (pa0, pa1) =>
                {
                    if (pa0 is null && pa1 is null)
                    {
                        return ArgsActionKindEnum.ReplaceLastArgs;
                    }
                    if (pa0 is null)
                    {
                        return ArgsActionKindEnum.AddToQueue;
                    }
                    if (pa1 is null)
                    {
                        return ArgsActionKindEnum.ReplaceLastArgs;
                    }

                    if (pa0.Kind == pa1.Kind)
                    {
                        return ArgsActionKindEnum.ReplaceLastArgs;
                    }

                    return ArgsActionKindEnum.AddToQueue;
                }
                );

            Started = DateTime.Now;

            ChatContext.ChatContextChangedEvent += ChatContextChangedRaised;
        }

        public static async System.Threading.Tasks.Task<Chat> CreateChatAsync(
            ChatDescription description,
            FreeAIr.BLogic.ChatOptions options
            )
        {
            var chatContext = await ChatContext.CreateChatContextAsync();
            var chatTools = await AvailableToolContainer.ReadSystemAsync();

            var result = new Chat(
                chatContext,
                description,
                options,
                chatTools
                );

            return result;
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

            _task = Task.Run(async () => await AskPromptAndReceiveAnswerAsync());

            PromptStateChanged(userPrompt, PromptChangeKindEnum.PromptAdded);
        }

        public Task WaitForPromptResultAsync()
        {
            return WaitForTaskAsync();
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource?.Cancel();

            await WaitForTaskAsync();
            
            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void ArchiveAllPrompts()
        {
            _prompts.ForEach(p =>
            {
                p.Archive();

                PromptStateChanged(p, PromptChangeKindEnum.PromptArchived);
            });
        }

        public void Dispose()
        {
            Description.Dispose();

            PromptStateChangedEvent.Dispose();
        }

        private async Task WaitForTaskAsync()
        {
            if (_task is null)
            {
                return;
            }
            if (_task.IsCompleted || _task.IsCanceled || _task.IsFaulted)
            {
                    return;
            }
            await _task;
        }

        private void ChatContextChangedRaised(object sender, ChatContextEventArgs e)
        {
            StatusChanged();
        }

        private async Task AskPromptAndReceiveAnswerAsync()
        {
            var cancellationToken = _cancellationTokenSource.Token;

            var dialog = await Dialog.CreateAsync(
                this
                );

            try
            {
                Status = ChatStatusEnum.WaitingForAnswer;

                var chosenAgent = this.Options.ChosenAgent;
                var chatClient = new ChatClient(
                    model: chosenAgent.Technical.ChosenModel,
                    new ApiKeyCredential(
                        chosenAgent.Technical.GetToken()
                        ),
                    new OpenAIClientOptions
                    {
                        NetworkTimeout = TimeSpan.FromHours(1),
                        Endpoint = chosenAgent.Technical.TryBuildEndpointUri(),
                    }
                    );

                var cco = new ChatCompletionOptions
                {
                    ToolChoice = Options.ToolChoice,
                    ResponseFormat = Options.ResponseFormat,
                    MaxOutputTokenCount = (await FreeAIrOptions.DeserializeUnsortedAsync()).MaxOutputTokenCount,
                };

                var toolCollection = McpServerProxyCollection.GetTools(ChatTools);
                var activeTools = toolCollection.GetActiveToolList();
                foreach (var tool in activeTools)
                {
                    cco.Tools.Add(
                        tool.CreateChatTool()
                        );
                }

                var continueTalk = false;
                do
                {
                    continueTalk = false;

                    var completionUpdates = chatClient.CompleteChatStreaming(
                        messages: await dialog.GetMessageListAsync(),
                        options: cco,
                        cancellationToken: cancellationToken
                        );

                    OpenAI.Chat.ChatFinishReason? chatFinishReason = null;
                    var toolCalls = new List<StreamingChatToolCallUpdate>();
                    var contentParts = new List<ChatMessageContentPart>();

                    Status = ChatStatusEnum.ReadingAnswer;

                    foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                    {
                        //completionUpdate.Usage.OutputTokenDetails.

                        if (completionUpdate.CompletionId is null)
                        {
                            dialog.UpdateUserVisibleAnswer("Server returns error.");
                            Status = ChatStatusEnum.Failed;
                            return;
                        }

                        chatFinishReason ??= completionUpdate.FinishReason;
                        toolCalls.AddRange(completionUpdate.ToolCallUpdates);

                        if (completionUpdate.FinishReason == ChatFinishReason.ToolCalls
                            && completionUpdate.ToolCallUpdates.Count > 0
                            )
                        {
                            dialog.AppendAssistantReply(
                                completionUpdate.ToolCallUpdates.ConvertToChatTools()
                                );
                        }
                        else
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
                            dialog.UpdateUserVisibleAnswer(contentPart.Text);

                            if (cancellationToken.IsCancellationRequested)
                            {
                                Status = ChatStatusEnum.Ready;
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
                                dialog.AppendUnsuccessfulToolCall(
                                    toolCall
                                    );

                                throw new InvalidOperationException($"Tool named {toolCall.FunctionName} does not exists.");
                            }

                            dialog.AppendToolCallResult(
                                toolCall,
                                toolResult
                                );
                        }

                        continueTalk = true;
                    }
                    else
                    {
                        dialog.AppendAssistantReply(
                            contentParts
                            );
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
                dialog.AppendException(excp);

                Status = ChatStatusEnum.Failed;

                //todo
            }
        }

        private void PromptStateChanged(
            UserPrompt prompt,
            PromptChangeKindEnum kind
            )
        {
            PromptStateChangedEvent.Fire(
                new PromptEventArgs(this, prompt, kind)
                );
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
                    toolArguments.Add(pair.Name, pair.Value.DeserializeToObject());
                }
            }

            return toolArguments;
        }

        public sealed class Dialog
        {
            private readonly Chat _chat;
            private readonly SystemChatMessage _systemChatMessage;

            private UserPrompt _lastPrompt => _chat.Prompts.Last();

            private Dialog(
                Chat chat,
                string formattedSystemPrompt
                )
            {
                if (chat is null)
                {
                    throw new ArgumentNullException(nameof(chat));
                }

                if (formattedSystemPrompt is null)
                {
                    throw new ArgumentNullException(nameof(formattedSystemPrompt));
                }

                _chat = chat;
                _systemChatMessage = new SystemChatMessage(
                    formattedSystemPrompt
                    );
            }

            public void AppendAssistantReply(
                IReadOnlyList<ChatMessageContentPart> contents
                )
            {
                if (contents is null)
                {
                    throw new ArgumentNullException(nameof(contents));
                }

                if (contents.Count == 0)
                {
                    return;
                }


                var content = string.Join(
                    "",
                    contents
                        .Where(c => c.Kind == ChatMessageContentPartKind.Text)
                        .Select(c => c.Text)
                    );

                var assistantReply = new AssistantChatMessage(
                    content
                    );

                _lastPrompt.Answer.AddPermanentReaction(assistantReply);

                _chat.PromptStateChanged(_lastPrompt, PromptChangeKindEnum.AnswerUpdated);
            }


            public void AppendAssistantReply(
                IReadOnlyList<ChatToolCall> chatToolCalls
                )
            {
                if (chatToolCalls is null)
                {
                    throw new ArgumentNullException(nameof(chatToolCalls));
                }

                if (chatToolCalls.Count == 0)
                {
                    return;
                }

                _lastPrompt.Answer.AddPermanentReaction(
                    new AssistantChatMessage(
                        chatToolCalls
                        )
                    );

                _chat.PromptStateChanged(_lastPrompt, PromptChangeKindEnum.AnswerUpdated);
            }

            public void UpdateUserVisibleAnswer(string helperText)
            {
                if (string.IsNullOrEmpty(helperText))
                {
                    return;
                }

                _lastPrompt.Answer.UpdateUserVisibleAnswer(
                    helperText
                    );

                _chat.PromptStateChanged(_lastPrompt, PromptChangeKindEnum.AnswerUpdated);
            }

            public void AppendException(Exception excp)
            {
                _lastPrompt.Answer.UpdateUserVisibleAnswer(
                    $"Error: {excp.Message}"
                    );

                _chat.PromptStateChanged(_lastPrompt, PromptChangeKindEnum.AnswerUpdated);
            }


            public void AppendUnsuccessfulToolCall(
                StreamingChatToolCallUpdate toolCall
                )
            {
                _lastPrompt.Answer.UpdateUserVisibleAnswer(
                     Environment.NewLine + $"> Tool {toolCall.FunctionName} has been called and the tool is FAILED." + Environment.NewLine + Environment.NewLine
                    );

                _lastPrompt.Answer.AddPermanentReaction(
                    new ToolChatMessage(
                        toolCall.ToolCallId,
                        $"Tool {toolCall.FunctionName} FAILED to produce results."
                        )
                    );

                _chat.PromptStateChanged(_lastPrompt, PromptChangeKindEnum.AnswerUpdated);
            }

            public void AppendToolCallResult(
                StreamingChatToolCallUpdate toolCall,
                string[] toolResult
                )
            {
                _lastPrompt.Answer.UpdateUserVisibleAnswer(
                     Environment.NewLine + $"> Tool {toolCall.FunctionName} has been called SUCESSFULLY." + Environment.NewLine + Environment.NewLine
                    );

                _lastPrompt.Answer.AddPermanentReaction(
                    new ToolChatMessage(
                        toolCall.ToolCallId,
                        string.Join(
                            Environment.NewLine,
                            toolResult
                            )
                        )
                    );

                _chat.PromptStateChanged(_lastPrompt, PromptChangeKindEnum.AnswerUpdated);
            }

            public void ArchiveAllPrompts()
            {
                _chat.ArchiveAllPrompts();
            }

            public async Task<IReadOnlyList<OpenAI.Chat.ChatMessage>> GetMessageListAsync()
            {
                var result = new List<OpenAI.Chat.ChatMessage>();

                result.Add(_systemChatMessage);

                var nonArchivedPrompts = _chat.Prompts.FindAll(p => !p.IsArchived);

                foreach (var prompt in nonArchivedPrompts.Take(nonArchivedPrompts.Count - 1))
                {
                    FillMessageList(prompt, result);
                }

                foreach (var contextItem in _chat.ChatContext.Items)
                {
                    result.Add(await contextItem.CreateChatMessageAsync());
                }

                FillMessageList(nonArchivedPrompts.Last(), result);

                return result;
            }


            public static async Task<Dialog> CreateAsync(
                Chat chat
                )
            {
                var formattedSystemPrompt = await chat.Options.ChosenAgent.GetFormattedSystemPromptAsync();
                var dialog = new Dialog(
                    chat,
                    formattedSystemPrompt
                    );
                return dialog;
            }

            private static void FillMessageList(
                UserPrompt prompt,
                List<OpenAI.Chat.ChatMessage> result
                )
            {
                if (result is null)
                {
                    throw new ArgumentNullException(nameof(result));
                }

                //put prompt
                var chatMessage = prompt.CreateChatMessage();
                result.Add(chatMessage);

                //put answers (assistant + tool)
                foreach (var reaction in prompt.Answer.Reactions)
                {
                    result.Add(reaction);
                }
            }

        }

    }


    public delegate void ChatStatusChangedDelegate(object sender, ChatEventArgs e);
    public delegate void PromptStateChangedDelegate(object sender, PromptEventArgs e);

    public sealed class PromptEventArgs : EventArgs
    {
        public Chat Chat
        {
            get;
        }

        public UserPrompt Prompt
        {
            get;
        }

        public PromptChangeKindEnum Kind
        {
            get;
        }

        public PromptEventArgs(Chat chat, UserPrompt prompt, PromptChangeKindEnum kind)
        {
            Chat = chat;
            Prompt = prompt;
            Kind = kind;
        }
    }

    public enum PromptChangeKindEnum
    {
        PromptAdded,
        AnswerUpdated,
        PromptArchived
    }

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


    public sealed class ChatOptions
    {
        public static async Task<ChatOptions> GetDefaultAsync(AgentJson? chosenAgent) =>
            new ChatOptions(
                ChatToolChoice.CreateAutoChoice(),
                await FreeAIrOptions.DeserializeAgentCollectionAsync(),
                chosenAgent,
                OpenAI.Chat.ChatResponseFormat.CreateTextFormat(),
                false
                );

        public static async Task<ChatOptions> NoToolAutoProcessedTextResponseAsync(
            AgentJson? chosenAgent
            )
        {
            var agentCollection = await FreeAIrOptions.DeserializeAgentCollectionAsync();

            return new ChatOptions(
                ChatToolChoice.CreateNoneChoice(),
                agentCollection,
                chosenAgent,
                OpenAI.Chat.ChatResponseFormat.CreateTextFormat(),
                true
                );
        }

        public static async Task<ChatOptions> NoToolAutoProcessedJsonResponseAsync(
            AgentJson? chosenAgent
            )
        {
            var agentCollection = await FreeAIrOptions.DeserializeAgentCollectionAsync();

            return new ChatOptions(
                ChatToolChoice.CreateNoneChoice(),
                agentCollection,
                chosenAgent,
                OpenAI.Chat.ChatResponseFormat.CreateJsonObjectFormat(),
                true
                );
        }

        public ChatToolChoice ToolChoice
        {
            get;
        }

        public AgentCollectionJson ChatAgents
        {
            get;
        }

        public AgentJson ChosenAgent
        {
            get;
            private set;
        }

        public OpenAI.Chat.ChatResponseFormat ResponseFormat
        {
            get;
        }

        public bool AutomaticallyProcessed
        {
            get;
        }

        private ChatOptions(
            ChatToolChoice toolChoice,
            AgentCollectionJson chatAgents,
            AgentJson? chosenAgent,
            OpenAI.Chat.ChatResponseFormat responseFormat,
            bool automaticallyProcessed
            )
        {
            if (toolChoice is null)
            {
                throw new ArgumentNullException(nameof(toolChoice));
            }

            if (chatAgents is null)
            {
                throw new ArgumentNullException(nameof(chatAgents));
            }

            if (responseFormat is null)
            {
                throw new ArgumentNullException(nameof(responseFormat));
            }

            ToolChoice = toolChoice;
            ChatAgents = chatAgents;
            ChosenAgent = chosenAgent ?? chatAgents.Agents[0];
            ResponseFormat = responseFormat;
            AutomaticallyProcessed = automaticallyProcessed;
        }

        public void ChangeChosenAgent(AgentJson chosenAgent)
        {
            if (chosenAgent is null)
            {
                throw new ArgumentNullException(nameof(chosenAgent));
            }

            ChosenAgent = chosenAgent;
        }
    }
}
