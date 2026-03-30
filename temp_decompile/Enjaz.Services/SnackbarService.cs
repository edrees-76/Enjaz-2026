using System;
using MaterialDesignThemes.Wpf;

namespace Enjaz.Services;

public class SnackbarService : INotificationService
{
	private readonly SnackbarMessageQueue _messageQueue;

	public ISnackbarMessageQueue MessageQueue => _messageQueue;

	public SnackbarService()
	{
		_messageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3.0));
	}

	public void ShowSuccess(string message)
	{
		_messageQueue.Enqueue(message, "OK", delegate
		{
		});
	}

	public void ShowError(string message)
	{
		_messageQueue.Enqueue(message, "DISMISS", delegate
		{
		});
	}

	public void ShowWarning(string message)
	{
		_messageQueue.Enqueue(message, "OK", delegate
		{
		});
	}

	public void ShowInfo(string message)
	{
		_messageQueue.Enqueue(message);
	}
}
