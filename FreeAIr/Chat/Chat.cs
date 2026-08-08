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
        /// <summary>
        /// Identifies the chat for the lifetime of the Visual Studio session. Not persisted — it is
        /// how the tool windows and <see cref="ChatContainer.LastCreatedChatId"/> refer to a chat
        /// without holding it alive.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// The whole dialogue in chronological order. See <see cref="GetMessageListAsync"/>
        /// for how it is converted into a request.
        /// </summary>
        private readonly List<IChatContent> _contents = new();

        /// <summary>Backing field for <see cref="Status"/>.</summary>
        private ChatStatusEnum _status;

        /// <summary>
        /// Chat-scoped enabled/disabled state of MCP tools. It is a copy of the global state made
        /// at the moment of the chat creation, so switching a tool here does not affect other chats.
        /// </summary>
        public AvailableToolContainer ChatTools
        {
            get;
        }

        /// <summary>
        /// The documents, selections and images given to the model alongside the prompts. Editing it
        /// raises the status changed event, because the chat window draws the context chips.
        /// </summary>
        public ChatContext ChatContext
        {
            get;
        }

        /// <summary>
        /// The agent, the tool choice and the response format fixed when the chat was created. They
        /// do not change afterwards: a dialogue half of which was answered by another model with
        /// another system prompt is not a dialogue the model can make sense of.
        /// </summary>
        public ChatOptions Options
        {
            get;
        }

        /// <summary>The dialogue as the UI reads it: prompts, answers and tool calls in order.</summary>
        public IReadOnlyList<IChatContent> Contents => _contents;

        /// <summary>
        /// Raised when the chat changes state or its context is edited. <see cref="ChatContainer"/>
        /// listens in order to aggregate all chats into the single progress indicator in the status
        /// bar.
        /// </summary>
        public event ChatStatusChangedDelegate ChatStatusChangedEvent;

        /// <summary>
        /// Raised once per new prompt, answer or tool call, so the chat window can append a control
        /// instead of rebuilding the whole transcript.
        /// </summary>
        public event ChatContentAddedDelegate ContentAddedEvent;

        /// <summary>
        /// What the chat is called in the tool window and what it was started from — a document, a
        /// selection or nothing at all.
        /// </summary>
        public ChatDescription Description
        {
            get;
        }

        /// <summary>When the chat was created. Shown in the chat list to tell several of them apart.</summary>
        public DateTime? Started
        {
            get;
            private set;
        }

        /// <summary>
        /// Where the chat currently is: not started, waiting for an answer, reading one, done or
        /// failed. Assigning it notifies the subscribers, so the status bar and the chat list follow
        /// the streaming without polling.
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

        /// <summary>
        /// Private on purpose: a chat has to be registered with <see cref="ChatContainer"/> to be
        /// stopped, disposed and shown, so it is built by <see cref="CreateChatAsync"/> only.
        /// </summary>
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

        /// <summary>
        /// Opens an empty answer for the reader to stream text into. Created before the first token
        /// arrives so the chat window can show the answer growing rather than appearing at the end.
        /// </summary>
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

        /// <summary>
        /// Cancels the request in flight, if any, and drops the reader. The contents already
        /// received are kept — a half streamed answer is still worth reading.
        /// </summary>
        public async Task StopAsync()
        {
            await LLMReaderPool.StopAndDeleteReaderForAsync(this);
        }

        /// <summary>
        /// Waits until the current turn is over, including any tool calls it triggered. Used by the
        /// features which need the answer rather than a chat window: whole line completion, commit
        /// message generation, natural language outlines.
        /// </summary>
        public Task WaitForPromptResultAsync()
        {
            return WaitForTaskAsync();
        }

        /// <summary>
        /// Marks everything said so far as archived, which keeps it on screen but leaves it out of
        /// the next request. This is how the user clears the history of a long dialogue without
        /// losing what they can read, and how the token cost of a chat is brought back down.
        /// </summary>
        public void ArchiveAllPrompts()
        {
            _contents.ForEach(p =>
            {
                p.Archive();
            });
        }

        /// <summary>
        /// Releases the description and every content which holds something — the temporary files
        /// behind images, the editor subscriptions behind selections. Called by
        /// <see cref="ChatContainer.RemoveChatAsync"/> after the reader has been stopped.
        /// </summary>
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

        /// <summary>
        /// Reports a context edit as a status change. The two are separate things, but every
        /// subscriber redraws on either, and one event spares them a second subscription.
        /// </summary>
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

        /// <summary>
        /// Appends one content to the request. A content may expand into several messages — a tool
        /// call becomes the assistant's request plus the tool's reply — which is why this is not a
        /// one-to-one mapping.
        /// </summary>
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

    /// <summary>Handler shape of <see cref="Chat.ChatStatusChangedEvent"/>.</summary>
    public delegate void ChatStatusChangedDelegate(object sender, ChatEventArgs e);

    /// <summary>Handler shape of <see cref="Chat.ContentAddedEvent"/>.</summary>
    public delegate void ChatContentAddedDelegate(object sender, ChatContentAddedEventArgs e);

    /// <summary>
    /// Says which content was appended and to which chat, so a window bound to one chat can ignore
    /// the others.
    /// </summary>
    public sealed class ChatContentAddedEventArgs : EventArgs
    {
        /// <summary>The chat the content was added to.</summary>
        public Chat Chat
        {
            get;
        }

        /// <summary>The prompt, answer or tool call just appended.</summary>
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

    /// <summary>
    /// Carries the chat whose status or context has changed. The new status is read from the chat
    /// rather than copied here, so a late handler sees the current value and not a stale one.
    /// </summary>
    public sealed class ChatEventArgs : EventArgs
    {
        /// <summary>The chat that changed.</summary>
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
