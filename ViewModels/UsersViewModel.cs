using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;

namespace Enjaz.ViewModels
{
    public class UsersViewModel : BaseViewModel
    {
        private readonly Services.Repositories.UserRepository _userRepository;
        private readonly UserService _userService;
        private readonly DatabaseService _dbService;
        private readonly INotificationService _notificationService;

#pragma warning disable CS0067 // Event is subscribed from MainViewModel
        public event Action<NavigationDestination>? RequestNavigation;
#pragma warning restore CS0067
        
        private ObservableCollection<User> _users;
        private ObservableCollection<AuditLog> _userActivities;
        private User? _selectedUser;
        private bool _isEditingUser;

        // User Form Properties
        private string _userUsername = string.Empty;
        private string _userFullName = string.Empty;
        private string _userPassword = string.Empty;
        private UserRole _selectedUserRole;
        private bool _isUserEditor = true;

        // Permissions
        private bool _canAccessSampleReceptions;
        private bool _canAccessCertificates;
        private bool _canAccessReports;
        private bool _canAccessSettings;
        private bool _canAccessAdminProcedures;
        private bool _canAccessUsers;
        private bool _isPermissionsSectionVisible;
        private bool _isUserDialogOpen;
        private string _searchText = string.Empty;
        
        // Audit Log Filter Properties
        private User? _selectedUserFilter;
        private DateTime? _filterStartDate;
        private DateTime? _filterEndDate;

        // Stats Properties
        private int _totalUsersCount;
        private int _activeUsersCount;
        private int _adminUsersCount;
        
        // Activity Stats Properties
        private int _activitiesTodayCount;
        private int _modificationsTodayCount;
        private string _mostActiveUser = "N/A";

        public ObservableCollection<UserRole> AvailableRoles { get; } = new ObservableCollection<UserRole>
        {
            UserRole.Admin,
            UserRole.User,
            UserRole.Viewer
        };

        public MaterialDesignThemes.Wpf.ISnackbarMessageQueue MessageQueue => _notificationService.MessageQueue;

        public UsersViewModel(Services.Repositories.UserRepository userRepository, UserService userService, DatabaseService dbService, INotificationService notificationService)
        {
            _userRepository = userRepository;
            _userService = userService;
            _dbService = dbService;
            _notificationService = notificationService;
            _users = new ObservableCollection<User>();
            _userActivities = new ObservableCollection<AuditLog>();
            
            InitializeCommands();

            // Default date range for activity filters (so "Today" is visible immediately)
            _filterStartDate = DateTime.Today.AddDays(-30);
            _filterEndDate = DateTime.Today;
            OnPropertyChanged(nameof(FilterStartDate));
            OnPropertyChanged(nameof(FilterEndDate));
        }

        #region Properties

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public ObservableCollection<AuditLog> UserActivities
        {
            get => _userActivities;
            set => SetProperty(ref _userActivities, value);
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                SetProperty(ref _selectedUser, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsEditingUser
        {
            get => _isEditingUser;
            set => SetProperty(ref _isEditingUser, value);
        }

        public string UserUsername 
        { 
            get => _userUsername; 
            set 
            {
                if (SetProperty(ref _userUsername, value))
                    CommandManager.InvalidateRequerySuggested();
            } 
        }

        public string UserFullName 
        { 
            get => _userFullName; 
            set 
            {
                if (SetProperty(ref _userFullName, value))
                    CommandManager.InvalidateRequerySuggested();
            } 
        }

        public string UserPassword 
        { 
            get => _userPassword; 
            set 
            {
                if (SetProperty(ref _userPassword, value))
                    CommandManager.InvalidateRequerySuggested();
            } 
        }
        public UserRole SelectedUserRole 
        { 
            get => _selectedUserRole; 
            set 
            {
                SetProperty(ref _selectedUserRole, value);
                UpdatePermissionsVisibility();
            }
        }
        public bool IsUserEditor { get => _isUserEditor; set => SetProperty(ref _isUserEditor, value); }
        
        public bool CanAccessSampleReceptions { get => _canAccessSampleReceptions; set => SetProperty(ref _canAccessSampleReceptions, value); }
        public bool CanAccessCertificates { get => _canAccessCertificates; set => SetProperty(ref _canAccessCertificates, value); }
        public bool CanAccessReports { get => _canAccessReports; set => SetProperty(ref _canAccessReports, value); }
        public bool CanAccessSettings { get => _canAccessSettings; set => SetProperty(ref _canAccessSettings, value); }
        public bool CanAccessAdminProcedures { get => _canAccessAdminProcedures; set => SetProperty(ref _canAccessAdminProcedures, value); }
        public bool CanAccessUsers { get => _canAccessUsers; set => SetProperty(ref _canAccessUsers, value); }

        public bool IsPermissionsSectionVisible
        {
            get => _isPermissionsSectionVisible;
            set => SetProperty(ref _isPermissionsSectionVisible, value);
        }

        public bool IsUserDialogOpen
        {
            get => _isUserDialogOpen;
            set => SetProperty(ref _isUserDialogOpen, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplySearchFilter();
                }
            }
        }

        public bool CanManageUsers => _userService.CurrentUser?.CanManageUsers ?? false;
        
        // Filter Properties
        public User? SelectedUserFilter
        {
            get => _selectedUserFilter;
            set
            {
                if (SetProperty(ref _selectedUserFilter, value))
                {
                    _ = LoadActivitiesAsync();
                }
            }
        }

        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set
            {
                if (SetProperty(ref _filterStartDate, value))
                {
                    _ = LoadActivitiesAsync();
                }
            }
        }

        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set
            {
                if (SetProperty(ref _filterEndDate, value))
                {
                    _ = LoadActivitiesAsync();
                }
            }
        }

