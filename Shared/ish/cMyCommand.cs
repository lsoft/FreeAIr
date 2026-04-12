#nullable disable
namespace FreeAIr.Shared.ish
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    public class cMyCommand : ICommand
    {
        public Predicate<object> CanExecuteDelegate { get; set; }
        public Action<object> ExecuteDelegate { get; set; }

        public bool CanExecute(object parameter) => (CanExecuteDelegate != null) ? CanExecuteDelegate(parameter) : true;

        public event EventHandler CanExecuteChanged
        {
            add  => CommandManager.RequerySuggested += value; 
            remove => CommandManager.RequerySuggested -= value; 
        }
        public void Execute(object parameter) => ExecuteDelegate?.Invoke(parameter);

        public static cMyCommand Create(Action<object> executeDelegate) => new() { ExecuteDelegate = executeDelegate };
        public static cMyCommand Create(Action executeDelegate) => new() { ExecuteDelegate = (o) => executeDelegate?.Invoke() };
        public static cMyCommand Create(Func<bool> executeDelegate) => new() { ExecuteDelegate = (o) => executeDelegate?.Invoke() };
        public static cMyCommand Create(Func<Task> executeDelegate) =>new() { ExecuteDelegate = async (o) => await executeDelegate() };
    }
}
