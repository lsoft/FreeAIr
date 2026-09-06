using FreeAIr.Llm;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.Options2;
using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FreeAIr.Chat.Content;
using FreeAIr.Chat.Context;
using FreeAIr.Chat.Persistence;
using FreeAIr.BLogic.Reader;
using FreeAIr.Helper;

namespace FreeAIr.Chat
{
    /// <summary>
    /// A single dialogue with the LLM.
    ///
    /// The chat owns everything a completion request is built from: the ordered list of contents
    /// (prompts, answers, tool calls), the chat context (documents and other material given to the
    /// model), the chat-scoped MCP tool switches and the chosen agent.
    ///
    /// User-started chats are written to `.freeair\chats` when that folder can be resolved, so they
    /// survive a restart of Visual Studio. Automatic chats, and chats created with no solution open,
    /// stay in memory only.
    ///
    /// The chat itself does not talk to the model. It only signals, via <see cref="LLMReaderPool"/>,
    /// that there is something new to send; the actual streaming is done by <see cref="LLMReader"/>.
    ///
    /// Instances are created by <see cref="ChatContainer"/>, never directly.
    /// </summary>
    public sealed class Chat : IAsyncDisposable
    {
        /// <summary>
        /// Identifies the chat across the Visual Studio session and, for a persisted chat, across
        /// restarts: it is the name of the json file under `.freeair\chats`.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Absolute path of the json file this chat is saved to, or null when the chat is not
        /// persistent — it is an automatic chat, or no solution was open when it was created.
        /// </summary>
        private string? _persistenceFilePath;

        /// <summary>Guards overlapping writes of the same chat file from status and content events.</summary>
        private readonly object _persistLock = new();

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

