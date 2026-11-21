// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Mv3;
    using Engine.Extensions;

    public sealed partial class BattleAnimationCatalog : IBattlePhysicalTimings
    {
        private readonly Dictionary<string, BattlePhysicalStageProfile> _physicalProfiles = new(StringComparer.Ordinal);

        private static int PhysicalIdentity(BattleParticipant actor) => actor.Side == BattleSide.Enemy ? -1 : BattleData.PlayerIdentity(actor.ActorId);

        public BattlePhysicalStageProfile Physical(string actor)
        {
            if (_physicalProfiles.TryGetValue(actor, out BattlePhysicalStageProfile cached))
                return cached;
            BattleParticipant participant = _participants[actor];
            int identity = PhysicalIdentity(participant);
            int weapon = 0, count = 1;
            if (identity >= 0 && participant.Equipment.Length > 0 && participant.Equipment[0] != 0)
            {
                var item = _data.Database.GameItemInfos[participant.Equipment[0]];
                weapon = (int)item.WeaponType;
                count = item.AttackHitCount;
            }
            var animations = new Dictionary<BattlePose, BattlePhysicalAnimation>();
            foreach (var pair in _actors[actor].Clips)
                animations[pair.Key] = CompilePhysicalAnimation(participant, pair.Value);
            if (participant.Side == BattleSide.Ally)
            {
                animations[BattlePose.Dying] = CompilePhysicalAnimation(participant, ActorConstants.ActionToNameMap[ActorActionType.Dying]);
                animations[BattlePose.Sleeping] = CompilePhysicalAnimation(participant, ActorConstants.ActionToNameMap[ActorActionType.Freeze]);
                animations[BattlePose.GuardHurt] = CompilePhysicalAnimation(participant, ActorConstants.ActionToNameMap[ActorActionType.DefenceBeAttack]);
            }
            animations[BattlePose.Casting] = CompilePhysicalAnimation(participant, ActorConstants.ActionToNameMap[participant.Side == BattleSide.Ally ? ActorActionType.MagicWait : ActorActionType.NpcMagic1]);
            animations[BattlePose.Attacking] = CompilePhysicalAnimation(participant, _data.AttackAction(participant));
            var profile = new BattlePhysicalStageProfile(BattleData.Variant, participant.ActorId, identity, weapon, count, animations, thrownStart: BattleData.Variant == "PAL3A" ? CompilePhysicalAnimation(participant, "z021") : null,
                thrownReturn: BattleData.Variant == "PAL3A" ? CompilePhysicalAnimation(participant, "z022") : null);
            _physicalProfiles.Add(actor, profile);
            return profile;
        }

        private BattlePhysicalAnimation CompilePhysicalAnimation(BattleParticipant actor, string clipName)
        {
            if (!_data.TryActionPath(actor, clipName, out string path))
                return null;
            BattleAnimationMetadata clip = _data.GetAnimationMetadata(path);
            if (!clip.HasBounds)
                throw new NotSupportedException($"Actor {actor.ActorId}, clip {clipName}: original collision bounds are missing.");
            float minimum = clip.MinimumZ;
            float maximum = clip.MaximumZ;
            var markers = new List<BattlePhysicalMarker>();
            foreach (Mv3AnimationEvent marker in clip.AnimationEvents)
            {
                int tick = Math.Max(0, checked((int)Math.Ceiling(marker.GameBoxTick /
                    (double)UnityPrimitivesConvertor.GameBoxTicksPerSecond / ActivePlaybackRate / BattleSession.StepSeconds)));
                markers.Add(new BattlePhysicalMarker(tick, PhysicalMarkerKind(marker.Name), marker.GameBoxTick, marker.Name));
            }
            int duration = ToTicks(clip.TotalGameBoxTicks / (double)UnityPrimitivesConvertor.GameBoxTicksPerSecond / ActivePlaybackRate);
            return new BattlePhysicalAnimation(clipName, duration, (maximum - minimum) / 2f, markers, clip.TotalGameBoxTicks);
        }

        private static BattlePhysicalMarkerKind PhysicalMarkerKind(string name) => name.ToLowerInvariant() switch
        {
            "work" => BattlePhysicalMarkerKind.Work,
            "work1" => BattlePhysicalMarkerKind.Work1,
            "work2" => BattlePhysicalMarkerKind.Work2,
            "work3" => BattlePhysicalMarkerKind.Work3,
            "over" => BattlePhysicalMarkerKind.Over,
            "flyup" or "sjump" => BattlePhysicalMarkerKind.FlyUp,
            "flydown" or "ejump" => BattlePhysicalMarkerKind.FlyDown,
            "start" => BattlePhysicalMarkerKind.Start,
            "catched" => BattlePhysicalMarkerKind.Catched,
            "throwed" => BattlePhysicalMarkerKind.Throwed,
            _ => BattlePhysicalMarkerKind.Other
        };

        private BattleAttackPlan CompilePhysicalAttackPlan(string source, string target, BattleMovementPoint sourceHome, BattleMovementPoint targetHome)
        {
            BattlePhysicalStageProfile profile = Physical(source);
            BattlePhysicalAnimation animation = profile.Animation(BattlePose.Attacking);
            int[] work = (animation?.Markers ?? Array.Empty<BattlePhysicalMarker>()).Where(marker => marker.Kind <= (BattleData.Variant == "PAL3A" ? BattlePhysicalMarkerKind.Work3 : BattlePhysicalMarkerKind.Work1))
                .Select(marker => marker.Tick).ToArray();
            // Travel is a native body task, not a precomputed linear duration. This
            // compatibility timeline exposes only the clip's real work messages.
            var timing = new AttackTiming(0, Math.Max(animation?.DurationTicks ?? 1, work.DefaultIfEmpty(0).Max()), work, allowEmptyHits: true);
            var direction = BattleMovement.NormalizeDirection(new BattleMovementPoint(sourceHome.X - targetHome.X,
                sourceHome.Y - targetHome.Y, sourceHome.Z - targetHome.Z));
            float distance = profile.Radius(BattlePose.Idle) + Physical(target).Radius(BattlePose.Idle);
            if (profile.ResourceId is 7364 or 7381)
                distance *= 2;
            bool stationary = profile.Approach().Count == 1 && profile.Approach()[0] == BattlePhysicalBodyKind.TurnToRole || profile.UsesThrownWeapon;
            var approach = stationary ? sourceHome : new BattleMovementPoint(targetHome.X + direction.X * distance,
                targetHome.Y + direction.Y * distance, targetHome.Z + direction.Z * distance);
            return new BattleAttackPlan(timing, sourceHome, approach, targetHome);
        }
    }
}
