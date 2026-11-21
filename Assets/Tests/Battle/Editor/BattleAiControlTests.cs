// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Linq;
    using Battle;
    using NUnit.Framework;

    public sealed class BattleAiControlTests
    {
        // The 64 rows of 64 outcomes came from exhaustive execution of both
        // published ProcAIList comparison branches; the two matrices match.
        private const string NativeCosts =
            "AAAAAP////8AAAAA/////wAAAAD/////AAAAAP////8AAAAA/////wAAAAD/////AAAAAP////8AAAAA/////wAAAAD/////AAAAAP////8AAAAA" +
            "/////wAAAAD/////AAAAAP////8AAAAA/////wAAAAD/////AAAAAP////8AAAAA/////wAAAAD/////AAAAAP////8AAAAA/////wAAAAD/////" +
            "AAAAAP////8AAAAA/////wAAAAD/////AAAAAP////8AAAAA/////wAAAAD/////AAAAAP////8AAAAA/////wAAAAD/////AAAAAP////8AAAAA" +
            "///////////8//////////z/////////8//////////z/////////8//////////z/////////8//////////z////////////z//////////P//" +
            "///////z//////////P/////////z//////////P/////////z//////////P////////////P/////////8//////////P/////////8///////" +
            "///P/////////8//////////P/////////8////////////8//////////z/////////8//////////z/////////8//////////z/////////8/" +
            "/////////z8=";

        private sealed class Catalog : IBattleCatalog
        {
            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "A" ? 0 : actor == "B" ? 1 : 2;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class Effects : IBattleEffectDirector, IBattleModalEffects
        {
            public bool BlocksBattleTime
            {
                get; set;
            }

            public bool SkipModalPresentation() => false;

            public long Start(BattleEffectRequest request) => 1;

            public bool IsRunning(long handle) => false;

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void Stop(long handle)
            {
            }
        }

        private static (BattleSession Session, BattleSetup Setup, Catalog Catalog, BattleDefinitions Definitions) Create(string variant, Effects effects = null)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "portable", DataFingerprint = "ai-control", Encounter = BattleEncounter.AllyAmbush };
            foreach (var entry in new[] { ("A", BattleSide.Ally, BattleSlot.Water), ("B", BattleSide.Ally, BattleSlot.Fire), ("E", BattleSide.Enemy, BattleSlot.Water) })
                scenario.Participants.Add(new BattleParticipant
                {
                    InstanceId = entry.Item1,
                    ActorId = entry.Item1 == "A" ? 101 : entry.Item1 == "B" ? variant == "PAL3" ? 104 : 102 : 701,
                    Side = entry.Item2,
                    Slot = entry.Item3,
                    Attributes = new[] { 100, 100, 100, 60, 20, 3, 2, 0, 0, 0, 0, 0 },
                    Vitals = new[] { 100, 100, 100 }
                });
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint, Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.InstanceId == "A" ? 0 : actor.InstanceId == "B" ? 1 : -1
                })));
            var setup = new BattleSetup(scenario);
            var catalog = new Catalog();
            var session = new BattleSession(setup, catalog, definitions: definitions, effects: effects);
            session.SetAutomatic(BattleSide.Enemy, false);
            return (session, setup, catalog, definitions);
        }

        private static BattleActorSnapshot Actor(BattleSession session, string id) => session.Actors.Single(actor => actor.Id == id);

        [Test]
        public void EveryProfileComparisonMatchesBothPublishedPrograms()
        {
            byte[] matrix = Convert.FromBase64String(NativeCosts);
            for (int before = 0; before < 64; before++)
                for (int after = 0; after < 64; after++)
                    Assert.That(BattleAiControlRules.ConsumesGauges(before, after),
                        Is.EqualTo((matrix[before * 8 + after / 8] & (1 << (after % 8))) != 0), $"before={before}, after={after}");
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ConfirmingEnabledAiConsumesOnlyTheControllersGaugesAndRecordsOneInput(string variant)
        {
            var fixture = Create(variant);
            BattleSession session = fixture.Session;
            BattleActorSnapshot before = Actor(session, "A"), companion = Actor(session, "B");
            uint random = session.Events.Last().RandomState;
            int count = session.Inputs.Count;
            int flags = session.GetAiProfile("A") | 33;
            Assert.That(session.CommitAiControl("A", flags), Is.True);
            Assert.That(session.Inputs.Skip(count).Select(input => input.Kind), Is.EqualTo(new[] { BattleInputKind.AiControl }));
            Assert.That(session.GetAiProfile("A"), Is.EqualTo(flags));
            BattleActorSnapshot after = Actor(session, "A");
            Assert.That((after.Gauge, after.CastGauge, after.Ready), Is.EqualTo((0, 0, false)));
            Assert.That((after.Hp, after.Sp, after.Mp, after.Rounds, after.Guarding, after.Pose),
                Is.EqualTo((before.Hp, before.Sp, before.Mp, before.Rounds, before.Guarding, before.Pose)));
            Assert.That(Actor(session, "B").Gauge, Is.EqualTo(companion.Gauge));
            Assert.That(session.WaitingActor, Is.EqualTo("B"));
            Assert.That(session.Events.Last().RandomState, Is.EqualTo(random));
        }

        [TestCase("PAL3", 2)]
        [TestCase("PAL3A", 2)]
        [TestCase("PAL3", 1)]
        [TestCase("PAL3A", 1)]
        public void DisabledChildrenAndAttackOnlyDifferencesStillCommitWithoutGaugeCost(string variant, int flags)
        {
            BattleSession session = Create(variant).Session;
            session.SetAiProfile("A", 0);
            int count = session.Inputs.Count;
            uint random = session.Events.Last().RandomState;
            Assert.That(session.CommitAiControl("A", flags), Is.True);
            Assert.That(session.GetAiProfile("A"), Is.EqualTo(flags));
            Assert.That((Actor(session, "A").Gauge, Actor(session, "A").Ready), Is.EqualTo((BattleSession.FullGauge, true)));
            Assert.That(session.WaitingActor, Is.EqualTo("A"));
            Assert.That(session.Inputs.Count, Is.EqualTo(count + 1));
            Assert.That(session.Inputs.Last().Kind, Is.EqualTo(BattleInputKind.AiControl));
            Assert.That(session.Events.Last().RandomState, Is.EqualTo(random));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void IdenticalConfirmationIsAcceptedWithoutAnInputOrMutation(string variant)
        {
            BattleSession session = Create(variant).Session;
            int inputs = session.Inputs.Count, events = session.Events.Count;
            Assert.That(session.CommitAiControl("A", session.GetAiProfile("A")), Is.True);
            Assert.That(session.Inputs.Count, Is.EqualTo(inputs));
            Assert.That(session.Events.Count, Is.EqualTo(events));
            Assert.That(Actor(session, "A").Gauge, Is.EqualTo(BattleSession.FullGauge));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void DebugSetterRemainsFreeAndAtomicConfirmReplaysWithTheSameGaugeCost(string variant)
        {
            var fixture = Create(variant);
            BattleSession session = fixture.Session;
            session.SetAiProfile("A", 0);
            Assert.That(Actor(session, "A").Gauge, Is.EqualTo(BattleSession.FullGauge));
            Assert.That(session.CommitAiControl("A", 2), Is.True);
            Assert.That(session.CommitAiControl("A", 35), Is.True);
            Assert.That(session.Submit("B", BattleAction.Defend), Is.True);
            for (int tick = 0; tick < 4; tick++)
                session.Step();
            BattleSession replay = BattleReplay.Play(fixture.Setup, fixture.Catalog, session.Inputs, session.Tick, definitions: fixture.Definitions);
            Assert.That(replay.GetAiProfile("A"), Is.EqualTo(session.GetAiProfile("A")));
            Assert.That(replay.Actors.Select(actor => (actor.Id, actor.Gauge, actor.CastGauge, actor.Ready, actor.Guarding, actor.Hp, actor.Rounds)),
                Is.EqualTo(session.Actors.Select(actor => (actor.Id, actor.Gauge, actor.CastGauge, actor.Ready, actor.Guarding, actor.Hp, actor.Rounds))));
            Assert.That(replay.Events.Select(entry => (entry.Tick, entry.Kind, entry.Source, entry.RandomState, entry.Detail)),
                Is.EqualTo(session.Events.Select(entry => (entry.Tick, entry.Kind, entry.Source, entry.RandomState, entry.Detail))));
            Assert.That(replay.Events.Last().RandomState, Is.EqualTo(session.Events.Last().RandomState));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void StaleEnemyModalAndCompletedControllersCannotChangeProfiles(string variant)
        {
            var effects = new Effects();
            BattleSession session = Create(variant, effects).Session;
            int count = session.Inputs.Count;
            uint random = session.Events.Last().RandomState;
            foreach (string controller in new[] { "B", "E", "missing", null })
                Assert.That(session.CommitAiControl(controller, 33), Is.False);
            effects.BlocksBattleTime = true;
            Assert.That(session.CommitAiControl("A", 33), Is.False);
            effects.BlocksBattleTime = false;
            Assert.That(session.Inputs.Count, Is.EqualTo(count));
            Assert.That(session.Events.Last().RandomState, Is.EqualTo(random));
            session.Abort();
            count = session.Inputs.Count;
            Assert.That(session.CommitAiControl("A", 33), Is.False);
            Assert.That(session.Inputs.Count, Is.EqualTo(count));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.CommitAiControl("A", 64));
        }
    }
}
