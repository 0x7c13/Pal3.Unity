// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.State
{
    using System;
    using System.Collections.Generic;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Utils;
    using Input;
    using Script;
    using UnityEngine;

    public sealed class GameStateManager : IDisposable,
        ICommandExecutor<PlayerEnableInputCommand>,
        ICommandExecutor<GameStateChangeRequest>,
        ICommandExecutor<DialogueRenderingStartedNotification>,
        ICommandExecutor<ScriptFinishedRunningNotification>,
        ICommandExecutor<ScriptFailedToRunNotification>,
        ICommandExecutor<GameSwitchToMainMenuCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private GameState _previousState;
        private GameState _currentState;
        private readonly InputManager _inputManager;
        private readonly ScriptManager _scriptManager;
        private bool _isDebugging;
        private readonly HashSet<Guid> _gamePlayStateLockers = new ();

        public GameStateManager(InputManager inputManager, ScriptManager scriptManager)
        {
            _inputManager = Requires.IsNotNull(inputManager, nameof(inputManager));
            _scriptManager = Requires.IsNotNull(scriptManager, nameof(scriptManager));
            _previousState = GameState.UI; // Initial state
            _currentState = GameState.UI; // Initial state
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public GameState GetCurrentState()
        {
            return _currentState;
        }

        /// <summary>
        /// Add a locker to prevent the game from entering GamePlay state.
        /// </summary>
        public void AddGamePlayStateLocker(Guid lockerId)
        {
            _gamePlayStateLockers.Add(lockerId);
        }

        public void RemoveGamePlayStateLocker(Guid lockerId)
        {
            _gamePlayStateLockers.Remove(lockerId);
        }

        public bool TryGoToState(GameState newState)
        {
            if (_currentState == newState) return true;

            if (newState == GameState.Gameplay)
            {
                if (_gamePlayStateLockers.Count > 0 ||
                    _scriptManager.GetNumberOfRunningScripts() > 0)
                {
                    // Do not allow to switch to Gameplay state if there are running scripts
                    // or there are GamePlay state lockers.
                    return false;
                }
            }

            Debug.Log($"[{nameof(GameStateManager)}] Goto State: {newState.ToString()}");

            _previousState = _currentState;
            _currentState = newState;

            UpdateInputManagerState();

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangedNotification(_previousState, _currentState));

            return true;
        }

        public void GoToPreviousState()
        {
            TryGoToState(_previousState);
        }

        private void UpdateInputManagerState()
        {
            if (!_isDebugging)
            {
                _inputManager.SwitchCurrentActionMap(_currentState);
            }
        }

        public void EnterDebugState()
        {
            _isDebugging = true;
            _inputManager.SwitchCurrentActionMap(GameState.Debug);
        }

        public void LeaveDebugState()
        {
            _isDebugging = false;
            _inputManager.SwitchCurrentActionMap(_currentState);
        }

        public void Execute(PlayerEnableInputCommand command)
        {
            if (command.Enable == 0)
            {
                TryGoToState(GameState.Cutscene);
            }
            else if (command.Enable == 1)
            {
                TryGoToState(GameState.Gameplay);
            }
        }

        public void Execute(GameStateChangeRequest request)
        {
            TryGoToState(request.NewState);
        }

        public void Execute(DialogueRenderingStartedNotification command)
        {
            TryGoToState(GameState.Cutscene);
        }

        public void Execute(ScriptFinishedRunningNotification notification)
        {
            TryGoToState(GameState.Gameplay);
        }

        public void Execute(ScriptFailedToRunNotification notification)
        {
            TryGoToState(GameState.Gameplay);
        }

        public void Execute(GameSwitchToMainMenuCommand command)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new ResetGameStateCommand());
        }

        public void Execute(ResetGameStateCommand command)
        {
            _gamePlayStateLockers.Clear();
        }
    }
}