// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using Pal3.Battle;

    public sealed class BattlePhysicalPresentationTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleStageGeometry, IBattlePhysicalTimings, IBattleAbilityTimings, IBattleAbilityEffects, IBattleAbilityBodyAnimations
        {
            internal readonly BattleScenario Scenario;
            internal readonly Dictionary<string, BattlePhysicalStageProfile> Profiles = new();
            internal int AbilityBodyTicks = 200;
            internal float AbilityWorkSeconds;
            internal int AbilityEffectGroup = -1;
            internal bool AbilityMoves;

            internal Catalog(BattleScenario scenario, bool thrown = false)
            {
                Scenario = scenario;
                foreach (BattleParticipant actor in scenario.Participants)
                {
                    var animations = Enum.GetValues(typeof(BattlePose)).Cast<BattlePose>().ToDictionary(pose => pose,
                        pose => new BattlePhysicalAnimation(pose.ToString(), 5, 10, pose == BattlePose.Attacking ? new[] { new BattlePhysicalMarker(2, BattlePhysicalMarkerKind.Work) } : null, 480));
                    Profiles.Add(actor.InstanceId, new BattlePhysicalStageProfile(scenario.Variant, actor.ActorId,
                        actor.Side == BattleSide.Ally ? actor.InstanceId == "A" ? 0 : 1 : -1, thrown && actor.InstanceId == "A" ? 2 : 1, 1, animations,
                        thrownStart: new BattlePhysicalAnimation("z021", 5, 10, new[] { new BattlePhysicalMarker(2, BattlePhysicalMarkerKind.Throwed) }, 480),
                        thrownReturn: new BattlePhysicalAnimation("z022", 5, 10, new[] { new BattlePhysicalMarker(2, BattlePhysicalMarkerKind.Catched) }, 480)));
                }
            }

            public BattlePhysicalStageProfile Physical(string actor) => Profiles[actor];

            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 99; // native binding must finish on Over, independent of this estimate

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "A" ? 0 : actor == "B" ? 1 : 5;

            public int DamageDivisor(string actor) => 1;

            public BattleMovementPoint InitialHome(string actor) => actor == "A" ? new(0, 0, 0) : actor == "B" ? new(100, 0, 0) : new(Scenario.Participants.Count == 2 ? 100 : 200, 0, 0);

            public BattleMovementPoint FormationHome(string actor, BattleSlot slot) => InitialHome(actor);

            public BattleAttackPlan PlanAttack(string source, string target, BattleMovementPoint sourceHome, BattleMovementPoint targetHome) => new(Attack(source, target), sourceHome, sourceHome, targetHome);

            public IReadOnlyList<BattleMovementPlan> PlanFormationMove(string actor, BattleMovementPoint from, BattleMovementPoint to, bool midpoint) => new[] { new BattleMovementPlan(BattleMovementKind.Run, from, to) };

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, AbilityBodyTicks, new[] { 0 }, bodyAnimationTicks: AbilityBodyTicks);

            public BattleAbilityStageProfile AbilityStage(string actor, BattleAbilityDefinition ability) => new(BattleAbilityStageKind.Skill,
                new BattleAbilityEffectBinding(AbilityEffectGroup, 0, 0), new BattleAbilityEffectBinding(-1, 0, 0), AbilityWorkSeconds, BattleAbilityWorkBoundary.WorkTime, AbilityBodyTicks, true,
                AbilityMoves ? BattleMovementKind.Run : null, AbilityMoves ? BattleMovementKind.Run : null);

            public IReadOnlyList<BattleMovementPlan> AbilityMovement(BattleActorSnapshot actor, BattleActorSnapshot target, BattleAbilityDefinition ability,
                BattleDefinitions definitions, bool returning) => new[] { new BattleMovementPlan(BattleMovementKind.Run, actor.Position, actor.Home) };

            public BattlePhysicalAnimation AbilityBody(string actor, BattleAbilityDefinition ability) => new("skill-long", AbilityBodyTicks, 10, gameBoxDuration: checked((uint)AbilityBodyTicks * 96));

            public int? AbilityNativeAction(string actor, BattleAbilityDefinition ability) => 35;

            public BattlePhysicalAnimation AbilityMovementBody(string actor, BattleAbilityDefinition ability, BattleMovementKind kind, bool returning) =>
                Physical(actor).Animation(BattlePose.Moving);
        }

        private sealed class Rules : IBattleRules
        {
            internal readonly Dictionary<string, int> InitialGauges = new();
            internal int GaugeIncrement;

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) =>
                InitialGauges.GetValueOrDefault(actor.InstanceId, BattleSession.FullGauge);

            public int GaugePerStep(BattleParticipant actor) => GaugeIncrement;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1) => new(-10, false, false);
        }

        private sealed class Effects : IBattleEffectDirector, IBattleModalEffects
        {
            internal readonly List<BattleEffectRequest> Started = new();
            internal readonly List<long> Stopped = new();
            internal bool KeepRunning;

            public bool BlocksBattleTime
            {
                get; set;
            }

            public bool SkipModalPresentation()
            {
                if (!BlocksBattleTime)
                    return false;
                BlocksBattleTime = false;
                return true;
            }

            private readonly HashSet<long> _running = new();
            private long _nextHandle;

            public long Start(BattleEffectRequest request)
            {
                Started.Add(request);
                long handle = ++_nextHandle;
                if (KeepRunning)
                    _running.Add(handle);
                return handle;
            }

            public bool IsRunning(long handle) => _running.Contains(handle);

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void Stop(long handle)
            {
                Stopped.Add(handle);
                _running.Remove(handle);
            }
        }

        private static BattleSession Session(string variant, out Catalog catalog, bool party = false, bool thrown = false, bool skill = false,
            IBattleEffectDirector effects = null, Action<BattleScenario> configure = null, Rules rules = null,
            BattleTargetRange enemyNormalRange = BattleTargetRange.OpponentSingle)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "fixture", DataFingerprint = "physical-native", Seed = 1 };
            scenario.Participants.Add(Participant("A", 101, BattleSide.Ally, BattleSlot.Center));
            if (party)
                scenario.Participants.Add(Participant("B", 102, BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(Participant("E", 701, BattleSide.Enemy, BattleSlot.Water));
            BattleAbilityDefinition ability = null;
            if (skill)
            {
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = 4201, Mastered = true, Level = 4 });
                ability = new BattleAbilityDefinition(new BattleAbilityData
                {
                    Id = 4201,
                    Name = "native role ownership",
                    SuccessRate = 100,
                    Range = BattleTargetRange.OpponentSingle,
                    Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -1) }
                });
            }
            configure?.Invoke(scenario);
            catalog = new Catalog(scenario, thrown);
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint, ability == null ? Array.Empty<BattleAbilityDefinition>() : new[] { ability }, Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.InstanceId == "A" ? 0 : actor.InstanceId == "B" ? 1 : -1,
                    NormalAttackMode = 924,
                    NormalAttackRange = actor.Side == BattleSide.Enemy ? enemyNormalRange : BattleTargetRange.OpponentSingle
                })));
            var session = new BattleSession(new BattleSetup(scenario), catalog, rules ?? new Rules(), definitions, effects);
            session.SetAutomatic(BattleSide.Enemy, false);
            return session;
        }

        private static BattleParticipant Participant(string id, int resource, BattleSide side, BattleSlot slot) => new()
        {
            InstanceId = id,
            ActorId = resource,
            Side = side,
            Slot = slot,
            Attributes = new[] { 500, 100, 200, 100, 0, 3, 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { 500, 10, 0 }
        };

        private static object Actor(BattleSession session, string id) => ((IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session))[id];

        private static object Invoke(BattleSession session, string method, params object[] args)
        {
            MethodInfo member = typeof(BattleSession).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            object[] supplied = args.Concat(member.GetParameters().Skip(args.Length).Select(parameter =>
                parameter.IsOptional ? parameter.DefaultValue : throw new ArgumentException("A required private test argument is missing."))).ToArray();
            return member.Invoke(session, supplied);
        }

        private static BattleActorSnapshot Snapshot(BattleSession session, string id) => session.Actors.Single(actor => actor.Id == id);

        private static void Steps(BattleSession session, int count)
        {
            for (int index = 0; index < count; index++)
                session.Step();
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
        }

        [TestCase("PAL3", BattleTargetRange.OpponentAll)]
        [TestCase("PAL3A", BattleTargetRange.OpponentAll)]
        [TestCase("PAL3A", BattleTargetRange.OpponentRow)]
        public void EnemyMultiTargetAttackSettlesAtAnUnsetSceneElement(string variant, BattleTargetRange range)
        {
            var rules = new Rules();
            rules.InitialGauges["A"] = rules.InitialGauges["B"] = 0;
            BattleSession session = Session(variant, out _, party: true, rules: rules, enemyNormalRange: range,
                configure: scenario => scenario.Participants.Single(actor => actor.InstanceId == "B").Slot = BattleSlot.Fire);
            var command = new BattleCommand("E", BattleAction.Attack, "A");
            Assert.That(session.GetPotentialAffectedTargets(command).Select(actor => actor.Id), Is.EquivalentTo(new[] { "A", "B" }));
            Assert.That(session.Submit(command), Is.True);
            Steps(session, 250);
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.ActionEnded && entry.Source == "E"), Is.True);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void FormationCannotChangeWhileTheModalFrameIsSuspended(string variant)
        {
            var effects = new Effects();
            BattleSession session = Session(variant, out _, party: true, effects: effects);
            Assert.That(session.BeginFormation("A"), Is.True);
            effects.BlocksBattleTime = true;
            session.Step();
            Assert.That(session.SwapFormation("B", BattleSlot.Fire), Is.False);
            Assert.That(session.EndFormation("A"), Is.False);
            Assert.That(session.SkipModalPresentation(), Is.True);
            Assert.That(session.IsModalPresentation, Is.False);
            Assert.That(session.SwapFormation("B", BattleSlot.Fire), Is.False);
            Assert.That(session.EndFormation("A"), Is.False);
            session.Step();
            Assert.That(session.SwapFormation("B", BattleSlot.Fire), Is.True);
            Assert.That(session.EndFormation("A"), Is.True);
        }

        [TestCase("formation")]
        [TestCase("protection")]
        [TestCase("sleep")]
        [TestCase("paralysis")]
        public void Pal3AFiveSpiritFieldCountsTheAnchorsSpentTurn(string operation)
        {
            BattleSession session = Session("PAL3A", out _, party: true, configure: scenario =>
            {
                scenario.FiveSpiritElement = 1;
                scenario.FiveSpiritForce = 2;
                if (operation is "sleep" or "paralysis")
                    scenario.Participants[0].Statuses.Add(new BattleStatus
                    {
                        Id = (int)(operation == "sleep" ? BattleState.Sleep : BattleState.Paralysis),
                        Rounds = 8
                    });
            });
            // Set up the fifth native turn and check each public dispatch path independently.
            typeof(BattleSession).GetField("_fiveSpiritRounds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, 4);
            switch (operation)
            {
                case "formation":
                    Assert.That(session.BeginFormation("A"), Is.True);
                    Assert.That(session.EndFormation("A"), Is.True);
                    Assert.That(session.FiveSpiritElement, Is.EqualTo(1));
                    Assert.That(session.BeginFormation("A"), Is.True);
                    Assert.That(session.SwapFormation("B", BattleSlot.Fire), Is.True);
                    Assert.That(session.EndFormation("A"), Is.True);
                    break;
                case "protection":
                    Assert.That(session.SetProtection("A", "B"), Is.True);
                    break;
                default:
                    Steps(session, 1);
                    break;
            }
            Assert.That(session.FiveSpiritElement, Is.Zero);
            Assert.That(session.FiveSpiritForce, Is.Zero);
        }

        private sealed class BodyReservation : IBattleStageJob
        {
            internal BattleSession Session;
            internal object Actor;
            internal bool Release;
            internal long ReleasedTick;

            public bool Advance(BattleStageContext context)
            {
                if (!Release)
                    return false;
                Invoke(Session, "ReleaseStageBody", Actor, context.Handle);
                ReleasedTick = context.Tick;
                return true;
            }
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AttackUsesNativeSpeedAndCurrentClipRadiusThenReturnsHome(string variant)
        {
            BattleSession session = Session(variant, out _);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Attack, "E")), Is.True);
            Steps(session, 1);
            Assert.That(Snapshot(session, "A").Position.X, Is.EqualTo(7.6f));
            Steps(session, 10);
            Assert.That(Snapshot(session, "A").Position.X, Is.EqualTo(80f));
            Steps(session, 30);
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Hit && entry.Target == "E"), Is.EqualTo(1));
            Assert.That(Snapshot(session, "A").Position.X, Is.Zero);
            Assert.That(Snapshot(session, "A").CurrentAction, Is.Null);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("Idle"));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RoundLimitKeepsItsVictoryReasonWhenAnActiveAttackKillsTheLastAlly(string variant)
        {
            var rules = new Rules { GaugeIncrement = 1 };
            rules.InitialGauges.Add("A", 0);
            rules.InitialGauges.Add("C", BattleSession.FullGauge - 1);
            BattleSession session = Session(variant, out Catalog catalog, rules: rules, configure: scenario =>
            {
                scenario.RoundLimit = 1;
                scenario.Participants[0].Vitals[0] = 1;
                scenario.Participants.Add(Participant("C", 702, BattleSide.Enemy, BattleSlot.Center));
            });
            Assert.That(session.Submit("E", BattleAction.Attack, "A"), Is.True);
            Steps(session, 1);
            Assert.That(session.BossRounds, Is.EqualTo(1));
            Assert.That(session.IsResolvingOutcome, Is.True);
            Assert.That(Snapshot(session, "A").Hp, Is.EqualTo(1));

            Steps(session, 200);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed));
            Assert.That(session.Result.PreSettlementActors.Single(actor => actor.Id == "A").Hp, Is.Zero);
            Assert.That(session.Result.PreSettlementActors.Where(actor => actor.Side == BattleSide.Enemy).All(actor => actor.Alive), Is.True);
            Assert.That(session.Result.Winner, Is.EqualTo(BattleSide.Ally));
            Assert.That(session.Result.Reason, Is.EqualTo("original boss round limit reached"));

            BattleSession replay = BattleReplay.Play(new BattleSetup(catalog.Scenario), new Catalog(catalog.Scenario),
                session.Inputs, session.Tick, rules, session.Definitions);
            Assert.That(replay.Result.Winner, Is.EqualTo(session.Result.Winner));
            Assert.That(replay.Result.Reason, Is.EqualTo(session.Result.Reason));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void CancellingAnAttackApproachRestoresHomeAndReplaysTheNextEnemyHit(string variant)
        {
            BattleSession session = Session(variant, out Catalog catalog);
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Steps(session, 4);
            Assert.That(Snapshot(session, "A").Position.X, Is.GreaterThan(13));
            Assert.That(session.CancelAction("A"), Is.True);
            Assert.That(Snapshot(session, "A").Movement, Is.Null);
            Assert.That(Snapshot(session, "A").Position, Is.EqualTo(Snapshot(session, "A").Home));

            Assert.That(session.Submit("E", BattleAction.Attack, "A"), Is.True);
            Steps(session, 200);
            Assert.That(Snapshot(session, "A").Hp, Is.LessThan(500));
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Hit && entry.Source == "E" && entry.Target == "A"), Is.EqualTo(1));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit && entry.Source == "A"), Is.False);

            BattleSession replay = BattleReplay.Play(new BattleSetup(catalog.Scenario), new Catalog(catalog.Scenario),
                session.Inputs, session.Tick, new Rules(), session.Definitions);
            Assert.That(Snapshot(replay, "A").Position, Is.EqualTo(Snapshot(session, "A").Position));
            Assert.That(Snapshot(replay, "A").Hp, Is.EqualTo(Snapshot(session, "A").Hp));
            Assert.That(replay.Events.Select(entry => (entry.Tick, entry.Kind, entry.RandomState, entry.ActualDelta)),
                Is.EqualTo(session.Events.Select(entry => (entry.Tick, entry.Kind, entry.RandomState, entry.ActualDelta))));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void CancellingAnActionWaitingForAnotherStageLeavesThatStagesBodyInPlace(string variant)
        {
            BattleSession session = Session(variant, out _, skill: true);
            object actor = Actor(session, "A");
            var scheduler = (BattleStageScheduler)typeof(BattleSession).GetField("_stage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            BattleStageHandle owner = scheduler.Add("A", "existing normal grab", new BodyReservation { Session = session, Actor = actor }, 0);
            Assert.That(Invoke(session, "TryGrabStageBody", actor, owner), Is.True);
            var position = new BattleMovementPoint(30, 0, 0);
            session.SetEffectActorPosition("A", position);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201)), Is.True);
            Steps(session, 1);

            Assert.That(session.CancelAction("A"), Is.True);
            Assert.That(Snapshot(session, "A").Position, Is.EqualTo(position));
            Assert.That(Invoke(session, "OwnsStageBody", actor, owner), Is.True);
            Assert.That(session.StageTasks.Single().Handle, Is.EqualTo(owner));
        }

        [TestCase("PAL3", 6)]
        [TestCase("PAL3A", 7)]
        public void IdleActorBlockFinishesOnNativeOverInsteadOfCatalogHurtEstimate(string variant, int endPass)
        {
            BattleSession session = Session(variant, out _);
            Invoke(session, "PushBodyBlock", Actor(session, "A"), false);
            Steps(session, 1);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("Hurt"));
            Assert.That(Snapshot(session, "A").NativeAnimationSeconds, Is.EqualTo(62 / 4800d));
            Steps(session, endPass - 1);
            Assert.That((bool)Invoke(session, "PhysicalBodyBusy", Actor(session, "A")), Is.False);
            Steps(session, 1);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("Idle"));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void BlockUsesDefenceClipAndRestartDoesNotSelectIdle(string variant)
        {
            BattleSession session = Session(variant, out _);
            object actor = Actor(session, "A");
            actor.GetType().GetField("Guard").SetValue(actor, true);
            Invoke(session, "PushBodyBlock", actor, false);
            Steps(session, 2);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("GuardHurt"));
            Invoke(session, "PushBodyBlock", actor, false);
            Steps(session, 1);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("GuardHurt"));
            Assert.That(Snapshot(session, "A").NativeAnimationSeconds, Is.EqualTo((variant == "PAL3" ? 124 : 96) / 4800d));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void DefenceBecomesActiveOnlyWhenStartBodyPopsOver(string variant)
        {
            BattleSession session = Session(variant, out _);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Defend)), Is.True);
            Steps(session, 1);
            Assert.That(Snapshot(session, "A").Guarding, Is.False);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("GuardEntry"));
            Steps(session, 6);
            Assert.That(Snapshot(session, "A").Guarding, Is.True);
            Steps(session, 4);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("Guard"));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AttackDistanceUsesActualRetainedClipBoundsAtStageTime(string variant)
        {
            BattleSession session = Session(variant, out _);
            var binding = (BattleBodyAnimationBinding)Invoke(session, "GetBodyBinding", Actor(session, "E"));
            binding.TryPlay(new BattlePhysicalAnimation("retained", 5, 40), true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Attack, "E")), Is.True);
            Steps(session, 7);
            Assert.That(Snapshot(session, "A").Position.X, Is.EqualTo(50f));
        }

        [TestCase("PAL3", 124, "Dying")]
        [TestCase("PAL3", 125, "Idle")]
        [TestCase("PAL3A", 124, "Dying")]
        [TestCase("PAL3A", 125, "Idle")]
        public void ProperIdleAnimationUsesNativeQuarterHpBoundary(string variant, int hp, string clip)
        {
            BattleSession session = Session(variant, out _);
            object actor = Actor(session, "A");
            actor.GetType().GetField("Hp").SetValue(actor, hp);
            Steps(session, 1);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo(clip));
        }

        [Test]
        public void ThrownWeaponRetainsItsCommandAfterStageReleasesAndRestoresBodyOnLanding()
        {
            BattleSession session = Session("PAL3A", out _, thrown: true);
            object target = Actor(session, "E");
            foreach (string field in new[] { "Position", "Home" })
                target.GetType().GetField(field).SetValue(target, new BattleMovementPoint(600, 0, 0));
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Attack, "E")), Is.True);
            Steps(session, 12);
            Assert.That(Snapshot(session, "A").CurrentAction, Is.Null);
            Assert.That(session.ThrownWeapons.Single().Active, Is.True);
            Assert.That(session.WeaponDrawingEnabled("A"), Is.False);
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Hit && entry.Target == "E"), Is.Zero);
            Steps(session, 120);
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Hit && entry.Target == "E"), Is.EqualTo(1));
            Assert.That(Snapshot(session, "A").Position.X, Is.Zero);
            Assert.That(session.ThrownWeapons.Single().Active, Is.False);
            Assert.That(session.WeaponDrawingEnabled("A"), Is.True);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("Idle"));
        }

        [Test]
        public void ExplicitCancellationStopsItsLaunchedWeaponAndPreventsDelayedDamage()
        {
            BattleSession session = Session("PAL3A", out _, thrown: true);
            object target = Actor(session, "E");
            foreach (string field in new[] { "Position", "Home" })
                target.GetType().GetField(field).SetValue(target, new BattleMovementPoint(600, 0, 0));
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Attack, "E")), Is.True);
            long actionId = Snapshot(session, "A").ActionId;
            for (int index = 0; index < 20 && !session.ThrownWeapons.Any(weapon => weapon.Active); index++)
                Steps(session, 1);
            Assert.That(session.ThrownWeapons.Single().Active, Is.True);
            Assert.That(Snapshot(session, "A").CurrentAction, Is.EqualTo(BattleAction.Attack));
            Assert.That(session.CancelAction("A"), Is.True);
            Assert.That(session.ThrownWeapons, Is.Empty);
            Assert.That(session.WeaponDrawingEnabled("A"), Is.True);
            Steps(session, 120);
            Assert.That(Snapshot(session, "E").Hp, Is.EqualTo(500));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit && entry.ActionId == actionId), Is.False);
            Assert.That(Snapshot(session, "A").CurrentAction, Is.Null);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("Idle"));
        }

        [Test]
        public void CancellingANewerCommandPreservesAnOlderIndependentWeaponFlight()
        {
            BattleSession session = Session("PAL3A", out _, thrown: true);
            object target = Actor(session, "E");
            foreach (string field in new[] { "Position", "Home" })
                target.GetType().GetField(field).SetValue(target, new BattleMovementPoint(600, 0, 0));
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Attack, "E")), Is.True);
            long attackId = Snapshot(session, "A").ActionId;
            Steps(session, 12);
            Assert.That(Snapshot(session, "A").CurrentAction, Is.Null);
            Assert.That(session.ThrownWeapons.Single().Active, Is.True);
            object actor = Actor(session, "A");
            actor.GetType().GetField("Gauge").SetValue(actor, BattleSession.FullGauge);
            actor.GetType().GetField("Ready").SetValue(actor, true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Defend)), Is.True);
            Assert.That(Snapshot(session, "A").ActionId, Is.Not.EqualTo(attackId));
            Steps(session, 1);
            Assert.That(Snapshot(session, "A").CurrentAction, Is.EqualTo(BattleAction.Defend));
            Assert.That(session.CancelAction("A"), Is.True);
            Assert.That(session.ThrownWeapons.Single().Active, Is.True);
            Assert.That(session.WeaponDrawingEnabled("A"), Is.False);
            Steps(session, 120);
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Hit && entry.ActionId == attackId), Is.EqualTo(1));
            Assert.That(Snapshot(session, "E").Hp, Is.LessThan(500));
            Assert.That(session.ThrownWeapons.Single().Active, Is.False);
            Assert.That(session.WeaponDrawingEnabled("A"), Is.True);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ProtectedCriticalFeedbackUsesTheActualDamageReceiver(string variant)
        {
            var effects = new Effects();
            BattleSession session = Session(variant, out _, party: true, effects: effects);
            Assert.That(session.SetProtection("A", "B"), Is.True);
            object target = Actor(session, "B");
            target.GetType().GetField("Hp").SetValue(target, 50);
            object enemy = Actor(session, "E");
            ((int[])enemy.GetType().GetField("Attributes").GetValue(enemy))[6] = 200;
            Assert.That(Invoke(session, "SubmitCommand", new BattleCommand("E", BattleAction.Attack, "B"), true), Is.True);
            for (int index = 0; index < 100 && effects.Started.All(request => request.GroupId != 470); index++)
                Steps(session, 1);
            BattleActorSnapshot guardian = Snapshot(session, "A");
            Assert.That(guardian.Hp, Is.LessThan(500));
            Assert.That(Snapshot(session, "B").Hp, Is.EqualTo(50));
            BattleEffectRequest critical = effects.Started.Single(request => request.GroupId == 470);
            Assert.That(critical.Source, Is.EqualTo("A"));
            Assert.That(critical.Targets, Is.EqualTo(new[] { "A" }));
            Assert.That(critical.Origin, Is.EqualTo(guardian.Position));
            Assert.That(critical.Origin, Is.Not.EqualTo(Snapshot(session, "B").Position));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void LethalHitDuringExternalControlWaitsForReleaseAndHurtOverBeforeModelDeath(string variant)
        {
            BattleSession session = Session(variant, out _, party: true, skill: true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201)), Is.True);
            Steps(session, 4);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("skill-long"));
            session.SetEffectActorControlled("A", true);
            Invoke(session, "ChangeHp", Actor(session, "A"), -1000, null, 0L, "external-control lethal fixture", true, false, false);
            Steps(session, 20);
            Assert.That(Snapshot(session, "A").Hp, Is.Zero);
            Assert.That(Snapshot(session, "A").AliveFlag, Is.False);
            Assert.That(Snapshot(session, "A").ModelAliveFlag, Is.True);
            Assert.That(Snapshot(session, "A").CurrentAction, Is.Null);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("skill-long"));
            session.SetEffectActorControlled("A", false);
            Steps(session, 1);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("Hurt"));
            Assert.That(Snapshot(session, "A").ModelAliveFlag, Is.True);
            for (int index = 0; index < 20 && Snapshot(session, "A").ModelAliveFlag; index++)
            {
                Assert.That(session.CaptureBodyAnimation("A").LastPoppedMarker, Is.Not.EqualTo(BattlePhysicalMarkerKind.Over));
                Steps(session, 1);
            }
            Assert.That(session.CaptureBodyAnimation("A").LastPoppedMarker, Is.EqualTo(BattlePhysicalMarkerKind.Over));
            Assert.That(Snapshot(session, "A").ModelAliveFlag, Is.False);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("Hurt"));
            Steps(session, 1);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("Dead"));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AStartedEffectWithoutActorControlContinuesAfterItsOwnerDies(string variant)
        {
            var effects = new Effects { KeepRunning = true };
            BattleSession session = Session(variant, out Catalog catalog, party: true, skill: true, effects: effects);
            catalog.AbilityEffectGroup = 7;
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201)), Is.True);
            Steps(session, 4);
            Assert.That(effects.Started.Single().GroupId, Is.EqualTo(7));
            Assert.That(session.IsActorExternallyControlled("A"), Is.False);
            Invoke(session, "ChangeHp", Actor(session, "A"), -1000, null, 0L, "independent-effect lethal fixture", true, false, false);
            Steps(session, 20);
            Assert.That(Snapshot(session, "A").Hp, Is.Zero);
            Assert.That(Snapshot(session, "A").AliveFlag, Is.False);
            Assert.That(Snapshot(session, "A").ModelAliveFlag, Is.False);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("Dead"));
            Assert.That(effects.IsRunning(1), Is.True);
            Assert.That(effects.Stopped, Is.Empty);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ProtectionSelectionCostsControllerOnlyWhenChangedAndNativeDirectorIntercepts(string variant)
        {
            BattleSession session = Session(variant, out _, true);
            Assert.That(session.SetProtection("A", "B"), Is.True);
            Assert.That(Snapshot(session, "A").Gauge, Is.Zero);
            object protectedActor = Actor(session, "B");
            protectedActor.GetType().GetField("Hp").SetValue(protectedActor, 50);
            Assert.That(Invoke(session, "SubmitCommand", new BattleCommand("E", BattleAction.Attack, "B"), true), Is.True);
            Steps(session, 70);
            Assert.That(Snapshot(session, "B").Hp, Is.EqualTo(50));
            Assert.That(Snapshot(session, "A").Hp, Is.LessThan(500));
            Assert.That(session.ProtectionTarget, Is.EqualTo("B"));
            Assert.That(Snapshot(session, "A").Position.X, Is.Zero);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ProtectionCannotStealAnActiveAbilityBodyFromTheSharedNativeRoleQueue(string variant)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "fixture", DataFingerprint = "physical-ownership", Seed = 1 };
            scenario.Participants.Add(Participant("A", 101, BattleSide.Ally, BattleSlot.Center));
            scenario.Participants.Add(Participant("B", 102, BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(Participant("E", 701, BattleSide.Enemy, BattleSlot.Water));
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 4201, Mastered = true, Level = 4 });
            var ability = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 4201,
                Name = "long native body",
                SuccessRate = 100,
                Range = BattleTargetRange.OpponentSingle,
                Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -1) }
            });
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint, new[] { ability }, Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.InstanceId == "A" ? 0 : actor.InstanceId == "B" ? 1 : -1,
                    NormalAttackMode = 924
                })));
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(scenario), new Rules(), definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(session.SetProtection("A", "B"), Is.True);
            object guardian = Actor(session, "A");
            guardian.GetType().GetField("Gauge").SetValue(guardian, BattleSession.FullGauge);
            guardian.GetType().GetField("Ready").SetValue(guardian, true);
            object target = Actor(session, "B");
            target.GetType().GetField("Hp").SetValue(target, 50);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201)), Is.True);
            Steps(session, 20);
            Assert.That((bool)Invoke(session, "HasActiveOrdinaryAbilityBody", guardian), Is.True);
            Assert.That((bool)Invoke(session, "PhysicalBodyIdle", guardian), Is.False);
            Assert.That(Invoke(session, "SubmitCommand", new BattleCommand("E", BattleAction.Attack, "B"), true), Is.True);
            Steps(session, 40);
            Assert.That(session.StageTasks.Any(task => task.Kind == "protection"), Is.False);
            Assert.That(Snapshot(session, "A").Position.X, Is.Zero);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("skill-long"));
            Assert.That(Snapshot(session, "B").Hp, Is.LessThan(50));
            Steps(session, 250);
            Assert.That(Snapshot(session, "A").CurrentAction, Is.Null);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("Idle"));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ProtectionWaitsForTheAbilityStageReleaseAfterItsBodyFinishes(string variant)
        {
            BattleSession session = Session(variant, out Catalog catalog, party: true, skill: true);
            catalog.AbilityBodyTicks = 5;
            catalog.AbilityWorkSeconds = 1.2f;
            Assert.That(session.SetProtection("A", "B"), Is.True);
            object guardian = Actor(session, "A");
            guardian.GetType().GetField("Gauge").SetValue(guardian, BattleSession.FullGauge);
            guardian.GetType().GetField("Ready").SetValue(guardian, true);
            object target = Actor(session, "B");
            target.GetType().GetField("Hp").SetValue(target, 50);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201)), Is.True);
            Steps(session, 20);
            Assert.That((bool)Invoke(session, "PhysicalBodyIdle", guardian), Is.True);
            Assert.That(Snapshot(session, "A").CurrentAction, Is.EqualTo(BattleAction.Skill));
            BattleStageHandle ability = session.StageTasks.Single(task => task.Owner == "A").Handle;
            Assert.That(Invoke(session, "SubmitCommand", new BattleCommand("E", BattleAction.Attack, "B"), true), Is.True);
            Steps(session, 20);
            Assert.That(session.StageTasks.Any(task => task.Kind == "protection"), Is.True);
            Assert.That(Snapshot(session, "A").Position.X, Is.Zero);
            for (int index = 0; index < 100 && Snapshot(session, "A").Position.X == 0; index++)
                Steps(session, 1);
            Assert.That(Snapshot(session, "A").Position.X, Is.GreaterThan(0));
            Assert.That(session.StageTasks.Single(task => task.Handle == ability).Finished, Is.True,
                "The higher protection slot must acquire the role in the same pass as ability Release.");
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AbilityWaitsForAnotherStageGrabAndAcquiresOnItsReleasePass(string variant)
        {
            BattleSession session = Session(variant, out _, skill: true);
            object actor = Actor(session, "A");
            var scheduler = (BattleStageScheduler)typeof(BattleSession).GetField("_stage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            var reservation = new BodyReservation { Session = session, Actor = actor };
            BattleStageHandle owner = scheduler.Add("A", "existing normal grab", reservation, 0);
            Assert.That(Invoke(session, "TryGrabStageBody", actor, owner), Is.True);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201)), Is.True);
            Steps(session, 20);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("Idle"));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit), Is.False);
            reservation.Release = true;
            Steps(session, 1);
            var owners = (IDictionary)typeof(BattleSession).GetField("_bodyStageOwners", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            BattleStageHandle ability = session.StageTasks.Single(task => task.Kind == "ability").Handle;
            Assert.That(owners["A"], Is.EqualTo(ability.Id));
            Assert.That(reservation.ReleasedTick, Is.EqualTo(session.SimulationTick));
            Steps(session, 4);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("skill-long"));
            session.CancelAction("A");
            Assert.That(owners.Contains("A"), Is.False);
            session.Abort();
            Assert.That(owners.Count, Is.Zero);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RetiringReleasedAbilityDoesNotClearItsSuccessorsMovement(string variant)
        {
            BattleSession session = Session(variant, out Catalog catalog, party: true, skill: true);
            catalog.AbilityBodyTicks = 5;
            catalog.AbilityMoves = true;
            Assert.That(session.SetProtection("A", "B"), Is.True);
            object guardian = Actor(session, "A");
            guardian.GetType().GetField("Gauge").SetValue(guardian, BattleSession.FullGauge);
            guardian.GetType().GetField("Ready").SetValue(guardian, true);
            object target = Actor(session, "B");
            target.GetType().GetField("Hp").SetValue(target, 50);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201)), Is.True);
            Steps(session, 40);
            Assert.That(Snapshot(session, "A").CurrentAction, Is.EqualTo(BattleAction.Skill), "The parallel name task still owns the command, after role Release.");
            Assert.That(Invoke(session, "SubmitCommand", new BattleCommand("E", BattleAction.Attack, "B"), true), Is.True);
            Steps(session, 11);
            Assert.That(Snapshot(session, "A").Movement, Is.Not.Null);
            Steps(session, 100);
            Assert.That(Snapshot(session, "A").Position.X, Is.Zero);
            Assert.That(Snapshot(session, "A").CurrentAction, Is.Null);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AbilityBecomingReadyDuringProtectionWaitsForTheProtectorsRelease(string variant)
        {
            BattleSession session = Session(variant, out _, party: true, skill: true);
            Assert.That(session.SetProtection("A", "B"), Is.True);
            object guardian = Actor(session, "A");
            guardian.GetType().GetField("Gauge").SetValue(guardian, BattleSession.FullGauge);
            guardian.GetType().GetField("Ready").SetValue(guardian, true);
            object target = Actor(session, "B");
            target.GetType().GetField("Hp").SetValue(target, 50);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201)), Is.True);
            object run = guardian.GetType().GetField("Run").GetValue(guardian);
            // Hold the native second-gauge wait, then complete it while the
            // protector owns the role. This avoids an artificial ready command
            // that the public input gate correctly rejects during movement.
            run.GetType().GetField("Casting").SetValue(run, true);
            guardian.GetType().GetField("Gauge").SetValue(guardian, BattleSession.FullGauge);
            Assert.That(Invoke(session, "SubmitCommand", new BattleCommand("E", BattleAction.Attack, "B"), true), Is.True);
            Steps(session, 4);
            Assert.That(Snapshot(session, "A").Position.X, Is.GreaterThan(0));
            run.GetType().GetField("Casting").SetValue(run, false);
            run.GetType().GetField("TimelineOriginTick").SetValue(run, session.SimulationTick);
            Steps(session, 20);
            Assert.That(session.StageTasks.Any(task => task.Kind == "ability"), Is.True);
            Assert.That((bool)Invoke(session, "HasActiveOrdinaryAbilityBody", guardian), Is.False);
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit && entry.Source == "A"), Is.False);
            for (int index = 0; index < 200 && Snapshot(session, "A").NativeAnimationClip != "skill-long"; index++)
                Steps(session, 1);
            Assert.That(Snapshot(session, "A").NativeAnimationClip, Is.EqualTo("skill-long"));
            Assert.That(Snapshot(session, "A").Position.X, Is.Zero);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ResurrectionAwayFromHomeQueuesNativeMovementAndReturnsAfterTheDeadCommandReleases(string variant)
        {
            BattleSession session = Session(variant, out _, party: true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Attack, "E")), Is.True);
            Steps(session, 4);
            float away = Snapshot(session, "A").Position.X;
            Assert.That(away, Is.GreaterThan(13));
            object actor = Actor(session, "A");
            Invoke(session, "ChangeHp", actor, -1000, null, 0L, "lethal fixture", true, false, false);
            Steps(session, 20);
            Assert.That(Snapshot(session, "A").ModelAliveFlag, Is.False);
            Assert.That(Invoke(session, "ApplyState", actor, BattleState.Death, true, null, false, null, 0L, true), Is.True);
            Assert.That(session.StageTasks.Count(task => task.Kind == "revival return"), Is.EqualTo(1));
            Assert.That(Invoke(session, "ApplyState", actor, BattleState.Death, true, null, false, null, 0L, true), Is.False);
            Assert.That(session.StageTasks.Count(task => task.Kind == "revival return"), Is.EqualTo(1));
            Invoke(session, "ChangeHp", actor, 100, null, 0L, "revival HP fixture", true, false, false);
            Steps(session, 1);
            Assert.That(Snapshot(session, "A").Position.X, Is.GreaterThan(0).And.LessThan(away));
            Assert.That(Snapshot(session, "A").Movement.Plan.Kind, Is.EqualTo(BattleMovementKind.Run));
            Steps(session, 30);
            Assert.That(Snapshot(session, "A").Position.X, Is.Zero);
            Assert.That(Snapshot(session, "A").FacingDegrees, Is.EqualTo(BattleFacingRules.Initial(BattleSide.Ally)));
            Assert.That(session.StageTasks.Any(task => task.Kind == "revival return"), Is.False);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ResurrectionAtHomeDoesNotCreateAReturnStage(string variant)
        {
            BattleSession session = Session(variant, out _, party: true);
            object actor = Actor(session, "A");
            Invoke(session, "ChangeHp", actor, -1000, null, 0L, "lethal fixture", true, false, false);
            Steps(session, 20);
            Assert.That(Invoke(session, "ApplyState", actor, BattleState.Death, true, null, false, null, 0L, true), Is.True);
            Assert.That(session.StageTasks.Any(task => task.Kind == "revival return"), Is.False);
        }
    }
}
