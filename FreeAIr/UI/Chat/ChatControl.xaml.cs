using FreeAIr.Antlr.Context;
using FreeAIr.Antlr.Prompt;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using FreeAIr.UI.Embedillo.VisualLine.Command;
using FreeAIr.UI.Embedillo.VisualLine.SolutionItem;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using FreeAIr.UI.Windows;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfHelpers;
using static FreeAIr.UI.ViewModels.ChatListViewModel;

namespace FreeAIr.UI.Chat
{
    public partial class ChatControl : UserControl, INotifyPropertyChanged
    {

        public static readonly DependencyProperty ChatProperty =
            DependencyProperty.Register(
                nameof(Chat),
                typeof(FreeAIr.BLogic.Chat),
                typeof(ChatControl),
                new PropertyMetadata(OnChatPropertyChanged)
                );

        #region Dependency properties changes callbacks

        private static void OnChatPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
            )
        {
            var control = d as ChatControl;

            if (e.OldValue is FreeAIr.BLogic.Chat ochat)
            {
                ochat.ChatStatusChangedEvent -= control.ChatStatusChangedEvent;
                //ochat.PromptStateChangedEvent.Event -= control.PromptStateChangedEvent_Event;
                control._chat = null;

                control.DialogViewModel.UpdateDialog(null);
            }

            if (e.NewValue is FreeAIr.BLogic.Chat nchat)
            {
                nchat.ChatStatusChangedEvent += control.ChatStatusChangedEvent;
                //nchat.PromptStateChangedEvent.Event += control.PromptStateChangedEvent_Event;

                control._chat = nchat;
                control.DialogViewModel.UpdateDialog(nchat);
            }


            control.OnPropertyChanged();
            control.FocusPromptControl();
        }

        #endregion

        private ICommand _chooseChatAgentCommand;
        private ICommand _editChatToolsCommand;
        private ICommand _addCustomFileToContextCommand;
        private ICommand _removeAllAutomaticItemsFromContextCommand;
        private ICommand _deleteItemFromContextCommand;
        private ICommand _addRelatedItemsToContextCommand;
        private ICommand _openContextItemCommand;
        private ICommand _addItemToContextCommand;
        private ICommand _createPromptCommand;
        private ICommand _stopCommand;

        private FreeAIr.BLogic.Chat? _chat;

        /// <summary>
        /// Событие изменения свойства
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;


        /// <summary>
        /// Called when any child window is opened or closed.
        /// </summary>
        public Action<bool>? ChildWindowAction
        {
            get;
            set;
        }

