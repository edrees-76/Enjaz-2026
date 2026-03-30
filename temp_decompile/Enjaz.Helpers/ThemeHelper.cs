using System.Windows;
using Enjaz.Services;

namespace Enjaz.Helpers;

public static class ThemeHelper
{
	public static readonly DependencyProperty IsDarkModeProperty;

	public static bool IsDark => ThemeService.IsDarkMode;

	public static bool GetIsDarkMode(DependencyObject obj)
	{
		return (bool)obj.GetValue(IsDarkModeProperty);
	}

	public static void SetIsDarkMode(DependencyObject obj, bool value)
	{
		obj.SetValue(IsDarkModeProperty, (object)value);
	}

	static ThemeHelper()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		IsDarkModeProperty = DependencyProperty.RegisterAttached("IsDarkMode", typeof(bool), typeof(ThemeHelper), new PropertyMetadata((object)false));
		ThemeService.ThemeChanged += OnThemeChanged;
	}

	private static void OnThemeChanged(bool isDark)
	{
	}
}
