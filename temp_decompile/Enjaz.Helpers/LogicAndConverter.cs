using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Enjaz.Helpers;

public class LogicAndConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null || values.Length == 0)
		{
			return false;
		}
		return values.All(delegate(object v)
		{
			bool flag = default(bool);
			int num;
			if (v is bool)
			{
				flag = (bool)v;
				num = 1;
			}
			else
			{
				num = 0;
			}
			return (byte)((uint)num & (flag ? 1u : 0u)) != 0;
		});
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
