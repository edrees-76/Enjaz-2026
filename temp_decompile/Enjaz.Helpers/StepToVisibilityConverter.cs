using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Enjaz.Helpers;

public class StepToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null || parameter == null)
		{
			return Visibility.Collapsed;
		}
		int num = (int)value;
		string text = parameter.ToString();
		string[] array = text.Split('|');
		int num2 = int.Parse(array[0]);
		bool flag = array.Length > 1 && array[1] == "Invert";
		bool flag2 = ((array.Length <= 1 || !(array[1] == "AtLeast")) ? (num == num2) : (num >= num2));
		if (flag)
		{
			flag2 = !flag2;
		}
		return (!flag2) ? Visibility.Collapsed : Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
