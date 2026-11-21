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
    public sealed class BattleAudioOneShotTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private ResourceCatalog _resources;
        private BattleAudioPreview _audio;
        private AudioClip _movie;
        private string _sound, _directory;
        private bool _recording, _listenerPause;
        private float _captureDelta, _listenerVolume, _peak;

        private GameObject Root => (GameObject)typeof(BattleAudioPreview).GetField("_root", Members).GetValue(_audio);

        private AudioSource[] Sources => Root.GetComponentsInChildren<AudioSource>();

        private IDictionary<string, AudioClip> Clips => (IDictionary<string, AudioClip>)typeof(BattleAudioPreview).GetField("_clips", Members).GetValue(_audio);

        private IDictionary State(string name) => (IDictionary)typeof(BattleAudioPreview).GetField(name, Members).GetValue(_audio);

        [SetUp]
        public void SetUp()
        {
            _listenerVolume = AudioListener.volume;
            _listenerPause = AudioListener.pause;
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            if (root == null)
                Assert.Ignore("One-shot audio verification requires local game resources.");
            _resources = ResourceCatalog.OpenBaseData(root, 936);
            _audio = new BattleAudioPreview(_resources) { PlaybackEnabled = false };
            var files = (IDictionary<string, string>)typeof(BattleAudioPreview).GetField("_soundFiles", Members).GetValue(_audio);
            _sound = files.Keys.First();
            var body = AudioClip.Create("cached overlap fixture", 480000, 1, 48000, false);
            var signal = new float[body.samples];
            Array.Fill(signal, .1f);
            body.SetData(signal, 0);
            Clips.Add(_sound, body);
            Clips.Add("music:silent.mp3", AudioClip.Create("music ownership fixture", 480000, 1, 48000, false));
            _movie = AudioClip.Create("movie ownership fixture", 480000, 1, 48000, false);
            _directory = Path.Combine(Environment.GetEnvironmentVariable("PAL3_BATTLE_REPORT_DIR") ?? Application.temporaryCachePath,
                "AudioOneShots-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            EndCapture();
            _audio?.Dispose();
            _resources?.Dispose();
            if (_movie != null)
                Object.DestroyImmediate(_movie);
            if (_directory != null)
                Directory.Delete(_directory, true);
            Assert.That(AudioListener.volume, Is.EqualTo(_listenerVolume));
            Assert.That(AudioListener.pause, Is.EqualTo(_listenerPause));
        }

        private int BeginCapture()
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
            _captureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            _recording = AudioRenderer.Start();
            if (!_recording)
            {
                Time.captureDeltaTime = _captureDelta;
                Assert.Ignore("This editor cannot enter offline audio recording mode.");
            }
            return channels;
        }

        private void EndCapture()
        {
            // Stop every source before leaving recording mode, which guarantees
            // external silence even when an assertion interrupts a PCM fixture.
            _audio?.Stop();
            if (_audio != null)
                _audio.PlaybackEnabled = false;
            if (!_recording)
                return;
            AudioRenderer.Stop();
            Time.captureDeltaTime = _captureDelta;
            _recording = false;
        }

        private IEnumerator CapturePeak(int channels, float minimum)
        {
            Assert.That(_recording, Is.True, "Only route test signals while Unity has taken the speaker output offline.");
            foreach (AudioSource source in Sources)
                source.mute = false;
            _peak = 0;
            double deadline = EditorApplication.timeSinceStartup + .5;
            while (EditorApplication.timeSinceStartup < deadline && _peak <= minimum)
            {
                yield return null;
                int frames = AudioRenderer.GetSampleCountForCaptureFrame();
                // The first capture frame can be empty; rendering it still
                // advances the offline recording clock for subsequent blocks.
                using var samples = new NativeArray<float>(frames * channels, Allocator.Temp);
                Assert.That(AudioRenderer.Render(samples), Is.True);
                foreach (float sample in samples)
                    _peak = Math.Max(_peak, Math.Abs(sample));
            }
            Assert.That(_peak, Is.GreaterThan(minimum), "The requested independent voices must reach the actual output graph.");
        }

        private string AddWave(string name, float value)
        {
            string path = Path.Combine(_directory, name);
            const int frames = 48000 * 5;
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + frames * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)1);
                writer.Write(48000);
                writer.Write(96000);
                writer.Write((ushort)2);
                writer.Write((ushort)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(frames * 2);
                short amplitude = (short)MathF.Round(value * short.MaxValue);
                for (int frame = 0; frame < frames; frame++)
                    writer.Write(amplitude);
            }
            var files = (IDictionary<string, string>)typeof(BattleAudioPreview).GetField("_soundFiles", Members).GetValue(_audio);
            files.Add(name, path);
            return name;
        }

        private IEnumerator WaitForDecoders()
        {
            double deadline = EditorApplication.timeSinceStartup + 10;
            while (State("_pending").Count > 0 && EditorApplication.timeSinceStartup < deadline)
                yield return null;
            Assert.That(State("_pending").Count, Is.Zero);
            Assert.That(_audio.Diagnostics, Is.Empty);
        }

        [UnityTest]
        public IEnumerator CachedSameNameOneShotsMixBothOriginalVoices()
        {
            int channels = BeginCapture();
            try
            {
                _audio.PlaybackEnabled = true;
                _audio.Play(_sound);
                AudioSource first = Sources.Single();
                yield return CapturePeak(channels, .01f);
                float single = _peak;
                _audio.Play(_sound);
                Assert.That(Sources.Length, Is.EqualTo(2));
                Assert.That(first == null, Is.False, "A repeated name must preserve the preceding sound's tail.");
                Assert.That(Sources.All(source => ReferenceEquals(source.clip, first.clip)), Is.True, "Only the decoded clip is shared.");
                yield return CapturePeak(channels, single * 1.8f);
                Assert.That(_peak, Is.EqualTo(single * 2).Within(single * .1f));
                Debug.Log($"Cached one-shot output: single={single}, overlap={_peak}");
            }
            finally
            {
                EndCapture();
            }
        }

        [UnityTest]
        public IEnumerator PendingSameNameRequestsShareDecodeAndRetainIndependentClocks()
        {
            string sound = AddWave("pending-overlap.wav", .1f);
            _audio.Play(sound);
            _audio.SetTime(.25);
            _audio.Play(sound);
            Assert.That(State("_pending").Count, Is.EqualTo(1));
            Assert.That(State("_desired").Count, Is.EqualTo(2));
            yield return WaitForDecoders();
            AudioSource[] sources = Sources;
            Assert.That(sources.Length, Is.EqualTo(2));
            Assert.That(ReferenceEquals(sources[0].clip, sources[1].clip), Is.True);
            AudioClip clip = sources[0].clip;
            Assert.That(Clips.Count, Is.EqualTo(3));
            int channels = BeginCapture();
            try
            {
                _audio.PlaybackEnabled = true;
                sources = Sources.OrderByDescending(source => source.timeSamples).ToArray();
                AudioSource earlier = sources[0], later = sources[1];
                Assert.That(earlier.timeSamples, Is.InRange(11000, 13000));
                Assert.That(later.timeSamples, Is.InRange(0, 1024));
                later.volume = 0;
                yield return CapturePeak(channels, .01f);
                float single = _peak;
                later.volume = 1;
                yield return CapturePeak(channels, single * 1.8f);
                Assert.That(_peak, Is.EqualTo(single * 2).Within(single * .1f));
                Debug.Log($"Pending one-shot output: single={single}, overlap={_peak}");
                _audio.PlaybackEnabled = false;
                double duration = clip.samples / (double)clip.frequency;
                _audio.SetTime(duration + .01);
                Assert.That(earlier == null, Is.True);
                Assert.That(later == null, Is.False);
                Assert.That(Sources.Length, Is.EqualTo(1));
                _audio.SetTime(duration + .25);
                Assert.That(Sources, Is.Empty);
                Assert.That(State("_desired").Count, Is.Zero);
                Assert.That(State("_voices").Count, Is.Zero);
                Assert.That(clip == null, Is.False, "The owned cache remains reusable after individual voices expire.");
                string expired = AddWave("expired-pending.wav", 0);
                _audio.Play(expired);
                _audio.SetTime(20);
                yield return WaitForDecoders();
                Assert.That(Sources, Is.Empty);
                Assert.That(State("_desired").Count, Is.Zero, "A request that expires during decode must be reclaimed immediately.");
            }
            finally
            {
                EndCapture();
            }
        }

        [UnityTest]
        public IEnumerator StopBoundariesClearEveryRepeatedRequestWithoutTakingOtherOwners()
        {
            _audio.SetMusic("silent");
            _audio.SynchronizeMovie(new object(), _movie, 0, false);
            _audio.PlayMovement("A", _sound, true);
            _audio.Play(_sound);
            _audio.Play(_sound);
            string sound = AddWave("stopped-pending.wav", 0);
            _audio.Play(sound);
            _audio.Play(sound);
            Assert.That(Sources.Length, Is.EqualTo(5));
            Assert.That(State("_pending").Count, Is.EqualTo(1));
            _audio.StopBodySounds();
            Assert.That(Sources.Length, Is.EqualTo(2), "Music and movie owners survive a body stop.");
            Assert.That(Sources.All(source => source.clip.name.Contains("ownership fixture")), Is.True);
            Assert.That(State("_desired").Count, Is.EqualTo(1));
            yield return WaitForDecoders();
            Assert.That(Sources.Length, Is.EqualTo(2), "A completed shared decode cannot resurrect stopped requests.");
            string aborted = AddWave("aborted-pending.wav", 0);
            _audio.Play(aborted);
            _audio.Play(aborted);
            Assert.That(State("_pending").Count, Is.EqualTo(1));
            _audio.Stop();
            Assert.That(Sources, Is.Empty);
            Assert.That(State("_desired").Count, Is.Zero);
            Assert.That(State("_voices").Count, Is.Zero);
            Assert.That(State("_pending").Count, Is.Zero);
            yield return null;
            Assert.That(Sources, Is.Empty);
            AudioClip[] clips = Clips.Values.ToArray();
            GameObject root = Root;
            Scene scene = root.scene;
            _audio.Dispose();
            Assert.That(root == null, Is.True);
            Assert.That(scene.IsValid(), Is.False);
            Assert.That(clips.All(clip => clip == null), Is.True);
            Assert.That(_movie == null, Is.False, "The movie clip remains with its own owner.");
        }

        [UnityTest]
        public IEnumerator CancellationFromTheFirstDecodeCallbackCannotResurrectAnotherVoice()
        {
            foreach (int boundary in new[] { 0, 1, 2 })
            {
                _audio.PlaybackEnabled = false;
                string sound = AddWave($"callback-{boundary}.wav", 0);
                _audio.Play(sound);
                _audio.Play(sound);
                _audio.Play(AddWave($"callback-other-{boundary}.wav", 0));
                int callbacks = 0;
                Action<string> cancel = null;
                cancel = played =>
                {
                    if (!played.Equals(sound, StringComparison.OrdinalIgnoreCase))
                        return;
                    _audio.SoundPlayed -= cancel;
                    callbacks++;
                    if (boundary == 0)
                        _audio.StopBodySounds();
                    else if (boundary == 1)
                        _audio.Stop();
                    else
                        _audio.Dispose();
                };
                _audio.SoundPlayed += cancel;
                GameObject root = Root;
                _audio.PlaybackEnabled = true; // Every file in this fixture is digital silence.
                yield return WaitForDecoders();
                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(State("_desired").Count, Is.Zero);
                Assert.That(State("_voices").Count, Is.Zero);
                if (boundary == 2)
                    Assert.That(root == null, Is.True);
                else
                    Assert.That(Sources, Is.Empty);
            }
        }
    }
}
