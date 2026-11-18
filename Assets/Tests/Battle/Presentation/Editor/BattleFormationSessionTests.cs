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
    using Battle;
    using Battle.Presentation;
    using NUnit.Framework;

    public sealed class BattleFormationSessionTests
    {
        private sealed class Catalog : IBattleCatalog, IBattleStageGeometry
        {
            private readonly Dictionary<string, BattleParticipant> _actors;
            public bool InvalidMove;

            public Catalog(BattleScenario scenario) => _actors = scenario.Participants.ToDictionary(actor => actor.InstanceId);

            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor switch { "A" => 0, "B" => 1, "C" => 4, _ => 0 };

            public int DamageDivisor(string actor) => 1;

            public BattleMovementPoint InitialHome(string actor) => FormationHome(actor, _actors[actor].Slot);

            public BattleMovementPoint FormationHome(string actor, BattleSlot slot) =>
                new((_actors[actor].Side == BattleSide.Enemy ? 1000 : 0) + 100 * (int)slot, 0, 0);

            public BattleAttackPlan PlanAttack(string source, string target, BattleMovementPoint sourceHome, BattleMovementPoint targetHome) =>
                new(Attack(source, target), sourceHome, targetHome, targetHome);

            public IReadOnlyList<BattleMovementPlan> PlanFormationMove(string actor, BattleMovementPoint home,
                BattleMovementPoint destination, bool midpoint) => Array.AsReadOnly(new[]
                {
                    new BattleMovementPlan(InvalidMove ? (BattleMovementKind)999 : BattleMovementKind.Run, home, destination)
                });
        }

        private sealed class Rules : IBattleRules
        {
            private readonly string _controller;
            private readonly BattleBasicRules _original = new();

            public Rules(string controller) => _controller = controller;

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) =>
                actor.InstanceId == _controller ? BattleSession.FullGauge : actor.Side == BattleSide.Ally ? 600 : 0;

            public int GaugePerStep(BattleParticipant actor) => _original.GaugePerStep(actor);

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) =>
                _original.Hit(source, target, guarding, random, targetAtHome, damageDivisor);
        }

        private static BattleParticipant Participant(string id, BattleSide side, BattleSlot slot) => new()
        {
            InstanceId = id,
            ActorId = side == BattleSide.Ally ? 101 : 701,
            Side = side,
            Slot = slot,
            Attributes = new[] { 500, 100, 100, 60, 10, 3, 0, 0, 0, 0, 0, 0 },
            Vitals = new[] { 500, 100, 100 }
        };

        private static BattleScenario Scenario(string variant, bool third = false)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "fixture", DataFingerprint = "fixture" };
            scenario.Participants.Add(Participant("A", BattleSide.Ally, BattleSlot.Water));
            scenario.Participants.Add(Participant("B", BattleSide.Ally, BattleSlot.Fire));
            if (third)
                scenario.Participants.Add(Participant("C", BattleSide.Ally, BattleSlot.Earth));
            scenario.Participants.Add(Participant("E", BattleSide.Enemy, BattleSlot.Water));
            return scenario;
        }

        private static BattleSession Session(BattleScenario scenario, out BattleSetup setup, out Catalog catalog,
            string controller = "A", bool invalid = false)
        {
            setup = new BattleSetup(scenario);
            catalog = new Catalog(scenario) { InvalidMove = invalid };
            var definitions = new BattleDefinitions(scenario.Variant, scenario.DataFingerprint,
                Array.Empty<BattleAbilityDefinition>(), Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.InstanceId switch { "A" => 0, "B" => 1, "C" => 4, _ => -1 },
                    NormalAttackMode = 924
                })));
            return new BattleSession(setup, catalog, new Rules(controller), definitions);
        }

        private static BattleActorSnapshot Actor(BattleSession session, string id) => session.Actors.Single(actor => actor.Id == id);

        private static string Trace(BattleSession session) => string.Join("\n", session.Events.Select(entry => entry.ToString()));

        [TestCase("PAL3", "A", false)]
        [TestCase("PAL3A", "A", false)]
        [TestCase("PAL3", "C", false)]
        [TestCase("PAL3A", "C", false)]
        [TestCase("PAL3", "A", true)]
        [TestCase("PAL3A", "A", true)]
        [TestCase("PAL3", "C", true)]
        [TestCase("PAL3A", "C", true)]
        public void ConfirmationMatchesNativeGaugeAndGuardWriteOwnership(string variant, string controller, bool restored)
        {
            BattleSession session = Session(Scenario(variant, true), out _, out _, controller);
            Assert.That(session.BeginFormation(controller), Is.True);
            // Native full-handler vectors use sentinels to detect unintended
            // writes. A normally readied controller already has Guard cleared
            // by the separate defense watcher; this checks ownership only.
            var actors = (IDictionary)typeof(BattleSession).GetField("_byId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            foreach (string id in new[] { "A", "B", "C" })
            {
                object actor = actors[id];
                actor.GetType().GetField("CastGauge").SetValue(actor, 1000 + id[0]);
                actor.GetType().GetField("Guard").SetValue(actor, true);
            }
            BattleActorSnapshot[] before = session.Actors.Where(actor => actor.Side == BattleSide.Ally).ToArray();
            Assert.That(session.SwapFormation("A", BattleSlot.Fire), Is.True);
            if (restored)
                Assert.That(session.SwapFormation("A", BattleSlot.Water), Is.True);
            Assert.That(session.EndFormation(controller), Is.True);
            foreach (BattleActorSnapshot original in before)
            {
                BattleActorSnapshot after = Actor(session, original.Id);
                bool pays = !restored && original.Id == controller;
                Assert.That((after.Gauge, after.CastGauge, after.Ready),
                    Is.EqualTo(pays ? (0, 0, false) : (original.Gauge, original.CastGauge, original.Ready)), original.Id);
                Assert.That(after.Guarding, Is.True, original.Id);
            }
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void FormationMovementDoesNotPauseEitherGaugeAndReplayCapturesRuntimeSlots(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            BattleSession session = Session(scenario, out BattleSetup setup, out Catalog catalog);
            Assert.That(session.BeginFormation("A"), Is.True);
            Assert.That(session.SwapFormation("A", BattleSlot.Fire), Is.True);
            Assert.That((Actor(session, "A").Slot, Actor(session, "B").Slot), Is.EqualTo((BattleSlot.Fire, BattleSlot.Water)));
            Assert.That((Actor(session, "A").Gauge, Actor(session, "B").Gauge), Is.EqualTo((11500, 600)));
            Assert.That(session.EndFormation("A"), Is.True);
            session.Step();
            Assert.That((Actor(session, "A").Gauge, Actor(session, "B").Gauge), Is.EqualTo((35, 635)));
            Assert.That(Actor(session, "A").Movement.Position.X, Is.EqualTo(7.6f).Within(0.00001f));
            for (int step = 1; step < 13; step++)
                session.Step();
            Assert.That(Actor(session, "A").Movement, Is.Null);
            Assert.That(Actor(session, "B").Movement, Is.Null);
            Assert.That((Actor(session, "A").Home.X, Actor(session, "B").Home.X), Is.EqualTo((100f, 0f)));
            BattleSession replay = BattleReplay.Play(setup, catalog, session.Inputs, session.Tick, new Rules("A"), session.Definitions);
            Assert.That(Trace(replay), Is.EqualTo(Trace(session)));
            Assert.That((Actor(replay, "A").Slot, Actor(replay, "A").Gauge), Is.EqualTo((Actor(session, "A").Slot, Actor(session, "A").Gauge)));
            Assert.That(scenario.Participants[0].Slot, Is.EqualTo(BattleSlot.Water));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void OnlyTheControllerPaysEvenWhenTwoOtherMembersSwap(string variant)
        {
            BattleSession session = Session(Scenario(variant, true), out _, out _, "C");
            Assert.That(session.BeginFormation("C"), Is.True);
            Assert.That(session.SwapFormation("A", BattleSlot.Fire), Is.True);
            Assert.That(session.EndFormation("C"), Is.True);
            Assert.That((Actor(session, "A").Gauge, Actor(session, "B").Gauge, Actor(session, "C").Gauge), Is.EqualTo((600, 600, 0)));
            session.Step();
            Assert.That((Actor(session, "A").Gauge, Actor(session, "B").Gauge, Actor(session, "C").Gauge), Is.EqualTo((635, 635, 35)));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void RestoringTheEntryLineupClearsQueuedMovementAndHasNoGaugeCost(string variant)
        {
            BattleSession session = Session(Scenario(variant), out _, out _);
            Assert.That(session.BeginFormation("A"), Is.True);
            Assert.That(session.SwapFormation("A", BattleSlot.Fire), Is.True);
            Assert.That(session.SwapFormation("A", BattleSlot.Water), Is.True);
            Assert.That(session.EndFormation("A"), Is.True);
            Assert.That(Actor(session, "A").Gauge, Is.EqualTo(11500));
            Assert.That(Actor(session, "A").Movement, Is.Null);
            Assert.That(Actor(session, "B").Movement, Is.Null);
            Assert.That((Actor(session, "A").Slot, Actor(session, "A").Home.X, Actor(session, "A").Gauge), Is.EqualTo((BattleSlot.Water, 0f, 11500)));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void OpenFormationFreezesGaugesAndRejectsCombatCommands(string variant)
        {
            BattleSession session = Session(Scenario(variant), out _, out _);
            Assert.That(session.BeginFormation("A"), Is.True);
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.False);
            for (int step = 0; step < 5; step++)
                session.Step();
            Assert.That((Actor(session, "A").Gauge, Actor(session, "B").Gauge, Actor(session, "E").Gauge), Is.EqualTo((11500, 600, 0)));
            Assert.That(session.EndFormation("A"), Is.True);
            Assert.That(Actor(session, "A").Ready, Is.True);
        }

        [Test]
        public void InvalidProviderPlanLeavesBothActorsAndTheInputLogUntouched()
        {
            BattleSession session = Session(Scenario("PAL3A"), out _, out _, invalid: true);
            Assert.That(session.BeginFormation("A"), Is.True);
            string trace = Trace(session);
            Assert.Throws<ArgumentException>(() => session.SwapFormation("A", BattleSlot.Fire));
            Assert.That(Trace(session), Is.EqualTo(trace));
            Assert.That(session.Inputs.Count, Is.EqualTo(1));
            Assert.That((Actor(session, "A").Slot, Actor(session, "B").Slot), Is.EqualTo((BattleSlot.Water, BattleSlot.Fire)));
            Assert.That(Actor(session, "A").Movement, Is.Null);
        }

        [TestCase("PAL3", "cancel")]
        [TestCase("PAL3A", "cancel")]
        [TestCase("PAL3", "same-slot")]
        [TestCase("PAL3A", "same-slot")]
        [TestCase("PAL3", "blank")]
        [TestCase("PAL3A", "blank")]
        [TestCase("PAL3", "same-actor")]
        [TestCase("PAL3A", "same-actor")]
        public void FinishedFormationDragsReleaseTheirActorWithoutChangingTheBattle(string variant, string release)
        {
            BattleSession session = Session(Scenario(variant), out _, out _);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            Assert.That(presenter.BeginFormation(), Is.True);
            string trace = Trace(session);
            int inputs = session.Inputs.Count;
            int gauge = Actor(session, "A").Gauge;
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerDown, "actor:A", 17)), Is.True);
            if (release == "cancel")
                Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerCancel, pointerId: 17)), Is.True);
            else
                presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerUp,
                    release == "same-slot" ? "slot:" + (int)BattleSlot.Water : release == "same-actor" ? "actor:A" : null, 17));
            Assert.That(presenter.FormationActor, Is.Null);
            Assert.That(presenter.CapturedPointerId, Is.Null);
            Assert.That(presenter.FocusItems().Where(item => item.Id.StartsWith("actor:", StringComparison.Ordinal)).All(item => item.Enabled), Is.True);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerDown, "actor:B", 22)), Is.True);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerCancel, pointerId: 22)), Is.True);
            Assert.That(Trace(session), Is.EqualTo(trace));
            Assert.That(session.Inputs.Count, Is.EqualTo(inputs));
            Assert.That(Actor(session, "A").Gauge, Is.EqualTo(gauge));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void KeyboardFormationSelectionKeepsItsActorUntilTheSlotIsConfirmed(string variant)
        {
            BattleSession session = Session(Scenario(variant), out _, out _);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            Assert.That(presenter.BeginFormation(), Is.True);
            int gauge = Actor(session, "A").Gauge;
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "actor:B")), Is.True);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Down)), Is.True);
            Assert.That(presenter.FormationActor, Is.EqualTo("B"));
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "slot:" + (int)BattleSlot.Earth)), Is.True);
            Assert.That(Actor(session, "B").Slot, Is.EqualTo(BattleSlot.Earth));
            Assert.That(presenter.FormationActor, Is.Null);
            Assert.That(session.Inputs.Count(input => input.Kind == BattleInputKind.SwapFormation), Is.EqualTo(1));
            Assert.That(Actor(session, "A").Gauge, Is.EqualTo(gauge));
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Back)), Is.True);
            Assert.That(session.ActiveFormationController, Is.Null);
            Assert.That(Actor(session, "A").Gauge, Is.Zero);
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        public void FormationBackDeselectsButExplicitConfirmationClosesAndPaysOnlyOnce(string variant, bool changed)
        {
            BattleSession session = Session(Scenario(variant), out _, out _);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            Assert.That(presenter.BeginFormation(), Is.True);
            int gauge = Actor(session, "A").Gauge;
            if (changed)
                Assert.That(presenter.MoveFormation("A", BattleSlot.Earth), Is.True);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "actor:B")), Is.True);
            Assert.That(presenter.FormationActor, Is.EqualTo("B"));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Back));
            Assert.That(presenter.FormationActor, Is.Null);
            Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Formation));
            Assert.That(session.ActiveFormationController, Is.EqualTo("A"));
            Assert.That(session.Inputs.Count(input => input.Kind == BattleInputKind.EndFormation), Is.Zero);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "actor:B")), Is.True);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "end")), Is.True);
            Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            Assert.That(session.ActiveFormationController, Is.Null);
            Assert.That(Actor(session, "A").Gauge, Is.EqualTo(changed ? 0 : gauge));
            Assert.That(session.Inputs.Count(input => input.Kind == BattleInputKind.EndFormation), Is.EqualTo(1));
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "end")), Is.False);
            Assert.That(session.Inputs.Count(input => input.Kind == BattleInputKind.EndFormation), Is.EqualTo(1));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ADeadOccupantPreventsExchangeWithoutBlockingOpeningThePanel(string variant)
        {
            BattleScenario scenario = Scenario(variant);
            scenario.Participants[1].Vitals[0] = 0;
            BattleSession session = Session(scenario, out _, out _);
            Assert.That(session.BeginFormation("A"), Is.True);
            Assert.That(session.CanSwapFormation("A", BattleSlot.Fire), Is.Not.Null);
            Assert.That(session.SwapFormation("A", BattleSlot.Fire), Is.False);
            Assert.That(session.Inputs.Count, Is.EqualTo(1));
        }
    }
}
