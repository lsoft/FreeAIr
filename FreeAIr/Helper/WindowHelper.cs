using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.Helper
{
    public static class WindowHelper
    {
        /// <summary>
        /// Корректируем позицию, если окно выходит за границы экрана с учетом custom DPI
        /// </summary>
        public static void CorrectWindowPosition(
            this Window window
            )
        {
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
    }
}
