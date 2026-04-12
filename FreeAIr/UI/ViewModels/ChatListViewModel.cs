using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ContextMenu;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfHelpers;
using WpfHelpers.ish;
using FreeAIr.Chat;
using FreeAIr.Shared.ish;

namespace FreeAIr.UI.ViewModels
{
    [Export(typeof(ChatListViewModel))]
    public sealed class ChatListViewModel : BaseViewModel
    {
        private readonly ChatContainer _chatContainer;

        private bool _showOnlyUserChats;

        public event Action ContextControlFocus;
        public event Action PromptControlFocus;

        public bool ShowOnlyUserChats
        {
            get => _showOnlyUserChats;
            set
            {
                _showOnlyUserChats = value;
                UpdateControl();
            }
        }

        public ObservableCollection2<ChatWrapper> ChatList
        {
            get;
        }

        public ImageMoniker StatusMoniker
        {
            get
            {
                if (SelectedChat is null)
                {
                    return KnownMonikers.Pause;
                }

                return SelectedChat.StatusMoniker;
            }
        }

        public ChatWrapper? SelectedChat
        {
            get;
            set
            {
                field = value;

                OnPropertyChanged();
            }
        }

        public bool ShowChatListPanel
        {
            get => UIPage.Instance.ShowChatListPanel;
            set
            {
                UIPage.Instance.ShowChatListPanel = value;
                UIPage.Instance.Save();

                OnPropertyChanged();
            }
        }

        public int ChatPanelColumn
        {
            get
            {
                if (ShowChatListPanel)
                {
                    return 2;
                }

                return 0;
            }
        }

        public int ChatPanelColumnSpan
        {
            get
            {
                if (ShowChatListPanel)
                {
                    return 0;
                }
                return 3;
            }
        }

        public Visibility ChatListVisibility
        {
            get
            {
                return ShowChatListPanel
                    ? Visibility.Visible
                    : Visibility.Collapsed
                    ;
            }
        }

        public Visibility ChatPanelVisibility
        {
            get
            {
                return SelectedChat is not null
                    ? Visibility.Visible
                    : Visibility.Collapsed
                    ;
            }
        }

        #region ish
        public cMyCommand cmdRenameChat => field ??= cMyCommand.Create(RenameChat);
        void RenameChat()
        {
            var wrapper = SelectedChat;
            if (wrapper == null) return;

            var newName =  ShowRenameDialog(wrapper.Title);
            if (!string.IsNullOrEmpty(newName))
            {
                wrapper.Title = newName;
                wrapper.Update();
            }
        }
        string ShowRenameDialog(string currentTitle)
        {
            var input = new cInputDlg("New chat name", currentTitle, "Enter new name ");
            return ((input.ShowDialog() == true) ? (string)input.Value : string.Empty);
        }
        #endregion


        public ICommand RemoveChatCommand
        {
            get
            {
                if (field == null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            _chatContainer.RemoveChatAsync(SelectedChat.Chat)
                                .FileAndForget(nameof(ChatContainer.RemoveChatAsync));
                        },
                        a => SelectedChat is not null && SelectedChat.Chat.Status.In(ChatStatusEnum.Failed, ChatStatusEnum.NotStarted, ChatStatusEnum.Ready)
                        );
                }

