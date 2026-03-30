using MaterialDesignThemes.Wpf;

namespace Enjaz.Services;

public interface INotificationService
{
	ISnackbarMessageQueue MessageQueue { get; }

	void ShowSuccess(string message);

	void ShowError(string message);

	void ShowWarning(string message);

	void ShowInfo(string message);
}
