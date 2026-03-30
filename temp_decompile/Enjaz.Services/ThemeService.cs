using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace Enjaz.Services;

public class ThemeService
{
	public enum ThemeType
	{
		DefaultBlue,
		MidnightPurple,
		OasisGreen,
		RoyalGold
	}

	private readonly PaletteHelper _paletteHelper = new PaletteHelper();

	private ThemeType _currentTheme = ThemeType.DefaultBlue;

	private double _currentFontScale = 1.0;

	public static bool IsDarkMode { get; private set; }

	public ThemeType CurrentTheme => _currentTheme;

	public double CurrentFontScale => _currentFontScale;

	public static event Action<bool>? ThemeChanged;

	public void Initialize(string themeName, bool isDark, double fontScale)
	{
		if (Enum.TryParse<ThemeType>(themeName, out var result))
		{
			ApplyTheme(result, isDark);
		}
		ApplyFontSize(fontScale);
	}

	public void ApplyTheme(ThemeType theme, bool isDark)
	{
		Theme theme2 = _paletteHelper.GetTheme();
		theme2.SetBaseTheme((!isDark) ? BaseTheme.Light : BaseTheme.Dark);
		switch (theme)
		{
		case ThemeType.DefaultBlue:
			if (isDark)
			{
				theme2.SetPrimaryColor(Color.FromRgb(5, 150, 105));
				theme2.SetSecondaryColor(Color.FromRgb(11, 25, 60));
			}
			else
			{
				theme2.SetPrimaryColor(Color.FromRgb(11, 25, 60));
				theme2.SetSecondaryColor(Color.FromRgb(5, 150, 105));
			}
			break;
		case ThemeType.MidnightPurple:
			theme2.SetPrimaryColor(Color.FromRgb(103, 58, 183));
			theme2.SetSecondaryColor(Color.FromRgb(byte.MaxValue, 64, 129));
			break;
		case ThemeType.OasisGreen:
			theme2.SetPrimaryColor(Color.FromRgb(5, 150, 105));
			theme2.SetSecondaryColor(Color.FromRgb(11, 25, 60));
			break;
		case ThemeType.RoyalGold:
			theme2.SetPrimaryColor(Color.FromRgb(byte.MaxValue, 215, 0));
			theme2.SetSecondaryColor(Color.FromRgb(63, 81, 181));
			break;
		}
		_paletteHelper.SetTheme(theme2);
		UpdateGlobalResources(theme2.PrimaryMid.Color, theme2.SecondaryMid.Color, isDark);
		_currentTheme = theme;
		IsDarkMode = isDark;
		ThemeService.ThemeChanged?.Invoke(isDark);
		LoggerService.LogInfo($"Theme changed to: {theme} (Dark: {isDark})");
	}

	private void UpdateGlobalResources(Color primary, Color secondary, bool isDark)
	{
		ResourceDictionary resources = Application.Current.Resources;
		string text = (isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml");
		try
		{
			ResourceDictionary resourceDictionary = new ResourceDictionary
			{
				Source = new Uri(text, UriKind.RelativeOrAbsolute)
			};
			foreach (object key in resourceDictionary.Keys)
			{
				resources[key] = resourceDictionary[key];
			}
			resources["PrimaryBrush"] = new SolidColorBrush(primary);
			resources["SecondaryBrush"] = new SolidColorBrush(secondary);
			resources["SuccessBrush"] = new SolidColorBrush(Color.FromRgb(5, 150, 105));
			resources["MaterialDesignSelection"] = new SolidColorBrush(Color.FromArgb(80, primary.R, primary.G, primary.B));
		}
		catch (Exception ex)
		{
			LoggerService.LogError("Failed to load theme file: " + text, ex);
		}
	}

	public void ApplyFontSize(double scale)
	{
		_currentFontScale = scale;
		ResourceDictionary resources = Application.Current.Resources;
		double num = 14.0;
		double num2 = 18.0;
		double num3 = 24.0;
		resources["FontSizeNormal"] = num * scale;
		resources["FontSizeLarge"] = num2 * scale;
		resources["FontSizeHeader"] = num3 * scale;
		LoggerService.LogInfo($"Font scale changed to: {scale}");
	}

	public string GetThemeDisplayName(ThemeType theme)
	{
		if (1 == 0)
		{
		}
		string result = theme switch
		{
			ThemeType.DefaultBlue => "الأزرق الافتراضي", 
			ThemeType.MidnightPurple => "الأرجواني الليلي", 
			ThemeType.OasisGreen => "الواحة الخضراء", 
			ThemeType.RoyalGold => "الذهبي الملكي", 
			_ => "غير معروف", 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
