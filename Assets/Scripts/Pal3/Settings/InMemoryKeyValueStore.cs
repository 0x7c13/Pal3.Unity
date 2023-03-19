﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Settings
{
    using System.Collections.Generic;

    public sealed class InMemoryKeyValueStore : ITransactionalKeyValueStore
    {
        private readonly Dictionary<string, object> _store = new ();

        public void Set<T>(string key, T value)
        {
            _store[key] = value;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_store.TryGetValue(key, out var obj))
            {
                value = (T)obj;
                return true;
            }

            value = default;
            return false;
        }

        public void DeleteKey(string key)
        {
            if (_store.ContainsKey(key))
            {
                _store.Remove(key);
            }
        }

        public void Save()
        {
            // Do nothing
        }
    }
}