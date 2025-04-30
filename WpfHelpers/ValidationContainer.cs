using System.Collections.Generic;
using System.Linq;

namespace WpfHelpers
{
    /// <summary>
    /// Класс, инкапсулирующий в себе информацию об ошибках валидации вводимых данных
    /// </summary>
    public sealed class ValidationContainer
    {
        /// <summary>
        /// Контейнер "название контрола, где провалилась валидация" - "строка с ошибкой валидации"
        /// </summary>
        private readonly Dictionary<string, string> _errors;

        /// <summary>
        /// Конструктор
        /// </summary>
        public ValidationContainer()
        {
            _errors = new Dictionary<string, string>();
        }

        /// <summary>
        /// Добавить новую ошибку валидации
        /// Если есть ошибка от этого источника - перезатереть ее
        /// </summary>
        /// <param name="propertyName">Источник ошибки валидации</param>
        /// <param name="message">Сообщение об ошибке</param>
        public void AddError(string propertyName, string message)
        {
            _errors[propertyName] = message;
        }

        /// <summary>
        /// Очистка всех ошибок валидации
        /// </summary>
        /// <param name="propertyName"></param>
        public void RemoveErrors(string propertyName)
        {
            _errors.Remove(propertyName);
        }

        /// <summary>
        /// Извлечение ошибки валидации
        /// </summary>
        /// <param name="propertyName">Имя источника</param>
        /// <returns>Сообщение об ошибке, string.Empty - если ошибки от этого источника не обнаружено</returns>
        public string ExtractMessageForProperty(string propertyName)
        {
            var message = string.Empty;

            if (_errors.ContainsKey(propertyName))
            {
                message = _errors[propertyName];
                _errors.Remove(propertyName);
            }

            return message;
        }

        /// <summary>
        /// Возвращает первую ошибку в контейнере
        /// </summary>
        /// <returns>Первая ошибка в контейнере, string.Empty - если ошибок нет</returns>
        public string GetFirstError()
        {
            if(this.HasErrors())
            {
                return
                    this._errors.Values.First();
            }

            return string.Empty;
        }

        /// <summary>
        /// Проверяет наличие какой-либо ошибки в контейнере
        /// </summary>
        /// <returns>true - если ошибки в контейнере содержатся</returns>
        public bool HasErrors()
        {
            return _errors.Count != 0;
        }
    }
}
