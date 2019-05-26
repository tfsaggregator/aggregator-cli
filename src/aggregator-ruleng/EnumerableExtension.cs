using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace aggregator.Engine
{
    public static class EnumerableExtension
    {
        /// <summary>
        /// This method exists for backward compatibility reasons.
        /// </summary>
        /// <param name="this"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        public static string ToSeparatedString<T>(this IEnumerable<T> listOfT, char separator=',')
        {
            return listOfT
                .Aggregate("",
                    (s, i) => FormattableString.Invariant($"{s}{separator}{i}"))
                .Substring(1);
        }
    }
}
