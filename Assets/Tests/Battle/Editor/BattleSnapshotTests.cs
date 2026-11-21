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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    public sealed class BattleSnapshotTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings
        {
            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 2;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => 0;

            public int DamageDivisor(string actor) => 1;
        }

        private static BattleScenario Scenario(string variant, bool completed = true)
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Variant = variant;
            scenario.DataFingerprint = "snapshot-fixture";
            scenario.Items.Add(new BattleItem { Id = 6201, Count = 3 });
            scenario.TreasureUses.Add(new BattleItem { Id = 6119, Count = 2 });
            scenario.Participants[0].Skills.Add(new BattleSkill
            {
                Id = 301,
                Level = completed ? 2 : 4,
                Uses = 7,
                Mastered = true,
                NativeMagicGroup = 1,
                NativeMagicSlot = 3
            });
            if (completed)
                scenario.Participants[1].Vitals[0] = 0;
            return scenario;
        }

        private static BattleSession Session(BattleScenario scenario)
        {
            var ability = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 301,
                Name = "snapshot magic",
                IsMagic = true,
                SkillTypeId = 2,
                ElementMask = 2,
                ApplicableIdentities = new[] { 0 },
                Range = BattleTargetRange.OpponentSingle,
                Attributes = new[] { new BattleAttributeEffect(0, BattleValueMode.Absolute, -20) }
            });
            var definitions = new BattleDefinitions(scenario.Variant, scenario.DataFingerprint,
                new[] { ability }, Array.Empty<BattleAbilityDefinition>(), new[]
                {
                    new BattleActorDefinition(new BattleActorData { InstanceId = "Ally", Identity = 0 }),
                    new BattleActorDefinition(new BattleActorData { InstanceId = "Enemy", LootItem = 6202, LootCount = 4 })
                });
            return new BattleSession(new BattleSetup(scenario), new Catalog(), definitions: definitions);
        }

        [Test]
        public void ValueSnapshotsCaptureAllAuthoringFieldsWithoutRetainingTheirSource()
        {
            var item = new BattleItem { Id = 6201, Count = 3 };
            var skill = new BattleSkill { Id = 301, Level = 2, Uses = 7, Mastered = true, NativeMagicGroup = 1, NativeMagicSlot = 3 };
            var capturedItem = new BattleItemSnapshot(item);
            var capturedSkill = new BattleSkillSnapshot(skill);
            item.Id = item.Count = 999;
            skill.Id = skill.Level = skill.Uses = skill.NativeMagicGroup = skill.NativeMagicSlot = 999;
            skill.Mastered = false;
            Assert.That((capturedItem.Id, capturedItem.Count), Is.EqualTo((6201, 3)));
            Assert.That((capturedSkill.Id, capturedSkill.Level, capturedSkill.Uses, capturedSkill.Mastered,
                capturedSkill.NativeMagicGroup, capturedSkill.NativeMagicSlot), Is.EqualTo((301, 2, 7, true, 1, 3)));
            Assert.Throws<ArgumentNullException>(() => new BattleItemSnapshot(null));
            Assert.Throws<ArgumentNullException>(() => new BattleSkillSnapshot(null));
        }

        [TestCase(typeof(BattleItemSnapshot))]
        [TestCase(typeof(BattleSkillSnapshot))]
        public void SnapshotElementsExposeOnlyGettersAndNoWritablePublicFields(Type type)
        {
            Assert.That(type.IsSealed, Is.True);
            Assert.That(type.GetFields(BindingFlags.Instance | BindingFlags.Public), Is.Empty);
            Assert.That(type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .All(property => property.GetMethod != null && property.SetMethod == null), Is.True);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RetainedResultsAndLiveReadsRejectCollectionWritesAndRemainIndependentOfAuthoring(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            BattleSession session = Session(scenario);
            BattleResult result = session.Result;
            Assert.That(result.Status, Is.EqualTo(BattleSessionStatus.Completed));
            BattleActorSnapshot actor = result.Actors.Single(value => value.Id == "Ally");
            AssertReadOnly(result.Items);
            AssertReadOnly(result.Loot);
            AssertReadOnly(result.TreasureUses);
            AssertReadOnly(actor.Skills);
            AssertReadOnly(session.Inventory);
            AssertReadOnly(session.InventoryEntries);
            AssertReadOnly(session.TreasureUses);
            scenario.Items[0].Count = 999;
            scenario.TreasureUses[0].Count = 999;
            scenario.Participants[0].Skills[0].Uses = 999;
            Assert.That(result.Items, Is.SameAs(result.Items));
            Assert.That(result.Actors[0].Skills, Is.SameAs(result.Actors[0].Skills));
            Assert.That(result.Items.Select(item => (item.Id, item.Count)), Is.EqualTo(new[] { (6201, 3), (6202, 4) }));
            Assert.That(result.Loot.Select(item => (item.Id, item.Count)), Is.EqualTo(new[] { (6202, 4) }));
            Assert.That(result.TreasureUses.Single().Count, Is.EqualTo(2));
            Assert.That(actor.Skills.Single().Uses, Is.EqualTo(7));
            Assert.That(session.Inventory.Select(item => (item.Id, item.Count)), Is.EqualTo(new[] { (6201, 3), (6202, 4) }));
            Assert.That(session.Actors.Single(value => value.Id == "Ally").Skills.Single().Uses, Is.EqualTo(7));
        }

        private static void AssertReadOnly<T>(IReadOnlyList<T> values)
        {
            Assert.That(values, Is.Not.Empty);
            var collection = (IList<T>)values;
            Assert.That(collection.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => collection[0] = collection[0]);
            Assert.Throws<NotSupportedException>(() => collection.Clear());
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void CapturedSkillValuesDoNotFollowLaterAuthoritativeSkillUse(string variant)
        {
            BattleSession session = Session(Scenario(variant, completed: false));
            BattleActorSnapshot before = session.Actors.Single(actor => actor.Id == "Ally");
            Assert.That(session.Submit(new BattleCommand("Ally", BattleAction.Skill, "Enemy", 301)), Is.True);
            for (int tick = 0; tick < 10; tick++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running));
            Assert.That(before.Skills.Single().Uses, Is.EqualTo(7));
            Assert.That(session.Actors.Single(actor => actor.Id == "Ally").Skills.Single().Uses, Is.EqualTo(8));
            session.Abort();
            Assert.That(session.Result.Actors.Single(actor => actor.Id == "Ally").Skills.Single().Uses, Is.EqualTo(8));
            Assert.That(before.Skills.Single().Uses, Is.EqualTo(7));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void JsonReportExportPreservesEveryItemAndLearnedSkillField(string variant)
        {
            BattleSession session = Session(Scenario(variant));
            // Battle Editor report export uses the same default Newtonsoft settings.
            JObject json = JObject.Parse(JsonConvert.SerializeObject(new
            {
                Result = session.Result
            }));
            JObject result = (JObject)json["Result"];
            Assert.That(JToken.DeepEquals(result["Items"], JArray.Parse("[{\"Id\":6201,\"Count\":3},{\"Id\":6202,\"Count\":4}]")), Is.True);
            Assert.That(JToken.DeepEquals(result["Loot"], JArray.Parse("[{\"Id\":6202,\"Count\":4}]")), Is.True);
            Assert.That(JToken.DeepEquals(result["TreasureUses"], JArray.Parse("[{\"Id\":6119,\"Count\":2}]")), Is.True);
            JObject skill = (JObject)result["Actors"].Single(actor => (string)actor["Id"] == "Ally")["Skills"][0];
            Assert.That(skill.Properties().Select(property => property.Name), Is.EquivalentTo(new[]
            {
                "Id", "Level", "Uses", "Mastered", "NativeMagicGroup", "NativeMagicSlot"
            }));
            Assert.That(JToken.DeepEquals(skill, JObject.Parse("{\"Id\":301,\"Level\":2,\"Uses\":7,\"Mastered\":true,\"NativeMagicGroup\":1,\"NativeMagicSlot\":3}")), Is.True);
            Assert.That(session.Actors.Single(actor => actor.Id == "Ally").Skills.Single().Uses, Is.EqualTo(7));
        }
    }
}
