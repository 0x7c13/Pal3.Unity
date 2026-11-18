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
    using UnityEngine;

    public sealed class BattleHudTests
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
            public int GaugeRate = 1;

            public int InitialGauge(BattleParticipant actor, BattleEncounter encounter, BattleRandom random) => BattleSession.FullGauge;

            public int GaugePerStep(BattleParticipant actor) => GaugeRate;

            public PhysicalHit Hit(BattleParticipant source, BattleParticipant target, bool guarding, BattleRandom random,
                bool targetAtHome = true, int damageDivisor = 1) => new BattleBasicRules().Hit(source, target, guarding, random, targetAtHome, damageDivisor);
        }

        internal static BattleSession Session(string variant = "PAL3", BattleTargetRange normalRange = BattleTargetRange.OpponentSingle, Action<BattleScenario> configure = null, BattleState? skillState = null, int gaugeRate = 1, bool withDefinitions = true)
        {
            var scenario = new BattleScenario { Variant = variant, Scene = "hud", SceneElement = 1, DataFingerprint = "hud", Money = 100 };
            foreach ((string id, BattleSide side, BattleSlot slot, int hp) in new[]
            {
                ("A", BattleSide.Ally, BattleSlot.Water, 100), ("B", BattleSide.Ally, BattleSlot.Fire, 0),
                ("E", BattleSide.Enemy, BattleSlot.Water, 100), ("E2", BattleSide.Enemy, BattleSlot.Wind, 100),
                ("E3", BattleSide.Enemy, BattleSlot.Fire, 100)
            })
            {
                var actor = new BattleParticipant
                {
                    InstanceId = id,
                    ActorId = side == BattleSide.Ally ? id == "A" ? 101 : 104 : 701,
                    Side = side,
                    Slot = slot,
                    Attributes = new[] { 100, 100, 100, 20, 10, 3, 0, 0, 0, 0, 0, 0 },
                    Vitals = new[] { hp, 100, 100 }
                };
                actor.Skills.Add(new BattleSkill { Id = 301, Level = 4, Mastered = true });
                actor.Skills.Add(new BattleSkill { Id = 302, Level = 4, Mastered = true });
                actor.Skills.Add(new BattleSkill { Id = 401, Level = 4, Mastered = true });
                scenario.Participants.Add(actor);
            }
            scenario.Items.Add(new BattleItem { Id = 6201, Count = 3 });
            configure?.Invoke(scenario);
            var definitions = new BattleDefinitions(variant, "hud", new[]
            {
                new BattleAbilityDefinition(new BattleAbilityData { Id = 301, Name = "水术", IsMagic = true, ElementMask = 1,
                    Range = BattleTargetRange.OpponentRow, SkillTypeId = 2,
                    ApplicableIdentities = new[] { 0 }, Costs = new[] { new BattleCost(2, BattleValueMode.Absolute, 10) } }),
                new BattleAbilityDefinition(new BattleAbilityData { Id = 302, Name = "群体火术", IsMagic = true, ElementMask = 2,
                    Range = BattleTargetRange.OpponentAll, SkillTypeId = 2, ApplicableIdentities = new[] { 0 } }),
                new BattleAbilityDefinition(new BattleAbilityData { Id = 401, Name = "特技", Range = BattleTargetRange.OpponentSingle,
                    SkillTypeId = 0, ApplicableIdentities = new[] { 0 },
                    States = skillState.HasValue ? new[] { new BattleStatusEffect(skillState.Value, false) } : Array.Empty<BattleStatusEffect>() })
            }, new[]
            {
                new BattleAbilityDefinition(new BattleAbilityData { Id = 6201, Name = "还魂", IsItem = true, ItemType = 1,
                    Range = BattleTargetRange.OwnSingle, States = new[] { new BattleStatusEffect(BattleState.Death, true) } })
            }, scenario.Participants.Select(actor => new BattleActorDefinition(new BattleActorData
            {
                InstanceId = actor.InstanceId,
                Identity = actor.Side == BattleSide.Ally ? actor.InstanceId == "A" ? 0 : 1 : -1,
                NormalAttackRange = normalRange
            })));
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new Rules { GaugeRate = gaugeRate }, withDefinitions ? definitions : null);
            session.SetAutomatic(BattleSide.Enemy, false);
            session.Step();
            return session;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CommandRingDisablesUnlearnedAbilitiesAndToleratesMissingDefinitions(bool withDefinitions)
        {
            BattleSession session = Session(BattleData.Variant, configure: scenario =>
            {
                foreach (BattleSkill skill in scenario.Participants[0].Skills)
                    skill.Mastered = false;
            }, withDefinitions: withDefinitions);
            var hud = new BattleHudPresenter();
            hud.Synchronize(session);
            var focus = hud.FocusItems().ToDictionary(item => item.Id);
            Assert.That(focus["attack"].Enabled, Is.True);
            Assert.That(focus["skills"].Enabled, Is.False);
            Assert.That(focus["magic"].Enabled, Is.False);
            Assert.That(focus["items"].Enabled, Is.EqualTo(withDefinitions));
            Assert.That(focus["ai"].Enabled, Is.EqualTo(withDefinitions));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ViewCanBindADefinitionFreeSession(string variant)
        {
            BattleSession session = Session(variant, withDefinitions: false);
            var view = new BattleHudView();
            view.BindBattle(session, null, null, null, null);
            Assert.That(view.Presenter.Controller, Is.EqualTo("A"));
            Assert.That(BattleHudView.ElementWheelVisible(session, view.Presenter), Is.False);
            var layout = (BattleHudLayout)typeof(BattleHudView).GetField("_layout", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            Assert.That(layout.IsPal3A, Is.EqualTo(variant == "PAL3A"));
            view.Release();
        }

        [TestCase(BattleHudPage.Items, BattleState.Forbidden, false)]
        [TestCase(BattleHudPage.Items, BattleState.Forbidden, true)]
        [TestCase(BattleHudPage.Magic, BattleState.Seal, false)]
        [TestCase(BattleHudPage.Magic, BattleState.Seal, true)]
        [TestCase(BattleHudPage.Commands, BattleState.Seal, true)]
        [TestCase(BattleHudPage.Commands, BattleState.Forbidden, true)]
        public void IncomingStateClosesItsListAndCancelsPendingTargetInput(BattleHudPage page, BattleState state, bool choose)
        {
            bool item = state == BattleState.Forbidden;
            BattleSession session = Session(BattleData.Variant, skillState: state);
            session.SetAutomatic(BattleSide.Enemy, true);
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Skill, "A", 401)), Is.True);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            presenter.Open(page);
            if (choose)
                Assert.That(presenter.ChooseAction(item ? BattleAction.Item : BattleAction.Skill, item ? 6201 : 301), Is.True);
            string control = choose ? "target:" + presenter.Target : "category:0";
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerDown, control, 37)), Is.True);
            for (int tick = 0; tick < 30 && presenter.Page != BattleHudPage.Commands; tick++)
            {
                session.Step();
                presenter.Synchronize(session);
            }
            Assert.That(session.Actors.Single(actor => actor.Id == "A").States.Any(value => value.State == state), Is.True);
            Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            Assert.That(presenter.CapturedPointerId, Is.Null);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerUp, control, 37)), Is.False);
            Assert.That(session.WaitingActor, Is.EqualTo("A"));
            Assert.That(session.Inputs.Any(input => input.Actor == "A" && input.Kind == BattleInputKind.Command), Is.False);
        }

        [Test]
        public void UnrelatedStateAndInsufficientMpKeepTheMagicListOpen()
        {
            BattleSession session = Session(BattleData.Variant, configure: scenario => scenario.Participants[0].Vitals[2] = 0,
                skillState: BattleState.Forbidden);
            session.SetAutomatic(BattleSide.Enemy, true);
            Assert.That(session.Submit(new BattleCommand("E", BattleAction.Skill, "A", 401)), Is.True);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            presenter.Open(BattleHudPage.Magic);
            for (int tick = 0; tick < 5; tick++)
            {
                session.Step();
                presenter.Synchronize(session);
            }
            Assert.That(session.Actors.Single(actor => actor.Id == "A").States.Any(value => value.State == BattleState.Forbidden), Is.True);
            Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Magic));
            Assert.That(presenter.Reason(BattleAction.Skill, 301), Does.Contain("Resource"));
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

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void BrowsingAndPotentialAreaQueriesAreCompletelyReadOnly(string variant)
        {
            BattleSession session = Session(variant);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            string before = State(session);
            for (int index = 0; index < 20; index++)
            {
                presenter.Open(BattleHudPage.Magic);
                Assert.That(presenter.Abilities().Select(row => row.Definition.Id), Is.EqualTo(new[] { 301 }));
                Assert.That(presenter.ChooseAbility(301), Is.True);
                Assert.That(presenter.HoverTarget("E"), Is.True);
                Assert.That(presenter.Affected.Select(actor => actor.Id), Is.EqualTo(new[] { "E", "E2" }));
                Assert.That(presenter.Costs.Count, Is.EqualTo(1));
                presenter.Back();
                Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Magic));
                presenter.Back();
            }
            Assert.That(State(session), Is.EqualTo(before));
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3", true)]
        public void TargetingStaysConsistentWhenAQueuedAttackKillsTheSelectedEnemy(string variant, bool hideSurvivors)
        {
            BattleSession session = Session(variant, configure: scenario =>
            {
                scenario.Participants.Single(actor => actor.InstanceId == "B").Vitals[0] = 100;
                scenario.Participants.Single(actor => actor.InstanceId == "E").Vitals[0] = 1;
                if (hideSurvivors)
                    foreach (BattleParticipant actor in scenario.Participants.Where(actor => actor.Side == BattleSide.Enemy && actor.InstanceId != "E"))
                        actor.Statuses.Add(new BattleStatus { Id = (int)BattleState.Invisible, Rounds = 5 });
            });
            Assert.That(session.Submit("A", BattleAction.Attack, "E"), Is.True);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            Assert.That(presenter.Controller, Is.EqualTo("B"));
            Assert.That(presenter.ChooseAction(BattleAction.Attack), Is.True);
            Assert.That(presenter.HoverTarget("E"), Is.True);
            for (int i = 0; i < 120 && session.Actors.Single(actor => actor.Id == "E").Alive; i++)
                session.Step();
            Assert.That(session.Actors.Single(actor => actor.Id == "E").Alive, Is.False);
            presenter.Synchronize(session);
            if (hideSurvivors)
            {
                Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Commands));
                Assert.That(session.Inputs.Count(input => input.Kind == BattleInputKind.Command), Is.EqualTo(1));
                return;
            }
            Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Targets));
            string survivor = presenter.Target;
            Assert.That(presenter.Targets.Any(actor => actor.Id == survivor && actor.Alive), Is.True);
            Assert.That(BattleHudView.PointerActors(session, presenter), Is.EqualTo(new[] { survivor }));
            Assert.That(presenter.FocusedId, Is.EqualTo("target:" + survivor));
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm)), Is.True);
            Assert.That(session.Inputs.Last(input => input.Kind == BattleInputKind.Command).Target, Is.EqualTo(survivor));
        }

        [Test]
        public void RevivalTargetRecoveringRemainsSelectableWithoutSpendingAnotherItem()
        {
            BattleSession session = Session(configure: scenario =>
            {
                BattleParticipant caster = scenario.Participants[0].Copy();
                caster.InstanceId = "C";
                caster.ActorId = 105;
                caster.Slot = BattleSlot.Thunder;
                scenario.Participants.Insert(1, caster);
            });
            Assert.That(session.Submit(new BattleCommand("A", BattleAction.Item, "B", 6201)), Is.True);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            Assert.That(presenter.Controller, Is.EqualTo("C"));
            presenter.Open(BattleHudPage.Items);
            Assert.That(presenter.ChooseAbility(6201), Is.True);
            for (int i = 0; i < 120 && !session.Actors.Single(actor => actor.Id == "B").Alive; i++)
                session.Step();
            Assert.That(session.Actors.Single(actor => actor.Id == "B").Alive, Is.True);
            presenter.Synchronize(session);
            Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Targets));
            Assert.That(presenter.Target, Is.EqualTo("B"));
            Assert.That(session.Inputs.Count(input => input.Kind == BattleInputKind.Command), Is.EqualTo(1));
        }

        [TestCase(BattleHudPage.Magic, 3)]
        [TestCase(BattleHudPage.Items, 1)]
        public void ConfirmInAnEmptyCategoryKeepsTheDisplayedTab(BattleHudPage page, int category)
        {
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(Session());
            presenter.Open(page);
            presenter.SetCategory(category);
            Assert.That(presenter.Abilities(), Is.Empty);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm)), Is.True);
            Assert.That(presenter.Category, Is.EqualTo(category));
            Assert.That(presenter.FocusedId, Is.EqualTo("category:" + category));
        }

        [Test]
        public void ClickingOnlyALegalTargetCommitsExactlyOnce()
        {
            BattleSession session = Session();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            int inputs = session.Inputs.Count;
            Assert.That(presenter.ChooseAction(BattleAction.Attack), Is.True);
            Assert.That(presenter.SelectTarget("B"), Is.False);
            Assert.That(session.Inputs.Count, Is.EqualTo(inputs));
            Assert.That(presenter.SelectTarget("E"), Is.True);
            Assert.That(session.Inputs.Count, Is.EqualTo(inputs + 1));
            Assert.That(presenter.SelectTarget("E"), Is.False);
            Assert.That(session.Inputs.Count, Is.EqualTo(inputs + 1));
        }

        [Test]
        public void RevivalSelectionDefaultsToTheFallenAllyAndAllowsALivingRecipient()
        {
            BattleSession session = Session();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            presenter.Open(BattleHudPage.Items);
            Assert.That(presenter.ChooseAbility(6201), Is.True);
            Assert.That(presenter.Targets.Select(actor => actor.Id), Is.EquivalentTo(new[] { "A", "B" }));
            Assert.That(presenter.Target, Is.EqualTo("B"));
            Assert.That(presenter.FocusedId, Is.EqualTo("target:B"));
            Assert.That(presenter.SelectTarget("A"), Is.True);
        }

        [Test]
        public void WaitingControllerChangesClearTheOldSelectionAndPermitManualEnemies()
        {
            BattleSession session = Session();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            presenter.ChooseAction(BattleAction.Attack);
            presenter.HoverTarget("E");
            Assert.That(session.Submit("A", BattleAction.Defend), Is.True);
            presenter.Synchronize(session);
            Assert.That(presenter.Controller, Is.EqualTo("E"));
            Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Commands));
            Assert.That(presenter.Target, Is.Null);
            Assert.That(presenter.ChooseAction(BattleAction.Attack), Is.True);
            Assert.That(presenter.Targets.Select(actor => actor.Id), Is.EqualTo(new[] { "A" }));
            Assert.That(presenter.SelectTarget("A"), Is.True);
        }

        [Test]
        public void AbortedSessionsClearSelectionAndCannotRequestAi()
        {
            BattleSession session = Session();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            presenter.ChooseAction(BattleAction.Attack);
            session.Abort();
            presenter.Synchronize(session);
            string before = State(session);
            Assert.That(presenter.Controller, Is.Null);
            Assert.That(presenter.RequestAi(false), Is.False);
            Assert.That(presenter.RequestAi(true), Is.False);
            Assert.That(State(session), Is.EqualTo(before));
        }

        [Test]
        public void UnknownActionsAndAbilitiesNeverBorrowNormalAttackRange()
        {
            BattleSession session = Session();
            string before = State(session);
            foreach (BattleCommand command in new[]
            {
                new BattleCommand("A", (BattleAction)999), new BattleCommand("A", BattleAction.Skill, abilityId: 999),
                new BattleCommand("A", BattleAction.Item, abilityId: 999), new BattleCommand("missing", BattleAction.Attack)
            })
            {
                Assert.That(session.GetActionTargetRange(command), Is.EqualTo(BattleTargetRange.None));
                Assert.That(session.GetPotentialAffectedTargets(command), Is.Empty);
            }
            Assert.That(State(session), Is.EqualTo(before));
        }

        [TestCase("PAL3", 2)]
        [TestCase("PAL3A", 2)]
        public void NativePointerSetsChangeFromTheControllerToTheWholePotentialArea(string variant, int enemyPointers)
        {
            BattleSession session = Session(variant);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            Assert.That(BattleHudView.PointerActors(session, presenter), Is.EqualTo(new[] { "A" }));
            presenter.Open(BattleHudPage.Magic);
            Assert.That(BattleHudView.PointerActors(session, presenter), Is.EqualTo(new[] { "A" }));
            presenter.ChooseAbility(301);
            presenter.HoverTarget("E");
            Assert.That(BattleHudView.PointerActors(session, presenter).Count, Is.EqualTo(enemyPointers));
            Assert.That(BattleHudView.PointerActors(session, presenter), Does.Not.Contain("A"));
        }

        [Test]
        public void KeyboardNavigationUsesTheSameFocusAndConfirmationIntentAsControllers()
        {
            BattleSession session = Session();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            Assert.That(presenter.FocusedId, Is.EqualTo("attack"));
            string before = State(session);
            presenter.Handle(BattleHudView.KeyboardIntent(KeyCode.UpArrow).Value);
            Assert.That(presenter.FocusedId, Is.EqualTo("skills"));
            presenter.Handle(BattleHudView.KeyboardIntent(KeyCode.Return).Value);
            Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Skills));
            Assert.That(presenter.FocusedId, Is.EqualTo("ability:401"));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm));
            Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Targets));
            Assert.That(presenter.Target, Is.EqualTo("E"));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: BattleHudDirection.Down));
            Assert.That(presenter.Target, Is.EqualTo("E3"));
            Assert.That(State(session), Is.EqualTo(before));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm));
            Assert.That(session.Inputs.Count(input => input.Kind == BattleInputKind.Command), Is.EqualTo(1));
        }

        [Test]
        public void PointerCaptureRejectsSecondPointerAndCancelsWithoutSubmitting()
        {
            BattleSession session = Session();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            string before = State(session);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerDown, "attack", 17)), Is.True);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerDown, "defend", 22)), Is.False);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerUp, "defend", 22)), Is.False);
            Assert.That(presenter.CapturedPointerId, Is.EqualTo(17));
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerCancel, pointerId: 17)), Is.True);
            Assert.That(presenter.CapturedPointerId, Is.Null);
            Assert.That(State(session), Is.EqualTo(before));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerDown, "attack", 17));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerUp, "attack", 17));
            Assert.That(presenter.Page, Is.EqualTo(BattleHudPage.Targets));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerDown, "target:E", 17));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerMove, "target:E2", 17));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerUp, "target:E2", 17));
            Assert.That(State(session), Is.EqualTo(before));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerDown, "target:E2", 17));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerUp, "target:E2", 17));
            Assert.That(session.Inputs.Count(input => input.Kind == BattleInputKind.Command), Is.EqualTo(1));
        }

        [Test]
        public void ChangingMenuOrWaitingActorClearsCapturedPointer()
        {
            BattleSession session = Session();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerDown, "attack", 2));
            presenter.Open(BattleHudPage.Skills);
            Assert.That(presenter.CapturedPointerId, Is.Null);
            presenter.Back();
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.PointerDown, "attack", 2));
            session.Submit("A", BattleAction.Defend);
            presenter.Synchronize(session);
            Assert.That(presenter.CapturedPointerId, Is.Null);
            Assert.That(presenter.Controller, Is.EqualTo("E"));
        }

        [TestCase(320, 640)]
        [TestCase(1600, 900)]
        [TestCase(2520, 1080)]
        public void AdaptiveHudUsesFullViewportAndKeepsCardsInsideExternalSafeArea(float width, float height)
        {
            Rect viewport = new(37, 91, width, height);
            var context = new BattleHudViewport(viewport, new Vector4(16, 12, 24, 18));
            Assert.That(context.Viewport, Is.EqualTo(viewport));
            Assert.That(context.SafeRect.x, Is.EqualTo(53));
            Assert.That(context.SafeRect.y, Is.EqualTo(103));
            for (int index = 0; index < 4; index++)
            {
                Rect logical = context.StateCard(index);
                Vector2 topLeft = context.ToViewport(logical.position);
                Vector2 bottomRight = context.ToViewport(logical.max);
                Assert.That(topLeft.x, Is.GreaterThanOrEqualTo(context.SafeRect.x - .001));
                Assert.That(bottomRight.x, Is.LessThanOrEqualTo(context.SafeRect.xMax + .001));
                Assert.That(bottomRight.y, Is.LessThanOrEqualTo(context.SafeRect.yMax + .001));
                Assert.That(Vector2.Distance(context.ToCanvas(topLeft), logical.position), Is.LessThan(.001));
            }
            Assert.That(context.ReferenceRect.width / context.ReferenceRect.height, Is.EqualTo(4f / 3).Within(.00001));
        }

        [TestCase(900, 675)]
        [TestCase(1260, 540)]
        [TestCase(1200, 600)]
        [TestCase(700, 700)]
        public void TouchKeepsTheNativeCanvasAndOnlyEnlargesTheLocalMenu(int width, int height)
        {
            var viewport = new BattleHudViewport(new Rect(29, 43, width, height), new Vector4(16, 12, 24, 18));
            var touch = new BattleHudInteractionProfile(true);
            float scale = touch.ScaleFor(viewport);
            Assert.That(scale, Is.EqualTo(1.2f));
            float maximum = new BattleHudInteractionProfile(true, 2).ScaleFor(viewport);
            Assert.That(viewport.CanvasSize.y - 118 - 322 * maximum, Is.GreaterThanOrEqualTo(0));
            Assert.That(new BattleHudInteractionProfile(false).ScaleFor(viewport), Is.EqualTo(1));
            Assert.That(viewport.StateCard(0).yMax, Is.LessThanOrEqualTo(viewport.CanvasSize.y));
        }

        [Test]
        public void ExpandedTouchHitsChooseTheNearestNativeControlInsteadOfRegistrationOrder()
        {
            BattleSession session = Session();
            var view = new BattleHudView();
            view.Presenter.Synchronize(session);
            void Set(string name, object value) => typeof(BattleHudView).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, value);
            Set("_battle", session);
            Set("_inputEnabled", true);
            Set("_interaction", new BattleHudInteractionProfile(true));
            Set("_viewport", new BattleHudViewport(new Rect(0, 0, 800, 600), default));
            var hits = (System.Collections.Generic.List<(string, Rect)>)typeof(BattleHudView).GetField("_hitControls", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
            hits.Add(("defend", new Rect(146, 100, 40, 40)));
            hits.Add(("attack", new Rect(100, 100, 40, 40)));
            string before = State(session);
            Assert.That(view.SendPointer(BattleHudIntentKind.PointerDown, 11, new Vector2(142, 120)), Is.True);
            Assert.That(view.SendPointer(BattleHudIntentKind.PointerUp, 11, new Vector2(142, 120)), Is.True);
            Assert.That(view.Presenter.Page, Is.EqualTo(BattleHudPage.Targets));
            Assert.That(State(session), Is.EqualTo(before));
        }

        [TestCase(320, 640)]
        [TestCase(1600, 900)]
        [TestCase(2520, 1080)]
        public void SpiritWheelPiecesShareOneUniformRightAnchoredMapping(int width, int height)
        {
            var context = new BattleHudViewport(new Rect(29, 43, width, height), new Vector4(16, 12, 16, 18));
            Vector2 water = new(722, 48), fire = new(732, 22);
            Assert.That(Vector2.Distance(context.NativeToViewport(fire) - context.NativeToViewport(water), (fire - water) * context.Scale), Is.LessThan(.001));
            Assert.That(context.NativeToViewport(new Vector2(800, 0)), Is.EqualTo(new Vector2(context.SafeRect.xMax, context.SafeRect.y)));
        }

        [TestCase(1600, 900, true)]
        [TestCase(2520, 1080, true)]
        [TestCase(700, 1000, true)]
        [TestCase(1600, 900, false)]
        public void DisplayFramingMapsTheNativeProjectionWithoutChangingCameraSamples(int width, int height, bool expand)
        {
            using var preview = new ResourceBrowser.ResourceModelPreview(null, false);
            preview.SetHorizontalCameraOrientation(Vector3.zero, Vector3.forward, Vector3.up, 50);
            Vector3 world = new(1, 1, 10);
            Vector2 native = preview.ProjectReference(world, 800, 600);
            Rect viewport = new(23, 49, width, height);
            preview.ExpandReferenceFraming = expand;
            preview.SetViewportContext(viewport);
            Assert.That(preview.Project(world, viewport, out Vector2 actual), Is.True);
            var context = new BattleHudViewport(viewport, Vector4.zero);
            Vector2 mapped = context.ProjectionReferencePoint(native, expand);
            Assert.That(Vector2.Distance(actual, mapped), Is.LessThan(.01));
            Assert.That(preview.HorizontalFieldOfView, Is.EqualTo(50));
        }

        [TestCase(320, 640)]
        [TestCase(1600, 900)]
        public void ExplicitAuthoringCameraKeepsItsDeclaredVerticalFovAndMapsTheReferenceConsistently(int width, int height)
        {
            using var preview = new ResourceBrowser.ResourceModelPreview(null, false);
            preview.SetCamera(Vector3.zero, Vector3.forward * 5, 54);
            Vector3 world = new(1, 1, 10);
            Vector2 native = preview.ProjectReference(world, 800, 600);
            Rect viewport = new(37, 71, width, height);
            preview.ExpandReferenceFraming = true;
            preview.SetViewportContext(viewport);
            preview.Project(world, viewport, out Vector2 actual);
            var context = new BattleHudViewport(viewport, Vector4.zero);
            Assert.That(Vector2.Distance(context.ProjectionReferencePoint(native, preview.ReferenceProjectionScale(viewport)), actual), Is.LessThan(.01));
            preview.GetCamera(out _, out _, out float fieldOfView);
            Assert.That(fieldOfView, Is.EqualTo(54));
        }

        [Test]
        public void SpiritMappingIsExactAtFourByThreeAndNeverFollowsLaterCameraMovement()
        {
            var context = new BattleHudViewport(new Rect(0, 0, 800, 600), Vector4.zero);
            Vector2 origin = new(311, 432), destination = new(748, 81);
            foreach (Vector2 point in new[] { origin, new Vector2(500, 250), destination })
                Assert.That(context.SpiritOrbPoint(point, origin, destination, true), Is.EqualTo(point));
            context = new BattleHudViewport(new Rect(40, 20, 2520, 1080), new Vector4(20, 10, 30, 15));
            Vector2 before = context.SpiritOrbPoint(new Vector2(500, 250), origin, destination, true);
            using var preview = new ResourceBrowser.ResourceModelPreview(null, false);
            preview.SetCamera(new Vector3(20, 30, 40), Vector3.zero, 80);
            preview.SetViewportContext(context.Viewport);
            Assert.That(context.SpiritOrbPoint(new Vector2(500, 250), origin, destination, true), Is.EqualTo(before));
            Assert.That(context.SpiritOrbPoint(destination, origin, destination, true), Is.EqualTo(context.NativeToViewport(destination)));
        }

        [TestCase(0, 5, 33)]
        [TestCase(31, 5, 63)]
        [TestCase(63, 5, 16)]
        [TestCase(63, 0, 16)]
        [TestCase(16, 0, 16)]
        [TestCase(32, 0, 33)]
        [TestCase(0, 1, 0)]
        [TestCase(2, 1, 0)]
        [TestCase(63, 1, 61)]
        [TestCase(63, 2, 59)]
        [TestCase(63, 3, 55)]
        [TestCase(16, 4, 16)]
        [TestCase(0, 4, 0)]
        [TestCase(63, 4, 47)]
        [TestCase(32, 3, 40)]
        public void AiMenuTogglesMatchBothReleasedPrograms(int flags, int bit, int expected)
        {
            // Captured by executing each real ProcAIList switch. Full evidence:
            // Hud/probe-ai-control-native.py: 768 widget cases per variant.
            Assert.That(BattleHudPresenter.ToggleAiFlag(flags, bit), Is.EqualTo(expected));
        }

        [Test]
        public void AiDraftDoesNotChangeTheLiveControllerBeforeConfirmationAndBackDiscardsIt()
        {
            BattleSession session = Session();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            string before = State(session);
            int live = session.GetAiProfile("A");
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "ai"));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "flag:5"));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "flag:1"));
            Assert.That(presenter.AiFlags("A"), Is.Not.EqualTo(live));
            Assert.That(session.GetAiProfile("A"), Is.EqualTo(live));
            Assert.That(session.WaitingActor, Is.EqualTo("A"));
            Assert.That(State(session), Is.EqualTo(before));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Back));
            Assert.That(session.GetAiProfile("A"), Is.EqualTo(live));
            Assert.That(State(session), Is.EqualTo(before));
        }

        [Test]
        public void AiConfirmationUsesOneAtomicNativeCommandAndLeavesPoseAndResourcesAlone()
        {
            BattleSession session = Session();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            BattleActorSnapshot before = session.Actors.First(actor => actor.Id == "A");
            int input = session.Inputs.Count;
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "ai"));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "flag:5"));
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "end")), Is.True);
            Assert.That(session.Inputs.Skip(input).Select(value => value.Kind.ToString()), Is.EqualTo(new[] { "AiControl" }));
            BattleActorSnapshot after = session.Actors.First(actor => actor.Id == "A");
            Assert.That(after.Gauge, Is.Zero);
            Assert.That(after.CastGauge, Is.Zero);
            Assert.That(after.Pose, Is.EqualTo(before.Pose));
            Assert.That((after.Hp, after.Sp, after.Mp), Is.EqualTo((before.Hp, before.Sp, before.Mp)));
        }

        [TestCase("PAL3", false)]
        [TestCase("PAL3A", false)]
        [TestCase("PAL3", true)]
        [TestCase("PAL3A", true)]
        public void AiConfirmationPreservesUntouchedFlagsChangedInTheDrawer(string variant, bool editSkill)
        {
            BattleSession session = Session(variant);
            session.SetAiProfile("E", 33);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "ai")), Is.True);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "ai-actor:E")), Is.True);
            if (editSkill)
                Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "flag:2")), Is.True);

            session.SetAiProfile("E", 49);
            int inputCount = session.RecordedInputCount;
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "end")), Is.True);
            Assert.That(session.GetAiProfile("E"), Is.EqualTo(editSkill ? 53 : 49));
            Assert.That(session.RecordedInputCount, Is.EqualTo(inputCount + (editSkill ? 1 : 0)));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void AiDraftFromAPreviousMonsterGenerationDoesNotOverwriteTheReplacement(string variant)
        {
            BattleSession session = Session(variant);
            session.SetAiProfile("E", 33);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "ai")), Is.True);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "ai-actor:E")), Is.True);
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "flag:2")), Is.True);

            var generations = (System.Collections.Generic.Dictionary<string, int>)typeof(BattleSession)
                .GetField("_monsterGenerations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            generations["E"] = 1;
            session.SetAiProfile("E", 49);
            int inputCount = session.RecordedInputCount;
            Assert.That(presenter.AiFlags("E"), Is.EqualTo(49));
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "end")), Is.True);
            Assert.That(session.GetAiProfile("E"), Is.EqualTo(49));
            Assert.That(session.RecordedInputCount, Is.EqualTo(inputCount));
        }

        [Test]
        public void EditingAnotherActorsAiIsRecordedAsAToolProfileWithoutSpendingTheControllerGauge()
        {
            BattleSession session = Session();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            int gauge = session.Actors.First(actor => actor.Id == "A").Gauge;
            int input = session.Inputs.Count;
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "ai"));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "ai-actor:E"));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "flag:5"));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "end"));
            Assert.That(session.Inputs.Skip(input).Select(value => value.Kind), Is.EqualTo(new[] { BattleInputKind.AiProfile }));
            Assert.That(session.Actors.First(actor => actor.Id == "A").Gauge, Is.EqualTo(gauge));
        }

        [Test]
        public void EnemyWaterFieldDisablesItsTabWithoutErasingRepeatOnEntry()
        {
            BattleSession session = Session("PAL3A", configure: scenario =>
            {
                scenario.FiveSpiritElement = 1;
                scenario.FiveSpiritForce = 2;
            }, gaugeRate: BattleSession.FullGauge);
            Assert.That(session.Submit("A", BattleAction.Defend), Is.True);
            for (int tick = 0; tick < 1000 && session.WaitingActor != "A"; tick++)
            {
                if (session.WaitingActor is { } other)
                    Assert.That(session.Submit(other, BattleAction.Defend), Is.True);
                session.Step();
            }
            Assert.That(session.WaitingActor, Is.EqualTo("A"));
            var hud = new BattleHudPresenter();
            hud.Synchronize(session);
            int inputs = session.RecordedInputCount;
            hud.Open(BattleHudPage.Magic);
            Assert.That(hud.Category, Is.EqualTo(1));
            Assert.That(hud.FocusItems().Single(item => item.Id == "category:0").Enabled, Is.False);
            hud.SetCategory(0);
            Assert.That(hud.Category, Is.EqualTo(1));
            Assert.That(hud.Handle(new BattleHudIntent(BattleHudIntentKind.CyclePrevious)), Is.True);
            Assert.That(hud.Category, Is.EqualTo(5));
            Assert.That(session.RecordedInputCount, Is.EqualTo(inputs));
            hud.Back();
            Assert.That(session.RequestAiRepeat("A"), Is.True);
            Assert.That(session.Inputs.Last().Action, Is.EqualTo(BattleAction.Defend));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void MagicCategoryMemoryIsPerControllerAndSession(string variant)
        {
            BattleSession Create() => Session(variant, configure: scenario => scenario.Participants[1].Vitals[0] = 100,
                gaugeRate: BattleSession.FullGauge);
            BattleSession session = Create();
            var hud = new BattleHudPresenter();
            hud.Synchronize(session);
            Assert.That(hud.Controller, Is.EqualTo("A"));
            hud.Open(BattleHudPage.Magic);
            hud.SetCategory(3);
            hud.Back();
            hud.Open(BattleHudPage.Magic);
            Assert.That(hud.Category, Is.EqualTo(3));

            ReadyAfterDefending(session, "B");
            hud.Synchronize(session);
            hud.Open(BattleHudPage.Magic);
            Assert.That(hud.Category, Is.Zero);
            hud.SetCategory(4);

            ReadyAfterDefending(session, "A");
            hud.Synchronize(session);
            hud.Open(BattleHudPage.Magic);
            Assert.That(hud.Category, Is.EqualTo(3));

            hud.Synchronize(Create());
            hud.Open(BattleHudPage.Magic);
            Assert.That(hud.Category, Is.Zero);
        }

        [Test]
        public void InactiveMagicRowIgnoresOldGeometryButVisibleForbiddenRowStillInvalidatesRepeat()
        {
            BattleSession session = Session("PAL3A", configure: scenario =>
            {
                // Render the water spell in an authored slot on an enabled tab,
                // while the enemy water field makes that individual spell unavailable.
                scenario.FiveSpiritElement = 1;
                scenario.FiveSpiritForce = 2;
                BattleSkill spell = scenario.Participants[0].Skills.Single(skill => skill.Id == 301);
                spell.NativeMagicGroup = 1;
                spell.NativeMagicSlot = 0;
            }, gaugeRate: BattleSession.FullGauge);
            ReadyAfterDefending(session, "A");
            var view = new BattleHudView();
            BattleHudPresenter hud = view.Presenter;
            hud.Synchronize(session);
            hud.Open(BattleHudPage.Magic);
            Assert.That(hud.Category, Is.EqualTo(1));
            const string row = "ability:301";
            Assert.That(hud.FocusItems().Single(item => item.Id == row).Enabled, Is.False);

            const BindingFlags fields = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(BattleHudView).GetField("_inputEnabled", fields).SetValue(view, true);
            typeof(BattleHudView).GetField("_viewport", fields).SetValue(view, new BattleHudViewport(new Rect(0, 0, 800, 600), default));
            var hits = (System.Collections.Generic.List<(string, Rect)>)typeof(BattleHudView).GetField("_hitControls", fields).GetValue(view);
            hits.Add((row, new Rect(100, 100, 40, 40)));
            var pointer = new Vector2(120, 120);
            int before = session.RecordedInputCount;
            hud.SetCategory(3);
            Assert.That(view.SendPointer(BattleHudIntentKind.PointerMove, 0, pointer), Is.False);
            Assert.That(hud.InspectAbilityRow(row), Is.False);
            Assert.That(session.RecordedInputCount, Is.EqualTo(before));

            hud.SetCategory(1);
            Assert.That(view.SendPointer(BattleHudIntentKind.PointerMove, 0, pointer), Is.True);
            Assert.That(session.RecordedInputCount, Is.EqualTo(before + 1));
            Assert.That(session.Inputs.Last().Kind, Is.EqualTo(BattleInputKind.MagicMenuFieldDenial));
        }

        private static void ReadyAfterDefending(BattleSession session, string actor)
        {
            Assert.That(session.Submit(session.WaitingActor, BattleAction.Defend), Is.True);
            for (int tick = 0; tick < 1000 && session.WaitingActor != actor; tick++)
            {
                if (session.WaitingActor is { } other)
                    Assert.That(session.Submit(other, BattleAction.Defend), Is.True);
                session.Step();
            }
            Assert.That(session.WaitingActor, Is.EqualTo(actor));
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void ANewMonsterGenerationRequiresAFreshPointerPress(string variant)
        {
            BattleSession session = Session(variant);
            var hud = new BattleHudPresenter();
            hud.Synchronize(session);
            Assert.That(hud.ChooseAction(BattleAction.Attack), Is.True);
            var down = new BattleHudIntent(BattleHudIntentKind.PointerDown, "target:E", 7);
            var up = new BattleHudIntent(BattleHudIntentKind.PointerUp, "target:E", 7);
            Assert.That(hud.Handle(down), Is.True);
            var generations = (System.Collections.Generic.Dictionary<string, int>)typeof(BattleSession)
                .GetField("_monsterGenerations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            generations["E"] = 1;
            hud.Synchronize(session);
            Assert.That(hud.Handle(up), Is.False);
            Assert.That(session.Inputs.Any(input => input.Kind == BattleInputKind.Command), Is.False);
            Assert.That(hud.Handle(down), Is.True);
            Assert.That(hud.Handle(up), Is.True);
            Assert.That(session.Inputs.Last().Action, Is.EqualTo(BattleAction.Attack));
        }

        [Test]
        public void ManualEnemyControllerKeepsItsExplicitToolAiEditingPath()
        {
            BattleSession session = Session();
            session.Submit("A", BattleAction.Defend);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            Assert.That(presenter.Controller, Is.EqualTo("E"));
            int input = session.Inputs.Count;
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "ai"));
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "flag:5"));
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm, "end")), Is.True);
            Assert.That(session.Inputs.Skip(input).Select(value => value.Kind), Is.EqualTo(new[] { BattleInputKind.AiProfile }));
        }

        [Test]
        public void AllTargetConfirmationRecordsTheNativeGroupMaskWithoutAnInventedSingleRecipient()
        {
            BattleSession session = Session();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            presenter.Open(BattleHudPage.Magic);
            presenter.SetCategory(1);
            Assert.That(presenter.ChooseAbility(302), Is.True);
            Assert.That(presenter.SelectTarget("E3"), Is.True);
            BattleInput command = session.Inputs.Last(input => input.Kind == BattleInputKind.Command);
            Assert.That(command.Target, Is.Null);
            Assert.That(command.AbilityId, Is.EqualTo(302));
        }

        [Test]
        public void AllTargetNormalAttackKeepsTheRequiredLegalAnchor()
        {
            BattleSession session = Session(normalRange: BattleTargetRange.OpponentAll);
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            Assert.That(presenter.ChooseAction(BattleAction.Attack), Is.True);
            Assert.That(presenter.SelectTarget("E"), Is.True);
            Assert.That(session.Inputs.Last(input => input.Kind == BattleInputKind.Command).Target, Is.EqualTo("E"));
        }

        [Test]
        public void TouchPanCanStartFromADisabledRowAndAnotherPointerCannotTakeIt()
        {
            var view = new BattleHudView();
            view.Presenter.Synchronize(Session());
            view.Presenter.Open(BattleHudPage.Magic);
            void Set(string name, object value) => typeof(BattleHudView).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, value);
            Set("_interaction", new BattleHudInteractionProfile(true));
            Set("_viewport", new BattleHudViewport(new Rect(0, 0, 320, 640), default));
            Set("_touchPanel", new Rect(10, 100, 300, 400));
            Set("_touchScrollLimit", 1000f);
            Set("_touchPanelScale", .5f);
            var gesture = typeof(BattleHudView).GetMethod("TouchGesture", BindingFlags.NonPublic | BindingFlags.Instance);
            bool Send(BattleHudIntentKind kind, int pointer, Vector2 point) => (bool)gesture.Invoke(view, new object[] { kind, pointer, point });
            Assert.That(Send(BattleHudIntentKind.PointerDown, 11, new Vector2(50, 300)), Is.False);
            Assert.That(view.Presenter.CapturedPointerId, Is.Null);
            Assert.That(Send(BattleHudIntentKind.PointerDown, 12, new Vector2(50, 310)), Is.True);
            Assert.That(Send(BattleHudIntentKind.PointerMove, 11, new Vector2(50, 200)), Is.True);
            Vector2 scroll = (Vector2)typeof(BattleHudView).GetField("_listScroll", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
            Assert.That(scroll.y, Is.GreaterThan(0));
            Assert.That(typeof(BattleHudView).GetField("_scrollFocus", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view), Is.EqualTo(view.Presenter.FocusedId), "A pan must not become an automatic scroll-to-focus on the next repaint.");
            Assert.That(Send(BattleHudIntentKind.PointerUp, 12, new Vector2(50, 190)), Is.True);
            Assert.That(Send(BattleHudIntentKind.PointerUp, 11, new Vector2(50, 200)), Is.True);
            Assert.That(typeof(BattleHudView).GetField("_touchCandidatePointer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view), Is.Null);
        }

        [TestCase("PAL3")]
        [TestCase("PAL3A")]
        public void NativeScrollbarCaptureReceivesTheWholeGestureWithoutBreakingRowPan(string variant)
        {
            var view = new BattleHudView();
            view.Presenter.Synchronize(Session(variant));
            view.Presenter.Open(BattleHudPage.Magic);
            const BindingFlags fields = BindingFlags.NonPublic | BindingFlags.Instance;
            void Set(string name, object value) => typeof(BattleHudView).GetField(name, fields).SetValue(view, value);
            Set("_interaction", new BattleHudInteractionProfile(true));
            Set("_touchPanel", new Rect(10, 100, 300, 400));
            Set("_touchScrollLimit", 1000f);
            var gesture = typeof(BattleHudView).GetMethod("TouchGesture", fields);
            bool Send(BattleHudIntentKind kind, Vector2 point) => (bool)gesture.Invoke(view, new object[] { kind, 11, point });
            int previous = GUIUtility.hotControl;
            try
            {
                GUIUtility.hotControl = 0;
                Assert.That(Send(BattleHudIntentKind.PointerDown, new Vector2(300, 300)), Is.False);
                // Model the owner established by BeginScrollView after Down.
                // The adapter must leave Move/Up and the native capture alone;
                // the separate desktop test exercises the actual scrollbar.
                GUIUtility.hotControl = 7001;
                Assert.That(Send(BattleHudIntentKind.PointerMove, new Vector2(300, 200)), Is.False);
                Assert.That(Send(BattleHudIntentKind.PointerUp, new Vector2(300, 200)), Is.False);
                Assert.That(GUIUtility.hotControl, Is.EqualTo(7001));
                Assert.That(typeof(BattleHudView).GetField("_touchCandidatePointer", fields).GetValue(view), Is.Null);
                Assert.That((Vector2)typeof(BattleHudView).GetField("_listScroll", fields).GetValue(view), Is.EqualTo(Vector2.zero));
                GUIUtility.hotControl = 0;
                Assert.That(Send(BattleHudIntentKind.PointerDown, new Vector2(50, 300)), Is.False);
                Assert.That(Send(BattleHudIntentKind.PointerMove, new Vector2(50, 200)), Is.True);
                Assert.That(Send(BattleHudIntentKind.PointerUp, new Vector2(50, 200)), Is.True);
                Assert.That(((Vector2)typeof(BattleHudView).GetField("_listScroll", fields).GetValue(view)).y, Is.GreaterThan(0));
            }
            finally
            {
                GUIUtility.hotControl = previous;
            }
        }

        [TestCase(100, 30, 1600, 900)]
        [TestCase(17, 51, 700, 1000)]
        public void LetterboxingAndHitTestingShareTheSameNonzeroViewport(float x, float y, float width, float height)
        {
            Rect available = new(x, y, width, height);
            Rect content = BattleHudLayout.ContentRect(available);
            Assert.That(content.width / content.height, Is.EqualTo(4f / 3).Within(.00001));
            Assert.That(Vector2.Distance(content.center, available.center), Is.LessThan(.001));
            Vector2 point = new(75, 400);
            Vector2 pixel = BattleHudLayout.ToViewport(point, content);
            Assert.That(Vector2.Distance(BattleHudLayout.ToCanvas(pixel, content), point), Is.LessThan(.001));
            Assert.That(BattleHudLayout.Hit(new Rect(63, 381, 46, 46), pixel, content), Is.True);
            Assert.That(BattleHudLayout.Hit(new Rect(63, 381, 46, 46), available.position, content), Is.False);
        }

        [TestCase(0d, 200f / 255)]
        [TestCase(Math.PI / 24, 250f / 255)]
        [TestCase(Math.PI / 8, 150f / 255)]
        public void NativeStatusPulseKeepsFractionalLuminance(double seconds, float expected)
        {
            Assert.That(BattleHudLayout.StatusLuminance(seconds), Is.EqualTo(expected).Within(.00001));
            Assert.That(BattleHudLayout.StatusLuminance(seconds), Is.GreaterThan(.5f));
        }

        [TestCase(BattleHudPage.Skills, BattleHudDirection.Down, 400)]
        [TestCase(BattleHudPage.Skills, BattleHudDirection.Up, 402)]
        [TestCase(BattleHudPage.Magic, BattleHudDirection.Down, 400)]
        [TestCase(BattleHudPage.Magic, BattleHudDirection.Up, 402)]
        [TestCase(BattleHudPage.Items, BattleHudDirection.Down, 400)]
        [TestCase(BattleHudPage.Items, BattleHudDirection.Up, 402)]
        public void FirstNavigationIntoAnAllDisabledListInspectsItsBoundaryRow(BattleHudPage page, BattleHudDirection direction, int expected)
        {
            var scenario = new BattleScenario { Scene = "disabled-list", DataFingerprint = "disabled-list" };
            foreach (var entry in new[] { (Id: "A", Actor: 101, Side: BattleSide.Ally), (Id: "E", Actor: 701, Side: BattleSide.Enemy) })
                scenario.Participants.Add(new BattleParticipant
                {
                    InstanceId = entry.Id,
                    ActorId = entry.Actor,
                    Side = entry.Side,
                    Attributes = new[] { 100, 100, 100, 20, 10, 3, 0, 0, 0, 0, 0, 0 },
                    Vitals = new[] { 100, 100, 100 }
                });
            BattleAbilityDefinition[] abilities = Enumerable.Range(400, 3).Select(id => new BattleAbilityDefinition(new BattleAbilityData
            {
                Id = id,
                Name = "Unavailable " + id,
                UnsupportedReason = "This authored effect has no verified battle handler.",
                IsMagic = page == BattleHudPage.Magic,
                SkillTypeId = page == BattleHudPage.Magic ? 2 : 0,
                ElementMask = 1,
                IsItem = page == BattleHudPage.Items,
                ItemType = 1,
                Range = BattleTargetRange.OpponentSingle,
                ApplicableIdentities = new[] { 0 },
                Costs = new[] { new BattleCost(2, BattleValueMode.Absolute, 100000) }
            })).ToArray();
            foreach (BattleAbilityDefinition ability in abilities)
            {
                scenario.Participants[0].Skills.Add(new BattleSkill { Id = ability.Id, Mastered = true });
                scenario.Items.Add(new BattleItem { Id = ability.Id, Count = 1 });
            }
            bool item = page == BattleHudPage.Items;
            var definitions = new BattleDefinitions("PAL3", "disabled-list", item ? Array.Empty<BattleAbilityDefinition>() : abilities,
                item ? abilities : Array.Empty<BattleAbilityDefinition>(), scenario.Participants.Select(actor =>
                    new BattleActorDefinition(new BattleActorData { InstanceId = actor.InstanceId, Identity = actor.Side == BattleSide.Ally ? 0 : -1 })));
            var session = new BattleSession(new BattleSetup(scenario), new Catalog(), new Rules(), definitions);
            session.SetAutomatic(BattleSide.Enemy, false);
            session.Step();
            var presenter = new BattleHudPresenter();
            presenter.Synchronize(session);
            presenter.Open(page);
            Assert.That(presenter.Abilities().Count, Is.EqualTo(3));
            Assert.That(presenter.Abilities().All(ability => ability.Denied != null), Is.True);
            int inputs = session.Inputs.Count;
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: direction)), Is.True);
            Assert.That(presenter.FocusedId, Is.EqualTo("ability:" + expected));
            Assert.That(presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Confirm)), Is.False);
            Assert.That(session.Inputs.Count, Is.EqualTo(inputs), "Disabled rows are inspectable without issuing a command.");
            presenter.Handle(new BattleHudIntent(BattleHudIntentKind.Navigate, direction: direction));
            Assert.That(presenter.FocusedId, Is.EqualTo("ability:401"));
        }

        [Test]
        public void ReleasedVariantsHaveIndependentPortraitAndQueueProfiles()
        {
            var pal3 = new BattleHudLayout("PAL3");
            var pal3a = new BattleHudLayout("PAL3A");
            Assert.That(pal3.StateFace(0), Is.EqualTo("state/jingtian"));
            Assert.That(pal3a.StateFace(0), Is.EqualTo("state/tx1"));
            Assert.That(pal3a.StateName(2), Is.EqualTo("state/wz_px"));
            Assert.That(pal3.AttackPortrait(0, 0, false).x, Is.EqualTo(460));
            Assert.That(pal3a.AttackPortrait(0, 0, false).x, Is.EqualTo(387));
            Assert.That(BattleHudLayout.StateCard(4, 1), Is.EqualTo(BattleHudLayout.StateCard(0)));
        }
    }
}
