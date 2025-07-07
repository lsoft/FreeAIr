using Microsoft.Build.Framework;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class DataPieceCache
    {
        private static readonly Dictionary<string, FileCacheEntry> _dict = new();
        private static readonly object _locker = new();

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

    public sealed class FileCacheEntry
    {
        private readonly Func<string, IComparable> _signatureProvider;
        private readonly Func<string, Task<ICloneable>> _converter;

        public string Key
        {
            get;
        }

        private IComparable? _oldSignature;
        private ICloneable? _cached;

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
