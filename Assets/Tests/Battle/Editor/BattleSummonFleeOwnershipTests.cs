// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Pal3.Battle;

    public sealed class BattleSummonFleeOwnershipTests
    {
        private const int Summon = 2001, FinishEncounter = 2002;

        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings, IBattlePhysicalTimings, IBattleStageGeometry, IBattleMonsterFactory
        {
            private readonly string _variant;

            internal Catalog(string variant) => _variant = variant;

            public int Priority(string actor) => actor == "Ally" ? 0 : 5;

            public int GuardEntry(string actor) => 2;

            public int Hurt(string actor) => 5;

            public int SpImpact(string actor) => 1;

            public int DamageDivisor(string actor) => 1;

            public AttackTiming Attack(string source, string target) => new(0, 10, new[] { 4 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 30, new[] { 3 });

            public BattlePhysicalStageProfile Physical(string actor) => new(_variant, actor == "Ally" ? 101 : 701,
                actor == "Ally" ? 0 : -1, 1, 1, Enum.GetValues(typeof(BattlePose)).Cast<BattlePose>().ToDictionary(pose => pose,
                    pose => new BattlePhysicalAnimation(pose.ToString(), 10, 10,
                        new[] { new BattlePhysicalMarker(4, BattlePhysicalMarkerKind.Work), new BattlePhysicalMarker(9, BattlePhysicalMarkerKind.Over) })));

            // Keep the native escape boundary short enough to put the summon
            // Work immediately before the old enemy-flee callback on tick 3.
            public BattleMovementPoint InitialHome(string actor) => actor == "Fleeing" ? new(790, 0, 790) : new(0, 0, actor == "Ally" ? 0 : 200);

            public BattleMovementPoint FormationHome(string actor, BattleSlot slot) => InitialHome(actor);

            public IReadOnlyList<BattleMovementPlan> PlanFormationMove(string actor, BattleMovementPoint from, BattleMovementPoint to, bool crossing) =>
                new[] { new BattleMovementPlan(BattleMovementKind.Run, from, to) };

            public BattleAttackPlan PlanAttack(string source, string target, BattleMovementPoint from, BattleMovementPoint to) =>
                new(Attack(source, target), from, from, to);

            public BattleMonsterCreation Prepare(int nativeIndex, int actorId, string instanceId) =>
                new(nativeIndex, Participant(instanceId, BattleSide.Enemy, (BattleSlot)(nativeIndex - 5), actorId),
                    new BattleActorDefinition(new BattleActorData { InstanceId = instanceId }));

            public void Commit(BattleMonsterCreation creation)
            {
            }
        }

        private sealed class Rules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) =>
                actor.InstanceId == "Fleeing" ? BattleSession.FullGauge - 1 : BattleSession.FullGauge;

            public int GaugePerStep(BattleParticipant actor) => 700;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => new(-100, false, false);
        }

        private static BattleParticipant Participant(string id, BattleSide side, BattleSlot slot, int actorId = 701) => new()
        {
            InstanceId = id,
            ActorId = actorId,
            Side = side,
            Slot = slot,
            Level = 10,
            Attributes = new[] { 1000, 100, 200, 60, 50, 4, 10, 0, 0, 0, 0, 0 },
            Vitals = new[] { 1000, 100, 200 }
        };

        private static (BattleSetup Setup, BattleDefinitions Definitions, BattleSession Session) Create(string variant)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "native-slot", DataFingerprint = "native-slot", Seed = variant == "PAL3" ? 1 : 2 };
            scenario.Participants.Add(Participant("Ally", BattleSide.Ally, BattleSlot.Water, 101));
            scenario.Participants.Add(Participant("Fleeing", BattleSide.Enemy, variant == "PAL3" ? BattleSlot.Water : BattleSlot.Fire));
            scenario.Participants.Add(Participant("Summoner", BattleSide.Enemy, BattleSlot.Center));
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = FinishEncounter, Level = 4, Mastered = true });
            scenario.Participants[2].Skills.Add(new BattleSkill { Id = Summon, Level = 4, Mastered = true });
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint, new[]
            {
                new BattleAbilityDefinition(new BattleAbilityData { Id = Summon, IsMagic = true, Range = BattleTargetRange.OpponentSingle, Handler = BattleSpecialHandler.Summon }),
                new BattleAbilityDefinition(new BattleAbilityData { Id = FinishEncounter, Range = BattleTargetRange.OpponentAll,
                    Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -1000000) } })
            }, Array.Empty<BattleAbilityDefinition>(), scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
            {
                InstanceId = actor.InstanceId,
                Identity = actor.Side == BattleSide.Ally ? 0 : -1,
                EscapePercent = actor.InstanceId == "Fleeing" ? 100 : 0
            })));
            var setup = new BattleSetup(scenario);
            var session = new BattleSession(setup, new Catalog(variant), new Rules(), definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            return (setup, definitions, session);
        }

        private static void Advance(BattleSession session, Func<bool> done, int maximum = 1000)
        {
            for (int step = 0; step < maximum && !done() && session.Status == BattleSessionStatus.Running; step++)
            {
                if (session.WaitingActor is { } waiting)
                    Assert.That(session.Submit(waiting, BattleAction.Defend), Is.True);
                session.Step();
            }
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
            Assert.That(done(), Is.True);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void OrdinaryEnemyFleeKeepsItsBodyUntilEscapeThenReleasesTheStage(string variant)
        {
            BattleSession session = Create(variant).Session;
            Assert.That(session.Submit("Ally", BattleAction.Defend), Is.True);
            Assert.That(session.Submit("Summoner", BattleAction.Defend), Is.True);
            session.Step();
            Assert.That(session.Actors.Single(actor => actor.Id == "Fleeing").Fleeing, Is.True);
            Assert.That(session.StageTasks.Single(task => task.Kind == "enemy flee").Finished, Is.False);
            var group = new BattleCommand("Ally", BattleAction.Skill, abilityId: FinishEncounter);
            Assert.That(session.GetTargetCandidates(group).Select(target => target.Id), Is.EqualTo(new[] { "Summoner" }));
            Advance(session, () => session.Actors.Single(actor => actor.Id == "Fleeing").Escaped);
            Advance(session, () => session.StageTasks.All(task => task.Kind != "enemy flee"));
            Assert.That(session.Events.Count(entry => entry.Source == "Fleeing" && entry.Kind == BattleEventKind.Escaped), Is.EqualTo(1));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void SummoningIntoTheJustEscapedSlotReleasesTheOldBodyLeaseAndReplays(string variant)
        {
            var fixture = Create(variant);
            BattleSession session = fixture.Session;
            Assert.That(session.Submit("Ally", BattleAction.Defend), Is.True);
            Assert.That(session.Submit(new BattleCommand("Summoner", BattleAction.Skill, "Ally", Summon)), Is.True);
            for (int step = 0; step < 3; step++)
                session.Step();
            Assert.That(session.MonsterGeneration("Fleeing"), Is.EqualTo(1));
            Assert.That(session.Events.Single(entry => entry.Source == "Fleeing" && entry.Kind == BattleEventKind.Escaped).Tick, Is.EqualTo(2));
            Assert.That(session.StageTasks.Single(task => task.Kind == "enemy flee").Finished, Is.True,
                "The recreated native role no longer belongs to its previous escape stage.");
            Advance(session, () => session.Events.Any(entry => entry.Source == "Fleeing" && entry.Kind == BattleEventKind.ActionEnded));
            Advance(session, () => session.WaitingActor == "Ally");
            Assert.That(session.Submit(new BattleCommand("Ally", BattleAction.Skill, "Fleeing", FinishEncounter)), Is.True);
            Advance(session, () => session.Status == BattleSessionStatus.Completed);
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Ally));
            Assert.That(session.StageTasks, Is.Empty);

            BattleSession replay = BattleReplay.Play(fixture.Setup, new Catalog(variant), session.Inputs, session.Tick,
                new Rules(), fixture.Definitions);
            Assert.That(replay.Status, Is.EqualTo(session.Status));
            Assert.That(replay.Result.Winner, Is.EqualTo(session.Result.Winner));
            Assert.That(replay.Events.Select(entry => (entry.Tick, entry.Kind, entry.Source, entry.Target, entry.ActionId, entry.ActualDelta, entry.RandomState)),
                Is.EqualTo(session.Events.Select(entry => (entry.Tick, entry.Kind, entry.Source, entry.Target, entry.ActionId, entry.ActualDelta, entry.RandomState))));
            Assert.That(replay.StageTasks, Is.Empty);
        }
    }
}
