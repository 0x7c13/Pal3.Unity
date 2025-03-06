// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
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
            foreach (KeyValuePair<TKey, TValue> entry in source)
            {
                dictionary.TryAdd(entry.Value, entry.Key);
            }
            return dictionary;
        }
    }
}