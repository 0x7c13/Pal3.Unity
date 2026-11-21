// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Core.Contract.Constants;
    using Core.DataReader.Dat;
    using NUnit.Framework;
    using UnityEngine;

    // These tests load original records and action clips. A present but invalid
    // data root or a failed supported resource is a failure, never a skipped case.
    [Category("LocalGameData")]
    public sealed class BattleAbilitiesIntegrationTests
    {
        private BattleData _data;
        private readonly List<BattlePreview> _previews = new();

        [SetUp]
        public void Load()
        {
            string configured = Environment.GetEnvironmentVariable(BattleData.Variant + "_TEST_DATA_ROOT");
            string root = configured ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", BattleData.Variant));
            if (!Directory.Exists(root) && configured == null)
                Assert.Ignore("Local original data unavailable.");
            Assert.That(Directory.Exists(root), Is.True, "The current variant test data root must point to its original data.");
            _data = new BattleData(root, 936);
        }

        [TearDown]
        public void Dispose()
        {
            foreach (BattlePreview preview in _previews)
                preview.Dispose();
            _previews.Clear();
            _data?.Dispose();
        }

        private BattleScenario Scenario()
        {
            var scenario = new BattleScenario
            {
                Variant = BattleData.Variant,
                Scene = _data.Scenes.First(),
                DataFingerprint = _data.Fingerprint,
                Encounter = BattleEncounter.AllyAmbush
            };
            BattleParticipant ally = _data.CreateParticipant(101, BattleSide.Ally, BattleSlot.Water);
            ally.InstanceId = "A";
            int enemyId = _data.Database.CombatActorInfos.Keys.OrderBy(id => id).First(id => BattleAnimationCatalog.IsBasicEnemy(_data, id) &&
                _data.Resources.Find(FileConstants.GetActorFolderVirtualPath(_data.Database.CombatActorInfos[id].ModelId) + _data.Database.CombatActorInfos[id].ModelId + ".ini") != null);
            BattleParticipant enemy = _data.CreateParticipant(enemyId, BattleSide.Enemy, BattleSlot.Fire);
            enemy.InstanceId = "E";
            foreach (BattleParticipant actor in new[] { ally, enemy })
            {
                actor.Level = 1;
                actor.Attributes = new[] { 1000, 100, 100, 60, 10, 3, 0, 0, 0, 0, 0, 0 };
                actor.Vitals = new[] { 1000, 100, 100 };
                actor.Skills.Clear();
#if PAL3
                if (actor.ActorId == 101)
                    actor.Equipment[0] = 6601; // Ordinary sword; magic-sword cultivation is a separate scenario.
#endif
                scenario.Participants.Add(actor);
            }
            return scenario;
        }

        private BattleSession Create(BattleScenario scenario, out BattleSetup setup, out BattleAnimationCatalog catalog)
        {
            setup = new BattleSetup(scenario);
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, setup);
            catalog = new BattleAnimationCatalog(_data, setup, definitions);
            var preview = new BattlePreview(_data, setup);
            _previews.Add(preview);
            preview.Audio.Muted = true;
            preview.Effects.Muted = true;
            var session = new BattleSession(setup, catalog, definitions: definitions, effects: preview.Effects);
            preview.BindBattle(session, catalog);
            session.SetAutomatic(BattleSide.Enemy, false);
            return session;
        }

        private static BattleActorSnapshot Actor(BattleSession session, string id) => session.Actors.Single(actor => actor.Id == id);

        private static void Steps(BattleSession session, int count)
        {
            for (int index = 0; index < count; index++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running), session.Result?.Reason);
        }

        private int NativeWorkTick(int abilityId, int preparationTicks)
        {
            float work = _data.Read("basedata.cpk\\cbdata\\efflinker.dat", new EffectLinkerFileReader())
                .SkillIdToEffectGroupMap[abilityId][0].WorkTimeInSeconds;
            int tick = preparationTicks;
            while ((double)(float)(tick * BattleSession.StepSeconds) - (float)(preparationTicks * BattleSession.StepSeconds) < work)
                tick++;
            return tick;
        }

        private static string Trace(BattleSession session) => string.Join("\n", session.Events.Select(entry =>
            $"{entry.Tick}|{entry.Kind}|{entry.Source}|{entry.Target}|{entry.ActionId}|{entry.AbilityId}|{entry.Resource}|{entry.ValueBefore}|{entry.ValueAfter}|{entry.RequestedDelta}|{entry.ActualDelta}|{entry.RandomState}|{entry.Detail}"));

        [Test]
        public void SpecialItemsAreClassifiedByTheirVerifiedOriginalHandlers()
        {
            BattleScenario scenario = Scenario();
            BattleDefinitions definitions = BattleDefinitionCompiler.Build(_data, new BattleSetup(scenario));
#if PAL3
            const int lastTreasure = 6125;
#else
            const int lastTreasure = 6124;
            Assert.That(definitions.Items.ContainsKey(6125), Is.False);
#endif
            for (int id = 6119; id <= lastTreasure; id++)
            {
                Assert.That(definitions.Items[id].IsTreasure, Is.True, "Treasure ID " + id);
                Assert.That(definitions.Items[id].Supported, Is.True, definitions.Items[id].UnsupportedReason);
            }
            Assert.That(definitions.Items[6230].Handler, Is.EqualTo(BattleSpecialHandler.PoisonDragonGall));
            Assert.That(definitions.Items[6230].Supported, Is.True, definitions.Items[6230].UnsupportedReason);
#if PAL3A
            Assert.That(definitions.Skills[4504].Handler, Is.EqualTo(BattleSpecialHandler.ReturnedMpAbsorb));
            Assert.That(definitions.Skills[4504].Supported, Is.True, definitions.Skills[4504].UnsupportedReason);
#endif
        }

        [Test]
        public void FourthMonsterSkillUsesNativeFixedProficiencyAndDoesNotReadPadding()
        {
#if PAL3
            const int actorId = 726;
            const int skillId = 217;
#else
            const int actorId = 703;
            const int skillId = 251;
#endif
            var record = _data.Database.CombatActorInfos[actorId];
            Assert.That(record.SkillLevels.Length, Is.EqualTo(3));
            Assert.That(record.SkillIds[3], Is.EqualTo(skillId));
            BattleParticipant actor = _data.CreateParticipant(actorId, BattleSide.Enemy, BattleSlot.Water);
            Assert.That(actor.Skills.Single(skill => skill.Id == skillId).Level, Is.EqualTo(4));
        }

        [Test]
        public void RealWindSpellUsesNativeWorkTimingAndPaysFourMpAfterItsEffect()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[0].Skills.Add(new BattleSkill { Id = 301, Level = 4, Mastered = true });
            BattleSession session = Create(scenario, out BattleSetup setup, out BattleAnimationCatalog catalog);
            BattleAbilityDefinition wind = session.Definitions.Skills[301];
            Assert.That(wind.Supported, Is.True, wind.UnsupportedReason);
            Assert.That(wind.IsMagic, Is.True);
            Assert.That(wind.SkillTypeId, Is.EqualTo(2));
            Assert.That(wind.Costs.Single(cost => cost.Resource == 2).Value, Is.EqualTo(4));
