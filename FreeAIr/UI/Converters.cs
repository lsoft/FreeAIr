using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FreeAIr.UI
{
    /// <summary>XAML value converter that maps a boolean to <see cref="Visibility"/> with the sense inverted: true hides the element, false shows it.</summary>
    public sealed class InvertBooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        /// <summary>Configures the converter to collapse on true and show on false.</summary>
        public InvertBooleanToVisibilityConverter() :
            base(Visibility.Collapsed, Visibility.Visible)
        {
        }
    }

    /// <summary>Generic XAML value converter that maps a bound boolean to one of two caller-supplied values of type <typeparamref name="T"/>.</summary>
    public class BooleanConverter<T> : IValueConverter
    {
        /// <summary>Value produced when the bound boolean is true.</summary>
        public T True
        {
            get; set;
        }
        /// <summary>Value produced when the bound boolean is false.</summary>
        public T False
        {
            get; set;
        }

        /// <summary>Creates the converter with the values to use for the true and false cases.</summary>
        public BooleanConverter(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        /// <summary>Converts a bound boolean into <see cref="True"/> or <see cref="False"/>.</summary>
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && ((bool)value) ? True : False;
        }

        /// <summary>Converts a bound value back to a boolean by checking whether it equals <see cref="True"/>.</summary>
        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T && EqualityComparer<T>.Default.Equals((T)value, True);
        }
    }
}
