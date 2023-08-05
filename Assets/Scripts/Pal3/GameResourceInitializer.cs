// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using Core.Animation;
    using Core.DataReader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Data;
    using Core.DataReader.Gdb;
    using Core.DataReader.Ini;
    using Core.DataReader.Lgt;
    using Core.DataReader.Mov;
    using Core.DataReader.Msh;
    using Core.DataReader.Mtl;
    using Core.DataReader.Mv3;
    using Core.DataReader.Nav;
    using Core.DataReader.Pol;
    using Core.DataReader.Sce;
    using Core.DataReader.Scn;
    using Core.DataReader.Txt;
    using Core.FileSystem;
    using Core.Services;
    using Data;
    using MetaData;
    using Renderer;
    using Settings;
    using SimpleFileBrowser;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// The Game Resource initializer
    /// Initialize file system and all required services.
    /// </summary>
    public sealed class GameResourceInitializer : MonoBehaviour
    {
        [SerializeField] private GameObject startingComponent;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI loadingText;

        private const int DEFAULT_CODE_PAGE = 936; // GBK Encoding's code page
                                                   // change it to 950 to supports Traditional Chinese (Big5)

        // Optional materials that are used in the game but not open sourced
        private Material _toonDefaultMaterial;
        private Material _toonTransparentMaterial;

        private IEnumerator Start()
        {
            loadingText.text = "正在加载游戏数据...";
            yield return null; // Wait for next frame to make sure the text is updated

            ResourceRequest toonDefaultMaterialLoadRequest = Resources.LoadAsync<Material>("Materials/ToonDefault");
            yield return toonDefaultMaterialLoadRequest;

            if (toonDefaultMaterialLoadRequest.asset != null)
            {
                _toonDefaultMaterial = toonDefaultMaterialLoadRequest.asset as Material;
            }

            ResourceRequest toonTransparentMaterialLoadRequest = Resources.LoadAsync<Material>("Materials/ToonTransparent");
            yield return toonTransparentMaterialLoadRequest;

            if (toonTransparentMaterialLoadRequest.asset != null)
            {
                _toonTransparentMaterial = toonTransparentMaterialLoadRequest.asset as Material;
            }

            yield return InitResourceAsync();
        }

        private IEnumerator InitResourceAsync()
        {
            Debug.Log($"[{nameof(GameResourceInitializer)}] Initializing game resources...");

            // TODO: let user to choose language? Or auto-detect encoding?
            int codepage = DEFAULT_CODE_PAGE;

            // Create and init Crc32 hash
            Crc32Hash crcHash = new ();
            crcHash.Init();
            ServiceLocator.Instance.Register<Crc32Hash>(crcHash);

            // If toon materials are not present, it's an open source build
            bool isOpenSourceVersion = _toonDefaultMaterial == null || _toonTransparentMaterial == null;

            // Init settings store
            ITransactionalKeyValueStore settingsStore = new PlayerPrefsStore();

            // Init settings
            GameSettings gameSettings = new (settingsStore, isOpenSourceVersion);
            gameSettings.InitOrLoadSettings();
            ServiceLocator.Instance.Register<GameSettings>(gameSettings);

            // Init file system
            Queue<string> gameDataFolderSearchLocations = new(gameSettings.GetGameDataFolderSearchLocations());
            ICpkFileSystem cpkFileSystem = null;

            if (gameDataFolderSearchLocations.Count == 0)
            {
                Debug.LogError($"[{nameof(GameResourceInitializer)}] No game data folder search locations found.");
                yield break;
            }

            while (gameDataFolderSearchLocations.Count > 0)
            {
                string gameDataFolderPath = gameDataFolderSearchLocations.Dequeue();
                Exception exception = null;

                yield return InitFileSystemAsync(gameDataFolderPath,
                    crcHash, codepage, (fileSystem, ex) =>
                {
                    cpkFileSystem = fileSystem;
                    exception = ex;
                });

                if (cpkFileSystem != null) // Init file system successfully
                {
                    ServiceLocator.Instance.Register<ICpkFileSystem>(cpkFileSystem);

                    // Save game data folder path when file system initialized successfully,
                    // since it's possible that user changed the game data folder path
                    // during the file system initialization
                    gameSettings.GameDataFolderPath = gameDataFolderPath;
                    gameSettings.SaveSettings();

                    break; // Stop searching when file system is initialized successfully
                }

                // If file system is not initialized successfully, retry with next search
                // location when there is any
                if (gameDataFolderSearchLocations.Count > 0) continue;

                string userPickedGameDataFolderPath = null;

                yield return WaitForUserToPickGameDataFolderAsync(path => userPickedGameDataFolderPath = path);

                if (!string.IsNullOrEmpty(userPickedGameDataFolderPath))
                {
                    // Enqueue the user picked game data folder path to the search
                    // locations and retry
                    gameDataFolderSearchLocations.Enqueue(userPickedGameDataFolderPath);
                    loadingText.text = "正在重新加载游戏数据...";
                    yield return null; // Wait for next frame to make sure the text is updated
                }
                else
                {
                    loadingText.text = exception == null ? "游戏数据加载失败，未能找到原游戏数据文件" : $"{exception.Message}";
                    yield break; // Stop initialization if failed to init file system
                }
            }

            // Create and register IFileReader<T> instances
            ServiceLocator.Instance.Register<IFileReader<CvdFile>>(new CvdFileReader(codepage));
            ServiceLocator.Instance.Register<IFileReader<Mv3File>>(new Mv3FileReader(codepage));
            ServiceLocator.Instance.Register<IFileReader<MovFile>>(new MovFileReader(codepage));
            ServiceLocator.Instance.Register<IFileReader<MshFile>>(new MshFileReader(codepage));
            ServiceLocator.Instance.Register<IFileReader<MtlFile>>(new MtlFileReader(codepage));
            ServiceLocator.Instance.Register<IFileReader<PolFile>>(new PolFileReader(codepage));
            ServiceLocator.Instance.Register<IFileReader<GdbFile>>(new GdbFileReader(codepage));
            ServiceLocator.Instance.Register<IFileReader<LgtFile>>(new LgtFileReader());
            ServiceLocator.Instance.Register<IFileReader<NavFile>>(new NavFileReader());
            ServiceLocator.Instance.Register<IFileReader<MovActionConfig>>(new MovConfigFileReader());
            ServiceLocator.Instance.Register<IFileReader<Mv3ActionConfig>>(new Mv3ConfigFileReader());
            ServiceLocator.Instance.Register<IFileReader<EffectDefinitionFile>>(new EffectDefinitionFileReader());
            ServiceLocator.Instance.Register<IFileReader<SceFile>>(new SceFileReader(codepage));
            ServiceLocator.Instance.Register<IFileReader<ScnFile>>(new ScnFileReader(codepage));
            ServiceLocator.Instance.Register<IFileReader<TaskDefinitionFile>>(new TaskDefinitionFileReader(codepage));

            // Init TextureLoaderFactory
            TextureLoaderFactory textureLoaderFactory = new ();
            ServiceLocator.Instance.Register<ITextureLoaderFactory>(textureLoaderFactory);

            // Init material factories
            IMaterialFactory unlitMaterialFactory = new UnlitMaterialFactory();
            IMaterialFactory litMaterialFactory = null;

            // Only create litMaterialFactory when toon materials are present
            if (_toonDefaultMaterial != null && _toonTransparentMaterial != null)
            {
                litMaterialFactory = new LitMaterialFactory(_toonDefaultMaterial, _toonTransparentMaterial);
            }

            // Init Game resource provider
            var resourceProvider = new GameResourceProvider(cpkFileSystem,
                new TextureLoaderFactory(),
                unlitMaterialFactory,
                litMaterialFactory,
                gameSettings);
            ServiceLocator.Instance.Register(resourceProvider);

            Debug.Log($"[{nameof(GameResourceInitializer)}] Game resources initialized.");

            // Instantiate starting component
            GameObject startingGameObject = Instantiate(startingComponent, null);
            startingGameObject.name = startingComponent.name;

            yield return FadeTextAndBackgroundImageAsync();

            FinalizeInit();
        }

        private IEnumerator WaitForUserToPickGameDataFolderAsync(Action<string> callback)
        {
            string userPickedGameDataFolderPath = null;

            yield return FileBrowser.WaitForLoadDialog(
                FileBrowser.PickMode.Folders,
                allowMultiSelection: false,
                initialPath: null,
                initialFilename: null,
                title: $"请选择<<{GameConstants.AppNameCNFull}>>原始游戏文件夹根目录",
                loadButtonText: "选择");

            if (FileBrowser.Success &&
                FileBrowser.Result.Length == 1)
            {
                #if !UNITY_EDITOR && UNITY_ANDROID
                // On Android 10+, the result is a content URI of the selected folder in
                // SAF (Storage Access Framework) format, so we need to copy it to the
                // persistent data path in order to use it with System.IO APIs
                if (FileBrowserHelpers.ShouldUseSAF)
                {
                    string persistentDataPath = Application.persistentDataPath +
                         Path.DirectorySeparatorChar +
                         GameConstants.AppName +
                         Path.DirectorySeparatorChar;

                    // Delete the old folder if it exists
                    if (Directory.Exists(persistentDataPath))
                    {
                        Directory.Delete(persistentDataPath, true);
                    }

                    loadingText.text = "正在拷贝游戏数据至游戏可访问目录（预计需要几分钟）...";
                    yield return null; // Wait for next frame to make sure the text is updated

                    FileBrowserHelpers.CopyDirectory(FileBrowser.Result[0], persistentDataPath);

                    userPickedGameDataFolderPath = persistentDataPath;
                }
                else
                #endif
                {
                    userPickedGameDataFolderPath = FileBrowser.Result[0];
                }
            }

            callback.Invoke(userPickedGameDataFolderPath);
        }

        private void FinalizeInit()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // Since everything except for ServiceLocator will be destroyed,
            // we can just name the current game object as ServiceLocator
            gameObject.name = nameof(ServiceLocator);

            Destroy(this);
        }

        private IEnumerator InitFileSystemAsync(string gameDataFolderPath,
            Crc32Hash crcHash,
            int codepage,
            Action<ICpkFileSystem, Exception> callback)
        {
            ICpkFileSystem cpkFileSystem = null;
            var path = gameDataFolderPath;
            Exception exception = null;

            var fileSystemInitThread = new Thread(() =>
            {
                try
                {
                    var timer = new Stopwatch();
                    timer.Start();
                    cpkFileSystem = InitializeCpkFileSystem(path, crcHash, codepage);
                    timer.Stop();
                    Debug.Log($"[{nameof(GameResourceInitializer)}] All .cpk files mounted successfully. " +
                              $"Total time: {timer.Elapsed.TotalSeconds:0.000}s");
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    GC.Collect();
                }
            })
            {
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.Highest
            };
            fileSystemInitThread.Start();

            while (fileSystemInitThread.IsAlive)
            {
                yield return null;
            }

            callback?.Invoke(cpkFileSystem, exception);
        }

        private IEnumerator FadeTextAndBackgroundImageAsync()
        {
            loadingText.text = string.Empty;
            loadingText.alpha = 0f;
            loadingText.enabled = false;

            yield return CoreAnimation.EnumerateValueAsync(1f, 0f, duration: 1f, AnimationCurveType.Linear,
                value =>
            {
                backgroundImage.color = new Color(0, 0, 0, value);
            });

            backgroundImage.color = new Color(0, 0, 0, 0);
            backgroundImage.enabled = false;
        }

        private ICpkFileSystem InitializeCpkFileSystem(string gameRootPath, Crc32Hash crcHash, int codepage)
        {
            ICpkFileSystem cpkFileSystem = new CpkFileSystem(gameRootPath, crcHash);

            List<string> filesToMount = new ()
            {
                FileConstants.BaseDataCpkFileRelativePath,
                FileConstants.MusicCpkFileRelativePath
            };

            foreach (var sceneCpkFileName in FileConstants.SceneCpkFileNames)
            {
                filesToMount.Add(FileConstants.GetSceneCpkFileRelativePath(sceneCpkFileName));
            }

            #if PAL3A
            filesToMount.Add(FileConstants.ScnCpkFileRelativePath);
            filesToMount.Add(FileConstants.SceCpkFileRelativePath);
            #endif

            foreach (var cpkFileRelativePath in filesToMount)
            {
                cpkFileSystem.Mount(cpkFileRelativePath, codepage);
            }

            return cpkFileSystem;
        }
    }
}