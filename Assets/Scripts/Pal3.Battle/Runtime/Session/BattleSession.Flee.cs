// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BattleFleeResultSnapshot
    {
        public string Actor
        {
            get;
        }

        public bool Success
        {
            get;
        }

        public float StartedAtSeconds
        {
            get;
        }

        internal BattleFleeResultSnapshot(string actor, bool success, float started)
        {
            Actor = actor;
            Success = success;
            StartedAtSeconds = started;
        }
    }

    public sealed partial class BattleSession
    {
        private sealed class FleeStage : IBattleStageJob
        {
            internal BattleSession Session;
            internal Actor Actor;
            internal ActionRun Run;
            internal BattleStageHandle Handle;
            internal bool Attempt, Grabbed, BodyQueued, BodyFinished, Finished, Interrupted, Running;
            internal bool FallbackBodyStarted, WaitInitialized;

            public bool Advance(BattleStageContext context) => Session.AdvanceFleeStage(this, context);
        }

        private sealed class EnemyFleeStage : IBattleStageJob
        {
            internal BattleSession Session;
            internal Actor Actor;
            internal bool Grabbed, BodyQueued;

            public bool Advance(BattleStageContext context)
            {
                if (Grabbed && !Session.OwnsStageBody(Actor, context.Handle))
                    return true;
                if (!Grabbed)
                {
                    if (!Session.TryGrabStageBody(Actor, context.Handle))
                        return false;
                    Grabbed = true;
                }
                if (!BodyQueued && Session.PhysicalBodyIdle(Actor))
                {
                    Session.QueueFleeBody(Actor, BattlePhysicalBodyKind.EnemyFlee, null);
                    BodyQueued = true;
                }
                if (!Actor.Escaped && (Actor.Alive || !Session.PhysicalBodyIdle(Actor)))
                    return false;
                Session.ReleaseStageBody(Actor, context.Handle);
                return true;
            }
        }

        private sealed class FleeResultStage : IBattleStageJob
        {
            internal BattleSession Session;
            internal string Actor;
            internal bool Success;
            private BattleFleeResultSnapshot _message;

            public bool Advance(BattleStageContext context)
            {
                if (_message == null)
                {
                    _message = new BattleFleeResultSnapshot(Actor, Success, context.CreatedTime);
                    Session.FleeResult = _message;
                    Session.ShowStageMessage(this, BattleSide.Ally, languageKey: Success ? "FleeSuccess" : "FleeFail", handle: context.Handle);
                }
                // Native ProcShowFleeResult uses strict > against the float clock.
                if (context.Time - context.CreatedTime <= 3f)
                    return false;
                Session.FleeResult = null;
                Session.HideStageMessage(this);
                return true;
            }
        }

        private readonly Dictionary<long, FleeStage> _fleeStages = new();
        private readonly Dictionary<string, BattlePhysicalBodyKind> _fallbackFleeBodies = new(StringComparer.Ordinal);
        private bool _fleeAlwaysSucceeds, _fleeAtbPaused;

        public bool FleeAlwaysSucceeds => _fleeAlwaysSucceeds;

        public BattleFleeResultSnapshot FleeResult
        {
            get; private set;
        }

        private bool IsFleeWaiting(Actor actor) => _fleeStages.Values.Any(stage => stage.Actor == actor && !stage.Attempt);

        private bool IsFleeStageRun(ActionRun run) => run != null && _fleeStages.ContainsKey(run.Id);

        private bool IsInterruptedFleeRun(ActionRun run) => run != null && _fleeStages.TryGetValue(run.Id, out FleeStage stage) && stage.Interrupted;

        private bool IsFleeing(Actor actor) => actor.EnemyFleeing;

        private bool AllFleeOpponentsGone => !_actors.Any(actor => actor.Initial.Side == BattleSide.Enemy && actor.Alive && !actor.Escaped);

        private void StartFleeAction(Actor actor, ActionRun run)
        {
            var stage = new FleeStage { Session = this, Actor = actor, Run = run };
            if (!ScheduleFleeStage(stage))
                Cancel(actor, "original escape stage allocation failed");
        }

        private bool ScheduleFleeStage(FleeStage stage)
        {
            if (!_stage.TryAdd(stage.Actor.Initial.InstanceId, stage.Attempt ? "party flee" : "flee wait", stage,
                BattleStageScheduler.TimeAt(SimulationTick), out BattleStageHandle handle, autoDelete: false))
                return false;
            stage.Handle = handle;
            stage.Run.StageHandle = handle;
            _fleeStages[stage.Run.Id] = stage;
            return true;
        }

        private void PrepareFleeThreads()
        {
            foreach (FleeStage stage in _fleeStages.Values.ToArray())
            {
                Actor actor = stage.Actor;
                if (!stage.Attempt && !stage.WaitInitialized)
                {
                    stage.WaitInitialized = true;
                    if (_fleeAlwaysSucceeds)
                    {
                        actor.Gauge = 0;
                        actor.CastGauge = FullCastGauge;
                        stage.Run.Casting = false;
                    }
                }
                if (!stage.Finished)
                {
                    if (!stage.Attempt && Has(actor, BattleState.Forbidden) && !_fleeAlwaysSucceeds)
                        InterruptFleeWaiting(actor, "forbidden ended escape waiting");
                    continue;
                }
                _stage.Cancel(stage.Handle);
                _fleeStages.Remove(stage.Run.Id);
                stage.Run.StageHandle = null;
                if (stage.Attempt)
                {
                    actor.Gauge = actor.CastGauge = 0;
                    if (stage.Running)
                    {
                        if (!_actors.Any(member => member.Initial.Side == actor.Initial.Side && member.Hp > 0))
                        {
                            ReleaseRunningFleeBodies(stage);
                            CompleteFleeThread(stage);
                            continue;
                        }
                        _partyEscaped = true;
                        Add(BattleEventKind.Escaped, actor.Initial.InstanceId, actionId: stage.Run.Id, detail: "party escape run completed");
                        End(BattleSessionStatus.Completed, null, "party escaped");
                        return;
                    }
                    CompleteFleeThread(stage);
                }
                else if (AllFleeOpponentsGone)
                    CompleteFleeThread(stage);
                else if (actor.CastGauge >= FullCastGauge || _fleeAlwaysSucceeds)
                {
                    var attempt = new FleeStage { Session = this, Actor = actor, Run = stage.Run, Attempt = true };
                    if (!ScheduleFleeStage(attempt))
                        CompleteFleeThread(stage);
                }
                else
                {
                    ShowFleeResult(actor, false);
                    CompleteFleeThread(stage);
                }
            }
        }

        private void CompleteFleeThread(FleeStage stage)
        {
            if (stage.Actor.Run == stage.Run)
                Finish(stage.Actor);
            else
                Add(BattleEventKind.ActionEnded, stage.Actor.Initial.InstanceId, actionId: stage.Run.Id);
        }

        private bool AdvanceFleeStage(FleeStage stage, BattleStageContext context)
        {
            Actor actor = stage.Actor;
            if (!stage.Grabbed)
            {
                if (!TryGrabStageBody(actor, context.Handle))
                    return false;
                stage.Grabbed = true;
            }
            if (!stage.Attempt)
            {
                if (!stage.BodyQueued && PhysicalBodyIdle(actor))
                {
                    QueueFleeBody(actor, BattlePhysicalBodyKind.FleeWait, stage.Run);
                    stage.BodyQueued = true;
                }
                if (!stage.BodyFinished)
                    return false;
            }
            else
            {
                if (!stage.Running)
                {
                    if (AllFleeOpponentsGone)
                    {
                        // Native victory replaces the stage without rolling escape.
                        // Our outcome drain must first release this pending attempt.
                        ReleaseStageBody(actor, context.Handle);
                        return stage.Finished = true;
                    }
                    Actor[] opponents = _actors.Where(target => target.Initial.Side != actor.Initial.Side).ToArray();
                    int chance = _effectRules.FleeSuccessPercent(actor.Level, EffectiveAttributes(actor)[6],
                        opponents.Select(target => target.Level).ToArray(), opponents.Select(target => EffectiveAttributes(target)[6]).ToArray());
                    // Both published executables roll and show the result before checking for an idle
                    // body. A successful roll while busy leaves the attempt pending for the next pass.
                    bool success = _random.Probability(chance);
                    if (_fleeAlwaysSucceeds)
                        success = true;
                    if (_scenario.ScriptBattle)
                        success = false;
                    ShowFleeResult(actor, success);
                    if (!success)
                    {
                        ReleaseStageBody(actor, context.Handle);
                        return stage.Finished = true;
                    }
                    if (!PhysicalBodyIdle(actor))
                        return false;
                    foreach (Actor ally in _actors.Where(value => value.Initial.Side == actor.Initial.Side && value.Alive).ToArray())
                    {
                        if (ally != actor)
                        {
                            Cancel(ally, "successful party escape replaced the actor's system threads");
                            foreach (FleeStage other in _fleeStages.Values.Where(value => value.Actor == ally).ToArray())
                                _fleeStages.Remove(other.Run.Id);
                            foreach (BattleStageTaskSnapshot task in _stage.Snapshot().Where(task => task.Owner == ally.Initial.InstanceId))
                                _stage.Cancel(task.Handle);
                            _bodyStageOwners.Remove(ally.Initial.InstanceId);
                            TryGrabStageBody(ally, context.Handle);
                            ally.Gauge = ally.CastGauge = 0;
                        }
                        ClearPhysicalBody(ally);
                        ally.Movement?.Stop();
                        ally.Movement = null;
                        ally.MovementQueue.Clear();
                        ally.HurtUntil = (int)SimulationTick;
                        ally.Ready = false;
                        if (_formationController == ally.Initial.InstanceId)
                        {
                            _formationController = null;
                            _formationInitialSlots = null;
                        }
                        QueueFleeBody(ally, BattlePhysicalBodyKind.Run, stage.Run);
                    }
                    stage.Running = true;
                    _fleeAtbPaused = true;
                }
                if (!PhysicalBodyIdle(actor))
                    return false;
            }
            ReleaseStageBody(actor, context.Handle);
            return stage.Finished = true;
        }

        private void ShowFleeResult(Actor actor, bool success)
        {
            _stage.TryAdd(success ? "__party_flee_result" : actor.Initial.InstanceId, "flee result",
                new FleeResultStage { Session = this, Actor = actor.Initial.InstanceId, Success = success },
                BattleStageScheduler.TimeAt(SimulationTick), out _);
            Add(success ? BattleEventKind.Escaped : BattleEventKind.Miss, actor.Initial.InstanceId,
                detail: success ? "party escape succeeded; run begins after the body becomes idle" : "escape failed");
        }

        private void InterruptFleeWaiting(Actor actor, string reason, int firstGauge = 0)
        {
            foreach (FleeStage stage in _fleeStages.Values.Where(value => value.Actor == actor && !value.Attempt))
            {
                if (stage.Interrupted)
                    continue;
                stage.Interrupted = true;
                stage.Run.Casting = false;
                Add(BattleEventKind.Interrupted, actor.Initial.InstanceId, actionId: stage.Run.Id, detail: reason);
            }
            actor.Gauge = firstGauge;
            actor.CastGauge = 0;
            actor.Ready = false;
        }

        private void ResolveForceFlee(Actor source, Actor target)
        {
            if (_scenario.ScriptBattle)
            {
                ShowFleeResult(source, false);
                return;
            }
            _fleeAlwaysSucceeds = true;
            var run = new ActionRun
            {
                Id = ++_nextAction,
                Kind = BattleAction.Flee,
                Target = target.Initial.InstanceId,
                SystemStarted = true,
                CreatedTick = SimulationTick,
                TimelineOriginTick = SimulationTick
            };
            StartFleeAction(source, run);
        }

        private void StartEnemyFlee(Actor actor)
        {
            actor.EnemyFleeing = true;
            actor.Ready = false;
            _stage.TryAdd(actor.Initial.InstanceId, "enemy flee", new EnemyFleeStage { Session = this, Actor = actor },
                BattleStageScheduler.TimeAt(SimulationTick), out _);
        }

        private void QueueFleeBody(Actor actor, BattlePhysicalBodyKind kind, ActionRun run)
        {
            if (_catalog is IBattlePhysicalTimings)
                QueuePhysicalTask(actor, new PhysicalBodyTask
                {
                    Kind = kind,
                    Run = run,
                    Destination = kind == BattlePhysicalBodyKind.EnemyFlee ? new BattleMovementPoint(800, 0, 800) : new BattleMovementPoint(-800, 0, -800)
                });
            else
                _fallbackFleeBodies[actor.Initial.InstanceId] = kind;
        }

        private void AdvanceFleeBody(Actor actor, PhysicalBodyState body, PhysicalBodyTask task)
        {
            if (task.Kind == BattlePhysicalBodyKind.FleeWait)
            {
                if (!task.Executed)
                {
                    body.BladeSeconds = .01f;
                    body.Pose = BattlePose.Fleeing;
                    TryPlayBodyAnimation(actor, body.Profile.Animation(body.Pose), true, body.Pose, 48);
                    task.Executed = true;
                    return;
                }
                if (!actor.Alive || actor.CastGauge >= FullCastGauge || !task.Run.Casting || AllFleeOpponentsGone)
                {
                    task.Finished = true;
                    if (_fleeStages.TryGetValue(task.Run.Id, out FleeStage stage))
                        stage.BodyFinished = true;
                }
                return;
            }
            if (!task.Executed)
            {
                actor.Movement = new BattleMovement(_scenario.Variant, new BattleMovementPlan(BattleMovementKind.EnemyFlee, actor.Position, task.Destination));
                body.Pose = BattlePose.Moving;
                TryPlayBodyAnimation(actor, body.Profile.Animation(body.Pose), true, body.Pose, 52);
                task.Executed = true;
                Add(BattleEventKind.MovementStarted, actor.Initial.InstanceId, detail: "enemy escape walk");
            }
            AdvanceBodyMovement(actor, actor.Alive);
            actor.Position = actor.Movement.Position;
            if (actor.Movement.FacingDegrees is { } facing)
                actor.FacingDegrees = facing;
            if (task.Finished = actor.Movement.Finished)
                CompleteEnemyFlee(actor);
        }

        private bool AdvanceFallbackFleeBody(Actor actor)
        {
            if (!_fallbackFleeBodies.TryGetValue(actor.Initial.InstanceId, out BattlePhysicalBodyKind kind))
                return false;
            FleeStage stage = _fleeStages.Values.FirstOrDefault(value => value.Actor == actor);
            if (kind == BattlePhysicalBodyKind.FleeWait)
            {
                if (stage == null)
                    _fallbackFleeBodies.Remove(actor.Initial.InstanceId);
                else if (!stage.FallbackBodyStarted)
                    stage.FallbackBodyStarted = true;
                else if (!actor.Alive || actor.CastGauge >= FullCastGauge || !stage.Run.Casting || AllFleeOpponentsGone)
                {
                    stage.BodyFinished = true;
                    _fallbackFleeBodies.Remove(actor.Initial.InstanceId);
                }
                return true;
            }
            actor.Movement ??= new BattleMovement(_scenario.Variant, new BattleMovementPlan(
                kind == BattlePhysicalBodyKind.EnemyFlee ? BattleMovementKind.EnemyFlee : BattleMovementKind.Run,
                actor.Position, kind == BattlePhysicalBodyKind.EnemyFlee ? new BattleMovementPoint(800, 0, 800) : new BattleMovementPoint(-800, 0, -800)));
            AdvanceBodyMovement(actor, actor.Alive);
            actor.Position = actor.Movement.Position;
            actor.FacingDegrees = actor.Movement.FacingDegrees ?? actor.FacingDegrees;
            if (actor.Movement.Finished)
            {
                actor.Movement = null;
                _fallbackFleeBodies.Remove(actor.Initial.InstanceId);
                if (kind == BattlePhysicalBodyKind.EnemyFlee)
                    CompleteEnemyFlee(actor);
            }
            return true;
        }

        private void CompleteEnemyFlee(Actor actor)
        {
            if (!actor.Alive)
                return;
            actor.Escaped = true;
            actor.Gauge = actor.CastGauge = 0;
            actor.Ready = false;
            if (_scenario.Variant == "PAL3A")
            {
                RequestRoleEnd(actor);
                actor.ModelAlive = false;
            }
            Add(BattleEventKind.Escaped, actor.Initial.InstanceId, detail: "original enemy escape walk reached its destination");
            Add(BattleEventKind.MovementEnded, actor.Initial.InstanceId);
        }

        private void CancelFleeStage(ActionRun run)
        {
            if (run == null || !_fleeStages.Remove(run.Id, out FleeStage stage))
                return;
            _fallbackFleeBodies.Remove(stage.Actor.Initial.InstanceId);
            if (stage.Running)
                ReleaseRunningFleeBodies(stage);
        }

        private void ReleaseRunningFleeBodies(FleeStage stage)
        {
            foreach (Actor actor in _actors.Where(actor => OwnsStageBody(actor, stage.Handle)))
            {
                _fallbackFleeBodies.Remove(actor.Initial.InstanceId);
                ReleaseStageBody(actor, stage.Handle);
                actor.Movement?.Stop();
                actor.Movement = null;
                actor.MovementQueue.Clear();
            }
            _fleeAtbPaused = _fleeStages.Values.Any(other => other.Running && _stage.IsActive(other.Handle));
        }

        private void StopFleeStages()
        {
            foreach (FleeStage stage in _fleeStages.Values.ToArray())
                CancelFleeStage(stage.Run);
            _fleeStages.Clear();
            _fallbackFleeBodies.Clear();
            _fleeAtbPaused = false;
            FleeResult = null;
        }
    }
}
