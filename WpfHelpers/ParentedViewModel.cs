namespace WpfHelpers
{
    /// <summary>
    /// Базовый класс вью-модели, которая может работать в стеке с родительским объектом
    /// </summary>
    /// <typeparam name="TFinish">Тип результата, который формирует вью-модель по окончании своей работы</typeparam>
    public class ParentedViewModel<TFinish> : BaseViewModel
    {
        /// <summary>
        /// Родительский объект
        /// </summary>
        protected readonly IViewModelParent<TFinish> _parent;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="parent">Родительский объект</param>
        public ParentedViewModel(
            IViewModelParent<TFinish> parent
            )
        {
            _parent = parent;
        }
    }
}
