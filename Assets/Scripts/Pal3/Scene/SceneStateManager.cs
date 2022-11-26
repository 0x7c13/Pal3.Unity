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

    public enum SceneObjectActivationState
    {
        Unknown,
        Disabled,
        Enabled,
    }
    
    public sealed class SceneStateManager : IDisposable,
        ICommandExecutor<SceneChangeGlobalObjectActivationStateCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private readonly Dictionary<(string cityName, string sceneName, int objectId), bool> _sceneObjectActivationStates = new ();

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

        public SceneObjectActivationState GetSceneObjectActivationState(string cityName,
            string sceneName,
            int objectId)
        {
            var key = (cityName.ToLower(), sceneName.ToLower(), objectId);
            if (_sceneObjectActivationStates.ContainsKey(key))
            {
                return _sceneObjectActivationStates[key] ?
                    SceneObjectActivationState.Enabled :
                    SceneObjectActivationState.Disabled;
            }
            else return SceneObjectActivationState.Unknown;
        }
        
        public void Execute(SceneChangeGlobalObjectActivationStateCommand command)
        {
            var key = (command.CityName.ToLower(), command.SceneName.ToLower(), command.ObjectId);
            _sceneObjectActivationStates[key] = command.IsActive != 0;
        }

        public void Execute(ResetGameStateCommand command)
        {
            _sceneObjectActivationStates.Clear();
        }
    }
}