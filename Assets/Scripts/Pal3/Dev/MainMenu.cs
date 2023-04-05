﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Dev
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Audio;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Utils;
    using GameSystem;
    using IngameDebugConsole;
    using Input;
    using MetaData;
    using Scene;
    using Script;
    using Script.Waiter;
    using Settings;
    using State;
    using TMPro;
    using UI;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.DualShock;
    using UnityEngine.UI;

    public sealed class MainMenu : MonoBehaviour,
        ICommandExecutor<ToggleMainMenuRequest>,
        ICommandExecutor<GameSwitchToMainMenuCommand>,
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<GameStateChangedNotification>
    {
        private const int SAVE_SLOT_COUNT = 5;

        private GameSettings _gameSettings;
        private AudioManager _audioManager;
        private InputManager _inputManager;
        private EventSystem _eventSystem;
        private PlayerInputActions _playerInputActions;
        private ScriptManager _scriptManager;
        private TeamManager _teamManager;
        private GameStateManager _gameStateManager;
        private SceneManager _sceneManager;
        private SaveManager _saveManager;
        private InformationManager _informationManager;
        private MazeSkipper _mazeSkipper;

        private CanvasGroup _mainMenuCanvasGroup;
        private GameObject _menuButtonPrefab;
        private RectTransform _backgroundTransform;
        private RectTransform _contentTransform;
        private GridLayoutGroup _contentGridLayoutGroup;
        private ScrollRect _contentScrollRect;

        private bool _isInInitView = true;
        private CancellationTokenSource _initViewCameraOrbitAnimationCts = new ();
        private Camera _mainCamera;

        private readonly List<string> _deferredExecutionCommands = new();

        private readonly List<GameObject> _menuItems = new();

        public void Init(GameSettings gameSettings,
            AudioManager audioManager,
            InputManager inputManager,
            EventSystem eventSystem,
            SceneManager sceneManager,
            GameStateManager gameStateManager,
            ScriptManager scriptManager,
            TeamManager teamManager,
            SaveManager saveManager,
            InformationManager informationManager,
            MazeSkipper mazeSkipper,
            CanvasGroup mainMenuCanvasGroup,
            GameObject menuButtonPrefab,
            ScrollRect contentScrollRect,
            RectTransform backgroundTransform,
            RectTransform contentTransform,
            Camera mainCamera)
        {
            _gameSettings = Requires.IsNotNull(gameSettings, nameof(gameSettings));
            _audioManager = Requires.IsNotNull(audioManager, nameof(audioManager));
            _inputManager = Requires.IsNotNull(inputManager, nameof(inputManager));
            _eventSystem = Requires.IsNotNull(eventSystem, nameof(eventSystem));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _gameStateManager = Requires.IsNotNull(gameStateManager, nameof(gameStateManager));
            _scriptManager = Requires.IsNotNull(scriptManager, nameof(scriptManager));
            _teamManager = Requires.IsNotNull(teamManager, nameof(teamManager));
            _saveManager = Requires.IsNotNull(saveManager, nameof(saveManager));
            _informationManager = Requires.IsNotNull(informationManager, nameof(informationManager));
            _mazeSkipper = Requires.IsNotNull(mazeSkipper, nameof(mazeSkipper));
            _mainMenuCanvasGroup = Requires.IsNotNull(mainMenuCanvasGroup, nameof(mainMenuCanvasGroup));
            _menuButtonPrefab = Requires.IsNotNull(menuButtonPrefab, nameof(menuButtonPrefab));
            _contentScrollRect = Requires.IsNotNull(contentScrollRect, nameof(contentScrollRect));
            _backgroundTransform = Requires.IsNotNull(backgroundTransform, nameof(backgroundTransform));
            _contentTransform = Requires.IsNotNull(contentTransform, nameof(contentTransform));
            _mainCamera = Requires.IsNotNull(mainCamera, nameof(mainCamera));

            _contentGridLayoutGroup = Requires.IsNotNull(
                _contentTransform.GetComponent<GridLayoutGroup>(), "ContentTransform's GridLayoutGroup");

            _playerInputActions = inputManager.GetPlayerInputActions();

            _mainMenuCanvasGroup.alpha = 0f;
            _mainMenuCanvasGroup.interactable = false;

            _playerInputActions.Gameplay.ToggleStorySelector.performed += ToggleMainMenuPerformed;
            _playerInputActions.Cutscene.ToggleStorySelector.performed += ToggleMainMenuPerformed;
            _playerInputActions.Cutscene.ExitCurrentShowingMenu.performed += HideMainMenuPerformed;
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            _playerInputActions.Gameplay.ToggleStorySelector.performed -= ToggleMainMenuPerformed;
            _playerInputActions.Cutscene.ToggleStorySelector.performed -= ToggleMainMenuPerformed;
            _playerInputActions.Cutscene.ExitCurrentShowingMenu.performed -= HideMainMenuPerformed;
        }

        private void ToggleMainMenuPerformed(InputAction.CallbackContext _)
        {
            ToggleMainMenu();
        }

        private void HideMainMenuPerformed(InputAction.CallbackContext _)
        {
            if (_isInInitView) return;

            if (_mainMenuCanvasGroup.interactable)
            {
                HideMenu();
                _gameStateManager.GoToState(GameState.Gameplay);
            }
        }

        private void ToggleMainMenu()
        {
            if (_isInInitView) return;

            if (_mainMenuCanvasGroup.interactable)
            {
                HideMenu();
                _gameStateManager.GoToState(GameState.Gameplay);
            }
            else if (_gameStateManager.GetCurrentState() == GameState.Gameplay)
            {
                ShowMenu();
            }
        }

        public void ShowInitView()
        {
            #if PAL3
            // 景天房间
            _deferredExecutionCommands.Add("SceneActivateObject 14 0");
            _deferredExecutionCommands.Add("SceneActivateObject 15 0");
            _deferredExecutionCommands.Add("PlayerEnableInput 0");
            _deferredExecutionCommands.Add("CameraFollowPlayer 0");
            _deferredExecutionCommands.Add("CameraSetTransform -33.24 -19.48 688.0 308.31 240.44 480.61");
            _deferredExecutionCommands.Add("CameraFadeIn");
            CommandDispatcher<ICommand>.Instance.Dispatch(new SceneLoadCommand("q01", "yn09a"));
            #elif PAL3A
            // 南宫煌房间
            _deferredExecutionCommands.Add("PlayerEnableInput 0");
            _deferredExecutionCommands.Add("CameraFollowPlayer 0");
            _deferredExecutionCommands.Add("CameraSetTransform -21.69 -22.48 688.0 182.87 263.07 531.61");
            _deferredExecutionCommands.Add("CameraFadeIn");
            CommandDispatcher<ICommand>.Instance.Dispatch(new SceneLoadCommand("q02", "qn08y"));
            #endif

            _audioManager.PlayThemeMusic();

            if (!_initViewCameraOrbitAnimationCts.IsCancellationRequested)
            {
                _initViewCameraOrbitAnimationCts.Cancel();
            }
            _initViewCameraOrbitAnimationCts = new CancellationTokenSource();
            StartCoroutine(StartCameraOrbitAnimationAsync(_initViewCameraOrbitAnimationCts.Token));
        }

        private IEnumerator StartCameraOrbitAnimationAsync(CancellationToken cancellationToken)
        {
            #if PAL3
            _mainCamera.fieldOfView = 14f;
            #elif PAL3A
            _mainCamera.fieldOfView = 16f;
            #endif

            yield return new WaitUntil(() => _deferredExecutionCommands.Count == 0);

            if (!_isInInitView || cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            const float animationDuration = 20f;
            WaitForSeconds waitDuration = new WaitForSeconds(animationDuration + 3f);

            while (_isInInitView && !cancellationToken.IsCancellationRequested)
            {
                #if PAL3
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new CameraOrbitHorizontalCommand(-43.99f, -28.73f, 688.0f, animationDuration, 1, 0));
                #elif PAL3A
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new CameraOrbitHorizontalCommand(-39.99f, -27.73f, 688.0f, animationDuration, 1, 0));
                #endif
                yield return waitDuration;
                if (!_isInInitView || cancellationToken.IsCancellationRequested) { yield break; }
                #if PAL3
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new CameraOrbitHorizontalCommand(-33.24f, -19.48f, 688.0f, animationDuration, 1, 0));
                #elif PAL3A
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new CameraOrbitHorizontalCommand(-21.69f, -22.48f, 688.0f, animationDuration, 1, 0));
                #endif
                yield return waitDuration;
            }
        }

        public void ShowMenu()
        {
            _gameStateManager.GoToState(GameState.Cutscene);
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            SetupMainMenuButtons();

            _mainMenuCanvasGroup.alpha = 1f;
            _mainMenuCanvasGroup.interactable = true;
        }

        public void HideMenu()
        {
            _gameSettings.SaveSettings();

            _mainMenuCanvasGroup.alpha = 0f;
            _mainMenuCanvasGroup.interactable = false;
            DestroyAllMenuItems();

            _isInInitView = false;
            if (!_initViewCameraOrbitAnimationCts.IsCancellationRequested)
            {
                _initViewCameraOrbitAnimationCts.Cancel();
            }
        }

        private void SetupMainMenuButtons()
        {
            SetupMenuLayout(isVertical: true);

            if (!_isInInitView)
            {
               CreateMenuButton("返回游戏", _ => delegate
               {
                   HideMenu();
                   _gameStateManager.GoToState(GameState.Gameplay);
               });
            }

            CreateMenuButton("新的游戏", _ => delegate
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new ResetGameStateCommand());
                HideMenu();
                StartNewGame();
            });

            if (!_isInInitView)
            {
                CreateMenuButton("保存游戏", _ => delegate
                {
                    DestroyAllMenuItems();
                    SetupSaveMenuButtons();
                });
            }

            CreateMenuButton("读取存档", _ => delegate
            {
                DestroyAllMenuItems();
                SetupLoadMenuButtons();
            });

            CreateMenuButton("剧情选择", _ => delegate
            {
                DestroyAllMenuItems();
                SetupStorySelectionButtons();
            });

            if (!_isInInitView &&
                _mazeSkipper.IsMazeSceneAndHasSkipperCommands(_sceneManager.GetCurrentScene().GetSceneInfo()))
            {
                CreateMenuButton("迷宫入口", _ => delegate
                {
                    _mazeSkipper.PortalToEntrance();
                    HideMenu();
                    _gameStateManager.GoToState(GameState.Gameplay);
                });
                CreateMenuButton("迷宫出口或剧情点", _ => delegate
                {
                    _mazeSkipper.PortalToExitOrNextStoryPoint();
                    HideMenu();
                    _gameStateManager.GoToState(GameState.Gameplay);
                });
            }

            CreateMenuButton("游戏设置", _ => delegate
            {
                DestroyAllMenuItems();
                SetupSettingButtons();
            });

            #if UNITY_STANDALONE
            CreateMenuButton("退出游戏", _ => delegate
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            });
            #endif

            SetupButtonNavigations(isUpAndDown: true);

            SelectFirstButtonForEventSystem();

            UpdateRectTransformWidthAndHeight(_backgroundTransform,
                _contentGridLayoutGroup.cellSize.x + 100f,
                (_contentGridLayoutGroup.cellSize.y + _contentGridLayoutGroup.spacing.y) * _menuItems.Count
                - _contentGridLayoutGroup.spacing.y + 100f);
        }

        private void SetupSettingButtons()
        {
            SetupMenuLayout(isVertical: true);

            const string lightingEnabledText = "实时光影：已开启";
            const string lightingDisabledText = "实时光影：已关闭";
            const string ssaoEnabledText = "环境光遮蔽：已开启";
            const string ssaoDisabledText = "环境光遮蔽：已关闭";

            // Toon materials are not available in open source build.
            // so lighting and shadow will not work, thus we remove the option.
            if (!_gameSettings.IsOpenSourceVersion)
            {
                string GetLightAndShadowButtonText() => _gameSettings.IsRealtimeLightingAndShadowsEnabled ?
                        lightingEnabledText :
                        lightingDisabledText;
                CreateMenuButton(GetLightAndShadowButtonText(), buttonTextUGUI => delegate
                {
                    // Do not allow toggling lighting and shadow if there are scripts running
                    // because it might cause unexpected behavior.
                    if (_scriptManager.GetNumberOfRunningScripts() > 0) return;

                    _gameSettings.IsRealtimeLightingAndShadowsEnabled = !_gameSettings.IsRealtimeLightingAndShadowsEnabled;

                    buttonTextUGUI.text = GetLightAndShadowButtonText();

                    // Should turn off SSAO if realtime lighting and shadows is disabled
                    if (!_gameSettings.IsRealtimeLightingAndShadowsEnabled && _gameSettings.IsAmbientOcclusionEnabled)
                    {
                        _gameSettings.IsAmbientOcclusionEnabled = false;
                        _menuItems.FirstOrDefault(_ => _.GetComponentInChildren<TextMeshProUGUI>() is {text: ssaoEnabledText})!
                            .GetComponentInChildren<TextMeshProUGUI>().text = ssaoDisabledText;
                        CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("环境光遮蔽已关闭"));
                    }

                    if (_isInInitView)
                    {
                        ShowInitView(); // Reload init scene to apply changes
                    }
                    else // reload scene to apply changes
                    {
                        var commands = _saveManager.ConvertCurrentGameStateToCommands(SaveLevel.Full);
                        var saveFileContent = string.Join('\n', commands.Select(CommandExtensions.ToString).ToList());
                        ExecuteCommandsFromSaveFile(saveFileContent);
                    }

                    CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("实时光影已" +
                        (_gameSettings.IsRealtimeLightingAndShadowsEnabled ? "开启（注意性能和耗电影响）" : "关闭") + ""));
                });

                #if !UNITY_ANDROID
                string GetSSAOButtonText() => _gameSettings.IsAmbientOcclusionEnabled ? ssaoEnabledText : ssaoDisabledText;
                CreateMenuButton(GetSSAOButtonText(), buttonTextUGUI => delegate
                {
                    if (!_gameSettings.IsAmbientOcclusionEnabled && !_gameSettings.IsRealtimeLightingAndShadowsEnabled)
                    {
                        CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("请先开启实时光影，再开启环境光遮蔽"));
                        return;
                    }

                    _gameSettings.IsAmbientOcclusionEnabled = !_gameSettings.IsAmbientOcclusionEnabled;

                    buttonTextUGUI.text = GetSSAOButtonText();

                    CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("环境光遮蔽已" +
                        (_gameSettings.IsAmbientOcclusionEnabled ? "开启（注意性能和耗电影响）" : "关闭") + ""));
                });
                #endif
            }

            #if UNITY_STANDALONE
            const string vsyncEnabledText = "垂直同步：已开启";
            const string vsyncDisabledText = "垂直同步：已关闭";
            string GetVsyncButtonText() => _gameSettings.VSyncCount == 0 ? vsyncDisabledText : vsyncEnabledText;
            CreateMenuButton(GetVsyncButtonText(), buttonTextUGUI => delegate
            {
                _gameSettings.VSyncCount = _gameSettings.VSyncCount == 0 ? 1 : 0;

                buttonTextUGUI.text = GetVsyncButtonText();

                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("垂直同步已" +
                    (_gameSettings.VSyncCount == 0 ? "关闭" : "开启") + ""));
            });
            #endif

            #if UNITY_IOS || UNITY_ANDROID
            const string fullResolutionScaleText = "渲染分辨率：100%";
            const string halfResolutionScaleText = "渲染分辨率：75%";
            string GetResolutionButtonText() => Math.Abs(_gameSettings.ResolutionScale - 1f) < 0.01f
                ? fullResolutionScaleText
                : halfResolutionScaleText;
            CreateMenuButton(GetResolutionButtonText(), buttonTextUGUI => delegate
            {
                _gameSettings.ResolutionScale = Math.Abs(_gameSettings.ResolutionScale - 1f) < 0.01f ? 0.75f : 1f;

                buttonTextUGUI.text = GetResolutionButtonText();

                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("渲染分辨率已" +
                    (Math.Abs(_gameSettings.ResolutionScale - 1f) < 0.01f ? "调整为100%" : "调整为75%") + ""));
            });
            #endif

            // #if UNITY_STANDALONE
            // Dictionary<int, string> antiAliasingText = new ()
            // {
            //     [0] = "抗锯齿：关闭",
            //     [2] = "抗锯齿：2x",
            //     [4] = "抗锯齿：4x",
            //     [8] = "抗锯齿：8x",
            // };
            //
            // string antiAliasingButtonText = antiAliasingText[0];
            // if (antiAliasingText.ContainsKey(_gameSettings.AntiAliasing))
            // {
            //     antiAliasingButtonText = antiAliasingText[_gameSettings.AntiAliasing];
            // }
            //
            // CreateMenuButton(antiAliasingButtonText, buttonTextUGUI => delegate
            // {
            //     _gameSettings.AntiAliasing = _gameSettings.AntiAliasing switch
            //     {
            //         0 => 2, 2 => 4, 4 => 8, 8 => 0, _ => 0,
            //     };
            //
            //     buttonTextUGUI.text = antiAliasingText[_gameSettings.AntiAliasing];
            //
            //     CommandDispatcher<ICommand>.Instance.Dispatch(_gameSettings.AntiAliasing == 0
            //         ? new UIDisplayNoteCommand("抗锯齿已关闭")
            //         : new UIDisplayNoteCommand("抗锯齿已开启，等级：" + _gameSettings.AntiAliasing));
            // });
            // #endif

            const string musicEnabledText = "音乐：已开启";
            const string musicDisabledText = "音乐：已关闭";
            string GetMusicButtonText() => _gameSettings.MusicVolume == 0f ? musicDisabledText : musicEnabledText;
            CreateMenuButton(GetMusicButtonText(), buttonTextUGUI => delegate
            {
                _gameSettings.MusicVolume = _gameSettings.MusicVolume == 0f ? 0.5f : 0f;

                buttonTextUGUI.text = GetMusicButtonText();

                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("音乐已" +
                    (_gameSettings.MusicVolume == 0f ? "关闭" : "开启") + ""));
            });

            const string sfxEnabledText = "音效：已开启";
            const string sfxDisabledText = "音效：已关闭";
            string GetSfxButtonText() => _gameSettings.SfxVolume == 0f ? sfxDisabledText : sfxEnabledText;
            CreateMenuButton(GetSfxButtonText(), buttonTextUGUI => delegate
            {
                _gameSettings.SfxVolume = _gameSettings.SfxVolume == 0f ? 0.5f : 0f;

                buttonTextUGUI.text = GetSfxButtonText();

                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("音效已" +
                    (_gameSettings.SfxVolume == 0f ? "关闭" : "开启") + ""));
            });

            CreateMenuButton("返回", _ => delegate
            {
                _gameSettings.SaveSettings();
                DestroyAllMenuItems();
                SetupMainMenuButtons();
            });

            SetupButtonNavigations(isUpAndDown: true);

            SelectFirstButtonForEventSystem();

            UpdateRectTransformWidthAndHeight(_backgroundTransform,
                _contentGridLayoutGroup.cellSize.x + 100f,
                (_contentGridLayoutGroup.cellSize.y + _contentGridLayoutGroup.spacing.y) * _menuItems.Count
                - _contentGridLayoutGroup.spacing.y + 100f);
        }

        private void SetupSaveMenuButtons()
        {
            SetupMenuLayout(isVertical: true);

            const string saveSlotButtonFormat = "存档位 {0}：{1}";
            // 0 is reserved for auto-save
            for (int i = 1; i <= SAVE_SLOT_COUNT; i++)
            {
                string GetSlotButtonText() => _saveManager.SaveSlotExists(i) ? string.Format(saveSlotButtonFormat, i,
                    _saveManager.GetSaveSlotLastWriteTime(i)) : string.Format(saveSlotButtonFormat, i, "空");

                var slotIndex = i;
                CreateMenuButton(GetSlotButtonText(), buttonTextUGUI => delegate
                {
                    IList<ICommand> gameStateCommands = _saveManager.ConvertCurrentGameStateToCommands(SaveLevel.Full);
                    bool success = _saveManager.SaveGameStateToSlot(slotIndex, gameStateCommands);
                    if (success)
                    {
                        buttonTextUGUI.text = string.Format(saveSlotButtonFormat, slotIndex,
                            _saveManager.GetSaveSlotLastWriteTime(slotIndex));
                    }
                    CommandDispatcher<ICommand>.Instance.Dispatch(success
                        ? new UIDisplayNoteCommand("游戏保存成功")
                        : new UIDisplayNoteCommand("游戏保存失败"));
                });
            }

            CreateMenuButton("返回", _ => delegate
            {
                DestroyAllMenuItems();
                SetupMainMenuButtons();
            });

            SetupButtonNavigations(isUpAndDown: true);

            SelectFirstButtonForEventSystem();

            UpdateRectTransformWidthAndHeight(_backgroundTransform,
                _contentGridLayoutGroup.cellSize.x + 100f,
                (_contentGridLayoutGroup.cellSize.y + _contentGridLayoutGroup.spacing.y) * _menuItems.Count
                - _contentGridLayoutGroup.spacing.y + 100f);
        }

        private void SetupLoadMenuButtons()
        {
            SetupMenuLayout(isVertical: true);

            bool autoSaveSlotExists = _saveManager.SaveSlotExists(SaveManager.AutoSaveSlotIndex);
            string GetAutoSaveSlotButtonText() => autoSaveSlotExists ?
                $"自动存档：{_saveManager.GetSaveSlotLastWriteTime(SaveManager.AutoSaveSlotIndex)}" : "自动存档：无";

            CreateMenuButton(GetAutoSaveSlotButtonText(), _ => delegate
            {
                if (!autoSaveSlotExists) return;
                var saveFileContent = _saveManager.LoadFromSaveSlot(SaveManager.AutoSaveSlotIndex);
                if (saveFileContent != null)
                {
                    HideMenu();
                    ExecuteCommandsFromSaveFile(saveFileContent);
                }
                else
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch( new UIDisplayNoteCommand("存档文件不存在或读取失败"));
                }
            });

            const string saveSlotButtonFormat = "存档位 {0}：{1}";
            // 0 is reserved for auto-save
            for (int i = 1; i <= SAVE_SLOT_COUNT; i++)
            {
                bool slotExists = _saveManager.SaveSlotExists(i);
                string slotButtonText = slotExists ? string.Format(saveSlotButtonFormat, i,
                    _saveManager.GetSaveSlotLastWriteTime(i)) : string.Format(saveSlotButtonFormat, i, "空");

                var slotIndex = i;
                if (slotExists)
                {
                    CreateMenuButton(slotButtonText, _ => delegate
                    {
                        var saveFileContent = _saveManager.LoadFromSaveSlot(slotIndex);
                        if (saveFileContent != null)
                        {
                            HideMenu();
                            ExecuteCommandsFromSaveFile(saveFileContent);
                        }
                        else
                        {
                            CommandDispatcher<ICommand>.Instance.Dispatch( new UIDisplayNoteCommand("存档文件不存在或读取失败"));
                        }
                    });
                }
                else
                {
                    CreateMenuButton(slotButtonText, null);
                }
            }

            CreateMenuButton("返回", _ => delegate
            {
                DestroyAllMenuItems();
                SetupMainMenuButtons();
            });

            SetupButtonNavigations(isUpAndDown: true);

            SelectFirstButtonForEventSystem();

            UpdateRectTransformWidthAndHeight(_backgroundTransform,
                _contentGridLayoutGroup.cellSize.x + 100f,
                (_contentGridLayoutGroup.cellSize.y + _contentGridLayoutGroup.spacing.y) * _menuItems.Count
                - _contentGridLayoutGroup.spacing.y + 100f);
        }

        private void SetupStorySelectionButtons()
        {
            SetupMenuLayout(isVertical: false);

            CreateMenuButton("返回", _ => delegate
            {
                _contentTransform.anchoredPosition = Vector2.zero;
                DestroyAllMenuItems();
                SetupMainMenuButtons();
            });

            foreach (var story in DevCommands.StoryJumpPoints)
            {
                CreateMenuButton(story.Key, _ => delegate
                {
                    HideMenu();
                    _saveManager.IsAutoSaveEnabled = false; // Disable auto save during loading
                    CommandDispatcher<ICommand>.Instance.Dispatch(new ResetGameStateCommand());
                    ExecuteCommands(story.Value);
                });
            }

            SetupButtonNavigations(isUpAndDown: false);

            SelectFirstButtonForEventSystem();

            UpdateRectTransformWidthAndHeight(_backgroundTransform,
                (_contentGridLayoutGroup.cellSize.x + _contentGridLayoutGroup.spacing.x) * _menuItems.Count
                - _contentGridLayoutGroup.spacing.x + 100f,
                _contentGridLayoutGroup.cellSize.y + 100f);
        }

        private void UpdateRectTransformWidthAndHeight(RectTransform rectTransform, float width, float height)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            rectTransform.ForceUpdateRectTransforms();
        }

        private void SelectFirstButtonForEventSystem()
        {
            InputDevice lastActiveInputDevice = _inputManager.GetLastActiveInputDevice();

            if (lastActiveInputDevice == Keyboard.current ||
                lastActiveInputDevice == Gamepad.current ||
                lastActiveInputDevice == DualShockGamepad.current)
            {
                Button firstButton = _menuItems.First(_ => _.GetComponent<Button>() != null).GetComponent<Button>();
                _eventSystem.firstSelectedGameObject = firstButton.gameObject;
                firstButton.Select();
            }
            else
            {
                _eventSystem.firstSelectedGameObject = null;
            }
        }

        private void CreateMenuButton(string text, Func<TextMeshProUGUI, UnityAction> onSelection)
        {
            GameObject menuButtonGo = Instantiate(_menuButtonPrefab, _contentTransform);
            var buttonTextUI = menuButtonGo.GetComponentInChildren<TextMeshProUGUI>();
            buttonTextUI.text = text;
            var button = menuButtonGo.GetComponent<Button>();
            button.colors = UITheme.GetButtonColors();
            if (onSelection != null)
            {
                button.onClick.AddListener(onSelection(buttonTextUI));
            }
            _menuItems.Add(menuButtonGo);
        }

        private void SetupMenuLayout(bool isVertical)
        {
            if (isVertical)
            {
                _contentGridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                _contentGridLayoutGroup.constraintCount = 1;
                _contentGridLayoutGroup.cellSize = new Vector2(500, 75);
                _contentGridLayoutGroup.spacing = new Vector2(20, 20);
                _contentScrollRect.horizontal = false;
                _contentScrollRect.vertical = false;
            }
            else
            {
                _contentGridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                _contentGridLayoutGroup.constraintCount = 1;
                _contentGridLayoutGroup.cellSize = new Vector2(45, 500);
                _contentGridLayoutGroup.spacing = new Vector2(10, 30);
                _contentScrollRect.horizontal = true;
                _contentScrollRect.vertical = false;
            }
        }

        private void SetupButtonNavigations(bool isUpAndDown)
        {
            // Setup button navigation
            void ConfigureButtonNavigation(Button button, int index, int count)
            {
                Navigation buttonNavigation = button.navigation;
                buttonNavigation.mode = Navigation.Mode.Explicit;

                int previousIndex = index == 0 ? count - 1 : index - 1;
                int nextIndex = index == count - 1 ? 0 : index + 1;

                if (isUpAndDown)
                {
                    buttonNavigation.selectOnUp = _menuItems[previousIndex].GetComponentInChildren<Button>();
                    buttonNavigation.selectOnDown = _menuItems[nextIndex].GetComponentInChildren<Button>();
                }
                else
                {
                    buttonNavigation.selectOnLeft = _menuItems[previousIndex].GetComponentInChildren<Button>();
                    buttonNavigation.selectOnRight = _menuItems[nextIndex].GetComponentInChildren<Button>();
                }

                button.navigation = buttonNavigation;
            }

            for (var i = 0; i < _menuItems.Count; i++)
            {
                var button = _menuItems[i].GetComponentInChildren<Button>();
                ConfigureButtonNavigation(button, i, _menuItems.Count);
            }
        }

        private void DestroyAllMenuItems()
        {
            foreach (GameObject item in _menuItems)
            {
                item.GetComponent<Button>()?.onClick.RemoveAllListeners();
                Destroy(item);
            }
            _menuItems.Clear();
        }

        private void StartNewGame()
        {
            // Init main story progress
            _scriptManager.SetGlobalVariable(ScriptConstants.MainStoryVariableName, 0);

            // Add init script
            _scriptManager.AddScript(ScriptConstants.InitScriptId);

            // Add main actor to the team
            _teamManager.AddActor(0);

            _gameStateManager.GoToState(GameState.Cutscene);
        }

        private void ExecuteCommands(string commands)
        {
            foreach (var command in commands.Split('\n'))
            {
                if (!string.IsNullOrEmpty(command))
                {
                    DebugLogConsole.ExecuteCommand(command);
                }
            }
        }

        private void ExecuteCommandsFromSaveFile(string saveFileContent)
        {
            _informationManager.EnableNoteDisplay(false);
            _saveManager.IsAutoSaveEnabled = false; // Disable auto save during loading

            _deferredExecutionCommands.Clear();

            var commandsToExecute = new List<string>();

            foreach (var command in saveFileContent.Split('\n'))
            {
                if (string.IsNullOrEmpty(command)) continue;

                var arguments = new List<string>();

                DebugLogConsole.FetchArgumentsFromCommand(command, arguments);

                // These commands should be executed after the scene script is executed
                // to prevent unexpected behavior
                if (arguments[0] + "Command" == nameof(ActorActivateCommand) ||
                    arguments[0] + "Command" == nameof(ActorSetNavLayerCommand) ||
                    arguments[0] + "Command" == nameof(ActorSetWorldPositionCommand) ||
                    arguments[0] + "Command" == nameof(ActorSetYPositionCommand) ||
                    arguments[0] + "Command" == nameof(ActorSetFacingCommand) ||
                    arguments[0] + "Command" == nameof(ActorSetScriptCommand) ||
                    arguments[0] + "Command" == nameof(CameraFadeInCommand))
                {
                    _deferredExecutionCommands.Add(command);
                }
                else
                {
                    commandsToExecute.Add(command);
                }
            }

            foreach (var command in commandsToExecute)
            {
                DebugLogConsole.ExecuteCommand(command);
            }

            _informationManager.EnableNoteDisplay(true);
        }

        public void Execute(ToggleMainMenuRequest command)
        {
            ToggleMainMenu();
        }

        public void Execute(GameSwitchToMainMenuCommand command)
        {
            _isInInitView = true;
            DestroyAllMenuItems();
            ShowInitView();
            ShowMenu();
        }

        public void Execute(ScenePostLoadingNotification command)
        {
            if (_deferredExecutionCommands.Count == 0)
            {
                if (!_isInInitView)
                {
                    _saveManager.IsAutoSaveEnabled = true;
                }
                return;
            }

            if (command.SceneScriptId == ScriptConstants.InvalidScriptId)
            {
                ExecuteCommands(string.Join('\n', _deferredExecutionCommands));
                _deferredExecutionCommands.Clear();
                if (!_isInInitView)
                {
                    _saveManager.IsAutoSaveEnabled = true;
                }
            }
            else
            {
                StartCoroutine(ExecuteDeferredCommandsAfterSceneScriptFinishedAsync(command.SceneScriptId));
            }
        }

        private IEnumerator ExecuteDeferredCommandsAfterSceneScriptFinishedAsync(uint scriptId)
        {
            yield return new WaitUntilScriptFinished(PalScriptType.Scene, scriptId);

            if (_deferredExecutionCommands.Count > 0)
            {
                ExecuteCommands(string.Join('\n', _deferredExecutionCommands));
                _deferredExecutionCommands.Clear();
            }

            if (!_isInInitView)
            {
                _saveManager.IsAutoSaveEnabled = true;
            }
        }

        public void Execute(GameStateChangedNotification command)
        {
            // If menu is still active, go to cutscene state
            // When turn on/off real time lighting and shadow, the game will reload the scene
            // and run scene script again, so we need to check if the menu is still active
            // and set the game state to cutscene
            if (_mainMenuCanvasGroup.interactable)
            {
                _gameStateManager.GoToState(GameState.Cutscene);
            }
        }
    }
}