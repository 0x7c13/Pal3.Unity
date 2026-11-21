// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Battle.Presentation;
    using NUnit.Framework;
    using UnityEngine;

    // Exercises production HUD dispatch and its real AudioSource sink without
    // opening an EditorWindow. Cached digital silence keeps device QA unobtrusive.
    [Category("LocalGameData")]
    [Category("OffscreenGraphics")]
    public sealed class BattleHudAudioTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
        private BattleEditorSession _session;
        private BattleHudView _hud;
        private readonly List<string> _sounds = new();

        private void Bind(string field, object value) => typeof(BattleHudView).GetField(field, Members).SetValue(_hud, value);

        [SetUp]
        public void SetUp()
        {
            string root = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            Assert.That(root, Is.Not.Null, "The offscreen audio route requires the selected game's local data.");
            _session = new BattleEditorSession();
            _session.Load(root, 936);
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = _session.Data.Scenes.First(),
                DataFingerprint = _session.Data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush,
                Music = "NONE"
            };
            scenario.Participants.Add(_session.Data.CreateTestParticipant(101, BattleSide.Ally, BattleSlot.Water));
            int enemy = _session.Data.Database.CombatActorInfos.Keys.First(id => BattleAnimationCatalog.IsBasicEnemy(_session.Data, id));
            scenario.Participants.Add(_session.Data.CreateParticipant(enemy, BattleSide.Enemy, BattleSlot.Water));
            _session.StartBattle(scenario);
            BattleAudioPreview audio = _session.Preview.Audio;
            audio.Stop();
            var clips = (IDictionary<string, AudioClip>)typeof(BattleAudioPreview).GetField("_clips", Members).GetValue(audio);
            foreach (string cue in new[] { "wa001", "wa002", "wa003", "wa004" })
                clips.Add(cue + ".wav", AudioClip.Create("silent HUD route: " + cue, 48000, 1, 48000, false));
            audio.PlaybackEnabled = true;
            audio.Muted = false;
            _sounds.Clear();
            audio.SoundPlayed += _sounds.Add;
            _hud = new BattleHudView();
            Bind("_battle", _session.Battle);
            Bind("_data", _session.Data);
            Bind("_preview", _session.Preview);
            Bind("_inputEnabled", true);
            typeof(BattleHudView).GetMethod("Synchronize", Members).Invoke(_hud, new object[] { _session.Battle });
            Assert.That(_sounds, Is.EqualTo(new[] { "wa003.wav" }), "The first ready command panel must open audibly exactly once.");
            typeof(BattleHudView).GetMethod("Synchronize", Members).Invoke(_hud, new object[] { _session.Battle });
            Assert.That(_sounds.Count, Is.EqualTo(1), "Repaint synchronization must not repeat the panel sound.");
            _sounds.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _hud?.Release();
            _session?.Dispose();
        }

        private void Confirm(string control) => Assert.That(_hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Confirm, control)), Is.True);

        private void Check(params string[] sounds)
        {
            Assert.That(_sounds, Is.EqualTo(sounds.Select(sound => sound + ".wav")));
            Assert.That(_session.Preview.Audio.Diagnostics, Is.Empty);
            var root = (GameObject)typeof(BattleAudioPreview).GetField("_root", Members).GetValue(_session.Preview.Audio);
            Assert.That(root.GetComponentsInChildren<AudioSource>().All(source => source.isPlaying), Is.True,
                "Menu requests must reach running owned AudioSources, not only a presenter notification.");
            _sounds.Clear();
        }

        [TestCase("start")]
        [TestCase("restart")]
        [TestCase("replay")]
        public void ReplacementBindsAudioAndCommandSynchronizationBeforeAnyGuiPass(string replacement)
        {
            _hud.Release(); // The window's PreviewReleased subscription does this during replacement.
            if (replacement == "start")
                _session.StartBattle(_session.CapturedScenario);
            else if (replacement == "restart")
                _session.Restart();
            else
                _session.ReplayBattle();
            PrepareReplacementAudio();
            _session.IsPlaying = true;
            int synchronized = 0;
            void Changed()
            {
                synchronized++;
                _session.SyncBattle();
            }
            _hud.BindBattle(_session.Battle, _session.Data, _session.Preview, Changed, null);
            _hud.TakeGamepadInput(_session.Battle);
            Assert.That(_sounds, Is.EqualTo(new[] { "wa003.wav" }));
            _hud.BindBattle(_session.Battle, _session.Data, _session.Preview, Changed, null);
            Assert.That(_sounds.Count, Is.EqualTo(1), "The subsequent GUI bind must not repeat the ready cue.");
            _session.IsPlaying = false;
            _sounds.Clear();
            int before = _session.Battle.RecordedInputCount;
            Assert.That(_hud.Presenter.SetFocus("defend"), Is.True);
            Assert.That(_hud.SendGamepadIntent(new BattleEditorGamepadIntent(BattleEditorGamepadIntentKind.Confirm)), Is.True);
            Assert.That(_session.Battle.RecordedInputCount, Is.GreaterThan(before));
            Assert.That(synchronized, Is.EqualTo(1), "Paused commands must update the preview immediately without waiting for a GUI pass or simulation tick.");
            Assert.That(_sounds, Is.Empty);
        }

        private void PrepareReplacementAudio()
        {
            BattleAudioPreview audio = _session.Preview.Audio;
            audio.Stop();
            var clips = (IDictionary<string, AudioClip>)typeof(BattleAudioPreview).GetField("_clips", Members).GetValue(audio);
            foreach (string cue in new[] { "wa001", "wa002", "wa003", "wa004" })
                clips.Add(cue + ".wav", AudioClip.Create("silent replacement route: " + cue, 48000, 1, 48000, false));
            _sounds.Clear();
            audio.SoundPlayed += _sounds.Add;
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void ReplacementBindingStaysSilentWhilePausedOrUnfocused(bool playing, bool focused)
        {
            _hud.Release();
            _session.Restart();
            PrepareReplacementAudio();
            _session.IsPlaying = playing;
            _hud.SetInputFocus(focused);
            _hud.BindBattle(_session.Battle, _session.Data, _session.Preview, _session.SyncBattle, null);
            Assert.That(_sounds, Is.Empty);
            _hud.SetInputFocus(true);
            _session.IsPlaying = true;
            _hud.BindBattle(_session.Battle, _session.Data, _session.Preview, _session.SyncBattle, null);
            Assert.That(_sounds, Is.Empty, "A suppressed ready cue must not be queued for focus or playback resume.");
        }

        [TestCase(false, 1f, false)]
        [TestCase(true, 1f, true)]
        [TestCase(true, 2f, false)]
        public void SilentMenuNavigationDoesNotQueueSoundsForResume(bool playing, float speed, bool muted)
        {
            BattleAudioPreview audio = _session.Preview.Audio;
            audio.Stop();
            _session.Speed = speed;
            _session.IsPlaying = playing;
            audio.Muted = muted;

            Confirm("skills");
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Back));
            var root = (GameObject)typeof(BattleAudioPreview).GetField("_root", Members).GetValue(audio);
            Assert.That(root.GetComponentsInChildren<AudioSource>(), Is.Empty);
            Assert.That(_sounds, Is.Empty);

            audio.Muted = false;
            _session.Speed = 1;
            _session.IsPlaying = true;
            Assert.That(root.GetComponentsInChildren<AudioSource>(), Is.Empty);
            Confirm("skills");
            Check("wa001", "wa003");
        }

        [Test]
        public void MenusAndCategoriesSoundOnActivationWithoutHoverOrRepaintNoise()
        {
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerMove, "magic", 4));
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Up));
            Check();
            Confirm("skills");
            Check("wa001", "wa003");
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Back));
            Check("wa003");
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerDown, "items", 4));
            Check();
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerUp, "items", 4));
            Check("wa001", "wa003");
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Right));
            Check("wa001");
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.CycleNext));
            Check("wa001");
        }

        [Test]
        public void RepeatingTheSameDeniedActivationSoundsEveryTimeWithoutSoundingOnStrayReleases()
        {
            Confirm("ai");
            Check("wa001", "wa003");
            // AI starts disabled. Each deliberate activation gets the native
            // error cue even when the description has not changed.
            for (int attempt = 0; attempt < 3; attempt++)
            {
                _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Confirm, "flag:1"));
                Check("wa004");
            }
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerUp, "flag:1", 8));
            Check();
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerDown, "flag:1", 8));
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerUp, "flag:1", 8));
            Check("wa004");
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerDown, "flag:1", 8));
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerCancel, pointerId: 8));
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerUp, "flag:1", 8));
            Check();
            if ((_hud.Presenter.AiFlags(_hud.Presenter.Controller) & 1) != 0)
            {
                Confirm("flag:0");
                Check("wa001");
            }
            for (int attempt = 0; attempt < 2; attempt++)
            {
                _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Confirm, "flag:0"));
                Check("wa004");
            }
            string enemy = _session.Battle.Actors.First(actor => actor.Side == BattleSide.Enemy).Id;
            Confirm("ai-actor:" + enemy);
            Check("wa001");
            Assert.That(_hud.Presenter.Message, Is.Null, "A successful new activation must clear stale failure feedback.");
            Confirm("end");
            Check("wa001", "wa003");
        }

        [Test]
        public void AbilityConfirmationSoundsOnceAndCancelledOrStrayReleasesAreSilent()
        {
            Confirm("skills");
            Check("wa001", "wa003");
            int skill = _hud.Presenter.Abilities().First(ability => ability.Denied == null &&
                ability.Definition.Range == BattleTargetRange.OpponentSingle).Definition.Id;
            string control = "ability:" + skill;
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerUp, control, 8));
            Check();
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerDown, control, 8));
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerCancel, pointerId: 8));
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerUp, control, 8));
            Check();
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerDown, control, 8));
            Check();
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.PointerUp, control, 8));
            Check("wa002");
            Assert.That(_hud.Presenter.Page, Is.EqualTo(BattleHudPage.Targets));
            _hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Back));
            Check("wa003");
            Confirm(control);
            Check("wa002");
        }
    }
}
