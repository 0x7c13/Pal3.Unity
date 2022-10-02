// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Audio
{
    using System;
    using System.Collections;
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
        ICommandExecutor<PlaySfxAtGameObjectRequest>,
        ICommandExecutor<StopSfxPlayingAtGameObjectRequest>,
        ICommandExecutor<GameStateChangedNotification>,
        ICommandExecutor<StopMusicCommand>,
        ICommandExecutor<ScenePreLoadingNotification>,
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        public float MusicVolume { get; set; } = 0.4f;
        public float SoundVolume { get; set; } = 0.5f;

        private AudioSource _musicPlayer;
        private AudioSource _sfxPlayer;
        private GameResourceProvider _resourceProvider;
        private SceneManager _sceneManager;

        private const string STOP_MUSIC_NAME = "NONE";
        private string _currentMusicClipName = string.Empty;
        private string _currentScriptMusic = string.Empty;

        private CancellationTokenSource _sceneAudioCts = new ();

        public void Init(GameResourceProvider resourceProvider,
            SceneManager sceneManager,
            AudioSource musicSource,
            AudioSource sfxSource)
        {
            _resourceProvider = resourceProvider;
            _sceneManager = sceneManager;
            _musicPlayer = musicSource;
            _sfxPlayer = sfxSource;
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
            yield return PlayMusic(musicFileVirtualPath, musicFileCachePath, -1);
        }

        public IEnumerator PlayAudioClip(AudioSource audioSource,
            AudioClip audioClip,
            int loopCount,
            float interval,
            float volume,
            CancellationToken cancellationToken)
        {
            if (loopCount <= 0 && interval == 0f)
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
            else if (loopCount <= 0 && interval > 0f)
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

        public IEnumerator PlaySfx(string sfxFilePath, int loopCount, CancellationToken cancellationToken)
        {
            AudioClip sfxAudioClip = null;
            yield return _resourceProvider.LoadAudioClip(sfxFilePath, AudioType.WAV, audioClip =>
            {
                sfxAudioClip = audioClip;
            });
            if (sfxAudioClip == null || cancellationToken.IsCancellationRequested) yield break;
            yield return PlayAudioClip(_sfxPlayer, sfxAudioClip, loopCount, 0f, SoundVolume, cancellationToken);
        }

        // Spatial Audio
        public IEnumerator PlaySfx(GameObject parent,
            string sfxFilePath,
            string audioSourceName,
            float volume,
            int loopCount = -1,
            float interval = 0f,
            CancellationToken cancellationToken = default)
        {
            if (parent == null || cancellationToken.IsCancellationRequested) yield break;

            AudioClip sfxAudioClip = null;
            
            yield return _resourceProvider.LoadAudioClip(sfxFilePath, AudioType.WAV, audioClip =>
            {
                sfxAudioClip = audioClip;
            });
            
            if (parent == null || cancellationToken.IsCancellationRequested || sfxAudioClip == null) yield break;
            
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

        private string GetMusicFileVirtualPath(string musicName)
        {
            var separator = CpkConstants.DirectorySeparator;
            return $"{FileConstants.MusicCpkPathInfo.cpkName}{separator}" +
                   $"{FileConstants.MusicCpkPathInfo.relativePath}{separator}{musicName}.mp3";
        }

        public IEnumerator PlayMusic(string musicFileVirtualPath, string musicFileCachePath, int loopCount)
        {
            yield return _resourceProvider.ExtractAndMoveMp3FileToCacheFolder(musicFileVirtualPath, musicFileCachePath);
            AudioClip musicClip = null;
            yield return _resourceProvider.LoadAudioClip(musicFileCachePath, AudioType.MPEG, audioClip =>
            {
                musicClip = audioClip;
            });
            if (musicClip == null) yield break;
            StartCoroutine(PlayAudioClip(_musicPlayer, musicClip, loopCount, 0f, MusicVolume,
                new CancellationToken(false))); // Should not stop music during scene switch
        }

        private IEnumerator PlaySfxAtGameObject(string sfxFilePath,
            string audioSourceName,
            float volume,
            float startDelayInSeconds,
            int loopCount,
            float interval,
            GameObject parent,
            CancellationToken cancellationToken)
        {
            if (startDelayInSeconds > 0)
            {
                yield return new WaitForSeconds(startDelayInSeconds);
            }

            if (!cancellationToken.IsCancellationRequested && parent != null)
            {
                yield return PlaySfx(parent,
                    sfxFilePath,
                    audioSourceName,
                    volume,
                    loopCount,
                    interval,
                    cancellationToken);
            }
        }

        public void Execute(PlaySfxCommand command)
        {
            var sfxName = command.SfxName;
            var loopCount = command.LoopCount;

            // TODO: Start/stop walk/run sfx based on actor behaviour
            if (command.SfxName.Contains("we022", StringComparison.OrdinalIgnoreCase))
            {
                loopCount = 3;
            }

            // TODO: Change walk/run sfx based on tile type
            if (command.SfxName.Equals("we022", StringComparison.OrdinalIgnoreCase))
            {
                sfxName = "we022b";
            }

            // TODO: This sfx will be triggered to loop forever after actor
            // leaving the hotel. Maybe we should play it by attaching
            // the sound to the hotel position?
            if (command.SfxName.Equals("wc019", StringComparison.OrdinalIgnoreCase) &&
                command.LoopCount == -1)
            {
                return;
            }

            var sfxFilePath = _resourceProvider.GetSfxFilePath(sfxName);
            CancellationToken cancellationToken = _sceneAudioCts.Token;
            StartCoroutine(PlaySfx(sfxFilePath, loopCount, cancellationToken));
        }

        public void Execute(PlaySfxAtGameObjectRequest request)
        {
            var sfxFilePath = _resourceProvider.GetSfxFilePath(request.SfxName);
            CancellationToken cancellationToken = _sceneAudioCts.Token;
            StartCoroutine(PlaySfxAtGameObject(sfxFilePath,
                request.AudioSourceName,
                request.Volume,
                request.StartDelayInSeconds,
                request.LoopCount,
                request.Interval,
                request.Parent,
                cancellationToken));
        }
        
        public void Execute(StopSfxPlayingAtGameObjectRequest command)
        {
            Transform audioSourceParentTransform = command.Parent.transform.Find(command.AudioSourceName);
            if (audioSourceParentTransform == null) return;

            GameObject audioSourceParent = audioSourceParentTransform.gameObject;
            var audioSource = audioSourceParent.GetComponentInChildren<AudioSource>();
            if (audioSource == default) return;
            
            audioSource.Stop();
            audioSource.clip = null;
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
                    command.MusicName.Equals(musicInfo.Value, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            _currentScriptMusic = command.MusicName;
            _currentMusicClipName = command.MusicName;
            var musicFileVirtualPath = GetMusicFileVirtualPath(command.MusicName);
            var musicFileCachePath = _resourceProvider.GetMp3FilePathInCacheFolder(musicFileVirtualPath);
            StartCoroutine(PlayMusic(musicFileVirtualPath,
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
            StartCoroutine(PlaySceneMusic(sceneInfo.CityName, sceneInfo.Name));
        }

        public void Execute(ScenePreLoadingNotification command)
        {
            if (_sfxPlayer.clip != null)
            {
                _sfxPlayer.loop = false;
                _sfxPlayer.Stop();
                Destroy(_sfxPlayer.clip);
            }

            _sceneAudioCts.Cancel();
            _sceneAudioCts = new CancellationTokenSource();
        }

        public void Execute(ScenePostLoadingNotification command)
        {
            if (string.IsNullOrEmpty(_currentScriptMusic))
            {
                StartCoroutine(PlaySceneMusic(command.NewSceneInfo.CityName, command.NewSceneInfo.Name));
            }
        }

        public void Execute(ResetGameStateCommand command)
        {
            _currentScriptMusic = string.Empty;
            _musicPlayer.Stop();
            _sfxPlayer.Stop();
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
                _sfxPlayer.mute = true;
            }
            else
            {
                if (_musicPlayer.mute) _musicPlayer.mute = false;
                if (_sfxPlayer.mute) _sfxPlayer.mute = false;
            }
        }
    }
}