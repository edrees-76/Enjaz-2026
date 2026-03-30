using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Enjaz.ViewModels;

public class MainViewModel : BaseViewModel
{
	private readonly INavigationService _navigationService;

	private readonly UserService _userService;

	private readonly ThemeService _themeService;

	private readonly INotificationService _notificationService;

	private readonly SessionTimeoutService _sessionTimeoutService;

	private readonly SettingsService _settingsService;

	private readonly DispatcherTimer _timer;

	private NavigationDestination _currentView = NavigationDestination.Home;

	private string _currentDateTime = string.Empty;

	private bool _isViewingDetails;

	private bool _isSidebarVisible = true;

	private bool _isSecurityChallengeOpen;

	private string _securityChallengeTitle = string.Empty;

	private string _securityChallengeMessage = string.Empty;

	private string _securityChallengePhraseInput = string.Empty;

	private string _securityChallengePasswordInput = string.Empty;

	private bool _isPasswordRequired;

	private bool _isPhraseRequired;

	private string _targetPhrase = string.Empty;

	private Action<bool>? _challengeCallback;

	private Action<bool>? _confirmCallback;

	private readonly IAppAlertService _alertService;

	private readonly BackupService _backupService;

	private ObservableCollection<AppAlert> _alerts = new ObservableCollection<AppAlert>();

	private bool _isAlertsPopupOpen;

	public DashboardViewModel DashboardVM { get; }

	public SampleReceptionsViewModel SampleReceptionsVM { get; }

	public CertificatesViewModel CertificatesVM { get; }

	public UsersViewModel UsersVM { get; }

	public SettingsViewModel SettingsVM { get; }

	public ReportingViewModel ReportingVM { get; }

	public HelpViewModel HelpVM { get; }

	public AboutViewModel AboutVM { get; }

	public AdminProceduresViewModel AdminProceduresVM { get; }

	public User? CurrentUser => _userService.CurrentUser;

	public ObservableCollection<AppAlert> Alerts
	{
		get
		{
			return _alerts;
		}
		set
		{
			SetProperty(ref _alerts, value, "Alerts");
			OnPropertyChanged("AlertCount");
			OnPropertyChanged("HasAlerts");
		}
	}

	public int AlertCount => Alerts.Count;

	public bool HasAlerts => Alerts.Any();

	public bool IsAlertsPopupOpen
	{
		get
		{
			return _isAlertsPopupOpen;
		}
		set
		{
			SetProperty(ref _isAlertsPopupOpen, value, "IsAlertsPopupOpen");
		}
	}

	public ICommand ToggleAlertsCommand => new RelayCommand(delegate
	{
		if (IsAlertsEnabled)
		{
			IsAlertsPopupOpen = !IsAlertsPopupOpen;
		}
	});

	public bool IsAlertsEnabled
	{
		get
		{
			return _settingsService.Current.EnableAlerts;
		}
		set
		{
			if (_settingsService.Current.EnableAlerts != value)
			{
				SettingsVM.EnableAlerts = value;
			}
		}
	}

	public ICommand ToggleAlertsEnabledCommand => new RelayCommand(delegate
	{
		IsAlertsEnabled = !IsAlertsEnabled;
	});

	public bool AppIsDarkMode
	{
		get
		{
			return _settingsService.Current.AppIsDarkMode;
		}
		set
		{
			if (_settingsService.Current.AppIsDarkMode != value)
			{
				AppSettings current = _settingsService.Current;
				current.AppIsDarkMode = value;
				current.LoginIsDarkMode = value;
				_settingsService.SaveSettings(current);
				_themeService.ApplyTheme(_themeService.CurrentTheme, value);
				OnPropertyChanged("AppIsDarkMode");
			}
		}
	}

	public NavigationDestination CurrentView
	{
		get
		{
			return _currentView;
		}
		set
		{
			SetProperty(ref _currentView, value, "CurrentView");
		}
	}

