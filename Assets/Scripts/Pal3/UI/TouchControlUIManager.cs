// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using System;
    using Command;
    using Command.InternalCommands;
    using Core.Utils;
    using State;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.DualShock;
    using UnityEngine.UI;

    public sealed class TouchControlUIManager : IDisposable,
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<ActiveInputDeviceChangedNotification>
    {
        private readonly Canvas _touchControlUI;
        private readonly Button _interactionButton;
        private readonly Button _bigMapButton;
        private readonly Button _storySelectionButton;
        private readonly bool _isTouchSupported;

        private bool _isInGamePlayState;
        private bool _lastActiveDeviceIsGamepadOrKeyboard;

        public TouchControlUIManager(Canvas touchControlUI,
            Button interactionButton,
            Button bigMapButton,
            Button storySelectionButton)
        {
            _touchControlUI = Requires.IsNotNull(touchControlUI, nameof(touchControlUI));
            _interactionButton = Requires.IsNotNull(interactionButton, nameof(interactionButton));
            _bigMapButton = Requires.IsNotNull(bigMapButton, nameof(bigMapButton));
            _storySelectionButton = Requires.IsNotNull(storySelectionButton, nameof(storySelectionButton));

            _touchControlUI.enabled = false;

            #if UNITY_EDITOR
            _isTouchSupported = true;
            #else
            _isTouchSupported = Utility.IsHandheldDevice();
            #endif

            if (_isTouchSupported)
            {
                _lastActiveDeviceIsGamepadOrKeyboard = false;
                _interactionButton.onClick.AddListener(InteractionButtonClicked);
                _bigMapButton.onClick.AddListener(BigMapButtonClicked);
                _storySelectionButton.onClick.AddListener(StorySelectionButtonClicked);
            }

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void BigMapButtonClicked()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new ToggleBigMapRequest());
        }

        private void InteractionButtonClicked()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerInteractionRequest());
        }

        private void StorySelectionButtonClicked()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new ToggleStorySelectorRequest());
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            if (_isTouchSupported)
            {
                _interactionButton.onClick.RemoveAllListeners();
                _bigMapButton.onClick.RemoveAllListeners();
                _storySelectionButton.onClick.RemoveAllListeners();
            }
        }

        public void Execute(GameStateChangedNotification command)
        {
            if (!_isTouchSupported) return;
            _isInGamePlayState = command.NewState == GameState.Gameplay;
            _touchControlUI.enabled = _isInGamePlayState && !_lastActiveDeviceIsGamepadOrKeyboard;
        }

        public void Execute(ActiveInputDeviceChangedNotification notification)
        {
            if (!_isTouchSupported) return;
            _lastActiveDeviceIsGamepadOrKeyboard = notification.Device == Keyboard.current ||
                                                   notification.Device == Gamepad.current ||
                                                   notification.Device == DualShockGamepad.current;
            _touchControlUI.enabled = _isInGamePlayState && !_lastActiveDeviceIsGamepadOrKeyboard;
        }
    }
}