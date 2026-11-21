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
    using Battle.Presentation;
    using NUnit.Framework;

    // No editor windows or native focus are needed for these routing checks.
    public sealed class BattleGamepadWheelTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;

        private sealed class InputRoute
        {
            public readonly BattleSession Battle;
            public readonly BattleHudView Hud = new();
            public readonly BattleEditorGamepadState Policy;
            public readonly List<BattleEditorGamepadIntent> Events = new();
            private double _time;

            public InputRoute(string variant = "PAL3", BattleEditorGamepadSnapshot initial = default, BattleSession battle = null)
            {
                Battle = battle ?? BattleHudTests.Session(variant);
                Hud.TakeGamepadInput(Battle);
                typeof(BattleHudView).GetField("_inputEnabled", Members).SetValue(Hud, true);
                Policy = new BattleEditorGamepadState(initial);
                Hud.KeyboardTookControl += Policy.Suspend;
            }

            public void Sample(BattleEditorGamepadSnapshot snapshot, bool receive = true) => Policy.Sample(snapshot, receive);

            public void Flush()
            {
                Policy.Advance(_time += .01, true, input =>
                {
                    Events.Add(input);
                    var before = (Hud.Presenter.Page, Hud.Presenter.Category, Hud.Presenter.FormationActor);
                    Hud.TakeGamepadInput(Battle);
                    Hud.SendGamepadIntent(input);
                    if (before != (Hud.Presenter.Page, Hud.Presenter.Category, Hud.Presenter.FormationActor))
                        Policy.ResetNavigation();
                });
            }
        }

        private sealed class SwordCatalog : IBattleCatalog, IBattleAbilityTimings, IBattleMagicSwordTimings
        {
            public AttackTiming Attack(string source, string target) => new(0, 5, new[] { 2 });

            public AttackTiming Ability(string actor, BattleAbilityDefinition ability) => Attack(actor, null);

            public BattleMagicSwordStageProfile MagicSword(string actor, BattleAbilityDefinition ability) => new(438, -1, 0, 0, 5);

            public int GuardEntry(string actor) => 1;

            public int Hurt(string actor) => 0;

            public int SpImpact(string actor) => 0;

            public int Priority(string actor) => actor == "A" ? 0 : 1;

            public int DamageDivisor(string actor) => 1;
        }

        private sealed class SwordRules : IBattleRules
        {
            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) =>
                actor.Side == BattleSide.Ally ? BattleSession.FullGauge : 0;

            public int GaugePerStep(BattleParticipant actor) => 1;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => new(-1, false, false);
        }

        private sealed class SwordDirector : IBattleEffectDirector
        {
            public long Start(BattleEffectRequest request) => 1;

            public bool IsRunning(long handle) => false;

            public void Advance(long frame, float scaledSeconds, float unscaledSeconds)
            {
            }

            public void Stop(long handle)
            {
            }
        }

        private static BattleSession ChargedSwordBattle()
        {
            var scenario = new BattleScenario { Variant = "PAL3", Scene = "wheel-sword", DataFingerprint = "wheel-sword" };
            foreach ((string id, BattleSide side) in new[] { ("A", BattleSide.Ally), ("E", BattleSide.Enemy) })
                scenario.Participants.Add(new BattleParticipant
                {
                    InstanceId = id,
                    ActorId = side == BattleSide.Ally ? 101 : 701,
                    Side = side,
                    Slot = BattleSlot.Water,
                    Attributes = new[] { 500, 100, 100, 100, 0, 3, 0, 0, 0, 0, 0, 0 },
                    Vitals = new[] { 500, 100, 100 }
                });
            scenario.Participants[0].Equipment[0] = BattleMagicSwordRules.WeaponId;
            scenario.Participants[0].MagicSword = new BattleMagicSword { Energy = 200, SkillId = 2224 };
            var skill = new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = 2224,
                Name = "charged sword",
                SkillTypeId = 4,
                Range = BattleTargetRange.OpponentAll,
                ApplicableIdentities = new[] { 1, 2, 3, 4 },
                SuccessRate = 0
            });
            var definitions = new BattleDefinitions("PAL3", scenario.DataFingerprint, new[] { skill }, Array.Empty<BattleAbilityDefinition>(),
                scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
                {
                    InstanceId = actor.InstanceId,
                    Identity = actor.Side == BattleSide.Ally ? 0 : -1,
                    MagicSword = actor.MagicSword?.Capture(),
                    Equipment = actor.MagicSword == null ? Array.Empty<BattleEquipmentDefinition>() : new[]
                    {
                        new BattleEquipmentDefinition(new BattleEquipmentData { Id = BattleMagicSwordRules.WeaponId, Slot = 0 })
                    }
                })));
            var battle = new BattleSession(new BattleSetup(scenario), new SwordCatalog(), new SwordRules(), definitions, new SwordDirector());
            battle.SetAutomatic(BattleSide.Enemy, false);
            battle.Step();
            return battle;
        }

        [TestCase(1, 0, "defend")]
        [TestCase(1, 1, "protection")]
        [TestCase(0, 1, "skills")]
        [TestCase(-1, 1, "ai")]
        [TestCase(-1, 0, "magic")]
        [TestCase(-1, -1, "formation")]
        [TestCase(0, -1, "items")]
        [TestCase(1, -1, "flee")]
        public void CurrentDpadAndStickDirectionSelectEachSectorAndNeutralReturnsToAttack(int x, int y, string expected)
        {
            foreach (string variant in new[] { "PAL3", "PAL3A" })
                foreach (bool dpad in new[] { false, true })
                {
                    var route = new InputRoute(variant);
                    int inputs = route.Battle.Inputs.Count;
                    route.Sample(new BattleEditorGamepadSnapshot(dpadX: -1));
                    route.Flush();
                    route.Sample(dpad ? new BattleEditorGamepadSnapshot(dpadX: x, dpadY: y) : new BattleEditorGamepadSnapshot(x, y));
                    route.Flush();
                    Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo(expected), "Select from the wheel center, independently of the previous selection.");
                    route.Sample(default);
                    route.Flush();
                    Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("attack"));
                    Assert.That(route.Events.Last().Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Point));
                    Assert.That(route.Events.Last().PointingDirection, Is.EqualTo(BattleHudDirection.Neutral));
                    Assert.That(route.Battle.Inputs.Count, Is.EqualTo(inputs));
                }
        }

        [Test]
        public void NeutralAndConfirmInOneUpdateUseTheCurrentCenterBeforeConfirming()
        {
            foreach (bool sameEvent in new[] { false, true })
            {
                var route = new InputRoute();
                route.Sample(new BattleEditorGamepadSnapshot(dpadY: 1));
                route.Flush();
                Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("skills"));
                if (!sameEvent)
                    route.Sample(default);
                route.Sample(new BattleEditorGamepadSnapshot(buttons: BattleEditorGamepadButtons.Confirm));
                route.Flush();
                Assert.That(route.Hud.Presenter.Page, Is.EqualTo(BattleHudPage.Targets));
                Assert.That(route.Hud.Presenter.Command.Action, Is.EqualTo(BattleAction.Attack));
                Assert.That(route.Events.Last().PointingDirection, Is.EqualTo(BattleHudDirection.Neutral));
            }
        }

        [Test]
        public void ConfirmKeepsItsEventDirectionBeforeALaterNativeRelease()
        {
            var route = new InputRoute();
            route.Sample(new BattleEditorGamepadSnapshot(0, 1, buttons: BattleEditorGamepadButtons.Confirm));
            route.Sample(default);
            route.Flush();
            Assert.That(route.Hud.Presenter.Page, Is.EqualTo(BattleHudPage.Skills));
            Assert.That(route.Events.Single(input => input.Kind == BattleEditorGamepadIntentKind.Confirm).PointingDirection,
                Is.EqualTo(BattleHudDirection.Up), "Use the Confirm event's position, not the later final device state.");
        }

        [Test]
        public void ConfirmWithoutADeviceSnapshotPreservesKeyboardAndMouseSelection()
        {
            var route = new InputRoute();
            route.Hud.Presenter.SetFocus("skills");
            route.Hud.SendGamepadIntent(new BattleEditorGamepadIntent(BattleEditorGamepadIntentKind.Confirm));
            Assert.That(route.Hud.Presenter.Page, Is.EqualTo(BattleHudPage.Skills));
            route.Hud.Presenter.Back();
            route.Hud.Presenter.SetFocus("magic");
            route.Hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Confirm));
            Assert.That(route.Hud.Presenter.Page, Is.EqualTo(BattleHudPage.Magic));
        }

        [Test]
        public void PageResetStillTracksHeldPositionWithoutMovingTheListAndBackRestoresItImmediately()
        {
            var route = new InputRoute();
            route.Sample(new BattleEditorGamepadSnapshot(0, 1, buttons: BattleEditorGamepadButtons.Confirm));
            route.Flush();
            Assert.That(route.Hud.Presenter.Page, Is.EqualTo(BattleHudPage.Skills));
            string focus = route.Hud.Presenter.FocusedId;
            route.Sample(new BattleEditorGamepadSnapshot(1, 0));
            route.Flush();
            Assert.That(route.Events.Last().Kind, Is.EqualTo(BattleEditorGamepadIntentKind.Point));
            Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo(focus));
            route.Sample(new BattleEditorGamepadSnapshot(1, 0, buttons: BattleEditorGamepadButtons.Cancel));
            route.Flush();
            Assert.That(route.Hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("defend"), "Returning to Commands shows the still-held current position before a repeat or Confirm.");
        }

        [TestCase(BattleHudPage.Skills)]
        [TestCase(BattleHudPage.Magic)]
        [TestCase(BattleHudPage.Items)]
        [TestCase(BattleHudPage.Targets)]
        public void NeutralDoesNotStepListsTargetsOrFormation(BattleHudPage page)
        {
            var route = new InputRoute();
            if (page == BattleHudPage.Targets)
                route.Hud.Presenter.ChooseAction(BattleAction.Attack);
            else
                route.Hud.Presenter.Open(page);
            route.Sample(new BattleEditorGamepadSnapshot(dpadY: -1));
            route.Flush();
            string focus = route.Hud.Presenter.FocusedId;
            string target = route.Hud.Presenter.Target;
            int inputs = route.Battle.Inputs.Count;
            route.Sample(default);
            route.Flush();
            Assert.That(route.Hud.Presenter.Page, Is.EqualTo(page));
            Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo(focus));
            Assert.That(route.Hud.Presenter.Target, Is.EqualTo(target));
            Assert.That(route.Battle.Inputs.Count, Is.EqualTo(inputs));
        }

        [Test]
        public void NeutralHasNoNavigationMeaningOnTheFormationPanel()
        {
            var route = new InputRoute();
            route.Hud.Presenter.Open(BattleHudPage.Formation);
            string focus = route.Hud.Presenter.FocusedId;
            int inputs = route.Battle.Inputs.Count;
            Assert.That(route.Hud.Presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Neutral)), Is.False);
            Assert.That(route.Hud.Presenter.Page, Is.EqualTo(BattleHudPage.Formation));
            Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo(focus));
            Assert.That(route.Battle.Inputs.Count, Is.EqualTo(inputs));
        }

        [Test]
        public void DpadReleaseFallsBackToHeldStickBeforeReturningToCenter()
        {
            var route = new InputRoute();
            route.Sample(new BattleEditorGamepadSnapshot(1, 0, dpadY: 1));
            route.Flush();
            Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("skills"));
            route.Sample(new BattleEditorGamepadSnapshot(1, 0));
            route.Flush();
            Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("defend"));
            route.Sample(default);
            route.Flush();
            Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("attack"));
        }

        [Test]
        public void FocusLossDoesNotReplayHeldPositionAndReleaseAfterPointerTakeoverDoesNotStealFocus()
        {
            var route = new InputRoute();
            route.Sample(new BattleEditorGamepadSnapshot(dpadY: 1));
            route.Flush();
            route.Policy.Suspend();
            route.Hud.SetInputFocus(false);
            route.Sample(new BattleEditorGamepadSnapshot(dpadX: 1), false);
            route.Hud.SetInputFocus(true);
            route.Events.Clear();
            route.Sample(new BattleEditorGamepadSnapshot(dpadX: 1));
            route.Flush();
            Assert.That(route.Events, Is.Empty);
            route.Hud.Presenter.SetFocus("magic");
            typeof(BattleHudView).GetField("_navigationInput", Members).SetValue(route.Hud, false);
            route.Policy.Suspend();
            route.Sample(default);
            route.Flush();
            Assert.That(route.Events, Is.Empty, "A release after mouse takeover cannot become a fresh gamepad selection.");
            Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("magic"));
        }

        [Test]
        public void SynchronizeRestoresCurrentWheelPositionWhenACommandsPageReopensWithoutAnInputEvent()
        {
            var route = new InputRoute();
            route.Sample(new BattleEditorGamepadSnapshot(dpadX: -1));
            route.Flush();
            route.Hud.Presenter.Open(BattleHudPage.Skills);
            // Record the non-command page before the next synchronization.
            typeof(BattleHudView).GetMethod("Synchronize", Members).Invoke(route.Hud, new object[] { route.Battle });
            route.Hud.Presenter.Back();
            typeof(BattleHudView).GetMethod("Synchronize", Members).Invoke(route.Hud, new object[] { route.Battle });
            Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("magic"));
        }

        [Test]
        public void KeyboardTakeoverSuspendsHeldAndAlreadyQueuedGamepadReleaseUntilAFreshGesture()
        {
            foreach (bool releaseWasQueued in new[] { false, true })
            {
                var route = new InputRoute();
                route.Sample(new BattleEditorGamepadSnapshot(0, 1));
                route.Flush();
                Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("skills"));
                if (releaseWasQueued)
                    route.Sample(default);
                route.Hud.TakeNavigationInput();
                route.Hud.SendIntent(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Left));
                if (!releaseWasQueued)
                    route.Sample(default);
                route.Events.Clear();
                route.Flush();
                Assert.That(route.Events, Is.Empty);
                Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("magic"), "The released old gamepad gesture cannot replace a keyboard selection.");
                route.Sample(new BattleEditorGamepadSnapshot(1, 0));
                route.Flush();
                Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("defend"), "A new physical gamepad gesture can deliberately take control again.");
            }
        }

        [Test]
        public void PageResetRetainsTheCurrentStickSectorUntilTheRealExitThreshold()
        {
            var route = new InputRoute();
            route.Sample(new BattleEditorGamepadSnapshot(0, 1));
            route.Flush();
            route.Sample(new BattleEditorGamepadSnapshot(0, .4f));
            route.Flush();
            route.Policy.ResetNavigation();
            route.Hud.Presenter.Open(BattleHudPage.Skills);
            route.Sample(new BattleEditorGamepadSnapshot(0, .4f, buttons: BattleEditorGamepadButtons.Cancel));
            route.Flush();
            Assert.That(route.Hud.Presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("skills"));
            Assert.That(route.Events.Last().PointingDirection, Is.EqualTo(BattleHudDirection.Up));
            route.Sample(new BattleEditorGamepadSnapshot(0, .34f));
            route.Flush();
            Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("attack"));
        }

        [Test]
        public void ShoulderNavigationSurvivesRepaintAndNeutralConfirmExecutesTheChargedSword()
        {
            var route = new InputRoute(battle: ChargedSwordBattle());
            Assert.That(route.Hud.Presenter.FocusItems().Single(item => item.Id == "sword").Enabled, Is.True, route.Hud.Presenter.Reason(BattleAction.Skill, 2224));
            for (int attempt = 0; attempt < 12 && route.Hud.Presenter.FocusedId != "sword"; attempt++)
            {
                route.Sample(new BattleEditorGamepadSnapshot(buttons: BattleEditorGamepadButtons.NextSection));
                route.Flush();
                string selected = route.Hud.Presenter.FocusedId;
                typeof(BattleHudView).GetMethod("Synchronize", Members).Invoke(route.Hud, new object[] { route.Battle });
                Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo(selected), "An ordinary repaint cannot snap a shoulder selection back to the wheel center.");
                route.Sample(default);
                route.Flush();
            }
            Assert.That(route.Hud.Presenter.FocusedId, Is.EqualTo("sword"));
            int before = route.Battle.Inputs.Count;
            route.Sample(new BattleEditorGamepadSnapshot(buttons: BattleEditorGamepadButtons.Confirm));
            route.Flush();
            Assert.That(route.Battle.Inputs.Count, Is.EqualTo(before + 1));
            Assert.That(route.Battle.Inputs.Last().Kind, Is.EqualTo(BattleInputKind.Command));
            Assert.That(route.Battle.Inputs.Last().Action, Is.EqualTo(BattleAction.Skill));
            Assert.That(route.Battle.Inputs.Last().AbilityId, Is.EqualTo(2224));
        }
    }
}
