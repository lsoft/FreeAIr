using FreeAIr.Helper;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FreeAIr.UI.Windows
{
    public partial class RecorderSetupWindow : Window
    {
        public RecorderSetupWindow(
            UserControl recorderConfigurationControl,
            Action closeAction
            )
        {
            if (recorderConfigurationControl is null)
            {
                throw new ArgumentNullException(nameof(recorderConfigurationControl));
            }

            if (closeAction is null)
            {
                throw new ArgumentNullException(nameof(closeAction));
            }

            InitializeComponent();

            Content = recorderConfigurationControl;

            Closed += (sender, e) => closeAction();
        }

        public static async Task ShowAsync(
            UserControl recorderConfigurationControl,
            System.Windows.Point position
            )
        {
            NonDisposableSemaphoreSlim semaphore = new NonDisposableSemaphoreSlim(0, 1);

            var window = new RecorderSetupWindow(
                recorderConfigurationControl,
                () => semaphore.Release()
                );
            PositionWindowAtScreenPointWithBoundsCheck(window, position);
            window.Show();

            await semaphore.WaitAsync();
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

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Close();
        }
    }

}
