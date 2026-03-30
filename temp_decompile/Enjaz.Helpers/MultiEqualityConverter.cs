using System;
using System.Globalization;
using System.Windows.Data;

namespace Enjaz.Helpers;

public class MultiEqualityConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null || values.Length < 2)
		{
			return false;
		}
		if (values[0] == null || values[1] == null)
		{
			return false;
		}
		return values[0].ToString()?.Equals(values[1].ToString(), StringComparison.OrdinalIgnoreCase) ?? false;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
