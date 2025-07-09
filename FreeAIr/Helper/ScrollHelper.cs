using System.Windows.Controls;

namespace FreeAIr.Helper
{
    public static class ScrollHelper
    {
        public static bool IsScrolledToBottom(
            this ScrollViewer scrollViewer
            )
        {
            if (scrollViewer is null)
            {
                throw new ArgumentNullException(nameof(scrollViewer));
            }

            return scrollViewer.VerticalOffset >= scrollViewer.ExtentHeight - scrollViewer.ViewportHeight;
        }
    }
}
