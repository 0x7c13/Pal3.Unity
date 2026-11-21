// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor.Tests
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using Pal3.Battle;

    public sealed class BattlePal3AFormRulesTests
    {
        [TestCase(1, 0, 0, -1, false, 0, 1u)]
        [TestCase(1, 0, 0, -1, true, 0, 3357800067u)]
        [TestCase(1, 0, 0, 0, false, 0, 1u)]
        [TestCase(1, 0, 0, 0, true, 0, 3357800067u)]
        [TestCase(1, 0, 0, 1, false, 0, 1u)]
        [TestCase(1, 0, 0, 1, true, 0, 3357800067u)]
        [TestCase(1, 10, 500, -1, false, 551, 3357800067u)]
        [TestCase(1, 10, 500, -1, true, 324, 3884216597u)]
        [TestCase(1, 10, 500, 0, false, 525, 3357800067u)]
        [TestCase(1, 10, 500, 0, true, 309, 3884216597u)]
        [TestCase(1, 10, 500, 1, false, 499, 3357800067u)]
        [TestCase(1, 10, 500, 1, true, 294, 3884216597u)]
        [TestCase(1, 101, 17, -1, false, -103, 3357800067u)]
        [TestCase(1, 101, 17, -1, true, -60, 3884216597u)]
        [TestCase(1, 101, 17, 0, false, -98, 3357800067u)]
        [TestCase(1, 101, 17, 0, true, -57, 3884216597u)]
        [TestCase(1, 101, 17, 1, false, -93, 3357800067u)]
        [TestCase(1, 101, 17, 1, true, -54, 3884216597u)]
        [TestCase(1, 1000, 300, -1, false, -891, 3357800067u)]
        [TestCase(1, 1000, 300, -1, true, -525, 3884216597u)]
        [TestCase(1, 1000, 300, 0, false, -849, 3357800067u)]
        [TestCase(1, 1000, 300, 0, true, -500, 3884216597u)]
        [TestCase(1, 1000, 300, 1, false, -807, 3357800067u)]
        [TestCase(1, 1000, 300, 1, true, -475, 3884216597u)]
        [TestCase(7, 0, 0, -1, false, 0, 7u)]
        [TestCase(7, 0, 0, -1, true, 0, 3289278137u)]
        [TestCase(7, 0, 0, 0, false, 0, 7u)]
        [TestCase(7, 0, 0, 0, true, 0, 3289278137u)]
        [TestCase(7, 0, 0, 1, false, 0, 7u)]
        [TestCase(7, 0, 0, 1, true, 0, 3289278137u)]
        [TestCase(7, 10, 500, -1, false, 590, 3289278137u)]
        [TestCase(7, 10, 500, -1, true, 324, 461394683u)]
        [TestCase(7, 10, 500, 0, false, 562, 3289278137u)]
        [TestCase(7, 10, 500, 0, true, 309, 461394683u)]
        [TestCase(7, 10, 500, 1, false, 534, 3289278137u)]
        [TestCase(7, 10, 500, 1, true, 294, 461394683u)]
        [TestCase(7, 101, 17, -1, false, -103, 3289278137u)]
        [TestCase(7, 101, 17, -1, true, -56, 461394683u)]
        [TestCase(7, 101, 17, 0, false, -98, 3289278137u)]
        [TestCase(7, 101, 17, 0, true, -53, 461394683u)]
        [TestCase(7, 101, 17, 1, false, -93, 3289278137u)]
        [TestCase(7, 101, 17, 1, true, -50, 461394683u)]
        [TestCase(7, 1000, 300, -1, false, -912, 3289278137u)]
        [TestCase(7, 1000, 300, -1, true, -501, 461394683u)]
        [TestCase(7, 1000, 300, 0, false, -869, 3289278137u)]
        [TestCase(7, 1000, 300, 0, true, -477, 461394683u)]
        [TestCase(7, 1000, 300, 1, false, -826, 3289278137u)]
        [TestCase(7, 1000, 300, 1, true, -453, 461394683u)]
        [TestCase(12345, 0, 0, -1, false, 0, 12345u)]
        [TestCase(12345, 0, 0, -1, true, 0, 1255958651u)]
        [TestCase(12345, 0, 0, 0, false, 0, 12345u)]
        [TestCase(12345, 0, 0, 0, true, 0, 1255958651u)]
        [TestCase(12345, 0, 0, 1, false, 0, 12345u)]
        [TestCase(12345, 0, 0, 1, true, 0, 1255958651u)]
        [TestCase(12345, 10, 500, -1, false, 597, 1255958651u)]
        [TestCase(12345, 10, 500, -1, true, 328, 1449997005u)]
        [TestCase(12345, 10, 500, 0, false, 569, 1255958651u)]
        [TestCase(12345, 10, 500, 0, true, 312, 1449997005u)]
        [TestCase(12345, 10, 500, 1, false, 541, 1255958651u)]
        [TestCase(12345, 10, 500, 1, true, 296, 1449997005u)]
        [TestCase(12345, 101, 17, -1, false, -98, 1255958651u)]
        [TestCase(12345, 101, 17, -1, true, -54, 1449997005u)]
        [TestCase(12345, 101, 17, 0, false, -93, 1255958651u)]
        [TestCase(12345, 101, 17, 0, true, -51, 1449997005u)]
        [TestCase(12345, 101, 17, 1, false, -88, 1255958651u)]
        [TestCase(12345, 101, 17, 1, true, -48, 1449997005u)]
        [TestCase(12345, 1000, 300, -1, false, -795, 1255958651u)]
        [TestCase(12345, 1000, 300, -1, true, -437, 1449997005u)]
        [TestCase(12345, 1000, 300, 0, false, -757, 1255958651u)]
        [TestCase(12345, 1000, 300, 0, true, -416, 1449997005u)]
        [TestCase(12345, 1000, 300, 1, false, -719, 1255958651u)]
        [TestCase(12345, 1000, 300, 1, true, -395, 1449997005u)]
        public void CaptureForecastMatchesReleasedInstructions(int seed, int attack, int defense, int difference, bool row, int expected, uint finalSeed)
        {
            var random = new BattleRandom(seed);
            Assert.That(BattlePal3ARules.CaptureForecast(31, attack, defense, difference, row, random), Is.EqualTo(expected));
            Assert.That(random.State, Is.EqualTo(finalSeed));
        }

        [TestCase(2, 9, 9, 9, 9, 0)]
        [TestCase(3, 9, 6, 3, 5, 3)]
        [TestCase(8, 9, 6, 9, 9, 6)]
        [TestCase(9, 9, 9, 9, 9, 9)]
        public void WheelUsesItsLeastChargedElement(int water, int fire, int wind, int thunder, int earth, int rounds)
        {
            var wheel = new BattleSpiritWheelSnapshot(new[] { water, fire, wind, thunder, earth });
            Assert.That(wheel.NangongRounds, Is.EqualTo(rounds));
            Assert.That(BattlePal3ARules.Gain(wheel, 0, 100).Charges[0], Is.EqualTo(9));
            Assert.That(wheel.Charges[0], Is.EqualTo(water));
        }

        [Test]
        public void AuthoredAndRuntimeWheelSnapshotsDoNotAlias()
        {
            var authored = new BattleSpiritWheel { Charges = new[] { 1, 2, 3, 4, 5 } };
            var copy = authored.Copy();
            BattleSpiritWheelSnapshot captured = authored.Capture();
            authored.Charges[0] = 9;
            Assert.That(copy.Charges[0], Is.EqualTo(1));
            Assert.That(captured.Charges[0], Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => new BattleSpiritWheelSnapshot(new[] { 1, 2, 3, 4, 10 }));
        }

        [TestCase(0, -100, 100)]
        [TestCase(100, -999999, 1000099)]
        [TestCase(int.MaxValue, -1, int.MinValue)]
        [TestCase(-1, -1, 0)]
        [TestCase(0, 2, 0)]
        public void LossUsesRequestedInt32MagnitudeBeforeResourceClamp(int before, int delta, int after)
            => Assert.That(BattlePal3ARules.AddLoss(before, delta), Is.EqualTo(after));

        private sealed class Catalog : IBattleCatalog, IBattleAbilityTimings, IBattlePal3AFormCatalog
        {
            internal bool MissingFormClip;
            internal bool FailHumanModel;
            internal uint FormDuration = 192;

            public AttackTiming Attack(string source, string target) => new(0, 3, new[] { 1 });

            public AttackTiming Ability(string source, BattleAbilityDefinition ability) => new(0, 3, new[] { 1 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "P" ? 2 : 0;

            public int DamageDivisor(string actor) => 1;

            public BattlePhysicalAnimation FormChangeAnimation(string actorId, bool transformed) => MissingFormClip ? null : new("z20", 2, 1f,
                Array.Empty<BattlePhysicalMarker>(), FormDuration);

            public void ActivatePal3AForm(string actorId, BattlePal3AFormKind kind)
            {
                if (FailHumanModel && kind == BattlePal3AFormKind.Human)
                    throw new InvalidOperationException("Human model unavailable");
            }
        }

        private sealed class ReadyRules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => BattleSession.FullGauge;

            public int GaugePerStep(BattleParticipant actor) => 1000;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random, bool targetAtHome = true, int damageDivisor = 1)
                => new BattleBasicRules().Hit(source, target, guarding, random, targetAtHome, damageDivisor);
        }

        private static BattleSession Session(bool nangong = false, bool pengxu = false, BattleTargetRange nangongRange = BattleTargetRange.OpponentSingle, Catalog catalog = null)
        {
            var scenario = new BattleScenario { Variant = "PAL3A", Scene = "native", DataFingerprint = "native" };
            scenario.Pal3A.NangongStartsTransformed = nangong;
            scenario.Pal3A.PengxuStartsTransformed = pengxu;
            scenario.Pal3A.SpiritWheel.Charges = new[] { 3, 3, 3, 3, 3 };
            scenario.Participants.AddRange(new[]
            {
                new BattleParticipant { InstanceId = "A", ActorId = 101, Side = BattleSide.Ally, Attributes = new[] { 1000, 100, 200, 100, 50, 5, 10, 0, 0, 0, 0, 0 }, Vitals = new[] { 777, 100, 200 } },
                new BattleParticipant { InstanceId = "P", ActorId = 103, Side = BattleSide.Ally, Slot = BattleSlot.Fire, Attributes = new[] { 2000, 100, 500, 100, 101, 5, 10, 0, 0, 0, 0, 0 }, Vitals = new[] { 1000, 100, 500 } },
                new BattleParticipant { InstanceId = "E", ActorId = 701, Side = BattleSide.Enemy, Attributes = new[] { 99999, 100, 500, 100, 30, 3, 0, 0, 0, 0, 0, 0 }, Vitals = new[] { 99999, 100, 500 } }
            });
            var definitions = new BattleDefinitions("PAL3A", "native", Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(), new[]
            {
                new BattleActorDefinition(new BattleActorData { InstanceId = "A", Identity = 0, NormalAttackRange = nangongRange }),
                new BattleActorDefinition(new BattleActorData { InstanceId = "P", Identity = 2 }),
                new BattleActorDefinition(new BattleActorData { InstanceId = "E" })
            });
            var session = new BattleSession(new BattleSetup(scenario), catalog ?? new Catalog(), new ReadyRules(), definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            return session;
        }

        private static object Actor(BattleSession session, string id) =>
            ((IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(session))[id];

        private static object Invoke(BattleSession session, string method, params object[] arguments) =>
            typeof(BattleSession).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(session, arguments);

        [Test]
        public void FailedHumanModelRestoreStillEndsBattle()
        {
            var catalog = new Catalog();
            BattleSession session = Session(nangong: true, pengxu: true, catalog: catalog);
            catalog.FailHumanModel = true;
            session.Fault("initial presentation failure");
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Faulted));
            Assert.That(session.Result.Reason, Is.EqualTo("initial presentation failure"));
            Assert.That(session.Result.Winner, Is.Null);
            Assert.That(session.StageTasks, Is.Empty);
            Assert.That(session.Result.Actors.Single(actor => actor.Id == "P").MaxHp, Is.EqualTo(2000));
            Assert.That(session.IsPal3ATransformed("A") || session.IsPal3ATransformed("P"), Is.False);
            Assert.That(session.Events.Where(entry => entry.Kind == BattleEventKind.PresentationCleanupFailed)
                .Select(entry => entry.Source), Is.EquivalentTo(new[] { "A", "P" }));
            Assert.That(session.Events.Count(entry => entry.Kind == BattleEventKind.Ended), Is.EqualTo(1));
        }

        [Test]
        public void SuccessfulPartyFleeCancelsActiveTeammateFormWithoutFreezingRun()
        {
            BattleSession session = Session(catalog: new Catalog { FormDuration = 19200 });
            typeof(BattleSession).GetField("_fleeAlwaysSucceeds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, true);
            Assert.That(session.Submit("A", BattleAction.Flee), Is.True);
            object flee = null;
            for (int tick = 0; tick < 20; tick++)
            {
                session.Step();
                flee = ((IDictionary)typeof(BattleSession).GetField("_fleeStages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session)).Values.Cast<object>().SingleOrDefault();
                if (flee != null && (bool)flee.GetType().GetField("BodyFinished", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(flee))
                    break;
            }
            Assert.That(flee, Is.Not.Null);
            Assert.That((bool)flee.GetType().GetField("BodyFinished", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(flee), Is.True);
            Invoke(session, "QueuePal3AFormChange", Actor(session, "P"), true, false);
            session.Step();
            Assert.That(session.Pal3AForm("P").Pending, Is.True);
            Assert.That(session.IsPal3ATransformed("P"), Is.True);
            session.Step();
            Assert.That(session.StageTasks.Any(task => task.Owner == "P" && task.Kind == "PAL3A form"), Is.False);
            for (int tick = 0; tick < 1200 && session.Status == BattleSessionStatus.Running; tick++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed), session.Result?.Reason);
            Assert.That(session.Result.Escaped, Is.True);
            Assert.That(session.Pal3AForm("P").Pending, Is.False);
            BattleActorSnapshot pengxu = session.Result.Actors.Single(actor => actor.Id == "P");
            Assert.That((pengxu.Hp, pengxu.MaxHp, pengxu.Attributes[4]), Is.EqualTo((1000, 2000, 101)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancellingFormTaskReleasesOnlyItsAcquiredAbsoluteTime(bool acquired)
        {
            BattleSession session = Session();
            var handle = (BattleStageHandle)Invoke(session, "QueuePal3AFormChange", Actor(session, "P"), true, false);
            if (acquired)
                session.Step();
            else
                typeof(BattleSession).GetField("_absoluteTimeActor", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, "P");
            var scheduler = (BattleStageScheduler)typeof(BattleSession).GetField("_stage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            Assert.That(scheduler.Cancel(handle), Is.True);
            Assert.That(session.Pal3AForm("P").Pending, Is.False);
            Assert.That(typeof(BattleSession).GetField("_absoluteTimeActor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session), Is.EqualTo(acquired ? null : "P"));
            Assert.That(scheduler.Cancel(handle), Is.False);
            session.Abort();
            Assert.That(session.Result.Actors.Single(actor => actor.Id == "P").MaxHp, Is.EqualTo(2000));
        }

        [Test]
        public void StoppingStageReleasesActiveFormOwnershipBeforeTerminalCleanup()
        {
            BattleSession session = Session();
            Invoke(session, "QueuePal3AFormChange", Actor(session, "P"), true, false);
            session.Step();
            Assert.That(session.Pal3AForm("P").Pending, Is.True);
            Invoke(session, "StopStage");
            Assert.That(session.Pal3AForm("P").Pending, Is.False);
            Assert.That(typeof(BattleSession).GetField("_absoluteTimeActor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session), Is.Null);
            Assert.That(session.StageTasks, Is.Empty);
            session.Abort();
            Assert.That(session.Result.Actors.Single(actor => actor.Id == "P").MaxHp, Is.EqualTo(2000));
        }

        [Test]
        public void CaptureIntMinForecastUsesSignedNativeAbs()
        {
            Assert.That(BattlePal3ARules.CanCapture(1, true, true, false, int.MinValue), Is.False);
            Assert.That(BattlePal3ARules.CanCapture(int.MinValue, true, true, false, int.MinValue), Is.True);
        }

        [Test]
        public void OpeningFormsCaptureResourcesAndKeepNativeIdentity()
        {
            BattleSession session = Session(true, true);
            Assert.That(session.Pal3AState.SpiritWheel.Charges, Is.All.Zero);
            Assert.That(session.Pal3AForm("A").RoundLimit, Is.EqualTo(9));
            Assert.That(session.Pal3AForm("A").Model, Is.EqualTo("107"));
            Assert.That(session.Actors.Single(actor => actor.Id == "A").Attributes[3], Is.EqualTo(200));
            Assert.That(session.Actors.Single(actor => actor.Id == "A").Attributes[4], Is.EqualTo(100));
            Assert.That(session.Actors.Single(actor => actor.Id == "P").Hp, Is.EqualTo(400));
            Assert.That(session.Actors.Single(actor => actor.Id == "P").Attributes[4], Is.EqualTo(202));
            Assert.That(session.Pal3AForm("P").RoundLimit, Is.EqualTo(5));
            Assert.That(session.Pal3AForm("P").Model, Is.EqualTo("106"));
            Assert.That(session.CaptureParticipant("P").ActorId, Is.EqualTo(103));
            Assert.That(session.Actors.Single(actor => actor.Id == "P").States.Single(state => state.State == BattleState.Barrier).Permanent, Is.False);
        }

        [Test]
        public void PengxuMpImmunityPrecedesSavingAndLossAccounting()
        {
            BattleSession session = Session(pengxu: true);
            object actor = Actor(session, "P");
            Assert.That(Invoke(session, "ChangeResource", actor, 2, -1000, null, 0L, "native MP", false), Is.EqualTo(0));
            Assert.That(session.Actors.Single(candidate => candidate.Id == "P").Mp, Is.EqualTo(500));
            Assert.That(session.Pal3AState.MpLoss, Is.Zero);
            Assert.That(BattlePal3ARules.PengxuSpellPower(-101, true), Is.EqualTo(-152));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancellingAnActiveTransitionPreservesOnlyEffectsAlreadyApplied(bool modelSwapped)
        {
            BattleSession session = Session(catalog: new Catalog { FormDuration = 192 });
            Assert.That(session.Submit("A", BattleAction.Transform), Is.True);
            session.Step();
            Assert.That(session.Pal3AForm("A").Pending, Is.True);
            if (modelSwapped)
                for (int step = 0; step < 20 && !session.IsPal3ATransformed("A"); step++)
                    session.Step();
            Assert.That(session.IsPal3ATransformed("A"), Is.EqualTo(modelSwapped));
            Assert.That(session.CancelAction("A"), Is.True);
            Assert.That(session.Pal3AForm("A").Pending, Is.False);
            Assert.That(session.StageTasks.Any(task => task.Owner == "A" && task.Kind == "PAL3A form"), Is.False);
            long simulationTick = session.SimulationTick;
            session.Step();
            Assert.That(session.SimulationTick, Is.EqualTo(simulationTick + 1));
            Assert.That(session.IsPal3ATransformed("A"), Is.EqualTo(modelSwapped));
            Assert.That(session.Pal3AState.SpiritWheel.Charges, Is.All.EqualTo(modelSwapped ? 0 : 3));
        }

        [Test]
        public void FirstRepeatInFireBeastFormOnlyInvalidatesAnIncompatibleLastCommand()
        {
            BattleSession session = Session(nangong: true);
            var commands = (IDictionary)typeof(BattleSession).GetField("_aiLastCommands", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            commands[0] = new BattleAiCommand { Kind = 0x105, AbilityId = 301, Source = 0, Target = 5 };
            Assert.That(session.RequestAiRepeat("A"), Is.False);
            Assert.That(commands.Contains(0), Is.False);
            Assert.That(session.WaitingActor, Is.EqualTo("A"));
            Assert.That(session.Inputs.Last().Kind, Is.EqualTo(BattleInputKind.AiRepeat));
            Assert.That(session.RequestAiRepeat("A"), Is.True);
            Assert.That(session.Inputs.Last().Action, Is.EqualTo(BattleAction.Attack));
        }

        [Test]
        public void RepeatChecksTheOtherTransformedRoleBeforeTheActiveController()
        {
            BattleSession session = Session(nangong: true, pengxu: true);
            var commands = (IDictionary)typeof(BattleSession).GetField("_aiLastCommands", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            commands[0] = new BattleAiCommand { Kind = 0x105 };
            commands[2] = new BattleAiCommand { Kind = 0x103 };
            Assert.That(session.WaitingActor, Is.EqualTo("A"));
            int count = session.Inputs.Count;
            Assert.That(session.RequestAiRepeat("A"), Is.False);
            Assert.That(commands.Contains(2), Is.False);
            Assert.That(commands.Contains(0), Is.True);
            Assert.That(session.Inputs.Count, Is.EqualTo(count + 1));
            Assert.That(session.RequestAiRepeat("A"), Is.False);
            Assert.That(session.Inputs.Count, Is.EqualTo(count + 1));
            commands[2] = new BattleAiCommand { Kind = 0x105 };
            Assert.That(session.RequestAiRepeat("A"), Is.False);
            Assert.That(commands.Contains(2), Is.True);
            Assert.That(commands.Contains(0), Is.False);
            Assert.That(session.RequestAiRepeat("A"), Is.True);
            Assert.That(session.Inputs.Last().Action, Is.EqualTo(BattleAction.Attack));
        }

        [Test]
        public void FairyBeastIgnoresNormalAttackShortcutWithoutOverwritingRepeat()
        {
            BattleSession session = Session(pengxu: true);
            Assert.That(session.Submit("A", BattleAction.Defend), Is.True);
            Assert.That(session.WaitingActor, Is.EqualTo("P"));
            var commands = (IDictionary)typeof(BattleSession).GetField("_aiLastCommands", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            var previous = new BattleAiCommand { Kind = 0x109, Source = 2, Target = -1 };
            commands[2] = previous;
            int inputs = session.Inputs.Count;
            Assert.That(session.RequestAiNormal("P"), Is.False);
            Assert.That(session.Inputs.Count, Is.EqualTo(inputs));
            Assert.That(commands[2], Is.SameAs(previous));
            Assert.That(session.RequestAiRepeat("P"), Is.True);
            Assert.That(session.Inputs.Last().Action, Is.EqualTo(BattleAction.Defend));
        }

        [Test]
        public void FairyBeastRepeatCanFallBackToAiAttackWhileManualAttackIsHidden()
        {
            BattleSession session = Session(pengxu: true);
            Assert.That(session.Submit("A", BattleAction.Defend), Is.True);
            Assert.That(session.WaitingActor, Is.EqualTo("P"));
            Assert.That(session.Submit("P", BattleAction.Attack, "E"), Is.False);
            var commands = (IDictionary)typeof(BattleSession).GetField("_aiLastCommands", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            commands[2] = new BattleAiCommand { Source = 2, Kind = 0x105, AbilityId = 9001, Target = 5 };
            Assert.That(session.RequestAiRepeat("P"), Is.True);
            Assert.That(session.Inputs.Last().Action, Is.EqualTo(BattleAction.Attack));
        }

        [Test]
        public void ReadyFireBeastCannotAcceptCommandsWhileReturningToHumanForm()
        {
            BattleSession session = Session(nangong: true);
            Assert.That(session.WaitingActor, Is.EqualTo("A"));
            Invoke(session, "QueuePal3AFormChange", Actor(session, "A"), false, false);
            Assert.That(session.WaitingActor, Is.Not.EqualTo("A"));
            session.Step();
            Assert.That(session.Pal3AForm("A").Pending, Is.True);
            int inputs = session.Inputs.Count;
            Assert.That(session.RequestAiNormal("A"), Is.False);
            Assert.That(session.CommitAiControl("A", 33), Is.False);
            Assert.That(session.Submit("A", BattleAction.Defend), Is.False);
            Assert.That(session.Inputs.Count, Is.EqualTo(inputs));
        }

        [Test]
        public void ManualNangongTransformationUsesRecordedCommandAndCompletesBodyQueue()
        {
            BattleSession session = Session();
            Assert.That(session.Submit("A", BattleAction.Transform), Is.True);
            for (int tick = 0; tick < 40 && (!session.IsPal3ATransformed("A") || session.Pal3AForm("A").Pending); tick++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running), session.Result?.Reason);
            Assert.That(session.IsPal3ATransformed("A"), Is.True);
            Assert.That(session.Pal3AForm("A").Pending, Is.False);
            Assert.That(session.Pal3AForm("A").RoundLimit, Is.EqualTo(3));
            Assert.That(session.Inputs.Any(input => input.Action == BattleAction.Transform), Is.True);
        }

        [Test]
        public void MissingFairyBeastFormActionKeepsSelectedModelClockAndFifoUntilOver()
        {
            BattleSession session = Session(catalog: new Catalog { MissingFormClip = true });
            object actor = Actor(session, "P");
            Invoke(session, "QueuePal3AFormChange", actor, true, false);
            session.Step();
            Assert.That(session.Pal3AForm("P").Kind, Is.EqualTo(BattlePal3AFormKind.PengxuFairyBeast));
            var idle = new BattlePhysicalAnimation("z00", 5, 1, new[]
            {
                new BattlePhysicalMarker(0, BattlePhysicalMarkerKind.Other, 1, "sound"),
                new BattlePhysicalMarker(0, BattlePhysicalMarkerKind.Work, 1, "work")
            }, 480);
            var binding = (BattleBodyAnimationBinding)Invoke(session, "GetBodyBinding", actor);
            binding.TryPlay(idle, true);
            binding.Advance();
            Assert.That(binding.PendingMessageCount, Is.EqualTo(2));
            session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running), session.Result?.Reason);
            Assert.That(binding.CurrentAnimation, Is.SameAs(idle));
            Assert.That(binding.RequestedNativeActionIndex, Is.EqualTo(60));
            Assert.That(binding.GameBoxTime, Is.EqualTo(192u));
            Assert.That(binding.LastPoppedName, Is.EqualTo("sound"));
            Assert.That(binding.PendingMessageCount, Is.EqualTo(1));
            Assert.That(session.Pal3AForm("P").Pending, Is.True);
            session.Step();
            Assert.That(binding.LastPoppedName, Is.EqualTo("work"));
            Assert.That(session.Pal3AForm("P").Pending, Is.True);
            for (int tick = 0; tick < 20 && session.Pal3AForm("P").Pending; tick++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running), session.Result?.Reason);
            Assert.That(session.Pal3AForm("P").Pending, Is.False);
            Assert.That(binding.CurrentAnimation, Is.SameAs(idle));
        }

        [Test]
        public void MissingFormActionWithoutAnExistingModelAnimationCannotInventCompletion()
        {
            BattleSession session = Session(catalog: new Catalog { MissingFormClip = true });
            Invoke(session, "QueuePal3AFormChange", Actor(session, "P"), true, false);
            for (int tick = 0; tick < 200; tick++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running), session.Result?.Reason);
            Assert.That(session.Pal3AForm("P").Kind, Is.EqualTo(BattlePal3AFormKind.PengxuFairyBeast));
            Assert.That(session.Pal3AForm("P").Pending, Is.True);
            Assert.That(session.CurrentBodyAnimation("P"), Is.Null);
        }

        [Test]
        public void NangongLethalHitReturnsHumanAtOneHpInsteadOfDying()
        {
            BattleSession session = Session(nangong: true);
            Invoke(session, "ChangeHp", Actor(session, "A"), -10000, null, 0L, "native lethal", false, false, false);
            Assert.That(session.Actors.Single(actor => actor.Id == "A").Hp, Is.EqualTo(1));
            for (int tick = 0; tick < 40 && session.IsPal3ATransformed("A"); tick++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running), session.Result?.Reason);
            Assert.That(session.IsPal3ATransformed("A"), Is.False);
            Assert.That(session.Actors.Single(actor => actor.Id == "A").Hp, Is.EqualTo(1));
            Assert.That(session.Pal3AState.HpLoss, Is.EqualTo(10000));
        }

        [TestCase(BattleTargetRange.OpponentSingle)]
        [TestCase(BattleTargetRange.OpponentRow)]
        public void NangongFireBeastClearsHumanWeaponRangeAndRestoresItOnReturn(BattleTargetRange humanRange)
        {
            BattleSession session = Session(nangong: true, nangongRange: humanRange);
            object actor = Actor(session, "A");
            Assert.That(Invoke(session, "ActionRange", actor, null), Is.EqualTo(BattleTargetRange.OpponentSingle));
            Assert.That(Invoke(session, "CanPal3ASpiritCapture", Actor(session, "E"), actor), Is.False);
            var random = (BattleRandom)typeof(BattleSession).GetField("_random", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(session);
            Assert.That(random.State, Is.EqualTo(3357800067u), "Fire beast forecast uses cleared single-target flags, without the human weapon's extra row/column roll.");
            Invoke(session, "QueuePal3AFormChange", actor, false, false);
            for (int tick = 0; tick < 40 && session.IsPal3ATransformed("A"); tick++)
                session.Step();
            Assert.That(session.IsPal3ATransformed("A"), Is.False);
            Assert.That(Invoke(session, "ActionRange", actor, null), Is.EqualTo(humanRange));
        }

        [Test]
        public void PengxuNormalReturnRestoresSavedHpMaximumAndDefense()
        {
            BattleSession session = Session(pengxu: true);
            object actor = Actor(session, "P");
            Invoke(session, "ChangeHp", actor, -100, null, 0L, "native fairy damage", false, false, false);
            Invoke(session, "QueuePal3AFormChange", actor, false, false);
            for (int tick = 0; tick < 40 && session.IsPal3ATransformed("P"); tick++)
                session.Step();
            BattleActorSnapshot restored = session.Actors.Single(candidate => candidate.Id == "P");
            Assert.That(restored.Hp, Is.EqualTo(1000));
            Assert.That(restored.MaxHp, Is.EqualTo(2000));
            Assert.That(restored.Attributes[4], Is.EqualTo(101));
        }

        private sealed class Effects : IBattleEffectDirector, IBattleSpiritCaptureLayout
        {
            public long Start(BattleEffectRequest request) => 1;

            public bool IsRunning(long handle) => false;

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void Stop(long handle)
            {
            }

            public BattleSpiritPoint CaptureSpiritOrigin(string actorId, BattleMovementPoint world) => new(400, 300);

            public BattleSpiritRect SpiritWheelTarget(int coreElement) => new(722, 300, 750, 345);
        }

        private static BattleSession SkillSession(string variant, BattleAbilityDefinition ability, int guCount = 0, bool boss = false, bool invisible = false, int attackBonus = 0, int defense = 30, int captureHp = 1)
        {
            bool capture = ability.Id == 4102;
            var scenario = new BattleScenario { Variant = variant, Scene = "native", DataFingerprint = "native" };
            var hero = new BattleParticipant
            {
                InstanceId = "A",
                ActorId = capture ? 101 : variant == "PAL3" ? 107 : 103,
                AttributeSource = attackBonus == 0 ? BattleAttributeSource.ExplicitEffective : BattleAttributeSource.BaseWithEquipment,
                Side = BattleSide.Ally,
                Attributes = new[] { 1000, 100, 100, 100, 10, 5, 10, 0, 0, 0, 0, 0 },
                Vitals = new[] { 1000, 100, 100 }
            };
            hero.Skills.Add(new BattleSkill { Id = ability.Id, Level = 4, Mastered = true });
            var enemy = new BattleParticipant
            {
                InstanceId = "E",
                ActorId = 7311,
                Level = 31,
                Side = BattleSide.Enemy,
                Attributes = new[] { 99999, 100, 500, 100, defense, 3, 0, 0, 0, 0, 0, 0 },
                Vitals = new[] { capture ? captureHp : 99999, 100, 500 }
            };
            if (invisible)
                enemy.Statuses.Add(new BattleStatus { Id = (int)BattleState.Invisible, Rounds = 5 });
            scenario.Participants.AddRange(new[] { hero, enemy });
            if (guCount > 0)
                scenario.Items.Add(new BattleItem { Id = 6113, Count = guCount });
            var definitions = new BattleDefinitions(variant, "native", new[] { ability }, Array.Empty<BattleAbilityDefinition>(), new[]
            {
                new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = "A", Identity = capture ? 0 : variant == "PAL3" ? 3 : 2,
                    Equipment = attackBonus == 0 ? Array.Empty<BattleEquipmentDefinition>() : new[]
                    {
                        new BattleEquipmentDefinition(new BattleEquipmentData
                        {
                            Id = 80001, Slot = 1, Attributes = new[] { new BattleAttributeEffect(3, BattleValueMode.Absolute, attackBonus) }
                        })
                    }
                }),
                new BattleActorDefinition(new BattleActorData { InstanceId = "E", Boss = boss, PhysicalAttackElementValues = new[] { 1, 2, 0, 0, 1 } })
            });
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new ReadyRules(), definitions, new Effects());
            session.SetAutomatic(BattleSide.Enemy, false);
            return session;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FullCaptureMatchesNativeStageAndKeepsVisualRngSeparate(bool invisible)
        {
            var ability = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 4102,
                Range = BattleTargetRange.OpponentAll,
                Costs = new[] { new BattleCost(1, BattleValueMode.Absolute, 4) }
            });
            BattleSession session = SkillSession("PAL3A", ability, invisible: invisible);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 4102)), Is.True);
            session.Step();
            Assert.That(session.WasSpiritCaptured("E"), Is.True);
            Assert.That(session.SpiritOrbs.Select(orb => orb.Element), Is.EqualTo(new[] { 0, 1, 1, 4 }));
            Assert.That(session.SpiritOrbs.Select(orb => orb.Acceleration), Is.EqualTo(new[] { 3.06f, 2.1599998f, 2.76f, 3.46f }));
            Assert.That(session.Events.Single(entry => entry.Kind == BattleEventKind.Hit).RandomState, Is.EqualTo(3884216597u));
            for (int tick = 0; tick < 250 && session.Status == BattleSessionStatus.Running; tick++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed), session.Result?.Reason);
            Assert.That(session.Result.Pal3A.SpiritWheel.Charges, Is.EqualTo(new[] { 1, 2, 0, 0, 1 }));
            Assert.That(session.Result.Actors.Single(actor => actor.Id == "A").Sp, Is.EqualTo(96));
        }

        [Test]
        public void GuardHalvesTheNativeCaptureHpRequestAndKeepsTheSpiritOrbs()
        {
            var ability = new BattleAbilityDefinition(new BattleAbilityData { Id = 4102, Range = BattleTargetRange.OpponentAll });
            BattleSession session = SkillSession("PAL3A", ability, attackBonus: 400, captureHp: 100);
            object enemy = Actor(session, "E");
            enemy.GetType().GetField("Guard").SetValue(enemy, true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 4102)), Is.True);
            session.Step();

            Assert.That(session.Actors.Single(actor => actor.Id == "E").Hp, Is.EqualTo(50));
            Assert.That(session.WasSpiritCaptured("E"), Is.True);
            Assert.That(session.SpiritOrbs.Select(orb => orb.Element), Is.EqualTo(new[] { 0, 1, 1, 4 }));
        }

        [Test]
        public void FullCaptureWithIndefiniteForecastDoesNotFaultOrCapture()
        {
            var ability = new BattleAbilityDefinition(new BattleAbilityData { Id = 4102, Range = BattleTargetRange.OpponentAll });
            BattleSession session = SkillSession("PAL3A", ability, attackBonus: int.MinValue, defense: 0);
            Assert.That(session.Actors.Single(actor => actor.Id == "A").Attributes[3], Is.EqualTo(int.MinValue + 100));
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 4102)), Is.True);
            session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Running), session.Result?.Reason);
            Assert.That(session.WasSpiritCaptured("E"), Is.False);
            Assert.That(session.SpiritOrbs, Is.Empty);
            Assert.That(session.Actors.Single(actor => actor.Id == "E").Hp, Is.EqualTo(1));
            var random = (BattleRandom)typeof(BattleSession).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            Assert.That(random.State, Is.EqualTo(3357800067u));
        }

        [Test]
        public void AlreadyFleeingEnemyIsSkippedBeforeAnyCaptureForecast()
        {
            var ability = new BattleAbilityDefinition(new BattleAbilityData { Id = 4102, Range = BattleTargetRange.OpponentAll });
            BattleSession session = SkillSession("PAL3A", ability);
            object enemy = Actor(session, "E");
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 4102)), Is.True);
            enemy.GetType().GetField("EnemyFleeing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(enemy, true);
            session.Step();
            Assert.That(session.WasSpiritCaptured("E"), Is.False);
            Assert.That(session.SpiritOrbs, Is.Empty);
            Assert.That(session.Actors.Single(actor => actor.Id == "E").Hp, Is.EqualTo(1));
            Assert.That(session.Events.Last().RandomState, Is.EqualTo(1u));
        }

        [Test]
        public void BossCaptureFailsBeforeForecastRng()
        {
            var ability = new BattleAbilityDefinition(new BattleAbilityData { Id = 4102, Range = BattleTargetRange.OpponentAll });
            BattleSession session = SkillSession("PAL3A", ability, boss: true);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 4102)), Is.True);
            session.Step();
            Assert.That(session.WasSpiritCaptured("E"), Is.False);
            Assert.That(session.SpiritOrbs, Is.Empty);
            Assert.That(session.Events.Last().RandomState, Is.EqualTo(1u));
        }

        [TestCase(true, false, true, true, 3884216597u)]
        [TestCase(false, false, true, false, 3357800067u)]
        [TestCase(true, true, false, true, 1924036713u)]
        public void CaptureUsesPrimaryOffenseLevelRangeAndEnemyDefense(bool strongPrimary, bool primaryRow, bool enemyRow, bool captured, uint randomState)
        {
            var ability = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 4102,
                Range = BattleTargetRange.OpponentAll,
                Costs = new[] { new BattleCost(1, BattleValueMode.Absolute, 4) }
            });
            var scenario = new BattleScenario { Variant = "PAL3A", Scene = "native", DataFingerprint = "native" };
            var primary = new BattleParticipant
            {
                InstanceId = "A",
                ActorId = 101,
                Side = BattleSide.Ally,
                Level = 20,
                Attributes = new[] { 1000, 100, 100, strongPrimary ? 500 : 20, strongPrimary ? 30 : 100, 5, 10, 0, 0, 0, 0, 0 },
                Vitals = new[] { 1000, 100, 100 }
            };
            primary.Skills.Add(new BattleSkill { Id = 4102, Level = 4, Mastered = true });
            scenario.Participants.Add(primary);
            scenario.Participants.Add(new BattleParticipant
            {
                InstanceId = "E",
                ActorId = 7311,
                Side = BattleSide.Enemy,
                Level = 50,
                Attributes = new[] { 1000, 100, 100, strongPrimary ? 20 : 500, strongPrimary ? 100 : 30, 5, 10, 0, 0, 0, 0, 0 },
                Vitals = new[] { 100, 100, 100 }
            });
            var definitions = new BattleDefinitions("PAL3A", "native", new[] { ability }, Array.Empty<BattleAbilityDefinition>(), new[]
            {
                new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = "A", Identity = 0,
                    NormalAttackRange = primaryRow ? BattleTargetRange.OpponentRow : BattleTargetRange.OpponentSingle
                }),
                new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = "E", PhysicalAttackElementValues = new[] { 1, 2, 0, 0, 1 },
                    NormalAttackRange = enemyRow ? BattleTargetRange.OpponentRow : BattleTargetRange.OpponentSingle
                })
            });
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new ReadyRules(), definitions, new Effects());
            session.SetAutomatic(BattleSide.Enemy, false);
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Skill, abilityId: 4102)), Is.True);
            session.Step();
            Assert.That(session.WasSpiritCaptured("E"), Is.EqualTo(captured));
            Assert.That(session.SpiritOrbs.Count, Is.EqualTo(captured ? 4 : 0));
            Assert.That(session.Events.Single(entry => entry.Kind == BattleEventKind.ResourceChanged && entry.Resource == 1).RandomState, Is.EqualTo(randomState));
        }

        [TestCase("PAL3", 4401)]
        [TestCase("PAL3A", 4304)]
        public void GuCostUsesActualItem6113InventoryAndThreeAndAHalfSecondPopup(string variant, int id)
        {
            var ability = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = id,
                Range = BattleTargetRange.OpponentSingle,
                Costs = new[] { new BattleCost(1, BattleValueMode.Absolute, 5), new BattleCost(16, BattleValueMode.Absolute, 1) }
            });
            BattleSession session = SkillSession(variant, ability, 99);
            var command = new BattleCommand("A", BattleAction.Skill, "E", id);
            BattleCostEstimate estimate = session.GetCostEstimate(command).Single(cost => cost.Resource == 16);
            Assert.That(estimate.Available, Is.EqualTo(99));
            Assert.That(estimate.Payable, Is.EqualTo(1));
            Assert.That(session.Submit(command), Is.True);
            session.Step();
            Assert.That(session.Inventory.Single(item => item.Id == 6113).Count, Is.EqualTo(98));
            BattleEvent number = session.Events.Single(entry => entry.Kind == BattleEventKind.NumberShown && entry.Resource == 16);
            Assert.That(number.DurationSeconds, Is.EqualTo(3.5f));
            Assert.That(number.RequestedDelta, Is.EqualTo(-1));
            BattleSession empty = SkillSession(variant, ability);
            Assert.That(empty.CanSubmit(command), Does.Contain("Resource 16"));
        }

        [Test]
        public void OrbPositionMatchesOneHundredNativeUiFrames()
        {
            var orb = new BattleSpiritOrb(new BattleSpiritPoint(0, 0), new BattleSpiritRect(5980, 7980, 6020, 8020), 3.06f);
            orb.Advance(.02f);
            Assert.That(orb.Position.X, Is.Zero);
            Assert.That(orb.Position.Y, Is.Zero);
            for (int frame = 1; frame < 50; frame++)
                orb.Advance(.02f);
            Assert.That(orb.Position.X, Is.EqualTo(156));
            Assert.That(orb.Position.Y, Is.EqualTo(217));
            for (int frame = 50; frame < 100; frame++)
                orb.Advance(.02f);
            Assert.That(orb.Position.X, Is.EqualTo(3663));
            Assert.That(orb.Position.Y, Is.EqualTo(4903));
        }

        [Test]
        public void VictoryRestoresBothHeroFormsBeforeSettlement()
        {
            BattleSession session = Session(nangong: true, pengxu: true);
            Invoke(session, "ChangeHp", Actor(session, "E"), -1000000, null, 0L, "native victory", false, false, false);
            for (int tick = 0; tick < 250 && session.Status == BattleSessionStatus.Running; tick++)
                session.Step();
            Assert.That(session.Status, Is.EqualTo(BattleSessionStatus.Completed), session.Result?.Reason);
            Assert.That(session.IsPal3ATransformed("A"), Is.False);
            Assert.That(session.IsPal3ATransformed("P"), Is.False);
            Assert.That(session.Result.Actors.Single(actor => actor.Id == "P").Hp, Is.EqualTo(1000));
            Assert.That(session.Result.Actors.Single(actor => actor.Id == "P").MaxHp, Is.EqualTo(2000));
        }

        [Test]
        public void BeastCommandGateMatchesNativeMenuMatrix()
        {
            BattleSession session = Session(nangong: true, pengxu: true);
            var magic = new BattleAbilityDefinition(new BattleAbilityData { Id = 301, IsMagic = true });
            var technique = new BattleAbilityDefinition(new BattleAbilityData { Id = 4101 });
            foreach (string id in new[] { "A", "P" })
            {
                object actor = Actor(session, id);
                Assert.That(Invoke(session, "ValidatePal3AFormCommand", actor, new BattleCommand(id, BattleAction.Defend), null, false), Is.Null);
                Assert.That(Invoke(session, "ValidatePal3AFormCommand", actor, new BattleCommand(id, BattleAction.Item), null, false), Is.Not.Null);
                Assert.That(Invoke(session, "ValidatePal3AFormCommand", actor, new BattleCommand(id, BattleAction.Flee), null, false), Is.Null);
                Assert.That(Invoke(session, "ValidatePal3AFormCommand", actor, new BattleCommand(id, BattleAction.Skill, abilityId: 4101), technique, false), Is.Not.Null);
            }
            Assert.That(Invoke(session, "ValidatePal3AFormCommand", Actor(session, "A"), new BattleCommand("A", BattleAction.Attack), null, false), Is.Null);
            Assert.That(Invoke(session, "ValidatePal3AFormCommand", Actor(session, "A"), new BattleCommand("A", BattleAction.Skill, abilityId: 301), magic, false), Is.Not.Null);
            Assert.That(Invoke(session, "ValidatePal3AFormCommand", Actor(session, "P"), new BattleCommand("P", BattleAction.Attack), null, false), Is.Not.Null);
            Assert.That(Invoke(session, "ValidatePal3AFormCommand", Actor(session, "P"), new BattleCommand("P", BattleAction.Skill, abilityId: 301), magic, false), Is.Null);
        }

        [TestCase(0, 3, false)]
        [TestCase(1, 3, false)]
        [TestCase(2, 3, true)]
        [TestCase(3, 5, false)]
        [TestCase(4, 5, true)]
        [TestCase(8, 9, true)]
        public void FormRoundLimitUsesNativePreincrementBoundary(int completed, int limit, bool expired)
            => Assert.That(BattlePal3ARules.ExpiresOnRound(completed, limit), Is.EqualTo(expired));

        [Test]
        public void LiaoRiCarryAppliesOnlyOnceToOriginalActor722()
        {
            var scenario = new BattleScenario { Variant = "PAL3A", Scene = "native", DataFingerprint = "native" };
            scenario.Pal3A.PreserveLiaoRiLosses = true;
            scenario.Pal3A.HpLoss = 1000;
            scenario.Pal3A.MpLoss = 700;
            scenario.Participants.AddRange(new[]
            {
                new BattleParticipant { InstanceId = "A", ActorId = 101, Side = BattleSide.Ally, Attributes = new[] { 1000, 100, 100, 100, 10, 5, 10, 0, 0, 0, 0, 0 }, Vitals = new[] { 1000, 100, 100 } },
                new BattleParticipant { InstanceId = "E", ActorId = 722, Side = BattleSide.Enemy, Attributes = new[] { 30000, 0, 3500, 1100, 450, 7, 10, 0, 6, 0, 3, 0 }, Vitals = new[] { 30000, 0, 3500 } }
            });
            var definitions = new BattleDefinitions("PAL3A", "native", Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(), new[]
            {
                new BattleActorDefinition(new BattleActorData { InstanceId = "A", Identity = 0 }),
                new BattleActorDefinition(new BattleActorData { InstanceId = "E", Boss = true })
            });
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new ReadyRules(), definitions);
            Assert.That(session.Actors.Single(actor => actor.Id == "E").MaxHp, Is.EqualTo(31000));
            Assert.That(session.Actors.Single(actor => actor.Id == "E").MaxMp, Is.EqualTo(4200));
            Assert.That(session.Pal3AState.PreserveLiaoRiLosses, Is.False);
            Invoke(session, "ApplyPal3ALiaoRiCarry", Actor(session, "E"));
            Assert.That(session.Actors.Single(actor => actor.Id == "E").MaxHp, Is.EqualTo(31000));
            Assert.That(scenario.Participants.Single(actor => actor.InstanceId == "E").Attributes[0], Is.EqualTo(30000));
        }

    }
}
