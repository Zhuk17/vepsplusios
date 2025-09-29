using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Globalization;

namespace VEPS_Plus.Converters
{
    // Конвертер для изменения цвета фона уведомления в зависимости от IsRead
    public class IsReadToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRead)
            {
                return isRead ? Color.FromArgb("#2A364F") : Color.FromArgb("#1B263B"); // Read vs Unread colors
            }
            return Color.FromArgb("#1B263B"); // Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
