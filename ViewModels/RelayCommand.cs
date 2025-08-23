using System;
using System.Windows.Input;

namespace BudgetManagement.ViewModels
{
    /// <summary>
    /// A command implementation for MVVM pattern, optimized for senior-friendly applications
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute) : this(execute, null)
        {
        }

        public RelayCommand(Action execute, Func<bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            // DEBUG: Log command execution
            System.Diagnostics.Debug.WriteLine($"RelayCommand.Execute called, CanExecute: {CanExecute(parameter)}");
            
            if (CanExecute(parameter))
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("RelayCommand executing action...");
                    _execute();
                    System.Diagnostics.Debug.WriteLine("RelayCommand action completed.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RelayCommand execution failed: {ex}");
                    throw; // Re-throw to maintain existing behavior
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("RelayCommand.Execute - CanExecute returned false");
            }
        }

        /// <summary>
        /// Manually trigger CanExecuteChanged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// A generic command implementation with parameter support
    /// </summary>
    /// <typeparam name="T">The type of the command parameter</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute) : this(execute, null)
        {
        }

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is T typedParameter)
            {
                return _canExecute?.Invoke(typedParameter) ?? true;
            }
            return _canExecute?.Invoke(default(T)!) ?? true;
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                if (parameter is T typedParameter)
                {
                    _execute(typedParameter);
                }
                else
                {
                    _execute(default(T)!);
                }
            }
        }

        /// <summary>
        /// Manually trigger CanExecuteChanged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// An async command implementation for long-running operations
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<System.Threading.Tasks.Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<System.Threading.Tasks.Task> execute) : this(execute, null)
        {
        }

        public AsyncRelayCommand(Func<System.Threading.Tasks.Task> execute, Func<bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            // DEBUG: Log command execution
            System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand.Execute called, CanExecute: {CanExecute(parameter)}");
            
            if (CanExecute(parameter))
            {
                try
                {
                    _isExecuting = true;
                    RaiseCanExecuteChanged();
                    System.Diagnostics.Debug.WriteLine("AsyncRelayCommand executing async method...");
                    await _execute();
                    System.Diagnostics.Debug.WriteLine("AsyncRelayCommand async method completed.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand execution failed: {ex}");
                    throw; // Re-throw to maintain existing behavior
                }
                finally
                {
                    _isExecuting = false;
                    RaiseCanExecuteChanged();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("AsyncRelayCommand.Execute - CanExecute returned false");
            }
        }

        /// <summary>
        /// Manually trigger CanExecuteChanged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// A generic async command implementation with parameter support for long-running operations
    /// </summary>
    /// <typeparam name="T">The type of the command parameter</typeparam>
    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T, System.Threading.Tasks.Task> _execute;
        private readonly Func<T, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<T, System.Threading.Tasks.Task> execute) : this(execute, null)
        {
        }

        public AsyncRelayCommand(Func<T, System.Threading.Tasks.Task> execute, Func<T, bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            if (!_isExecuting)
            {
                if (parameter is T typedParameter)
                {
                    return _canExecute?.Invoke(typedParameter) ?? true;
                }
                return _canExecute?.Invoke(default(T)!) ?? true;
            }
            return false;
        }

        public async void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    _isExecuting = true;
                    RaiseCanExecuteChanged();
                    
                    if (parameter is T typedParameter)
                    {
                        await _execute(typedParameter);
                    }
                    else
                    {
                        await _execute(default(T)!);
                    }
                }
                finally
                {
                    _isExecuting = false;
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Manually trigger CanExecuteChanged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}