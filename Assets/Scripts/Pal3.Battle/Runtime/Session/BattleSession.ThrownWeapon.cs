// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleThrownWeaponSnapshot
    {
        public string Actor
        {
            get;
        }

        public BattleMovementPoint Position
        {
            get;
        }

        public BattleMovementPoint RotationDegrees
        {
            get;
        }

        public bool Active
        {
            get;
        }

        internal BattleThrownWeaponSnapshot(string actor, BattleThrownWeapon projectile)
        {
            Actor = actor;
            Position = projectile.Position;
            RotationDegrees = projectile.Rotation;
            Active = projectile.Active;
        }
    }

    public sealed partial class BattleSession
    {
        private sealed class ThrownWeaponRun
        {
            internal Actor Source;
            internal ActionRun Command;
            internal BattleThrownWeapon Projectile;
            internal bool RestoreQueued;
        }

        private readonly Dictionary<string, ThrownWeaponRun> _thrownWeapons = new(StringComparer.Ordinal);

        public IReadOnlyList<BattleThrownWeaponSnapshot> ThrownWeapons => Array.AsReadOnly(_thrownWeapons.Values
            .OrderBy(run => NativeActorIndex(run.Source)).Select(run => new BattleThrownWeaponSnapshot(run.Source.Initial.InstanceId, run.Projectile)).ToArray());

        private void AdvanceThrownWeapon(Actor actor)
        {
            if (!_thrownWeapons.TryGetValue(actor.Initial.InstanceId, out ThrownWeaponRun run))
                return;
            run.Projectile.Advance(actor.Alive);
            SetWeaponDrawingEnabled(actor.Initial.InstanceId, run.Projectile.WeaponVisible);
            if (run.Projectile.HitThisPass)
            {
                run.Command.NextHit++;
                ResolveActionMarker(actor, run.Command);
            }
            if (run.Projectile.RestoreRequested && !run.RestoreQueued)
            {
                run.RestoreQueued = true;
                PushThrownWeaponRestore(actor);
            }
        }

        private void AdvanceThrownWeaponBody(Actor actor, PhysicalBodyState body, PhysicalBodyTask task)
        {
            if (task.Kind == BattlePhysicalBodyKind.ThrownWeapon)
            {
                if (!task.Executed)
                {
                    var projectile = new BattleThrownWeapon(actor.Position, task.Destination);
                    _thrownWeapons[actor.Initial.InstanceId] = new ThrownWeaponRun { Source = actor, Command = task.Run, Projectile = projectile };
                    body.Pose = BattlePose.Attacking;
                    body.BladeSeconds = .25f;
                    TryPlayBodyAnimation(actor, body.Profile.ThrownStart, true, body.Pose, 61);
                    task.Executed = true;
                }
                BattleThrownWeapon projectileState = _thrownWeapons[actor.Initial.InstanceId].Projectile;
                projectileState.AdvanceBody(GetBodyBinding(actor).LastPoppedMarker, actor.Alive);
                SetWeaponDrawingEnabled(actor.Initial.InstanceId, projectileState.WeaponVisible);
                task.Finished = projectileState.BodyFinished;
                return;
            }
            if (!task.Executed)
            {
                body.Pose = BattlePose.Attacking;
                TryPlayBodyAnimation(actor, body.Profile.ThrownReturn, true, body.Pose, 62);
                task.Executed = true;
            }
            BattlePhysicalMarkerKind? marker = GetBodyBinding(actor).LastPoppedMarker;
            if (marker is BattlePhysicalMarkerKind.Catched or BattlePhysicalMarkerKind.Over || !actor.Alive)
            {
                if (_thrownWeapons.TryGetValue(actor.Initial.InstanceId, out ThrownWeaponRun run))
                    run.Projectile.FinishRestore();
                SetWeaponDrawingEnabled(actor.Initial.InstanceId, true);
                if (marker == BattlePhysicalMarkerKind.Catched)
                {
                    body.Pose = BattlePose.Idle;
                    TryPlayBodyAnimation(actor, body.Profile.Animation(BattlePose.Idle), true, body.Pose, 32);
                }
                task.Finished = true;
            }
        }

        private void PushThrownWeaponRestore(Actor actor)
        {
            PhysicalBodyState body = PhysicalBody(actor);
            body.Pushed = body.PushExecuted = false;
            actor.HurtUntil = checked((int)SimulationTick);
            actor.Dodging = false;
            RestartOrdinaryAbilityBodyAfterPush(actor);
            if (body.Tasks.Count > 0 && body.Tasks[0].DiscardOnPush)
                body.Tasks.RemoveAt(0);
            if (body.Tasks.Count > 0)
                body.Tasks[0].Executed = false;
            else if (actor.Movement != null)
            {
                body.PausedMovement = actor.Movement.Plan;
                body.PausedBlend = actor.Movement.Blend;
                body.PausedAnimation = GetBodyBinding(actor).CurrentAnimation;
                body.PausedPose = GetBodyBinding(actor).LogicalPose;
                body.PausedNativeAction = GetBodyBinding(actor).RequestedNativeActionIndex;
            }
            actor.Movement = null;
            body.Tasks.Insert(0, new PhysicalBodyTask { Kind = BattlePhysicalBodyKind.RestoreThrownWeapon, DiscardOnPush = true });
        }

        private void CancelOwnedThrownWeapon(Actor actor, ActionRun run)
        {
            // Natural command completion leaves the projectile independent.
            // An explicit cancellation must not destroy an older command's flight.
            if (_thrownWeapons.TryGetValue(actor.Initial.InstanceId, out ThrownWeaponRun thrown) && ReferenceEquals(thrown.Command, run))
                ClearActorThrownWeapon(actor.Initial.InstanceId);
        }

        private void ClearActorThrownWeapon(string actorId)
        {
            if (_thrownWeapons.Remove(actorId))
                SetWeaponDrawingEnabled(actorId, true);
        }

        private void ClearThrownWeapons()
        {
            foreach (ThrownWeaponRun run in _thrownWeapons.Values)
                SetWeaponDrawingEnabled(run.Source.Initial.InstanceId, true);
            _thrownWeapons.Clear();
        }
    }
}
