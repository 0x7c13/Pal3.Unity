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
        private sealed class AbilityPresentationSubtask
        {
            public int Status;
            public float Started;
            public bool Executed;
        }

        private sealed class AbilityPresentationRun
        {
            public BattleAbilityStageProfile Profile;
            public Actor Owner;
            public bool OwnsMovement;
            public bool ReturnTurnQueued;
            public readonly AbilityPresentationSubtask[] Steps = Enumerable.Range(0, 7).Select(_ => new AbilityPresentationSubtask()).ToArray();
            public bool Worked, FirstStarted, SecondStarted;
            public long FirstHandle, SecondHandle;
            public long BodyQueuedAt;
            public bool UsesBodyBinding, BodyExecuted, BodyFinished;
            public BattlePhysicalAnimation BodyRequested;
            public int? BodyNativeAction;
        }

        private readonly Dictionary<long, AbilityPresentationRun> _abilityPresentations = new();
        private readonly Dictionary<string, (BattlePhysicalAnimation Animation, bool Loop, BattlePose Pose)> _pendingAbilityMovementBodies = new(StringComparer.Ordinal);

        private string ValidateAbilityPresentation(Actor actor, BattleAbilityDefinition ability, bool elementWheel = false)
        {
            if (IsMagicSwordAbility(ability) || _catalog is not IBattleAbilityEffects catalog)
                return null;
            BattleAbilityStageProfile profile = elementWheel ? ((IBattleElementWheelTimings)_catalog).ElementWheelStage(actor.Initial.InstanceId, ability) :
                catalog.AbilityStage(actor.Initial.InstanceId, ability);
            if (_effects == null && (profile.First.GroupId >= 0 || profile.Second.GroupId >= 0 ||
                profile.WorkBoundary == BattleAbilityWorkBoundary.WorkTimeAndFirstGroupFinished))
                return "This ability requires its original effect completion director.";
            return null;
        }

        // Catalogs without presentation profiles preserve the independent pure
        // mechanics fixtures. Resource-backed catalogs always use this director.
        private bool TryAdvanceAbilityPresentation(Actor actor, ActionRun run, BattleStageContext context, out bool finished)
        {
            finished = false;
            if (IsMagicSwordAbility(run.Ability) || _catalog is not IBattleAbilityEffects catalog)
                return false;
            if (!_abilityPresentations.TryGetValue(run.Id, out AbilityPresentationRun stage))
            {
                stage = new AbilityPresentationRun
                {
                    Profile = run.ElementWheel ? ((IBattleElementWheelTimings)_catalog).ElementWheelStage(actor.Initial.InstanceId, run.Ability) :
                        catalog.AbilityStage(actor.Initial.InstanceId, run.Ability),
                    Owner = actor
                };
                stage.Steps[0].Status = 1;
                stage.Steps[0].Started = context.CreatedTime;
                _abilityPresentations.Add(run.Id, stage);
            }
            run.Elapsed = checked((int)(context.Tick - run.TimelineOriginTick));
            for (int index = 0; index < stage.Steps.Length; index++)
            {
                if (index > 0 && stage.Steps[(int)BattleAbilityStageStep.Grab].Status != 2)
                    break;
                AbilityPresentationSubtask sub = stage.Steps[index];
                if (index > 0 && stage.Steps[index - 1].Status == 0)
                    break;
                if (sub.Status == 2)
                    continue;
                if (run.ElementWheel && index is >= (int)BattleAbilityStageStep.Name and <= (int)BattleAbilityStageStep.PrepareBody)
                {
                    sub.Status = 2;
                    sub.Started = stage.Steps[(int)BattleAbilityStageStep.Grab].Started;
                    continue;
                }
                if (index == (int)BattleAbilityStageStep.Lineup && stage.Profile.Kind != BattleAbilityStageKind.Magic)
                {
                    sub.Status = 2;
                    sub.Started = stage.Steps[(int)BattleAbilityStageStep.Name].Started;
                    continue;
                }
                if (sub.Status == 0)
                {
                    AbilityPresentationSubtask previous = stage.Steps[index - 1];
                    bool parallel = index == (int)BattleAbilityStageStep.Lineup && stage.Profile.Kind == BattleAbilityStageKind.Magic ||
                        index == (int)BattleAbilityStageStep.PrepareBody;
                    if (parallel)
                    {
                        // Skill has no separate lineup task. Its prepare wait is relative to Name.
                        if (stage.Profile.Kind != BattleAbilityStageKind.Magic && index == (int)BattleAbilityStageStep.PrepareBody)
                            previous = stage.Steps[(int)BattleAbilityStageStep.Name];
                        float wait = index == (int)BattleAbilityStageStep.PrepareBody ? stage.Profile.PrepareWaitSeconds : 0;
                        if ((double)context.Time - previous.Started <= wait)
                            continue;
                    }
                    else if (previous.Status != 2)
                        continue;
                    sub.Status = 1;
                    sub.Started = context.Time;
                }
                if (AdvanceAbilityPresentationStep(actor, run, stage, (BattleAbilityStageStep)index, sub, context))
                    sub.Status = 2;
                if (actor.Run != run || Status != BattleSessionStatus.Running)
                {
                    finished = true;
                    return true;
                }
            }
            finished = stage.Steps.All(sub => sub.Status == 2);
            if (finished)
                _abilityPresentations.Remove(run.Id);
            return true;
        }

        private bool AbilityRoleIdle(Actor actor, ActionRun run, AbilityPresentationRun stage) =>
            !IsActorExternallyControlled(actor.Initial.InstanceId) && !PhysicalBodyBusy(actor) && !HasFormationMovement(actor) && SimulationTick >= actor.HurtUntil &&
            (!run.StageStarted || (stage.UsesBodyBinding ? stage.BodyFinished : SimulationTick > stage.BodyQueuedAt && run.BodyElapsed >= stage.Profile.BodyAnimationTicks));

        private bool AdvanceAbilityPresentationStep(Actor actor, ActionRun run, AbilityPresentationRun stage,
            BattleAbilityStageStep step, AbilityPresentationSubtask sub, BattleStageContext context)
        {
            switch (step)
            {
                case BattleAbilityStageStep.Grab:
                    return !HasFormationMovement(actor) && context.Tick >= actor.HurtUntil && TryGrabStageBody(actor, context.Handle);
                case BattleAbilityStageStep.Name:
                    if (!sub.Executed)
                    {
                        sub.Executed = true;
                        ShowStageMessage(stage, actor.Initial.Side, run.Ability.Name, allowTrail: run.Ability.IsItem, handle: context.Handle);
                    }
                    bool nameDone = run.EndRequested || (double)context.Time - sub.Started > stage.Profile.NameSeconds;
                    if (nameDone)
                        HideStageMessage(stage);
                    return nameDone;
                case BattleAbilityStageStep.Lineup:
                    return stage.Profile.Kind != BattleAbilityStageKind.Magic || run.EndRequested ||
                        (double)context.Time - sub.Started > stage.Profile.LineupSeconds;
                case BattleAbilityStageStep.PrepareBody:
                    if (!sub.Executed)
                    {
                        if (!AbilityRoleIdle(actor, run, stage))
                            return stage.Profile.Kind == BattleAbilityStageKind.Magic && run.EndRequested;
                        sub.Executed = true;
                        if (stage.Profile.Kind == BattleAbilityStageKind.Magic)
                            ResetActorFacing(actor);
                        if (stage.Profile.ApproachKind.HasValue)
                        {
                            stage.OwnsMovement = true;
                            QueueAbilityMovement(actor, run, false);
                        }
                    }
                    return run.EndRequested || !HasFormationMovement(actor) && context.Tick >= actor.HurtUntil;
                case BattleAbilityStageStep.Act:
                    if (!sub.Executed)
                    {
                        // Native MagicAct rechecks both common and special costs before body/effects.
                        if (stage.Profile.Kind == BattleAbilityStageKind.Magic &&
                            GetCostEstimate(new BattleCommand(actor.Initial.InstanceId, run.Kind, run.Target, run.Ability.Id)).Any(cost => cost.Required > cost.Available))
                        {
                            stage.Worked = run.AbilityResolved = true;
                            return true;
                        }
                        if (HasFormationMovement(actor) || PhysicalBodyBusy(actor) || IsActorExternallyControlled(actor.Initial.InstanceId) || context.Tick < actor.HurtUntil)
                            return false;
                        sub.Executed = true;
                        if (stage.Profile.Kind == BattleAbilityStageKind.Item)
                            ResetActorFacing(actor);
                        run.StageStarted = true;
                        if (_catalog is IBattleAbilityBodyAnimations bodies)
                        {
                            stage.UsesBodyBinding = true;
                            stage.BodyRequested = run.ElementWheel ? ((IBattleElementWheelTimings)_catalog).ElementWheelBody(actor.Initial.InstanceId, run.Ability) :
                                bodies.AbilityBody(actor.Initial.InstanceId, run.Ability);
                            stage.BodyNativeAction = run.ElementWheel ? null : bodies.AbilityNativeAction(actor.Initial.InstanceId, run.Ability);
                        }
                        run.BodyElapsed = 0;
                        run.BodyRestartPending = true;
                        stage.BodyQueuedAt = context.Tick;
                    }
                    double elapsed = (double)context.Time - sub.Started;
                    if (!stage.FirstStarted && elapsed >= stage.Profile.First.WaitSeconds)
                    {
                        stage.FirstStarted = true;
                        stage.FirstHandle = StartAbilityEffect(actor, run, stage.Profile, stage.Profile.First);
                    }
                    if (!stage.SecondStarted && elapsed >= stage.Profile.Second.WaitSeconds)
                    {
                        stage.SecondStarted = true;
                        stage.SecondHandle = StartAbilityEffect(actor, run, stage.Profile, stage.Profile.Second);
                    }
                    if (!stage.Worked && elapsed >= stage.Profile.WorkSeconds &&
                        (stage.Profile.Kind != BattleAbilityStageKind.Item || stage.FirstStarted) &&
                        (stage.Profile.WorkBoundary != BattleAbilityWorkBoundary.WorkTimeAndFirstGroupFinished ||
                            stage.FirstStarted && (stage.FirstHandle == 0 || !_effects.IsRunning(stage.FirstHandle))))
                    {
                        stage.Worked = true;
                        if (stage.Profile.WorkBoundary != BattleAbilityWorkBoundary.WorkTimeAndBodyIdle && actor.Alive && !actor.Escaped)
                            ResolveActionMarker(actor, run);
                    }
                    if (stage.Worked && AbilityRoleIdle(actor, run, stage))
                    {
                        if (stage.Profile.WorkBoundary == BattleAbilityWorkBoundary.WorkTimeAndBodyIdle && actor.Alive && !actor.Escaped)
                            ResolveActionMarker(actor, run);
                        return true;
                    }
                    return run.EndRequested;
                case BattleAbilityStageStep.ReturnHome:
                    if (run.EndRequested)
                        return true;
                    if (!sub.Executed)
                    {
                        if (!AbilityRoleIdle(actor, run, stage))
                            return false;
                        sub.Executed = true;
                        if (stage.Profile.ReturnKind.HasValue)
                        {
                            stage.OwnsMovement = true;
                            QueueAbilityMovement(actor, run, true);
                        }
                    }
                    if (HasFormationMovement(actor) || context.Tick < actor.HurtUntil)
                        return false;
                    if (stage.Profile.Kind == BattleAbilityStageKind.Skill && !stage.ReturnTurnQueued)
                    {
                        stage.ReturnTurnQueued = true;
                        // ProcSkillEnd queues ProcTurn after the return movement.
                        // Keep the body lease until that final body task has run.
                        if (_catalog is IBattlePhysicalTimings)
                        {
                            QueuePhysicalTask(actor, new PhysicalBodyTask
                            {
                                Kind = BattlePhysicalBodyKind.Turn,
                                Destination = new BattleMovementPoint(BattleFacingRules.Initial(actor.Initial.Side), 0, 0)
                            });
                            return false;
                        }
                        ResetActorFacing(actor);
                    }
                    return !PhysicalBodyBusy(actor);
                case BattleAbilityStageStep.Release:
                    ReleaseStageBody(actor, context.Handle);
                    return true;
                default:
                    throw new InvalidOperationException("Unknown original ability stage step.");
            }
        }

        private bool HasActiveOrdinaryAbilityBody(Actor actor) => actor.Run != null && actor.Run.StageStarted &&
            (_abilityPresentations.TryGetValue(actor.Run.Id, out AbilityPresentationRun stage) && stage.UsesBodyBinding && !stage.BodyFinished ||
                _magicSwordStages.TryGetValue(actor.Run.Id, out MagicSwordStageRun sword) && sword.UsesBodyBinding && !sword.BodyFinished);

        private void RestartOrdinaryAbilityBodyAfterPush(Actor actor)
        {
            if (actor.Run != null && _abilityPresentations.TryGetValue(actor.Run.Id, out AbilityPresentationRun stage) &&
                stage.UsesBodyBinding && !stage.BodyFinished)
            {
                stage.BodyExecuted = false;
                actor.Run.BodyElapsed = 0;
            }
            if (actor.Run != null && _magicSwordStages.TryGetValue(actor.Run.Id, out MagicSwordStageRun sword) &&
                sword.UsesBodyBinding && !sword.BodyFinished)
            {
                sword.BodyExecuted = false;
                actor.Run.BodyElapsed = 0;
            }
        }

        private bool TryAdvanceOrdinaryAbilityBody(Actor actor, ActionRun run)
        {
            if (TryAdvanceMagicSwordBody(actor, run))
                return true;
            if (!_abilityPresentations.TryGetValue(run.Id, out AbilityPresentationRun stage) || !stage.UsesBodyBinding || !run.StageStarted)
                return false;
            if (stage.BodyFinished)
            {
                EnsureProperBodyAnimation(actor);
                return true;
            }
            if (!stage.BodyExecuted)
            {
                stage.BodyExecuted = true;
                run.BodyRestartPending = false;
                if (stage.Profile.NpcItemIdle)
                {
                    SetBodyBladeSeconds(actor, .01f);
                    if (GetBodyBinding(actor).LogicalPose != BattlePose.Idle)
                        TryPlayBodyAnimation(actor, stage.BodyRequested, true, BattlePose.Idle, 32);
                    stage.BodyFinished = true;
                    run.BodyElapsed = run.Timing.BodyAnimationTicks;
                    return true;
                }
                if (stage.Profile.RetainsBody || stage.Profile.BodyAnimationTicks > 0)
                {
                    SetBodyBladeSeconds(actor, 0.25f);
                    if (stage.Profile.Kind != BattleAbilityStageKind.Magic)
                        ResetActorFacing(actor);
                    TryPlayBodyAnimation(actor, stage.BodyRequested, false, run.Kind == BattleAction.Item ? BattlePose.UsingItem : BattlePose.UsingSkill, stage.BodyNativeAction);
                    if (stage.BodyNativeAction == 39)
                        PlaySound("we033");
                }
                else
                    stage.BodyFinished = true;
                return true;
            }
            if (!actor.Alive || GetBodyBinding(actor).LastPoppedMarker == BattlePhysicalMarkerKind.Over)
            {
                stage.BodyFinished = true;
                SetBodyBladeSeconds(actor, 0.01f);
                run.BodyElapsed = run.Timing.BodyAnimationTicks;
            }
            return true;
        }

        private void QueueAbilityMovement(Actor actor, ActionRun run, bool returning)
        {
            var catalog = (IBattleAbilityEffects)_catalog;
            Actor target = run.Target != null && _byId.TryGetValue(run.Target, out Actor chosen) ? chosen : null;
            IReadOnlyList<BattleMovementPlan> plans = catalog.AbilityMovement(Snapshot(actor), target == null ? null : Snapshot(target), run.Ability, Definitions, returning);
            if (plans == null || plans.Count == 0 || plans.Any(plan => plan == null))
                throw new InvalidOperationException("An original ability movement requires a complete captured plan.");
            QueueFormationMoves(actor, plans);
            if (_catalog is IBattleAbilityBodyAnimations bodies)
                _pendingAbilityMovementBodies[actor.Initial.InstanceId] = (bodies.AbilityMovementBody(actor.Initial.InstanceId, run.Ability, plans[0].Kind, returning),
                    plans[0].Kind != BattleMovementKind.Fly, plans[0].Kind == BattleMovementKind.Teleport ? BattlePose.Idle : BattlePose.Moving);
        }

        private bool TryStartAbilityMovementBody(Actor actor)
        {
            if (!_pendingAbilityMovementBodies.Remove(actor.Initial.InstanceId, out var pending))
                return false;
            TryPlayBodyAnimation(actor, pending.Animation, pending.Loop, pending.Pose, pending.Pose == BattlePose.Idle ? 32 : 31);
            return true;
        }

        private long StartAbilityEffect(Actor source, ActionRun run, BattleAbilityStageProfile profile, BattleAbilityEffectBinding binding)
        {
            if (binding.GroupId < 0)
                return 0;
            var geometry = _catalog as IBattleStageGeometry;
            if (geometry == null && binding.PlacementFlags != 0)
                throw new InvalidOperationException("An actor-relative effect requires original formation geometry.");
            bool singleTarget = run.Ability.Range is not (BattleTargetRange.OwnAll or BattleTargetRange.OpponentAll);
            Actor target = run.Target != null && _byId.TryGetValue(run.Target, out Actor chosen) ? chosen : null;
            Actor centerActor = singleTarget ? target : _actors.FirstOrDefault(actor =>
                (actor.Initial.Side != source.Initial.Side) == profile.TargetCenterIsOpposing);
            BattleMovementPoint Center(Actor reference) => reference?.Initial.Side == BattleSide.Enemy &&
                _actors.FirstOrDefault(actor => actor.Initial.Side == BattleSide.Enemy && actor.Slot == BattleSlot.Center) is { } center
                ? center.Home : reference == null ? default : geometry?.FormationHome(reference.Initial.InstanceId, BattleSlot.Center) ?? default;
            BattleMovementPoint sourceCenter = Center(source);
            BattleMovementPoint targetCenter = centerActor == null ? sourceCenter : Center(centerActor);
            BattleMovementPoint origin = binding.Resolve(sourceCenter, targetCenter, source.Home, target?.Home ?? targetCenter, singleTarget && target != null);
            return StartEffect(new BattleEffectRequest(binding.GroupId, source.Initial.InstanceId, run.Targets, origin, unchecked((uint)_scenario.Seed)));
        }

        private void CancelAbilityPresentation(ActionRun run)
        {
            if (run == null || !_abilityPresentations.Remove(run.Id, out AbilityPresentationRun stage))
                return;
            HideStageMessage(stage);
            _pendingAbilityMovementBodies.Remove(stage.Owner.Initial.InstanceId);
            if (stage.OwnsMovement && run.StageHandle is { } handle && OwnsStageBody(stage.Owner, handle))
            {
                stage.Owner.Movement?.Stop();
                stage.Owner.Movement = null;
                stage.Owner.MovementQueue.Clear();
            }
            if (stage.FirstHandle != 0)
                StopEffect(stage.FirstHandle);
            if (stage.SecondHandle != 0)
                StopEffect(stage.SecondHandle);
        }
    }
}
