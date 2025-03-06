// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.UI
{
    using System;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Contract.Enums;
    using Core.Utilities;
    using Engine.Utilities;
    using Scene;
    using State;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.DualShock;
    using UnityEngine.UI;

    public sealed class TouchControlUIManager : IDisposable,
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<ActiveInputDeviceChangedNotification>
    {
        private readonly SceneManager _sceneManager;
        private readonly Canvas _touchControlUI;
        private readonly Button _interactionButton;
        private readonly Button _multiFunctionButton;
        private readonly Button _mainMenuButton;
        private readonly bool _isTouchSupported;

        private bool _isInGamePlayState;
        private bool _lastActiveDeviceIsGamepadOrKeyboard;

        public TouchControlUIManager(SceneManager sceneManager,
            Canvas touchControlUI,
            Button interactionButton,
            Button multiFunctionButton,
            Button mainMenuButton)
        {
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _touchControlUI = Requires.IsNotNull(touchControlUI, nameof(touchControlUI));
            _interactionButton = Requires.IsNotNull(interactionButton, nameof(interactionButton));
            _multiFunctionButton = Requires.IsNotNull(multiFunctionButton, nameof(multiFunctionButton));
            _mainMenuButton = Requires.IsNotNull(mainMenuButton, nameof(mainMenuButton));

            _touchControlUI.enabled = false;

            #if UNITY_EDITOR
            _isTouchSupported = true;
            #else
            _isTouchSupported = UnityEngineUtility.IsHandheldDevice();
            #endif

            if (_isTouchSupported)
            {
                _lastActiveDeviceIsGamepadOrKeyboard = false;
                _interactionButton.onClick.AddListener(InteractionButtonClicked);
                _multiFunctionButton.onClick.AddListener(MultiFunctionButtonClicked);
                _mainMenuButton.onClick.AddListener(MainMenuButtonClicked);
            }

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void MultiFunctionButtonClicked()
        {
            if (_sceneManager.GetCurrentScene() is { } currentScene &&
                currentScene.GetSceneInfo().SceneType == SceneType.Maze)
            {
                Pal3.Instance.Execute(new SwitchPlayerActorRequest());
            }
            else
            {
                Pal3.Instance.Execute(new ToggleWorldMapRequest());
            }
        }

        private void InteractionButtonClicked()
        {
            Pal3.Instance.Execute(new PlayerInteractionRequest());
        }

        private void MainMenuButtonClicked()
        {
            Pal3.Instance.Execute(new ToggleMainMenuRequest());
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            if (_isTouchSupported)
            {
                _interactionButton.onClick.RemoveAllListeners();
                _multiFunctionButton.onClick.RemoveAllListeners();
                _mainMenuButton.onClick.RemoveAllListeners();
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