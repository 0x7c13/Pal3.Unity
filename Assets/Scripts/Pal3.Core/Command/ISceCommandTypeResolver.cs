// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command
{
    using System;

    /// <summary>
    /// Interface for resolving the type of a SceCommand based on its ID and user variable mask.
    /// </summary>
    public interface ISceCommandTypeResolver
    {
        /// <summary>
        /// Get SceCommand Type for the given command id.
        /// </summary>
        /// <param name="commandId">SceCommand Id</param>
        /// <param name="userVariableMask">User variable mask</param>
        /// <returns>Type of the command, null if not found</returns>
        public Type GetType(ushort commandId, ushort userVariableMask);
    }
}