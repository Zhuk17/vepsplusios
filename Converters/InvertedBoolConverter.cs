using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace VEPS_Plus.Converters
{
    // Конвертер для инвертирования булевого значения (true -> false, false -> true)
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value; // Return original value if not a boolean
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
