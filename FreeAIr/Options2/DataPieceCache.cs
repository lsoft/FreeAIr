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
        /// Обновление значения не атомарно (внутри есть await), а настройки читаются
        /// одновременно из UI и из фоновых потоков. Без этого семафора два читателя способны
        /// оставить подпись от одного чтения, а значение - от другого.
        /// </summary>
        private readonly NonDisposableSemaphoreSlim _semaphore = new(1, 1);

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
        /// <returns>Копия кэшированного значения, либо null, если значение получить не удалось.</returns>
        public async Task<T?> GetCachedValueAsync<T>()
            where T : class
        {
            await _semaphore.WaitAsync();
            try
            {
                var newSignature = _signatureProvider(Key);
                if (_oldSignature is null || _oldSignature.CompareTo(newSignature) != 0)
                {
                    var converted = await _converter(Key);
                    if (converted is null)
                    {
                        //конвертер не смог прочитать значение (например, файл исчез между
                        //проверкой существования и чтением); подпись не запоминаем, чтобы
                        //следующий вызов попробовал ещё раз
                        return null;
                    }

                    _cached = converted;
                    _oldSignature = newSignature;
                }

                return (T)_cached.Clone();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
