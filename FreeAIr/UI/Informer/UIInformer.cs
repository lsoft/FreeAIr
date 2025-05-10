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
    [Export(typeof(UIInformer))]
    public sealed class UIInformer
    {
        private readonly System.Windows.Window _mainWindow;
        private readonly DTEEvents _dteEvents;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private Label _statusControl;

        private ChatsStatusEnum _status = ChatsStatusEnum.Idle;

        public event DoubleClickDelegate DoubleClickEvent;

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

        public async void UpdateUIStatusAsync(
            ChatsStatusEnum status
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

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

        private void UpdateControlText(
            )
        {
            string sbs;
            string title;

            switch (_status)
            {
                case ChatsStatusEnum.Idle:
                    sbs ="AI is getting colder, it nothing is asked about.";
                    title = "⏸";
                    break;
                case ChatsStatusEnum.Working:
                    sbs = "Some tasks are in progress.";
                    title = "▶";
                    break;
                default:
                    sbs = "Status is unknown.";
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
            //pup.Child = new TextBlock
            //{
            //    Text = sbs
            //};
            pup.IsOpen = true;
        }

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

        private void DTEEvents_OnBeginShutdown()
        {
            _cancellationTokenSource.Cancel();
        }

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


    public enum ChatsStatusEnum
    {
        Working,
        Idle
    }

    public delegate void DoubleClickDelegate(object sender, EventArgs e);
}
