// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Core.Command;
    using Core.Contract.Constants;
    using Core.Utilities;
    using GamePlay;

    public sealed class CommandPreprocessor
    {
        private readonly PlayerActorManager _playerActorManager;

        private readonly Dictionary<Type, PropertyInfo[]> _sceCommandToActorIdPropertiesCache = new();

        public CommandPreprocessor(PlayerActorManager playerActorManager)
        {
            _playerActorManager = Requires.IsNotNull(playerActorManager, nameof(playerActorManager));
            BuildTypeCache();
        }

        private void BuildTypeCache()
        {
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
        }

        public void Process(ref ICommand command)
        {
            Type commandType = command.GetType();

            if (_sceCommandToActorIdPropertiesCache.TryGetValue(commandType, out PropertyInfo[] actorIdProperties))
            {
                foreach (PropertyInfo actorIdProperty in actorIdProperties)
                {
                    if (actorIdProperty.GetValue(command) is ActorConstants.PlayerActorVirtualID)
                    {
                        actorIdProperty.SetValue(command, (int)_playerActorManager.GetPlayerActor());
                    }
                }
            }
        }
    }
}