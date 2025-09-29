using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace VEPS_Plus.Converters
{
	public class GreaterThanZeroToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is int intValue)
			{
				return intValue > 0;
			}
			if (value is long longValue)
			{
				return longValue > 0;
			}
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
