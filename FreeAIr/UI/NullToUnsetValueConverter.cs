﻿using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FreeAIr.UI
{
    public class NullToUnsetValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value ?? DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
