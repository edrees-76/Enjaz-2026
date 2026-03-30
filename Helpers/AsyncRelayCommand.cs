using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Enjaz.Services;

namespace Enjaz.Helpers
{
    /// <summary>
    /// A command that executes an asynchronous task.
    /// Catches and logs exceptions to prevent application crashes.
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private readonly Predicate<object?>? _canExecute;
        private bool _isExecuting;
        private readonly IErrorHandler? _errorHandler;

        public interface IErrorHandler
        {
            void HandleError(Exception ex);
        }

        public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null, IErrorHandler? errorHandler = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }

        // Convenience constructor for parameterless async methods
        public AsyncRelayCommand(Func<Task> execute, Predicate<object?>? canExecute = null, IErrorHandler? errorHandler = null)
            : this(_ => execute(), canExecute, errorHandler)
        {
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute(parameter));
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute(parameter);
            }
            catch (Exception ex)
            {
                LoggerService.LogError("AsyncRelayCommand Execution Failed", ex);
                _errorHandler?.HandleError(ex);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
