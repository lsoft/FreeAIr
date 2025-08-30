using FreeAIr.Helper;
using FreeAIr.UI.InSitu;
using FreeAIr.UI.ViewModels;
using System.Windows;
using System.Windows.Media;

namespace FreeAIr.UI.Windows
{
    public partial class InSituChatWindow : Window
    {
        public InSituChatWindow(
            FreeAIr.Chat.Chat chat
            )
        {
            var viewModel = new InSituChatViewModel(
                chat
                );
            DataContext = viewModel;
            
            InitializeComponent();

            FreeAIrPackage.WindowOpened += OtherWindowOpened;
            FreeAIrPackage.WindowClosed += OtherWindowClosed;
            //ChatControlName.ChildWindowAction +=
            //    (opened) =>
            //    {
            //        this.Topmost = !opened;
            //    };

            Activated += (sender, e) =>
            {
                this.Opacity = 1f;

                InSituChatInputCommandFilter.SetSuppressMode(true);
            };
            Deactivated += (sender, e) =>
            {
                InSituChatInputCommandFilter.SetSuppressMode(false);

                if (UIPage.Instance.CloseIfUserSwitchedAwayFromInSituWindow)
                {
                    this.Close();
                    return;
                }

                this.Opacity = 0.5f;
            };
            Loaded += (sender, e) =>
            {
                var vm = this.DataContext as InSituChatViewModel;
                if (vm is not null)
                {
                    vm.CloseWindow = Close;
                }
            };
            Closed += (sender, e) =>
            {
                try
                {
                    InSituChatInputCommandFilter.SetSuppressMode(false);
                }
                finally
                {
                    FreeAIrPackage.WindowOpened -= OtherWindowOpened;
                    FreeAIrPackage.WindowClosed -= OtherWindowClosed;
                }
            };
        }

        private void OtherWindowClosed(Window window)
        {
            if (ReferenceEquals(this, window))
            {
                return;
            }

            this.Topmost = true;
        }

        private void OtherWindowOpened(Window window)
        {
            if (ReferenceEquals(this, window))
            {
                return;
            }

            this.Topmost = false;
        }

        public static async Task ShowAsync(
            FreeAIr.Chat.Chat chat,
            Point position
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            var window = new InSituChatWindow(chat);
            PositionWindowAtScreenPointWithBoundsCheck(window, position);
            window.Show();
        }

        private static void PositionWindowAtScreenPointWithBoundsCheck(
            Window window,
            System.Windows.Point screenPoint
            )
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;

            // Get the DPI scale for the current monitor
            var dpiScale = VisualTreeHelper.GetDpi(window);

            // Convert device pixels to DIPs (device independent pixels)
            double dipX = screenPoint.X / dpiScale.DpiScaleX;
            double dipY = screenPoint.Y / dpiScale.DpiScaleY;

            window.Left = dipX;
            window.Top = dipY;
            window.Width = Math.Max(100.0, UIPage.Instance.InSituWidth);
            window.Height = Math.Max(100.0, UIPage.Instance.InSituHeight);

            window.CorrectWindowPosition();
        }

        private void Window_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UIPage.Instance.SetInSituSize(
                this.ActualWidth,
                this.ActualHeight
                );
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }

}
