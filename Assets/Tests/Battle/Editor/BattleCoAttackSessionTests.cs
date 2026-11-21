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

    public sealed class BattleCoAttackSessionTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings, IBattleCoAttackTimings, IBattleStageGeometry
        {
            private readonly Dictionary<string, BattleParticipant> _actors;
            internal BattleComboStageProfile Profile = new(439, 440, .05f, .2f, .1f, enemyCenter: new BattleMovementPoint(1500, 0, 0));

            internal Catalog(BattleScenario scenario) => _actors = scenario.Participants.ToDictionary(actor => actor.InstanceId);

            public AttackTiming Attack(string source, string target) => new(0, 1000, new[] { 500 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 20, new[] { 5 });

            public BattleComboStageProfile CoAttack(string actor, BattleComboDefinition combo) => Profile;

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 5;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor switch { "A" => 0, "B" => 1, "C" => 2, "D" => 3, "Z" => 10, _ => 5 };

            public int DamageDivisor(string actor) => 1;

            public BattleMovementPoint CoAttackHome(string actor, BattleSlot slot) => FormationHome(actor, slot);

            public BattleMovementPoint InitialHome(string actor) => FormationHome(actor, _actors[actor].Slot);

            public BattleMovementPoint FormationHome(string actor, BattleSlot slot) =>
                new((_actors[actor].Side == BattleSide.Enemy ? 1000 : 0) + 100 * (int)slot, 0, 0);

            public BattleAttackPlan PlanAttack(string source, string target, BattleMovementPoint sourceHome, BattleMovementPoint targetHome) =>
                new(Attack(source, target), sourceHome, sourceHome, targetHome);

            public IReadOnlyList<BattleMovementPlan> PlanFormationMove(string actor, BattleMovementPoint from, BattleMovementPoint to, bool midpoint) =>
                new[] { new BattleMovementPlan(BattleMovementKind.Run, from, to) };
        }

        private sealed class Rules : IBattleRules
        {
            private readonly HashSet<string> _ready;

            internal Rules(params string[] ready) => _ready = new HashSet<string>(ready.Length == 0 ? new[] { "A" } : ready);

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) =>
                _ready.Contains(actor.InstanceId) ? BattleSession.FullGauge : 600;

            public int GaugePerStep(BattleParticipant actor) => 1;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => new(-10, false, false);
        }

        private sealed class Director : IBattleEffectDirector, IBattleEffectPreloader
        {
            private readonly Dictionary<long, float> _running = new();
            private long _next;
            internal BattleSession Session;
            internal float Duration = .4f;
            internal readonly List<BattleEffectRequest> Started = new();
            internal readonly List<long> Stopped = new();
            internal readonly List<int> Preloaded = new();

            public void Preload(int groupId) => Preloaded.Add(groupId);

            public long Start(BattleEffectRequest request)
            {
                Started.Add(request);
                _running.Add(++_next, Session.CoAttacks.Single().Steps[(int)BattleCoAttackStep.Act].StartTime);
                return _next;
            }

            public bool IsRunning(long handle)
            {
                if (!_running.TryGetValue(handle, out float actStarted))
                    return false;
                return (double)BattleStageScheduler.TimeAt(Session.SimulationTick) - actStarted < Duration;
            }

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void Stop(long handle)
            {
                _running.Remove(handle);
                Stopped.Add(handle);
            }
        }

        private sealed class Occupied : IBattleStageJob
        {
            public bool Advance(BattleStageContext context) => false;
        }

        private sealed class CompletedEffects : IBattleEffectDirector
        {
            private long _next;

            public long Start(BattleEffectRequest request) => ++_next;

            public bool IsRunning(long handle) => false;

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void Stop(long handle)
            {
            }
        }

        private static BattleParticipant Participant(string id, int resource, BattleSide side, BattleSlot slot) => new()
        {
            InstanceId = id,
            ActorId = resource,
            Side = side,
            Slot = slot,
            Level = 1,
            Attributes = new[] { 5000, 500, 500, 100, 0, 3, 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { 5000, 500, 500 }
        };

        private static BattleScenario Scenario(string variant, bool party = false, bool centerEnemy = false)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "fixture", DataFingerprint = "coattack", Seed = 1 };
            scenario.Participants.Add(Participant("A", 101, BattleSide.Ally, BattleSlot.Water));
            if (party)
            {
                scenario.Participants.Add(Participant("B", variant == "PAL3" ? 104 : 102, BattleSide.Ally, BattleSlot.Fire));
                scenario.Participants.Add(Participant("C", variant == "PAL3" ? 105 : 103, BattleSide.Ally, BattleSlot.Wind));
                scenario.Participants.Add(Participant("D", variant == "PAL3" ? 107 : 104, BattleSide.Ally, BattleSlot.Center));
            }
            scenario.Participants.Add(Participant("E", 701, BattleSide.Enemy, BattleSlot.Water));
            if (centerEnemy)
                scenario.Participants.Add(Participant("Z", 702, BattleSide.Enemy, BattleSlot.Center));
            return scenario;
        }

        private static BattleComboDefinition Combo(int id = 501, int trigger = 0, int range = 2,
            int second = 0, int[] values = null, int first = 101) => new("native combo " + id,
            new BattleComboRequirement(id, trigger, new[] { first, second, 0, 0 }, new int[4], new int[4], new[] { -1, -1, -1 }),
            range, values ?? new int[12], 100);

        private static BattleSession Session(BattleScenario scenario, out Catalog catalog, out Director director,
            IEnumerable<BattleComboDefinition> combos = null, BattleAbilityDefinition ability = null, params string[] ready)
        {
            if (ability != null)
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = ability.Id, Level = 1, Mastered = true });
            catalog = new Catalog(scenario);
            director = new Director();
            var definitions = new BattleDefinitions(scenario.Variant, scenario.DataFingerprint,
                ability == null ? Array.Empty<BattleAbilityDefinition>() : new[] { ability }, Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.InstanceId switch { "A" => 0, "B" => 1, "C" => 2, "D" => 3, _ => -1 },
                    NormalAttackMode = 924,
                    Boss = actor.InstanceId == "Z"
                })), combos ?? new[] { Combo() });
            var session = new BattleSession(new BattleSetup(scenario), catalog, new Rules(ready), definitions, director);
            director.Session = session;
            return session;
        }

        private static object NativeActor(BattleSession session, string id) =>
            ((IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session))[id];

        private static void RequestEnd(BattleSession session) => typeof(BattleSession).GetMethod("RequestRoleEnd", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(session, new[] { NativeActor(session, "A") });

        private static BattleActorSnapshot Actor(BattleSession session, string id) => session.Actors.Single(actor => actor.Id == id);

        private static void Step(BattleSession session)
        {
            session.Step();
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
        }

        private static void Until(BattleSession session, Func<bool> done, int maximum = 700)
        {
            for (int i = 0; i < maximum && !done(); i++)
                Step(session);
            Assert.That(done(), Is.True, "The original combo boundary was not reached.");
        }

        [Test]
        public void ACompanionWithZeroInitialHpQualifiesForADeathConditionOnTheFirstCommand()
        {
            BattleScenario scenario = Scenario("PAL3", party: true);
            scenario.Participants.Single(actor => actor.InstanceId == "B").Vitals[0] = 0;
            var combo = new BattleComboDefinition("Death condition",
                new BattleComboRequirement(501, 0, new[] { 101, 104, 0, 0 }, new int[4], new int[4],
                    new[] { (int)BattleState.Death, -1, -1 }), 2, new int[12], 100);
            BattleSession session = Session(scenario, out _, out _, new[] { combo });

            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Step(session);

            Assert.That(session.CoAttacks.Single().ComboId, Is.EqualTo(combo.Id));
        }

        public static IEnumerable<TestCaseData> NativeWriterCases()
        {
            yield return new TestCaseData("PAL3", -1, 0, false, 4812, 500, 0, 3403800452u);
            yield return new TestCaseData("PAL3", -1, 0, true, 4812, 500, 0, 3884216597u);
            yield return new TestCaseData("PAL3", -1, 5, false, 4812, 500, 5, 3403800452u);
            yield return new TestCaseData("PAL3", -1, 5, true, 4812, 500, 5, 3884216597u);
            yield return new TestCaseData("PAL3", -1, 500, false, 4812, 500, 500, 3403800452u);
            yield return new TestCaseData("PAL3", -1, 500, true, 4812, 500, 500, 3884216597u);
            yield return new TestCaseData("PAL3", 0, 0, false, 5000, 500, 0, 1u);
            yield return new TestCaseData("PAL3", 0, 0, true, 5000, 500, 0, 1u);
            yield return new TestCaseData("PAL3", 0, 5, false, 5000, 500, 5, 1u);
            yield return new TestCaseData("PAL3", 0, 5, true, 5000, 500, 5, 1u);
            yield return new TestCaseData("PAL3", 0, 500, false, 5000, 500, 500, 1u);
            yield return new TestCaseData("PAL3", 0, 500, true, 5000, 500, 500, 1u);
            yield return new TestCaseData("PAL3", 1, 0, false, 5188, 500, 0, 3403800452u);
            yield return new TestCaseData("PAL3", 1, 0, true, 5188, 500, 0, 3884216597u);
            yield return new TestCaseData("PAL3", 1, 5, false, 5188, 500, 5, 3403800452u);
            yield return new TestCaseData("PAL3", 1, 5, true, 5188, 500, 5, 3884216597u);
            yield return new TestCaseData("PAL3", 1, 500, false, 5188, 500, 500, 3403800452u);
            yield return new TestCaseData("PAL3", 1, 500, true, 5188, 500, 500, 3884216597u);
            yield return new TestCaseData("PAL3A", -1, 0, false, 4903, 396, 0, 2597632883u);
            yield return new TestCaseData("PAL3A", -1, 0, true, 4903, 396, 0, 2652862833u);
            yield return new TestCaseData("PAL3A", -1, 5, false, 4903, 396, 0, 2597632883u);
            yield return new TestCaseData("PAL3A", -1, 5, true, 4903, 396, 0, 2652862833u);
            yield return new TestCaseData("PAL3A", -1, 500, false, 4903, 396, 397, 2597632883u);
            yield return new TestCaseData("PAL3A", -1, 500, true, 4903, 396, 397, 2652862833u);
            yield return new TestCaseData("PAL3A", 0, 0, false, 4903, 396, 0, 2597632883u);
            yield return new TestCaseData("PAL3A", 0, 0, true, 4903, 396, 0, 2652862833u);
            yield return new TestCaseData("PAL3A", 0, 5, false, 4903, 396, 0, 2597632883u);
            yield return new TestCaseData("PAL3A", 0, 5, true, 4903, 396, 0, 2652862833u);
            yield return new TestCaseData("PAL3A", 0, 500, false, 4903, 396, 397, 2597632883u);
            yield return new TestCaseData("PAL3A", 0, 500, true, 4903, 396, 397, 2652862833u);
            yield return new TestCaseData("PAL3A", 1, 0, false, 5097, 396, 0, 2597632883u);
            yield return new TestCaseData("PAL3A", 1, 0, true, 5097, 396, 0, 2652862833u);
            yield return new TestCaseData("PAL3A", 1, 5, false, 5097, 396, 0, 2597632883u);
            yield return new TestCaseData("PAL3A", 1, 5, true, 5097, 396, 0, 2652862833u);
            yield return new TestCaseData("PAL3A", 1, 500, false, 5097, 396, 397, 2597632883u);
            yield return new TestCaseData("PAL3A", 1, 500, true, 5097, 396, 397, 2652862833u);
        }

        [TestCaseSource(nameof(NativeWriterCases))]
        public void OriginalNormalDamagePowerAndWritersKeepNativeValuesAndRng(string variant, int rawHp, int initialMp,
            bool poolFull, int hp, int sp, int mp, uint expectedSeed)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[1].Attributes[0] = 6000;
            scenario.Participants[1].Vitals[2] = initialMp;
            BattleSession session = Session(scenario, out _, out _, new[] { Combo(values: new[] { rawHp, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }) });
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Until(session, () => session.CoAttacks.SingleOrDefault()?.Steps[(int)BattleCoAttackStep.Act].Status == BattleCoAttackStepStatus.Running);
            if (poolFull)
            {
                var stage = (BattleStageScheduler)typeof(BattleSession).GetField("_stage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
                while (stage.Count < BattleStageScheduler.DefaultCapacity)
                    stage.Add("occupied slot", "fixture", new Occupied(), BattleStageScheduler.TimeAt(session.SimulationTick));
            }
            Until(session, () => session.CoAttacks.Single().Worked);
            Assert.That((Actor(session, "E").Hp, Actor(session, "E").Sp, Actor(session, "E").Mp), Is.EqualTo((hp, sp, mp)));
            var random = (BattleRandom)typeof(BattleSession).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            Assert.That(random.State, Is.EqualTo(expectedSeed));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void PreloadRunsBehindTheFirstFadeBeforeAnyEffectStarts(string variant)
        {
            BattleSession session = Session(Scenario(variant), out _, out Director director);
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Until(session, () => director.Preloaded.Count > 0);
            Assert.That(director.Preloaded, Is.EqualTo(new[] { 439, 440 }));
            Assert.That(director.Started, Is.Empty);
            Assert.That(session.CoAttackScreenOpacity, Is.EqualTo(1f));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AnyKeyOnlySkipsPresentationAndCannotFastForwardNativeWork(string variant)
        {
            BattleSession session = Session(Scenario(variant), out _, out _);
            Assert.That(session.SkipCoAttackPresentation(), Is.False);
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            for (int tick = 0; tick < 20 && session.CoAttacks
                .FirstOrDefault()?.Steps[(int)BattleCoAttackStep.Act].Status != BattleCoAttackStepStatus.Running; tick++)
            {
                if (session.IsCoAttackPresentationBlockingInput)
                {
                    Assert.That(session.SkipCoAttackPresentation(), Is.True);
                    Assert.That(session.SkipCoAttackPresentation(), Is.False, "One skip applies to one presentation subtask.");
                }
                Step(session);
            }

            BattleCoAttackSnapshot stage = session.CoAttacks.Single();
            Assert.That(stage.Steps[(int)BattleCoAttackStep.Act].Status, Is.EqualTo(BattleCoAttackStepStatus.Running));
            Assert.That(session.IsCoAttackPresentationBlockingInput, Is.False);
            Assert.That(stage.Worked, Is.False);
            Assert.That(stage.NameVisible, Is.True);
            int inputs = session.Inputs.Count;
            Assert.That(session.SkipCoAttackPresentation(), Is.False);
            Step(session);
            Assert.That(session.CoAttacks.Single().NameVisible, Is.True, "The title runs its own native timer.");
            Assert.That(session.Inputs.Count, Is.EqualTo(inputs));
            Until(session, () => session.CoAttacks.Single().Worked);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void PresentationSkipInputsReplayAtTheSameWallBoundaries(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            BattleSession session = Session(scenario, out Catalog catalog, out Director director);
            director.Duration = 0;
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            for (int tick = 0; tick < 30; tick++)
            {
                session.SkipCoAttackPresentation();
                Step(session);
            }
            Assert.That(session.Inputs.Any(input => input.Kind == BattleInputKind.CoAttackPresentationSkip), Is.True);
            BattleSession replay = BattleReplay.Play(new BattleSetup(scenario), catalog, session.Inputs,
                session.Tick, new Rules(), session.Definitions, new CompletedEffects());
            Assert.That(replay.Actors.Select(actor => (actor.Id, actor.Hp, actor.Sp, actor.Mp, actor.Gauge, actor.Position)),
                Is.EqualTo(session.Actors.Select(actor => (actor.Id, actor.Hp, actor.Sp, actor.Mp, actor.Gauge, actor.Position))));
            Assert.That(replay.Events.Select(value => (value.Tick, value.Kind, value.RandomState)),
                Is.EqualTo(session.Events.Select(value => (value.Tick, value.Kind, value.RandomState))));
        }

        public static IEnumerable<TestCaseData> NativeStageCases()
        {
            yield return new TestCaseData("PAL3", 0.4f, 0, new[] { 1, 1, 52, 52, 52, 103, 104, 104, 124, 124, 175, 175, 226 }, 109, 227);
            yield return new TestCaseData("PAL3", 0.4f, 1, new[] { 1, 1, 2, 2, 2, 3, 4, 4, 25, 25, 26, 26, 27 }, 9, 28);
            yield return new TestCaseData("PAL3", 0.4f, 2, new[] { 1, 1, 52, 52, 52, 103, 104, 104, 124, 124, 125, 125, 126 }, 109, 127);
            yield return new TestCaseData("PAL3", 5.0f, 0, new[] { 1, 1, 52, 52, 52, 103, 104, 104, 354, 354, 405, 405, 456 }, 109, 457);
            yield return new TestCaseData("PAL3", 5.0f, 1, new[] { 1, 1, 2, 2, 2, 3, 4, 4, 255, 255, 256, 256, 257 }, 9, 258);
            yield return new TestCaseData("PAL3", 5.0f, 2, new[] { 1, 1, 52, 52, 52, 103, 104, 104, 354, 354, 355, 355, 356 }, 109, 357);
            yield return new TestCaseData("PAL3A", 0.4f, 0, new[] { 1, 1, 52, 52, 52, 103, 104, 104, 109, 109, 160, 160, 211 }, 109, 212);
            yield return new TestCaseData("PAL3A", 0.4f, 1, new[] { 1, 1, 2, 2, 2, 3, 4, 4, 9, 9, 10, 10, 11 }, 9, 12);
            yield return new TestCaseData("PAL3A", 0.4f, 2, new[] { 1, 1, 52, 52, 52, 103, 104, 104, 109, 109, 110, 110, 111 }, 109, 112);
            yield return new TestCaseData("PAL3A", 5.0f, 0, new[] { 1, 1, 52, 52, 52, 103, 104, 104, 109, 109, 160, 160, 211 }, 109, 212);
            yield return new TestCaseData("PAL3A", 5.0f, 1, new[] { 1, 1, 2, 2, 2, 3, 4, 4, 9, 9, 10, 10, 11 }, 9, 12);
            yield return new TestCaseData("PAL3A", 5.0f, 2, new[] { 1, 1, 52, 52, 52, 103, 104, 104, 109, 109, 110, 110, 111 }, 109, 112);
        }

        [TestCaseSource(nameof(NativeStageCases))]
        public void ThirteenStagesMatchPublishedDoTaskFadeAndActVectors(string variant, float effectDuration, int requireMode,
            int[] starts, int workTick, int finishTick)
        {
            BattleSession session = Session(Scenario(variant), out _, out Director director);
            director.Duration = effectDuration;
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            if (requireMode == 1)
                RequestEnd(session);
            int? observedWork = null;
            BattleCoAttackSnapshot last = null;
            for (int tick = 1; tick <= finishTick; tick++)
            {
                Step(session);
                last = session.CoAttacks.Single();
                if (last.Worked && !observedWork.HasValue)
                    observedWork = tick;
                for (int index = 0; index < starts.Length; index++)
                {
                    if (last.Steps[index].Status != BattleCoAttackStepStatus.Ready)
                        Assert.That(last.Steps[index].StartTime, Is.EqualTo(BattleStageScheduler.TimeAt(starts[index])), variant + " sub " + index);
                }
                if (requireMode == 2 && tick == starts[(int)BattleCoAttackStep.Act])
                    RequestEnd(session);
                Assert.That(last.Finished, Is.EqualTo(tick == finishTick), variant + " tick " + tick);
            }
            Assert.That(observedWork, Is.EqualTo(workTick));
            Assert.That(last.Steps.All(sub => sub.Status == BattleCoAttackStepStatus.Finished), Is.True);
            Step(session);
            Assert.That(session.CoAttacks, Is.Empty);
            Assert.That(session.IsCoAttackAtbPaused, Is.False);
            Assert.That(Actor(session, "A").CurrentAction, Is.Null);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AutomaticReplacementKeepsTheFirstMatchingRecord(string variant)
        {
            BattleSession session = Session(Scenario(variant), out _, out _, new[] { Combo(502), Combo(501) });
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Step(session);
            Assert.That(session.CoAttacks.Single().ComboId, Is.EqualTo(502));
            Assert.That(Actor(session, "A").AbilityId, Is.EqualTo(502));
            Assert.That(Actor(session, "A").Pose, Is.EqualTo(BattlePose.Idle));
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3A", true)]
        public void TriggerSkillDoesNotStartCastingOrPayItsOrdinaryCost(string variant, bool magic)
        {
            var ability = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 2001,
                SkillTypeId = magic ? 1 : 0,
                IsMagic = magic,
                Range = BattleTargetRange.OpponentSingle,
                Costs = new[] { new BattleCost(magic ? 2 : 1, BattleValueMode.Absolute, 50) },
                Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -20) }
            });
            BattleSession session = Session(Scenario(variant), out _, out _, new[] { Combo(trigger: ability.Id) }, ability);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", ability.Id)), Is.True);
            Step(session);
            Assert.That(session.CoAttacks.Count, Is.EqualTo(1));
            Assert.That((Actor(session, "A").Gauge, Actor(session, "A").CastGauge), Is.EqualTo((0, 0)));
            Until(session, () => session.CoAttacks.Count == 0);
            Assert.That((Actor(session, "A").Sp, Actor(session, "A").Mp, Actor(session, "A").Skills.Single().Uses), Is.EqualTo((500, 500, 0)));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void HelpersRetainTheirExistingActionsAndUnspentGauges(string variant)
        {
            BattleScenario scenario = Scenario(variant, true);
            BattleSession session = Session(scenario, out _, out _, new[] { Combo(second: scenario.Participants[1].ActorId) }, ready: new[] { "A", "B" });
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Assert.That(session.Submit("B", BattleAction.Attack, "E"), Is.True);
            long helperAction = Actor(session, "B").ActionId;
            for (int i = 0; i < 10; i++)
                Step(session);
            Assert.That(Actor(session, "B").CurrentAction, Is.EqualTo(BattleAction.Attack));
            Assert.That((Actor(session, "B").ActionId, Actor(session, "B").ActionTick), Is.EqualTo((helperAction, 10)));
            Assert.That((Actor(session, "C").Gauge, Actor(session, "D").Gauge), Is.EqualTo((600, 600)));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void DeadAuxiliariesRemainEligibleAndTheirOriginalBodiesAreNotCleared(string variant)
        {
            BattleScenario scenario = Scenario(variant, true);
            scenario.Participants[1].Vitals[0] = 0;
            BattleSession session = Session(scenario, out _, out _, new[] { Combo(second: scenario.Participants[1].ActorId, values: new[] { -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }) });
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Step(session);
            Assert.That(session.CoAttacks.Count, Is.EqualTo(1));
            Until(session, () => session.CoAttacks.Single().Worked);
            Assert.That(Actor(session, "E").Hp, Is.LessThan(5000));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void SourceDeathDuringActDoesNotPreventNativeWork(string variant)
        {
            BattleSession session = Session(Scenario(variant, true), out _, out _, new[] { Combo(values: new[] { -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }) });
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Until(session, () => session.CoAttacks.SingleOrDefault()?.Steps[(int)BattleCoAttackStep.Act].Status == BattleCoAttackStepStatus.Running);
            typeof(BattleSession).GetMethod("ChangeHp", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session,
                new[] { NativeActor(session, "A"), (object)-100000, null, 0L, "native death port", false, false, false });
            Until(session, () => session.CoAttacks.Single().Worked);
            Assert.That(Actor(session, "A").AliveFlag, Is.False);
            Assert.That(Actor(session, "E").Hp, Is.LessThan(5000));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RangeTwoCentersTheTargetAndRestoresBothTargetAndCenterVisibility(string variant)
        {
            BattleSession session = Session(Scenario(variant, true, true), out _, out _, new[] { Combo() });
            // The old enemy target is kept even when another target is a boss.
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Until(session, () => session.CoAttacks.SingleOrDefault()?.Steps[(int)BattleCoAttackStep.SetComboPositions].Status == BattleCoAttackStepStatus.Finished);
            Assert.That(Actor(session, "E").Position.X, Is.EqualTo(1500));
            Assert.That(session.CoAttackActorVisible("Z"), Is.False);
            Assert.That(session.CoAttackActorVisible("D"), Is.EqualTo(variant != "PAL3A"));
            Until(session, () => session.CoAttacks.Count == 0);
            Assert.That(Actor(session, "E").Position.X, Is.EqualTo(1000));
            Assert.That(session.CoAttackActorVisible("Z"), Is.True);
            Assert.That(session.CoAttackActorVisible("D"), Is.True);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void CenterEnemyDeathDuringComboDoesNotLeaveAStaleVisibilityMask(string variant)
        {
            BattleSession session = Session(Scenario(variant, true, true), out _, out _);
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Until(session, () => !session.CoAttackActorVisible("Z"));
            typeof(BattleSession).GetMethod("ChangeHp", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session,
                new[] { NativeActor(session, "Z"), (object)-5000, null, 0L, "lethal during combo", false, false, false });
            Until(session, () => !Actor(session, "Z").ModelAliveFlag);
            Assert.That(session.CoAttackActorVisible("Z"), Is.False);
            Until(session, () => session.CoAttacks.Count == 0);
            Assert.That(Actor(session, "Z").ModelAliveFlag, Is.False);
            Assert.That(session.CoAttackActorVisible("Z"), Is.True);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void CancellingOneOverlappingComboKeepsTheOtherComboPresentation(string variant)
        {
            BattleScenario scenario = Scenario(variant, true, true);
            BattleComboDefinition[] combos = { Combo(), Combo(id: 502, first: scenario.Participants[1].ActorId) };
            BattleSession session = Session(scenario, out _, out _, combos, ready: new[] { "A", "B" });
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Step(session);
            Assert.That(session.Submit("B", BattleAction.Attack, "E"), Is.True);
            Until(session, () => session.CoAttacks.Count == 2 && session.CoAttacks.All(combo =>
                combo.Steps[(int)BattleCoAttackStep.SetComboPositions].Status == BattleCoAttackStepStatus.Finished));
            float opacity = session.CoAttackScreenOpacity;
            int enemyGauge = Actor(session, "E").Gauge;

            Assert.That(session.CancelAction("A"), Is.True);
            Assert.That(session.CoAttacks.Single().Source, Is.EqualTo("B"));
            Assert.That(session.IsCoAttackAtbPaused, Is.True);
            Assert.That(session.CoAttackActorVisible("Z"), Is.False);
            Assert.That(Actor(session, "E").Position.X, Is.EqualTo(1500));
            Assert.That(session.CoAttackScreenOpacity, Is.EqualTo(opacity));
            Step(session);
            Assert.That(Actor(session, "E").Gauge, Is.EqualTo(enemyGauge));

            Assert.That(session.CancelAction("B"), Is.True);
            Assert.That(session.IsCoAttackAtbPaused, Is.False);
            Assert.That(session.CoAttackActorVisible("Z"), Is.True);
            Assert.That(Actor(session, "E").Position.X, Is.EqualTo(1000));
            Assert.That(session.CoAttackScreenOpacity, Is.Zero);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AlreadyReadyHelpersCanStillChooseCommandsWhileTheGaugeIsPaused(string variant)
        {
            BattleSession session = Session(Scenario(variant, true), out _, out _, ready: new[] { "A", "B" });
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Step(session);
            Assert.That(session.IsCoAttackAtbPaused, Is.True);
            Assert.That(session.WaitingActor, Is.EqualTo("B"));
            Assert.That(session.Submit("B", BattleAction.Attack, "E"), Is.True);
        }

        [Test]
        public void Pal3RedLongkuiMemberUsesTheSameOriginalBodyAsBlueLongkui()
        {
            BattleSession session = Session(Scenario("PAL3", true), out _, out _, new[] { Combo(second: 106, values: new[] { -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }) });
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Until(session, () => session.CoAttacks.SingleOrDefault()?.Worked == true);
            Assert.That(Actor(session, "E").Hp, Is.LessThan(4700));
        }

        [Test]
        public void Pal3AEarlyCompletionLetsEffectsRetireAfterTheDirectorThread()
        {
            BattleSession session = Session(Scenario("PAL3A"), out _, out Director director);
            director.Duration = 5f;
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Step(session);
            Until(session, () => session.CoAttacks.Count == 0);
            Assert.That(director.Started.Count, Is.EqualTo(1));
            Assert.That(director.Stopped, Is.Empty);
            session.Abort();
            Assert.That(director.Stopped.Count, Is.EqualTo(1));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void StageAllocationFailureClearsOnlySourceAndLeavesTheBattleRunning(string variant)
        {
            BattleSession session = Session(Scenario(variant, true), out _, out _);
            var stage = (BattleStageScheduler)typeof(BattleSession).GetField("_stage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            for (int i = 0; i < BattleStageScheduler.DefaultCapacity; i++)
                stage.Add("occupied slot", "fixture", new Occupied(), 0);
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Step(session);
            Assert.That(session.CoAttacks, Is.Empty);
            Assert.That(session.IsCoAttackAtbPaused, Is.False);
            Assert.That(Actor(session, "A").CurrentAction, Is.Null);
            Assert.That(Actor(session, "B").Gauge, Is.EqualTo(601));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AbortStopsOwnedEffectsAndReleasesThePresentation(string variant)
        {
            BattleSession session = Session(Scenario(variant), out _, out Director director);
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            Until(session, () => director.Started.Count > 0);
            session.Abort();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Aborted));
            Assert.That(session.CoAttacks, Is.Empty);
            Assert.That(session.IsCoAttackAtbPaused, Is.False);
            Assert.That(session.CoAttackScreenOpacity, Is.Zero);
            Assert.That(director.Stopped.Count, Is.GreaterThan(0));
        }
    }
}
