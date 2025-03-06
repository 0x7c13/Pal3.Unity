// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Actor
{
    using System;
    using System.Collections.Generic;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Ini;
    using Data;
    using Engine.Logging;

    public enum ActorAnimationType
    {
        Vertex = 0,    // .mv3 animation
        Skeletal = 1,  // .msh + .mov animation
    }

    public abstract class ActorBase
    {
        public int Id { get; }
        public string Name { get; private set; }
        public bool IsActive { get; set; }

        public ActorAnimationType AnimationType { get; private set; }

        private readonly Dictionary<string, string> _actionNameToFileNameMap = new (StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _actionNameToMeshFileNameMap = new (StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _actionNameToMaterialFileNameMap = new (StringComparer.OrdinalIgnoreCase);

        private readonly GameResourceProvider _resourceProvider;

        protected ActorBase(GameResourceProvider resourceProvider, int id, string name)
        {
            _resourceProvider = resourceProvider;

            Id = id;
            Name = name;

            InitActorConfig(name);
        }

        private void InitActorConfig(string name)
        {
            ActorActionConfig defaultConfig = _resourceProvider.GetActorActionConfig(name, $"{name}.ini");

            // Add default config if it is MV3 animated
            if (defaultConfig is Mv3ActionConfig mv3Config)
            {
                AddActorConfig(mv3Config);
                AnimationType = ActorAnimationType.Vertex;
                return; // No need to continue since MV3 animated actor has only one action config
            }

            // Add default config if it is MOV animated
            if (defaultConfig is MovActionConfig)
            {
                AddActorConfig(defaultConfig);
            }

            // For MOV animated skeletal actor, additional action config files are defined as
            // 01.ini, 02.ini ...up tp 09.ini
            for (int i = 0; i <= 9; i++)
            {
                if (_resourceProvider.GetActorActionConfig(name, $"{i:D2}.ini")
                    is MovActionConfig movConfig)
                {
                    AddActorConfig(movConfig);
                }
            }

            AnimationType = ActorAnimationType.Skeletal;
        }

        private void AddActorConfig(ActorActionConfig config)
        {
            foreach (ActorAction action in config.ActorActions)
            {
                _actionNameToFileNameMap.TryAdd(action.ActionName, action.ActionFileName);
            }

            if (config is MovActionConfig movActionConfig)
            {
                foreach (ActorAction action in config.ActorActions)
                {
                    _actionNameToMeshFileNameMap.TryAdd(action.ActionName, movActionConfig.Actor.MeshFileName);
                    _actionNameToMaterialFileNameMap.TryAdd(action.ActionName, movActionConfig.Actor.MaterialFileName);
                }
            }
        }

        public void ChangeName(string name)
        {
            if (string.Equals(Name, name, StringComparison.Ordinal)) return;

            Name = name;

            // Re-init actor config since name is changed
            InitActorConfig(name);
        }

        public bool IsMainActor()
        {
            return Name.StartsWith("1"); // 101-110 are all main actor models
        }

        public bool IsMonsterActor()
        {
            return !Name.StartsWith("1") && !Name.StartsWith("2");
        }

        public string GetIdleAction()
        {
            if (IsMainActor())
            {
                return ActorConstants.ActionToNameMap[ActorActionType.Stand];
            }

            if (IsMonsterActor() && HasAction(ActorConstants.MonsterIdleAction))
            {
                return ActorConstants.MonsterIdleAction;
            }

            if (HasAction(ActorConstants.ActionToNameMap[ActorActionType.NpcStand1]))
            {
                return ActorConstants.ActionToNameMap[ActorActionType.NpcStand1];
            }

            if (HasAction(ActorConstants.ActionToNameMap[ActorActionType.NpcStand2]))
            {
                return ActorConstants.ActionToNameMap[ActorActionType.NpcStand2];
            }

            EngineLogger.LogError($"No default idle animation found for {Id}_{Name}");
            return ActorConstants.ActionToNameMap[ActorActionType.NpcStand1];
        }

        public string GetMovementAction(MovementMode mode)
        {
            if (IsMainActor())
            {
                return mode switch
                {
                    MovementMode.Walk => ActorConstants.ActionToNameMap[ActorActionType.Walk],
                    MovementMode.Run => HasAction(ActorConstants.ActionToNameMap[ActorActionType.Run])
                        ? ActorConstants.ActionToNameMap[ActorActionType.Run]
                        : ActorConstants.ActionToNameMap[ActorActionType.Walk],
                    MovementMode.StepBack => ActorConstants.ActionToNameMap[ActorActionType.StepBack],
                    _ => ActorConstants.ActionToNameMap[ActorActionType.Walk]
                };
            }

            if (IsMonsterActor() && HasAction(ActorConstants.MonsterWalkAction))
            {
                return ActorConstants.MonsterWalkAction;
            }

            return mode switch
            {
                MovementMode.Walk => ActorConstants.ActionToNameMap[ActorActionType.NpcWalk],
                MovementMode.Run => ActorConstants.ActionToNameMap[ActorActionType.NpcRun],
                _ => ActorConstants.ActionToNameMap[ActorActionType.NpcWalk]
            };
        }

        // TODO: Get weapon based on inventory context
        public string GetTagObjectName()
        {
            return ActorConstants.MainActorWeaponMap.GetValueOrDefault(Name);
        }

        public bool HasAction(string actionName)
        {
            return _actionNameToFileNameMap.ContainsKey(actionName);
        }

        public string GetActionFilePath(string actionName)
        {
            if (!_actionNameToFileNameMap.TryGetValue(actionName, out var actionFileName))
            {
                throw new ArgumentException($"Action file name not found for action name {actionName}");
            }

            return $"{FileConstants.GetActorFolderVirtualPath(Name)}{actionFileName}";
        }

        public string GetMeshFilePath(string actionName)
        {
            if (AnimationType != ActorAnimationType.Skeletal)
            {
                throw new InvalidOperationException("Msh file path is only available for skeletal animation type.");
            }

            if (!_actionNameToMeshFileNameMap.TryGetValue(actionName, out var meshFileName))
            {
                throw new ArgumentException($"Msh file name not found for action name {actionName}");
            }

            return $"{FileConstants.GetActorFolderVirtualPath(Name)}{meshFileName}";
        }

        public string GetMaterialFilePath(string actionName)
        {
            if (AnimationType != ActorAnimationType.Skeletal)
            {
                throw new InvalidOperationException("Mtl file path is only available for skeletal animation type.");
            }

            if (!_actionNameToMaterialFileNameMap.TryGetValue(actionName, out var mtlFileName))
            {
                throw new ArgumentException($"Mtl file name not found for action name {actionName}");
            }

            return $"{FileConstants.GetActorFolderVirtualPath(Name)}{mtlFileName}";
        }
    }
}