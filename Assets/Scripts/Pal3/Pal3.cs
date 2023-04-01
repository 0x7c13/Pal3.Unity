// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3
{
    using System;
    using System.Linq;
    using System.Text;
    using Actor;
    using Audio;
    using Camera;
    using Command;
    using Core.DataReader.Scn;
    using Core.FileSystem;
    using Core.Services;
    using Core.Utils;
    using Data;
    using Dev;
    using Effect;
    using Feature;
    using Input;
    using IngameDebugConsole;
    using Player;
    using Scene;
    using Script;
    using Settings;
    using State;
    using TMPro;
    using UI;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Rendering.PostProcessing;
    using UnityEngine.UI;
    using UnityEngine.Video;
    using Video;
    using PostProcessManager = Effect.PostProcessing.PostProcessManager;

    #if PAL3
    using MiniGame;
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

        // BigMap
        [SerializeField] private CanvasGroup bigMapCanvasGroup;
        [SerializeField] private GameObject bigMapRegionButtonPrefab;

        // Main menu
        [SerializeField] private CanvasGroup mainMenuCanvasGroup;
        [SerializeField] private GameObject titlePrefab;
        [SerializeField] private GameObject mainMenuButtonPrefab;
        [SerializeField] private GameObject menuButtonPrefab;
        [SerializeField] private RectTransform blurPanelTransform;
        [SerializeField] private RectTransform contentTransform;
        [SerializeField] private ScrollRect contentScrollRect;
        [SerializeField] private CanvasGroup backgroundCanvasGroup;

        // Touch control
        [SerializeField] private Canvas touchControlUI;
        [SerializeField] private Button interactionButton;
        [SerializeField] private Button bigMapButton;
        [SerializeField] private Button mainMenuButton;

        // Event system
        [SerializeField] private EventSystem eventSystem;

        // Post-process volume and layer
        [SerializeField] private PostProcessVolume postProcessVolume;
        [SerializeField] private PostProcessLayer postProcessLayer;

        // Global texture cache store
        private readonly TextureCache _textureCache = new ();

        // Core game systems
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
        private PlayerManager _playerManager;
        private InventoryManager _inventoryManager;
        private DialogueManager _dialogueManager;
        private PostProcessManager _postProcessManager;
        private EffectManager _effectManager;

        // Game components
        private TouchControlUIManager _touchControlUIManager;
        private PlayerGamePlayController _playerGamePlayController;
        private TeamManager _teamManager;
        private HotelManager _hotelManager;
        private InformationManager _informationManager;
        private BigMapManager _bigMapManager;
        private FavorManager _favorManager;
        private CaptionRenderer _captionRenderer;
        private CursorManager _cursorManager;
        private SaveManager _saveManager;

        // Mini games
        #if PAL3
        private AppraisalsMiniGame _appraisalsMiniGame;
        private SailingMiniGame _sailingMiniGame;
        private HideFightMiniGame _hideFightMiniGame;
        private EncampMiniGame _encampMiniGame;
        private SkiMiniGame _skiMiniGame;
        private SwatAFlyMiniGame _swatAFlyMiniGame;
        private CaveExperienceMiniGame _caveExperienceMiniGame;
        #endif

        // Dev tools
        private MazeSkipper _mazeSkipper;
        private MainMenu _mainMenu;

        private void OnEnable()
        {
            _gameSettings = ServiceLocator.Instance.Get<GameSettings>();
            _fileSystem = ServiceLocator.Instance.Get<ICpkFileSystem>();
            _gameResourceProvider = ServiceLocator.Instance.Get<GameResourceProvider>();
            _gameResourceProvider.UseTextureCache(_textureCache);

            _fileSystemCacheManager = new FileSystemCacheManager(_fileSystem);
            ServiceLocator.Instance.Register(_fileSystemCacheManager);
            _inputActions = new PlayerInputActions();
            ServiceLocator.Instance.Register(_inputActions);
            _inputManager= new InputManager(_inputActions);
            ServiceLocator.Instance.Register(_inputManager);
            _scriptManager = new ScriptManager(_gameResourceProvider);
            ServiceLocator.Instance.Register(_scriptManager);
            _gameStateManager = new GameStateManager(_inputManager, _scriptManager);
            ServiceLocator.Instance.Register(_gameStateManager);
            _sceneStateManager = new SceneStateManager();
            ServiceLocator.Instance.Register(_sceneStateManager);
            _sceneManager = new SceneManager(_gameResourceProvider,
                _sceneStateManager, _scriptManager, _gameSettings, mainCamera);
            ServiceLocator.Instance.Register(_sceneManager);
            _playerManager = new PlayerManager();
            ServiceLocator.Instance.Register(_playerManager);
            _inventoryManager = new InventoryManager(_gameResourceProvider);
            ServiceLocator.Instance.Register(_inventoryManager);
            _teamManager = new TeamManager(_playerManager, _sceneManager);
            ServiceLocator.Instance.Register(_teamManager);
            _touchControlUIManager = new TouchControlUIManager(touchControlUI,
                interactionButton, bigMapButton, mainMenuButton);
            ServiceLocator.Instance.Register(_touchControlUIManager);
            _favorManager = new FavorManager();
            ServiceLocator.Instance.Register(_favorManager);

            #if PAL3
            _appraisalsMiniGame = new AppraisalsMiniGame();
            ServiceLocator.Instance.Register(_appraisalsMiniGame);
            _sailingMiniGame = new SailingMiniGame();
            ServiceLocator.Instance.Register(_sailingMiniGame);
            _hideFightMiniGame = new HideFightMiniGame();
            ServiceLocator.Instance.Register(_hideFightMiniGame);
            _encampMiniGame = new EncampMiniGame();
            ServiceLocator.Instance.Register(_encampMiniGame);
            _skiMiniGame = new SkiMiniGame(_scriptManager);
            ServiceLocator.Instance.Register(_skiMiniGame);
            _swatAFlyMiniGame = new SwatAFlyMiniGame();
            ServiceLocator.Instance.Register(_swatAFlyMiniGame);
            _caveExperienceMiniGame = new CaveExperienceMiniGame();
            ServiceLocator.Instance.Register(_caveExperienceMiniGame);
            #endif

            _videoManager = gameObject.AddComponent<VideoManager>();
            _videoManager.Init(_gameResourceProvider,
                _gameStateManager,
                _inputActions,
                videoPlayerCanvas,
                videoPlayer);
            ServiceLocator.Instance.Register(_videoManager);

            _captionRenderer = gameObject.AddComponent<CaptionRenderer>();
            _captionRenderer.Init(_gameResourceProvider, _inputActions, captionImage);
            ServiceLocator.Instance.Register(_captionRenderer);

            _playerGamePlayController = gameObject.AddComponent<PlayerGamePlayController>();
            _playerGamePlayController.Init(_gameResourceProvider,
                _gameStateManager,
                _playerManager,
                _teamManager,
                _inputActions,
                _sceneManager,
                mainCamera);
            ServiceLocator.Instance.Register(_playerGamePlayController);

            _cameraManager = gameObject.AddComponent<CameraManager>();
            _cameraManager.Init(_inputActions,
                _playerGamePlayController,
                _sceneManager,
                _gameStateManager,
                mainCamera,
                touchControlUI,
                curtainImage);
            ServiceLocator.Instance.Register(_cameraManager);

            _audioManager = gameObject.AddComponent<AudioManager>();
            _audioManager.Init(mainCamera,
                _gameResourceProvider,
                _sceneManager,
                musicSource,
                _gameSettings);
            ServiceLocator.Instance.Register(_audioManager);

            _informationManager = gameObject.AddComponent<InformationManager>();
            _informationManager.Init(noteCanvasGroup, noteText, debugInfo);
            ServiceLocator.Instance.Register(_informationManager);

            _dialogueManager = gameObject.AddComponent<DialogueManager>();
            _dialogueManager.Init(_gameResourceProvider,
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
                dialogueSelectionButtonPrefab);
            ServiceLocator.Instance.Register(_dialogueManager);

            #if UNITY_STANDALONE || UNITY_EDITOR
            _cursorManager = gameObject.AddComponent<CursorManager>();
            _cursorManager.Init(_gameResourceProvider);
            ServiceLocator.Instance.Register(_cursorManager);
            #endif

            _hotelManager = gameObject.AddComponent<HotelManager>();
            _hotelManager.Init(_scriptManager, _sceneManager);
            ServiceLocator.Instance.Register(_hotelManager);

            _bigMapManager = gameObject.AddComponent<BigMapManager>();
            _bigMapManager.Init(eventSystem,
                _gameStateManager,
                _sceneManager,
                _inputManager,
                _scriptManager,
                bigMapCanvasGroup,
                bigMapRegionButtonPrefab);
            ServiceLocator.Instance.Register(_bigMapManager);

            _postProcessManager = gameObject.AddComponent<PostProcessManager>();
            _postProcessManager.Init(postProcessVolume, postProcessLayer, _gameSettings);
            ServiceLocator.Instance.Register(_postProcessManager);

            _effectManager = gameObject.AddComponent<EffectManager>();
            _effectManager.Init(_gameResourceProvider, _sceneManager);
            ServiceLocator.Instance.Register(_effectManager);

            _saveManager = new SaveManager(_sceneManager,
                _playerManager,
                _teamManager,
                _inventoryManager,
                _sceneStateManager,
                _bigMapManager,
                _scriptManager,
                _favorManager,
                _cameraManager,
                _audioManager,
                _postProcessManager);
            ServiceLocator.Instance.Register(_saveManager);

            _mazeSkipper = new MazeSkipper(_sceneManager);
            ServiceLocator.Instance.Register(_mazeSkipper);

            _mainMenu = gameObject.AddComponent<MainMenu>();
            _mainMenu.Init(
                _gameSettings,
                _inputManager,
                eventSystem,
                _sceneManager,
                _gameStateManager,
                _scriptManager,
                _teamManager,
                _saveManager,
                _informationManager,
                _mazeSkipper,
                mainMenuCanvasGroup,
                backgroundCanvasGroup,
                titlePrefab,
                mainMenuButtonPrefab,
                menuButtonPrefab,
                contentScrollRect,
                blurPanelTransform,
                contentTransform);
            ServiceLocator.Instance.Register(_mainMenu);

            DebugLogManager.Instance.OnLogWindowShown += OnDebugWindowShown;
            DebugLogManager.Instance.OnLogWindowHidden += OnDebugWindowHidden;

            DebugLogConsole.AddCommand("state", "Get current game state in commands form.", PrintCurrentGameStateInCommandsForm);
            DebugLogConsole.AddCommand("info", "Get current game info.", PrintCurrentGameInfo);
            DebugLogConsole.AddCommand<int>("fps", "Set target FPS.", SetTargetFps);
            DebugLogConsole.AddCommand<float>("fov", "Set camera FOV.", SetCameraFov);

            DisableInGameDebugConsoleButtonNavigation();
        }

        private void Start()
        {
            _mainMenu.Show();
        }

        private void OnDisable()
        {
            _gameSettings.Dispose();
            _gameResourceProvider.Dispose();
            _fileSystemCacheManager.Dispose();
            _inputManager.Dispose();
            _inputActions.Dispose();
            _gameStateManager.Dispose();
            _sceneStateManager.Dispose();
            _scriptManager.Dispose();
            _playerManager.Dispose();
            _inventoryManager.Dispose();
            _teamManager.Dispose();
            _sceneManager.Dispose();
            _touchControlUIManager.Dispose();
            _favorManager.Dispose();

            #if PAL3
            _appraisalsMiniGame.Dispose();
            _sailingMiniGame.Dispose();
            _hideFightMiniGame.Dispose();
            _encampMiniGame.Dispose();
            _skiMiniGame.Dispose();
            _swatAFlyMiniGame.Dispose();
            _caveExperienceMiniGame.Dispose();
            #endif

            Destroy(_videoManager);
            Destroy(_playerGamePlayController);
            Destroy(_cameraManager);
            Destroy(_audioManager);
            Destroy(_informationManager);
            Destroy(_dialogueManager);
            Destroy(_hotelManager);
            Destroy(_bigMapManager);
            Destroy(_captionRenderer);
            Destroy(_postProcessManager);
            Destroy(_effectManager);
            Destroy(_mainMenu);

            if (_cursorManager != null)
            {
                Destroy(_cursorManager);
            }

            DebugLogManager.Instance.OnLogWindowShown -= OnDebugWindowShown;
            DebugLogManager.Instance.OnLogWindowHidden -= OnDebugWindowHidden;
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

        private void Update()
        {
            GameState currentState = _gameStateManager.GetCurrentState();
            if (currentState is GameState.Cutscene or GameState.Gameplay)
            {
                _scriptManager.Update(Time.deltaTime);
            }
        }

        private void PrintCurrentGameStateInCommandsForm()
        {
            var commands = _saveManager.ConvertCurrentGameStateToCommands(SaveLevel.Minimal);
            var state = commands == null ? null : string.Join('\n', commands.Select(CommandExtensions.ToString).ToList());
            Debug.Log(state + '\n');
        }

        private void PrintCurrentGameInfo()
        {
            if (_sceneManager.GetCurrentScene() is not { } currentScene) return;

            var info = new StringBuilder();

            ScnSceneInfo currentSceneInfo = currentScene.GetSceneInfo();

            info.Append($"----- Scene info -----\n" +
                        $"{currentSceneInfo.ToString()}\n");

            var playerActorMovementController = currentScene
                .GetActorGameObject((int) _playerManager.GetPlayerActor()).GetComponent<ActorMovementController>();

            info.Append($"----- Player info -----\n" +
                        $"Nav layer: {playerActorMovementController.GetCurrentLayerIndex()}\n" +
                        $"Tile position: {playerActorMovementController.GetTilePosition()}\n");

            info.Append("----- Team info -----\n" +
                        $"Actors in team: {string.Join(", ", _teamManager.GetActorsInTeam().Select(_ => _.ToString()))}\n");

            info.Append(_scriptManager.GetGlobalVariables()
                .Aggregate("----- Variables info -----\n", (current, variable) => current + $"{variable.Key}: {variable.Value}\n"));

            info.Append(_inventoryManager);

            Debug.Log(info.ToString() + '\n');
        }

        private void SetTargetFps(int targetFps)
        {
            Application.targetFrameRate = targetFps;
        }

        private void SetCameraFov(float fieldOfView)
        {
            mainCamera.fieldOfView = fieldOfView;
        }
    }
}