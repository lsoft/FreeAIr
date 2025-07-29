using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI.Windows
{
    public partial class ControlCenterWindow : Window
    {

        public ControlCenterWindow(
            )
        {
            InitializeComponent();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;

            // Определяем направление прокрутки
            var direction = Math.Sign(e.Delta);

            // Проверяем возможность прокрутки в этом направлении
            if (direction < 0 && (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight))
            {
                // Если нельзя прокрутить вниз — делегируем прокрутку внешнему ScrollViewer
                MainScrollViewerName.ScrollToVerticalOffset(MainScrollViewerName.VerticalOffset - direction * 30); // 50 — шаг прокрутки
                e.Handled = true;
            }
            else if (direction > 0 && scrollViewer.VerticalOffset <= 0)
            {
                // Если нельзя прокрутить вверх — тоже делегируем
                MainScrollViewerName.ScrollToVerticalOffset(MainScrollViewerName.VerticalOffset - direction * 30);
                e.Handled = true;
            }
        }
    }

}
