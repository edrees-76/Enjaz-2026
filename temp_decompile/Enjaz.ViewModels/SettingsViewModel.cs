using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using Microsoft.Win32;

namespace Enjaz.ViewModels;

public class SettingsViewModel : BaseViewModel
{
	private readonly SettingsService _settingsService;

	private readonly BackupService _backupService;

	private readonly DatabaseService _dbService;

	private readonly INotificationService _notificationService;

	private readonly UserService _userService;

	private string _backupPath;

	private bool _autoBackupEnabled;

	private int _autoBackupIntervalHours;

	private string _selectedFrequency;

	private string _databasePath;

	private string _selectedTab = "Data";

	private bool _isDaily;

	private bool _isWeekly;

	private bool _isMonthly;

	public static readonly string[] Frequencies = new string[3] { "يومي", "أسبوعي", "شهري" };

	private readonly ArchiveService _archiveService;

	private int _archiveMonths = 6;

	public bool EnableAlerts
	{
		get
		{
			return _settingsService.Current.EnableAlerts;
		}
		set
		{
			if (_settingsService.Current.EnableAlerts != value)
			{
				_settingsService.Current.EnableAlerts = value;
				_settingsService.SaveSettings(_settingsService.Current);
				OnPropertyChanged("EnableAlerts");
			}
		}
	}

	public string SelectedTab
	{
		get
		{
			return _selectedTab;
		}
		set
		{
			SetProperty(ref _selectedTab, value, "SelectedTab");
		}
	}

	public string BackupPath
	{
		get
		{
			return _backupPath;
		}
		set
		{
			SetProperty(ref _backupPath, value, "BackupPath");
		}
	}

	public string DatabasePath
	{
		get
		{
			return _databasePath;
		}
		set
		{
			SetProperty(ref _databasePath, value, "DatabasePath");
		}
	}

	public bool AutoBackupEnabled
	{
		get
		{
			return _autoBackupEnabled;
		}
		set
		{
			SetProperty(ref _autoBackupEnabled, value, "AutoBackupEnabled");
		}
	}

	public int AutoBackupIntervalHours
	{
		get
		{
			return _autoBackupIntervalHours;
		}
		set
		{
			if (SetProperty(ref _autoBackupIntervalHours, value, "AutoBackupIntervalHours"))
			{
				UpdateSelectedFrequencyFromHours();
			}
		}
	}

	public string SelectedFrequency
	{
		get
		{
			return _selectedFrequency;
		}
		set
		{
			if (SetProperty(ref _selectedFrequency, value, "SelectedFrequency"))
			{
				UpdateHoursFromSelectedFrequency();
			}
		}
	}

	public string[] AvailableFrequencies => Frequencies;

	public bool IsDaily
	{
		get
		{
			return _isDaily;
		}
		set
		{
			if (SetProperty(ref _isDaily, value, "IsDaily") && value)
			{
				AutoBackupIntervalHours = 24;
			}
		}
	}

	public bool IsWeekly
	{
		get
		{
			return _isWeekly;
		}
		set
		{
			if (SetProperty(ref _isWeekly, value, "IsWeekly") && value)
			{
				AutoBackupIntervalHours = 168;
			}
		}
	}

	public bool IsMonthly
	{
		get
		{
			return _isMonthly;
		}
		set
		{
			if (SetProperty(ref _isMonthly, value, "IsMonthly") && value)
			{
				AutoBackupIntervalHours = 720;
			}
		}
	}

	public bool IsAdmin
	{
		get
		{
			User? currentUser = _userService.CurrentUser;
			return currentUser != null && currentUser.Role == UserRole.Admin;
		}
	}

	public ICommand BrowseFolderCommand { get; }

	public ICommand SaveSettingsCommand { get; }

	public ICommand ManualBackupCommand { get; }

	public ICommand RestoreBackupCommand { get; }

	public ICommand FactoryResetCommand { get; }

	public ICommand ArchiveLogsCommand { get; }

	public ICommand SelectTabCommand { get; }

	public ICommand BrowseDatabaseFolderCommand { get; }

	public int ArchiveMonths
	{
		get
		{
			return _archiveMonths;
		}
		set
		{
			SetProperty(ref _archiveMonths, value, "ArchiveMonths");
		}
	}

	public event Func<Task>? FactoryResetCompleted;

	public event Action<string, string, string, bool, Action<bool>>? RequestSecurityChallenge;

