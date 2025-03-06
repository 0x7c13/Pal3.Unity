// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Extensions
{
    using Core.Abstraction;

    public static class GameEntityExtensions
    {
        /// <summary>
        /// Checks if the game entity is null or disposed.
        /// </summary>
        public static bool IsNullOrDisposed(this IGameEntity entity)
        {
            return entity == null || entity.IsNativeObjectDisposed;
        }
    }
}