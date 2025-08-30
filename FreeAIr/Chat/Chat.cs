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
    public sealed class Chat : IAsyncDisposable
    {
        private readonly List<IChatContent> _contents = new();

        //private CancellationTokenSource _cancellationTokenSource = new();
        //private Task? _task;

        //public CancellationToken CancellationToken => _cancellationTokenSource.Token;

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
