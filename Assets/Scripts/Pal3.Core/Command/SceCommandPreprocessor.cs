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
    using Contract.Constants;

    public sealed class SceCommandPreprocessor : ISceCommandPreprocessor
    {
        private bool _isInitialized = false;
        private readonly Dictionary<Type, PropertyInfo[]> _sceCommandToActorIdPropertiesCache = new();

        public void Init()
        {
            if (_isInitialized) return;

            IEnumerable<Type> commandTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsClass && t.GetInterfaces().Contains(typeof(ICommand)));

            foreach (Type commandType in commandTypes)
            {
                PropertyInfo[] properties = commandType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                PropertyInfo[] actorIdProperties = properties.Where(
                    prop => Attribute.IsDefined(prop, typeof(SceActorIdAttribute))).ToArray();

                // Skip if no actorIdProperties
                if (actorIdProperties is not {Length: > 0}) continue;

                // Sanity checks
                foreach (PropertyInfo actorIdProperty in actorIdProperties)
                {
                    // Sanity check: throw if actorIdProperty is not int
                    if (actorIdProperty.PropertyType != typeof(int))
                    {
                        throw new InvalidDataContractException(
                            $"Property [{actorIdProperty.Name}] of command [{commandType.Name}] " +
                            $"should be [int] when marked with [{nameof(SceActorIdAttribute)}]");
                    }

                    // Sanity check: throw if actorIdProperty is read-only
                    if (!actorIdProperty.CanWrite)
                    {
                        throw new InvalidOperationException(
                            $"Property [{actorIdProperty.Name}] of command [{commandType.Name}] " +
                            $"should be writable when marked with [{nameof(SceActorIdAttribute)}]");
                    }
                }

                _sceCommandToActorIdPropertiesCache[commandType] = actorIdProperties;
            }

            _isInitialized = true;
        }

        public void Process(ICommand command, int currentPlayerActorId)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    $"[{nameof(SceCommandPreprocessor)}] is not initialized. " +
                    $"Please call [{nameof(Init)}] before using it.");
            }

            Type commandType = command.GetType();

            if (_sceCommandToActorIdPropertiesCache.TryGetValue(commandType, out PropertyInfo[] actorIdProperties))
            {
                foreach (PropertyInfo actorIdProperty in actorIdProperties)
                {
                    // Set actorIdProperty to current player actor id if it is PlayerActorVirtualID (-1)
                    if (actorIdProperty.GetValue(command) is ActorConstants.PlayerActorVirtualID)
                    {
                        actorIdProperty.SetValue(command, currentPlayerActorId);
                    }
                }
            }
        }
    }
}