﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Data
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Command;
    using Command.InternalCommands;
    using Core.DataLoader;
    using Core.DataReader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Gdb;
    using Core.DataReader.Ini;
    using Core.DataReader.Lgt;
    using Core.DataReader.Mov;
    using Core.DataReader.Mv3;
    using Core.DataReader.Nav;
    using Core.DataReader.Pol;
    using Core.DataReader.Sce;
    using Core.DataReader.Scn;
    using Core.FileSystem;
    using Core.Services;
    using Core.Utils;
    using MetaData;
    using Renderer;
    using Settings;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Single resource provider for accessing game data.
    /// Also manages the lifecycle of the resource it provides.
    /// </summary>
    public sealed class GameResourceProvider : IDisposable,
        ICommandExecutor<ScenePreLoadingNotification>
    {
        private const string CACHE_FOLDER_NAME = "CacheData";
        private const string MV3_ACTOR_CONFIG_HEADER = ";#MV3#";

        private const char PathSeparator = CpkConstants.DirectorySeparator;

        private readonly ICpkFileSystem _fileSystem;
        private readonly ITextureLoaderFactory _textureLoaderFactory;
        private readonly IMaterialFactory _unlitMaterialFactory;
        private readonly IMaterialFactory _litMaterialFactory;
        private readonly GameSettings _gameSettings;

        private readonly GdbFile _gameDatabase;

        private TextureCache _textureCache;

        private readonly Dictionary<string, Sprite> _spriteCache = new ();
        private readonly Dictionary<string, AudioClip> _audioClipCache = new ();
        private readonly Dictionary<int, Object> _vfxEffectPrefabCache = new ();

        private readonly Dictionary<string, ActorActionConfig> _actorConfigCache = new (); // Cache forever
        private readonly Dictionary<Type, Dictionary<string, object>> _gameResourceFileCache = new ();

        // Cache player actor movement sfx audio clips
        private readonly HashSet<string> _audioClipCacheList = AudioConstants.PlayerActorMovementSfxAudioFileNames;

        // No need to deallocate the shadow texture since it is been used almost every where.
        private static readonly Texture2D ShadowTexture = Resources.Load<Texture2D>("Textures/shadow");

        public GameResourceProvider(ICpkFileSystem fileSystem,
            ITextureLoaderFactory textureLoaderFactory,
            IMaterialFactory unlitMaterialFactory,
            IMaterialFactory litMaterialFactory,
            GameSettings gameSettings)
        {
            _fileSystem = Requires.IsNotNull(fileSystem, nameof(fileSystem));
            _textureLoaderFactory = Requires.IsNotNull(textureLoaderFactory, nameof(textureLoaderFactory));
            _unlitMaterialFactory = Requires.IsNotNull(unlitMaterialFactory, nameof(unlitMaterialFactory));
            _litMaterialFactory = litMaterialFactory; // Lit materials are not required
            _gameSettings = Requires.IsNotNull(gameSettings, nameof(gameSettings));

            var gdbFilePath =
                $"{FileConstants.BaseDataCpkPathInfo.cpkName}{PathSeparator}" +
                $"{FileConstants.CombatDataFolderName}{PathSeparator}{GameConstants.AppName}_Softstar.gdb";

            _gameDatabase = GetGameResourceFileReader<GdbFile>().Read(_fileSystem.ReadAllBytes(gdbFilePath));

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private IFileReader<T> GetGameResourceFileReader<T>()
        {
            return ServiceLocator.Instance.Get<IFileReader<T>>();
        }

        public void Dispose()
        {
            _textureCache?.DisposeAll();
            _spriteCache.Clear();
            _gameResourceFileCache.Clear();
            _actorConfigCache.Clear();
            _audioClipCache.Clear();
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void UseTextureCache(TextureCache textureCache)
        {
            _textureCache = textureCache;
        }

        public bool FileExists(string path)
        {
            return _fileSystem.FileExists(path);
        }

        public Texture2D GetShadowTexture()
        {
            return ShadowTexture;
        }

        public IMaterialFactory GetMaterialFactory()
        {
            return !_gameSettings.IsOpenSourceVersion && _gameSettings.IsRealtimeLightingAndShadowsEnabled ?
                _litMaterialFactory :
                _unlitMaterialFactory;
        }

        public ITextureResourceProvider CreateTextureResourceProvider(string relativeDirectoryPath, bool useCache = true)
        {
            return (_textureCache != null && useCache) ?
                new TextureProvider(_fileSystem, _textureLoaderFactory, relativeDirectoryPath, _textureCache) :
                new TextureProvider(_fileSystem, _textureLoaderFactory, relativeDirectoryPath);
        }

        public T GetGameResourceFile<T>(string filePath, bool useCache = true)
        {
            filePath = filePath.ToLower();

            if (useCache &&
                _gameResourceFileCache.ContainsKey(typeof(T)) &&
                _gameResourceFileCache[typeof(T)].ContainsKey(filePath))
            {
                return (T)_gameResourceFileCache[typeof(T)][filePath];
            }

            T file = GetGameResourceFileReader<T>().Read(_fileSystem.ReadAllBytes(filePath));

            if (useCache)
            {
                // initialize cache for this type of file if not exists
                if (!_gameResourceFileCache.ContainsKey(typeof(T)))
                {
                    _gameResourceFileCache[typeof(T)] = new Dictionary<string, object>();
                }
                _gameResourceFileCache[typeof(T)][filePath] = file;
            }

            return file;
        }

        public LgtFile GetLgt(string lgtFilePath)
        {
            return GetGameResourceFileReader<LgtFile>().Read(_fileSystem.ReadAllBytes(lgtFilePath));
        }

        public NavFile GetNav(string sceneFileName, string sceneName)
        {
            #if PAL3
            var navFilePath = $"{sceneFileName}{CpkConstants.FileExtension}{PathSeparator}" +
                              $"{sceneName}{PathSeparator}{sceneName}.nav";
            #elif PAL3A
            var navFilePath = $"{FileConstants.ScnCpkPathInfo.cpkName}{PathSeparator}SCN{PathSeparator}" +
                              $"{sceneFileName}{PathSeparator}{sceneName}{PathSeparator}{sceneName}.nav";
            #endif

            return GetGameResourceFileReader<NavFile>().Read(_fileSystem.ReadAllBytes(navFilePath));
        }

        public ScnFile GetScn(string sceneFileName, string sceneName)
        {
            #if PAL3
            var scnFilePath = $"{sceneFileName}{CpkConstants.FileExtension}{PathSeparator}{sceneName}.scn";
            #elif PAL3A
            var scnFilePath = $"{FileConstants.ScnCpkPathInfo.cpkName}{PathSeparator}SCN{PathSeparator}" +
                              $"{sceneFileName}{PathSeparator}{sceneFileName}_{sceneName}.scn";
            #endif

            return GetGameResourceFileReader<ScnFile>().Read(_fileSystem.ReadAllBytes(scnFilePath));
        }

        public SceFile GetSceneSce(string sceneFileName)
        {
            #if PAL3
            var sceFilePath = $"{sceneFileName}{CpkConstants.FileExtension}{PathSeparator}{sceneFileName}.sce";
            #elif PAL3A
            var sceFilePath = $"{FileConstants.SceCpkPathInfo.cpkName}{PathSeparator}Sce{PathSeparator}{sceneFileName}.sce";
            #endif

            return GetGameResourceFileReader<SceFile>().Read(_fileSystem.ReadAllBytes(sceFilePath));
        }

        public SceFile GetSystemSce()
        {
            return GetGameResourceFileReader<SceFile>().Read(_fileSystem.ReadAllBytes(FileConstants.SystemSceFileVirtualPath));
        }

        public SceFile GetBigMapSce()
        {
            return GetGameResourceFileReader<SceFile>().Read(_fileSystem.ReadAllBytes(FileConstants.BigMapSceFileVirtualPath));
        }

        public Dictionary<int, GameItem> GetGameItems()
        {
            return _gameDatabase.GameItems;
        }

        /// <summary>
        /// Get music file path in cache folder.
        /// </summary>
        /// <param name="musicFileVirtualPath">music file virtual path</param>
        /// <returns>Mp3 file path in cache folder</returns>
        public string GetMp3FilePathInCacheFolder(string musicFileVirtualPath)
        {
            return Application.persistentDataPath
                   + Path.DirectorySeparatorChar
                   + CACHE_FOLDER_NAME
                   + Path.DirectorySeparatorChar
                   + musicFileVirtualPath.Replace(PathSeparator, Path.DirectorySeparatorChar)
                   .Replace(CpkConstants.FileExtension, string.Empty);
        }

        /// <summary>
        /// Since unity cannot directly load audio clip (mp3 in this case) from memory,
        /// we need to first extract the audio clip from cpk archive, write it to a cached folder
        /// and then use this cached mp3 file path for Unity to consume (using UnityWebRequest).
        /// </summary>
        /// <param name="musicFileVirtualPath">music file virtual path</param>
        /// <param name="musicFileCachePath">music file path in cache folder</param>
        public IEnumerator ExtractAndMoveMp3FileToCacheFolderAsync(string musicFileVirtualPath, string musicFileCachePath)
        {
            if (File.Exists(musicFileCachePath)) yield break;

            Debug.Log($"Writing MP3 file to App's persistent folder: {musicFileVirtualPath}");
            var dataMovementThread = new Thread(() =>
            {
                try
                {
                    new DirectoryInfo(Path.GetDirectoryName(musicFileCachePath) ?? string.Empty).Create();
                    File.WriteAllBytes(musicFileCachePath, _fileSystem.ReadAllBytes(musicFileVirtualPath));
                }
                catch (Exception)
                {
                    // ignore
                }
            })
            {
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.Normal
            };
            dataMovementThread.Start();

            while (dataMovementThread.IsAlive)
            {
                yield return null;
            }
        }

        public string GetSfxFilePath(string sfxName)
        {
            #if PAL3
            string sfxFileName = (sfxName + ".wav").ToLower();
            #elif PAL3A
            string sfxFileName = (sfxName + ".wav").ToUpper();
            #endif

            var sfxFileRelativePath = $"{FileConstants.SfxFolderName}{Path.DirectorySeparatorChar}" + sfxFileName;

            var rootPath = _fileSystem.GetRootPath();
            var sfxFilePath = $"{rootPath}{sfxFileRelativePath}";

            return sfxFilePath;
        }

        public IEnumerator LoadAudioClipAsync(string filePath,
            AudioType audioType,
            bool streamAudio,
            Action<AudioClip> onLoaded)
        {
            var fileName = Path.GetFileName(filePath);
            var shouldCache = _audioClipCacheList.Contains(fileName);

            if (shouldCache && _audioClipCache.ContainsKey(fileName))
            {
                if (_audioClipCache[fileName] != null) // check if clip has been destroyed
                {
                    onLoaded?.Invoke(_audioClipCache[fileName]);
                    yield break;
                }
            }

            yield return AudioClipLoader.LoadAudioClipAsync(filePath,
                audioType,
                streamAudio,
                audioClip =>
            {
                if (shouldCache && audioClip != null)
                {
                    _audioClipCache[fileName] = audioClip;
                }

                onLoaded?.Invoke(audioClip);
            });
        }

        private Texture2D GetActorAvatarTexture(string actorName, string avatarTextureName)
        {
            var roleAvatarTextureRelativePath =
                $"{FileConstants.BaseDataCpkPathInfo.cpkName}{PathSeparator}" +
                $"{FileConstants.ActorFolderName}{PathSeparator}{actorName}{PathSeparator}";

            ITextureResourceProvider textureProvider = CreateTextureResourceProvider(roleAvatarTextureRelativePath);
            return textureProvider.GetTexture($"{avatarTextureName}.tga");
        }

        public Sprite GetActorAvatarSprite(string actorName, string avatarName)
        {
            var cacheKey = $"ActorAvatar_{actorName}_{avatarName}";

            if (_spriteCache.TryGetValue(cacheKey, out Sprite sprite))
            {
                if (sprite != null && sprite.texture != null) return sprite;
            }

            Texture2D texture = GetActorAvatarTexture(
                actorName, avatarName);

            if (texture == null) return null;

            var avatarSprite = Sprite.Create(texture,
                // Cut 2f to hide artifacts near edges for some of the avatar textures
                new Rect(2f, 0f, texture.width - 2f, texture.height),
                new Vector2(0.5f, 0f));

            _spriteCache[cacheKey] = avatarSprite;
            return avatarSprite;
        }

        private Texture2D GetEmojiSpriteSheetTexture(ActorEmojiType emojiType)
        {
            var emojiSpriteSheetRelativePath =
                $"{FileConstants.BaseDataCpkPathInfo.cpkName}{PathSeparator}" +
                $"{FileConstants.UIFolderName}{PathSeparator}{FileConstants.EmojiFolderName}{PathSeparator}";

            ITextureResourceProvider textureProvider = CreateTextureResourceProvider(emojiSpriteSheetRelativePath);
            return textureProvider.GetTexture($"EM_{(int)emojiType:00}.tga");
        }

        public Texture2D GetCaptionTexture(string name)
        {
            var captionTextureRelativePath =
                $"{FileConstants.BaseDataCpkPathInfo.cpkName}{PathSeparator}" +
                $"{FileConstants.CaptionFolderName}{PathSeparator}";

            // No need to cache caption texture since it is a one time thing
            ITextureResourceProvider textureProvider = CreateTextureResourceProvider(captionTextureRelativePath, useCache: false);
            return textureProvider.GetTexture($"{name}.tga");
        }

        public Texture2D[] GetSkyBoxTextures(int skyBoxId)
        {
            var relativeFilePath = string.Format(SceneConstants.SkyBoxTexturePathFormat.First(), skyBoxId);

            ITextureResourceProvider textureProvider = CreateTextureResourceProvider(
                Utility.GetRelativeDirectoryPath(relativeFilePath));

            var textures = new Texture2D[SceneConstants.SkyBoxTexturePathFormat.Length];
            for (var i = 0; i < SceneConstants.SkyBoxTexturePathFormat.Length; i++)
            {
                var textureNameFormat = Utility.GetFileName(
                    string.Format(SceneConstants.SkyBoxTexturePathFormat[i], skyBoxId), PathSeparator);
                Texture2D texture = textureProvider.GetTexture(string.Format(textureNameFormat, i));
                // Set wrap mode to clamp to remove "edges" between sides
                texture.wrapMode = TextureWrapMode.Clamp;
                textures[i] = texture;
            }

            return textures;
        }

        public Texture2D GetEffectTexture(string name, out bool hasAlphaChannel)
        {
            var effectFolderRelativePath =
                $"{FileConstants.BaseDataCpkPathInfo.cpkName}{PathSeparator}" +
                $"{FileConstants.EffectFolderName}{PathSeparator}";

            ITextureResourceProvider textureProvider = CreateTextureResourceProvider(effectFolderRelativePath);
            return textureProvider.GetTexture(name, out hasAlphaChannel);
        }

        public Sprite[] GetEmojiSprites(ActorEmojiType emojiType)
        {
            (int Width, int Height, int Frames) textureInfo = ActorEmojiConstants.TextureInfo[emojiType];
            Texture2D spriteSheet = GetEmojiSpriteSheetTexture(emojiType);

            var widthIndex = 0f;
            var sprites = new Sprite[textureInfo.Frames];

            for (var i = 0; i < textureInfo.Frames; i++)
            {
                var cacheKey = $"EmojiSprite_{emojiType}_{i}";

                if (_spriteCache.TryGetValue(cacheKey, out Sprite sprite))
                {
                    if (sprite != null && sprite.texture != null)
                    {
                        sprites[i] = sprite;
                        continue;
                    }
                }

                var emojiSprite = Sprite.Create(spriteSheet, new Rect(widthIndex, 0f,
                        textureInfo.Width, textureInfo.Height), new Vector2(0.5f, 0f));
                _spriteCache[cacheKey] = emojiSprite;
                sprites[i] = emojiSprite;

                widthIndex += textureInfo.Width;
            }

            return sprites;
        }

        public Sprite[] GetJumpIndicatorSprites()
        {
            var relativePath = FileConstants.UISceneFolderVirtualPath + PathSeparator;
            var textureProvider = CreateTextureResourceProvider(relativePath);

            var sprites = new Sprite[4];

            for (var i = 0; i < 4; i++)
            {
                var cacheKey = $"JumpIndicatorSprite_{i}";

                if (_spriteCache.TryGetValue(cacheKey, out Sprite sprite))
                {
                    if (sprite != null && sprite.texture != null)
                    {
                        sprites[i] = sprite;
                        continue;
                    }
                }

                var texture = textureProvider.GetTexture($"tiao{i}.tga");
                var jumpIndicatorSprite = Sprite.Create(texture,
                    new Rect(0f, 0f, 40f, 32f),
                    new Vector2(0.5f, 0f));
                _spriteCache[cacheKey] = jumpIndicatorSprite;
                sprites[i] = jumpIndicatorSprite;
            }

            return sprites;
        }

        public ActorActionConfig GetActorActionConfig(string actorConfigFilePath)
        {
            actorConfigFilePath = actorConfigFilePath.ToLower();

            if (_actorConfigCache.TryGetValue(actorConfigFilePath, out ActorActionConfig actorActionConfig))
            {
                return actorActionConfig;
            }

            if (!_fileSystem.FileExists(actorConfigFilePath))
            {
                _actorConfigCache[actorConfigFilePath] = null;
                return null;
            }

            var configData = _fileSystem.ReadAllBytes(actorConfigFilePath);
            var configHeaderStr = Encoding.ASCII.GetString(configData[..MV3_ACTOR_CONFIG_HEADER.Length]);

            if (string.Equals(MV3_ACTOR_CONFIG_HEADER, configHeaderStr))
            {
                Mv3ActionConfig config = GetGameResourceFileReader<Mv3ActionConfig>().Read(configData);
                _actorConfigCache[actorConfigFilePath] = config;
                return config;
            }
            else // MOV actor config
            {
                MovActionConfig config = GetGameResourceFileReader<MovActionConfig>().Read(configData);
                _actorConfigCache[actorConfigFilePath] = config;
                return config;
            }
        }

        public string GetVideoFilePath(string videoName)
        {
            var videoFolder = $"{_fileSystem.GetRootPath()}{FileConstants.MovieFolderName}{Path.DirectorySeparatorChar}";

            if (!Directory.Exists(videoFolder))
            {
                throw new Exception($"Video directory does not exists: {videoFolder}.");
            }

            var supportedVideoFormats = UnitySupportedVideoFormats.GetSupportedVideoFormats(Application.platform);

            foreach (FileInfo file in new DirectoryInfo(videoFolder).GetFiles($"*.*", SearchOption.AllDirectories))
            {
                var fileExtension = Path.GetExtension(file.Name);
                if (supportedVideoFormats.Contains(fileExtension.ToLower()) &&
                    Path.GetFileNameWithoutExtension(file.Name).ToLower().Equals(videoName.ToLower()))
                {
                    return file.FullName;
                }
            }

            throw new Exception($"Cannot find video file: {videoName} under path: {videoFolder}. " +
                                $"Supported video formats are: {string.Join(" ", supportedVideoFormats)}.");
        }

        public (Texture2D texture, bool hasAlphaChannel)[] GetEffectTextures(GraphicsEffect effect, string texturePathFormat)
        {
            ITextureResourceProvider textureProvider = CreateTextureResourceProvider(
                Utility.GetRelativeDirectoryPath(texturePathFormat));

            if (effect == GraphicsEffect.Fire)
            {
                var numberOfFrames = EffectConstants.AnimatedFireEffectFrameCount;
                var textures = new (Texture2D texture, bool hasAlphaChannel)[numberOfFrames];
                for (var i = 0; i < numberOfFrames; i++)
                {
                    var textureNameFormat = Utility.GetFileName(texturePathFormat, PathSeparator);
                    Texture2D texture = textureProvider.GetTexture(string.Format(textureNameFormat, i + 1), out var hasAlphaChannel);
                    textures[i] = (texture, hasAlphaChannel);
                }

                return textures;
            }

            return Array.Empty<(Texture2D, bool)>();
        }

        private string GetVfxPrefabPath(int effectGroupId)
        {
            return $"Prefabs/VFX/{GameConstants.AppName}/{effectGroupId}";
        }

        public Object GetVfxEffectPrefab(int effectGroupId)
        {
            if (_vfxEffectPrefabCache.TryGetValue(effectGroupId, out Object vfxEffectPrefab))
            {
                return vfxEffectPrefab;
            }

            Object vfxPrefab = Resources.Load(GetVfxPrefabPath(effectGroupId));

            if (vfxPrefab == null)
            {
                Debug.LogWarning("VFX prefab not found: " + effectGroupId);
            }

            _vfxEffectPrefabCache[effectGroupId] = vfxPrefab;
            return vfxPrefab;
        }

        public IEnumerator PreLoadVfxEffectAsync(int effectGroupId)
        {
            if (_vfxEffectPrefabCache.ContainsKey(effectGroupId))
            {
                yield break;
            }

            ResourceRequest request = Resources.LoadAsync(GetVfxPrefabPath(effectGroupId));

            while (!request.isDone)
            {
                yield return request;
            }

            if (request.asset == null)
            {
                Debug.LogWarning("VFX prefab not found: " + effectGroupId);
            }
            else
            {
                Debug.Log("VFX prefab preloaded: " + effectGroupId);
            }

            _vfxEffectPrefabCache[effectGroupId] = request.asset;
        }

        public Texture2D GetCursorTexture()
        {
            var cursorSpriteRelativePath =
                $"{FileConstants.BaseDataCpkPathInfo.cpkName}{PathSeparator}" +
                $"{FileConstants.UIFolderName}{PathSeparator}{FileConstants.CursorFolderName}{PathSeparator}";

            ITextureResourceProvider textureProvider = CreateTextureResourceProvider(cursorSpriteRelativePath);
            Texture2D cursorTexture = textureProvider.GetTexture($"jt.tga");
            return cursorTexture;
        }

        /// <summary>
        /// To warm up the main actor action cache to reduce gameplay shutters.
        /// </summary>
        public void PreLoadMainActorMv3()
        {
            var cachedActions = new HashSet<string>()
            {
                ActorConstants.ActionToNameMap[ActorActionType.Stand],
                ActorConstants.ActionToNameMap[ActorActionType.Walk],
                ActorConstants.ActionToNameMap[ActorActionType.Run],
                ActorConstants.ActionToNameMap[ActorActionType.StepBack],
            };

            foreach (var name in ActorConstants.MainActorNameMap.Values)
            {
                var actorConfigFile = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{PathSeparator}" +
                                      $"{FileConstants.ActorFolderName}{PathSeparator}{name}{PathSeparator}{name}.ini";

                ActorActionConfig actorActionConfig = GetActorActionConfig(actorConfigFile);

                foreach (ActorAction actorAction in actorActionConfig.ActorActions)
                {
                    // Cache known actions only.
                    if (!cachedActions.Contains(actorAction.ActionName.ToLower())) continue;

                    var mv3FilePath = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{PathSeparator}" +
                                      $"{FileConstants.ActorFolderName}{PathSeparator}{name}{PathSeparator}" +
                                      $"{actorAction.ActionFileName}";

                    _ = GetGameResourceFile<Mv3File>(mv3FilePath); // Call GetMv3 to cache the mv3 file.
                }
            }
        }

        private string _currentCityName;
        public void Execute(ScenePreLoadingNotification notification)
        {
            var newCityName = notification.NewSceneInfo.CityName.ToLower();

            if (string.IsNullOrEmpty(_currentCityName))
            {
                _currentCityName = newCityName;
                return;
            }

            if (!newCityName.Equals(_currentCityName, StringComparison.OrdinalIgnoreCase))
            {
                // Clean up cache after exiting current scene block
                _textureCache?.DisposeAll();

                foreach (Sprite sprite in _spriteCache.Values)
                {
                    Object.Destroy(sprite);
                }
                _spriteCache.Clear();

                if (_gameResourceFileCache.ContainsKey(typeof(PolFile)))
                {
                    _gameResourceFileCache[typeof(PolFile)]?.Clear();
                }

                if (_gameResourceFileCache.ContainsKey(typeof(CvdFile)))
                {
                    _gameResourceFileCache[typeof(CvdFile)]?.Clear();
                }

                if (_gameResourceFileCache.ContainsKey(typeof(MovFile)))
                {
                    _gameResourceFileCache[typeof(MovFile)]?.Clear();
                }

                if (_gameResourceFileCache.ContainsKey(typeof(Mv3File)))
                {
                    var mv3FileCache = _gameResourceFileCache[typeof(Mv3File)];

                    // Dispose non-main actor mv3 files
                    // All main actor names start with "1"
                    var mainActorMv3 = $"{FileConstants.ActorFolderName}{PathSeparator}1".ToLower();
                    var mv3FilesToDispose = mv3FileCache.Keys
                        .Where(mv3FilePath => !mv3FilePath.Contains(mainActorMv3))
                        .ToArray();
                    foreach (var mv3File in mv3FilesToDispose)
                    {
                        mv3FileCache.Remove(mv3File);
                    }
                }

                // clear all vfx prefabs in cache
                _vfxEffectPrefabCache.Clear();

                _currentCityName = newCityName;
            }

            // Unloads assets that are not used (textures etc.)
            Resources.UnloadUnusedAssets();
        }
    }
}