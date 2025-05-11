using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObject.Helper
{
    public static class CollectionHelper
    {
        public static bool IsCollectionEquals<T>(
            IReadOnlyList<T>? first,
            IReadOnlyList<T>? second
            )
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }
            if (first is null && second is null)
            {
                return true;
            }
            if (first is null)
            {
                return false;
            }
            if (second is null)
            {
                return false;
            }

            for (var i = 0; i < first.Count; i++)
            {
                var fs = first[i];
                var ss = second[i];

                if (ReferenceEquals(first, second))
                {
                    continue;
                }
                if (first is null && second is null)
                {
                    continue;
                }
                if (first is null)
                {
                    return false;
                }
                if (second is null)
                {
                    return false;
                }

                if (!fs.Equals(ss))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
