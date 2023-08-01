// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Sce;
    using Core.DataReader.Scn;
    using Core.Utils;
    using Data;
    using MetaData;
    using Newtonsoft.Json;
    using Script;
    using Settings;
    using State;
    using UnityEngine;
    using Debug = UnityEngine.Debug;
    using Object = UnityEngine.Object;

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
        private Scene _currentScene;

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

        public void LoadScene(string sceneFileName, string sceneName)
        {
            var timer = new Stopwatch();
            timer.Start();
            DisposeCurrentScene();

            ScnFile scnFile = _resourceProvider.GetScnFile(sceneFileName, sceneName);

            CommandDispatcher<ICommand>.Instance.Dispatch(new ScenePreLoadingNotification(scnFile.SceneInfo));
            Debug.Log("Loading scene: " + JsonConvert.SerializeObject(scnFile.SceneInfo));

            _currentSceneRoot = new GameObject($"Scene_{sceneFileName}_{sceneName}");
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
            SceFile sceFile = _resourceProvider.GetSceneSceFile(sceneFileName);
            CommandDispatcher<ICommand>.Instance.Dispatch(
                _scriptManager.TryAddSceneScript(sceFile, $"_{sceneFileName}_{sceneName}", out var sceneScriptId)
                    ? new ScenePostLoadingNotification(scnFile.SceneInfo, sceneScriptId)
                    : new ScenePostLoadingNotification(scnFile.SceneInfo, ScriptConstants.InvalidScriptId));

            Debug.Log($"Scene loaded in {timer.Elapsed.TotalSeconds} seconds.");

            // Also a good time to collect garbage
            System.GC.Collect();
        }

        private void DisposeCurrentScene()
        {
            if (_currentScene != null)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new SceneLeavingCurrentSceneNotification());
                Object.Destroy(_currentScene);
                _currentScene = null;
            }

            if (_currentSceneRoot != null)
            {
                Object.Destroy(_currentSceneRoot);
                _currentSceneRoot = null;
            }
        }

        public void Execute(SceneLoadCommand command)
        {
            LoadScene(command.SceneFileName, command.SceneName);
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