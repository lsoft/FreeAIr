using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Embedillo.Answer.Parser;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    [Export(typeof(ChatListViewModel))]
    public sealed class ChatListViewModel : BaseViewModel
    {
        private readonly ChatContainer _chatContainer;

        private ChatWrapper _selectedChat;
        
        private ICommand _openInEditorCommand;
        private ICommand _removeCommand;
        private ICommand _stopCommand;
        private ICommand _createPromptCommand;
        private ICommand _startChatCommand;
        private ICommand _deleteItemFromContextCommand;
        private ICommand _openContextItemCommand;
        private ICommand _addItemToContextCommand;
        private ICommand _removeAllAutomaticItemsFromContextCommand;
        private ICommand _addRelatedItemsToContextCommand;
        private ICommand _addCustomFileToContextCommand;
        private ICommand _editChatToolsCommand;
        private ICommand _openControlCenterCommand;
        private ICommand _chooseChatAgentCommand;

        public event Action ContextControlFocus;
        public event Action PromptControlFocus;

        public ObservableCollection2<ChatWrapper> ChatList
        {
            get;
        }

        public ImageMoniker StatusMoniker
        {
            get
            {
                if (_selectedChat is null)
                {
                    return KnownMonikers.Pause;
                }

                return _selectedChat.StatusMoniker;
            }
        }

        public bool IsReadyToAcceptNewPrompt
        {
            get
            {
                if (_selectedChat is null)
                {
                    return false;
                }
                if (_selectedChat.Chat.Options.AutomaticallyProcessed)
                {
                    return false;
                }

                return _selectedChat.IsReadyToAcceptNewPrompt;
            }
        }

        public ChatWrapper? SelectedChat
        {
            get => _selectedChat;
            set
            {
                _selectedChat = value;

                DialogViewModel.UpdateDialog(value);

                FocusPromptControl();

                OnPropertyChanged();
            }
        }

        public DialogViewModel DialogViewModel
        {
            get;
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


        public ICommand OpenInEditorCommand
        {
            get
            {
                if (_openInEditorCommand == null)
                {
                    _openInEditorCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var wrapper = a as ChatWrapper;
                            if (wrapper is null)
                            {
                                return;
                            }

                            await VS.Documents.OpenAsync(wrapper.Chat.ResultFilePath);
                        },
                        a =>
                        {
                            var wrapper = a as ChatWrapper;
                            if (wrapper is null)
                            {
                                return false;
                            }

                            return wrapper.Chat.Status.In(ChatStatusEnum.Ready, ChatStatusEnum.Failed);
                        });
                }

                return _openInEditorCommand;
            }
        }

        public ICommand RemoveCommand
        {
            get
            {
                if (_removeCommand == null)
                {
                    _removeCommand = new RelayCommand(
                        a =>
                        {
                            _chatContainer.RemoveChatAsync(_selectedChat.Chat)
                                .FileAndForget(nameof(ChatContainer.RemoveChatAsync));
                        },
                        a => _selectedChat is not null && _selectedChat.Chat.Status.In(ChatStatusEnum.Failed, ChatStatusEnum.NotStarted, ChatStatusEnum.Ready)
                        );
                }

                return _removeCommand;
            }
        }

        public ICommand StopCommand
        {
            get
            {
                if (_stopCommand == null)
                {
                    _stopCommand = new RelayCommand(
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

                return _stopCommand;
            }
        }
        

        public ICommand StartChatCommand
        {
            get
            {
                if (_startChatCommand == null)
                {
                    _startChatCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                                "Choose agent (with a non-empty token) for a chat:"
                                );
                            if (chosenAgent is null)
                            {
                                return;
                            }

                            _ = await _chatContainer.StartChatAsync(
                                new ChatDescription(ChatKindEnum.Discussion, null),
                                null,
                                await FreeAIr.BLogic.ChatOptions.GetDefaultAsync(chosenAgent)
                                );

                            FocusPromptControl();

                            OnPropertyChanged();
                        }
                        );
                }

                return _startChatCommand;
            }
        }

        public ICommand CreatePromptCommand
        {
            get
            {
                if (_createPromptCommand == null)
                {
                    _createPromptCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            if (_selectedChat is null)
                            {
                                return;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready, ChatStatusEnum.Failed))
                            {
                                return;
                            }

                            var parsed = a as Parsed;
                            if (parsed is null)
                            {
                                return;
                            }

                            //добавляем в корзину итемы, которые пользователь перечислил
                            //в промпте, но которых нет в контексте
                            AddContextItemsFromPrompt(chat, parsed);

                            var parsedRepresentation = await parsed.ComposeStringRepresentationAsync();
                            if (string.IsNullOrEmpty(parsedRepresentation))
                            {
                                return;
                            }

                            chat.AddPrompt(
                                UserPrompt.CreateTextBasedPrompt(
                                    parsedRepresentation
                                    )
                                );

                            OnPropertyChanged();
                        },
                        (a) =>
                        {
                            if (_selectedChat is null)
                            {
                                return false;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return false;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready, ChatStatusEnum.Failed))
                            {
                                return false;
                            }

                            var parsed = a as Parsed;
                            if (parsed is null)
                            {
                                return false;
                            }

                            return true;
                        }
                        );
                }

                return _createPromptCommand;
            }
        }

        #region chat context

        public ObservableCollection2<ChatContextItemViewModel> ChatContextItems
        {
            get
            {
                if (SelectedChat is null)
                {
                    return new ObservableCollection2<ChatContextItemViewModel>();
                }
                if (SelectedChat.Chat is null)
                {
                    return new ObservableCollection2<ChatContextItemViewModel>();
                }

                var r = new ObservableCollection2<ChatContextItemViewModel>();
                r.AddRange(
                    SelectedChat.Chat.ChatContext.Items.ConvertAll(c => new ChatContextItemViewModel(c))
                    );

                return r;
            }
        }

        public ICommand OpenContextItemCommand
        {
            get
            {
                if (_openContextItemCommand == null)
                {
                    _openContextItemCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            if (a is not ChatContextItemViewModel item)
                            {
                                return;
                            }

                            await item.ContextItem.OpenInNewWindowAsync();

                            OnPropertyChanged();
                        }
                        );
                }

                return _openContextItemCommand;
            }
        }

        public ICommand DeleteItemFromContextCommand
        {
            get
            {
                if (_deleteItemFromContextCommand == null)
                {
                    _deleteItemFromContextCommand = new RelayCommand(
                        a =>
                        {
                            if (_selectedChat is null)
                            {
                                return;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return;
                            }

                            if (a is not ChatContextItemViewModel itemViewModel)
                            {
                                return;
                            }

                            chat.ChatContext.RemoveItem(itemViewModel.ContextItem);

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_selectedChat is null)
                            {
                                return false;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return false;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return false;
                            }

                            if (a is not ChatContextItemViewModel itemViewModel)
                            {
                                return false;
                            }

                            return true;
                        }
                        );
                }

                return _deleteItemFromContextCommand;
            }
        }

        public ICommand AddItemToContextCommand
        {
            get
            {
                if (_addItemToContextCommand == null)
                {
                    _addItemToContextCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            if (_selectedChat is null)
                            {
                                return;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return;
                            }

                            var parsed = a as Parsed;
                            if (parsed is null)
                            {
                                return;
                            }

                            var parsedRepresentation = await parsed.ComposeStringRepresentationAsync();
                            if (string.IsNullOrEmpty(parsedRepresentation))
                            {
                                return;
                            }

                            AddContextItemsFromPrompt(chat, parsed);

                            FocusContextControl();

                            OnPropertyChanged();
                        },
                        (a) =>
                        {
                            if (_selectedChat is null)
                            {
                                return false;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return false;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return false;
                            }

                            var parsed = a as Parsed;
                            if (parsed is null)
                            {
                                return false;
                            }

                            return true;
                        }
                        );
                }

                return _addItemToContextCommand;
            }
        }

        public ICommand RemoveAllAutomaticItemsFromContextCommand
        {
            get
            {
                if (_removeAllAutomaticItemsFromContextCommand == null)
                {
                    _removeAllAutomaticItemsFromContextCommand = new RelayCommand(
                        a =>
                        {
                            if (_selectedChat is null)
                            {
                                return;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return;
                            }

                            chat.ChatContext.RemoveAutomaticItems();

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_selectedChat is null)
                            {
                                return false;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return false;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return false;
                            }

                            return true;
                        }
                        );
                }

                return _removeAllAutomaticItemsFromContextCommand;
            }
        }

        public ICommand OpenControlCenterCommand
        {
            get
            {
                if (_openControlCenterCommand == null)
                {
                    _openControlCenterCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await Commands.Other.OpenControlCenterCommand.ShowAsync(
                                );
                            OnPropertyChanged();
                        }
                        );
                }

                return _openControlCenterCommand;
            }
        }

        public string ChosenChatAgentText
        {
            get
            {
                if (_selectedChat is not null)
                {
                    var agent = _selectedChat.Chat.Options.ChosenAgent;
                    if (agent is not null && !string.IsNullOrEmpty(agent.Name))
                    {
                        return $"Change chat agent ({agent.Name})...";
                    }
                }

                return "Choose chat agent...";
            }
        }

        public ICommand ChooseChatAgentCommand
        {
            get
            {
                if (_chooseChatAgentCommand == null)
                {
                    _chooseChatAgentCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var w = new NestedCheckBoxWindow();
                            var vm = new ChooseChatAgentViewModel(
                                _selectedChat.Chat.Options.ChatAgents,
                                _selectedChat.Chat.Options.ChosenAgent
                                );
                            w.DataContext = vm;
                            _ = await w.ShowDialogAsync();

                            _selectedChat.Chat.Options.ChangeChosenAgent(
                                vm.ChosenAgent
                                );

                            OnPropertyChanged();
                        },
                        (a) =>
                        {
                            if (_selectedChat is null)
                            {
                                return false;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return false;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return false;
                            }

                            return true;
                        }
                        );
                }

                return _chooseChatAgentCommand;
            }
        }

        public ICommand EditChatToolsCommand
        {
            get
            {
                if (_editChatToolsCommand == null)
                {
                    _editChatToolsCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var w = new NestedCheckBoxWindow();
                            w.DataContext = new AvailableToolsViewModel(
                                _selectedChat.Chat.ChatTools
                                );
                            _ = await w.ShowDialogAsync();

                            OnPropertyChanged();
                        },
                        (a) =>
                        {
                            if (_selectedChat is null)
                            {
                                return false;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return false;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return false;
                            }

                            return true;
                        }
                        );
                }

                return _editChatToolsCommand;
            }
        }

        public ICommand AddRelatedItemsToContextCommand
        {
            get
            {
                if (_addRelatedItemsToContextCommand == null)
                {
                    _addRelatedItemsToContextCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            if (_selectedChat is null)
                            {
                                return;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return;
                            }

                            if (a is not ChatContextItemViewModel itemViewModel)
                            {
                                return;
                            }

                            try
                            {
                                var backgroundTask = new AddRelatedItemsToContextBackgroundTask(
                                    itemViewModel
                                    );
                                var w = new WaitForTaskWindow(
                                    backgroundTask
                                    );
                                await w.ShowDialogAsync();

                                if (backgroundTask.Result is not null)
                                {
                                    if (backgroundTask.Result.Count > 0)
                                    {
                                        chat.ChatContext.AddItems(
                                            backgroundTask.Result
                                            );
                                    }
                                    else
                                    {
                                        await VS.MessageBox.ShowAsync(
                                            "No dependencies found. This may occurs because Intellisense is not loaded (yet), or the context item is not a .Net source code.",
                                            buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                                            );
                                    }
                                }
                                else
                                {
                                    await VS.MessageBox.ShowAsync(
                                        "Unknown error occurred during scanning for dependencies",
                                        buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                                        );
                                }
                            }
                            catch (Exception excp)
                            {
                                //todo log

                                await VS.MessageBox.ShowErrorAsync(
                                    Resources.Resources.Error,
                                    $"Error occurred: {excp.Message}{Environment.NewLine}{excp.StackTrace}"
                                    );
                            }

                            OnPropertyChanged();
                        },
                        (a) =>
                        {
                            if (_selectedChat is null)
                            {
                                return false;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return false;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return false;
                            }

                            if (a is not ChatContextItemViewModel itemViewModel)
                            {
                                return false;
                            }

                            return true;
                        }
                        );
                }

                return _addRelatedItemsToContextCommand;
            }
        }

        
        public ICommand AddCustomFileToContextCommand
        {
            get
            {
                if (_addCustomFileToContextCommand == null)
                {
                    _addCustomFileToContextCommand = new RelayCommand(
                        a =>
                        {
                            if (_selectedChat is null)
                            {
                                return;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return;
                            }

                            var ofd = new OpenFileDialog();
                            ofd.CheckFileExists = true;
                            ofd.CheckPathExists = true;
                            ofd.Filter = "TXT Files(*.txt;)|*.txt;|Other Files(*.*)|*.*";
                            var sw = ofd.ShowDialog();
                            if (!sw.GetValueOrDefault(false))
                            {
                                return;
                            }

                            var contextItem = new CustomFileContextItem(
                                ofd.FileName,
                                false
                                );

                            chat.ChatContext.AddItem(
                                contextItem
                                );

                            FocusContextControl();

                            OnPropertyChanged();
                        },
                        (a) =>
                        {
                            if (_selectedChat is null)
                            {
                                return false;
                            }
                            if (_selectedChat.Chat is null)
                            {
                                return false;
                            }

                            var chat = _selectedChat.Chat;

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return false;
                            }

                            return true;
                        }
                        );
                }

                return _addCustomFileToContextCommand;
            }
        }

        private void AddContextItemsFromPrompt(
            Chat chat,
            Parsed parsed
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            if (parsed is null)
            {
                throw new ArgumentNullException(nameof(parsed));
            }

            foreach (var part in parsed.Parts)
            {
                var contextItem = part.TryCreateChatContextItem();
                if (contextItem is null)
                {
                    continue;
                }

                chat.ChatContext.AddItem(
                    contextItem
                    );
            }
        }

        #endregion


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

            ChatList = new ObservableCollection2<ChatWrapper>();

            DialogViewModel = new DialogViewModel(
                chatContainer
                );

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



                if (e.Chat is not null)
                {
                    if (e.Chat.Status.NotIn(ChatStatusEnum.WaitingForAnswer, ChatStatusEnum.ReadingAnswer))
                    {
                        FocusPromptControl();
                    }
                }
            }
            catch (Exception excp)
            {
                //todo
            }
        }

        private void FocusContextControl()
        {
            var pcf = ContextControlFocus;
            if (pcf is not null)
            {
                pcf();
            }
        }

        private void FocusPromptControl()
        {
            var pcf = PromptControlFocus;
            if (pcf is not null)
            {
                pcf();
            }
        }

        private void UpdateControl()
        {
            ChatList.Clear();
            ChatList.AddRange(
                _chatContainer.Chats.Reverse().Select(t => new ChatWrapper(t))
                );

            SelectedChat = ChatList.FirstOrDefault();
        }

        public sealed class ChatWrapper : BaseViewModel
        {
            public Chat Chat
            {
                get;
            }

            public ImageMoniker StatusMoniker
            {
                get
                {
                    if (Chat.Status.In(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                    {
                        return KnownMonikers.Pause;
                    }
                    if (Chat.Status.In(ChatStatusEnum.WaitingForAnswer, ChatStatusEnum.ReadingAnswer))
                    {
                        return KnownMonikers.Run;
                    }
                    if (Chat.Status == ChatStatusEnum.Failed)
                    {
                        return KnownMonikers.StatusErrorOutline;
                    }

                    return KnownMonikers.QuestionMark;
                }
            }

            public bool IsReadyToAcceptNewPrompt
            {
                get
                {
                    return Chat.Status.In(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready, ChatStatusEnum.Failed);
                }
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
                        ? "Started: " + Chat.Started.Value.ToString()
                        : "Not started"
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

            public ChatWrapper(
                Chat chat
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

        private sealed class AddRelatedItemsToContextBackgroundTask : BackgroundTask
        {
            private readonly ChatContextItemViewModel _viewModel;

            public override string TaskDescription => "Please wait for the code dependencies scanning...";

            public IReadOnlyList<IChatContextItem>? Result
            {
                get;
                private set;
            }

            public AddRelatedItemsToContextBackgroundTask(
                ChatContextItemViewModel viewModel
                )
            {
                if (viewModel is null)
                {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _viewModel = viewModel;

                StartAsyncTask();
            }

            protected override async Task RunWorkingTaskAsync(
                )
            {
                //in case of exception set it null first
                Result = null;

                Result = await _viewModel.ContextItem.SearchRelatedContextItemsAsync();
            }
        }

    }

    public sealed class ChatContextItemViewModel : BaseViewModel
    {
        public IChatContextItem ContextItem
        {
            get;
        }

        public string ChatContextDescription => ContextItem.ContextUIDescription;

        public ImageMoniker Moniker =>
            ContextItem.IsAutoFound
                ? KnownMonikers.Computer
                : KnownMonikers.User
                ;

        public string Tooltip =>
            ContextItem.IsAutoFound
                ? "This item came from software logic"
                : "This item came from user"
                ;

        public ChatContextItemViewModel(
            IChatContextItem contextItem
            )
        {
            if (contextItem is null)
            {
                throw new ArgumentNullException(nameof(contextItem));
            }

            ContextItem = contextItem;
        }
    }

    public delegate void MarkdownReReadDelegate(object sender, EventArgs e);

}
