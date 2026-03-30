using System;
using System.Windows.Input;

namespace Enjaz.Helpers
{
    /// <summary>
    /// أمر قابل لإعادة الاستخدام لنمط MVVM
    /// Reusable command for MVVM pattern
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        /// <summary>
        /// إنشاء أمر جديد
        /// </summary>
        /// <param name="execute">الإجراء المراد تنفيذه</param>
        /// <param name="canExecute">شرط إمكانية التنفيذ (اختياري)</param>
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// حدث تغيير إمكانية التنفيذ
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// التحقق من إمكانية تنفيذ الأمر
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// تنفيذ الأمر
        /// </summary>
        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}
