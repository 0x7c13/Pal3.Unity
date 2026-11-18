// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using NUnit.Framework;

    public sealed class BattleEffectAudioSessionTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings
        {
            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public AttackTiming Ability(string source, BattleAbilityDefinition ability) => new(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "A" ? 0 : 5;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class Rules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => BattleSession.FullGauge;

            public int GaugePerStep(BattleParticipant actor) => BattleSession.FullGauge;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1) => new(-1, false, false);
        }

        private sealed class Audio : IBattleEffectDirector, IBattleAudio
        {
            public readonly List<string> Sounds = new();

            public long Start(BattleEffectRequest request) => throw new InvalidOperationException("The fixture has no native effect groups.");

            public bool IsRunning(long handle) => false;

            public void Stop(long handle)
            {
            }

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void PlaySound(string sound) => Sounds.Add(sound);
        }

        private static BattleParticipant Participant(string id, BattleSide side) => new()
        {
            InstanceId = id,
            ActorId = side == BattleSide.Ally ? 101 : 701,
            Side = side,
            Slot = BattleSlot.Water,
            Attributes = new[] { 500, 100, 200, 10, 0, 3, 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { 200, 50, 50 }
        };

        private static BattleSession Create(string variant, Audio audio, BattleAbilityData item,
            BattleState[] initial = null, BattleState[] permanent = null, bool deadRecipient = false, bool stealableItem = false, int stealableMoney = 0)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "fixture", DataFingerprint = "audio", Seed = 10 };
            scenario.Participants.Add(Participant("A", BattleSide.Ally));
            scenario.Participants.Add(Participant("E", BattleSide.Enemy));
            if (deadRecipient)
            {
                BattleParticipant dead = Participant("B", BattleSide.Ally);
                dead.Slot = BattleSlot.Fire;
                dead.Vitals[0] = 0;
                scenario.Participants.Add(dead);
            }
            if (item.IsItem)
                scenario.Items.Add(new BattleItem { Id = item.Id, Count = 10 });
            else
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = item.Id, Level = 4, Mastered = true });
            var definition = new BattleAbilityDefinition(item);
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint,
                item.IsItem ? Array.Empty<BattleAbilityDefinition>() : new[] { definition }, item.IsItem ? new[] { definition } : Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Enemy ? -1 : actor.InstanceId == "A" ? 0 : 1,
                    InitialStates = actor.InstanceId == "A" ? initial ?? Array.Empty<BattleState>() : Array.Empty<BattleState>(),
                    PermanentStates = actor.InstanceId == "A" ? permanent ?? Array.Empty<BattleState>() : Array.Empty<BattleState>(),
                    StealableMoney = actor.InstanceId == "E" ? stealableMoney : 0,
                    StealableItemId = actor.InstanceId == "E" && stealableItem ? 6202 : 0,
                    StealableItemCount = actor.InstanceId == "E" && stealableItem ? 4 : 0
                })));
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new Rules(), definitions, effects: audio);
            session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(audio?.Sounds ?? new List<string>(), Is.Empty, "Authored and equipped initial states must not replay application sounds.");
            return session;
        }

        private static BattleAbilityData Item(params BattleStatusEffect[] states) => new()
        {
            Id = 6201,
            IsItem = true,
            ItemType = 1,
            Range = BattleTargetRange.OwnSingle,
            States = states
        };

        private static void Use(BattleSession session, string target = "A", bool steal = false)
        {
            Assert.That(session.Submit(new BattleCommand("A", steal ? BattleAction.Skill : BattleAction.Item, target, steal ? 4101 : 6201)), Is.True);
            for (int index = 0; index < 5; index++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running), session.Result?.Reason);
        }

        private static IEnumerable<TestCaseData> StateCues()
        {
            // Frozen native SetSpecState sound-port traces for all audible additions.
            string[] added = { "wd005", "wd005", "wd005", "wd005", "wd005", "wd021", "wd022", "wd021", "wd022", "wd021", "wd022", "wd021", "wd022", "wd007", "wd008", "wd009", "wd010", "wd012", "wd011", "wd016", "wd013", "wd014", "wd015" };
            foreach (string variant in new[] { "PAL3", "PAL3A" })
                for (int state = 0; state < added.Length; state++)
                    yield return new TestCaseData(variant, (BattleState)state, added[state]);
        }

        [TestCaseSource(nameof(StateCues))]
        public void ApplyingAnItemStateEmitsTheNativeCueThroughTheSession(string variant, BattleState state, string expected)
        {
            var audio = new Audio();
            BattleSession session = Create(variant, audio, Item(new BattleStatusEffect(state, false)));
            Use(session);
            Assert.That(audio.Sounds, Is.EqualTo(new[] { expected }));
        }

        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        public void CuringPoisonSoundsForEachRequestedRemovalEvenIfTheStateWasAbsent(string variant, bool present)
        {
            var audio = new Audio();
            var item = Item(Enumerable.Range(0, 5).Select(state => new BattleStatusEffect((BattleState)state, true)).ToArray());
            BattleSession session = Create(variant, audio, item, present ? new[] { BattleState.PoisonWater } : null);
            Use(session);
            Assert.That(audio.Sounds, Is.EqualTo(Enumerable.Repeat("wd006", 5)));
        }

        private static IEnumerable<TestCaseData> GoodRemovals()
        {
            foreach (string variant in new[] { "PAL3", "PAL3A" })
                foreach (BattleState state in new[] { BattleState.Reflection, BattleState.Evade, BattleState.Barrier, BattleState.Invisible })
                    foreach (bool present in new[] { false, true })
                        yield return new TestCaseData(variant, state, present);
        }

        [TestCaseSource(nameof(GoodRemovals))]
        public void RemovingAProtectiveStatePlaysItsNativeCueEvenWhenAbsent(string variant, BattleState state, bool present)
        {
            var audio = new Audio();
            BattleSession session = Create(variant, audio, Item(new BattleStatusEffect(state, true)), present ? new[] { state } : null);
            Use(session);
            Assert.That(audio.Sounds, Is.EqualTo(new[] { "wd020" }));
        }

        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        public void AilmentCuresAreGroupedAtTheAbilityBoundary(string variant, bool all)
        {
            var states = new[] { BattleState.Paralysis, BattleState.Seal, BattleState.Forbidden, BattleState.Sleep, BattleState.Madness, BattleState.Chaos };
            var item = Item(states.Take(all ? 6 : 5).Select(state => new BattleStatusEffect(state, true)).ToArray());
            var audio = new Audio();
            BattleSession session = Create(variant, audio, item);
            Use(session);
            Assert.That(audio.Sounds, Is.EqualTo(all ? new[] { "wd019" } : new[] { "wd017", "wd017", "wd017", "wd018", "wd018" }));
        }

        [TestCase("PAL3", BattleState.AttackIncrease, "wd021")]
        [TestCase("PAL3A", BattleState.AttackIncrease, "wd021")]
        [TestCase("PAL3", BattleState.PoisonWater, null)]
        [TestCase("PAL3A", BattleState.PoisonWater, null)]
        public void BuffRefreshSoundsButDuplicatePoisonIsRejectedSilently(string variant, BattleState state, string expected)
        {
            var audio = new Audio();
            BattleSession session = Create(variant, audio, Item(new BattleStatusEffect(state, false)), new[] { state });
            Use(session);
            Assert.That(audio.Sounds, Is.EqualTo(expected == null ? Array.Empty<string>() : new[] { expected }));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void InvisibleRecipientsBlockStateSoundsAlongWithTheirChanges(string variant)
        {
            var audio = new Audio();
            BattleSession session = Create(variant, audio, Item(new BattleStatusEffect(BattleState.AttackIncrease, false)), new[] { BattleState.Invisible });
            Use(session);
            Assert.That(audio.Sounds, Is.Empty);
        }

        [TestCase("PAL3", 20, 0, 0, "wd001")]
        [TestCase("PAL3A", 20, 0, 0, "wd001")]
        [TestCase("PAL3", 0, 0, 20, "wd002")]
        [TestCase("PAL3A", 0, 0, 20, "wd002")]
        [TestCase("PAL3", 20, 0, 20, "wd003")]
        [TestCase("PAL3A", 20, 0, 20, "wd003")]
        [TestCase("PAL3", 0, 20, 0, null)]
        [TestCase("PAL3A", 0, 20, 0, null)]
        [TestCase("PAL3", -20, 0, -20, null)]
        [TestCase("PAL3A", -20, 0, -20, null)]
        public void RecoveryUsesOneCombinedCueAndDoesNotSoundForQiOrDamage(string variant, int hp, int sp, int mp, string expected)
        {
            var audio = new Audio();
            var item = Item();
            item.Attributes = new[] { hp, sp, mp }.Select((value, resource) => new BattleAttributeEffect(resource, BattleValueMode.Absolute, value)).ToArray();
            BattleSession audible = Create(variant, audio, item);
            BattleSession silent = Create(variant, null, item);
            Use(audible);
            Use(silent);
            Assert.That(audio.Sounds, Is.EqualTo(expected == null ? Array.Empty<string>() : new[] { expected }));
            Assert.That(audible.Actors.Select(actor => (actor.Hp, actor.Sp, actor.Mp)), Is.EqualTo(silent.Actors.Select(actor => (actor.Hp, actor.Sp, actor.Mp))));
            Assert.That(audible.Events.Select(entry => (entry.Kind, entry.RandomState)), Is.EqualTo(silent.Events.Select(entry => (entry.Kind, entry.RandomState))));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ResurrectionSoundsBeforeItsHpRecovery(string variant)
        {
            var audio = new Audio();
            var item = Item(new BattleStatusEffect(BattleState.Death, true));
            item.Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, 20) };
            BattleSession session = Create(variant, audio, item, deadRecipient: true);
            Use(session, "B");
            Assert.That(audio.Sounds, Is.EqualTo(new[] { "wd004", "wd001" }));
            Assert.That(session.Actors.Single(actor => actor.Id == "B").Hp, Is.EqualTo(20));
        }

        [TestCase("PAL3", true, 0)]
        [TestCase("PAL3A", true, 0)]
        [TestCase("PAL3", false, 10)]
        [TestCase("PAL3A", false, 10)]
        [TestCase("PAL3", false, 0)]
        [TestCase("PAL3A", false, 0)]
        public void SuccessfulItemTheftSoundsButMoneyOnlyAndEmptyPoolsDoNot(string variant, bool item, int money)
        {
            var audio = new Audio();
            var steal = new BattleAbilityData { Id = 4101, SkillTypeId = 0, Handler = BattleSpecialHandler.Steal, Range = BattleTargetRange.OpponentSingle };
            BattleSession session = Create(variant, audio, steal, stealableItem: item, stealableMoney: money);
            Use(session, "E", true);
            Assert.That(audio.Sounds, Is.EqualTo(item ? new[] { "wa006" } : Array.Empty<string>()));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.ItemChanged), Is.EqualTo(item));
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        public void VictoryCleanupPlaysTheNativeRemovalCueForLivingAndDeadAllies(string variant, bool invisible)
        {
            var audio = new Audio();
            var item = Item();
            item.Range = BattleTargetRange.OpponentSingle;
            item.Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -10000) };
            BattleSession session = Create(variant, audio, item, invisible ? new[] { BattleState.Invisible } : null, deadRecipient: true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "E", 6201)), Is.True);
            for (int index = 0; index < 1000 && session.Status == BattleSessionStatus.Running; index++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed), session.Result?.Reason);
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Ally));
            Assert.That(audio.Sounds.Count(sound => sound == "wd020"), Is.EqualTo(2),
                "Native EndAll removes Invisible for every valid ally, even if absent or dead.");
            Assert.That(audio.Sounds, Does.Not.Contain("wd017").And.Not.Contain("wd018"),
                "Direct cleanup is not an ability's grouped ailment cure.");
            Assert.That(session.Result.Actors.Single(actor => actor.Id == "B").Hp, Is.EqualTo(1),
                "Sound output must preserve the existing settlement resource writes.");
        }

    }
}
