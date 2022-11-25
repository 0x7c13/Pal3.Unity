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
        private readonly Dictionary<string, bool> _sceneObjectActivationStates = new ();

        public SceneStateManager()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }
        
        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public Dictionary<string, bool> GetSceneObjectActivationStates()
        {
            return _sceneObjectActivationStates;
        }
        
        public string GetSceneObjectHashName(string sceneCityName,
            string sceneName,
            int objectId)
        {
            return $"{sceneCityName.ToLower()}_{sceneName.ToLower()}_{objectId}";
        }
        
        public SceneObjectActivationState GetSceneObjectActivationState(string sceneCityName,
            string sceneName,
            int objectId)
        {
            string sceneObjectHashName = GetSceneObjectHashName(sceneCityName, sceneName, objectId);
            if (_sceneObjectActivationStates.ContainsKey(sceneObjectHashName))
            {
                return _sceneObjectActivationStates[sceneObjectHashName] ?
                    SceneObjectActivationState.Enabled :
                    SceneObjectActivationState.Disabled;
            }
            else return SceneObjectActivationState.Unknown;
        }
        
        public void Execute(SceneChangeGlobalObjectActivationStateCommand command)
        {
            _sceneObjectActivationStates[command.SceneObjectHashName] = command.IsActive != 0;
        }

        public void Execute(ResetGameStateCommand command)
        {
            _sceneObjectActivationStates.Clear();
        }
    }
}