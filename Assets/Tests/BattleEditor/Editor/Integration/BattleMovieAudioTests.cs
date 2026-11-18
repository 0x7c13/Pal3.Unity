// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Battle;
    using Battle.Presentation;
    using Game.Rendering.Native;
    using NUnit.Framework;
    using Pal3.Tests.EditorSupport;
    using ResourceBrowser;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    [Category("LocalGameData")]
    public sealed class BattleMovieAudioTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private BattleData _data;
        private ResourceModelPreview _preview;
        private BattleMoviePlayer _movies;
        private object _movie;
        private string _directory;

        private static Type Device => typeof(BattleAudioPreview).Assembly.GetType("Pal3.BattleEditor.BattleAudioPreviewDevice", true);

        private static object Call(string name, params object[] arguments) => Device.GetMethod(name, Static).Invoke(null, arguments);

        [SetUp]
        public void SetUp()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (root == null)
                Assert.Ignore("The editor transport tests require local game resources.");
            _data = new BattleData(root, 936);
            _preview = new ResourceModelPreview(_data.Resources, false);
            _movies = new BattleMoviePlayer(_data, _preview) { AudioPlaybackEnabled = false };
            // A named, silent transport fixture exercises the actual editor
            // audio device in both variants without substituting absent A media.
            _directory = Path.Combine(Environment.GetEnvironmentVariable("PAL3_BATTLE_REPORT_DIR") ?? Application.temporaryCachePath, "AudioTransport-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            var frames = new byte[4 * 3 * 4 * 300];
            for (int pixel = 0; pixel < 4 * 3; pixel++)
            {
                frames[pixel * 4] = 255;
                frames[pixel * 4 + 3] = 255;
            }
            File.WriteAllBytes(Path.Combine(_directory, "video.rgba"), frames);
            using (var writer = new BinaryWriter(File.Create(Path.Combine(_directory, "audio.wav"))))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + 48000 * 10 * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)1);
                writer.Write(48000);
                writer.Write(96000);
                writer.Write((ushort)2);
                writer.Write((ushort)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(48000 * 10 * 2);
                writer.Write(new byte[48000 * 10 * 2]);
            }
            _movie = AddMovie(_movies);
        }

        private object AddMovie(BattleMoviePlayer player)
        {
            Type decodedType = typeof(BattleMoviePlayer).GetNestedType("DecodedMovie", BindingFlags.NonPublic);
            object decoded = Activator.CreateInstance(decodedType, true);
            foreach (var pair in new[] { ("Width", 4), ("Height", 3), ("Frames", 300) })
                decodedType.GetField(pair.Item1, Members).SetValue(decoded, pair.Item2);
            decodedType.GetField("Directory", Members).SetValue(decoded, _directory);
            decodedType.GetField("Name", Members).SetValue(decoded, "fixture.bik");
            decodedType.GetField("Rate", Members).SetValue(decoded, 30u);
            decodedType.GetField("Divisor", Members).SetValue(decoded, 1u);
            decodedType.GetField("HasAudio", Members).SetValue(decoded, true);
            object movie = Activator.CreateInstance(typeof(BattleMoviePlayer).GetNestedType("Movie", BindingFlags.NonPublic), Members, null,
                new[] { (object)player, decoded }, null);
            ((IList)typeof(BattleMoviePlayer).GetField("_movies", Members).GetValue(player)).Add(movie);
            return movie;
        }

        [TearDown]
        public void TearDown()
        {
            _movies?.Dispose();
            _preview?.Dispose();
            _data?.Dispose();
            if (_directory != null)
                Directory.Delete(_directory, true);
        }

        [Test]
        public void ModalMovieCoversSortedBattleGeometryUntilSkipped()
        {
            _preview.SetCamera(new Vector3(0, 0, -5), Vector3.zero, 40);
            var geometry = _preview.CreateObject("Battle geometry behind modal video");
            var quad = new Mesh
            {
                vertices = new[] { new Vector3(-2, -2, 0), new Vector3(2, -2, 0), new Vector3(2, 2, 0), new Vector3(-2, 2, 0) },
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                colors = new[] { Color.green, Color.green, Color.green, Color.green },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            var material = new Material(Shader.Find("Pal3/Effect")) { renderQueue = 3003 };
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            geometry.AddComponent<MeshFilter>().sharedMesh = quad;
            var renderer = geometry.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = GameBoxBattleRenderOrder.DirectEffects;
            _preview.Rendering += camera => geometry.transform.SetPositionAndRotation(
                camera.transform.position + camera.transform.forward, camera.transform.rotation);

            var image = new Texture2D(420, 180);
            RenderTexture previous = RenderTexture.active;
            try
            {
                void ReadFrame()
                {
                    RenderTexture.active = _preview.Render(420, 180);
                    image.ReadPixels(new Rect(0, 0, 420, 180), 0, 0);
                    image.Apply();
                }

                ReadFrame();
                Assert.That(image.GetPixel(20, 90).g, Is.EqualTo(1f).Within(.03), "The geometry fills the bar area.");
                _preview.Rendering += _movies.Render;
                ReadFrame();
                Color bar = image.GetPixel(20, 90), video = image.GetPixel(210, 90);
                Assert.That(bar.g, Is.Zero.Within(.03), $"Black bar at 20,90: {bar}");
                Assert.That(video.r, Is.EqualTo(1f).Within(.03), $"Red frame at 210,90: {video}");
                Assert.That(video.g, Is.Zero.Within(.03), $"Red frame at 210,90: {video}");

                Assert.That(_movies.Skip(), Is.True);
                ReadFrame();
                Assert.That(image.GetPixel(20, 90).g, Is.EqualTo(1f).Within(.03), "The stage returns after skipping.");
                Assert.That(image.GetPixel(210, 90).g, Is.EqualTo(1f).Within(.03));
                _preview.Frame();
                _preview.GetCamera(out _, out Vector3 target, out _);
                Assert.That(Vector3.Distance(target, renderer.bounds.center), Is.LessThan(.001f),
                    "The movie overlay must stay outside the stage's framing bounds.");
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(geometry);
                Object.DestroyImmediate(quad);
                Object.DestroyImmediate(material);
            }
        }

        [TestCase(320, 240)]
        [TestCase(420, 180)]
        [TestCase(300, 150)]
        [TestCase(240, 240)]
        public void MovieKeepsItsAspectAndFillsTheUnusedCanvasWithBlack(int width, int height)
        {
            _preview.Rendering += _movies.Render;
            RenderTexture target = _preview.Render(width, height);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(width, height);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                int frameWidth = Mathf.Min(width, height * 4 / 3);
                int frameHeight = Mathf.Min(height, width * 3 / 4);
                int x = (width - frameWidth) / 2;
                int y = (height - frameHeight) / 2;

                void AssertPixel(int sampleX, int sampleY, bool visible)
                {
                    Color color = image.GetPixel(sampleX, sampleY);
                    Assert.That(color.r, Is.EqualTo(visible ? 1f : 0f).Within(.03), $"Red at {sampleX},{sampleY}");
                    Assert.That(color.g, Is.Zero.Within(.03));
                    Assert.That(color.b, Is.Zero.Within(.03));
                }

                AssertPixel(x + 2, height / 2, true);
                AssertPixel(width / 2, y + 2, true);
                if (x > 0)
                {
                    AssertPixel(x - 2, height / 2, false);
                    AssertPixel(width - x + 2, height / 2, false);
                }
                if (y > 0)
                {
                    AssertPixel(width / 2, y - 2, false);
                    AssertPixel(width / 2, height - y + 2, false);
                }
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }

        private static void RequireAudioPreviewApi()
        {
            if (!(bool)Device.GetProperty("IsAvailable", Static).GetValue(null))
                Assert.Ignore("This editor does not expose Unity's audio preview controls.");
        }

        private static IEnumerator RequireRunningAudioClock()
        {
            RequireAudioPreviewApi();
            var owner = new object();
            AudioClip silent = AudioClip.Create("audio clock probe", 48000, 1, 48000, false);
            try
            {
                Assert.That(Call("Play", owner, silent, 0, false), Is.True);
                int first = (int)Call("Position", owner);
                int now = first;
                double timeout = EditorApplication.timeSinceStartup + .5;
                while (now <= Math.Max(0, first) && EditorApplication.timeSinceStartup < timeout)
                {
                    yield return null;
                    now = (int)Call("Position", owner);
                }
                if (now <= Math.Max(0, first))
                    Assert.Ignore("The editor audio preview backend has no advancing sample clock.");
            }
            finally
            {
                Call("Stop", owner);
                Object.DestroyImmediate(silent);
            }
        }

        [UnityTest]
        public IEnumerator PauseFreezesTheRealAudioTransportAndResumeSeeksToTheVideoClock()
        {
            yield return RequireRunningAudioClock();
            _movies.AudioPlaybackEnabled = true;
            _movies.Advance(.8);
            _movies.AudioPlaybackEnabled = false;
            Assert.That((bool)_movie.GetType().GetField("_audioPaused", Members).GetValue(_movie), Is.True);
            int start = (int)Call("Position", _movie);
            Assert.That(start, Is.GreaterThanOrEqualTo(0));
            double until = EditorApplication.timeSinceStartup + .15;
            while (EditorApplication.timeSinceStartup < until)
                yield return null;
            int paused = (int)Call("Position", _movie);
            Assert.That(Math.Abs(paused - start), Is.LessThanOrEqualTo(2048), "The real preview transport must stop advancing when the editor pauses.");
            _movies.Advance(.4); // Silent single-step/inspection may move video.
            _movies.AudioPlaybackEnabled = true;
            Assert.That((bool)_movie.GetType().GetField("_audioPaused", Members).GetValue(_movie), Is.False);
            yield return null;
            int resumed = (int)Call("Position", _movie);
            Assert.That(resumed, Is.InRange((int)(1.1 * 48000), (int)(1.4 * 48000)), "Resume must seek to the current video sample rather than restarting or retaining the old sample.");
        }

        [Test]
        public void MovieOwnsOneTransportAndFormerSoundOwnersCannotStopIt()
        {
            RequireAudioPreviewApi();
            AudioClip clip = AudioClip.Create("silent ownership probe", 48000, 1, 48000, false);
            var soundOwner = new object();
            try
            {
                Assert.That(Call("Play", soundOwner, clip, 0, false), Is.True);
                _movies.AudioPlaybackEnabled = true;
                Assert.That(Call("Owns", _movie), Is.True);
                Call("Stop", soundOwner);
                Assert.That(Call("Owns", _movie), Is.True);
                Assert.That(Call("Play", soundOwner, clip, 0, false), Is.False, "A pending SFX decode cannot replace a modal movie soundtrack.");
                _movies.Muted = true;
                Assert.That(Call("Owns", _movie), Is.False, "Muting must release audio immediately without requiring a simulation step.");
                Assert.That(Call("Play", soundOwner, clip, 0, false), Is.True);
                _movies.Dispose();
                Assert.That(Call("Owns", soundOwner), Is.True, "Disposing a former movie owner must preserve the new sound owner.");
            }
            finally
            {
                Call("Stop", soundOwner);
                Object.DestroyImmediate(clip);
            }
        }

        [TestCase(PlayModeStateChange.ExitingEditMode)]
        [TestCase(PlayModeStateChange.ExitingPlayMode)]
        public void ModeTransitionReleasesAnAbandonedPreviewLease(PlayModeStateChange change)
        {
            var abandoned = new object();
            Device.GetField("_owner", Static).SetValue(null, abandoned);
            Device.GetField("_exclusive", Static).SetValue(null, true);
            try
            {
                Call("OnPlayModeStateChanged", change);
                Assert.That(Call("Owns", abandoned), Is.False);
                Assert.That(Device.GetField("_exclusive", Static).GetValue(null), Is.False);
            }
            finally
            {
                Call("Reset");
            }
        }

        private sealed class UnsupportedMovieAudio : IBattleMovieAudioTransport
        {
            private readonly Exception _error;

            public int Attempts
            {
                get; private set;
            }

            public UnsupportedMovieAudio(Exception error) => _error = error;

            public void SynchronizeMovie(object owner, AudioClip clip, double elapsed, bool playing)
            {
                if (playing)
                {
                    Attempts++;
                    throw _error;
                }
            }

            public void StopMovie(object owner)
            {
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnavailableAudioBackendLeavesTheVideoRunningAndReportsOnce(bool reflectionFailure)
        {
            Exception unsupported = new NotSupportedException("Audio backend unavailable");
            if (reflectionFailure)
                unsupported = new TargetInvocationException(new TypeInitializationException("AudioUtil", unsupported));
            var transport = new UnsupportedMovieAudio(unsupported);
            using var player = new BattleMoviePlayer(_data, _preview, transport) { AudioPlaybackEnabled = false };
            object movie = AddMovie(player);

            player.AudioPlaybackEnabled = true;
            player.Advance(.5);
            Assert.That((double)movie.GetType().GetField("Elapsed", Members).GetValue(movie), Is.EqualTo(.5));
            Assert.That(player.BlocksBattleTime, Is.True);
            Assert.That(player.HasDecodedFrame, Is.True);
            Assert.That(transport.Attempts, Is.EqualTo(1));
            Assert.That(player.Diagnostics.Single(), Does.Contain("fixture.bik").And.Contain("Audio backend unavailable"));

            player.Advance(10);
            Assert.That(player.BlocksBattleTime, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingConfiguredRootStillSearchesInstallationAndReportsAbsentMovies(bool installed)
        {
            const string option = "PAL3_BATTLE_MOVIE_ROOT";
            string saved = Environment.GetEnvironmentVariable(option);
            string name = Guid.NewGuid().ToString("N") + ".bik";
            try
            {
                Environment.SetEnvironmentVariable(option, Path.Combine(_directory, "missing-override"));
                typeof(BattleMoviePlayer).GetField("_workspace", Members).SetValue(_movies, Path.Combine(_directory, "workspace"));
                if (installed)
                {
                    string install = Directory.CreateDirectory(Path.Combine(_directory, BattleData.Variant + " Installation")).FullName;
                    // The invalid Bink header identifies the file that was found without invoking a decoder.
                    File.WriteAllBytes(Path.Combine(install, name), new byte[4]);
                }
                PrepareMovie(_movies, name);
                _movies.Advance(0);
                Assert.That(_movies.MissingMovies.Contains(name), Is.EqualTo(!installed));
                Assert.That(_movies.Diagnostics.Single(), Does.Contain(installed ? "Invalid original Bink header" : "未找到影片文件"));
            }
            finally
            {
                Environment.SetEnvironmentVariable(option, saved);
            }
        }

        [TestCase("PAL3_BATTLE_FFMPEG")]
        [TestCase("video.rgba")]
        [TestCase("audio.wav")]
        [TestCase("Invalid decoded PCM")]
        public void UnavailableDecoderOrBrokenCacheSkipsTheMovieAndExplainsWhy(string expectedDiagnostic)
        {
            const string movie = "decoder-fixture.bik";
            byte[] bytes = new byte[44];
            bytes[0] = (byte)'B';
            bytes[1] = (byte)'I';
            bytes[2] = (byte)'K';
            foreach (int offset in new[] { 8, 20, 24, 28, 32 })
                BitConverter.GetBytes(1u).CopyTo(bytes, offset);
            if (expectedDiagnostic is "audio.wav" or "Invalid decoded PCM")
                BitConverter.GetBytes(1u).CopyTo(bytes, 40);
            File.WriteAllBytes(Path.Combine(_directory, movie), bytes);
            string cache = Path.Combine(_directory, "cache");
            string ready = null;
            if (expectedDiagnostic != "PAL3_BATTLE_FFMPEG")
            {
                using var sha = System.Security.Cryptography.SHA256.Create();
                string hash = BitConverter.ToString(sha.ComputeHash(bytes))
                    .Replace("-", "").ToLowerInvariant();
                string entry = Path.Combine(cache, hash, "rgba-pcm-v1");
                Directory.CreateDirectory(entry);
                ready = Path.Combine(entry, "complete");
                File.WriteAllText(ready, "");
                if (expectedDiagnostic is "audio.wav" or "Invalid decoded PCM")
                    File.WriteAllBytes(Path.Combine(entry, "video.rgba"), new byte[4]);
                if (expectedDiagnostic == "Invalid decoded PCM")
                {
                    byte[] emptyWave = File.ReadAllBytes(Path.Combine(_directory, "audio.wav"))[..44];
                    BitConverter.GetBytes(36).CopyTo(emptyWave, 4);
                    BitConverter.GetBytes(0).CopyTo(emptyWave, 40);
                    File.WriteAllBytes(Path.Combine(entry, "audio.wav"), emptyWave);
                }
            }
            const string rootKey = "PAL3_BATTLE_MOVIE_ROOT";
            const string cacheKey = "PAL3_BATTLE_MOVIE_CACHE";
            const string decoderKey = "PAL3_BATTLE_FFMPEG";
            string previousRoot = Environment.GetEnvironmentVariable(rootKey);
            string previousCache = Environment.GetEnvironmentVariable(cacheKey);
            string previousDecoder = Environment.GetEnvironmentVariable(decoderKey);
            try
            {
                Environment.SetEnvironmentVariable(rootKey, _directory);
                Environment.SetEnvironmentVariable(cacheKey, cache);
                Environment.SetEnvironmentVariable(decoderKey, Path.Combine(_directory, "missing-ffmpeg"));
                using var player = new BattleMoviePlayer(_data, _preview);
                PrepareMovie(player, movie);
                using var first = player.Play(movie);
                using var repeat = player.Play(movie);
                player.Advance(0);
                Assert.That(first.Finished && repeat.Finished, Is.True);
                Assert.That(player.BlocksBattleTime, Is.False);
                Assert.That(player.MissingMovies, Is.Empty, "The source was found.");
                Assert.That(player.Diagnostics.Single(), Does.Contain(movie).And.Contain(expectedDiagnostic));
                if (ready != null)
                    Assert.That(File.Exists(ready), Is.False, "A future preview must be able to rebuild the broken entry.");
            }
            finally
            {
                Environment.SetEnvironmentVariable(rootKey, previousRoot);
                Environment.SetEnvironmentVariable(cacheKey, previousCache);
                Environment.SetEnvironmentVariable(decoderKey, previousDecoder);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedPreparationEndsTheMovieAndReportsOnlyOnce(bool canceled)
        {
            const string name = "preparation-failed.bik";
            Type result = typeof(BattleMoviePlayer).GetNestedType("Preparation", BindingFlags.NonPublic);
            MethodInfo factory = typeof(Task).GetMethods().Single(method =>
                method.Name == (canceled ? nameof(Task.FromCanceled) : nameof(Task.FromException)) && method.IsGenericMethod);
            object reason = canceled ? new CancellationToken(true) : new InvalidOperationException("test movie preparation fault");
            object prepared = factory.MakeGenericMethod(result).Invoke(null, new[] { reason });
            using var player = new BattleMoviePlayer(_data, _preview);
            var cache = (IDictionary)typeof(BattleMoviePlayer).GetField("_prepared", Members).GetValue(player);
            cache.Add(name, prepared);

            using var first = player.Play(name);
            player.Advance(.02);
            Assert.That(first.Finished, Is.True);
            using var second = player.Play(name);
            player.Advance(.02);
            Assert.That(second.Finished, Is.True);
            Assert.That(player.BlocksBattleTime, Is.False);
            Assert.That(player.Diagnostics.Single(), Does.Contain(name).And.Contain(canceled ? "已取消" : "test movie preparation fault"));
        }

        private static void PrepareMovie(BattleMoviePlayer player, string name)
        {
            player.Preload(name);
            var pending = (IDictionary)typeof(BattleMoviePlayer).GetField("_prepared", Members).GetValue(player);
            Assert.That(((Task)pending[name]).Wait(TimeSpan.FromSeconds(30)), Is.True, "The fixture's movie preparation did not finish.");
        }

#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        [Test]
        public void ColdPlaybackReturnsWhileTheDecoderRunsAndDisposingCancelsIt()
        {
            const string movie = "slow-movie.bik";
            byte[] header = new byte[44];
            header[0] = (byte)'B';
            header[1] = (byte)'I';
            header[2] = (byte)'K';
            foreach (int offset in new[] { 8, 20, 24, 28, 32 })
                BitConverter.GetBytes(1u).CopyTo(header, offset);
            File.WriteAllBytes(Path.Combine(_directory, movie), header);
            string decoder = Path.Combine(_directory, "slow-decoder");
            File.WriteAllText(decoder, "#!/bin/sh\nprintf '%s\\n' \"$@\" > \"$0.args\"\n: > \"$0.started\"\nexec /bin/sleep 30\n");
            using (var chmod = System.Diagnostics.Process.Start("/bin/chmod", "+x \"" + decoder + "\""))
            {
                Assert.That(chmod.WaitForExit(2000), Is.True);
                Assert.That(chmod.ExitCode, Is.Zero);
            }

            const string rootKey = "PAL3_BATTLE_MOVIE_ROOT";
            const string cacheKey = "PAL3_BATTLE_MOVIE_CACHE";
            const string decoderKey = "PAL3_BATTLE_FFMPEG";
            string previousRoot = Environment.GetEnvironmentVariable(rootKey);
            string previousCache = Environment.GetEnvironmentVariable(cacheKey);
            string previousDecoder = Environment.GetEnvironmentVariable(decoderKey);
            try
            {
                Environment.SetEnvironmentVariable(rootKey, _directory);
                Environment.SetEnvironmentVariable(cacheKey, Path.Combine(_directory, "slow cache"));
                Environment.SetEnvironmentVariable(decoderKey, decoder);
                using var player = new BattleMoviePlayer(_data, _preview);
                using var playback = player.Play(movie);
                Assert.That(player.IsPreparingPlayback, Is.True);
                Assert.That(playback.Finished, Is.False);
                Assert.That(SpinWait.SpinUntil(() => File.Exists(decoder + ".started"), TimeSpan.FromSeconds(5)), Is.True);
                string[] arguments = File.ReadAllLines(decoder + ".args");
                int inputIndex = Array.IndexOf(arguments, "-i");
                Assert.That(inputIndex, Is.GreaterThanOrEqualTo(0));
                string input = arguments[inputIndex + 1];
                Assert.That(input, Does.StartWith(Path.Combine(_directory, "slow cache") + Path.DirectorySeparatorChar));
                Assert.That(Path.GetFileName(input), Is.EqualTo("original.bik"));
                Assert.That(arguments[^1], Is.EqualTo(Path.Combine(Path.GetDirectoryName(input), "video.rgba")));
                var pending = (IDictionary)typeof(BattleMoviePlayer).GetField("_prepared", Members).GetValue(player);
                var preparation = (Task)pending[movie];
                Assert.That(preparation.IsCompleted, Is.False);
                Assert.That(player.Skip(), Is.True);
                Assert.That(player.BlocksBattleTime, Is.False);
                player.Dispose();
                Assert.That(SpinWait.SpinUntil(() => preparation.IsCompleted, TimeSpan.FromSeconds(5)), Is.True);
                Assert.That(preparation.IsCanceled, Is.True);
            }
            finally
            {
                Environment.SetEnvironmentVariable(rootKey, previousRoot);
                Environment.SetEnvironmentVariable(cacheKey, previousCache);
                Environment.SetEnvironmentVariable(decoderKey, previousDecoder);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StalledDecoderIsStoppedOnTimeoutOrCancellation(bool cancel)
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo("/bin/sleep", "30")
                {
                    UseShellExecute = false,
                    RedirectStandardError = true
                }
            };
            using var cancellation = new CancellationTokenSource();
            if (cancel)
                cancellation.CancelAfter(TimeSpan.FromMilliseconds(250));
            MethodInfo run = typeof(BattleMoviePlayer).GetMethod("RunDecoder", Static);
            TimeSpan timeout = TimeSpan.FromSeconds(cancel ? 4 : .15);
            var error = Assert.Throws<TargetInvocationException>(() => run.Invoke(null, new object[]
            {
                process, "stalled.bik", timeout, cancellation.Token
            }));
            Assert.That(error.InnerException, Is.TypeOf(cancel ? typeof(OperationCanceledException) : typeof(TimeoutException)));
            Assert.That(process.HasExited, Is.True);
        }
#endif

        [Test]
        public void CompletingAMovieReleasesItsFrameAudioAndOwnerEntry()
        {
            Texture texture = _movies.CurrentTexture;
            _movies.Advance(11);
            Assert.That(_movies.BlocksBattleTime, Is.False);
            Assert.That(_movies.CurrentTexture, Is.Null);
            Assert.That(texture == null, Is.True);
            Assert.That(((IList)typeof(BattleMoviePlayer).GetField("_movies", Members).GetValue(_movies)).Count, Is.Zero);
        }

        [Test]
        public void SkippingAMovieReleasesItsTextureAndAudioImmediately()
        {
            RequireAudioPreviewApi();
            _movies.AudioPlaybackEnabled = true;
            Assert.That(Call("Owns", _movie), Is.True);
            Texture texture = _movies.CurrentTexture;
            Assert.That(_movies.Skip(), Is.True);
            Assert.That(_movies.Skip(), Is.False);
            Assert.That(_movies.BlocksBattleTime, Is.False);
            Assert.That(_movies.CurrentTexture, Is.Null);
            Assert.That(texture == null, Is.True);
            Assert.That(Call("Owns", _movie), Is.False);
        }

        [TestCase(false)]
#if PAL3
        [TestCase(true)]
#endif
        public void EscapeInTheWorkbenchSkipsTheMovieAndReleasesThePausedBattleFrame(bool originalMovie)
        {
            var window = ScriptableObject.CreateInstance<BattleEditorWindow>();
            try
            {
                window.position = new Rect(0, 0, 1000, 720);
                Type windowType = typeof(BattleEditorWindow);
                var session = (BattleEditorSession)windowType.GetField("_session", Members).GetValue(window);
                windowType.GetMethod("ConnectData", Members).Invoke(window, new object[] { _data.Resources.Root, 936 });
                var draft = (BattleScenarioAsset)windowType.GetField("_draft", Members).GetValue(window);
                draft.Scenario = Scenario(session.Data);
                windowType.GetMethod("UpdateDirty", Members).Invoke(window, null);
                windowType.GetMethod("StartBattle", Members).Invoke(window, null);
                Assert.That(session.Battle, Is.Not.Null);
                session.IsPlaying = false;

                var player = (BattleMoviePlayer)typeof(BattleEffectDirector).GetField("_movies", Members).GetValue(session.Preview.Effects);
                if (originalMovie)
                {
                    const string name = @"2DEFFECT\t102.bik";
                    PrepareMovie(player, name);
                    player.Play(name);
                }
                else
                    AddMovie(player);
                Assert.That(player.Diagnostics, Is.Empty);
                session.StepBattle();
                Assert.That(session.Battle.IsModalPresentation, Is.True);
                Assert.That(session.Preview.Trail.Suspended, Is.True);
                Assert.That(session.Battle.SimulationTick, Is.Zero);
                var draw = (Action)windowType.GetMethod("DrawWindow", Members).CreateDelegate(typeof(Action), window);
                using var gui = new OffscreenGui(draw);
                gui.Paint(window.position.size);
                if (gui.RenderTarget != null)
                {
                    Color32[] moviePixels = ReadCanvasPixels(gui.RenderTarget);
                    windowType.GetField("_debugBattle", Members).SetValue(window, true);
                    gui.Paint(window.position.size);
                    Assert.That(ReadCanvasPixels(gui.RenderTarget).SequenceEqual(moviePixels), Is.True,
                        "Debug actor labels must stay behind the modal movie.");
                }

                foreach (var tab in new[]
                {
                    new Event { type = EventType.KeyDown, keyCode = KeyCode.Tab, character = '\t' },
                    new Event { type = EventType.KeyDown, character = '\t' }
                })
                {
                    gui.SendEvent(tab, window.position.size);
                    Assert.That(tab.type, Is.EqualTo(EventType.Used));
                }
                gui.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.F }, window.position.size);
                Assert.That(session.Battle.IsModalPresentation, Is.True);
                gui.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }, window.position.size);
                Assert.That(session.Battle.IsModalPresentation, Is.False);
                Assert.That(session.Preview.Trail.Suspended, Is.False);
                Assert.That(session.Battle.Inputs.Count(input => input.Kind == BattleInputKind.ModalPresentationSkip), Is.EqualTo(1));
                session.StepBattle();
                Assert.That(session.Battle.SimulationTick, Is.EqualTo(1));
            }
            finally
            {
                window.DiscardChanges();
                Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void ReplayWithADifferentMovieTimelinePreservesTheLiveBattleAndPlayback()
        {
            using var session = new BattleEditorSession();
            StartBattle(session);
            BattleSession original = session.Battle;
            BattlePreview preview = session.Preview;
            var player = (BattleMoviePlayer)typeof(BattleEffectDirector).GetField("_movies", Members).GetValue(preview.Effects);
            AddMovie(player);
            session.StepBattle();
            session.IsPlaying = true;
            Assert.That(original.IsModalPresentation, Is.True);
            Assert.That(original.SimulationTick, Is.Zero);

            var error = Assert.Throws<InvalidOperationException>(session.ReplayBattle);
            Assert.That(error.Message, Does.Contain("battle movies"));
            Assert.That(session.Battle, Is.SameAs(original));
            Assert.That(session.Preview, Is.SameAs(preview));
            Assert.That(session.IsPlaying, Is.True);
            Assert.That(original.IsModalPresentation, Is.True);
        }

        [Test]
        public void EditorPlaybackInspectionSpeedAndReplayHaveExplicitAudioBoundaries()
        {
            using var session = new BattleEditorSession();
            StartBattle(session);
            Assert.That(session.Preview.Effects.AudioPlaybackEnabled, Is.False);
            Assert.That(session.Preview.Audio.PlaybackEnabled, Is.False);
            session.IsPlaying = true;
            Assert.That(session.Preview.Effects.AudioPlaybackEnabled, Is.True);
            session.Speed = 2;
            Assert.That(session.Preview.Effects.AudioPlaybackEnabled, Is.False);
            session.Speed = 1;
            Assert.That(session.Preview.Effects.AudioPlaybackEnabled, Is.True);
            session.StepBattle();
            Assert.That(session.IsPlaying, Is.False);
            Assert.That(session.Preview.Effects.AudioPlaybackEnabled, Is.False);
            session.IsPlaying = true;
            session.AdvanceTo(BattleEventKind.Hit, 1);
            Assert.That(session.Preview.Effects.AudioPlaybackEnabled, Is.False);
            session.ReplayBattle();
            Assert.That(session.Preview.Effects.AudioPlaybackEnabled, Is.False);
            Assert.That(session.Preview.Audio.PlaybackEnabled, Is.False);
        }

        private static Color32[] ReadCanvasPixels(RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                return image.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }

        [TestCase(KeyCode.Return)]
        [TestCase(KeyCode.Escape)]
        public void HeldKeyboardActionsDoNotConsumeTwoHudPages(KeyCode key)
        {
            var window = ScriptableObject.CreateInstance<BattleEditorWindow>();
            try
            {
                window.position = new Rect(0, 0, 1000, 720);
                var session = (BattleEditorSession)typeof(BattleEditorWindow).GetField("_session", Members).GetValue(window);
                var hud = (BattleHudView)typeof(BattleEditorWindow).GetField("_hud", Members).GetValue(window);
                typeof(BattleEditorWindow).GetMethod("ConnectData", Members).Invoke(window, new object[] { _data.Resources.Root, 936 });
                var draft = (BattleScenarioAsset)typeof(BattleEditorWindow).GetField("_draft", Members).GetValue(window);
                draft.Scenario = Scenario(session.Data);
                draft.Scenario.Participants[0] = session.Data.CreateTestParticipant(101, BattleSide.Ally, BattleSlot.Water);
                typeof(BattleEditorWindow).GetMethod("UpdateDirty", Members).Invoke(window, null);
                typeof(BattleEditorWindow).GetMethod("StartBattle", Members).Invoke(window, null);
                string ally = draft.Scenario.Participants[0].InstanceId;
                string enemy = draft.Scenario.Participants[1].InstanceId;
                for (int step = 0; step < 5000 && session.Battle.WaitingActor != ally; step++)
                {
                    if (session.Battle.WaitingActor is { } waiting)
                        session.Battle.Submit(waiting, BattleAction.Defend);
                    session.StepBattle();
                }
                Assert.That(session.Battle.WaitingActor, Is.EqualTo(ally));
                var draw = (Action)typeof(BattleEditorWindow).GetMethod("DrawWindow", Members).CreateDelegate(typeof(Action), window);
                using var gui = new OffscreenGui(draw);
                gui.Paint(window.position.size);
                if (key == KeyCode.Escape)
                {
                    hud.Presenter.Open(BattleHudPage.Skills);
                    int skill = session.Battle.Definitions.Skills.Values
                        .First(ability => !ability.IsMagic && ability.Range == BattleTargetRange.OpponentSingle &&
                            session.Battle.CanSubmit(new BattleCommand(ally, BattleAction.Skill, enemy, ability.Id)) == null).Id;
                    Assert.That(hud.Presenter.ChooseAbility(skill), Is.True);
                    gui.Paint(window.position.size);
                }
                int inputs = session.Battle.RecordedInputCount;
                BattleHudPage expected = key == KeyCode.Return ? BattleHudPage.Targets : BattleHudPage.Skills;
                gui.SendEvent(new Event { type = EventType.KeyDown, keyCode = key }, window.position.size);
                Assert.That(hud.Presenter.Page, Is.EqualTo(expected));
                gui.SendEvent(new Event { type = EventType.KeyDown, keyCode = key }, window.position.size);
                Assert.That(hud.Presenter.Page, Is.EqualTo(expected));
                Assert.That(session.Battle.RecordedInputCount, Is.EqualTo(inputs));
                gui.SendEvent(new Event { type = EventType.KeyUp, keyCode = key }, window.position.size);
                gui.SendEvent(new Event { type = EventType.KeyDown, keyCode = key }, window.position.size);
                Assert.That(hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands));
                Assert.That(session.Battle.RecordedInputCount, Is.EqualTo(inputs + (key == KeyCode.Return ? 1 : 0)));
            }
            finally
            {
                window.DiscardChanges();
                Object.DestroyImmediate(window);
            }
        }

        private void StartBattle(BattleEditorSession session)
        {
            session.Load(_data.Resources.Root, 936);
            session.StartBattle(Scenario(session.Data));
        }

        private static BattleScenario Scenario(BattleData data)
        {
            var scenario = new BattleScenario { Variant = BattleData.Variant, Scene = data.Scenes.First(), DataFingerprint = data.Fingerprint };
            scenario.Participants.Add(data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(data, id));
            scenario.Participants.Add(data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            foreach (BattleParticipant actor in scenario.Participants)
                actor.Skills.Clear();
            return scenario;
        }
    }
}
