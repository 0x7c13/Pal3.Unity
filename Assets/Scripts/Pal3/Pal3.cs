// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Actor;
    using Audio;
    using Camera;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
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
    using MetaData;
    #if PAL3
    using MiniGame;
    #endif
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
    using PostProcessManager = Effect.PostProcessManager;

    /// <summary>
    /// Pal3 game model
    /// </summary>
    public class Pal3 : Singleton<Pal3>
    {
        // Camera
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private Image curtainImage;

        // Audio
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        // Video
        [SerializeField] private Canvas videoPlayerCanvas;
        [SerializeField] private VideoPlayer videoPlayer;

        // Information
        [SerializeField] private CanvasGroup noteCanvasGroup;
        [SerializeField] private TextMeshProUGUI noteText;

        // Dialogue
        [SerializeField] private Canvas dialogueCanvas;
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
        [SerializeField] private CanvasGroup mazeSkipperCanvasGroup;
        [SerializeField] private Button mazeEntranceButton;
        [SerializeField] private Button mazeExitButton;

        // BigMap
        [SerializeField] private CanvasGroup bigMapCanvasGroup;
        [SerializeField] private GameObject bigMapRegionButtonPrefab;

        // Story selector
        [SerializeField] private CanvasGroup storySelectionCanvasGroup;
        [SerializeField] private GameObject storySelectionButtonPrefab;

        // Touch control
        [SerializeField] private Canvas touchControlUI;
        [SerializeField] private Button interactionButton;
        [SerializeField] private Button bigMapButton;
        [SerializeField] private Button storySelectionButton;

        // Event system
        [SerializeField] private EventSystem eventSystem;

        // Post-process volume and layer
        [SerializeField] private PostProcessVolume postProcessVolume;
        [SerializeField] private PostProcessLayer postProcessLayer;

        // Global texture cache store
        private readonly TextureCache _textureCache = new ();

        // Core game systems
        private ICpkFileSystem _fileSystem;
        private GameResourceProvider _gameResourceProvider;
        private FileSystemCacheManager _fileSystemCacheManager;
        private PlayerInputActions _inputActions;
        private InputManager _inputManager;
        private GameStateManager _gameStateManager;
        private ScriptManager _scriptManager;
        private VideoManager _videoManager;
        private SceneManager _sceneManager;
        private CameraManager _cameraManager;
        private AudioManager _audioManager;
        private PlayerManager _playerManager;
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
        private StorySelector _storySelector;

        private SettingsManager _settingsManager;
        
        private void OnEnable()
        {
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
            _sceneManager = new SceneManager(_gameResourceProvider, _scriptManager, mainCamera);
            ServiceLocator.Instance.Register(_sceneManager);
            _playerManager = new PlayerManager();
            ServiceLocator.Instance.Register(_playerManager);
            _teamManager = new TeamManager(_playerManager, _sceneManager);
            ServiceLocator.Instance.Register(_teamManager);
            _touchControlUIManager = new TouchControlUIManager(touchControlUI,
                interactionButton, bigMapButton, storySelectionButton);
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
            
            _mazeSkipper = new MazeSkipper(_gameStateManager,
                _sceneManager,
                mazeSkipperCanvasGroup,
                mazeEntranceButton,
                mazeExitButton);
            ServiceLocator.Instance.Register(_mazeSkipper);

            _storySelector = gameObject.AddComponent<StorySelector>();
            _storySelector.Init(
                _inputManager,
                eventSystem,
                _sceneManager,
                _gameStateManager,
                _scriptManager,
                storySelectionCanvasGroup,
                storySelectionButtonPrefab);
            ServiceLocator.Instance.Register(_storySelector);

            _videoManager = gameObject.AddComponent<VideoManager>();
            _videoManager.Init(_gameResourceProvider,_inputActions, videoPlayerCanvas, videoPlayer);
            ServiceLocator.Instance.Register(_videoManager);

            _captionRenderer = gameObject.AddComponent<CaptionRenderer>();
            _captionRenderer.Init(_gameResourceProvider, _inputActions, captionImage);
            ServiceLocator.Instance.Register(_captionRenderer);

            _playerGamePlayController = gameObject.AddComponent<PlayerGamePlayController>();
            _playerGamePlayController.Init(_gameStateManager,
                _playerManager,
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
            _audioManager.Init(_gameResourceProvider,
                _sceneManager,
                musicSource,
                sfxSource);
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
                dialogueCanvas,
                dialogueBackgroundImage,
                dialogueAvatarImageLeft,
                dialogueAvatarImageRight,
                dialogueTextLeft,
                dialogueTextRight,
                dialogueTextDefault,
                dialogueSelectionButtonsCanvas,
                dialogueSelectionButtonPrefab);
            ServiceLocator.Instance.Register(_dialogueManager);

            #if UNITY_STANDALONE
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
            _postProcessManager.Init(postProcessVolume, postProcessLayer);
            ServiceLocator.Instance.Register(_postProcessManager);

            _effectManager = gameObject.AddComponent<EffectManager>();
            _effectManager.Init(_sceneManager);
            ServiceLocator.Instance.Register(_effectManager);
            
            DebugLogManager.Instance.OnLogWindowShown += OnDebugWindowShown;
            DebugLogManager.Instance.OnLogWindowHidden += OnDebugWindowHidden;

            DebugLogConsole.AddCommand("vars", "List current global variables.", ListCurrentGlobalVariables);
            DebugLogConsole.AddCommand("save", "Save game state into executable commands.", ConvertCurrentGameStateToCommands);
            DebugLogConsole.AddCommand("info", "Get current game info.", GetCurrentGameInfo);

            _settingsManager = new SettingsManager();
            _settingsManager.ApplyDefaultRenderingSettings();
            _settingsManager.ApplyPlatformSpecificSettings();

            _storySelector.Show();
        }

        private void OnDisable()
        {
            _gameResourceProvider.Dispose();
            _fileSystemCacheManager.Dispose();
            _inputManager.Dispose();
            _gameStateManager.Dispose();
            _scriptManager.Dispose();
            _playerManager.Dispose();
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

            _mazeSkipper.Dispose();

            Destroy(_videoManager);
            Destroy(_playerGamePlayController);
            Destroy(_cameraManager);
            Destroy(_cameraManager);
            Destroy(_audioManager);
            Destroy(_informationManager);
            Destroy(_dialogueManager);
            Destroy(_hotelManager);
            Destroy(_bigMapManager);
            Destroy(_storySelector);
            Destroy(_captionRenderer);
            Destroy(_postProcessManager);

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
        }

        private void Update()
        {
            GameState currentState = _gameStateManager.GetCurrentState();
            if (currentState is GameState.Cutscene or GameState.Gameplay)
            {
                _scriptManager.Update(Time.deltaTime);
            }
        }

        private string ConvertCurrentGameStateToCommands()
        {
            if (_sceneManager.GetCurrentScene() is not { } currentScene) return null;
            
            var playerActorMovementController = currentScene
                .GetActorGameObject((byte) _playerManager.GetPlayerActor()).GetComponent<ActorMovementController>();
            Vector2Int playerActorTilePosition = playerActorMovementController.GetTilePosition();
            var varsToSave = _scriptManager.GetGlobalVariables()
                .Where(_ => _.Key == ScriptConstants.MainStoryVariableName); // Save main story var only
            ScnSceneInfo currentSceneInfo = currentScene.GetSceneInfo();

            var commands = new List<ICommand>
            {
                new ResetGameStateCommand(),
            };
            
            commands.AddRange(varsToSave.Select(var =>
                new ScriptVarSetValueCommand(var.Key, var.Value)));
            
            commands.AddRange(new List<ICommand>()
            {
                new SceneLoadCommand(currentSceneInfo.CityName, currentSceneInfo.Name),
                new ActorActivateCommand(ActorConstants.PlayerActorVirtualID, 1),
                new ActorEnablePlayerControlCommand(ActorConstants.PlayerActorVirtualID),
                new PlayerEnableInputCommand(1),
                new ActorSetNavLayerCommand(ActorConstants.PlayerActorVirtualID,
                    playerActorMovementController.GetCurrentLayerIndex()),
                new ActorSetTilePositionCommand(ActorConstants.PlayerActorVirtualID,
                    playerActorTilePosition.x, playerActorTilePosition.y)
            });

            commands.AddRange(_teamManager.GetActorsInTeam()
                .Select(actorId => new TeamAddOrRemoveActorCommand((int) actorId, 1)));
            // commands.AddRange(_favorManager.GetAllActorFavorInfo()
            //     .Select(favorInfo => new FavorAddCommand(favorInfo.Key, favorInfo.Value)));
            commands.AddRange(_bigMapManager.GetRegionEnablementInfo()
                .Select(regionEnablement => new BigMapEnableRegionCommand(regionEnablement.Key, regionEnablement.Value)));
            commands.Add(new CameraFadeInCommand());

            return string.Join('\n', commands.Select(ToString).ToList());
        }

        private string GetCurrentGameInfo()
        {
            if (_sceneManager.GetCurrentScene() is not { } currentScene) return null;
            
            var info = new StringBuilder();

            ScnSceneInfo currentSceneInfo = currentScene.GetSceneInfo();

            info.Append($"Current scene: {currentSceneInfo.CityName} {currentSceneInfo.Name}\n");

            var playerActorMovementController = currentScene
                .GetActorGameObject((byte) _playerManager.GetPlayerActor()).GetComponent<ActorMovementController>();

            info.Append($"Player actor current nav layer: {playerActorMovementController.GetCurrentLayerIndex()} " +
                        $"tile position: {playerActorMovementController.GetTilePosition()}\n");

            return info.ToString();
        }

        private static string ToString(ICommand command)
        {
            var builder = new StringBuilder();
            Type type = command.GetType();
            
            builder.Append(type.Name[..^"Command".Length]);
            builder.Append(' ');
            
            foreach (PropertyInfo propertyInfo in type.GetProperties())
            {
                builder.Append(propertyInfo.GetValue(command));
                builder.Append(' ');
            }

            var commandStr = builder.ToString();
            if (commandStr.EndsWith(' ')) commandStr = commandStr[..^1];
            return commandStr;
        }

        private string ListCurrentGlobalVariables()
        {
            if (_scriptManager.GetGlobalVariables() is not { Count: > 0 }) return null;
            
            return _scriptManager.GetGlobalVariables()
                .Aggregate("Global vars: ", (current, variable) => current + $"{variable.Key}: {variable.Value} ");
        }
    }
}