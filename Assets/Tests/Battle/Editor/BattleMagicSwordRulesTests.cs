// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using Battle;
    using NUnit.Framework;

    public sealed class BattleMagicSwordRulesTests
    {
        private static BattleMagicSwordSnapshot Snapshot(float energy = 0, uint coefficient = 10, int skill = 2224, int purifications = 0) =>
            new(35, BattleSwordPurificationElement.Water, purifications, skill, coefficient, energy);

        [TestCase(BattleSwordPurificationElement.Water, 7)]
        [TestCase(BattleSwordPurificationElement.Fire, 8)]
        [TestCase(BattleSwordPurificationElement.Thunder, 10)]
        [TestCase(BattleSwordPurificationElement.Wind, 9)]
        [TestCase(BattleSwordPurificationElement.Earth, 11)]
        [TestCase(BattleSwordPurificationElement.None, -1)]
        public void EquipmentUsesSelectedPurificationElement(BattleSwordPurificationElement element, int attribute)
        {
            var snapshot = new BattleMagicSwordSnapshot(101, element, 0, 2224, 10, 0);
            for (int index = 0; index < 12; index++)
                Assert.That(BattleMagicSwordRules.EquipmentAttributeBonus(snapshot, index), Is.EqualTo(index == 3 ? 101 : index == attribute ? 1 : 0));
        }

        [TestCase(0f, 0u, 1f, 1u)]
        [TestCase(190f, 20u, 200f, 20u)]
        [TestCase(10f, uint.MaxValue, 200f, uint.MaxValue)]
        [TestCase(199.9f, 1u, 200f, 1u)]
        public void PhysicalWorkChargesAndRepairsZeroCoefficient(float energy, uint coefficient, float expected, uint effectiveCoefficient)
        {
            BattleMagicSwordSnapshot original = Snapshot(energy, coefficient);
            BattleMagicSwordSnapshot charged = BattleMagicSwordRules.GainOnPhysicalWork(original, 0, true);
            Assert.That(charged.Energy, Is.EqualTo(expected));
            Assert.That(charged.ChargeCoefficient, Is.EqualTo(effectiveCoefficient));
            Assert.That(original.Energy, Is.EqualTo(energy));
            Assert.That(original.ChargeCoefficient, Is.EqualTo(coefficient));
        }

        [TestCase(1, true, 2224)]
        [TestCase(0, false, 2224)]
        [TestCase(0, true, 0)]
        public void InactiveSwordDoesNotChargeOrRepairCoefficient(int identity, bool equipped, int skill)
        {
            BattleMagicSwordSnapshot original = Snapshot(coefficient: 0, skill: skill);
            Assert.That(BattleMagicSwordRules.GainOnPhysicalWork(original, identity, equipped), Is.SameAs(original));
        }

        [TestCase(199f, false)]
        [TestCase(199.9f, false)]
        [TestCase(199.90000915527344f, true)]
        [TestCase(200f, true)]
        public void ReadinessUsesStrictNativeFloatThreshold(float energy, bool expected)
        {
            Assert.That(BattleMagicSwordRules.IsReady(Snapshot(energy), 0, true), Is.EqualTo(expected));
        }

        [TestCase(0, -101)]
        [TestCase(1, -151)]
        [TestCase(2, -202)]
        [TestCase(3, -252)]
        [TestCase(20, -1111)]
        public void ResidualSwordScalesByTotalPurificationAndTruncates(int count, int expected)
        {
            var snapshot = Snapshot(purifications: count);
            Assert.That(BattleMagicSwordRules.SkillPower(snapshot, 2224, -101, BattleSide.Enemy, out BattleValueMode mode), Is.EqualTo(expected));
            Assert.That(mode, Is.EqualTo(BattleValueMode.Absolute));
        }

        [TestCase(2201)]
        [TestCase(2218)]
        [TestCase(2221)]
        [TestCase(2223)]
        public void OtherWeaponSkillsKeepRawPowerAndForceAllyPercentage(int id)
        {
            Assert.That(BattleMagicSwordRules.SkillPower(Snapshot(purifications: 20), id, 101, BattleSide.Ally, out BattleValueMode mode), Is.EqualTo(101));
            Assert.That(mode, Is.EqualTo(BattleValueMode.Percentage));
        }

        [Test]
        public void StartingSkillConsumesEnergyWithoutChangingCultivation()
        {
            BattleMagicSwordSnapshot original = Snapshot(200, 0, 2223, 9);
            BattleMagicSwordSnapshot consumed = BattleMagicSwordRules.ConsumeEnergy(original);
            Assert.That(consumed.Energy, Is.Zero);
            Assert.That(consumed.ChargeCoefficient, Is.Zero);
            Assert.That(consumed.SkillId, Is.EqualTo(2223));
            Assert.That(consumed.PurificationCount, Is.EqualTo(9));
            Assert.That(original.Energy, Is.EqualTo(200));
        }

        [TestCase(-1f)]
        [TestCase(201f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidEnergyIsRejected(float energy)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Snapshot(energy));
        }

        [TestCase(0)]
        [TestCase(2200)]
        [TestCase(2225)]
        public void UnknownWeaponSkillIsExplicitlyUnsupported(int id)
        {
            Assert.Throws<NotSupportedException>(() => BattleMagicSwordRules.SkillPower(Snapshot(), id, 100, BattleSide.Enemy, out _));
        }
    }
}
