using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Small collection extensions missing from `ICollection&lt;T&gt;` and
    /// <see cref="CaptureCollection"/>: index lookups by predicate, and ordering regex captures.
    /// </summary>
    public static class CollectionHelper
    {
        /// <summary>
        /// The index of the last item matching the predicate, or -1 when none match.
        /// </summary>
        public static int FindLastIndex<T>(
            this ICollection<T> items,
            Func<T, bool> predicate
            )
        {
            var result = -1;
            var index = 0;
            foreach (var item in items)
            {
                if (predicate(item))
                {
                    result = index;
                }

                index++;
            }

            return result;
        }

        /// <summary>
        /// The index of the first item matching the predicate, or -1 when none match.
        /// </summary>
        public static int FindIndex<T>(
            this ICollection<T> items,
            Func<T, bool> predicate
            )
        {
            var index = 0;
            foreach (var item in items)
            {
                if (predicate(item))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        /// <summary>
        /// Orders a regex <see cref="CaptureCollection"/> by the given key, since it does not
        /// implement `IEnumerable&lt;Capture&gt;` on its own.
        /// </summary>
        public static IEnumerable<Capture> OrderBy<TKey>(
            this CaptureCollection collection,
            Func<Capture, TKey> keySelector
            )
        {
            var list = new List<Capture>(collection.Count);
            for (var i = 0; i < collection.Count; i++)
            {
                list.Add(collection[i]);
            }

            return list.OrderBy(keySelector);
        }
    }
}
