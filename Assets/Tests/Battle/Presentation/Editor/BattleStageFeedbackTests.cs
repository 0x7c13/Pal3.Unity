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

    public sealed class BattleStageFeedbackTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings, IBattleAbilityEffects
        {
            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 1 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 1, new[] { 0 });

            public BattleAbilityStageProfile AbilityStage(string actor, BattleAbilityDefinition ability) => new(
                ability.IsItem ? BattleAbilityStageKind.Item : ability.IsMagic ? BattleAbilityStageKind.Magic : BattleAbilityStageKind.Skill,
                new BattleAbilityEffectBinding(-1, 0), new BattleAbilityEffectBinding(-1, 0), 0,
                BattleAbilityWorkBoundary.WorkTime, 1, true);

            public IReadOnlyList<BattleMovementPlan> AbilityMovement(BattleActorSnapshot source, BattleActorSnapshot target,
                BattleAbilityDefinition ability, BattleDefinitions definitions, bool returning) => Array.Empty<BattleMovementPlan>();

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "A" ? 0 : actor == "B" ? 1 : 2;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class Rules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) =>
                actor.Side == BattleSide.Ally ? BattleSession.FullGauge : 0;

            public int GaugePerStep(BattleParticipant actor) => 1;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => new(-1, false, false);
        }

        private sealed class StageObserver : IBattleEffectDirector, IBattleStagePresentation
        {
            public BattleSession Session;
            public readonly BattleStageMessageProjection Projection = new();
            public readonly List<string> Frames = new();

            public long Start(BattleEffectRequest request) => throw new InvalidOperationException("This message fixture has no effect groups.");

            public bool IsRunning(long handle) => false;

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void Stop(long handle)
            {
            }

            public void AdvanceStage(double scaledSeconds, double absoluteSeconds)
            {
                Projection.Synchronize(Session.StageMessageReferences, Session.Tick is > 8 and < 30);
                Frames.Add(Session.Tick + ":" + Projection.VisibleReferenceCount(BattleSide.Ally) + ":" +
                    string.Join("|", Projection.Messages.Select(message => message.Text ?? message.LanguageKey)));
            }
        }

        private static BattleSession Session(string variant, BattleEncounter encounter = BattleEncounter.Normal, bool magic = false,
            IBattleEffectDirector effects = null, Action<BattleSetup> capture = null)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "fixture", DataFingerprint = "feedback", Encounter = encounter, ScriptBattle = true };
            foreach (string id in new[] { "A", "B", "E" })
            {
                var actor = new BattleParticipant
                {
                    InstanceId = id,
                    ActorId = id == "A" ? 101 : id == "B" ? 104 : 701,
                    Side = id == "E" ? BattleSide.Enemy : BattleSide.Ally,
                    Slot = id == "B" ? BattleSlot.Fire : BattleSlot.Water,
                    Attributes = new[] { 500, 100, 100, 100, 0, 3, 0, 0, 0, 0, 0, 0 },
                    Vitals = new[] { 500, 100, 100 }
                };
                if (id != "E")
                    actor.Skills.Add(new BattleSkill { Id = 4201, Mastered = true, Level = 4 });
                scenario.Participants.Add(actor);
            }
            scenario.Items.Add(new BattleItem { Id = 1001, Count = 2 });
            BattleAbilityDefinition Ability(bool item) => new(new BattleAbilityData
            {
                Id = item ? 1001 : 4201,
                Name = item ? "物品标题" : "招式标题",
                IsItem = item,
                IsMagic = !item && magic,
                SkillTypeId = magic ? 2 : 0,
                ItemType = item ? 2 : 0,
                ElementMask = magic ? 4 : 0,
                Range = BattleTargetRange.OpponentSingle,
                Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -1) }
            });
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint, new[] { Ability(false) }, new[] { Ability(true) },
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.InstanceId == "A" ? 0 : actor.InstanceId == "B" ? 1 : -1
                })));
            var setup = new BattleSetup(scenario);
            capture?.Invoke(setup);
            var session = new BattleSession(setup, new Catalog(), new Rules(), definitions, effects);
            session.SetAutomatic(BattleSide.Ally, false);
            session.SetAutomatic(BattleSide.Enemy, false);
            return session;
        }

        private static void Until(BattleSession session, Func<bool> condition)
        {
            for (int index = 0; index < 500 && !condition() && session.Status == BattleSessionStatus.Running; index++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running), session.Result?.Reason);
            Assert.That(condition(), Is.True);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void LaterNameEndingKeepsItsTextUntilTheOlderItemReferenceEnds(string variant)
        {
            BattleSession session = Session(variant);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "E", 1001)), Is.True);
            Until(session, () => session.StageMessages.Count > 0);
            for (int index = 0; index < 10; index++)
                session.Step();
            Assert.That(session.Submit(new BattleCommand("B", BattleAction.Skill, "E", 4201)), Is.True);
            Until(session, () => session.StageMessages.Single().ReferenceCount == 2);
            Assert.That(session.StageMessages.Single().Text, Is.EqualTo("招式标题"));
            Until(session, () => session.StageMessages.Single().ReferenceCount == 1);
            Assert.That(session.StageMessages.Single().Text, Is.EqualTo("招式标题"), "UIStaticCB retains the last SetText while another ShowWindow reference survives.");
            Until(session, () => session.StageMessages.Count == 0);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void CancelingOneActionReleasesOnlyItsOwnMessageReference(string variant)
        {
            BattleSession session = Session(variant);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "E", 1001)), Is.True);
            Until(session, () => session.StageMessages.Count > 0);
            Assert.That(session.Submit(new BattleCommand("B", BattleAction.Skill, "E", 4201)), Is.True);
            Until(session, () => session.StageMessages.Single().ReferenceCount == 2);
            Assert.That(session.CancelAction("B"), Is.True);
            Assert.That(session.StageMessages.Single().ReferenceCount, Is.EqualTo(1));
            Assert.That(session.StageMessages.Single().Text, Is.EqualTo("招式标题"));
            Assert.That(session.CancelAction("A"), Is.True);
            Assert.That(session.StageMessages, Is.Empty);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void FailedFleeActivatesTheNativeSharedTitleInsteadOfOnlyItsDebugSnapshot(string variant)
        {
            BattleSession session = Session(variant);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Flee)), Is.True);
            Assert.That(session.Submit(new BattleCommand("B", BattleAction.Defend)), Is.True);
            Until(session, () => session.FleeResult != null);
            Assert.That(session.FleeResult.Success, Is.False);
            Assert.That(session.StageMessages.Single().LanguageKey, Is.EqualTo("FleeFail"));
            float started = session.FleeResult.StartedAtSeconds;
            while (BattleStageScheduler.TimeAt(session.SimulationTick) - started <= 3f)
                session.Step();
            Assert.That(session.StageMessages, Is.Empty);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void TrailDoesNotLetANewSkillOverwriteTheItemOrResurrectAnInterruptedName(string variant)
        {
            BattleSession session = Session(variant);
            var projection = new BattleStageMessageProjection();
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "E", 1001)), Is.True);
            Until(session, () => session.StageMessageReferences.Count > 0);
            projection.Synchronize(session.StageMessageReferences, false);
            Assert.That(projection.Messages.Single().Text, Is.EqualTo("物品标题"));
            projection.Synchronize(session.StageMessageReferences, true);
            Assert.That(session.Submit(new BattleCommand("B", BattleAction.Skill, "E", 4201)), Is.True);
            Until(session, () => session.StageMessageReferences.Count == 2);
            projection.Synchronize(session.StageMessageReferences, true);
            Assert.That(projection.Messages.Single().Text, Is.EqualTo("物品标题"));
            projection.Synchronize(session.StageMessageReferences, false);
            Assert.That(projection.VisibleReferenceCount(BattleSide.Ally), Is.EqualTo(1));
            Assert.That(projection.Messages.Single().Text, Is.EqualTo("物品标题"));
            session.CancelAction("A");
            projection.Synchronize(session.StageMessageReferences, false);
            Assert.That(projection.Messages, Is.Empty, "The skill that began during Trail must not reappear.");

            session = Session(variant);
            projection.Clear();
            session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201));
            Until(session, () => session.StageMessageReferences.Count > 0);
            projection.Synchronize(session.StageMessageReferences, false);
            Assert.That(projection.Messages.Single().Text, Is.EqualTo("招式标题"));
            projection.Synchronize(session.StageMessageReferences, true);
            projection.Synchronize(session.StageMessageReferences, false);
            Assert.That(projection.Messages, Is.Empty, "A previously visible name interrupted by Trail must not reappear.");
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void PerStageConsumptionAndReplayKeepTheSameTitleHistoryAcrossTrailAndPausedInspection(string variant)
        {
            var observer = new StageObserver();
            BattleSetup setup = null;
            BattleSession session = Session(variant, effects: observer, capture: value => setup = value);
            observer.Session = session;
            session.Submit(new BattleCommand("A", BattleAction.Item, "E", 1001));
            for (int index = 0; index < 12; index++)
                session.Step();
            session.Submit(new BattleCommand("B", BattleAction.Skill, "E", 4201));
            for (int index = 0; index < 48; index++)
                session.Step();
            Assert.That(observer.Projection.Messages.Single().Text, Is.EqualTo("物品标题"));
            long tick = session.Tick;
            for (int index = 0; index < 10; index++)
                observer.Projection.Synchronize(session.StageMessageReferences, false);
            Assert.That(session.Tick, Is.EqualTo(tick));
            Assert.That(observer.Projection.Messages.Single().Text, Is.EqualTo("物品标题"));
            var replayObserver = new StageObserver();
            BattleSession replay = BattleReplay.Play(setup, new Catalog(), session.Inputs, session.Tick, new Rules(),
                session.Definitions, replayObserver, created => replayObserver.Session = created);
            Assert.That(replayObserver.Frames, Is.EqualTo(observer.Frames));
            Assert.That(replayObserver.Projection.Messages.Single().Text, Is.EqualTo("物品标题"));
            Assert.That(replay.StageMessageReferences.Select(message => message.ReferenceId),
                Is.EqualTo(session.StageMessageReferences.Select(message => message.ReferenceId)));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void StageQueriesAreImmutableAndExposeParallelMagicLineupAfterTheNameEnds(string variant)
        {
            BattleSession session = Session(variant, magic: true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", 4201)), Is.True);
            Until(session, () => session.AbilityPresentations.Any(value => value.NameVisible && value.LineupVisible));
            BattleAbilityPresentationSnapshot captured = session.AbilityPresentations.Single();
            long tick = session.Tick;
            int events = session.Events.Count, inputs = session.Inputs.Count;
            var random = (BattleRandom)typeof(BattleSession).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            uint randomState = random.State;
            for (int index = 0; index < 10; index++)
            {
                Assert.That(session.StageMessages.Single().Text, Is.EqualTo("招式标题"));
                Assert.That(session.AbilityPresentations.Single().ElementMask, Is.EqualTo(4));
            }
            Assert.That((session.Tick, session.Events.Count, session.Inputs.Count), Is.EqualTo((tick, events, inputs)));
            Assert.That(random.State, Is.EqualTo(randomState));
            Until(session, () => session.AbilityPresentations.Any(value => !value.NameVisible && value.LineupVisible));
            Assert.That(session.StageMessages, Is.Empty);
            Assert.That(captured.NameVisible, Is.True);
            Assert.That(captured.LineupDurationSeconds, Is.EqualTo(2));
            session.Abort();
            Assert.That(session.StageMessages, Is.Empty);
            Assert.That(session.AbilityPresentations, Is.Empty);
        }

        [TestCase("PAL3", BattleEncounter.AllyAmbush, BattleSide.Ally, "WeSneakAttackEnemy")]
        [TestCase("PAL3", BattleEncounter.EnemyAmbush, BattleSide.Enemy, "EnemySneakAttackUs")]
        [TestCase("PAL3A", BattleEncounter.AllyAmbush, BattleSide.Ally, "WeSneakAttackEnemy")]
        [TestCase("PAL3A", BattleEncounter.EnemyAmbush, BattleSide.Enemy, "EnemySneakAttackUs")]
        public void AmbushFeedbackUsesTheThreeSecondSimulationClockWithoutOwningTheStage(string variant, BattleEncounter encounter, BattleSide side, string key)
        {
            BattleSession session = Session(variant, encounter);
            session.Step();
            Assert.That(session.StageMessages.Single().Side, Is.EqualTo(side));
            Assert.That(session.StageMessages.Single().LanguageKey, Is.EqualTo(key));
            Assert.That(session.StageTasks, Is.Empty);
            Assert.That(session.WaitingActor, Is.EqualTo("A"));
            for (int index = 1; index < 150; index++)
                session.Step();
            Assert.That(session.StageMessages.Count, Is.EqualTo(1), "The original end boundary is strict > 3 seconds.");
            session.Step();
            Assert.That(session.StageMessages, Is.Empty);
            Assert.That(session.WaitingActor, Is.EqualTo("A"));
        }
    }
}