        #region commands

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
                                _chat.Options.ChatAgents,
                                _chat.Options.ChosenAgent
                                );
                            w.DataContext = vm;
                            await ShowDialogAsync(w);

                            _chat.Options.ChangeChosenAgent(
                                vm.ChosenAgent
                                );

                            OnPropertyChanged();
                        },
                        (a) =>
                        {
                            if (_chat is null)
                            {
                                return false;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
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
                                _chat.ChatTools
                                );
                            await ShowDialogAsync(w);

                            OnPropertyChanged();
                        },
                        (a) =>
                        {
                            if (_chat is null)
                            {
                                return false;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
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

        public ICommand AddCustomFileToContextCommand
        {
            get
            {
                if (_addCustomFileToContextCommand == null)
                {
                    _addCustomFileToContextCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            if (_chat is null)
                            {
                                return;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return;
                            }

                            var ofd = new OpenFileDialog();
                            ofd.CheckFileExists = true;
                            ofd.CheckPathExists = true;
                            ofd.Multiselect = true;
                            ofd.Filter =
                                "All Files(*.*)|*.*"
                                + "|Text Files|"
                                + string.Join(
                                    ";",
                                    FileTypeHelper.TextFileExtensions.Select(e => "*" + e)
                                    )
                                ;

                            var solution = await VS.Solutions.GetCurrentSolutionAsync();
                            if (solution is not null)
                            {
                                var sfi = new FileInfo(solution.FullPath);
                                ofd.InitialDirectory = sfi.Directory.FullName;
                            }

                            var sw = ShowDialog(ofd);
                            if (!sw.GetValueOrDefault(false))
                            {
                                return;
                            }

                            foreach (var fileName in ofd.FileNames)
                            {
                                var contextItem = new CustomFileChatContextItem(
                                    fileName,
                                    false
                                    );

                                _chat.ChatContext.AddItem(
                                    contextItem
                                    );
                            }

                            FocusContextControl();

                            OnPropertyChanged();
                        },
                        (a) =>
                        {
                            if (_chat is null)
                            {
                                return false;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
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

        public ICommand RemoveAllAutomaticItemsFromContextCommand
        {
            get
            {
                if (_removeAllAutomaticItemsFromContextCommand == null)
                {
                    _removeAllAutomaticItemsFromContextCommand = new RelayCommand(
                        a =>
                        {
                            if (_chat is null)
                            {
                                return;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return;
                            }

                            _chat.ChatContext.RemoveAutomaticItems();

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_chat is null)
                            {
                                return false;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
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

        public ICommand DeleteItemFromContextCommand
        {
            get
            {
                if (_deleteItemFromContextCommand == null)
                {
                    _deleteItemFromContextCommand = new RelayCommand(
                        a =>
                        {
                            if (_chat is null)
                            {
                                return;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
                            {
                                return;
                            }

                            if (a is not ChatContextItemViewModel itemViewModel)
                            {
                                return;
                            }

                            _chat.ChatContext.RemoveItem(itemViewModel.ContextItem);

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_chat is null)
                            {
                                return false;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
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

        public ICommand AddRelatedItemsToContextCommand
        {
            get
            {
                if (_addRelatedItemsToContextCommand == null)
                {
                    _addRelatedItemsToContextCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            if (_chat is null)
                            {
                                return;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
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
                                await ShowDialogAsync(w);

                                if (backgroundTask.Result is not null)
                                {
                                    if (backgroundTask.Result.Count > 0)
                                    {
                                        _chat.ChatContext.AddItems(
                                            backgroundTask.Result
                                            );
                                    }
                                    else
                                    {
                                        await VS.MessageBox.ShowAsync(
                                            FreeAIr.Resources.Resources.No_dependencies_found__This_may_occurs,
                                            buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                                            );
                                    }
                                }
                                else
                                {
                                    await VS.MessageBox.ShowAsync(
                                        FreeAIr.Resources.Resources.Unknown_error_occurred_during_scanning,
                                        buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                                        );
                                }
                            }
                            catch (Exception excp)
                            {
                                excp.ActivityLogException();

                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    $"{FreeAIr.Resources.Resources.Error_occurred}: {excp.Message}{Environment.NewLine}{excp.StackTrace}"
                                    );
                            }

                            OnPropertyChanged();
                        },
                        (a) =>
                        {
                            if (_chat is null)
                            {
                                return false;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
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

        public ICommand AddItemToContextCommand
        {
            get
            {
                if (_addItemToContextCommand == null)
                {
                    _addItemToContextCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            if (_chat is null)
                            {
                                return;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
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

                            AddContextItemsFromPrompt(_chat, parsed);

                            FocusContextControl();

                            OnPropertyChanged();
                        },
                        (a) =>
                        {
                            if (_chat is null)
                            {
                                return false;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready))
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

        public ICommand CreatePromptCommand
        {
            get
            {
                if (_createPromptCommand == null)
                {
                    _createPromptCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            if (_chat is null)
                            {
                                return;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready, ChatStatusEnum.Failed))
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
                            AddContextItemsFromPrompt(_chat, parsed);

                            var parsedRepresentation = await parsed.ComposeStringRepresentationAsync();
                            if (string.IsNullOrEmpty(parsedRepresentation))
                            {
                                return;
                            }

                            _chat.AddPrompt(
                                UserPrompt.CreateTextBasedPrompt(
                                    parsedRepresentation
                                    )
                                );

                            OnPropertyChanged();
                        },
                        (a) =>
                        {
                            if (_chat is null)
                            {
                                return false;
                            }

                            if (_chat.Status.NotIn(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready, ChatStatusEnum.Failed))
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

        public ICommand StopCommand
        {
            get
            {
                if (_stopCommand == null)
                {
                    _stopCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
                            var chatContainer = componentModel.GetService<ChatContainer>();

                            chatContainer.StopChatAsync(_chat)
                                .FileAndForget(nameof(ChatContainer.StopChatAsync));
                        },
                        a =>
                        {
                            if (_chat is null)
                            {
                                return false;
                            }

                            return _chat.Status.In(ChatStatusEnum.WaitingForAnswer, ChatStatusEnum.ReadingAnswer);
                        });
                }

                return _stopCommand;
            }
        }

        #endregion


        public FreeAIr.BLogic.Chat? Chat
        {
            get => (FreeAIr.BLogic.Chat)GetValue(ChatProperty);
            set
            {
                SetValue(ChatProperty, value);
            }
        }

        public ObservableCollection2<ChatContextItemViewModel> ChatContextItems
        {
            get
            {
                if (_chat is null)
                {
                    return new ObservableCollection2<ChatContextItemViewModel>();
                }

                var r = new ObservableCollection2<ChatContextItemViewModel>();
                r.AddRange(
                    _chat.ChatContext.Items.ConvertAll(c => new ChatContextItemViewModel(c))
                    );

                return r;
            }
        }

        public bool IsReadyToAcceptNewPrompt
        {
            get
            {
                if (_chat is null)
                {
                    return false;
                }
                if (_chat.Options.AutomaticallyProcessed)
                {
                    return false;
                }

                return _chat.Status.In(ChatStatusEnum.NotStarted, ChatStatusEnum.Ready, ChatStatusEnum.Failed);
            }
        }

        public string ChatAgentText
        {
            get
            {
                if (_chat is not null)
                {
                    var agent = _chat.Options.ChosenAgent;
                    if (agent is not null && !string.IsNullOrEmpty(agent.Name))
                    {
                        return string.Format(FreeAIr.Resources.Resources.Choose_chat_agent, $" ({agent.Name})");
                    }
                }

                return string.Format(FreeAIr.Resources.Resources.Choose_chat_agent, string.Empty);
            }
        }

        public DialogViewModel DialogViewModel
        {
            get;
        }

        public ImageMoniker StatusMoniker
        {
            get
            {
                if (_chat is null)
                {
                    return KnownMonikers.Pause;
                }

                return ChatWrapper.GetStatusMoniker(_chat);
            }
        }


        public ChatControl()
        {
            InitializeComponent();

            DialogViewModel = new DialogViewModel(
                );

            SetupPromptControl();

            SetupAddToContextControl();
        }

        private void ChatStatusChangedEvent(object sender, ChatEventArgs e)
        {
            OnPropertyChanged();
        }

        private void AddContextItemsFromPrompt(
            FreeAIr.BLogic.Chat chat,
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

        #region show child windows

        private bool? ShowDialog(CommonDialog d)
        {
            try
            {
                ChildWindowAction?.Invoke(true);

                return d.ShowDialog();
            }
            finally
            {
                ChildWindowAction?.Invoke(false);
            }
        }

        private async Task ShowDialogAsync(Window w)
        {
            try
            {
                ChildWindowAction?.Invoke(true);

                _ = await w.ShowDialogAsync();
            }
            finally
            {
                ChildWindowAction?.Invoke(false);
            }
        }

        #endregion

        #region setup

        private void SetupAddToContextControl()
        {
            AddToContextControl.Setup(
                new ContextParser(
                    new SolutionItemVisualLineGeneratorFactory()
                    )
                );
        }

        private void SetupPromptControl()
        {
            PromptControl.Setup(
                new PromptParser(
                    new SolutionItemVisualLineGeneratorFactory(),
                    new CommandVisualLineGeneratorFactory()
                    )
                );
        }

        #endregion

        #region focus

        public void FocusContextControl()
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                AddToContextControl.MakeFocused();
            });
        }

        public void FocusPromptControl()
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                PromptControl.MakeFocused();
            });
        }

        #endregion

        #region drag and drop

        private void EmbedilloControl_Drop(object sender, DragEventArgs e)
        {
            var solutionItemsPaths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (solutionItemsPaths is null || solutionItemsPaths.Length == 0)
            {
                return;
            }

            var embedillo = sender as Embedillo.EmbedilloControl;
            if (embedillo is null)
            {
                return;
            }

            e.Handled = true;

            AddMovedFilesAndTheirDescendantsToChatPromptAsync(
                embedillo,
                solutionItemsPaths
                ).FileAndForget(nameof(AddMovedFilesAndTheirDescendantsToChatPromptAsync));
        }

        private async System.Threading.Tasks.Task AddMovedFilesAndTheirDescendantsToChatPromptAsync(
            Embedillo.EmbedilloControl embedillo,
            string[] solutionItemsPaths
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var children = await ChatListToolWindowControl.GetSolutionItemsWithChildrenAsync(solutionItemsPaths);

            foreach (var child in children)
            {
                if (
                    !string.IsNullOrEmpty(embedillo.AvalonTextEditor.Text)
                    && !char.IsWhiteSpace(embedillo.AvalonTextEditor.Text[embedillo.AvalonTextEditor.Text.Length - 1]))
                {
                    embedillo.AvalonTextEditor.Text += " ";
                }

                embedillo.AvalonTextEditor.Text += SolutionItemVisualLineGenerator.Anchor + child.FullPath;
            }

            embedillo.AvalonTextEditor.CaretOffset = embedillo.AvalonTextEditor.Text.Length;

            embedillo.UpdateHintStatus();
        }

        #endregion

        #region OnPropertyChanged

        /// <summary>
        /// Активация евента изменения бинденого свойства
        /// </summary>
        private void OnPropertyChanged()
        {
            OnPropertyChanged(string.Empty);
        }

        /// <summary>
        /// Активация евента изменения бинденого свойства
        /// </summary>
        private void OnPropertyChanged(string propertyName)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        #endregion
    }
}
