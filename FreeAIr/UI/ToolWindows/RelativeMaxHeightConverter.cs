using System.Globalization;
using System.Windows.Data;

namespace FreeAIr.UI.ToolWindows
{
    public class RelativeMaxHeightConverter : IValueConverter
    {
        public double Ratio { get; set; } = 0.2; // 20% от высоты контейнера по умолчанию

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                return height * Ratio; // Возвращаем относительную высоту
            }
            return double.NaN;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

