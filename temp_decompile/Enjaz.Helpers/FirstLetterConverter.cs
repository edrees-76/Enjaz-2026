using System;
using System.Globalization;
using System.Windows.Data;

namespace Enjaz.Helpers;

public class FirstLetterConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string text && !string.IsNullOrEmpty(text))
		{
			return text[0].ToString().ToUpper();
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
