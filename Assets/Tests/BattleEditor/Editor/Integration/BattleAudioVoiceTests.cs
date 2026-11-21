// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using ResourceBrowser;
    using Unity.Collections;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    [Category("LocalGameData")]
    public sealed class BattleAudioVoiceTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private ResourceCatalog _resources;
        private BattleAudioPreview _audio;
        private AudioClip _movie;
        private string _sound;
        private float _listenerVolume;
        private bool _listenerPause;

        private GameObject Root => (GameObject)typeof(BattleAudioPreview).GetField("_root", Members).GetValue(_audio);

        private AudioSource Source(string name) => Root.GetComponentsInChildren<AudioSource>().Single(source => source.clip.name == name);

        private AudioClip Silent(string name) => AudioClip.Create(name, 480000, 1, 48000, false);

        [SetUp]
        public void SetUp()
        {
            _listenerVolume = AudioListener.volume;
            _listenerPause = AudioListener.pause;
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (root == null)
                Assert.Ignore("Audio scene verification requires local game resources.");
            _resources = ResourceCatalog.OpenBaseData(root, 936);
            _audio = new BattleAudioPreview(_resources) { PlaybackEnabled = false };
            var files = (IDictionary<string, string>)typeof(BattleAudioPreview).GetField("_soundFiles", Members).GetValue(_audio);
            _sound = files.Keys.First();
            var clips = (IDictionary<string, AudioClip>)typeof(BattleAudioPreview).GetField("_clips", Members).GetValue(_audio);
            // Every actual device fixture is ten seconds of digital silence.
            // Real files supply only path lookup; no game sound is played.
            clips.Add(_sound, Silent("body fixture"));
            clips.Add("music:silent.mp3", Silent("music fixture"));
            _movie = Silent("movie fixture");
        }

        [TearDown]
        public void TearDown()
        {
            _audio?.Dispose();
            _resources?.Dispose();
            if (_movie != null)
                Object.DestroyImmediate(_movie);
            Assert.That(AudioListener.volume, Is.EqualTo(_listenerVolume));
            Assert.That(AudioListener.pause, Is.EqualTo(_listenerPause));
        }

        private static IEnumerator Wait(double seconds)
        {
            double until = EditorApplication.timeSinceStartup + seconds;
            while (EditorApplication.timeSinceStartup < until)
                yield return null;
        }

        [UnityTest]
        public IEnumerator PerVoiceStopPreservesMusicMovieAndOneShotOutput()
        {
            _audio.SetMusic("silent");
            _audio.PlayMovement("A", _sound, true);
            _audio.Play(_sound);
            var owner = new object();
            _audio.SynchronizeMovie(owner, _movie, 1, true);
            _audio.PlaybackEnabled = true;
            yield return Wait(.25);
            AudioSource music = Source("music fixture");
            AudioSource movie = Source("movie fixture");
            AudioSource oneShot = Root.GetComponentsInChildren<AudioSource>().Single(source => source.clip.name == "body fixture" && !source.loop);
            Assert.That(music.timeSamples, Is.GreaterThan(4800));
            Assert.That(movie.timeSamples, Is.GreaterThan(48000));
            Assert.That(oneShot.timeSamples, Is.GreaterThan(4800));
            _audio.StopMovement("A");
            Assert.That(Root.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(3));
            Assert.That(music.isPlaying && movie.isPlaying && oneShot.isPlaying, Is.True);
            int musicAtStop = music.timeSamples;
            _audio.StopMovie(owner);
            yield return Wait(.1);
            Assert.That(music.timeSamples, Is.GreaterThan(musicAtStop));
            Assert.That(oneShot.isPlaying, Is.True);
            Assert.That(movie == null, Is.True);
        }

        [UnityTest]
        public IEnumerator RepeatedInputSynchronizationDoesNotRewindRunningMusic()
        {
            _audio.SetMusic("silent");
            _audio.PlaybackEnabled = true;
            yield return Wait(.25);
            AudioSource music = Source("music fixture");
            Assert.That(music.timeSamples, Is.GreaterThan(4800));
            var samples = new List<string>();
            for (int input = 0; input < 8; input++)
            {
                int before = music.timeSamples;
                // HUD commands publish the current battle state immediately,
                // even when no fixed simulation step occurred between inputs.
                // The editor also caps elapsed simulation time after a stall.
                _audio.SetTime(input * .02);
                _audio.PlaybackEnabled = true;
                Assert.That(Source("music fixture"), Is.SameAs(music));
                Assert.That(music.isPlaying, Is.True);
                samples.Add($"{before}->{music.timeSamples}");
                Assert.That(music.timeSamples, Is.GreaterThanOrEqualTo(before - 1024),
                    "Publishing a battle tick must not seek a continuously playing music voice back to the simulation clock.");
                yield return Wait(.15);
            }
            Debug.Log("Continuous music samples across input synchronization: " + string.Join(", ", samples));
            _audio.PlaybackEnabled = false;
            _audio.SetTime(1.5);
            _audio.PlaybackEnabled = true;
            Assert.That(music.timeSamples, Is.InRange(70000, 78000),
                "Resuming after silent inspection must still seek to the inspected battle time.");
        }

        [UnityTest]
        public IEnumerator SlowOrRepeatedEditorTicksCannotRewindRunningSoundVoices()
        {
            _audio.SetMusic("silent");
            _audio.Play(_sound);
            _audio.PlayMovement("actor", _sound, true);
            _audio.PlaybackEnabled = true;
            yield return Wait(.3);
            AudioSource[] voices = Root.GetComponentsInChildren<AudioSource>();
            Assert.That(voices, Has.Length.EqualTo(3));
            Assert.That(voices.Select(voice => voice.mute), Is.All.EqualTo(Application.isBatchMode));
            Assert.That(voices.Select(voice => voice.timeSamples), Is.All.GreaterThan(4800));
            for (int input = 0; input < 5; input++)
            {
                int[] before = voices.Select(voice => voice.timeSamples).ToArray();
                _audio.SetTime(input * .02);
                for (int index = 0; index < voices.Length; index++)
                    Assert.That(voices[index].timeSamples, Is.GreaterThanOrEqualTo(before[index] - 1024),
                        "A throttled or repeated editor tick must not rewind " + voices[index].clip.name);
                yield return Wait(.12);
            }
            _audio.PlaybackEnabled = false;
            _audio.SetTime(1.5);
            _audio.PlaybackEnabled = true;
            Assert.That(voices.Select(voice => voice.timeSamples), Is.All.InRange(70000, 78000));
        }

        [UnityTest]
        public IEnumerator PauseSingleStepAndMovieSeekRetainIndependentPhases()
        {
            _audio.SetMusic("silent");
            var owner = new object();
            _audio.SynchronizeMovie(owner, _movie, .5, true);
            _audio.PlaybackEnabled = true;
            yield return Wait(.15);
            AudioSource music = Source("music fixture");
            AudioSource movie = Source("movie fixture");
            _audio.PlaybackEnabled = false;
            int pausedMusic = music.timeSamples;
            int pausedMovie = movie.timeSamples;
            yield return Wait(.1);
            Assert.That(music.timeSamples, Is.EqualTo(pausedMusic).Within(1024));
            Assert.That(movie.timeSamples, Is.EqualTo(pausedMovie).Within(1024));
            _audio.SetTime(1.5); // Silent inspection advances the battle clock.
            _audio.SynchronizeMovie(owner, _movie, 2, false);
            _audio.PlaybackEnabled = true;
            _audio.SynchronizeMovie(owner, _movie, 2, true);
            yield return null;
            Assert.That(music.timeSamples, Is.InRange(70000, 78000));
            Assert.That(movie.timeSamples, Is.InRange(94000, 102000));
            _audio.SetMusicGain(.25f);
            Assert.That(music.volume, Is.EqualTo(.25f));
            Assert.That(movie.volume, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MutingPausesEveryVoiceAndDisposalClosesOnlyItsPreviewScene()
        {
            _audio.SetMusic("silent");
            _audio.PlayMovement("A", _sound, true);
            _audio.PlaybackEnabled = true;
            yield return Wait(.1);
            AudioSource music = Source("music fixture");
            Scene scene = music.gameObject.scene;
            Assert.That(UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(scene), Is.True);
            _audio.Muted = true;
            Assert.That(Root.GetComponentsInChildren<AudioSource>().All(source => !source.isPlaying), Is.True);
            _audio.StopBodySounds();
            Assert.That(Root.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(1));
            _audio.Muted = false;
            Assert.That(music.isPlaying, Is.True);
            _audio.Dispose();
            Assert.That(scene.IsValid(), Is.False);
            Assert.That(music == null, Is.True);
        }

        [UnityTest]
        public IEnumerator PreviewSceneRoutesSignalToTheSilentOfflineAudioRenderer()
        {
            int channels = AudioSettings.speakerMode switch
            {
                AudioSpeakerMode.Mono => 1,
                AudioSpeakerMode.Stereo or AudioSpeakerMode.Prologic => 2,
                AudioSpeakerMode.Quad => 4,
                AudioSpeakerMode.Surround => 5,
                AudioSpeakerMode.Mode5point1 => 6,
                AudioSpeakerMode.Mode7point1 => 8,
                _ => 0
            };
            if (channels == 0)
                Assert.Ignore("The current speaker layout has no PCM capture mapping.");
            float captureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            bool recording = AudioRenderer.Start();
            if (!recording)
            {
                Time.captureDeltaTime = captureDelta;
                Assert.Ignore("This editor cannot enter offline audio recording mode.");
            }
            var owner = new object();
            try
            {
                // Unity's bundled AudioModule documentation guarantees physical
                // output is silence throughout AudioRenderer recording mode.
                // A synthetic signal can therefore verify the actual output
                // graph without playing it through the user's speakers.
                var signal = new float[_movie.samples];
                for (int sample = 0; sample < signal.Length; sample++)
                    signal[sample] = .25f * MathF.Sin(sample * .1f);
                _movie.SetData(signal, 0);
                _audio.PlaybackEnabled = true;
                _audio.SynchronizeMovie(owner, _movie, 0, true);
                Source("movie fixture").mute = false;
                float peak = 0;
                double deadline = EditorApplication.timeSinceStartup + .5;
                while (EditorApplication.timeSinceStartup < deadline && peak <= .01f)
                {
                    yield return null;
                    int samples = AudioRenderer.GetSampleCountForCaptureFrame();
                    // Even an empty initial frame must be rendered so the
                    // recording clock can begin producing subsequent blocks.
                    using var output = new NativeArray<float>(samples * channels, Allocator.Temp);
                    Assert.That(AudioRenderer.Render(output), Is.True);
                    foreach (float value in output)
                        peak = Math.Max(peak, Math.Abs(value));
                }
                Assert.That(peak, Is.GreaterThan(.01f), "An editor preview source must reach the real output graph without adding or modifying a listener.");
            }
            finally
            {
                _audio.StopMovie(owner);
                _audio.PlaybackEnabled = false;
                AudioRenderer.Stop();
                Time.captureDeltaTime = captureDelta;
            }
        }

        [UnityTest]
        public IEnumerator EveryNativeCommonCueDecodesIntoAnOwnedVoice()
        {
            using var actual = new BattleAudioPreview(_resources) { PlaybackEnabled = false };
            string[] cues = new[] { 1, 2, 3, 4, 6 }.Select(id => "wa" + id.ToString("000"))
                .Concat(Enumerable.Range(1, 22).Select(id => "wd" + id.ToString("000"))).ToArray();
            foreach (string cue in cues)
                actual.Play(cue);
            var pending = (IDictionary)typeof(BattleAudioPreview).GetField("_pending", Members).GetValue(actual);
            double deadline = EditorApplication.timeSinceStartup + 15;
            while (pending.Count > 0 && EditorApplication.timeSinceStartup < deadline)
                yield return null;
            Assert.That(pending.Count, Is.Zero);
            Assert.That(actual.Diagnostics, Is.Empty);
            var root = (GameObject)typeof(BattleAudioPreview).GetField("_root", Members).GetValue(actual);
            AudioSource[] voices = root.GetComponentsInChildren<AudioSource>();
            Assert.That(voices.Select(voice => voice.clip.name), Is.EquivalentTo(cues.Select(cue => cue + ".wav")));
            foreach (AudioSource voice in voices)
            {
                Assert.That(voice.isPlaying, Is.False, "Decoding original assets must remain silent during background QA.");
                AudioClip clip = voice.clip;
                Assert.That(clip.samples, Is.GreaterThan(0), clip.name);
                var samples = new float[clip.samples * clip.channels];
                Assert.That(clip.GetData(samples, 0), Is.True, clip.name);
                Assert.That(samples.Any(sample => Math.Abs(sample) > .001f), Is.True,
                    clip.name + " must contain audible PCM, not just a successful file lookup.");
            }
        }

        [Test]
        public void FailedAudioRequestCreationReleasesTheExtractedFileAndDesiredVoice()
        {
            string temporary = Path.GetTempFileName();
            try
            {
                MethodInfo queue = typeof(BattleAudioPreview).GetMethod("Queue", Members);
                const string voice = "fixture voice";
                var failure = Assert.Throws<TargetInvocationException>(() =>
                    queue.Invoke(_audio, new object[] { voice, "fixture.mp3", "", AudioType.MPEG, true, temporary }));
                Assert.That(failure.InnerException, Is.TypeOf<UriFormatException>());
                Assert.That(File.Exists(temporary), Is.False);
                var desired = (IDictionary)typeof(BattleAudioPreview).GetField("_desired", Members).GetValue(_audio);
                var pending = (IDictionary)typeof(BattleAudioPreview).GetField("_pending", Members).GetValue(_audio);
                Assert.That(desired.Contains(voice), Is.False);
                Assert.That(pending.Count, Is.Zero);
            }
            finally
            {
                File.Delete(temporary);
            }
        }

        [Test]
        public void DisposeCleansUpWhenAChildVoiceIsGoneAndATemporaryFileCannotBeDeleted()
        {
            GameObject root = Root;
            Scene scene = root.scene;
            _audio.Play(_sound);
            AudioSource voice = Source("body fixture");
            AudioClip clip = voice.clip;
            Object.DestroyImmediate(voice.gameObject);

            string music = BattleData.Variant == "PAL3" ? "PI24" : "P54";
            _audio.SetMusic(music);
            var pending = (IDictionary)typeof(BattleAudioPreview).GetField("_pending", Members).GetValue(_audio);
            object decode = pending.Values.Cast<object>().Single();
            FieldInfo temporary = decode.GetType().GetField("Temporary", Members);
            string extracted = (string)temporary.GetValue(decode);
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                // File.Delete rejects directories on all desktop platforms.
                temporary.SetValue(decode, directory);
                Assert.DoesNotThrow(_audio.Dispose);
                Assert.That(_audio.Diagnostics, Has.Some.Contains(directory));
                Assert.That(pending.Count, Is.Zero);
                Assert.That(clip == null && root == null && !scene.IsValid(), Is.True);
                Assert.DoesNotThrow(_audio.Dispose);
            }
            finally
            {
                if (extracted != null)
                    File.Delete(extracted);
                Directory.Delete(directory);
            }
        }

        [UnityTest]
        public IEnumerator PackedMusicDecodesWithoutPlaybackAndStopAbortsItsPendingOwner()
        {
            string music = BattleData.Variant == "PAL3" ? "PI24" : "P54";
            _audio.Muted = true;
            _audio.SetMusic(music);
            double until = EditorApplication.timeSinceStartup + 15;
            var pending = (IDictionary)typeof(BattleAudioPreview).GetField("_pending", Members).GetValue(_audio);
            while (pending.Count > 0 && EditorApplication.timeSinceStartup < until)
                yield return null;
            Assert.That(pending.Count, Is.Zero);
            Assert.That(_audio.Diagnostics, Is.Empty);
            AudioSource source = Root.GetComponentsInChildren<AudioSource>().Single();
            Assert.That(source.clip.samples, Is.GreaterThan(48000));
            Assert.That(source.loop, Is.True);
            Assert.That(source.isPlaying, Is.False);
            _audio.Stop();
            Assert.That(Root.GetComponentsInChildren<AudioSource>(), Is.Empty);
            Assert.That(pending.Count, Is.Zero);
        }
    }
}
