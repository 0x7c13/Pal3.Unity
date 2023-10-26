// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Script
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a store for variables of type <typeparamref name="TValue"/>
    /// associated with keys of type <typeparamref name="TVariable"/>.
    /// </summary>
    /// <typeparam name="TVariable">The type of the variable keys.</typeparam>
    /// <typeparam name="TValue">The type of the variable values.</typeparam>
    public interface IUserVariableStore<TVariable, TValue> :
        IEnumerable<KeyValuePair<TVariable, TValue>>
    {
        /// <summary>
        /// Sets the value of the variable associated with the specified key.
        /// </summary>
        /// <param name="variable">The key of the variable to set.</param>
        /// <param name="value">The value to set.</param>
        public void Set(TVariable variable, TValue value);

        /// <summary>
        /// Gets the value of the variable associated with the specified key.
        /// </summary>
        /// <param name="variable">The key of the variable to get.</param>
        /// <returns>The value of the variable associated with the specified key.</returns>
        public TValue Get(TVariable variable);
    }
}