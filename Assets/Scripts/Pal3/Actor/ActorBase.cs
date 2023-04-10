// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System;
    using System.Collections.Generic;
    using Core.DataReader.Cpk;
    using Core.DataReader.Ini;
    using Data;
    using MetaData;

    public enum ActorAnimationType
    {
        Mv3 = 0,
        Mov
    }

    public abstract class ActorBase
    {
        public ActorAnimationType AnimationType { get; private set; }

        private readonly Dictionary<string, string> _actionNameToFileNameMap = new (StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _actionNameToMeshFileNameMap = new (StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _actionNameToMaterialFileNameMap = new (StringComparer.OrdinalIgnoreCase);

        private readonly GameResourceProvider _resourceProvider;

        private string _actorName;

        protected ActorBase(GameResourceProvider resourceProvider, string name)
        {
            _actorName = name;
            _resourceProvider = resourceProvider;

            InitActorConfig(name);
        }

        private void InitActorConfig(string name)
        {
            char separator = CpkConstants.DirectorySeparator;

            string actorActionConfigFolder = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}" +
                                             $"{FileConstants.ActorFolderName}{separator}{name}{separator}";

            ActorActionConfig defaultConfig = _resourceProvider.GetActorActionConfig(actorActionConfigFolder + $"{name}.ini");

            if (defaultConfig is Mv3ActionConfig mv3Config)
            {
                AddActorConfig(mv3Config);
                AnimationType = ActorAnimationType.Mv3;
                return;
            }

            // Add default config if it is MOV
            if (defaultConfig is MovActionConfig)
            {
                AddActorConfig(defaultConfig);
            }

            // For MOV animation actor, additional action config files are defined as
            // 01.ini, 02.ini ...up tp 09.ini
            for (var i = 0; i <= 9; i++)
            {
                if (_resourceProvider.GetActorActionConfig(actorActionConfigFolder + $"{i:D2}.ini")
                    is MovActionConfig movConfig)
                {
                    AddActorConfig(movConfig);
                }
            }

            AnimationType = ActorAnimationType.Mov;
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

        protected void ChangeName(string name)
        {
            if (_actorName == name) return;
            _actorName = name;
            InitActorConfig(name);
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

            char separator = CpkConstants.DirectorySeparator;
            return $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}" +
                   $"{FileConstants.ActorFolderName}{separator}{_actorName}{separator}" +
                   $"{actionFileName}";
        }

        public string GetMeshFilePath(string actionName)
        {
            if (AnimationType != ActorAnimationType.Mov)
            {
                throw new InvalidOperationException("Mesh file path is only available for MOV animation type.");
            }

            if (!_actionNameToMeshFileNameMap.TryGetValue(actionName, out var meshFileName))
            {
                throw new ArgumentException($"Mesh file name not found for action name {actionName}");
            }

            char separator = CpkConstants.DirectorySeparator;
            return $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}" +
                   $"{FileConstants.ActorFolderName}{separator}{_actorName}{separator}" +
                   $"{meshFileName}";
        }
    }
}