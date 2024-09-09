// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Audio
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.DataReader.Scn;
    using Core.Utilities;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Coroutine;
    using Scene;
    using Settings;
    using State;
    using UnityEngine;

    public sealed class AudioManager : IDisposable,
        ICommandExecutor<PlaySfxCommand>,
        ICommandExecutor<PlayScriptMusicCommand>,
        ICommandExecutor<StopScriptMusicCommand>,
        ICommandExecutor<AttachSfxToGameEntityRequest>,
        ICommandExecutor<StopSfxPlayingAtGameEntityRequest>,
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<ScenePreLoadingNotification>,
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<ResetGameStateCommand>,
        ICommandExecutor<SettingChangedNotification>
    {
        private readonly IGameEntity _cameraEntity;

        private readonly AudioSource _musicPlayer;
        private readonly GameResourceProvider _resourceProvider;
        private readonly SceneManager _sceneManager;
        private readonly GameSettings _gameSettings;

        private float _musicVolume;
        private float _sfxVolume;

        private string _currentMusicClipName = string.Empty;
        private string _currentScriptMusic = string.Empty;
        private readonly List<string> _playingSfxSourceNames = new ();

        // TODO: Maybe it is better to control movement sfx
        // by the player gameplay controller instead of AudioManager?
        private bool _playerMovementSfxInProgress;

        private CancellationTokenSource _sceneAudioCts = new ();

        public AudioManager(IGameEntity cameraEntity,
            GameResourceProvider resourceProvider,
            SceneManager sceneManager,
            AudioSource musicSource,
            GameSettings gameSettings)
        {
            _cameraEntity = Requires.IsNotNull(cameraEntity, nameof(cameraEntity));;
            _resourceProvider = Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _sceneManager = Requires.IsNotNull(sceneManager, nameof(sceneManager));
            _musicPlayer = Requires.IsNotNull(musicSource, nameof(musicSource));
            _gameSettings = Requires.IsNotNull(gameSettings, nameof(gameSettings));

            _musicVolume = _gameSettings.MusicVolume;
            _sfxVolume = _gameSettings.SfxVolume;

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(SettingChangedNotification command)
        {
            if (command.SettingName == nameof(_gameSettings.MusicVolume))
            {
                _musicVolume = _gameSettings.MusicVolume;
                _musicPlayer.volume = _musicVolume;
            }
            else if (command.SettingName == nameof(_gameSettings.SfxVolume))
            {
                _sfxVolume = _gameSettings.SfxVolume;
            }
        }

        private IEnumerator PlaySceneMusicAsync(string cityName, string sceneName)
        {
            string musicName = string.Empty;

            if (MusicConstants.SceneMusicInfo.TryGetValue($"{cityName}_{sceneName}".ToLower(), out var sceneMusic))
            {
                musicName = sceneMusic;
            }
            else if (MusicConstants.SceneMusicInfo.TryGetValue(cityName.ToLower(), out var sceneCityMusic))
            {
                musicName = sceneCityMusic;
            }

            if (string.IsNullOrEmpty(musicName) || musicName.Equals(AudioConstants.StopMusicName))
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
            string musicFileVirtualPath = FileConstants.GetMusicFileVirtualPath(musicName);
            string musicFileCachePath = _resourceProvider.GetMusicFilePathInCacheFolder(musicName);

            yield return PlayMusicAsync(musicName, musicFileVirtualPath, musicFileCachePath, -1);
        }

        private IEnumerator PlayAudioClipAsync(AudioSource audioSource,
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
                    yield return CoroutineYieldInstruction.WaitForSeconds(audioClip.length + interval);
                }
            }
            else
            {
                while (--loopCount >= 0 && !cancellationToken.IsCancellationRequested && audioSource != null)
                {
                    audioSource.PlayOneShot(audioClip, volume);
                    yield return CoroutineYieldInstruction.WaitForSeconds(audioClip.length);
                }
            }
        }

        // Spatial Audio
        private IEnumerator AttachSfxToGameEntityAndPlaySfxAsync(IGameEntity parent,
            string sfxFilePath,
            string audioSourceName,
            int loopCount,
            float volume,
            float interval = 0f,
            CancellationToken cancellationToken = default)
        {
            if (parent == null || cancellationToken.IsCancellationRequested) yield break;

            AudioClip sfxAudioClip = null;

            yield return _resourceProvider.LoadAudioClipAsync(sfxFilePath, AudioType.WAV, streamAudio: false,
                audioClip => { sfxAudioClip = audioClip; });

            if (parent.IsNativeObjectDisposed ||
                cancellationToken.IsCancellationRequested ||
                sfxAudioClip == null) yield break;

            if (parent != _cameraEntity &&
                audioSourceName == AudioConstants.PlayerActorMovementSfxAudioSourceName &&
                _playerMovementSfxInProgress == false)
            {
                yield break; // Means movement sfx is already stopped before it started due to the delay of the coroutine
            }

            if (parent == _cameraEntity &&
                !_playingSfxSourceNames.Contains(audioSourceName))
            {
                yield break; // Means sfx is already stopped before it started due to the delay of the coroutine
            }

            IGameEntity audioSourceParent = parent.FindChild(audioSourceName);
            if (audioSourceParent == null)
            {
                audioSourceParent = GameEntityFactory.Create(audioSourceName, parent, worldPositionStays: true);
                audioSourceParent.Transform.Position = parent.Transform.Position;
            }

            var audioSource = audioSourceParent.GetOrAddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = 75f;

            yield return PlayAudioClipAsync(audioSource, sfxAudioClip, loopCount, interval, volume, cancellationToken);
        }

        private IEnumerator StartWithDelayAsync(
            float delayInSeconds,
            IEnumerator coroutine,
            CancellationToken cancellationToken)
        {
            if (delayInSeconds > 0)
            {
                yield return CoroutineYieldInstruction.WaitForSeconds(delayInSeconds);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                yield return coroutine;
            }
        }

        public IEnumerator PlayMusicAsync(string musicName,
            string musicFileVirtualPath,
            string musicFileCachePath,
            int loopCount)
        {
            yield return _resourceProvider.ExtractAndMoveMp3FileToCacheFolderAsync(musicFileVirtualPath, musicFileCachePath);

            AudioClip musicClip = null;
            yield return _resourceProvider.LoadAudioClipAsync(musicFileCachePath, AudioType.MPEG, streamAudio: true,
                audioClip => { musicClip = audioClip; });

            // We need to check if current music clip is the same as the one we are trying to play,
            // since we are using coroutine to load and play music, there is a chance that the music
            // clip we are trying to play is not the same as the one we supposed to play.
            if (musicClip == null ||
                !string.Equals(_currentMusicClipName, musicName, StringComparison.OrdinalIgnoreCase)) yield break;

            yield return PlayAudioClipAsync(_musicPlayer, musicClip, loopCount, 0f, _musicVolume,
                new CancellationToken(false)); // Should not stop music during scene switch
        }

        public string GetCurrentScriptMusic()
        {
            return _currentScriptMusic;
        }

        private void DestroyAllSfxAudioSource()
        {
            // Destroy current playing sfx
            foreach (IGameEntity audioSourceParent in _playingSfxSourceNames
                         .Select(sfxName => _cameraEntity.FindChild(sfxName)))
            {
                if (audioSourceParent?.GetComponent<AudioSource>() is { } audioSource)
                {
                    audioSource.Stop();
                }

                audioSourceParent?.Destroy();
            }

            _playingSfxSourceNames.Clear();
        }

        private void ChangeAllSfxAudioSourceMuteSetting(bool mute)
        {
            foreach (IGameEntity audioSourceParent in _playingSfxSourceNames
                         .Select(sfxName => _cameraEntity.FindChild(sfxName)))
            {
                if (audioSourceParent?.GetComponent<AudioSource>() is { } audioSource)
                {
                    audioSource.mute = mute;
                }
            }
        }

        public void Execute(PlaySfxCommand command)
        {
            #if PAL3
            string sfxName = command.SfxName.ToLower();
            #elif PAL3A
            string sfxName = command.SfxName.ToUpper();
            #endif

            int loopCount = command.LoopCount;

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
                    Pal3.Instance.StartCoroutine(AttachSfxToGameEntityAndPlaySfxAsync(
                        _cameraEntity,
                        _resourceProvider.GetSfxFilePath(sfxName),
                        sfxName, // use sfx name as audio source name
                        loopCount: -1, // loop indefinitely
                        _sfxVolume,
                        interval: 0f,
                        _sceneAudioCts.Token));
                    break;
                case -1: // stop playing sfx by sfxName
                    _playingSfxSourceNames.Remove(sfxName);
                    Execute(new StopSfxPlayingAtGameEntityRequest(
                        _cameraEntity,
                        sfxName,
                        disposeSource: true));
                    break;
                default:
                {
                    _playingSfxSourceNames.Add(sfxName);
                    string sfxFilePath = _resourceProvider.GetSfxFilePath(sfxName);
                    CancellationToken cancellationToken = _sceneAudioCts.Token;
                    Pal3.Instance.StartCoroutine(AttachSfxToGameEntityAndPlaySfxAsync(
                        _cameraEntity,
                        sfxFilePath,
                        sfxName, // use sfx name as audio source name
                        loopCount,
                        _sfxVolume,
                        interval: 0f,
                        cancellationToken));
                    break;
                }
            }
        }

        public void Execute(AttachSfxToGameEntityRequest request)
        {
            if (request.AudioSourceName == AudioConstants.PlayerActorMovementSfxAudioSourceName)
            {
                _playerMovementSfxInProgress = true;
            }

            string sfxFilePath = _resourceProvider.GetSfxFilePath(request.SfxName);
            CancellationToken cancellationToken = _sceneAudioCts.Token;
            Pal3.Instance.StartCoroutine(StartWithDelayAsync(request.StartDelayInSeconds,
                AttachSfxToGameEntityAndPlaySfxAsync(request.Parent,
                    sfxFilePath,
                    request.AudioSourceName,
                    request.LoopCount,
                    request.Volume,
                    request.Interval,
                    cancellationToken),
                cancellationToken));
        }

        public void Execute(StopSfxPlayingAtGameEntityRequest command)
        {
            if (command.AudioSourceName == AudioConstants.PlayerActorMovementSfxAudioSourceName)
            {
                _playerMovementSfxInProgress = false;
            }

            if (command.Parent == null || command.Parent.IsNativeObjectDisposed) return;

            IGameEntity audioSourceParent = command.Parent.FindChild(command.AudioSourceName);
            if (audioSourceParent == null) return;

            AudioSource audioSource = audioSourceParent.GetComponentInChildren<AudioSource>();
            if (audioSource == default) return;

            audioSource.Stop();
            audioSource.clip = null;

            if (command.DisposeSource)
            {
                audioSourceParent.Destroy();
            }
        }

        public void Execute(PlayScriptMusicCommand command)
        {
            if (command.MusicName.Equals(AudioConstants.StopMusicName))
            {
                _currentScriptMusic = string.Empty;
                _musicPlayer.Stop();
                return;
            }

            // Ignore combat music if combat is not enabled
            if (!_gameSettings.IsTurnBasedCombatEnabled)
            {
                if (MusicConstants.CombatMusicInfo.Any(musicInfo =>
                        command.MusicName.Equals(musicInfo.Value, StringComparison.OrdinalIgnoreCase)) ||
                    command.MusicName.Equals("PI25A", StringComparison.OrdinalIgnoreCase) ||
                    command.MusicName.Equals("PI25", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            _currentScriptMusic = command.MusicName;
            _currentMusicClipName = command.MusicName;
            string musicFileVirtualPath = FileConstants.GetMusicFileVirtualPath(command.MusicName);
            string musicFileCachePath = _resourceProvider.GetMusicFilePathInCacheFolder(musicFileVirtualPath);
            Pal3.Instance.StartCoroutine(PlayMusicAsync(command.MusicName,
                musicFileVirtualPath,
                musicFileCachePath,
                command.Loop == 0 ? -1 : command.Loop));
        }

        public void Execute(StopScriptMusicCommand command)
        {
            if (string.IsNullOrEmpty(_currentScriptMusic)) return;

            if (_musicPlayer.isPlaying)
            {
                _musicPlayer.Stop();
            }

            _currentScriptMusic = string.Empty;
            _currentMusicClipName = string.Empty;

            if (_sceneManager.GetCurrentScene() is {} scene)
            {
                ScnSceneInfo sceneInfo = scene.GetSceneInfo();
                Pal3.Instance.StartCoroutine(PlaySceneMusicAsync(sceneInfo.CityName, sceneInfo.SceneName));
            }
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
                Pal3.Instance.StartCoroutine(
                    PlaySceneMusicAsync(command.NewSceneInfo.CityName, command.NewSceneInfo.SceneName));
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