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
        private sealed class Pal3AFormState
        {
            internal BattlePal3AFormKind Kind;
            internal int Generation, Rounds, RoundLimit;
            internal int SavedHp, SavedMaxHp, SavedDefense;
            internal bool Pending, LethalReturnPending, PengxuStatsPrepared;
        }

        private readonly Dictionary<string, Pal3AFormState> _pal3AForms = new(StringComparer.Ordinal);
        private BattleSpiritWheelSnapshot _spiritWheel = new(new int[5]);
        private BattleRandom _spiritVisualRandom = new(1);
        private bool _preserveLiaoRiLosses;
        private int _pal3AHpLoss, _pal3AMpLoss;

        public BattlePal3AStateSnapshot Pal3AState => new(_spiritWheel, _preserveLiaoRiLosses, _pal3AHpLoss, _pal3AMpLoss, _spiritVisualRandom.State);

        public bool IsPal3ATransformed(string actorId) => actorId != null && _pal3AForms.TryGetValue(actorId, out Pal3AFormState state) &&
            state.Kind != BattlePal3AFormKind.Human;

        public BattlePal3AFormSnapshot Pal3AForm(string actorId) => actorId != null && _pal3AForms.TryGetValue(actorId, out Pal3AFormState state)
            ? new BattlePal3AFormSnapshot(state.Kind, state.Generation, state.Rounds, state.RoundLimit, state.Pending, state.LethalReturnPending)
            : new BattlePal3AFormSnapshot(BattlePal3AFormKind.Human, 0, 0, 0, false, false);

        private void InitializePal3AState(BattleScenario scenario)
        {
            if (scenario.Variant != "PAL3A")
                return;
            BattlePal3AState captured = scenario.Pal3A ?? throw new ArgumentException("PAL3A state is missing.");
            captured.Validate();
            _spiritWheel = captured.SpiritWheel.Capture();
            _spiritVisualRandom = new BattleRandom(captured.SpiritVisualSeed);
            _preserveLiaoRiLosses = captured.PreserveLiaoRiLosses;
            if (_preserveLiaoRiLosses)
            {
                _pal3AHpLoss = captured.HpLoss;
                _pal3AMpLoss = captured.MpLoss;
            }
        }

        private void InitializePal3AForms()
        {
            if (_scenario.Variant != "PAL3A" || Definitions == null)
                return;
            foreach (Actor actor in _actors)
            {
                if (_preserveLiaoRiLosses && actor.Initial.ActorId == BattlePal3ARules.LiaoRiCarryActor &&
                    ((long)actor.Attributes[0] + _pal3AHpLoss > int.MaxValue || (long)actor.Attributes[2] + _pal3AMpLoss > int.MaxValue))
                    throw new ArgumentException("Captured PAL3A losses exceed Liao Ri's signed int32 attribute capacity.");
                ApplyPal3ALiaoRiCarry(actor);
            }
            foreach (Actor actor in _actors.Where(actor => actor.Initial.Side == BattleSide.Ally && NativeActorIndex(actor) is 0 or 2))
                _pal3AForms.Add(actor.Initial.InstanceId, new Pal3AFormState());
            Actor nangong = ActorAtNativeIndex(0);
            if (_scenario.Pal3A.NangongStartsTransformed)
            {
                if (nangong == null)
                    throw new ArgumentException("Opening fire beast form needs Nangong Huang in native slot zero.");
                _spiritWheel = new BattleSpiritWheelSnapshot(Enumerable.Repeat(9, 5));
                SwapPal3AForm(nangong, true);
            }
            Actor pengxu = ActorAtNativeIndex(2);
            if (_scenario.Pal3A.PengxuStartsTransformed)
            {
                if (pengxu == null)
                    throw new ArgumentException("Opening fairy beast form needs Wang Pengxu in native slot two.");
                PreparePal3APengxuStats(pengxu);
                SwapPal3AForm(pengxu, true);
            }
        }

        private string ValidatePal3AFormCommand(Actor actor, BattleCommand command, BattleAbilityDefinition ability, bool fromController)
        {
            if (_scenario.Variant != "PAL3A")
                return command.Action == BattleAction.Transform ? "Transformation is exclusive to PAL3A." : null;
            Pal3AFormState state = _pal3AForms.GetValueOrDefault(actor.Initial.InstanceId);
            if (state?.Pending == true)
                return "The original form transition is still running.";
            if (command.Action == BattleAction.Transform)
            {
                if (NativeActorIndex(actor) != 0 || actor.Initial.ActorId != 101 || state == null || state.Kind != BattlePal3AFormKind.Human)
                    return "Only Nangong Huang's human form can request the fire beast transformation.";
                if (_spiritWheel.Tier == 0)
                    return "All five spirit wheel charges must reach three.";
                return _catalog is IBattlePal3AFormCatalog ? null : "The original form animation catalog is missing.";
            }
            if (ability?.Id == BattlePal3ARules.PengxuTransformSkill)
            {
                if (NativeActorIndex(actor) != 2 || actor.Initial.ActorId != 103 || state == null || state.Kind != BattlePal3AFormKind.Human)
                    return "朱仙变 requires Wang Pengxu's human form.";
                if (actor.Sp < actor.Attributes[1])
                    return "朱仙变 requires full SP before equipment savings.";
                return _catalog is IBattlePal3AFormCatalog ? null : "The original form animation catalog is missing.";
            }
            if (ability?.Id == BattlePal3ARules.SpiritCaptureSkill && NativeActorIndex(actor) != 0)
                return "摄灵法阵 requires Nangong Huang in native slot zero.";
            return fromController ? null : FormActionDenial(actor.Initial.InstanceId, command.Action, ability?.IsMagic == true);
        }

        public string FormActionDenial(string actorId, BattleAction action, bool magic = false)
        {
            if (action == BattleAction.Flee || actorId == null || !_pal3AForms.TryGetValue(actorId, out Pal3AFormState state))
                return null;
            if (state.Kind == BattlePal3AFormKind.NangongFireBeast && action is not (BattleAction.Attack or BattleAction.Defend))
                return "The original fire beast form permits only physical attack, defense, or escape.";
            if (state.Kind == BattlePal3AFormKind.PengxuFairyBeast && action != BattleAction.Defend && !(action == BattleAction.Skill && magic))
                return "The original fairy beast form permits only magic, defense, or escape.";
            return null;
        }

        private void AdjustPal3AEffectiveAttributes(Actor actor, int[] attributes)
        {
            if (_scenario.Variant != "PAL3A" || NativeActorIndex(actor) != 0 || !IsPal3ATransformed(actor.Initial.InstanceId))
                return;
            // Native GetAttack/GetDefend return before ordinary status modifiers.
            attributes[3] = unchecked(actor.Attributes[3] * 2);
            attributes[4] = unchecked(actor.Attributes[4] * 2);
        }

        private int AdjustPal3AMagicPower(Actor source, int originalPower) => BattlePal3ARules.PengxuSpellPower(originalPower,
            _scenario.Variant == "PAL3A" && NativeActorIndex(source) == 2 && IsPal3ATransformed(source.Initial.InstanceId));

        private bool Pal3AIgnoresMpChange(Actor actor) => _scenario.Variant == "PAL3A" && NativeActorIndex(actor) == 2 &&
            IsPal3ATransformed(actor.Initial.InstanceId);

        private void TrackPal3AHpLoss(Actor target, int negativeDelta)
        {
            if (_scenario.Variant == "PAL3A" && NativeActorIndex(target) is >= 0 and < 5)
                _pal3AHpLoss = BattlePal3ARules.AddLoss(_pal3AHpLoss, negativeDelta);
        }

        private void TrackPal3AMpLoss(Actor target, int negativeDelta)
        {
            if (_scenario.Variant == "PAL3A" && NativeActorIndex(target) is >= 0 and < 5)
                _pal3AMpLoss = BattlePal3ARules.AddLoss(_pal3AMpLoss, negativeDelta);
        }

        private void ApplyPal3ALiaoRiCarry(Actor actor)
        {
            if (_scenario.Variant != "PAL3A" || !_preserveLiaoRiLosses || actor.Initial.ActorId != BattlePal3ARules.LiaoRiCarryActor)
                return;
            actor.Hp = actor.Attributes[0] = BattlePal3ARules.CarryAttribute(actor.Attributes[0], _pal3AHpLoss);
            actor.Mp = actor.Attributes[2] = BattlePal3ARules.CarryAttribute(actor.Attributes[2], _pal3AMpLoss);
            _preserveLiaoRiLosses = false;
        }

        private void AdvancePal3AFormsOnRound(Actor actor)
        {
            if (!_pal3AForms.TryGetValue(actor.Initial.InstanceId, out Pal3AFormState state) || state.Kind == BattlePal3AFormKind.Human || state.Pending || !actor.Alive)
                return;
            if (BattlePal3ARules.ExpiresOnRound(state.Rounds, state.RoundLimit))
                QueuePal3AFormChange(actor, false, false);
            else
                state.Rounds++;
        }

        private void HandlePal3AFormHpFloor(Actor actor, long actionId)
        {
            if (actor.Hp > 0 || !_pal3AForms.TryGetValue(actor.Initial.InstanceId, out Pal3AFormState state) || state.Kind == BattlePal3AFormKind.Human)
                return;
            if (NativeActorIndex(actor) == 0)
            {
                actor.Hp = 1;
                if (!state.LethalReturnPending)
                {
                    state.LethalReturnPending = true;
                    QueuePal3AFormChange(actor, false, true);
                }
            }
            else if (!state.Pending)
                QueuePal3AFormChange(actor, false, true);
        }

        private void PreparePal3APengxuStats(Actor actor)
        {
            Pal3AFormState state = _pal3AForms[actor.Initial.InstanceId];
            state.PengxuStatsPrepared = true;
            state.SavedHp = actor.Hp;
            state.SavedMaxHp = actor.Attributes[0];
            state.SavedDefense = actor.Attributes[4];
            actor.Hp = actor.Attributes[0] = 400;
            actor.Attributes[4] = unchecked(actor.Attributes[4] * 2);
        }

        private void RestorePal3APengxuStats(Actor actor, bool lethal)
        {
            Pal3AFormState state = _pal3AForms[actor.Initial.InstanceId];
            if (!state.PengxuStatsPrepared)
                return;
            state.PengxuStatsPrepared = false;
            actor.Attributes[0] = state.SavedMaxHp;
            actor.Attributes[4] = state.SavedDefense;
            actor.Hp = lethal ? 0 : state.SavedHp;
        }

        private void SwapPal3AForm(Actor actor, bool entering)
        {
            Pal3AFormState state = _pal3AForms[actor.Initial.InstanceId];
            int identity = NativeActorIndex(actor);
            BattlePal3AFormKind kind = entering ? identity == 0 ? BattlePal3AFormKind.NangongFireBeast : BattlePal3AFormKind.PengxuFairyBeast : BattlePal3AFormKind.Human;
            if (identity == 0)
            {
                int luck = EffectiveAttributes(actor)[6];
                state.RoundLimit = entering ? _spiritWheel.NangongRounds : 0;
                if (entering)
                    _spiritWheel = new BattleSpiritWheelSnapshot(new int[5]);
                actor.Attributes[6] = Math.Max(0, unchecked(actor.Attributes[6] + (entering ? luck : luck / -2)));
            }
            else
            {
                state.RoundLimit = entering ? 5 : 0;
                if (entering)
                    ApplyState(actor, BattleState.Barrier, false, 5);
                else
                    ApplyState(actor, BattleState.Invisible, true, sound: !_ending);
                actor.Attributes[6] = Math.Max(0, actor.Attributes[6] + (entering ? 3 : -3));
            }
            state.Rounds = 0;
            state.Kind = kind;
            state.Generation++;
            if (!entering)
                state.LethalReturnPending = false;
            try
            {
                (_catalog as IBattlePal3AFormCatalog)?.ActivatePal3AForm(actor.Initial.InstanceId, kind);
            }
            catch (Exception exception) when (_ending)
            {
                Add(BattleEventKind.PresentationCleanupFailed, actor.Initial.InstanceId, detail: exception.Message);
            }
            ClearActorThrownWeapon(actor.Initial.InstanceId);
            _physicalBodies.Remove(actor.Initial.InstanceId);
            _bodyAnimations.Remove(actor.Initial.InstanceId);
            Add(BattleEventKind.ResourceChanged, target: actor.Initial.InstanceId, detail: "native PAL3A form swapped to " + kind);
        }

        private bool _pal3AWinReturnQueued;

        private void QueuePal3AVictoryReturns()
        {
            if (_scenario.Variant != "PAL3A" || _pal3AWinReturnQueued)
                return;
            Actor[] transformed = _actors.Where(actor => IsPal3ATransformed(actor.Initial.InstanceId)).ToArray();
            if (transformed.Length == 0)
                return;
            _pal3AWinReturnQueued = true;
            foreach (Actor actor in transformed)
                QueuePal3AFormChange(actor, false, false);
        }

        private void RestorePal3AFormsAfterStageStop()
        {
            foreach (Actor actor in _actors)
            {
                if (!_pal3AForms.TryGetValue(actor.Initial.InstanceId, out Pal3AFormState state))
                    continue;
                // Pengxu's native stats change when the task is queued, before
                // the model swap. A stopped task can therefore still be human.
                if (state.PengxuStatsPrepared)
                    RestorePal3APengxuStats(actor, actor.Hp <= 0);
                if (state.Kind != BattlePal3AFormKind.Human)
                    SwapPal3AForm(actor, false);
                state.Pending = false;
                state.LethalReturnPending = false;
            }
            foreach (Pal3AFormStage stage in _pal3AFormStages)
                stage.Finished = true;
            _absoluteTimeActor = null;
        }

        private sealed class Pal3AFormStage : IBattleStageCancellableJob
        {
            internal BattleSession Session;
            internal Actor Actor;
            internal bool Entering, Lethal;
            internal int Phase;
            internal bool AnimationStarted;
            internal bool Finished;
            internal BattleStageHandle Handle;
            internal long EffectHandle;

            public bool Advance(BattleStageContext context) => Session.AdvancePal3AFormStage(this);

            public void Cancel()
            {
                try
                {
                    if (Entering && Phase < 3 && NativeActorIndex(Actor) == 2)
                        Session.RestorePal3APengxuStats(Actor, Actor.Hp <= 0);
                    Session.StopEffect(EffectHandle);
                }
                finally
                {
                    Session.CompletePal3AFormStage(this);
                }
            }
        }

        private readonly List<Pal3AFormStage> _pal3AFormStages = new();

        private IEnumerable<int> ActivePal3AFormTasks => _pal3AFormStages.Where(stage => !stage.Finished).Select(_ => 31);

        private bool HasActivePal3AFormBody(Actor actor) => _pal3AFormStages.Any(stage => stage.Actor == actor && !stage.Finished && stage.Phase > 0);

        private BattleStageHandle QueuePal3AFormChange(Actor actor, bool entering, bool lethal)
        {
            Pal3AFormState state = _pal3AForms[actor.Initial.InstanceId];
            if (state.Pending)
                return _pal3AFormStages.Last(stage => stage.Actor == actor && !stage.Finished).Handle;
            if (NativeActorIndex(actor) == 2)
            {
                if (entering)
                    PreparePal3APengxuStats(actor);
                else
                    RestorePal3APengxuStats(actor, lethal);
            }
            var stage = new Pal3AFormStage { Session = this, Actor = actor, Entering = entering, Lethal = lethal };
            if (!_stage.TryAdd(actor.Initial.InstanceId, "PAL3A form", stage, BattleStageScheduler.TimeAt(SimulationTick), out BattleStageHandle handle))
                throw new InvalidOperationException("The original form transition task could not be allocated.");
            stage.Handle = handle;
            state.Pending = true;
            _pal3AFormStages.Add(stage);
            return handle;
        }

        private void StartPal3ATransformAction(Actor actor, ActionRun run)
        {
            if (run.Kind == BattleAction.Transform)
                run.StageHandle = QueuePal3AFormChange(actor, true, false);
        }

        private void StartPal3APengxuAction(Actor actor, ActionRun run)
        {
            if (_scenario.Variant == "PAL3A" && run.Ability?.Id == BattlePal3ARules.PengxuTransformSkill && !_pal3AForms[actor.Initial.InstanceId].Pending)
                run.FormStageHandle = QueuePal3AFormChange(actor, true, false);
        }

        private bool AdvancePal3APengxuStage(Actor actor, ActionRun run, BattleStageContext context)
        {
            if (run.AbilityResolved)
                return true;
            if (context.Tick - run.TimelineOriginTick < run.Timing.PreparationTicks || !PhysicalBodyIdle(actor))
                return false;
            StartPal3APengxuAction(actor, run);
            run.StageStarted = run.AbilityResolved = true;
            PaySkillCost(actor, run.Ability, run.Id, 0);
            return true;
        }

        private bool AdvancePal3AFormStage(Pal3AFormStage stage)
        {
            Actor actor = stage.Actor;
            Pal3AFormState state = _pal3AForms[actor.Initial.InstanceId];
            if (_catalog is not IBattlePal3AFormCatalog catalog)
                throw new NotSupportedException("The original form animation catalog is missing.");
            if (stage.Phase == 0)
            {
                if (!PhysicalBodyIdle(actor) || IsActorExternallyControlled(actor.Initial.InstanceId) ||
                    _absoluteTimeActor != null && _absoluteTimeActor != actor.Initial.InstanceId || !TryGrabStageBody(actor, stage.Handle))
                    return false;
                _absoluteTimeActor = actor.Initial.InstanceId;
                stage.Phase = NativeActorIndex(actor) == 0 ? 1 : 2;
            }
            if (stage.Phase == 1)
            {
                if (!AdvancePal3AFormAnimation(stage, catalog, 164))
                    return false;
                stage.Phase = 2;
                stage.AnimationStarted = false;
            }
            if (stage.Phase == 2)
            {
                SwapPal3AForm(actor, stage.Entering);
                stage.Phase = 3;
                if (!stage.Entering)
                {
                    if (NativeActorIndex(actor) == 2 && _effects != null)
                        StartEffect(new BattleEffectRequest(605, actor.Initial.InstanceId, new[] { actor.Initial.InstanceId }, actor.Position,
                            unchecked((uint)_scenario.Seed), true));
                    CompletePal3AFormStage(stage);
                    return true;
                }
                return false;
            }
            if (!AdvancePal3AFormAnimation(stage, catalog, NativeActorIndex(actor) == 2 && stage.Entering ? 605 : 0))
                return false;
            CompletePal3AFormStage(stage);
            return true;
        }

        private void CompletePal3AFormStage(Pal3AFormStage stage)
        {
            if (stage.Finished)
                return;
            _pal3AForms[stage.Actor.Initial.InstanceId].Pending = false;
            ReleaseStageBody(stage.Actor, stage.Handle);
            stage.Finished = true;
            if (stage.Phase > 0 && _absoluteTimeActor == stage.Actor.Initial.InstanceId)
                _absoluteTimeActor = null;
        }

        private bool AdvancePal3AFormAnimation(Pal3AFormStage stage, IBattlePal3AFormCatalog catalog, int effect)
        {
            Actor actor = stage.Actor;
            if (!stage.AnimationStarted)
            {
                // Native Actor::GotoAction keeps the selected model's clock and
                // message FIFO when z20 is absent, as it is for model 106. The
                // form body still waits for that model's next Over message.
                BattlePhysicalAnimation animation = catalog.FormChangeAnimation(actor.Initial.InstanceId, IsPal3ATransformed(actor.Initial.InstanceId));
                TryPlayBodyAnimation(actor, animation, false, BattlePose.UsingSkill, 60);
                if (effect > 0 && _effects != null)
                    stage.EffectHandle = StartEffect(new BattleEffectRequest(effect, actor.Initial.InstanceId, new[] { actor.Initial.InstanceId }, actor.Position,
                        unchecked((uint)_scenario.Seed), true));
                stage.AnimationStarted = true;
                return false;
            }
            return GetBodyBinding(actor).LastPoppedMarker == BattlePhysicalMarkerKind.Over;
        }
    }
}
