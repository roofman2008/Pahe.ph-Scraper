using System;
using System.Collections.Generic;

namespace PaheScrapper.Helpers
{
    public static class LinqHelpers
    {
        private static Random rng = new Random();

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            var list = new List<T>(source);
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return list;
        }
    }
}