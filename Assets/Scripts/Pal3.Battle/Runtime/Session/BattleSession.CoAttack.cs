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
        private sealed class CoAttackSubtask
        {
            internal BattleCoAttackStepStatus Status;
            internal float Started;
            internal bool Executed, SkipRequested;
        }

        private sealed class CoAttackStageRun : IBattleStageJob
        {
            internal readonly BattleSession Session;
            internal readonly Actor Source;
            internal readonly ActionRun Run;
            internal readonly BattleComboDefinition Combo;
            internal readonly BattleComboStageProfile Profile;
            internal readonly BattleCoAttackTargetSelection Selection;
            internal readonly string[] Targets;
            internal readonly CoAttackSubtask[] Subtasks;
            internal readonly HashSet<string> HiddenActors = new(StringComparer.Ordinal);
            internal BattleStageHandle Handle;
            internal bool NameVisible, Worked, FirstStarted, SecondStarted, Finished;
            internal long FirstHandle, SecondHandle;

            internal CoAttackStageRun(BattleSession session, Actor source, ActionRun run,
                BattleComboDefinition combo, BattleComboStageProfile profile, BattleCoAttackTargetSelection selection,
                string[] targets, float createdTime)
            {
                Session = session;
                Source = source;
                Run = run;
                Combo = combo;
                Profile = profile;
                Selection = selection;
                Targets = targets;
                Subtasks = Enumerable.Range(0, 13).Select(_ => new CoAttackSubtask()).ToArray();
                Subtasks[0].Status = BattleCoAttackStepStatus.Running;
                Subtasks[0].Started = createdTime;
            }

            internal IEnumerable<CoAttackSubtask> SkippableSubtasks => Subtasks.Where((sub, index) =>
                sub.Status == BattleCoAttackStepStatus.Running && (BattleCoAttackStep)index is
                    BattleCoAttackStep.FadeOutToCombo or BattleCoAttackStep.FadeInToCombo or
                    BattleCoAttackStep.FadeOutToHome or BattleCoAttackStep.FadeInToHome);

            internal bool HasComboPositions =>
                Subtasks[(int)BattleCoAttackStep.SetComboPositions].Status == BattleCoAttackStepStatus.Finished &&
                Subtasks[(int)BattleCoAttackStep.RestorePositions].Status != BattleCoAttackStepStatus.Finished;

            public bool Advance(BattleStageContext context) => Session.AdvanceCoAttack(this, context);

            internal BattleCoAttackSnapshot Capture() => new(Run.Id, Combo, Source.Initial.InstanceId,
                NameVisible, Worked, Finished, Targets, Subtasks.Select((sub, index) =>
                    new BattleCoAttackStepSnapshot((BattleCoAttackStep)index, sub.Status, sub.Started)));
        }

        private readonly Dictionary<long, CoAttackStageRun> _coAttackStages = new();
        private readonly HashSet<long> _coAttackChecked = new();
        private bool _coAttackAtbPaused;
        private CoAttackStageRun _coAttackFadeOwner;

        public bool IsCoAttackAtbPaused => _coAttackAtbPaused;

        public float CoAttackScreenOpacity
        {
            get; private set;
        }

        public uint CoAttackScreenColor
        {
            get; private set;
        }

        public IReadOnlyList<BattleCoAttackSnapshot> CoAttacks => Array.AsReadOnly(_coAttackStages.Values
            .OrderBy(stage => stage.Handle.Slot).Select(stage => stage.Capture()).ToArray());

        public bool CoAttackActorVisible(string actor) => !_coAttackStages.Values.Any(stage => stage.HiddenActors.Contains(actor));

        public bool IsCoAttackPresentationBlockingInput => Status == BattleSessionStatus.Running &&
            (CoAttackScreenOpacity > 0 || _coAttackStages.Values.Any(stage => stage.SkippableSubtasks.Any()));

        public bool SkipCoAttackPresentation()
        {
            if (Status != BattleSessionStatus.Running || IsBattleInputSuspended)
                return false;
            bool requested = false;
            foreach (CoAttackSubtask sub in _coAttackStages.Values.SelectMany(stage => stage.SkippableSubtasks))
            {
                if (sub.SkipRequested)
                    continue;
                sub.SkipRequested = requested = true;
            }
            if (requested)
                _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.CoAttackPresentationSkip });
            return requested;
        }

        private bool IsCoAttackRun(ActionRun run) => run != null && _coAttackStages.ContainsKey(run.Id);

        private int CoAttackId(ActionRun run) => run != null && _coAttackStages.TryGetValue(run.Id, out CoAttackStageRun stage) ? stage.Combo.Id : 0;

        // Call once on an original Attack/Skill system thread's first pass, before
        // ordinary casting/stage setup. A matching combo replaces that command.
        private bool TryReplaceWithCoAttack(Actor source, ActionRun run)
        {
            if (IsCoAttackRun(run))
                return true;
            if (Definitions == null || run == null || run.ElementWheel || !_coAttackChecked.Add(run.Id) || source.Initial.Side != BattleSide.Ally ||
                run.Kind is not (BattleAction.Attack or BattleAction.Skill) ||
                run.Kind == BattleAction.Skill && (run.Ability == null || run.Ability.IsItem || run.Ability.SkillTypeId == 4))
                return false;
            int trigger = run.Kind == BattleAction.Attack ? 0 : run.Ability.Id;
            BattleComboDefinition combo = Definitions.Combos.FirstOrDefault(candidate =>
                FormationRules().CanCoAttack(source.Definition.Identity, trigger, candidate.Requirement, FormationActors()).Allowed);
            if (combo == null)
                return false;
            if (_catalog is not IBattleCoAttackTimings timings || _effects == null)
                throw new NotSupportedException($"Combo {combo.Id} requires its original effect linker and effect completion director.");
            BattleComboStageProfile profile = timings.CoAttack(source.Initial.InstanceId, combo) ??
                throw new InvalidOperationException("The original combo profile is missing.");
            BattleCoAttackTargetSelection selection = SelectCoAttackTargets(run, combo);
            string[] targets = selection.TargetIndices.Select(ActorAtNativeIndex).Where(actor => actor != null)
                .Select(actor => actor.Initial.InstanceId).ToArray();
            float now = BattleStageScheduler.TimeAt(SimulationTick);
            var stage = new CoAttackStageRun(this, source, run, combo, profile, selection, targets, now);
            // The successful command substitution clears source slot 1 before the
            // new CoAttack thread allocates its stage. Helpers retain their bars.
            if (run.StageHandle is { } previous)
                _stage.Cancel(previous);
            run.StageHandle = null;
            run.Casting = false;
            run.AttackPlan = null;
            source.Gauge = source.CastGauge = 0;
            source.Ready = source.Guard = false;
            _coAttackAtbPaused = true;
            if (!_stage.TryAdd(source.Initial.InstanceId, "coattack", stage, now, out BattleStageHandle handle, autoDelete: false))
            {
                _coAttackAtbPaused = false;
                Cancel(source, "original combo stage allocation failed");
                return true;
            }
            stage.Handle = handle;
            run.StageHandle = handle;
            _coAttackStages.Add(run.Id, stage);
            return true;
        }

        private BattleCoAttackTargetSelection SelectCoAttackTargets(ActionRun run, BattleComboDefinition combo)
        {
            var mask = new bool[11];
            foreach (string id in run.Targets ?? Array.Empty<string>())
                if (_byId.TryGetValue(id, out Actor actor))
                    mask[NativeActorIndex(actor)] = true;
            int target = run.Target != null && _byId.TryGetValue(run.Target, out Actor selected) ? NativeActorIndex(selected) : -1;
            BattleCoAttackTargetActorSnapshot[] actors = Enumerable.Range(0, 11).Select(index =>
            {
                Actor actor = ActorAtNativeIndex(index);
                return actor == null ? new BattleCoAttackTargetActorSnapshot(false, false, false, false, 0) :
                    new BattleCoAttackTargetActorSnapshot(true, actor.Alive, actor.Escaped, actor.Definition.Boss, actor.Hp);
            }).ToArray();
            return new BattleCoAttackTargets(Definitions.Variant).Resolve(new BattleCoAttackTargetInput(target, mask, combo.NativeRange, actors));
        }

        private static int NativeActorIndex(Actor actor) => actor.NativeIndex;

        private Actor ActorAtNativeIndex(int index) => _actors.FirstOrDefault(actor => NativeActorIndex(actor) == index);

        // This runs in System.Proceed before ATB/state/stage. Native non-auto
        // CoAttack stages are deleted by their thread after the stage is Finished.
        private void PrepareCoAttackThreads()
        {
            foreach (CoAttackStageRun stage in _coAttackStages.Values.Where(stage => stage.Finished).ToArray())
            {
                _coAttackStages.Remove(stage.Run.Id);
                _coAttackChecked.Remove(stage.Run.Id);
                _stage.Cancel(stage.Handle);
                // Both native system threads release the global ATB pause on completion.
                _coAttackAtbPaused = false;
                if (_coAttackFadeOwner == stage)
                    _coAttackFadeOwner = null;
                if (stage.Source.Run == stage.Run)
                    Finish(stage.Source);
            }
        }

        private bool AdvanceCoAttack(CoAttackStageRun stage, BattleStageContext context)
        {
            for (int index = 0; index < stage.Subtasks.Length; index++)
            {
                CoAttackSubtask sub = stage.Subtasks[index];
                if (index > 0 && stage.Subtasks[index - 1].Status == BattleCoAttackStepStatus.Ready)
                    break;
                if (sub.Status == BattleCoAttackStepStatus.Finished)
                {
                    if (stage.Subtasks.All(value => value.Status == BattleCoAttackStepStatus.Finished))
                        return stage.Finished = true;
                    continue;
                }
                if (sub.Status == BattleCoAttackStepStatus.Ready)
                {
                    CoAttackSubtask previous = stage.Subtasks[index - 1];
                    bool ready = index == (int)BattleCoAttackStep.WaitForSource
                        ? (double)context.Time - previous.Started > 0f
                        : previous.Status == BattleCoAttackStepStatus.Finished;
                    if (!ready)
                        continue;
                    sub.Status = BattleCoAttackStepStatus.Running;
                    sub.Started = context.Time;
                }
                if (AdvanceCoAttackSubtask(stage, (BattleCoAttackStep)index, sub, context))
                    sub.Status = BattleCoAttackStepStatus.Finished;
            }
            return false;
        }

        private bool AdvanceCoAttackSubtask(CoAttackStageRun stage, BattleCoAttackStep step, CoAttackSubtask sub, BattleStageContext context)
        {
            bool requireEnd = stage.Run.EndRequested;
            bool presentationSkip = sub.SkipRequested;
            switch (step)
            {
                case BattleCoAttackStep.GrabSource:
                    return TryGrabStageBody(stage.Source, stage.Handle);
                case BattleCoAttackStep.Preload:
                    if (_effects is IBattleEffectPreloader preloader)
                    {
                        if (stage.Profile.FirstGroupId >= 0)
                            preloader.Preload(stage.Profile.FirstGroupId);
                        if (stage.Profile.SecondGroupId >= 0)
                            preloader.Preload(stage.Profile.SecondGroupId);
                    }
                    return true;
                case BattleCoAttackStep.FadeOutToCombo:
                case BattleCoAttackStep.FadeInToCombo:
                case BattleCoAttackStep.FadeOutToHome:
                case BattleCoAttackStep.FadeInToHome:
                    if (!sub.Executed)
                    {
                        sub.Executed = true;
                        return false;
                    }
                    bool fadeOut = step is BattleCoAttackStep.FadeOutToCombo or BattleCoAttackStep.FadeOutToHome;
                    double fadeElapsed = (double)context.Time - sub.Started;
                    bool done = requireEnd || presentationSkip || fadeElapsed > BattleComboStageProfile.FadeSeconds;
                    _coAttackFadeOwner = stage;
                    CoAttackScreenColor = step is BattleCoAttackStep.FadeOutToCombo or BattleCoAttackStep.FadeInToCombo ? uint.MaxValue : 0;
                    CoAttackScreenOpacity = done ? fadeOut ? 1f : 0f : (float)Math.Clamp(fadeOut ? fadeElapsed : 1 - fadeElapsed, 0, 1);
                    return done;
                case BattleCoAttackStep.SetComboPositions:
                    SetCoAttackPositions(stage);
                    return true;
                case BattleCoAttackStep.ShowName:
                    if (!sub.Executed)
                    {
                        sub.Executed = true;
                        ShowStageMessage(stage, stage.Source.Initial.Side, stage.Combo.Name, handle: context.Handle);
                    }
                    stage.NameVisible = true;
                    bool nameDone = requireEnd || (double)context.Time - sub.Started > BattleComboStageProfile.NameSeconds;
                    if (nameDone)
                    {
                        stage.NameVisible = false;
                        HideStageMessage(stage);
                    }
                    return nameDone;
                case BattleCoAttackStep.WaitForSource:
                case BattleCoAttackStep.WaitForSourceAfterAct:
                    return requireEnd || CoAttackSourceIdle(stage.Source);
                case BattleCoAttackStep.Act:
                    if (!sub.Executed)
                    {
                        sub.Executed = true;
                        ResetActorFacing(stage.Source);
                    }
                    double elapsed = (double)context.Time - sub.Started;
                    if (!stage.FirstStarted && elapsed >= stage.Profile.FirstWaitSeconds)
                    {
                        stage.FirstStarted = true;
                        stage.FirstHandle = StartCoAttackEffect(stage, stage.Profile.FirstGroupId, stage.Profile.FirstOrigin);
                    }
                    if (!stage.SecondStarted && elapsed >= stage.Profile.SecondWaitSeconds)
                    {
                        stage.SecondStarted = true;
                        stage.SecondHandle = StartCoAttackEffect(stage, stage.Profile.SecondGroupId, stage.Profile.SecondOrigin);
                    }
                    if (!stage.Worked && elapsed >= stage.Profile.WorkSeconds)
                    {
                        stage.Worked = true;
                        ResolveCoAttackValues(stage);
                    }
                    // Both games ignore RequireToEnd and source data life here.
                    // PAL3A writes Finish before checking the two running groups.
                    return stage.Worked && CoAttackSourceIdle(stage.Source) && (Definitions.Variant == "PAL3A" ||
                        !CoAttackEffectRunning(stage.FirstHandle) && !CoAttackEffectRunning(stage.SecondHandle));
                case BattleCoAttackStep.RestorePositions:
                    RestoreCoAttackPositions(stage);
                    return true;
                case BattleCoAttackStep.ReleaseSource:
                    ReleaseStageBody(stage.Source, stage.Handle);
                    ReleaseCoAttackSourceBody(stage.Source);
                    return true;
                default:
                    throw new InvalidOperationException("Unknown original combo stage.");
            }
        }

        private bool CoAttackSourceIdle(Actor source) => PhysicalBodyIdle(source);

        private long StartCoAttackEffect(CoAttackStageRun stage, int group, BattleMovementPoint origin) => group < 0 ? 0 :
            StartEffect(new BattleEffectRequest(group, stage.Source.Initial.InstanceId, stage.Targets, origin, unchecked((uint)_scenario.Seed)));

        private bool CoAttackEffectRunning(long handle) => handle != 0 && _effects.IsRunning(handle);

        private void SetCoAttackPositions(CoAttackStageRun stage)
        {
            if (_catalog is not IBattleCoAttackTimings positions)
                throw new NotSupportedException("Original combo positions require the scene formation geometry.");
            foreach (Actor actor in _actors.Where(actor => actor.Initial.Side == BattleSide.Ally && actor.Alive))
            {
                SetActorPosition(actor, positions.CoAttackHome(actor.Initial.InstanceId, actor.Slot));
                ResetActorFacing(actor);
            }
            int targetIndex = stage.Selection.NativeTargetIndex;
            if (stage.Combo.NativeRange != 2 || targetIndex == -1)
                return;
            Actor center = ActorAtNativeIndex(10);
            if (targetIndex != 10)
            {
                if (center != null && !center.Escaped && center.ModelAlive)
                {
                    SetActorPosition(center, center.Home);
                    stage.HiddenActors.Add(center.Initial.InstanceId);
                }
                Actor target = ActorAtNativeIndex(targetIndex);
                if (target != null)
                    SetActorPosition(target, center?.Home ?? stage.Profile.EnemyCenter);
            }
            if (Definitions.Variant == "PAL3A")
                foreach (Actor actor in _actors.Where(actor => actor.Initial.Side == BattleSide.Ally && !actor.Escaped &&
                    actor.Slot == BattleSlot.Center && actor.Initial.ActorId != stage.Combo.Requirement.Members[0]))
                    stage.HiddenActors.Add(actor.Initial.InstanceId);
        }

        private void RestoreCoAttackPositions(CoAttackStageRun stage)
        {
            foreach (Actor actor in _actors.Where(actor => !actor.Escaped))
            {
                SetActorPosition(actor, actor.Home);
                ResetActorFacing(actor);
            }
            // Visibility due to death or escape is handled separately by the preview.
            stage.HiddenActors.Clear();
        }

        private void ResolveCoAttackValues(CoAttackStageRun stage)
        {
            var rules = new BattleCoAttackRules(Definitions.Variant);
            BattleFormationRules formation = FormationRules();
            foreach (string id in stage.Targets)
            {
                Actor target = _byId[id];
                int returned = 0;
                bool hpRecovered = false, mpRecovered = false;
                foreach (int resource in rules.EvaluationIndices(stage.Combo.ResourceValues))
                {
                    int power = rules.Power(stage.Combo.Requirement.Members, memberId =>
                    {
                        Actor member = ActorAtNativeIndex(formation.MemberIndex(memberId)) ??
                            throw new InvalidOperationException("An original combo participant disappeared.");
                        if (Has(target, BattleState.Barrier) || Has(target, BattleState.Invisible))
                            return 0;
                        return _effectRules.PhysicalPower(RuleParticipant(member), RuleParticipant(target), member.Definition,
                            target.Definition, BattleTargetRange.OpponentSingle, _catalog.DamageDivisor(member.Initial.InstanceId),
                            Has(member, BattleState.Madness), _random);
                    }, stage.Combo.PowerMultiplierPercentage, _random);
                    if (BattleCoAttackRules.WritesResource(resource))
                    {
                        returned = ChangeResource(target, resource,
                            BattleCoAttackRules.SignedPower(power, stage.Combo.ResourceValues[resource]),
                            stage.Source, stage.Run.Id, stage.Combo.Name, showNumber: Definitions.Variant == "PAL3A" && resource == 2);
                        hpRecovered |= resource == 0 && returned > 0;
                        mpRecovered |= resource == 2 && returned > 0;
                    }
                }
                PlaySound(BattleEffectAudioRules.Recovery(hpRecovered, mpRecovered));
                if (returned < 0 && target.Run?.Casting == true)
                {
                    Cancel(target, "original combo value interrupted the second gauge slot");
                    target.Gauge = target.CastGauge = 0;
                    target.Ready = false;
                }
            }
            // No trigger cost, proficiency gain, auxiliary gauge reset, or status write.
            CheckResult();
        }

        private void ReleaseCoAttackSourceBody(Actor source)
        {
            source.Movement?.Stop();
            source.Movement = null;
            source.MovementQueue.Clear();
            source.HurtUntil = checked((int)SimulationTick);
            source.Dodging = false;
        }

        // Explicit session/editor cancellation may destroy the stage. Native role
        // death instead sets EndRequested and must leave Act running.
        private void CancelCoAttackStage(ActionRun run)
        {
            if (run == null)
                return;
            _coAttackChecked.Remove(run.Id);
            if (!_coAttackStages.Remove(run.Id, out CoAttackStageRun stage))
                return;
            HideStageMessage(stage);
            if (stage.FirstHandle != 0)
                StopEffect(stage.FirstHandle);
            if (stage.SecondHandle != 0)
                StopEffect(stage.SecondHandle);
            // Editor cancellation releases this combo's presentation only.
            if (_coAttackStages.Count == 0)
                _coAttackAtbPaused = false;
            if (_coAttackFadeOwner == stage)
            {
                _coAttackFadeOwner = null;
                CoAttackScreenOpacity = 0;
            }
            if (stage.HasComboPositions && !_coAttackStages.Values.Any(other => other.HasComboPositions))
                RestoreCoAttackPositions(stage);
            if (OwnsStageBody(stage.Source, stage.Handle))
            {
                ReleaseStageBody(stage.Source, stage.Handle);
                ReleaseCoAttackSourceBody(stage.Source);
            }
        }
    }
}
