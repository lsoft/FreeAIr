using FreeAIr.Helper;
using FreeAIr.UI.InSitu;
using FreeAIr.UI.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
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


        #region resize window

        private bool _isResizing = false;
        private System.Drawing.Point _lastPos; // Храним позицию относительно экрана

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

        private void Resize_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isResizing = false;
            ((UIElement)sender).ReleaseMouseCapture();

            e.Handled = true;
        }

        #endregion
    }

}
