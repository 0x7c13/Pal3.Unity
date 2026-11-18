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
    using UnityEngine;

    // Compiles resource metadata once. It owns no scene objects and never observes the mutable editor draft.
    public sealed partial class BattleAnimationCatalog : IBattleCatalog, IBattleAbilityTimings, IBattleStageGeometry, IBattleMagicSwordTimings, IBattleDodgeTimings
    {
#if PAL3
        private const double ActivePlaybackRate = 1.3f;
#else
        private const double ActivePlaybackRate = 1.0;
#endif

        private sealed class ActorDefinition
        {
            public bool Main;
            public int Priority, Sp, Divisor = 1;
            public Vector3 Home;
            public readonly Dictionary<BattlePose, string> Clips = new();
            public Dictionary<BattlePose, int> Durations = new();
        }

        private readonly Dictionary<string, ActorDefinition> _actors = new(StringComparer.Ordinal);
        private readonly Dictionary<(string Source, string Target), BattleAttackPlan> _initialAttacks = new();
        private readonly Dictionary<(string, int, bool), (string Clip, AttackTiming Timing)> _abilities = new();
        private readonly Dictionary<string, BattleParticipant> _participants;
        private readonly BattleData _data;
        private readonly int _sceneElement;
        private readonly EffectLinkerFile _linkers;

        public List<string> Diagnostics { get; } = new();

        public BattleAnimationCatalog(BattleData data, BattleSetup setup, BattleDefinitions definitions = null)
            : this(data, setup, definitions, data.Read("basedata.cpk\\cbdata\\efflinker.dat", new EffectLinkerFileReader()))
        {
        }

        private BattleAnimationCatalog(BattleData data, BattleSetup setup, BattleDefinitions definitions, EffectLinkerFile linkers)
        {
            _data = data;
            _linkers = linkers;
            BattleScenario scenario = setup.CreateInstance();
            _sceneElement = scenario.SceneElement;
            _participants = scenario.Participants.ToDictionary(actor => actor.InstanceId, StringComparer.Ordinal);
            foreach (BattleParticipant actor in scenario.Participants)
            {
                var info = data.Database.CombatActorInfos[actor.ActorId];
                bool main = info.Type == CombatActorType.MainActor;
                if (data.CombatRosterDenial(actor.ActorId, actor.Side) is string rosterDenial)
                    throw new NotSupportedException(rosterDenial);
                int divisor = 1;
                if (main && (actor.Equipment.Length == 0 || actor.Equipment[0] == 0))
                    throw new NotSupportedException($"Actor {actor.ActorId}: choose a compatible basic weapon before starting combat.");
                if (main && actor.Equipment.Length > 0 && actor.Equipment[0] != 0)
                {
                    var weapon = data.Database.GameItemInfos[actor.Equipment[0]];
                    PlayerActorId identity = (PlayerActorId)PhysicalIdentity(actor);
                    if (weapon.Type != ItemType.Weapon || !BattleLoadoutRules.IsNativeDefaultWeapon(BattleData.Variant, actor) && !weapon.ApplicableActors.Contains(identity))
                        throw new ArgumentException($"Actor {actor.ActorId}: slot 0 must contain a compatible weapon.");
                    divisor = Math.Max(1, weapon.AttackHitCount);
                }
                var definition = new ActorDefinition
                {
                    Main = main,
                    Home = data.Position(actor, _sceneElement),
                    Sp = Math.Max(0, info.SpImpactValue),
                    Divisor = divisor,
                    Priority = main ? PhysicalIdentity(actor) : (int)actor.Slot
                };
                _actors.Add(actor.InstanceId, definition);
                AddClip(BattlePose.Idle, data.IdleAction(actor));
                AddClip(BattlePose.Attacking, data.AttackAction(actor));
                AddClip(BattlePose.Moving, ActorConstants.ActionToNameMap[main ? ActorActionType.AttackMove : ActorActionType.NpcWalk]);
                AddClip(BattlePose.Hurt, ActorConstants.ActionToNameMap[main ? ActorActionType.BeAttack : ActorActionType.NpcBeAttacked]);
                if (main)
                {
                    AddClip(BattlePose.Dodging, ActorConstants.ActionToNameMap[ActorActionType.Dodge]);
                    AddClip(BattlePose.GuardEntry, ActorConstants.ActionToNameMap[ActorActionType.StartDefence]);
                    AddClip(BattlePose.Guard, ActorConstants.ActionToNameMap[ActorActionType.Defence]);
                    AddClip(BattlePose.Dead, ActorConstants.ActionToNameMap[ActorActionType.Dead]);
                    AddClip(BattlePose.Fleeing, ActorConstants.ActionToNameMap[ActorActionType.Flee]);
                }
                else
                {
                    definition.Clips[BattlePose.Dodging] = definition.Clips[BattlePose.Idle];
                    definition.Durations[BattlePose.Dodging] = definition.Durations[BattlePose.Idle];
                    definition.Clips[BattlePose.GuardEntry] = definition.Clips[BattlePose.Idle];
                    definition.Durations[BattlePose.GuardEntry] = 1;
                    definition.Clips[BattlePose.Guard] = definition.Clips[BattlePose.Idle];
                    definition.Durations[BattlePose.Guard] = definition.Durations[BattlePose.Idle];
                }

                void AddClip(BattlePose pose, string name)
                {
                    definition.Clips[pose] = name;
                    if (!data.TryActionPath(actor, name, out string path))
                    {
                        if (pose == BattlePose.Idle)
                            throw new InvalidOperationException($"Actor {actor.ActorId}: missing initial animation {name}.");
                        return;
                    }
                    BattleAnimationMetadata clip = data.GetAnimationMetadata(path);
                    double rate = pose == BattlePose.Idle ? 0.65f : ActivePlaybackRate;
                    definition.Durations[pose] = ToTicks(clip.TotalGameBoxTicks / (double)UnityPrimitivesConvertor.GameBoxTicksPerSecond / rate);
                }
            }
            foreach (BattleParticipant source in scenario.Participants)
                foreach (BattleParticipant target in scenario.Participants.Where(target => target.InstanceId != source.InstanceId))
                    InitialAttack(source.InstanceId, target.InstanceId);
            if (definitions != null)
                foreach (BattleParticipant actor in scenario.Participants)
                    foreach (BattleAbilityDefinition ability in actor.Skills.Select(skill => definitions.Skills.GetValueOrDefault(skill.Id))
                        .Concat(actor.MagicSword == null ? Array.Empty<BattleAbilityDefinition>() : new[] { definitions.Skills.GetValueOrDefault(actor.MagicSword.SkillId) })
                        .Concat(scenario.Items.Select(item => definitions.Items.GetValueOrDefault(item.Id))).Where(ability => ability != null))
                        try
                        {
                            Ability(actor.InstanceId, ability);
                        }
                        catch (Exception ex)
                        {
                            Diagnostics.Add($"Actor {actor.ActorId}, ability {ability.Id}: {ex.Message}");
                        }
        }

        public static Vector3 UnityPosition(BattleMovementPoint point) =>
            new(-point.X / UnityPrimitivesConvertor.GameBoxUnitToUnityUnit,
                point.Y / UnityPrimitivesConvertor.GameBoxUnitToUnityUnit,
                point.Z / UnityPrimitivesConvertor.GameBoxUnitToUnityUnit);

        private static BattleMovementPoint NativePosition(Vector3 point) =>
            new(-point.x * UnityPrimitivesConvertor.GameBoxUnitToUnityUnit,
                point.y * UnityPrimitivesConvertor.GameBoxUnitToUnityUnit,
                point.z * UnityPrimitivesConvertor.GameBoxUnitToUnityUnit);

        public BattleMovementPoint InitialHome(string actor) => NativePosition(_actors[actor].Home);

        public BattleMovementPoint FormationHome(string actor, BattleSlot slot) =>
            NativePosition(_data.FormationPosition(_participants[actor].Side, slot, _sceneElement));

        public BattleAttackPlan PlanAttack(string source, string target, BattleMovementPoint sourceHome, BattleMovementPoint targetHome)
        {
            return CompilePhysicalAttackPlan(source, target, sourceHome, targetHome);
        }

        public IReadOnlyList<BattleMovementPlan> PlanFormationMove(string actor, BattleMovementPoint currentHome,
            BattleMovementPoint destination, bool passThroughMidpoint)
        {
            BattleParticipant participant = _participants[actor];
            int identity = Priority(actor);
            BattleMovementKind kind = identity == 2 ? BattleMovementKind.Teleport : BattleMovementKind.Run;
#if PAL3
            if (identity is 1 or 3)
                kind = BattleMovementKind.Fly;
#endif
            if (passThroughMidpoint)
            {
                if (BattleData.Variant != "PAL3" || identity is not (0 or 4))
                    throw new ArgumentException("The original crossing route is limited to PAL3 identities 0 and 4.");
                float x = currentHome.X - destination.X;
                float z = currentHome.Z - destination.Z;
                float length = MathF.Sqrt(x * x + z * z);
                var normal = length < 0.005f ? new BattleMovementPoint(0, 1, 0) : new BattleMovementPoint(z / length, 0, -x / length);
                var midpoint = new BattleMovementPoint((currentHome.X + destination.X) / 2 + normal.X * 30,
                    (currentHome.Y + destination.Y) / 2 + normal.Y * 30, (currentHome.Z + destination.Z) / 2 + normal.Z * 30);
                return Array.AsReadOnly(new[]
                {
                    new BattleMovementPlan(BattleMovementKind.Run, currentHome, midpoint),
                    new BattleMovementPlan(BattleMovementKind.Run, midpoint, destination)
                });
            }
            if (kind != BattleMovementKind.Fly)
            {
                if (kind == BattleMovementKind.Teleport)
                    _data.ActionPath(participant, ActorConstants.ActionToNameMap[ActorActionType.PreAttack]);
                return Array.AsReadOnly(new[] { new BattleMovementPlan(kind, currentHome, destination) });
            }
            BattleAnimationMetadata clip = _data.GetAnimationMetadata(_data.ActionPath(participant, _actors[actor].Clips[BattlePose.Moving]));
            int? Marker(string name)
            {
                var markers = clip.AnimationEvents.Where(marker => marker.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (markers.Length == 0)
                    return null;
                if (markers.Length != 1)
                    throw new NotSupportedException("Formation movement needs one native " + name + " marker.");
                return Math.Max(0, checked((int)Math.Ceiling(markers[0].GameBoxTick /
                    (double)UnityPrimitivesConvertor.GameBoxTicksPerSecond / ActivePlaybackRate / BattleSession.StepSeconds)));
            }
            int? up = Marker("flyup"), down = Marker("flydown");
            int over = Marker("over") ?? ToTicks(clip.TotalGameBoxTicks / (double)UnityPrimitivesConvertor.GameBoxTicksPerSecond / ActivePlaybackRate);
            if (!up.HasValue)
                throw new NotSupportedException("PAL3 flying formation movement has no verified flyup marker.");
            return Array.AsReadOnly(new[] { new BattleMovementPlan(kind, currentHome, destination,
                flyUpTick: up, flyDownTick: down, overTick: over) });
        }

        public AttackTiming Attack(string source, string target) => InitialAttack(source, target).Timing;

        private BattleAttackPlan InitialAttack(string source, string target)
        {
            if (!_initialAttacks.TryGetValue((source, target), out BattleAttackPlan plan))
            {
                plan = PlanAttack(source, target, InitialHome(source), InitialHome(target));
                _initialAttacks.Add((source, target), plan);
            }
            return plan;
        }

        private void InvalidateInitialAttacks(string actor)
        {
            foreach (var key in _initialAttacks.Keys.Where(key => key.Source == actor || key.Target == actor).ToArray())
                _initialAttacks.Remove(key);
        }

        public BattleMagicSwordStageProfile MagicSword(string actor, BattleAbilityDefinition ability)
        {
            if (BattleData.Variant != "PAL3" || ability.IsItem || ability.SkillTypeId != 4 ||
                !BattleMagicSwordRules.IsSupportedSkill(ability.Id) || _participants[actor].MagicSword?.SkillId != ability.Id ||
                _participants[actor].Equipment[0] != BattleMagicSwordRules.WeaponId)
                throw new NotSupportedException("A magic sword stage requires the captured PAL3 weapon skill.");
            if (!_linkers.SkillIdToEffectGroupMap.TryGetValue(ability.Id, out EffectGroupInfo[] groups) || groups.Length != 2)
                throw new NotSupportedException("The weapon skill has no verified original effect linker.");
            if (groups.Any(group => group.IsCenteredAroundCaster || group.IsCenteredAroundReceiver || group.IsCasterActor || group.IsReceiverActor))
                throw new NotSupportedException("The original weapon skill uses an unverified effect placement mode.");
            string clip = ActorConstants.ActionToNameMap[ActorActionType.WeaponSkill];
            BattleAnimationMetadata model = _data.GetAnimationMetadata(_data.ActionPath(_participants[actor], clip));
            int body = ToTicks(model.TotalGameBoxTicks / (double)UnityPrimitivesConvertor.GameBoxTicksPerSecond / ActivePlaybackRate);
            return new BattleMagicSwordStageProfile(groups[0].EffGroupId, groups[1].EffGroupId,
                groups[0].WaitDurationInSeconds, groups[1].WaitDurationInSeconds, body,
                new BattleMovementPoint(groups[0].EffectGameBoxPosition.X, groups[0].EffectGameBoxPosition.Y, groups[0].EffectGameBoxPosition.Z),
                new BattleMovementPoint(groups[1].EffectGameBoxPosition.X, groups[1].EffectGameBoxPosition.Y, groups[1].EffectGameBoxPosition.Z));
        }

        public AttackTiming Ability(string actor, BattleAbilityDefinition ability)
        {
            var key = (actor, ability.Id, ability.IsItem);
            if (_abilities.TryGetValue(key, out var cached))
                return cached.Timing;
            BattleParticipant participant = _participants[actor];
            if (!ability.IsItem && ability.SkillTypeId == 4)
            {
                BattleMagicSwordStageProfile profile = MagicSword(actor, ability);
                // Only the body duration is used. The dedicated stage waits for group 1 completion.
                var body = new AttackTiming(0, profile.BodyAnimationTicks, new[] { 0 }, bodyAnimationTicks: profile.BodyAnimationTicks);
                _abilities.Add(key, (ActorConstants.ActionToNameMap[ActorActionType.WeaponSkill], body));
                return body;
            }
            BattleAbilityStageProfile ordinary = AbilityStage(actor, ability);
            string clip = ordinary.RetainsBody ? null : ordinary.BodyAnimationTicks == 0 ? _data.IdleAction(participant) : AbilityClip(participant, ability);
            int preparation = ordinary.Kind == BattleAbilityStageKind.Item ? 1 : ordinary.Kind == BattleAbilityStageKind.Magic ? 2 : 3;
            int hit = Math.Max(0, checked((int)Math.Ceiling(ordinary.WorkSeconds / BattleSession.StepSeconds)));
            int end = Math.Max(1, Math.Max(hit, ordinary.BodyAnimationTicks));
            var timing = new AttackTiming(0, end, new[] { hit }, preparationTicks: preparation,
                bodyAnimationTicks: ordinary.BodyAnimationTicks, nativeWorkTimeSeconds: ordinary.WorkSeconds);
            _abilities.Add(key, (clip, timing));
            return timing;
        }

        private string AbilityClip(BattleParticipant actor, BattleAbilityDefinition ability)
        {
            bool main = _actors[actor.InstanceId].Main;
#if PAL3A
            if (main && !ability.IsItem && !ability.IsMagic && ability.Id == 4506)
                return "z171";
#endif
            ActorActionType action;
            if (!main && !ability.IsItem)
            {
                uint[] skills = _data.Database.CombatActorInfos[actor.ActorId].SkillIds;
                action = skills.Length > 1 && skills[1] == ability.Id ? ActorActionType.NpcMagic2
                    : skills.Length > 2 && skills[2] == ability.Id ? ActorActionType.NpcMagic3 : ActorActionType.NpcMagic1;
            }
            else if (ability.IsItem)
            {
                bool Recover(int attribute) => ability.ItemType == 1 && ability.Attributes.Any(effect => effect.Attribute == attribute &&
                    (effect.Mode == BattleValueMode.RecoverToMax || effect.Mode is BattleValueMode.Absolute or BattleValueMode.Percentage && effect.Value > 0));
                bool recover = Recover(0);
#if PAL3A
                recover |= Recover(2) || ability.ItemType == 1 && ability.States.Any(state => state.State is not (BattleState.Dying or BattleState.Death));
#endif
                action = recover ? ActorActionType.UseItemRecover : ActorActionType.UseItemAttack;
            }
            else
            {
#if PAL3
                bool second = ability.Id == 4101;
#else
                bool second = ability.Id == 4106;
#endif
                action = ability.IsMagic ? ActorActionType.Magic : second ? ActorActionType.Skill2 : ActorActionType.Skill1;
            }
            return ActorConstants.ActionToNameMap[action];
        }

        public string Clip(BattleActorSnapshot actor, BattleDefinitions definitions)
        {
            if (AbilityMovementClip(actor) is { } movingClip)
                return movingClip;
            if (actor.Pose == BattlePose.Repositioning && actor.Movement?.Finished == false)
                return actor.Movement.Plan.Kind == BattleMovementKind.Teleport
                    ? ActorConstants.ActionToNameMap[ActorActionType.PreAttack] : _actors[actor.Id].Clips[BattlePose.Moving];
            if (actor.Pose == BattlePose.Casting)
            {
                return ActorConstants.ActionToNameMap[_actors[actor.Id].Main ? ActorActionType.MagicWait : ActorActionType.NpcMagic1];
            }
            if (actor.Pose is BattlePose.UsingSkill or BattlePose.UsingItem)
            {
                var abilities = actor.CurrentAction == BattleAction.Item ? definitions.Items : definitions.Skills;
                BattleAbilityDefinition ability = abilities[actor.AbilityId];
                Ability(actor.Id, ability);
                return _abilities[(actor.Id, actor.AbilityId, ability.IsItem)].Clip;
            }
            return Clip(actor.Id, actor.Pose);
        }

        private static int ToTicks(double seconds) => Math.Max(1, checked((int)Math.Ceiling(seconds / BattleSession.StepSeconds)));

        public double PlaybackRate(BattleActorSnapshot actor) =>
            actor.Pose == BattlePose.Repositioning && actor.Movement?.Plan.Kind == BattleMovementKind.Teleport
                ? 0.65f : PlaybackRate(actor.Id, actor.Pose);

        public double PlaybackRate(string actor, BattlePose pose) => pose == BattlePose.Idle ||
            pose == BattlePose.Guard && !_actors[actor].Main ? 0.65f : ActivePlaybackRate;

        public static bool IsBasicEnemy(BattleData data, int actorId)
        {
            if (!data.Database.CombatActorInfos.TryGetValue(actorId, out var actor) || actor.Type == CombatActorType.MainActor || actorId == 7366 ||
                !data.Database.SkillInfos.TryGetValue((int)actor.NormalAttackModeId, out var mode))
                return false;
            return mode.TargetRangeType is TargetRangeType.FirstPartySingle or TargetRangeType.EnemyPartySingle &&
                mode.CombatStateImpactTypes.Count == 0 && mode.SpecialSkillId == 0;
        }

        public int GuardEntry(string actor) => _actors[actor].Durations.GetValueOrDefault(BattlePose.GuardEntry, 1);

        public int Dodge(string actor) => _actors[actor].Durations.GetValueOrDefault(BattlePose.Dodging, 1);

        public int Hurt(string actor) => _actors[actor].Durations.GetValueOrDefault(BattlePose.Hurt, 1);

        public int SpImpact(string actor) => _actors[actor].Sp;

        public int Priority(string actor) => _actors[actor].Priority;

        public int DamageDivisor(string actor) => _actors[actor].Divisor;

        public Vector3 Approach(string source, string target) => UnityPosition(InitialAttack(source, target).Approach);

        public string Clip(string actor, BattlePose pose) => _actors[actor].Clips.GetValueOrDefault(pose == BattlePose.Returning ? BattlePose.Moving : pose);

        public int Duration(string actor, BattlePose pose) => _actors[actor].Durations.GetValueOrDefault(pose);
    }
}
