// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GameSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using Core.Utils;
    using Input;
    using Script;
    using MetaData;
    using Scene;
    using State;
    using TMPro;
    using UI;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.DualShock;
    using UnityEngine.UI;

    public sealed class BigMapManager : IDisposable,
        ICommandExecutor<BigMapEnableRegionCommand>,
        ICommandExecutor<ResetGameStateCommand>,
        ICommandExecutor<GameSwitchRenderingStateCommand>,
        ICommandExecutor<GameSwitchToMainMenuCommand>,
        ICommandExecutor<ToggleBigMapRequest>
    {
        private readonly EventSystem _eventSystem;
        private readonly GameStateManager _gameStateManager;
        private readonly InputManager _inputManager;
        private readonly PlayerInputActions _playerInputActions;
        private readonly ScriptManager _scriptManager;
        private readonly SceneManager _sceneManager;
        private readonly CanvasGroup _bigMapCanvas;
        private readonly GridLayoutGroup _bigMapCanvasGridLayoutGroup;
        private readonly GameObject _bigMapRegionButtonPrefab;
        private readonly RectTransform _bigMapBackgroundTransform;
        private readonly CanvasGroup _bigMapBackgroundCanvasGroup;

        private bool _isVisible;

        private readonly Guid _stateLockerGuid = Guid.NewGuid();

        private readonly List<GameObject> _selectionButtons = new();
        private readonly Dictionary<int, int> _regionEnablementInfo = new ();

        public BigMapManager(EventSystem eventSystem,
            GameStateManager gameStateManager,
            SceneManager sceneManager,
            InputManager inputManager,
            ScriptManager scriptManager,
            CanvasGroup bigMapCanvas,
            GameObject bigMapRegionButtonPrefab,
            GameObject bigMapBackground)
        {
            _eventSystem = Requires.IsNotNull(eventSystem, nameof(eventSystem));
            _gameStateManager = Requires.IsNotNull(gameStateManager, nameof(gameStateManager));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _inputManager = Requires.IsNotNull(inputManager, nameof(inputManager));
            _scriptManager = Requires.IsNotNull(scriptManager, nameof(scriptManager));
            _bigMapCanvas = Requires.IsNotNull(bigMapCanvas, nameof(bigMapCanvas));
            _bigMapCanvasGridLayoutGroup = Requires.IsNotNull(_bigMapCanvas.GetComponent<GridLayoutGroup>(), "bigMapCanvasGridLayoutGroup");
            _bigMapRegionButtonPrefab = Requires.IsNotNull(bigMapRegionButtonPrefab, nameof(bigMapRegionButtonPrefab));

            Requires.IsNotNull(bigMapBackground, nameof(bigMapBackground));
            _bigMapBackgroundTransform = Requires.IsNotNull(bigMapBackground.GetComponent<RectTransform>(), "bigMapBackgroundRectTransform");
            _bigMapBackgroundCanvasGroup = Requires.IsNotNull(bigMapBackground.GetComponent<CanvasGroup>(), "bigMapBackgroundCanvasGroup");

            _playerInputActions = inputManager.GetPlayerInputActions();

            _bigMapCanvas.alpha = 0f;
            _bigMapCanvas.interactable = false;
            _bigMapBackgroundCanvasGroup.alpha = 0f;
            _isVisible = false;

            _playerInputActions.Gameplay.ToggleBigMap.performed += ToggleBigMapOnPerformed;
            _playerInputActions.UI.ToggleBigMap.performed += ToggleBigMapOnPerformed;
            _playerInputActions.UI.ExitCurrentShowingMenu.performed += HideBigMapOnPerformed;

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            _playerInputActions.Gameplay.ToggleBigMap.performed -= ToggleBigMapOnPerformed;
            _playerInputActions.UI.ToggleBigMap.performed -= ToggleBigMapOnPerformed;
            _playerInputActions.UI.ExitCurrentShowingMenu.performed -= HideBigMapOnPerformed;
        }

        private void ToggleBigMapOnPerformed(InputAction.CallbackContext _)
        {
            ToggleBigMap();
        }

        private void HideBigMapOnPerformed(InputAction.CallbackContext _)
        {
            if (_isVisible)
            {
                Hide();
                _gameStateManager.TryGoToState(GameState.Gameplay);
            }
        }

        public bool IsVisible()
        {
            return _isVisible;
        }

        public Dictionary<int, int> GetRegionEnablementInfo()
        {
            return _regionEnablementInfo;
        }

        private void ToggleBigMap()
        {
            if (_sceneManager.GetCurrentScene() == null) return;

            if (_isVisible)
            {
                Hide();
                _gameStateManager.TryGoToState(GameState.Gameplay);
            }
            else if (_gameStateManager.GetCurrentState() == GameState.Gameplay)
            {
                // Only enable big map toggling in non-maze scenes.
                if (_sceneManager.GetCurrentScene().GetSceneInfo().SceneType != ScnSceneType.Maze)
                {
                    Show();
                }
            }
        }

        private void Show()
        {
            if (_regionEnablementInfo.Count == 0)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("大地图尚未开启"));
                return;
            }

            _gameStateManager.TryGoToState(GameState.UI);
            // Scene script can execute GameSwitchRenderingStateCommand to toggle BigMap
            // After the script finishes, the state will be reset to Gameplay. Thus we need to
            // block the state change.
            _gameStateManager.AddGamePlayStateLocker(_stateLockerGuid);

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

            GameObject exitButtonObj = UnityEngine.Object.Instantiate(_bigMapRegionButtonPrefab,
                _bigMapCanvas.transform);
            var exitButtonTextUI = exitButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            exitButtonTextUI.text =  "关闭";
            var exitButton = exitButtonObj.GetComponent<Button>();
            exitButton.colors = UITheme.GetButtonColors();
            exitButton.onClick.AddListener(delegate { BigMapButtonClicked(-1);});
            _selectionButtons.Add(exitButtonObj);

            for (var i = 0; i < BigMapConstants.BigMapRegions.Length; i++)
            {
                if (!_regionEnablementInfo.ContainsKey(i) || _regionEnablementInfo[i] != 2) continue;
                GameObject selectionButton = UnityEngine.Object.Instantiate(_bigMapRegionButtonPrefab,
                    _bigMapCanvas.transform);
                var buttonTextUI = selectionButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonTextUI.text = BigMapConstants.BigMapRegions[i];
                var buttonIndex = i;
                var button = selectionButton.GetComponent<Button>();
                button.colors = UITheme.GetButtonColors();
                button.onClick.AddListener(delegate { BigMapButtonClicked(buttonIndex);});
                _selectionButtons.Add(selectionButton);
            }

            var firstButton = _selectionButtons.First().GetComponent<Button>();

            InputDevice lastActiveInputDevice = _inputManager.GetLastActiveInputDevice();
            if (lastActiveInputDevice == Keyboard.current ||
                lastActiveInputDevice == Gamepad.current ||
                lastActiveInputDevice == DualShockGamepad.current)
            {
                _eventSystem.firstSelectedGameObject = firstButton.gameObject;
                firstButton.Select();
            }
            else
            {
                _eventSystem.firstSelectedGameObject = null;
            }

            if (_selectionButtons.Count > 1)
            {
                // Setup button navigation
                void ConfigureButtonNavigation(Button button, int index, int count)
                {
                    Navigation buttonNavigation = button.navigation;
                    buttonNavigation.mode = Navigation.Mode.Explicit;

                    int leftIndex = index == 0 ? count - 1 : index - 1;
                    int rightIndex = index == count - 1 ? 0 : index + 1;

                    buttonNavigation.selectOnLeft = _selectionButtons[leftIndex].GetComponentInChildren<Button>();
                    buttonNavigation.selectOnRight = _selectionButtons[rightIndex].GetComponentInChildren<Button>();

                    button.navigation = buttonNavigation;
                }

                for (var i = 0; i < _selectionButtons.Count; i++)
                {
                    var button = _selectionButtons[i].GetComponentInChildren<Button>();
                    ConfigureButtonNavigation(button, i, _selectionButtons.Count);
                }

                float width = (_bigMapCanvasGridLayoutGroup.cellSize.x + _bigMapCanvasGridLayoutGroup.spacing.x) *
                    _selectionButtons.Count - _bigMapCanvasGridLayoutGroup.spacing.x;
                float height = _bigMapCanvasGridLayoutGroup.cellSize.y;

                _bigMapBackgroundTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width + 50f);
                _bigMapBackgroundTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height + 60f);
                _bigMapBackgroundTransform.ForceUpdateRectTransforms();

                _bigMapCanvas.alpha = 1f;
                _bigMapCanvas.interactable = true;
                _bigMapBackgroundCanvasGroup.alpha = 1f;
                _isVisible = true;
            }
        }

        private void BigMapButtonClicked(int buttonIndex)
        {
            if (buttonIndex != -1)
            {
                _scriptManager.AddScript((uint)(100 + buttonIndex), true);
            }
            Hide();
            _gameStateManager.TryGoToState(GameState.Gameplay);
        }

        private void Hide()
        {
            _bigMapCanvas.alpha = 0f;
            _bigMapCanvas.interactable = false;
            _bigMapBackgroundCanvasGroup.alpha = 0f;
            _isVisible = false;

            foreach (GameObject button in _selectionButtons)
            {
                button.GetComponent<Button>().onClick.RemoveAllListeners();
                UnityEngine.Object.Destroy(button);
            }
            _selectionButtons.Clear();
            _gameStateManager.RemoveGamePlayStateLocker(_stateLockerGuid);
        }

        public void Execute(BigMapEnableRegionCommand command)
        {
            _regionEnablementInfo[command.Region] = command.EnablementFlag;
        }

        public void Execute(ResetGameStateCommand command)
        {
            _regionEnablementInfo.Clear();
        }

        public void Execute(GameSwitchRenderingStateCommand command)
        {
            if ((RenderingState)command.State == RenderingState.BigMap)
            {
                Show();
            }
        }

        public void Execute(ToggleBigMapRequest command)
        {
            ToggleBigMap();
        }

        public void Execute(GameSwitchToMainMenuCommand command)
        {
            if (_bigMapCanvas.interactable)
            {
                Hide();
            }
        }
    }
}