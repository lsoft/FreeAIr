using FreeAIr.BLogic;
using FreeAIr.Helper;
using OpenAI;
using OpenAI.Chat;
using SauronEye.UI.Informer;
using System;
using System.ClientModel;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    [Export(typeof(ChatContainer))]
    public sealed class ChatContainer
    {
        private readonly object _locker = new();

        private readonly UIInformer _uIInformer;
        private readonly List<Chat> _chats = new();

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
        }

        public void StartChat(
            ChatDescription kind,
            UserPrompt prompt
            )
        {
            var chat = new Chat(
                kind
                );
            chat.ChatStatusChangedEvent += ChatStatusChanged;

            lock (_locker)
            {
                _chats.Add(chat);
                FireChatCollection();
            }

            chat.AddPrompt(prompt);
        }

        public async Task RemoveChatAsync(Chat chat)
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
                FireChatCollection();
            }

            chat.Dispose();
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
            var anyIsInProgress = _chats.Any(c => c.Status.In(ChatStatusEnum.WaitForAnswer, ChatStatusEnum.ReadAnswer));
            if (anyIsInProgress)
            {
                _uIInformer.UpdateUIStatusAsync(ChatsStatusEnum.Working);
            }
            else
            {
                _uIInformer.UpdateUIStatusAsync(ChatsStatusEnum.Idle);
            }

            FireChatEvent(ea);
        }

        private void FireChatCollection()
        {
            var e = ChatCollectionChangedEvent;
            if (e is not null)
            {
                e(this, new EventArgs());
            }
        }
        
        private void FireChatEvent(ChatEventArgs ea)
        {
            var e = ChatStatusChangedEvent;
            if (e is not null)
            {
                e(this, ea);
            }
        }

    }

    public delegate void ChatCollectionChangedDelegate(object sender, EventArgs e);
}
