// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed partial class BattleSession
    {
        private string _formationController;
        private Dictionary<string, BattleSlot> _formationInitialSlots;

        public string ActiveFormationController => _formationController;

        public string CanBeginFormation(string controller)
        {
            if (Status != BattleSessionStatus.Running)
                return "The battle has ended.";
            if (Definitions == null || _catalog is not IBattleStageGeometry)
                return "This combat catalog has no verified formation movement plans.";
            if (_formationController != null)
                return "A formation panel is already open.";
            if (WaitingActor != controller || !_byId.TryGetValue(controller ?? "", out Actor actor) || actor.Initial.Side != BattleSide.Ally)
                return "Choose the current ready allied controller.";
            Actor[] allies = _actors.Where(value => value.Initial.Side == BattleSide.Ally).ToArray();
            if (allies.Any(value => value.Definition.Identity is < 0 or > 4) || allies.GroupBy(value => value.Definition.Identity).Any(group => group.Count() > 1))
                return "Original formations require distinct original player identities.";
            return FormationRules().CanChangeLineup(FormationActors()).FailureReason;
        }

        public bool BeginFormation(string controller)
        {
            if (CanBeginFormation(controller) != null)
                return false;
            _formationController = controller;
            _formationInitialSlots = _actors.Where(actor => actor.Initial.Side == BattleSide.Ally)
                .ToDictionary(actor => actor.Initial.InstanceId, actor => actor.Slot, StringComparer.Ordinal);
            _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.BeginFormation, Actor = controller });
            Add(BattleEventKind.FormationOpened, controller);
            return true;
        }

        public string CanSwapFormation(string actorId, BattleSlot destination)
        {
            if (Status != BattleSessionStatus.Running || _formationController == null)
                return "Open the formation panel first.";
            if (IsBattleInputSuspended)
                return "The battle movie is playing or its frame is resuming.";
            if (!Enum.IsDefined(typeof(BattleSlot), destination))
                return "Unknown formation slot.";
            if (!_byId.TryGetValue(actorId ?? "", out Actor actor) || actor.Initial.Side != BattleSide.Ally)
                return "Only an allied participant can change formation.";
            string denied = FormationRules().CanMove(FormationActor(actor)).FailureReason;
            if (denied != null)
                return denied;
            Actor other = _actors.FirstOrDefault(value => value.Initial.Side == BattleSide.Ally && value.Slot == destination && value != actor);
            if (other != null)
                return FormationRules().CanMove(FormationActor(other)).FailureReason;
            return null;
        }

        public bool SwapFormation(string actorId, BattleSlot destination)
        {
            if (CanSwapFormation(actorId, destination) != null)
                return false;
            Actor actor = _byId[actorId];
            if (actor.Slot == destination)
                return false;
            Actor other = _actors.FirstOrDefault(value => value.Initial.Side == BattleSide.Ally && value.Slot == destination && value != actor);
            var geometry = (IBattleStageGeometry)_catalog;
            bool crossing = _scenario.Variant == "PAL3" && other != null &&
                (actor.Definition.Identity == 0 && other.Definition.Identity == 4 || actor.Definition.Identity == 4 && other.Definition.Identity == 0);
            BattleMovementPoint sourceHome = actor.Home;
            BattleMovementPoint destinationHome = other?.Home ?? geometry.FormationHome(actorId, destination);
            // Compile both sides before changing slots, queues, logs or resource values.
            BattleMovementPlan[] first = ValidateMovementPlans(geometry.PlanFormationMove(actorId, sourceHome, destinationHome, crossing), sourceHome, destinationHome);
            BattleMovementPlan[] second = other == null ? null : ValidateMovementPlans(geometry.PlanFormationMove(other.Initial.InstanceId, other.Home, sourceHome, crossing), other.Home, sourceHome);
            BattleSlot previous = actor.Slot;
            actor.Slot = destination;
            actor.Home = destinationHome;
            QueueFormationMoves(actor, first);
            if (other != null)
            {
                other.Slot = previous;
                other.Home = sourceHome;
                QueueFormationMoves(other, second);
            }
            _inputs.Add(new BattleInput
            {
                Tick = Tick,
                Kind = BattleInputKind.SwapFormation,
                Actor = actorId,
                Target = _formationController,
                FormationSlot = destination
            });
            Add(BattleEventKind.FormationChanged, actorId, other?.Initial.InstanceId,
                detail: previous + " -> " + destination + "; slot/home changed immediately; gauges unchanged");
            return true;
        }

        public bool EndFormation(string controller)
        {
            if (_formationController == null || controller != _formationController || Status != BattleSessionStatus.Running || IsBattleInputSuspended)
                return false;
            bool changed = _actors.Any(actor => actor.Initial.Side == BattleSide.Ally &&
                _formationInitialSlots.TryGetValue(actor.Initial.InstanceId, out BattleSlot original) && original != actor.Slot);
            ReleaseFormationBodies();
            if (changed)
            {
                Actor actor = _byId[controller];
                AdvanceFiveSpiritFieldTurn(actor);
                actor.Gauge = actor.CastGauge = 0;
                actor.Ready = false;
            }
            _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.EndFormation, Actor = controller });
            _formationController = null;
            _formationInitialSlots = null;
            Add(BattleEventKind.FormationClosed, controller,
                detail: changed ? "changed lineup: controller gauges cleared" : "original lineup restored: no gauge cost");
            return true;
        }

        private BattleFormationRules FormationRules() => new(_scenario.Variant);

        private BattleFormationActor FormationActor(Actor actor)
        {
            int index = NativeActorIndex(actor);
            int weapon = actor.Definition.Equipment.FirstOrDefault(item => item.Slot == 0)?.WeaponType ?? 0;
            return new BattleFormationActor(index, actor.Initial.ActorId, actor.Slot, weapon,
                alive: actor.Alive, hp: actor.Hp, maxHp: actor.Attributes[0], states: actor.States.Keys,
                taskCount: StageTaskCount(actor),
                atHome: IsAtHome(actor), idle: actor.Run == null && ActionBodyIdle(actor),
                fled: actor.Escaped, transformed: IsPal3ATransformed(actor.Initial.InstanceId));
        }

        private IReadOnlyList<BattleFormationActor> FormationActors() =>
            Array.AsReadOnly(_actors.Select(FormationActor).ToArray());

        private BattleMovementPlan[] ValidateMovementPlans(IReadOnlyList<BattleMovementPlan> plans, BattleMovementPoint from, BattleMovementPoint to)
        {
            if (plans == null || plans.Count == 0 || plans.Any(plan => plan == null))
                throw new InvalidOperationException("The formation has no complete verified movement plan.");
            BattleMovementPoint previous = from;
            foreach (BattleMovementPlan plan in plans)
            {
                if (!SamePoint(plan.From, previous))
                    throw new InvalidOperationException("Formation movement segments must form a continuous path.");
                _ = new BattleMovement(_scenario.Variant, plan);
                previous = plan.To;
            }
            if (!SamePoint(previous, to))
                throw new InvalidOperationException("The movement plan does not finish at the new home.");
            return plans.ToArray();
        }

        private static bool HasFormationMovement(Actor actor) => actor.Movement?.Finished == false || actor.MovementQueue.Count > 0;

        private void QueueFormationMoves(Actor actor, IEnumerable<BattleMovementPlan> plans)
        {
            foreach (BattleMovementPlan plan in plans)
                actor.MovementQueue.Enqueue(plan);
            if (actor.Movement == null || actor.Movement.Finished)
                StartNextFormationMovement(actor);
        }

        private void StartNextFormationMovement(Actor actor)
        {
            BattleMovementPlan queued = actor.MovementQueue.Dequeue();
            float opacity = Has(actor, BattleState.Invisible) ? 0.44f : 1f;
            var plan = new BattleMovementPlan(queued.Kind, queued.From, queued.To,
                queued.FlyUpTick, queued.FlyDownTick, queued.OverTick,
                targetBlend: opacity, initialBlend: actor.Movement?.Blend ?? opacity);
            actor.Movement = new BattleMovement(_scenario.Variant, plan);
            Add(BattleEventKind.MovementStarted, actor.Initial.InstanceId, detail: actor.Movement.Plan.Kind.ToString());
        }

        private void AdvanceFormationMovements()
        {
            foreach (Actor actor in _actors)
                AdvanceFormationMovement(actor);
        }

        private void AdvanceFormationMovement(Actor actor)
        {
            if (actor.Movement == null || _fallbackFleeBodies.ContainsKey(actor.Initial.InstanceId) || PhysicalBodyBusy(actor) || IsActorExternallyControlled(actor.Initial.InstanceId) || SimulationTick < actor.HurtUntil)
                return;
            bool wasFinished = actor.Movement.Finished;
            if (_catalog is IBattlePhysicalTimings)
            {
                if (!TryStartAbilityMovementBody(actor))
                    EnsureMovementBodyAnimation(actor);
                AdvanceBodyMovement(actor, actor.Alive && !actor.Escaped, CurrentBodyMovementMessage(actor));
            }
            else
                AdvanceBodyMovement(actor, actor.Alive && !actor.Escaped);
            actor.Position = actor.Movement.Position;
            if (actor.Movement.FacingDegrees is { } facing)
                actor.FacingDegrees = facing;
            if (!wasFinished && actor.Movement.Finished)
                Add(BattleEventKind.MovementEnded, actor.Initial.InstanceId);
            if (!actor.Movement.Finished)
                return;
            if (actor.MovementQueue.Count > 0)
                StartNextFormationMovement(actor);
            else
                actor.Movement = null;
        }

        private static bool SamePoint(BattleMovementPoint first, BattleMovementPoint second) =>
            first.X == second.X && first.Y == second.Y && first.Z == second.Z;

        private void ReleaseUnavailableFormationController()
        {
            if (_formationController == null)
                return;
            Actor controller = _byId[_formationController];
            if (controller.Hp > 0 && !controller.Escaped && !IsUncontrollable(controller))
                return;
            string owner = _formationController;
            ReleaseFormationBodies();
            _formationController = null;
            _formationInitialSlots = null;
            Add(BattleEventKind.FormationClosed, owner, detail: "controller unavailable; existing lineup changes retained");
        }

        private static bool IsWithinHome(BattleMovementPoint position, BattleMovementPoint home)
        {
            double x = position.X - home.X, y = position.Y - home.Y, z = position.Z - home.Z;
            return x * x + y * y + z * z <= 13d * 13d;
        }

        private void ReleaseFormationBodies()
        {
            var geometry = (IBattleStageGeometry)_catalog;
            // Validate every return before releasing any body ownership.
            var returns = _actors.Where(actor => actor.Initial.Side == BattleSide.Ally)
                .OrderBy(NativeActorIndex).Select(actor => (Actor: actor,
                    Moves: IsWithinHome(actor.Position, actor.Home) ? null :
                        ValidateMovementPlans(geometry.PlanFormationMove(actor.Initial.InstanceId, actor.Position, actor.Home, false), actor.Position, actor.Home)))
                .ToArray();
            foreach (var (actor, moves) in returns)
            {
                ClearPhysicalBody(actor);
                actor.Movement?.Stop();
                actor.Movement = null;
                actor.MovementQueue.Clear();
                actor.HurtUntil = (int)SimulationTick;
                if (moves != null && QueueReturnHomeStage(actor))
                    QueueFormationMoves(actor, moves);
            }
        }

        private static BattleMovementPoint Interpolate(BattleMovementPoint from, BattleMovementPoint to, float amount) =>
            new(from.X + (to.X - from.X) * amount, from.Y + (to.Y - from.Y) * amount, from.Z + (to.Z - from.Z) * amount);

        private static void ResetActorFacing(Actor actor)
        {
            actor.FacingDegrees = BattleFacingRules.Initial(actor.Initial.Side);
            actor.Movement?.SetHeading(actor.FacingDegrees);
        }

        private static void SetActorPosition(Actor actor, BattleMovementPoint position)
        {
            actor.Position = position;
            actor.Movement?.Relocate(position);
        }

        private void UpdateAttackPosition(Actor actor, ActionRun run)
        {
            if (run.Kind != BattleAction.Attack || run.AttackPlan == null)
                return;
            BattleAttackPlan plan = run.AttackPlan;
            int elapsed = run.Elapsed - run.Timing.PreparationTicks;
            int travel = run.Timing.TravelTicks;
            if (elapsed == 1 && travel > 0)
                actor.FacingDegrees = BattleMovement.FacingBetween(plan.Origin, plan.Approach);
            if (elapsed == travel + run.Timing.AnimationTicks && !IsWithinHome(actor.Position, actor.Home))
                actor.FacingDegrees = BattleMovement.FacingBetween(actor.Position, actor.Home);
            BattleMovementPoint sample;
            if (elapsed <= travel)
                sample = Interpolate(plan.Origin, plan.Approach, travel == 0 ? 1f : Math.Clamp(elapsed / (float)travel, 0f, 1f));
            else if (elapsed >= travel + run.Timing.AnimationTicks)
                sample = Interpolate(plan.Approach, actor.Home, travel == 0 ? 1f : Math.Clamp((elapsed - travel - run.Timing.AnimationTicks) / (float)travel, 0f, 1f));
            else
                return;
            SetActorPosition(actor, new BattleMovementPoint(actor.Position.X + sample.X - run.LastAttackSample.X,
                actor.Position.Y + sample.Y - run.LastAttackSample.Y, actor.Position.Z + sample.Z - run.LastAttackSample.Z));
            run.LastAttackSample = sample;
        }

        private void StopFormationMovements()
        {
            _formationController = null;
            _formationInitialSlots = null;
            foreach (Actor actor in _actors)
            {
                actor.MovementQueue.Clear();
                // Preserve the final visual position; no queued task runs after termination.
                actor.Movement?.Stop();
            }
        }

        private BattleAttackPlan PlanAttack(Actor source, Actor target) => _catalog is IBattleStageGeometry geometry
            ? geometry.PlanAttack(source.Initial.InstanceId, target.Initial.InstanceId, source.Home, target.Home)
            : new BattleAttackPlan(_catalog.Attack(source.Initial.InstanceId, target.Initial.InstanceId), source.Home, source.Home, target.Home);
    }
}
