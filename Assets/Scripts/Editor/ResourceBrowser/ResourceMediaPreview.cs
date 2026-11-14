// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.SceneManagement;
    using UnityEngine.Video;

    // Owns a single selection. Pending extraction can finish after disposal but cannot create Unity objects.
    public sealed class ResourceMediaPreview : IDisposable
    {
        private readonly ResourceEntry _entry;
        private readonly string _temporaryDirectory;
        private Task<string> _file;
        private UnityWebRequest _request;
        private UnityEditor.Editor _audioEditor;
        private GameObject _root;
        private Scene _scene;
        private double _started;
        private bool _disposed, _seeking, _pauseAfterSeek;
        private bool _wasUpdating = true;
        private double _seekTime, _seekStarted;
        private double? _queuedSeek;

        public AudioClip Audio
        {
            get; private set;
        }

        public VideoPlayer Video
        {
            get; private set;
        }

        public string Error
        {
            get; private set;
        }

        public bool Ready => Audio != null || Video != null && Video.isPrepared;
        public bool Loading => Error == null && !Ready;
        public string TemporaryDirectory => _temporaryDirectory;

        private static string TemporaryRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../../workspace/ResourceBrowserMedia"));

        public ResourceMediaPreview(ResourceCatalog catalog, ResourceEntry entry)
        {
            _entry = entry;
            _started = EditorApplication.timeSinceStartup;
            if (entry.Extension == ".bik")
            {
                Error = "BIK is not decoded by Unity. Export the original, or select a converted WebM file.";
                return;
            }
            var sizes = catalog.GetSizes(entry);
            if ((entry.IsArchived || entry.Category == "Audio") && sizes.length + sizes.packedLength > ResourceCatalog.PreviewByteLimit)
                throw new InvalidDataException("Media exceeds the 64 MiB extraction/audio preview limit. Export the original instead.");
            if (entry.IsArchived)
            {
                using var process = Process.GetCurrentProcess();
                _temporaryDirectory = Path.Combine(TemporaryRoot, process.Id + "-" + Guid.NewGuid().ToString("N"));
                string destination = Path.Combine(_temporaryDirectory, "preview" + entry.Extension);
                _file = Task.Run(() =>
                {
                    catalog.ExportOriginal(entry, destination);
                    return destination;
                });
            }
            else
                _file = Task.FromResult(entry.PhysicalPath);
        }

        // AudioClip's native inspector repaints itself during playback. Refresh loading/video
        // state here, including one final repaint when loading or playback stops.
        public bool Tick()
        {
            if (_disposed)
                return false;
            bool repaint = _wasUpdating;
            UpdateMedia();
            _wasUpdating = Error == null && (Loading || Video != null && (Video.isPlaying || _seeking || _queuedSeek.HasValue));
            return repaint || _wasUpdating;
        }

        private void UpdateMedia()
        {
            if (_disposed || Error != null)
                return;
            try
            {
                if (_file is { IsCompleted: true })
                {
                    string path = _file.GetAwaiter().GetResult();
                    _file = null;
                    if (_entry.Category == "Audio")
                    {
                        AudioType type = _entry.Extension switch
                        {
                            ".wav" => AudioType.WAV,
                            ".mp3" => AudioType.MPEG,
                            _ => AudioType.OGGVORBIS
                        };
                        _request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, type);
                        ((DownloadHandlerAudioClip)_request.downloadHandler).streamAudio = true;
                        _request.SendWebRequest();
                    }
                    else
                    {
                        _scene = EditorSceneManager.NewPreviewScene();
                        _root = new GameObject("Resource video preview") { hideFlags = HideFlags.HideAndDontSave };
                        SceneManager.MoveGameObjectToScene(_root, _scene);
                        Video = _root.AddComponent<VideoPlayer>();
                        Video.playOnAwake = false;
                        Video.renderMode = VideoRenderMode.APIOnly;
                        Video.audioOutputMode = VideoAudioOutputMode.Direct;
                        Video.source = VideoSource.Url;
                        Video.url = new Uri(path).AbsoluteUri;
                        Video.errorReceived += OnVideoError;
                        Video.seekCompleted += OnVideoSeek;
                        Video.sendFrameReadyEvents = true;
                        Video.frameReady += OnVideoFrame;
                        Video.Prepare();
                    }
                }
                if (_request is { isDone: true })
                {
                    if (_request.result != UnityWebRequest.Result.Success)
                        throw new InvalidDataException(_request.error);
                    Audio = DownloadHandlerAudioClip.GetContent(_request);
                    if (Audio == null)
                        throw new InvalidDataException("Unity returned no audio clip.");
                    Audio.name = _entry.Name;
                    Audio.hideFlags = HideFlags.HideAndDontSave;
                    _audioEditor = UnityEditor.Editor.CreateEditor(Audio);
                    _request.Dispose();
                    _request = null;
                }
                if (Loading && EditorApplication.timeSinceStartup - _started > 30)
                    throw new TimeoutException("Unity did not prepare this media within 30 seconds. Export it to inspect with another player.");
                if (!_seeking && _queuedSeek is { } requested)
                    SeekVideo(requested);
                if (_seeking && EditorApplication.timeSinceStartup - _seekStarted > 10)
                    throw new TimeoutException("Video seek did not produce a frame within 10 seconds.");
                if (Video != null && (!Video.isPrepared || Video.isPlaying || _seeking))
                    EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                StopLoading();
            }
        }

        public void SeekVideo(double seconds)
        {
            if (_disposed || Error != null || !double.IsFinite(seconds) || Video == null || !Video.isPrepared || !Video.canSetTime)
                return;
            _wasUpdating = true;
            if (_seeking)
            {
                _queuedSeek = seconds;
                return;
            }
            _queuedSeek = null;
            _seeking = true;
            _pauseAfterSeek = !Video.isPlaying;
            _seekStarted = EditorApplication.timeSinceStartup;
            // Container duration can include timestamps past the last decodable frame.
            // Leave a small end margin so Play does not interpret a paused seek as EOF and restart.
            double endMargin = Math.Max(0.1, 2 / Math.Max(0.001, Video.frameRate));
            _seekTime = Math.Clamp(seconds, 0, Math.Max(0, Video.length - endMargin));
            if (_pauseAfterSeek)
                SetSeekMute(true);
            Video.time = _seekTime;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private void SetSeekMute(bool mute)
        {
            for (ushort track = 0; track < Math.Min(Video.audioTrackCount, Video.controlledAudioTrackCount); track++)
                Video.SetDirectAudioMute(track, mute);
        }

        private void OnVideoSeek(VideoPlayer source)
        {
            if (_disposed || Error != null || !_seeking || source != Video)
                return;
            // Some URL decoders do not present a new frame while paused. Decode one silently.
            if (_pauseAfterSeek)
                source.Play();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private void OnVideoFrame(VideoPlayer source, long frame)
        {
            if (_disposed || Error != null || !_seeking || source != Video ||
                Math.Abs(frame / Math.Max(0.001, source.frameRate) - _seekTime) > Math.Max(0.2, 1 / Math.Max(0.001, source.frameRate)))
                return;
            if (_pauseAfterSeek)
            {
                source.Pause();
                SetSeekMute(false);
            }
            _seeking = false;
            _pauseAfterSeek = false;
            _wasUpdating = true;
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            _wasUpdating = true;
            Error = message;
            StopLoading();
        }

        public void Draw()
        {
            if (Error != null)
            {
                EditorGUILayout.HelpBox(Error, MessageType.Warning);
                return;
            }
            if (!Ready)
            {
                EditorGUILayout.HelpBox("Preparing media…", MessageType.Info);
                return;
            }
            if (Audio != null)
            {
                GUILayout.Label($"{Audio.length:F2} s · {Audio.frequency:N0} Hz · {Audio.channels} channels");
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                    _audioEditor.OnPreviewSettings();
                EditorGUILayout.HelpBox("Unity audio preview: Play / Auto Play / Loop. Click the preview to seek. Waveforms require imported assets and are not generated here.", MessageType.None);
                Rect audioRect = GUILayoutUtility.GetRect(100, 10000, 100, 250);
                GUI.BeginGroup(audioRect);
                try
                {
                    _audioEditor.DrawPreview(new Rect(0, 0, audioRect.width, audioRect.height));
                }
                finally
                {
                    GUI.EndGroup();
                }
                return;
            }
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(_seeking || _queuedSeek.HasValue))
                {
                    if (GUILayout.Button(Video.isPlaying ? "Pause" : "Play", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    {
                        if (Video.isPlaying)
                            Video.Pause();
                        else
                            Video.Play();
                    }
                }
                Video.isLooping = GUILayout.Toggle(Video.isLooping, "Loop", EditorStyles.toolbarButton, GUILayout.Width(50));
                using (new EditorGUI.DisabledScope(!Video.canSetTime))
                {
                    float displayedTime = (float)(_queuedSeek ?? (_seeking ? _seekTime : Video.time));
                    EditorGUI.BeginChangeCheck();
                    float sliderTime = GUILayout.HorizontalSlider(displayedTime, 0, (float)Video.length);
                    if (EditorGUI.EndChangeCheck())
                        SeekVideo(sliderTime);
                    EditorGUI.BeginChangeCheck();
                    float enteredTime = EditorGUILayout.DelayedFloatField(displayedTime, GUILayout.Width(70));
                    if (EditorGUI.EndChangeCheck())
                        SeekVideo(enteredTime);
                }
            }
            GUILayout.Label($"{Video.width} × {Video.height} · {Video.frameRate:F2} fps · {Video.time:F2} / {Video.length:F2} s", EditorStyles.miniLabel);
            Rect rect = GUILayoutUtility.GetRect(100, 10000, 100, 10000, GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, Color.black);
            if (Video.texture != null)
                GUI.DrawTexture(rect, Video.texture, ScaleMode.ScaleToFit);
        }

        private void StopLoading()
        {
            _seeking = false;
            _pauseAfterSeek = false;
            _queuedSeek = null;
            _request?.Abort();
            _request?.Dispose();
            _request = null;
            if (Video != null)
                Video.Stop();
        }

        private static void RemoveTemporaryFiles(string directory)
        {
            if (directory == null)
                return;
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public static void CleanupStaleTemporaryFiles()
        {
            try
            {
                if (!Directory.Exists(TemporaryRoot))
                    return;
                ResourceCatalog.CheckNoLinks(TemporaryRoot);
                foreach (string directory in Directory.EnumerateDirectories(TemporaryRoot))
                {
                    string name = Path.GetFileName(directory);
                    int separator = name.IndexOf('-');
                    if (separator <= 0 || !int.TryParse(name[..separator], out int processId) || processId <= 0 ||
                        !Guid.TryParseExact(name[(separator + 1)..], "N", out _) ||
                        (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                        continue;
                    try
                    {
                        using var process = Process.GetProcessById(processId);
                        if (!process.HasExited)
                            continue;
                    }
                    catch (ArgumentException) { } // No process owns this directory anymore.
                    catch (InvalidOperationException) { continue; }
                    catch (Win32Exception) { continue; }
                    catch (NotSupportedException) { continue; }
                    RemoveTemporaryFiles(directory);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            StopLoading();
            if (_audioEditor != null)
                UnityEngine.Object.DestroyImmediate(_audioEditor);
            if (Audio != null)
                UnityEngine.Object.DestroyImmediate(Audio);
            if (Video != null)
            {
                Video.errorReceived -= OnVideoError;
                Video.seekCompleted -= OnVideoSeek;
                Video.frameReady -= OnVideoFrame;
            }
            if (_root != null)
                UnityEngine.Object.DestroyImmediate(_root);
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Audio = null;
            Video = null;
            _audioEditor = null;
            if (_file != null)
            {
                string directory = _temporaryDirectory;
                _ = _file.ContinueWith(task =>
                {
                    _ = task.Exception;
                    RemoveTemporaryFiles(directory);
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                _file = null;
            }
            else
                RemoveTemporaryFiles(_temporaryDirectory);
        }
    }
}
