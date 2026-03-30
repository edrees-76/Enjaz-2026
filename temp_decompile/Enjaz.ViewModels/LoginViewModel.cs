using System;
using System.Windows;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Services.Repositories;
using Enjaz.Views;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace Enjaz.ViewModels;

public class LoginViewModel : BaseViewModel
{
	private readonly UserRepository _userRepository;

	private readonly INotificationService _notificationService;

	private readonly UserService _userService;

	private readonly SettingsService _settingsService;

	private readonly ThemeService _themeService;

	private readonly IServiceProvider _serviceProvider;

	private string _username = string.Empty;

	private string _password = string.Empty;

	private string _errorMessage = string.Empty;

	private bool _rememberMe = true;

	private bool _isPasswordVisible = false;

	private bool _isDarkMode = true;

	private const int MaxFailedAttempts = 5;

	private const int LockoutMinutes = 5;

	private int _failedAttempts = 0;

	private DateTime _lockoutEnd = DateTime.MinValue;

	public string Username
	{
		get
		{
			return _username;
		}
		set
		{
			SetProperty(ref _username, value, "Username");
			ErrorMessage = string.Empty;
		}
	}

	public string Password
	{
		get
		{
			return _password;
		}
		set
		{
			SetProperty(ref _password, value, "Password");
			ErrorMessage = string.Empty;
		}
	}

	public string ErrorMessage
	{
		get
		{
			return _errorMessage;
		}
		set
		{
			SetProperty(ref _errorMessage, value, "ErrorMessage");
		}
	}

	public bool RememberMe
	{
		get
		{
			return _rememberMe;
		}
		set
		{
			SetProperty(ref _rememberMe, value, "RememberMe");
		}
	}

	public bool IsPasswordVisible
	{
		get
		{
			return _isPasswordVisible;
		}
		set
		{
			SetProperty(ref _isPasswordVisible, value, "IsPasswordVisible");
		}
	}

	public bool IsDarkMode
	{
		get
		{
			return _isDarkMode;
		}
		set
		{
			SetProperty(ref _isDarkMode, value, "IsDarkMode");
		}
	}

	public ISnackbarMessageQueue MessageQueue => _notificationService.MessageQueue;

	public ICommand LoginCommand { get; }

	public ICommand TogglePasswordVisibilityCommand { get; }

	public ICommand ToggleThemeCommand { get; }

	public LoginViewModel(UserRepository userRepository, INotificationService notificationService, UserService userService, SettingsService settingsService, ThemeService themeService, IServiceProvider serviceProvider)
	{
		_userRepository = userRepository;
		_notificationService = notificationService;
		_userService = userService;
		_settingsService = settingsService;
		_themeService = themeService;
		_serviceProvider = serviceProvider;
		AppSettings current = _settingsService.Current;
		RememberMe = current.RememberMeEnabled;
		IsDarkMode = current.LoginIsDarkMode;
		if (RememberMe)
		{
			Username = current.RememberedUsername;
		}
		LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
		TogglePasswordVisibilityCommand = new RelayCommand(delegate
		{
			IsPasswordVisible = !IsPasswordVisible;
		});
		ToggleThemeCommand = new RelayCommand(delegate
		{
			IsDarkMode = !IsDarkMode;
			_themeService.ApplyTheme(_themeService.CurrentTheme, IsDarkMode);
			AppSettings current2 = _settingsService.Current;
			current2.LoginIsDarkMode = IsDarkMode;
			current2.AppIsDarkMode = IsDarkMode;
			_settingsService.SaveSettings(current2);
		});
	}

	public void ShowSessionExpiredMessage()
	{
		SetNotification("انتهت الجلسة", "تم تسجيل الخروج تلقائيا\u064b بسبب عدم النشاط لفترة طويلة (20 دقيقة) لحماية بياناتك.", NotificationType.Warning, "TimerOffOutline");
	}

	private bool CanExecuteLogin(object? parameter)
	{
		if (DateTime.Now < _lockoutEnd)
		{
			return false;
		}
		return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password) && !base.IsBusy;
	}

	private async void ExecuteLogin(object? parameter)
	{
		if (base.IsBusy)
		{
			return;
		}
		base.IsBusy = true;
		ErrorMessage = string.Empty;
		try
		{
			if (DateTime.Now < _lockoutEnd)
			{
				int remaining = (_lockoutEnd - DateTime.Now).Minutes + 1;
				base.IsBusy = false;
				SetNotification("محاولات كثيرة", $"تم قفل تسجيل الدخول مؤقتا\u064b. يرجى الانتظار {remaining} دقيقة.", NotificationType.Error, "LockOutline");
			}
			else
			{
				if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
				{
					return;
				}
				base.BusyMessage = "جاري التحقق من بيانات الدخول والتهيئة...";
				(User? user, LoginResult result) tuple = await _userRepository.ValidateUserWithStatusAsync(Username, Password);
				var (user, _) = tuple;
				switch (tuple.result)
				{
				case LoginResult.Success:
				{
					_failedAttempts = 0;
					_lockoutEnd = DateTime.MinValue;
					AppSettings settings = _settingsService.Current;
					settings.RememberMeEnabled = RememberMe;
					settings.RememberedUsername = (RememberMe ? Username : string.Empty);
					_settingsService.SaveSettings(settings);
					_userService.Login(user);
					settings.AppIsDarkMode = IsDarkMode;
					settings.LoginIsDarkMode = IsDarkMode;
					_settingsService.SaveSettings(settings);
					_themeService.ApplyTheme(_themeService.CurrentTheme, IsDarkMode);
					MainWindow mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
					mainWindow.Show();
					if (parameter is Window loginWindow)
					{
						loginWindow.Close();
						break;
					}
					foreach (Window window in Application.Current.Windows)
					{
						if (window is LoginWindow)
						{
							window.Close();
							break;
						}
					}
					break;
				}
				case LoginResult.AccountFrozen:
					base.IsBusy = false;
					SetNotification("حساب مجمد", "حسابك مجمد، يرجى مراجعة مدير النظام.", NotificationType.Error, "AccountCancelOutline");
					break;
				case LoginResult.InvalidCredentials:
					base.IsBusy = false;
					_failedAttempts++;
					if (_failedAttempts >= 5)
					{
						_lockoutEnd = DateTime.Now.AddMinutes(5.0);
						_failedAttempts = 0;
						LoggerService.LogWarning($"Login lockout triggered for user '{Username}' after {5} failed attempts.");
						SetNotification("تم القفل مؤقتا\u064b", $"تم قفل تسجيل الدخول لمدة {5} دقائق بسبب محاولات فاشلة متكررة.", NotificationType.Error, "LockOutline");
					}
					else
					{
						int remaining2 = 5 - _failedAttempts;
						SetNotification("خطأ في البيانات", $"خطأ في المعلومات. لديك {remaining2} محاولات متبقية قبل القفل المؤقت.", NotificationType.Error, "LockAlertOutline");
					}
					break;
				default:
					base.IsBusy = false;
					SetNotification("خطأ نظام", "حدث خطأ غير متوقع أثناء تسجيل الدخول. يرجى المحاولة لاحقا\u064b.", NotificationType.Error);
					break;
				}
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			base.IsBusy = false;
			SetNotification("فشل العملية", "حدث خطأ فني: " + ex2.Message, NotificationType.Error, "BugOutline");
			LoggerService.LogError("Login UI Error", ex2);
		}
		finally
		{
			if (base.IsNotificationDialogOpen)
			{
				base.IsBusy = false;
			}
		}
	}
}
