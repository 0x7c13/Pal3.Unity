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
        private readonly BattleStageScheduler _stage = new();

        public IReadOnlyList<BattleStageTaskSnapshot> StageTasks => _stage.Snapshot();

        private sealed class AbilityStageJob : IBattleStageJob
        {
            private readonly BattleSession _session;
            private readonly Actor _actor;
            private readonly ActionRun _run;

            internal AbilityStageJob(BattleSession session, Actor actor, ActionRun run)
            {
                _session = session;
                _actor = actor;
                _run = run;
            }

            public bool Advance(BattleStageContext context) => _session.AdvanceAbilityStage(_actor, _run, context);
        }

        private sealed class ReturnHomeStageJob : IBattleStageJob
        {
            private readonly BattleSession _session;
            private readonly Actor _actor;

            internal ReturnHomeStageJob(BattleSession session, Actor actor)
            {
                _session = session;
                _actor = actor;
            }

            public bool Advance(BattleStageContext context) =>
                !HasFormationMovement(_actor) && context.Tick >= _actor.HurtUntil;
        }

        private void ResumeOriginalAtb() => _coAttackAtbPaused = false;

        private int StageTaskCount(Actor actor) => _stage.CountOwner(actor.Initial.InstanceId) +
            (actor.Run != null && !actor.Run.StageHandle.HasValue ? 1 : 0);

        // System threads prepare a stage action before ATB/state/AI and stage traversal.
        private void PrepareStageActions()
        {
            PrepareFleeThreads();
            if (Status != BattleSessionStatus.Running)
                return;
            PrepareAiActions();
            PreparePhysicalThreads();
            PrepareCoAttackThreads();
            foreach (Actor actor in _actors.Where(actor => actor.Run is { SystemStarted: false }).OrderBy(actor => actor.Run.Id))
            {
                actor.Run.SystemStarted = true;
                if (actor.Run.Kind == BattleAction.Flee)
                    StartFleeAction(actor, actor.Run);
                else if (actor.Run.Kind == BattleAction.Transform)
                    StartPal3ATransformAction(actor, actor.Run);
                else if (!TryReplaceWithCoAttack(actor, actor.Run) && !TryStartPhysicalStage(actor, actor.Run))
                    StartMagicSwordAction(actor, actor.Run);
            }
            foreach (Actor actor in _actors.Where(actor => actor.Run?.EndRequested == true && actor.Run.Ability == null && !IsFleeStageRun(actor.Run) && !IsCoAttackRun(actor.Run) && !IsPhysicalRun(actor.Run)).ToArray())
                Cancel(actor, "physical stage received an end request");
            foreach (Actor actor in _actors.Where(actor => actor.Run?.Ability != null && !IsCoAttackRun(actor.Run)).OrderBy(actor => actor.Run.Id).ToArray())
            {
                ActionRun run = actor.Run;
                if (run == null)
                    continue;
                if (run.Casting)
                {
                    if (run.EndRequested || actor.Gauge < FullGauge)
                        Cancel(actor, "casting lost its first gauge");
                    continue;
                }
                if (run.StageHandle is { } handle)
                {
                    if ((_stage.IsFinished(handle) || !_stage.IsActive(handle)) && (run.EndRequested || SimulationTick - run.TimelineOriginTick >= run.Timing.MinimumEndTick))
                        Finish(actor);
                    continue;
                }
                ScheduleAbilityStage(actor, run);
            }
        }

        private void ScheduleAbilityStage(Actor actor, ActionRun run)
        {
            if (run.Ability == null || run.Casting || run.StageHandle.HasValue || IsCoAttackRun(run))
                return;
            if (_stage.TryAdd(actor.Initial.InstanceId, "ability", new AbilityStageJob(this, actor, run),
                BattleStageScheduler.TimeAt(SimulationTick), out BattleStageHandle handle, autoDelete: false))
            {
                run.StageHandle = handle;
                if (run.Kind == BattleAction.Item && !run.ItemConsumed)
                {
                    ConsumeItemAtStart(actor, run.Ability);
                    run.ItemConsumed = true;
                }
            }
            else
                Cancel(actor, "stage action allocation failed");
        }

        private bool AdvanceAbilityStage(Actor actor, ActionRun run, BattleStageContext context)
        {
            if (actor.Run != run || Status != BattleSessionStatus.Running)
                return true;
            if (IsMagicSwordAbility(run.Ability))
                return AdvanceMagicSwordStage(actor, run, context);
            if (_scenario.Variant == "PAL3A" && run.Ability?.Id == BattlePal3ARules.PengxuTransformSkill)
                return AdvancePal3APengxuStage(actor, run, context);
            if (TryAdvanceAbilityPresentation(actor, run, context, out bool presentationFinished))
                return presentationFinished;
            long stageStart = run.TimelineOriginTick + run.Timing.PreparationTicks;
            int elapsed = checked((int)(context.Tick - stageStart));
            if (elapsed < 0)
                return false;
            run.Elapsed = checked((int)(context.Tick - run.TimelineOriginTick));
            if (!run.StageStarted)
            {
                if (!run.ElementWheel && run.Ability.IsMagic && GetCostEstimate(new BattleCommand(actor.Initial.InstanceId, BattleAction.Skill,
                    run.Target, run.Ability.Id)).Any(cost => cost.Required > cost.Available))
                {
                    run.AbilityResolved = true;
                    return true;
                }
                if (HasFormationMovement(actor) || context.Tick < actor.HurtUntil || !TryGrabStageBody(actor, context.Handle))
                    return false;
                run.StageStarted = true;
                run.BodyElapsed = 0;
                run.BodyRestartPending = true;
            }
            while (run.NextHit < run.Timing.HitTicks.Count)
            {
                bool reached = run.Timing.NativeWorkTimeSeconds is { } seconds
                    ? (double)context.Time - BattleStageScheduler.TimeAt(stageStart) >= seconds
                    : elapsed >= run.Timing.HitTicks[run.NextHit];
                if (!reached)
                    break;
                run.NextHit++;
                if (actor.Alive && !actor.Escaped)
                    ResolveActionMarker(actor, run);
                if (actor.Run != run || Status != BattleSessionStatus.Running)
                    return true;
            }
            bool finished = run.EndRequested || run.NextHit >= run.Timing.HitTicks.Count && elapsed >= run.Timing.AnimationTicks &&
                run.BodyElapsed >= run.Timing.BodyAnimationTicks && context.Tick >= actor.HurtUntil &&
                context.Tick - run.TimelineOriginTick >= run.Timing.MinimumEndTick - 1;
            if (finished)
                ReleaseStageBody(actor, context.Handle);
            return finished;
        }

        private void AdvanceAbilityBody(Actor actor, ActionRun run)
        {
            if (HasActivePal3AFormBody(actor) || _scenario.Variant == "PAL3A" && run.Ability?.Id == BattlePal3ARules.PengxuTransformSkill)
                return;
            if (TryAdvanceExternalEffectBody(actor, run))
                return;
            if (TryAdvanceOrdinaryAbilityBody(actor, run))
                return;
            if (!run.StageStarted || SimulationTick < actor.HurtUntil || !actor.ModelAlive)
                return;
            if (run.BodyRestartPending)
                run.BodyRestartPending = false;
            else
                run.BodyElapsed++;
        }

        private void FixNativeState()
        {
            foreach (Actor actor in _actors)
            {
                if (actor.Hp <= 0)
                {
                    actor.Alive = false;
                    actor.States.Remove(BattleState.Dying);
                    actor.States[BattleState.Death] = new BattleStatusInstance(BattleState.Death, 0, short.MaxValue, true);
                }
                else if (actor.Hp < actor.Attributes[0] / 10)
                    actor.States[BattleState.Dying] = new BattleStatusInstance(BattleState.Dying, 0, short.MaxValue, true);
            }
        }

        private void UpdateModelLife(Actor actor)
        {
            if (actor.Escaped)
                return;
            if (actor.Alive)
            {
                if (!actor.ModelAlive && actor.Hp > 0)
                    actor.ModelAlive = true;
                return;
            }
            // FixState changes data life first. A lethal hit still plays the
            // queued block; the body becomes dead only when that work ends.
            if (actor.ModelAlive && SimulationTick >= actor.HurtUntil && !HasFormationMovement(actor) && !PhysicalBodyBusy(actor) &&
                (actor.Run == null || _catalog is IBattlePhysicalTimings && actor.Run.Ability == null || actor.Run.EndRequested || actor.Run.Ability != null &&
                    actor.Run.BodyElapsed >= actor.Run.Timing.BodyAnimationTicks))
            {
                actor.ModelAlive = false;
                actor.PoseStarted = checked((int)SimulationTick);
            }
        }

        private void RequestRoleEnd(Actor actor)
        {
            RequestPhysicalRoleEnd(actor);
            if (actor.Run != null)
                actor.Run.EndRequested = true;
            actor.States.Clear();
            actor.Guard = actor.Ready = false;
            actor.Gauge = actor.CastGauge = 0;
        }

        private bool QueueReturnHomeStage(Actor actor)
        {
            return _stage.TryAdd(actor.Initial.InstanceId, "return home", new ReturnHomeStageJob(this, actor),
                BattleStageScheduler.TimeAt(SimulationTick), out _);
        }

        private bool QueueNumber(Actor actor, int resource, int signedValue, float duration,
            bool blockBody, bool miss = false, bool dodgeBody = false, bool recovery = false)
        {
            if (Definitions == null)
                return false;
            var number = new BattleStageNumber(duration, context =>
            {
                int offset = _random.Next() % 32;
                // Freeze the native anchor at activation. Repainting and replay
                // must not resample an actor which has since moved or vanished.
                BattleMovementPoint anchor = miss || resource == 16 ? actor.Home : actor.Position;
                _events.Add(new BattleEvent
                {
                    Tick = Tick,
                    Kind = BattleEventKind.NumberShown,
                    Target = actor.Initial.InstanceId,
                    Resource = resource,
                    RequestedDelta = signedValue,
                    RandomState = _random.State,
                    NumberOffset = offset,
                    DurationSeconds = duration,
                    NumberActivationTick = context.Tick,
                    NumberStartedTime = blockBody || dodgeBody ? context.Time : context.CreatedTime,
                    NumberPosition = new BattleMovementPoint(anchor.X, miss ? anchor.Y + 80 : 50, anchor.Z),
                    NumberIsMiss = miss,
                    NumberIsRecovery = recovery,
                    Detail = miss ? "native miss" : "native resource number"
                });
            }, blockBody || dodgeBody ? _ => PushBodyBlock(actor, dodgeBody) : null);
            return _stage.TryAdd(actor.Initial.InstanceId, miss ? "miss number" : "resource number", number, BattleStageScheduler.TimeAt(SimulationTick), out _);
        }

        private void PushBodyBlock(Actor actor, bool dodge = false)
        {
            if (!actor.ModelAlive || Has(actor, BattleState.Paralysis) || Has(actor, BattleState.Sleep))
            {
                if (actor.ModelAlive && !dodge)
                    ShowOriginalHitFlash(actor);
                return;
            }
            actor.PoseStarted = (int)SimulationTick;
            actor.Dodging = dodge;
            int duration = dodge ? (_catalog as IBattleDodgeTimings)?.Dodge(actor.Initial.InstanceId) ?? 1 : _catalog.Hurt(actor.Initial.InstanceId);
            actor.HurtUntil = checked((int)SimulationTick + Math.Max(1, duration));
            if (PushPhysicalBody(actor, dodge))
                return;
            ActionRun run = actor.Run;
            if (run != null && !run.Casting)
            {
                if (run.Ability != null)
                {
                    if (run.BodyElapsed < run.Timing.BodyAnimationTicks)
                    {
                        run.BodyElapsed = 0;
                        run.BodyRestartPending = true;
                    }
                }
                else if (run.Kind is BattleAction.Attack or BattleAction.Defend)
                {
                    bool afterHit = run.Kind == BattleAction.Attack && run.NextHit > 0;
                    run.DiscardRemainingHits = afterHit;
                    if (!afterHit)
                        run.Elapsed = 0;
                    Add(BattleEventKind.Interrupted, actor.Initial.InstanceId, run.Target, run.Id,
                        afterHit ? "body hurt after work: discard remaining physical work" : "body task restarts after hurt");
                }
            }
            if (actor.Movement?.Finished == false)
            {
                BattleMovementPlan old = actor.Movement.Plan;
                actor.Movement = new BattleMovement(_scenario.Variant, new BattleMovementPlan(old.Kind, actor.Position, old.To,
                    old.FlyUpTick, old.FlyDownTick, old.OverTick, old.TargetBlend, actor.Movement.Blend));
            }
        }

        private void CancelStageAction(ActionRun run)
        {
            CancelFleeStage(run);
            CancelPhysicalStage(run);
            CancelAbilityPresentation(run);
            CancelMagicSwordStage(run);
            CancelCoAttackStage(run);
            if (run?.StageHandle is { } handle && run.Kind != BattleAction.Transform)
            {
                Actor owner = _actors.Find(actor => actor.Run == run);
                if (owner != null)
                    ReleaseStageBody(owner, handle);
                _stage.Cancel(handle);
            }
        }

        private void StopStage()
        {
            ClearStageFeedback();
            foreach (BattleStageTaskSnapshot task in _stage.Snapshot())
                _stage.Cancel(task.Handle);
            _bodyStageOwners.Clear();
            StopFleeStages();
            ClearThrownWeapons();
        }
    }
}
