// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.DataReader.Cpk;
    using Core.FileSystem;
    using Game.Effect.Runtime;
    using ResourceBrowser;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Object = UnityEngine.Object;

    // EditMode has no reliable VideoPlayer update loop. Decode once outside the
    // fixed simulation; present original frames directly on the Bink frame clock.
    public sealed class BattleMoviePlayer : IDisposable
    {
        private sealed class DecodedMovie
        {
            public string Directory;
            public string Name;
            public int Width, Height, Frames;
            public uint Rate, Divisor;
            public bool HasAudio;

            public int FrameBytes => checked(Width * Height * 4);
        }

        private sealed class Preparation
        {
            public DecodedMovie Data;
            public string Error;
            public bool Missing;
        }

        private sealed class Movie : IEffectMovie
        {
            private readonly BattleMoviePlayer _owner;
            private readonly Task<Preparation> _preparation;
            private DecodedMovie _data;
            private FileStream _frames;
            private byte[] _pixels;
            private int _frame = -1;
            private bool _disposed, _audioPlaying, _audioPaused, _audioUnavailable;
            private AudioClip _audio;
            public Texture2D Texture;
            public double Elapsed;

            public Movie(BattleMoviePlayer owner, string name, Task<Preparation> preparation)
            {
                _owner = owner;
                Name = name;
                _preparation = preparation;
            }

            private Movie(BattleMoviePlayer owner, DecodedMovie data) : this(owner, data.Name, null)
            {
                Open(data);
            }

            private void Open(DecodedMovie data)
            {
                _data = data;
                try
                {
                    _frames = File.OpenRead(Path.Combine(data.Directory, "video.rgba"));
                    CheckFrameCount(data, _frames.Length);
                    _pixels = new byte[data.FrameBytes];
                    Texture = new Texture2D(data.Width, data.Height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
                    if (data.HasAudio)
                        _audio = ReadAudio(Path.Combine(data.Directory, "audio.wav"), silent: Application.isBatchMode && _owner._audioTransport == null);
                    PresentFrame();
                    UpdateAudio();
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            public bool Waiting => !_disposed && _data == null && !_preparation.IsCompleted;

            public bool Finished => _disposed || _data != null && Elapsed + 1e-9 >= _data.Frames * (double)_data.Divisor / _data.Rate;

            public bool HasFrame => _frame >= 0;

            public string Name
            {
                get;
            }

            public void Advance(double seconds)
            {
                if (_disposed || Waiting)
                    return;
                if (_data == null)
                {
                    Preparation result = CompletedPreparation(_preparation);
                    if (result.Data == null)
                    {
                        Dispose();
                        return;
                    }
                    Open(result.Data);
                }
                Elapsed = Math.Min(_data.Frames * (double)_data.Divisor / _data.Rate, Elapsed + seconds);
                PresentFrame();
                UpdateAudio();
            }

            private void PresentFrame()
            {
                int frame = Math.Min(_data.Frames - 1, (int)Math.Floor(Elapsed * _data.Rate / _data.Divisor));
                if (frame == _frame)
                    return;
                _frames.Position = (long)frame * _pixels.Length;
                int count = 0;
                while (count < _pixels.Length)
                {
                    int read = _frames.Read(_pixels, count, _pixels.Length - count);
                    if (read == 0)
                        throw new EndOfStreamException("Incomplete decoded Bink frame.");
                    count += read;
                }
                Texture.LoadRawTextureData(_pixels);
                Texture.Apply(false, false);
                _frame = frame;
            }

            public void UpdateAudio()
            {
                if (_owner.Muted || Finished)
                {
                    StopAudio();
                    return;
                }
                if (_audio == null || _audioUnavailable)
                    return;
                try
                {
                    UpdateTransport();
                }
                catch (Exception error) when (BattleAudioPreviewDevice.IsUnavailable(error))
                {
                    ReportUnavailableAudio(error);
                    StopAudio();
                }
            }

            private void UpdateTransport()
            {
                if (_owner._audioTransport != null)
                {
                    _owner._audioTransport.SynchronizeMovie(this, _audio, Elapsed, _owner.AudioPlaybackEnabled);
                    _audioPlaying = _owner.AudioPlaybackEnabled;
                    _audioPaused = !_owner.AudioPlaybackEnabled;
                    return;
                }
                if (!_owner.AudioPlaybackEnabled)
                {
                    if (_audioPlaying)
                    {
                        BattleAudioPreviewDevice.Pause(this);
                        _audioPaused = true;
                    }
                    return;
                }
                int sample = Math.Min(_audio.samples - 1, (int)(Elapsed * _audio.frequency));
                if (!_audioPlaying || !BattleAudioPreviewDevice.Owns(this))
                    _audioPlaying = BattleAudioPreviewDevice.Play(this, _audio, sample, exclusive: true);
                else if (_audioPaused)
                    BattleAudioPreviewDevice.Resume(this, _audio, sample);
                else
                {
                    int actual = BattleAudioPreviewDevice.Position(this);
                    if (actual >= 0 && Math.Abs(actual - sample) > _audio.frequency / 10)
                        BattleAudioPreviewDevice.Seek(this, _audio, sample);
                }
                _audioPaused = false;
            }

            private void StopAudio()
            {
                _audioPlaying = false;
                _audioPaused = false;
                try
                {
                    if (_owner._audioTransport != null)
                        _owner._audioTransport.StopMovie(this);
                    else
                        BattleAudioPreviewDevice.Stop(this);
                }
                catch (Exception error) when (BattleAudioPreviewDevice.IsUnavailable(error))
                {
                    ReportUnavailableAudio(error);
                }
            }

            private void ReportUnavailableAudio(Exception error)
            {
                if (_audioUnavailable)
                    return;
                _audioUnavailable = true;
                _owner.Diagnostics.Add($"战斗影片 {Name} 继续静音播放：{error.GetBaseException().Message}");
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                StopAudio();
                _frames?.Dispose();
                _pixels = null;
                if (Texture != null)
                    Object.DestroyImmediate(Texture);
                if (_audio != null)
                    Object.DestroyImmediate(_audio);
            }
        }

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> DecodeLeases = new(StringComparer.Ordinal);
        private static readonly TimeSpan CacheWait = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan DecodeWait = TimeSpan.FromMinutes(2);
        private readonly string _dataRoot;
        private readonly int _codepage;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly IBattleMovieAudioTransport _audioTransport;
        private readonly string _workspace, _cache, _ffmpeg;
        private readonly ResourceModelPreview _preview;
        private readonly Dictionary<string, Task<Preparation>> _prepared = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _reportedFailures = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<Movie> _movies = new();
        private readonly Vector3[] _screenVertices = new Vector3[8];
        private Material _frameMaterial;
        private Material _backdrop;
        private Mesh _screen;
        private MeshRenderer _overlay;
        private Vector2 _frameScale;
        private bool _muted, _audioPlaybackEnabled = true, _disposed;

        public bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                foreach (Movie movie in _movies)
                    movie.UpdateAudio();
            }
        }

        public bool AudioPlaybackEnabled
        {
            get => _audioPlaybackEnabled;
            set
            {
                _audioPlaybackEnabled = value;
                foreach (Movie movie in _movies)
                    movie.UpdateAudio();
            }
        }

        public readonly List<string> MissingMovies = new();
        public readonly List<string> Diagnostics = new();

        public bool HasDecodedFrame => _movies.Any(movie => !movie.Finished && movie.HasFrame);

        public Texture CurrentTexture => _movies.LastOrDefault(movie => !movie.Finished)?.Texture;

        public bool BlocksBattleTime => _movies.Any(movie => !movie.Finished);

        public bool IsPreparingPlayback => _movies.Any(movie => movie.Waiting);

        public BattleMoviePlayer(BattleData data, ResourceModelPreview preview, IBattleMovieAudioTransport audio = null)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            _dataRoot = data.Resources.FileSystem.GetRootPath();
            _codepage = data.Resources.Codepage;
            _audioTransport = audio;
            _preview = preview ?? throw new ArgumentNullException(nameof(preview));
            string current = _dataRoot;
            while (current != null && !Directory.Exists(Path.Combine(current, "workspace")))
                current = Directory.GetParent(current)?.FullName;
            _workspace = current == null ? null : Path.Combine(current, "workspace");
            _cache = Environment.GetEnvironmentVariable("PAL3_BATTLE_MOVIE_CACHE") ?? (_workspace == null ?
                Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/BattleMovies")) : Path.Combine(_workspace, "Cache", "BattleMovies"));
            string configured = Environment.GetEnvironmentVariable("PAL3_BATTLE_FFMPEG");
            string filename = Application.platform == RuntimePlatform.WindowsEditor ? "ffmpeg.exe" : "ffmpeg";
            _ffmpeg = configured ?? (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
                .Select(path => Path.Combine(path, filename)).FirstOrDefault(File.Exists) ?? (_workspace == null ? null : Path.Combine(_workspace, "Tools", "ffmpeg", "bin", filename));
        }

        public void Preload(string original)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BattleMoviePlayer));
            if (_prepared.ContainsKey(original))
                return;
            string configured = Environment.GetEnvironmentVariable("PAL3_BATTLE_MOVIE_ROOT");
            CancellationToken cancellation = _shutdown.Token;
            _prepared.Add(original, Task.Run(() => Prepare(original, configured, cancellation), cancellation));
        }

        internal void ReusePreparedMovies(BattleMoviePlayer previous)
        {
            foreach (var (name, task) in previous._prepared)
                if (task.IsCompletedSuccessfully)
                    _prepared.TryAdd(name, task);
        }

        public IEffectMovie Play(string original)
        {
            Preload(original);
            var movie = new Movie(this, original, _prepared[original]);
            _movies.Add(movie);
            return movie;
        }

        private static bool IsMediaFailure(Exception error) =>
            error is IOException or InvalidDataException or UnauthorizedAccessException or Win32Exception or OverflowException or TimeoutException;

        private Preparation Prepare(string original, string configured, CancellationToken cancellation)
        {
            try
            {
                byte[] bytes = ReadOriginal(original, configured, cancellation);
                return bytes == null ? new Preparation { Missing = true, Error = "未找到影片文件。" } :
                    new Preparation { Data = Decode(original, bytes, cancellation) };
            }
            catch (Exception exception) when (IsMediaFailure(exception))
            {
                return new Preparation { Error = exception.Message };
            }
        }

        private static Preparation CompletedPreparation(Task<Preparation> task)
        {
            if (task.IsCompletedSuccessfully)
                return task.GetAwaiter().GetResult();
            if (task.IsCanceled)
                return new Preparation { Error = "影片准备已取消。" };
            return new Preparation { Error = task.Exception.GetBaseException().Message };
        }

        private void ReportFailure(string name, Preparation result)
        {
            if (result.Error == null || !_reportedFailures.Add(name))
                return;
            if (result.Missing)
                MissingMovies.Add(name);
            Diagnostics.Add($"已跳过战斗影片 {name}：{result.Error}");
        }

        private byte[] ReadOriginal(string original, string configured, CancellationToken cancellation)
        {
            var roots = new List<string> { _dataRoot };
            if (!string.IsNullOrEmpty(configured))
                roots.Add(configured);
            if (_workspace != null)
                roots.Add(Path.Combine(Directory.GetParent(_workspace).FullName, BattleData.Variant + " Installation"));
            foreach (string root in roots.Distinct())
            {
                cancellation.ThrowIfCancellationRequested();
                string loose = Path.Combine(root, original.Replace('\\', Path.DirectorySeparatorChar));
                if (File.Exists(loose))
                    return File.ReadAllBytes(loose);
                string movieDirectory = Path.Combine(root, "movie");
                if (!Directory.Exists(movieDirectory))
                    continue;
                var hash = new Crc32Hash();
                hash.Init();
                var files = new CpkFileSystem(root, hash, _codepage);
                foreach (string name in new[] { "movie.cpk", "movie_end.cpk" })
                {
                    string[] archives = Directory.EnumerateFiles(movieDirectory)
                        .Where(file => Path.GetFileName(file).Equals(name, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
                    if (archives.Length == 0)
                        continue;
                    if (archives.Length > 1)
                        throw new InvalidDataException($"Multiple archives match {name} in {movieDirectory}.");
                    string archive = archives[0];
                    files.Mount(Path.GetRelativePath(root, archive), _codepage);
                    string path = name + "\\" + original;
                    if (files.FileExists(path))
                        return files.ReadAllBytes(path);
                }
            }
            return null;
        }

        private DecodedMovie Decode(string original, byte[] bytes, CancellationToken cancellation)
        {
            if (bytes.Length < 44 || bytes[0] != 'B' || bytes[1] != 'I' || bytes[2] != 'K')
                throw new InvalidDataException("Invalid original Bink header: " + original);
            var result = new DecodedMovie
            {
                Name = original,
                Frames = checked((int)BitConverter.ToUInt32(bytes, 8)),
                Width = checked((int)BitConverter.ToUInt32(bytes, 20)),
                Height = checked((int)BitConverter.ToUInt32(bytes, 24)),
                Rate = BitConverter.ToUInt32(bytes, 28),
                Divisor = BitConverter.ToUInt32(bytes, 32),
                HasAudio = BitConverter.ToUInt32(bytes, 40) > 0
            };
            if (result.Frames <= 0 || result.Width <= 0 || result.Height <= 0 || result.Rate == 0 || result.Divisor == 0)
                throw new InvalidDataException("Invalid original Bink frame clock: " + original);
            string hash;
            using (SHA256 algorithm = SHA256.Create())
                hash = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            string entry = Path.Combine(_cache, hash);
            result.Directory = Path.Combine(entry, "rgba-pcm-v1");
            if (TryReadPublishedMovie(result, out _))
                return result;
            SemaphoreSlim decodeLease = DecodeLeases.GetOrAdd(entry, _ => new SemaphoreSlim(1));
            if (!decodeLease.Wait(CacheWait, cancellation))
                throw new TimeoutException("Timed out waiting for the battle movie decoder.");
            try
            {
                Directory.CreateDirectory(entry);
                using FileStream lease = LockCacheEntry(entry, cancellation);
                if (TryReadPublishedMovie(result, out string cacheError))
                    return result;
                if (cacheError != null)
                    File.Delete(Path.Combine(result.Directory, "complete"));
                if (_ffmpeg == null || !File.Exists(_ffmpeg))
                {
                    const string missingDecoder = "Set PAL3_BATTLE_FFMPEG to a local ffmpeg binary for original Bink playback.";
                    throw new FileNotFoundException(cacheError == null ? missingDecoder : cacheError + " " + missingDecoder, _ffmpeg);
                }
                string staging = Path.Combine(entry, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(staging);
                try
                {
                    DecodeInto(staging, original, bytes, result.HasAudio, cancellation);
                    cancellation.ThrowIfCancellationRequested();
                    ValidateDecodedFiles(result, staging);
                    File.WriteAllText(Path.Combine(staging, "complete"), "Original Bink SHA256 " + hash);
                    if (Directory.Exists(result.Directory))
                        Directory.Delete(result.Directory, recursive: true);
                    Directory.Move(staging, result.Directory);
                }
                finally
                {
                    if (Directory.Exists(staging))
                        Directory.Delete(staging, recursive: true);
                }
            }
            finally
            {
                decodeLease.Release();
            }
            return result;
        }

        private static bool TryReadPublishedMovie(DecodedMovie movie, out string error)
        {
            error = null;
            if (!File.Exists(Path.Combine(movie.Directory, "complete")))
                return false;
            try
            {
                ValidateDecodedFiles(movie, movie.Directory);
                return true;
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException)
            {
                error = exception.Message;
                return false;
            }
        }

        private void DecodeInto(string directory, string original, byte[] bytes, bool hasAudio, CancellationToken cancellation)
        {
            string input = Path.Combine(directory, "original.bik");
            File.WriteAllBytes(input, bytes);
            var startInfo = new ProcessStartInfo(_ffmpeg)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            string[] videoArguments =
            {
                "-nostdin", "-hide_banner", "-loglevel", "error", "-y", "-i", input,
                "-map", "0:v:0", "-pix_fmt", "rgba", "-f", "rawvideo", Path.Combine(directory, "video.rgba")
            };
            foreach (string argument in videoArguments)
                startInfo.ArgumentList.Add(argument);
            if (hasAudio)
            {
                string[] audioArguments = { "-map", "0:a:0", "-ac", "2", "-ar", "48000", "-c:a", "pcm_s16le", Path.Combine(directory, "audio.wav") };
                foreach (string argument in audioArguments)
                    startInfo.ArgumentList.Add(argument);
            }
            using var process = new Process { StartInfo = startInfo };
            RunDecoder(process, original, DecodeWait, cancellation);
        }

        private static void RunDecoder(Process process, string original, TimeSpan timeout, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            process.Start();
            try
            {
                Task<string> stderr = process.StandardError.ReadToEndAsync();
                Stopwatch elapsed = Stopwatch.StartNew();
                while (!process.WaitForExit(50))
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (elapsed.Elapsed >= timeout)
                        throw new TimeoutException("ffmpeg timed out decoding " + original);
                }
                int remaining = (int)Math.Max(0, Math.Ceiling((timeout - elapsed.Elapsed).TotalMilliseconds));
                if (Task.WaitAny(new Task[] { stderr }, remaining, cancellation) < 0)
                    throw new TimeoutException("ffmpeg did not close its output for " + original);
                string errors = stderr.GetAwaiter().GetResult();
                if (process.ExitCode != 0)
                    throw new InvalidDataException("ffmpeg could not decode " + original + ": " + errors);
            }
            finally
            {
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (InvalidOperationException) when (process.HasExited)
                    {
                        // It exited just before the cancellation reached it.
                    }
                    process.WaitForExit(1000);
                }
            }
        }

        private static FileStream LockCacheEntry(string directory, CancellationToken cancellation)
        {
            // The cache is shared by Unity processes. The OS releases the writer lease
            // even if a batch job terminates while FFmpeg is producing the files.
            var lease = new FileStream(Path.Combine(directory, "decode.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.ReadWrite);
            try
            {
                Stopwatch wait = Stopwatch.StartNew();
                while (true)
                {
                    cancellation.ThrowIfCancellationRequested();
                    try
                    {
                        lease.Lock(0, 1);
                        return lease;
                    }
                    catch (IOException exception)
                    {
                        if (wait.Elapsed >= CacheWait)
                            throw new TimeoutException("Timed out waiting for the battle movie cache writer.", exception);
                        cancellation.WaitHandle.WaitOne(50);
                    }
                }
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }

        private static void CheckFrameCount(DecodedMovie movie, long bytes)
        {
            if (bytes != (long)movie.FrameBytes * movie.Frames)
                throw new InvalidDataException("Decoded Bink frame count differs from its original header.");
        }

        private static void ValidateDecodedFiles(DecodedMovie movie, string directory)
        {
            CheckFrameCount(movie, new FileInfo(Path.Combine(directory, "video.rgba")).Length);
            if (!movie.HasAudio)
                return;
            using var stream = File.OpenRead(Path.Combine(directory, "audio.wav"));
            using var reader = new BinaryReader(stream, Encoding.ASCII);
            _ = ReadPcmHeader(reader);
        }

        private static AudioClip ReadAudio(string path, bool silent)
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream, Encoding.ASCII);
            (int channels, int rate, int count) = ReadPcmHeader(reader);
            var samples = new float[count];
            if (!silent)
                for (int index = 0; index < samples.Length; index++)
                    samples[index] = reader.ReadInt16() / 32768f;
            AudioClip clip = AudioClip.Create("Original combat movie", samples.Length / channels, channels, rate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }

        private static (int channels, int rate, int samples) ReadPcmHeader(BinaryReader reader)
        {
            Stream stream = reader.BaseStream;
            if (new string(reader.ReadChars(4)) != "RIFF")
                throw new InvalidDataException("Invalid decoded PCM header.");
            reader.ReadUInt32();
            if (new string(reader.ReadChars(4)) != "WAVE")
                throw new InvalidDataException("Invalid decoded PCM format.");
            int channels = 0, rate = 0, bits = 0;
            while (stream.Position + 8 <= stream.Length)
            {
                string name = new(reader.ReadChars(4));
                uint count = reader.ReadUInt32();
                long end = checked(stream.Position + count + (count & 1));
                if (end > stream.Length)
                    throw new InvalidDataException("Invalid decoded PCM chunk length.");
                if (name == "fmt ")
                {
                    if (count < 16)
                        throw new InvalidDataException("Invalid decoded PCM format chunk.");
                    if (reader.ReadUInt16() != 1)
                        throw new InvalidDataException("Expected signed 16-bit PCM from the local decoder.");
                    channels = reader.ReadUInt16();
                    rate = reader.ReadInt32();
                    reader.ReadUInt32();
                    reader.ReadUInt16();
                    bits = reader.ReadUInt16();
                }
                else if (name == "data")
                {
                    if (count == 0 || channels is not (1 or 2) || rate <= 0 || bits != 16 || count % (channels * 2) != 0)
                        throw new InvalidDataException("Invalid decoded PCM sample data.");
                    return (channels, rate, (int)(count / 2));
                }
                stream.Position = end;
            }
            throw new InvalidDataException("Decoded movie has no PCM sample chunk.");
        }

        public void Advance(double unscaledSeconds)
        {
            foreach (var (name, preparation) in _prepared)
                if (preparation.IsCompleted && !_reportedFailures.Contains(name))
                    ReportFailure(name, CompletedPreparation(preparation));

            double elapsed = unscaledSeconds == (double)(float)Battle.BattleSession.StepSeconds ? Battle.BattleSession.StepSeconds : unscaledSeconds;
            foreach (Movie movie in _movies)
            {
                try
                {
                    movie.Advance(elapsed);
                }
                catch (Exception exception) when (IsMediaFailure(exception))
                {
                    movie.Dispose();
                    var failure = new Preparation { Error = exception.Message };
                    _prepared[movie.Name] = Task.FromResult(failure);
                    ReportFailure(movie.Name, failure);
                }
            }
            for (int index = _movies.Count - 1; index >= 0; index--)
                if (_movies[index].Finished)
                {
                    _movies[index].Dispose();
                    _movies.RemoveAt(index);
                }
        }

        public bool Skip()
        {
            Movie movie = _movies.LastOrDefault(item => !item.Finished);
            if (movie == null)
                return false;
            movie.Dispose();
            _movies.Remove(movie);
            return true;
        }

        public void Render(Camera camera)
        {
            Movie movie = _movies.LastOrDefault(item => !item.Finished);
            if (movie?.Texture == null)
            {
                if (_overlay != null)
                    _overlay.enabled = false;
                return;
            }
            if (_overlay == null)
                CreateOverlay(camera);

            _overlay.enabled = true;
            _frameMaterial.mainTexture = movie.Texture;
            float distance = camera.nearClipPlane + 0.001f;
            float height = camera.orthographic ? camera.orthographicSize : distance * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            _overlay.transform.localPosition = new Vector3(0, 0, distance);
            _overlay.transform.localScale = new Vector3(height * camera.aspect, height, 1);

            float aspect = movie.Texture.width / (float)movie.Texture.height;
            var scale = new Vector2(Mathf.Min(1, aspect / camera.aspect), Mathf.Min(1, camera.aspect / aspect));
            if (!_frameScale.Equals(scale))
            {
                _frameScale = scale;
                SetQuad(4, scale.x, scale.y);
                _screen.vertices = _screenVertices;
            }
        }

        private void CreateOverlay(Camera camera)
        {
            _frameMaterial = new Material(Shader.Find("Pal3/Effect")) { hideFlags = HideFlags.DontSave, renderQueue = 5000 };
            _frameMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
            _frameMaterial.SetInt("_DstBlend", (int)BlendMode.Zero);
            _frameMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
            _frameMaterial.SetInt("_Cull", (int)CullMode.Off);
            _backdrop = new Material(_frameMaterial)
            {
                hideFlags = HideFlags.DontSave,
                renderQueue = 4999,
                mainTexture = Texture2D.blackTexture
            };

            SetQuad(0, 1, 1);
            _screen = new Mesh { hideFlags = HideFlags.DontSave, vertices = _screenVertices };
            var uv = new[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0) };
            _screen.uv = uv.Concat(uv).ToArray();
            _screen.colors = Enumerable.Repeat(Color.white, _screenVertices.Length).ToArray();
            _screen.subMeshCount = 2;
            _screen.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            _screen.SetTriangles(new[] { 4, 5, 6, 4, 6, 7 }, 1);

            GameObject surface = _preview.CreateObject("Battle movie overlay");
            // Parent to the preview camera so the overlay follows its pose and stays out of stage framing.
            surface.transform.SetParent(camera.transform, false);
            surface.layer = _preview.Root.layer;
            surface.AddComponent<MeshFilter>().sharedMesh = _screen;
            _overlay = surface.AddComponent<MeshRenderer>();
            // URP compares renderer sorting order before material queue.
            _overlay.sortingOrder = short.MaxValue;
            _overlay.shadowCastingMode = ShadowCastingMode.Off;
            _overlay.receiveShadows = false;
            _overlay.sharedMaterials = new[] { _backdrop, _frameMaterial };
        }

        private void SetQuad(int offset, float halfWidth, float halfHeight)
        {
            _screenVertices[offset] = new Vector3(-halfWidth, -halfHeight, 0);
            _screenVertices[offset + 1] = new Vector3(halfWidth, -halfHeight, 0);
            _screenVertices[offset + 2] = new Vector3(halfWidth, halfHeight, 0);
            _screenVertices[offset + 3] = new Vector3(-halfWidth, halfHeight, 0);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _shutdown.Cancel();
            // Workers own plain files and processes. Closing a preview must never join them on the UI thread.
            _ = Task.WhenAll(_prepared.Values).ContinueWith(done =>
            {
                _ = done.Exception;
                _shutdown.Dispose();
            }, TaskScheduler.Default);
            foreach (Movie movie in _movies)
                movie.Dispose();
            _movies.Clear();
            if (_overlay != null)
                Object.DestroyImmediate(_overlay.gameObject);
            if (_frameMaterial != null)
                Object.DestroyImmediate(_frameMaterial);
            if (_backdrop != null)
                Object.DestroyImmediate(_backdrop);
            if (_screen != null)
                Object.DestroyImmediate(_screen);
        }
    }
}
