using EnvDTE;
using EnvDTE80;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.Informer;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    [Export(typeof(ChatContainer))]
    public sealed class ChatContainer
    {
        private readonly object _locker = new();

        private readonly UIInformer _uIInformer;
        private readonly List<Chat> _chats = new();
        
        private readonly DTEEvents _dteEvents;

        public IReadOnlyList<Chat> Chats => _chats;

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

            _uIInformer = uiInformer;

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            _dteEvents = ((Events2)dte.Events).DTEEvents;
            _dteEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;
        }

        public async Task<Chat?> StartChatAsync(
            ChatDescription kind,
            UserPrompt? prompt
            )
        {
            if (kind is null)
            {
                throw new ArgumentNullException(nameof(kind));
            }

            if (!await ApiPage.Instance.VerifyUriAndShowErrorIfNotAsync())
            {
                return null;
            }

            var chat = await Chat.CreateChatAsync(
                kind
                );
            chat.ChatStatusChangedEvent += ChatStatusChanged;

            lock (_locker)
            {
                _chats.Add(chat);
                FireChatCollectionChanged();
            }

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
                FireChatCollectionChanged();
            }

            chat.Dispose();
        }


        private void RemoveAllChats()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(
                async () =>
                {
                    while (_chats.Count > 0)
                    {
                        await RemoveChatAsync(_chats[0]);
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

        private void ChatStatusChanged(object sender, ChatEventArgs ea)
        {
            var anyIsInProgress = _chats.Any(c => c.Status.In(ChatStatusEnum.WaitingForAnswer, ChatStatusEnum.ReadingAnswer));
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
