using FreeAIr.BLogic;
using FreeAIr.Commands;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.InSitu;
using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows;
using System.Windows.Media;

namespace FreeAIr.UI.Windows
{
    public partial class InSituChatWindow : Window
    {
        public InSituChatWindow(
            FreeAIr.BLogic.Chat chat
            )
        {
            var viewModel = new InSituChatViewModel(
                chat
                );
            DataContext = viewModel;
            
            InitializeComponent();

            Activated += (sender, e) =>
            {
                InSituChatInputCommandFilter.SetSuppressMode(true);
            };
            Deactivated += (sender, e) =>
            {
                InSituChatInputCommandFilter.SetSuppressMode(false);

                if (UIPage.Instance.CloseIfUserSwitchedAwayFromInSituWindow)
                {
                    this.Close();
                }
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
                InSituChatInputCommandFilter.SetSuppressMode(false);
            };
        }

        public static async Task ShowAsync(
            FreeAIr.BLogic.Chat chat,
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

            // Корректируем позицию, если окно выходит за границы экрана с учетом custom DPI
            _ = window.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Получаем информацию о первичном экране (наиболее надежный способ в чистом WPF)
                double workAreaWidth = SystemParameters.WorkArea.Width;
                double workAreaHeight = SystemParameters.WorkArea.Height;
                double workAreaLeft = SystemParameters.WorkArea.Left;
                double workAreaTop = SystemParameters.WorkArea.Top;

                // Корректируем левую позицию
                if (window.Left + window.ActualWidth > workAreaLeft + workAreaWidth)
                {
                    window.Left = workAreaLeft + workAreaWidth - window.ActualWidth;
                }

                if (window.Left < workAreaLeft)
                {
                    window.Left = workAreaLeft;
                }

                // Корректируем верхнюю позицию
                if (window.Top + window.ActualHeight > workAreaTop + workAreaHeight)
                {
                    window.Top = workAreaTop + workAreaHeight - window.ActualHeight;
                }

                if (window.Top < workAreaTop)
                {
                    window.Top = workAreaTop;
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
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
