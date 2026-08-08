using FreeAIr.Helper;
using FreeAIr.UI.InSitu;
using FreeAIr.UI.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FreeAIr.UI.Windows
{
    /// <summary>
    /// The floating, borderless "in-situ" chat popup shown next to the editor caret. Tracks focus and
    /// visibility against other FreeAIr windows via <see cref="FreeAIrPackage.WindowOpened"/>/<see cref="FreeAIrPackage.WindowClosed"/>,
    /// and supports manual dragging and edge-resizing since it has no standard window chrome.
    /// </summary>
    public partial class InSituChatWindow : Window
    {
        /// <summary>Creates the popup bound to a new <see cref="InSituChatViewModel"/> for the given chat, and wires up activation, sizing and window-lifecycle handlers.</summary>
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

        /// <summary>Restores topmost when a different FreeAIr window closes, so this popup stays visible again.</summary>
        private void OtherWindowClosed(Window window)
        {
            if (ReferenceEquals(this, window))
            {
                return;
            }

            this.Topmost = true;
        }

        /// <summary>Drops topmost when a different FreeAIr window opens, so it does not cover the new window.</summary>
        private void OtherWindowOpened(Window window)
        {
            if (ReferenceEquals(this, window))
            {
                return;
            }

            this.Topmost = false;
        }

        /// <summary>Creates and shows an in-situ chat popup for <paramref name="chat"/>, positioned at the given screen point with bounds checking.</summary>
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

        /// <summary>Converts the device-pixel screen point to DPI-independent units, sizes the window from the saved in-situ dimensions, and nudges it back on-screen if it would fall outside the monitor bounds.</summary>
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

        /// <summary>Closes the popup when the user presses Escape.</summary>
        private void Window_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }

        /// <summary>Persists the popup's current size to settings so the next in-situ window opens at the same dimensions.</summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UIPage.Instance.SetInSituSize(
                this.ActualWidth,
                this.ActualHeight
                );
        }

        /// <summary>Lets the user drag the borderless popup by its content, since it has no title bar.</summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }


        #region resize window

        /// <summary>True while the user is dragging the resize handle.</summary>
        private bool _isResizing = false;
        /// <summary>The screen cursor position at the last resize-move event, used to compute the delta for the next one.</summary>
        private System.Drawing.Point _lastPos; // Храним позицию относительно экрана

        /// <summary>Begins a resize drag, capturing the mouse and recording the starting cursor position.</summary>
        private void Resize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            _isResizing = true;

            // Переводим локальную точку в экранные координаты
            _lastPos = System.Windows.Forms.Cursor.Position;
            ((UIElement)sender).CaptureMouse();

            e.Handled = true;
        }

        /// <summary>While resizing, grows or shrinks the window by half the cursor delta since the last move, with a 50-pixel minimum.</summary>
        private void Resize_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizing)
            {
                return;
            }

            // Получаем текущую позицию относительно экрана
            var currentPos = System.Windows.Forms.Cursor.Position;

            var deltaX = (currentPos.X - _lastPos.X) / 2;
            var deltaY = (currentPos.Y - _lastPos.Y) / 2;

            if (deltaX == 0 && deltaY == 0)
            {
                return;
            }

            // Применяем изменения
            if (Width + deltaX > 50)
            {
                Width += deltaX;
            }
            if (Height + deltaY > 50)
            {
                Height += deltaY;
            }

            _lastPos = currentPos;

            e.Handled = true;
        }

        /// <summary>Ends the resize drag and releases mouse capture.</summary>
        private void Resize_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isResizing = false;
            ((UIElement)sender).ReleaseMouseCapture();

            e.Handled = true;
        }

        #endregion
    }

}
