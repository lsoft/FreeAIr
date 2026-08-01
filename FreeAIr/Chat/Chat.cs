using FreeAIr.MCP.McpServerProxy;
using FreeAIr.Options2;
using FreeAIr.Shared.Helper;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeAIr.Chat.Content;
using FreeAIr.Chat.Context;
using FreeAIr.BLogic.Reader;

namespace FreeAIr.Chat
{
    /// <summary>
    /// A single dialogue with the LLM.
    ///
    /// The chat owns everything a completion request is built from: the ordered list of contents
    /// (prompts, answers, tool calls), the chat context (documents and other material given to the
    /// model), the chat-scoped MCP tool switches and the chosen agent.
    ///
    /// The chat itself does not talk to the model. It only signals, via <see cref="LLMReaderPool"/>,
    /// that there is something new to send; the actual streaming is done by <see cref="LLMReader"/>.
    ///
    /// Instances are created by <see cref="ChatContainer"/>, never directly.
    /// </summary>
    public sealed class Chat : IAsyncDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// The whole dialogue in chronological order. See <see cref="GetMessageListAsync"/>
        /// for how it is converted into a request.
        /// </summary>
        private readonly List<IChatContent> _contents = new();

        private ChatStatusEnum _status;

        /// <summary>
        /// Chat-scoped enabled/disabled state of MCP tools. It is a copy of the global state made
        /// at the moment of the chat creation, so switching a tool here does not affect other chats.
        /// </summary>
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

        public IReadOnlyList<IChatContent> Contents => _contents;

        public event ChatStatusChangedDelegate ChatStatusChangedEvent;

