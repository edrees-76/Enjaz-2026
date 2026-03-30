using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Enjaz.ViewModels
{
    public enum NotificationType
    {
        Information,
        Success,
        Warning,
        Error,
        Question
    }

    /// <summary>
    /// الفئة الأساسية لجميع ViewModels - تطبق INotifyPropertyChanged
    /// Base class for all ViewModels - Implements INotifyPropertyChanged
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged, System.IDisposable
    {
        private bool _isNotificationDialogOpen;
        private string _notificationTitle = string.Empty;
        private string _notificationMessage = string.Empty;
        private string _notificationIcon = "AlertCircleOutline";
        private System.Windows.Media.Brush _notificationColor = System.Windows.Media.Brushes.Gray;
        private NotificationType _notificationType = NotificationType.Information;
        private bool _isConfirmMode;

        /// <summary>
        /// حدث طلب تأكيد - يُستخدم لتوجيه حوارات التأكيد عبر MainViewModel
        /// </summary>
        public event Action<string, string, NotificationType, Action<bool>, string?>? RequestConfirmation;

        public NotificationType NotificationType
        {
            get => _notificationType;
            set => SetProperty(ref _notificationType, value);
        }

        public bool IsConfirmMode
        {
            get => _isConfirmMode;
            set => SetProperty(ref _isConfirmMode, value);
        }

        /// <summary>
        /// يطلب حوار تأكيد من MainViewModel عبر الحدث
        /// </summary>
        protected void RaiseConfirmation(string title, string message, NotificationType type, Action<bool> callback, string? icon = null)
        {
            RequestConfirmation?.Invoke(title, message, type, callback, icon);
        }

        public void SetNotification(string title, string message, NotificationType type = NotificationType.Information, string? icon = null, string? imagePath = null)
        {
            NotificationTitle = title;
            NotificationMessage = message;
            NotificationType = type;
            NotificationImagePath = imagePath;
            NotificationIcon = icon ?? GetDefaultIcon(type);

            // Ensure Busy stays false when a notification is shown to prevent UI blocking
            IsBusy = false;

            // Resolve color from resources (null-safe for unit testing)
            NotificationColor = ResolveNotificationColor(type);

            IsNotificationDialogOpen = true;
        }

        /// <summary>
        /// تحديد لون الإشعار — معزول عن Application.Current للتوافق مع Unit Tests
        /// </summary>
        private static System.Windows.Media.Brush ResolveNotificationColor(NotificationType type)
        {
            string resourceName = type switch
            {
                NotificationType.Success => "SuccessBrush",
                NotificationType.Error => "ErrorBrush",
                NotificationType.Warning => "WarningBrush",
                _ => "PrimaryBrush"
            };

            try
            {
                var app = System.Windows.Application.Current;
                if (app?.Resources?.Contains(resourceName) == true)
                {
                    return (System.Windows.Media.Brush)app.FindResource(resourceName);
                }
            }
            catch (InvalidOperationException)
            {
                // Application.Current may not exist in test context
            }

            // Fallback colors
            return type switch
            {
                NotificationType.Warning => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)),
                NotificationType.Error => System.Windows.Media.Brushes.Red,
                NotificationType.Success => System.Windows.Media.Brushes.Green,
                _ => System.Windows.Media.Brushes.DodgerBlue
            };
        }

        private string GetDefaultIcon(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => "CheckCircleOutline",
                NotificationType.Error => "AlertCircleOutline",
                NotificationType.Warning => "AlertOutline",
                NotificationType.Question => "HelpCircleOutline",
                _ => "InformationOutline"
            };
        }

        public BaseViewModel()
        {
            CloseNotificationCommand = new Helpers.RelayCommand(_ => IsNotificationDialogOpen = false);
        }

        public System.Windows.Input.ICommand CloseNotificationCommand { get; }

        public bool IsNotificationDialogOpen
        {
            get => _isNotificationDialogOpen;
            set => SetProperty(ref _isNotificationDialogOpen, value);
        }

        public string NotificationTitle
        {
            get => _notificationTitle;
            set => SetProperty(ref _notificationTitle, value);
        }

        public string NotificationMessage
        {
            get => _notificationMessage;
            set => SetProperty(ref _notificationMessage, value);
        }

        public string NotificationIcon
        {
            get => _notificationIcon;
            set => SetProperty(ref _notificationIcon, value);
        }

        public System.Windows.Media.Brush NotificationColor
        {
            get => _notificationColor;
            set
            {
                if (SetProperty(ref _notificationColor, value))
                {
                    OnPropertyChanged(nameof(NotificationForeground));
                }
            }
        }

        public System.Windows.Media.Brush NotificationForeground => 
            IsDarkColor(NotificationColor) ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black;

        private bool IsDarkColor(System.Windows.Media.Brush brush)
        {
            if (brush == null) return true;
            
            if (brush is System.Windows.Media.SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                // Standard relative luminance calculation
                double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
                return luminance < 0.6; // Slightly increased threshold for better readability on mid-tones
            }
            return true; // Default to dark background (white text)
        }

        private string? _notificationImagePath;
        public string? NotificationImagePath
        {
            get => _notificationImagePath;
            set
            {
                if (SetProperty(ref _notificationImagePath, value))
                {
                    OnPropertyChanged(nameof(IsNotificationImage));
                }
            }
        }

        public bool IsNotificationImage => !string.IsNullOrEmpty(NotificationImagePath);

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _busyMessage = "جاري التحميل...";
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// حدث تغيير الخاصية
        /// Property changed event
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// إطلاق حدث تغيير الخاصية
        /// Raise property changed event
        /// </summary>
        /// <param name="propertyName">اسم الخاصية</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// تعيين قيمة الخاصية مع إطلاق الحدث
        /// Set property value and raise event
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// تنفيذ إجراء غير متزامن بأمان مع معالجة الأخطاء وعرض إشعار
        /// Safely execute async action with try-catch fallback
        /// </summary>
        protected async System.Threading.Tasks.Task SafeExecuteAsync(Func<System.Threading.Tasks.Task> action, string errorMessage = "حدث خطأ أثناء تنفيذ العملية")
        {
            try
            {
                IsBusy = true;
                await action();
            }
            catch (Exception ex)
            {
                SetNotification("خطأ", $"{errorMessage}\n{ex.Message}", NotificationType.Error);
                // We rely on caller to also log it, or we could add a logger here if available.
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// تنفيذ إجراء متزامن بأمان مع معالجة الأخطاء وعرض إشعار
        /// Safely execute sync action with try-catch fallback
        /// </summary>
        protected void SafeExecute(Action action, string errorMessage = "حدث خطأ أثناء تنفيذ العملية")
        {
            try
            {
                IsBusy = true;
                action();
            }
            catch (Exception ex)
            {
                SetNotification("خطأ", $"{errorMessage}\n{ex.Message}", NotificationType.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        #region IDisposable implementation
        
        // Flag to check if Dispose has been called
        private bool _disposed = false;
        
        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">true if disposing managed state</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Unhook generic events here if needed at base level
                // Example: CommandManager.RequerySuggested -= OnRequerySuggested;
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer for safety fallback
        /// </summary>
        ~BaseViewModel()
        {
            Dispose(false);
        }

        #endregion
    }
}
