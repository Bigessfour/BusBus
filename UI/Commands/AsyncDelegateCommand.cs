#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BusBus.UI.Commands
{
    /// <summary>
    /// Async command implementation for .NET 8 Windows Forms
    /// Supports modern async/await patterns with ICommand
    /// </summary>
    public class AsyncDelegateCommand : ICommand
    {
        private readonly Func<object?, Task> _executeAsync;
        private readonly Func<object?, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncDelegateCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public AsyncDelegateCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            ArgumentNullException.ThrowIfNull(executeAsync);
            _executeAsync = _ => executeAsync();
            _canExecute = canExecute != null ? _ => canExecute() : null;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (_isExecuting || !CanExecute(parameter))
                return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _executeAsync(parameter);
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                Console.WriteLine($"Error executing async command: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"AsyncCommand Error: {ex}");
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
