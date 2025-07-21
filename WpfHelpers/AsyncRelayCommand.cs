using System.Windows.Input;

namespace WpfHelpers
{
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

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

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

        protected abstract Task ExecuteInternalAsync(object parameter);
        protected virtual bool CanExecuteInternal(object parameter) => true;

    }

    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Predicate<object> _canExecute;

        private long _isExecuting;

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

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public bool CanExecute(object parameter)
        {
            if (Interlocked.Read(ref _isExecuting) != 0)
            {
                return false;
            }

            return _canExecute(parameter);
        }

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

    public sealed class AsyncRelayCommand<TParameter> : ICommand
        where TParameter : class
    {
        private readonly Func<TParameter, Task> _execute;
        private readonly Func<TParameter, bool> _canExecute;

        private long _isExecuting;

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

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public bool CanExecute(object parameter)
        {
            if (Interlocked.Read(ref _isExecuting) != 0)
            {
                return false;
            }

            return _canExecute(parameter as TParameter);
        }

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