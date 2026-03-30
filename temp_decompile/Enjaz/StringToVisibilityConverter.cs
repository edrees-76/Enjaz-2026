using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Enjaz;

public class StringToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return Visibility.Collapsed;
		}
		string text = value.ToString() ?? string.Empty;
		if (parameter != null)
		{
			string value2 = parameter.ToString() ?? string.Empty;
			return (!text.Equals(value2, StringComparison.OrdinalIgnoreCase)) ? Visibility.Collapsed : Visibility.Visible;
		}
		return string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
