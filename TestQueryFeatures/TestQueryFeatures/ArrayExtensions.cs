using System;
using System.Collections.Generic;
using System.Text;

namespace TestQueryFeatures
{
    internal static class ArrayExtensions
    {
        public static IEnumerable<T> EnumerateColumnsThenRows<T>(this T[,] items)
        {
            for (int i = items.GetLowerBound(1); i<= items.GetUpperBound(1); i++)
            {
                for (int j = items.GetLowerBound(0); j <= items.GetUpperBound(0); j++)
                {
                    var item = items[j, i];
                    yield return item;
                }
            }
        }
    }
}
