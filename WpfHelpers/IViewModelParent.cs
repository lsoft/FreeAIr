using System.Windows.Threading;

namespace WpfHelpers
{
    /// <summary>
    /// Интерфейс, который объект передает своим дочерним вью-моделям
    /// Через этот интерфейс вью-модель уведомляет родительский объект, что она завершилась
    /// </summary>
    /// <typeparam name="TFinish">Тип параметра, который передает дочерняя вью-модель в качестве результата работы</typeparam>
    public interface IViewModelParent<TFinish>
    {
        /// <summary>
        /// Родительский объект должен содержать диспатчер WPF, чтобы дочерняя вью-модель могла его использовать
        /// </summary>
        Dispatcher Dispatcher
        {
            get;
        }

        /// <summary>
        /// Метод завершения вью-модели
        /// </summary>
        /// <param name="result">Результат работы вью-модели</param>
        void Finish(TFinish result);
    }
}