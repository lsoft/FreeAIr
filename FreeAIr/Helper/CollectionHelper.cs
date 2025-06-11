using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FreeAIr.Helper
{
    public static class CollectionHelper
    {
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
