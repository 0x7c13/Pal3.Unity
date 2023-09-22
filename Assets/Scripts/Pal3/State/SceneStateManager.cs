// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.State
{
    using System;
    using System.Collections.Generic;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Engine.Extensions;

    public class SceneObjectStateOverride
    {
        // In-game state overrides
        public bool? IsActivated { get; set; }

        // ScnObjectInfo overrides
        public byte? SwitchState { get; set; }
        public byte? TimesCount { get; set; }
        public byte? LayerIndex { get; set; }
        public GameBoxVector3? GameBoxPosition { get; set; }
        public float? GameBoxYRotation { get; set; }

        // Special object states
        #if PAL3
        public int? BidirectionalPushableObjectState { get; set; }
        #elif PAL3A
        public int? ThreePhaseSwitchPreviousState { get; set; }
        public int? ThreePhaseSwitchCurrentState { get; set; }
        #endif

        public ScnObjectInfo ApplyOverrides(ScnObjectInfo objectInfo)
        {
            if (SwitchState.HasValue) objectInfo.SwitchState = SwitchState.Value;
            if (TimesCount.HasValue) objectInfo.Times = TimesCount.Value;
            if (LayerIndex.HasValue) objectInfo.LayerIndex = LayerIndex.Value;
            if (GameBoxPosition.HasValue) objectInfo.GameBoxPosition = GameBoxPosition.Value;
            if (GameBoxYRotation.HasValue) objectInfo.GameBoxYRotation = GameBoxYRotation.Value;
            return objectInfo;
        }

        public IEnumerable<ICommand> ToCommands((string cityName, string sceneName, int objectId) info)
        {
            if (IsActivated.HasValue)
            {
                yield return new SceneSaveGlobalObjectActivationStateCommand(
                    info.cityName, info.sceneName, info.objectId, IsActivated.Value);
            }
            if (SwitchState.HasValue)
            {
                yield return new SceneSaveGlobalObjectSwitchStateCommand(
                    info.cityName, info.sceneName, info.objectId, SwitchState.Value);
            }
            if (TimesCount.HasValue)
            {
                yield return new SceneSaveGlobalObjectTimesCountCommand(
                    info.cityName, info.sceneName, info.objectId, TimesCount.Value);
            }
            if (LayerIndex.HasValue)
            {
                yield return new SceneSaveGlobalObjectLayerIndexCommand(
                    info.cityName, info.sceneName, info.objectId, LayerIndex.Value);
            }
            if (GameBoxPosition.HasValue)
            {
                yield return new SceneSaveGlobalObjectPositionCommand(
                    info.cityName, info.sceneName, info.objectId, GameBoxPosition.Value.ToUnityPosition(scale: 1f));
            }
            if (GameBoxYRotation.HasValue)
            {
                yield return new SceneSaveGlobalObjectYRotationCommand(
                    info.cityName, info.sceneName, info.objectId, GameBoxYRotation.Value);
            }
            #if PAL3
            if (BidirectionalPushableObjectState.HasValue)
            {
                yield return new SceneSaveGlobalBidirectionalPushableObjectStateCommand(
                    info.cityName, info.sceneName, info.objectId, BidirectionalPushableObjectState.Value);
            }
            #elif PAL3A
            if (ThreePhaseSwitchPreviousState.HasValue && ThreePhaseSwitchCurrentState.HasValue)
            {
                yield return new SceneSaveGlobalThreePhaseSwitchStateCommand(
                    info.cityName, info.sceneName, info.objectId,
                    ThreePhaseSwitchPreviousState.Value, ThreePhaseSwitchCurrentState.Value);
            }
            #endif
        }
    }

    public sealed class SceneStateManager : IDisposable,
        ICommandExecutor<SceneSaveGlobalObjectActivationStateCommand>,
        ICommandExecutor<SceneSaveGlobalObjectSwitchStateCommand>,
        ICommandExecutor<SceneSaveGlobalObjectTimesCountCommand>,
        ICommandExecutor<SceneSaveGlobalObjectLayerIndexCommand>,
        ICommandExecutor<SceneSaveGlobalObjectPositionCommand>,
        ICommandExecutor<SceneSaveGlobalObjectYRotationCommand>,
        #if PAL3
        ICommandExecutor<SceneSaveGlobalBidirectionalPushableObjectStateCommand>,
        #elif PAL3A
        ICommandExecutor<SceneSaveGlobalThreePhaseSwitchStateCommand>,
        #endif
        ICommandExecutor<ResetGameStateCommand>
    {
        private readonly Dictionary<(string cityName, string sceneName, int objectId), SceneObjectStateOverride>
            _sceneObjectStateOverrides = new ();

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
            if (_sceneObjectStateOverrides.TryGetValue(key, out SceneObjectStateOverride objectStateOverride))
            {
                stateOverride = objectStateOverride;
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
            _sceneObjectStateOverrides.TryAdd(key, new SceneObjectStateOverride());
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
            InitKeyIfNotExists(key);
            _sceneObjectStateOverrides[key].GameBoxPosition = command.GameBoxPosition.ToGameBoxPosition(scale: 1f);
        }

        public void Execute(SceneSaveGlobalObjectYRotationCommand command)
        {
            var key = (command.CityName.ToLower(), command.SceneName.ToLower(), command.ObjectId);
            InitKeyIfNotExists(key);
            _sceneObjectStateOverrides[key].GameBoxYRotation = command.GameBoxYRotation;
        }

        #if PAL3
        public void Execute(SceneSaveGlobalBidirectionalPushableObjectStateCommand command)
        {
            var key = (command.CityName.ToLower(), command.SceneName.ToLower(), command.ObjectId);
            InitKeyIfNotExists(key);
            _sceneObjectStateOverrides[key].BidirectionalPushableObjectState = command.State;
        }
        #elif PAL3A
        public void Execute(SceneSaveGlobalThreePhaseSwitchStateCommand command)
        {
            var key = (command.CityName.ToLower(), command.SceneName.ToLower(), command.ObjectId);
            InitKeyIfNotExists(key);
            _sceneObjectStateOverrides[key].ThreePhaseSwitchPreviousState = command.PreviousState;
            _sceneObjectStateOverrides[key].ThreePhaseSwitchCurrentState = command.CurrentState;
        }
        #endif

        public void Execute(ResetGameStateCommand command)
        {
            _sceneObjectStateOverrides.Clear();
        }
    }
}