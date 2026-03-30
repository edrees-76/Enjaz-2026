using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Enjaz.Services
{
    /// <summary>
    /// خدمة إدارة انتهاء الجلسة بعد فترة من عدم النشاط
    /// Session timeout service for auto-logout after inactivity
    /// </summary>
    public class SessionTimeoutService
    {
        private readonly DispatcherTimer _inactivityTimer;
        private readonly int _timeoutMinutes;
        private DateTime _lastActivityTime;

        public event Action? SessionExpired;

        /// <summary>
        /// إنشاء خدمة انتهاء الجلسة
        /// </summary>
        /// <param name="timeoutMinutes">فترة الخمول بالدقائق قبل تسجيل الخروج (الافتراضي: 30 دقيقة)</param>
        public SessionTimeoutService(int timeoutMinutes = 10)
        {
            _timeoutMinutes = timeoutMinutes;
            _lastActivityTime = DateTime.Now;

            _inactivityTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1) // فحص كل دقيقة
            };
            _inactivityTimer.Tick += CheckInactivity;
        }

        /// <summary>
        /// بدء مراقبة النشاط
        /// </summary>
        public void Start()
        {
            ResetTimer();
            _inactivityTimer.Start();

            // ربط أحداث المستخدم لتتبع النشاط
            Application.Current.MainWindow?.AddHandler(
                UIElement.PreviewMouseMoveEvent,
                new MouseEventHandler(OnUserActivity));
            Application.Current.MainWindow?.AddHandler(
                UIElement.PreviewKeyDownEvent,
                new KeyEventHandler(OnUserKeyActivity));

            LoggerService.LogInfo($"Session timeout started ({_timeoutMinutes} minutes)");
        }

        /// <summary>
        /// إيقاف مراقبة النشاط
        /// </summary>
        public void Stop()
        {
            _inactivityTimer.Stop();
            LoggerService.LogInfo("Session timeout stopped");
        }

        /// <summary>
        /// إعادة تعيين عداد الخمول
        /// </summary>
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
            var inactivityDuration = DateTime.Now - _lastActivityTime;
            
            if (inactivityDuration.TotalMinutes >= _timeoutMinutes)
            {
                _inactivityTimer.Stop();
                LoggerService.LogWarning($"Session expired after {_timeoutMinutes} minutes of inactivity");
                
                // Trigger event immediately (MainViewModel handles the logout)
                SessionExpired?.Invoke();
            }
        }

        /// <summary>
        /// الوقت المتبقي قبل انتهاء الجلسة
        /// </summary>
        public TimeSpan RemainingTime
        {
            get
            {
                var elapsed = DateTime.Now - _lastActivityTime;
                var remaining = TimeSpan.FromMinutes(_timeoutMinutes) - elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
    }
}
