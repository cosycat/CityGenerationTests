
using System;

namespace System.Collections.Generic
{
    /// <summary>
    /// Source: https://gist.github.com/shoffing/9a4fb8fe6ffbe0afd2173faab22419fd
    /// </summary>
    public static class MinMaxBy
    {
        public static T MinBy<T>(this IEnumerable<T> list, Func<T, IComparable> selector)
        {
            IComparable lowest = null;
            T result = default(T);
            foreach (T elem in list)
            {
                var currElemVal = selector(elem);
                if (lowest == null || currElemVal.CompareTo(lowest) < 0)
                {
                    lowest = currElemVal;
                    result = elem;
                }
            }
            return result;
        }

        public static T MaxBy<T>(this IEnumerable<T> list, Func<T, IComparable> selector)
        {
            IComparable highest = null;
            T result = default(T);
            foreach (T elem in list)
            {
                var currElemVal = selector(elem);
                if (highest == null || currElemVal.CompareTo(highest) > 0)
                {
                    highest = currElemVal;
                    result = elem;
                }
            }
            return result;
        }
    }
}