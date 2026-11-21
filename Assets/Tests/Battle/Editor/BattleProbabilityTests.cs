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

    public sealed class BattleProbabilityTests
    {
        private sealed class AiPort : BattleAiPort
        {
            public override long TryAttempt(BattleAiAttempt attempt) => -1;
        }

        private sealed class Catalog : IBattleCatalog
        {
            public AttackTiming Attack(string source, string target) => new(0, 2, new[] { 1 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => 0;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class ReadyRules : IBattleRules
        {
            private readonly BattleBasicRules _original = new();

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => actor.Side == BattleSide.Enemy ? BattleSession.FullGauge : 0;

            public int GaugePerStep(BattleParticipant actor) => 1;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => _original.Hit(source, target, guarding, random, targetAtHome, damageDivisor);
        }

        // Independent oracle: released PAL3 inline instructions and PAL3A helper
        // 0x43268c, executed by Battle-Stage17-ProbabilityReview/probe-probability.py.
        [TestCase(1, 2940990853u, true, 594484u)]
        [TestCase(1, 3768644997u, false, 660020u)]
        [TestCase(4, 2828465541u, true, 2626100u)]
        [TestCase(4, 3656119685u, false, 2691636u)]
        [TestCase(37, 3402691973u, true, 24777268u)]
        [TestCase(37, 4230346117u, false, 24842804u)]
        [TestCase(50, 1811543429u, true, 33493556u)]
        [TestCase(50, 2639197573u, false, 33559092u)]
        [TestCase(99, 682096005u, true, 66392628u)]
        [TestCase(99, 1509750149u, false, 66458164u)]
        public void NativeThresholdUses1024ResiduesAndStrictComparison(int percent, uint seed, bool expected, uint finalSeed)
        {
            foreach (string variant in new[] { "PAL3", "PAL3A" })
            {
                var random = new BattleRandom(unchecked((int)seed), variant);
                Assert.That(random.Probability(percent), Is.EqualTo(expected), variant);
                Assert.That(random.State, Is.EqualTo(finalSeed), variant);
            }
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NonpositiveProbabilityDoesNotConsumeTheStream(string variant)
        {
            var random = new BattleRandom(123, variant);
            Assert.That(random.Probability(-10), Is.False);
            Assert.That(random.Probability(0), Is.False);
            Assert.That(random.State, Is.EqualTo(123u));
        }

        [TestCase("PAL3", 100, 28854610u)]
        [TestCase("PAL3", 101, 28854610u)]
        [TestCase("PAL3", 255, 28854610u)]
        [TestCase("PAL3A", 100, 123u)]
        [TestCase("PAL3A", 101, 123u)]
        [TestCase("PAL3A", 255, 123u)]
        public void CertainSuccessConsumesOneDrawOnlyInPal3(string variant, int percent, uint finalSeed)
        {
            var random = new BattleRandom(123, variant);
            Assert.That(random.Probability(percent), Is.True);
            Assert.That(random.State, Is.EqualTo(finalSeed));
        }

        [TestCase("PAL3", false, 3396176589u)]
        [TestCase("PAL3A", true, 28854610u)]
        public void CertainSuccessPreservesTheOriginalFollowingRoll(string variant, bool followingResult, uint finalSeed)
        {
            var random = new BattleRandom(123, variant);
            Assert.That(random.Probability(100), Is.True);
            Assert.That(random.Probability(50), Is.EqualTo(followingResult));
            Assert.That(random.State, Is.EqualTo(finalSeed));
        }

        [TestCase("PAL3", 1512272476u)]
        [TestCase("PAL3A", 3396176589u)]
        public void HighTechniqueSuccessRetainsItsWarpDrawsBeforeTheProbabilityGate(string variant, uint finalSeed)
        {
            var random = new BattleRandom(123, variant);
            int percent = new BattleEffectRules(variant).TechniqueSuccessPercent(13, 10, 0, random);
            Assert.That(percent, Is.EqualTo(110));
            Assert.That(random.State, Is.EqualTo(3396176589u));
            Assert.That(random.Probability(percent), Is.True);
            Assert.That(random.State, Is.EqualTo(finalSeed));
        }

        [TestCase("PAL3", 28854610u)]
        [TestCase("PAL3A", 123u)]
        public void SingleOverfullAiCandidateUsesTheNativeCertainSuccessGate(string variant, uint finalSeed)
        {
            var random = new BattleRandom(123, variant);
            var ai = new BattleAi(variant == "PAL3A", random, new AiPort());
            // HP above maximum deliberately bypasses the ordinary lowest-ratio
            // selection and reaches the published 100/candidateCount fallback.
            ai.Roles[5] = new BattleAiRole { Valid = true, Alive = true, Hp = 200, MaxHp = 100 };
            Assert.That(ai.SelectPlayerNormalTarget(0), Is.EqualTo(5));
            Assert.That(random.State, Is.EqualTo(finalSeed));
        }

        [TestCase(true, "PAL3")]
        [TestCase(false, "PAL3A")]
        public void AiRejectsAMismatchedProbabilityVariant(bool pal3A, string randomVariant)
        {
            Assert.Throws<ArgumentException>(() => new BattleAi(pal3A, new BattleRandom(123, randomVariant), new AiPort()));
        }

        [Test]
        public void RandomDefaultsToPal3AndRejectsUnknownVariants()
        {
            Assert.That(new BattleRandom(123).Variant, Is.EqualTo("PAL3"));
            Assert.Throws<ArgumentException>(() => new BattleRandom(123, "other"));
        }

        private static BattleParticipant Participant(string id, BattleSide side) => new()
        {
            InstanceId = id,
            ActorId = side == BattleSide.Ally ? 101 : 701,
            Side = side,
            Level = 1,
            Attributes = new[] { 100, 100, 100, 60, 10, side == BattleSide.Ally ? 23 : 3, 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { 100, 100, 100 }
        };

        [TestCase("PAL3", false, 1512272476u)]
        [TestCase("PAL3A", false, 3396176589u)]
        [TestCase("PAL3", true, 1512272476u)]
        [TestCase("PAL3A", true, 3396176589u)]
        public void SessionHighDodgeUsesItsVariantInBasicAndCapturedRules(string variant, bool capturedRules, uint finalSeed)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "portable", DataFingerprint = "probability-fixture", Seed = 123 };
            scenario.Participants.Add(Participant("A", BattleSide.Ally));
            scenario.Participants.Add(Participant("E", BattleSide.Enemy));
            BattleDefinitions definitions = capturedRules ? new BattleDefinitions(variant, scenario.DataFingerprint,
                Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Ally ? 0 : -1
                }))) : null;
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new ReadyRules(), definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            session.SetAutomatic(BattleSide.Ally, false);
            Assert.That(session.Submit("E", BattleAction.Attack, "A"), Is.True);
            for (int i = 0; i < 20 && !session.Events.Any(entry => entry.Kind == BattleEventKind.Miss); i++)
                session.Step();
            Assert.That(session.Status, Is.Not.EqualTo(BattleSessionStatus.Faulted), session.Result?.Reason);
            BattleEvent miss = session.Events.Single(entry => entry.Kind == BattleEventKind.Miss);
            Assert.That(miss.Dodged, Is.True);
            Assert.That(miss.RandomState, Is.EqualTo(finalSeed));
            Assert.That(session.Actors.Single(actor => actor.Id == "A").Hp, Is.EqualTo(100));
        }
    }
}