        public int TotalUsersCount { get => _totalUsersCount; set => SetProperty(ref _totalUsersCount, value); }
        public int ActiveUsersCount { get => _activeUsersCount; set => SetProperty(ref _activeUsersCount, value); }
        public int AdminUsersCount { get => _adminUsersCount; set => SetProperty(ref _adminUsersCount, value); }
        
        public int ActivitiesTodayCount { get => _activitiesTodayCount; set => SetProperty(ref _activitiesTodayCount, value); }
        public int ModificationsTodayCount { get => _modificationsTodayCount; set => SetProperty(ref _modificationsTodayCount, value); }
        public string MostActiveUser { get => _mostActiveUser; set => SetProperty(ref _mostActiveUser, value); }

        #endregion

        #region Commands

        public ICommand AddUserCommand { get; private set; } = null!;
        public ICommand EditUserCommand { get; private set; } = null!;
        public ICommand SaveUserCommand { get; private set; } = null!;
        public ICommand CancelUserEditCommand { get; private set; } = null!;
        public ICommand ToggleUserFreezeCommand { get; private set; } = null!;

        public ICommand LoadActivitiesCommand { get; private set; } = null!;
        public ICommand ClearFiltersCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddUserCommand = new RelayCommand(_ => StartAddUser(), _ => CanManageUsers);
            EditUserCommand = new RelayCommand(param => StartEditUser(param as User), param => CanEditUser(param as User));
            SaveUserCommand = new RelayCommand(async _ => await SaveUserAsync(), _ => CanSaveUser());
            CancelUserEditCommand = new RelayCommand(_ => CancelUserEdit());
            ToggleUserFreezeCommand = new RelayCommand(async param => await ToggleUserFreezeAsync(param as User), param => CanFreezeUser(param as User));
            LoadActivitiesCommand = new RelayCommand(async _ => await LoadActivitiesAsync(), _ => CanManageUsers);
            ClearFiltersCommand = new RelayCommand(async _ => await ClearFiltersAsync(), _ => CanManageUsers);
        }

        #endregion

        #region Methods

        public async System.Threading.Tasks.Task LoadUsersAsync()
        {
            if (!CanManageUsers) return;

            try
            {
                IsBusy = true;
                BusyMessage = "جاري تحميل قائمة المستخدمين...";
                var users = await _userRepository.GetAllUsersAsync();
                Users = new ObservableCollection<User>(users);
                ApplySearchFilter();
                
                // Update stats
                TotalUsersCount = Users.Count;
                ActiveUsersCount = Users.Count(u => u.IsActive);
                AdminUsersCount = Users.Count(u => u.Role == UserRole.Admin);
            }
            catch (Exception ex)
            {
                NotificationTitle = "خطأ تحميل";
                NotificationMessage = $"تعذر تحميل قائمة المستخدمين من قاعدة البيانات: {ex.Message}";
                NotificationIcon = "AlertCircleOutline";
                IsNotificationDialogOpen = true;
                
                StatusMessage = $"خطأ في تحميل المستخدمين: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async System.Threading.Tasks.Task LoadActivitiesAsync()
        {
            if (!CanManageUsers) return;

            try
            {
                IsBusy = true;
                BusyMessage = "جاري تحميل سجل النشاطات...";
                var activities = await _dbService.GetAuditLogsAsync(SelectedUserFilter?.Id, FilterStartDate, FilterEndDate);
                UserActivities = new ObservableCollection<AuditLog>(activities);
                
                // Calculate Stats
                var today = DateTime.Today;
                ActivitiesTodayCount = UserActivities.Count(log => log.Timestamp.Date == today);
                ModificationsTodayCount = UserActivities.Count(log => log.Timestamp.Date == today && 
                    (log.Action.Contains("حذف") || log.Action.Contains("تعديل") || 
                     log.Action.Contains("Delete") || log.Action.Contains("Edit")));
                
                MostActiveUser = UserActivities
                    .GroupBy(log => log.UserName)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "N/A";
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في تحميل السجل: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
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
            var target = user ?? SelectedUser;
            if (target != null)
            {
                SelectedUser = target;
                LoadUserForEdit(target);
                IsEditingUser = true;
                IsUserDialogOpen = true;
            }
        }
        
        private bool CanEditUser(User? user)
        {
             var target = user ?? SelectedUser;
             return target != null && CanManageUsers;
        }

        private void LoadUserForEdit(User user)
        {
            UserUsername = user.Username;
            UserFullName = user.FullName;
            UserPassword = ""; // لا نعرض كلمة المرور
            SelectedUserRole = user.Role;
            IsUserEditor = user.IsEditor;

            // Unpack Permissions
            var perms = (user.Permissions ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            CanAccessSampleReceptions = perms.Contains("SampleReceptions");
            CanAccessCertificates = perms.Contains("Certificates");
            CanAccessReports = perms.Contains("Reports");
            CanAccessSettings = perms.Contains("Settings");
            CanAccessAdminProcedures = perms.Contains("AdminProcedures");
            CanAccessUsers = perms.Contains("Users");

            UpdatePermissionsVisibility();
        }

        private void ClearUserForm()
        {
            UserUsername = string.Empty;
            UserFullName = string.Empty;
            UserPassword = string.Empty;
            SelectedUserRole = UserRole.User;
            IsUserEditor = true;
            
            // Default Permissions
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
             // Hide detailed permissions for Admin (always has all)
             // Show for User/Viewer
             IsPermissionsSectionVisible = SelectedUserRole != UserRole.Admin;
        }

        private bool CanSaveUser()
        {
            if (SelectedUser == null)
            {
                return !string.IsNullOrWhiteSpace(UserUsername) &&
                       !string.IsNullOrWhiteSpace(UserFullName) &&
                       !string.IsNullOrWhiteSpace(UserPassword);
            }
            else
            {
                return !string.IsNullOrWhiteSpace(UserFullName);
            }
        }

        private async System.Threading.Tasks.Task SaveUserAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "جاري حفظ بيانات المستخدم...";
                if (SelectedUser == null)
                {
                    var newUser = new User
                    {
                        Username = UserUsername,
                        FullName = UserFullName,
                        Role = SelectedUserRole,
                        IsActive = true,
                        IsEditor = IsUserEditor,
                        Permissions = PackPermissions()
                    };

                    if (!await _userRepository.IsUsernameUniqueAsync(UserUsername))
                    {
                        IsBusy = false;
                        _notificationService.ShowError("اسم المستخدم موجود بالفعل. يرجى اختيار اسم آخر.");
                        StatusMessage = "خطأ: اسم المستخدم مكرر";
                        return;
                    }

                    await _userRepository.AddUserAsync(newUser, UserPassword);
                    
                    IsBusy = false;
                    SetNotification("تم الإضافة", $"تم إنشاء حساب جديد للمستخدم ({UserFullName}) بنجاح.", NotificationType.Success);

                    StatusMessage = "تم إضافة المستخدم بنجاح";
                    await LoadUsersAsync();
                    CancelUserEdit();
                }
                else
                {
                    SelectedUser.FullName = UserFullName;
                    SelectedUser.Role = SelectedUserRole;
                    SelectedUser.IsEditor = IsUserEditor;
                    SelectedUser.Permissions = PackPermissions();
                    
                    string? newPass = string.IsNullOrWhiteSpace(UserPassword) ? null : UserPassword;

                    if (!await _userRepository.IsUsernameUniqueAsync(SelectedUser.Username, SelectedUser.Id))
                    {
                        IsBusy = false;
                        _notificationService.ShowError("اسم المستخدم هذا مستخدم من قبل حساب آخر.");
                        StatusMessage = "خطأ: اسم المستخدم مكرر";
                        return;
                    }

                    await _userRepository.UpdateUserAsync(SelectedUser, newPass);
                    
                    IsBusy = false;
                    SetNotification("تم التحديث", $"تم تحديث بيانات المستخدم ({UserFullName}) وحفظ التعديلات بنجاح.", NotificationType.Success);

                    StatusMessage = "تم تحديث بيانات المستخدم بنجاح";
                    await LoadUsersAsync();
                    CancelUserEdit();
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                SetNotification("فشل العملية", $"حدث خطأ أثناء محاولة حفظ البيانات: {ex.Message}", NotificationType.Error);

                StatusMessage = $"خطأ: {ex.Message}";
            }
        }

        private void CancelUserEdit()
        {
            IsUserDialogOpen = false;
            IsEditingUser = false;
            ClearUserForm();
            SelectedUser = null;
        }

        private bool CanFreezeUser(User? user)
        {
            var target = user ?? SelectedUser;
            return target != null && target.Id != _userService.CurrentUser?.Id && CanManageUsers;
        }

        private string PackPermissions()
        {
            if (SelectedUserRole == UserRole.Admin) return "All"; // Admin has implicit all
            
            var perms = new System.Collections.Generic.List<string>();
            if (CanAccessSampleReceptions) perms.Add("SampleReceptions");
            if (CanAccessCertificates) perms.Add("Certificates");
            if (CanAccessReports) perms.Add("Reports");
            if (CanAccessSettings) perms.Add("Settings");
            if (CanAccessAdminProcedures) perms.Add("AdminProcedures");
            if (CanAccessUsers) perms.Add("Users");
            
            return string.Join(",", perms);
        }

        private System.Threading.Tasks.Task ToggleUserFreezeAsync(User? user)
        {
            var target = user ?? SelectedUser;
            if (target == null) return System.Threading.Tasks.Task.CompletedTask;
            
            // Ensure selected is updated if clicked from card
            SelectedUser = target;

            string action = target.IsActive ? "تجميد" : "تنشيط";
            string title = target.IsActive ? "تأكيد التجميد" : "تأكيد التنشيط";
            string message = target.Role == UserRole.Admin && target.IsActive 
                ? $"تنبيه: أنت على وشك تجميد حساب مدير ({target.FullName}). هل أنت متأكد من الاستمرار؟"
                : $"هل أنت متأكد من {action} حساب المستخدم: {target.FullName}؟";
            NotificationType type = target.Role == UserRole.Admin && target.IsActive ? NotificationType.Warning : NotificationType.Question;

            RaiseConfirmation(
                title,
                message,
                type,
                async (confirmed) => 
                {
                    if (confirmed)
                    {
                        bool freeze = target.IsActive;

                        try 
                        {
                    IsBusy = true;
                    BusyMessage = $"جاري {action} الحساب...";
                    await _userRepository.ToggleUserFreezeAsync(target.Id, freeze);
                    
                    IsBusy = false;
                    SetNotification("تغيير الحالة", $"تم بنجاح {action} حساب المستخدم: {target.FullName}.", NotificationType.Success);

                            IsBusy = true;
                            BusyMessage = $"جاري {action} الحساب...";
                            await _userRepository.ToggleUserFreezeAsync(target.Id, freeze);
                            
                            IsBusy = false;
                            SetNotification("تغيير الحالة", $"تم بنجاح {action} حساب المستخدم: {target.FullName}.", NotificationType.Success);

                            StatusMessage = $"تم {action} الحساب بنجاح";
                            await LoadUsersAsync();
                        }
                        catch (Exception ex)
                        {
                            Services.LoggerService.LogError("Error toggling user freeze", ex);
                            IsBusy = false;
                            SetNotification("فشل الإجراء", $"تعذر {action} الحساب حالياً. يرجى المحاولة مرة أخرى.", NotificationType.Error);
                            
                            StatusMessage = $"فشل في {action} الحساب";
                        }
                    }
                });
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public async System.Threading.Tasks.Task ClearFiltersAsync()
        {
             SelectedUserFilter = null;
             FilterStartDate = null;
             FilterEndDate = null;
             await LoadActivitiesAsync();
        }

        private void ApplySearchFilter()
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(Users);
            if (view != null)
            {
                view.Filter = item =>
                {
                    if (string.IsNullOrWhiteSpace(SearchText)) return true;
                    if (item is User user)
                    {
                        return (user.Username?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                               (user.FullName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
                    }
                    return false;
                };
                view.Refresh();
            }
        }

        #endregion
    }
}
