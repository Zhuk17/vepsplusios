using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace VEPS_Plus.Converters
{
    // Конвертер для проверки, не является ли объект null
    public class IsNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
