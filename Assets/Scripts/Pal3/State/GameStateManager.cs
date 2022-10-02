// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.State
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Services;
    using Input;
    using Script;
    using UI;
    using UnityEngine;

    public sealed class GameStateManager :
        ICommandExecutor<PlayVideoCommand>,
        ICommandExecutor<VideoEndedNotification>,
        ICommandExecutor<PlayerEnableInputCommand>,
        ICommandExecutor<PlayerInteractionTriggeredNotification>,
        ICommandExecutor<DialogueRenderingStartedNotification>,
        ICommandExecutor<ScriptFinishedRunningNotification>,
        ICommandExecutor<ScriptFailedToRunNotification>,
        ICommandExecutor<GameSwitchToMainMenuCommand>
    {
        private GameState _previousState;
        private GameState _state;
        private readonly InputManager _inputManager;
        private readonly ScriptManager _scriptManager;
        private bool _isDebugging;

        private GameStateManager() { }

        public GameStateManager(InputManager inputManager, ScriptManager scriptManager)
        {
            _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
            _scriptManager = scriptManager ?? throw new ArgumentNullException(nameof(scriptManager));
            _previousState = GameState.UI;
            _state = GameState.UI;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public GameState GetCurrentState()
        {
            return _state;
        }

        public void GoToState(GameState state)
        {
            if (_state == state) return;

            Debug.Log($"Goto State: {state.ToString()}");

            _previousState = _state;
            _state = state;

            UpdateInputManagerState();

            CommandDispatcher<ICommand>.Instance.Dispatch(new GameStateChangedNotification(state));
        }

        public void GoToPreviousState()
        {
            GoToState(_previousState);
        }

        private void UpdateInputManagerState()
        {
            if (!_isDebugging)
            {
                _inputManager.SwitchCurrentActionMap(_state);
            }
        }

        public void EnterDebugState()
        {
            _isDebugging = true;
            _inputManager.SwitchCurrentActionMap(GameState.UI);
        }

        public void LeaveDebugState()
        {
            _isDebugging = false;
            _inputManager.SwitchCurrentActionMap(_state);
        }

        public void Execute(PlayVideoCommand command)
        {
            GoToState(GameState.VideoPlaying);
        }

        public void Execute(VideoEndedNotification notification)
        {
            GoToPreviousState();
        }

        public void Execute(PlayerEnableInputCommand command)
        {
            if (command.Enable == 0)
            {
                GoToState(GameState.Cutscene);
            }
            else if (command.Enable == 1 && _scriptManager.GetNumberOfRunningScripts() == 0)
            {
                GoToState(GameState.Gameplay);
            }
        }

        public void Execute(PlayerInteractionTriggeredNotification notification)
        {
            GoToState(GameState.Cutscene);
        }

        public void Execute(DialogueRenderingStartedNotification command)
        {
            GoToState(GameState.Cutscene);
        }

        public void Execute(ScriptFinishedRunningNotification notification)
        {
            if (_scriptManager.GetNumberOfRunningScripts() == 0)
            {
                // Scene script can execute GameSwitchRenderingStateCommand to toggle BigMap
                // Thus we need to check if BigMap is visible before switching state back to Gameplay.
                if (ServiceLocator.Instance.Get<BigMapManager>().IsVisible())
                {
                    return;
                }
                
                GoToState(GameState.Gameplay);
            }
        }

        public void Execute(ScriptFailedToRunNotification notification)
        {
            if (_scriptManager.GetNumberOfRunningScripts() == 0)
            {
                GoToState(GameState.Gameplay);
            }
        }

        public void Execute(GameSwitchToMainMenuCommand command)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new ResetGameStateCommand());
        }
    }
}