        /// <summary>
        /// True when this chat is written to `.freeair\chats`. Automatic chats never are; a user
        /// chat is only when the solution folder was known at creation time.
        /// </summary>
        public bool IsPersistent => _persistenceFilePath is not null;

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
        /// stopped, disposed and shown, so it is built by <see cref="CreateChatAsync"/> or
        /// <see cref="CreateFromPersistedAsync"/> only.
        /// </summary>
        private Chat(
            ChatContext chatContext,
            ChatDescription description,
            ChatOptions options,
            AvailableToolContainer chatTools,
            Guid? id = null,
            DateTime? started = null
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

            Id = id ?? Guid.NewGuid();
            ChatContext = chatContext;
            Description = description;
            Options = options;
            ChatTools = chatTools;
            _status = ChatStatusEnum.NotStarted;

            Started = started ?? DateTime.Now;

            ChatContext.ChatContextChangedEvent += ChatContextChangedRaised;
            Description.PropertyChanged += DescriptionPropertyChanged;
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
        /// Rebuilds a user chat from a json file under `.freeair\chats`. Returns null when the file
        /// cannot be parsed or there is no agent left to attach. Does not start a reader: the
        /// transcript is already complete.
        /// </summary>
        public static async Task<Chat?> CreateFromPersistedAsync(
            PersistedChatJson payload,
            string filePath
            )
        {
            if (payload is null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var agent = await FreeAIrOptions.DeserializeAgentByNameAsync(payload.AgentName);
            if (agent is null)
            {
                var agents = await FreeAIrOptions.DeserializeAgentCollectionAsync();
                agent = agents.Agents.FirstOrDefault();
            }

            if (agent is null)
            {
                return null;
            }

            var options = await ChatOptions.GetDefaultAsync(agent);
            var chatTools = payload.Tools is not null
                ? AvailableToolContainer.Create(payload.Tools)
                : await AvailableToolContainer.ReadSystemAsync();

            IOriginalTextDescriptor? selected = null;
            if (!string.IsNullOrEmpty(payload.SelectedFilePath) && File.Exists(payload.SelectedFilePath))
            {
                selected = new WholeFileTextDescriptor(
                    payload.SelectedFilePath,
                    LineEndingHelper.Actual.GetDocumentLineEnding(payload.SelectedFilePath)
                    );
            }

            var description = new ChatDescription(selected)
            {
                Title = string.IsNullOrEmpty(payload.Title) ? "Untitled" : payload.Title,
            };

            var chatContext = ChatContext.CreateEmpty();
            var chat = new Chat(
                chatContext,
                description,
                options,
                chatTools,
                payload.Id,
                payload.Started
                );

            foreach (var contextRow in payload.Context)
            {
                var item = contextRow.ToItem();
                if (item is not null)
                {
                    chatContext.AddItem(item);
                }
            }

            foreach (var contentRow in payload.Contents)
            {
                var content = chat.RestoreContent(contentRow);
                if (content is not null)
                {
                    chat._contents.Add(content);
                }
            }

            var restoredStatus = payload.Status;
            if (restoredStatus == ChatStatusEnum.WaitingForAnswer || restoredStatus == ChatStatusEnum.ReadingAnswer)
            {
                restoredStatus = ChatStatusEnum.Ready;
            }

            if (chat._contents.Count == 0)
            {
                restoredStatus = ChatStatusEnum.NotStarted;
            }
            else if (restoredStatus == ChatStatusEnum.NotStarted)
            {
                restoredStatus = ChatStatusEnum.Ready;
            }

            chat._status = restoredStatus;
            chat.EnablePersistence(filePath);
            return chat;
        }

        /// <summary>
        /// Marks this chat as written to <paramref name="filePath"/>. Called once, either at
        /// creation when the `.freeair\chats` folder can be resolved, or when the chat is loaded
        /// back from that folder.
        /// </summary>
        public void EnablePersistence(string filePath)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _persistenceFilePath = filePath;
        }

        /// <summary>
        /// Writes the whole chat to its json file. No-op when the chat is not persistent. Safe to
        /// call from the UI thread or from a reader thread: the write is short and the file is
        /// replaced atomically.
        /// </summary>
        public void PersistNow()
        {
            var filePath = _persistenceFilePath;
            if (filePath is null)
            {
                return;
            }

            lock (_persistLock)
            {
                try
                {
                    ChatPersistence.Save(filePath, PersistedChatJson.FromChat(this));
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();
                }
            }
        }

        /// <summary>
        /// Removes the json file because the user closed the chat. Visual Studio shutting down does
        /// not call this, so the transcript is still there next time the solution opens.
        /// </summary>
        public void DeletePersistentFile()
        {
            var filePath = _persistenceFilePath;
            if (filePath is null)
            {
                return;
            }

            ChatPersistence.Delete(filePath);
            _persistenceFilePath = null;
        }

        /// <summary>
        /// Rebuilds one transcript entry from the saved json without starting a reader or notifying
        /// the window. The window reads <see cref="Contents"/> when it binds.
        /// </summary>
        private IChatContent? RestoreContent(
            PersistedContentJson row
            )
        {
            switch (row.Type)
            {
                case ChatContentTypeEnum.Prompt:
                    var prompt = UserPrompt.CreateTextBasedPrompt(row.Body ?? string.Empty);
                    if (row.IsArchived)
                    {
                        prompt.Archive();
                    }

                    return prompt;

                case ChatContentTypeEnum.LLMAnswer:
                    return AnswerChatContent.CreateCompleted(
                        row.Body ?? string.Empty,
                        row.IsArchived
                        );

                case ChatContentTypeEnum.ToolCall:
                    return ToolCallChatContent.Restore(
                        row.ToolCallId,
                        row.Name,
                        row.Arguments,
                        row.Status ?? ToolCallStatusEnum.Failed,
                        row.Result,
                        row.IsArchived,
                        ContinueTurnIfAllToolsHaveResult
                        );

                default:
                    return null;
            }
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
            LlmToolCall toolCall
            )
        {
            var content = new ToolCallChatContent(
                toolCall,
                ContinueTurnIfAllToolsHaveResult
                );
            _contents.Add(content);

            RaiseContentAdded(content);

            return content;
        }

        /// <summary>
        /// When every tool call of the current turn has finished, starts the reader again so the
        /// model can consume the results. Shared by a live stream and by a restored call the user
        /// is still being asked about.
        /// </summary>
        private void ContinueTurnIfAllToolsHaveResult()
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
                PersistNow();
                LLMReaderPool.StartReaderFor(this);
            }
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

            PersistNow();
        }

