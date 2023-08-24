// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GameSystems.WorldMap
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Contracts;
    using Core.Extensions;
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

    public sealed class WorldMapManager : IDisposable,
        ICommandExecutor<WorldMapEnableRegionCommand>,
        ICommandExecutor<ResetGameStateCommand>,
        ICommandExecutor<GameSwitchRenderingStateCommand>,
        ICommandExecutor<GameSwitchToMainMenuCommand>,
        ICommandExecutor<ToggleWorldMapRequest>
    {
        private readonly EventSystem _eventSystem;
        private readonly GameStateManager _gameStateManager;
        private readonly InputManager _inputManager;
        private readonly PlayerInputActions _playerInputActions;
        private readonly ScriptManager _scriptManager;
        private readonly SceneManager _sceneManager;
        private readonly CanvasGroup _worldMapCanvas;
        private readonly GridLayoutGroup _worldMapCanvasGridLayoutGroup;
        private readonly GameObject _worldMapRegionButtonPrefab;
        private readonly RectTransform _worldMapBackgroundTransform;
        private readonly CanvasGroup _worldMapBackgroundCanvasGroup;

        private bool _isVisible;

        private readonly Guid _stateLockerGuid = Guid.NewGuid();

        private readonly List<GameObject> _selectionButtons = new();
        private readonly Dictionary<int, int> _regionEnablementInfo = new ();

        public WorldMapManager(EventSystem eventSystem,
            GameStateManager gameStateManager,
            SceneManager sceneManager,
            InputManager inputManager,
            ScriptManager scriptManager,
            CanvasGroup worldMapCanvas,
            GameObject worldMapRegionButtonPrefab,
            GameObject worldMapBackground)
        {
            _eventSystem = Requires.IsNotNull(eventSystem, nameof(eventSystem));
            _gameStateManager = Requires.IsNotNull(gameStateManager, nameof(gameStateManager));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _inputManager = Requires.IsNotNull(inputManager, nameof(inputManager));
            _scriptManager = Requires.IsNotNull(scriptManager, nameof(scriptManager));
            _worldMapCanvas = Requires.IsNotNull(worldMapCanvas, nameof(worldMapCanvas));
            _worldMapCanvasGridLayoutGroup = Requires.IsNotNull(
                _worldMapCanvas.GetComponent<GridLayoutGroup>(), "worldMapCanvasGridLayoutGroup");
            _worldMapRegionButtonPrefab = Requires.IsNotNull(worldMapRegionButtonPrefab, nameof(worldMapRegionButtonPrefab));

            Requires.IsNotNull(worldMapBackground, nameof(worldMapBackground));
            _worldMapBackgroundTransform = Requires.IsNotNull(
                worldMapBackground.GetComponent<RectTransform>(), "worldMapBackgroundRectTransform");
            _worldMapBackgroundCanvasGroup = Requires.IsNotNull(
                worldMapBackground.GetComponent<CanvasGroup>(), "worldMapBackgroundCanvasGroup");

            _playerInputActions = inputManager.GetPlayerInputActions();

            _worldMapCanvas.alpha = 0f;
            _worldMapCanvas.interactable = false;
            _worldMapBackgroundCanvasGroup.alpha = 0f;
            _isVisible = false;

            _playerInputActions.Gameplay.ToggleWorldMap.performed += ToggleWorldMapOnPerformed;
            _playerInputActions.UI.ToggleWorldMap.performed += ToggleWorldMapOnPerformed;
            _playerInputActions.UI.ExitCurrentShowingMenu.performed += HideWorldMapOnPerformed;

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            _playerInputActions.Gameplay.ToggleWorldMap.performed -= ToggleWorldMapOnPerformed;
            _playerInputActions.UI.ToggleWorldMap.performed -= ToggleWorldMapOnPerformed;
            _playerInputActions.UI.ExitCurrentShowingMenu.performed -= HideWorldMapOnPerformed;
        }

        private void ToggleWorldMapOnPerformed(InputAction.CallbackContext _)
        {
            ToggleWorldMap();
        }

        private void HideWorldMapOnPerformed(InputAction.CallbackContext _)
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

        private void ToggleWorldMap()
        {
            if (_sceneManager.GetCurrentScene() == null) return;

            if (_isVisible)
            {
                Hide();
                _gameStateManager.TryGoToState(GameState.Gameplay);
            }
            else if (_gameStateManager.GetCurrentState() == GameState.Gameplay)
            {
                // Only enable world map toggling in non-maze scenes.
                if (_sceneManager.GetCurrentScene().GetSceneInfo().SceneType != SceneType.Maze)
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
            // Scene script can execute GameSwitchRenderingStateCommand to toggle WorldMap
            // After the script finishes, the state will be reset to Gameplay. Thus we need to
            // block the state change.
            _gameStateManager.AddGamePlayStateLocker(_stateLockerGuid);

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

            GameObject exitButtonObj = UnityEngine.Object.Instantiate(_worldMapRegionButtonPrefab,
                _worldMapCanvas.transform);
            var exitButtonTextUI = exitButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            exitButtonTextUI.text =  "关闭";
            var exitButton = exitButtonObj.GetComponent<Button>();
            exitButton.colors = UITheme.GetButtonColors();
            exitButton.onClick.AddListener(delegate { WorldMapButtonClicked(-1);});
            _selectionButtons.Add(exitButtonObj);

            for (var i = 0; i < WorldMapConstants.WorldMapRegions.Length; i++)
            {
                if (!_regionEnablementInfo.ContainsKey(i) || _regionEnablementInfo[i] != 2) continue;
                GameObject selectionButton = UnityEngine.Object.Instantiate(_worldMapRegionButtonPrefab,
                    _worldMapCanvas.transform);
                var buttonTextUI = selectionButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonTextUI.text = WorldMapConstants.WorldMapRegions[i];
                var buttonIndex = i;
                var button = selectionButton.GetComponent<Button>();
                button.colors = UITheme.GetButtonColors();
                button.onClick.AddListener(delegate { WorldMapButtonClicked(buttonIndex);});
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

                float width = (_worldMapCanvasGridLayoutGroup.cellSize.x + _worldMapCanvasGridLayoutGroup.spacing.x) *
                    _selectionButtons.Count - _worldMapCanvasGridLayoutGroup.spacing.x;
                float height = _worldMapCanvasGridLayoutGroup.cellSize.y;

                _worldMapBackgroundTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width + 50f);
                _worldMapBackgroundTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height + 60f);
                _worldMapBackgroundTransform.ForceUpdateRectTransforms();

                _worldMapCanvas.alpha = 1f;
                _worldMapCanvas.interactable = true;
                _worldMapBackgroundCanvasGroup.alpha = 1f;
                _isVisible = true;
            }
        }

        private void WorldMapButtonClicked(int buttonIndex)
        {
            // Stop existing script music
            CommandDispatcher<ICommand>.Instance.Dispatch(new StopMusicCommand());

            if (buttonIndex != -1)
            {
                _scriptManager.AddScript((uint)(100 + buttonIndex), true);
            }

            Hide();
            _gameStateManager.TryGoToState(GameState.Gameplay);
        }

        private void Hide()
        {
            _worldMapCanvas.alpha = 0f;
            _worldMapCanvas.interactable = false;
            _worldMapBackgroundCanvasGroup.alpha = 0f;
            _isVisible = false;

            foreach (GameObject button in _selectionButtons)
            {
                button.GetComponent<Button>().onClick.RemoveAllListeners();
                button.Destroy();
            }
            _selectionButtons.Clear();
            _gameStateManager.RemoveGamePlayStateLocker(_stateLockerGuid);
        }

        public void Execute(WorldMapEnableRegionCommand command)
        {
            _regionEnablementInfo[command.Region] = command.EnablementFlag;
        }

        public void Execute(ResetGameStateCommand command)
        {
            _regionEnablementInfo.Clear();
        }

        public void Execute(GameSwitchRenderingStateCommand command)
        {
            if ((RenderingState)command.State == RenderingState.WorldMap)
            {
                Show();
            }
        }

        public void Execute(ToggleWorldMapRequest command)
        {
            ToggleWorldMap();
        }

        public void Execute(GameSwitchToMainMenuCommand command)
        {
            if (_worldMapCanvas.interactable)
            {
                Hide();
            }
        }
    }
}