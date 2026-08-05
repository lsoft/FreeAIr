using System.Windows.Input;

namespace WpfHelpers
{
    /// <summary>
    /// Base <see cref="ICommand"/> for async command handlers: tracks an in-flight execution via
    /// <see cref="_isExecuting"/> so <see cref="CanExecute"/> returns false for the duration (preventing
    /// re-entrant double-clicks) and reports execution/can-execute exceptions in a message box.
    /// </summary>
    public abstract class AsyncBaseRelayCommand : ICommand
    {
        private long _isExecuting;

        public AsyncBaseRelayCommand(
            )
        {
        }

        public event EventHandler CanExecuteChanged
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

        /// <summary>Forces WPF to re-query <see cref="CanExecute"/> on bound controls.</summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>False while a previous <see cref="Execute"/> call is still running, otherwise delegates to <see cref="CanExecuteInternal"/>.</summary>
        public bool CanExecute(object parameter)
        {
            if (Interlocked.Read(ref _isExecuting) != 0)
            {
                return false;
            }

            try
            {
                return CanExecuteInternal(parameter);
            }
            catch (Exception excp)
            {
                //todo log
                System.Windows.MessageBox.Show(
                    excp.Message
                    + Environment.NewLine
                    + excp.StackTrace,
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                    );
            }

            return false;
        }

        /// <summary>Runs <see cref="ExecuteInternalAsync"/>, marking the command busy for the duration and showing a message box on failure.</summary>
        public async void Execute(object parameter)
        {
            Interlocked.Exchange(ref _isExecuting, 1);
            RaiseCanExecuteChanged();

            try
            {
                await ExecuteInternalAsync(parameter);
            }
            catch (Exception excp)
            {
                //todo log
                System.Windows.MessageBox.Show(
                    excp.Message
                    + Environment.NewLine
                    + excp.StackTrace,
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                    );
            }
            finally
            {
                Interlocked.Exchange(ref _isExecuting, 0);
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>Async command body implemented by derived commands.</summary>
        protected abstract Task ExecuteInternalAsync(object parameter);
        /// <summary>Extra can-execute condition beyond the busy check; defaults to always true.</summary>
        protected virtual bool CanExecuteInternal(object parameter) => true;

    }

    /// <summary>Delegate-backed async <see cref="ICommand"/>: same busy-guarding and error-reporting behavior as <see cref="AsyncBaseRelayCommand"/>, without requiring a subclass per command.</summary>
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Predicate<object> _canExecute;

        private long _isExecuting;

        /// <summary>Wraps <paramref name="execute"/> (and optional <paramref name="canExecute"/>, default always-true) as an <see cref="ICommand"/>.</summary>
        public AsyncRelayCommand(
            Func<object, Task> execute,
            Predicate<object>? canExecute = null
            )
        {
            _execute = execute;
            _canExecute = canExecute ?? (o => true);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>Forces WPF to re-query <see cref="CanExecute"/> on bound controls.</summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>False while a previous <see cref="Execute"/> call is still running, otherwise delegates to the <see cref="_canExecute"/> predicate.</summary>
        public bool CanExecute(object parameter)
        {
            if (Interlocked.Read(ref _isExecuting) != 0)
            {
                return false;
            }

            return _canExecute(parameter);
        }

        /// <summary>Runs <see cref="_execute"/>, marking the command busy for the duration and showing a message box on failure.</summary>
        public async void Execute(object parameter)
        {
            Interlocked.Exchange(ref _isExecuting, 1);
            RaiseCanExecuteChanged();

            try
            {
                await _execute(parameter);
            }
            catch (Exception excp)
            {
                //todo log
                System.Windows.MessageBox.Show(
                    excp.Message
                    + Environment.NewLine
                    + excp.StackTrace,
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                    );
            }
            finally
            {
                Interlocked.Exchange(ref _isExecuting, 0);
                RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Typed variant of <see cref="AsyncRelayCommand"/>: casts the command parameter to <typeparamref name="TParameter"/> before calling the delegates.</summary>
    public sealed class AsyncRelayCommand<TParameter> : ICommand
        where TParameter : class
    {
        private readonly Func<TParameter, Task> _execute;
        private readonly Func<TParameter, bool> _canExecute;

        private long _isExecuting;

        /// <summary>Wraps <paramref name="execute"/> (and optional <paramref name="canExecute"/>, default always-true) as an <see cref="ICommand"/>.</summary>
        public AsyncRelayCommand(
            Func<TParameter, Task> execute,
            Func<TParameter, bool>? canExecute = null
        )
        {
            _execute = execute;
            _canExecute = canExecute ?? (o => true);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>Forces WPF to re-query <see cref="CanExecute"/> on bound controls.</summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>False while a previous <see cref="Execute"/> call is still running, otherwise delegates to the <see cref="_canExecute"/> predicate.</summary>
        public bool CanExecute(object parameter)
        {
            if (Interlocked.Read(ref _isExecuting) != 0)
            {
                return false;
            }

            return _canExecute(parameter as TParameter);
        }

        /// <summary>Runs <see cref="_execute"/>, marking the command busy for the duration and showing a message box on failure.</summary>
        public async void Execute(object parameter)
        {
            Interlocked.Exchange(ref _isExecuting, 1);
            RaiseCanExecuteChanged();

            try
            {
                await _execute(parameter as TParameter);
            }
            catch (Exception excp)
            {
                //todo log
                System.Windows.MessageBox.Show(
                    excp.Message
                    + Environment.NewLine
                    + excp.StackTrace,
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                    );
            }
            finally
            {
                Interlocked.Exchange(ref _isExecuting, 0);
                RaiseCanExecuteChanged();
            }
        }
    }
}