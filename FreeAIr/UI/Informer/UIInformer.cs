using EnvDTE;
using EnvDTE80;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WpfHelpers;

namespace FreeAIr.UI.Informer
{
    /// <summary>
    /// Owns the small status indicator ("⏸"/"▶") injected into the Visual Studio status bar that shows
    /// whether any FreeAIr chat is currently working, plus the popup tooltip that explains the current
    /// state and the double-click gesture used to jump to the activity log.
    /// </summary>
    [Export(typeof(UIInformer))]
    public sealed class UIInformer
    {
        /// <summary>
        /// The Visual Studio main window, used to locate the status bar panel the indicator is attached to.
        /// </summary>
        private readonly System.Windows.Window _mainWindow;

        /// <summary>
        /// DTE shutdown event source, subscribed to so the background retry loop in <see cref="InitAsync"/>
        /// stops cleanly when Visual Studio begins closing.
        /// </summary>
        private readonly DTEEvents _dteEvents;

        /// <summary>
        /// Cancels the polling loop that waits for the status bar panel to become available and stops
        /// pending work when the IDE begins shutting down.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The status bar label control showing the current chat activity indicator; null until the status
        /// bar panel becomes available and <see cref="CreateControl"/> has run.
        /// </summary>
        private Label _statusControl;

        /// <summary>
        /// The chat activity state currently reflected by the status indicator.
        /// </summary>
        private ChatsStatusEnum _status = ChatsStatusEnum.Idle;

        /// <summary>
        /// Raised when the user double-clicks the status indicator; used to open the activity log.
        /// </summary>
        public event DoubleClickDelegate DoubleClickEvent;

        /// <summary>
        /// MEF constructor that captures the main window and subscribes to DTE shutdown so the status
        /// indicator can later be created and torn down.
        /// </summary>
        [ImportingConstructor]
        public UIInformer(
            )
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _mainWindow = System.Windows.Application.Current.MainWindow;

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            _dteEvents = ((Events2)dte.Events).DTEEvents;
            _dteEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;
        }

        /// <summary>
        /// Updates the status indicator to reflect a new chat activity state (idle vs. working), switching
        /// to the UI thread and refreshing the control text only when the state actually changed.
        /// </summary>
        public async void UpdateUIStatusAsync(
            ChatsStatusEnum status
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_status == status)
                {
                    return;
                }

                _status = status;

                if (_statusControl is null)
                {
                    return;
                }

                UpdateControlText();
            }
            catch (Exception excp)
            {
                //todo _log.Error(excp, "Cannot update UI control");
            }
        }

        /// <summary>
        /// Refreshes the status indicator's glyph and tooltip text to match <see cref="_status"/>, and opens
        /// the tooltip popup so the change is briefly visible to the user.
        /// </summary>
        private void UpdateControlText(
            )
        {
            string sbs;
            string title;

            switch (_status)
            {
                case ChatsStatusEnum.Idle:
                    sbs = Resources.Resources.AI_is_getting_colder;
                    title = "⏸";
                    break;
                case ChatsStatusEnum.Working:
                    sbs = Resources.Resources.Some_tasks_are_in_progress;
                    title = "▶";
                    break;
                default:
                    sbs = Resources.Resources.Status_is_unknown;
                    title = "�";
                    break;
            }

            _statusControl.Content = title;
            var pup = _statusControl.ToolTip as Popup;
            if (pup is null)
            {
                return;
            }
            var tb = pup.Child as StatusPopup;
            if (tb is null)
            {
                return;
            }
            tb.SetText(sbs);
            pup.IsOpen = true;
        }

        /// <summary>
        /// Repeatedly attempts to create and attach the status indicator to the VS status bar, retrying once
        /// a second until the status bar panel exists (it may not be ready yet during IDE startup) or the
        /// operation is cancelled.
        /// </summary>
        public async Task InitAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var cancellationToken = _cancellationTokenSource.Token;

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (CreateControl())
                    {
                        break;
                    }

                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                //not a bug
            }
            catch (Exception excp)
            {
                //todo _log.Error(excp, "Cannot init UI control");
            }
        }

        /// <summary>
        /// Cancels pending status-indicator work when Visual Studio begins shutting down.
        /// </summary>
        private void DTEEvents_OnBeginShutdown()
        {
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Builds the status label and its tooltip popup, styled to match the VS status bar's default font
        /// and colors, and adds it to the status bar panel. Returns false if the panel or the reference
        /// control used to read the default styling cannot be found yet.
        /// </summary>
        private bool CreateControl()
        {
            if (_statusControl is not null)
            {
                return true;
            }

            var defaultUiParameters = GetDefaultUiParameters();
            if (defaultUiParameters is null)
            {
                return false;
            }

            var statusBarPanel = _mainWindow.GetRecursiveByName("StatusBarPanel") as DockPanel;
            if (statusBarPanel is null)
            {
                return false;
            }

            _statusControl = new Label
            {
                Margin = new Thickness(5, 0, 5, 0),
                Foreground = defaultUiParameters.ActualForeground,
                FontFamily = defaultUiParameters.FontFamily,
                FontSize = defaultUiParameters.FontSize,
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(0),
            };
            UpdateControlText();

            var popup = new Popup
            {
                StaysOpen = false,
                PlacementTarget = _statusControl,
                Placement = PlacementMode.Top,
                PopupAnimation = PopupAnimation.Slide,
                AllowsTransparency = true,
                Child = new StatusPopup(string.Empty)
            };

            _statusControl.ToolTip = popup;

            _statusControl.MouseDoubleClick += StatusControl_MouseDoubleClick;

            statusBarPanel.Children.Add(_statusControl);

            return true;
        }

        /// <summary>
        /// Raises <see cref="DoubleClickEvent"/> when the user left-double-clicks the status indicator.
        /// </summary>
        private void StatusControl_MouseDoubleClick(
            object sender, 
            System.Windows.Input.MouseButtonEventArgs e
            )
        {
            if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
            {
                return;
            }

            var dce = DoubleClickEvent;
            if (dce is not null)
            {
                dce(this, new EventArgs());
            }
        }

        /// <summary>
        /// Reads the font family, font size and foreground brush from the VS status bar's built-in source
        /// control indicator, via reflection, so the FreeAIr status label can visually match it.
        /// </summary>
        private DefaultUIParameters? GetDefaultUiParameters()
        {
            var scc = _mainWindow.GetRecursiveByName("PART_SccCompartmentText");
            if (scc == null)
            {
                return null;
            }

            var sccType = scc.GetType();
            var sccTypeProperty = sccType.GetProperty("Child", BindingFlags.Public | BindingFlags.Instance);
            if (sccTypeProperty is null)
            {
                return null;
            }

            var sccChild = sccTypeProperty.GetValue(scc, null) as TextBlock;
            if (sccChild is null)
            {
                return null;
            }

            return new DefaultUIParameters(
                actualForeground: sccChild.Foreground,
                fontFamily: sccChild.FontFamily,
                fontSize: sccChild.FontSize
                );
        }

    }


    /// <summary>
    /// The chat activity state shown by the status bar indicator.
    /// </summary>
    public enum ChatsStatusEnum
    {
        /// <summary>
        /// At least one chat has a request in progress.
        /// </summary>
        Working,

        /// <summary>
        /// No chat currently has a request in progress.
        /// </summary>
        Idle
    }

    /// <summary>
    /// Signature for the event raised when the user double-clicks the status bar indicator.
    /// </summary>
    public delegate void DoubleClickDelegate(object sender, EventArgs e);
}
