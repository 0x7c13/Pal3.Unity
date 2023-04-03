// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace ResourceViewer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Gdb;
    using Core.DataReader.Mv3;
    using Core.DataReader.Pol;
    using Core.DataReader.Sce;
    using Core.DataReader.Scn;
    using Core.FileSystem;
    using Core.Services;
    using Core.Utils;
    using IngameDebugConsole;
    using Newtonsoft.Json;
    using Pal3.Command;
    using Pal3.Command.SceCommands;
    using Pal3.Data;
    using Pal3.MetaData;
    using Pal3.Renderer;
    using Pal3.Script;
    using Pal3.Settings;
    using TMPro;
    using UnityEditor;
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
        private GameSettings _gameSettings;

        private IList<string> _polFiles = new List<string>();
        private IList<string> _cvdFiles = new List<string>();
        private IList<string> _mv3Files = new List<string>();
        private IList<string> _mp3Files = new List<string>();
        private static readonly Random Random = new ();

        private const int DEFAULT_CODE_PAGE = 936; // GBK Encoding's code page,
                                                   // change it to 950 to supports Traditional Chinese (Big5)
        private GameObject _renderingRoot;

        private void OnEnable()
        {
            _fileSystem = ServiceLocator.Instance.Get<ICpkFileSystem>();
            _resourceProvider = ServiceLocator.Instance.Get<GameResourceProvider>();
            _gameSettings = ServiceLocator.Instance.Get<GameSettings>();

            _renderingRoot = new GameObject("Model");
            _renderingRoot.transform.SetParent(null);

            #if UNITY_2022_1_OR_NEWER
            var monitorRefreshRate = (int)Screen.currentResolution.refreshRateRatio.value;
            #else
            var monitorRefreshRate = Screen.currentResolution.refreshRate;
            #endif

            if (Application.platform is RuntimePlatform.IPhonePlayer or RuntimePlatform.Android)
            {
                DebugLogManager.Instance.PopupEnabled = true;
            }

            if (_gameSettings.IsRealtimeLightingAndShadowsEnabled)
            {
                RenderSettings.ambientIntensity = 1f;
                RenderSettings.ambientLight = Color.white;
            }

            _polFiles = _fileSystem.Search(".pol").ToList();
            _cvdFiles = _fileSystem.Search(".cvd").ToList();
            _mv3Files = _fileSystem.Search(".mv3").ToList();
            _mp3Files = _fileSystem.Search(".mp3").ToList();

            randomPolButton.onClick.AddListener(RandPol);
            randomCvdButton.onClick.AddListener(RandCvd);
            randomMv3Button.onClick.AddListener(RandMv3);
            randomMp3Button.onClick.AddListener(RandMp3);

            DebugLogConsole.AddCommand<string>("Search", "Search files using keyword.", Search);
            DebugLogConsole.AddCommand<string, bool>("Load", "Load a file to the viewer (.pol, .cvd, .mp3 or .mv3).", Load);
            #if UNITY_EDITOR
            DebugLogConsole.AddCommand("DecompileAllSceScripts",
                "Decompile all .sce scripts into txt format.",
                () => DecompileAllSceScripts(dialogueOnly: false));
            DebugLogConsole.AddCommand("DecompileAllDialogues",
                "Decompile all dialogues in .sce scripts into txt format.",
                () => DecompileAllSceScripts(dialogueOnly: true));
            DebugLogConsole.AddCommand("ExtractAllCpkArchives",
                "Extract all .cpk archives into the output directory.",
                ExtractAllCpkArchives);
            #endif

            //__Malicious__Dev_Only__();
        }

        private void __Malicious__Dev_Only__()
        {
            // var objectInfoDic = new Dictionary<ScnSceneObjectType, HashSet<ScnFile>>();
            // var sb = new StringBuilder();
            //
            // foreach (ScnSceneObjectType objectType in Enum.GetValues(typeof(ScnSceneObjectType)).Cast<ScnSceneObjectType>())
            // {
            //     objectInfoDic[objectType] = new HashSet<ScnFile>();
            // }
            //
            // var scnFilePaths = _fileSystem.Search(".scn").ToList();
            // scnFilePaths.Sort();
            //
            // foreach (var scnFilePath in scnFilePaths)
            // {
            //     using var scnFileStream = new MemoryStream(_fileSystem.ReadAllBytes(scnFilePath));
            //     ScnFile scnFile = ScnFileReader.Read(scnFileStream, DEFAULT_CODE_PAGE);
            //
            //     foreach (ScnSceneObjectType objectType in Enum.GetValues(typeof(ScnSceneObjectType)).Cast<ScnSceneObjectType>())
            //     {
            //         if (scnFile.ObjectInfos.Any(_ => _.Type == objectType))
            //         {
            //             objectInfoDic[objectType].Add(scnFile);
            //         }
            //     }
            // }
            //
            //  // Print objectInfoDic to console
            //  foreach (var (objectType, scnFiles) in objectInfoDic)
            //  {
            //      sb.Append($"{objectType}:");
            //      foreach (var scnFile in scnFiles)
            //      {
            //          sb.Append($" {scnFile.SceneInfo.CityName}-{scnFile.SceneInfo.SceneName}");
            //      }
            //      sb.Append("\n");
            //  }
            //
            // File.WriteAllText(@"E:\Workspace\objectDic.txt", sb.ToString());

            // HashSet<char> charSet = File.ReadAllText("charset.txt", Encoding.UTF8).ToHashSet();
            // var newChars = new HashSet<char>();
            //
            // foreach (GameItem item in _resourceProvider.GetGameItems().Values)
            // {
            //     foreach (var nameChar in item.Name.Where(nameChar => !charSet.Contains(nameChar)))
            //     {
            //         newChars.Add(nameChar);
            //     }
            //     foreach (var descChar in item.Description.Where(descChar => !charSet.Contains(descChar)))
            //     {
            //         newChars.Add(descChar);
            //     }
            // }
            // var sb = new StringBuilder();
            // foreach (var ch in newChars) sb.Append(ch);
            // Debug.Log(sb.ToString());
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
                (PolFile polyFile, ITextureResourceProvider textureProvider) = _resourceProvider.GetPol(filePath);

                var mesh = new GameObject(Utility.GetFileName(filePath, CpkConstants.DirectorySeparator));
                var meshRenderer = mesh.AddComponent<PolyModelRenderer>();
                mesh.transform.SetParent(_renderingRoot.transform);
                meshRenderer.Render(polyFile,
                    textureProvider,
                    _resourceProvider.GetMaterialFactory(),
                    Color.white);

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
                (CvdFile cvdFile, ITextureResourceProvider textureProvider) = _resourceProvider.GetCvd(filePath);

                var mesh = new GameObject(Utility.GetFileName(filePath, CpkConstants.DirectorySeparator));
                var meshRenderer = mesh.AddComponent<CvdModelRenderer>();
                mesh.transform.SetParent(_renderingRoot.transform);

                meshRenderer.Init(cvdFile,
                    _resourceProvider.GetMaterialFactory(),
                    textureProvider,
                    Color.white,
                    0f);

                meshRenderer.LoopAnimation();

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
                (Mv3File mv3File, ITextureResourceProvider textureProvider) = _resourceProvider.GetMv3(filePath);

                if (mv3File.Meshes.Length > 1)
                {
                    Debug.LogWarning($"{filePath} has {mv3File.Meshes.Length} meshes.");
                }

                var animationNode = new GameObject(Utility.GetFileName(filePath, CpkConstants.DirectorySeparator));
                animationNode.transform.SetParent(_renderingRoot.transform);

                var mv3AnimationRenderer = animationNode.AddComponent<Mv3ModelRenderer>();

                // For debugging tag node:
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
                    mv3AnimationRenderer.Init(mv3File,
                        _resourceProvider.GetMaterialFactory(),
                        textureProvider);
                }

                mv3AnimationRenderer.StartAnimation();

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

        #if UNITY_EDITOR
        private void DecompileAllSceScripts(bool dialogueOnly)
        {
            var sceFiles = _fileSystem.Search(".sce")
                .Where(_ => !_.Contains(".bak", StringComparison.OrdinalIgnoreCase));

            string title = dialogueOnly ? "选择脚本导出目录(仅对话)" : "选择脚本导出目录";

            var outputFolderPath = EditorUtility.SaveFolderPanel(title, "", "");
            outputFolderPath += $"{Path.DirectorySeparatorChar}{GameConstants.AppName}" + (dialogueOnly ? "_对话脚本" : "");

            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            foreach (var sceFile in sceFiles) if (!DecompileSce(sceFile, outputFolderPath, dialogueOnly)) break;
        }

        private void ExtractAllCpkArchives()
        {
            var outputFolderPath = EditorUtility.SaveFolderPanel("选择CPK解包后的导出目录", "", "");
            outputFolderPath += Path.DirectorySeparatorChar;

            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            consoleTextUI.text = "正在解包全部CPK文件，请稍等...";
            StartCoroutine(ExtractAllCpkArchivesInternalAsync(outputFolderPath));
        }

        private IEnumerator ExtractAllCpkArchivesInternalAsync(string outputFolderPath)
        {
            var workerThread = new Thread(() =>
            {
                _fileSystem.ExtractTo(outputFolderPath);
            })
            {
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.Highest
            };

            workerThread.Start();

            while (workerThread.IsAlive)
            {
                yield return null;
            }

            consoleTextUI.text = "全部CPK文件已解包完成！";
        }
        #endif

        private readonly Dictionary<string, int> _actorDialogueCountMap = new ();
        private bool DecompileSce(string filePath, string outputFolderPath, bool dialogueOnly)
        {
            var output = new StringBuilder();

            using var sceFileStream = new MemoryStream(_fileSystem.ReadAllBytes(filePath));

            SceFile sceFile;

            try
            {
                sceFile = SceFileReader.Read(sceFileStream, DEFAULT_CODE_PAGE);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return true;
            }

            foreach (var scriptBlock in sceFile.ScriptBlocks)
            {
                output.Append($"----------------------------------------------------------");
                output.Append($"\n{scriptBlock.Value.Id} {scriptBlock.Value.Description}\n");

                using var scriptDataReader = new BinaryReader(new MemoryStream(scriptBlock.Value.ScriptData));

                int dialogueIndex = 1;
                ICommand lastCommand = null;

                while (scriptDataReader.BaseStream.Position < scriptDataReader.BaseStream.Length)
                {
                    var currentPosition = scriptDataReader.BaseStream.Position;
                    var commandId = scriptDataReader.ReadUInt16();
                    var parameterFlag = scriptDataReader.ReadUInt16();

                    ICommand command = SceCommandParser.ParseSceCommand(scriptDataReader, commandId, parameterFlag, DEFAULT_CODE_PAGE);

                    if (dialogueOnly && command is DialogueRenderTextCommand
                            or DialogueRenderTextWithTimeLimitCommand)
                    {
                        string dialogueText = command is DialogueRenderTextCommand textCommand
                            ? textCommand.DialogueText
                            : ((DialogueRenderTextWithTimeLimitCommand) command).DialogueText;

                        bool isTimeLimited = command is DialogueRenderTextWithTimeLimitCommand;

                        string avatarTextureName = lastCommand is DialogueRenderActorAvatarCommand avatarCommand
                            ? avatarCommand.AvatarTextureName
                            : string.Empty;

                        if (dialogueText.Contains("龙葵") && avatarTextureName.StartsWith("106"))
                        {
                            dialogueText = dialogueText.Replace("龙葵", "龙葵(红)");
                        }

                        dialogueText = dialogueText
                            .Replace("\\n", "")
                            .Replace("\\i", "")
                            .Replace("\\r", "");

                        output.Append(isTimeLimited
                            ? $"{dialogueIndex:000} [限时对话] {dialogueText}\n"
                            : $"{dialogueIndex:000} {dialogueText}\n");

                        if (dialogueText.Contains("："))
                        {
                            var actorName = dialogueText.Substring(0, dialogueText.IndexOf("：", StringComparison.Ordinal));
                            if (_actorDialogueCountMap.ContainsKey(actorName)) _actorDialogueCountMap[actorName]++;
                            else _actorDialogueCountMap.Add(actorName, 1);
                        }

                        dialogueIndex++;
                    }
                    else if (!dialogueOnly)
                    {
                        output.Append($"{currentPosition} {command.GetType().Name.Replace("Command", "")} " +
                                      $"{JsonConvert.SerializeObject(command)}\n");
                    }

                    lastCommand = command;
                }

                output.Append(dialogueOnly
                    ? $"共{dialogueIndex - 1}句对话 \n"
                    : $"{scriptDataReader.BaseStream.Length} __END__\n");
            }

            var cpkFileName = filePath.Substring(filePath.LastIndexOf(CpkConstants.DirectorySeparator) + 1).Replace(".sce", "");

            var sceneName = SceneConstants.SceneCpkNameInfos
                .FirstOrDefault(_ => string.Equals(_.cpkName, cpkFileName + CpkConstants.FileExtension, StringComparison.OrdinalIgnoreCase)).sceneName;

            if (dialogueOnly && _actorDialogueCountMap.Count > 0)
            {
                output.AppendLine().AppendLine();
                output.Append("-----角色对话统计----\n");
                foreach (var actorDialogueCount in from entry
                             in _actorDialogueCountMap orderby entry.Value descending select entry)
                {
                    output.Append($"{actorDialogueCount.Key} - {actorDialogueCount.Value}句\n");
                }
            }

            File.WriteAllText(string.IsNullOrEmpty(sceneName)
                    ? $"{outputFolderPath}{Path.DirectorySeparatorChar}{cpkFileName}.txt"
                    : $"{outputFolderPath}{Path.DirectorySeparatorChar}{cpkFileName}_{sceneName}.txt", output.ToString());

            _actorDialogueCountMap.Clear();
            return true;
        }

        private bool LoadMp3(string filePath)
        {
            nowPlayingTextUI.text = "* Now Playing: " + Utility.GetFileName(filePath, CpkConstants.DirectorySeparator);
            StartCoroutine(LoadMp3AudioClipAsync(filePath,
                _resourceProvider.GetMp3FilePathInCacheFolder(filePath)));
            return true;
        }

        private IEnumerator LoadMp3AudioClipAsync(string fileVirtualPath, string writePath)
        {
            yield return _resourceProvider.ExtractAndMoveMp3FileToCacheFolderAsync(fileVirtualPath, writePath);
            yield return _resourceProvider.LoadAudioClipAsync(writePath, AudioType.MPEG, streamAudio: true, audioClip =>
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