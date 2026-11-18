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
    using Battle;
    using NUnit.Framework;

    public sealed class BattleWeaponBodyIntegrationTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings, IBattleAbilityEffects,
            IBattleAbilityBodyAnimations, IBattlePhysicalTimings, IBattleEffectActorAnimations
        {
            public readonly BattlePhysicalAnimation Idle = new("z10", 500, 10);
            public readonly BattlePhysicalAnimation External = new("z33", 20, 10);
            public int NativeAction = 38;
            public bool NpcItemIdle;

            public AttackTiming Attack(string source, string target) => new(0, 1, new[] { 0 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 1, new[] { 0 });

            public BattleAbilityStageProfile AbilityStage(string actor, BattleAbilityDefinition ability) =>
                new(BattleAbilityStageKind.Item, new BattleAbilityEffectBinding(-1, 0, 0), new BattleAbilityEffectBinding(-1, 0, 0),
                    10, BattleAbilityWorkBoundary.WorkTime, 0, true, retainsBody: !NpcItemIdle, npcItemIdle: NpcItemIdle);

            public IReadOnlyList<BattleMovementPlan> AbilityMovement(BattleActorSnapshot source, BattleActorSnapshot target,
                BattleAbilityDefinition ability, BattleDefinitions definitions, bool returning) => Array.Empty<BattleMovementPlan>();

            public BattlePhysicalAnimation AbilityBody(string actor, BattleAbilityDefinition ability) => NpcItemIdle ? External : null;

            public int? AbilityNativeAction(string actor, BattleAbilityDefinition ability) => NativeAction;

            public BattlePhysicalAnimation AbilityMovementBody(string actor, BattleAbilityDefinition ability, BattleMovementKind kind, bool returning) => null;

            public BattlePhysicalStageProfile Physical(string actor) => new("PAL3A", actor == "A" ? 101 : 701,
                actor == "A" ? 0 : -1, 1, 1, new Dictionary<BattlePose, BattlePhysicalAnimation> { [BattlePose.Idle] = Idle });

            public BattlePhysicalAnimation ExternalBody(BattleActorSnapshot actor, string requested, BattleDefinitions definitions) => requested == "z33" ? External : null;

            public string EffectActor(uint modelCrc) => null;

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "A" ? 0 : 5;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class Rules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => actor.Side == BattleSide.Ally ? BattleSession.FullGauge : 0;

            public int GaugePerStep(BattleParticipant actor) => 0;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1) => new(0, false, false);
        }

        private sealed class Director : IBattleEffectDirector, IBattleAudio
        {
            public readonly List<string> Sounds = new();

            public void PlaySound(string sound) => Sounds.Add(sound);

            public long Start(BattleEffectRequest request) => throw new InvalidOperationException("This fixture has no effect groups.");

            public bool IsRunning(long handle) => false;

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void Stop(long handle)
            {
            }
        }

        private static BattleSession Session(Catalog catalog, Director director = null)
        {
            var scenario = new BattleScenario { Variant = "PAL3A", Scene = "fixture", DataFingerprint = "native-action-binding" };
            foreach (string id in new[] { "A", "E" })
                scenario.Participants.Add(new BattleParticipant
                {
                    InstanceId = id,
                    ActorId = id == "A" ? 101 : 701,
                    Side = id == "A" ? BattleSide.Ally : BattleSide.Enemy,
                    Attributes = new[] { 500, 100, 100, 100, 0, 3, 0, 0, 0, 0, 0, 0 },
                    Vitals = new[] { 500, 100, 100 }
                });
            scenario.Participants[0].Equipment[0] = 80001;
            scenario.Items.Add(new BattleItem { Id = 6201, Count = 2 });
            var item = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 6201,
                IsItem = true,
                ItemType = 2,
                Range = BattleTargetRange.OwnSingle,
                Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, 1) }
            });
            var definitions = new BattleDefinitions(scenario.Variant, scenario.DataFingerprint, Array.Empty<BattleAbilityDefinition>(), new[] { item },
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.InstanceId == "A" ? 0 : -1,
                    Equipment = actor.InstanceId == "A" ? new[] { new BattleEquipmentDefinition(new BattleEquipmentData { Id = 80001, Slot = 0, WeaponType = 1 }) } : Array.Empty<BattleEquipmentDefinition>()
                })));
            var session = new BattleSession(new BattleSetup(scenario), catalog, new Rules(), definitions, director);
            session.SetAutomatic(BattleSide.Enemy, false);
            return session;
        }

        private static BattleBodyAnimationBinding Binding(BattleSession session)
        {
            var actors = (IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            return (BattleBodyAnimationBinding)typeof(BattleSession).GetMethod("GetBodyBinding", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, new[] { actors["A"] });
        }

        private static void StartItem(BattleSession session, int nativeAction)
        {
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "A", 6201)), Is.True);
            for (int step = 0; step < 20 && Binding(session).RequestedNativeActionIndex != nativeAction; step++)
                session.Step();
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
            Assert.That(Binding(session).RequestedNativeActionIndex, Is.EqualTo(nativeAction));
        }

        [TestCase(38, false)]
        [TestCase(39, true)]
        public void FailedItemGotoKeepsTheActualClipButChangesNativeWeaponAndClockRules(int nativeAction, bool right)
        {
            var catalog = new Catalog { NativeAction = nativeAction };
            BattleSession session = Session(catalog);
            Assert.That(session.WeaponVisibility("A").Right, Is.True);
            StartItem(session, nativeAction);
            BattleBodyAnimationBinding body = Binding(session);
            Assert.That(body.CurrentAnimation, Is.SameAs(catalog.Idle));
            Assert.That(session.WeaponVisibility("A").Right, Is.EqualTo(right));
            uint before = body.GameBoxTime;
            session.Step();
            Assert.That(body.GameBoxTime - before, Is.EqualTo(96), "PAL3A active action uses 1.0 even while the actual idle clip is retained.");
        }

        [Test]
        public void ExternalGotoAndFailedResetRetainTheUnderlyingNativeAction()
        {
            var catalog = new Catalog();
            BattleSession session = Session(catalog);
            StartItem(session, 38);
            session.SetEffectActorControlled("A", true);
            session.SetEffectActorAction("A", "z33");
            Assert.That(Binding(session).CurrentAnimation, Is.SameAs(catalog.External));
            Assert.That(Binding(session).RequestedNativeActionIndex, Is.EqualTo(38));
            session.Step();
            session.SetEffectActorAction("A", "missing");
            Assert.That(Binding(session).CurrentAnimation, Is.SameAs(catalog.External));
            Assert.That(Binding(session).GameBoxTime, Is.Zero);
            Assert.That(Binding(session).RequestedNativeActionIndex, Is.EqualTo(38));
            session.SetEffectActorControlled("A", false);
            Assert.That(session.WeaponVisibility("A").Right, Is.False);
        }

        [TestCase(38, 0)]
        [TestCase(39, 1)]
        public void AttackItemBodyPlaysThePublishedHeavyThrowSoundOnceOnItsFirstRolePass(int nativeAction, int sounds)
        {
            var catalog = new Catalog { NativeAction = nativeAction };
            var director = new Director();
            BattleSession session = Session(catalog, director);
            StartItem(session, nativeAction);
            Assert.That(director.Sounds.Count, Is.EqualTo(sounds));
            Assert.That(director.Sounds.All(sound => sound == "we033"), Is.True);
            session.Step();
            Assert.That(director.Sounds.Count, Is.EqualTo(sounds));
        }

        [TestCase(true, 55)]
        [TestCase(false, 32)]
        public void Pal3aNpcItemFinishesImmediatelyAndOnlyChangesTheActionWhenEnteringIdle(bool alreadyIdle, int nativeAction)
        {
            var catalog = new Catalog { NpcItemIdle = true };
            var director = new Director();
            BattleSession session = Session(catalog, director);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "A", 6201)), Is.True);
            session.Step();
            session.Step();
            const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var actors = (IDictionary)typeof(BattleSession).GetField("_byId", members).GetValue(session);
            object actor = actors["A"];
            object run = actor.GetType().GetField("Run", members).GetValue(actor);
            long action = (long)run.GetType().GetField("Id", members).GetValue(run);
            var stages = (IDictionary)typeof(BattleSession).GetField("_abilityPresentations", members).GetValue(session);
            object stage = stages[action];
            stage.GetType().GetField("BodyExecuted", members).SetValue(stage, false);
            stage.GetType().GetField("BodyFinished", members).SetValue(stage, false);
            BattleBodyAnimationBinding body = Binding(session);
            body.SetLogicalPose(alreadyIdle ? BattlePose.Idle : BattlePose.UsingSkill);
            body.SetRequestedNativeAction(55);
            typeof(BattleSession).GetMethod("TryAdvanceOrdinaryAbilityBody", members).Invoke(session, new[] { actor, run });
            Assert.That(body.RequestedNativeActionIndex, Is.EqualTo(nativeAction), "Published PAL3A 502d2e / 502e37 preserves the action if its state is already idle.");
            Assert.That(stage.GetType().GetField("BodyFinished", members).GetValue(stage), Is.True);
            Assert.That(session.CurrentBladeSeconds("A"), Is.EqualTo(.01f));
            Assert.That(director.Sounds, Is.Empty);
            Assert.That(body.CurrentAnimation, Is.SameAs(alreadyIdle ? catalog.Idle : catalog.External));
        }
    }
}
