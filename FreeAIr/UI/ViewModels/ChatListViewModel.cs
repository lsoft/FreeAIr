using FreeAIr.Antlr.Answer;
using FreeAIr.Antlr.Answer.Parts;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.MCP.Agent;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using FreeAIr.UI.Windows;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfHelpers;
using static FreeAIr.UI.Dialog.DialogControl;
using static FreeAIr.UI.ViewModels.ChatListViewModel;

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
        private ICommand _copyCodeBlockCommand;
        private ICommand _replaceSelectedTextCommand;
        private ICommand _createNewFileCommand;
        private ICommand _deleteItemFromContextCommand;
        private ICommand _openContextItemCommand;
        private ICommand _addItemToContextCommand;
        private ICommand _updateContextItemCommand;
        private ICommand _removeAllAutomaticItemsFromContextCommand;
        private ICommand _addRelatedItemsToContextCommand;
        private ICommand _addCustomFileToContextCommand;
        private ICommand _editAvailableToolsCommand;
        private ICommand _editChatToolsCommand;

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
                            if (!await ApiPage.Instance.VerifyUriAndShowErrorIfNotAsync())
                            {
                                return;
                            }

                            _ = await _chatContainer.StartChatAsync(
                                new ChatDescription(ChatKindEnum.Discussion, null),
                                null,
                                null
                                );

                            FocusPromptControl();

                            OnPropertyChanged();
                        }
                        );
                }

                return _startChatCommand;
            }
        }

        public ICommand CopyCodeBlockCommand
        {
            get
            {
                if (_copyCodeBlockCommand == null)
                {
                    _copyCodeBlockCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var textArea = a as ICSharpCode.AvalonEdit.Editing.TextArea;
                            if (textArea is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot copy code block. Please copy manually."
                                    );
                                return;
                            }

                            var codeText = textArea?.Document?.Text ?? string.Empty;
                            Clipboard.SetText(codeText);
                        }
                        );
                }

                return _copyCodeBlockCommand;
            }
        }

        public ICommand ReplaceSelectedTextCommand
        {
            get
            {
                if (_replaceSelectedTextCommand == null)
                {
                    _replaceSelectedTextCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var textArea = a as ICSharpCode.AvalonEdit.Editing.TextArea;
                            if (textArea is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            var codeText = textArea?.Document?.Text;
                            if (codeText is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            var chat = SelectedChat?.Chat;
                            if (chat is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            var std = chat.Description.SelectedTextDescriptor;
                            if (std is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            if (!std.IsAbleToManipulate)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            await std.ReplaceOriginalTextWithNewAsync(
                                codeText
                                );
                        },
                        a =>
                        {
                            var textArea = a as ICSharpCode.AvalonEdit.Editing.TextArea;
                            if (textArea is null)
                            {
                                return false;
                            }

                            var codeText = textArea?.Document?.Text;
                            if (string.IsNullOrEmpty(codeText))
                            {
                                return false;
                            }

                            var chat = SelectedChat?.Chat;
                            if (chat is null)
                            {
                                return false;
                            }

                            var std = chat.Description.SelectedTextDescriptor;
                            if (std is null)
                            {
                                return false;
                            }

                            if (!std.IsAbleToManipulate)
                            {
                                return false;
                            }

                            return true;
                        }
                        );
                }

                return _replaceSelectedTextCommand;
            }
        }

        public ICommand CreateNewFileCommand
        {
            get
            {
                if (_createNewFileCommand == null)
                {
                    _createNewFileCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var textArea = a as ICSharpCode.AvalonEdit.Editing.TextArea;
                            if (textArea is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot create a new file. Please create the file manually."
                                    );
                                return;
                            }

                            var codeText = textArea?.Document?.Text;
                            if (codeText is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot create a new file. Please create the file manually."
                                    );
                                return;
                            }

                            var solution = await VS.Solutions.GetCurrentSolutionAsync();
                            if (solution is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot create a new file. Please create the file manually."
                                    );
                                return;
                            }

                            var sfi = new FileInfo(solution.FullPath);

                            var sfd = new SaveFileDialog();
                            sfd.InitialDirectory = sfi.Directory.FullName;
                            sfd.FileName = "_.cs";
                            var r = sfd.ShowDialog();
                            if (!r.HasValue || !r.Value)
                            {
                                return;
                            }

                            var targetFilePath = sfd.FileName;

                            var lineEndings = LineEndingHelper.EditorConfig.GetLineEndingFor(targetFilePath);

                            File.WriteAllText(
                                targetFilePath,
                                codeText.WithLineEnding(lineEndings)
                                );
                        }
                        );
                }

                return _createNewFileCommand;
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

        public ICommand EditAvailableToolsCommand
        {
            get
            {
                if (_editAvailableToolsCommand == null)
                {
                    _editAvailableToolsCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var toolContainer = AvailableToolContainer.ReadSystem();

                            var w = new NestedCheckBoxWindow();
                            w.DataContext = new AvailableToolsViewModel(
                                toolContainer
                                );
                            if ((await w.ShowDialogAsync()).GetValueOrDefault())
                            {
                                toolContainer.SaveToSystem();
                            }

                            OnPropertyChanged();
                        }
                        );
                }

                return _editAvailableToolsCommand;
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

        public ICommand UpdateContextItemCommand
        {
            get
            {
                if (_updateContextItemCommand == null)
                {
                    _updateContextItemCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            if (a is not Tuple<object, object> tuple)
                            {
                                return;
                            }

                            var vm = tuple.Item1 as ChatContextItemViewModel;
                            if (vm is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot replace context document body. Please replace manually."
                                    );
                                return;
                            }

                            var textArea = tuple.Item2 as TextArea;
                            if (textArea is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot replace context document body. Please replace manually."
                                    );
                                return;
                            }

                            var codeText = textArea?.Document?.Text;
                            if (codeText is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot replace context document body. Please replace manually."
                                    );
                                return;
                            }

                            vm.ContextItem.ReplaceWithText(
                                codeText
                                );

                            OnPropertyChanged();
                        }
                        );
                }

                return _updateContextItemCommand;
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

            DialogViewModel = new DialogViewModel(chatContainer);

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

            public string FirstRow
            {
                get
                {
                    return Chat.Description.Kind.AsUIString();
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

    public class DialogViewModel : BaseViewModel
    {
        private ChatWrapper? _selectedChat;

        public ObservableCollection2<Replic> Dialog
        {
            get;
        } = new();

        public AdditionalCommandContainer AdditionalCommandContainer
        {
            get;
        } = new();

        public DialogViewModel(ChatContainer chatContainer)
        {
            if (chatContainer is null)
            {
                throw new ArgumentNullException(nameof(chatContainer));
            }

            #region AdditionalCommandContainer

            var defaultAdditionalBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6));

            AdditionalCommandContainer.AddAdditionalCommand(
                new AdditionalCommand(
                    PartTypeEnum.CodeLine | PartTypeEnum.CodeBlock | PartTypeEnum.Xml | PartTypeEnum.Url,
                    "↑",
                    "Click to copy to clipboard",
                    new RelayCommand(
                        a =>
                        {
                            var code = a as string;
                            if (!string.IsNullOrEmpty(code))
                            {
                                Clipboard.SetText(code);
                            }
                        }),
                    defaultAdditionalBrush
                    )
                );
            AdditionalCommandContainer.AddAdditionalCommand(
                new AdditionalCommand(
                    PartTypeEnum.Image,
                    "↑",
                    "Click to copy to clipboard",
                    new RelayCommand(
                        a =>
                        {
                            var bitmap = a as BitmapImage;
                            if (bitmap is not null)
                            {
                                Clipboard.SetImage(bitmap);
                            }
                        }),
                    defaultAdditionalBrush
                    )
                );

            #endregion

            chatContainer.PromptStateChangedEvent += PromptStateChanged;
        }

        public void UpdateDialog(
            ChatWrapper? selectedChat
            )
        {
            Dialog.Clear();

            _selectedChat = selectedChat;

            if (selectedChat is not null)
            {
                var chat = selectedChat.Chat;
                if (chat is not null)
                {
                    foreach (var prompt in chat.Prompts)
                    {
                        UpdateDialogPrompt(
                            prompt,
                            PromptChangeKindEnum.PromptAdded
                            );
                    }
                }
            }
        }

        private void UpdateDialogPrompt(
            UserPrompt prompt,
            PromptChangeKindEnum kind
            )
        {
            switch (kind)
            {
                case PromptChangeKindEnum.PromptAdded:
                    {
                        var promptReplic = new Replic(
                            AnswerParser.Parse(prompt.PromptBody),
                            prompt,
                            true,
                            AdditionalCommandContainer,
                            false
                            );
                        Dialog.Add(promptReplic);

                        var answerReplic = new Replic(
                            AnswerParser.Parse(prompt.Answer.GetUserVisibleAnswer()),
                            prompt,
                            false,
                            AdditionalCommandContainer,
                            false
                            );
                        Dialog.Add(answerReplic);
                    }
                    break;
                case PromptChangeKindEnum.AnswerUpdated:
                    {
                        var replic = Dialog.FirstOrDefault(d => ReferenceEquals(d.Prompt, prompt) && !d.IsPrompt);
                        if (replic is not null)
                        {
                            replic.Update(
                                AnswerParser.Parse(
                                    prompt.Answer.GetUserVisibleAnswer()
                                    ),
                                true
                                );
                        }
                    }
                    break;
                case PromptChangeKindEnum.PromptArchived:
                    break;
                default:
                    break;
            }
        }

        private async void PromptStateChanged(object sender, PromptEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_selectedChat is not null && ReferenceEquals(_selectedChat.Chat, e.Chat))
                {
                    UpdateDialogPrompt(
                        e.Prompt,
                        e.Kind
                        );
                }

            }
            catch (Exception excp)
            {
                //todo
            }
        }

    }

}
