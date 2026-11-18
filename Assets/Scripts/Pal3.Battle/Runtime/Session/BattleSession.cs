// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    // Single writer. Fixed reference steps decouple rule rounding and marker order from render frames.
    public sealed partial class BattleSession
    {
        public const double StepSeconds = 0.02;
        public const int FullGauge = 11500;
        public const int FullCastGauge = 11500;

        private sealed class Actor
        {
            public BattleParticipant Initial;
            public int NativeIndex = -1;
            public BattleSlot Slot;
            public BattleMovementPoint Home;
            public BattleMovementPoint Position;
            public float FacingDegrees;
            public BattleMovement Movement;
            public readonly Queue<BattleMovementPlan> MovementQueue = new();
            public int Hp, Sp, Mp, Gauge, CastGauge, CastSpeed, Rounds, ProcessedRounds, HurtUntil, PoseStarted;
            public int Level, Experience, Love;
            public long PresentationTick;
            public int[] Attributes;
            public bool Escaped, EnemyFleeing;
            public bool Alive, ModelAlive, Dodging;
            public bool Reviving;
            public int CorpseItem;
            public string Killer;
            public long DeathTick = -1;
            public BattleActorDefinition Definition;
            public readonly List<BattleSkill> Skills = new();
            public readonly Dictionary<BattleState, BattleStatusInstance> States = new();
            public bool Ready, Guard;
            public ActionRun Run;
        }

        private sealed class ActionRun
        {
            public long Id;
            public int Elapsed;
            public long CreatedTick, TimelineOriginTick;
            public BattleStageHandle? StageHandle;
            public BattleStageHandle? FormStageHandle;
            public bool SystemStarted, StageStarted, BodyRestartPending, EndRequested, ItemConsumed;
            public int BodyElapsed;
            public BattleAction Kind;
            public string Target;
            public AttackTiming Timing;
            public BattleAttackPlan AttackPlan;
            public BattleMovementPoint LastAttackSample;
            public int NextHit, GuardTicks;
            public bool DiscardRemainingHits;
            public bool Casting;
            public bool ElementWheel;
            public int CastingPoseTick;
            public bool AbilityResolved;
            public BattleAbilityDefinition Ability;
            public string[] Targets;
        }

        private readonly List<Actor> _actors;
        private readonly Dictionary<string, Actor> _byId;
        private readonly IBattleCatalog _catalog;
        private readonly IBattleRules _rules;
        private readonly BattleRandom _random;
        private readonly bool[] _automatic = new bool[2];
        private readonly List<BattleEvent> _events = new();
        private readonly List<BattleInput> _inputs = new();
        private double _remainder;
        private long _nextAction;

        public long Tick
        {
            get; private set;
        }

        public long SimulationTick
        {
            get; private set;
        }

        private readonly IBattleEffectDirector _effects;
        private string _absoluteTimeActor;
        private bool _pendingEffectFrame, _pendingEffectFrameFrozen;
        private double _pendingTimeScale, _simulationStepCredit;

        internal IBattleRules ReplayRules => _rules;

        public bool IsModalPresentation => (_effects as IBattleModalEffects)?.BlocksBattleTime == true;

        public bool IsPreparingPresentation => (_effects as IBattlePresentationPreparation)?.IsPreparingPlayback == true;

        private bool IsBattleInputSuspended => _pendingEffectFrame || IsModalPresentation;

        public BattleSessionStatus Status { get; private set; } = BattleSessionStatus.Running;

        public BattleResult Result
        {
            get; private set;
        }

        public IReadOnlyList<BattleEvent> Events => _events.AsReadOnly();

        public IReadOnlyList<BattleInput> Inputs => Array.AsReadOnly(_inputs.Select(input => input.Copy()).ToArray());

        public int RecordedInputCount => _inputs.Count;

        internal bool MatchesRecordedInput(int index, BattleInput expected) =>
            index >= 0 && index < _inputs.Count && _inputs[index].Matches(expected);

        public string Variant => _scenario.Variant;

        public IReadOnlyList<BattleActorSnapshot> Actors => Array.AsReadOnly(_actors.OrderBy(NativeActorIndex).Select(Snapshot).ToArray());

        public string WaitingActor => Status != BattleSessionStatus.Running || _ending || _resolvingOutcome || IsBattleInputSuspended ? null : _formationController ?? _actors.FirstOrDefault(actor => actor.Ready && actor.Alive && !actor.Escaped && (Definitions == null || actor.ProcessedRounds >= actor.Rounds) && actor.Run == null && ActionBodyIdle(actor) && !IsUncontrollable(actor) && !IsAiEnabled(actor))?.Initial.InstanceId;

        public BattleSession(BattleSetup setup, IBattleCatalog catalog, IBattleRules rules = null, BattleDefinitions definitions = null, IBattleEffectDirector effects = null)
        {
            BattleScenario scenario = setup?.CreateInstance() ?? throw new ArgumentNullException(nameof(setup));
            _effects = effects;
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _rules = rules ?? new BattleBasicRules();
            _random = new BattleRandom(scenario.Seed, scenario.Variant);
            InitializeMechanics(scenario, definitions);
            InitializeOriginalEncounter();
            if (definitions == null && (scenario.RoundLimit != -1 || scenario.Participants.Any(actor => actor.Statuses.Count > 0)))
                throw new NotSupportedException("Basic combat does not yet support round limits or initial abnormal states.");
            if (scenario.Participants.Any(actor => actor.Attributes.Any(value => value > 1000000)))
                throw new ArgumentOutOfRangeException(nameof(setup), "Basic combat attributes must not exceed 1,000,000.");
            // Original internal priority: allied character identity, then enemy formation slot.
            _actors = scenario.Participants.OrderBy(actor => actor.Side).ThenBy(actor => _catalog.Priority(actor.InstanceId))
                .ThenBy(actor => actor.InstanceId, StringComparer.Ordinal)
                .Select(actor => new Actor { Initial = actor, Slot = actor.Slot, Alive = actor.Vitals[0] > 0, ModelAlive = actor.Vitals[0] > 0, Hp = actor.Vitals[0], Sp = actor.Vitals[1], Mp = actor.Vitals[2], Level = actor.Level, Experience = actor.Experience, Love = actor.Love, Attributes = actor.Attributes.ToArray() }).ToList();
            _byId = _actors.ToDictionary(actor => actor.Initial.InstanceId, StringComparer.Ordinal);
            foreach (Actor actor in _actors)
            {
                actor.Home = (_catalog as IBattleStageGeometry)?.InitialHome(actor.Initial.InstanceId) ?? default;
                actor.Position = actor.Home;
                actor.FacingDegrees = BattleFacingRules.Starting(actor.Initial.Side, scenario.Encounter);
                InitializeActorMechanics(actor);
            }
            foreach (Actor actor in _actors)
            {
                if (_catalog.GuardEntry(actor.Initial.InstanceId) < 1 || _catalog.Hurt(actor.Initial.InstanceId) < 0 || _catalog.SpImpact(actor.Initial.InstanceId) < 0 || _catalog.DamageDivisor(actor.Initial.InstanceId) < 1)
                    throw new ArgumentException("Invalid actor combat definition.");
                foreach (Actor target in _actors.Where(target => target.Initial.Side != actor.Initial.Side))
                    _ = _catalog.Attack(actor.Initial.InstanceId, target.Initial.InstanceId) ?? throw new ArgumentException("Missing attack timeline.");
                // InitSequen visits valid slots, including dead party members.
                actor.Gauge = _rules.InitialGauge(RuleParticipant(actor), scenario.Encounter, _random);
                if (actor.Alive)
                    MakeReady(actor, false);
            }
            if (definitions != null)
                FixNativeState();
            AssignNativeActorIndices();
            InitializePal3AForms();
            InitializeAi();
            InitializeStartupSpecials();
            Add(BattleEventKind.Started, detail: scenario.Variant + (Definitions == null ? "; explicit attributes; neutral elements" : "; captured definitions, equipment and ordinary statuses") + "; encounter is already resolved.");
            CheckResult();
        }

        public bool IsAutomatic(BattleSide side) => _automatic[(int)side];

        public void SetAutomatic(BattleSide side, bool enabled)
        {
            if (!Enum.IsDefined(typeof(BattleSide), side))
                throw new ArgumentOutOfRangeException(nameof(side));
            if (Status != BattleSessionStatus.Running || _automatic[(int)side] == enabled && AutomaticMatches(side, enabled))
                return;
            _automatic[(int)side] = enabled;
            SetOriginalAutomatic(side, enabled);
            _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.ControlChange, Side = side, Automatic = enabled });
            Add(BattleEventKind.ControlChanged, detail: side + ": " + (enabled ? Definitions == null ? "basic test AI" : "original AI" : "manual"));
        }

        public string CanSubmit(string actorId, BattleAction action, string targetId = null)
            => CanSubmit(new BattleCommand(actorId, action, targetId));

        public bool Submit(string actorId, BattleAction action, string targetId = null)
            => Submit(new BattleCommand(actorId, action, targetId));

        public bool Submit(BattleCommand command) => SubmitCommand(command, false);

        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > 60)
                throw new ArgumentOutOfRangeException(nameof(seconds), "Advance between 0 and 60 seconds per call.");
            _remainder += seconds;
            while (_remainder + 1e-10 >= StepSeconds && Status == BattleSessionStatus.Running)
            {
                if (IsPreparingPresentation)
                {
                    // Preparing files is outside the battle and the movie's native clock.
                    _remainder = 0;
                    break;
                }
                _remainder -= StepSeconds;
                Step();
            }
        }

        public void Step()
        {
            if (Status != BattleSessionStatus.Running || IsPreparingPresentation)
                return;
            try
            {
                Tick++;
                bool frozen = _pendingEffectFrame ? _pendingEffectFrameFrozen : _absoluteTimeActor != null;
                double scale = _pendingEffectFrame ? _pendingTimeScale : (_effects as IBattleTimeScale)?.TimeScale ?? 1d;
                if (!double.IsFinite(scale) || scale is < 0d or > 1d)
                    throw new InvalidOperationException("Battle clock scale must be between zero and one.");
                double scaledSeconds = frozen ? 0d : StepSeconds * scale;
                _effects?.Advance(Tick, _pendingEffectFrame ? 0f : (float)scaledSeconds, (float)StepSeconds);
                // The original movie player returns synchronously and excludes its
                // wall time. Keep this frame's domain continuation until that return.
                if (IsModalPresentation)
                {
                    if (!_pendingEffectFrame)
                        _modalTransitions.Add(Tick);
                    _pendingEffectFrame = true;
                    _pendingEffectFrameFrozen = frozen;
                    _pendingTimeScale = scale;
                    return;
                }
                if (_pendingEffectFrame)
                    _modalTransitions.Add(Tick);
                _pendingEffectFrame = false;
                PruneEffects();
                AdvancePal3ASpiritOrbs();
                if (!frozen)
                {
                    _simulationStepCredit += scale;
                    if (_simulationStepCredit < 1d)
                    {
                        (_effects as IBattleStagePresentation)?.AdvanceStage(scaledSeconds, StepSeconds);
                        return;
                    }
                    _simulationStepCredit -= 1d;
                    SimulationTick++;
                }
                foreach (Actor actor in _actors)
                    if (!frozen || _absoluteTimeActor == actor.Initial.InstanceId)
                        actor.PresentationTick++;
                UpdateEncounterStageFeedback();
                if (Definitions != null)
                {
                    PrepareStageActions();
                    if (Status != BattleSessionStatus.Running)
                        return;
                    if (!frozen && !_resolvingOutcome && WaitingActor == null)
                        AdvanceGauges();
                    FixNativeState();
                    CheckResult();
                    if (Status != BattleSessionStatus.Running)
                        return;
                    ProcessRounds();
                    if (Status != BattleSessionStatus.Running)
                        return;
                    if (!frozen && !_resolvingOutcome)
                        RunTestAi();
                    _stage.Advance(SimulationTick);
                    if (Status != BattleSessionStatus.Running)
                        return;
                    for (int nativeIndex = 0; nativeIndex < 11; nativeIndex++)
                    {
                        Actor actor = ActorAtNativeIndex(nativeIndex);
                        if (actor == null)
                            continue;
                        if (!frozen || _absoluteTimeActor == actor.Initial.InstanceId)
                        {
                            BeginBodyAnimationPass(actor);
                            if (!IsActorExternallyControlled(actor.Initial.InstanceId))
                            {
                                if (_catalog is IBattlePhysicalTimings)
                                    UpdateModelLife(actor);
                                AdvanceFormationMovement(actor);
                                AdvanceAction(actor);
                            }
                            EndBodyAnimationPass(actor);
                        }
                        if (_catalog is not IBattlePhysicalTimings)
                            UpdateModelLife(actor);
                        if (Status != BattleSessionStatus.Running)
                            return;
                    }
                    (_effects as IBattleStagePresentation)?.AdvanceStage(scaledSeconds, StepSeconds);
                    ReleaseUnavailableFormationController();
                    CheckResult();
                    return;
                }
                AdvanceFormationMovements();
                foreach (Actor actor in _actors)
                {
                    AdvanceAction(actor);
                    if (Status != BattleSessionStatus.Running)
                        return;
                }
                ReleaseUnavailableFormationController();
                ProcessRounds();
                if (Status != BattleSessionStatus.Running)
                    return;
                RunTestAi();
                if (WaitingActor != null)
                    return; // Input pauses gauges, not already-running animations/hits.
                AdvanceGauges();
                ProcessRounds();
                if (Status != BattleSessionStatus.Running)
                    return;
                RunTestAi();
            }
            catch (Exception exception)
            {
                Fault(exception.GetType().Name + ": " + exception.Message);
            }
        }

        private void MakeReady(Actor actor, bool countRound = true)
        {
            if (actor.Gauge < FullGauge || actor.Ready || actor.Hp <= 0 || actor.Escaped)
                return;
            actor.Ready = true;
            if (countRound)
                actor.Rounds++;
            actor.Guard = false;
            Add(BattleEventKind.Ready, actor.Initial.InstanceId);
        }

        private void RunTestAi()
        {
            if (Definitions != null)
            {
                RunAi();
                return;
            }
            foreach (Actor actor in _actors.Where(actor => actor.Ready && actor.Alive && !actor.Escaped && actor.Run == null && ActionBodyIdle(actor) && !Has(actor, BattleState.Paralysis) && !Has(actor, BattleState.Sleep) && (_automatic[(int)actor.Initial.Side] || IsUncontrollable(actor))))
            {
                Actor target = ChooseTestTarget(actor);
                if (target == null)
                {
                    CheckResult();
                    continue;
                }
                // Do not send both sides chasing one another forever. Native AI also rejects away targets.
                SubmitCommand(new BattleCommand(actor.Initial.InstanceId, BattleAction.Attack, target.Initial.InstanceId), true);
            }
        }

        private void AdvanceAction(Actor actor)
        {
            ActionRun run = actor.Run;
            if (HasActivePal3AFormBody(actor))
                return;
            if (run?.Kind == BattleAction.Transform)
            {
                if (run.StageHandle is { } formHandle && (_stage.IsFinished(formHandle) || !_stage.IsActive(formHandle)))
                    Finish(actor);
                return;
            }
            if (IsActorExternallyControlled(actor.Initial.InstanceId) || AdvancePhysicalBody(actor) || IsPhysicalRun(run) || IsCoAttackRun(run))
                return;
            if (AdvanceFallbackFleeBody(actor) || run?.Kind == BattleAction.Flee)
                return;
            if (run == null || !actor.ModelAlive || actor.Escaped || SimulationTick < actor.HurtUntil || SimulationTick <= run.CreatedTick || !run.Casting && SimulationTick <= run.TimelineOriginTick)
                return;
            if (run.Casting)
            {
                run.CastingPoseTick++;
                return;
            }
            if (Definitions != null && run.Ability != null)
            {
                AdvanceAbilityBody(actor, run);
                return;
            }
            if (run.EndRequested && run.Ability == null)
            {
                Finish(actor);
                return;
            }
            if (run.DiscardRemainingHits)
            {
                Finish(actor);
                return;
            }
            int elapsed = ++run.Elapsed;
            UpdateAttackPosition(actor, run);
            if (run.Kind == BattleAction.Defend)
            {
                if (elapsed >= run.GuardTicks)
                {
                    actor.Guard = !actor.Ready;
                    Finish(actor);
                }
                return;
            }
            while (run.NextHit < run.Timing.HitTicks.Count && elapsed >= run.Timing.PreparationTicks + run.Timing.TravelTicks + run.Timing.HitTicks[run.NextHit])
            {
                run.NextHit++;
                if (Definitions != null)
                    ResolveActionMarker(actor, run);
                else
                    ResolveHit(actor, _byId[run.Target], run);
                if (Status != BattleSessionStatus.Running || actor.Run != run || actor.Hp <= 0)
                    return;
            }
            if (elapsed >= run.Timing.EndTick)
                Finish(actor);
        }

        // Lightweight definition-free scenarios run the supplied IBattleRules per
        // hit and clamp HP directly. Authored game-data sessions use native stage
        // markers, one power roll per attack, and the full death/corpse lifecycle.
        private void ResolveHit(Actor source, Actor target, ActionRun run)
        {
            if (target.Hp <= 0)
            {
                Add(BattleEventKind.Miss, source.Initial.InstanceId, target.Initial.InstanceId, run.Id, detail: "target died; no retarget");
                return;
            }
            bool atHome = IsAtHome(target);
            PhysicalHit hit = _rules.Hit(source.Initial, target.Initial, target.Guard, _random, atHome, _catalog.DamageDivisor(source.Initial.InstanceId));
            int before = target.Hp;
            target.Hp = Math.Clamp(target.Hp + hit.Delta, 0, target.Attributes[0]);
            int actual = target.Hp - before;
            if (actual < 0)
            {
                target.HurtUntil = checked((int)SimulationTick + _catalog.Hurt(target.Initial.InstanceId));
                target.PoseStarted = (int)SimulationTick;
                if (target.Run != null)
                {
                    bool afterHit = target.Run.Kind == BattleAction.Attack && target.Run.NextHit > 0;
                    target.Run.DiscardRemainingHits = afterHit;
                    if (!afterHit)
                        target.Run.Elapsed = 0;
                    Add(BattleEventKind.Interrupted, target.Initial.InstanceId, target.Run.Target, target.Run.Id,
                        detail: afterHit ? "hurt after work: discard remaining hits, recover then finish" : "hurt before work: restart animation after recovery");
                }
                if (target.Initial.Side == BattleSide.Ally)
                    target.Sp = (int)Math.Min(target.Attributes[1], (long)target.Sp + _catalog.SpImpact(source.Initial.InstanceId));
            }
            _events.Add(new BattleEvent
            {
                Tick = Tick,
                Kind = actual < 0 ? BattleEventKind.Hit : BattleEventKind.Miss,
                Source = source.Initial.InstanceId,
                Target = target.Initial.InstanceId,
                ActionId = run.Id,
                HpBefore = before,
                HpAfter = target.Hp,
                RequestedDelta = hit.Delta,
                ActualDelta = actual,
                Critical = hit.Critical,
                Dodged = hit.Dodged,
                RandomState = _random.State,
                Detail = !atHome ? "target away from home" : hit.Dodged ? "dodged" : actual == 0 ? "nonnegative native damage" : target.Guard ? "guard" : "physical"
            });
            if (target.Hp == 0)
            {
                // This fallback has no native body/state pass to synchronize death.
                target.Alive = target.ModelAlive = false;
                Cancel(target, "death");
                target.Gauge = 0;
                target.Ready = target.Guard = false;
                target.PoseStarted = (int)SimulationTick;
                Add(BattleEventKind.Death, source.Initial.InstanceId, target.Initial.InstanceId, run.Id);
            }
            CheckResult();
        }

        private void Finish(Actor actor)
        {
            CancelStageAction(actor.Run);
            if (actor.Run.Kind == BattleAction.Attack && actor.Run.AttackPlan != null)
                actor.Position = actor.Home;
            Add(BattleEventKind.ActionEnded, actor.Initial.InstanceId, actor.Run.Target, actor.Run.Id);
            actor.Run = null;
            actor.CastGauge = 0;
        }

        private void Cancel(Actor actor, string reason)
        {
            CancelStageAction(actor.Run);
            if (actor.Run != null)
                Add(BattleEventKind.Cancelled, actor.Initial.InstanceId, actor.Run.Target, actor.Run.Id, detail: reason);
            actor.Run = null;
            actor.CastGauge = 0;
        }

        public bool CancelAction(string actorId)
        {
            if (Status != BattleSessionStatus.Running || actorId == null || !_byId.TryGetValue(actorId, out Actor actor) || actor.Run == null)
                return false;
            ActionRun run = actor.Run;
            Actor[] ownedBodies = run.StageHandle is { } handle
                ? _actors.Where(candidate => OwnsStageBody(candidate, handle)).ToArray()
                : run.AttackPlan is { } plan && !SamePoint(run.LastAttackSample, plan.Origin) ? new[] { actor } : Array.Empty<Actor>();
            _inputs.Add(new BattleInput { Tick = Tick, Actor = actorId, Kind = BattleInputKind.CancelAction });
            CancelOwnedThrownWeapon(actor, run);
            BattleStageHandle? form = run.Kind == BattleAction.Transform ? run.StageHandle : run.FormStageHandle;
            bool cancelledTransition;
            try
            {
                cancelledTransition = form is { } transition && _stage.Cancel(transition);
            }
            finally
            {
                Cancel(actor, "editor cancellation; gauge cost retained");
            }
            if (cancelledTransition)
                EnsureProperBodyAnimation(actor);
            // Explicit cancellation removes the action's normal return movement.
            // A still-owned body continues under its own stage or effect.
            foreach (Actor body in ownedBodies)
            {
                if (!body.Alive || body.Escaped || SamePoint(body.Position, body.Home) || body.Run != null ||
                    _bodyStageOwners.ContainsKey(body.Initial.InstanceId) || PhysicalBodyBusy(body) ||
                    HasFormationMovement(body) || IsActorExternallyControlled(body.Initial.InstanceId))
                    continue;
                SetActorPosition(body, body.Home);
                ResetActorFacing(body);
            }
            return true;
        }

        public void Abort(string reason = "editor stop")
        {
            if (Status == BattleSessionStatus.Running)
            {
                _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.Abort, Reason = reason ?? "editor stop" });
                End(BattleSessionStatus.Aborted, null, reason ?? "editor stop");
            }
        }

        public void Fault(string reason)
        {
            if (Status == BattleSessionStatus.Running)
            {
                reason ??= "external fault";
                _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.Fault, Reason = reason });
                End(BattleSessionStatus.Faulted, null, reason);
            }
        }

        private bool IsAtHome(Actor actor) => _catalog is IBattleStageGeometry
            ? IsWithinHome(actor.Position, actor.Home)
            : actor.Run?.Kind != BattleAction.Attack || actor.Run.Timing.TravelTicks == 0;

        private bool _resolvingOutcome;
        private bool _ending;
        private (BattleSide? Winner, string Reason) _pendingOutcome;

        public bool IsResolvingOutcome => _resolvingOutcome;

        private IReadOnlyCollection<int> UnfinishedNativeTasks()
        {
            var tasks = new HashSet<int>(ActivePal3AFormTasks);
            if (_coAttackStages.Values.Any(stage => !stage.Finished && _stage.IsActive(stage.Handle)))
                tasks.Add(Definitions.Variant == "PAL3A" ? 13 : 12);
            if (Definitions.Variant == "PAL3A")
                foreach (Actor actor in _actors)
                {
                    ActionRun run = actor.Run;
                    if (run?.SystemStarted != true || IsCoAttackRun(run))
                        continue;
                    bool finished = run.StageHandle is { } handle && _stage.IsFinished(handle);
                    if (finished)
                        continue;
                    if (run.Kind == BattleAction.Attack)
                        tasks.Add(11);
                    else if (run.Kind == BattleAction.Skill && run.Ability != null && !run.Casting &&
                        (_catalog is IBattleAbilityEffects catalog ? catalog.AbilityStage(actor.Initial.InstanceId, run.Ability).Kind == BattleAbilityStageKind.Skill : !run.Ability.IsMagic))
                        tasks.Add(17);
                }
            return tasks;
        }

        private void CheckResult()
        {
            BattleSide? winner;
            string reason;
            if (Definitions != null)
            {
                BattleEncounterOutcome outcome = EncounterOutcome(UnfinishedNativeTasks());
                if (outcome == BattleEncounterOutcome.None && !_pal3AWinReturnQueued)
                {
                    _resolvingOutcome = false;
                    return;
                }
                winner = outcome == BattleEncounterOutcome.Win || _pal3AWinReturnQueued ? BattleSide.Ally : BattleSide.Enemy;
                reason = RoundLimitReached && outcome == BattleEncounterOutcome.Win ? "original boss round limit reached" : "opposing team defeated";
            }
            else
            {
                bool ally = _actors.Any(actor => actor.Hp > 0 && !actor.Escaped && actor.Initial.Side == BattleSide.Ally);
                bool enemy = _actors.Any(actor => actor.Hp > 0 && !actor.Escaped && actor.Initial.Side == BattleSide.Enemy);
                if (ally && enemy)
                {
                    _resolvingOutcome = false;
                    return;
                }
                winner = ally ? BattleSide.Ally : enemy ? BattleSide.Enemy : null;
                reason = ally || enemy ? "opposing team defeated" : "both teams defeated";
            }
            if (winner == BattleSide.Ally)
                QueuePal3AVictoryReturns();
            if (Definitions == null || SimulationTick == 0 && !_pal3AWinReturnQueued)
            {
                End(BattleSessionStatus.Completed, winner, reason);
                return;
            }
            if (!_resolvingOutcome)
            {
                _resolvingOutcome = true;
                _pendingOutcome = (winner, reason);
                foreach (Actor actor in _actors.Where(actor => actor.Run?.Casting == true))
                    Cancel(actor, "encounter outcome is resolving");
            }
            if (_stage.Count == 0 && _actors.All(actor => actor.Run == null && !PhysicalBodyBusy(actor) && !HasFormationMovement(actor)))
                End(BattleSessionStatus.Completed, _pendingOutcome.Winner, _pendingOutcome.Reason);
        }

        private void End(BattleSessionStatus status, BattleSide? winner, string reason)
        {
            _ending = true;
            try
            {
                StopFormationMovements();
                StopStage();
                RestorePal3AFormsAfterStageStop();
                StopEffects();
                foreach (Actor actor in _actors)
                {
                    Cancel(actor, "session ended");
                    ClearPhysicalBody(actor);
                }
            }
            finally
            {
                _ending = false;
            }
            try
            {
                Result = BuildResult(status, winner, reason);
                Status = status;
            }
            catch (Exception exception)
            {
                string fault = "settlement: " + exception.GetType().Name + ": " + exception.Message;
                _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.Fault, Reason = fault });
                Status = BattleSessionStatus.Faulted;
                Result = BuildResult(BattleSessionStatus.Faulted, null, fault);
            }
            Add(BattleEventKind.Ended, detail: Result.Status + ": " + Result.Winner + " " + Result.Reason);
        }

        private BattleActorSnapshot Snapshot(Actor actor)
        {
            ActionRun run = actor.Run;
            int elapsed = run?.Elapsed ?? 0;
            BattlePose pose = !actor.ModelAlive ? BattlePose.Dead : actor.Guard ? BattlePose.Guard : BattlePose.Idle;
            int local = !actor.ModelAlive ? (int)SimulationTick - actor.PoseStarted : (int)actor.PresentationTick;
            if (run != null)
            {
                if (IsCoAttackRun(run) || run.Kind == BattleAction.Transform)
                    pose = actor.ModelAlive ? BattlePose.Idle : BattlePose.Dead;
                else if (run.Casting)
                    pose = run.Kind == BattleAction.Flee ? BattlePose.Fleeing : BattlePose.Casting;
                else if (elapsed < (run.Timing?.PreparationTicks ?? 0))
                    pose = BattlePose.Idle;
                else if (run.Ability != null && !run.StageStarted)
                    pose = BattlePose.Idle;
                else if (run.Kind == BattleAction.Skill)
                    pose = run.BodyElapsed >= run.Timing.BodyAnimationTicks ? BattlePose.Idle : BattlePose.UsingSkill;
                else if (run.Kind == BattleAction.Item)
                    pose = run.BodyElapsed >= run.Timing.BodyAnimationTicks ? BattlePose.Idle : BattlePose.UsingItem;
                else if (run.Kind == BattleAction.Flee)
                    pose = BattlePose.Fleeing;
                else if (run.Kind == BattleAction.Defend)
                    pose = BattlePose.GuardEntry;
                else if (elapsed < run.Timing.TravelTicks)
                    pose = BattlePose.Moving;
                else if (elapsed < run.Timing.TravelTicks + run.Timing.AnimationTicks)
                    pose = BattlePose.Attacking;
                else
                    pose = BattlePose.Returning;
                local = pose == BattlePose.Attacking ? elapsed - run.Timing.PreparationTicks - run.Timing.TravelTicks :
                    pose == BattlePose.Returning ? elapsed - run.Timing.PreparationTicks - run.Timing.TravelTicks - run.Timing.AnimationTicks :
                    pose is BattlePose.UsingSkill or BattlePose.UsingItem ? run.BodyElapsed : elapsed;
            }
            if (run?.Ability != null && pose == BattlePose.Idle)
                local = (int)actor.PresentationTick;
            CapturePhysicalPose(actor, ref pose, ref local);
            if (actor.ModelAlive && HasFormationMovement(actor) && !PhysicalBodyBusy(actor))
            {
                pose = BattlePose.Repositioning;
                local = Math.Max(0, actor.Movement.ElapsedTicks - 1);
            }
            if (_catalog is not IBattlePhysicalTimings && actor.ModelAlive && SimulationTick < actor.HurtUntil)
            {
                pose = actor.Dodging ? BattlePose.Dodging : BattlePose.Hurt;
                local = (int)SimulationTick - actor.PoseStarted;
            }
            else if (run?.Casting == true)
                local = run.CastingPoseTick;
            return new BattleActorSnapshot
            {
                Id = actor.Initial.InstanceId,
                ActorId = actor.Initial.ActorId,
                NativeIndex = NativeActorIndex(actor),
                NativeAnimationClip = GetBodyBinding(actor).CurrentAnimation?.Clip,
                NativePresentationTick = actor.PresentationTick,
                NativePlaybackRate = _bodyPassRates.GetValueOrDefault(actor.Initial.InstanceId, 1f),
                WeaponTrailSeconds = CurrentBladeSeconds(actor.Initial.InstanceId),
                NativeAnimationHalfDepth = GetBodyBinding(actor).CurrentAnimation?.HalfDepth,
                NativeAnimationSeconds = GetBodyBinding(actor).CurrentAnimationTimeSeconds,
                MagicSword = CaptureMagicSword(actor),
                Side = actor.Initial.Side,
                Slot = actor.Slot,
                Home = actor.Home,
                Position = actor.Position,
                FacingDegrees = actor.FacingDegrees,
                Movement = actor.Movement?.Capture(),
                AttackPlan = run?.AttackPlan,
                Hp = actor.Hp,
                MaxHp = actor.Attributes[0],
                Sp = actor.Sp,
                Mp = actor.Mp,
                MaxSp = actor.Attributes[1],
                MaxMp = actor.Attributes[2],
                Level = actor.Level,
                Experience = actor.Experience,
                Love = actor.Love,
                CorpseItem = actor.CorpseItem,
                Attributes = Array.AsReadOnly(EffectiveAttributes(actor)),
                States = StatusSnapshots(actor),
                Skills = Array.AsReadOnly(actor.Skills.Select(skill => new BattleSkillSnapshot(skill)).ToArray()),
                CurrentAction = run?.Kind,
                AbilityId = IsCoAttackRun(run) ? CoAttackId(run) : run?.Ability?.Id ?? 0,
                Escaped = actor.Escaped,
                Fleeing = IsFleeing(actor),
                AliveFlag = actor.Alive,
                ModelAliveFlag = actor.ModelAlive,
                Gauge = actor.Gauge,
                CastGauge = actor.CastGauge,
                Rounds = actor.Rounds,
                Ready = actor.Ready,
                Guarding = actor.Guard,
                Pose = pose,
                PoseTick = local,
                Target = run?.Target,
                ActionId = run?.Id ?? 0,
                ActionTick = elapsed
            };
        }

        private void Add(BattleEventKind kind, string source = null, string target = null, long actionId = 0, string detail = "") =>
            _events.Add(new BattleEvent { Tick = Tick, Kind = kind, Source = source, Target = target, ActionId = actionId, Detail = detail, RandomState = _random.State });
    }
}