	public SettingsViewModel(SettingsService settingsService, BackupService backupService, DatabaseService dbService, INotificationService notificationService, UserService userService, ArchiveService archiveService)
	{
		_settingsService = settingsService;
		_backupService = backupService;
		_dbService = dbService;
		_notificationService = notificationService;
		_userService = userService;
		_archiveService = archiveService;
		AppSettings current = _settingsService.Current;
		_backupPath = current.BackupPath;
		_databasePath = current.DatabasePath ?? string.Empty;
		_autoBackupEnabled = current.AutoBackupEnabled;
		_autoBackupIntervalHours = current.AutoBackupIntervalHours;
		_selectedFrequency = "يومي";
		UpdateSelectedFrequencyFromHours();
		BrowseFolderCommand = new RelayCommand(delegate
		{
			BrowseFolder();
		});
		SaveSettingsCommand = new RelayCommand(delegate
		{
			SaveSettings();
		});
		ManualBackupCommand = new RelayCommand(async delegate
		{
			await ExecuteManualBackup();
		});
		RestoreBackupCommand = new RelayCommand(delegate
		{
			ExecuteRestoreBackup();
		});
		FactoryResetCommand = new RelayCommand(delegate
		{
			ExecuteFactoryReset();
		}, (object? _) => IsAdmin);
		ArchiveLogsCommand = new RelayCommand(async delegate
		{
			await ExecuteArchiveLogs();
		}, (object? _) => IsAdmin);
		SelectTabCommand = new RelayCommand(delegate(object? tab)
		{
			SelectedTab = tab?.ToString() ?? "Data";
		});
		BrowseDatabaseFolderCommand = new RelayCommand(delegate
		{
			BrowseDatabaseFolder();
		});
		_userService.UserChanged += delegate
		{
			OnPropertyChanged("IsAdmin");
		};
	}

	private Task ExecuteArchiveLogs()
	{
		if (!IsAdmin)
		{
			_notificationService.ShowError("لا تملك صلاحية الوصول لهذه الميزة.");
			return Task.CompletedTask;
		}
		RaiseConfirmation("تأكيد الأرشفة الآمنة", $"هل أنت متأكد من نقل سجلات النشاط الأقدم من {ArchiveMonths} أشهر إلى الأرشيف الآمن؟\nهذا سيساعد في تسريع أداء المنظومة وتخفيف حجمها، مع الحفاظ على البيانات في ملف الأرشيف المنفصل.", NotificationType.Question, async delegate(bool confirmed)
		{
			if (!confirmed)
			{
				return;
			}
			base.IsBusy = true;
			base.BusyMessage = "جاري أرشفة البيانات...";
			try
			{
				int count = await _archiveService.ArchiveOldLogsAsync(ArchiveMonths);
				_notificationService.ShowSuccess($"تمت أرشفة {count} سجل بنجاح.");
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				_notificationService.ShowError("فشل الأرشفة: " + ex2.Message);
				LoggerService.LogError("Log Archiving Failed", ex2);
			}
			finally
			{
				base.IsBusy = false;
			}
		});
		return Task.CompletedTask;
	}

	private void BrowseFolder()
	{
		OpenFolderDialog openFolderDialog = new OpenFolderDialog
		{
			Title = "اختر مجلد الحفظ الاحتياطي",
			InitialDirectory = (Directory.Exists(BackupPath) ? BackupPath : string.Empty)
		};
		if (openFolderDialog.ShowDialog() == true)
		{
			BackupPath = openFolderDialog.FolderName;
		}
	}

	private void BrowseDatabaseFolder()
	{
		OpenFolderDialog openFolderDialog = new OpenFolderDialog
		{
			Title = "اختر مجلد قاعدة البيانات (لربط الشبكة)",
			InitialDirectory = (Directory.Exists(DatabasePath) ? DatabasePath : string.Empty)
		};
		if (openFolderDialog.ShowDialog() == true)
		{
			DatabasePath = openFolderDialog.FolderName;
		}
	}

	private void SaveSettings()
	{
		AppSettings current = _settingsService.Current;
		current.BackupPath = BackupPath;
		current.DatabasePath = (string.IsNullOrWhiteSpace(DatabasePath) ? null : DatabasePath);
		current.AutoBackupEnabled = AutoBackupEnabled;
		current.AutoBackupIntervalHours = AutoBackupIntervalHours;
		current.EnableAlerts = EnableAlerts;
		_settingsService.SaveSettings(current);
		_notificationService.ShowSuccess("تم حفظ الإعدادات بنجاح");
	}

