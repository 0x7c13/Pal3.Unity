// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Actor.Controllers;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Sce;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Core.Utils;
    using Data;
    using MetaData;
    using Newtonsoft.Json;
    using Script;
    using Settings;
    using State;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    public sealed class SceneManager : IDisposable,
        ICommandExecutor<SceneLoadCommand>,
        ICommandExecutor<SceneObjectDoNotLoadFromSaveStateCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private readonly GameResourceProvider _resourceProvider;
        private readonly SceneStateManager _sceneStateManager;
        private readonly ScriptManager _scriptManager;
        private readonly GameSettings _gameSettings;
        private readonly Camera _mainCamera;

        private GameObject _currentSceneRoot;
        private GameObject _currentCombatSceneRoot;

        private Scene _currentScene;
        private CombatScene _currentCombatScene;

        private readonly HashSet<int> _sceneObjectIdsToNotLoadFromSaveState = new ();

        public SceneManager(GameResourceProvider resourceProvider,
            SceneStateManager sceneStateManager,
            ScriptManager scriptManager,
            GameSettings gameSettings,
            Camera mainCamera)
        {
            _resourceProvider = Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _sceneStateManager = Requires.IsNotNull(sceneStateManager, nameof(sceneStateManager));
            _scriptManager = Requires.IsNotNull(scriptManager, nameof(scriptManager));
            _gameSettings = Requires.IsNotNull(gameSettings, nameof(gameSettings));
            _mainCamera = Requires.IsNotNull(mainCamera, nameof(mainCamera));
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            DisposeCurrentScene();
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public Scene GetCurrentScene()
        {
            return _currentScene;
        }

        public GameObject GetSceneRootGameObject()
        {
            return _currentSceneRoot;
        }

        public void LoadScene(string sceneCityName, string sceneName)
        {
            var timer = new Stopwatch();
            timer.Start();
            DisposeCurrentScene();

            ScnFile scnFile = _resourceProvider.GetGameResourceFile<ScnFile>(
                    FileConstants.GetScnFileVirtualPath(sceneCityName, sceneName));

            CommandDispatcher<ICommand>.Instance.Dispatch(new ScenePreLoadingNotification(scnFile.SceneInfo));
            Debug.Log($"[{nameof(SceneManager)}] Loading scene: " + JsonConvert.SerializeObject(scnFile.SceneInfo));

            _currentSceneRoot = new GameObject($"Scene_{sceneCityName}_{sceneName}");
            _currentSceneRoot.transform.SetParent(null);
            _currentScene = _currentSceneRoot.AddComponent<Scene>();
            _currentScene.Init(_resourceProvider,
                _sceneStateManager,
                _gameSettings.IsRealtimeLightingAndShadowsEnabled,
                _mainCamera,
                _sceneObjectIdsToNotLoadFromSaveState);
            _currentScene.Load(scnFile, _currentSceneRoot);

            // Must to clear the exclude list after loading the scene.
            _sceneObjectIdsToNotLoadFromSaveState.Clear();

            // Add scene script if exists.
            SceFile sceFile = _resourceProvider.GetGameResourceFile<SceFile>(
                FileConstants.GetSceneSceFileVirtualPath(sceneCityName));

            CommandDispatcher<ICommand>.Instance.Dispatch(
                _scriptManager.TryAddSceneScript(sceFile, $"_{sceneCityName}_{sceneName}", out var sceneScriptId)
                    ? new ScenePostLoadingNotification(scnFile.SceneInfo, sceneScriptId)
                    : new ScenePostLoadingNotification(scnFile.SceneInfo, ScriptConstants.InvalidScriptId));

            timer.Stop();
            Debug.Log($"[{nameof(SceneManager)}] Scene loaded in {timer.Elapsed.TotalSeconds} seconds.");

            // Also a good time to collect garbage
            System.GC.Collect();
        }

        public void LoadCombatScene(string combatSceneName)
        {
            var timer = new Stopwatch();
            timer.Start();

            HideCurrentScene();

            _currentCombatSceneRoot = new GameObject($"CombatScene_{combatSceneName}");
            _currentCombatSceneRoot.transform.SetParent(null);
            _currentCombatSceneRoot.transform.localPosition = new Vector3(0f, 0f, 0f);

            _currentCombatScene = _currentCombatSceneRoot.AddComponent<CombatScene>();
            _currentCombatScene.Init(_resourceProvider);
            _currentCombatScene.Load(_currentCombatSceneRoot, combatSceneName);

            timer.Stop();
            Debug.Log($"[{nameof(SceneManager)}] CombatScene loaded in {timer.Elapsed.TotalSeconds} seconds.");

            // Also a good time to collect garbage
            System.GC.Collect();
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

        private void HideCurrentScene()
        {
            foreach (ActorMovementController movementController in
                     _currentSceneRoot.GetComponentsInChildren<ActorMovementController>())
            {
                movementController.PauseMovement();
            }

            foreach (MeshRenderer meshRenderer in
                     _currentSceneRoot.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.enabled = false;
            }

            foreach (SpriteRenderer spriteRenderer in
                     _currentSceneRoot.GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderer.enabled = false;
            }
        }

        private void ShowCurrentScene()
        {
            foreach (SpriteRenderer spriteRenderer in
                     _currentSceneRoot.GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderer.enabled = true;
            }

            foreach (MeshRenderer meshRenderer in _currentSceneRoot.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.enabled = true;
            }

            foreach (ActorMovementController movementController in
                     _currentSceneRoot.GetComponentsInChildren<ActorMovementController>())
            {
                movementController.ResumeMovement();
            }
        }

        private void DisposeCurrentScene()
        {
            if (_currentScene != null)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new SceneLeavingCurrentSceneNotification());
                _currentScene.Destroy();
                _currentScene = null;
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

        public void Execute(SceneLoadCommand command)
        {
            LoadScene(command.SceneCityName, command.SceneName);
        }

        public void Execute(ResetGameStateCommand command)
        {
            DisposeCurrentScene();
        }

        public void Execute(SceneObjectDoNotLoadFromSaveStateCommand command)
        {
            _sceneObjectIdsToNotLoadFromSaveState.Add(command.ObjectId);
        }
    }
}