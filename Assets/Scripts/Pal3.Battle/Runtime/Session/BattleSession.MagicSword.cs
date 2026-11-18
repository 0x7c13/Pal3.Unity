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

    public sealed partial class BattleSession
    {
        private readonly Dictionary<string, BattleMagicSwordSnapshot> _magicSwords = new(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, BattleMagicSwordSnapshot> MagicSwords =>
            new ReadOnlyDictionary<string, BattleMagicSwordSnapshot>(_magicSwords.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));

        public int MagicSwordAbilityId(string actor) => actor != null && _magicSwords.TryGetValue(actor, out BattleMagicSwordSnapshot snapshot) ? snapshot.SkillId : 0;

        private static bool IsMagicSwordAbility(BattleAbilityDefinition ability) => ability is { IsItem: false, SkillTypeId: 4 };

        private void InitializeActorMagicSword(Actor actor)
        {
            BattleMagicSwordSnapshot captured = actor.Definition?.MagicSword;
            BattleMagicSword authored = actor.Initial.MagicSword;
            if (captured == null && authored == null)
                return;
            if (Definitions?.Variant != "PAL3" || actor.Initial.Side != BattleSide.Ally || actor.Definition?.Identity != 0 ||
                !actor.Definition.Equipment.Any(item => item.Slot == 0 && item.Id == BattleMagicSwordRules.WeaponId) ||
                captured == null || authored == null)
                throw new ArgumentException("A magic sword needs PAL3 Jingtian, weapon 6603 and matching captured cultivation.");
            BattleMagicSwordSnapshot initial = authored.Capture();
            if (captured.AttackBonus != initial.AttackBonus || captured.SelectedElement != initial.SelectedElement ||
                captured.PurificationCount != initial.PurificationCount || captured.SkillId != initial.SkillId ||
                captured.ChargeCoefficient != initial.ChargeCoefficient || captured.Energy != initial.Energy)
                throw new ArgumentException("Magic sword definitions do not match the captured scenario.");
            if (captured.SkillId != 0 && (!Definitions.Skills.TryGetValue(captured.SkillId, out BattleAbilityDefinition ability) ||
                !IsMagicSwordAbility(ability) || !BattleMagicSwordRules.IsSupportedSkill(ability.Id) || !ability.Supported))
                throw new NotSupportedException("The captured magic sword has no verified original weapon skill record.");
            _magicSwords.Add(actor.Initial.InstanceId, initial);
        }

        private BattleMagicSwordSnapshot CaptureMagicSword(Actor actor) => _magicSwords.GetValueOrDefault(actor.Initial.InstanceId);

        private string ValidateMagicSwordCommand(Actor actor, BattleAbilityDefinition ability)
        {
            BattleMagicSwordSnapshot snapshot = CaptureMagicSword(actor);
            if (Definitions?.Variant != "PAL3" || snapshot == null || !BattleMagicSwordRules.IsSupportedSkill(ability.Id) ||
                snapshot.SkillId != ability.Id || actor.Definition?.Identity != 0)
                return "This actor has no captured magic sword with the selected weapon skill.";
            if (!BattleMagicSwordRules.IsReady(snapshot, actor.Definition.Identity, true))
                return "The magic sword energy must be greater than 199.9.";
            return null;
        }

        private void GainMagicSwordOnPhysicalWork(Actor actor, long actionId)
        {
            BattleMagicSwordSnapshot snapshot = CaptureMagicSword(actor);
            if (snapshot == null)
                return;
            _magicSwords[actor.Initial.InstanceId] = BattleMagicSwordRules.GainOnPhysicalWork(snapshot, actor.Definition.Identity, true);
        }

        // Called on the native system-thread first pass, before stage waits or target effects.
        private void StartMagicSwordAction(Actor actor, ActionRun run)
        {
            if (!IsMagicSwordAbility(run.Ability))
                return;
            BattleMagicSwordSnapshot snapshot = CaptureMagicSword(actor) ?? throw new InvalidOperationException("Missing magic sword state.");
            run.Targets = FightSkillRange(actor, run.Ability.Range).Select(target => target.Initial.InstanceId).ToArray();
            _magicSwords[actor.Initial.InstanceId] = BattleMagicSwordRules.ConsumeEnergy(snapshot);
        }

        private sealed class MagicSwordStageRun
        {
            public BattleMagicSwordStageProfile Profile;
            public string Actor;
            public float NameStarted, ActStarted;
            public bool NameShown, FirstStarted, SecondStarted, Worked;
            public bool UsesBodyBinding, BodyExecuted, BodyFinished;
            public BattlePhysicalAnimation BodyRequested;
            public long FirstHandle, SecondHandle;
        }

        private readonly Dictionary<long, MagicSwordStageRun> _magicSwordStages = new();

        private bool AdvanceMagicSwordStage(Actor actor, ActionRun run, BattleStageContext context)
        {
            if (!_magicSwordStages.TryGetValue(run.Id, out MagicSwordStageRun stage))
            {
                if (_catalog is not IBattleMagicSwordTimings timings || _effects == null)
                    throw new NotSupportedException("The magic sword needs its original effect completion director.");
                stage = new MagicSwordStageRun { Actor = actor.Initial.InstanceId, Profile = timings.MagicSword(actor.Initial.InstanceId, run.Ability) };
                if (_catalog is IBattleAbilityBodyAnimations bodies)
                {
                    stage.UsesBodyBinding = true;
                    stage.BodyRequested = bodies.AbilityBody(actor.Initial.InstanceId, run.Ability);
                }
                _magicSwordStages.Add(run.Id, stage);
            }
            if (run.EndRequested || actor.Escaped || !actor.Alive)
            {
                ReleaseStageBody(actor, context.Handle);
                CancelMagicSwordStage(run);
                return true;
            }
            if (!stage.NameShown)
            {
                if (HasFormationMovement(actor) || PhysicalBodyBusy(actor) || IsActorExternallyControlled(actor.Initial.InstanceId) || context.Tick < actor.HurtUntil ||
                    !TryGrabStageBody(actor, context.Handle))
                    return false;
                stage.NameShown = true;
                stage.NameStarted = context.Time;
                ShowStageMessage(stage, actor.Initial.Side, run.Ability.Name, handle: context.Handle);
                return false;
            }
            if ((double)context.Time - stage.NameStarted > BattleMagicSwordStageProfile.NameDelaySeconds)
                HideStageMessage(stage);
            if (!run.StageStarted)
            {
                if (context.Time <= stage.NameStarted ||
                    HasFormationMovement(actor) || PhysicalBodyBusy(actor) || IsActorExternallyControlled(actor.Initial.InstanceId) || context.Tick < actor.HurtUntil)
                    return false;
                run.StageStarted = true;
                run.BodyElapsed = 0;
                run.BodyRestartPending = true;
                stage.ActStarted = context.Time;
            }
            double elapsed = (double)context.Time - stage.ActStarted;
            if (!stage.FirstStarted && elapsed >= stage.Profile.FirstWaitSeconds)
            {
                if (_absoluteTimeActor != null && _absoluteTimeActor != actor.Initial.InstanceId)
                    return false;
                stage.FirstHandle = StartMagicSwordGroup(stage.Profile.FirstGroupId, stage.Profile.FirstOrigin, actor, run);
                stage.FirstStarted = true;
                _absoluteTimeActor = actor.Initial.InstanceId;
            }
            if (!stage.SecondStarted && elapsed >= stage.Profile.SecondWaitSeconds)
            {
                stage.SecondHandle = StartMagicSwordGroup(stage.Profile.SecondGroupId, stage.Profile.SecondOrigin, actor, run);
                stage.SecondStarted = true;
            }
            if (!stage.Worked && stage.FirstStarted && (stage.FirstHandle == 0 || !_effects.IsRunning(stage.FirstHandle)))
            {
                _absoluteTimeActor = null;
                stage.Worked = true;
                ResolveActionMarker(actor, run);
                if (actor.Run != run || Status != BattleSessionStatus.Running)
                    return true;
            }
            if (!stage.Worked || (stage.UsesBodyBinding ? !stage.BodyFinished : run.BodyElapsed < stage.Profile.BodyAnimationTicks) ||
                PhysicalBodyBusy(actor) || IsActorExternallyControlled(actor.Initial.InstanceId) || context.Tick < actor.HurtUntil || HasFormationMovement(actor))
                return false;
            // ShowName is parallel: Act starts one tick later, while the whole
            // stage keeps its name subtask until the strict one-second boundary.
            if ((double)context.Time - stage.NameStarted <= BattleMagicSwordStageProfile.NameDelaySeconds)
                return false;
            // Group 2 belongs to the ATS effect manager and may outlive the role/stage task.
            _magicSwordStages.Remove(run.Id);
            HideStageMessage(stage);
            ReleaseStageBody(actor, context.Handle);
            return true;
        }

        private bool TryAdvanceMagicSwordBody(Actor actor, ActionRun run)
        {
            if (!_magicSwordStages.TryGetValue(run.Id, out MagicSwordStageRun stage) || !stage.UsesBodyBinding || !run.StageStarted)
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
                SetBodyBladeSeconds(actor, 0.25f);
                ResetActorFacing(actor);
                TryPlayBodyAnimation(actor, stage.BodyRequested, false, BattlePose.UsingSkill, 40);
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

        private long StartMagicSwordGroup(int group, BattleMovementPoint origin, Actor actor, ActionRun run) => group < 0 ? 0 :
            StartEffect(new BattleEffectRequest(group, actor.Initial.InstanceId, run.Targets, origin, unchecked((uint)_scenario.Seed), absoluteTime: true));

        private void CancelMagicSwordStage(ActionRun run)
        {
            if (run == null || !_magicSwordStages.Remove(run.Id, out MagicSwordStageRun stage))
                return;
            HideStageMessage(stage);
            if (_absoluteTimeActor == stage.Actor)
                _absoluteTimeActor = null;
            if (stage.FirstHandle != 0)
                StopEffect(stage.FirstHandle);
            if (stage.SecondHandle != 0)
                StopEffect(stage.SecondHandle);
        }

        // This marker is the effect-group-1 completion callback, never the linker work time.
        private bool ResolveMagicSwordMarker(Actor source, ActionRun run)
        {
            if (!IsMagicSwordAbility(run.Ability))
                return false;
            if (run.AbilityResolved)
                return true;
            run.AbilityResolved = true;
            BattleMagicSwordSnapshot snapshot = CaptureMagicSword(source) ?? throw new InvalidOperationException("Missing magic sword state.");
            BattleAbilityDefinition ability = run.Ability;
            int lastTargetReturn = 0;
            foreach (string targetId in run.Targets)
            {
                Actor target = _byId[targetId];
                if (target.Escaped)
                    continue;
                bool group = ability.Range is not (BattleTargetRange.OwnSingle or BattleTargetRange.OpponentSingle);
                if (!group && !IsAtHome(target))
                {
                    Add(BattleEventKind.Miss, source.Initial.InstanceId, targetId, run.Id, "weapon skill target away from home");
                    continue;
                }
                int returned = 0;
                bool hpRecovered = false, mpRecovered = false;
                foreach (BattleAttributeEffect effect in ability.Attributes.OrderBy(value => value.Attribute))
                {
                    if (effect.Value == 0)
                        continue;
                    if (effect.Attribute > 2)
                        throw new NotSupportedException("An original weapon skill cannot change this combat resource.");
                    int power = BattleMagicSwordRules.SkillPower(snapshot, ability.Id, checked((short)effect.Value), target.Initial.Side, out BattleValueMode mode);
                    int requested = mode == BattleValueMode.Percentage ? (int)((long)target.Attributes[effect.Attribute] * power / 100) : power;
                    returned = ChangeResource(target, effect.Attribute, requested, source, run.Id, ability.Name);
                    hpRecovered |= effect.Attribute == 0 && returned > 0;
                    mpRecovered |= effect.Attribute == 2 && returned > 0;
                }
                PlaySound(BattleEffectAudioRules.Recovery(hpRecovered, mpRecovered));
                // Released WeaponSkillAffect changes values first, including on dead targets.
                ApplyAbilityStates(source, target, run);
                lastTargetReturn = returned;
                if (returned < 0 && target.Run?.Casting == true)
                {
                    Cancel(target, "weapon skill hit interrupted casting");
                    target.Ready = false;
                    target.Gauge = 0;
                }
            }
            PaySkillCost(source, ability, run.Id, lastTargetReturn);
            // Native WeaponSkillDamage clears again after every target and FightSkillCost.
            _magicSwords[source.Initial.InstanceId] = BattleMagicSwordRules.ConsumeEnergy(CaptureMagicSword(source));
            CheckResult();
            return true;
        }
    }
}
