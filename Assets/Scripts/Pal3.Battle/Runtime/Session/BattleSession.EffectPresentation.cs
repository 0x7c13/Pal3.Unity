// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public interface IBattleEffectActorAnimations
    {
        BattlePhysicalAnimation ExternalBody(BattleActorSnapshot actor, string requested, BattleDefinitions definitions);

        string EffectActor(uint modelCrc);
    }

    public interface IBattleModalEffects
    {
        bool BlocksBattleTime
        {
            get;
        }

        bool SkipModalPresentation();
    }

    public interface IBattlePresentationPreparation
    {
        bool IsPreparingPlayback
        {
            get;
        }
    }

    public sealed class BattleEffectActorSnapshot
    {
        public bool HasBlend
        {
            get; internal set;
        }

        public string Id
        {
            get; internal set;
        }

        public BattleMovementPoint Position
        {
            get; internal set;
        }

        public float FacingDegrees
        {
            get; internal set;
        }

        public float Blend
        {
            get; internal set;
        }

        public bool Controlled
        {
            get; internal set;
        }

        public string Clip
        {
            get; internal set;
        }

        public int AnimationTick
        {
            get; internal set;
        }

        public bool ActionOver
        {
            get; internal set;
        }

        public bool LongBlade
        {
            get; internal set;
        }

        public float LeftWeapon
        {
            get; internal set;
        }

        public float RightWeapon
        {
            get; internal set;
        }
    }

    public sealed partial class BattleSession
    {
        private sealed class ExternalEffectActor
        {
            public readonly HashSet<object> Controllers = new();
            public bool LongBlade;

            public bool Controlled => Controllers.Count != 0;

            public float? Blend;
            public float LeftWeapon, RightWeapon;
        }

        private readonly List<long> _modalTransitions = new();

        // Consecutive entries mark the beginning and resumption of blocked battle frames.
        internal bool MatchesModalTimeline(BattleSession other) =>
            IsModalPresentation == other.IsModalPresentation && _modalTransitions.SequenceEqual(other._modalTransitions);

        public bool SkipModalPresentation()
        {
            if (Status != BattleSessionStatus.Running || _effects is not IBattleModalEffects modal ||
                !modal.BlocksBattleTime || !modal.SkipModalPresentation())
                return false;
            _inputs.Add(new BattleInput { Tick = Tick, Kind = BattleInputKind.ModalPresentationSkip });
            return true;
        }

        private readonly Dictionary<string, ExternalEffectActor> _externalEffectActors = new(StringComparer.Ordinal);

        public string EffectActorId(int nativeIndex, uint modelCrc = 0) => modelCrc > 0 ?
            (_catalog as IBattleEffectActorAnimations)?.EffectActor(modelCrc) : ActorAtNativeIndex(nativeIndex)?.Initial.InstanceId;

        public bool IsActorExternallyControlled(string actor) => _externalEffectActors.TryGetValue(actor, out ExternalEffectActor state) && state.Controlled;

        public BattleEffectActorSnapshot EffectActor(string actorId)
        {
            if (!_byId.TryGetValue(actorId ?? "", out Actor actor))
                return null;
            _externalEffectActors.TryGetValue(actorId, out ExternalEffectActor state);
            BattleBodyAnimationBinding body = GetBodyBinding(actor);
            return new BattleEffectActorSnapshot
            {
                Id = actorId,
                Position = actor.Position,
                FacingDegrees = actor.FacingDegrees,
                Blend = state?.Blend ?? actor.Movement?.Blend ?? (actor.States.ContainsKey(BattleState.Invisible) ? 0.44f : 1),
                HasBlend = state?.Blend.HasValue == true,
                Controlled = state?.Controlled == true,
                Clip = state?.Controlled == true ? body.CurrentAnimation?.Clip : null,
                AnimationTick = body.LocalTick,
                ActionOver = body.LastPoppedMarker == BattlePhysicalMarkerKind.Over,
                LongBlade = state?.LongBlade == true,
                LeftWeapon = state?.LeftWeapon ?? 0,
                RightWeapon = state?.RightWeapon ?? 0
            };
        }

        private ExternalEffectActor ExternalActor(string actorId)
        {
            if (!_byId.ContainsKey(actorId ?? ""))
                throw new ArgumentException("Unknown effect-controlled actor.", nameof(actorId));
            if (!_externalEffectActors.TryGetValue(actorId, out ExternalEffectActor state))
                _externalEffectActors.Add(actorId, state = new ExternalEffectActor());
            return state;
        }

        public void SetEffectActorControlled(string actorId, bool controlled) => SetEffectActorControlled(actorId, controlled, null);

        public void SetEffectActorControlled(string actorId, bool controlled, object owner)
        {
            // An explicit stage flag and each running action node release independently.
            var controllers = ExternalActor(actorId).Controllers;
            if (controlled)
            {
                controllers.Add(owner);
            }
            else
            {
                controllers.Remove(owner);
            }
        }

        public void SetEffectActorAction(string actorId, string requested)
        {
            if (requested == null)
                return;
            Actor actor = _byId[actorId];
            if (_catalog is not IBattleEffectActorAnimations catalog)
                throw new InvalidOperationException("External actor control requires original animation resources.");
            ExternalActor(actorId);
            TryPlayBodyAnimation(actor, catalog.ExternalBody(Snapshot(actor), requested, Definitions), false);
            ResetBodyAnimation(actor);
        }

        public void SetEffectActorPosition(string actorId, BattleMovementPoint position) => SetActorPosition(_byId[actorId], position);

        public void SetEffectActorFacing(string actorId, float facing)
        {
            if (!float.IsFinite(facing))
                throw new ArgumentException("An original facing must be finite.", nameof(facing));
            Actor actor = _byId[actorId];
            actor.FacingDegrees = facing;
            actor.Movement?.SetHeading(facing);
        }

        public void SetEffectActorBlend(string actorId, float blend) => ExternalActor(actorId).Blend = Math.Clamp(blend, 0, 1);

        public void SetEffectActorBlade(string actorId, bool longBlade)
        {
            ExternalActor(actorId).LongBlade = longBlade;
            SetBodyBladeSeconds(_byId[actorId], longBlade ? 0.25f : 0.01f);
        }

        public void SetEffectActorWeapons(string actorId, float left, float right)
        {
            ExternalEffectActor actor = ExternalActor(actorId);
            actor.LeftWeapon = left;
            actor.RightWeapon = right;
        }

        // CCBStageRole keeps its body task while external control suppresses
        // Proceed. After release that task observes the external clip's over.
        private bool TryAdvanceExternalEffectBody(Actor actor, ActionRun run) => IsActorExternallyControlled(actor.Initial.InstanceId);
    }
}
