using System.Windows;
using System.Windows.Controls;

namespace Enjaz.Helpers;

public static class PasswordBoxHelper
{
	public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordBoxHelper), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)string.Empty, new PropertyChangedCallback(OnPasswordPropertyChanged)));

	public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached("Attach", typeof(bool), typeof(PasswordBoxHelper), new PropertyMetadata((object)false, new PropertyChangedCallback(Attach)));

	private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(PasswordBoxHelper));

	public static void SetAttach(DependencyObject dp, bool value)
	{
		dp.SetValue(AttachProperty, (object)value);
	}

	public static bool GetAttach(DependencyObject dp)
	{
		return (bool)dp.GetValue(AttachProperty);
	}

	public static string GetPassword(DependencyObject dp)
	{
		return (string)dp.GetValue(PasswordProperty);
	}

	public static void SetPassword(DependencyObject dp, string value)
	{
		dp.SetValue(PasswordProperty, (object)value);
	}

	private static bool GetIsUpdating(DependencyObject dp)
	{
		return (bool)dp.GetValue(IsUpdatingProperty);
	}

	private static void SetIsUpdating(DependencyObject dp, bool value)
	{
		dp.SetValue(IsUpdatingProperty, (object)value);
	}

	private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is PasswordBox passwordBox)
		{
			passwordBox.PasswordChanged -= PasswordChanged;
			if (!GetIsUpdating((DependencyObject)(object)passwordBox))
			{
				passwordBox.Password = (string)((DependencyPropertyChangedEventArgs)(ref e)).NewValue;
			}
			passwordBox.PasswordChanged += PasswordChanged;
		}
	}

	private static void Attach(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is PasswordBox passwordBox)
		{
			object oldValue = ((DependencyPropertyChangedEventArgs)(ref e)).OldValue;
			bool flag = default(bool);
			int num;
			if (oldValue is bool)
			{
				flag = (bool)oldValue;
				num = 1;
			}
			else
			{
				num = 0;
			}
			if (((uint)num & (flag ? 1u : 0u)) != 0)
			{
				passwordBox.PasswordChanged -= PasswordChanged;
			}
			oldValue = ((DependencyPropertyChangedEventArgs)(ref e)).NewValue;
			bool flag2 = default(bool);
			int num2;
			if (oldValue is bool)
			{
				flag2 = (bool)oldValue;
				num2 = 1;
			}
			else
			{
				num2 = 0;
			}
			if (((uint)num2 & (flag2 ? 1u : 0u)) != 0)
			{
				passwordBox.PasswordChanged += PasswordChanged;
			}
		}
	}

	private static void PasswordChanged(object sender, RoutedEventArgs e)
	{
		if (sender is PasswordBox passwordBox)
		{
			SetIsUpdating((DependencyObject)(object)passwordBox, value: true);
			SetPassword((DependencyObject)(object)passwordBox, passwordBox.Password);
			SetIsUpdating((DependencyObject)(object)passwordBox, value: false);
		}
	}
}
