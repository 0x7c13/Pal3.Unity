// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Input
{
    using System;
    using System.Linq;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Utilities;
    using State;
    using UnityEngine.InputSystem;

    public sealed class InputManager : IDisposable
    {
        private readonly PlayerInputActions _playerInputActions;
        private InputDevice _lastActiveInputDevice;

        public InputManager(PlayerInputActions playerInputActions)
        {
            _playerInputActions = Requires.IsNotNull(playerInputActions, nameof(playerInputActions));

            // Goto initial state
            SwitchCurrentActionMap(GameState.UI);

            // Disable unsupported devices
            DisableUnsupportedDevices();
            
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
                InputAction inputAction = (InputAction) action;
                InputControl lastControl = inputAction.activeControl;
                if (_lastActiveInputDevice != lastControl.device)
                {
                    _lastActiveInputDevice = lastControl.device;
                    Pal3.Instance.Execute(new ActiveInputDeviceChangedNotification(lastControl.device));
                }
            }
        }

        public void SwitchCurrentActionMap(GameState state)
        {
            _playerInputActions.Disable();

            InputActionMap inputActionMap = state switch
            {
                GameState.UI           => _playerInputActions.UI.Get(),
                GameState.Gameplay     => _playerInputActions.Gameplay.Get(),
                GameState.Cutscene     => _playerInputActions.Cutscene.Get(),
                GameState.VideoPlaying => _playerInputActions.VideoPlaying.Get(),
                GameState.Combat       => _playerInputActions.Combat.Get(),
                GameState.Debug        => _playerInputActions.Debug.Get(),
                _ => null
            };

            inputActionMap?.Enable();
        }

        private void DisableUnsupportedDevices()
        {
            // Disable all devices that are not supported
            // Especially for controllers that are used for flight simulation and racing games
            foreach (InputDevice device in 
                     InputSystem.devices.Where(device => device.name.StartsWith("Thrustmaster") ||
                                                         device.name.StartsWith("Thustmaster") ||
                                                         device.name.StartsWith("Winwing")))
            {
                // Disable the device
                InputSystem.DisableDevice(device);
            }
        }

        public void DisablePlayerInput()
        {
            _playerInputActions.Disable();
        }
    }
}