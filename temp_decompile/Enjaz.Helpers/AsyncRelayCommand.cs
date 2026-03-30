using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Enjaz.Services;

namespace Enjaz.Helpers;

public class AsyncRelayCommand : ICommand
{
	public interface IErrorHandler
	{
		void HandleError(Exception ex);
	}

	private readonly Func<object?, Task> _execute;

	private readonly Predicate<object?>? _canExecute;

	private bool _isExecuting;

	private readonly IErrorHandler? _errorHandler;

	public event EventHandler? CanExecuteChanged
	{
		add
		{
			CommandManager.RequerySuggested += value;
		}
		remove
		{
			CommandManager.RequerySuggested -= value;
		}
	}

	public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null, IErrorHandler? errorHandler = null)
	{
		_execute = execute ?? throw new ArgumentNullException("execute");
		_canExecute = canExecute;
		_errorHandler = errorHandler;
	}

	public AsyncRelayCommand(Func<Task> execute, Predicate<object?>? canExecute = null, IErrorHandler? errorHandler = null)
		: this((object? _) => execute(), canExecute, errorHandler)
	{
	}

	public bool CanExecute(object? parameter)
	{
		return !_isExecuting && (_canExecute == null || _canExecute(parameter));
	}

	public async void Execute(object? parameter)
	{
		if (!CanExecute(parameter))
		{
			return;
		}
		_isExecuting = true;
		RaiseCanExecuteChanged();
		try
		{
			await _execute(parameter);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			LoggerService.LogError("AsyncRelayCommand Execution Failed", ex2);
			_errorHandler?.HandleError(ex2);
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
