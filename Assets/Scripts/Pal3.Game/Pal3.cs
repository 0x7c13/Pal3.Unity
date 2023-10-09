// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Actor.Controllers;
    using Audio;
    using Camera;
    using Command;
    using Command.Extensions;
    using Constants;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.DataReader.Scn;
    using Core.FileSystem;
    using Core.Utilities;
    using Data;
    using Dev;
    using Effect;
    using Engine.Abstraction;
    using Engine.Coroutine;
    using Engine.Extensions;
    using Engine.Logging;
    using Engine.Services;
    using Engine.Utilities;
    using GamePlay;
    using GameSystems.Caption;
    using GameSystems.Combat;
    using GameSystems.Dialogue;
    using GameSystems.Favor;
    using GameSystems.Inventory;
    using GameSystems.MiniGames;
    using GameSystems.Minimap;
    using GameSystems.Rest;
    using GameSystems.Team;
    using GameSystems.Trading;
    using GameSystems.WorldMap;
    using Input;
    using IngameDebugConsole;
    using Scene;
    using Script;
    using Script.Patcher;
    using Settings;
    using State;
    using TMPro;
    using UI;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.Controls;
    using UnityEngine.InputSystem.LowLevel;
    using UnityEngine.Rendering.PostProcessing;
    using UnityEngine.UI;
    using UnityEngine.Video;
    using Video;
    using PostProcessManager = Effect.PostProcessing.PostProcessManager;

    #if PAL3A
    using GameSystems.Task;
    #endif

    /// <summary>
    /// Pal3 game model
    /// </summary>
    public sealed class Pal3 : Singleton<Pal3>
    {
        // Camera
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private Image curtainImage;

        // Audio
        [SerializeField] private AudioSource musicSource;

        // Video
        [SerializeField] private Canvas videoPlayerCanvas;
        [SerializeField] private VideoPlayer videoPlayer;

        // Information
        [SerializeField] private CanvasGroup noteCanvasGroup;
        [SerializeField] private TextMeshProUGUI noteText;
        [SerializeField] private TextMeshProUGUI taskInfoText;
        [SerializeField] private CanvasGroup miniMapCanvasGroup;
        [SerializeField] private Image miniMapImage;

        // Dialogue
        [SerializeField] private CanvasGroup dialogueCanvasGroup;
        [SerializeField] private Image dialogueBackgroundImage;
        [SerializeField] private Image dialogueAvatarImageLeft;
        [SerializeField] private Image dialogueAvatarImageRight;
        [SerializeField] private TextMeshProUGUI dialogueTextLeft;
        [SerializeField] private TextMeshProUGUI dialogueTextRight;
        [SerializeField] private TextMeshProUGUI dialogueTextDefault;
        [SerializeField] private Canvas dialogueSelectionButtonsCanvas;
        [SerializeField] private GameObject dialogueSelectionButtonPrefab;

        // Caption
        [SerializeField] private Image captionImage;

        // Debug
        [SerializeField] private TextMeshProUGUI debugInfo;
        [SerializeField] private FpsCounter fpsCounter;

        // WorldMap
        [SerializeField] private CanvasGroup worldMapCanvasGroup;
        [SerializeField] private GameObject worldMapRegionButtonPrefab;
        [SerializeField] private GameObject worldMapBackground;

        // Main menu
        [SerializeField] private CanvasGroup mainMenuCanvasGroup;
        [SerializeField] private GameObject menuButtonPrefab;
        [SerializeField] private RectTransform backgroundTransform;
        [SerializeField] private RectTransform contentTransform;
        [SerializeField] private ScrollRect contentScrollRect;

        // Touch control
        [SerializeField] private Canvas touchControlUI;
        [SerializeField] private Button interactionButton;
        [SerializeField] private Button multiFunctionButton;
        [SerializeField] private Button mainMenuButton;

        // Event system
        [SerializeField] private EventSystem eventSystem;

        // Post-process volume and layer
        [SerializeField] private PostProcessVolume postProcessVolume;
        [SerializeField] private PostProcessLayer postProcessLayer;

        // Game logo
        [SerializeField] private GameObject logoCanvas;
        [SerializeField] private Image logoImage;

        // Global texture cache store
        private readonly TextureCache _textureCache = new ();

        // Core game systems and components
        private GameTimeProvider _gameTimeProvider;
        private GameSettings _gameSettings;
        private ICpkFileSystem _fileSystem;
        private GameResourceProvider _gameResourceProvider;
        private FileSystemCacheManager _fileSystemCacheManager;
        private PlayerInputActions _inputActions;
        private InputManager _inputManager;
        private GameStateManager _gameStateManager;
        private SceneStateManager _sceneStateManager;
        private ScriptManager _scriptManager;
        private VideoManager _videoManager;
        private SceneManager _sceneManager;
        private CameraManager _cameraManager;
        private AudioManager _audioManager;
        private PlayerActorManager _playerActorManager;
        private InventoryManager _inventoryManager;
        private TradingManager _tradingManager;
        private DialogueManager _dialogueManager;
        private PostProcessManager _postProcessManager;
        private EffectManager _effectManager;
        private MinimapManager _minimapManager;
        private TouchControlUIManager _touchControlUIManager;
        private PlayerGamePlayManager _playerGamePlayManager;
        private TeamManager _teamManager;
        private HotelManager _hotelManager;
        private InformationManager _informationManager;
        private WorldMapManager _worldMapManager;
        private FavorManager _favorManager;
        private CaptionRenderer _captionRenderer;
        private CursorManager _cursorManager;
        private SaveManager _saveManager;
        private RenderingSettingsManager _renderingSettingsManager;
        private CombatManager _combatManager;
        private CombatCoordinator _combatCoordinator;

        #if PAL3 // PAL3 specific components
        private AppraisalsMiniGame _appraisalsMiniGame;
        private SailingMiniGame _sailingMiniGame;
        private HideFightMiniGame _hideFightMiniGame;
        private EncampMiniGame _encampMiniGame;
        private SkiMiniGame _skiMiniGame;
        private SwatAFlyMiniGame _swatAFlyMiniGame;
        private CaveExperienceMiniGame _caveExperienceMiniGame;
        #elif PAL3A // PAL3A specific components
        private GhostHuntingMiniGame _ghostHuntingMiniGame;
        private TaskManager _taskManager;
        #endif

        // Dev tools
        private MazeSkipper _mazeSkipper;
        private MainMenu _mainMenu;

        private IEnumerable<object> _allDisposableServices;

        private void OnEnable()
        {
            EngineLogger.Log("Game setup and initialization started...");

            ServiceLocator.Instance.Register<IGameTimeProvider>(_gameTimeProvider =
                new GameTimeProvider()
            );

            IGameEntity cameraEntity = new GameEntity(mainCamera.gameObject);

            _gameSettings = ServiceLocator.Instance.Get<GameSettings>();
            _fileSystem = ServiceLocator.Instance.Get<ICpkFileSystem>();
            _gameResourceProvider = ServiceLocator.Instance.Get<GameResourceProvider>();
            _gameResourceProvider.UseTextureCache(_textureCache);

            ServiceLocator.Instance.Register(_fileSystemCacheManager =
                new FileSystemCacheManager(_fileSystem)
            );

            ServiceLocator.Instance.Register(_inputActions =
                new PlayerInputActions()
            );

            ServiceLocator.Instance.Register(_inputManager =
                new InputManager(_inputActions)
            );

            ServiceLocator.Instance.Register(_scriptManager =
                new ScriptManager(_gameResourceProvider,
                    new PalScriptCommandPreprocessor(new PalScriptPatcher()))
            );

            ServiceLocator.Instance.Register(_gameStateManager =
                new GameStateManager(_inputManager, _scriptManager)
            );

            ServiceLocator.Instance.Register(_sceneStateManager =
                new SceneStateManager());

            ServiceLocator.Instance.Register(_sceneManager =
                new SceneManager(_gameResourceProvider,
                    _sceneStateManager,
                    _scriptManager,
                    _gameSettings,
                    cameraEntity)
            );

            ServiceLocator.Instance.Register(_playerActorManager =
                new PlayerActorManager()
            );

            ServiceLocator.Instance.Register(_inventoryManager =
                new InventoryManager(_gameResourceProvider)
            );

            ServiceLocator.Instance.Register(_tradingManager =
                new TradingManager()
            );

            ServiceLocator.Instance.Register(_teamManager =
                new TeamManager(_playerActorManager, _sceneManager)
            );

            ServiceLocator.Instance.Register(_touchControlUIManager =
                new TouchControlUIManager(_sceneManager,
                    touchControlUI,
                    interactionButton,
                    multiFunctionButton,
                    mainMenuButton)
            );

            ServiceLocator.Instance.Register(_favorManager =
                new FavorManager()
            );

            ServiceLocator.Instance.Register(_videoManager =
                new VideoManager(_gameResourceProvider,
                    _gameStateManager,
                    _inputActions,
                    videoPlayerCanvas,
                    videoPlayer)
            );

            ServiceLocator.Instance.Register(_audioManager =
                new AudioManager(cameraEntity,
                    _gameResourceProvider,
                    _sceneManager,
                    musicSource,
                    _gameSettings)
            );

            ServiceLocator.Instance.Register(_captionRenderer =
                new CaptionRenderer(_gameResourceProvider,
                    _inputActions,
                    captionImage)
            );

            ServiceLocator.Instance.Register(_hotelManager =
                new HotelManager(_scriptManager, _sceneManager)
            );

            ServiceLocator.Instance.Register(_worldMapManager =
                new WorldMapManager(eventSystem,
                    _gameStateManager,
                   _sceneManager,
                   _inputManager,
                   _scriptManager,
                   worldMapCanvasGroup,
                   worldMapRegionButtonPrefab,
                   worldMapBackground)
            );

            ServiceLocator.Instance.Register(_postProcessManager =
                new PostProcessManager(postProcessVolume,
                    postProcessLayer,
                    _gameSettings)
            );

            ServiceLocator.Instance.Register(_effectManager =
                new EffectManager(_gameResourceProvider, _sceneManager)
            );

            ServiceLocator.Instance.Register(_mazeSkipper =
                new MazeSkipper(_sceneManager)
            );

            ServiceLocator.Instance.Register(_renderingSettingsManager =
                new RenderingSettingsManager(_gameSettings)
            );

            #if UNITY_STANDALONE || UNITY_EDITOR
            ServiceLocator.Instance.Register(_cursorManager =
                new CursorManager(_gameResourceProvider)
            );
            #endif

            #if PAL3
            ServiceLocator.Instance.Register(_appraisalsMiniGame =
                new AppraisalsMiniGame()
            );
            ServiceLocator.Instance.Register(_sailingMiniGame =
                new SailingMiniGame()
            );
            ServiceLocator.Instance.Register(_hideFightMiniGame =
                new HideFightMiniGame()
            );
            ServiceLocator.Instance.Register(_encampMiniGame =
                new EncampMiniGame()
            );
            ServiceLocator.Instance.Register(_skiMiniGame =
                new SkiMiniGame(_scriptManager)
            );
            ServiceLocator.Instance.Register(_swatAFlyMiniGame =
                new SwatAFlyMiniGame()
            );
            ServiceLocator.Instance.Register(_caveExperienceMiniGame =
                new CaveExperienceMiniGame()
            );
            #elif PAL3A
            ServiceLocator.Instance.Register(_ghostHuntingMiniGame =
                new GhostHuntingMiniGame()
            );
            ServiceLocator.Instance.Register(_taskManager =
                new TaskManager(_gameResourceProvider, taskInfoText)
            );
            #endif

            ServiceLocator.Instance.Register(_cameraManager =
                new CameraManager(_inputActions,
                    _sceneManager,
                    _gameStateManager,
                    _playerActorManager,
                    mainCamera,
                    cameraEntity.Transform,
                    touchControlUI,
                    curtainImage)
            );

            ServiceLocator.Instance.Register(_playerGamePlayManager =
                new PlayerGamePlayManager(_gameTimeProvider,
                    _gameResourceProvider,
                    _gameStateManager,
                    _playerActorManager,
                    _teamManager,
                    _inputActions,
                    _sceneManager,
                    _cameraManager)
            );

            ServiceLocator.Instance.Register(_minimapManager =
                new MinimapManager(cameraEntity.Transform,
                    _sceneManager,
                    miniMapCanvasGroup,
                    miniMapImage,
                    new MinimapTextureCreator(
                        UITheme.MinimapObstacleColor,
                        UITheme.MinimapWallColor,
                        UITheme.MinimapFloorColor))
            );

            ServiceLocator.Instance.Register(_informationManager =
                new InformationManager(_gameTimeProvider,
                    _gameSettings,
                    fpsCounter,
                    noteCanvasGroup,
                    noteText,
                    debugInfo)
            );

            ServiceLocator.Instance.Register(_dialogueManager =
                new DialogueManager(_gameTimeProvider,
                    _gameResourceProvider,
                    _gameStateManager,
                    _sceneManager,
                    _inputManager,
                    eventSystem,
                    dialogueCanvasGroup,
                    dialogueBackgroundImage,
                    dialogueAvatarImageLeft,
                    dialogueAvatarImageRight,
                    dialogueTextLeft,
                    dialogueTextRight,
                    dialogueTextDefault,
                    dialogueSelectionButtonsCanvas,
                    dialogueSelectionButtonPrefab)
            );

            ServiceLocator.Instance.Register(_combatManager =
                new CombatManager(_gameResourceProvider,
                    _teamManager,
                    _cameraManager,
                    _sceneManager)
            );

            ServiceLocator.Instance.Register(_combatCoordinator =
                new CombatCoordinator(_gameResourceProvider,
                    _gameSettings,
                    _combatManager,
                    _playerActorManager,
                    _audioManager,
                    _sceneManager,
                    _gameStateManager)
            );

            ServiceLocator.Instance.Register(_saveManager =
                new SaveManager(_gameTimeProvider,
                    _sceneManager,
                    _playerActorManager,
                    _teamManager,
                    _inventoryManager,
                    _sceneStateManager,
                    _worldMapManager,
                    _scriptManager,
                    _favorManager,
                    #if PAL3A
                    _taskManager,
                    #endif
                    _cameraManager,
                    _audioManager,
                    _postProcessManager)
            );

            ServiceLocator.Instance.Register(_mainMenu =
                new MainMenu(_gameSettings,
                    _inputManager,
                    _sceneManager,
                    _gameStateManager,
                    _scriptManager,
                    _teamManager,
                    _saveManager,
                    _informationManager,
                    _mazeSkipper,
                    mainMenuCanvasGroup,
                    menuButtonPrefab,
                    contentScrollRect,
                    backgroundTransform,
                    contentTransform,
                    eventSystem)
            );

            _allDisposableServices = ServiceLocator.Instance.GetAllRegisteredServices().Where(o => o is IDisposable);

            DebugLogManager.Instance.OnLogWindowShown += OnDebugWindowShown;
            DebugLogManager.Instance.OnLogWindowHidden += OnDebugWindowHidden;

            DebugLogConsole.AddCommand("state", "Get current game state in commands form.", PrintCurrentGameStateInCommandsForm);
            DebugLogConsole.AddCommand("info", "Get current game info.", PrintCurrentGameInfo);
            DebugLogConsole.AddCommand<int>("fps", "Set target FPS.", SetTargetFps);
            DebugLogConsole.AddCommand<float>("fov", "Set camera FOV.", SetCameraFov);

            DisableInGameDebugConsoleButtonNavigation();

            EngineLogger.Log("Game initialized");
        }

        private void Start()
        {
            EngineLogger.Log("Game started");

            _mainMenu.ShowInitView();

            // Show logo image.
            logoImage.sprite = _gameResourceProvider.GetLogoSprite();
            logoImage.preserveAspect = true;
            #if PAL3
            logoImage.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            #elif PAL3A
            logoImage.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            #endif
            logoImage.enabled = true;

            // Listen to input events to detect any key or touch press
            // to hide the logo and show the main menu.
            InputSystem.onEvent += OnInputEvent;

            #if !UNITY_EDITOR
            // Check latest version and notify if a newer version is found.
            StartCoroutine(CheckLatestVersionAndNotifyAsync());
            #endif
        }

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) return;

            var controls = device.allControls;
            var buttonPressPoint = InputSystem.settings.defaultButtonPressPoint;

            foreach (InputControl control in controls)
            {
                if (control.synthetic || control.noisy) continue;

                switch (control)
                {
                    case TouchControl:
                        OnAnyKeyOrTouchTriggered();
                        return;
                    case ButtonControl buttonControl when
                        buttonControl.ReadValueFromEvent(eventPtr, out var value) &&
                        value >= buttonPressPoint:
                        OnAnyKeyOrTouchTriggered();
                        return;
                }
            }

            return;

            void OnAnyKeyOrTouchTriggered()
            {
                if (!Application.isPlaying) return;

                InputSystem.onEvent -= OnInputEvent; // Only listen to the first touch event.
                logoImage.sprite.texture.Destroy();
                logoImage.sprite.Destroy();
                logoImage.Destroy();
                logoCanvas.Destroy();
                StartCoroutine(ShowMainMenuAfterLogoAsync());
            }
        }

        private IEnumerator ShowMainMenuAfterLogoAsync()
        {
            // Wait one frame to make sure the logo image is destroyed.
            // Also make sure InputManager captures the active input device, so
            // that the main menu can auto-select the first button if necessary.
            yield return null;
            _mainMenu.ShowMenu();
        }

        private IEnumerator CheckLatestVersionAndNotifyAsync()
        {
            yield return CoroutineYieldInstruction.WaitForSeconds(1f);
            yield return GithubReleaseVersionFetcher.GetLatestReleaseVersionAsync(GameConstants.GithubRepoOwner, GameConstants.GithubRepoName,
                latestVersion =>
                {
                    if (!string.IsNullOrEmpty(latestVersion) &&
                        CoreUtility.IsVersionGreater(latestVersion.Replace("v", "", StringComparison.OrdinalIgnoreCase), Application.version))
                    {
                        CommandDispatcher<ICommand>.Instance.Dispatch(
                            #if UNITY_IOS
                            new UIDisplayNoteCommand($"检测到新版本：{latestVersion}，请用TestFlight应用更新。"));
                            #else
                            new UIDisplayNoteCommand($"检测到新版本：{latestVersion}，请在讨论群内或者Github下载。"));
                            #endif
                    }
                });
        }

        /// <summary>
        /// Main game loop.
        /// </summary>
        private void Update()
        {
            var deltaTime = Time.deltaTime;
            _gameTimeProvider.Tick(deltaTime);

            GameState currentState = _gameStateManager.GetCurrentState();
            if (currentState != GameState.VideoPlaying &&
                currentState != GameState.Combat)
            {
                _scriptManager.Update(deltaTime);
                _dialogueManager.Update(deltaTime);
            }

            if (currentState == GameState.Combat)
            {
                _combatManager.Update(deltaTime);
            }

            _playerGamePlayManager.Update(deltaTime);
            _informationManager.Update(deltaTime);
            _renderingSettingsManager.Update(deltaTime);
        }

        /// <summary>
        /// Main game late update loop.
        /// </summary>
        private void LateUpdate()
        {
            var deltaTime = Time.deltaTime;

            GameState currentState = _gameStateManager.GetCurrentState();
            if (currentState != GameState.VideoPlaying &&
                currentState != GameState.Combat)
            {
                _cameraManager.LateUpdate(deltaTime);
                _minimapManager.LateUpdate(deltaTime);
            }
        }

        private void OnDisable()
        {
            DebugLogManager.Instance.OnLogWindowShown -= OnDebugWindowShown;
            DebugLogManager.Instance.OnLogWindowHidden -= OnDebugWindowHidden;

            foreach (IDisposable service in _allDisposableServices)
            {
                EngineLogger.Log($"Disposing service: [{service.GetType().Name}]");
                service.Dispose();
            }

            EngineLogger.Log("Game exited");
        }

        private void OnDebugWindowShown()
        {
            _gameStateManager.EnterDebugState();
        }

        private void OnDebugWindowHidden()
        {
            _gameStateManager.LeaveDebugState();
            // We need to do this since InGameDebugConsole will reset
            // it's button navigation when it's hidden (not sure why tho).
            DisableInGameDebugConsoleButtonNavigation();
        }

        // Disable button navigation for InGameDebugConsole
        private void DisableInGameDebugConsoleButtonNavigation()
        {
            foreach (Button button in DebugLogManager.Instance.gameObject.GetComponentsInChildren<Button>())
            {
                button.navigation = new Navigation()
                {
                    mode = Navigation.Mode.None
                };
            }
        }

        private void PrintCurrentGameStateInCommandsForm()
        {
            var commands = _saveManager.ConvertCurrentGameStateToCommands(SaveLevel.Minimal);
            var state = commands == null ? null : string.Join('\n', commands.Select(CommandExtensions.ToString).ToList());
            EngineLogger.Log(state + '\n');
        }

        private void PrintCurrentGameInfo()
        {
            if (_sceneManager.GetCurrentScene() is not { } currentScene) return;

            var info = new StringBuilder();

            ScnSceneInfo currentSceneInfo = currentScene.GetSceneInfo();

            info.Append($"----- Scene info -----\n" +
                        $"{currentSceneInfo.ToString()}\n");

            var playerActorMovementController = currentScene
                .GetActorGameEntity((int) _playerActorManager.GetPlayerActor()).GetComponent<ActorMovementController>();

            info.Append($"----- Player info -----\n" +
                        $"Nav layer: {playerActorMovementController.GetCurrentLayerIndex()}\n" +
                        $"Tile position: {playerActorMovementController.GetTilePosition()}\n");

            info.Append("----- Team info -----\n" +
                        $"Actors in team: {string.Join(", ", _teamManager.GetActorsInTeam().Select(_ => _.ToString()))}\n");

            info.Append(_scriptManager.GetGlobalVariables()
                .Aggregate("----- Variables info -----\n", (current, variable) => current + $"{variable.Key}: {variable.Value}\n"));

            info.Append(_inventoryManager);

            EngineLogger.Log(info.ToString() + '\n');
        }

        private void SetTargetFps(int targetFps)
        {
            Application.targetFrameRate = targetFps;
        }

        private void SetCameraFov(float fieldOfView)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraSetFieldOfViewCommand(fieldOfView));
        }
    }
}