        /// <summary>
        /// Releases the description and every content which holds something — the temporary files
        /// behind images, the editor subscriptions behind selections. Called by
        /// <see cref="ChatContainer.RemoveChatAsync"/> after the reader has been stopped.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            Description.PropertyChanged -= DescriptionPropertyChanged;
            ChatContext.ChatContextChangedEvent -= ChatContextChangedRaised;

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
        /// Builds the whole next request: the agent's system prompt, the transcript, and the tools
        /// which are enabled in <see cref="ChatTools"/>. Nothing here is protocol specific - which
        /// wire format this becomes is settled by <see cref="CreateTransport"/>.
        /// </summary>
        public async Task<LlmRequest> BuildRequestAsync()
        {
            var toolCollection = McpServerProxyCollection.GetTools(ChatTools);
            var activeTools = toolCollection.GetActiveToolList();

            var tools = new List<LlmToolDefinition>(activeTools.Count);
            foreach (var tool in activeTools)
            {
                tools.Add(tool.CreateToolDefinition());
            }

            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            return new LlmRequest(
                model: Options.ChosenAgent.Technical.ChosenModel,
                systemPrompt: await Options.ChosenAgent.GetFormattedSystemPromptAsync(),
                messages: await GetMessageListAsync(),
                tools: tools,
                toolChoice: Options.ToolChoice,
                //a zero in the settings means "unset", not "answer with nothing"
                maxOutputTokens: unsorted.MaxOutputTokenCount > 0
                    ? unsorted.MaxOutputTokenCount
                    : (int?)null
                );
        }

        /// <summary>
        /// The transport for the agent chosen for this chat, picked by the protocol its endpoint
        /// speaks. A transport is cheap and holds nothing worth keeping between turns, and the
        /// agent - therefore possibly the protocol - can change from one turn to the next.
        /// </summary>
        public ILlmTransport CreateTransport()
        {
            var technical = Options.ChosenAgent.Technical;

            var endpoint = technical.TryBuildEndpointUri();
            if (endpoint is null)
            {
                throw new InvalidOperationException(
                    $"Agent '{Options.ChosenAgent.Name}' has an endpoint which is not a valid uri: '{technical.Endpoint}'."
                    );
            }

            return LlmTransportFactory.Create(
                technical.ApiProtocol,
                endpoint,
                technical.GetToken()
                );
        }

        private async Task WaitForTaskAsync()
        {
            await LLMReaderPool.WaitForTaskAsync(this);
        }

        /// <summary>
        /// Reports a context edit as a status change. The two are separate things, but every
        /// subscriber redraws on either, and one event spares them a second subscription.
        /// A persistent chat is saved here too: attaching a file is a real change, and it is not
        /// the per-token flood of a streaming answer.
        /// </summary>
        private void ChatContextChangedRaised(object sender, ChatContextEventArgs e)
        {
            StatusChanged();
            PersistNow();
        }

        /// <summary>Saves after the user renamed the chat in the list.</summary>
        private void DescriptionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(ChatDescription.Title))
            {
                PersistNow();
            }
        }

        private void RaiseContentAdded(
            IChatContent content
            )
        {
            ContentAddedEvent?.Invoke(
                this,
                new ChatContentAddedEventArgs(this, content)
                );

            //an answer is empty when it is created and then grows token by token; wait until the
            //turn settles (Ready / Failed) rather than writing the file on every chunk
            if (content.Type != ChatContentTypeEnum.LLMAnswer)
            {
                PersistNow();
            }
        }

        private void StatusChanged()
        {
            var e = ChatStatusChangedEvent;
            if (e is not null)
            {
                e(this, new ChatEventArgs(this));
            }

            if (Status == ChatStatusEnum.Ready || Status == ChatStatusEnum.Failed)
            {
                PersistNow();
            }
        }

        /// <summary>
        /// Flattens the chat into the message list to be sent to the model: everything before the
        /// last prompt, then the chat context items, then the last prompt and everything after it.
        ///
        /// The context is injected right before the last prompt on purpose: models pay much more
        /// attention to what stands close to the instruction they are asked to follow.
        /// Archived contents are skipped entirely.
        ///
        /// The system prompt is not part of this list. It is a field of <see cref="LlmRequest"/>,
        /// because one of the two protocols has no system role at all.
        /// </summary>
        public async Task<IReadOnlyList<LlmMessage>> GetMessageListAsync()
        {
            var result = new List<LlmMessage>();

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
            List<LlmMessage> result
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
