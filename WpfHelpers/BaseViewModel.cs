using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace WpfHelpers
{
    /// <summary>
    /// Класс, реализующий базовую функциональность viewmodel идеологии MVVM
    /// </summary>
    public class BaseViewModel : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// Событие изменения свойства
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Тестовое свойство бросания исключения в случае не нахождения биндинга
        /// </summary>
        protected bool _throwOnInvalidPropertyName;

        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="dispatcher">Диспатчер WPF</param>
        protected BaseViewModel()
        {
        }

        /// <summary>
        /// Активация евента изменения бинденого свойства
        /// </summary>
        protected void OnPropertyChanged()
        {
            OnPropertyChanged(string.Empty);
        }

        /// <summary>
        /// Активация евента изменения бинденого свойства
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.VerifyPropertyName(propertyName);

            var handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Debug-only check that <paramref name="propertyName"/> matches a real public property on
        /// this view model, catching typos in <see cref="OnPropertyChanged(string)"/> calls early.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                // Verify that the property name matches a real,  
                // public, instance property on this object.
                var propertiesList = TypeDescriptor.GetProperties(this);
                if (propertiesList[propertyName] == null)
                {
                    var msg = "Invalid property name: " + propertyName;

                    if (this._throwOnInvalidPropertyName)
                    {
                        throw new Exception(msg);
                    }

                    Debug.Fail(msg);
                }
            }
        }

        /// <summary>Override point for derived view models to release resources; called by <see cref="Dispose"/>.</summary>
        protected virtual void DisposeViewModel()
        {

        }

        #region Implementation of IDisposable

        /// <summary>Calls <see cref="DisposeViewModel"/>.</summary>
        public void Dispose()
        {
            this.DisposeViewModel();
        }

        #endregion
    }
}
