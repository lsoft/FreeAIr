using MdXaml;
using System.Windows.Controls;
using WpfHelpers;

namespace FreeAIr.Helper
{
    public static class MarkdownScrollViewerExtensions
    {
        /// <summary>
        /// Прокручивает содержимое MarkdownScrollViewer до самого низа.
        /// </summary>
        public static void ScrollToEnd(
            this MarkdownScrollViewer viewer
            )
        {
            if (viewer == null)
            {
                return;
            }

            // Ищем внутренний ScrollViewer
            var scrollViewers = viewer.FindVisualChildren<ScrollViewer>();
            if (scrollViewers.Count == 0)
            {
                return;
            }

            var scrollViewer = scrollViewers[0];
            if (scrollViewer == null)
            {
                return;
            }

            scrollViewer.ScrollToBottom();
        }
    }
}
