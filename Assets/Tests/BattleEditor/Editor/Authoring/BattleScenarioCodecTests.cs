// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Battle;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class BattleScenarioCodecTests
    {
        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void LegacyUnityDraftRecoversAbsentSwordSnapshotsWithoutChangingOtherInputs(string variant)
        {
            BattleScenario scenario = BattleScenarioTests.Scenario();
            scenario.Variant = variant;
            string legacy = JsonUtility.ToJson(scenario);
            BattleScenario unmodified = BattleScenarioJson.Deserialize(legacy);
            Assert.That(unmodified.Participants[1].MagicSword, Is.Not.Null, "Exercise the actual old Unity codec defect.");
            BattleScenario restored = BattleScenarioJson.RestoreDraft(legacy);
            Assert.That(restored.Participants.All(actor => actor.MagicSword == null), Is.True);
            Assert.That(restored.Participants[0].Attributes, Is.EqualTo(scenario.Participants[0].Attributes));
            Assert.That(restored.Participants[0].Vitals, Is.EqualTo(scenario.Participants[0].Vitals));
            Assert.That(restored.Validate(), Is.Empty);
        }

        [Test]
        public void LegacyEquippedSwordRetainsItsRealCultivationAndEnergy()
        {
            BattleScenario scenario = BattleScenarioTests.Scenario();
            scenario.Participants[0].Equipment[0] = 6603;
            scenario.Participants[0].MagicSword = new BattleMagicSword { AttackBonus = 77, Energy = 132.5f };
            BattleScenario restored = BattleScenarioJson.RestoreDraft(JsonUtility.ToJson(scenario));
            Assert.That(restored.Participants[0].MagicSword.AttackBonus, Is.EqualTo(77));
            Assert.That(restored.Participants[0].MagicSword.Energy, Is.EqualTo(132.5f));
            Assert.That(restored.Participants[1].MagicSword, Is.Null);
            Assert.That(restored.Validate(), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CanonicalDraftKeepsExplicitInvalidInputForValidation(bool beforeMarker)
        {
            BattleScenario scenario = BattleScenarioTests.Scenario();
            scenario.Participants[1].MagicSword = new BattleMagicSword { AttackBonus = 777 };
            string json = beforeMarker ? JsonConvert.SerializeObject(scenario) : BattleScenarioJson.Serialize(scenario);
            BattleScenario restored = BattleScenarioJson.RestoreDraft(json);
            Assert.That(restored.Participants[1].MagicSword.AttackBonus, Is.EqualTo(777));
            Assert.That(restored.Validate().Any(error => error.Contains("magic sword snapshot requires")), Is.True);
        }

        [Test]
        public void OldWindowJsonIsMigratedByTheActualOnEnablePath()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var window = ScriptableObject.CreateInstance<BattleEditorWindow>();
            MethodInfo disable = typeof(BattleEditorWindow).GetMethod("OnDisable", flags);
            try
            {
                disable.Invoke(window, null);
                string legacy = JsonUtility.ToJson(BattleScenarioTests.Scenario());
                typeof(BattleEditorWindow).GetField("_draftJson", flags).SetValue(window, legacy);
                typeof(BattleEditorWindow).GetField("_savedJson", flags).SetValue(window, legacy);
                typeof(BattleEditorWindow).GetMethod("OnEnable", flags).Invoke(window, null);
                var draft = (BattleScenarioAsset)typeof(BattleEditorWindow).GetField("_draft", flags).GetValue(window);
                Assert.That(draft.Scenario.Participants.All(actor => actor.MagicSword == null), Is.True);
                Assert.That(draft.Scenario.Validate(), Is.Empty);
                disable.Invoke(window, null);
                Assert.That((string)typeof(BattleEditorWindow).GetField("_draftJson", flags).GetValue(window), Does.Contain("$battleScenarioFormat"));
            }
            finally
            {
                window.DiscardChanges();
                Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void AnUnsetAuthoringAssetDoesNotThrowDuringUndoSerialization()
        {
            var asset = ScriptableObject.CreateInstance<BattleScenarioAsset>();
            try
            {
                Assert.That(asset.Scenario.Variant, Is.EqualTo(BattleData.Variant));
                asset.Scenario = null;
                Assert.DoesNotThrow(asset.OnBeforeSerialize);
                asset.OnAfterDeserialize();
                Assert.That(asset.Scenario.Variant, Is.EqualTo(BattleData.Variant));
                Assert.That(BattleScenarioEditing.NeedsRepair(asset.Scenario), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void UnsupportedAuthoredStatusIsDiagnosedBeforeStartingBattle()
        {
            var scenario = new BattleScenario { Scene = "fixture" };
            var actor = new BattleParticipant { ActorId = 101, InstanceId = "ally" };
            scenario.Participants.Add(actor);
            actor.Statuses.Add(new BattleStatus { Id = (int)BattleState.ExpIncrease, Rounds = 1 });
            Assert.That(scenario.Validate(), Has.Some.Contains("not supported as an initial status"));
        }

        [Test]
        public void MissingAuthoringContainersHaveAnExplicitLosslessRepair()
        {
            var actor = new BattleParticipant
            {
                ActorId = 101,
                Attributes = new[] { 77, 8 },
                Vitals = null,
                Equipment = new[] { 123 },
                Skills = null,
                Statuses = new() { null, new BattleStatus { Id = 2 } }
            };
            var scenario = new BattleScenario
            {
                Camera = null,
                Pal3A = null,
                Items = null,
                TreasureUses = new() { null, new BattleItem { Id = 9, Count = 2 } },
                Participants = new() { actor, null }
            };
            Assert.That(BattleScenarioEditing.NeedsRepair(scenario), Is.True);
            BattleScenario roundTrip = BattleScenarioJson.Deserialize(BattleScenarioJson.Serialize(scenario));
            Assert.That(roundTrip.Items, Is.Null, "Serialization must not silently repair invalid authoring input.");
            BattleScenario repaired = BattleScenarioEditing.Repair(roundTrip);
            Assert.That(BattleScenarioEditing.NeedsRepair(repaired), Is.False);
            Assert.That(repaired.Participants, Has.Count.EqualTo(1));
            Assert.That(repaired.Participants[0].Attributes.Take(2), Is.EqualTo(new[] { 77, 8 }));
            Assert.That(repaired.Participants[0].Equipment[0], Is.EqualTo(123));
            repaired.Participants[0].Skills.Add(new BattleSkill { Id = 301 });
            repaired.Participants[0].Statuses.Add(new BattleStatus { Id = 3 });
            repaired.Items.Add(new BattleItem { Id = 10 });
            Assert.That(repaired.TreasureUses.Single().Count, Is.EqualTo(2));
            Assert.That(scenario.Participants, Has.Count.EqualTo(2), "Repairing a deserialized draft must not mutate the source.");
            string stable = BattleScenarioJson.Serialize(repaired);
            Assert.That(BattleScenarioJson.Serialize(BattleScenarioEditing.Repair(repaired)), Is.EqualTo(stable));
        }

        [TestCase(typeof(byte), -1, 0L)]
        [TestCase(typeof(byte), 300, 255L)]
        [TestCase(typeof(sbyte), -17, -17L)]
        [TestCase(typeof(short), -17, -17L)]
        [TestCase(typeof(ushort), -1, 0L)]
        [TestCase(typeof(uint), -1, 0L)]
        [TestCase(typeof(uint), long.MaxValue, 4294967295L)]
        [TestCase(typeof(int), -17, -17L)]
        [TestCase(typeof(int), long.MaxValue, 2147483647L)]
        [TestCase(typeof(long), -17, -17L)]
        public void TypedIntegerEditsRetainTheirClrType(Type type, long input, long expected)
        {
            MethodInfo convert = typeof(BattleScenarioTypedFields).GetMethod("IntegerValue", BindingFlags.Static | BindingFlags.NonPublic);
            object value = convert.Invoke(null, new object[] { type, input });
            Assert.That(value.GetType(), Is.EqualTo(type), "Reflection assignment must receive the exact field type.");
            Assert.That(Convert.ToInt64(value), Is.EqualTo(expected));
        }
    }
}