                return field;
            }
        }

        public ICommand StopChatCommand
        {
            get
            {
                if (field == null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            var wrapper = a as ChatWrapper;
                            if (wrapper is null)
                            {
                                return;
                            }

                            _chatContainer.StopChatAsync(wrapper.Chat)
                                .FileAndForget(nameof(ChatContainer.StopChatAsync));
                        },
                        a =>
                        {
                            var wrapper = a as ChatWrapper;
                            if (wrapper is null)
                            {
                                return false;
                            }

                            return wrapper.Chat.Status.In(ChatStatusEnum.WaitingForAnswer, ChatStatusEnum.ReadingAnswer);
                        });
                }

                return field;
            }
        }

        public ICommand StartChatCommand
        {
            get
            {
                if (field == null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                                FreeAIr.Resources.Resources.Choose_agent__with_a_non_empty_token
                                );
                            if (chosenAgent is null)
                            {
                                return;
                            }

                            _ = await _chatContainer.StartChatAsync(
                                new ChatDescription(null),
                                null,
                                await FreeAIr.Chat.ChatOptions.GetDefaultAsync(chosenAgent)
                                );

                            OnPropertyChanged();
                        }
                        );
                }

                return field;
            }
        }

        public ICommand OpenControlCenterCommand
        {
            get
            {
                if (field == null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await Commands.Other.OpenControlCenterCommand.ShowAsync(
                                );
                            OnPropertyChanged();
                        }
                        );
                }

                return field;
            }
        }


        [ImportingConstructor]
        public ChatListViewModel(
            ChatContainer chatContainer
            )
        {
            if (chatContainer is null)
            {
                throw new ArgumentNullException(nameof(chatContainer));
            }

            _chatContainer = chatContainer;

            chatContainer.ChatCollectionChangedEvent += ChatCollectionChanged;
            chatContainer.ChatStatusChangedEvent += ChatStatusChanged;

            _showOnlyUserChats = true;

            ChatList = new ObservableCollection2<ChatWrapper>();

            UpdateControl();
        }

        private async void ChatCollectionChanged(object sender, EventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                UpdateControl();
            }
            catch (Exception excp)
            {
                //todo
            }
        }

        private async void ChatStatusChanged(object sender, ChatEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                foreach (var chat in ChatList)
                {
                    chat.Update();
                }

                if (SelectedChat is null || ReferenceEquals(SelectedChat.Chat, e.Chat))
                {
                    OnPropertyChanged();
                }
            }
            catch (Exception excp)
            {
                //todo
            }
        }

        private void UpdateControl()
        {
            ChatList.Clear();

            if (_showOnlyUserChats)
            {
                ChatList.AddRange(
                    _chatContainer.Chats
                        .Where(c => !c.Options.AutomaticallyProcessed)
                        .OrderByDescending(c => c.Started)
                        .Select(t => new ChatWrapper(t))
                    );
            }
            else
            {
                ChatList.AddRange(
                    _chatContainer.Chats
                        .OrderByDescending(c => c.Options.AutomaticallyProcessed)
                        .ThenBy(c => c.Started)
                        .Reverse()
                        .Select(t => new ChatWrapper(t))
                    );
            }

            SelectedChat = ChatList.FirstOrDefault();
        }

        public sealed class  ChatWrapper : BaseViewModel
        {
            public FreeAIr.Chat.Chat Chat
            {
                get;
            }

            public ImageMoniker StatusMoniker => GetStatusMoniker(Chat);

            public static ImageMoniker GetStatusMoniker(FreeAIr.Chat.Chat chat)
            {
                if (chat.Status.In(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                {
                    return KnownMonikers.Pause;
                }
                if (chat.Status.In(ChatStatusEnum.WaitingForAnswer, ChatStatusEnum.ReadingAnswer))
                {
                    return KnownMonikers.Run;
                }
                if (chat.Status == ChatStatusEnum.Failed)
                {
                    return KnownMonikers.StatusErrorOutline;
                }

                return KnownMonikers.QuestionMark;
            }

            public bool IsReadyToAcceptNewPrompt
            {
                get
                {
                    return Chat.Status.In(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready, ChatStatusEnum.Failed);
                }
            }

            public Visibility SecondRowVisibility
            {
                get
                {
                    return string.IsNullOrEmpty(SecondRow)
                        ? Visibility.Collapsed
                        : Visibility.Visible
                        ;
                }
            }

            public string Title
            {
                get => Chat.Description?.Title;
                set => Chat.Description?.Title = value;
            }

            public string SecondRow
            {
                get
                {
                    return Chat.Description?.SelectedTextDescriptor?.FileName ?? string.Empty;
                }
            }

            public string ThirdRow
            {
                get
                {
                    return Chat.Started.HasValue
                        ? FreeAIr.Resources.Resources.Started + ": " + Chat.Started.Value.ToString()
                        : Resources.Resources.Not_started
                        ;
                }
            }

            public string FourthRow
            {
                get
                {
                    return Chat.Status.AsUIString();
                }
            }

            public double OpacityLevel
            {
                get
                {
                    if (Chat.Options.AutomaticallyProcessed)
                    {
                        return 0.3;
                    }

                    return 1.0;
                }
            }

            public ChatWrapper(
                FreeAIr.Chat.Chat chat
                )
            {
                if (chat is null)
                {
                    throw new ArgumentNullException(nameof(chat));
                }

                Chat = chat;
            }

            public void Update()
            {
                OnPropertyChanged();
            }
        }

    }
}
