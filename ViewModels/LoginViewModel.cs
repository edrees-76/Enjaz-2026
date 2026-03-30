using System;
using System.Windows;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Enjaz.ViewModels
{
    /// <summary>
    /// ViewModel لواجهة تسجيل الدخول
    /// ViewModel for Login Interface
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        private readonly Services.Repositories.UserRepository _userRepository;
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

        // Rate Limiting — حماية ضد محاولات الاختراق
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 5;
        private int _failedAttempts = 0;
        private DateTime _lockoutEnd = DateTime.MinValue;

        /// <summary>
        /// إنشاء مثيل جديد
        /// </summary>
        public LoginViewModel(
            Services.Repositories.UserRepository userRepository, 
            INotificationService notificationService,
            UserService userService,
            SettingsService settingsService,
            ThemeService themeService,
            IServiceProvider serviceProvider)
        {
            _userRepository = userRepository;
            _notificationService = notificationService;
            _userService = userService;
            _settingsService = settingsService;
            _themeService = themeService;
            _serviceProvider = serviceProvider;
            
            // Load Remember Me settings
            var settings = _settingsService.Current;
            
            RememberMe = settings.RememberMeEnabled;
            IsDarkMode = settings.LoginIsDarkMode;
            if (RememberMe)
            {
                Username = settings.RememberedUsername;
            }
            
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            TogglePasswordVisibilityCommand = new RelayCommand(_ => IsPasswordVisible = !IsPasswordVisible);
            ToggleThemeCommand = new RelayCommand(_ => {
                IsDarkMode = !IsDarkMode;
                _themeService.ApplyTheme(_themeService.CurrentTheme, IsDarkMode);
                
                // Save preference for both Login and App to keep them synced
                var s = _settingsService.Current;
                s.LoginIsDarkMode = IsDarkMode;
                s.AppIsDarkMode = IsDarkMode;
                _settingsService.SaveSettings(s);
            });
        }

        /// <summary>
        /// عرض رسالة تنبيه بانتهاء الجلسة
        /// </summary>
        public void ShowSessionExpiredMessage()
        {
            SetNotification("انتهت الجلسة", "تم تسجيل الخروج تلقائياً بسبب عدم النشاط لفترة طويلة (20 دقيقة) لحماية بياناتك.", NotificationType.Warning, "TimerOffOutline");
        }

        #region Properties

        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                ErrorMessage = string.Empty;
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                ErrorMessage = string.Empty;
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set => SetProperty(ref _isDarkMode, value);
        }

        public MaterialDesignThemes.Wpf.ISnackbarMessageQueue MessageQueue => _notificationService.MessageQueue;

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }
        public ICommand TogglePasswordVisibilityCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        private bool CanExecuteLogin(object? parameter)
        {
            if (DateTime.Now < _lockoutEnd) return false;
            return !string.IsNullOrWhiteSpace(Username) && 
                   !string.IsNullOrWhiteSpace(Password) && 
                   !IsBusy;
        }

        private async void ExecuteLogin(object? parameter)
        {
            if (IsBusy) return; // Immediate guard against spam clicks

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                // Rate Limiting check
                if (DateTime.Now < _lockoutEnd)
                {
                    var remaining = (_lockoutEnd - DateTime.Now).Minutes + 1;
                    IsBusy = false;
                    SetNotification("محاولات كثيرة", $"تم قفل تسجيل الدخول مؤقتاً. يرجى الانتظار {remaining} دقيقة.", NotificationType.Error, "LockOutline");
                    return;
                }

                // Validate first (quick check)
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                     // Should be caught by CanExecute, but just in case
                     return;
                }

                
                // Consolidating steps to make login instantaneous
                BusyMessage = "جاري التحقق من بيانات الدخول والتهيئة...";
                
                var (user, result) = await _userRepository.ValidateUserWithStatusAsync(Username, Password);

                switch (result)
                {
                    case LoginResult.Success:
                        // Reset rate limiting on success
                        _failedAttempts = 0;
                        _lockoutEnd = DateTime.MinValue;

                        // 0. Update Remember Me settings
                        var settings = _settingsService.Current;
                        settings.RememberMeEnabled = RememberMe;
                        settings.RememberedUsername = RememberMe ? Username : string.Empty;
                        _settingsService.SaveSettings(settings);

                        // 1. Set current user in UserService
                        _userService.Login(user!);

                        try
                        {
                            var dbService = _serviceProvider.GetService(typeof(DatabaseService)) as DatabaseService;
                            if (dbService != null)
                            {
                                await dbService.LogActionAsync(user!.Id, user.Username, "تسجيل دخول", "تم تسجيل الدخول إلى المنظومة بنجاح.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Services.LoggerService.LogError("Audit Log Error", ex);
                        }

                        // 1.5 Apply Unified Theme (Sync App with current Login preference)
                        settings.AppIsDarkMode = IsDarkMode;
                        settings.LoginIsDarkMode = IsDarkMode;
                        _settingsService.SaveSettings(settings);
                        
                        _themeService.ApplyTheme(_themeService.CurrentTheme, IsDarkMode);

                        // 2. Resolve MainWindow using DI
                        var mainWindow = _serviceProvider.GetRequiredService<Views.MainWindow>();
                        mainWindow.Show();

                        // 3. Close Login Window
                        if (parameter is Window loginWindow)
                        {
                            loginWindow.Close();
                        }
                        else
                        {
                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window is Views.LoginWindow)
                                {
                                    window.Close();
                                    break;
                                }
                            }
                        }
                        break;

                    case LoginResult.AccountFrozen:
                        IsBusy = false;
                        SetNotification("حساب مجمد", "حسابك مجمد، يرجى مراجعة مدير النظام.", NotificationType.Error, "AccountCancelOutline");
                        break;

                    case LoginResult.InvalidCredentials:
                        IsBusy = false;
                        _failedAttempts++;
                        if (_failedAttempts >= MaxFailedAttempts)
                        {
                            _lockoutEnd = DateTime.Now.AddMinutes(LockoutMinutes);
                            _failedAttempts = 0;
                            Services.LoggerService.LogWarning($"Login lockout triggered for user '{Username}' after {MaxFailedAttempts} failed attempts.");
                            SetNotification("تم القفل مؤقتاً", $"تم قفل تسجيل الدخول لمدة {LockoutMinutes} دقائق بسبب محاولات فاشلة متكررة.", NotificationType.Error, "LockOutline");
                        }
                        else
                        {
                            int remaining = MaxFailedAttempts - _failedAttempts;
                            SetNotification("خطأ في البيانات", $"خطأ في المعلومات. لديك {remaining} محاولات متبقية قبل القفل المؤقت.", NotificationType.Error, "LockAlertOutline");
                        }
                        break;

                    default:
                        IsBusy = false;
                        SetNotification("خطأ نظام", "حدث خطأ غير متوقع أثناء تسجيل الدخول. يرجى المحاولة لاحقاً.", NotificationType.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                SetNotification("فشل العملية", $"حدث خطأ فني: {ex.Message}", NotificationType.Error, "BugOutline");
                Services.LoggerService.LogError("Login UI Error", ex);
            }
            finally
            {
                // Only turn off busy if we didn't succeed (success closes the window)
                if (IsNotificationDialogOpen) 
                    IsBusy = false;
            }
        }

        #endregion
    }
}
