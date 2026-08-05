using FreeAIr.Antlr.Context;
using FreeAIr.Antlr.Prompt;
using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.Options2.Agent;
using FreeAIr.Record;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Embedillo.Answer.Parser;
using FreeAIr.UI.Embedillo.VisualLine.Command;
using FreeAIr.UI.Embedillo.VisualLine.SolutionItem;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfHelpers;
using static FreeAIr.UI.ViewModels.ChatListViewModel;
using FreeAIr.Chat;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.UI.Chat
{
    /// <summary>
    /// The main chat panel: shows the dialog history, the prompt editor, the context basket and the
    /// recording button, and wires them all to the bound <see cref="FreeAIr.Chat.Chat"/> instance.
    /// </summary>
    public partial class ChatControl : UserControl, INotifyPropertyChanged
    {

        /// <summary>
        /// WPF dependency property backing <see cref="Chat"/>; changing it swaps which
        /// <see cref="FreeAIr.Chat.Chat"/> the control displays and rewires its status events.
        /// </summary>
        public static readonly DependencyProperty ChatProperty =
            DependencyProperty.Register(
                nameof(Chat),
                typeof(FreeAIr.Chat.Chat),
                typeof(ChatControl),
                new PropertyMetadata(OnChatPropertyChanged)
                );

        #region Dependency properties changes callbacks

        /// <summary>
        /// Detaches status handlers from the previously bound chat and attaches them to the new one,
        /// so the control's status moniker and command availability track whichever chat is active.
        /// </summary>
        private static void OnChatPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
            )
        {
            var control = d as ChatControl;

            if (e.OldValue is FreeAIr.Chat.Chat ochat)
            {
                ochat.ChatStatusChangedEvent -= control.ChatStatusChangedEvent;
                //ochat.PromptStateChangedEvent.Event -= control.PromptStateChangedEvent_Event;
                control._chat = null;

                control.DialogViewModel.UpdateDialog(null);
            }

            if (e.NewValue is FreeAIr.Chat.Chat nchat)
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

        /// <summary>
        /// The chat currently bound to this control, kept in sync with <see cref="Chat"/> so command
        /// handlers do not have to go through the dependency property indirection.
        /// </summary>
        private FreeAIr.Chat.Chat? _chat;

        /// <summary>
        /// Событие изменения свойства
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;


        ///// <summary>
        ///// Called when any child window is opened or closed.
        ///// </summary>
        //public Action<bool>? ChildWindowAction
        //{
        //    get;
        //    set;
        //}

        #region commands

        /// <summary>
        /// Opens the agent picker so the user can switch which chat agent (<see cref="AgentJson"/>)
        /// answers this chat; enabled only while the chat is idle or ready for a new prompt.
        /// </summary>
        public ICommand ChooseChatAgentCommand
        {
            get
            {
                if (field == null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            var chosenAgent = await VisualStudioContextMenuCommandBridge.ShowAsync<AgentJson>(
                                FreeAIr.Resources.Resources.Choose_the_available_agent,
                                _chat.Options.ChatAgents.Agents
                                    .FindAll(a => a.Technical.HasToken())
                                    .ConvertAll(a => (a.Name, a.Name == _chat.Options.ChosenAgent.Name, a as object))
                                );
                            if (chosenAgent is null)
                            {
                                return;
                            }

                            _chat.Options.ChangeChosenAgent(
                                chosenAgent
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

                return field;
            }
        }

        /// <summary>
        /// Opens the nested checkbox dialog used to enable or disable individual MCP tools available
        /// to this chat.
        /// </summary>
        public ICommand EditChatToolsCommand
        {
            get
            {
                if (field == null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Shows a file-open dialog and adds each selected file to the chat context as a
        /// <see cref="CustomFileChatContextItem"/>, letting the user attach arbitrary files to the prompt.
        /// </summary>
        public ICommand AddCustomFileToContextCommand
        {
            get
            {
                if (field == null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Clears every automatically added item from the chat context basket, keeping only the ones
        /// the user added by hand.
        /// </summary>
        public ICommand RemoveAllAutomaticItemsFromContextCommand
        {
            get
            {
                if (field == null)
                {
                    field = new RelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Removes a single item, identified by its <see cref="ChatContextItemViewModel"/>, from the
        /// chat context basket.
        /// </summary>
        public ICommand DeleteItemFromContextCommand
        {
            get
            {
                if (field == null)
                {
                    field = new RelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Scans the solution for items related to a given context item (e.g. referencing files) via
        /// <see cref="AddRelatedItemsToContextBackgroundTask"/> and adds any found to the chat context,
        /// showing progress in a wait dialog and reporting when nothing was found.
        /// </summary>
        public ICommand AddRelatedItemsToContextCommand
        {
            get
            {
                if (field == null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Opens a chat context item (file, snippet, or other solution item) in its own editor window.
        /// </summary>
        public ICommand OpenContextItemCommand
        {
            get
            {
                if (field == null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Adds the mentions and file references parsed from the "add to context" input line
        /// (a <see cref="Parsed"/> prompt) into the chat context basket without sending a prompt.
        /// </summary>
        public ICommand AddItemToContextCommand
        {
            get
            {
                if (field == null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Sends the text typed into the prompt editor as a new user prompt to the chat, first
        /// resolving any file/solution-item mentions in the text into context items.
        /// </summary>
        public ICommand CreatePromptCommand
        {
            get
            {
                if (field == null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Cancels the chat's in-flight request by asking the owning <see cref="ChatContainer"/> to
        /// stop it; enabled only while the chat is waiting for or reading an answer.
        /// </summary>
        public ICommand StopCommand
        {
            get
            {
                if (field == null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        #endregion

        /// <summary>
        /// The chat this control displays. Bound from the containing tool window; setting it triggers
        /// <see cref="OnChatPropertyChanged"/> to rewire the dialog and status events.
        /// </summary>
        public FreeAIr.Chat.Chat? Chat
        {
            get => (FreeAIr.Chat.Chat)GetValue(ChatProperty);
            set
            {
                SetValue(ChatProperty, value);
            }
        }

        /// <summary>
        /// View models for every item currently in the chat's context basket, used to populate the
        /// context list shown in the chat panel.
        /// </summary>
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

        /// <summary>
        /// True when the prompt editor should accept a new prompt: the chat is bound, not configured
        /// for automatic processing, and currently idle, ready or failed.
        /// </summary>
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

        /// <summary>
        /// Display text for the "choose chat agent" button, showing the currently chosen agent's name
        /// in parentheses when one is set.
        /// </summary>
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

        /// <summary>
        /// View model backing the message-history dialog area of this chat panel.
        /// </summary>
        public DialogViewModel DialogViewModel
        {
            get;
        }

        /// <summary>
        /// Icon shown for the chat's current status (e.g. paused, running), reflecting the bound
        /// <see cref="FreeAIr.Chat.Chat"/>'s status via <see cref="ChatWrapper.GetStatusMoniker"/>.
        /// </summary>
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


        /// <summary>
        /// Builds the control, wiring up the prompt editor, the "add to context" editor and the
        /// voice-recording pipeline.
        /// </summary>
        public ChatControl()
        {
            InitializeComponent();

            DialogViewModel = new DialogViewModel(
                );

            SetupPromptControl();

            SetupAddToContextControl();

            _rtpProcessor.RecordingStatusChangedSignal += RecordingStatusChangedSignal;
        }

        /// <summary>
        /// Refreshes the control's bindings whenever the bound chat's status changes.
        /// </summary>
        private void ChatStatusChangedEvent(object sender, ChatEventArgs e)
        {
            OnPropertyChanged();
        }

        /// <summary>
        /// Adds every mention or file reference found in a parsed prompt to the chat context basket,
        /// so items the user typed about are attached even if they never used "add to context" directly.
        /// </summary>
        private void AddContextItemsFromPrompt(
            FreeAIr.Chat.Chat chat,
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

        #region recording

        /// <summary>
        /// Drives voice recording, transcription and post-processing for this chat's prompt input,
        /// backing the microphone button and the right-Ctrl push-to-talk shortcut.
        /// </summary>
        private readonly RecorderTranscriberPostProcessor _rtpProcessor = new RecorderTranscriberPostProcessor(
            );

        /// <summary>
        /// Opens the recorder-selection menu so the user can pick which speech-to-text engine
        /// transcribes their voice prompts.
        /// </summary>
        public ICommand ChooseRecorderCommand
        {
            get
            {
                if (field == null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await ChosenRecorder.ChooseRecorderAsync();

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

                            if (!ChosenRecorder.IsReady())
                            {
                                return false;
                            }

                            if (_rtpProcessor.IsWorking)
                            {
                                return false;
                            }

                            return true;
                        }
                        );
                }

                return field;
            }
        }

        /// <summary>
        /// Icon reflecting the current stage of voice recording (idle, recording, transcribing or
        /// post-processing) shown on the microphone button.
        /// </summary>
        public ImageMoniker RecordingMoniker
        {
            get
            {
                switch (_rtpProcessor.RecordingProcessStatus)
                {
                    case RecordingProcessStatusEnum.Idle:
                        return KnownMonikers.RecordingNotStarted;
                    case RecordingProcessStatusEnum.Recording:
                        return KnownMonikers.Record;
                    case RecordingProcessStatusEnum.Transcribing:
                        return KnownMonikers.FluidLayout;
                    case RecordingProcessStatusEnum.PostProcessing:
                        return KnownMonikers.Process;
                    default:
                        return KnownMonikers.QuestionMark;
                }
            }
        }

        /// <summary>
        /// Tooltip text for the microphone button, summarizing the chosen recorder, the chosen
        /// post-process action and the current recording status.
        /// </summary>
        public string RecordingToolTip
        {
            get
            {
                var sb = new StringBuilder();

                sb.AppendLine(
                    "Chosen model: " + (ChosenRecorder.GetRecorderName() ?? "Not chosen")
                    );
                var ppn = RecordingPage.Instance.ChosenPostProcessActionName;
                sb.AppendLine(
                    "Chosen post process: " + (string.IsNullOrEmpty(ppn) ? "No post process" : ppn)
                    );
                sb.AppendLine(
                    "Status: " + _rtpProcessor.RecordingProcessStatus.ToString()
                    );
                sb.Append(
                    "Left click to choose recorder. Press and hold right Ctrl to record prompt by voice."
                    );

                return sb.ToString();
            }
        }

        /// <summary>
        /// Refreshes the recording icon and tooltip bindings whenever <see cref="_rtpProcessor"/>'s
        /// status changes.
        /// </summary>
        private void RecordingStatusChangedSignal(
            RecorderTranscriberPostProcessor sender,
            RecordingProcessStatusEnum newStatus
            )
        {
            OnPropertyChanged();
        }

        /// <summary>
        /// Records, transcribes and post-processes a voice prompt, then appends the resulting text to
        /// the prompt editor, or shows an error if transcription failed.
        /// </summary>
        private async Task StartRecordingAsync()
        {

            try
            {
                var transcribeResult = await _rtpProcessor.RecordTranscribeAndPostProcessAsync();
                if (transcribeResult is null)
                {
                    return;
                }

                if (transcribeResult.TryGetText(out var text) && !string.IsNullOrEmpty(text))
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    PromptControl.AvalonTextEditor.Text += text;
                    PromptControl.AvalonTextEditor.CaretOffset = PromptControl.AvalonTextEditor.Text.Length;
                }
                else if (transcribeResult.TryGetError(out var error))
                {
                    await VS.MessageBox.ShowErrorAsync(
                        "Error during recording",
                        error
                        );
                }
            }
            finally
            {
                OnPropertyChanged();
            }

        }

        /// <summary>
        /// Stops an in-progress voice recording, letting the pipeline move on to transcription.
        /// </summary>
        private async Task StopRecordingAsync()
        {
            await _rtpProcessor.StopRecordingAsync();
        }

        #endregion

        #region key process

        /// <summary>
        /// Routes key-down events on the chat control to the push-to-talk recording logic.
        /// </summary>
        private void ChatControlName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ProcessRecording(e);
        }

        /// <summary>
        /// Implements push-to-talk: starts recording when the right Ctrl key is first pressed, and
        /// stops it if any other key is pressed while recording is in progress.
        /// </summary>
        private void ProcessRecording(KeyEventArgs e)
        {
            if (!RecordingPage.Instance.Enabled)
            {
                return;
            }

            if (e.IsRepeat)
            {
                return;
            }

            if (e.Key == Key.RightCtrl)
            {
                if (!_rtpProcessor.IsWorking)
                {
                    //start recording
                    StartRecordingAsync()
                        .FileAndForget(nameof(StartRecordingAsync));
                }
            }
            else
            {
                //any other key pressed

                if (_rtpProcessor.IsWorking)
                {
                    //stop recording
                    StopRecordingAsync()
                        .FileAndForget(nameof(StopRecordingAsync));
                }
            }
        }

        /// <summary>
        /// Stops push-to-talk recording as soon as any key is released, regardless of which key it was.
        /// </summary>
        private void ChatControlName_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (_rtpProcessor.IsWorking)
            {
                //any key up
                //stop recording regardless of the key
                StopRecordingAsync()
                    .FileAndForget(nameof(StopRecordingAsync));
            }
        }

        #endregion


        #region show child windows

        /// <summary>
        /// Shows a WinForms common dialog (used for the open-file picker) modally over this control.
        /// </summary>
        private bool? ShowDialog(CommonDialog d)
        {
            try
            {
                //ChildWindowAction?.Invoke(true);

                return d.ShowDialog();
            }
            finally
            {
                //ChildWindowAction?.Invoke(false);
            }
        }

        /// <summary>
        /// Shows a WPF window (such as the tools editor or the related-items wait dialog) modally
        /// over this control.
        /// </summary>
        private async Task ShowDialogAsync(Window w)
        {
            try
            {
                //ChildWindowAction?.Invoke(true);

                _ = await w.ShowDialogAsync();
            }
            finally
            {
                //ChildWindowAction?.Invoke(false);
            }
        }

        #endregion

        #region setup

        /// <summary>
        /// Wires the "add to context" input editor to a parser that recognizes solution-item mentions.
        /// </summary>
        private void SetupAddToContextControl()
        {
            AddToContextControl.Setup(
                new ContextParser(
                    new SolutionItemVisualLineGeneratorFactory()
                    )
                );
        }

        /// <summary>
        /// Wires the main prompt editor to a parser that recognizes solution-item mentions and slash
        /// commands.
        /// </summary>
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

        /// <summary>
        /// Moves keyboard focus to the "add to context" input editor.
        /// </summary>
        public void FocusContextControl()
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                AddToContextControl.MakeFocused();
            });
        }

        /// <summary>
        /// Moves keyboard focus to the main prompt editor.
        /// </summary>
        public void FocusPromptControl()
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                PromptControl.MakeFocused();
            });
        }

        #endregion

        #region drag and drop

        /// <summary>
        /// Handles files dropped onto the prompt editor from Windows Explorer or Solution Explorer by
        /// inserting solution-item mentions for the dropped paths and their descendants.
        /// </summary>
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

        /// <summary>
        /// Resolves dropped solution-item paths and their child items, then inserts a mention anchor
        /// for each into the target editor's text so they become part of the prompt.
        /// </summary>
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
