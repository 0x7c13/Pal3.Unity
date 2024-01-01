// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Utilities
{
    /// <summary>
    /// Interface for a transactional key-value store.
    /// </summary>
    public interface ITransactionalKeyValueStore
    {
        /// <summary>
        /// Sets the value of a key in the store.
        /// </summary>
        /// <typeparam name="T">The type of the value to set.</typeparam>
        /// <param name="key">The key to set.</param>
        /// <param name="value">The value to set.</param>
        public void Set<T>(string key, T value);

        /// <summary>
        /// Tries to get the value of a key in the store.
        /// </summary>
        /// <typeparam name="T">The type of the value to get.</typeparam>
        /// <param name="key">The key to get.</param>
        /// <param name="value">The value of the key, if found.</param>
        /// <returns>True if the key was found, false otherwise.</returns>
        public bool TryGet<T>(string key, out T value);

        /// <summary>
        /// Deletes a key from the store.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        public void DeleteKey(string key);

        /// <summary>
        /// Commits all changes made to the store.
        /// </summary>
        public void Commit();
    }
}