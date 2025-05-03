using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.Commands;
using FreeAIr.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private string _promptText;

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
                    _startDiscussionCommand = new RelayCommand(
                        a =>
                        {
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

        public bool IsEnabledPrompt
        {
            get
            {
                return
                    _selectedChat is not null
                    && _selectedChat.Chat.Status.In(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready)
                    ;
            }
        }

        public string PromptText
        {
            get => _promptText;
            set => _promptText = value;
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
                            _selectedChat.Chat.AddPrompt(
                                UserPrompt.CreateTextBasedPrompt(
                                    PromptText
                                    )
                                );

                            PromptText = string.Empty;
                            OnPropertyChanged();
                        }
                        );
                }

                return _createPromptCommand;
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

                OnPropertyChanged();
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
                    return Chat.Description.Kind.AsShortString();
                }
            }

            public string SecondRow
            {
                get
                {
                    return Chat.Description?.FileName ?? string.Empty;
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
                    return Chat.Status.AsString();
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

    public delegate void MarkdownReReadDelegate(object sender, EventArgs e);
}
