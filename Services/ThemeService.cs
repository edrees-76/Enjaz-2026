using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Enjaz.Helpers;

namespace Enjaz.Services
{
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

        // Static state for CustomDatePicker integration
        public static bool IsDarkMode { get; private set; }
        public static event Action<bool>? ThemeChanged;
        private double _currentFontScale = 1.0;

        public ThemeType CurrentTheme => _currentTheme;
        public double CurrentFontScale => _currentFontScale;

        public void Initialize(string themeName, bool isDark, double fontScale)
        {
            if (Enum.TryParse<ThemeType>(themeName, out var theme))
            {
                ApplyTheme(theme, isDark);
            }
            ApplyFontSize(fontScale);
        }

        public void ApplyTheme(ThemeType theme, bool isDark)
        {
            var themeObject = _paletteHelper.GetTheme();
            // Restored: CustomDatePicker handles its own popup colors, so PaletteHelper can be dynamic again
            themeObject.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
            
            switch (theme)
            {
                case ThemeType.DefaultBlue:
                    if (isDark)
                    {
                        themeObject.SetPrimaryColor(Color.FromRgb(5, 150, 105)); // Enjaz Emerald (#059669)
                        themeObject.SetSecondaryColor(Color.FromRgb(11, 25, 60)); // Enjaz Navy (#0B193C)
                    }
                    else
                    {
                        themeObject.SetPrimaryColor(Color.FromRgb(11, 25, 60)); // Enjaz Navy (#0B193C)
                        themeObject.SetSecondaryColor(Color.FromRgb(5, 150, 105)); // Enjaz Emerald (#059669)
                    }
                    break;
                case ThemeType.MidnightPurple:
                    themeObject.SetPrimaryColor(Color.FromRgb(103, 58, 183)); // Deep Purple 500
                    themeObject.SetSecondaryColor(Color.FromRgb(255, 64, 129)); // Pink A200
                    break;
                case ThemeType.OasisGreen:
                    themeObject.SetPrimaryColor(Color.FromRgb(5, 150, 105)); // Enjaz Emerald
                    themeObject.SetSecondaryColor(Color.FromRgb(11, 25, 60)); // Enjaz Navy
                    break;
                case ThemeType.RoyalGold:
                    themeObject.SetPrimaryColor(Color.FromRgb(255, 215, 0)); // Gold
                    themeObject.SetSecondaryColor(Color.FromRgb(63, 81, 181)); // Indigo 500
                    break;
            }

            _paletteHelper.SetTheme(themeObject);
            
            // Synchronize global application brushes
            UpdateGlobalResources(themeObject.PrimaryMid.Color, themeObject.SecondaryMid.Color, isDark);
            
            _currentTheme = theme;
            IsDarkMode = isDark;
            ThemeChanged?.Invoke(isDark);
            LoggerService.LogInfo($"Theme changed to: {theme} (Dark: {isDark})");
        }

        private void UpdateGlobalResources(Color primary, Color secondary, bool isDark)
        {
            var resources = Application.Current.Resources;
            string themeFile = isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";

            try
            {
                var dict = new ResourceDictionary { Source = new Uri(themeFile, UriKind.RelativeOrAbsolute) };
                
                // Merge keys from the dictionary into main resources to trigger DynamicResource updates
                foreach (var key in dict.Keys)
                {
                    resources[key] = dict[key];
                }

                // Sync MaterialDesign Primary/Secondary as well
                resources["PrimaryBrush"] = new SolidColorBrush(primary);
                resources["SecondaryBrush"] = new SolidColorBrush(secondary);
                resources["SuccessBrush"] = new SolidColorBrush(Color.FromRgb(5, 150, 105)); // Always Enjaz Emerald (#059669)
                resources["MaterialDesignSelection"] = new SolidColorBrush(Color.FromArgb(80, primary.R, primary.G, primary.B));
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"Failed to load theme file: {themeFile}", ex);
            }
        }

        public void ApplyFontSize(double scale)
        {
            _currentFontScale = scale;
            var resources = Application.Current.Resources;

            // Define base sizes
            double baseNormal = 14;
            double baseLarge = 18;
            double baseHeader = 24;

            resources["FontSizeNormal"] = baseNormal * scale;
            resources["FontSizeLarge"] = baseLarge * scale;
            resources["FontSizeHeader"] = baseHeader * scale;

            LoggerService.LogInfo($"Font scale changed to: {scale}");
        }

        public string GetThemeDisplayName(ThemeType theme)
        {
            return theme switch
            {
                ThemeType.DefaultBlue => "الأزرق الافتراضي",
                ThemeType.MidnightPurple => "الأرجواني الليلي",
                ThemeType.OasisGreen => "الواحة الخضراء",
                ThemeType.RoyalGold => "الذهبي الملكي",
                _ => "غير معروف"
            };
        }
    }
}

