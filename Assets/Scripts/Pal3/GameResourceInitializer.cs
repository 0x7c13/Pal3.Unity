// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
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
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using Core.Services;
    using Core.Utils;
    using Data;
    using MetaData;
    using Renderer;
    using TMPro;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// The Game Resource initializer
    /// Initialize file system etc.
    /// </summary>
    public sealed class GameResourceInitializer : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera uiCamera;
        [SerializeField] private GameObject startingComponent;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI loadingText;

        private const string CUSTOM_GAME_FOLDER_PATH_FILE_NAME = "CustomGameFolderPath.txt";
        private const int GBK_CODE_PAGE = 936; // GBK Encoding's code page
        
        private string GetCustomGameFolderPathFilePath()
        {
            return Application.persistentDataPath +
                   Path.DirectorySeparatorChar +
                   CUSTOM_GAME_FOLDER_PATH_FILE_NAME;
        }

        private string GetDefaultGameFolderPath()
        {
            return Application.persistentDataPath +
                   Path.DirectorySeparatorChar +
                   GameConstants.AppName +
                   Path.DirectorySeparatorChar;
        }

        private string GetGameFolderPath()
        {
            string gameFolderRootPath;

            if (Utility.IsDesktopDevice())
            {
                gameFolderRootPath = File.Exists(GetCustomGameFolderPathFilePath()) ?
                    File.ReadAllText(GetCustomGameFolderPathFilePath()).Trim() :
                    #if UNITY_EDITOR
                    PickGameRootPath();
                    #else
                    GetDefaultGameFolderPath();
                    #endif
            }
            else
            {
                gameFolderRootPath = GetDefaultGameFolderPath();
            }

            if (Utility.IsHandheldDevice())
            {
                string fileName = Application.persistentDataPath + Path.DirectorySeparatorChar +
                                  $"把{GameConstants.AppNameCNShort}文件夹{GameConstants.AppName}拖到这里";
                StreamWriter fileWriter = File.CreateText(fileName);
                fileWriter.WriteLine($"把{GameConstants.AppNameCNShort}文件夹{GameConstants.AppName}拖到这里");
                fileWriter.Close();
            }

            return gameFolderRootPath;
        }

        #if UNITY_EDITOR
        private string PickGameRootPath()
        {
            return EditorUtility.OpenFolderPanel($"请选择{GameConstants.AppNameCNFull}原始游戏文件夹根目录",
                "", GameConstants.AppName);
        }
        #endif

        private IEnumerator Start()
        {
            // Create and init CRC hash
            var crcHash = new CrcHash();
            crcHash.Init();
            ServiceLocator.Instance.Register<CrcHash>(crcHash);

            loadingText.text = "Loading game assets...";

            // TODO: let user to choose language? Or auto-detect encoding?
            var codepage = GBK_CODE_PAGE;
            
            yield return InitResource(GetGameFolderPath(), crcHash, codepage);
        }

        private IEnumerator InitResource(string gameRootPath, CrcHash crcHash, int codepage)
        {
            ICpkFileSystem cpkFileSystem = null;
            // Init file system
            yield return InitFileSystem(gameRootPath, crcHash, codepage, (fileSystem) =>
            {
                cpkFileSystem = fileSystem;
            });
            if (cpkFileSystem == null) yield break;
            ServiceLocator.Instance.Register<ICpkFileSystem>(cpkFileSystem);

            // Init TextureLoaderFactory
            var textureLoaderFactory = new TextureLoaderFactory();
            ServiceLocator.Instance.Register<ITextureLoaderFactory>(textureLoaderFactory);

            // Init Game resource provider
            var resourceProvider = new GameResourceProvider(cpkFileSystem,
                new TextureLoaderFactory(),
                new MaterialFactory(),
                codepage);
            ServiceLocator.Instance.Register(resourceProvider);

            // Instantiate starting component
            GameObject startingGameObject = Instantiate(startingComponent, null);
            startingGameObject.name = startingComponent.name;

            yield return FadeTextAndBackgroundImage();

            if (Utility.IsDesktopDevice())
            {
                File.WriteAllText(GetCustomGameFolderPathFilePath(), gameRootPath);
            }

            Dispose();
        }

        private void Dispose()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            gameObject.name = nameof(ServiceLocator);
            Destroy(this);
        }

        private IEnumerator InitFileSystem(string gameRootPath,
            CrcHash crcHash,
            int codepage,
            Action<ICpkFileSystem> callback)
        {
            ICpkFileSystem cpkFileSystem = null;
            var path = gameRootPath;
            Exception exception = null;

            var fileSystemInitThread = new Thread(() =>
            {
                try
                {
                    var timer = new Stopwatch();
                    timer.Start();
                    cpkFileSystem = InitializeCpkFileSystem(path, crcHash, codepage);
                    timer.Stop();
                    Debug.Log($"All cpk files mounted successfully under {path}. Total time: {timer.Elapsed.TotalSeconds:0.000}s");
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    System.GC.Collect();
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

            if (cpkFileSystem != null)
            {
                callback?.Invoke(cpkFileSystem);
            }
            else
            {
                loadingText.text = $"{exception.Message}";

                #if UNITY_EDITOR
                if (Utility.IsDesktopDevice())
                {
                    gameRootPath = PickGameRootPath();

                    if (!string.IsNullOrEmpty(gameRootPath))
                    {
                        StartCoroutine(InitResource(gameRootPath, crcHash, codepage));
                    }
                }
                #endif
            }
        }

        private IEnumerator FadeTextAndBackgroundImage()
        {
            loadingText.text = string.Empty;
            loadingText.alpha = 0f;
            loadingText.enabled = false;

            yield return AnimationHelper.EnumerateValue(1f, 0f, duration: 1f, AnimationCurveType.Linear,
                value =>
            {
                backgroundImage.color = new Color(0, 0, 0, value);
            });

            backgroundImage.color = new Color(0, 0, 0, 0);
            backgroundImage.enabled = false;
        }

        private ICpkFileSystem InitializeCpkFileSystem(string gameRootPath, CrcHash crcHash, int codepage)
        {
            var cpkFileSystem = new CpkFileSystem(gameRootPath, crcHash);

            var filesToMount = new List<string>();

            var baseDataCpk = FileConstants.BaseDataCpkPathInfo.relativePath +
                              Path.DirectorySeparatorChar + FileConstants.BaseDataCpkPathInfo.cpkName;

            filesToMount.Add(baseDataCpk);

            var musicCpk = FileConstants.MusicCpkPathInfo.relativePath +
                           Path.DirectorySeparatorChar + FileConstants.MusicCpkPathInfo.cpkName;

            filesToMount.Add(musicCpk);

            foreach ((string cpkName, string relativePath) sceneCpkPathInfo in FileConstants.SceneCpkPathInfos)
            {
                var sceneCpkPath = sceneCpkPathInfo.relativePath + Path.DirectorySeparatorChar +
                                   sceneCpkPathInfo.cpkName;
                filesToMount.Add(sceneCpkPath);
            }

            #if PAL3A
            var scnCpk = FileConstants.ScnCpkPathInfo.relativePath +
                          Path.DirectorySeparatorChar + FileConstants.ScnCpkPathInfo.cpkName;
            filesToMount.Add(scnCpk);
            var sceCpk = FileConstants.SceCpkPathInfo.relativePath +
                          Path.DirectorySeparatorChar + FileConstants.SceCpkPathInfo.cpkName;
            filesToMount.Add(sceCpk);
            #endif

            foreach (var cpkFilePath in filesToMount)
            {
                cpkFileSystem.Mount(cpkFilePath, codepage);
            }

            return cpkFileSystem;
        }
    }
}