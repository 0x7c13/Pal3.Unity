// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using System;
    using System.Collections.Generic;
    using Command;
    using Command.InternalCommands;
    using Core.DataReader.Scn;
    using UnityEngine;

    public class SceneObjectStateOverride
    {
        // In-game state overrides
        public bool? IsActivated { get; set; }

        // ScnObjectInfo overrides
        public byte? SwitchState { get; set; }
        public byte? TimesCount { get; set; }
        public byte? LayerIndex { get; set; }
        public Vector3? GameBoxPosition { get; set; }

        public ScnObjectInfo ApplyOverrides(ScnObjectInfo objectInfo)
        {
            if (SwitchState.HasValue) objectInfo.SwitchState = SwitchState.Value;
            if (TimesCount.HasValue) objectInfo.Times = TimesCount.Value;
            if (LayerIndex.HasValue) objectInfo.LayerIndex = LayerIndex.Value;
            if (GameBoxPosition.HasValue) objectInfo.GameBoxPosition = GameBoxPosition.Value;

            return objectInfo;
        }
    }

    public sealed class SceneStateManager : IDisposable,
        ICommandExecutor<SceneSaveGlobalObjectActivationStateCommand>,
        ICommandExecutor<SceneSaveGlobalObjectSwitchStateCommand>,
        ICommandExecutor<SceneSaveGlobalObjectTimesCountCommand>,
        ICommandExecutor<SceneSaveGlobalObjectLayerIndexCommand>,
        ICommandExecutor<SceneSaveGlobalObjectPositionCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private readonly Dictionary<(string cityName, string sceneName, int objectId), SceneObjectStateOverride>
            _sceneObjectStateOverrides = new ();

        // There are some objects' position purely controlled by the script, so we don't want to persist them.
        private readonly HashSet<(string cityName, string sceneName)> _sceneObjectPositionStateIgnoredScenes =
            new ()
            {
                ("m08", "3"),
                ("m15", "a"),
                ("m15", "b"),
                ("m15", "c"),
                ("m15", "d"),
                ("m15", "a1"),
                ("m15", "b1"),
                ("m15", "c1"),
                ("m15", "d1"),
            };

        public SceneStateManager()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public Dictionary<(string cityName, string sceneName, int objectId), SceneObjectStateOverride> GetSceneObjectStateOverrides()
        {
            return _sceneObjectStateOverrides;
        }

        public bool TryGetSceneObjectStateOverride(string cityName,
            string sceneName,
            int objectId,
            out SceneObjectStateOverride stateOverride)
        {
            var key = (cityName.ToLower(), sceneName.ToLower(), objectId);
            if (_sceneObjectStateOverrides.ContainsKey(key))
            {
                stateOverride = _sceneObjectStateOverrides[key];
                return true;
            }
            else
            {
                stateOverride = default;
                return false;
            }
        }

        private void InitKeyIfNotExists((string cityName, string sceneName, int objectId) key)
        {
            if (!_sceneObjectStateOverrides.ContainsKey(key))
            {
                _sceneObjectStateOverrides[key] = new SceneObjectStateOverride();
            }
        }

        public void Execute(SceneSaveGlobalObjectActivationStateCommand command)
        {
            var key = (command.CityName.ToLower(), command.SceneName.ToLower(), command.ObjectId);
            InitKeyIfNotExists(key);
            _sceneObjectStateOverrides[key].IsActivated = command.IsActivated;
        }

        public void Execute(SceneSaveGlobalObjectSwitchStateCommand command)
        {
            var key = (command.CityName.ToLower(), command.SceneName.ToLower(), command.ObjectId);
            InitKeyIfNotExists(key);
            _sceneObjectStateOverrides[key].SwitchState = command.SwitchState;
        }

        public void Execute(SceneSaveGlobalObjectTimesCountCommand command)
        {
            var key = (command.CityName.ToLower(), command.SceneName.ToLower(), command.ObjectId);
            InitKeyIfNotExists(key);
            _sceneObjectStateOverrides[key].TimesCount = command.TimesCount;
        }

        public void Execute(SceneSaveGlobalObjectLayerIndexCommand command)
        {
            var key = (command.CityName.ToLower(), command.SceneName.ToLower(), command.ObjectId);
            InitKeyIfNotExists(key);
            _sceneObjectStateOverrides[key].LayerIndex = command.LayerIndex;
        }

        public void Execute(SceneSaveGlobalObjectPositionCommand command)
        {
            var key = (command.CityName.ToLower(), command.SceneName.ToLower(), command.ObjectId);

            if (_sceneObjectPositionStateIgnoredScenes.Contains(
                    (command.CityName.ToLower(), command.SceneName.ToLower())))
            {
                return; // Don't save the position of the objects in these scenes.
            }

            InitKeyIfNotExists(key);
            _sceneObjectStateOverrides[key].GameBoxPosition = command.GameBoxPosition;
        }

        public void Execute(ResetGameStateCommand command)
        {
            _sceneObjectStateOverrides.Clear();
        }
    }
}