using Microsoft.Internal.VisualStudio.PlatformUI;
using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI.Windows
{
    public partial class ControlCenterWindow : Window
    {
        private readonly ControlCenterSectionEnum _scrollTo;

        public ControlCenterWindow(
            ControlCenterSectionEnum scrollTo
            )
        {
            _scrollTo = scrollTo;
            
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            switch (_scrollTo)
            {
                case ControlCenterSectionEnum.None:
                    break;
                case ControlCenterSectionEnum.ModelContextProtocol:
                    ModelContextProtocolLabel.BringIntoView();
                    break;
                case ControlCenterSectionEnum.SystemPrompt:
                    MCPAppyButton.BringIntoView();
                    break;
                case ControlCenterSectionEnum.Agents:
                    AgentsAppyButton.BringIntoView();
                    break;
                default:
                    break;
            }
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

    public enum ControlCenterSectionEnum
    {
        None,
        ModelContextProtocol,
        SystemPrompt,
        Agents
    }

}
