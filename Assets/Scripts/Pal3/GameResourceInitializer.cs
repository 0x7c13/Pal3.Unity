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
    using Core.FileSystem;
    using Core.Services;
    using Data;
    using MetaData;
    using TMPro;
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

        private string GetGameFolderPath()
        {
            string gameRootPath = Application.platform switch
            {
                RuntimePlatform.WindowsEditor => $"F:\\{GameConstants.AppName}\\",
                RuntimePlatform.OSXEditor => $"/Users/{Environment.UserName}/Workspace/{GameConstants.AppName}/",
                _ => Application.persistentDataPath + Path.DirectorySeparatorChar + GameConstants.AppName + Path.DirectorySeparatorChar,
            };

            if (Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.IPhonePlayer)
            {
                string fileName = Application.persistentDataPath + Path.DirectorySeparatorChar +
                                  $"把仙三文件夹{GameConstants.AppName}拖到这里";
                StreamWriter fileWriter = File.CreateText(fileName);
                fileWriter.WriteLine($"把仙三文件夹{GameConstants.AppName}拖到这里");
                fileWriter.Close();
            }

            return gameRootPath;
        }

        private IEnumerator Start()
        {
            var gameRootPath = GetGameFolderPath();

            loadingText.text = "Loading game assets...";
            yield return new WaitForSeconds(0.2f); // Give some time for UI to render

            // Init file system
            var fileSystem = InitFileSystem(gameRootPath);
            ServiceLocator.Instance.Register<ICpkFileSystem>(fileSystem);

            // Init Game resource provider
            var resourceProvider = new GameResourceProvider(fileSystem);
            ServiceLocator.Instance.Register(resourceProvider);

            // Instantiate starting component
            var startingGameObject = Instantiate(startingComponent, null);
            startingGameObject.name = startingComponent.name;

            yield return FadeTextAndBackgroundImage();

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

        private ICpkFileSystem InitFileSystem(string gameRootPath)
        {
            var result = InitializeCpkFileSystem(gameRootPath);

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

        private (bool Success, ICpkFileSystem FileSystem) InitializeCpkFileSystem(string gameRootPath)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                var cpkFileSystem = new CpkFileSystem(gameRootPath);

                var baseDataCpk  = FileConstants.BaseDataCpkPathInfo.relativePath +
                                  Path.DirectorySeparatorChar + FileConstants.BaseDataCpkPathInfo.cpkName;
                var musicCpk = FileConstants.MusicCpkPathInfo.relativePath +
                               Path.DirectorySeparatorChar + FileConstants.MusicCpkPathInfo.cpkName;

                cpkFileSystem.Mount(baseDataCpk);
                cpkFileSystem.Mount(musicCpk);

                foreach (var sceneCpkPathInfo in FileConstants.SceneCpkPathInfos)
                {
                    var sceneCpkPath = sceneCpkPathInfo.relativePath + Path.DirectorySeparatorChar + sceneCpkPathInfo.cpkName;
                    cpkFileSystem.Mount(sceneCpkPath);
                }

                #if PAL3A
                var scnCpk  = FileConstants.ScnCpkPathInfo.relativePath +
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
            }

            return (false, null);
        }
    }
}