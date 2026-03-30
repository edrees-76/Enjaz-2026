using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Enjaz.Services;

public class SessionTimeoutService
{
	private readonly DispatcherTimer _inactivityTimer;

	private readonly int _timeoutMinutes;

	private DateTime _lastActivityTime;

	public TimeSpan RemainingTime
	{
		get
		{
			TimeSpan timeSpan = DateTime.Now - _lastActivityTime;
			TimeSpan timeSpan2 = TimeSpan.FromMinutes(_timeoutMinutes) - timeSpan;
			return (timeSpan2 > TimeSpan.Zero) ? timeSpan2 : TimeSpan.Zero;
		}
	}

	public event Action? SessionExpired;

	public SessionTimeoutService(int timeoutMinutes = 10)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		_timeoutMinutes = timeoutMinutes;
		_lastActivityTime = DateTime.Now;
		_inactivityTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMinutes(1.0)
		};
		_inactivityTimer.Tick += CheckInactivity;
	}

	public void Start()
	{
		ResetTimer();
		_inactivityTimer.Start();
		Application.Current.MainWindow?.AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(OnUserActivity));
		Application.Current.MainWindow?.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnUserKeyActivity));
		LoggerService.LogInfo($"Session timeout started ({_timeoutMinutes} minutes)");
	}

	public void Stop()
	{
		_inactivityTimer.Stop();
		LoggerService.LogInfo("Session timeout stopped");
	}

	public void ResetTimer()
	{
		_lastActivityTime = DateTime.Now;
	}

	private void OnUserActivity(object sender, MouseEventArgs e)
	{
		ResetTimer();
	}

	private void OnUserKeyActivity(object sender, KeyEventArgs e)
	{
		ResetTimer();
	}

	private void CheckInactivity(object? sender, EventArgs e)
	{
		if ((DateTime.Now - _lastActivityTime).TotalMinutes >= (double)_timeoutMinutes)
		{
			_inactivityTimer.Stop();
			LoggerService.LogWarning($"Session expired after {_timeoutMinutes} minutes of inactivity");
			this.SessionExpired?.Invoke();
		}
	}
}
