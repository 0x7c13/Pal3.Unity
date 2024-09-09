// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    public sealed class SceCommandTypeResolver : ISceCommandTypeResolver
    {
        private static bool _isInitialized;
        private static readonly Dictionary<uint, Type> SceCommandTypeCache = new ();

        public void Init()
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
                    ushort userVariableMask = CalculateUserVariableMask(commandType);
                    uint hashCode = GetHashCode(attribute.CommandId, userVariableMask);
                    SceCommandTypeCache[hashCode] = commandType;
                }
            }

            _isInitialized = true;
        }

        private static ushort CalculateUserVariableMask(Type commandType)
        {
            PropertyInfo[] propertyInfos = commandType.GetProperties();

            ushort userVariableMask = 0;
            for (int i = 0; i < propertyInfos.Length; i++)
            {
                PropertyInfo propertyInfo = propertyInfos[i];

                // Skip if property is not marked with SceUserVariableAttribute
                if (propertyInfo.GetCustomAttribute(typeof(SceUserVariableAttribute)) == null) continue;

                // Sanity check: throw if property is not ushort since
                // user variable is always ushort (2-byte UInt16)
                if (propertyInfo.PropertyType != typeof(ushort))
                {
                    throw new InvalidDataContractException(
                        $"Property [{propertyInfo.Name}] of command [{commandType.Name}] " +
                        $"should be ushort when marked with [{nameof(SceUserVariableAttribute)}]");
                }

                // Bitwise OR to set the corresponding bit to 1 according to property index
                // to indicate that the property is user variable
                userVariableMask |= (ushort)(1 << i);
            }
            return userVariableMask;
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
        public Type GetType(ushort commandId, ushort userVariableMask)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    $"[{nameof(SceCommandTypeResolver)}] is not initialized, " +
                    $"call [{nameof(Init)}] first");
            }

            uint hashCode = GetHashCode(commandId, userVariableMask);
            return SceCommandTypeCache.GetValueOrDefault(hashCode);
        }
    }
}
