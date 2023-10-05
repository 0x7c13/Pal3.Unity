// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Utilities
{
    public interface ITransactionalKeyValueStore
    {
        void Set<T>(string key, T value);

        bool TryGet<T>(string key, out T value);

        public void DeleteKey(string key);

        void Save();
    }
}