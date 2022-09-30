// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using Input;
    using Script;
    using MetaData;
    using Scene;
    using State;
    using TMPro;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;
    using UnityEngine.UI;

    public class BigMapManager : MonoBehaviour,
        ICommandExecutor<BigMapEnableRegionCommand>,
        ICommandExecutor<ResetGameStateCommand>,
        ICommandExecutor<GameSwitchRenderingStateCommand>,
        ICommandExecutor<ToggleBigMapRequest>
    {
        private EventSystem _eventSystem;
        private GameStateManager _gameStateManager;
        private InputManager _inputManager;
        private PlayerInputActions _playerInputActions;
        private ScriptManager _scriptManager;
        private SceneManager _sceneManager;
        private CanvasGroup _bigMapCanvas;
        private GameObject _bigMapRegionButtonPrefab;

        private bool _isVisible;
        
        private readonly List<GameObject> _selectionButtons = new();
        private readonly Dictionary<int, int> _regionEnablementInfo = new ();

        public void Init(EventSystem eventSystem,
            GameStateManager gameStateManager,
            SceneManager sceneManager,
            InputManager inputManager,
            ScriptManager scriptManager,
            CanvasGroup bigMapCanvas,
            GameObject bigMapRegionButtonPrefab)
        {
            _eventSystem = eventSystem;
            _gameStateManager = gameStateManager;
            _sceneManager = sceneManager;
            _inputManager = inputManager;
            _playerInputActions = inputManager.GetPlayerInputActions();
            _scriptManager = scriptManager;
            _bigMapCanvas = bigMapCanvas;
            _bigMapRegionButtonPrefab = bigMapRegionButtonPrefab;

            _bigMapCanvas.alpha = 0f;
            _bigMapCanvas.interactable = false;
            _isVisible = false;

            _playerInputActions.Gameplay.ToggleBigMap.performed += ToggleBigMapOnPerformed;
            _playerInputActions.Cutscene.ToggleBigMap.performed += ToggleBigMapOnPerformed;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            _playerInputActions.Gameplay.ToggleBigMap.performed -= ToggleBigMapOnPerformed;
            _playerInputActions.Cutscene.ToggleBigMap.performed -= ToggleBigMapOnPerformed;
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private void ToggleBigMapOnPerformed(InputAction.CallbackContext obj)
        {
            ToggleBigMap();
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
            }
            else if (_gameStateManager.GetCurrentState() == GameState.Gameplay)
            {
                if (_sceneManager.GetCurrentScene().GetSceneInfo().SceneType == ScnSceneType.Maze)
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("迷宫中无法使用大地图"));
                }
                else
                {
                    Show();
                }
            }
        }

        public void Show()
        {
            if (_regionEnablementInfo.Count == 0)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("大地图尚未开启"));
                return;
            }

            _gameStateManager.GoToState(GameState.Cutscene);

            GameObject exitButton = Instantiate(_bigMapRegionButtonPrefab, _bigMapCanvas.transform);
            exitButton.GetComponentInChildren<TextMeshProUGUI>().text = "关闭";
            exitButton.GetComponent<Button>().onClick
                .AddListener(delegate { BigMapButtonClicked(-1);});
            _selectionButtons.Add(exitButton);

            for (var i = 0; i < BigMapConstants.BigMapRegions.Length; i++)
            {
                if (!_regionEnablementInfo.ContainsKey(i) || _regionEnablementInfo[i] != 2) continue;
                GameObject selectionButton = Instantiate(_bigMapRegionButtonPrefab, _bigMapCanvas.transform);
                var buttonTextUI = selectionButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonTextUI.text = BigMapConstants.BigMapRegions[i];
                var buttonIndex = i;
                var button = selectionButton.GetComponent<Button>();
                button.onClick.AddListener(delegate { BigMapButtonClicked(buttonIndex);});
                _selectionButtons.Add(selectionButton);
            }

            // Setup button navigation
            for (var i = 0; i < _selectionButtons.Count; i++)
            {
                var button = _selectionButtons[i].GetComponent<Button>();
                Navigation buttonNavigation = button.navigation;
                buttonNavigation.mode = Navigation.Mode.Explicit;

                if (i == 0)
                {
                    buttonNavigation.selectOnLeft = _selectionButtons[^1].GetComponent<Button>();
                    buttonNavigation.selectOnRight = _selectionButtons[i + 1].GetComponent<Button>();
                }
                else if (i == _selectionButtons.Count - 1)
                {
                    buttonNavigation.selectOnLeft = _selectionButtons[i - 1].GetComponent<Button>();
                    buttonNavigation.selectOnRight = _selectionButtons[0].GetComponent<Button>();
                }
                else
                {
                    buttonNavigation.selectOnLeft = _selectionButtons[i - 1].GetComponent<Button>();
                    buttonNavigation.selectOnRight = _selectionButtons[i + 1].GetComponent<Button>();
                }

                button.navigation = buttonNavigation;
            }

            var firstButton = _selectionButtons.First().GetComponent<Button>();
            _eventSystem.firstSelectedGameObject = firstButton.gameObject;

            InputDevice lastActiveInputDevice = _inputManager.GetLastActiveInputDevice();
            if (lastActiveInputDevice == Keyboard.current ||
                lastActiveInputDevice == Gamepad.current)
            {
                firstButton.Select();
            }

            if (_selectionButtons.Count > 0)
            {
                _bigMapCanvas.alpha = 1f;
                _bigMapCanvas.interactable = true;
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
        }

        public void Hide()
        {
            _bigMapCanvas.alpha = 0f;
            _bigMapCanvas.interactable = false;
            _isVisible = false;
            
            foreach (GameObject button in _selectionButtons)
            {
                button.GetComponent<Button>().onClick.RemoveAllListeners();
                Destroy(button);
            }
            _selectionButtons.Clear();
            _gameStateManager.GoToState(GameState.Gameplay);
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
            if (command.State == 13)
            {
                Show();
            }
        }

        public void Execute(ToggleBigMapRequest command)
        {
            ToggleBigMap();
        }
    }
}