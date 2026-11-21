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

    public sealed class BattleMagicSwordSessionTests
    {
        private class Catalog : IBattleCatalog, IBattleAbilityTimings, IBattleMagicSwordTimings
        {
            public BattleMagicSwordStageProfile Profile = new(438, -1, 0, 0, 5);

            public AttackTiming Attack(string source, string target) => new(0, 7, new[] { 2, 4 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 5, new[] { 0 });

            public BattleMagicSwordStageProfile MagicSword(string actor, BattleAbilityDefinition ability) => Profile;

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "A" ? 0 : 1;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class BodyCatalog : Catalog, IBattleAbilityBodyAnimations, IBattlePhysicalTimings
        {
            public readonly BattlePhysicalAnimation Weapon = new("z13", 100, 10, gameBoxDuration: 1200);

            public BattlePhysicalAnimation AbilityBody(string actor, BattleAbilityDefinition ability) => Weapon;

            public int? AbilityNativeAction(string actor, BattleAbilityDefinition ability) => 40;

            public BattlePhysicalAnimation AbilityMovementBody(string actor, BattleAbilityDefinition ability, BattleMovementKind kind, bool returning) => null;

            public BattlePhysicalStageProfile Physical(string actor) => new("PAL3", actor == "A" ? 101 : 701,
                actor == "A" ? 0 : -1, 1, 1, new Dictionary<BattlePose, BattlePhysicalAnimation>
                {
                    [BattlePose.Idle] = new("z10", 100, 10)
                });
        }

        private sealed class ReadyRules : IBattleRules
        {
            public int Speed = 1;

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => BattleSession.FullGauge;

            public int GaugePerStep(BattleParticipant actor) => Speed;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1) => new(-1, false, false);
        }

        private sealed class Director : IBattleEffectDirector
        {
            private readonly Dictionary<long, int> _frames = new();
            private long _next;
            public readonly List<BattleEffectRequest> Started = new();
            public readonly List<long> Stopped = new();
            public int RunningFrames = 4;

            public long Start(BattleEffectRequest request)
            {
                Started.Add(request);
                _frames.Add(++_next, RunningFrames);
                return _next;
            }

            public bool IsRunning(long handle) => _frames.GetValueOrDefault(handle) > 0;

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
                foreach (long key in _frames.Keys.ToArray())
                    if (unscaledSeconds > 0)
                        _frames[key]--;
            }

            public void Stop(long handle)
            {
                Stopped.Add(handle);
                _frames.Remove(handle);
            }
        }

        private static BattleParticipant Participant(string id, BattleSide side, int hp = 500) => new()
        {
            InstanceId = id,
            ActorId = side == BattleSide.Ally ? 101 : 701,
            Side = side,
            Attributes = new[] { 500, 100, 100, 100, 0, 3, 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { hp, 100, 100 }
        };

        private static BattleScenario Scenario(float energy = 200, int skill = 2224)
        {
            var scenario = new BattleScenario { Scene = "fixture", DataFingerprint = "magic-sword", Seed = 1 };
            BattleParticipant source = Participant("A", BattleSide.Ally);
            source.Equipment[0] = BattleMagicSwordRules.WeaponId;
            source.MagicSword = new BattleMagicSword { Energy = energy, SkillId = skill };
            scenario.Participants.Add(source);
            scenario.Participants.Add(Participant("E", BattleSide.Enemy));
            return scenario;
        }

        private static BattleAbilityDefinition WeaponSkill(int id = 2224, BattleTargetRange range = BattleTargetRange.OpponentAll, int value = -100, bool revive = false) => new(new BattleAbilityData
        {
            Id = id,
            Name = "native weapon " + id,
            SkillTypeId = 4,
            Range = range,
            SuccessRate = 0,
            ApplicableIdentities = new[] { 1, 2, 3, 4 },
            Attributes = revive ? new[] { new BattleAttributeEffect(0, BattleValueMode.Percentage, 100), new BattleAttributeEffect(2, BattleValueMode.Percentage, 100) } :
                new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, value) },
            States = revive ? new[] { new BattleStatusEffect(BattleState.Death, true) } : Array.Empty<BattleStatusEffect>()
        });

        private static BattleDefinitions Definitions(BattleScenario scenario, BattleAbilityDefinition ability, BattleState? enemyState = null) => new(scenario.Variant, scenario.DataFingerprint,
            new[] { ability }, Array.Empty<BattleAbilityDefinition>(), scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
            {
                InstanceId = actor.InstanceId,
                Identity = actor.InstanceId == "A" ? 0 : actor.Side == BattleSide.Ally ? 1 : -1,
                MagicSword = actor.MagicSword?.Capture(),
                Equipment = actor.MagicSword == null ? Array.Empty<BattleEquipmentDefinition>() : new[]
                {
                    new BattleEquipmentDefinition(new BattleEquipmentData { Id = BattleMagicSwordRules.WeaponId, Slot = 0 })
                },
                InitialStates = actor.InstanceId == "E" && enemyState.HasValue ? new[] { enemyState.Value } : Array.Empty<BattleState>()
            })));

        private static BattleSession Session(BattleScenario scenario, BattleAbilityDefinition ability, Director director = null, Catalog catalog = null, BattleState? enemyState = null) =>
            new(new BattleSetup(scenario), catalog ?? new Catalog(), new ReadyRules(), Definitions(scenario, ability, enemyState), director ?? new Director());

        private static void Until(BattleSession session, Func<bool> done)
        {
            for (int index = 0; index < 250 && !done() && session.Status == BattleSessionStatus.Running; index++)
                session.Step();
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
            Assert.That(done(), Is.True, "The expected native boundary was not reached.");
        }

        [Test]
        public void ReleasedDefaultsAndCapturedCopiesKeepTheActualRawWindIndex()
        {
            BattleScenario scenario = Scenario();
            var setup = new BattleSetup(scenario);
            scenario.Participants[0].MagicSword.AttackBonus = 999;
            BattleScenario first = setup.CreateInstance();
            first.Participants[0].MagicSword.Energy = 1;
            BattleMagicSword captured = setup.CreateInstance().Participants[0].MagicSword;
            Assert.That((captured.AttackBonus, (int)captured.SelectedElement, captured.SkillId, captured.ChargeCoefficient, captured.Energy), Is.EqualTo((35, 3, 2224, 10u, 200f)));
        }

        [TestCase(-1f)]
        [TestCase(201f)]
        [TestCase(float.NaN)]
        public void ScenarioRejectsInvalidCapturedEnergy(float energy)
        {
            Assert.Throws<ArgumentException>(() => new BattleSetup(Scenario(energy)));
        }

        [Test]
        public void ScenarioRejectsSwordWithoutCultivationAndPal3aSword()
        {
            BattleScenario scenario = Scenario();
            scenario.Variant = "PAL3A";
            Assert.That(scenario.Validate(), Is.Not.Empty);
            scenario.Variant = "PAL3";
            scenario.Participants[0].MagicSword = null;
            Assert.That(scenario.Validate(), Is.Not.Empty);
        }

        [TestCase(199.9f, false)]
        [TestCase(199.90000915527344f, true)]
        public void WeaponCommandUsesStrictNativeEnergyAndIgnoresOrdinaryLearningAndIdentityLists(float energy, bool allowed)
        {
            BattleSession session = Session(Scenario(energy), WeaponSkill());
            Assert.That(session.Actors[0].Skills, Is.Empty);
            Assert.That(session.CanSubmit(new BattleCommand("A", BattleAction.Skill, abilityId: 2224)) == null, Is.EqualTo(allowed));
        }

        [Test]
        public void EveryPhysicalWorkChargesEvenWhenEvadeRejectsDamage()
        {
            BattleSession session = Session(Scenario(0), WeaponSkill(), enemyState: BattleState.Evade);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Attack, "E")), Is.True);
            Until(session, () => session.Events.Count(entry => entry.Kind == BattleEventKind.Miss) >= 2);
            Assert.That(session.MagicSwords["A"].Energy, Is.EqualTo(20));
            Assert.That(session.Actors.Single(actor => actor.Id == "E").Hp, Is.EqualTo(500));
        }

        [Test]
        public void OriginalEffectCompletionFreezesCombatClockAndResumesBeforeDamage()
        {
            var director = new Director { RunningFrames = 8 };
            BattleSession session = Session(Scenario(), WeaponSkill(), director);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 2224)), Is.True);
            Until(session, () => director.Started.Count > 0);
            Assert.That(session.MagicSwords["A"].Energy, Is.Zero);
            long combat = session.SimulationTick, wall = session.Tick;
            session.Step();
            session.Step();
            Assert.That(session.Tick, Is.EqualTo(wall + 2));
            Assert.That(session.SimulationTick, Is.EqualTo(combat));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit), Is.False);
            Assert.That(director.Started.Single().AbsoluteTime, Is.True);
            Until(session, () => session.Events.Any(entry => entry.Kind == BattleEventKind.Hit));
            Assert.That(session.Events.Single(entry => entry.Kind == BattleEventKind.Hit).RequestedDelta, Is.EqualTo(-100));
            Until(session, () => session.SimulationTick > combat);
        }

        [Test]
        public void WeaponBodyUsesTheActualSharedClipAndAdvancesDuringTheAbsoluteEffect()
        {
            var director = new Director { RunningFrames = 8 };
            var catalog = new BodyCatalog();
            BattleSession session = Session(Scenario(), WeaponSkill(), director, catalog);
            session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 2224)), Is.True);
            Until(session, () => director.Started.Count > 0);
            BattleActorSnapshot first = session.Actors.Single(actor => actor.Id == "A");
            Assert.That(first.NativeAnimationClip, Is.EqualTo("z13"));
            Assert.That(session.CurrentBodyAnimation("A"), Is.SameAs(catalog.Weapon));
            Assert.That(session.CurrentBladeSeconds("A"), Is.EqualTo(.25f));
            long combat = session.SimulationTick;
            session.Step();
            BattleActorSnapshot next = session.Actors.Single(actor => actor.Id == "A");
            Assert.That(session.SimulationTick, Is.EqualTo(combat));
            Assert.That(next.NativeAnimationSeconds, Is.GreaterThan(first.NativeAnimationSeconds));
            Until(session, () => session.CurrentBladeSeconds("A") == .01f);
            Assert.That(session.Tick, Is.LessThan(100), "Actual raw duration and popped Over finish the body before its deliberately overstated tick estimate.");
            Until(session, () => session.Events.Any(entry => entry.Kind == BattleEventKind.ActionEnded));
            Assert.That(session.CurrentBodyAnimation("A").Clip, Is.EqualTo("z10"));
        }

        [Test]
        public void ResidualSwordUsesPurificationPowerAndBypassesOrdinaryBarrierAndTechniqueRolls()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[0].MagicSword.PurificationCount = 3;
            BattleSession session = Session(scenario, WeaponSkill(), enemyState: BattleState.Barrier);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 2224)), Is.True);
            Until(session, () => session.Events.Any(entry => entry.Kind == BattleEventKind.Hit));
            Assert.That(session.Events.Single(entry => entry.Kind == BattleEventKind.Hit).RequestedDelta, Is.EqualTo(-250));
            Assert.That(session.MagicSwords["A"].Energy, Is.Zero);
        }

        [Test]
        public void AlliedWeaponPowerUsesMaxPercentageEvenWhenGdbModeIsAbsolute()
        {
            BattleScenario scenario = Scenario(skill: 2218);
            scenario.Participants[0].Vitals[0] = 10;
            BattleSession session = Session(scenario, WeaponSkill(2218, BattleTargetRange.OwnAll, 50));
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 2218)), Is.True);
            Until(session, () => session.Events.Any(entry => entry.Resource == 0));
            BattleEvent recovery = session.Events.First(entry => entry.Resource == 0);
            Assert.That((recovery.RequestedDelta, recovery.HpAfter), Is.EqualTo((250, 260)));
        }

        [Test]
        public void WeaponSkillCapturesUnselectedSlotsAndPreservesThePreviousPlayerCommandForRepeat()
        {
            BattleScenario scenario = Scenario();
            BattleParticipant dead = Participant("D", BattleSide.Enemy, 0);
            dead.Slot = BattleSlot.Fire;
            scenario.Participants.Add(dead);
            var setup = new BattleSetup(scenario);
            var definitions = Definitions(scenario, WeaponSkill(), BattleState.Invisible);
            var director = new Director();
            var rules = new ReadyRules { Speed = BattleSession.FullGauge };
            var session = new BattleSession(setup, new Catalog(), rules, definitions, director);
            session.SetAutomatic(BattleSide.Enemy, false);
            void WaitForPlayer()
            {
                for (int step = 0; step < 250 && session.WaitingActor != "A"; step++)
                {
                    if (session.WaitingActor == "E")
                        Assert.That(session.Submit("E", BattleAction.Defend), Is.True);
                    session.Step();
                }
                Assert.That(session.WaitingActor, Is.EqualTo("A"), session.Result?.Reason);
            }

            Assert.That(session.GetTargetCandidates(new BattleCommand("A", BattleAction.Attack)), Is.Empty);
            Assert.That(session.Submit("A", BattleAction.Defend), Is.True);
            WaitForPlayer();
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 2224)), Is.True);
            WaitForPlayer();
            Assert.That(director.Started.First().Targets, Is.EqualTo(new[] { "E", "D" }));
            Assert.That(session.RequestAiRepeat("A"), Is.True);
            Assert.That(session.Inputs.Last().Action, Is.EqualTo(BattleAction.Defend));
            BattleSession replay = BattleReplay.Play(setup, new Catalog(), session.Inputs, session.Tick, rules, definitions, new Director());
            Assert.That(replay.Inputs.Last().Action, Is.EqualTo(BattleAction.Defend));
        }

        [Test]
        public void Native2221AttemptsValuesBeforeDeathRemovalAndDoesNotGrantRecoveryToTheDead()
        {
            BattleScenario scenario = Scenario(skill: 2221);
            BattleParticipant dead = Participant("D", BattleSide.Ally, 0);
            dead.Slot = BattleSlot.Fire;
            dead.Vitals[2] = 0;
            scenario.Participants.Add(dead);
            BattleSession session = Session(scenario, WeaponSkill(2221, BattleTargetRange.OwnAll, revive: true));
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 2221)), Is.True);
            Until(session, () => session.Events.Any(entry => entry.Kind == BattleEventKind.StatusRemoved && entry.Target == "D"));
            BattleActorSnapshot target = session.Actors.Single(actor => actor.Id == "D");
            Assert.That((target.Hp, target.Mp), Is.EqualTo((0, 0)));
            Assert.That(session.Events.Any(entry => entry.Target == "D" && entry.Resource is 0 or 2 && entry.ActualDelta > 0), Is.False);
        }

        [Test]
        public void CancellingAnAbsoluteTimeActionRestoresTheClockAndRetainsSpentEnergy()
        {
            var director = new Director { RunningFrames = 100 };
            BattleSession session = Session(Scenario(), WeaponSkill(), director);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 2224)), Is.True);
            Until(session, () => director.Started.Count > 0);
            long before = session.SimulationTick;
            Assert.That(session.CancelAction("A"), Is.True);
            session.Step();
            Assert.That(session.SimulationTick, Is.GreaterThan(before));
            Assert.That(session.MagicSwords["A"].Energy, Is.Zero);
            Assert.That(director.Stopped, Is.Not.Empty);
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.Hit), Is.False);
        }

        [Test]
        public void UnspentEnergyIsCapturedInTheResultAndReplayUsesItsOwnGauge()
        {
            BattleScenario scenario = Scenario(190);
            var setup = new BattleSetup(scenario);
            var catalog = new Catalog();
            BattleDefinitions definitions = Definitions(scenario, WeaponSkill());
            var session = new BattleSession(setup, catalog, new ReadyRules(), definitions, new Director());
            session.Submit(new BattleCommand("A", BattleAction.Attack, "E"));
            Until(session, () => session.MagicSwords["A"].Energy == 200);
            session.Abort();
            Assert.That(session.Result.MagicSwords["A"].Energy, Is.EqualTo(200));
            Assert.That(definitions.Actors["A"].MagicSword.Energy, Is.EqualTo(190));
            Assert.That(setup.CreateInstance().Participants[0].MagicSword.Energy, Is.EqualTo(190));
            BattleSession replay = BattleReplay.Play(setup, catalog, session.Inputs, session.Tick, new ReadyRules(), definitions);
            Assert.That(replay.Result.MagicSwords["A"].Energy, Is.EqualTo(200));
        }
    }
}
