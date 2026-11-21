// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using ResourceBrowser;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using UnityEngine.Networking;
    using Object = UnityEngine.Object;

    // Hidden AudioSources in an owned preview scene work in EditMode. Every
    // body, movement, music and movie voice has its own pause/seek/stop handle;
    // no battle operation changes Unity's global preview transport or listener.
    public sealed class BattleAudioPreview : IDisposable, IBattleMovieAudioTransport
    {
        private sealed class Request
        {
            public string File;
            public double Start;
            public bool Loop;
            public float Gain = 1;
        }

        private sealed class Decode : IDisposable
        {
            public UnityWebRequest Request;
            public string Temporary;

            public void Dispose()
            {
                try
                {
                    Request?.Abort();
                }
                finally
                {
                    Request?.Dispose();
                }
            }
        }

        private sealed class Voice
        {
            public AudioSource Source;
            public Request Request;
            public double MovieTime;
            public bool MoviePlaying = true;
            public bool Started, Paused;
        }

        private readonly Scene _scene;
        private readonly bool _ownsScene;
        private readonly GameObject _root;
        private readonly Dictionary<object, Voice> _voices = new();
        private const string MusicVoice = "music";
        private readonly ResourceCatalog _resources;
        private readonly Dictionary<string, string> _soundFiles = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _missingSounds = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AudioClip> _clips = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Decode> _pending = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<object, Request> _desired = new();

        private readonly string _cache;
        private double _time;
        private bool _disposed, _muted, _playbackEnabled = true;
        private float _musicGain = 1;

        public bool PlaybackEnabled
        {
            get => _playbackEnabled;
            set
            {
                if (_playbackEnabled == value)
                    return;
                _playbackEnabled = value;
                UpdateTransport(true);
            }
        }

        public bool Muted
        {
            get => _muted;
            set
            {
                if (_muted == value)
                    return;
                _muted = value;
                UpdateTransport(true);
            }
        }

        public List<string> Diagnostics { get; } = new();

        public event Action<string> SoundPlayed;

        public BattleAudioPreview(ResourceCatalog resources, Scene scene = default)
        {
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            if (scene.IsValid() && !EditorSceneManager.IsPreviewScene(scene))
                throw new ArgumentException("Battle audio requires an editor preview scene.", nameof(scene));
            _ownsScene = !scene.IsValid();
            _scene = _ownsScene ? EditorSceneManager.NewPreviewScene() : scene;
            _root = new GameObject("Battle audio preview") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(_root, _scene);
            try
            {
                string directory = Path.Combine(resources.Root, "snd");
                if (Directory.Exists(directory))
                    foreach (string path in Directory.EnumerateFiles(directory).Where(path => Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase)))
                        _soundFiles.TryAdd(Path.GetFileName(path), path);
                string current = resources.Root;
                while (current != null && !Directory.Exists(Path.Combine(current, "workspace")))
                    current = Directory.GetParent(current)?.FullName;
                _cache = current == null ? Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/BattleAudio")) : Path.Combine(current, "workspace", "Cache", "BattleAudio");
                EditorApplication.update += Update;
            }
            catch
            {
                if (_ownsScene)
                    EditorSceneManager.ClosePreviewScene(_scene);
                else
                    Object.DestroyImmediate(_root);
                throw;
            }
        }

        public void SetTime(double seconds)
        {
            if (!double.IsFinite(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            _time = seconds;
            foreach (var pair in _desired.ToArray())
                if (!pair.Value.Loop && _clips.TryGetValue(pair.Value.File, out AudioClip clip) && _time - pair.Value.Start >= clip.length)
                    RemoveVoice(pair.Key);
            UpdateTransport(false);
        }

        // Native LoadSnd allocates another node when this sound is already
        // playing. Decode/cache ownership is shared; one-shot voices are not.
        public void Play(string sound) => RequestSound(new object(), sound, false);

        public void PlayMovement(string actor, string sound, bool loop) => RequestSound("movement:" + actor, sound, loop);

        public void StopMovement(string actor) => RemoveVoice("movement:" + actor);

        public void StopBodySounds()
        {
            foreach (object key in _desired.Keys.Where(key => !Equals(key, MusicVoice)).ToArray())
            {
                RemoveVoice(key);
            }
        }

        private static string Normalize(string sound)
        {
            if (string.IsNullOrWhiteSpace(sound))
                return "";
            return Path.GetFileName(sound.Replace('\\', '/')).ToLowerInvariant();
        }

        private void RequestSound(object voice, string sound, bool loop)
        {
            if (_disposed || string.IsNullOrWhiteSpace(sound))
                return;
            string name = Normalize(sound);
            string file = name.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ? name : name + ".wav";
            if (!_soundFiles.TryGetValue(file, out string path))
            {
                if (_missingSounds.Add(file))
                    Diagnostics.Add("Missing battle sound: " + file);
                return;
            }
            Queue(voice, file, path, AudioType.WAV, loop);
        }

        public void SetMusic(string music)
        {
            StopMusic();
            if (_disposed || string.IsNullOrWhiteSpace(music) || music.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                return;
            string name = Normalize(music);
            string file = name.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? name : name + ".mp3";
            string key = "music:" + file;
            string temporary = null;
            try
            {
                RetainMusic(key);
                if (_clips.ContainsKey(key) || _pending.ContainsKey(key))
                {
                    Queue(MusicVoice, key, null, AudioType.MPEG, true);
                    return;
                }
                string musicDirectory = Path.Combine(_resources.Root, "music");
                string loose = Directory.Exists(musicDirectory) ? Directory.EnumerateFiles(musicDirectory).FirstOrDefault(path => Path.GetFileName(path).Equals(file, StringComparison.OrdinalIgnoreCase)) : null;
                if (loose != null)
                {
                    Queue(MusicVoice, key, loose, AudioType.MPEG, true);
                    return;
                }
                string archive = Directory.EnumerateFiles(musicDirectory).Single(path => Path.GetFileName(path).Equals("music.cpk", StringComparison.OrdinalIgnoreCase));
                _resources.FileSystem.Mount(Path.GetRelativePath(_resources.Root, archive), _resources.Codepage);
                byte[] bytes = _resources.FileSystem.ReadAllBytes("music.cpk\\music\\" + file);
                Directory.CreateDirectory(_cache);
                temporary = Path.Combine(_cache, Guid.NewGuid().ToString("N") + ".mp3");
                File.WriteAllBytes(temporary, bytes);
                string extracted = temporary;
                temporary = null;
                Queue(MusicVoice, key, extracted, AudioType.MPEG, true, extracted);
            }
            catch (Exception ex)
            {
                Diagnostics.Add("Battle music " + file + ": " + ex.Message);
            }
            finally
            {
                DeleteTemporary(temporary);
            }
        }

        private void RetainMusic(string current)
        {
            bool Obsolete(string key) => key.StartsWith("music:", StringComparison.Ordinal) &&
                !key.Equals(current, StringComparison.OrdinalIgnoreCase);
            foreach (string key in _pending.Keys.Where(Obsolete).ToArray())
                if (_pending.Remove(key, out Decode decode))
                    ReleaseDecode(decode);
            foreach (string key in _clips.Keys.Where(Obsolete).ToArray())
                if (_clips.Remove(key, out AudioClip clip))
                    Object.DestroyImmediate(clip);
        }

        public void SetMusicGain(float gain)
        {
            if (!float.IsFinite(gain) || gain < 0 || gain > 1)
                throw new ArgumentOutOfRangeException(nameof(gain));
            _musicGain = gain;
            if (_desired.TryGetValue(MusicVoice, out Request request))
                request.Gain = gain;
            if (_voices.TryGetValue(MusicVoice, out Voice voice))
                voice.Source.volume = gain;
        }

        public void StopMusic() => RemoveVoice(MusicVoice);

        private void Queue(object voice, string file, string path, AudioType type, bool loop, string temporary = null)
        {
            var desired = new Request { File = file, Start = _time, Loop = loop, Gain = Equals(voice, MusicVoice) ? _musicGain : 1f };
            _desired[voice] = desired;
            if (_clips.TryGetValue(file, out AudioClip clip))
            {
                DeleteTemporary(temporary);
                StartVoice(voice, desired, clip);
                return;
            }
            if (_pending.ContainsKey(file))
            {
                DeleteTemporary(temporary);
                return;
            }
            var decode = new Decode { Temporary = temporary };
            try
            {
                decode.Request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, type);
                ((DownloadHandlerAudioClip)decode.Request.downloadHandler).streamAudio = false;
                decode.Request.SendWebRequest();
                _pending.Add(file, decode);
            }
            catch
            {
                _desired.Remove(voice);
                ReleaseDecode(decode);
                throw;
            }
        }

        private void Update()
        {
            foreach (var pair in _pending.Where(pair => pair.Value.Request.isDone).ToArray())
            {
                if (_disposed || !_pending.TryGetValue(pair.Key, out Decode current) || !ReferenceEquals(current, pair.Value))
                    continue;
                _pending.Remove(pair.Key);
                Decode decode = pair.Value;
                UnityWebRequest request = decode.Request;
                AudioClip clip = null;
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                        throw new InvalidDataException(request.error);
                    clip = DownloadHandlerAudioClip.GetContent(request);
                    if (clip != null)
                        clip.name = pair.Key;
                    if (clip == null)
                        throw new InvalidDataException("The audio decoder returned no clip.");
                    clip.hideFlags = HideFlags.HideAndDontSave;
                    AudioClip pcm = clip;
                    clip = null;
                    _clips.Add(pair.Key, pcm);
                    foreach (var voice in _desired.Where(voice => voice.Value.File == pair.Key).ToArray())
                    {
                        // A started voice may synchronously stop/replace this
                        // battle through SoundPlayed. Honor its current owner.
                        if (!_desired.TryGetValue(voice.Key, out Request desired) || !ReferenceEquals(desired, voice.Value))
                            continue;
                        if (voice.Value.Loop || _time - voice.Value.Start < pcm.length)
                            StartVoice(voice.Key, voice.Value, pcm);
                        else
                            RemoveVoice(voice.Key);
                    }
                }
                catch (Exception ex)
                {
                    Diagnostics.Add(pair.Key + ": " + ex.Message);
                    foreach (object key in _desired.Where(voice => voice.Value.File == pair.Key).Select(voice => voice.Key).ToArray())
                        RemoveVoice(key);
                }
                finally
                {
                    if (clip != null)
                        Object.DestroyImmediate(clip);
                    ReleaseDecode(decode);
                }
            }
        }

        private void StartVoice(object voice, Request request, AudioClip clip, double movieTime = 0, bool moviePlaying = true)
        {
            RemoveVoiceSource(voice);
            var child = new GameObject("Battle voice: " + request.File) { hideFlags = HideFlags.HideAndDontSave };
            child.transform.SetParent(_root.transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.clip = clip;
            source.loop = request.Loop;
            source.volume = request.Gain;
            // Keep the transport clock running for background regressions without audible output.
            source.mute = Application.isBatchMode;
            _voices.Add(voice, new Voice { Source = source, Request = request, MovieTime = movieTime, MoviePlaying = moviePlaying });
            SynchronizeVoice(_voices[voice], true);
            if (request.File != "movie" && !Muted && PlaybackEnabled)
                SoundPlayed?.Invoke(request.File);
        }

        private void RemoveVoice(object voice)
        {
            _desired.Remove(voice);
            RemoveVoiceSource(voice);
        }

        private void RemoveVoiceSource(object voice)
        {
            if (!_voices.Remove(voice, out Voice entry) || entry.Source == null)
                return;
            entry.Source.Stop();
            Object.DestroyImmediate(entry.Source.gameObject);
        }

        public void SynchronizeMovie(object owner, AudioClip clip, double elapsed, bool playing)
        {
            if (_disposed)
                return;
            if (!_voices.TryGetValue(owner, out Voice voice))
            {
                var request = new Request { File = "movie", Start = _time - elapsed };
                StartVoice(owner, request, clip, elapsed, playing);
                voice = _voices[owner];
            }
            voice.MovieTime = elapsed;
            voice.MoviePlaying = playing;
            SynchronizeVoice(voice, false);
        }

        public void StopMovie(object owner) => RemoveVoiceSource(owner);

        private void UpdateTransport(bool seek)
        {
            if (_disposed)
                return;
            foreach (Voice voice in _voices.Values)
                SynchronizeVoice(voice, seek);
        }

        private void SynchronizeVoice(Voice voice, bool seek)
        {
            AudioSource source = voice.Source;
            if (source.clip == null || source.clip.samples <= 0 || source.clip.frequency <= 0)
            {
                source.Stop();
                voice.Started = voice.Paused = false;
                return;
            }
            bool movie = voice.Request.File == "movie";
            bool playing = PlaybackEnabled && !Muted && (!movie || voice.MoviePlaying);
            if (!playing)
            {
                if (voice.Started && !voice.Paused)
                {
                    source.Pause();
                    voice.Paused = true;
                }
                return;
            }
            double elapsed = movie ? voice.MovieTime : Math.Max(0, _time - voice.Request.Start);
            double duration = source.clip.samples / (double)source.clip.frequency;
            if (!source.loop && elapsed >= duration)
            {
                source.Stop();
                voice.Started = voice.Paused = false;
                return;
            }
            int sample = (int)((elapsed % duration) * source.clip.frequency);
            // The logical battle clock also moves during silent stepping. A
            // resumed voice seeks to that clock instead of replaying old cues.
            bool synchronize = seek || !voice.Started || voice.Paused;
            // Music and sound effects run on the audio clock. Input callbacks
            // can repeat a battle tick, and background editor updates may lag.
            // Keep running voices continuous; only movies track video drift.
            // Starting or resuming after silent inspection still seeks above.
            if (!synchronize && movie)
            {
                int distance = Math.Abs(source.timeSamples - sample);
                if (source.loop)
                    distance = Math.Min(distance, source.clip.samples - distance);
                synchronize = distance > source.clip.frequency / 10;
            }
            if (synchronize)
                source.timeSamples = Math.Clamp(sample, 0, source.clip.samples - 1);
            if (!voice.Started)
            {
                source.Play();
                voice.Started = true;
            }
            else if (voice.Paused)
            {
                source.UnPause();
                voice.Paused = false;
            }
        }

        public void Stop()
        {
            _desired.Clear();
            foreach (object voice in _voices.Keys.ToArray())
                RemoveVoiceSource(voice);
            Decode[] pending = _pending.Values.ToArray();
            _pending.Clear();
            foreach (Decode decode in pending)
                ReleaseDecode(decode);
        }

        private void ReleaseDecode(Decode decode)
        {
            try
            {
                decode.Dispose();
            }
            finally
            {
                DeleteTemporary(decode.Temporary);
            }
        }

        private void DeleteTemporary(string path)
        {
            if (path == null)
                return;
            try
            {
                File.Delete(path);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Diagnostics.Add($"Cannot remove temporary battle audio {path}: {error.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            EditorApplication.update -= Update;
            try
            {
                Stop();
            }
            finally
            {
                foreach (AudioClip clip in _clips.Values)
                    Object.DestroyImmediate(clip);
                _clips.Clear();
                if (_scene.IsValid())
                {
                    if (_ownsScene)
                        EditorSceneManager.ClosePreviewScene(_scene);
                    else
                        Object.DestroyImmediate(_root);
                }
            }
        }
    }
}