#if PAL3A
            Assert.That(wind.UsesBeforeLevelUp, Is.EqualTo(new[] { 0, 10, 20, 30 }));
#else
            Assert.That(wind.UsesBeforeLevelUp, Is.EqualTo(new[] { 0, 15, 30, 45 }));
#endif
            var command = new BattleCommand("A", BattleAction.Skill, "E", 301);
            Assert.That(session.CanSubmit(command), Is.Null, "LearningActorLevel gates the next unlock, not an already mastered spell.");
            Assert.That(session.Submit(command), Is.True);
            int workTick = NativeWorkTick(301, 3);
            Steps(session, workTick - 1);
            Assert.That(Actor(session, "E").Hp, Is.EqualTo(1000));
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(100));
            Steps(session, 1);
            Assert.That(Actor(session, "E").Hp, Is.LessThan(1000));
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(96));
            Assert.That(Actor(session, "A").Skills.Single().Uses, Is.EqualTo(1));
            BattleEvent[] effects = session.Events.Where(entry => entry.Kind == BattleEventKind.Hit || entry.Kind == BattleEventKind.ResourceChanged && entry.Resource == 2).ToArray();
            Assert.That(effects.Select(entry => entry.Resource), Is.EqualTo(new[] { 0, 2 }));
            var replayPreview = new BattlePreview(_data, setup);
            _previews.Add(replayPreview);
            replayPreview.Audio.Muted = replayPreview.Effects.Muted = true;
            BattleSession replay = BattleReplay.Play(setup, catalog, session.Inputs, session.Tick, definitions: session.Definitions,
                effects: replayPreview.Effects, onCreated: created => replayPreview.BindBattle(created, catalog));
            Assert.That(Trace(replay), Is.EqualTo(Trace(session)));
            Assert.That(scenario.Participants[0].Vitals[2], Is.EqualTo(100));
            Assert.That(scenario.Participants[0].Skills.Single().Uses, Is.Zero);
        }

        [Test]
        public void RealFlameTechniqueApproachesHitsAndReturnsWithoutFaulting()
        {
#if PAL3A
            const int sourceId = 105, skillId = 4506, remainingSp = 75;
            const BattleMovementKind approach = BattleMovementKind.Fly;
#else
            const int sourceId = 102, skillId = 4505, remainingSp = 65;
            const BattleMovementKind approach = BattleMovementKind.Run;
#endif
            BattleScenario scenario = Scenario();
            var source = _data.CreateParticipant(sourceId, BattleSide.Ally, BattleSlot.Water);
            source.InstanceId = "A";
            source.Level = 45;
            source.Attributes[1] = source.Vitals[1] = 100;
            source.Skills.Clear();
            source.Skills.Add(new BattleSkill { Id = skillId, Level = 4, Mastered = true });
            scenario.Participants[0] = source;
            BattleParticipant enemy = scenario.Participants[1];
            enemy.Attributes[0] = enemy.Vitals[0] = 100000;
            BattleSession session = Create(scenario, out _, out BattleAnimationCatalog catalog);
            BattlePreview preview = _previews.Last();
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, "E", skillId)), Is.True);

            bool travelled = false;
            for (int step = 0; step < 500 && !session.Events.Any(entry => entry.Kind == BattleEventKind.ActionEnded && entry.Source == "A"); step++)
            {
                Steps(session, 1);
                preview.ApplyBattle(session, catalog);
                BattleActorSnapshot actor = Actor(session, "A");
                if (actor.Movement is { } motion && motion.Plan.Kind == approach &&
                    !actor.Position.Equals(motion.Plan.From) && !actor.Position.Equals(motion.Plan.To) &&
                    !session.Events.Any(entry => entry.Kind == BattleEventKind.Hit && entry.Source == "A"))
                    travelled = true;
            }

            Assert.That(travelled, Is.True, "The actor must travel before impact, including when takeoff comes from a jump marker.");
            Assert.That(Actor(session, "E").Hp, Is.LessThan(100000));
            Assert.That(session.Events.Any(entry => entry.Kind == BattleEventKind.ActionEnded && entry.Source == "A"), Is.True);
            Assert.That(Actor(session, "A").Position, Is.EqualTo(Actor(session, "A").Home));
            Assert.That(Actor(session, "A").Sp, Is.EqualTo(remainingSp));
        }

        [Test]
        public void RealMedicineConsumesOneAtStartAndHealsExactlyItsOriginalAbsoluteValue()
        {
            BattleScenario scenario = Scenario();
            scenario.Participants[0].Vitals[0] = 50;
            scenario.Items.Add(new BattleItem { Id = 6201, Count = 2 });
            BattleSession session = Create(scenario, out _, out _);
            BattleAbilityDefinition medicine = session.Definitions.Items[6201];
            Assert.That(medicine.Supported, Is.True, medicine.UnsupportedReason);
            Assert.That(medicine.BattleUsable, Is.True);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "A", 6201)), Is.True);
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(2));
            Assert.That(Actor(session, "A").Hp, Is.EqualTo(50));
            Steps(session, 1);
            Assert.That(session.Inventory.Single().Count, Is.EqualTo(1));
            int workTick = NativeWorkTick(6201, 2);
            Steps(session, workTick - 2);
            Assert.That(Actor(session, "A").Hp, Is.EqualTo(50));
            Steps(session, 1);
            Assert.That(Actor(session, "A").Hp, Is.EqualTo(150));
            Assert.That(Actor(session, "A").Mp, Is.EqualTo(100));
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.ItemChanged), Is.EqualTo(1));
            Assert.That(scenario.Items.Single().Count, Is.EqualTo(2));
        }

        [Test]
        public void RealRevivalMedicineCanSelectEitherAllyAndRestoresTenPercentOnRevival()
        {
            BattleScenario scenario = Scenario();
            BattleParticipant dead = _data.CreateParticipant(104, BattleSide.Ally, BattleSlot.Fire);
            dead.InstanceId = "B";
            dead.Attributes = new[] { 500, 100, 100, 60, 10, 3, 0, 0, 0, 0, 0, 0 };
            dead.Vitals = new[] { 0, 100, 100 };
            dead.Skills.Clear();
            scenario.Participants.Add(dead);
            scenario.Items.Add(new BattleItem { Id = 6211, Count = 1 });
            BattleSession session = Create(scenario, out _, out _);
            BattleAbilityDefinition revival = session.Definitions.Items[6211];
            Assert.That(revival.Supported, Is.True, revival.UnsupportedReason);
            Assert.That(revival.Revives, Is.True);
            var command = new BattleCommand("A", BattleAction.Item, "B", 6211);
            Assert.That(session.GetTargetCandidates(command).Select(actor => actor.Id), Is.EquivalentTo(new[] { "A", "B" }));
            Assert.That(session.Submit(command), Is.True);
            Steps(session, NativeWorkTick(6211, 2));
            Assert.That(Actor(session, "B").Hp, Is.EqualTo(50));
            Assert.That(Actor(session, "B").Gauge, Is.GreaterThanOrEqualTo(11500 * 2 / 3));
            Assert.That(session.Inventory, Is.Empty);
            Assert.That(dead.Vitals[0], Is.Zero);
        }
    }
}
