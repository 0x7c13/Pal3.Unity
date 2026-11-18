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
    using Battle.Presentation;
    using Newtonsoft.Json;
    using NUnit.Framework;

    public sealed class BattleHudDenialTextTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings
        {
            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => new(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => 0;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class Rules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => BattleSession.FullGauge;

            public int GaugePerStep(BattleParticipant actor) => 1;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => new BattleBasicRules().Hit(source, target, guarding, random, targetAtHome, damageDivisor);
        }

        private static BattleSession Session(string variant, int mp = 9)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "denial", DataFingerprint = "denial" };
            foreach ((string id, BattleSide side) in new[] { ("A", BattleSide.Ally), ("E", BattleSide.Enemy) })
            {
                var actor = new BattleParticipant
                {
                    InstanceId = id,
                    ActorId = side == BattleSide.Ally ? 101 : 701,
                    Side = side,
                    Slot = BattleSlot.Water,
                    Attributes = new[] { 100, 100, 100, 20, 10, 3, 0, 0, 0, 0, 0, 0 },
                    Vitals = new[] { 100, 100, mp }
                };
                actor.Skills.Add(new BattleSkill { Id = 301, Level = 4, Mastered = true });
                scenario.Participants.Add(actor);
            }
            var skill = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 301,
                Name = "水术",
                IsMagic = true,
                Range = BattleTargetRange.OpponentSingle,
                ApplicableIdentities = new[] { 0 },
                Costs = new[] { new BattleCost(2, BattleValueMode.Absolute, 10) }
            });
            var definitions = new BattleDefinitions(variant, "denial", new[] { skill }, Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Ally ? 0 : -1,
                    Equipment = new[] { new BattleEquipmentDefinition(new BattleEquipmentData { Id = 80001, Slot = 1, MpSaving = 50 }) }
                })));
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new Rules(), definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            session.Step();
            return session;
        }

        private static string State(BattleSession session)
        {
            object random = typeof(BattleSession).GetField("_random", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(session);
            return JsonConvert.SerializeObject(new
            {
                session.Tick,
                session.SimulationTick,
                session.Events,
                session.Inputs,
                session.Actors,
                session.Inventory,
                session.Money,
                session.Pal3AState,
                session.StageTasks,
                RandomState = random.GetType().GetProperty("State").GetValue(random)
            });
        }

        [TestCase(0, "精")]
        [TestCase(1, "气")]
        [TestCase(2, "神")]
        [TestCase(10, "金钱")]
        [TestCase(16, "蛊")]
        public void ResourceNamesAndAmountsComeFromTypedEstimatesRatherThanDiagnosticFields(int resource, string name)
        {
            var costs = new[] { new BattleCostEstimate(resource, 80, 20, 30) };
            string brief = BattleHudDenialText.Format("Resource 999: need 999, available 999.", costs);
            Assert.That(brief, Is.EqualTo(name + "不足：需80，现30"));
            Assert.That(brief.Sum(character => character < 0x80 ? 1 : 2), Is.LessThanOrEqualTo(20));
            Assert.That(BattleHudDenialText.Format("Resource 999: need 999, available 999.", costs, detailed: true),
                Is.EqualTo(name + "不足：需要 80，当前 30。"));
        }

        [Test]
        public void ResourceDenialUsesTheFirstInsufficientRequirementInDomainOrder()
        {
            var costs = new[]
            {
                new BattleCostEstimate(0, 3, 3, 50),
                new BattleCostEstimate(2, 80, 20, 30),
                new BattleCostEstimate(10, 100, 100, 0)
            };
            Assert.That(BattleHudDenialText.Format("Resource 2: need 80, available 30.", costs), Is.EqualTo("神不足：需80，现30"));
            Assert.That(BattleHudDenialText.Format("This actor has not mastered the skill.", costs), Is.EqualTo("尚未学会此技能"));
            Assert.That(BattleHudDenialText.Format(null, costs), Is.Null);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AbilityDenialKeepsTheOriginalGateAndLeavesSessionAndRandomStateUntouched(string variant)
        {
            BattleSession session = Session(variant);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            BattleAbilityDefinition skill = session.Definitions.Skills[301];
            BattleCostEstimate estimate = session.GetCostEstimate(new BattleCommand("A", BattleAction.Skill, abilityId: 301)).Single();
            Assert.That((estimate.Required, estimate.Payable, estimate.Available), Is.EqualTo((10, 5, 9)));
            string diagnostic = presenter.Reason(BattleAction.Skill, 301);
            Assert.That(diagnostic, Is.EqualTo("Resource 2: need 10, available 9."));
            string before = State(session);
            for (int index = 0; index < 20; index++)
                Assert.That(BattleHudDenialText.ForAbility(session, presenter, skill), Is.EqualTo("神不足：需10，现9"));
            Assert.That(BattleHudDenialText.ForAbility(session, presenter, skill, detailed: true), Is.EqualTo("神不足：需要 10，当前 9。"));
            Assert.That(presenter.Reason(BattleAction.Skill, 301), Is.EqualTo(diagnostic));
            Assert.That(State(session), Is.EqualTo(before));
            Assert.That(presenter.Message, Is.Null);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void UsableAbilitiesDoNotReplaceTheirOriginalDescriptionWithADenial(string variant)
        {
            BattleSession session = Session(variant, 100);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            Assert.That(BattleHudDenialText.ForAbility(session, presenter, session.Definitions.Skills[301]), Is.Null);
            Assert.That(BattleHudDenialText.ForAbility(session, presenter, session.Definitions.Skills[301], detailed: true), Is.Null);
        }

        [TestCase("Actor is not ready.", "等待角色就绪。")]
        [TestCase("There is no legal target.", "没有可选目标。")]
        [TestCase("No item remains in this battle inventory.", "此物品已用完或已被其他指令占用。")]
        [TestCase("封期间不能施放仙术。", "封期间不能施放仙术。")]
        [TestCase("乱／狂期间由原版异常状态 AI 接管。", "乱／狂期间不能手动下达指令。")]
        [TestCase("The magic sword energy must be greater than 199.9.", "魔剑能量不足。")]
        [TestCase("朱仙变 requires full SP before equipment savings.", "气满后才能使用朱仙变。")]
        [TestCase("This spirit wheel element needs nine charges.", "此系五灵轮需充能九次。")]
        [TestCase("The ability has no verified action timeline.", "此技能暂时无法使用。")]
        public void CommonFailuresUsePlayerFacingChinese(string diagnostic, string expected) =>
            Assert.That(BattleHudDenialText.Format(diagnostic, detailed: true), Is.EqualTo(expected));

        [TestCase("")]
        [TestCase("Missing EffectLinker group 12345 in /local/assets.")]
        [TestCase("角色处于Paralysis，不能换位。")]
        [TestCase("未核实成员资源12345的原版身份映射。")]
        public void UnknownDiagnosticsNeverEnterThePlayerPanel(string diagnostic)
        {
            Assert.That(BattleHudDenialText.Format(diagnostic), Is.EqualTo("当前无法使用"));
            Assert.That(BattleHudDenialText.Format(diagnostic, detailed: true), Is.EqualTo("当前无法使用。"));
        }

        [TestCase(0, "精")]
        [TestCase(1, "气")]
        [TestCase(2, "神")]
        [TestCase(10, "金钱")]
        [TestCase(16, "蛊")]
        public void LargeAmountsUseAShortLabelAndRetainCompleteTooltipValues(int resource, string name)
        {
            var costs = new[] { new BattleCostEstimate(resource, int.MaxValue, 1, int.MaxValue - 1) };
            const string diagnostic = "Resource 999: need 999, available 999.";
            Assert.That(BattleHudDenialText.Format(diagnostic, costs), Is.EqualTo(name + "不足"));
            Assert.That(BattleHudDenialText.Format(diagnostic, costs, detailed: true),
                Is.EqualTo(name + "不足：需要 2147483647，当前 2147483646。"));
        }

        [TestCase("No item remains in this battle inventory.", "此物品无可用数量")]
        [TestCase("乱／狂期间由原版异常状态 AI 接管。", "乱／狂期间自动行动")]
        [TestCase("The original fire beast form permits only physical attack, defense, or escape.", "炎兽仅可攻、防、逃")]
        public void BriefReasonsFitTheNarrowDescriptionBand(string diagnostic, string expected)
        {
            string brief = BattleHudDenialText.Format(diagnostic);
            Assert.That(brief, Is.EqualTo(expected));
            Assert.That(brief.Sum(character => character < 0x80 ? 1 : 2), Is.LessThanOrEqualTo(20));
        }

        [Test]
        public void UnknownOrUnavailableResourceContextNeverLeaksRawResourceNumbers()
        {
            Assert.That(BattleHudDenialText.Format("Resource 999: need 10, available 0.", new[] { new BattleCostEstimate(999, 10, 10, 0) }),
                Is.EqualTo("所需资源不足"));
            Assert.That(BattleHudDenialText.Format("Resource 2: need 80, available 30."), Is.EqualTo("所需资源不足"));
        }
    }
}
