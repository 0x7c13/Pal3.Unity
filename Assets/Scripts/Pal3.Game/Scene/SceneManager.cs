// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Actor.Controllers;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.DataReader.Sce;
    using Core.DataReader.Scn;
    using Core.Utilities;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using Engine.Logging;
    using GameSystems.Combat.Scene;
    using Newtonsoft.Json;
    using Script;
    using Settings;
    using State;
    using UnityEngine;

    public sealed class SceneManager : IDisposable,
        ICommandExecutor<SceneLoadCommand>,
        ICommandExecutor<SceneObjectDoNotLoadFromSaveStateCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private readonly GameResourceProvider _resourceProvider;
        private readonly ISceneObjectFactory _sceneObjectFactory;
        private readonly SceneStateManager _sceneStateManager;
        private readonly ScriptManager _scriptManager;
        private readonly GameSettings _gameSettings;
        private readonly IGameEntity _cameraEntity;

        private IGameEntity _currentSceneRoot;
        private IGameEntity _currentCombatSceneRoot;

        private GameScene _currentGameScene;
        private CombatScene _currentCombatScene;

        private readonly HashSet<int> _sceneObjectIdsToNotLoadFromSaveState = new ();

        public SceneManager(GameResourceProvider resourceProvider,
            ISceneObjectFactory sceneObjectFactory,
            SceneStateManager sceneStateManager,
            ScriptManager scriptManager,
            GameSettings gameSettings,
            IGameEntity cameraEntity)
        {
            _resourceProvider = Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _sceneObjectFactory = Requires.IsNotNull(sceneObjectFactory, nameof(sceneObjectFactory));
            _sceneStateManager = Requires.IsNotNull(sceneStateManager, nameof(sceneStateManager));
            _scriptManager = Requires.IsNotNull(scriptManager, nameof(scriptManager));
            _gameSettings = Requires.IsNotNull(gameSettings, nameof(gameSettings));
            _cameraEntity = Requires.IsNotNull(cameraEntity, nameof(cameraEntity));
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            DisposeCurrentScene();
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public GameScene GetCurrentScene()
        {
            return _currentGameScene;
        }

        public CombatScene GetCurrentCombatScene()
        {
            return _currentCombatScene;
        }

        public IGameEntity GetSceneRootGameEntity()
        {
            if (_currentCombatSceneRoot != null)
            {
                return _currentCombatSceneRoot;
            }
            else if (_currentSceneRoot != null)
            {
                return _currentSceneRoot;
            }

            return null;
        }

        public GameScene LoadScene(string sceneCityName, string sceneName)
        {
            Stopwatch timer = Stopwatch.StartNew();

            DisposeCurrentScene();

            ScnFile scnFile = _resourceProvider.GetGameResourceFile<ScnFile>(
                    FileConstants.GetScnFileVirtualPath(sceneCityName, sceneName));

            Pal3.Instance.Execute(new ScenePreLoadingNotification(scnFile.SceneInfo));

            EngineLogger.Log($"Loading scene: {JsonConvert.SerializeObject(scnFile.SceneInfo)}");

            _currentSceneRoot = GameEntityFactory.Create($"Scene_{sceneCityName}_{sceneName}",
                null, worldPositionStays: false);
            _currentGameScene = _currentSceneRoot.AddComponent<GameScene>();
            _currentGameScene.Init(_resourceProvider,
                _sceneObjectFactory,
                _sceneStateManager,
                _gameSettings.IsRealtimeLightingAndShadowsEnabled,
                _cameraEntity,
                _sceneObjectIdsToNotLoadFromSaveState);
            _currentGameScene.Load(scnFile, _currentSceneRoot);

            // Must to clear the exclude list after loading the scene.
            _sceneObjectIdsToNotLoadFromSaveState.Clear();

            // Add scene script if exists.
            SceFile sceFile = _resourceProvider.GetGameResourceFile<SceFile>(
                FileConstants.GetSceneSceFileVirtualPath(sceneCityName));

            Pal3.Instance.Execute(
                _scriptManager.TryAddSceneScript(sceFile, $"_{sceneCityName}_{sceneName}", out var sceneScriptId)
                    ? new ScenePostLoadingNotification(scnFile.SceneInfo, sceneScriptId)
                    : new ScenePostLoadingNotification(scnFile.SceneInfo, ScriptConstants.InvalidScriptId));

            EngineLogger.Log($"Scene loaded in {timer.ElapsedMilliseconds} ms");

            // Also a good time to collect garbage
            System.GC.Collect();

            return _currentGameScene;
        }

        public CombatScene LoadCombatScene(string combatSceneName)
        {
            Stopwatch timer = Stopwatch.StartNew();

            HideCurrentScene();

            _currentCombatSceneRoot = GameEntityFactory.Create($"CombatScene_{combatSceneName}",
                null, worldPositionStays: false);

            _currentCombatScene = _currentCombatSceneRoot.AddComponent<CombatScene>();
            _currentCombatScene.Init(_resourceProvider);
            _currentCombatScene.Load(_currentCombatSceneRoot,
                combatSceneName,
                _gameSettings.IsRealtimeLightingAndShadowsEnabled);

            EngineLogger.Log($"CombatScene loaded in {timer.ElapsedMilliseconds} ms");

            // Also a good time to collect garbage
            System.GC.Collect();

            return _currentCombatScene;
        }

        public void UnloadCombatScene()
        {
            if (_currentCombatScene != null)
            {
                _currentCombatScene.Destroy();
                _currentCombatScene = null;
            }

            if (_currentCombatSceneRoot != null)
            {
                _currentCombatSceneRoot.Destroy();
                _currentCombatSceneRoot = null;
            }

            ShowCurrentScene();
        }

        private readonly IList<Renderer> _temporarilyDisabledRenderers = new List<Renderer>();
        private readonly IList<GameObject> _temporarilyDisabledGameObjects = new List<GameObject>();
        private void HideCurrentScene()
        {
            _temporarilyDisabledRenderers.Clear();
            _temporarilyDisabledGameObjects.Clear();

            foreach (ActorMovementController movementController in
                     _currentSceneRoot.GetComponentsInChildren<ActorMovementController>())
            {
                movementController.PauseMovement();
            }

            foreach (Renderer renderer in _currentSceneRoot.GetComponentsInChildren<Renderer>())
            {
                if (renderer.enabled)
                {
                    renderer.enabled = false;
                    _temporarilyDisabledRenderers.Add(renderer);
                }
            }

            foreach (ParticleSystem particleSystem in _currentSceneRoot.GetComponentsInChildren<ParticleSystem>())
            {
                GameObject particleGo = particleSystem.gameObject;
                if (particleGo.activeInHierarchy)
                {
                    particleGo.SetActive(false);
                    _temporarilyDisabledGameObjects.Add(particleGo);
                }
            }

            // Move the scene root to a lower position just to be safe.
            _currentSceneRoot.Transform.LocalPosition = Vector3.down * 100f;
        }

        private void ShowCurrentScene()
        {
            // Move the scene root back to the original position.
            _currentSceneRoot.Transform.LocalPosition = Vector3.zero;

            foreach (GameObject disabledGo in _temporarilyDisabledGameObjects)
            {
                if (disabledGo != null) disabledGo.SetActive(true);
            }

            foreach (Renderer renderer in _temporarilyDisabledRenderers)
            {
                if (renderer != null) renderer.enabled = true;
            }

            foreach (ActorMovementController movementController in
                     _currentSceneRoot.GetComponentsInChildren<ActorMovementController>())
            {
                movementController.ResumeMovement();
            }

            _temporarilyDisabledRenderers.Clear();
            _temporarilyDisabledGameObjects.Clear();
        }

        private void DisposeCurrentScene()
        {
            _temporarilyDisabledRenderers.Clear();
            _temporarilyDisabledGameObjects.Clear();

            if (_currentGameScene != null)
            {
                Pal3.Instance.Execute(new SceneLeavingCurrentSceneNotification());
                _currentGameScene.Destroy();
                _currentGameScene = null;
            }

            if (_currentCombatScene != null)
            {
                _currentCombatScene.Destroy();
                _currentCombatScene = null;
            }

            if (_currentSceneRoot != null)
            {
                _currentSceneRoot.Destroy();
                _currentSceneRoot = null;
            }
        }

        public void Execute(SceneLoadCommand command) => LoadScene(command.SceneCityName, command.SceneName);

        public void Execute(ResetGameStateCommand command) => DisposeCurrentScene();

        public void Execute(SceneObjectDoNotLoadFromSaveStateCommand command)
        {
            _sceneObjectIdsToNotLoadFromSaveState.Add(command.ObjectId);
        }
    }
}