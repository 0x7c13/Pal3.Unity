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

    public sealed class BattleAbilityPresentationTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings, IBattleAbilityEffects, IBattleStageGeometry
        {
            public BattleAbilityStageProfile Profile;

            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 1 });

            public AttackTiming Ability(string source, BattleAbilityDefinition ability) => new(0, Math.Max(1, Profile.BodyAnimationTicks), new[] { 0 }, bodyAnimationTicks: Profile.BodyAnimationTicks);

            public BattleAbilityStageProfile AbilityStage(string actor, BattleAbilityDefinition ability) => Profile;

            public IReadOnlyList<BattleMovementPlan> AbilityMovement(BattleActorSnapshot source, BattleActorSnapshot target, BattleAbilityDefinition ability, BattleDefinitions definitions, bool returning) =>
                new[] { new BattleMovementPlan(BattleMovementKind.Run, source.Position, returning ? source.Home : new BattleMovementPoint(0, 0, 20)) };

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 1;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "A" ? 0 : 1;

            public int DamageDivisor(string actor) => 1;

            public BattleMovementPoint InitialHome(string actor) => new(0, 0, actor == "A" ? 100 : 0);

            public BattleMovementPoint FormationHome(string actor, BattleSlot slot) => actor == "A" ? new(10, 20, 30) : new(-10, -20, -30);

            public BattleAttackPlan PlanAttack(string source, string target, BattleMovementPoint sourceHome, BattleMovementPoint targetHome) => new(Attack(source, target), sourceHome, sourceHome, targetHome);

            public IReadOnlyList<BattleMovementPlan> PlanFormationMove(string actor, BattleMovementPoint currentHome, BattleMovementPoint destination, bool passThroughMidpoint) => new[] { new BattleMovementPlan(BattleMovementKind.Run, currentHome, destination) };
        }

        private sealed class Rules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => actor.Side == BattleSide.Ally ? BattleSession.FullGauge : 0;

            public int GaugePerStep(BattleParticipant actor) => 1;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1) => new(-1, false, false);
        }

        private sealed class Director : IBattleEffectDirector, IBattleMovementAudio
        {
            public readonly List<(long Frame, long Handle, BattleEffectRequest Request)> Started = new();
            public readonly List<long> Stopped = new();
            private readonly Dictionary<long, (int Frames, bool Absolute)> _active = new();
            private long _frame, _next;
            public int FirstFrames = 5, SecondFrames = 5;
            public bool FailDuringAdvance, FailDuringCleanup;

            public long Start(BattleEffectRequest request)
            {
                long handle = ++_next;
                _active.Add(handle, (request.GroupId == 10 ? FirstFrames : SecondFrames, request.AbsoluteTime));
                Started.Add((_frame, handle, request));
                return handle;
            }

            public bool IsRunning(long handle) => _active.TryGetValue(handle, out var state) && state.Frames > 0;

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
                if (FailDuringAdvance)
                    throw new InvalidOperationException("effect update failed");
                _frame = frame;
                foreach (long handle in _active.Keys.ToArray())
                {
                    var state = _active[handle];
                    if ((state.Absolute ? unscaledSeconds : scaledSeconds) > 0)
                        _active[handle] = (state.Frames - 1, state.Absolute);
                }
            }

            public void Stop(long handle)
            {
                Stopped.Add(handle);
                _active.Remove(handle);
                if (FailDuringCleanup && handle == 1)
                    throw new InvalidOperationException("effect release failed");
            }

            public void PlayMovementSound(string actor, string sound, bool loop)
            {
            }

            public void StopMovementSound(string actor)
            {
            }

            public void StopBodySounds()
            {
                if (FailDuringCleanup)
                    throw new InvalidOperationException("audio release failed");
            }
        }

        private static BattleAbilityStageProfile Profile(BattleAbilityStageKind kind = BattleAbilityStageKind.Skill, float work = 0,
            int body = 5, BattleAbilityWorkBoundary boundary = BattleAbilityWorkBoundary.WorkTime,
            float wait1 = 0, float wait2 = 0.04f, bool move = false, int firstFlags = 4, int secondFlags = 8) =>
            new(kind, new BattleAbilityEffectBinding(10, wait1, firstFlags), new BattleAbilityEffectBinding(11, wait2, secondFlags), work, boundary, body, true,
                move ? BattleMovementKind.Run : null, move ? BattleMovementKind.Run : null);

        private static BattleScenario Scenario(int id = 4201, bool item = false, bool treasure = false)
        {
            var scenario = new BattleScenario { Scene = "fixture", DataFingerprint = "ability-presentation" };
            foreach (string instance in new[] { "A", "E" })
                scenario.Participants.Add(new BattleParticipant
                {
                    InstanceId = instance,
                    ActorId = instance == "A" ? 101 : 701,
                    Side = instance == "A" ? BattleSide.Ally : BattleSide.Enemy,
                    Attributes = new[] { 500, 100, 100, 100, 0, 3, 0, 0, 0, 0, 0, 0 },
                    Vitals = new[] { 500, 100, 100 }
                });
            if (item)
                scenario.Items.Add(new BattleItem { Id = id, Count = 2 });
            else
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = id, Mastered = true, Level = 4 });
            if (treasure)
                scenario.TreasureUses.Add(new BattleItem { Id = id, Count = 2 });
            return scenario;
        }

        private static BattleDefinitions Definitions(BattleScenario scenario, int id, bool magic = false, bool item = false, bool treasure = false, BattleTargetRange range = BattleTargetRange.OpponentSingle) =>
            new(scenario.Variant, scenario.DataFingerprint,
                item ? Array.Empty<BattleAbilityDefinition>() : new[] { Ability(id, magic, item, treasure, range) },
                item ? new[] { Ability(id, magic, item, treasure, range) } : Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData { InstanceId = actor.InstanceId, Identity = actor.InstanceId == "A" ? 0 : -1 })));

        private static BattleAbilityDefinition Ability(int id, bool magic, bool item, bool treasure, BattleTargetRange range) => new(new BattleAbilityData
        {
            Id = id,
            Name = "native ordinary ability",
            IsMagic = magic,
            SkillTypeId = magic ? 2 : 0,
            IsItem = item,
            IsTreasure = treasure,
            ItemType = item ? 2 : 0,
            SuccessRate = 100,
            Range = range,
            Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -20) }
        });

        private static void Until(BattleSession session, Func<bool> done)
        {
            for (int step = 0; step < 500 && !done() && session.Status == BattleSessionStatus.Running; step++)
                session.Step();
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
            Assert.That(done(), Is.True);
        }

        private static BattleSession Session(BattleScenario scenario, Catalog catalog, Director director, int id = 4201,
            bool magic = false, bool item = false, bool treasure = false, BattleTargetRange range = BattleTargetRange.OpponentSingle) =>
            new(new BattleSetup(scenario), catalog, new Rules(), Definitions(scenario, id, magic, item, treasure, range), director);

        [Test]
        public void OrdinaryGroupsUseBothWaitsAndCurrentHomesWithoutFreezingCombat()
        {
            var catalog = new Catalog { Profile = Profile(work: 0.1f) };
            var director = new Director();
            BattleSession session = Session(Scenario(), catalog, director);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201)), Is.True);
            Until(session, () => director.Started.Count == 2);
            Assert.That(director.Started[0].Request.GroupId, Is.EqualTo(10));
            Assert.That(director.Started[1].Frame, Is.GreaterThan(director.Started[0].Frame));
            Assert.That(director.Started.All(entry => !entry.Request.AbsoluteTime), Is.True);
            Assert.That(director.Started[0].Request.Origin.Z, Is.EqualTo(100));
            Assert.That(director.Started[1].Request.Origin.Z, Is.Zero);
            Assert.That(session.SimulationTick, Is.EqualTo(session.Tick));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit), Is.False);
            Until(session, () => session.Events.Any(entry => entry.Kind == BattleEventKind.Hit));
        }

        [Test]
        public void GroupTargetsUseTheOriginalFormationCenterInsteadOfAnArbitraryFirstActor()
        {
            var catalog = new Catalog { Profile = Profile(firstFlags: 2) };
            var director = new Director();
            BattleSession session = Session(Scenario(), catalog, director, range: BattleTargetRange.OpponentAll);
            session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 4201));
            Until(session, () => director.Started.Count > 0);
            BattleMovementPoint origin = director.Started[0].Request.Origin;
            Assert.That((origin.X, origin.Y, origin.Z), Is.EqualTo((-10f, -20f, -30f)));
        }

        [Test]
        public void ParallelNameAndLineupTasksKeepTheMagicActionOwnedAfterDamage()
        {
            var catalog = new Catalog { Profile = Profile(BattleAbilityStageKind.Magic, body: 1) };
            var director = new Director();
            BattleSession session = Session(Scenario(301), catalog, director, 301, magic: true);
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 301));
            Until(session, () => session.Events.Any(entry => entry.Kind == BattleEventKind.Hit));
            long workTick = session.Tick;
            Assert.That(session.Actors.Single(actor => actor.Id == "A").ActionId, Is.Not.Zero);
            Assert.That(workTick, Is.LessThan(50));
            Until(session, () => session.Events.Any(entry => entry.Kind == BattleEventKind.ActionEnded));
            Assert.That(session.Events.Single(entry => entry.Kind == BattleEventKind.ActionEnded).Tick, Is.GreaterThan(100));
        }

        [Test]
        public void StealWaitsForTheRoleBodyAfterItsWorkTime()
        {
            var catalog = new Catalog { Profile = Profile(body: 12, boundary: BattleAbilityWorkBoundary.WorkTimeAndBodyIdle) };
            var director = new Director();
            BattleSession session = Session(Scenario(4101), catalog, director, 4101);
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4101));
            Until(session, () => director.Started.Count > 0);
            long started = session.Tick;
            for (int step = 0; step < 5; step++)
                session.Step();
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit), Is.False);
            Until(session, () => session.Events.Any(entry => entry.Kind == BattleEventKind.Hit));
            Assert.That(session.Events.Single(entry => entry.Kind == BattleEventKind.Hit).Tick, Is.GreaterThanOrEqualTo(started + 12));
        }

        [Test]
        public void TreasureWaitsForGroupOneAndKeepsTheRoleIdle()
        {
            var catalog = new Catalog { Profile = Profile(BattleAbilityStageKind.Item, body: 0, boundary: BattleAbilityWorkBoundary.WorkTimeAndFirstGroupFinished) };
            var director = new Director { FirstFrames = 20 };
            BattleSession session = Session(Scenario(6119, item: true, treasure: true), catalog, director, 6119, item: true, treasure: true);
            session.Submit(new BattleCommand("A", BattleAction.Item, "E", 6119));
            Until(session, () => director.Started.Count > 0);
            long started = session.Tick;
            for (int step = 0; step < 10; step++)
            {
                session.Step();
                Assert.That(session.Actors.Single(actor => actor.Id == "A").Pose, Is.EqualTo(BattlePose.Idle));
            }
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit), Is.False);
            Until(session, () => session.Events.Any(entry => entry.Kind == BattleEventKind.Hit));
            Assert.That(session.Events.Single(entry => entry.Kind == BattleEventKind.Hit).Tick, Is.EqualTo(started + 20));
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(2));
            Assert.That(session.TreasureUses.Single().Count, Is.EqualTo(1));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ApproachAndReturnRestoreFacingBeforeTheRoleActionIsReleased(string variant)
        {
            var catalog = new Catalog { Profile = Profile(move: true) };
            var director = new Director();
            BattleScenario scenario = Scenario();
            scenario.Variant = variant;
            BattleSession session = Session(scenario, catalog, director);
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201));
            Until(session, () => director.Started.Count > 0);
            Assert.That(session.Actors.Single(actor => actor.Id == "A").Position.Z, Is.EqualTo(20));
            Until(session, () => session.Events.Any(entry => entry.Kind == BattleEventKind.ActionEnded));
            Assert.That(session.Actors.Single(actor => actor.Id == "A").Position.Z, Is.EqualTo(100));
            Assert.That(session.Actors.Single(actor => actor.Id == "A").FacingDegrees, Is.EqualTo(BattleFacingRules.Initial(BattleSide.Ally)));
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.MovementStarted), Is.EqualTo(2));
        }

        [Test]
        public void RetiredAbilityTailEffectsStillBelongToTheSession()
        {
            var catalog = new Catalog { Profile = Profile(body: 1) };
            var director = new Director { SecondFrames = 400 };
            BattleSession session = Session(Scenario(), catalog, director);
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201));
            Until(session, () => session.Events.Any(entry => entry.Kind == BattleEventKind.ActionEnded));
            long tail = director.Started.Single(entry => entry.Request.GroupId == 11).Handle;
            Assert.That(director.IsRunning(tail), Is.True);
            session.Abort();
            Assert.That(director.Stopped, Does.Contain(tail));
        }

        [Test]
        public void FaultRetainsItsOriginalReasonAndReleasesRemainingEffectsAfterCleanupErrors()
        {
            var catalog = new Catalog { Profile = Profile(work: 0.1f) };
            var director = new Director { FirstFrames = 400, SecondFrames = 400 };
            BattleSession session = Session(Scenario(), catalog, director);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201)), Is.True);
            Until(session, () => director.Started.Count == 2);
            director.FailDuringAdvance = director.FailDuringCleanup = true;

            Assert.DoesNotThrow(session.Step);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Faulted));
            Assert.That(session.Result.Reason, Is.EqualTo("InvalidOperationException: effect update failed"));
            Assert.That(session.Result.Winner, Is.Null);
            Assert.That(director.Stopped, Is.EquivalentTo(director.Started.Select(entry => entry.Handle)));
            Assert.That(session.StageTasks, Is.Empty);
            Assert.That(session.Actors.All(actor => actor.ActionId == 0), Is.True);
            Assert.That(session.Events.Where(entry => entry.Kind == BattleEventKind.PresentationCleanupFailed)
                .Select(entry => entry.Detail), Is.EquivalentTo(new[] { "Body sounds: audio release failed", "Effect 1: effect release failed" }));
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Ended), Is.EqualTo(1));
        }

        [Test]
        public void CancellingAnAbilityStopsItsPendingMovementAndEffects()
        {
            var catalog = new Catalog { Profile = Profile(move: true) };
            var director = new Director();
            BattleSession session = Session(Scenario(), catalog, director);
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201));
            Until(session, () => session.Events.Any(entry => entry.Kind == BattleEventKind.MovementStarted));
            session.CancelAction("A");
            float stopped = session.Actors.Single(actor => actor.Id == "A").Position.Z;
            for (int step = 0; step < 10; step++)
                session.Step();
            Assert.That(session.Actors.Single(actor => actor.Id == "A").Position.Z, Is.EqualTo(stopped));
            Assert.That(director.Started, Is.Empty);
        }
    }
}
