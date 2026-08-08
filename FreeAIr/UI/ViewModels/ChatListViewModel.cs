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
using FreeAIr.Chat;
using FreeAIr.UI.Windows;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// Backs the chat list panel of the FreeAIr tool window: the sidebar that lists open chats,
    /// lets the user start, stop, rename or remove a chat, and tracks which chat is currently selected.
    /// </summary>
    [Export(typeof(ChatListViewModel))]
    public sealed class ChatListViewModel : BaseViewModel
    {
        /// <summary>
        /// The application-wide collection of chats this view model lists, starts, stops and removes chats from.
        /// </summary>
        private readonly ChatContainer _chatContainer;

        /// <summary>
        /// Whether the chat list is filtered down to chats started by the user, hiding chats that were
        /// started automatically (e.g. by background/automatic processing).
        /// </summary>
        private bool _showOnlyUserChats;

        /// <summary>
        /// Raised to move keyboard focus to the chat context control, e.g. after selecting a chat.
        /// </summary>
        public event Action ContextControlFocus;
        /// <summary>
        /// Raised to move keyboard focus to the prompt input control, e.g. after starting a new chat.
        /// </summary>
        public event Action PromptControlFocus;

        /// <summary>
        /// Whether the chat list should show only chats started by the user, hiding automatically
        /// processed chats; toggling it refreshes <see cref="ChatList"/>.
        /// </summary>
        public bool ShowOnlyUserChats
        {
            get => _showOnlyUserChats;
            set
            {
                _showOnlyUserChats = value;
                UpdateControl();
            }
        }

        /// <summary>
        /// The chats currently shown in the chat list panel, wrapped for display and filtered/ordered
        /// per <see cref="ShowOnlyUserChats"/>.
        /// </summary>
        public ObservableCollection2<ChatWrapper> ChatList
        {
            get;
        }

        /// <summary>
        /// The status icon shown for the selected chat (paused, running, error, unknown); shows a
        /// paused icon when no chat is selected.
        /// </summary>
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

        /// <summary>
        /// The chat currently selected in the chat list panel; drives which chat's conversation is
        /// shown in the chat panel.
        /// </summary>
        public ChatWrapper? SelectedChat
        {
            get;
            set
            {
                field = value;

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether the chat list panel (the sidebar of chats) is shown; persisted to the FreeAIr
        /// options page so the layout choice survives across sessions.
        /// </summary>
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

        /// <summary>
        /// The grid column the chat panel starts at, adjusted depending on whether the chat list
        /// panel is showing so the chat panel expands to fill the freed space.
        /// </summary>
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

        /// <summary>
        /// How many grid columns the chat panel spans, widened to fill the layout when the chat
        /// list panel is hidden.
        /// </summary>
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

        /// <summary>
        /// Whether the chat list panel is visible, mirroring <see cref="ShowChatListPanel"/> as a
        /// WPF <see cref="Visibility"/> for binding.
        /// </summary>
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

        /// <summary>
        /// Whether the chat conversation panel is visible; hidden until a chat is selected.
        /// </summary>
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


        /// <summary>
        /// Removes the selected chat from the chat container; enabled only for chats that are not
        /// currently running (failed, not started, or ready/idle).
        /// </summary>
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

        /// <summary>
        /// Stops the in-progress chat passed as the command parameter; enabled only while that chat
        /// is waiting for or reading an answer from the model.
        /// </summary>
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

        /// <summary>
        /// Opens the Rename Chat dialog for the chat passed as the command parameter and applies the
        /// new title to it if the user confirms.
        /// </summary>
        public ICommand RenameChatCommand
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

                            var renameViewModel = new RenameChatViewModel(
                                wrapper.Title
                                );
                            var renameWindow = new RenameChatWindow
                            {
                                DataContext = renameViewModel
                            };
                            if (renameWindow.ShowDialog() != true)
                            {
                                return;
                            }

                            var newChatName = renameViewModel.ChatName;
                            if (string.IsNullOrEmpty(newChatName))
                            {
                                return;
                            }

                            wrapper.Title = newChatName;
                            wrapper.Update();
                        }
                        );
                }

                return field;
            }
        }

        /// <summary>Prompts the user to choose an agent, then starts a new empty chat with that agent's default options.</summary>
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

        /// <summary>Opens the FreeAIr Control Center window.</summary>
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


        /// <summary>Creates the view model bound to the application-wide chat container, subscribes to its collection/status change events, and populates the initial chat list.</summary>
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

        /// <summary>Refreshes <see cref="ChatList"/> on the UI thread whenever chats are added to or removed from the container.</summary>
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

        /// <summary>Updates every chat wrapper's display state and re-raises property-changed for the selected chat when its status changes.</summary>
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

        /// <summary>Rebuilds <see cref="ChatList"/> from the chat container, applying the <see cref="ShowOnlyUserChats"/> filter and ordering, and re-selects the first chat.</summary>
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

        /// <summary>Display adapter around a <see cref="FreeAIr.Chat.Chat"/> for one row of the chat list, exposing its title, status icon and metadata rows for binding.</summary>
        public sealed class  ChatWrapper : BaseViewModel
        {
            /// <summary>The underlying chat this row represents.</summary>
            public FreeAIr.Chat.Chat Chat
            {
                get;
            }

            /// <summary>The status icon for this chat's row, derived from <see cref="GetStatusMoniker"/>.</summary>
            public ImageMoniker StatusMoniker => GetStatusMoniker(Chat);

            /// <summary>Maps a chat's status to the icon shown for it: paused for not-started/ready, running while waiting for or reading an answer, an error icon on failure, and a question mark otherwise.</summary>
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

            /// <summary>Whether this chat can accept a new prompt right now: not started, ready/idle, or failed (but not currently running).</summary>
            public bool IsReadyToAcceptNewPrompt
            {
                get
                {
                    return Chat.Status.In(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready, ChatStatusEnum.Failed);
                }
            }

            /// <summary>Whether the second metadata row (the source file name) has anything to show.</summary>
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

            /// <summary>The chat's display title, editable via the Rename Chat dialog.</summary>
            public string Title
            {
                get => Chat.Description?.Title;
                set => Chat.Description?.Title = value;
            }

            /// <summary>The name of the file the chat's selected text came from, or empty if the chat has none.</summary>
            public string SecondRow
            {
                get
                {
                    return Chat.Description?.SelectedTextDescriptor?.FileName ?? string.Empty;
                }
            }

            /// <summary>The chat's start time as display text, or a "not started" placeholder.</summary>
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

            /// <summary>The chat's current status as localized display text.</summary>
            public string FourthRow
            {
                get
                {
                    return Chat.Status.AsUIString();
                }
            }

            /// <summary>Dims automatically-processed chats in the list, so user-started chats stand out.</summary>
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

            /// <summary>Wraps a chat for display in the chat list.</summary>
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

            /// <summary>Re-raises property-changed for all bound properties, refreshing this row's display.</summary>
            public void Update()
            {
                OnPropertyChanged();
            }
        }

    }
}
