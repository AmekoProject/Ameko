// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;

namespace Ameko.Utilities;

public static class ICollectionExtensions
{
    /// <summary>
    /// Add <paramref name="elements"/> to a <paramref name="collection"/>
    /// </summary>
    /// <param name="collection">Collection to add items to</param>
    /// <param name="elements">Items to add to the collection</param>
    /// <typeparam name="T">Type of collection</typeparam>
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> elements)
    {
        foreach (var element in elements)
            collection.Add(element);
    }
}