	private async Task ExecuteManualBackup()
	{
		if (string.IsNullOrWhiteSpace(BackupPath))
		{
			_notificationService.ShowError("يرجى تحديد مسار الحفظ أولا\u064b");
			return;
		}
		base.IsBusy = true;
		base.BusyMessage = "جاري إنشاء نسخة احتياطية...";
		try
		{
			string resultPath = await _backupService.PerformBackupAsync(BackupPath);
			if (resultPath != null)
			{
				_notificationService.ShowSuccess("تم إنشاء النسخة الاحتياطية بنجاح في:\n" + resultPath);
				base.StatusMessage = $"آخر نسخة احتياطية: {DateTime.Now:yyyy/MM/dd HH:mm}";
				SaveSettings();
			}
			else
			{
				_notificationService.ShowError("فشل إنشاء النسخة الاحتياطية. تأكد من أن المسار متاح وأن لديك صلاحيات الكتابة.");
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			LoggerService.LogError("Manual backup failed", ex2);
			_notificationService.ShowError("حدث خطأ غير متوقع أثناء النسخ الاحتياطي");
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	private void ExecuteRestoreBackup()
	{
		OpenFileDialog dialog = new OpenFileDialog
		{
			Title = "اختر ملف النسخة الاحتياطية للاستعادة",
			Filter = "ملفات قاعدة البيانات (*.db)|*.db",
			InitialDirectory = BackupPath
		};
		if (dialog.ShowDialog() != true)
		{
			return;
		}
		RaiseConfirmation("تأكيد استعادة البيانات", "تحذير: ستقوم هذه العملية باستبدال جميع البيانات الحالية ببيانات النسخة الاحتياطية المختارة. هل أنت متأكد من الاستمرار؟", NotificationType.Warning, delegate(bool confirmed)
		{
			if (!confirmed)
			{
				return;
			}
			try
			{
				if (_dbService.RestoreDatabase(dialog.FileName))
				{
					_notificationService.ShowSuccess("تم استعادة البيانات بنجاح. يرجى إعادة تشغيل المنظومة لضمان عرض البيانات الجديدة.");
				}
				else
				{
					_notificationService.ShowError("فشل استعادة البيانات. تأكد من أن الملف ليس قيد الاستخدام.");
				}
			}
			catch (Exception ex)
			{
				LoggerService.LogError("Database restoration error", ex);
				_notificationService.ShowError("حدث خطأ أثناء الاستعادة: " + ex.Message);
			}
		});
	}

	private async void ExecuteFactoryReset()
	{
		if (!IsAdmin)
		{
			_notificationService.ShowError("لا تملك صلاحية الوصول لهذه الميزة الحساسة.");
		}
		else
		{
			if (base.IsBusy)
			{
				return;
			}
			try
			{
				base.IsBusy = true;
				base.BusyMessage = "جاري تأمين البيانات (نسخة احتياطية تلقائية)...";
				await _backupService.PerformBackupAsync();
				base.IsBusy = false;
				RaiseConfirmation("تأكيد إعادة ضبط البرنامج", "تحذير هام: أنت على وشك مسح كافة البيانات المسجلة في المنظومة وإعادتها لحالة البرنامج الجديدة.\nهذه العملية تشمل حذف الشهادات، العينات، سجلات النشاط، وحسابات المستخدمين الأخرى.\n\nهل تود الاستمرار؟", NotificationType.Warning, delegate(bool confirmed)
				{
					if (confirmed)
					{
						this.RequestSecurityChallenge?.Invoke("تأكيد الجملة النصية", "يرجى كتابة الجملة التالية لضمان عدم تنفيذ العملية بالخطأ.", "اريد ذلك بالظبط", arg4: false, delegate(bool phraseConfirmed)
						{
							if (phraseConfirmed)
							{
								this.RequestSecurityChallenge?.Invoke("تحقق من الهوية", "الخطوة الأخيرة: يرجى إدخال كلمة المرور الخاصة بك لتنفيذ عملية التصفير.", null, arg4: true, async delegate(bool passwordConfirmed)
								{
									if (passwordConfirmed)
									{
										await PerformActualResetAsync();
									}
								});
							}
						});
					}
				});
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				_notificationService.ShowError("حدث خطأ أثناء بدء عملية الضبط: " + ex2.Message);
			}
			finally
			{
				base.IsBusy = false;
			}
		}
	}

	private async Task PerformActualResetAsync()
	{
		base.IsBusy = true;
		base.BusyMessage = "جاري تنفيذ إعادة ضبط البرنامج الشامل...";
		try
		{
			int currentId = _userService.CurrentUser?.Id ?? 0;
			if (await _dbService.PerformFactoryResetAsync(currentId))
			{
				_notificationService.ShowSuccess("تمت عملية إعادة ضبط البرنامج بنجاح. تم تصفير كافة البيانات، المنظومة الآن خالية باستثناء حسابك الحالي.");
				if (this.FactoryResetCompleted != null)
				{
					await this.FactoryResetCompleted();
				}
			}
			else
			{
				_notificationService.ShowError("فشلت عملية إعادة الضبط.");
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			_notificationService.ShowError("خطأ في قاعدة البيانات: " + ex2.Message);
			LoggerService.LogError("Factory Reset Error", ex2);
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	private void UpdateSelectedFrequencyFromHours()
	{
		if (_autoBackupIntervalHours <= 24)
		{
			_selectedFrequency = "يومي";
		}
		else if (_autoBackupIntervalHours <= 168)
		{
			_selectedFrequency = "أسبوعي";
		}
		else
		{
			_selectedFrequency = "شهري";
		}
		_isDaily = _autoBackupIntervalHours <= 24;
		_isWeekly = _autoBackupIntervalHours > 24 && _autoBackupIntervalHours <= 168;
		_isMonthly = _autoBackupIntervalHours > 168;
		OnPropertyChanged("SelectedFrequency");
		OnPropertyChanged("IsDaily");
		OnPropertyChanged("IsWeekly");
		OnPropertyChanged("IsMonthly");
	}

	private void UpdateHoursFromSelectedFrequency()
	{
		switch (_selectedFrequency)
		{
		case "يومي":
			AutoBackupIntervalHours = 24;
			break;
		case "أسبوعي":
			AutoBackupIntervalHours = 168;
			break;
		case "شهري":
			AutoBackupIntervalHours = 720;
			break;
		}
	}
}
