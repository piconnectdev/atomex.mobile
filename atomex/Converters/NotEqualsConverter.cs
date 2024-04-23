using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Converters
{
    public class NotEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
                return !value.ToString().Equals(parameter);
            else
                return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}