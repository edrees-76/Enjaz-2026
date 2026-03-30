using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Enjaz.Helpers;

public static class NumericInputBehavior
{
	public static readonly DependencyProperty IsNumericOnlyProperty = DependencyProperty.RegisterAttached("IsNumericOnly", typeof(bool), typeof(NumericInputBehavior), new PropertyMetadata((object)false, new PropertyChangedCallback(OnIsNumericOnlyChanged)));

	public static bool GetIsNumericOnly(DependencyObject obj)
	{
		return (bool)obj.GetValue(IsNumericOnlyProperty);
	}

	public static void SetIsNumericOnly(DependencyObject obj, bool value)
	{
		obj.SetValue(IsNumericOnlyProperty, (object)value);
	}

	private static void OnIsNumericOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TextBox textBox)
		{
			if ((bool)((DependencyPropertyChangedEventArgs)(ref e)).NewValue)
			{
				textBox.PreviewTextInput += TextBox_PreviewTextInput;
				DataObject.AddPastingHandler((DependencyObject)(object)textBox, TextBox_Pasting);
			}
			else
			{
				textBox.PreviewTextInput -= TextBox_PreviewTextInput;
				DataObject.RemovePastingHandler((DependencyObject)(object)textBox, TextBox_Pasting);
			}
		}
	}

	private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
	{
		Regex regex = new Regex("^[0-9\\/\\-]+$");
		e.Handled = !regex.IsMatch(e.Text);
	}

	private static void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
	{
		if (e.DataObject.GetDataPresent(typeof(string)))
		{
			string input = (string)e.DataObject.GetData(typeof(string));
			Regex regex = new Regex("^[0-9\\/\\-]+$");
			if (!regex.IsMatch(input))
			{
				e.CancelCommand();
			}
		}
		else
		{
			e.CancelCommand();
		}
	}
}
