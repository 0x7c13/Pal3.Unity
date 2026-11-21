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
    using NUnit.Framework;
    using Pal3.Battle;

    public sealed class BattleReviewBoundaryTests
    {
        private sealed class Catalog : IBattleCatalog, IBattlePal3AFormCatalog
        {
            public AttackTiming Attack(string source, string target) => new(0, 3, new[] { 1 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "P" ? 2 : 0;

            public int DamageDivisor(string actor) => 1;

            public BattlePhysicalAnimation FormChangeAnimation(string actorId, bool transformed) => new("z20", 2, 1, gameBoxDuration: 192);

            public void ActivatePal3AForm(string actorId, BattlePal3AFormKind kind)
            {
            }
        }

        private static object Actor(BattleSession session, string id) =>
            ((IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session))[id];

        private static object Invoke(BattleSession session, string name, params object[] arguments) =>
            typeof(BattleSession).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, arguments);

        private static BattleSession Forms(bool transformed = false)
        {
            var scenario = new BattleScenario { Variant = "PAL3A", Scene = "fixture", DataFingerprint = "fixture", GameOverOnDefeat = false };
            scenario.Pal3A.NangongStartsTransformed = scenario.Pal3A.PengxuStartsTransformed = transformed;
            scenario.Participants.AddRange(new[]
            {
                new BattleParticipant { InstanceId = "A", ActorId = 101, Side = BattleSide.Ally, Attributes = new[] { 1000, 100, 200, 100, 50, 5, 10, 0, 0, 0, 0, 0 }, Vitals = new[] { 777, 100, 200 } },
                new BattleParticipant { InstanceId = "P", ActorId = 103, Side = BattleSide.Ally, Slot = BattleSlot.Fire, Attributes = new[] { 2000, 100, 500, 100, 101, 5, 10, 0, 0, 0, 0, 0 }, Vitals = new[] { 1000, 100, 500 } },
                new BattleParticipant { InstanceId = "E", ActorId = 701, Side = BattleSide.Enemy, Attributes = new[] { 99999, 100, 500, 100, 30, 3, 0, 0, 0, 0, 0, 0 }, Vitals = new[] { 99999, 100, 500 } }
            });
            var definitions = new BattleDefinitions("PAL3A", "fixture", Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(), new[]
            {
                new BattleActorDefinition(new BattleActorData { InstanceId = "A", Identity = 0 }),
                new BattleActorDefinition(new BattleActorData { InstanceId = "P", Identity = 2 }),
                new BattleActorDefinition(new BattleActorData { InstanceId = "E" })
            });
            return new BattleSession(new BattleSetup(scenario), new Catalog(), definitions: definitions);
        }

        [TestCase("PAL3", int.MaxValue, 2f, 2147483606, 3357800067u)]
        [TestCase("PAL3A", int.MaxValue, 2f, 2147483606, 3357800067u)]
        [TestCase("PAL3", int.MinValue, 1f, 2147483607, 3357800067u)]
        [TestCase("PAL3A", int.MinValue, 1f, 2147483607, 3357800067u)]
        [TestCase("PAL3", 1, float.PositiveInfinity, -40, 3357800067u)]
        [TestCase("PAL3A", 1, float.PositiveInfinity, -40, 3357800067u)]
        [TestCase("PAL3", 100, .1f, 99, 3357800067u)]
        [TestCase("PAL3A", 100, .1f, 99, 3357800067u)]
        [TestCase("PAL3", 0, .1f, 0, 1u)]
        [TestCase("PAL3A", 0, .1f, 0, 1u)]
        [TestCase("PAL3", int.MaxValue, 0f, int.MaxValue, 1u)]
        [TestCase("PAL3A", int.MaxValue, 0f, int.MaxValue, 1u)]
        public void WarpSignedWidthMatchesNativeInt32AndRandomStream(string variant, int value, float fraction, int expected, uint seed)
        {
            var random = new BattleRandom(1, variant);
            Assert.That(random.Warp(value, fraction), Is.EqualTo(expected));
            Assert.That(random.State, Is.EqualTo(seed));
        }

        private sealed class AiPort : BattleAiPort
        {
            public override int SceneFiveSpiritMode => 1;

            public override bool CanCast(int source, int skillId) => true;

            public override long TryAttempt(BattleAiAttempt attempt) => -1;
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3A", true)]
        public void AiSelectionKeepsNativeSignedDistanceAtIntMinWeight(string variant, bool attack)
        {
            var random = new BattleRandom(1, variant);
            var ai = new BattleAi(variant == "PAL3A", random, new AiPort());
            ai.Roles[5].Valid = ai.Roles[5].Alive = true;
            ai.Roles[5].Skills = new[] { 10, 40, 100, 400 }.Select((power, index) => new BattleAiSkill
            {
                Id = 9001 + index,
                Type = attack ? 2 : 3,
                Range = attack ? 3 : 1,
                Power = attack ? -power : power
            }).ToArray();
            Assert.That(ai.SelectMagic(5, attack, false, int.MinValue), Is.EqualTo(9004));
            foreach (BattleAiSkill skill in ai.Roles[5].Skills)
            {
                skill.IsMagic = false;
                skill.Type = 0;
            }
            Assert.That(ai.SelectSupport(5, attack ? BattleAiAttemptKind.AttackSkill : BattleAiAttemptKind.RecoverSkill, false, int.MinValue), Is.EqualTo(9004));
            Assert.That(random.State, Is.EqualTo(1u));
        }

        [Test]
        public void FiveSpiritSelectionKeepsNativeSignedDistanceAtIntMinWeight()
        {
            var ai = new BattleAi(true, new BattleRandom(1, "PAL3A"), new AiPort());
            ai.Roles[0].Valid = ai.Roles[0].Alive = true;
            ai.Roles[0].Skills = new[]
            {
                new BattleAiSkill { Id = 9001, Type = 2, Range = 4, Power = -10, ElementMask = 1 },
                new BattleAiSkill { Id = 265, Type = 2, Range = 4, Power = -40, ElementMask = 2 },
                new BattleAiSkill { Id = 264, Type = 2, Range = 4, Power = -100, ElementMask = 4 },
                new BattleAiSkill { Id = 9004, Type = 2, Range = 4, Power = -400, ElementMask = 8 }
            };
            Assert.That(ai.SelectFiveSpiritMagic(0, true, int.MinValue), Is.EqualTo(9001));
        }

        [Test]
        public void UndisplayedMpReturnRetainsNativeSignedAbsAtIntMin()
        {
            BattleSession session = Forms();
            int returned = (int)Invoke(session, "ChangeResource", Actor(session, "A"), 2, int.MinValue, null, 0L, "native return boundary", false);
            Assert.That(returned, Is.EqualTo(int.MinValue));
            Assert.That(BattleSpecialRules.AbsorbedMp(returned), Is.EqualTo(int.MinValue));
        }

        [Test]
        public void LiveFormationActorsRejectTransformedInitiatorsAndAuxiliaryMembers()
        {
            var requirement = new BattleComboRequirement(1, 0, new[] { 101, 103, 0, 0 }, new int[4], new int[4], new[] { -1, -1, -1 });
            var rules = new BattleFormationRules("PAL3A");
            BattleSession session = Forms();
            BattleFormationActor[] Formation() => new[] { "A", "P" }.Select(id => (BattleFormationActor)Invoke(session, "FormationActor", Actor(session, id))).ToArray();
            Assert.That(rules.CanCoAttack(0, 0, requirement, Formation()).Allowed, Is.True);
            Invoke(session, "SwapPal3AForm", Actor(session, "A"), true);
            Assert.That(rules.CanCoAttack(0, 0, requirement, Formation()).Allowed, Is.False);
            Invoke(session, "SwapPal3AForm", Actor(session, "A"), false);
            Invoke(session, "PreparePal3APengxuStats", Actor(session, "P"));
            Invoke(session, "SwapPal3AForm", Actor(session, "P"), true);
            Assert.That(rules.CanCoAttack(0, 0, requirement, Formation()).Allowed, Is.False);
        }

        [TestCase(BattleSessionStatus.Aborted, null, false)]
        [TestCase(BattleSessionStatus.Faulted, null, false)]
        [TestCase(BattleSessionStatus.Completed, BattleSide.Enemy, false)]
        [TestCase(BattleSessionStatus.Completed, BattleSide.Ally, true)]
        public void StoppedFormsReturnHumanAttributesBeforeResultCapture(BattleSessionStatus status, BattleSide? winner, bool escaped)
        {
            BattleSession session = Forms(true);
            typeof(BattleSession).GetField("_partyEscaped", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, escaped);
            Invoke(session, "End", status, winner, "boundary fixture");
            Assert.That(session.Status, Is.EqualTo(status), session.Result.Reason);
            BattleActorSnapshot pengxu = session.Result.Actors.Single(actor => actor.Id == "P");
            Assert.That((pengxu.Hp, pengxu.MaxHp, pengxu.Attributes[4], pengxu.Attributes[6]), Is.EqualTo((1000, 2000, 101, 10)));
            Assert.That(session.Result.Actors.Single(actor => actor.Id == "A").Attributes[6], Is.EqualTo(10));
            Assert.That(session.IsPal3ATransformed("A") || session.IsPal3ATransformed("P"), Is.False);
            Assert.That(session.Pal3AForm("P").Pending, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StopBeforePengxuModelSwapRestoresQueuedStatsAndPreservesDeath(bool dead)
        {
            BattleSession session = Forms();
            object actor = Actor(session, "P");
            Invoke(session, "QueuePal3AFormChange", actor, true, false);
            Assert.That(session.IsPal3ATransformed("P"), Is.False);
            Assert.That(session.Actors.Single(value => value.Id == "P").MaxHp, Is.EqualTo(400));
            if (dead)
                Invoke(session, "ChangeHp", actor, -1000, null, 0L, "pre-swap damage", false, false, false);
            session.Abort();
            BattleActorSnapshot result = session.Result.Actors.Single(value => value.Id == "P");
            Assert.That((result.Hp, result.MaxHp, result.Attributes[4], result.Attributes[6]), Is.EqualTo((dead ? 0 : 1000, 2000, 101, 10)));
            Assert.That(session.Pal3AForm("P").Pending, Is.False);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void MustFailSessionSurvivesEarthPoisonBeyondSignedInt32Damage(string variant)
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Variant = variant;
            scenario.MustFail = true;
            scenario.DataFingerprint = "poison-boundary";
            scenario.Encounter = BattleEncounter.Normal;
            scenario.Participants[1].Slot = BattleSlot.Center;
            scenario.Participants[1].Attributes[0] = scenario.Participants[1].Vitals[0] = 1000000;
            scenario.Participants[1].Statuses.Add(new BattleStatus { Id = (int)BattleState.PoisonEarth, Rounds = short.MaxValue });
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint, Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(), scenario.Participants.Select(actor =>
                new BattleActorDefinition(new BattleActorData { InstanceId = actor.InstanceId })));
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), definitions: definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            object enemy = Actor(session, "Enemy");
            Type type = enemy.GetType();
            for (int round = 1; round <= 50; round++)
            {
                type.GetField("Rounds").SetValue(enemy, round);
                session.Step();
                Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running), session.Result?.Reason);
            }
            Assert.That(session.Actors.Single(actor => actor.Id == "Enemy").Hp, Is.EqualTo(1000000));
            Assert.That(session.Events.Count(entry => entry.Target == "Enemy" && entry.Detail == "poison PoisonEarth" && entry.RequestedDelta == int.MinValue), Is.EqualTo(21));
        }

        [TestCase(0)]
        [TestCase(2)]
        public void CapturedLiaoRiLossesRejectAttributeOverflowBeforeCombatStarts(int resource)
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Variant = "PAL3A";
            scenario.DataFingerprint = "loss-boundary";
            scenario.Participants[1].ActorId = BattlePal3ARules.LiaoRiCarryActor;
            scenario.Pal3A.PreserveLiaoRiLosses = true;
            int overflow = int.MaxValue - scenario.Participants[1].Attributes[resource] + 1;
            if (resource == 0)
                scenario.Pal3A.HpLoss = overflow;
            else
                scenario.Pal3A.MpLoss = overflow;
            Assert.That(scenario.Validate(), Is.Empty);
            var definitions = new BattleDefinitions("PAL3A", scenario.DataFingerprint, Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(), scenario.Participants.Select(actor =>
                new BattleActorDefinition(new BattleActorData { InstanceId = actor.InstanceId })));
            Assert.Throws<ArgumentException>(() => new BattleSession(new BattleSetup(scenario), new Catalog(), definitions: definitions));
        }

        [Test]
        public void RuntimeLiaoRiCarryRetainsNativeInt32WrapAfterAuthoringValidation()
        {
            BattleSession session = Forms();
            object enemy = Actor(session, "E");
            ((BattleParticipant)enemy.GetType().GetField("Initial").GetValue(enemy)).ActorId = BattlePal3ARules.LiaoRiCarryActor;
            typeof(BattleSession).GetField("_preserveLiaoRiLosses", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, true);
            typeof(BattleSession).GetField("_pal3AHpLoss", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, int.MaxValue);
            Assert.DoesNotThrow(() => Invoke(session, "ApplyPal3ALiaoRiCarry", enemy));
            Assert.That(session.Actors.Single(actor => actor.Id == "E").Hp, Is.EqualTo(unchecked(99999 + int.MaxValue)));
            Assert.That(session.Pal3AState.PreserveLiaoRiLosses, Is.False);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void EarthPoisonKeepsNativeIndefiniteDamageThroughItsFullDuration(string variant)
        {
            var rules = new BattleStatusRules(variant);
            BattleStatusInstance state = rules.Create(BattleState.PoisonEarth, 1000000);
            int firstIndefinite = -1;
            for (int round = 0; round < short.MaxValue; round++)
            {
                int delta = rules.TickRound(state, 1000000, 1000000);
                if (delta == int.MinValue && firstIndefinite < 0)
                    firstIndefinite = round;
                if (firstIndefinite >= 0)
                    Assert.That(delta, Is.EqualTo(int.MinValue));
            }
            Assert.That(firstIndefinite, Is.EqualTo(29));
            Assert.That(float.IsNegativeInfinity(state.EarthDamage), Is.True);
            Assert.That(state.RemainingRounds, Is.Zero);
        }

        [TestCase(2147483520f, 2147483520)]
        [TestCase(2147483648f, int.MinValue)]
        [TestCase(-2147483904f, int.MinValue)]
        [TestCase(float.PositiveInfinity, int.MinValue)]
        [TestCase(float.NegativeInfinity, int.MinValue)]
        [TestCase(float.NaN, int.MinValue)]
        public void RoundMatchesPublishedMaskedFistp(float value, int expected) => Assert.That(BattleBasicRules.Round(value), Is.EqualTo(expected));

        [Test]
        public void OrbRetainsPublishedHalfSizeAndInt64TruncationAtTheRightEdge()
        {
            var orb = new BattleSpiritOrb(new BattleSpiritPoint(114, 0), new BattleSpiritRect(100, 100, 128, 128), 3.06f);
            for (int frame = 1; frame <= 1000; frame++)
            {
                Assert.That(orb.Advance(.02f), Is.False);
                if (frame == 500)
                    Assert.That(orb.Position.Y, Is.EqualTo(-1011351552));
            }
            Assert.That((orb.Position.X, orb.Position.Y), Is.EqualTo((114f, 0f)));
            var right = new BattleSpiritOrb(new BattleSpiritPoint(200, 0), new BattleSpiritRect(100, 100, 128, 128), 3.06f);
            Assert.That(right.Advance(.02f), Is.True);
            Assert.That((right.Position.X, right.Position.Y), Is.EqualTo((199f, 0f)));
        }

        [TestCase(1u, 4294967293u)]
        [TestCase(2u, 4294967294u)]
        [TestCase(3u, uint.MaxValue)]
        [TestCase(4u, 0u)]
        public void ShortOneShotClipsRetainPublishedUnsignedGameBoxClock(uint duration, uint expected)
        {
            var binding = new BattleBodyAnimationBinding(new BattlePhysicalAnimation("short", 1, 1, gameBoxDuration: duration), false);
            for (int frame = 0; frame < 3; frame++)
            {
                binding.Advance();
                Assert.That(binding.GameBoxTime, Is.EqualTo(expected));
                binding.PopMessage();
                Assert.That(binding.LastPoppedMarker, Is.EqualTo(BattlePhysicalMarkerKind.Over));
            }
        }

        [Test]
        public void PublicInputAndHitFlashReadsCannotWriteSessionHistoryOrCollections()
        {
            BattleSession session = Forms();
            session.SetAutomatic(BattleSide.Enemy, false);
            BattleInput input = session.Inputs.Last();
            BattleInputKind kind = input.Kind;
            input.Kind = BattleInputKind.Fault;
            input.Tick = long.MaxValue;
            input.Reason = "external mutation";
            Assert.That(session.Inputs.Last().Kind, Is.EqualTo(kind));
            Assert.That(session.Inputs.Last().Tick, Is.Zero);
            Invoke(session, "ShowOriginalHitFlash", Actor(session, "E"));
            var flashes = (IList<BattleHitFlashSnapshot>)session.HitFlashes;
            Assert.That(flashes.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => flashes[0] = null);
            Assert.That(session.HitFlashes.Single(), Is.Not.Null);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ResultBodyBindingsAreDetachedForRoleAndVictoryControllers(string variant)
        {
            var animation = new BattlePhysicalAnimation("idle", 5, 1, new[] { new BattlePhysicalMarker(1, BattlePhysicalMarkerKind.Work) });
            var initial = new BattleBodyAnimationBinding(animation);
            var role = new BattleResultRoleBody(variant, initial, new Dictionary<int, BattlePhysicalAnimation> { [32] = animation }, new BattleResultBodyState(100, 100));
            role.Step();
            BattleBodyAnimationBinding captured = role.Binding;
            uint time = captured.GameBoxTime;
            captured.Reset();
            captured.ClearMessages();
            captured.SetRequestedNativeAction(60);
            Assert.That(role.Binding.GameBoxTime, Is.EqualTo(time));
            Assert.That(role.Binding.RequestedNativeActionIndex, Is.EqualTo(32));
            var victory = new BattleVictoryBody(variant, initial, animation, animation);
            victory.Step();
            time = victory.Binding.GameBoxTime;
            victory.Binding.Reset();
            Assert.That(victory.Binding.GameBoxTime, Is.EqualTo(time));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ScenarioRejectsDuplicateSkillAndStatusIdsBeforeSessionInitialization(bool skills)
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            if (skills)
                scenario.Participants[0].Skills.AddRange(new[] { new BattleSkill { Id = 301 }, new BattleSkill { Id = 301 } });
            else
                scenario.Participants[0].Statuses.AddRange(new[] { new BattleStatus { Id = 1, Rounds = 2 }, new BattleStatus { Id = 1, Rounds = 3 } });
            Assert.That(scenario.Validate(), Is.Not.Empty);
            Assert.Throws<ArgumentException>(() => new BattleSetup(scenario));
        }

        [Test]
        public void ScenarioRejectsNullSkillAndStatusEntries()
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Participants[0].Skills.Add(null);
            scenario.Participants[0].Statuses.Add(null);
            Assert.That(scenario.Validate(), Has.Some.Contains("initial skill is missing"));
            Assert.That(scenario.Validate(), Has.Some.Contains("initial status is missing"));
            Assert.Throws<ArgumentException>(() => new BattleSetup(scenario));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        public void PreservedLossesRejectNegativeAuthoringValues(int hp, int mp)
        {
            var state = new BattlePal3AState { PreserveLiaoRiLosses = true, HpLoss = hp, MpLoss = mp };
            Assert.Throws<ArgumentException>(() => state.Validate());
            state.PreserveLiaoRiLosses = false;
            Assert.DoesNotThrow(state.Validate);
        }

        [TestCase(0, 20)]
        [TestCase(252, 0)]
        [TestCase(508, 0)]
        [TestCase(1000000, 84)]
        public void AtbRetainsPublishedByteStorageForAuthoredSpeeds(int speed, int expected)
        {
            BattleParticipant actor = BattleSessionTests.Scenario().Participants[0];
            actor.Attributes[5] = speed;
            Assert.That(new BattleBasicRules().GaugePerStep(actor), Is.EqualTo(expected));
        }

        private static BattleSession ExtremeRewardSession(string variant)
        {
            BattleScenario scenario = BattleSessionTests.Scenario();
            scenario.Variant = variant;
            scenario.DataFingerprint = "reward-boundary";
            scenario.Participants[1].Vitals[0] = 0;
            BattleParticipant second = scenario.Participants[1].Copy();
            second.InstanceId = "Enemy2";
            second.Slot = BattleSlot.Fire;
            scenario.Participants.Add(second);
            var definitions = new BattleDefinitions(variant, scenario.DataFingerprint, Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(), scenario.Participants.Select(actor =>
                new BattleActorDefinition(new BattleActorData { InstanceId = actor.InstanceId, ExperienceReward = int.MaxValue, MoneyReward = int.MaxValue })));
            return new BattleSession(new BattleSetup(scenario), new Catalog(), definitions: definitions);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ExtremeRewardTotalsUseNativeInt32AccumulationWithoutFaulting(string variant)
        {
            BattleSession session = ExtremeRewardSession(variant);
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed), session.Result?.Reason);
            Assert.That(session.Result.Rewards.Single().ExperienceGained, Is.EqualTo(-2));
            Assert.That(session.Result.MoneyAfter, Is.Zero);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void CompletedNativeSignedRewardsCanBeConsumedByResultStatistics(string variant)
        {
            BattleSession session = ExtremeRewardSession(variant);
            BattleResult result = session.Result;
            var presentation = new BattleResultPresentation(variant, result, session.Events);
            for (int step = 0; step < 1000 && !presentation.StatisticsVisible; step++)
                presentation.Step();
            Assert.That(presentation.StatisticsVisible, Is.True);
            Assert.That(presentation.StatisticsSeconds, Is.Zero);
            var experience = new List<int>();
            for (int frame = 0; frame <= 100; frame++)
            {
                foreach (BattleActorSnapshot actor in presentation.Actors.Where(actor => actor.Side == BattleSide.Ally))
                {
                    BattleActorReward reward = result.Rewards.Single(value => value.Actor == actor.Id);
                    BattleActorSnapshot before = result.PreSettlementActors.Single(value => value.Id == actor.Id);
                    double seconds = presentation.StatisticsSeconds;
                    Assert.That(BattleResultStatistics.CardY(0, seconds), Is.InRange(-256, 0));
                    Assert.That(BattleResultStatistics.MagicUseCounts(variant, session.Definitions,
                        session.Definitions.Actors[actor.Id].Identity, before.Skills), Is.EqualTo(new int[6]));
                    int projected = BattleResultStatistics.Experience(reward.ExperienceGained, seconds);
                    if (frame is 0 or 50 or 100)
                        experience.Add(projected);
                }
                presentation.Step();
            }
            Assert.That(experience, Is.EqualTo(new[] { 0, -1, -2 }));
            Assert.That(result.Rewards.Single().ExperienceGained, Is.EqualTo(-2));
        }
    }
}
