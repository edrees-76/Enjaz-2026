using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Enjaz.Helpers;

public class RoleToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null || parameter == null)
		{
			return Visibility.Collapsed;
		}
		string text = value.ToString();
		string text2 = parameter.ToString();
		if (text != null && text2 != null && text.Equals(text2, StringComparison.OrdinalIgnoreCase))
		{
			return Visibility.Visible;
		}
		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
