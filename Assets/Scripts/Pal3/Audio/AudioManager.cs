// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Audio
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.Extensions;
    using Data;
    using MetaData;
    using Scene;
    using UnityEngine;

    public class AudioManager : MonoBehaviour,
        ICommandExecutor<PlaySfxCommand>,
        ICommandExecutor<PlayMusicCommand>,
        ICommandExecutor<PlaySfxAtGameObjectRequest>,
        ICommandExecutor<PlayVideoCommand>,
        ICommandExecutor<StopMusicCommand>,
        ICommandExecutor<ScenePreLoadingNotification>,
        ICommandExecutor<ScenePostLoadingNotification>,
        ICommandExecutor<ResetGameStateCommand>
    {
        public float MusicVolume { get; set; } = 0.8f;
        public float SoundVolume { get; set; } = 0.8f;

        private AudioSource _musicPlayer;
        private AudioSource _sfxPlayer;
        private GameResourceProvider _resourceProvider;
        private SceneManager _sceneManager;

        private const string STOP_MUSIC_NAME = "NONE";
        private string _currentMusicClipName = string.Empty;
        private string _currentScriptMusic = string.Empty;

        private readonly Queue<Coroutine> _sfxCoroutines = new ();

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

        private void PlaySceneMusic(string cityName, string sceneName)
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
                return;
            }

            if (_musicPlayer.isPlaying &&
                string.Equals(_currentMusicClipName, musicName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _currentMusicClipName = musicName;
            var musicFilePath = GetMusicFilePath(musicName);
            StartCoroutine(PlayMusic(musicFilePath, -1));
        }

        public IEnumerator PlayAudioClip(AudioSource audioSource, AudioClip audioClip, int loopCount, float volume)
        {
            if (loopCount <= 0)
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
            else
            {
                while (--loopCount >= 0)
                {
                    audioSource.PlayOneShot(audioClip, volume);
                    yield return new WaitForSeconds(audioClip.length);
                }
            }
        }

        public IEnumerator PlaySfx(string sfxFilePath, int loopCount)
        {
            yield return AudioClipLoader.LoadAudioClip(sfxFilePath, AudioType.WAV, audioClip =>
            {
                _sfxCoroutines.Enqueue(StartCoroutine(
                    PlayAudioClip(_sfxPlayer, audioClip, loopCount, SoundVolume)));
            });
        }

        // Spatial Audio
        public IEnumerator PlaySfx(string sfxFilePath, int loopCount, GameObject parent)
        {
            yield return AudioClipLoader.LoadAudioClip(sfxFilePath, AudioType.WAV, audioClip =>
            {
                if (parent == null) return;

                var audioSource = parent.GetOrAddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.maxDistance = 75f;

                _sfxCoroutines.Enqueue(StartCoroutine(
                    PlayAudioClip(audioSource, audioClip, loopCount, SoundVolume)));
            });
        }

        private string GetMusicFilePath(string musicName)
        {
            var separator = CpkConstants.CpkDirectorySeparatorChar;
            var musicFileVirtualPath =
                $"{FileConstants.MusicCpkPathInfo.cpkName}{separator}" +
                $"{FileConstants.MusicCpkPathInfo.relativePath}{separator}{musicName}.mp3";

            return _resourceProvider.GetMp3FilePathInCacheFolder(musicFileVirtualPath);
        }

        public IEnumerator PlayMusic(string musicFilePath, int loopCount)
        {
            yield return AudioClipLoader.LoadAudioClip(musicFilePath, AudioType.MPEG, audioClip =>
            {
                StartCoroutine(PlayAudioClip(_musicPlayer, audioClip, loopCount, MusicVolume));
            });
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

            var sfxFilePath = _resourceProvider.GetSfxFilePath(sfxName);
            _sfxCoroutines.Enqueue(StartCoroutine(PlaySfx(sfxFilePath, loopCount)));
        }

        public void Execute(PlaySfxAtGameObjectRequest request)
        {
            var sfxFilePath = _resourceProvider.GetSfxFilePath(request.SfxName);
            _sfxCoroutines.Enqueue(StartCoroutine(
                PlaySfx(sfxFilePath, request.LoopCount, request.Parent)));
        }

        public void Execute(PlayMusicCommand command)
        {
            if (command.MusicName.Equals(STOP_MUSIC_NAME))
            {
                _currentScriptMusic = string.Empty;
                _musicPlayer.Stop();
                return;
            }

            _currentScriptMusic = command.MusicName;
            _currentMusicClipName = command.MusicName;
            var musicFilePath = GetMusicFilePath(command.MusicName);
            StartCoroutine(PlayMusic(musicFilePath, command.Loop == 0 ? -1 : command.Loop));
        }

        public void Execute(StopMusicCommand command)
        {
            if (_musicPlayer.isPlaying)
            {
                _musicPlayer.Stop();
            }

            _currentScriptMusic = string.Empty;
            _currentMusicClipName = string.Empty;

            var sceneInfo = _sceneManager.GetCurrentScene().GetSceneInfo();
            PlaySceneMusic(sceneInfo.CityName, sceneInfo.Name);
        }

        public void Execute(PlayVideoCommand command)
        {
            if (_musicPlayer.isPlaying)
            {
                _musicPlayer.Stop();
            }
        }

        public void Execute(ScenePreLoadingNotification command)
        {
            if (_sfxPlayer.clip != null)
            {
                _sfxPlayer.loop = false;
                _sfxPlayer.Stop();
                Destroy(_sfxPlayer.clip);
            }

            while (_sfxCoroutines.Count > 0)
            {
                if (_sfxCoroutines.Dequeue() is {} coroutine)
                {
                    StopCoroutine(coroutine);
                }
            }
        }

        public void Execute(ScenePostLoadingNotification command)
        {
            if (string.IsNullOrEmpty(_currentScriptMusic))
            {
                PlaySceneMusic(command.SceneInfo.CityName, command.SceneInfo.Name);
            }
        }

        public void Execute(ResetGameStateCommand command)
        {
            _currentScriptMusic = string.Empty;
        }
    }
}