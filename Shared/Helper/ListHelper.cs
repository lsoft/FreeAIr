using System;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.Shared.Helper
{
    /// <summary>Small collection/LINQ extension helpers used across the codebase (indexing, filtering, batching, membership checks) that aren't already covered by <see cref="System.Linq.Enumerable"/>.</summary>
    public static class ListHelper
    {
        /// <summary>Returns the n-th element (0-based), throwing if the sequence is shorter.</summary>
        public static T Nth<T>(this IEnumerable<T> c, int n)
        {
            return c.Skip(n).First();
        }

        /// <summary>Returns the n-th element (0-based), or default if the sequence is shorter.</summary>
        public static T? NThOrDefault<T>(this IEnumerable<T> c, int n)
        {
            return c.Skip(n).FirstOrDefault();
        }

        /// <summary>Returns the second element, throwing if the sequence has fewer than two.</summary>
        public static T Second<T>(this IEnumerable<T> c)
        {
            return c.Skip(1).First();
        }

        /// <summary>Returns the second element, or default if the sequence has fewer than two.</summary>
        public static T? SecondOrDefault<T>(this IEnumerable<T> c)
        {
            return c.Skip(1).FirstOrDefault();
        }

        /// <summary>Filters an <see cref="IReadOnlyList{T}"/> into a new list, same as <c>Where(...).ToList()</c> but without the LINQ allocation chain.</summary>
        public static List<T> FindAll<T>(
            this IReadOnlyList<T> list,
            Func<T, bool> predicate
            )
        {
            if (list is null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var result = new List<T>();

            foreach (var i in list)
            {
                if (predicate(i))
                {
                    result.Add(i);
                }
            }

            return result;
        }

        /// <summary>Returns a new list with all elements matching <paramref name="selector"/> removed (the source is left untouched).</summary>
        public static IReadOnlyList<T> RemoveAll<T>(
            this IEnumerable<T> source,
            Func<T, bool> selector
            )
        {
            var list = new List<T>();

            foreach (var s in source)
            {
                if (selector(s))
                {
                    continue;
                }

                list.Add(s);
            }

            return list;
        }

        /// <summary>Invokes <paramref name="action"/> for every element, same as a foreach loop written as a fluent call.</summary>
        public static void ForEach<T>(
            this IEnumerable<T> list,
            Action<T> action
            )
        {
            if (list is null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (var a in list)
            {
                action(a);
            }

        }

        /// <summary>Flattens each element into zero or more results via <paramref name="converter"/> and concatenates them, i.e. <c>SelectMany(...).ToList()</c>.</summary>
        public static List<T1> Collapse<T1, T2>(
            this IEnumerable<T2> list,
            Func<T2, IEnumerable<T1>> converter
            )
        {
            if (list is null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (converter is null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            var result = new List<T1>();

            foreach (var a in list)
            {
                foreach (var b in converter(a))
                {
                    result.Add(b);
                }
            }

            return result;
        }

        /// <summary>Returns a new list with the elements randomly reordered (Fisher-Yates-style swap pass).</summary>
        public static List<T> Shuffle<T>(
            this IEnumerable<T> list
            )
        {
            var rnd = new Random(
                BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)
                );

            var result = new List<T>(list);

            for (var i = 0; i < result.Count - 1; i++)
            {
                if (rnd.Next() >= 0.5f)
                {
                    var newIndex = rnd.Next(result.Count);

                    var tmp = result[i];
                    result[i] = result[newIndex];
                    result[newIndex] = tmp;
                }
            }

            return result;
        }

        /// <summary>Maps every element via <paramref name="converter"/> into a pre-sized list, i.e. <c>Select(...).ToList()</c> without re-growing the buffer.</summary>
        public static List<T2> ConvertAll<T1, T2>(
            this IReadOnlyList<T1> list,
            Func<T1, T2> converter
            )
        {
            if (list is null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (converter is null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            var result = new List<T2>(list.Count);

            for (var a = 0; a < list.Count; a++)
            {
                result.Add(converter(list[a]));
            }

            return result;
        }


        /// <summary>Converts each element to a string via <paramref name="converter"/> and joins them with <paramref name="separator"/> (defaults to <see cref="Environment.NewLine"/>).</summary>
        public static string Join<T>(
            this IEnumerable<T> list,
            Func<T, string> converter,
            string? separator = null
            )
        {
            if (separator is null)
            {
                separator = Environment.NewLine;
            }

            return string.Join(separator, list.Select(a => converter(a)));
        }

        /// <summary>Membership check readable as <c>v.NotIn(array)</c>; the inverse of <see cref="In{T}(T, IEnumerable{T})"/>.</summary>
        public static bool NotIn<T>(
            this T v,
            IEnumerable<T> array
            )
        {
            return
                !array.Contains(v);
        }

        /// <summary>Membership check readable as <c>v.In(array)</c>, i.e. <c>array.Contains(v)</c> with the receiver flipped.</summary>
        public static bool In<T>(
            this T v,
            IEnumerable<T> array
            )
        {
            return
                array.Contains(v);
        }

        /// <summary>Params-array overload of <see cref="NotIn{T}(T, IEnumerable{T})"/> for inline literal lists.</summary>
        public static bool NotIn<T>(
            this T v,
            params T[] array
            )
        {
            return
                !array.Contains(v);
        }

        /// <summary>Params-array overload of <see cref="In{T}(T, IEnumerable{T})"/> for inline literal lists.</summary>
        public static bool In<T>(
            this T v,
            params T[] array
            )
        {
            return
                array.Contains(v);
        }

        /// <summary>Membership check using a custom <paramref name="comparer"/> instead of the default equality.</summary>
        public static bool NotIn<T>(
            this T v,
            IEqualityComparer<T> comparer,
            params T[] array
            )
        {
            return
                !array.Contains(v, comparer);
        }

        /// <summary>Membership check using a custom <paramref name="comparer"/> instead of the default equality.</summary>
        public static bool In<T>(
            this T v,
            IEqualityComparer<T> comparer,
            params T[] array
            )
        {
            return
                array.Contains(v, comparer);
        }

        /// <summary>Batches an <see cref="IEnumerator{T}"/> into fixed-size chunks (last chunk may be smaller), consuming the enumerator as it goes.</summary>
        public static IEnumerable<List<T>> Split<T>(
            this IEnumerator<T> list,
            int splitCount
            )
        {
            if (splitCount <= 0)
            {
                throw new ArgumentException("splitCount <= 0");
            }

            var nextList = new List<T>();

            while (list.MoveNext())
            {
                var item = list.Current;

                nextList.Add(item);

                if (nextList.Count == splitCount)
                {
                    yield return nextList;

                    nextList = new List<T>();
                }
            }

            //if (list.Count % splitCount != 0)
            if (nextList.Count > 0)
            {
                yield return nextList;
            }
        }

        /// <summary><see cref="IEnumerable{T}"/> overload of <see cref="Split{T}(IEnumerator{T}, int)"/> for batching into fixed-size chunks.</summary>
        public static IEnumerable<List<T>> Split<T>(
            this IEnumerable<T> list,
            int splitCount
            )
        {
            if (splitCount <= 0)
            {
                throw new ArgumentException("splitCount <= 0");
            }

            var nextList = new List<T>();

            foreach (var item in list)
            {
                nextList.Add(item);

                if (nextList.Count == splitCount)
                {
                    yield return nextList;

                    nextList = new List<T>();
                }
            }

            //if (list.Count % splitCount != 0)
            if (nextList.Count > 0)
            {
                yield return nextList;
            }
        }

    }

}
