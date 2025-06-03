using System.Windows.Controls;

namespace FreeAIr.Helper
{
    public static class ScrollHelper
    {
        public static bool IsScrolledToBottom(
            this ScrollViewer scrollViewer
            )
        {
            return scrollViewer.VerticalOffset >= scrollViewer.ExtentHeight - scrollViewer.ViewportHeight;
        }
    }
}
