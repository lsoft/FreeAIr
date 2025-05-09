using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.Commands;
using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
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
        private ICommand _startDiscussionCommand;
        private ICommand _copyCodeBlockCommand;
        private ICommand _replaceSelectedTextCommand;
        private ICommand _createNewFileCommand;
        private ICommand _deleteItemFromContextCommand;
        private ICommand _openContextItemCommand;
        private ICommand _addItemToContextCommand;

        public event MarkdownReReadDelegate MarkdownReReadEvent;

        public ObservableCollection2<ChatWrapper> ChatList
        {
            get;
        }

        public ChatWrapper? SelectedChat
        {
            get => _selectedChat;
            set
            {
                _selectedChat = value;

                OnPropertyChanged();
            }
        }

        public string SelectedChatResponse
        {
            get
            {
                RaiseMarkdownReReadEvent();

                if (_selectedChat is null)
                {
                    return string.Empty;
                }

                return _selectedChat.Chat.ReadResponse();
            }
        }

        public ObservableCollection2<ChatContextItemViewModel> ChatContextItems
        {
            get;
        } = new();

        public ICommand OpenInEditorCommand
        {
            get
            {
                if (_openInEditorCommand == null)
                {
                    _openInEditorCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await VS.Documents.OpenAsync(_selectedChat.Chat.ResultFilePath);
                        },
                        a => _selectedChat is not null && _selectedChat.Chat.Status == ChatStatusEnum.Ready
                        );
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
                        a => _selectedChat is not null && _selectedChat.Chat.Status.In(ChatStatusEnum.Failed, ChatStatusEnum.NotStarted, ChatStatusEnum.Cancelled, ChatStatusEnum.Ready)
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
                            _chatContainer.StopChatAsync(_selectedChat.Chat)
                                .FileAndForget(nameof(ChatContainer.StopChatAsync));
                        },
                        a => _selectedChat is not null && _selectedChat.Chat.Status.In(ChatStatusEnum.WaitingForAnswer, ChatStatusEnum.ReadingAnswer)
                        );
                }

                return _stopCommand;
            }
        }
        

        public ICommand StartDiscussionCommand
        {
            get
            {
                if (_startDiscussionCommand == null)
                {
                    _startDiscussionCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            if (!await ApiPage.Instance.VerifyUriAndShowErrorIfNotAsync())
                            {
                                return;
                            }

                            _chatContainer.StartChat(
                                new ChatDescription(ChatKindEnum.Discussion, null),
                                null
                                );
                        }
                        );
                }

                return _startDiscussionCommand;
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
                                    "Resources.Resources.Error",
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
                                    "Resources.Resources.Error",
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            var codeText = textArea?.Document?.Text;
                            if (codeText is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    "Resources.Resources.Error",
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            var chat = SelectedChat?.Chat;
                            if (chat is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    "Resources.Resources.Error",
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            var std = chat.Description.SelectedTextDescriptor;
                            if (std is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    "Resources.Resources.Error",
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            if (!std.IsAbleToManipulate)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    "Resources.Resources.Error",
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            await std.ReplaceOriginalTextWithNewAsync(
                                codeText.WithLineEnding(std.LineEnding)
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
                                    "Resources.Resources.Error",
                                    "Cannot create a new file. Please create the file manually."
                                    );
                                return;
                            }

                            var codeText = textArea?.Document?.Text;
                            if (codeText is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    "Resources.Resources.Error",
                                    "Cannot create a new file. Please create the file manually."
                                    );
                                return;
                            }

                            var solution = await VS.Solutions.GetCurrentSolutionAsync();
                            if (solution is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    "Resources.Resources.Error",
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
                    _createPromptCommand = new RelayCommand(
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

                            var parsedAnswer = a as ParsedAnswer;
                            if (parsedAnswer is null)
                            {
                                return;
                            }

                            var parsedRepresentation = parsedAnswer.ComposeStringRepresentation();
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

                            if (chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return false;
                            }

                            var parsedAnswer = a as ParsedAnswer;
                            if (parsedAnswer is null)
                            {
                                return false;
                            }

                            var parsedRepresentation = parsedAnswer.ComposeStringRepresentation();
                            if (string.IsNullOrEmpty(parsedRepresentation))
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

                            await item.AnswerPart.OpenInNewWindowAsync();

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
                            if (a is not ChatContextItemViewModel item)
                            {
                                return;
                            }

                            ChatContextItems.Remove(item);

                            OnPropertyChanged();
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
                    _addItemToContextCommand = new RelayCommand(
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

                            var parsedAnswer = a as ParsedAnswer;
                            if (parsedAnswer is null)
                            {
                                return;
                            }

                            var parsedRepresentation = parsedAnswer.ComposeStringRepresentation();
                            if (string.IsNullOrEmpty(parsedRepresentation))
                            {
                                return;
                            }

                            foreach (var part in parsedAnswer.Parts)
                            {
                                if (part is not SourceFileAnswerPart filePart)
                                {
                                    continue;
                                }

                                ChatContextItems.Add(
                                    new ChatContextItemViewModel(part)
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

                            var parsedAnswer = a as ParsedAnswer;
                            if (parsedAnswer is null)
                            {
                                return false;
                            }

                            var parsedRepresentation = parsedAnswer.ComposeStringRepresentation();
                            if (string.IsNullOrEmpty(parsedRepresentation))
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


        [ImportingConstructor]
        public ChatListViewModel(
            ChatContainer chatContainer
            )
        {
            if (chatContainer is null)
            {
                throw new ArgumentNullException(nameof(chatContainer));
            }

            ChatContextItems = new ObservableCollection2<ChatContextItemViewModel>();
            ChatContextItems.Add(
                new ChatContextItemViewModel(
                    new SourceFileAnswerPart(@"C:\projects\github\SlnfUpdater\SlnfUpdater\Helper\ArrayHelper.cs")
                    )
                );
            ChatContextItems.Add(
                new ChatContextItemViewModel(
                    new SourceFileAnswerPart(@"c:\temp\1.txt")
                    )
                );

            _chatContainer = chatContainer;

            chatContainer.ChatCollectionChangedEvent += ChatCollectionChanged;
            chatContainer.ChatStatusChangedEvent += ChatStatusChanged;

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

                if (ReferenceEquals(SelectedChat.Chat, e.Chat))
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
            ChatList.AddRange(
                _chatContainer.Chats.Reverse().Select(t => new ChatWrapper(t))
                );

            SelectedChat = ChatList.FirstOrDefault();
        }

        private void RaiseMarkdownReReadEvent()
        {
            var e = MarkdownReReadEvent;
            if (e is not null)
            {
                e(this, new EventArgs());
            }
        }

        public sealed class ChatWrapper : BaseViewModel
        {
            public Chat Chat
            {
                get;
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
    }

    public sealed class ChatContextItemViewModel : BaseViewModel
    {
        public IAnswerPart AnswerPart
        {
            get;
        }

        public string ChatContextDescription => AnswerPart.ContextUIDescription;

        public ChatContextItemViewModel(
            IAnswerPart answerPart
            )
        {
            if (answerPart is null)
            {
                throw new ArgumentNullException(nameof(answerPart));
            }

            AnswerPart = answerPart;
        }
    }


    public delegate void MarkdownReReadDelegate(object sender, EventArgs e);
}
