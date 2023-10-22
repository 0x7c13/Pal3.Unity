// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class SceCommandTypeResolver
    {
        private static bool _isInitialized;
        private static readonly Dictionary<uint, Type> SceCommandTypeCache = new ();

        private static void Init()
        {
            if (_isInitialized) return;

            // All SceCommand types are in Pal3.Core.Command.SceCommands namespace
            // So we can just search for all types in current assembly.
            IEnumerable<Type> commandTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsClass && t.GetInterfaces().Contains(typeof(ICommand)));

            foreach (Type commandType in commandTypes)
            {
                if (commandType.GetCustomAttribute(typeof(SceCommandAttribute)) is SceCommandAttribute attribute)
                {
                    SceCommandTypeCache[GetHashCode(attribute.CommandId, attribute.UserVariableMask)] = commandType;
                }
            }

            _isInitialized = true;
        }

        private static uint GetHashCode(ushort commandId, ushort userVariableMask)
        {
            // 16 bits for command id, 16 bits for user variable mask
            return ((uint)commandId << 16) | (uint)userVariableMask;
        }

        /// <summary>
        /// Get SceCommand Type for the given command id.
        /// </summary>
        /// <param name="commandId">SceCommand Id</param>
        /// <param name="userVariableMask">User variable mask</param>
        /// <returns>Type of the command, null if not found</returns>
        public static Type GetType(ushort commandId, ushort userVariableMask)
        {
            if (!_isInitialized) Init();

            uint hashCode = GetHashCode(commandId, userVariableMask);

            return SceCommandTypeCache.TryGetValue(hashCode, out Type type) ? type : null; // not found
        }
    }
}
