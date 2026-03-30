using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Services.Repositories;
using MaterialDesignThemes.Wpf;

namespace Enjaz.ViewModels;

public class UsersViewModel : BaseViewModel
{
	private readonly UserRepository _userRepository;

	private readonly UserService _userService;

	private readonly DatabaseService _dbService;

	private readonly INotificationService _notificationService;

	private ObservableCollection<User> _users;

	private ObservableCollection<AuditLog> _userActivities;

	private User? _selectedUser;

	private bool _isEditingUser;

	private string _userUsername = string.Empty;

	private string _userFullName = string.Empty;

	private string _userPassword = string.Empty;

	private UserRole _selectedUserRole;

	private bool _isUserEditor = true;

	private bool _canAccessSampleReceptions;

	private bool _canAccessCertificates;

	private bool _canAccessReports;

	private bool _canAccessSettings;

	private bool _canAccessAdminProcedures;

	private bool _canAccessUsers;

	private bool _isPermissionsSectionVisible;

	private bool _isUserDialogOpen;

	private string _searchText = string.Empty;

	private User? _selectedUserFilter;

	private DateTime? _filterStartDate;

	private DateTime? _filterEndDate;

	private int _totalUsersCount;

	private int _activeUsersCount;

	private int _adminUsersCount;

	private int _activitiesTodayCount;

	private int _modificationsTodayCount;

	private string _mostActiveUser = "N/A";

	public ObservableCollection<UserRole> AvailableRoles { get; } = new ObservableCollection<UserRole>
	{
		UserRole.Admin,
		UserRole.User,
		UserRole.Viewer
	};

	public ISnackbarMessageQueue MessageQueue => _notificationService.MessageQueue;

	public ObservableCollection<User> Users
	{
		get
		{
			return _users;
		}
		set
		{
			SetProperty(ref _users, value, "Users");
		}
	}

	public ObservableCollection<AuditLog> UserActivities
	{
		get
		{
			return _userActivities;
		}
		set
		{
			SetProperty(ref _userActivities, value, "UserActivities");
		}
	}

	public User? SelectedUser
	{
		get
		{
			return _selectedUser;
		}
		set
		{
			SetProperty(ref _selectedUser, value, "SelectedUser");
			CommandManager.InvalidateRequerySuggested();
		}
	}

	public bool IsEditingUser
	{
		get
		{
			return _isEditingUser;
		}
		set
		{
			SetProperty(ref _isEditingUser, value, "IsEditingUser");
		}
	}

