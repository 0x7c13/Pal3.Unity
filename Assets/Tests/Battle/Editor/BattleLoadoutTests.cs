// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Linq;
    using Battle;
    using NUnit.Framework;

    public sealed class BattleLoadoutTests
    {
        [TestCase("PAL3", 101, 0, 6603)]
        [TestCase("PAL3", 104, 1, 6603)]
        [TestCase("PAL3", 106, 2, 6603)]
        [TestCase("PAL3", 107, 3, 6603)]
        [TestCase("PAL3", 102, 4, 6603)]
        [TestCase("PAL3A", 101, 0, 82110)]
        [TestCase("PAL3A", 102, 1, 82209)]
        [TestCase("PAL3A", 103, 2, 82310)]
        [TestCase("PAL3A", 104, 3, 82402)]
        [TestCase("PAL3A", 105, 4, 82508)]
        public void MissingWeaponUsesTheCompletePublishedInitializationResult(string variant, int actorId, int identity, int weapon)
        {
            // Complete InitRoleState probes, not inferred weapon suitability:
            // PAL3 0x4ed8c0 / PAL3A 0x4df83f.
            var draft = new BattleParticipant { ActorId = actorId };
            BattleParticipant prepared = BattleLoadoutRules.Prepare(variant, draft);
            Assert.That(BattleLoadoutRules.PlayerIdentity(variant, actorId), Is.EqualTo(identity));
            Assert.That(prepared.Equipment[0], Is.EqualTo(weapon));
            Assert.That(prepared.UsesNativeDefaultWeapon, Is.True);
            Assert.That(draft.Equipment[0], Is.Zero);
            Assert.That(draft.UsesNativeDefaultWeapon, Is.False);
            Assert.That(prepared.MagicSword != null, Is.EqualTo(variant == "PAL3" && identity == 0));
            if (prepared.MagicSword != null)
            {
                Assert.That((prepared.MagicSword.AttackBonus, prepared.MagicSword.SelectedElement, prepared.MagicSword.SkillId,
                    prepared.MagicSword.ChargeCoefficient, prepared.MagicSword.Energy),
                    Is.EqualTo((35, BattleSwordPurificationElement.Wind, 2224, 10u, 0f)));
            }
            BattleParticipant again = BattleLoadoutRules.Prepare(variant, prepared);
            Assert.That(again.Equipment, Is.EqualTo(prepared.Equipment));
            again.Equipment[0] = 1;
            Assert.That(prepared.Equipment[0], Is.EqualTo(weapon));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void PreparationKeepsExplicitEquipmentAndDoesNotAssignMonsterWeapons(string variant)
        {
            var explicitActor = new BattleParticipant { ActorId = 101 };
            explicitActor.Equipment[0] = 123;
            Assert.That(BattleLoadoutRules.Prepare(variant, explicitActor).Equipment[0], Is.EqualTo(123));
            Assert.That(BattleLoadoutRules.Prepare(variant, explicitActor).UsesNativeDefaultWeapon, Is.False);
            var monster = new BattleParticipant { ActorId = 701, Side = BattleSide.Enemy };
            Assert.That(BattleLoadoutRules.Prepare(variant, monster).Equipment[0], Is.Zero);
            Assert.That(BattleLoadoutRules.Prepare(variant, monster).UsesNativeDefaultWeapon, Is.False);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NativeDefaultProvenanceCannotAuthorizeAnotherWeapon(string variant)
        {
            BattleParticipant prepared = BattleLoadoutRules.Prepare(variant, new BattleParticipant { ActorId = 101 });
            prepared.Equipment[0]++;
            var scenario = new BattleScenario { Variant = variant, Scene = "synthetic", Participants = { prepared } };
            Assert.That(scenario.Validate().Any(error => error.Contains("provenance")), Is.True);
            Assert.That(BattleLoadoutRules.IsNativeDefaultWeapon(variant, prepared), Is.False);
            Assert.That(BattleLoadoutRules.IsNativeDefaultWeapon("unknown", prepared), Is.False);
        }

        [TestCase(104, 1)]
        [TestCase(106, 2)]
        [TestCase(107, 3)]
        [TestCase(102, 4)]
        public void DefaultSwordStatsDoNotRequireOrCreateAnotherCharactersCombatSwordState(int actorId, int identity)
        {
            BattleParticipant prepared = BattleLoadoutRules.Prepare("PAL3", new BattleParticipant
            {
                ActorId = actorId,
                InstanceId = "P",
                AttributeSource = BattleAttributeSource.BaseWithEquipment,
                Attributes = new[] { 100, 20, 30, 40, 50, 6, 7, 8, 9, 10, 11, 12 }
            });
            var definition = new BattleActorDefinition(new BattleActorData
            {
                InstanceId = "P",
                Identity = identity,
                Equipment = new[] { new BattleEquipmentDefinition(new BattleEquipmentData { Id = 6603, MagicSwordCultivation = new BattleMagicSword().Capture() }) }
            });
            int[] attributes = BattleAttributeCalculator.Calculate(prepared, definition, "PAL3");
            Assert.That(attributes[3], Is.EqualTo(75));
            Assert.That(attributes[BattleMagicSwordRules.SelectedAttribute(BattleSwordPurificationElement.Wind)],
                Is.EqualTo(prepared.Attributes[BattleMagicSwordRules.SelectedAttribute(BattleSwordPurificationElement.Wind)] + 1));
            Assert.That(prepared.MagicSword, Is.Null);
            Assert.That(definition.MagicSword, Is.Null);
            prepared.UsesNativeDefaultWeapon = false;
            Assert.Throws<NotSupportedException>(() => BattleAttributeCalculator.Calculate(prepared, definition, "PAL3"));
        }
    }
}
