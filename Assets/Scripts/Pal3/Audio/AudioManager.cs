// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Audio
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Data;
    using MetaData;
    using Scene;
    using State;
    using UnityEngine;

    public sealed class AudioManager : MonoBehaviour,
        ICommandExecutor<PlaySfxCommand>,
        ICommandExecutor<PlayMusicCommand>,
        ICommandExecutor<AttachSfxToGameObjectRequest>,
        ICommandExecutor<StopSfxPlayingAtGameObjectRequest>,
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<StopMusicCommand>,
        ICommandExecutor<ScenePreLoadingNotification>,
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        public float DefaultMusicVolume { get; set; } = 0.45f;
        public float DefaultSoundVolume { get; set; } = 0.65f;

        private Camera _mainCamera;
        private AudioSource _musicPlayer;
        private GameResourceProvider _resourceProvider;
        private SceneManager _sceneManager;

        private const string STOP_MUSIC_NAME = "NONE";
        private string _currentMusicClipName = string.Empty;
        private string _currentScriptMusic = string.Empty;
        private readonly List<string> _playingSfxSourceNames = new ();

        // TODO: Fix and remove this hack
        private const string PLAYER_ACTOR_MOVEMENT_SFX_AUDIO_SOURCE_NAME = "PlayerActorMovementSfx";
        private bool _playerMovementSfxInProgress;
        
        private CancellationTokenSource _sceneAudioCts = new ();

        public void Init(Camera mainCamera,
            GameResourceProvider resourceProvider,
            SceneManager sceneManager,
            AudioSource musicSource)
        {
            _mainCamera = mainCamera != null ? mainCamera : throw new ArgumentNullException(nameof(mainCamera));
            _resourceProvider = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
            _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
            _musicPlayer = musicSource != null ? musicSource : throw new ArgumentNullException(nameof(musicSource));
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        private IEnumerator PlaySceneMusic(string cityName, string sceneName)
        {
            var key = $"{cityName}_{sceneName}";
            var musicName = string.Empty;
            if (MusicConstants.SceneMusicInfo.ContainsKey(key.ToLower()))
            {
                musicName = MusicConstants.SceneMusicInfo[key.ToLower()];
            }
            else if (MusicConstants.SceneMusicInfo.ContainsKey(cityName.ToLower()))
            {
                musicName = MusicConstants.SceneMusicInfo[cityName.ToLower()];
            }

            if (string.IsNullOrEmpty(musicName) || musicName.Equals(STOP_MUSIC_NAME))
            {
                _currentMusicClipName = string.Empty;
                _musicPlayer.Stop();
                yield break;
            }

            if (_musicPlayer.isPlaying &&
                string.Equals(_currentMusicClipName, musicName, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            _currentMusicClipName = musicName;
            var musicFileVirtualPath = GetMusicFileVirtualPath(musicName);
            var musicFileCachePath = _resourceProvider.GetMp3FilePathInCacheFolder(musicFileVirtualPath);
            
            yield return PlayMusic(musicName, musicFileVirtualPath, musicFileCachePath, -1);
        }

        private IEnumerator PlayAudioClip(AudioSource audioSource,
            AudioClip audioClip,
            int loopCount,
            float interval,
            float volume,
            CancellationToken cancellationToken)
        {
            if (audioSource == null || !audioSource.enabled) yield break;

            if (loopCount == -1 && interval == 0f)
            {
                if (audioSource.clip != null)
                {
                    audioSource.Stop();
                    Destroy(audioSource.clip);
                }

                audioSource.clip = audioClip;
                audioSource.volume = volume;
                audioSource.loop = true;
                audioSource.Play();
            }
            else if (loopCount == -1 && interval > 0f)
            {
                while (!cancellationToken.IsCancellationRequested && audioSource != null)
                {
                    audioSource.PlayOneShot(audioClip, volume);
                    yield return new WaitForSeconds(audioClip.length + interval);
                }
            }
            else
            {
                while (--loopCount >= 0 && !cancellationToken.IsCancellationRequested && audioSource != null)
                {
                    audioSource.PlayOneShot(audioClip, volume);
                    yield return new WaitForSeconds(audioClip.length);
                }
            }
        }
        
        // Spatial Audio
        private IEnumerator AttachSfxToGameObjectAndPlaySfx(GameObject parent,
            string sfxFilePath,
            string audioSourceName,
            int loopCount,
            float volume,
            float interval = 0f,
            CancellationToken cancellationToken = default)
        {
            if (parent == null || cancellationToken.IsCancellationRequested) yield break;

            AudioClip sfxAudioClip = null;

            yield return _resourceProvider.LoadAudioClip(sfxFilePath, AudioType.WAV, streamAudio: false,
                audioClip => { sfxAudioClip = audioClip; });
            
            if (parent == null ||
                cancellationToken.IsCancellationRequested ||
                sfxAudioClip == null) yield break;

            if (parent != _mainCamera.gameObject &&
                audioSourceName == PLAYER_ACTOR_MOVEMENT_SFX_AUDIO_SOURCE_NAME &&
                _playerMovementSfxInProgress == false)
            {
                yield break; // Means movement sfx is already stopped before it started due to the delay of the coroutine
            }

            if (parent == _mainCamera.gameObject &&
                !_playingSfxSourceNames.Contains(audioSourceName))
            {
                yield break; // Means sfx is already stopped before it started due to the delay of the coroutine
            }

            GameObject audioSourceParent;
            Transform audioSourceParentTransform = parent.transform.Find(audioSourceName);
            if (audioSourceParentTransform != null)
            {
                audioSourceParent = audioSourceParentTransform.gameObject;
            }
            else
            {
                audioSourceParent = new GameObject(audioSourceName);
                audioSourceParent.transform.parent = parent.transform;
                audioSourceParent.transform.position = parent.transform.position;
            }

            var audioSource = audioSourceParent.GetOrAddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = 75f;

            yield return PlayAudioClip(audioSource, sfxAudioClip, loopCount, interval, volume, cancellationToken);
        }
        
        private IEnumerator StartWithDelay(
            float delayInSeconds,
            IEnumerator coroutine,
            CancellationToken cancellationToken)
        {
            if (delayInSeconds > 0)
            {
                yield return new WaitForSeconds(delayInSeconds);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                yield return coroutine;
            }
        }

        private string GetMusicFileVirtualPath(string musicName)
        {
            var separator = CpkConstants.DirectorySeparator;
            return $"{FileConstants.MusicCpkPathInfo.cpkName}{separator}" +
                   $"{FileConstants.MusicCpkPathInfo.relativePath}{separator}{musicName}.mp3";
        }

        public IEnumerator PlayMusic(string musicName,
            string musicFileVirtualPath,
            string musicFileCachePath,
            int loopCount)
        {
            yield return _resourceProvider.ExtractAndMoveMp3FileToCacheFolder(musicFileVirtualPath, musicFileCachePath);
            
            AudioClip musicClip = null;
            yield return _resourceProvider.LoadAudioClip(musicFileCachePath, AudioType.MPEG, streamAudio: true,
                audioClip => { musicClip = audioClip; });
            
            // We need to check if current music clip is the same as the one we are trying to play,
            // since we are using coroutine to load and play music, there is a chance that the music
            // clip we are trying to play is not the same as the one we supposed to play.
            if (musicClip == null ||
                !string.Equals(_currentMusicClipName, musicName, StringComparison.OrdinalIgnoreCase)) yield break;
            
            yield return PlayAudioClip(_musicPlayer, musicClip, loopCount, 0f, DefaultMusicVolume,
                new CancellationToken(false)); // Should not stop music during scene switch
        }

        public string GetCurrentScriptMusic()
        {
            return _currentScriptMusic;
        }

        private void DestroyAllSfxAudioSource()
        {
            // Destroy current playing sfx
            foreach (Transform audioSourceParentTransform in _playingSfxSourceNames
                         .Select(sfxName => _mainCamera.transform.Find(sfxName))
                         .Where(audioSourceParentTransform => audioSourceParentTransform != null))
            {
                if (audioSourceParentTransform.GetComponent<AudioSource>() is { } audioSource)
                {
                    audioSource.Stop();
                    
                    if (audioSource.clip != null)
                    {
                        Destroy(audioSource.clip);
                    }
                }
                
                Destroy(audioSourceParentTransform.gameObject);
            }
            
            _playingSfxSourceNames.Clear();
        }

        private void ChangeAllSfxAudioSourceMuteSetting(bool mute)
        {
            foreach (Transform audioSourceParentTransform in _playingSfxSourceNames
                         .Select(sfxName => _mainCamera.transform.Find(sfxName))
                         .Where(audioSourceParentTransform => audioSourceParentTransform != null))
            {
                if (audioSourceParentTransform.GetComponent<AudioSource>() is { } audioSource)
                {
                    audioSource.mute = mute;
                }
            }
        }

        public void Execute(PlaySfxCommand command)
        {
            #if PAL3
            var sfxName = command.SfxName.ToLower();
            #elif PAL3A
            var sfxName = command.SfxName.ToUpper();
            #endif
            
            var loopCount = command.LoopCount;

            #if PAL3
            // Fix actor movement sfx name
            if (sfxName.Equals("we021", StringComparison.OrdinalIgnoreCase))
            {
                sfxName = "we021a";
            }
            if (sfxName.Equals("we022", StringComparison.OrdinalIgnoreCase))
            {
                sfxName = "we022a";
            }
            #endif

            switch (loopCount)
            {
                case 0: // start playing sfx indefinitely for the given sfxName
                    _playingSfxSourceNames.Add(sfxName);
                    StartCoroutine(AttachSfxToGameObjectAndPlaySfx(_mainCamera.gameObject,
                        _resourceProvider.GetSfxFilePath(sfxName),
                        sfxName, // use sfx name as audio source name
                        loopCount: -1, // loop indefinitely
                        DefaultSoundVolume,
                        interval: 0f,
                        _sceneAudioCts.Token));
                    break;
                case -1: // stop playing sfx by sfxName
                    _playingSfxSourceNames.Remove(sfxName);
                    Execute(new StopSfxPlayingAtGameObjectRequest(
                        _mainCamera.gameObject,
                        sfxName,
                        disposeSource: true));
                    break;
                default:
                {
                    _playingSfxSourceNames.Add(sfxName);
                    var sfxFilePath = _resourceProvider.GetSfxFilePath(sfxName);
                    CancellationToken cancellationToken = _sceneAudioCts.Token;
                    StartCoroutine(AttachSfxToGameObjectAndPlaySfx(_mainCamera.gameObject,
                        sfxFilePath,
                        sfxName, // use sfx name as audio source name
                        loopCount,
                        DefaultSoundVolume,
                        interval: 0f,
                        cancellationToken));
                    break;
                }
            }
        }

        public void Execute(AttachSfxToGameObjectRequest request)
        {
            if (request.AudioSourceName == PLAYER_ACTOR_MOVEMENT_SFX_AUDIO_SOURCE_NAME)
            {
                _playerMovementSfxInProgress = true;
            }
            
            var sfxFilePath = _resourceProvider.GetSfxFilePath(request.SfxName);
            CancellationToken cancellationToken = _sceneAudioCts.Token;
            StartCoroutine(StartWithDelay(request.StartDelayInSeconds, 
                AttachSfxToGameObjectAndPlaySfx(request.Parent,
                    sfxFilePath,
                    request.AudioSourceName,
                    request.LoopCount,
                    request.Volume,
                    request.Interval,
                    cancellationToken),
                cancellationToken));
        }
        
        public void Execute(StopSfxPlayingAtGameObjectRequest command)
        {
            if (command.AudioSourceName == PLAYER_ACTOR_MOVEMENT_SFX_AUDIO_SOURCE_NAME)
            {
                _playerMovementSfxInProgress = false;
            }            
            
            Transform audioSourceParentTransform = command.Parent.transform.Find(command.AudioSourceName);
            if (audioSourceParentTransform == null) return;

            GameObject audioSourceParent = audioSourceParentTransform.gameObject;
            var audioSource = audioSourceParent.GetComponentInChildren<AudioSource>();
            if (audioSource == default) return;
            
            audioSource.Stop();
            audioSource.clip = null;

            if (command.DisposeSource)
            {
                Destroy(audioSourceParent);
            }
        }

        public void Execute(PlayMusicCommand command)
        {
            if (command.MusicName.Equals(STOP_MUSIC_NAME))
            {
                _currentScriptMusic = string.Empty;
                _musicPlayer.Stop();
                return;
            }

            // TODO: Disable combat music since combat system has not been implemented yet.
            if (MusicConstants.CombatMusicInfo.Any(musicInfo =>
                    command.MusicName.Equals(musicInfo.Value, StringComparison.OrdinalIgnoreCase)) ||
                command.MusicName.Equals("PI25A", StringComparison.OrdinalIgnoreCase) ||
                command.MusicName.Equals("PI25", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _currentScriptMusic = command.MusicName;
            _currentMusicClipName = command.MusicName;
            var musicFileVirtualPath = GetMusicFileVirtualPath(command.MusicName);
            var musicFileCachePath = _resourceProvider.GetMp3FilePathInCacheFolder(musicFileVirtualPath);
            StartCoroutine(PlayMusic(command.MusicName,
                musicFileVirtualPath,
                musicFileCachePath,
                command.Loop == 0 ? -1 : command.Loop));
        }

        public void Execute(StopMusicCommand command)
        {
            if (_musicPlayer.isPlaying)
            {
                _musicPlayer.Stop();
            }

            _currentScriptMusic = string.Empty;
            _currentMusicClipName = string.Empty;

            ScnSceneInfo sceneInfo = _sceneManager.GetCurrentScene().GetSceneInfo();
            StartCoroutine(PlaySceneMusic(sceneInfo.CityName, sceneInfo.SceneName));
        }

        public void Execute(ScenePreLoadingNotification command)
        {
            DestroyAllSfxAudioSource();
            _sceneAudioCts.Cancel();
            _sceneAudioCts = new CancellationTokenSource();
        }

        public void Execute(ScenePostLoadingNotification command)
        {
            if (string.IsNullOrEmpty(_currentScriptMusic))
            {
                StartCoroutine(PlaySceneMusic(command.NewSceneInfo.CityName, command.NewSceneInfo.SceneName));
            }
        }

        public void Execute(ResetGameStateCommand command)
        {
            DestroyAllSfxAudioSource();
            _currentScriptMusic = string.Empty;
            _musicPlayer.Stop();
        }

        public void Execute(GameStateChangedNotification command)
        {
            if (command.NewState == GameState.VideoPlaying)
            {
                if (_musicPlayer.isPlaying)
                {
                    _musicPlayer.Stop();
                }

                _musicPlayer.mute = true;
                ChangeAllSfxAudioSourceMuteSetting(true);
            }
            else if (command.PreviousState == GameState.VideoPlaying)
            {
                _musicPlayer.mute = false;
                ChangeAllSfxAudioSourceMuteSetting(false);
            }
        }
    }
}