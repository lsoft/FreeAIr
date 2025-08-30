using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    /// <summary>
    /// Класс для кэширования значений с возможностью асинхронного получения данных.
    /// </summary>
    public static class DataPieceCache
    {
        /// <summary>
        /// Словарь для хранения кэшированных значений.
        /// </summary>
        private static readonly Dictionary<string, FileCacheEntry> _dict = new();

        /// <summary>
        /// Объект для синхронизации доступа к словарю.
        /// </summary>
        private static readonly object _locker = new();

        /// <summary>
        /// Асинхронно получает значение из кэша или обновляет его, если необходимо.
        /// </summary>
        /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
        /// <param name="key">Ключ для получения значения.</param>
        /// <param name="statusChecker">Функция для проверки статуса ключа.</param>
        /// <param name="signatureProvider">Функция для получения подписи ключа.</param>
        /// <param name="converter">Функция для преобразования ключа в значение.</param>
        /// <returns>Кэшированное значение или null, если ключ не найден или статус не соответствует.</returns>
        public static async Task<T?> GetValueAsync<T>(
            string key,
            Func<string, bool> statusChecker,
            Func<string, IComparable> signatureProvider,
            Func<string, Task<ICloneable>> converter
            ) where T : class
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (statusChecker is null)
            {
                throw new ArgumentNullException(nameof(statusChecker));
            }

            if (converter is null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            if (signatureProvider is null)
            {
                throw new ArgumentNullException(nameof(signatureProvider));
            }

            if (!statusChecker(key))
            {
                lock (_locker)
                {
                    _dict.Remove(key);
                }
                return null;
            }

            FileCacheEntry entry;
            lock (_locker)
            {
                if (!_dict.TryGetValue(key, out entry))
                {
                    entry = new FileCacheEntry(key, signatureProvider, converter);
                    _dict[key] = entry;
                }
            }

            var result = await entry.GetCachedValueAsync<T>();
            return result;
        }
    }

    /// <summary>
    /// Представляет запись в кэше для файла.
    /// </summary>
    public sealed class FileCacheEntry
    {
        /// <summary>
        /// Функция для получения подписи ключа.
        /// </summary>
        private readonly Func<string, IComparable> _signatureProvider;

        /// <summary>
        /// Функция для преобразования ключа в значение.
        /// </summary>
        private readonly Func<string, Task<ICloneable>> _converter;

        /// <summary>
        /// Ключ для получения значения.
        /// </summary>
        public string Key
        {
            get;
        }

        /// <summary>
        /// Старая подпись ключа.
        /// </summary>
        private IComparable? _oldSignature;

        /// <summary>
        /// Кэшированное значение.
        /// </summary>
        private ICloneable? _cached;

        /// <summary>
        /// Инициализирует новый экземпляр класса FileCacheEntry.
        /// </summary>
        /// <param name="key">Ключ для получения значения.</param>
        /// <param name="signatureProvider">Функция для получения подписи ключа.</param>
        /// <param name="converter">Функция для преобразования ключа в значение.</param>
        public FileCacheEntry(
            string key,
            Func<string, IComparable> signatureProvider,
            Func<string, Task<ICloneable>> converter
            )
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (signatureProvider is null)
            {
                throw new ArgumentNullException(nameof(signatureProvider));
            }

            if (converter is null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            Key = key;
            _signatureProvider = signatureProvider;
            _converter = converter;
        }

        /// <summary>
        /// Асинхронно получает кэшированное значение или обновляет его, если необходимо.
        /// </summary>
        /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
        /// <returns>Кэшированное значение.</returns>
        public async Task<T?> GetCachedValueAsync<T>()
            where T : class
        {
            var newSignature = _signatureProvider(Key);
            if (_oldSignature is null || _oldSignature.CompareTo(newSignature) != 0)
            {
                _cached = await _converter(Key);
                _oldSignature = newSignature;
            }

            return (T)_cached.Clone();
        }
    }
}
