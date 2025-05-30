using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FreeAIr.UI
{
    public sealed class InvertBooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public InvertBooleanToVisibilityConverter() :
            base(Visibility.Collapsed, Visibility.Visible)
        {
        }
    }

    public sealed class BooleanToBrushConverter : BooleanConverter<Brush>
    {
        public BooleanToBrushConverter(
            Brush? enabledBrush = null,
            Brush? disabledBrush = null
            ) : base(
                enabledBrush ?? new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6)),
                disabledBrush ?? new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88))
                )
        {
        }
    }

    public class BooleanConverter<T> : IValueConverter
    {
        public T True
        {
            get; set;
        }
        public T False
        {
            get; set;
        }

        public BooleanConverter(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && ((bool)value) ? True : False;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T && EqualityComparer<T>.Default.Equals((T)value, True);
        }
    }
}
