using System;
using System.Collections.Generic;
using System.Linq;

namespace OLS.Casy.Base
{
    public static class CollectionHelpers
    {
        /// <summary>
        /// Extension method for arrays of any type to extract a subarray.
        /// </summary>
        /// <typeparam name="T">Type of the array items</typeparam>
        /// <param name="data">The array</param>
        /// <param name="index">Index, where the subsarray shall start</param>
        /// <param name="length">Length of the subarray</param>
        /// <returns>The requested subarray</returns>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            if(data == null)
            {
                throw new ArgumentNullException("data");
            }

            if(index < 0 || index > data.Length - 1)
            {
                return new T[0];
            }

            if (length < 0)
            {
                return null;
            }

            if (index + length > data.Length)
            {
                //throw new ArgumentOutOfRangeException("length");
                return new T[0];
            }

            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static bool UnorderedEqual<T>(this IEnumerable<T> a, IEnumerable<T> b)
        {
            if(b == null)
            {
                throw new ArgumentNullException("b");
            }

            // 1
            // Require that the counts are equal
            if (a.Count() != b.Count())
            {
                return false;
            }
            // 2
            // Initialize new Dictionary of the type
            Dictionary<T, int> d = new Dictionary<T, int>();
            // 3
            // Add each key's frequency from collection A to the Dictionary
            foreach (T item in a)
            {
                int c;
                if (d.TryGetValue(item, out c))
                {
                    d[item] = c + 1;
                }
                else
                {
                    d.Add(item, 1);
                }
            }
            // 4
            // Add each key's frequency from collection B to the Dictionary
            // Return early if we detect a mismatch
            foreach (T item in b)
            {
                int c;
                if (d.TryGetValue(item, out c))
                {
                    if (c == 0)
                    {
                        return false;
                    }
                    else
                    {
                        d[item] = c - 1;
                    }
                }
                else
                {
                    // Not in dictionary
                    return false;
                }
            }
            // 5
            // Verify that all frequencies are zero
            foreach (int v in d.Values)
            {
                if (v != 0)
                {
                    return false;
                }
            }
            // 6
            // We know the collections are equal
            return true;
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
