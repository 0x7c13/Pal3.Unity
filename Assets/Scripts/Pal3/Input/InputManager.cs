// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Input
{
    using System;
    using Command;
    using Command.InternalCommands;
    using State;
    using UnityEngine.InputSystem;

    public sealed class InputManager : IDisposable
    {
        private readonly PlayerInputActions _playerInputActions;
        private InputDevice _lastActiveInputDevice;

        public InputManager(PlayerInputActions playerInputActions)
        {
            _playerInputActions = playerInputActions ?? throw new ArgumentNullException(nameof(playerInputActions));
            SwitchCurrentActionMap(GameState.UI);
            InputSystem.onActionChange += OnInputActionChange;
        }

        public void Dispose()
        {
            InputSystem.onActionChange -= OnInputActionChange;
        }

        public PlayerInputActions GetPlayerInputActions()
        {
            return _playerInputActions;
        }

        public InputDevice GetLastActiveInputDevice()
        {
            return _lastActiveInputDevice;
        }

        private void OnInputActionChange(object action, InputActionChange change)
        {
            if (change == InputActionChange.ActionPerformed)
            {
                var inputAction = (InputAction) action;
                InputControl lastControl = inputAction.activeControl;
                if (_lastActiveInputDevice != lastControl.device)
                {
                    _lastActiveInputDevice = lastControl.device;
                    CommandDispatcher<ICommand>.Instance.Dispatch(
                        new ActiveInputDeviceChangedNotification(lastControl.device));
                }
            }
        }

        public void SwitchCurrentActionMap(GameState state)
        {
            _playerInputActions.Disable();

            InputActionMap inputActionMap = state switch
            {
                GameState.Gameplay     => _playerInputActions.Gameplay.Get(),
                GameState.Cutscene     => _playerInputActions.Cutscene.Get(),
                GameState.VideoPlaying => _playerInputActions.VideoPlaying.Get(),
                GameState.UI           => _playerInputActions.UI.Get(),
                _ => null
            };

            inputActionMap?.Enable();
        }

        public void DisablePlayerInput()
        {
            _playerInputActions.Disable();
        }
    }
}