	public string CurrentDateTime
	{
		get
		{
			return _currentDateTime;
		}
		set
		{
			SetProperty(ref _currentDateTime, value, "CurrentDateTime");
		}
	}

	public bool IsViewingDetails
	{
		get
		{
			return _isViewingDetails;
		}
		set
		{
			SetProperty(ref _isViewingDetails, value, "IsViewingDetails");
		}
	}

	public bool IsSidebarVisible
	{
		get
		{
			return _isSidebarVisible;
		}
		set
		{
			SetProperty(ref _isSidebarVisible, value, "IsSidebarVisible");
		}
	}

	public bool IsAnyModalOpen => SampleReceptionsVM.IsEditing || SampleReceptionsVM.IsViewingDetails || SampleReceptionsVM.IsSelectingType || CertificatesVM.IsEditing || CertificatesVM.IsViewingDetails || UsersVM.IsEditingUser || UsersVM.IsUserDialogOpen;

	public bool IsSecurityChallengeOpen
	{
		get
		{
			return _isSecurityChallengeOpen;
		}
		set
		{
			SetProperty(ref _isSecurityChallengeOpen, value, "IsSecurityChallengeOpen");
		}
	}

	public string SecurityChallengeTitle
	{
		get
		{
			return _securityChallengeTitle;
		}
		set
		{
			SetProperty(ref _securityChallengeTitle, value, "SecurityChallengeTitle");
		}
	}

	public string SecurityChallengeMessage
	{
		get
		{
			return _securityChallengeMessage;
		}
		set
		{
			SetProperty(ref _securityChallengeMessage, value, "SecurityChallengeMessage");
		}
	}

	public string SecurityChallengeRequiredPhrase
	{
		get
		{
			return _targetPhrase;
		}
		set
		{
			SetProperty(ref _targetPhrase, value, "SecurityChallengeRequiredPhrase");
		}
	}

	public string SecurityChallengePhraseInput
	{
		get
		{
			return _securityChallengePhraseInput;
		}
		set
		{
			SetProperty(ref _securityChallengePhraseInput, value, "SecurityChallengePhraseInput");
		}
	}

	public string SecurityChallengePasswordInput
	{
		get
		{
			return _securityChallengePasswordInput;
		}
		set
		{
			SetProperty(ref _securityChallengePasswordInput, value, "SecurityChallengePasswordInput");
		}
	}

	public bool IsPasswordRequired
	{
		get
		{
			return _isPasswordRequired;
		}
		set
		{
			SetProperty(ref _isPasswordRequired, value, "IsPasswordRequired");
		}
	}

	public bool IsPhraseRequired
	{
		get
		{
			return _isPhraseRequired;
		}
		set
		{
			SetProperty(ref _isPhraseRequired, value, "IsPhraseRequired");
		}
	}

	public ICommand ShowHomeCommand { get; private set; } = null;

	public ICommand ShowSampleReceptionsCommand { get; private set; } = null;

	public ICommand ShowCertificatesCommand { get; private set; } = null;

	public ICommand ShowUsersCommand { get; private set; } = null;

	public ICommand ShowReportsCommand { get; private set; } = null;

	public ICommand ShowAdminProceduresCommand { get; private set; } = null;

	public ICommand RefreshCommand { get; private set; } = null;

	public ICommand LogoutCommand { get; private set; } = null;

	public ICommand ShowSettingsCommand { get; private set; } = null;

	public ICommand ShowHelpCommand { get; private set; } = null;

	public ICommand ShowAboutCommand { get; private set; } = null;

	public ICommand CloseDetailsCommand { get; private set; } = null;

	public ICommand EscapeCommand { get; private set; } = null;

	public ICommand SubmitSecurityChallengeCommand { get; private set; } = null;

	public ICommand CancelSecurityChallengeCommand { get; private set; } = null;

	public ICommand ToggleSidebarCommand { get; private set; } = null;

