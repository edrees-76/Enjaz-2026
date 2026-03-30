using System.Windows;
using Enjaz.Services;

namespace Enjaz.Helpers
{
    public static class ThemeHelper
    {
        public static readonly DependencyProperty IsDarkModeProperty =
            DependencyProperty.RegisterAttached("IsDarkMode", typeof(bool), typeof(ThemeHelper), new PropertyMetadata(false));

        public static bool GetIsDarkMode(DependencyObject obj) => (bool)obj.GetValue(IsDarkModeProperty);
        public static void SetIsDarkMode(DependencyObject obj, bool value) => obj.SetValue(IsDarkModeProperty, value);

        static ThemeHelper()
        {
            // Update global state when theme changes
            ThemeService.ThemeChanged += OnThemeChanged;
        }

        private static void OnThemeChanged(bool isDark)
        {
            // This is a global helper, but attached properties usually need an instance.
            // For Styles, we can bind to a static property or use a singleton.
        }
        
        // Simple static property for direct XAML binding if needed
        public static bool IsDark => ThemeService.IsDarkMode;
    }
}
