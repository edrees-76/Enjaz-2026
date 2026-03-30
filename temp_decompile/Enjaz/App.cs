using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Enjaz.Services;
using Enjaz.Services.Repositories;
using Enjaz.ViewModels;
using Enjaz.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Enjaz;

public class App : Application
{
	private bool _contentLoaded;

	public IServiceProvider ServiceProvider { get; private set; } = null;

	public App()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		base.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
		AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
	}

	private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		LoggerService.LogError("AppDomain Unhandled Exception", e.ExceptionObject as Exception);
	}

	private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		LoggerService.LogError("TaskScheduler Unobserved Exception", e.Exception);
		e.SetObserved();
	}

	private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		LoggerService.LogError("Unhandled Exception", e.Exception);
		ErrorDialogWindow errorDialogWindow = new ErrorDialogWindow("خطأ غير متوقع", e.Exception.Message);
		errorDialogWindow.ShowDialog();
		e.Handled = true;
	}

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		LoggerService.LogInfo("Application Started");
		ServiceCollection serviceCollection = new ServiceCollection();
		ConfigureServices(serviceCollection);
		ServiceProvider = serviceCollection.BuildServiceProvider();
		try
		{
			ServiceProvider.GetRequiredService<DatabaseService>();
			UserRepository userRepository = ServiceProvider.GetRequiredService<UserRepository>();
			await userRepository.CreateDefaultAdminAsync();
			SettingsService settingsService = ServiceProvider.GetRequiredService<SettingsService>();
			ThemeService themeService = ServiceProvider.GetRequiredService<ThemeService>();
			AppSettings settings = settingsService.Current;
			themeService.Initialize(settings.CurrentTheme, settings.LoginIsDarkMode, settings.FontSizeScale);
			LoginWindow loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
			loginWindow.Show();
			LoggerService.LogInfo("LoginWindow Shown.");
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			LoggerService.LogError("Application Startup Failed", ex2);
			ErrorDialogWindow errorWindow = new ErrorDialogWindow("خطأ جسيم", "فشل بدء تشغيل النظام: " + ex2.Message + "\n\nيرجى التواصل مع الدعم الفني.");
			errorWindow.ShowDialog();
			Shutdown();
		}
	}

	private void ConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<ILoggerService, LoggerService>();
		services.AddSingleton<DatabaseService>();
		services.AddSingleton<UserRepository>();
		services.AddSingleton<CertificateRepository>();
		services.AddSingleton<SampleReceptionRepository>();
		services.AddSingleton<INavigationService, NavigationService>();
		services.AddSingleton<IPdfService, PdfService>();
		services.AddSingleton<ThemeService>();
		services.AddSingleton<INotificationService, SnackbarService>();
		services.AddSingleton<UserService>();
		services.AddSingleton<ExcelExportService>();
		services.AddSingleton(new SessionTimeoutService(20));
		services.AddSingleton<SettingsService>();
		services.AddSingleton<BackupService>();
		services.AddSingleton<ReportingService>();
		services.AddSingleton<AdvancedExcelService>();
		services.AddSingleton<ReportPdfService>();
		services.AddSingleton<ChartImageGenerator>();
		services.AddSingleton<IOSService, OSService>();
		services.AddSingleton<ArchiveService>();
		services.AddSingleton<IAppAlertService, AppAlertService>();
		services.AddSingleton<IDialogService, WpfDialogService>();
		services.AddSingleton<IReceptionSearchService, WpfReceptionSearchService>();
		services.AddSingleton<HelpDataService>();
		services.AddTransient<LoginViewModel>();
		services.AddTransient<MainViewModel>();
		services.AddTransient<DashboardViewModel>();
		services.AddTransient<SampleReceptionsViewModel>();
		services.AddTransient<CertificatesViewModel>();
		services.AddTransient<UsersViewModel>();
		services.AddTransient<SettingsViewModel>();
		services.AddTransient<ReportingViewModel>();
		services.AddTransient<HelpViewModel>();
		services.AddTransient<AboutViewModel>();
		services.AddTransient<AdminProceduresViewModel>();
		services.AddTransient<LoginWindow>();
		services.AddTransient<MainWindow>();
		services.AddTransient<LockScreenWindow>();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.21.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Enjaz;component/app.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.21.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
