// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using Command;
    using Command.InternalCommands;
    using Core.Utils;
    using State;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.UI;

    public class TouchControlUIManager :
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<ActiveInputDeviceChangedNotification>
    {
        private readonly Canvas _touchControlUI;
        private readonly Button _interactionButton;
        private readonly Button _bigMapButton;
        private readonly Button _storySelectionButton;
        private readonly bool _isTouchSupported;

        private bool _isInGamePlayState;
        private bool _lastActiveDeviceIsTouchscreen;

        public TouchControlUIManager(Canvas touchControlUI,
            Button interactionButton,
            Button bigMapButton,
            Button storySelectionButton)
        {
            _touchControlUI = touchControlUI;
            _touchControlUI.enabled = false;
            _interactionButton = interactionButton;
            _bigMapButton = bigMapButton;
            _storySelectionButton = storySelectionButton;
            _isTouchSupported = Utility.IsHandheldDevice();

            if (_isTouchSupported)
            {
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
            if (_isTouchSupported)
            {
                _interactionButton.onClick.RemoveAllListeners();
                _bigMapButton.onClick.RemoveAllListeners();
                _storySelectionButton.onClick.RemoveAllListeners();
            }
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(GameStateChangedNotification command)
        {
            if (!_isTouchSupported) return;
            _isInGamePlayState = command.NewState == GameState.Gameplay;
            _touchControlUI.enabled = _isInGamePlayState && _lastActiveDeviceIsTouchscreen;
        }

        public void Execute(ActiveInputDeviceChangedNotification notification)
        {
            if (!_isTouchSupported) return;
            _lastActiveDeviceIsTouchscreen = notification.Device == Touchscreen.current ||
                                             notification.Device == Joystick.current;
            if (!_isInGamePlayState) return;
            _touchControlUI.enabled = _lastActiveDeviceIsTouchscreen;
        }
    }
}