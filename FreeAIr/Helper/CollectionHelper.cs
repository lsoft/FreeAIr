using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class CollectionHelper
    {
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
