// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;

    public sealed partial class BattleSession
    {
        private readonly Dictionary<string, BattleBodyAnimationBinding> _bodyAnimations = new(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _bodyPassRates = new(StringComparer.Ordinal);

        public BattlePhysicalAnimation CurrentBodyAnimation(string actorId) => _byId.TryGetValue(actorId, out Actor actor) ? GetBodyBinding(actor).CurrentAnimation : null;

        public BattleBodyAnimationBinding CaptureBodyAnimation(string actorId) => _byId.TryGetValue(actorId, out Actor actor) ? GetBodyBinding(actor).Clone() : null;

        private BattleBodyAnimationBinding GetBodyBinding(Actor actor)
        {
            if (_bodyAnimations.TryGetValue(actor.Initial.InstanceId, out BattleBodyAnimationBinding binding))
                return binding;
            BattlePhysicalStageProfile profile = (_catalog as IBattlePhysicalTimings)?.Physical(actor.Initial.InstanceId);
            BattlePose pose = !actor.ModelAlive && actor.Initial.Side == BattleSide.Ally ? BattlePose.Dead : BattlePose.Idle;
            BattlePhysicalAnimation initial = profile?.Animation(pose);
            binding = new BattleBodyAnimationBinding(initial);
            binding.SetLogicalPose(pose);
            binding.SetRequestedNativeAction(actor.Initial.Side == BattleSide.Enemy ? 50 : pose == BattlePose.Dead ? 47 : 32);
            _bodyAnimations.Add(actor.Initial.InstanceId, binding);
            return binding;
        }

        private void SetBodyAnimationPose(Actor actor, BattlePose pose) => GetBodyBinding(actor).SetLogicalPose(pose);

        private bool TryPlayBodyAnimation(Actor actor, BattlePhysicalAnimation animation, bool loop, BattlePose? requestedPose = null, int? requestedNativeActionIndex = null)
        {
            BattleBodyAnimationBinding binding = GetBodyBinding(actor);
            bool sameRequest = requestedNativeActionIndex.HasValue ? requestedNativeActionIndex.Value == binding.RequestedNativeActionIndex : requestedPose == binding.LogicalPose;
            bool unchanged = animation != null && loop && sameRequest && string.Equals(animation.Clip, binding.CurrentAnimation?.Clip, StringComparison.Ordinal);
            if (requestedPose.HasValue)
                binding.SetLogicalPose(requestedPose.Value);
            if (requestedNativeActionIndex.HasValue)
                binding.SetRequestedNativeAction(requestedNativeActionIndex.Value);
            return unchanged || binding.TryPlay(animation, loop);
        }

        private BattleMovementMessage CurrentBodyMovementMessage(Actor actor) => GetBodyBinding(actor).LastPoppedMarker switch
        {
            BattlePhysicalMarkerKind.FlyUp => BattleMovementMessage.FlyUp,
            BattlePhysicalMarkerKind.FlyDown => BattleMovementMessage.FlyDown,
            BattlePhysicalMarkerKind.Over => BattleMovementMessage.Over,
            _ => BattleMovementMessage.None
        };

        private void EnsureMovementBodyAnimation(Actor actor)
        {
            if (_catalog is not IBattlePhysicalTimings || actor.Movement == null || actor.Movement.ElapsedTicks != 0)
                return;
            BattlePose pose = actor.Movement.Plan.Kind == BattleMovementKind.Teleport ? BattlePose.Idle : BattlePose.Moving;
            TryPlayBodyAnimation(actor, PhysicalBody(actor).Profile.Animation(pose), true, pose, PhysicalBody(actor).Profile.NativeAction(pose));
        }

        private void SetBodyBladeSeconds(Actor actor, float seconds)
        {
            PhysicalBodyState body = PhysicalBody(actor);
            if (body != null)
                body.BladeSeconds = seconds;
        }

        public float CurrentBladeSeconds(string actorId) => _byId.TryGetValue(actorId, out Actor actor) ? PhysicalBody(actor)?.BladeSeconds ?? .01f : .01f;

        private void EnsureProperBodyAnimation(Actor actor)
        {
            if (_catalog is not IBattlePhysicalTimings || PhysicalBodyBusy(actor) || HasFormationMovement(actor) ||
                IsActorExternallyControlled(actor.Initial.InstanceId) || HasActiveOrdinaryAbilityBody(actor) || HasActivePal3AFormBody(actor))
                return;
            PhysicalBodyState body = PhysicalBody(actor);
            BattlePose pose = BattlePose.Idle;
            if (actor.Initial.Side == BattleSide.Ally)
            {
                if (actor.Hp == 0)
                    pose = BattlePose.Dead;
                else if (actor.Run?.Casting == true)
                    pose = BattlePose.Casting;
                else if (actor.Guard)
                    pose = BattlePose.Guard;
                else if (Has(actor, BattleState.Sleep) || Has(actor, BattleState.Paralysis))
                    pose = BattlePose.Sleeping;
                else if (actor.Hp < actor.Attributes[0] / 4)
                    pose = BattlePose.Dying;
            }
            body.Pose = pose;
            body.BladeSeconds = pose == BattlePose.Guard ? .25f : .01f;
            TryPlayBodyAnimation(actor, body.Profile.Animation(pose), true, pose, body.Profile.NativeAction(pose));
        }

        private void SetNativeBodyAction(Actor actor, int action) => GetBodyBinding(actor).SetRequestedNativeAction(action);

        private void ResetBodyAnimation(Actor actor) => GetBodyBinding(actor).Reset();

        private void BeginBodyAnimationPass(Actor actor)
        {
            BattleBodyAnimationBinding binding = GetBodyBinding(actor);
            float rate = _scenario.Variant == "PAL3" ? 1.3f : 1f;
            if (!IsActorExternallyControlled(actor.Initial.InstanceId) && binding.RequestedNativeActionIndex is 32 or 50)
                rate = .65f;
            _bodyPassRates[actor.Initial.InstanceId] = rate;
            binding.PopMessage();
            if (!IsActorExternallyControlled(actor.Initial.InstanceId))
                AdvanceThrownWeapon(actor);
        }

        private void EndBodyAnimationPass(Actor actor)
        {
            SyncBodyMovementSound(actor);
            GetBodyBinding(actor).Advance(_bodyPassRates.GetValueOrDefault(actor.Initial.InstanceId, 1f));
        }
    }
}
