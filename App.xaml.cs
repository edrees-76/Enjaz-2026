using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Enjaz
{
    /// <summary>
    /// منطق التطبيق الرئيسي
    /// Main Application Logic
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// معالجة بدء التطبيق
        /// Handle application startup
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; } = null!;

        public App()
        {
            // Global Exception Handling
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Services.LoggerService.LogError("AppDomain Unhandled Exception", e.ExceptionObject as Exception);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            Services.LoggerService.LogError("TaskScheduler Unobserved Exception", e.Exception);
            e.SetObserved(); // Prevent crash
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Services.LoggerService.LogError("Unhandled Exception", e.Exception);
            
            var errorWindow = new Views.ErrorDialogWindow("خطأ غير متوقع", e.Exception.Message);
            errorWindow.ShowDialog();
            
            e.Handled = true;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Services.LoggerService.LogInfo("Application Started");

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                // 1. Initialize Database Service (Ensure DB exists)
                var dbService = ServiceProvider.GetRequiredService<Services.DatabaseService>();
                var userRepository = ServiceProvider.GetRequiredService<Services.Repositories.UserRepository>();
                await userRepository.CreateDefaultAdminAsync();

                // 2. Load Appearance Settings
                var settingsService = ServiceProvider.GetRequiredService<Services.SettingsService>();
                var themeService = ServiceProvider.GetRequiredService<Services.ThemeService>();
                var settings = settingsService.Current;
                themeService.Initialize(settings.CurrentTheme, settings.LoginIsDarkMode, settings.FontSizeScale);
                
                // 3. Show Login Window using DI
                var loginWindow = ServiceProvider.GetRequiredService<Views.LoginWindow>();
                loginWindow.Show();
                Services.LoggerService.LogInfo("LoginWindow Shown.");
            }
            catch (Exception ex)
            {
                Services.LoggerService.LogError("Application Startup Failed", ex);
                
                var errorWindow = new Views.ErrorDialogWindow("خطأ جسيم", $"فشل بدء تشغيل النظام: {ex.Message}\n\nيرجى التواصل مع الدعم الفني.");
                errorWindow.ShowDialog();
                
                Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Services
            services.AddSingleton<Services.ILoggerService, Services.LoggerService>();
            services.AddSingleton<Services.DatabaseService>();
            services.AddSingleton<Services.Repositories.UserRepository>();
            services.AddSingleton<Services.Repositories.CertificateRepository>();
            services.AddSingleton<Services.Repositories.SampleReceptionRepository>();
            services.AddSingleton<Services.INavigationService, Services.NavigationService>();
            services.AddSingleton<Services.IPdfService, Services.PdfService>();
            services.AddSingleton<Services.ThemeService>();
            services.AddSingleton<Services.INotificationService, Services.SnackbarService>();
            services.AddSingleton<Services.UserService>();
            services.AddSingleton<Services.ExcelExportService>();
            services.AddSingleton(new Services.SessionTimeoutService(20));
            services.AddSingleton<Services.SettingsService>();
            services.AddSingleton<Services.BackupService>();
            services.AddSingleton<Services.ReportingService>();
            services.AddSingleton<Services.AdvancedExcelService>();
            services.AddSingleton<Services.ReportPdfService>();
            services.AddSingleton<Services.ChartImageGenerator>();
            services.AddSingleton<Services.IOSService, Services.OSService>();
            services.AddSingleton<Services.ArchiveService>();
            services.AddSingleton<Services.IAppAlertService, Services.AppAlertService>();
            services.AddSingleton<Services.IDialogService, Services.WpfDialogService>();
            services.AddSingleton<Services.IReceptionSearchService, Services.WpfReceptionSearchService>();
            services.AddSingleton<Services.HelpDataService>();

            // ViewModels
            services.AddTransient<ViewModels.LoginViewModel>();
            services.AddTransient<ViewModels.MainViewModel>();
            services.AddTransient<ViewModels.DashboardViewModel>();
            services.AddTransient<ViewModels.SampleReceptionsViewModel>();
            services.AddTransient<ViewModels.CertificatesViewModel>();
            services.AddTransient<ViewModels.UsersViewModel>();
            services.AddTransient<ViewModels.SettingsViewModel>();
            services.AddTransient<ViewModels.ReportingViewModel>();
            services.AddTransient<ViewModels.HelpViewModel>();
            services.AddTransient<ViewModels.AboutViewModel>();
            services.AddTransient<ViewModels.AdminProceduresViewModel>();

            // Windows
            services.AddTransient<Views.LoginWindow>();
            services.AddTransient<Views.MainWindow>();
            services.AddTransient<Views.LockScreenWindow>();
        }
    }

    /// <summary>
    /// محول النص إلى الظهور
    /// String to Visibility Converter
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Visibility.Collapsed;

            string valueStr = value.ToString() ?? string.Empty;

            // If a parameter is provided, compare it with the value
            if (parameter != null)
            {
                string paramStr = parameter.ToString() ?? string.Empty;
                return valueStr.Equals(paramStr, StringComparison.OrdinalIgnoreCase) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }

            // Fallback for no parameter: visible if string is not empty
            return !string.IsNullOrEmpty(valueStr) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
