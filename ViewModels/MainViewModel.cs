using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;

namespace Enjaz.ViewModels
{
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
        
        // Security Challenge Properties
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

        // Child ViewModels
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
        private readonly BackupService _backupService;

        private System.Collections.ObjectModel.ObservableCollection<AppAlert> _alerts = new();
        public System.Collections.ObjectModel.ObservableCollection<AppAlert> Alerts
        {
            get => _alerts;
            set 
            {
                SetProperty(ref _alerts, value);
                OnPropertyChanged(nameof(AlertCount));
                OnPropertyChanged(nameof(HasAlerts));
            }
        }

        public int AlertCount => Alerts.Count;
        public bool HasAlerts => Alerts.Any();

        private bool _isAlertsPopupOpen;
        public bool IsAlertsPopupOpen
        {
            get => _isAlertsPopupOpen;
            set => SetProperty(ref _isAlertsPopupOpen, value);
        }

        public MainViewModel(
            INavigationService navigationService,
            UserService userService,
            ThemeService themeService,
            INotificationService notificationService,
            SessionTimeoutService sessionTimeoutService,
            SettingsService settingsService,
            BackupService backupService,
            IAppAlertService alertService,
            DashboardViewModel dashboardVM,
            SampleReceptionsViewModel sampleReceptionsVM,
            CertificatesViewModel certificatesVM,
            UsersViewModel usersVM,
            SettingsViewModel settingsVM,
            ReportingViewModel reportingVM,
            HelpViewModel helpVM,
            AboutViewModel aboutVM,
            AdminProceduresViewModel adminProceduresVM)
        {
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

            // Initialize CurrentView from Service
            _currentView = _navigationService.CurrentDestination;
            
            // Subscribe to Navigation Changes
            _navigationService.NavigationChanged += OnNavigationChanged;

            // Wire up navigation requests
            SampleReceptionsVM.RequestNavigation += (dest) => _navigationService.NavigateTo(dest);
            CertificatesVM.RequestNavigation += (dest) => _navigationService.NavigateTo(dest);
            UsersVM.RequestNavigation += (dest) => _navigationService.NavigateTo(dest);

            // Update Dashboard and Alerts when certificate is saved or deleted
            CertificatesVM.CertificateSaved += async () => 
            {
                if (CurrentView == NavigationDestination.CertificateForm)
                {
                    _navigationService.NavigateTo(NavigationDestination.Certificates);
                }
                await DashboardVM.LoadDashboardDataAsync();
                await RefreshAlertsAsync();
            };

            // Refresh all data after Factory Reset
            SettingsVM.FactoryResetCompleted += async () =>
            {
                await DashboardVM.LoadDashboardDataAsync();
                await CertificatesVM.LoadCertificatesAsync();
                await UsersVM.LoadUsersAsync();
                await UsersVM.LoadActivitiesAsync();
                await RefreshAlertsAsync();
            };

            SettingsVM.RequestSecurityChallenge += (title, message, phrase, requirePassword, callback) =>
                ShowSecurityChallenge(title, message, phrase, requirePassword, callback);

            // Synchronize alerts state
            SettingsVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SettingsViewModel.EnableAlerts))
                {
                    OnPropertyChanged(nameof(IsAlertsEnabled));
                    _ = RefreshAlertsAsync();
                }
            };


            // Propagate Status Messages
            CertificatesVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(StatusMessage)) StatusMessage = CertificatesVM.StatusMessage;
            };
            UsersVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(StatusMessage)) StatusMessage = UsersVM.StatusMessage;
                if (e.PropertyName == nameof(IsNotificationDialogOpen) && UsersVM.IsNotificationDialogOpen)
                {
                    ShowGlobalNotification(UsersVM.NotificationTitle, UsersVM.NotificationMessage, UsersVM.NotificationIcon, null, UsersVM.NotificationType);
                    UsersVM.IsNotificationDialogOpen = false; // Reset to allow repeated triggers
                }
            };
            
            CertificatesVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsNotificationDialogOpen) && CertificatesVM.IsNotificationDialogOpen)
                {
                    ShowGlobalNotification(CertificatesVM.NotificationTitle, CertificatesVM.NotificationMessage, CertificatesVM.NotificationIcon, null, CertificatesVM.NotificationType);
                    CertificatesVM.IsNotificationDialogOpen = false; // Reset to allow repeated triggers
                }
            };



            // Wire up notification/confirmation events for self and children
            this.RequestConfirmation += OnRequestConfirmation;
            DashboardVM.RequestConfirmation += OnRequestConfirmation;
            SampleReceptionsVM.RequestConfirmation += OnRequestConfirmation;
            CertificatesVM.RequestConfirmation += OnRequestConfirmation;
            UsersVM.RequestConfirmation += OnRequestConfirmation;
            SettingsVM.RequestConfirmation += OnRequestConfirmation;
            ReportingVM.RequestConfirmation += OnRequestConfirmation;
            HelpVM.RequestConfirmation += OnRequestConfirmation;
            AboutVM.RequestConfirmation += OnRequestConfirmation;
            AdminProceduresVM.RequestConfirmation += OnRequestConfirmation;

            // Initialize DateTime Timer
            UpdateDateTime();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateDateTime();
            _timer.Start();

            // Start background services
            _sessionTimeoutService.Start();
            
            // Handle session timeout
            _sessionTimeoutService.SessionExpired += async () => await PerformLogoutAsync(askConfirmation: false, isAutomatic: true);

            InitializeCommands();

            // Load Initial Data (safe fire-and-forget with error logging)
            _ = DashboardVM.LoadDashboardDataAsync().ContinueWith(t => 
                Services.LoggerService.LogError("Failed to load dashboard", t.Exception!), 
                System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
            _ = CertificatesVM.LoadCertificatesAsync().ContinueWith(t => 
                Services.LoggerService.LogError("Failed to load certificates", t.Exception!), 
                System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
            _ = UsersVM.LoadUsersAsync().ContinueWith(t => 
                Services.LoggerService.LogError("Failed to load users", t.Exception!), 
                System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
            _ = RefreshAlertsAsync().ContinueWith(t => 
                Services.LoggerService.LogError("Failed to refresh alerts", t.Exception!), 
                System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);

            // Subscribe to user changes to update UI dynamically
            _userService.UserChanged += (u) => OnPropertyChanged(nameof(CurrentUser));

            // Show Welcome Message (deferred until window is fully loaded to prevent DialogHost NullRef)
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                string userName = CurrentUser?.FullName ?? "المستخدم";
                ShowGlobalNotification("منظومة إنجاز", $"مرحباً بك، {userName}، تم تسجيل الدخول بنجاح الى المنظومة", null, "/Assets/enjaz_3d_icon_transparent.png");
            });
        }

        public async Task RefreshAlertsAsync()
        {
            try
            {
                if (!_settingsService.Current.EnableAlerts)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Alerts.Clear();
                    });
                    return;
                }

                var newAlerts = await _alertService.GetCurrentAlertsAsync();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Alerts = new System.Collections.ObjectModel.ObservableCollection<AppAlert>(newAlerts);
                });
            }
            catch (Exception ex)
            {
                LoggerService.LogError("MainViewModel: Failed to refresh alerts", ex);
            }
        }

        public ICommand ToggleAlertsCommand => new RelayCommand(_ => 
        {
            if (IsAlertsEnabled)
                IsAlertsPopupOpen = !IsAlertsPopupOpen;
        });

        public bool IsAlertsEnabled
        {
            get => _settingsService.Current.EnableAlerts;
            set
            {
                if (_settingsService.Current.EnableAlerts != value)
                {
                    // Update via SettingsVM to trigger propagation and saving
                    SettingsVM.EnableAlerts = value;
                }
            }
        }

        public ICommand ToggleAlertsEnabledCommand => new RelayCommand(_ =>
        {
            IsAlertsEnabled = !IsAlertsEnabled;
        });

        public bool AppIsDarkMode
        {
            get => _settingsService.Current.AppIsDarkMode;
            set
            {
                if (_settingsService.Current.AppIsDarkMode != value)
                {
                    var settings = _settingsService.Current;
                    settings.AppIsDarkMode = value;
                    settings.LoginIsDarkMode = value; // Synchronize for login screen as well
                    _settingsService.SaveSettings(settings);

                    _themeService.ApplyTheme(_themeService.CurrentTheme, value);
                    OnPropertyChanged();
                }
            }
        }

        private void OnNavigationChanged(NavigationDestination destination)
        {
            // إذا كان المستخدم يغادر تبويب التقارير أو ينتقل إلى أي تبويب آخر، نقوم بإعادة ضبط حالة التقارير
            if (destination != NavigationDestination.Reports)
            {
                ReportingVM.ResetReportingState();
            }
            
            CurrentView = destination;

            // Refresh Dashboard and Alerts when navigating to Home
            if (destination == NavigationDestination.Home)
            {
                _ = DashboardVM.LoadDashboardDataAsync();
                _ = RefreshAlertsAsync();
            }
        }

        #region Properties

        public NavigationDestination CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string CurrentDateTime
        {
            get => _currentDateTime;
            set => SetProperty(ref _currentDateTime, value);
        }
        
        // Expose IsViewingDetails if needed by MainWindow binding or sub-views
        public bool IsViewingDetails
        {
             get => _isViewingDetails;
             set => SetProperty(ref _isViewingDetails, value);
        }

        public bool IsSidebarVisible
        {
            get => _isSidebarVisible;
            set => SetProperty(ref _isSidebarVisible, value);
        }

        public bool IsAnyModalOpen
        {
            get
            {
                return SampleReceptionsVM.IsEditing ||
                       SampleReceptionsVM.IsViewingDetails ||
                       SampleReceptionsVM.IsSelectingType ||
                       CertificatesVM.IsEditing ||
                       CertificatesVM.IsViewingDetails ||
                       UsersVM.IsEditingUser ||
                       UsersVM.IsUserDialogOpen;
            }
        }

        public bool IsSecurityChallengeOpen
        {
            get => _isSecurityChallengeOpen;
            set => SetProperty(ref _isSecurityChallengeOpen, value);
        }

        public string SecurityChallengeTitle
        {
            get => _securityChallengeTitle;
            set => SetProperty(ref _securityChallengeTitle, value);
        }

        public string SecurityChallengeMessage
        {
            get => _securityChallengeMessage;
            set => SetProperty(ref _securityChallengeMessage, value);
        }

        public string SecurityChallengeRequiredPhrase
        {
            get => _targetPhrase;
            set => SetProperty(ref _targetPhrase, value);
        }

        public string SecurityChallengePhraseInput
        {
            get => _securityChallengePhraseInput;
            set => SetProperty(ref _securityChallengePhraseInput, value);
        }

        public string SecurityChallengePasswordInput
        {
            get => _securityChallengePasswordInput;
            set => SetProperty(ref _securityChallengePasswordInput, value);
        }

        public bool IsPasswordRequired
        {
            get => _isPasswordRequired;
            set => SetProperty(ref _isPasswordRequired, value);
        }

        public bool IsPhraseRequired
        {
            get => _isPhraseRequired;
            set => SetProperty(ref _isPhraseRequired, value);
        }


        #endregion

        #region Commands

        public ICommand ShowHomeCommand { get; private set; } = null!;
        public ICommand ShowSampleReceptionsCommand { get; private set; } = null!;
        public ICommand ShowCertificatesCommand { get; private set; } = null!;
        public ICommand ShowUsersCommand { get; private set; } = null!;
        public ICommand ShowReportsCommand { get; private set; } = null!;
        public ICommand ShowAdminProceduresCommand { get; private set; } = null!;

        
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand LogoutCommand { get; private set; } = null!;
        public ICommand ShowSettingsCommand { get; private set; } = null!;
        public ICommand ShowHelpCommand { get; private set; } = null!;
        public ICommand ShowAboutCommand { get; private set; } = null!;

        public ICommand CloseDetailsCommand { get; private set; } = null!;
        public ICommand EscapeCommand { get; private set; } = null!;
        
        public ICommand SubmitSecurityChallengeCommand { get; private set; } = null!;
        public ICommand CancelSecurityChallengeCommand { get; private set; } = null!;
        public ICommand ToggleSidebarCommand { get; private set; } = null!;
        public ICommand ToggleThemeCommand { get; private set; } = null!;
        public ICommand ShowContextualHelpCommand { get; private set; } = null!;
        public ICommand ConfirmCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            ShowHomeCommand = new RelayCommand(_ => { if (CheckNavigationSafety()) _navigationService.NavigateTo(NavigationDestination.Home); });
            ShowSettingsCommand = new RelayCommand(_ => { if (CheckNavigationSafety()) _navigationService.NavigateTo(NavigationDestination.Settings); });
            ShowHelpCommand = new RelayCommand(_ => { if (CheckNavigationSafety()) _navigationService.NavigateTo(NavigationDestination.Help); });
            ShowAboutCommand = new RelayCommand(_ => { if (CheckNavigationSafety()) _navigationService.NavigateTo(NavigationDestination.About); });
            ShowAdminProceduresCommand = new RelayCommand(_ => 
            {
                if (!CheckNavigationSafety()) return;
                _navigationService.NavigateTo(NavigationDestination.AdminProcedures);
                AdminProceduresVM.CurrentStep = 1;
                _ = AdminProceduresVM.RefreshSendersAsync();
                _ = AdminProceduresVM.LoadReferralHistoryAsync();
            });
            
            ShowSampleReceptionsCommand = new AsyncRelayCommand(async _ => 
            {
                if (!CheckNavigationSafety()) return;
                _navigationService.NavigateTo(NavigationDestination.SampleReceptions);
                await SampleReceptionsVM.LoadReceptionsAsync();
            });

            ShowCertificatesCommand = new AsyncRelayCommand(async _ => 
            {
                if (!CheckNavigationSafety()) return;
                _navigationService.NavigateTo(NavigationDestination.Certificates);
                await CertificatesVM.LoadCertificatesAsync();
            });

            ShowUsersCommand = new AsyncRelayCommand(async _ => 
            {
                if (!CheckNavigationSafety()) return;
                _navigationService.NavigateTo(NavigationDestination.Users);
                await UsersVM.LoadUsersAsync();
                await UsersVM.LoadActivitiesAsync();
            }, _ => CurrentUser?.CanManageUsers ?? false);

            ShowReportsCommand = new RelayCommand(_ => 
            {
                if (!CheckNavigationSafety()) return;
                _navigationService.NavigateTo(NavigationDestination.Reports);
                ReportingVM.CurrentStep = 1;
            });

            RefreshCommand = new AsyncRelayCommand(async _ => 
            {
                await DashboardVM.LoadDashboardDataAsync();
                if (CurrentView == NavigationDestination.SampleReceptions) await SampleReceptionsVM.LoadReceptionsAsync();
                if (CurrentView == NavigationDestination.Certificates) await CertificatesVM.LoadCertificatesAsync();
                if (CurrentView == NavigationDestination.Users)
                {
                    await UsersVM.LoadUsersAsync();
                    await UsersVM.LoadActivitiesAsync();
                }
                if (CurrentView == NavigationDestination.AdminProcedures) await AdminProceduresVM.RefreshSendersAsync();
            });

            LogoutCommand = new AsyncRelayCommand(ExecuteLogout);
            
            CloseDetailsCommand = new RelayCommand(_ => IsViewingDetails = false);
            EscapeCommand = new RelayCommand(_ => ExecuteEscape());
            
            SubmitSecurityChallengeCommand = new RelayCommand(_ => ExecuteSubmitSecurityChallenge());
            CancelSecurityChallengeCommand = new RelayCommand(_ => 
            {
                IsSecurityChallengeOpen = false;
                _challengeCallback?.Invoke(false);
            });
            ToggleSidebarCommand = new RelayCommand(_ => IsSidebarVisible = !IsSidebarVisible);
            ToggleThemeCommand = new RelayCommand(_ => AppIsDarkMode = !AppIsDarkMode);
            ShowContextualHelpCommand = new RelayCommand(obj => ShowContextualHelp(obj?.ToString() ?? string.Empty));
            
            ConfirmCommand = new RelayCommand(_ => 
            {
                IsNotificationDialogOpen = false;
                _confirmCallback?.Invoke(true);
            });
            
            CancelCommand = new RelayCommand(_ => 
            {
                IsNotificationDialogOpen = false;
                _confirmCallback?.Invoke(false);
            });
        }

        private void ShowContextualHelp(string viewName)
        {
            if (!CheckNavigationSafety()) return;
            _navigationService.NavigateTo(NavigationDestination.Help);
            _ = HelpVM.ShowHelpForViewAsync(viewName);
        }

        /// <summary>
        /// معالج مفتاح Esc الذكي - يغلق النوافذ المنبثقة أو يلغي التعديل حسب الحالة
        /// Smart Esc key handler - closes overlays or cancels edits based on current state
        /// </summary>
        private void ExecuteEscape()
        {
            // 1. إذا كانت نافذة تفاصيل الاستلام مفتوحة
            if (SampleReceptionsVM.IsViewingDetails)
            {
                SampleReceptionsVM.CloseDetailsCommand.Execute(null);
                return;
            }

            // 1b. إذا كانت نافذة تفاصيل الشهادة مفتوحة
            if (CertificatesVM.IsViewingDetails)
            {
                CertificatesVM.CloseDetailsCommand.Execute(null);
                return;
            }

            // 2. إذا كان المستخدم في وضع التعديل (شهادة أو استلام)
            if (SampleReceptionsVM.IsEditing)
            {
                SampleReceptionsVM.CancelEditCommand.Execute(null);
                return;
            }

            if (CertificatesVM.IsEditing)
            {
                CertificatesVM.CancelEditCommand.Execute(null);
                return;
            }

            // 3. إذا كان المستخدم في وضع تعديل المستخدمين
            if (UsersVM.IsEditingUser)
            {
                UsersVM.CancelUserEditCommand.Execute(null);
                return;
            }

            // 4. إذا كان في صفحة فرعية، العودة للرئيسية
            if (CurrentView == NavigationDestination.CertificateForm || CurrentView == NavigationDestination.UserForm || CurrentView == NavigationDestination.SampleReceptionForm)
            {
                if (CurrentView == NavigationDestination.SampleReceptionForm)
                    _navigationService.NavigateTo(NavigationDestination.SampleReceptions);
                else if (CurrentView == NavigationDestination.CertificateForm)
                    _navigationService.NavigateTo(NavigationDestination.Certificates);
                else if (CurrentView == NavigationDestination.UserForm)
                    _navigationService.NavigateTo(NavigationDestination.Users);
                return;
            }
        }

        #endregion

        #region Methods

        private void UpdateDateTime()
        {
            CurrentDateTime = DateTime.Now.ToString("dddd، dd MMMM yyyy - hh:mm tt", new System.Globalization.CultureInfo("ar-LY"));
        }

        private async System.Threading.Tasks.Task ExecuteLogout(object? parameter)
        {
            if (!CheckNavigationSafety()) return;
            await PerformLogoutAsync(true);
        }

        public async System.Threading.Tasks.Task PerformLogoutAsync(bool askConfirmation, bool isAutomatic = false)
        {
            if (askConfirmation)
            {
                bool confirmed = false;
                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                
                ShowConfirmDialog(
                    "تأكيد الخروج",
                    "هل أنت متأكد من تسجيل الخروج والعودة لصفحة الدخول؟",
                    NotificationType.Question,
                    (result) => tcs.SetResult(result),
                    "Logout");

                confirmed = await tcs.Task;
                if (!confirmed) return;
            }

            try
            {
                // Ensure we are off the current call stack to avoid reentrancy issues with Closing events
                await System.Threading.Tasks.Task.Yield();
                
                // Start Logout Sequence
                IsBusy = true;
                
                BusyMessage = "جاري حفظ الإعدادات والنسخ الاحتياطي...";
                await _backupService.AutoBackupAsync();

                _timer?.Stop();
                _sessionTimeoutService.Stop();
                
                try
                {
                    var currentUser = _userService.CurrentUser;
                    if (currentUser != null)
                    {
                        var app = (App)Application.Current;
                        var dbService = app.ServiceProvider.GetService(typeof(DatabaseService)) as DatabaseService;
                        if (dbService != null)
                        {
                            string details = isAutomatic ? "تم تسجيل الخروج تلقائياً بسبب اكتمال وقت الجلسة." : "قام المستخدم بتسجيل الخروج من المنظومة.";
                            await dbService.LogActionAsync(currentUser.Id, currentUser.Username, "تسجيل خروج", details);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Services.LoggerService.LogError("Audit Log Error (Logout)", ex);
                }

                _userService.Logout();
                
                // Re-apply Login Theme independently
                _themeService.ApplyTheme(_themeService.CurrentTheme, _settingsService.Current.LoginIsDarkMode);

                // Reset IsBusy BEFORE window transitions to ensure UI state is clean
                IsBusy = false;

                // If this was an auto-logout (isAutomatic is true), show the Lock Screen first
                Window? nextWindow = null;
                if (isAutomatic)
                {
                    if (Application.Current is App app)
                    {
                        nextWindow = app.ServiceProvider.GetRequiredService<Views.LockScreenWindow>();
                    }
                    else
                    {
                        nextWindow = new Views.LockScreenWindow();
                    }
                }
                else
                {
                    // For manual logout, go directly to LoginWindow
                    if (Application.Current is App app)
                    {
                        nextWindow = app.ServiceProvider.GetRequiredService<Views.LoginWindow>();
                    }
                    else
                    {
                        nextWindow = new Views.LoginWindow(); 
                    }
                }
                nextWindow?.Show();
                
                // Special case for non-automatic logout that still skips confirmation (like closing window)
                // If we went directly to LoginWindow but confirmation was skipped, show the session expired message context manually if needed
                // (Wait, actually for manual closing window, we don't need the "Session Expired" message)
                // So keeping it simple: only isAutomatic shows LockScreen.
                
                // Close all MainWindows and ensure they don't prompt for confirmation
                var openWindows = Application.Current.Windows.Cast<Window>().ToList();
                foreach (var window in openWindows)
                {
                    if (window == nextWindow) continue;

                    try
                    {
                        if (window is Views.MainWindow main)
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
                // Some windows might be already closed or in a state where they can't close
                        Services.LoggerService.LogWarning($"Failed to close a window during logout: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                Services.LoggerService.LogError("Error during logout", ex);
                
                // Invoke on UI Thread to ensure window creation is safe
                Application.Current.Dispatcher.Invoke(() => 
                {
                    var errorWindow = new Views.ErrorDialogWindow("خطأ في النظام", "حدث خطأ أثناء تسجيل الخروج، سيتم إغلاق البرنامج.");
                    errorWindow.ShowDialog();
                });
                
                Application.Current.Shutdown();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ShowGlobalNotification(string title, string message, string? icon = null, string? imagePath = null, NotificationType type = NotificationType.Information)
        {
            IsConfirmMode = false;
            SetNotification(title, message, type, icon, imagePath);
        }

        private void OnRequestConfirmation(string title, string message, NotificationType type, Action<bool> callback, string? icon)
        {
            ShowConfirmDialog(title, message, type, callback, icon);
        }

        public void ShowConfirmDialog(string title, string message, NotificationType type, Action<bool> callback, string? icon = null)
        {
            _confirmCallback = callback;
            IsConfirmMode = true;
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
            // 1. Verify Phrase if required
            if (IsPhraseRequired)
            {
                if (SecurityChallengePhraseInput != _targetPhrase)
                {
                    _notificationService.ShowError("الجملة التأكيدية غير صحيحة. يرجى التأكد من كتابتها بدقة.");
                    return;
                }
            }

            // 2. Verify Password if required
            if (IsPasswordRequired)
            {
                // Note: We use PasswordHelper which is available in Enjaz.Helpers
                if (CurrentUser == null || !PasswordHelper.VerifyPassword(SecurityChallengePasswordInput, CurrentUser.PasswordHash))
                {
                    _notificationService.ShowError("كلمة المرور غير صحيحة.");
                    return;
                }
            }

            // Success
            IsSecurityChallengeOpen = false;
            _challengeCallback?.Invoke(true);
        }
        private bool CheckNavigationSafety()
        {
            if (IsAnyModalOpen)
            {
                ShowGlobalNotification(
                    "نافذة مفتوحة", 
                    "يرجى إغلاق النافذة الحالية أو إنهاء العملية القائمة قبل الانتقال إلى قسم آخر.", 
                    "InformationOutline", 
                    null, 
                    NotificationType.Warning);
                return false;
            }
            return true;
        }
#endregion
    }
}
