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
    using Core.DataReader.Dat;
    using Engine.Extensions;

    public sealed partial class BattleAnimationCatalog : IBattleAbilityEffects, IBattleAbilityBodyAnimations, IBattleEffectActorAnimations
    {
        private readonly Dictionary<(string, int, bool), BattleAbilityStageProfile> _abilityStages = new();

        public BattleAbilityStageProfile AbilityStage(string actor, BattleAbilityDefinition ability)
        {
            if (ability == null || !_participants.TryGetValue(actor, out BattleParticipant participant) ||
                !ability.IsItem && ability.SkillTypeId == 4)
                throw new ArgumentException("An ordinary ability stage needs its captured actor and original ability.");
            var key = (actor, ability.Id, ability.IsItem);
            if (_abilityStages.TryGetValue(key, out BattleAbilityStageProfile captured))
                return captured;
            if (!_linkers.SkillIdToEffectGroupMap.TryGetValue(ability.Id, out EffectGroupInfo[] groups) || groups.Length != 2)
                throw new NotSupportedException($"Ability {ability.Id} has no original two-group effect linker.");
            bool main = _actors[actor].Main;
            BattleAbilityStageKind kind = ability.IsItem ? BattleAbilityStageKind.Item : ability.IsMagic || !main ? BattleAbilityStageKind.Magic : BattleAbilityStageKind.Skill;
            bool npcItemIdle = BattleData.Variant == "PAL3A" && !main && ability.IsItem;
            bool noBody = npcItemIdle || ability.IsTreasure || BattleData.Variant == "PAL3A" && kind == BattleAbilityStageKind.Skill && ability.Id is 4102 or 4103 or 4108 or 4507;
            int bodyTicks = 0;
            bool retainedBody = false;
            if (!noBody)
            {
                if (_data.TryActionPath(participant, AbilityClip(participant, ability), out string path))
                {
                    BattleAnimationMetadata body = _data.GetAnimationMetadata(path);
                    bodyTicks = ToTicks(body.TotalGameBoxTicks / (double)UnityPrimitivesConvertor.GameBoxTicksPerSecond / ActivePlaybackRate);
                }
                else
                    retainedBody = true;
            }
            BattleMovementKind? approach = null, returning = null;
            if (kind == BattleAbilityStageKind.Skill && main)
            {
#if PAL3
                approach = ability.Id switch
                {
                    4101 or 4304 => BattleMovementKind.Teleport,
                    4205 => BattleMovementKind.Fly,
                    4505 => BattleMovementKind.Run,
                    _ => null
                };
                returning = Priority(actor) switch
                {
                    1 or 3 => BattleMovementKind.Fly,
                    2 => BattleMovementKind.Teleport,
                    _ => BattleMovementKind.Run
                };

#else
                approach = ability.Id switch
                {
                    4101 => BattleMovementKind.Teleport,
                    4504 or 4505 => BattleMovementKind.Run,
                    4506 => BattleMovementKind.Fly,
                    _ => null
                };
                returning = Priority(actor) switch
                {
                    0 => ability.Id is 4101 or 4107 or 4108 ? BattleMovementKind.Run : null,
                    4 => ability.Id is 4504 or 4505 or 4506 ? BattleMovementKind.Run : BattleMovementKind.Teleport,
                    _ => BattleMovementKind.Run
                };

#endif
            }
            BattleAbilityWorkBoundary work = ability.IsTreasure ? BattleAbilityWorkBoundary.WorkTimeAndFirstGroupFinished :
                kind == BattleAbilityStageKind.Skill && ability.Id == 4101 ? BattleAbilityWorkBoundary.WorkTimeAndBodyIdle : BattleAbilityWorkBoundary.WorkTime;
            bool opposing = kind switch
            {
                BattleAbilityStageKind.Item => ability.ItemType == 2,
                BattleAbilityStageKind.Magic => ability.SkillTypeId == 2,
                _ => (int)ability.Range > 2
            };
            var profile = new BattleAbilityStageProfile(kind, CaptureAbilityBinding(groups[0]), CaptureAbilityBinding(groups[1]),
                groups[0].WorkTimeInSeconds, work, bodyTicks, opposing, approach, returning, retainedBody, npcItemIdle);
            _abilityStages.Add(key, profile);
            return profile;
        }

        private static BattleAbilityEffectBinding CaptureAbilityBinding(EffectGroupInfo group) => new(group.EffGroupId, group.WaitDurationInSeconds,
            (group.IsCenteredAroundCaster ? 1 : 0) | (group.IsCenteredAroundReceiver ? 2 : 0) | (group.IsCasterActor ? 4 : 0) | (group.IsReceiverActor ? 8 : 0),
            new BattleMovementPoint(group.EffectGameBoxPosition.X, group.EffectGameBoxPosition.Y, group.EffectGameBoxPosition.Z));

        public IReadOnlyList<BattleMovementPlan> AbilityMovement(BattleActorSnapshot source, BattleActorSnapshot target,
            BattleAbilityDefinition ability, BattleDefinitions definitions, bool returning)
        {
            if (source == null || ability == null || definitions == null)
                throw new ArgumentNullException(source == null ? nameof(source) : ability == null ? nameof(ability) : nameof(definitions));
            BattleAbilityStageProfile profile = AbilityStage(source.Id, ability);
            BattleMovementKind kind = (returning ? profile.ReturnKind : profile.ApproachKind) ?? throw new InvalidOperationException("This original ability has no corresponding movement task.");
            BattleMovementPoint destination = source.Home;
            if (!returning)
            {
                if (target == null)
                    throw new InvalidOperationException("This original ability approaches one explicit target.");
                float radius = AbilityRadius(source, definitions) + AbilityRadius(target, definitions);
#if PAL3A
                BattlePhysicalStageProfile sourceProfile = Physical(source.Id);
                int identity = sourceProfile.Identity;
                int weapon = sourceProfile.WeaponType;
                float extra = identity switch
                {
                    0 => 40,
                    1 => 50,
                    2 => 40,
                    3 => 32,
                    _ => 0
                };
                bool extended = identity switch
                {
                    0 => weapon == 1,
                    1 => weapon == 3,
                    2 => weapon == 6,
                    3 => weapon == 7,
                    _ => false
                };
                if (extended)
                    radius += extra;
                if (_participants[target.Id].ActorId == 706)
                    radius -= extra;

#endif
                if (_participants[source.Id].ActorId is 7364 or 7381)
                    radius *= 2;
                float x = source.Position.X - target.Home.X, y = source.Position.Y - target.Home.Y, z = source.Position.Z - target.Home.Z;
                double length = Math.Sqrt((double)x * x + (double)y * y + (double)z * z);
                BattleMovementPoint normal = length < (double)0.005f ? new BattleMovementPoint(0, 1, 0) :
                    new BattleMovementPoint((float)(x / length), (float)(y / length), (float)(z / length));
                destination = new BattleMovementPoint(target.Home.X + normal.X * radius, target.Home.Y + normal.Y * radius, target.Home.Z + normal.Z * radius);
            }
            if (kind != BattleMovementKind.Fly)
                return Array.AsReadOnly(new[] { new BattleMovementPlan(kind, source.Position, destination) });
            string moving = AbilityMovementAction(source.Id, ability, kind, returning);
            BattleAnimationMetadata model = _data.GetAnimationMetadata(_data.ActionPath(_participants[source.Id], moving));
            int? Marker(BattlePhysicalMarkerKind markerKind)
            {
                var matches = model.AnimationEvents.Where(marker => PhysicalMarkerKind(marker.Name) == markerKind).ToArray();
                if (matches.Length > 1)
                    throw new NotSupportedException($"Animation {moving} has multiple {markerKind} markers.");
                return matches.Length == 0 ? null : Math.Max(0, checked((int)Math.Ceiling(matches[0].GameBoxTick /
                    (double)UnityPrimitivesConvertor.GameBoxTicksPerSecond / ActivePlaybackRate / BattleSession.StepSeconds)));
            }
            int? up = Marker(BattlePhysicalMarkerKind.FlyUp);
            if (!up.HasValue)
                throw new NotSupportedException($"Animation {moving} has no takeoff marker.");
            int? down = Marker(BattlePhysicalMarkerKind.FlyDown);
            int over = Marker(BattlePhysicalMarkerKind.Over) ?? ToTicks(model.TotalGameBoxTicks / (double)UnityPrimitivesConvertor.GameBoxTicksPerSecond / ActivePlaybackRate);
            return Array.AsReadOnly(new[] { new BattleMovementPlan(kind, source.Position, destination, up, down, over) });
        }

        private float AbilityRadius(BattleActorSnapshot actor, BattleDefinitions definitions)
        {
            int identity = PhysicalIdentity(_participants[actor.Id]);
            float adjustment = identity == (BattleData.Variant == "PAL3" ? 1 : 3) ? 10f : 0f;
            if (actor.NativeAnimationHalfDepth.HasValue)
                return actor.NativeAnimationHalfDepth.Value - adjustment;
            string clip = Clip(actor, definitions) ?? _data.IdleAction(_participants[actor.Id]);
            BattleAnimationMetadata model = _data.GetAnimationMetadata(_data.ActionPath(_participants[actor.Id], clip));
            if (!model.HasBounds)
                throw new InvalidOperationException("An original combat radius requires the current MV3 collision bounds.");
            return (model.MaximumZ - model.MinimumZ) * 0.5f - adjustment;
        }

        public string EffectActor(uint modelCrc)
        {
            var hash = new Core.DataReader.Cpk.Crc32Hash();
            hash.Init();
            return _participants.Values.OrderBy(actor => actor.Side == BattleSide.Enemy ? 5 + (int)actor.Slot : Priority(actor.InstanceId))
                .FirstOrDefault(actor => (hash.Compute(_data.Model(actor), _data.Resources.Codepage) == modelCrc ||
                    hash.Compute(_data.Model(actor).ToLowerInvariant(), _data.Resources.Codepage) == modelCrc))?.InstanceId;
        }

        public int? AbilityNativeAction(string actor, BattleAbilityDefinition ability)
        {
            if (ability.SkillTypeId == 4)
                return 40;
            BattleAbilityStageProfile profile = AbilityStage(actor, ability);
            if (profile.NpcItemIdle)
                return null;
            if (!profile.RetainsBody && profile.BodyAnimationTicks == 0)
                return null;
            bool main = _actors[actor].Main;
            if (!main && !ability.IsItem)
            {
                uint[] skills = _data.Database.CombatActorInfos[_participants[actor].ActorId].SkillIds;
                return skills.Length > 1 && skills[1] == ability.Id ? 56 : skills.Length > 2 && skills[2] == ability.Id ? 57 : 55;
            }
            if (ability.IsItem)
                return AbilityClip(_participants[actor], ability) == ActorConstants.ActionToNameMap[ActorActionType.UseItemRecover] ? 38 : 39;
            if (ability.IsMagic)
                return 35;
            return ability.Id == (BattleData.Variant == "PAL3" ? 4101 : 4106) ? 37 : 36;
        }

        public BattlePhysicalAnimation AbilityBody(string actor, BattleAbilityDefinition ability)
        {
            if (ability.SkillTypeId == 4)
                return CompilePhysicalAnimation(_participants[actor], ActorConstants.ActionToNameMap[ActorActionType.WeaponSkill]);
            BattleAbilityStageProfile profile = AbilityStage(actor, ability);
            if (profile.NpcItemIdle)
            {
                string idle = ActorConstants.ActionToNameMap[ActorActionType.PreAttack];
                return _data.TryActionPath(_participants[actor], idle, out _) ? CompilePhysicalAnimation(_participants[actor], idle) : null;
            }
            if (!profile.RetainsBody && profile.BodyAnimationTicks == 0)
                return null;
            string clip = AbilityClip(_participants[actor], ability);
            return _data.TryActionPath(_participants[actor], clip, out _) ? CompilePhysicalAnimation(_participants[actor], clip) : null;
        }

        public BattlePhysicalAnimation AbilityMovementBody(string actor, BattleAbilityDefinition ability, BattleMovementKind kind, bool returning)
        {
            string clip = AbilityMovementAction(actor, ability, kind, returning);
            return CompilePhysicalAnimation(_participants[actor], clip);
        }

        private string AbilityMovementAction(string actor, BattleAbilityDefinition ability, BattleMovementKind kind, bool returning) =>
            kind == BattleMovementKind.Teleport ? _data.IdleAction(_participants[actor]) :
                BattleData.Variant == "PAL3A" && !returning && ability.Id == 4506 ? "z22" : _actors[actor].Clips[BattlePose.Moving];

        public BattlePhysicalAnimation ExternalBody(BattleActorSnapshot actor, string requested, BattleDefinitions definitions) =>
            _data.TryActionPath(_participants[actor.Id], requested, out _) ? CompilePhysicalAnimation(_participants[actor.Id], requested) : null;

        public string AbilityMovementClip(BattleActorSnapshot actor) =>
            BattleData.Variant == "PAL3A" && actor.AbilityId == 4506 && actor.Movement?.Plan.Kind == BattleMovementKind.Fly ? "z22" : null;
    }
}
