using System.Globalization;
using System.Windows.Data;

namespace FreeAIr.UI.ToolWindows
{
    /// <summary>
    /// XAML value converter that turns a container's height into a fraction of itself, used to
    /// cap a child element's <c>MaxHeight</c> at a proportion (e.g. 20%) of the available space.
    /// </summary>
    public class RelativeMaxHeightConverter : IValueConverter
    {
        /// <summary>
        /// The fraction of the source height to return; defaults to 20%.
        /// </summary>
        public double Ratio { get; set; } = 0.2; // 20% от высоты контейнера по умолчанию

        /// <summary>
        /// Multiplies the incoming height by <see cref="Ratio"/>, or returns NaN if the value
        /// isn't a double.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                return height * Ratio; // Возвращаем относительную высоту
            }
            return double.NaN;
        }

        /// <summary>
        /// Not supported: this converter is one-way only.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

