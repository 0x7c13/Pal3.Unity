// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Actor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.DataReader.Cpk;
    using Core.DataReader.Ini;
    using Data;
    using MetaData;
    using UnityEngine;

    public enum ActorAnimationFileType
    {
        Mv3 = 0,
        Mov
    }

    public abstract class ActorBase
    {
        public ActorAnimationFileType AnimationFileType;

        private ActorActionConfig[] _actorActionConfigs;

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
                _actorActionConfigs = new ActorActionConfig[] { mv3Config };
                AnimationFileType = ActorAnimationFileType.Mv3;
            }
            else
            {
                List<ActorActionConfig> actorActionConfigs = new ();

                // Add default config if it is MOV
                if (defaultConfig is MovActionConfig)
                {
                    actorActionConfigs.Add(defaultConfig);
                }

                // For MOV animation actor, config files are defined as 01.ini, 02.ini ...up tp 09.ini
                for (var i = 0; i <= 9; i++)
                {
                    if (_resourceProvider.GetActorActionConfig(actorActionConfigFolder + $"{i:D2}.ini")
                        is MovActionConfig movConfig)
                    {
                        actorActionConfigs.Add(movConfig);
                    }
                }

                if (actorActionConfigs.Count == 0)
                {
                    throw new Exception($"No actor action config found for: actor {name}");
                }

                Debug.LogWarning($"{actorActionConfigs.Count} MOV actor config(s) found for: actor {name}");

                _actorActionConfigs = actorActionConfigs.ToArray();
                AnimationFileType = ActorAnimationFileType.Mov;
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
            if (_actorActionConfigs == null || _actorActionConfigs.Length == 0) return false;

            return _actorActionConfigs.Any(_ => _.ActorActions.Any(act =>
                act.ActionName.Equals(actionName, StringComparison.OrdinalIgnoreCase)));
        }

        public string GetActionFilePath(string actionName)
        {
            return (from config in _actorActionConfigs from actorAction in config.ActorActions
                where actorAction.ActionName.Equals(actionName, StringComparison.OrdinalIgnoreCase)
                let separator = CpkConstants.DirectorySeparator
                select $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}" +
                       $"{FileConstants.ActorFolderName}{separator}{_actorName}{separator}" +
                       $"{actorAction.ActionFileName}").FirstOrDefault();
        }
    }
}