	public ICommand ToggleThemeCommand { get; private set; } = null;

	public ICommand ShowContextualHelpCommand { get; private set; } = null;

	public ICommand ConfirmCommand { get; private set; } = null;

	public ICommand CancelCommand { get; private set; } = null;

	public MainViewModel(INavigationService navigationService, UserService userService, ThemeService themeService, INotificationService notificationService, SessionTimeoutService sessionTimeoutService, SettingsService settingsService, BackupService backupService, IAppAlertService alertService, DashboardViewModel dashboardVM, SampleReceptionsViewModel sampleReceptionsVM, CertificatesViewModel certificatesVM, UsersViewModel usersVM, SettingsViewModel settingsVM, ReportingViewModel reportingVM, HelpViewModel helpVM, AboutViewModel aboutVM, AdminProceduresViewModel adminProceduresVM)
	{
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Expected O, but got Unknown
		_navigationService = navigationService;
		_userService = userService;
		_themeService = themeService;
		_notificationService = notificationService;
		_sessionTimeoutService = sessionTimeoutService;
		_settingsService = settingsService;
		_backupService = backupService;
		_alertService = alertService;
		DashboardVM = dashboardVM;
		SampleReceptionsVM = sampleReceptionsVM;
		CertificatesVM = certificatesVM;
		UsersVM = usersVM;
		SettingsVM = settingsVM;
		ReportingVM = reportingVM;
		HelpVM = helpVM;
		AboutVM = aboutVM;
		AdminProceduresVM = adminProceduresVM;
		_currentView = _navigationService.CurrentDestination;
		_navigationService.NavigationChanged += OnNavigationChanged;
		SampleReceptionsVM.RequestNavigation += delegate(NavigationDestination dest)
		{
			_navigationService.NavigateTo(dest);
		};
		CertificatesVM.RequestNavigation += delegate(NavigationDestination dest)
		{
			_navigationService.NavigateTo(dest);
		};
		UsersVM.RequestNavigation += delegate(NavigationDestination dest)
		{
			_navigationService.NavigateTo(dest);
		};
		CertificatesVM.CertificateSaved += async delegate
		{
			if (CurrentView == NavigationDestination.CertificateForm)
			{
				_navigationService.NavigateTo(NavigationDestination.Certificates);
			}
			await DashboardVM.LoadDashboardDataAsync();
			await RefreshAlertsAsync();
		};
		SettingsVM.FactoryResetCompleted += async delegate
		{
			await DashboardVM.LoadDashboardDataAsync();
			await CertificatesVM.LoadCertificatesAsync();
			await UsersVM.LoadUsersAsync();
			await UsersVM.LoadActivitiesAsync();
			await RefreshAlertsAsync();
		};
		SettingsVM.RequestSecurityChallenge += delegate(string title, string message, string phrase, bool requirePassword, Action<bool> callback)
		{
			ShowSecurityChallenge(title, message, phrase, requirePassword, callback);
		};
		SettingsVM.PropertyChanged += delegate(object? s, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "EnableAlerts")
			{
				OnPropertyChanged("IsAlertsEnabled");
				RefreshAlertsAsync();
			}
		};
		CertificatesVM.PropertyChanged += delegate(object? s, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "StatusMessage")
			{
				base.StatusMessage = CertificatesVM.StatusMessage;
			}
		};
		UsersVM.PropertyChanged += delegate(object? s, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "StatusMessage")
			{
				base.StatusMessage = UsersVM.StatusMessage;
			}
			if (e.PropertyName == "IsNotificationDialogOpen" && UsersVM.IsNotificationDialogOpen)
			{
				ShowGlobalNotification(UsersVM.NotificationTitle, UsersVM.NotificationMessage, UsersVM.NotificationIcon, null, UsersVM.NotificationType);
				UsersVM.IsNotificationDialogOpen = false;
			}
		};
		CertificatesVM.PropertyChanged += delegate(object? s, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsNotificationDialogOpen" && CertificatesVM.IsNotificationDialogOpen)
			{
				ShowGlobalNotification(CertificatesVM.NotificationTitle, CertificatesVM.NotificationMessage, CertificatesVM.NotificationIcon, null, CertificatesVM.NotificationType);
				CertificatesVM.IsNotificationDialogOpen = false;
			}
		};
		base.RequestConfirmation += OnRequestConfirmation;
		DashboardVM.RequestConfirmation += OnRequestConfirmation;
		SampleReceptionsVM.RequestConfirmation += OnRequestConfirmation;
		CertificatesVM.RequestConfirmation += OnRequestConfirmation;
		UsersVM.RequestConfirmation += OnRequestConfirmation;
		SettingsVM.RequestConfirmation += OnRequestConfirmation;
		ReportingVM.RequestConfirmation += OnRequestConfirmation;
		HelpVM.RequestConfirmation += OnRequestConfirmation;
		AboutVM.RequestConfirmation += OnRequestConfirmation;
		AdminProceduresVM.RequestConfirmation += OnRequestConfirmation;
		UpdateDateTime();
		_timer = new DispatcherTimer();
		_timer.Interval = TimeSpan.FromSeconds(1.0);
		_timer.Tick += delegate
		{
			UpdateDateTime();
		};
		_timer.Start();
		_sessionTimeoutService.Start();
		_sessionTimeoutService.SessionExpired += async delegate
		{
			await PerformLogoutAsync(askConfirmation: false, isAutomatic: true);
		};
		InitializeCommands();
		DashboardVM.LoadDashboardDataAsync().ContinueWith(delegate(Task t)
		{
			LoggerService.LogError("Failed to load dashboard", t.Exception);
		}, TaskContinuationOptions.OnlyOnFaulted);
		CertificatesVM.LoadCertificatesAsync().ContinueWith(delegate(Task t)
		{
			LoggerService.LogError("Failed to load certificates", t.Exception);
		}, TaskContinuationOptions.OnlyOnFaulted);
		UsersVM.LoadUsersAsync().ContinueWith(delegate(Task t)
		{
			LoggerService.LogError("Failed to load users", t.Exception);
		}, TaskContinuationOptions.OnlyOnFaulted);
		RefreshAlertsAsync().ContinueWith(delegate(Task t)
		{
			LoggerService.LogError("Failed to refresh alerts", t.Exception);
		}, TaskContinuationOptions.OnlyOnFaulted);
		_userService.UserChanged += delegate
		{
			OnPropertyChanged("CurrentUser");
		};
		((DispatcherObject)Application.Current).Dispatcher.BeginInvoke((DispatcherPriority)6, (Delegate)(Action)delegate
		{
			string text = CurrentUser?.FullName ?? "المستخدم";
			ShowGlobalNotification("منظومة إنجاز", "مرحبا\u064b بك، " + text + "، تم تسجيل الدخول بنجاح الى المنظومة", null, "/Assets/enjaz_3d_icon_transparent.png");
		});
	}

	public async Task RefreshAlertsAsync()
	{
		try
		{
			if (!_settingsService.Current.EnableAlerts)
			{
				await ((DispatcherObject)Application.Current).Dispatcher.InvokeAsync((Action)delegate
				{
					Alerts.Clear();
				});
				return;
			}
			List<AppAlert> newAlerts = await _alertService.GetCurrentAlertsAsync();
			await ((DispatcherObject)Application.Current).Dispatcher.InvokeAsync((Action)delegate
			{
				Alerts = new ObservableCollection<AppAlert>(newAlerts);
			});
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			LoggerService.LogError("MainViewModel: Failed to refresh alerts", ex2);
		}
	}

	private void OnNavigationChanged(NavigationDestination destination)
	{
		if (destination != NavigationDestination.Reports)
		{
			ReportingVM.ResetReportingState();
		}
		CurrentView = destination;
		if (destination == NavigationDestination.Home)
		{
			DashboardVM.LoadDashboardDataAsync();
			RefreshAlertsAsync();
		}
	}

	private void InitializeCommands()
	{
		ShowHomeCommand = new RelayCommand(delegate
		{
			if (CheckNavigationSafety())
			{
				_navigationService.NavigateTo(NavigationDestination.Home);
			}
		});
		ShowSettingsCommand = new RelayCommand(delegate
		{
			if (CheckNavigationSafety())
			{
				_navigationService.NavigateTo(NavigationDestination.Settings);
			}
		});
		ShowHelpCommand = new RelayCommand(delegate
		{
			if (CheckNavigationSafety())
			{
				_navigationService.NavigateTo(NavigationDestination.Help);
			}
		});
		ShowAboutCommand = new RelayCommand(delegate
		{
			if (CheckNavigationSafety())
			{
				_navigationService.NavigateTo(NavigationDestination.About);
			}
		});
		ShowAdminProceduresCommand = new RelayCommand(delegate(object? _)
		{
			if (CheckNavigationSafety())
			{
				_navigationService.NavigateTo(NavigationDestination.AdminProcedures);
				AdminProceduresVM.CurrentStep = 1;
				_ = AdminProceduresVM.RefreshSendersAsync();
				_ = AdminProceduresVM.LoadReferralHistoryAsync();
			}
		});
		ShowSampleReceptionsCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			if (CheckNavigationSafety())
			{
				_navigationService.NavigateTo(NavigationDestination.SampleReceptions);
				await SampleReceptionsVM.LoadReceptionsAsync();
			}
		}, (Predicate<object?>?)null, (AsyncRelayCommand.IErrorHandler?)null);
		ShowCertificatesCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			if (CheckNavigationSafety())
			{
				_navigationService.NavigateTo(NavigationDestination.Certificates);
				await CertificatesVM.LoadCertificatesAsync();
			}
		}, (Predicate<object?>?)null, (AsyncRelayCommand.IErrorHandler?)null);
		ShowUsersCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			if (CheckNavigationSafety())
			{
				_navigationService.NavigateTo(NavigationDestination.Users);
				await UsersVM.LoadUsersAsync();
				await UsersVM.LoadActivitiesAsync();
			}
		}, (Predicate<object?>?)((object? _) => CurrentUser?.CanManageUsers ?? false), (AsyncRelayCommand.IErrorHandler?)null);
		ShowReportsCommand = new RelayCommand(delegate
		{
			if (CheckNavigationSafety())
			{
				_navigationService.NavigateTo(NavigationDestination.Reports);
				ReportingVM.CurrentStep = 1;
			}
		});
		RefreshCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await DashboardVM.LoadDashboardDataAsync();
			if (CurrentView == NavigationDestination.SampleReceptions)
			{
				await SampleReceptionsVM.LoadReceptionsAsync();
			}
			if (CurrentView == NavigationDestination.Certificates)
			{
				await CertificatesVM.LoadCertificatesAsync();
			}
			if (CurrentView == NavigationDestination.Users)
			{
				await UsersVM.LoadUsersAsync();
				await UsersVM.LoadActivitiesAsync();
			}
			if (CurrentView == NavigationDestination.AdminProcedures)
			{
				await AdminProceduresVM.RefreshSendersAsync();
			}
		}, (Predicate<object?>?)null, (AsyncRelayCommand.IErrorHandler?)null);
		LogoutCommand = new AsyncRelayCommand(ExecuteLogout);
		CloseDetailsCommand = new RelayCommand(delegate
		{
			IsViewingDetails = false;
		});
		EscapeCommand = new RelayCommand(delegate
		{
			ExecuteEscape();
		});
		SubmitSecurityChallengeCommand = new RelayCommand(delegate
		{
			ExecuteSubmitSecurityChallenge();
		});
		CancelSecurityChallengeCommand = new RelayCommand(delegate
		{
			IsSecurityChallengeOpen = false;
			_challengeCallback?.Invoke(obj: false);
		});
		ToggleSidebarCommand = new RelayCommand(delegate
		{
			IsSidebarVisible = !IsSidebarVisible;
		});
		ToggleThemeCommand = new RelayCommand(delegate
		{
			AppIsDarkMode = !AppIsDarkMode;
		});
		ShowContextualHelpCommand = new RelayCommand(delegate(object? obj)
		{
			ShowContextualHelp(obj?.ToString() ?? string.Empty);
		});
		ConfirmCommand = new RelayCommand(delegate
		{
			base.IsNotificationDialogOpen = false;
			_confirmCallback?.Invoke(obj: true);
		});
		CancelCommand = new RelayCommand(delegate
		{
			base.IsNotificationDialogOpen = false;
			_confirmCallback?.Invoke(obj: false);
		});
	}

	private void ShowContextualHelp(string viewName)
	{
		if (CheckNavigationSafety())
		{
			_navigationService.NavigateTo(NavigationDestination.Help);
			HelpVM.ShowHelpForViewAsync(viewName);
		}
	}

	private void ExecuteEscape()
	{
		if (SampleReceptionsVM.IsViewingDetails)
		{
			SampleReceptionsVM.CloseDetailsCommand.Execute(null);
		}
		else if (CertificatesVM.IsViewingDetails)
		{
			CertificatesVM.CloseDetailsCommand.Execute(null);
		}
		else if (SampleReceptionsVM.IsEditing)
		{
			SampleReceptionsVM.CancelEditCommand.Execute(null);
		}
		else if (CertificatesVM.IsEditing)
		{
			CertificatesVM.CancelEditCommand.Execute(null);
		}
		else if (UsersVM.IsEditingUser)
		{
			UsersVM.CancelUserEditCommand.Execute(null);
		}
		else if (CurrentView == NavigationDestination.CertificateForm || CurrentView == NavigationDestination.UserForm || CurrentView == NavigationDestination.SampleReceptionForm)
		{
			if (CurrentView == NavigationDestination.SampleReceptionForm)
			{
				_navigationService.NavigateTo(NavigationDestination.SampleReceptions);
			}
			else if (CurrentView == NavigationDestination.CertificateForm)
			{
				_navigationService.NavigateTo(NavigationDestination.Certificates);
			}
			else if (CurrentView == NavigationDestination.UserForm)
			{
				_navigationService.NavigateTo(NavigationDestination.Users);
			}
		}
	}

	private void UpdateDateTime()
	{
		CurrentDateTime = DateTime.Now.ToString("dddd، dd MMMM yyyy - hh:mm tt", new CultureInfo("ar-LY"));
	}

	private async Task ExecuteLogout(object? parameter)
	{
		if (CheckNavigationSafety())
		{
			await PerformLogoutAsync(askConfirmation: true);
		}
	}

	public async Task PerformLogoutAsync(bool askConfirmation, bool isAutomatic = false)
	{
		if (askConfirmation)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			ShowConfirmDialog("تأكيد الخروج", "هل أنت متأكد من تسجيل الخروج والعودة لصفحة الدخول؟", NotificationType.Question, delegate(bool result)
			{
				tcs.SetResult(result);
			}, "Logout");
			if (!(await tcs.Task))
			{
				return;
			}
		}
		try
		{
			await Task.Yield();
			base.IsBusy = true;
			base.BusyMessage = "جاري حفظ الإعدادات والنسخ الاحتياطي...";
			await _backupService.AutoBackupAsync();
			DispatcherTimer timer = _timer;
			if (timer != null)
			{
				timer.Stop();
			}
			_sessionTimeoutService.Stop();
			_userService.Logout();
			_themeService.ApplyTheme(_themeService.CurrentTheme, _settingsService.Current.LoginIsDarkMode);
			base.IsBusy = false;
			Window nextWindow;
			if (isAutomatic)
			{
				Application current = Application.Current;
				nextWindow = ((!(current is App app)) ? new LockScreenWindow() : app.ServiceProvider.GetRequiredService<LockScreenWindow>());
			}
			else
			{
				Application current = Application.Current;
				nextWindow = ((!(current is App app2)) ? new LoginWindow() : app2.ServiceProvider.GetRequiredService<LoginWindow>());
			}
			nextWindow?.Show();
			List<Window> openWindows = Application.Current.Windows.Cast<Window>().ToList();
			foreach (Window window in openWindows)
			{
				if (window == nextWindow)
				{
					continue;
				}
				try
				{
					if (window is MainWindow main)
					{
						main.SetLoggingOut();
						main.Close();
					}
					else
					{
						window.Close();
					}
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					LoggerService.LogWarning("Failed to close a window during logout: " + ex2.Message);
				}
			}
		}
		catch (Exception ex)
		{
			Exception ex3 = ex;
			base.IsBusy = false;
			LoggerService.LogError("Error during logout", ex3);
			((DispatcherObject)Application.Current).Dispatcher.Invoke((Action)delegate
			{
				ErrorDialogWindow errorDialogWindow = new ErrorDialogWindow("خطأ في النظام", "حدث خطأ أثناء تسجيل الخروج، سيتم إغلاق البرنامج.");
				errorDialogWindow.ShowDialog();
			});
			Application.Current.Shutdown();
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	private void ShowGlobalNotification(string title, string message, string? icon = null, string? imagePath = null, NotificationType type = NotificationType.Information)
	{
		base.IsConfirmMode = false;
		SetNotification(title, message, type, icon, imagePath);
	}

	private void OnRequestConfirmation(string title, string message, NotificationType type, Action<bool> callback, string? icon)
	{
		ShowConfirmDialog(title, message, type, callback, icon);
	}

	public void ShowConfirmDialog(string title, string message, NotificationType type, Action<bool> callback, string? icon = null)
	{
		_confirmCallback = callback;
		base.IsConfirmMode = true;
		SetNotification(title, message, type, icon);
	}

	public void ShowSecurityChallenge(string title, string message, string targetPhrase, bool requirePassword, Action<bool> callback)
	{
		SecurityChallengeTitle = title;
		SecurityChallengeMessage = message;
		SecurityChallengeRequiredPhrase = targetPhrase;
		IsPhraseRequired = !string.IsNullOrEmpty(targetPhrase);
		IsPasswordRequired = requirePassword;
		SecurityChallengePhraseInput = string.Empty;
		SecurityChallengePasswordInput = string.Empty;
		_challengeCallback = callback;
		IsSecurityChallengeOpen = true;
	}

	private void ExecuteSubmitSecurityChallenge()
	{
		if (IsPhraseRequired && SecurityChallengePhraseInput != _targetPhrase)
		{
			_notificationService.ShowError("الجملة التأكيدية غير صحيحة. يرجى التأكد من كتابتها بدقة.");
			return;
		}
		if (IsPasswordRequired && (CurrentUser == null || !PasswordHelper.VerifyPassword(SecurityChallengePasswordInput, CurrentUser.PasswordHash)))
		{
			_notificationService.ShowError("كلمة المرور غير صحيحة.");
			return;
		}
		IsSecurityChallengeOpen = false;
		_challengeCallback?.Invoke(obj: true);
	}

	private bool CheckNavigationSafety()
	{
		if (IsAnyModalOpen)
		{
			ShowGlobalNotification("نافذة مفتوحة", "يرجى إغلاق النافذة الحالية أو إنهاء العملية القائمة قبل الانتقال إلى قسم آخر.", "InformationOutline", null, NotificationType.Warning);
			return false;
		}
		return true;
	}
}
