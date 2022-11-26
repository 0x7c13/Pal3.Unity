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
    
    public sealed class SceneStateManager : IDisposable,
        ICommandExecutor<SceneChangeGlobalObjectActivationStateCommand>,
        ICommandExecutor<SceneChangeGlobalObjectSwitchStateCommand>,
        ICommandExecutor<SceneChangeGlobalObjectTimesCountCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private readonly Dictionary<(string cityName, string sceneName, int objectId), bool> _sceneObjectActivationStates = new ();
        private readonly Dictionary<(string cityName, string sceneName, int objectId), bool> _sceneObjectSwitchStates = new ();
        private readonly Dictionary<(string cityName, string sceneName, int objectId), byte> _sceneObjectTimesCount = new ();

        public SceneStateManager()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }
        
        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public Dictionary<(string cityName, string sceneName, int objectId), bool> GetSceneObjectActivationStates()
        {
            return _sceneObjectActivationStates;
        }
        
        public Dictionary<(string cityName, string sceneName, int objectId), bool> GetSceneObjectSwitchStates()
        {
            return _sceneObjectSwitchStates;
        }
        
        public Dictionary<(string cityName, string sceneName, int objectId), byte> GetSceneObjectTimesCount()
        {
            return _sceneObjectTimesCount;
        }

        public bool TryGetSceneObjectActivationState(string cityName,
            string sceneName,
            int objectId,
            out bool isActivated)
        {
            var key = (cityName.ToLower(), sceneName.ToLower(), objectId);
            if (_sceneObjectActivationStates.ContainsKey(key))
            {
                isActivated = _sceneObjectActivationStates[key];
                return true;
            }
            else
            {
                isActivated = false;
                return false;
            }
        }
        
        public bool TryGetSceneObjectSwitchState(string cityName,
            string sceneName,
            int objectId,
            out bool isSwitchOn)
        {
            var key = (cityName.ToLower(), sceneName.ToLower(), objectId);
            if (_sceneObjectSwitchStates.ContainsKey(key))
            {
                isSwitchOn = _sceneObjectSwitchStates[key];
                return true;
            }
            else
            {
                isSwitchOn = false;
                return false;
            }
        }
        
        public bool TryGetSceneObjectTimesCount(string cityName,
            string sceneName,
            int objectId,
            out byte timesCount)
        {
            var key = (cityName.ToLower(), sceneName.ToLower(), objectId);
            if (_sceneObjectTimesCount.ContainsKey(key))
            {
                timesCount = _sceneObjectTimesCount[key];
                return true;
            }
            else
            {
                timesCount = 0xFF;
                return false;
            }
        }
        
        public void Execute(SceneChangeGlobalObjectActivationStateCommand command)
        {
            var key = (command.CityName.ToLower(), command.SceneName.ToLower(), command.ObjectId);
            _sceneObjectActivationStates[key] = command.IsActive != 0;
        }
        
        public void Execute(SceneChangeGlobalObjectSwitchStateCommand command)
        {
            var key = (command.CityName.ToLower(), command.SceneName.ToLower(), command.ObjectId);
            _sceneObjectSwitchStates[key] = command.SwitchState != 0;
        }

        public void Execute(SceneChangeGlobalObjectTimesCountCommand command)
        {
            var key = (command.CityName.ToLower(), command.SceneName.ToLower(), command.ObjectId);
            _sceneObjectTimesCount[key] = command.TimesCount;
        }
        
        public void Execute(ResetGameStateCommand command)
        {
            _sceneObjectActivationStates.Clear();
            _sceneObjectSwitchStates.Clear();
            _sceneObjectTimesCount.Clear();
        }
    }
}