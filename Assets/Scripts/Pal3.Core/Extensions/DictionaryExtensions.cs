// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Extensions
{
    using System.Collections.Generic;

    public static class DictionaryExtensions
    {
        public static Dictionary<TValue, TKey> Reverse<TKey, TValue>(this IDictionary<TKey, TValue> source)
        {
            Dictionary<TValue, TKey> dictionary = new ();
            foreach (var entry in source)
            {
                dictionary.TryAdd(entry.Value, entry.Key);
            }
            return dictionary;
        }
    }
}