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
    using Core.DataReader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Gdb;
    using Core.DataReader.Mov;
    using Core.DataReader.Msh;
    using Core.DataReader.Mtl;
    using Core.DataReader.Mv3;
    using Core.DataReader.Pol;
    using Core.DataReader.Sce;
    using Core.DataReader.Scn;
    using Core.DataReader.Txt;
    using Core.Extensions;
    using Core.FileSystem;
    using Core.Services;
    using Core.Utils;
    using IngameDebugConsole;
    using Newtonsoft.Json;
    using Pal3.Command;
    using Pal3.Command.SceCommands;
    using Pal3.Data;
    using Pal3.MetaData;
    using Pal3.Rendering.Renderer;
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
        [SerializeField] private Button randomMovButton;
        [SerializeField] private Button extractAllCpkFilesButton;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private FpsCounter fpsCounter;

        private ICpkFileSystem _fileSystem;
        private GameResourceProvider _resourceProvider;
        private GameSettings _gameSettings;

        private IList<string> _polFiles = new List<string>();
        private IList<string> _cvdFiles = new List<string>();
        private IList<string> _mv3Files = new List<string>();
        private IList<string> _movFiles = new List<string>();
        private IList<string> _mp3Files = new List<string>();
        private static readonly Random Random = new ();

        private GameObject _renderingRoot;

        private int _codePage;
        private HashSet<char> _charSet;

        private void OnEnable()
        {
            _gameSettings = ServiceLocator.Instance.Get<GameSettings>();
            _codePage = _gameSettings.Language == Language.SimplifiedChinese ? 936 : 950;

            _fileSystem = ServiceLocator.Instance.Get<ICpkFileSystem>();
            _resourceProvider = ServiceLocator.Instance.Get<GameResourceProvider>();

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
            _movFiles = _fileSystem.Search(".mov").ToList();
            _mp3Files = _fileSystem.Search(".mp3").ToList();

            randomPolButton.onClick.AddListener(RandPol);
            randomCvdButton.onClick.AddListener(RandCvd);
            randomMv3Button.onClick.AddListener(RandMv3);
            randomMovButton.onClick.AddListener(RandMov);
            randomMp3Button.onClick.AddListener(RandMp3);

            #if UNITY_EDITOR
            extractAllCpkFilesButton.onClick.AddListener(ExtractAllCpkArchives);
            #else
            extractAllCpkFilesButton.interactable = false;
            #endif

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
            //     ScnFile scnFile = _resourceProvider.GetGameResourceFile<ScnFile>(scnFilePath);
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

        private void RandMov()
        {
            while (!LoadMov(_movFiles[Random.Next(0, _movFiles.Count)])) { }
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
                ".mov" => LoadMov(filePath),
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
                PolFile polyFile = _resourceProvider.GetGameResourceFile<PolFile>(filePath);
                ITextureResourceProvider textureProvider = _resourceProvider.CreateTextureResourceProvider(
                    Utility.GetDirectoryName(filePath, CpkConstants.DirectorySeparatorChar));

                var mesh = new GameObject(Utility.GetFileName(filePath, CpkConstants.DirectorySeparatorChar));
                var meshRenderer = mesh.AddComponent<PolyModelRenderer>();
                mesh.transform.SetParent(_renderingRoot.transform);
                meshRenderer.Render(polyFile,
                    textureProvider,
                    _resourceProvider.GetMaterialFactory(),
                    isStaticObject: true,
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
                CvdFile cvdFile = _resourceProvider.GetGameResourceFile<CvdFile>(filePath);
                ITextureResourceProvider textureProvider = _resourceProvider.CreateTextureResourceProvider(
                    Utility.GetDirectoryName(filePath, CpkConstants.DirectorySeparatorChar));

                var animationNode = new GameObject(Utility.GetFileName(filePath, CpkConstants.DirectorySeparatorChar));
                var meshRenderer = animationNode.AddComponent<CvdModelRenderer>();
                animationNode.transform.SetParent(_renderingRoot.transform);

                meshRenderer.Init(cvdFile,
                    textureProvider,
                    _resourceProvider.GetMaterialFactory());

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
                Mv3File mv3File = _resourceProvider.GetGameResourceFile<Mv3File>(filePath);
                ITextureResourceProvider textureProvider = _resourceProvider.CreateTextureResourceProvider(
                    Utility.GetDirectoryName(filePath, CpkConstants.DirectorySeparatorChar));

                if (mv3File.Meshes.Length > 1)
                {
                    Debug.LogWarning($"{filePath} has {mv3File.Meshes.Length} meshes.");
                }

                var animationNode = new GameObject(Utility.GetFileName(filePath, CpkConstants.DirectorySeparatorChar));
                animationNode.transform.SetParent(_renderingRoot.transform);
                var mv3AnimationRenderer = animationNode.AddComponent<Mv3ModelRenderer>();

                //For debugging tag node:
                // if (mv3File.TagNodes is {Length: > 0} &&
                //     !mv3File.TagNodes[0].Name.Equals("tag_weapon3", StringComparison.OrdinalIgnoreCase) &&
                //     filePath.Contains(@"ROLE\101"))
                // {
                //     var weaponName = "JT13";
                //
                //     var weaponPath = FileConstants.GetWeaponModelFileVirtualPath(weaponName);
                //
                //     PolFile polFile = _resourceProvider.GetGameResourceFile<PolFile>(weaponPath);
                //     ITextureResourceProvider weaponTextureProvider = _resourceProvider.CreateTextureResourceProvider(
                //         Utility.GetRelativeDirectoryPath(weaponPath));
                //     mv3AnimationRenderer.Init(mv3File,
                //         _resourceProvider.GetMaterialFactory(),
                //         textureProvider,
                //         Color.white,
                //         polFile,
                //         weaponTextureProvider);
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

        private bool LoadMov(string filePath)
        {
            DestroyExistingRenderingObjects();

            consoleTextUI.text = $"Loading: {filePath}";
            Debug.Log($"Loading: {filePath}");

            try
            {
                var actorFolderPath = Utility.GetDirectoryName(filePath, CpkConstants.DirectorySeparatorChar);
                var actorName = Utility.GetFileName(actorFolderPath, CpkConstants.DirectorySeparatorChar);

                var mshFilePath = filePath.Replace(".mov", ".msh", StringComparison.OrdinalIgnoreCase);
                if (!_fileSystem.FileExists(mshFilePath))
                {
                    mshFilePath = actorFolderPath + CpkConstants.DirectorySeparatorChar + actorName + ".msh";
                }

                var mshFile = _resourceProvider.GetGameResourceFile<MshFile>(mshFilePath);

                string mtlFilePath = actorFolderPath + CpkConstants.DirectorySeparatorChar + actorName + ".mtl";
                var mtlFile = _resourceProvider.GetGameResourceFile<MtlFile>(mtlFilePath);

                var movFile = _resourceProvider.GetGameResourceFile<MovFile>(filePath);

                ITextureResourceProvider textureProvider = _resourceProvider.CreateTextureResourceProvider(
                    Utility.GetDirectoryName(mtlFilePath, CpkConstants.DirectorySeparatorChar));

                var animationNode = new GameObject(Utility.GetFileName(filePath, CpkConstants.DirectorySeparatorChar));
                animationNode.transform.SetParent(_renderingRoot.transform);

                var skeletalModelRenderer = animationNode.GetOrAddComponent<SkeletalModelRenderer>();

                skeletalModelRenderer.Init(mshFile,
                    mtlFile,
                    _resourceProvider.GetMaterialFactory(),
                    textureProvider);

                skeletalModelRenderer.StartAnimation(movFile);

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
            if (string.IsNullOrWhiteSpace(outputFolderPath)) return;

            outputFolderPath += $"{Path.DirectorySeparatorChar}{GameConstants.AppName}" + (dialogueOnly ? "_对话脚本" : "");

            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            _charSet = new HashSet<char>();

            foreach (var sceFile in sceFiles) if (!DecompileSce(sceFile, outputFolderPath, dialogueOnly)) break;

            foreach (var itemInfo in _resourceProvider.GetGameItemInfos().Values)
            {
                foreach (var ch in itemInfo.Name) _charSet.Add(ch);
                foreach (var ch in itemInfo.Description) _charSet.Add(ch);
            }

            foreach (var actorInfo in _resourceProvider.GetCombatActorInfos().Values)
            {
                foreach (var ch in actorInfo.Name) _charSet.Add(ch);
                foreach (var ch in actorInfo.Description) _charSet.Add(ch);
            }

            foreach (var skillInfo in _resourceProvider.GetSkillInfos().Values)
            {
                foreach (var ch in skillInfo.Name) _charSet.Add(ch);
                foreach (var ch in skillInfo.Description) _charSet.Add(ch);
            }

            foreach (var comboSkillInfo in _resourceProvider.GetComboSkillInfos().Values)
            {
                foreach (var ch in comboSkillInfo.Name) _charSet.Add(ch);
                foreach (var ch in comboSkillInfo.Description) _charSet.Add(ch);
            }

            #if PAL3A
            var taskDefinitionFile = _resourceProvider.GetGameResourceFile<TaskDefinitionFile>(
                FileConstants.DataScriptFolderVirtualPath + "task.txt");
            foreach (Task task in taskDefinitionFile.Tasks)
            {
                foreach (var ch in task.Title) _charSet.Add(ch);
                foreach (var ch in task.Description) _charSet.Add(ch);
            }
            #endif

            File.WriteAllText($"{outputFolderPath}{Path.DirectorySeparatorChar}charset.txt", string.Join("", _charSet));
            _charSet.Clear();
        }

        private void ExtractAllCpkArchives()
        {
            var outputFolderPath = EditorUtility.SaveFolderPanel("选择CPK解包后的导出目录", "", "");
            if (string.IsNullOrWhiteSpace(outputFolderPath)) return;

            outputFolderPath += Path.DirectorySeparatorChar + GameConstants.AppName + Path.DirectorySeparatorChar;

            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            extractAllCpkFilesButton.interactable = false;
            consoleTextUI.text = "正在解包全部CPK文件，请稍等...";
            StartCoroutine(ExtractAllCpkArchivesInternalAsync(outputFolderPath));
        }

        private bool _isMovieCpkMounted = false;
        private IEnumerator ExtractAllCpkArchivesInternalAsync(string outputFolderPath)
        {
            // Movie CPKs are not mounted by default since we don't need them in normal gameplay.
            // Mount them here if they are not mounted yet. So that we can extract them as well.
            if (!_isMovieCpkMounted)
            {
                foreach (string movieCpkFileName in FileConstants.MovieCpkFileNames)
                {
                    var movieCpkFilePath = FileConstants.GetMovieCpkFileRelativePath(movieCpkFileName);
                    if (File.Exists(_fileSystem.GetRootPath() + movieCpkFilePath))
                    {
                        _fileSystem.Mount(movieCpkFilePath, _codePage);
                    }
                }

                _isMovieCpkMounted = true;
            }

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
            extractAllCpkFilesButton.interactable = true;
        }
        #endif

        private readonly Dictionary<string, int> _actorDialogueCountMap = new ();
        private bool DecompileSce(string filePath, string outputFolderPath, bool dialogueOnly)
        {
            var output = new StringBuilder();

            SceFile sceFile;

            try
            {
                IFileReader<SceFile> sceFileReader = new SceFileReader();
                sceFile = sceFileReader.Read(_fileSystem.ReadAllBytes(filePath), _codePage);
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

                using var scriptDataReader = new SafeBinaryReader(scriptBlock.Value.ScriptData);

                int dialogueIndex = 1;
                ICommand lastCommand = null;

                while (scriptDataReader.BaseStream.Position < scriptDataReader.BaseStream.Length)
                {
                    var currentPosition = scriptDataReader.BaseStream.Position;

                    ICommand command = SceCommandParser.ParseSceCommand(scriptDataReader, _codePage);

                    if (command is DialogueRenderTextCommand dtc) foreach (var ch in dtc.DialogueText) _charSet.Add(ch);
                    if (command is DialogueRenderTextWithTimeLimitCommand dttlc) foreach (var ch in dttlc.DialogueText) _charSet.Add(ch);
                    if (command is UIDisplayNoteCommand unc) foreach (var ch in unc.Note) _charSet.Add(ch);

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

            var cpkFileName = filePath.Substring(filePath.LastIndexOf(CpkConstants.DirectorySeparatorChar) + 1).Replace(".sce", "");

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
            nowPlayingTextUI.text = "* Now Playing: " + Utility.GetFileName(filePath, CpkConstants.DirectorySeparatorChar);
            StartCoroutine(LoadMp3AudioClipAsync(filePath,
                _resourceProvider.GetMusicFilePathInCacheFolder(filePath)));
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
                    audioSource.clip.Destroy();
                }
                audioSource.clip = audioClip;
                audioSource.volume = 0.5f;
                audioSource.loop = true;
                audioSource.Play();
            });
        }

        private void DestroyExistingRenderingObjects()
        {
            foreach (Transform child in _renderingRoot.transform)
            {
                child.gameObject.Destroy();
            }

            // Unloads assets that are not used (textures etc.)
            Resources.UnloadUnusedAssets();
        }
    }
}