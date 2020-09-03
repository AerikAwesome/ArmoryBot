using System;
using System.Collections.Generic;
using System.Text;

namespace ArmoryBot.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> TakeUntilIncluding<T>(this IEnumerable<T> list, Func<T, bool> predicate)
        {
            foreach (var el in list)
            {
                yield return el;
                if (!predicate(el))
                    yield break;
            }
        }
    }
}
