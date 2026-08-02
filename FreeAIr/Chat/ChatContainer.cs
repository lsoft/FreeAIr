using EnvDTE;
using EnvDTE80;
using FreeAIr.Options2.Agent;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.Informer;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace FreeAIr.Chat
{
    /// <summary>
    /// The owner of every live <see cref="Chat"/>.
    ///
    /// This is a MEF singleton: obtain it via `IComponentModel.GetService&lt;ChatContainer&gt;()`,
    /// never construct it. Besides keeping the collection, it aggregates the status of all chats
    /// into a single indicator shown by <see cref="UIInformer"/>, and disposes everything when
    /// Visual Studio shuts down.
    /// </summary>
    [Export(typeof(ChatContainer))]
    public sealed class ChatContainer
    {
        /// <summary>
        /// Id of the most recently created chat. Commands invoked with Ctrl held down reuse this
        /// chat instead of starting a new one.
        /// </summary>
        public Guid? LastCreatedChatId
        {
            get;
            private set;
        }

        private readonly object _locker = new();

        private readonly UIInformer _uIInformer;
        private readonly List<Chat> _chats = new();
        
        private readonly DTEEvents _dteEvents;

        /// <summary>
        /// A snapshot of the live chats, not a view over the collection: chats are added and
        /// removed from the UI thread while their statuses change on the background threads which
        /// stream the answers, so handing out the list itself would break every enumeration of it.
        /// </summary>
        public IReadOnlyList<Chat> Chats
        {
            get
            {
                lock (_locker)
                {
                    return _chats.ToList();
                }
            }
        }

        public event ChatCollectionChangedDelegate ChatCollectionChangedEvent;
        public event ChatStatusChangedDelegate ChatStatusChangedEvent;

        [ImportingConstructor]
        public ChatContainer(
            UIInformer uiInformer
            )
        {
            if (uiInformer is null)
            {
                throw new ArgumentNullException(nameof(uiInformer));
            }

            //DTE is not free-threaded; the imported UIInformer asserts the same thing in its own
            //constructor, so this only makes the requirement of this class explicit as well
            ThreadHelper.ThrowIfNotOnUIThread();

            _uIInformer = uiInformer;

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            if (dte is null)
            {
                throw new InvalidOperationException("Cannot obtain DTE service.");
            }

            _dteEvents = ((Events2)dte.Events).DTEEvents;
            _dteEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;
        }

        public Chat? GetLastCreatedChat()
        {
            if (!LastCreatedChatId.HasValue)
            {
                return null;
            }

            lock (_locker)
            {
                return _chats.FirstOrDefault(c => c.Id == LastCreatedChatId.Value);
            }
        }

        /// <summary>
        /// Creates a chat and, if a prompt is given, immediately starts the first turn.
        /// Returns null if the chosen agent is not usable (no token, no endpoint, etc.);
        /// the error is reported to the user by the agent verification itself.
        /// </summary>
        public async Task<Chat?> StartChatAsync(
            ChatDescription kind,
            UserPrompt? prompt,
            FreeAIr.Chat.ChatOptions options
            )
        {
            if (kind is null)
            {
                throw new ArgumentNullException(nameof(kind));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (!await options.ChosenAgent.VerifyAgentAndShowErrorIfNotAsync())
            {
                return null;
            }

            var chat = await Chat.CreateChatAsync(
                kind,
                options
                );
            chat.ChatStatusChangedEvent += ChatStatusChanged;
            //chat.PromptStateChangedEvent.Event += PromptStateChanged;

            lock (_locker)
            {
                _chats.Add(chat);
            }

            //подписчики - это viewmodel'и, они лезут обратно в контейнер за списком чатов;
            //звать их из-под лока значит однажды получить дедлок на ровном месте
            FireChatCollectionChanged();

            LastCreatedChatId = chat.Id;

            //последним, чтобы к моменту старта чтения чат уже был и в коллекции, и в LastCreatedChatId
            if (prompt is not null)
            {
                chat.AddPrompt(prompt);
            }

            return chat;
        }

        public async Task RemoveChatAsync(
            Chat chat
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            if (!CheckIfChatIsInCollection(chat))
            {
                return;
            }

            await chat.StopAsync();

            chat.ChatStatusChangedEvent -= ChatStatusChanged;

            lock (_locker)
            {
                _chats.Remove(chat);
            }

            FireChatCollectionChanged();

            await chat.DisposeAsync();
        }


        private void RemoveAllChats()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(
                async () =>
                {
                    while (true)
                    {
                        Chat chat;
                        lock (_locker)
                        {
                            if (_chats.Count == 0)
                            {
                                break;
                            }

                            chat = _chats[0];
                        }

                        await RemoveChatAsync(chat);
                    }
                }).FileAndForget(nameof(RemoveAllChats));
        }

        public async Task StopChatAsync(
            Chat chat
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            if (!CheckIfChatIsInCollection(chat))
            {
                return;
            }

            await chat.StopAsync();
        }

        private bool CheckIfChatIsInCollection(Chat chat)
        {
            lock (_locker)
            {
                if (_chats.Any(c => ReferenceEquals(c, chat)))
                {
                    return true;
                }
            }

            return false;
        }

        //private void PromptStateChanged(object sender, PromptAddedEventArgs pea)
        //{
        //    var e = PromptStateChangedEvent;
        //    if (e is not null)
        //    {
        //        e(this, pea);
        //    }
        //}


        private void ChatStatusChanged(object sender, ChatEventArgs ea)
        {
            bool anyIsInProgress;
            lock (_locker)
            {
                anyIsInProgress = _chats.Any(c => c.Status.In(ChatStatusEnum.WaitingForAnswer, ChatStatusEnum.ReadingAnswer));
            }

            if (anyIsInProgress)
            {
                _uIInformer.UpdateUIStatusAsync(ChatsStatusEnum.Working);
            }
            else
            {
                _uIInformer.UpdateUIStatusAsync(ChatsStatusEnum.Idle);
            }

            FireChatStatusChanged(ea);
        }

        private void FireChatCollectionChanged()
        {
            var e = ChatCollectionChangedEvent;
            if (e is not null)
            {
                e(this, new EventArgs());
            }
        }
        
        private void FireChatStatusChanged(ChatEventArgs ea)
        {
            var e = ChatStatusChangedEvent;
            if (e is not null)
            {
                e(this, ea);
            }
        }

        private void DTEEvents_OnBeginShutdown()
        {
            RemoveAllChats();
        }

    }

    public delegate void ChatCollectionChangedDelegate(object sender, EventArgs e);
}
