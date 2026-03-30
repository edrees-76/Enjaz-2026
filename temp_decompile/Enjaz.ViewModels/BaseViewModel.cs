using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Enjaz.Helpers;

namespace Enjaz.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
	private bool _isNotificationDialogOpen;

	private string _notificationTitle = string.Empty;

	private string _notificationMessage = string.Empty;

	private string _notificationIcon = "AlertCircleOutline";

	private Brush _notificationColor = Brushes.Gray;

	private NotificationType _notificationType = NotificationType.Information;

	private bool _isConfirmMode;

	private string? _notificationImagePath;

	private bool _isBusy;

	private string _busyMessage = "جاري التحميل...";

	private string _statusMessage = string.Empty;

	public NotificationType NotificationType
	{
		get
		{
			return _notificationType;
		}
		set
		{
			SetProperty(ref _notificationType, value, "NotificationType");
		}
	}

	public bool IsConfirmMode
	{
		get
		{
			return _isConfirmMode;
		}
		set
		{
			SetProperty(ref _isConfirmMode, value, "IsConfirmMode");
		}
	}

	public ICommand CloseNotificationCommand { get; }

	public bool IsNotificationDialogOpen
	{
		get
		{
			return _isNotificationDialogOpen;
		}
		set
		{
			SetProperty(ref _isNotificationDialogOpen, value, "IsNotificationDialogOpen");
		}
	}

	public string NotificationTitle
	{
		get
		{
			return _notificationTitle;
		}
		set
		{
			SetProperty(ref _notificationTitle, value, "NotificationTitle");
		}
	}

	public string NotificationMessage
	{
		get
		{
			return _notificationMessage;
		}
		set
		{
			SetProperty(ref _notificationMessage, value, "NotificationMessage");
		}
	}

	public string NotificationIcon
	{
		get
		{
			return _notificationIcon;
		}
		set
		{
			SetProperty(ref _notificationIcon, value, "NotificationIcon");
		}
	}

	public Brush NotificationColor
	{
		get
		{
			return _notificationColor;
		}
		set
		{
			if (SetProperty(ref _notificationColor, value, "NotificationColor"))
			{
				OnPropertyChanged("NotificationForeground");
			}
		}
	}

	public Brush NotificationForeground => IsDarkColor(NotificationColor) ? Brushes.White : Brushes.Black;

	public string? NotificationImagePath
	{
		get
		{
			return _notificationImagePath;
		}
		set
		{
			if (SetProperty(ref _notificationImagePath, value, "NotificationImagePath"))
			{
				OnPropertyChanged("IsNotificationImage");
			}
		}
	}

	public bool IsNotificationImage => !string.IsNullOrEmpty(NotificationImagePath);

	public bool IsBusy
	{
		get
		{
			return _isBusy;
		}
		set
		{
			SetProperty(ref _isBusy, value, "IsBusy");
		}
	}

	public string BusyMessage
	{
		get
		{
			return _busyMessage;
		}
		set
		{
			SetProperty(ref _busyMessage, value, "BusyMessage");
		}
	}

	public string StatusMessage
	{
		get
		{
			return _statusMessage;
		}
		set
		{
			SetProperty(ref _statusMessage, value, "StatusMessage");
		}
	}

	public event Action<string, string, NotificationType, Action<bool>, string?>? RequestConfirmation;

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void RaiseConfirmation(string title, string message, NotificationType type, Action<bool> callback, string? icon = null)
	{
		this.RequestConfirmation?.Invoke(title, message, type, callback, icon);
	}

	public void SetNotification(string title, string message, NotificationType type = NotificationType.Information, string? icon = null, string? imagePath = null)
	{
		NotificationTitle = title;
		NotificationMessage = message;
		NotificationType = type;
		NotificationImagePath = imagePath;
		NotificationIcon = icon ?? GetDefaultIcon(type);
		IsBusy = false;
		NotificationColor = ResolveNotificationColor(type);
		IsNotificationDialogOpen = true;
	}

	private static Brush ResolveNotificationColor(NotificationType type)
	{
		if (1 == 0)
		{
		}
		string text = type switch
		{
			NotificationType.Success => "SuccessBrush", 
			NotificationType.Error => "ErrorBrush", 
			NotificationType.Warning => "WarningBrush", 
			_ => "PrimaryBrush", 
		};
		if (1 == 0)
		{
		}
		string text2 = text;
		try
		{
			Application current = Application.Current;
			if (current != null && current.Resources?.Contains(text2) == true)
			{
				return (Brush)current.FindResource(text2);
			}
		}
		catch (InvalidOperationException)
		{
		}
		if (1 == 0)
		{
		}
		SolidColorBrush result = type switch
		{
			NotificationType.Warning => new SolidColorBrush(Color.FromRgb(byte.MaxValue, 152, 0)), 
			NotificationType.Error => Brushes.Red, 
			NotificationType.Success => Brushes.Green, 
			_ => Brushes.DodgerBlue, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private string GetDefaultIcon(NotificationType type)
	{
		if (1 == 0)
		{
		}
		string result = type switch
		{
			NotificationType.Success => "CheckCircleOutline", 
			NotificationType.Error => "AlertCircleOutline", 
			NotificationType.Warning => "AlertOutline", 
			NotificationType.Question => "HelpCircleOutline", 
			_ => "InformationOutline", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public BaseViewModel()
	{
		CloseNotificationCommand = new RelayCommand(delegate
		{
			IsNotificationDialogOpen = false;
		});
	}

	private bool IsDarkColor(Brush brush)
	{
		if (brush == null)
		{
			return true;
		}
		if (brush is SolidColorBrush { Color: var color })
		{
			double num = (0.299 * (double)(int)color.R + 0.587 * (double)(int)color.G + 0.114 * (double)(int)color.B) / 255.0;
			return num < 0.6;
		}
		return true;
	}

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (object.Equals(field, value))
		{
			return false;
		}
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}
}
