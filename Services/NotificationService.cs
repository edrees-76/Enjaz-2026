using System;
using MaterialDesignThemes.Wpf;

namespace Enjaz.Services
{
    public interface INotificationService
    {
        ISnackbarMessageQueue MessageQueue { get; }
        void ShowSuccess(string message);
        void ShowError(string message);
        void ShowWarning(string message);
        void ShowInfo(string message);
    }

    public class SnackbarService : INotificationService
    {
        private readonly SnackbarMessageQueue _messageQueue;

        public ISnackbarMessageQueue MessageQueue => _messageQueue;

        public SnackbarService()
        {
            _messageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));
        }

        public void ShowSuccess(string message)
        {
            _messageQueue.Enqueue(message, "OK", () => { });
        }

        public void ShowError(string message)
        {
            _messageQueue.Enqueue(message, "DISMISS", () => { });
        }

        public void ShowWarning(string message)
        {
            _messageQueue.Enqueue(message, "OK", () => { });
        }

        public void ShowInfo(string message)
        {
             _messageQueue.Enqueue(message);
        }
    }
}
