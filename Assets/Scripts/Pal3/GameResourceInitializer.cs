// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.IO;
    using Core.Animation;
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using Core.Services;
    using Core.Utils;
    using Data;
    using MetaData;
    using TMPro;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// The Game Resource initializer
    /// Initialize file system etc.
    /// </summary>
    public class GameResourceInitializer : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera uiCamera;
        [SerializeField] private GameObject startingComponent;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI loadingText;

        private const string CUSTOM_GAME_FOLDER_PATH_FILE_NAME = "CustomGameFolderPath.txt";

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
            loadingText.text = "Loading game assets...";
            yield return new WaitForEndOfFrame();
            yield return InitResource(GetGameFolderPath());
        }

        private IEnumerator InitResource(string gameRootPath)
        {
            yield return new WaitForEndOfFrame(); // Give some time for UI to render

            // Init CRC hash
            var crcHash = new CrcHash();
            ServiceLocator.Instance.Register<CrcHash>(crcHash);

            // Init file system
            var fileSystem = InitFileSystem(gameRootPath, crcHash);
            ServiceLocator.Instance.Register<ICpkFileSystem>(fileSystem);

            // Init TextureLoaderFactory
            var textureLoaderFactory = new TextureLoaderFactory();
            ServiceLocator.Instance.Register<ITextureLoaderFactory>(textureLoaderFactory);

            // Init Game resource provider
            var resourceProvider = new GameResourceProvider(fileSystem, new TextureLoaderFactory());
            ServiceLocator.Instance.Register(resourceProvider);

            // Instantiate starting component
            var startingGameObject = Instantiate(startingComponent, null);
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

        private ICpkFileSystem InitFileSystem(string gameRootPath, CrcHash crcHash)
        {
            var result = InitializeCpkFileSystem(gameRootPath, crcHash);

            if (result.Success)
            {
                return result.FileSystem;
            }
            else
            {
                throw new Exception("Failed to initialize CpkFileSystem.");
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

        private (bool Success, ICpkFileSystem FileSystem) InitializeCpkFileSystem(string gameRootPath, CrcHash crcHash)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                var cpkFileSystem = new CpkFileSystem(gameRootPath, crcHash);

                var baseDataCpk = FileConstants.BaseDataCpkPathInfo.relativePath +
                                  Path.DirectorySeparatorChar + FileConstants.BaseDataCpkPathInfo.cpkName;
                var musicCpk = FileConstants.MusicCpkPathInfo.relativePath +
                               Path.DirectorySeparatorChar + FileConstants.MusicCpkPathInfo.cpkName;

                cpkFileSystem.Mount(baseDataCpk);
                cpkFileSystem.Mount(musicCpk);

                foreach (var sceneCpkPathInfo in FileConstants.SceneCpkPathInfos)
                {
                    var sceneCpkPath = sceneCpkPathInfo.relativePath + Path.DirectorySeparatorChar +
                                       sceneCpkPathInfo.cpkName;
                    cpkFileSystem.Mount(sceneCpkPath);
                }

                #if PAL3A
                var scnCpk = FileConstants.ScnCpkPathInfo.relativePath +
                              Path.DirectorySeparatorChar + FileConstants.ScnCpkPathInfo.cpkName;
                var sceCpk = FileConstants.SceCpkPathInfo.relativePath +
                              Path.DirectorySeparatorChar + FileConstants.SceCpkPathInfo.cpkName;
                cpkFileSystem.Mount(scnCpk);
                cpkFileSystem.Mount(sceCpk);
                #endif

                stopWatch.Stop();
                Debug.Log($"All files mounted successfully. Total time: {stopWatch.Elapsed.TotalSeconds}s");

                return (true, cpkFileSystem);
            }
            catch (Exception ex)
            {
                loadingText.text = $"{ex.Message}";

                #if UNITY_EDITOR
                if (Utility.IsDesktopDevice())
                {
                    gameRootPath = PickGameRootPath();

                    if (!string.IsNullOrEmpty(gameRootPath))
                    {
                        StartCoroutine(InitResource(gameRootPath));
                    }
                }
                #endif
            }
            finally
            {
                System.GC.Collect();
            }

            return (false, null);
        }
    }
}