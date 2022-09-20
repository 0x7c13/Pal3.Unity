// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Data
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Command;
    using Command.InternalCommands;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Ini;
    using Core.DataReader.Mv3;
    using Core.DataReader.Nav;
    using Core.DataReader.Pol;
    using Core.DataReader.Sce;
    using Core.DataReader.Scn;
    using Core.FileSystem;
    using Core.Utils;
    using MetaData;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// Single resource provider for accessing game data.
    /// Also manages the lifecycle of the resource it provides.
    /// </summary>
    public class GameResourceProvider : ICommandExecutor<ScenePreLoadingNotification>
    {
        private const string CACHE_FOLDER_NAME = "CacheData";
        private const string MV3_ACTOR_CONFIG_HEADER = ";#MV3#";

        private readonly ICpkFileSystem _fileSystem;
        private readonly ITextureLoaderFactory _textureLoaderFactory;
        private TextureCache _textureCache;
        private readonly Dictionary<string, Sprite> _spriteCache = new ();
        private readonly Dictionary<string, (PolFile PolFile, ITextureResourceProvider TextureProvider)> _polCache = new ();
        private readonly Dictionary<string, (CvdFile PolFile, ITextureResourceProvider TextureProvider)> _cvdCache = new ();
        private readonly Dictionary<string, (Mv3File mv3File, ITextureResourceProvider textureProvider)> _mv3Cache = new ();
        private readonly Dictionary<string, ActorConfigFile> _actorConfigCache = new (); // Cache forever

        // No need to deallocate the shadow texture since it is been used almost every where.
        private static readonly Texture2D ShadowTexture = Resources.Load<Texture2D>("Textures/shadow");

        public GameResourceProvider(ICpkFileSystem fileSystem, ITextureLoaderFactory textureLoaderFactory)
        {
            _fileSystem = fileSystem;
            _textureLoaderFactory = textureLoaderFactory;
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            _textureCache.DisposeAll();
            _spriteCache.Clear();
            _polCache.Clear();
            _mv3Cache.Clear();
            _actorConfigCache.Clear();
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

        private ITextureResourceProvider GetTextureResourceProvider(string relativePath, bool useCache = true)
        {
            return (_textureCache != null && useCache) ?
                new TextureProvider(_fileSystem, _textureLoaderFactory, relativePath, _textureCache) :
                new TextureProvider(_fileSystem, _textureLoaderFactory, relativePath);
        }

        public (PolFile PolFile, ITextureResourceProvider TextureProvider) GetPol(string polFilePath)
        {
            polFilePath = polFilePath.ToLower();
            if (_polCache.ContainsKey(polFilePath)) return _polCache[polFilePath];
            var polData = _fileSystem.ReadAllBytes(polFilePath);
            var polFile = PolFileReader.Read(polData);
            var relativePath = Utility.GetDirectoryName(polFilePath, CpkConstants.CpkDirectorySeparatorChar);
            var textureProvider = GetTextureResourceProvider(relativePath);
            _polCache[polFilePath] = (polFile, textureProvider);
            return (polFile, textureProvider);
        }

        public (CvdFile CvdFile, ITextureResourceProvider TextureProvider) GetCvd(string cvdFilePath)
        {
            cvdFilePath = cvdFilePath.ToLower();
            if (_cvdCache.ContainsKey(cvdFilePath)) return _cvdCache[cvdFilePath];
            var cvdData =_fileSystem.ReadAllBytes(cvdFilePath);
            var cvdFile = CvdFileReader.Read(cvdData);
            var relativePath = Utility.GetDirectoryName(cvdFilePath, CpkConstants.CpkDirectorySeparatorChar);
            var textureProvider = GetTextureResourceProvider(relativePath);
            _cvdCache[cvdFilePath] = (cvdFile, textureProvider);
            return (cvdFile, textureProvider);
        }

        public (Mv3File Mv3File, ITextureResourceProvider TextureProvider) GetMv3(string mv3FilePath)
        {
            mv3FilePath = mv3FilePath.ToLower();
            if (_mv3Cache.ContainsKey(mv3FilePath)) return _mv3Cache[mv3FilePath];
            var mv3Data = _fileSystem.ReadAllBytes(mv3FilePath);
            var mv3File = Mv3FileReader.Read(mv3Data);
            var relativePath = Utility.GetDirectoryName(mv3FilePath, CpkConstants.CpkDirectorySeparatorChar);
            var textureProvider = GetTextureResourceProvider(relativePath);
            _mv3Cache[mv3FilePath] = (mv3File, textureProvider);
            return (mv3File, textureProvider);
        }

        public NavFile GetNav(string sceneFileName, string sceneName)
        {
            var separator = CpkConstants.CpkDirectorySeparatorChar;
            #if PAL3
            var navFilePath = $"{sceneFileName}.cpk{separator}" +
                              $"{sceneName}{separator}{sceneName}.nav";
            #elif PAL3A
            var navFilePath = $"{FileConstants.ScnCpkPathInfo.cpkName}{separator}SCN{separator}" +
                              $"{sceneFileName}{separator}{sceneName}{separator}{sceneName}.nav";
            #endif

            using var navFileStream = new MemoryStream(_fileSystem.ReadAllBytes(navFilePath));
            return NavFileReader.Read(navFileStream);
        }

        public ScnFile GetScn(string sceneFileName, string sceneName)
        {
            var separator = CpkConstants.CpkDirectorySeparatorChar;
            #if PAL3
            var scnFilePath = $"{sceneFileName}.cpk{separator}{sceneName}.scn";
            #elif PAL3A
            var scnFilePath = $"{FileConstants.ScnCpkPathInfo.cpkName}{separator}SCN{separator}" +
                              $"{sceneFileName}{separator}{sceneFileName}_{sceneName}.scn";
            #endif
            using var scnFileStream = new MemoryStream(_fileSystem.ReadAllBytes(scnFilePath));
            return ScnFileReader.Read(scnFileStream);
        }

        public SceFile GetSceneSce(string sceneFileName)
        {
            var separator = CpkConstants.CpkDirectorySeparatorChar;
            #if PAL3
            var sceFilePath = $"{sceneFileName}.cpk{separator}{sceneFileName}.sce";
            #elif PAL3A
            var sceFilePath = $"{FileConstants.SceCpkPathInfo.cpkName}{separator}Sce{separator}{sceneFileName}.sce";
            #endif
            using var sceFileStream = new MemoryStream(_fileSystem.ReadAllBytes(sceFilePath));
            return SceFileReader.Read(sceFileStream);
        }

        public SceFile GetSystemSce()
        {
            using var sceFileStream = new MemoryStream(
                _fileSystem.ReadAllBytes(ScriptConstants.SystemSceFileVirtualPath));
            return SceFileReader.Read(sceFileStream);
        }

        public SceFile GetBigMapSce()
        {
            using var sceFileStream = new MemoryStream(
                _fileSystem.ReadAllBytes(ScriptConstants.BigMapSceFileVirtualPath));
            return SceFileReader.Read(sceFileStream);
        }

        /// <summary>
        /// Get music file path in cache folder.
        /// </summary>
        /// <param name="musicFileVirtualPath">music file virtual path</param>
        /// <returns>Mp3 file path in cache folder</returns>
        public string GetMp3FilePathInCacheFolder(string musicFileVirtualPath)
        {
            return Application.persistentDataPath + Path.DirectorySeparatorChar + CACHE_FOLDER_NAME
                            +  Path.DirectorySeparatorChar + musicFileVirtualPath.Replace(
                                    CpkConstants.CpkDirectorySeparatorChar, Path.DirectorySeparatorChar)
                                .Replace(".cpk", string.Empty);
        }

        /// <summary>
        /// Since unity cannot directly load audio clip (mp3 in this case) from memory,
        /// we need to first extract the audio clip from cpk archive, write it to a cached folder
        /// and then use this cached mp3 file path for Unity to consume (using UnityWebRequest).
        /// </summary>
        /// <param name="musicFileVirtualPath">music file virtual path</param>
        /// <param name="musicFileCachePath">music file path in cache folder</param>
        public IEnumerator ExtractAndMoveMp3FileToCacheFolder(string musicFileVirtualPath, string musicFileCachePath)
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
            sfxName = sfxName.ToLower();
            #elif PAL3A
            sfxName = sfxName.ToUpper();
            #endif
            var sfxFileRelativePath = $"{FileConstants.SfxFolderName}{Path.DirectorySeparatorChar}" +
                                      $"{sfxName}.wav";

            var rootPath = _fileSystem.GetRootPath();
            var sfxFilePath = $"{rootPath}{sfxFileRelativePath}";

            return sfxFilePath;
        }

        private Texture2D GetActorAvatarTexture(string actorName, string avatarTextureName)
        {
            var separator = CpkConstants.CpkDirectorySeparatorChar;

            var roleAvatarTextureRelativePath =
                $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}" +
                $"{FileConstants.ActorFolderName}{separator}{actorName}{separator}";

            var textureProvider = GetTextureResourceProvider(roleAvatarTextureRelativePath);
            return textureProvider.GetTexture($"{avatarTextureName}.tga");
        }

        public Sprite GetActorAvatarSprite(string actorName, string avatarName)
        {
            var cacheKey = "ActorAvatar" + actorName + avatarName;

            if (_spriteCache.ContainsKey(cacheKey))
            {
                var sprite = _spriteCache[cacheKey];
                if (sprite.texture != null) return sprite;
            }

            var texture = GetActorAvatarTexture(
                actorName, avatarName);

            var avatarSprite = Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0f));

            _spriteCache[cacheKey] = avatarSprite;
            return avatarSprite;
        }

        private Texture2D GetEmojiSpriteSheetTexture(ActorEmojiType emojiType)
        {
            var separator = CpkConstants.CpkDirectorySeparatorChar;

            var emojiSpriteSheetRelativePath =
                $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}" +
                $"{FileConstants.UIFolderName}{separator}{FileConstants.EmojiFolderName}{separator}";

            var textureProvider = GetTextureResourceProvider(emojiSpriteSheetRelativePath);
            return textureProvider.GetTexture($"EM_{(int)emojiType:00}.tga");
        }

        public Texture2D GetCaptionTexture(string name)
        {
            var separator = CpkConstants.CpkDirectorySeparatorChar;

            var captionTextureRelativePath =
                $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}" +
                $"{FileConstants.CaptionFolderName}{separator}";

            // No need to cache caption texture since it is a one time thing
            var textureProvider = GetTextureResourceProvider(captionTextureRelativePath, useCache: false);
            return textureProvider.GetTexture($"{name}.tga");
        }

        public Texture2D[] GetSkyBoxTextures(int skyBoxId)
        {
            var separator = CpkConstants.CpkDirectorySeparatorChar;

            var relativePath = string.Format(SceneConstants.SkyBoxTexturePathFormat.First(), skyBoxId);

            var textureProvider = GetTextureResourceProvider(
                Utility.GetDirectoryName(relativePath, separator));

            var textures = new Texture2D[SceneConstants.SkyBoxTexturePathFormat.Length];
            for (var i = 0; i < SceneConstants.SkyBoxTexturePathFormat.Length; i++)
            {
                var textureNameFormat = Utility.GetFileName(
                    string.Format(SceneConstants.SkyBoxTexturePathFormat[i], skyBoxId), separator);
                var texture = textureProvider.GetTexture(string.Format(textureNameFormat, i));
                // Set wrap mode to clamp to remove "edges" between sides
                texture.wrapMode = TextureWrapMode.Clamp;
                textures[i] = texture;
            }

            return textures;
        }

        public Texture2D GetEffectTexture(string name)
        {
            var separator = CpkConstants.CpkDirectorySeparatorChar;

            var effectFolderRelativePath =
                $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}" +
                $"{FileConstants.EffectFolderName}{separator}";

            var textureProvider = GetTextureResourceProvider(effectFolderRelativePath);
            var effectTexture = textureProvider.GetTexture(name);
            Utility.ApplyTransparencyBasedOnColorLuminance(effectTexture);
            return effectTexture;
        }

        public Sprite[] GetEmojiSprites(ActorEmojiType emojiType)
        {
            var textureInfo = ActorEmojiConstants.TextureInfo[emojiType];
            var spriteSheet = GetEmojiSpriteSheetTexture(emojiType);

            var widthIndex = 0f;
            var sprites = new Sprite[textureInfo.Frames];

            for (var i = 0; i < textureInfo.Frames; i++)
            {
                var cacheKey = "EmojiSprite" + emojiType + i;

                if (_spriteCache.ContainsKey(cacheKey))
                {
                    var sprite = _spriteCache[cacheKey];
                    if (sprite.texture != null)
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

        public ActorConfigFile GetActorConfig(string actorConfigFilePath)
        {
            actorConfigFilePath = actorConfigFilePath.ToLower();

            if (_actorConfigCache.ContainsKey(actorConfigFilePath)) return _actorConfigCache[actorConfigFilePath];

            if (!_fileSystem.FileExists(actorConfigFilePath))
            {
                _actorConfigCache[actorConfigFilePath] = null;
                return null;
            }

            var configData = _fileSystem.ReadAllBytes(actorConfigFilePath);
            var configHeaderStr = Encoding.ASCII.GetString(configData[..MV3_ACTOR_CONFIG_HEADER.Length]);

            if (string.Equals(MV3_ACTOR_CONFIG_HEADER, configHeaderStr))
            {
                var config = ActorConfigFileReader.Read(configData);
                _actorConfigCache[actorConfigFilePath] = config;
                return config;
            }
            else
            {
                _actorConfigCache[actorConfigFilePath] = null;
                return null;
            }
        }

        public string GetVideoFilePath(string videoName)
        {
            var rootPath = _fileSystem.GetRootPath();
            var separator = Path.DirectorySeparatorChar;

            var videoFolder = $"{rootPath}{FileConstants.MovieFolderName}{separator}";

            if (!Directory.Exists(videoFolder))
            {
                throw new Exception($"Video directory does not exists: {videoFolder}.");
            }

            var supportedVideoFormats = UnitySupportedVideoFormats.GetSupportedVideoFormats(Application.platform);

            foreach (var file in new DirectoryInfo(videoFolder).GetFiles($"*.*", SearchOption.AllDirectories))
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

        public Texture2D[] GetEffectTextures(GraphicsEffect effect, string texturePathFormat)
        {
            var separator = CpkConstants.CpkDirectorySeparatorChar;
            var textureProvider = GetTextureResourceProvider(
                Utility.GetDirectoryName(texturePathFormat, separator));

            if (effect == GraphicsEffect.Fire)
            {
                var numberOfFrames = EffectConstants.EffectAnimationInfo[effect].NumberOfFrames;
                var textures = new Texture2D[numberOfFrames];
                for (var i = 0; i < numberOfFrames; i++)
                {
                    var textureNameFormat = Utility.GetFileName(texturePathFormat, separator);
                    var texture = textureProvider.GetTexture(string.Format(textureNameFormat, i + 1));
                    textures[i] = texture;
                }

                return textures;
            }

            return Array.Empty<Texture2D>();
        }

        private string _currentSceneCityName;
        public void Execute(ScenePreLoadingNotification notification)
        {
            var newSceneCityName = notification.NewSceneInfo.CityName.ToLower();

            if (string.IsNullOrEmpty(_currentSceneCityName))
            {
                _currentSceneCityName = newSceneCityName;
                return;
            }

            if (!newSceneCityName.Equals(_currentSceneCityName, StringComparison.OrdinalIgnoreCase))
            {
                // Clean up cache after exiting current scene block
                _textureCache.DisposeAll();
                _spriteCache.Clear();

                // TODO: Have a better way to manage the lifecycle of pol, mv3, cvd data.
                // _polCache.Clear();
                // _mv3Cache.Clear();
                // _cvdCache.Clear();

                // Unloads assets that are not used (textures etc.)
                Resources.UnloadUnusedAssets();

                _currentSceneCityName = newSceneCityName;
            }
        }
    }
}