	public string UserUsername
	{
		get
		{
			return _userUsername;
		}
		set
		{
			if (SetProperty(ref _userUsername, value, "UserUsername"))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	public string UserFullName
	{
		get
		{
			return _userFullName;
		}
		set
		{
			if (SetProperty(ref _userFullName, value, "UserFullName"))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	public string UserPassword
	{
		get
		{
			return _userPassword;
		}
		set
		{
			if (SetProperty(ref _userPassword, value, "UserPassword"))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	public UserRole SelectedUserRole
	{
		get
		{
			return _selectedUserRole;
		}
		set
		{
			SetProperty(ref _selectedUserRole, value, "SelectedUserRole");
			UpdatePermissionsVisibility();
		}
	}

	public bool IsUserEditor
	{
		get
		{
			return _isUserEditor;
		}
		set
		{
			SetProperty(ref _isUserEditor, value, "IsUserEditor");
		}
	}

	public bool CanAccessSampleReceptions
	{
		get
		{
			return _canAccessSampleReceptions;
		}
		set
		{
			SetProperty(ref _canAccessSampleReceptions, value, "CanAccessSampleReceptions");
		}
	}

	public bool CanAccessCertificates
	{
		get
		{
			return _canAccessCertificates;
		}
		set
		{
			SetProperty(ref _canAccessCertificates, value, "CanAccessCertificates");
		}
	}

	public bool CanAccessReports
	{
		get
		{
			return _canAccessReports;
		}
		set
		{
			SetProperty(ref _canAccessReports, value, "CanAccessReports");
		}
	}

	public bool CanAccessSettings
	{
		get
		{
			return _canAccessSettings;
		}
		set
		{
			SetProperty(ref _canAccessSettings, value, "CanAccessSettings");
		}
	}

	public bool CanAccessAdminProcedures
	{
		get
		{
			return _canAccessAdminProcedures;
		}
		set
		{
			SetProperty(ref _canAccessAdminProcedures, value, "CanAccessAdminProcedures");
		}
	}

	public bool CanAccessUsers
	{
		get
		{
			return _canAccessUsers;
		}
		set
		{
			SetProperty(ref _canAccessUsers, value, "CanAccessUsers");
		}
	}

	public bool IsPermissionsSectionVisible
	{
		get
		{
			return _isPermissionsSectionVisible;
		}
		set
		{
			SetProperty(ref _isPermissionsSectionVisible, value, "IsPermissionsSectionVisible");
		}
	}

	public bool IsUserDialogOpen
	{
		get
		{
			return _isUserDialogOpen;
		}
		set
		{
			SetProperty(ref _isUserDialogOpen, value, "IsUserDialogOpen");
		}
	}

	public string SearchText
	{
		get
		{
			return _searchText;
		}
		set
		{
			if (SetProperty(ref _searchText, value, "SearchText"))
			{
				ApplySearchFilter();
			}
		}
	}

	public bool CanManageUsers => _userService.CurrentUser?.CanManageUsers ?? false;

	public User? SelectedUserFilter
	{
		get
		{
			return _selectedUserFilter;
		}
		set
		{
			if (SetProperty(ref _selectedUserFilter, value, "SelectedUserFilter"))
			{
				LoadActivitiesAsync();
			}
		}
	}

	public DateTime? FilterStartDate
	{
		get
		{
			return _filterStartDate;
		}
		set
		{
			if (SetProperty(ref _filterStartDate, value, "FilterStartDate"))
			{
				LoadActivitiesAsync();
			}
		}
	}

	public DateTime? FilterEndDate
	{
		get
		{
			return _filterEndDate;
		}
		set
		{
			if (SetProperty(ref _filterEndDate, value, "FilterEndDate"))
			{
				LoadActivitiesAsync();
			}
		}
	}

	public int TotalUsersCount
	{
		get
		{
			return _totalUsersCount;
		}
		set
		{
			SetProperty(ref _totalUsersCount, value, "TotalUsersCount");
		}
	}

	public int ActiveUsersCount
	{
		get
		{
			return _activeUsersCount;
		}
		set
		{
			SetProperty(ref _activeUsersCount, value, "ActiveUsersCount");
		}
	}

	public int AdminUsersCount
	{
		get
		{
			return _adminUsersCount;
		}
		set
		{
			SetProperty(ref _adminUsersCount, value, "AdminUsersCount");
		}
	}

	public int ActivitiesTodayCount
	{
		get
		{
			return _activitiesTodayCount;
		}
		set
		{
			SetProperty(ref _activitiesTodayCount, value, "ActivitiesTodayCount");
		}
	}

	public int ModificationsTodayCount
	{
		get
		{
			return _modificationsTodayCount;
		}
		set
		{
			SetProperty(ref _modificationsTodayCount, value, "ModificationsTodayCount");
		}
	}

	public string MostActiveUser
	{
		get
		{
			return _mostActiveUser;
		}
		set
		{
			SetProperty(ref _mostActiveUser, value, "MostActiveUser");
		}
	}

	public ICommand AddUserCommand { get; private set; } = null;

	public ICommand EditUserCommand { get; private set; } = null;

	public ICommand SaveUserCommand { get; private set; } = null;

	public ICommand CancelUserEditCommand { get; private set; } = null;

	public ICommand ToggleUserFreezeCommand { get; private set; } = null;

	public ICommand LoadActivitiesCommand { get; private set; } = null;

	public ICommand ClearFiltersCommand { get; private set; } = null;

	public event Action<NavigationDestination>? RequestNavigation;

	public UsersViewModel(UserRepository userRepository, UserService userService, DatabaseService dbService, INotificationService notificationService)
	{
		_userRepository = userRepository;
		_userService = userService;
		_dbService = dbService;
		_notificationService = notificationService;
		_users = new ObservableCollection<User>();
		_userActivities = new ObservableCollection<AuditLog>();
		InitializeCommands();
		_filterStartDate = DateTime.Today.AddDays(-30.0);
		_filterEndDate = DateTime.Today;
		OnPropertyChanged("FilterStartDate");
		OnPropertyChanged("FilterEndDate");
	}

	private void InitializeCommands()
	{
		AddUserCommand = new RelayCommand(delegate
		{
			StartAddUser();
		}, (object? _) => CanManageUsers);
		EditUserCommand = new RelayCommand(delegate(object? param)
		{
			StartEditUser(param as User);
		}, (object? param) => CanEditUser(param as User));
		SaveUserCommand = new RelayCommand(async delegate
		{
			await SaveUserAsync();
		}, (object? _) => CanSaveUser());
		CancelUserEditCommand = new RelayCommand(delegate
		{
			CancelUserEdit();
		});
		ToggleUserFreezeCommand = new RelayCommand(async delegate(object? param)
		{
			await ToggleUserFreezeAsync(param as User);
		}, (object? param) => CanFreezeUser(param as User));
		LoadActivitiesCommand = new RelayCommand(async delegate
		{
			await LoadActivitiesAsync();
		}, (object? _) => CanManageUsers);
		ClearFiltersCommand = new RelayCommand(async delegate
		{
			await ClearFiltersAsync();
		}, (object? _) => CanManageUsers);
	}

	public async Task LoadUsersAsync()
	{
		if (!CanManageUsers)
		{
			return;
		}
		try
		{
			base.IsBusy = true;
			base.BusyMessage = "جاري تحميل قائمة المستخدمين...";
			Users = new ObservableCollection<User>(await _userRepository.GetAllUsersAsync());
			ApplySearchFilter();
			TotalUsersCount = Users.Count;
			ActiveUsersCount = Users.Count((User u) => u.IsActive);
			AdminUsersCount = Users.Count((User u) => u.Role == UserRole.Admin);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			base.NotificationTitle = "خطأ تحميل";
			base.NotificationMessage = "تعذر تحميل قائمة المستخدمين من قاعدة البيانات: " + ex2.Message;
			base.NotificationIcon = "AlertCircleOutline";
			base.IsNotificationDialogOpen = true;
			base.StatusMessage = "خطأ في تحميل المستخدمين: " + ex2.Message;
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	public async Task LoadActivitiesAsync()
	{
		if (!CanManageUsers)
		{
			return;
		}
		try
		{
			base.IsBusy = true;
			base.BusyMessage = "جاري تحميل سجل النشاطات...";
			UserActivities = new ObservableCollection<AuditLog>(await _dbService.GetAuditLogsAsync(SelectedUserFilter?.Id, FilterStartDate, FilterEndDate));
			DateTime today = DateTime.Today;
			ActivitiesTodayCount = UserActivities.Count((AuditLog log) => log.Timestamp.Date == today);
			ModificationsTodayCount = UserActivities.Count((AuditLog log) => log.Timestamp.Date == today && (log.Action.Contains("حذف") || log.Action.Contains("تعديل") || log.Action.Contains("Delete") || log.Action.Contains("Edit")));
			MostActiveUser = (from log in UserActivities
				group log by log.UserName into g
				orderby g.Count() descending
				select g).FirstOrDefault()?.Key ?? "N/A";
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			base.StatusMessage = "خطأ في تحميل السجل: " + ex2.Message;
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	private void StartAddUser()
	{
		ClearUserForm();
		SelectedUser = null;
		IsEditingUser = false;
		IsUserDialogOpen = true;
	}

	private void StartEditUser(User? user)
	{
		User user2 = user ?? SelectedUser;
		if (user2 != null)
		{
			SelectedUser = user2;
			LoadUserForEdit(user2);
			IsEditingUser = true;
			IsUserDialogOpen = true;
		}
	}

	private bool CanEditUser(User? user)
	{
		User user2 = user ?? SelectedUser;
		return user2 != null && CanManageUsers;
	}

	private void LoadUserForEdit(User user)
	{
		UserUsername = user.Username;
		UserFullName = user.FullName;
		UserPassword = "";
		SelectedUserRole = user.Role;
		IsUserEditor = user.IsEditor;
		HashSet<string> hashSet = (user.Permissions ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
		CanAccessSampleReceptions = hashSet.Contains("SampleReceptions");
		CanAccessCertificates = hashSet.Contains("Certificates");
		CanAccessReports = hashSet.Contains("Reports");
		CanAccessSettings = hashSet.Contains("Settings");
		CanAccessAdminProcedures = hashSet.Contains("AdminProcedures");
		CanAccessUsers = hashSet.Contains("Users");
		UpdatePermissionsVisibility();
	}

	private void ClearUserForm()
	{
		UserUsername = string.Empty;
		UserFullName = string.Empty;
		UserPassword = string.Empty;
		SelectedUserRole = UserRole.User;
		IsUserEditor = true;
		CanAccessSampleReceptions = true;
		CanAccessCertificates = true;
		CanAccessReports = false;
		CanAccessSettings = false;
		CanAccessAdminProcedures = false;
		CanAccessUsers = false;
		UpdatePermissionsVisibility();
	}

	private void UpdatePermissionsVisibility()
	{
		IsPermissionsSectionVisible = SelectedUserRole != UserRole.Admin;
	}

	private bool CanSaveUser()
	{
		if (SelectedUser == null)
		{
			return !string.IsNullOrWhiteSpace(UserUsername) && !string.IsNullOrWhiteSpace(UserFullName) && !string.IsNullOrWhiteSpace(UserPassword);
		}
		return !string.IsNullOrWhiteSpace(UserFullName);
	}

	private async Task SaveUserAsync()
	{
		try
		{
			base.IsBusy = true;
			base.BusyMessage = "جاري حفظ بيانات المستخدم...";
			if (SelectedUser == null)
			{
				User newUser = new User
				{
					Username = UserUsername,
					FullName = UserFullName,
					Role = SelectedUserRole,
					IsActive = true,
					IsEditor = IsUserEditor,
					Permissions = PackPermissions()
				};
				if (!(await _userRepository.IsUsernameUniqueAsync(UserUsername)))
				{
					base.IsBusy = false;
					_notificationService.ShowError("اسم المستخدم موجود بالفعل. يرجى اختيار اسم آخر.");
					base.StatusMessage = "خطأ: اسم المستخدم مكرر";
					return;
				}
				await _userRepository.AddUserAsync(newUser, UserPassword);
				base.IsBusy = false;
				SetNotification("تم الإضافة", "تم إنشاء حساب جديد للمستخدم (" + UserFullName + ") بنجاح.", NotificationType.Success);
				base.StatusMessage = "تم إضافة المستخدم بنجاح";
				await LoadUsersAsync();
				CancelUserEdit();
			}
			else
			{
				SelectedUser.FullName = UserFullName;
				SelectedUser.Role = SelectedUserRole;
				SelectedUser.IsEditor = IsUserEditor;
				SelectedUser.Permissions = PackPermissions();
				string newPass = (string.IsNullOrWhiteSpace(UserPassword) ? null : UserPassword);
				if (!(await _userRepository.IsUsernameUniqueAsync(SelectedUser.Username, SelectedUser.Id)))
				{
					base.IsBusy = false;
					_notificationService.ShowError("اسم المستخدم هذا مستخدم من قبل حساب آخر.");
					base.StatusMessage = "خطأ: اسم المستخدم مكرر";
					return;
				}
				await _userRepository.UpdateUserAsync(SelectedUser, newPass);
				base.IsBusy = false;
				SetNotification("تم التحديث", "تم تحديث بيانات المستخدم (" + UserFullName + ") وحفظ التعديلات بنجاح.", NotificationType.Success);
				base.StatusMessage = "تم تحديث بيانات المستخدم بنجاح";
				await LoadUsersAsync();
				CancelUserEdit();
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			base.IsBusy = false;
			SetNotification("فشل العملية", "حدث خطأ أثناء محاولة حفظ البيانات: " + ex2.Message, NotificationType.Error);
			base.StatusMessage = "خطأ: " + ex2.Message;
		}
	}

	private void CancelUserEdit()
	{
		IsUserDialogOpen = false;
		ClearUserForm();
		SelectedUser = null;
	}

	private bool CanFreezeUser(User? user)
	{
		User user2 = user ?? SelectedUser;
		return user2 != null && user2.Id != _userService.CurrentUser?.Id && CanManageUsers;
	}

	private string PackPermissions()
	{
		if (SelectedUserRole == UserRole.Admin)
		{
			return "All";
		}
		List<string> list = new List<string>();
		if (CanAccessSampleReceptions)
		{
			list.Add("SampleReceptions");
		}
		if (CanAccessCertificates)
		{
			list.Add("Certificates");
		}
		if (CanAccessReports)
		{
			list.Add("Reports");
		}
		if (CanAccessSettings)
		{
			list.Add("Settings");
		}
		if (CanAccessAdminProcedures)
		{
			list.Add("AdminProcedures");
		}
		if (CanAccessUsers)
		{
			list.Add("Users");
		}
		return string.Join(",", list);
	}

	private Task ToggleUserFreezeAsync(User? user)
	{
		User target = user ?? SelectedUser;
		if (target == null)
		{
			return Task.CompletedTask;
		}
		SelectedUser = target;
		string action = (target.IsActive ? "تجميد" : "تنشيط");
		string title = (target.IsActive ? "تأكيد التجميد" : "تأكيد التنشيط");
		string message = ((target.Role == UserRole.Admin && target.IsActive) ? ("تنبيه: أنت على وشك تجميد حساب مدير (" + target.FullName + "). هل أنت متأكد من الاستمرار؟") : $"هل أنت متأكد من {action} حساب المستخدم: {target.FullName}؟");
		NotificationType type = ((target.Role == UserRole.Admin && target.IsActive) ? NotificationType.Warning : NotificationType.Question);
		RaiseConfirmation(title, message, type, async delegate(bool confirmed)
		{
			if (confirmed)
			{
				bool freeze = target.IsActive;
				try
				{
					base.IsBusy = true;
					base.BusyMessage = "جاري " + action + " الحساب...";
					await _userRepository.ToggleUserFreezeAsync(target.Id, freeze);
					base.IsBusy = false;
					SetNotification("تغيير الحالة", $"تم بنجاح {action} حساب المستخدم: {target.FullName}.", NotificationType.Success);
					base.IsBusy = true;
					base.BusyMessage = "جاري " + action + " الحساب...";
					await _userRepository.ToggleUserFreezeAsync(target.Id, freeze);
					base.IsBusy = false;
					SetNotification("تغيير الحالة", $"تم بنجاح {action} حساب المستخدم: {target.FullName}.", NotificationType.Success);
					base.StatusMessage = "تم " + action + " الحساب بنجاح";
					await LoadUsersAsync();
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					LoggerService.LogError("Error toggling user freeze", ex2);
					base.IsBusy = false;
					SetNotification("فشل الإجراء", "تعذر " + action + " الحساب حاليا\u064b. يرجى المحاولة مرة أخرى.", NotificationType.Error);
					base.StatusMessage = "فشل في " + action + " الحساب";
				}
			}
		});
		return Task.CompletedTask;
	}

	public async Task ClearFiltersAsync()
	{
		SelectedUserFilter = null;
		FilterStartDate = null;
		FilterEndDate = null;
		await LoadActivitiesAsync();
	}

	private void ApplySearchFilter()
	{
		ICollectionView defaultView = CollectionViewSource.GetDefaultView(Users);
		if (defaultView == null)
		{
			return;
		}
		defaultView.Filter = delegate(object item)
		{
			if (string.IsNullOrWhiteSpace(SearchText))
			{
				return true;
			}
			if (item is User user)
			{
				string username = user.Username;
				return (username != null && username.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) || (user.FullName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);
			}
			return false;
		};
		defaultView.Refresh();
	}
}
