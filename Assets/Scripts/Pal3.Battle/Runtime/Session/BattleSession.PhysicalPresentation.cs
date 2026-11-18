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
        private sealed class PhysicalBodyTask
        {
            internal BattlePhysicalBodyKind Kind;
            internal BattleMovementPoint Destination;
            internal Actor Target;
            internal ActionRun Run;
            internal BattlePhysicalAnimation Animation;
            internal bool Executed, Finished, DiscardOnPush;
        }

        private sealed class PhysicalBodyState
        {
            internal readonly List<PhysicalBodyTask> Tasks = new();
            internal BattlePhysicalStageProfile Profile;
            internal long Owner;
            internal bool Pushed, PushExecuted, PushDodge;
            internal BattleMovementPlan PausedMovement;
            internal BattlePhysicalAnimation PausedAnimation;
            internal BattlePose PausedPose;
            internal int PausedNativeAction;
            internal float PausedBlend;
            internal BattlePose Pose = BattlePose.Idle;
            internal int LocalTick;
            internal float BladeSeconds = .01f;
        }

        private sealed class PhysicalStageRun : IBattleStageJob
        {
            internal BattleSession Session;
            internal Actor Source, Protected, Attacker;
            internal ActionRun Run;
            internal BattlePhysicalStageProfile Profile;
            internal BattleStageHandle Handle;
            internal readonly int[] Status = new int[5];
            internal readonly bool[] Executed = new bool[5];
            internal bool Protection, RevivalReturn, EndRequested, Finished, CanStartProtection = true;

            public bool Advance(BattleStageContext context) => Session.AdvancePhysicalStage(this);
        }

        private readonly Dictionary<long, PhysicalStageRun> _physicalStages = new();
        private readonly List<PhysicalStageRun> _protectionStages = new();
        private readonly Dictionary<string, PhysicalBodyState> _physicalBodies = new(StringComparer.Ordinal);
        private int _physicalProtectionTarget = -1;

        public string ProtectionTarget => ActorAtNativeIndex(_physicalProtectionTarget)?.Initial.InstanceId;

        private bool IsPhysicalRun(ActionRun run) => run != null && _physicalStages.ContainsKey(run.Id);

        private bool PhysicalBodyBusy(Actor actor) => _physicalBodies.TryGetValue(actor.Initial.InstanceId, out PhysicalBodyState body) && (body.Pushed || body.PausedMovement != null || body.Tasks.Count > 0);

        private PhysicalBodyState PhysicalBody(Actor actor)
        {
            if (_physicalBodies.TryGetValue(actor.Initial.InstanceId, out PhysicalBodyState body))
                return body;
            if (_catalog is not IBattlePhysicalTimings catalog)
                return null;
            body = new PhysicalBodyState { Profile = catalog.Physical(actor.Initial.InstanceId) };
            _physicalBodies.Add(actor.Initial.InstanceId, body);
            return body;
        }

        // Native IsIdle counts every queued role task, including ability and
        // form bodies. PhysicalBodyBusy stays limited to the physical queue so
        // an ability can advance its own body without waiting on itself.
        private bool PhysicalBodyIdle(Actor actor) => !PhysicalBodyBusy(actor) && !_fallbackFleeBodies.ContainsKey(actor.Initial.InstanceId) && !HasFormationMovement(actor) &&
            !HasActiveOrdinaryAbilityBody(actor) && !HasActivePal3AFormBody(actor) && SimulationTick >= actor.HurtUntil;

        private bool ActionBodyIdle(Actor actor) => PhysicalBodyIdle(actor) &&
            _pal3AForms.GetValueOrDefault(actor.Initial.InstanceId)?.Pending != true;

        private bool TryStartPhysicalStage(Actor actor, ActionRun run)
        {
            if (run == null || run.Kind is not (BattleAction.Attack or BattleAction.Defend) || _catalog is not IBattlePhysicalTimings || IsCoAttackRun(run))
                return false;
            PhysicalBodyState body = PhysicalBody(actor);
            var stage = new PhysicalStageRun { Session = this, Source = actor, Run = run, Profile = body.Profile };
            float now = BattleStageScheduler.TimeAt(SimulationTick);
            if (!_stage.TryAdd(actor.Initial.InstanceId, "physical attack", stage, now, out BattleStageHandle handle, autoDelete: false))
            {
                Cancel(actor, "original physical stage allocation failed");
                return true;
            }
            stage.Handle = handle;
            run.StageHandle = handle;
            run.AttackPlan = null;
            actor.Gauge = actor.CastGauge = 0;
            actor.Ready = false;
            _physicalStages.Add(run.Id, stage);
            return true;
        }

        private void PreparePhysicalThreads()
        {
            foreach (PhysicalStageRun stage in _physicalStages.Values.Where(stage => stage.Finished).ToArray())
            {
                _physicalStages.Remove(stage.Run.Id);
                _stage.Cancel(stage.Handle);
                if (stage.Source.Run == stage.Run)
                    Finish(stage.Source);
            }
            _protectionStages.RemoveAll(stage => !_stage.IsActive(stage.Handle));
        }

        private bool AdvancePhysicalStage(PhysicalStageRun stage)
        {
            for (int index = 0; index < stage.Status.Length; index++)
            {
                if (index > 0 && stage.Status[index - 1] == 0)
                    break;
                if (stage.Status[index] == 4)
                {
                    if (stage.Status.All(status => status == 4))
                        return stage.Finished = true;
                    continue;
                }
                if (stage.Status[index] == 0)
                {
                    if (index > 0 && stage.Status[index - 1] != 4)
                        continue;
                    stage.Status[index] = 2;
                }
                if (AdvancePhysicalSubtask(stage, index))
                    stage.Status[index] = 4;
            }
            return false;
        }

        private bool AdvancePhysicalSubtask(PhysicalStageRun stage, int index)
        {
            Actor actor = stage.Source;
            PhysicalBodyState body = PhysicalBody(actor);
            bool require = stage.EndRequested || stage.Run?.EndRequested == true;
            if (index == 0)
            {
                if (!TryGrabStageBody(actor, stage.Handle))
                    return false;
                body.Owner = stage.Handle.Id;
                return true;
            }
            if (index == 4)
            {
                ReleaseStageBody(actor, stage.Handle);
                return true;
            }
            if (body.Owner != stage.Handle.Id)
                return true;
            if (stage.RevivalReturn)
                return AdvanceRevivalReturn(stage, index);
            if (stage.Protection)
                return AdvanceProtectionSubtask(stage, index, require);
            if (stage.Run.Kind == BattleAction.Defend)
            {
                if (index != 1)
                    return true;
                if (!stage.Executed[index] && PhysicalBodyIdle(actor))
                {
                    QueuePhysicalTask(actor, new PhysicalBodyTask { Kind = BattlePhysicalBodyKind.StartGuard });
                    stage.Executed[index] = true;
                }
                return require || PhysicalBodyIdle(actor);
            }
            if (!stage.Executed[index] && PhysicalBodyIdle(actor))
            {
                if (index == 1)
                    QueuePhysicalApproach(stage);
                else if (index == 2)
                    QueuePhysicalTask(actor, new PhysicalBodyTask { Kind = BattlePhysicalBodyKind.Attack, Run = stage.Run, Target = TargetForPhysicalTurn(actor, stage.Run), Animation = stage.Profile.Animation(BattlePose.Attacking) });
                else
                    QueuePhysicalReturn(stage, stage.Profile.Return(IsPal3ATransformed(actor.Initial.InstanceId)));
                stage.Executed[index] = true;
            }
            if (index == 1)
            {
                if (actor.Initial.Side == BattleSide.Ally && !PhysicalTargetsAlive(stage.Run))
                    return true;
                TryStartProtection(stage);
            }
            return PhysicalBodyIdle(actor) || require && (index != 3 || actor.Hp == 0);
        }

        private bool PhysicalTargetsAlive(ActionRun run) => run.Targets.Any(id => _byId[id].Alive);

        private Actor TargetForPhysicalTurn(Actor actor, ActionRun run) => ActionRange(actor, null) == BattleTargetRange.OpponentSingle &&
            run.Target != null && _byId.TryGetValue(run.Target, out Actor target) ? target : null;

        private BattleMovementPoint PhysicalHome(int index)
        {
            Actor actor = ActorAtNativeIndex(index);
            if (actor != null)
                return actor.Home;
            BattleSide side = index < 5 ? BattleSide.Ally : BattleSide.Enemy;
            Actor representative = _actors.First(candidate => candidate.Initial.Side == side);
            return ((IBattleStageGeometry)_catalog).FormationHome(representative.Initial.InstanceId, (BattleSlot)(index < 5 ? index : index - 5));
        }

        private float PhysicalRadius(Actor actor)
        {
            PhysicalBodyState body = PhysicalBody(actor);
            return (GetBodyBinding(actor).CurrentAnimation?.HalfDepth ?? 0f) -
                (body.Profile.Identity == (body.Profile.Variant == "PAL3" ? 1 : 3) ? 10f : 0f);
        }

        private void QueuePhysicalApproach(PhysicalStageRun stage)
        {
            Actor actor = stage.Source;
            BattleTargetRange range = ActionRange(actor, null);
            bool multi = range is BattleTargetRange.OpponentAll or BattleTargetRange.OpponentRow or BattleTargetRange.OpponentColumn;
            int[] targets = stage.Run.Targets.Select(id => NativeActorIndex(_byId[id])).OrderBy(index => index).ToArray();
            var slots = _actors.Where(candidate => candidate.Initial.Side == BattleSide.Ally).ToDictionary(NativeActorIndex, candidate => candidate.Slot);
            BattlePhysicalAnchor anchor = multi ? BattlePhysicalGeometry.Anchor(targets, slots, _scenario.SceneElement) : new BattlePhysicalAnchor(NativeActorIndex(_byId[stage.Run.Target]));
            Actor target = ActorAtNativeIndex(anchor.First);
            float radius = target != null ? PhysicalRadius(target) : 0;
            BattleMovementPoint destination = BattlePhysicalGeometry.Approach(actor.Position, PhysicalRadius(actor), PhysicalHome(anchor.First), radius,
                anchor.Second.HasValue ? PhysicalHome(anchor.Second.Value) : null,
                spear: multi && stage.Profile.Identity == 4 && stage.Profile.Variant == "PAL3" && stage.Profile.WeaponType == 1,
                doubleRadius: !multi && stage.Profile.ResourceId is 7364 or 7381);
            float heading = BattleMovement.FacingBetween(actor.Position, destination);
            foreach (BattlePhysicalBodyKind kind in stage.Profile.Approach(IsPal3ATransformed(actor.Initial.InstanceId)))
                QueuePhysicalTask(actor, new PhysicalBodyTask
                {
                    Kind = kind,
                    Destination = kind == BattlePhysicalBodyKind.Turn ? new BattleMovementPoint(heading, 0, 0) : destination,
                    Target = multi ? null : _byId[stage.Run.Target],
                    Run = stage.Run,
                    Animation = stage.Profile.Animation(BattlePose.Moving)
                });
        }

        private void QueuePhysicalReturn(PhysicalStageRun stage, IReadOnlyList<BattlePhysicalBodyKind> kinds)
        {
            foreach (BattlePhysicalBodyKind kind in kinds)
                QueuePhysicalTask(stage.Source, new PhysicalBodyTask
                {
                    Kind = kind,
                    Destination = kind == BattlePhysicalBodyKind.Turn ? new BattleMovementPoint(BattleFacingRules.Initial(stage.Source.Initial.Side), 0, 0) : stage.Source.Home,
                    Animation = stage.Profile.Animation(BattlePose.Moving)
                });
        }

        private void QueuePhysicalTask(Actor actor, PhysicalBodyTask task)
        {
            PhysicalBodyState body = PhysicalBody(actor);
            if (body.Tasks.Count >= 63)
                throw new InvalidOperationException("The original body task pool is full.");
            body.Tasks.Add(task);
        }

        // One role pass executes one task, removes at most one finished task,
        // and then clears pushed tasks that have already performed their work.
        private bool AdvancePhysicalBody(Actor actor)
        {
            PhysicalBodyState body = PhysicalBody(actor);
            if (body == null)
                return false;
            body.LocalTick = GetBodyBinding(actor).LocalTick;
            if (body.Pushed)
            {
                if (!body.PushExecuted)
                {
                    body.PushExecuted = true;
                    if (!body.PushDodge)
                    {
                        PlayHurtBodySound(actor, body);
                        ShowOriginalHitFlash(actor);
                    }
                    body.BladeSeconds = .25f;
                    BattlePose pose = body.PushDodge ? BattlePose.Dodging : actor.Guard ? BattlePose.GuardHurt : BattlePose.Hurt;
                    if (!Has(actor, BattleState.Paralysis) || body.PushDodge)
                    {
                        if (body.PushDodge ? GetBodyBinding(actor).RequestedNativeActionIndex == 44 : GetBodyBinding(actor).RequestedNativeActionIndex is 33 or 43 or 54)
                        {
                            ResetBodyAnimation(actor);
                            pose = GetBodyBinding(actor).LogicalPose;
                        }
                        else if (!body.PushDodge || actor.Initial.Side == BattleSide.Ally)
                            TryPlayBodyAnimation(actor, body.Profile.Animation(pose), false, pose, body.Profile.NativeAction(pose));
                    }
                    body.Pose = pose;
                    return true;
                }
                if (GetBodyBinding(actor).LastPoppedMarker == BattlePhysicalMarkerKind.Over ||
                    (body.PushDodge ? !actor.Alive : Has(actor, BattleState.Paralysis)))
                {
                    body.Pushed = body.PushExecuted = false;
                    body.BladeSeconds = .01f;
                    actor.HurtUntil = checked((int)SimulationTick);
                    actor.Dodging = false;
                    if (!actor.Alive)
                        actor.ModelAlive = false;
                }
                return true;
            }
            if (body.Tasks.Count == 0 && body.PausedMovement is { } paused)
            {
                body.PausedMovement = null;
                actor.Movement = new BattleMovement(_scenario.Variant, new BattleMovementPlan(paused.Kind, actor.Position, paused.To,
                    paused.FlyUpTick, paused.FlyDownTick, paused.OverTick, paused.TargetBlend, body.PausedBlend));
                actor.Movement.SetHeading(actor.FacingDegrees);
                body.Pose = body.PausedPose;
                TryPlayBodyAnimation(actor, body.PausedAnimation, true, body.Pose, body.PausedNativeAction);
                AdvanceBodyMovement(actor, actor.Alive, BattleMovementMessage.None);
                actor.Position = actor.Movement.Position;
                if (actor.Movement.FacingDegrees is { } facing)
                    actor.FacingDegrees = facing;
                return true;
            }
            if (body.Tasks.Count == 0)
            {
                EnsureProperBodyAnimation(actor);
                return body.Owner != 0;
            }
            PhysicalBodyTask task = body.Tasks[0];
            AdvancePhysicalTask(actor, body, task);
            body.LocalTick = GetBodyBinding(actor).LocalTick;
            if (task.Finished)
            {
                body.Tasks.RemoveAt(0);
                actor.Movement = null;
            }
            return true;
        }

        private void AdvancePhysicalTask(Actor actor, PhysicalBodyState body, PhysicalBodyTask task)
        {
            if (task.Kind is BattlePhysicalBodyKind.FleeWait or BattlePhysicalBodyKind.EnemyFlee)
            {
                AdvanceFleeBody(actor, body, task);
                return;
            }
            if (task.Kind is BattlePhysicalBodyKind.Turn or BattlePhysicalBodyKind.TurnToPosition or BattlePhysicalBodyKind.TurnToRole)
            {
                actor.FacingDegrees = task.Kind == BattlePhysicalBodyKind.Turn ? task.Destination.X :
                    BattleMovement.FacingBetween(actor.Position, task.Kind == BattlePhysicalBodyKind.TurnToRole && task.Target != null ? task.Target.Position : task.Destination);
                if (task.Kind == BattlePhysicalBodyKind.Turn)
                {
                    body.Pose = BattlePose.Idle;
                    TryPlayBodyAnimation(actor, body.Profile.Animation(BattlePose.Idle), true, BattlePose.Idle, body.Profile.NativeAction(BattlePose.Idle));
                }
                task.Finished = true;
                return;
            }
            if (task.Kind == BattlePhysicalBodyKind.StartGuard)
            {
                if (!task.Executed)
                {
                    body.BladeSeconds = .01f;
                    actor.FacingDegrees = BattleFacingRules.Initial(actor.Initial.Side);
                    if (actor.Initial.Side == BattleSide.Ally)
                    {
                        body.Pose = BattlePose.GuardEntry;
                        TryPlayBodyAnimation(actor, body.Profile.Animation(body.Pose), false, body.Pose, body.Profile.NativeAction(body.Pose));
                    }
                    task.Executed = true;
                    return;
                }
                if (!actor.Alive || GetBodyBinding(actor).LastPoppedMarker == BattlePhysicalMarkerKind.Over)
                {
                    task.Finished = true;
                    body.BladeSeconds = .01f;
                    actor.Guard = true;
                }
                return;
            }
            if (task.Kind == BattlePhysicalBodyKind.Attack)
            {
                AdvancePhysicalAttackBody(actor, body, task);
                return;
            }
            if (task.Kind == BattlePhysicalBodyKind.Protect)
            {
                AdvancePhysicalGuardBody(actor, body, task);
                return;
            }
            if (task.Kind is BattlePhysicalBodyKind.ThrownWeapon or BattlePhysicalBodyKind.RestoreThrownWeapon)
            {
                AdvanceThrownWeaponBody(actor, body, task);
                return;
            }
            if (!task.Executed)
            {
                BattleMovementKind kind = task.Kind switch
                {
                    BattlePhysicalBodyKind.Fly => BattleMovementKind.Fly,
                    BattlePhysicalBodyKind.FlyRotate => BattleMovementKind.FlyRotate,
                    BattlePhysicalBodyKind.NpcFly => BattleMovementKind.NpcFly,
                    BattlePhysicalBodyKind.Teleport => BattleMovementKind.Teleport,
                    BattlePhysicalBodyKind.FastRun => BattleMovementKind.FastRun,
                    _ => BattleMovementKind.Run
                };
                actor.Movement = new BattleMovement(_scenario.Variant, new BattleMovementPlan(kind, actor.Position, task.Destination,
                    targetBlend: Has(actor, BattleState.Invisible) ? .44f : 1f));
                actor.Movement.SetHeading(actor.FacingDegrees);
                body.Pose = kind == BattleMovementKind.Teleport ? BattlePose.Idle : BattlePose.Moving;
                bool nearAtStart = BattleMovement.Distance(actor.Position, task.Destination) < (kind == BattleMovementKind.FastRun ? 4f : 2f) &&
                    !(body.Profile.Variant == "PAL3A" && kind == BattleMovementKind.Fly);
                if (!nearAtStart)
                    TryPlayBodyAnimation(actor, body.Profile.Animation(body.Pose), true, body.Pose, body.Profile.NativeAction(body.Pose));
                body.BladeSeconds = kind == BattleMovementKind.FlyRotate ? .4f : .01f;
                task.Executed = true;
                Add(BattleEventKind.MovementStarted, actor.Initial.InstanceId, detail: kind.ToString());
            }
            actor.Movement.SetTargetBlend(Has(actor, BattleState.Invisible) ? .44f : 1f);
            AdvanceBodyMovement(actor, actor.Alive, CurrentBodyMovementMessage(actor));
            actor.Position = actor.Movement.Position;
            if (actor.Movement.FacingDegrees is { } facing)
                actor.FacingDegrees = facing;
            if (task.Finished = actor.Movement.Finished)
                Add(BattleEventKind.MovementEnded, actor.Initial.InstanceId);
        }

        private void AdvancePhysicalAttackBody(Actor actor, PhysicalBodyState body, PhysicalBodyTask task)
        {
            if (!task.Executed)
            {
                body.BladeSeconds = .25f;
                task.Finished = !PhysicalTargetsAlive(task.Run) || body.Profile.UsesThrownWeaponFor(IsPal3ATransformed(actor.Initial.InstanceId));
                if (!body.Profile.UsesThrownWeaponFor(IsPal3ATransformed(actor.Initial.InstanceId)))
                {
                    body.Pose = BattlePose.Attacking;
                    TryPlayBodyAnimation(actor, task.Animation, false, BattlePose.Attacking, body.Profile.NativeAction(BattlePose.Attacking));
                    if (body.Profile.TurnsAtAttack && task.Target != null)
                        actor.FacingDegrees = BattleMovement.FacingBetween(actor.Position, task.Target.Position);
                }
                task.Executed = true;
                return;
            }
            BattlePhysicalMarkerKind? marker = GetBodyBinding(actor).LastPoppedMarker;
            if (marker is >= BattlePhysicalMarkerKind.Work and <= BattlePhysicalMarkerKind.Work3)
            {
                task.DiscardOnPush = true;
                PlayAttackBodySound(actor, body, marker.Value);
                int count = body.Profile.DamageCount(marker.Value, IsPal3ATransformed(actor.Initial.InstanceId));
                // Both native bodies dispatch all repeats of this marker before checking source life.
                for (int hit = 0; hit < count; hit++)
                {
                    task.Run.NextHit++;
                    ResolveActionMarker(actor, task.Run);
                }
            }
            if (!actor.Alive || marker == BattlePhysicalMarkerKind.Over)
                task.Finished = true;
        }

        private bool PushPhysicalBody(Actor actor, bool dodge)
        {
            PhysicalBodyState body = PhysicalBody(actor);
            if (body == null)
                return false;
            body.Pushed = true;
            body.PushExecuted = false;
            body.PushDodge = dodge;
            RestartOrdinaryAbilityBodyAfterPush(actor);
            if (body.Tasks.Count == 0 && actor.Movement != null)
            {
                body.PausedMovement = actor.Movement.Plan;
                body.PausedBlend = actor.Movement.Blend;
                body.PausedAnimation = GetBodyBinding(actor).CurrentAnimation;
                body.PausedPose = GetBodyBinding(actor).LogicalPose;
                body.PausedNativeAction = GetBodyBinding(actor).RequestedNativeActionIndex;
            }
            if (body.Tasks.Count > 0 && body.Tasks[0].DiscardOnPush)
                body.Tasks.RemoveAt(0);
            if (body.Tasks.Count > 0)
                body.Tasks[0].Executed = false;
            actor.Movement = null;
            return true;
        }

        private void ClearPhysicalBody(Actor actor)
        {
            StopMovementBodySound(actor);
            if (!_physicalBodies.TryGetValue(actor.Initial.InstanceId, out PhysicalBodyState body))
                return;
            body.Tasks.Clear();
            body.Owner = 0;
            body.Pushed = false;
            body.PausedMovement = null;
            actor.Movement?.Stop();
            actor.Movement = null;
            actor.MovementQueue.Clear();
            actor.HurtUntil = checked((int)SimulationTick);
            actor.Dodging = false;
        }

        private void CancelPhysicalStage(ActionRun run)
        {
            if (run == null || !_physicalStages.Remove(run.Id, out PhysicalStageRun stage))
                return;
            _stage.Cancel(stage.Handle);
            ReleaseStageBody(stage.Source, stage.Handle);
        }

        private void RequestPhysicalRoleEnd(Actor actor)
        {
            foreach (PhysicalStageRun stage in _protectionStages.Where(stage => stage.Source == actor))
                stage.EndRequested = true;
            if (NativeActorIndex(actor) == _physicalProtectionTarget)
                _physicalProtectionTarget = -1;
        }

        private void CapturePhysicalPose(Actor actor, ref BattlePose pose, ref int localTick)
        {
            if (!_physicalBodies.TryGetValue(actor.Initial.InstanceId, out PhysicalBodyState body) || body.Owner == 0 && body.Tasks.Count == 0 && !body.Pushed)
                return;
            pose = body.Pose;
            localTick = body.LocalTick;
        }
    }
}
