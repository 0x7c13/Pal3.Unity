// ---------------------------------------------------------------------------------------------
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
    using Command.Extensions;
    using Constants;
    using Core.Command;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Gdb;
    using Core.DataReader.Ini;
    using Core.DataReader.Mv3;
    using Core.FileSystem;
    using Core.Utilities;
    using Engine.DataLoader;
    using Engine.Extensions;
    using Engine.Logging;
    using Engine.Services;
    using Engine.Utilities;
    using Rendering.Material;
    using Settings;
    using UnityEngine;

    /// <summary>
    /// Single resource provider for accessing game data.
    /// Also manages the lifecycle of the resource it provides.
    /// </summary>
    public sealed class GameResourceProvider : IDisposable,
        ICommandExecutor<ScenePreLoadingNotification>,
        ICommandExecutor<SettingChangedNotification>
    {
        private const string CACHE_FOLDER_NAME = "CacheData";
        private const string MV3_ACTOR_CONFIG_HEADER = ";#MV3#";

        private const char DIR_SEPARATOR = CpkConstants.DirectorySeparatorChar;

        private readonly ICpkFileSystem _fileSystem;
        private readonly ITextureLoaderFactory _textureLoaderFactory;
        private readonly IMaterialFactory _unlitMaterialFactory;
        private readonly IMaterialFactory _litMaterialFactory;
        private readonly GameSettings _gameSettings;

        private readonly GdbFile _gameDatabase;

        private TextureCache _textureCache;

        private readonly Dictionary<string, Sprite> _spriteCache = new ();
        private readonly Dictionary<string, AudioClip> _audioClipCache = new ();
        private readonly Dictionary<int, UnityEngine.Object> _vfxEffectPrefabCache = new ();

        private readonly Dictionary<Type, Dictionary<string, object>> _gameResourceFileCache = new ();

        // No need to deallocate the shadow texture since it is been used almost every where.
        private static readonly Texture2D ShadowTexture = Resources.Load<Texture2D>("Textures/shadow");
        private static readonly Sprite ShadowSprite = Sprite.Create(ShadowTexture,
            new Rect(0, 0, ShadowTexture.width, ShadowTexture.height), new Vector2(0.5f, 0.5f));

        private readonly int _codepage;

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

            _codepage = _gameSettings.Language == Language.SimplifiedChinese ? 936 : 950;
            _gameDatabase = GetGameDatabaseFile(); // Initialize game database file

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private GdbFile GetGameDatabaseFile()
        {
            var gdbFilePath = FileConstants.CombatDataFolderVirtualPath + $"{GameConstants.AppName}_Softstar.gdb";
            return GetGameResourceFile<GdbFile>(gdbFilePath);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);

            _textureCache?.DisposeAll();

            foreach (Sprite sprite in _spriteCache.Values)
            {
                sprite.Destroy();
            }
            _spriteCache.Clear();

            foreach (AudioClip audioClip in _audioClipCache.Values)
            {
                audioClip.Destroy();
            }
            _audioClipCache.Clear();

            _vfxEffectPrefabCache.Clear();

            _unlitMaterialFactory?.DeallocateMaterialPool();
            _litMaterialFactory?.DeallocateMaterialPool();

            _gameResourceFileCache.Clear();
        }

        public void UseTextureCache(TextureCache textureCache)
        {
            _textureCache = textureCache;
        }

        public bool FileExists(string path)
        {
            return _fileSystem.FileExists(path);
        }

        public Sprite GetShadowSprite()
        {
            return ShadowSprite;
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

        public T GetGameResourceFile<T>(string fileVirtualPath, bool useCache = true, byte[] data = null)
        {
            fileVirtualPath = fileVirtualPath.ToLower();

            if (useCache &&
                _gameResourceFileCache.ContainsKey(typeof(T)) &&
                _gameResourceFileCache[typeof(T)].ContainsKey(fileVirtualPath))
            {
                return (T)_gameResourceFileCache[typeof(T)][fileVirtualPath];
            }

            T file = ServiceLocator.Instance.Get<IFileReader<T>>().Read(
                data ?? _fileSystem.ReadAllBytes(fileVirtualPath), _codepage);

            if (useCache)
            {
                // initialize cache for this type of file if not exists
                if (!_gameResourceFileCache.ContainsKey(typeof(T)))
                {
                    _gameResourceFileCache[typeof(T)] = new Dictionary<string, object>();
                }
                _gameResourceFileCache[typeof(T)][fileVirtualPath] = file;
            }

            return file;
        }

        public IDictionary<int, GameItemInfo> GetGameItemInfos()
        {
            return _gameDatabase.GameItemInfos;
        }

        public IDictionary<int, CombatActorInfo> GetCombatActorInfos()
        {
            return _gameDatabase.CombatActorInfos;
        }

        public IDictionary<int, SkillInfo> GetSkillInfos()
        {
            return _gameDatabase.SkillInfos;
        }

        public IDictionary<int, ComboSkillInfo> GetComboSkillInfos()
        {
            return _gameDatabase.ComboSkillInfos;
        }

        /// <summary>
        /// Get music file path in cache folder.
        /// </summary>
        /// <param name="musicFileVirtualPath">music file virtual path</param>
        /// <returns>Music file path in cache folder</returns>
        public string GetMusicFilePathInCacheFolder(string musicFileVirtualPath)
        {
            return Application.persistentDataPath
                   + Path.DirectorySeparatorChar
                   + CACHE_FOLDER_NAME
                   + Path.DirectorySeparatorChar
                   + musicFileVirtualPath
                       .Replace(CpkConstants.DirectorySeparatorChar, Path.DirectorySeparatorChar)
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

            EngineLogger.Log($"Writing MP3 file to App's persistent folder: {musicFileVirtualPath}");
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
            string cacheKey = filePath.ToLower();

            if (_audioClipCache.ContainsKey(cacheKey) &&
                _audioClipCache[cacheKey] != null)
            {
                onLoaded?.Invoke(_audioClipCache[cacheKey]);
                yield break;
            }

            yield return AudioClipLoader.LoadAudioClipAsync(filePath,
                audioType,
                streamAudio,
                audioClip =>
                {
                    _audioClipCache[cacheKey] = audioClip;
                    onLoaded?.Invoke(audioClip);
                });
        }

        public Sprite GetLogoSprite()
        {
            #if PAL3
            const string mainLogoTextureFileName = "11.tga";
            Rect mainLogoTextureRect = new Rect(0, 600, 375, 145);
            #elif PAL3A
            const string mainLogoTextureFileName = "12.tga";
            Rect mainLogoTextureRect = new Rect(0, 770, 500, 253);
            #endif

            ITextureResourceProvider uiLibTextureProvider = CreateTextureResourceProvider(FileConstants.UILibFolderVirtualPath);
            Texture2D logoTexture = uiLibTextureProvider.GetTexture(mainLogoTextureFileName);
            return Sprite.Create(logoTexture, mainLogoTextureRect, new Vector2(0.5f, 0.5f));
        }

        private Texture2D GetActorAvatarTexture(string actorName, string avatarTextureName)
        {
            var roleAvatarTextureRelativePath = FileConstants.GetActorFolderVirtualPath(actorName);
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
            string emojiSpriteSheetRelativePath = FileConstants.EmojiSpriteSheetFolderVirtualPath;
            ITextureResourceProvider textureProvider = CreateTextureResourceProvider(emojiSpriteSheetRelativePath);
            return textureProvider.GetTexture($"EM_{(int)emojiType:00}.tga");
        }

        public Texture2D GetCaptionTexture(string name)
        {
            var captionTextureRelativePath = FileConstants.CaptionFolderVirtualPath;
            // No need to cache caption texture since it is a one time thing
            ITextureResourceProvider textureProvider = CreateTextureResourceProvider(captionTextureRelativePath, useCache: false);
            return textureProvider.GetTexture($"{name}.tga");
        }

        public Texture2D[] GetSkyBoxTextures(int skyBoxId)
        {
            var relativeFilePath = string.Format(FileConstants.SkyBoxTexturePathFormat.First(), skyBoxId);

            ITextureResourceProvider textureProvider = CreateTextureResourceProvider(
                CoreUtility.GetDirectoryName(relativeFilePath, DIR_SEPARATOR));

            var textures = new Texture2D[FileConstants.SkyBoxTexturePathFormat.Length];
            for (var i = 0; i < FileConstants.SkyBoxTexturePathFormat.Length; i++)
            {
                var textureNameFormat = CoreUtility.GetFileName(
                    string.Format(FileConstants.SkyBoxTexturePathFormat[i], skyBoxId), DIR_SEPARATOR);
                Texture2D texture = textureProvider.GetTexture(string.Format(textureNameFormat, i));
                // Set wrap mode to clamp to remove "edges" between sides
                texture.wrapMode = TextureWrapMode.Clamp;
                textures[i] = texture;
            }

            return textures;
        }

        public Texture2D GetEffectTexture(string name, out bool hasAlphaChannel)
        {
            var effectFolderRelativePath = FileConstants.EffectFolderVirtualPath;
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
            var relativePath = FileConstants.UISceneFolderVirtualPath;
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

        public ActorActionConfig GetActorActionConfig(string actorName, string configFileName)
        {
            string actorConfigFilePath = FileConstants.GetActorFolderVirtualPath(actorName) + configFileName;

            actorConfigFilePath = actorConfigFilePath.ToLower();

            if (!FileExists(actorConfigFilePath))
            {
                return null;
            }

            var configData = _fileSystem.ReadAllBytes(actorConfigFilePath);
            var configHeaderStr = Encoding.ASCII.GetString(configData[..MV3_ACTOR_CONFIG_HEADER.Length]);

            if (string.Equals(MV3_ACTOR_CONFIG_HEADER, configHeaderStr))
            {
                return GetGameResourceFile<Mv3ActionConfig>(actorConfigFilePath, data: configData);
            }
            else // MOV actor config
            {
                return GetGameResourceFile<MovActionConfig>(actorConfigFilePath, data: configData);
            }
        }

        public string GetVideoFilePath(string videoName)
        {
            var videoFolder = $"{_fileSystem.GetRootPath()}{FileConstants.MovieFolderName}{Path.DirectorySeparatorChar}";

            if (!Directory.Exists(videoFolder))
            {
                throw new DirectoryNotFoundException($"Video directory does not exists: {videoFolder}");
            }

            var supportedVideoFormats = UnitySupportedVideoFormats.GetSupportedVideoFormats(Application.platform);

            foreach (FileInfo file in new DirectoryInfo(videoFolder).GetFiles($"*.*", SearchOption.AllDirectories))
            {
                var fileExtension = Path.GetExtension(file.Name).ToLower();
                if (supportedVideoFormats.Contains(fileExtension) &&
                    Path.GetFileNameWithoutExtension(file.Name).Equals(videoName, StringComparison.OrdinalIgnoreCase))
                {
                    return file.FullName;
                }
            }

            throw new FileNotFoundException($"Cannot find video file: {videoName} under path: {videoFolder}. " +
                                $"Supported video formats are: {string.Join(" ", supportedVideoFormats)}");
        }

        public (Texture2D texture, bool hasAlphaChannel)[] GetEffectTextures(GraphicsEffectType effect, string texturePathFormat)
        {
            ITextureResourceProvider textureProvider = CreateTextureResourceProvider(
                CoreUtility.GetDirectoryName(texturePathFormat, DIR_SEPARATOR));

            if (effect == GraphicsEffectType.Fire)
            {
                var numberOfFrames = EffectConstants.AnimatedFireEffectFrameCount;
                var textures = new (Texture2D texture, bool hasAlphaChannel)[numberOfFrames];
                for (var i = 0; i < numberOfFrames; i++)
                {
                    var textureNameFormat = CoreUtility.GetFileName(texturePathFormat, DIR_SEPARATOR);
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

        public UnityEngine.Object GetVfxEffectPrefab(int effectGroupId)
        {
            if (_vfxEffectPrefabCache.TryGetValue(effectGroupId, out UnityEngine.Object vfxEffectPrefab))
            {
                return vfxEffectPrefab;
            }

            UnityEngine.Object vfxPrefab = Resources.Load(GetVfxPrefabPath(effectGroupId));

            if (vfxPrefab == null)
            {
                EngineLogger.LogWarning($"VFX prefab not found: {effectGroupId}");
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
                EngineLogger.LogWarning($"VFX prefab not found: {effectGroupId}");
            }
            else
            {
                EngineLogger.Log($"VFX prefab preloaded: {effectGroupId}");
            }

            _vfxEffectPrefabCache[effectGroupId] = request.asset;
        }

        public Texture2D GetCursorTexture()
        {
            var cursorSpriteRelativePath = FileConstants.CursorSpriteFolderVirtualPath;
            ITextureResourceProvider textureProvider = CreateTextureResourceProvider(cursorSpriteRelativePath);
            Texture2D cursorTexture = textureProvider.GetTexture($"jt.tga");
            return cursorTexture;
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
                DisposeTemporaryInMemoryGameResources();
                _currentCityName = newCityName;
            }

            // Unloads assets that are not used (textures etc.)
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Dispose temporary in-memory resources that are only used in current scene block.
        /// </summary>
        private void DisposeTemporaryInMemoryGameResources()
        {
            // Clean up texture cache after exiting current scene block
            _textureCache?.DisposeAll();

            // Clean up sprite cache after exiting current scene block
            foreach (Sprite sprite in _spriteCache.Values)
            {
                sprite.Destroy();
            }
            _spriteCache.Clear();

            // Dispose in-memory game resource files
            foreach (var fileCache in _gameResourceFileCache)
            {
                if (fileCache.Key == typeof(Mv3File))
                {
                    // Dispose non-main actor mv3 files
                    // All main actor names start with "1"
                    var mainActorMv3 = $"{FileConstants.GetActorFolderName()}{DIR_SEPARATOR}1".ToLower();
                    var mv3FilesToDispose = fileCache.Value.Keys
                        .Where(mv3FilePath => !mv3FilePath.Contains(mainActorMv3))
                        .ToArray();
                    foreach (var mv3File in mv3FilesToDispose)
                    {
                        fileCache.Value.Remove(mv3File);
                    }
                }
                else if (fileCache.Key == typeof(Mv3ActionConfig) ||
                         fileCache.Key == typeof(MovActionConfig))
                {
                    // These files are used/cached across scenes
                    // Do not dispose them since they will be used in next scene block
                }
                else // Dispose all other files during scene block transition
                {
                    fileCache.Value.Clear();
                }
            }

            // clear all vfx prefabs in cache
            _vfxEffectPrefabCache.Clear();
        }

        public void Execute(SettingChangedNotification command)
        {
            // Need to deallocate and reallocate material pool when switching between lit and unlit materials
            if (command.SettingName == nameof(_gameSettings.IsRealtimeLightingAndShadowsEnabled))
            {
                if (_gameSettings.IsRealtimeLightingAndShadowsEnabled)
                {
                    _unlitMaterialFactory?.DeallocateMaterialPool();
                    _litMaterialFactory?.AllocateMaterialPool();
                }
                else
                {
                    _litMaterialFactory?.DeallocateMaterialPool();
                    _unlitMaterialFactory?.AllocateMaterialPool();
                }
            }
        }
    }
}