using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// Checks if the collection is null or empty.
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || !collection.Any();
        }

        /// <summary>
        /// Returns a random element from the list.
        /// </summary>
        public static T GetRandom<T>(this IList<T> list)
        {
            if (list.IsNullOrEmpty()) return default;
            return list[Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Shuffles the list using the Fisher-Yates algorithm.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary>
        /// Executes an action for each element in the collection.
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> collection, System.Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
        }

        /// <summary>
        /// Adds an item to the list only if it's not already present.
        /// </summary>
        public static bool AddUnique<T>(this IList<T> list, T item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
                return true;
            }
            return false;
        }
    }
}