        public event ChatContentAddedDelegate ContentAddedEvent;

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
            set
            {
                _status = value;

                StatusChanged();
            }
        }

        private Chat(
            ChatContext chatContext,
            ChatDescription description,
            ChatOptions options,
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

            Started = DateTime.Now;

            ChatContext.ChatContextChangedEvent += ChatContextChangedRaised;
        }

        /// <summary>
        /// Creates a chat with a fresh context (with `copilot-instructions.md` picked up
        /// automatically, if there is one) and a snapshot of the global MCP tool statuses.
        /// </summary>
        public static async System.Threading.Tasks.Task<Chat> CreateChatAsync(
            ChatDescription description,
            FreeAIr.Chat.ChatOptions options
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

        /// <summary>
        /// Appends a user prompt and immediately asks the reader pool to send the dialogue to the
        /// model. This is the only entry point which starts a new turn.
        /// </summary>
        public void AddPrompt(
            UserPrompt userPrompt
            )
        {
            if (userPrompt is null)
            {
                throw new ArgumentNullException(nameof(userPrompt));
            }

            _contents.Add(userPrompt);

            LLMReaderPool.StartReaderFor(this);

            RaiseContentAdded(userPrompt);
        }

        public AnswerChatContent CreateAnswer()
        {
            var content = new AnswerChatContent();
            _contents.Add(content);

            RaiseContentAdded(content);

            return content;
        }

        /// <summary>
        /// Registers a tool call requested by the model.
        ///
        /// The callback passed to the content is invoked when this particular tool call reaches a
        /// terminal state. Once every tool call of the current turn is finished (succeeded, failed
        /// or blocked by the user), the reader is started again so the model can consume the
        /// results — this is what makes the tool-calling loop go round.
        /// </summary>
        public ToolCallChatContent CreateToolCall(
            StreamingChatToolCallUpdate toolCall
            )
        {
            var content = new ToolCallChatContent(
                toolCall,
                () =>
                {
                    var lastPromptIndex = _contents.FindLastIndex(c => c.Type == ChatContentTypeEnum.Prompt);
                    if (lastPromptIndex == -1)
                    {
                        return;
                    }

                    var lastTools = _contents
                        .Skip(lastPromptIndex)
                        .Where(c => c.Type == ChatContentTypeEnum.ToolCall)
                        .Cast<ToolCallChatContent>()
                        .ToList()
                        ;
                    var allToolsHaveResult = lastTools.All(t => t.Status.In(ToolCallStatusEnum.Succeeded, ToolCallStatusEnum.Failed, ToolCallStatusEnum.Blocked));
                    if (allToolsHaveResult)
                    {
                        LLMReaderPool.StartReaderFor(this);
                    }
                }
                );
            _contents.Add(content);

            RaiseContentAdded(content);

            return content;
        }

        public async Task StopAsync()
        {
            await LLMReaderPool.StopAndDeleteReaderForAsync(this);
        }

        public Task WaitForPromptResultAsync()
        {
            return WaitForTaskAsync();
        }

        public void ArchiveAllPrompts()
        {
            _contents.ForEach(p =>
            {
                p.Archive();
            });
        }

        public async ValueTask DisposeAsync()
        {
            Description.Dispose();

            foreach (var content in Contents)
            {
                if (content is IAsyncDisposable ad)
                {
                    await ad.DisposeAsync();
                }
                else if (content is IDisposable d)
                {
                    d.Dispose();
                }
            }
        }

        /// <summary>
        /// Builds the completion options for the next request. Only the tools which are enabled in
        /// <see cref="ChatTools"/> are offered to the model; if there are none, tool choice is
        /// forced to `none` because some providers reject an empty tool list.
        /// </summary>
        public async Task<ChatCompletionOptions> CreateChatCompletionOptionsAsync()
        {
            var toolCollection = McpServerProxyCollection.GetTools(ChatTools);
            var activeTools = toolCollection.GetActiveToolList();

            var cco = new ChatCompletionOptions
            {
                ToolChoice =
                    activeTools.Count > 0
                    ? Options.ToolChoice
                    : ChatToolChoice.CreateNoneChoice(),
                ResponseFormat = Options.ResponseFormat,
                MaxOutputTokenCount = (await FreeAIrOptions.DeserializeUnsortedAsync()).MaxOutputTokenCount,
            };

            foreach (var tool in activeTools)
            {
                cco.Tools.Add(
                    tool.CreateChatTool()
                    );
            }

            return cco;
        }

        /// <summary>
        /// Creates an OpenAI client configured for the agent chosen for this chat. The network
        /// timeout is deliberately huge: a local LLM on a slow machine can think for a long time.
        /// </summary>
        public ChatClient CreateChatClient()
        {
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
            return chatClient;
        }

        private async Task WaitForTaskAsync()
        {
            await LLMReaderPool.WaitForTaskAsync(this);
        }

        private void ChatContextChangedRaised(object sender, ChatContextEventArgs e)
        {
            StatusChanged();
        }

        private void RaiseContentAdded(
            IChatContent content
            )
        {
            ContentAddedEvent?.Invoke(
                this,
                new ChatContentAddedEventArgs(this, content)
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

        /// <summary>
        /// Flattens the chat into the message list to be sent to the model:
        /// system prompt, then everything before the last prompt, then the chat context items,
        /// then the last prompt and everything after it.
        ///
        /// The context is injected right before the last prompt on purpose: models pay much more
        /// attention to what stands close to the instruction they are asked to follow.
        /// Archived contents are skipped entirely.
        /// </summary>
        public async Task<IReadOnlyList<OpenAI.Chat.ChatMessage>> GetMessageListAsync()
        {
            var result = new List<OpenAI.Chat.ChatMessage>();

            var formattedSystemPrompt = await Options.ChosenAgent.GetFormattedSystemPromptAsync();
            result.Add(formattedSystemPrompt);

            var nonArchivedContents = Contents.FindAll(p => !p.IsArchived);

            var lastPromptIndex = nonArchivedContents.FindLastIndex(c => c.Type == ChatContentTypeEnum.Prompt);

            foreach (var content in nonArchivedContents.Take(lastPromptIndex))
            {
                FillMessageList(content, result);
            }

            foreach (var contextItem in ChatContext.Items)
            {
                result.Add(await contextItem.CreateChatMessageAsync());
            }

            foreach (var content in nonArchivedContents.Skip(lastPromptIndex))
            {
                FillMessageList(content, result);
            }

            return result;
        }

        private static void FillMessageList(
            IChatContent content,
            List<OpenAI.Chat.ChatMessage> result
            )
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            //put prompt
            var chatMessages = content.CreateChatMessages();
            result.AddRange(chatMessages);
        }

    }

    public delegate void ChatStatusChangedDelegate(object sender, ChatEventArgs e);
    public delegate void ChatContentAddedDelegate(object sender, ChatContentAddedEventArgs e);

    public sealed class ChatContentAddedEventArgs : EventArgs
    {
        public Chat Chat
        {
            get;
        }

        public IChatContent ChatContent
        {
            get;
        }

        public ChatContentAddedEventArgs(Chat chat, IChatContent chatContent)
        {
            Chat = chat;
            ChatContent = chatContent;
        }
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
}
