﻿using FreeAIr.Agents;
using FreeAIr.BLogic.Context;
using FreeAIr.Helper;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.Shared.Helper;
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
        public event PromptStateChangedDelegate PromptStateChangedEvent;

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
            FreeAIr.BLogic.ChatOptions options
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
            _status = ChatStatusEnum.NotStarted;

            ChatTools = AvailableToolContainer.ReadSystem();

            Started = DateTime.Now;

            ResultFilePath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString() + ".md"
                );

            ChatContext.ChatContextChangedEvent += ChatContextChangedRaised;
        }

        public static async System.Threading.Tasks.Task<Chat> CreateChatAsync(
            ChatDescription description,
            FreeAIr.BLogic.ChatOptions options
            )
        {
            var chatContext = await ChatContext.CreateChatContextAsync();

            var result = new Chat(
                chatContext,
                description,
                options
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
            DeleteResultFile();

            Description.Dispose();
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

            var dialog = new Dialog(
                ResultFilePath,
                this
                );
            dialog.Init();

            try
            {
                Status = ChatStatusEnum.WaitingForAnswer;

                var activeAgent = this.Options.ChatAgents.GetActiveAgent();
                var chatClient = new ChatClient(
                    model: activeAgent.Technical.ChosenModel,
                    new ApiKeyCredential(
                        activeAgent.Technical.Token
                        ),
                    new OpenAIClientOptions
                    {
                        NetworkTimeout = TimeSpan.FromHours(1),
                        Endpoint = activeAgent.Technical.TryBuildEndpointUri(),
                    }
                    );

                var cco = new ChatCompletionOptions
                {
                    ToolChoice = Options.ToolChoice,
                    ResponseFormat = Options.ResponseFormat,
                    MaxOutputTokenCount = ResponsePage.Instance.MaxOutputTokenCount,
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

        private void PromptStateChanged(
            UserPrompt prompt,
            PromptChangeKindEnum kind
            )
        {
            var e = PromptStateChangedEvent;
            if (e is not null)
            {
                e(this, new PromptEventArgs(this, prompt, kind));
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

        //private static Dictionary<string, object?> ParseToolInvocationArguments(
        //    ChatToolCall toolCall
        //    )
        //{
        //    var toolArguments = new Dictionary<string, object?>();
        //    if (toolCall.FunctionArguments.ToMemory().Length > 0)
        //    {
        //        using JsonDocument toolArgumentJson = JsonDocument.Parse(
        //            toolCall.FunctionArguments
        //            );

        //        foreach (var pair in toolArgumentJson.RootElement.EnumerateObject())
        //        {
        //            toolArguments.Add(pair.Name, pair.Value.DeserializeToObject());
        //        }
        //    }

        //    return toolArguments;
        //}

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
            private readonly string _resultFilePath;
            private readonly Chat _chat;
            private readonly SystemChatMessage _systemChatMessage;

            private UserPrompt _lastPrompt => _chat.Prompts.Last();

            public Dialog(
                string resultFilePath,
                Chat chat
                )
            {
                if (resultFilePath is null)
                {
                    throw new ArgumentNullException(nameof(resultFilePath));
                }

                if (chat is null)
                {
                    throw new ArgumentNullException(nameof(chat));
                }

                _resultFilePath = resultFilePath;
                _chat = chat;
                _systemChatMessage = new SystemChatMessage(
                    chat.Options.GetActiveAgent().GetFormattedSystemPrompt()
                    );
            }

            public void Init()
            {
                AppendTextToFile(
                    Environment.NewLine + Environment.NewLine
                    );
                AppendTextToFile(
                    $"> {"UI_Prompt".GetLocalizedResourceByName()}:" + Environment.NewLine
                    );
                AppendTextToFile(
                    _lastPrompt.PromptBody + Environment.NewLine + Environment.NewLine
                    );
                AppendTextToFile(
                    $"> {"UI_Answer".GetLocalizedResourceByName()}:" + Environment.NewLine
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

            public void AppendTextToFile(string helperText)
            {
                if (string.IsNullOrEmpty(helperText))
                {
                    return;
                }

                AppendToFile(
                    helperText
                    );
            }

            public void UpdateUserVisibleAnswer(string helperText)
            {
                if (string.IsNullOrEmpty(helperText))
                {
                    return;
                }

                AppendTextToFile(helperText);

                _lastPrompt.Answer.UpdateUserVisibleAnswer(
                    helperText
                    );

                _chat.PromptStateChanged(_lastPrompt, PromptChangeKindEnum.AnswerUpdated);
            }

            public void AppendException(Exception excp)
            {
                AppendToFile(
                    excp.Message
                    + Environment.NewLine
                    + "```"
                    + Environment.NewLine
                    + excp.StackTrace
                    + Environment.NewLine
                    + "```"
                    );

                _lastPrompt.Answer.UpdateUserVisibleAnswer(
                    $"Error: {excp.Message}"
                    );

                _chat.PromptStateChanged(_lastPrompt, PromptChangeKindEnum.AnswerUpdated);
            }


            public void AppendUnsuccessfulToolCall(
                StreamingChatToolCallUpdate toolCall
                )
            {
                AppendToFile(
                    Environment.NewLine
                    + "<ToolCall>"
                    + Environment.NewLine
                    + toolCall.FunctionName
                    + Environment.NewLine
                    + (toolCall.FunctionArgumentsUpdate.ToMemory().Length > 0
                        ? toolCall.FunctionArgumentsUpdate.ToString()
                        : string.Empty)
                    + Environment.NewLine
                    + "</ToolCall>"
                    + Environment.NewLine
                    );

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
                AppendToFile(
                    Environment.NewLine
                    + "<ToolCall>"
                    + Environment.NewLine
                    + toolCall.FunctionName
                    + Environment.NewLine
                    + (toolCall.FunctionArgumentsUpdate.ToMemory().Length > 0
                        ? toolCall.FunctionArgumentsUpdate.ToString()
                        : string.Empty)
                    + Environment.NewLine
                    + Environment.NewLine
                    + string.Join(
                        Environment.NewLine,
                        toolResult
                        )
                    + Environment.NewLine
                    + "</ToolCall>"
                    + Environment.NewLine
                    );

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

            private void AppendToFile(string contentPart)
            {
                File.AppendAllText(_resultFilePath, contentPart);
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
        public static ChatOptions Default => new ChatOptions(
            ChatToolChoice.CreateAutoChoice(),
            InternalPage.Instance.GetAgentCollection(),
            OpenAI.Chat.ChatResponseFormat.CreateTextFormat(),
            false
            );

        public static ChatOptions NoToolAutoProcessedTextResponse(
            Agent? defaultAgent = null
            )
        {
            var agentCollection = InternalPage.Instance.GetAgentCollection();
            if (defaultAgent is not null)
            {
                agentCollection.SetDefaultAgent(defaultAgent);
            }

            return new ChatOptions(
                ChatToolChoice.CreateNoneChoice(),
                agentCollection,
                OpenAI.Chat.ChatResponseFormat.CreateTextFormat(),
                true
                );
        }

        public static ChatOptions NoToolAutoProcessedJsonResponse(
            Agent? defaultAgent = null
            )
        {
            var agentCollection = InternalPage.Instance.GetAgentCollection();
            if (defaultAgent is not null)
            {
                agentCollection.SetDefaultAgent(defaultAgent);
            }

            return new ChatOptions(
                ChatToolChoice.CreateNoneChoice(),
                agentCollection,
                OpenAI.Chat.ChatResponseFormat.CreateJsonObjectFormat(),
                true
                );
        }

        public ChatToolChoice ToolChoice
        {
            get;
        }

        public AgentCollection ChatAgents
        {
            get;
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
            AgentCollection chatAgents,
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
            ResponseFormat = responseFormat;
            AutomaticallyProcessed = automaticallyProcessed;
        }

        public Agent GetActiveAgent()
        {
            return ChatAgents.GetActiveAgent();
        }
    }
}
