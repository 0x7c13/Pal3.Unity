// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace ResourceViewer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Sce;
    using Core.FileSystem;
    using Core.Services;
    using Core.Utils;
    using IngameDebugConsole;
    using Newtonsoft.Json;
    using Pal3.Data;
    using Pal3.Dev;
    using Pal3.Renderer;
    using Pal3.Script;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using Random = System.Random;

    /// <summary>
    /// The PAL3/PAL3A Game Resource Viewer app model
    /// </summary>
    public class GameResourceViewer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI consoleTextUI;
        [SerializeField] private TextMeshProUGUI nowPlayingTextUI;
        [SerializeField] private TextMeshProUGUI fpsTextUI;
        [SerializeField] private Button randomPolButton;
        [SerializeField] private Button randomCvdButton;
        [SerializeField] private Button randomMv3Button;
        [SerializeField] private Button randomMp3Button;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private FpsCounter fpsCounter;

        private ICpkFileSystem _fileSystem;
        private GameResourceProvider _resourceProvider;
        private static IList<string> _polFiles = new List<string>();
        private static IList<string> _cvdFiles = new List<string>();
        private static IList<string> _mv3Files = new List<string>();
        private static IList<string> _mp3Files = new List<string>();
        private static IList<string> _sceFiles = new List<string>();
        private static readonly Random Random = new ();

        private GameObject _renderingRoot;

        private void OnEnable()
        {
            _fileSystem = ServiceLocator.Instance.Get<ICpkFileSystem>();
            _resourceProvider = ServiceLocator.Instance.Get<GameResourceProvider>();

            _renderingRoot = new GameObject("Model");
            _renderingRoot.transform.SetParent(null);

            Application.targetFrameRate = Application.platform switch
            {
                RuntimePlatform.WindowsEditor => 120,
                RuntimePlatform.WindowsPlayer => 120,
                RuntimePlatform.OSXEditor => 120,
                RuntimePlatform.OSXPlayer => 120,
                RuntimePlatform.LinuxEditor => 120,
                RuntimePlatform.LinuxPlayer => 120,
                RuntimePlatform.IPhonePlayer => 60,
                RuntimePlatform.Android => 60,
                _ => -1,
            };

            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.Android)
            {
                DebugLogManager.Instance.PopupEnabled = true;
            }

            _polFiles = _fileSystem.Search(".pol").ToList();
            _cvdFiles = _fileSystem.Search(".cvd").ToList();
            _mv3Files = _fileSystem.Search(".mv3").ToList();
            _mp3Files = _fileSystem.Search(".mp3").ToList();
            _sceFiles = _fileSystem.Search(".sce").ToList();

            randomPolButton.onClick.AddListener(RandPol);
            randomCvdButton.onClick.AddListener(RandCvd);
            randomMv3Button.onClick.AddListener(RandMv3);
            randomMp3Button.onClick.AddListener(RandMp3);

            DebugLogConsole.AddCommand<string>("search", "Search files using keyword.", Search);
            DebugLogConsole.AddCommand<string, bool>("load", "Load a file to the viewer (.pol or .mv3).", Load);

            // foreach (var sceFile in _sceFiles)
            // {
            //     if (!LoadSce(sceFile)) break;
            // }
        }

        private void Update()
        {
            fpsTextUI.text = $"{Mathf.Ceil(fpsCounter.GetFps())} fps";
        }

        private void RandPol()
        {
            while (!LoadPol(_polFiles[Random.Next(0, _polFiles.Count)])) { }
        }

        private void RandCvd()
        {
            while (!LoadCvd(_cvdFiles[Random.Next(0, _cvdFiles.Count)])) { }
        }

        private void RandMv3()
        {
            while (!LoadMv3(_mv3Files[Random.Next(0, _mv3Files.Count)])) { }
        }

        private void RandMp3()
        {
            while (!LoadMp3(_mp3Files[Random.Next(0, _mp3Files.Count)])) { }
        }

        private void Search(string keyword)
        {
            foreach (var result in _fileSystem.Search(keyword))
            {
                Debug.Log(result);
            }
        }

        private bool Load(string filePath)
        {
            if (!_fileSystem.FileExists(filePath))
            {
                Debug.LogError($"{filePath} does not exists.");
                return false;
            }

            var ext = Path.GetExtension(filePath).ToLower();
            return ext switch
            {
                ".pol" => LoadPol(filePath),
                ".cvd" => LoadCvd(filePath),
                ".mv3" => LoadMv3(filePath),
                ".mp3" => LoadMp3(filePath),
                ".sce" => LoadSce(filePath),
                _ => throw new Exception($"File extension: <{ext}> not supported.")
            };
        }

        private bool LoadPol(string filePath)
        {
            DestroyExistingRenderingObjects();

            consoleTextUI.text = $"Loading: {filePath}";
            Debug.Log($"Loading: {filePath}");

            try
            {
                var (polyFile, textureProvider) = _resourceProvider.GetPol(filePath);

                var mesh = new GameObject(Utility.GetFileName(filePath, CpkConstants.CpkDirectorySeparatorChar));
                var meshRenderer = mesh.AddComponent<PolyModelRenderer>();
                mesh.transform.SetParent(_renderingRoot.transform);
                meshRenderer.Render(polyFile, textureProvider, Color.white);

                consoleTextUI.text = $"{filePath}";

                Camera.main!.transform.LookAt(new Vector3(0, 0, 0));

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                consoleTextUI.text = $"Failed to load: {filePath}";
                return false;
            }
        }

        private bool LoadCvd(string filePath)
        {
            DestroyExistingRenderingObjects();

            consoleTextUI.text = $"Loading: {filePath}";
            Debug.Log($"Loading: {filePath}");

            try
            {
                var (cvdFile, textureProvider) = _resourceProvider.GetCvd(filePath);

                var mesh = new GameObject(Utility.GetFileName(filePath, CpkConstants.CpkDirectorySeparatorChar));
                var meshRenderer = mesh.AddComponent<CvdModelRenderer>();
                mesh.transform.SetParent(_renderingRoot.transform);

                meshRenderer.Init(cvdFile, textureProvider, Color.white, 0f);
                meshRenderer.PlayAnimation();

                consoleTextUI.text = $"{filePath}";

                Camera.main!.transform.LookAt(new Vector3(0, 0, 0));

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                consoleTextUI.text = $"Failed to load: {filePath}";
                return false;
            }
        }

        private bool LoadMv3(string filePath)
        {
            DestroyExistingRenderingObjects();

            consoleTextUI.text = $"Loading: {filePath}";
            Debug.Log($"Loading: {filePath}");

            try
            {
                var (mv3File, textureProvider) = _resourceProvider.GetMv3(filePath);

                if (mv3File.Meshes.Length > 1)
                {
                    Debug.LogWarning($"{filePath} has {mv3File.Meshes.Length} meshes.");
                }

                var animationNode = new GameObject(Utility.GetFileName(filePath, CpkConstants.CpkDirectorySeparatorChar));
                animationNode.transform.SetParent(_renderingRoot.transform);

                var mv3AnimationRenderer = animationNode.AddComponent<Mv3ModelRenderer>();

                // For debuging tag node:
                // if (mv3File.TagNodes is {Length: > 0} &&
                //     !mv3File.TagNodes[0].Name.Equals("tag_weapon3", StringComparison.OrdinalIgnoreCase) &&
                //     filePath.Contains(@"ROLE\101"))
                // {
                //     var weaponName = "JT13";
                //
                //     var separator = CpkConstants.CpkDirectorySeparatorChar;
                //
                //     var weaponPath = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}" +
                //                      $"{FileConstants.WeaponFolderName}{separator}{weaponName}{separator}{weaponName}.pol";
                //
                //     var (polFile, weaponTextureProvider) = _resourceProvider.GetPol(weaponPath);
                //     mv3AnimationRenderer.Init(mv3File, textureProvider, Color.white,
                //         polFile, weaponTextureProvider, Color.white);
                // }
                // else
                {
                    mv3AnimationRenderer.Init(mv3File, textureProvider, Color.white);
                }

                mv3AnimationRenderer.PlayAnimation();

                consoleTextUI.text = $"{filePath}";

                Camera.main!.transform.LookAt(new Vector3(0, 2, 0));

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                consoleTextUI.text = $"Failed to load: {filePath}";
                return false;
            }
        }
        
        private bool LoadSce(string filePath)
        {
            consoleTextUI.text = $"Loading: {filePath}";
            Debug.Log($"Loading: {filePath}");

            using var sceFileStream = new MemoryStream(_fileSystem.ReadAllBytes(filePath));
            
            var sceFile = SceFileReader.Read(sceFileStream);

            foreach (var scriptBlock in sceFile.ScriptBlocks)
            {
                Debug.Log($"{filePath}_{scriptBlock.Value.Id}_{scriptBlock.Value.Description}");
                
                var scriptDataReader = new BinaryReader(new MemoryStream(scriptBlock.Value.ScriptData));

                while (scriptDataReader.BaseStream.Position < scriptDataReader.BaseStream.Length)
                {
                    var commandId = scriptDataReader.ReadUInt16();
                    var parameterFlag = scriptDataReader.ReadUInt16();
                    
                    var command = SceCommandParser.ParseSceCommand(scriptDataReader, commandId, parameterFlag);

                    if (command == null)
                    {
                        UnknownSceCommandAnalyzer.AnalyzeCommand(scriptDataReader, commandId, parameterFlag);
                        return false;
                    }
                    
                    Debug.Log($"{command.GetType().Name.Replace("Command", "")} " +
                              $"{JsonConvert.SerializeObject(command)}");
                }
            }
            
            return true;
        }

        private bool LoadMp3(string filePath)
        {
            nowPlayingTextUI.text = "* Now Playing: " + Utility.GetFileName(filePath, CpkConstants.CpkDirectorySeparatorChar);
            StartCoroutine(LoadMp3AudioClip(filePath,
                _resourceProvider.GetMp3FilePathInCacheFolder(filePath)));
            return true;
        }

        private IEnumerator LoadMp3AudioClip(string fileVirtualPath, string writePath)
        {
            yield return _resourceProvider.ExtractAndMoveMp3FileToCacheFolder(fileVirtualPath, writePath);
            yield return AudioClipLoader.LoadAudioClip(writePath, AudioType.MPEG, audioClip =>
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                    Destroy(audioSource.clip);
                }
                audioSource.clip = audioClip;
                audioSource.volume = 0.8f;
                audioSource.loop = true;
                audioSource.Play();
            });
        }

        private void DestroyExistingRenderingObjects()
        {
            foreach (Transform child in _renderingRoot.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Unloads assets that are not used (textures etc.)
            Resources.UnloadUnusedAssets();
        }
    }
}