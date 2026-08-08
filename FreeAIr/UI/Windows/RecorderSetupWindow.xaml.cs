using FreeAIr.Helper;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FreeAIr.BLogic;

namespace FreeAIr.UI.Windows
{
    /// <summary>
    /// Small popup window that hosts a voice-recorder configuration control near the point the
    /// user invoked it from, closing itself (and signalling the caller) on Escape, deactivation,
    /// or an explicit close action.
    /// </summary>
    public partial class RecorderSetupWindow : Window
    {
        /// <summary>
        /// Creates the window hosting the given configuration control and invokes
        /// <paramref name="closeAction"/> once the window closes.
        /// </summary>
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

        /// <summary>
        /// Shows the recorder configuration control in a popup positioned at the given screen
        /// point (clamped to stay on-screen), and awaits until the user closes it.
        /// </summary>
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

        /// <summary>
        /// Converts the given device-pixel screen point to DIPs for the window's monitor and
        /// positions the window there, then nudges it back on-screen if it would otherwise hang
        /// off an edge.
        /// </summary>
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

        /// <summary>
        /// Closes the popup when the user presses Escape.
        /// </summary>
        private void Window_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Closes the popup as soon as it loses focus, so it behaves like a transient flyout.
        /// </summary>
        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Close();
        }
    }

}
