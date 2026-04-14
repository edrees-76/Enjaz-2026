using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Services;
using System.Threading.Tasks;
using System.IO;

namespace Enjaz.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly SettingsService _settingsService;
        private readonly BackupService _backupService;
        private readonly DatabaseService _dbService;
        private readonly INotificationService _notificationService;
        private readonly UserService _userService;


        private string _backupPath;
        private string _cloudSyncPath;
        private bool _autoBackupEnabled;
        private int _autoBackupIntervalHours;
        private string _selectedFrequency;
        private string _databasePath;
        
        private string _selectedTab = "Data"; // Default to Data tab
        
        public event Func<System.Threading.Tasks.Task>? FactoryResetCompleted;
        public event Action<string, string, string, bool, Action<bool>>? RequestSecurityChallenge;
        
        private bool _isDaily;
        private bool _isWeekly;
        private bool _isMonthly;


        public bool EnableAlerts
        {
            get => _settingsService.Current.EnableAlerts;
            set
            {
                if (_settingsService.Current.EnableAlerts != value)
                {
                    _settingsService.Current.EnableAlerts = value;
                    _settingsService.SaveSettings(_settingsService.Current);
                    OnPropertyChanged(nameof(EnableAlerts));
                }
            }
        }

        public string SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }

        public static readonly string[] Frequencies = { "يومي", "أسبوعي", "شهري" };

        public string BackupPath
        {
            get => _backupPath;
            set => SetProperty(ref _backupPath, value);
        }

        public string CloudSyncPath
        {
            get => _cloudSyncPath;
            set => SetProperty(ref _cloudSyncPath, value);
        }

        public string DatabasePath
        {
            get => _databasePath;
            set => SetProperty(ref _databasePath, value);
        }

        public bool AutoBackupEnabled
        {
            get => _autoBackupEnabled;
            set => SetProperty(ref _autoBackupEnabled, value);
        }

        public int AutoBackupIntervalHours
        {
            get => _autoBackupIntervalHours;
            set 
            {
                if (SetProperty(ref _autoBackupIntervalHours, value))
                {
                    UpdateSelectedFrequencyFromHours();
                }
            }
        }

        public string SelectedFrequency
        {
            get => _selectedFrequency;
            set
            {
                if (SetProperty(ref _selectedFrequency, value))
                {
                    UpdateHoursFromSelectedFrequency();
                }
            }
        }

        public string[] AvailableFrequencies => Frequencies;

        public bool IsDaily
        {
            get => _isDaily;
            set
            {
                if (SetProperty(ref _isDaily, value) && value)
                {
                    AutoBackupIntervalHours = 24;
                }
            }
        }

        public bool IsWeekly
        {
            get => _isWeekly;
            set
            {
                if (SetProperty(ref _isWeekly, value) && value)
                {
                    AutoBackupIntervalHours = 168;
                }
            }
        }

        public bool IsMonthly
        {
            get => _isMonthly;
            set
            {
                if (SetProperty(ref _isMonthly, value) && value)
                {
                    AutoBackupIntervalHours = 720;
                }
            }
        }
        
        public bool IsAdmin => _userService.CurrentUser?.Role == Models.UserRole.Admin;

        public ICommand BrowseFolderCommand { get; }
        public ICommand BrowseCloudFolderCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand ManualBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }
        public ICommand FactoryResetCommand { get; }
        public ICommand ArchiveLogsCommand { get; }
        public ICommand SelectTabCommand { get; }
        public ICommand BrowseDatabaseFolderCommand { get; }
        public ICommand UpgradeAtqaanDbCommand { get; }

        private readonly ArchiveService _archiveService;
        private readonly IDialogService _dialogService;
        private int _archiveMonths = 6;

        public int ArchiveMonths
        {
            get => _archiveMonths;
            set => SetProperty(ref _archiveMonths, value);
        }

        public SettingsViewModel(SettingsService settingsService, BackupService backupService, DatabaseService dbService, INotificationService notificationService, UserService userService, ArchiveService archiveService, IDialogService dialogService)
        {
            _settingsService = settingsService;
            _backupService = backupService;
            _dbService = dbService;
            _notificationService = notificationService;
            _userService = userService;
            _archiveService = archiveService;
            _dialogService = dialogService;

            var settings = _settingsService.Current;
            _backupPath = settings.BackupPath;
            _cloudSyncPath = settings.CloudSyncPath;
            _databasePath = settings.DatabasePath ?? string.Empty;
            _autoBackupEnabled = settings.AutoBackupEnabled;
            _autoBackupIntervalHours = settings.AutoBackupIntervalHours;
            _selectedFrequency = "يومي"; // Default
            

            UpdateSelectedFrequencyFromHours();

            BrowseFolderCommand = new RelayCommand(_ => BrowseFolder());
            BrowseCloudFolderCommand = new RelayCommand(_ => BrowseCloudFolder());
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
            ManualBackupCommand = new RelayCommand(async _ => await ExecuteManualBackup());
            RestoreBackupCommand = new RelayCommand(_ => ExecuteRestoreBackup());
            FactoryResetCommand = new RelayCommand(_ => ExecuteFactoryReset(), _ => IsAdmin);
            ArchiveLogsCommand = new RelayCommand(async _ => await ExecuteArchiveLogs(), _ => IsAdmin);
            SelectTabCommand = new RelayCommand(tab => SelectedTab = tab?.ToString() ?? "Data");
            BrowseDatabaseFolderCommand = new RelayCommand(_ => BrowseDatabaseFolder());
            UpgradeAtqaanDbCommand = new RelayCommand(_ => ExecuteUpgradeAtqaanDb());

            // الاشتراك في تغيير المستخدم لتحديث الصلاحيات
            _userService.UserChanged += (u) => OnPropertyChanged(nameof(IsAdmin));
        }

        private Task ExecuteArchiveLogs()
        {
            if (!IsAdmin)
            {
                _notificationService.ShowError("لا تملك صلاحية الوصول لهذه الميزة.");
                return Task.CompletedTask;
            }

            RaiseConfirmation(
                "تأكيد الأرشفة الآمنة",
                $"هل أنت متأكد من نقل سجلات النشاط الأقدم من {ArchiveMonths} أشهر إلى الأرشيف الآمن؟\nهذا سيساعد في تسريع أداء المنظومة وتخفيف حجمها، مع الحفاظ على البيانات في ملف الأرشيف المنفصل.",
                NotificationType.Question,
                async (confirmed) => 
                {
                    if (!confirmed) return;

                    IsBusy = true;
                    BusyMessage = "جاري أرشفة البيانات...";

                    try
                    {
                        int count = await _archiveService.ArchiveOldLogsAsync(ArchiveMonths);
                        _notificationService.ShowSuccess($"تمت أرشفة {count} سجل بنجاح.");
                    }
                    catch (Exception ex)
                    {
                        _notificationService.ShowError($"فشل الأرشفة: {ex.Message}");
                        LoggerService.LogError("Log Archiving Failed", ex);
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                });
            return Task.CompletedTask;
        }


        private void BrowseFolder()
        {
            string? selectedFolder = _dialogService.ShowFolderBrowserDialog();
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                BackupPath = selectedFolder;
            }
        }

        private void BrowseCloudFolder()
        {
            string? selectedFolder = _dialogService.ShowFolderBrowserDialog();
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                CloudSyncPath = selectedFolder;
            }
        }

        private void BrowseDatabaseFolder()
        {
            string? selectedFolder = _dialogService.ShowFolderBrowserDialog();
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                DatabasePath = selectedFolder;
            }
        }

        private void SaveSettings()
        {
            var settings = _settingsService.Current;
            settings.BackupPath = BackupPath;
            settings.CloudSyncPath = CloudSyncPath;
            settings.DatabasePath = string.IsNullOrWhiteSpace(DatabasePath) ? null : DatabasePath;
            settings.AutoBackupEnabled = AutoBackupEnabled;
            settings.AutoBackupIntervalHours = AutoBackupIntervalHours;
            
            settings.EnableAlerts = EnableAlerts;

            _settingsService.SaveSettings(settings);
            _notificationService.ShowSuccess("تم حفظ الإعدادات بنجاح");
        }

        private async Task ExecuteManualBackup()
        {
            if (string.IsNullOrWhiteSpace(BackupPath))
            {
                _notificationService.ShowError("يرجى تحديد مسار الحفظ أولاً");
                return;
            }

            IsBusy = true;
            BusyMessage = "جاري إنشاء نسخة احتياطية...";
            
            try 
            {
                // نمرر المسار الموجود في الواجهة حالياً لضمان الحفظ في المكان الصحيح
                string? resultPath = await _backupService.PerformBackupAsync(BackupPath);
                
                if (resultPath != null)
                {
                    _notificationService.ShowSuccess($"تم إنشاء النسخة الاحتياطية بنجاح في:\n{resultPath}");
                    StatusMessage = $"آخر نسخة احتياطية: {DateTime.Now:yyyy/MM/dd HH:mm}";
                    
                    // تحديث المسار في الإعدادات المحفوظة أيضاً لضمان الاستمرارية
                    SaveSettings();
                }
                else
                {
                    _notificationService.ShowError("فشل إنشاء النسخة الاحتياطية. تأكد من أن المسار متاح وأن لديك صلاحيات الكتابة.");
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Manual backup failed", ex);
                _notificationService.ShowError("حدث خطأ غير متوقع أثناء النسخ الاحتياطي");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteUpgradeAtqaanDb()
        {
            string? selectedFile = _dialogService.ShowOpenFileDialog("ملفات قاعدة البيانات (*.db)|*.db");
            if (!string.IsNullOrEmpty(selectedFile))
            {
                
                RaiseConfirmation(
                    "تأكيد ترقية قاعدة البيانات",
                    "سيقوم البرنامج الآن بترقية هذا الملف وإضافة الأقسام الجديدة إليه (مثل استلام العينات) مع الحفاظ على بياناتك القديمة.\nهل تود الاستمرار؟",
                    NotificationType.Question,
                    async (confirmed) => 
                    {
                        if (!confirmed) return;

                        IsBusy = true;
                        BusyMessage = "جاري ترقية مخرجات اتقان...";

                        try
                        {
                            bool success = await _dbService.UpgradeAtqaanDatabaseAsync(selectedFile);
                            if (success)
                            {
                                RaiseConfirmation(
                                    "تمت الترقية بنجاح",
                                    "تمت ترقية الملف بنجاح وتوليد سجلات الاستلام المفقودة.\nهل تود تفعيل هذا الملف الآن كقاعدة بيانات أساسية للمنظومة؟ (سيتم أخذ نسخة احتياطية من ملفك الحالي أولاً)",
                                    NotificationType.Success,
                                    async (activate) =>
                                    {
                                        if (activate)
                                        {
                                            await _backupService.PerformBackupAsync(); // Backup current
                                            if (_dbService.RestoreDatabase(selectedFile))
                                            {
                                                _notificationService.ShowSuccess("تم تفعيل البيانات المرقّاة بنجاح. يرجى إعادة تشغيل المنظومة.");
                                            }
                                        }
                                        else
                                        {
                                            _notificationService.ShowInfo("تمت الترقية بنجاح. يمكنك العثور على الملف المرقّى في مساره الأصلي.");
                                        }
                                    });
                            }
                            else
                            {
                                _notificationService.ShowError("فشلت عملية الترقية. يرجى التأكد من أن الملف ليس قيد الاستخدام.");
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggerService.LogError("Atqaan Migration ViewModel Error", ex);
                            _notificationService.ShowError($"حدث خطأ أثناء الترقية: {ex.Message}");
                        }
                        finally
                        {
                            IsBusy = false;
                        }
                    });
            }
        }

        private void ExecuteRestoreBackup()
        {
            string? selectedFile = _dialogService.ShowOpenFileDialog("ملفات قاعدة البيانات المشفّرة والتقليدية (*.edb;*.db)|*.edb;*.db|جميع الملفات (*.*)|*.*");

            if (!string.IsNullOrEmpty(selectedFile))
            {
                // رسالة تحذير قبل الاستعادة
                RaiseConfirmation(
                    "تأكيد استعادة البيانات",
                    "تحذير: ستقوم هذه العملية باستبدال جميع البيانات الحالية ببيانات النسخة الاحتياطية المختارة. هل أنت متأكد من الاستمرار؟",
                    NotificationType.Warning,
                    async (confirmed) => 
                    {
                        if (!confirmed) return;
                        
                        try
                        {
                            bool restoreSuccess = false;
                            
                            if (Path.GetExtension(selectedFile).Equals(".edb", StringComparison.OrdinalIgnoreCase))
                            {
                                string targetDbPath = _dbService.DbFilePath;
                                restoreSuccess = await _backupService.RestoreEncryptedBackupAsync(selectedFile, targetDbPath);
                            }
                            else
                            {
                                restoreSuccess = _dbService.RestoreDatabase(selectedFile);
                            }

                            if (restoreSuccess)
                            {
                                _notificationService.ShowSuccess("تم استعادة البيانات بنجاح. يرجى إعادة تشغيل المنظومة لضمان عرض البيانات الجديدة.");
                            }
                            else
                            {
                                _notificationService.ShowError("فشل استعادة البيانات. تأكد من صحة الملف وكلمة المرور المشفرة إن وجدت.");
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggerService.LogError("Database restoration error", ex);
                            _notificationService.ShowError($"حدث خطأ أثناء الاستعادة: {ex.Message}");
                        }
                    });
            }
        }

        private async void ExecuteFactoryReset()
        {
            if (!IsAdmin)
            {
                _notificationService.ShowError("لا تملك صلاحية الوصول لهذه الميزة الحساسة.");
                return;
            }

            if (IsBusy) return; // Spam protection

            try 
            {
                // 1. النسخ الاحتياطي التلقائي أولاً (بدون تدخل المستخدم)
                IsBusy = true;
                BusyMessage = "جاري تأمين البيانات (نسخة احتياطية تلقائية)...";
                await _backupService.PerformBackupAsync();
                IsBusy = false;

                // 2. نافذة التأكيد الأولى
                RaiseConfirmation(
                    "تأكيد إعادة ضبط البرنامج",
                    "تحذير هام: أنت على وشك مسح كافة البيانات المسجلة في المنظومة وإعادتها لحالة البرنامج الجديدة.\nهذه العملية تشمل حذف الشهادات، العينات، سجلات النشاط، وحسابات المستخدمين الأخرى.\n\nهل تود الاستمرار؟",
                    NotificationType.Warning,
                    (confirmed) => 
                    {
                        if (!confirmed) return;

                        // 3. المرحلة الثانية: طلب الجملة النصية فقط
                        RequestSecurityChallenge?.Invoke(
                            "تأكيد الجملة النصية",
                            "يرجى كتابة الجملة التالية لضمان عدم تنفيذ العملية بالخطأ.",
                            "اريد ذلك بالظبط",
                            false, // كلمة المرور مطلوبة في الخطوة التالية
                            (phraseConfirmed) => 
                            {
                                if (phraseConfirmed)
                                {
                                    // 4. المرحلة الثالثة: طلب كلمة المرور فقط
                                    RequestSecurityChallenge?.Invoke(
                                        "تحقق من الهوية",
                                        "الخطوة الأخيرة: يرجى إدخال كلمة المرور الخاصة بك لتنفيذ عملية التصفير.",
                                        null!, // الجملة تم تأكيدها بالفعل
                                        true, // طلب كلمة المرور
                                        async (passwordConfirmed) => 
                                        {
                                            if (passwordConfirmed)
                                            {
                                                await PerformActualResetAsync();
                                            }
                                        });
                                }
                            });
                    });
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"حدث خطأ أثناء بدء عملية الضبط: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async System.Threading.Tasks.Task PerformActualResetAsync()
        {
            IsBusy = true;
            BusyMessage = "جاري تنفيذ إعادة ضبط البرنامج الشامل...";
            try 
            {
                int currentId = _userService.CurrentUser?.Id ?? 0;
                if (await _dbService.PerformFactoryResetAsync(currentId))
                {
                    _notificationService.ShowSuccess("تمت عملية إعادة ضبط البرنامج بنجاح. تم تصفير كافة البيانات، المنظومة الآن خالية باستثناء حسابك الحالي.");

                    // إطلاق الحدث لتنبيه الواجهات الأخرى (مثل لوحة التحكم) لتحديث بياناتها
                    if (FactoryResetCompleted != null)
                    {
                        await FactoryResetCompleted.Invoke();
                    }
                }
                else
                {
                    _notificationService.ShowError("فشلت عملية إعادة الضبط.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"خطأ في قاعدة البيانات: {ex.Message}");
                Services.LoggerService.LogError("Factory Reset Error", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
        private void UpdateSelectedFrequencyFromHours()
        {
            if (_autoBackupIntervalHours <= 24) _selectedFrequency = "يومي";
            else if (_autoBackupIntervalHours <= 168) _selectedFrequency = "أسبوعي";
            else _selectedFrequency = "شهري";
            
            _isDaily = _autoBackupIntervalHours <= 24;
            _isWeekly = _autoBackupIntervalHours > 24 && _autoBackupIntervalHours <= 168;
            _isMonthly = _autoBackupIntervalHours > 168;

            OnPropertyChanged(nameof(SelectedFrequency));
            OnPropertyChanged(nameof(IsDaily));
            OnPropertyChanged(nameof(IsWeekly));
            OnPropertyChanged(nameof(IsMonthly));
        }

        private void UpdateHoursFromSelectedFrequency()
        {
            switch (_selectedFrequency)
            {
                case "يومي": AutoBackupIntervalHours = 24; break;
                case "أسبوعي": AutoBackupIntervalHours = 168; break;
                case "شهري": AutoBackupIntervalHours = 720; break;
            }
        }
    }
}
