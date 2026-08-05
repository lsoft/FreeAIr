using System.Diagnostics;
using System.Windows.Input;

namespace WpfHelpers
{
    /// <summary>
    /// A command whose sole purpose is to 
    /// relay its functionality to other
    /// objects by invoking delegates. The
    /// default return value for the CanExecute
    /// method is 'true'.
    /// </summary>
    public sealed class RelayCommand<T> : ICommand
        where T : class
    {
        #region Fields

        /// <summary>The delegate invoked when the command runs, receiving the parameter cast to <typeparamref name="T"/>.</summary>
        readonly Action<T> _execute;
        /// <summary>Optional predicate consulted by <see cref="CanExecute"/>; null means always executable.</summary>
        readonly Predicate<T> _canExecute;

        #endregion // Fields

        #region Constructors

        /// <summary>
        /// Creates a new command that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public RelayCommand(Action<T> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Creates a new command.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }

        #endregion // Constructors

        #region ICommand Members

        /// <summary>False if <paramref name="parameter"/> isn't a <typeparamref name="T"/>, otherwise delegates to the optional can-execute predicate (default always true).</summary>
        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            if ((parameter as T) == null)
            {
                return false;
            }

            return _canExecute == null || _canExecute(parameter as T);
        }

        /// <summary>Forwards to WPF's <see cref="CommandManager.RequerySuggested"/> so bound controls re-evaluate <see cref="CanExecute"/> automatically.</summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>Casts <paramref name="parameter"/> to <typeparamref name="T"/> and runs the execute delegate, showing a message box on failure.</summary>
        public void Execute(object parameter)
        {
            try
            {
                if ((parameter as T) == null)
                {
                    return;
                }

                _execute(parameter as T);
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
        }

        #endregion // ICommand Members
    }

    /// <summary>
    /// A command whose sole purpose is to 
    /// relay its functionality to other
    /// objects by invoking delegates. The
    /// default return value for the CanExecute
    /// method is 'true'.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        #region Fields

        /// <summary>The delegate invoked when the command runs.</summary>
        readonly Action<object> _execute;
        /// <summary>Optional predicate consulted by <see cref="CanExecute"/>; null means always executable.</summary>
        readonly Predicate<object> _canExecute;

        #endregion // Fields

        #region Constructors

        /// <summary>
        /// Creates a new command that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Creates a new command.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }

        #endregion // Constructors

        #region ICommand Members

        /// <summary>Delegates to the optional can-execute predicate (default always true).</summary>
        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>Forwards to WPF's <see cref="CommandManager.RequerySuggested"/> so bound controls re-evaluate <see cref="CanExecute"/> automatically.</summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>Runs the execute delegate, showing a message box on failure.</summary>
        public void Execute(object parameter)
        {
            try
            {
                _execute(parameter);
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
        }

        #endregion // ICommand Members
    }
}
