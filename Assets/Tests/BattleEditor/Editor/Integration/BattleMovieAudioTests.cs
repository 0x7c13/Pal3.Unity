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
    using Battle;
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
            File.WriteAllBytes(Path.Combine(_directory, "video.rgba"), new byte[2 * 2 * 4 * 300]);
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
            foreach (var pair in new[] { ("Width", 2), ("Height", 2), ("Frames", 300) })
                decodedType.GetField(pair.Item1, Members).SetValue(decoded, pair.Item2);
            decodedType.GetField("Directory", Members).SetValue(decoded, _directory);
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

        [UnityTest]
        public IEnumerator PauseFreezesTheRealAudioTransportAndResumeSeeksToTheVideoClock()
        {
            _movies.AudioPlaybackEnabled = true;
            _movies.Advance(.8);
            _movies.AudioPlaybackEnabled = false;
            Assert.That((bool)_movie.GetType().GetField("_audioPaused", Members).GetValue(_movie), Is.True);
            int start = (int)Call("Position", _movie);
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
                _movies.Preload(name);
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
                player.Preload(movie);
                using var first = player.Play(movie);
                using var repeat = player.Play(movie);
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
