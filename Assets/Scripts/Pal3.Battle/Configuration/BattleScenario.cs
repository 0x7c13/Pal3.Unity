// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public enum BattleSide
    {
        Ally,
        Enemy,
    }

    public enum BattleSlot
    {
        Water,
        Fire,
        Wind,
        Thunder,
        Earth,
        Center,
    }

    public enum BattleEncounter
    {
        Normal,
        AllyAmbush,
        EnemyAmbush,
    }

    public enum BattleAttributeSource
    {
        ExplicitEffective,
        BaseWithEquipment,
        LevelGrowth,
    }

    // Authoring contracts contain resource IDs and explicit values, never engine objects.
    // Level and loadout are inputs; generating their derived values belongs to a verified ruleset.
    [Serializable]
    public sealed class BattleScenario
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string Variant = "PAL3";
        public string DataFingerprint = "";
        public string Scene = "";
        public int SceneElement;
        public int FiveSpiritElement;
        public int FiveSpiritForce;
        public string EnemyAiIgnoredActor = "";
        public string Music = "";
        public int Seed = 1;
        public int RoundLimit = -1;
        public bool ScriptBattle;
        public bool MustFail;
        public bool GameOverOnDefeat = true;
        public int Money;
        public BattleEncounter Encounter;
        public bool ToonRendering = true;
        public bool MotionBlur = true;
        public BattlePal3AState Pal3A = new();
        public BattleCamera Camera = new();
        public List<BattleParticipant> Participants = new();
        public List<BattleItem> Items = new();
        public List<BattleItem> TreasureUses = new();

        public BattleScenario Copy()
        {
            var copy = (BattleScenario)MemberwiseClone();
            copy.Participants = Participants?.Select(actor => actor?.Copy()).ToList();
            copy.Items = Items?.Select(item => item?.Copy()).ToList();
            copy.TreasureUses = TreasureUses?.Select(item => item?.Copy()).ToList();
            copy.Camera = Camera?.Copy();
            copy.Pal3A = Pal3A?.Copy();
            return copy;
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (Version != CurrentVersion)
                errors.Add($"Unsupported scenario version {Version}; expected {CurrentVersion}.");
            if (Variant is not ("PAL3" or "PAL3A"))
                errors.Add("Select PAL3 or PAL3A.");
            if (string.IsNullOrWhiteSpace(Scene))
                errors.Add("Select a combat scene.");
            if (SceneElement < 0 || SceneElement > 5 || !Enum.IsDefined(typeof(BattleEncounter), Encounter))
                errors.Add("Invalid scene element or encounter type.");
            if (FiveSpiritElement < 0 || FiveSpiritElement > 5 || FiveSpiritForce < 0 || FiveSpiritForce > 2)
                errors.Add("Invalid original five-spirit field element or force.");
            if (Variant == "PAL3A")
            {
                try
                {
                    (Pal3A ?? throw new ArgumentException("PAL3A needs its original resource snapshot.")).Validate();
                }
                catch (ArgumentException exception)
                {
                    errors.Add(exception.Message);
                }
            }
            if (Money < 0 || RoundLimit < -1)
                errors.Add("Money must be nonnegative; round limit must be -1 or nonnegative.");
            if (Camera == null || !Camera.Position.IsFinite || !Camera.Target.IsFinite ||
                !float.IsFinite(Camera.FieldOfView) || Camera.FieldOfView < 1 || Camera.FieldOfView > 120)
                errors.Add("Camera needs finite positions and a field of view between 1 and 120.");
            else if (Camera.Override && Camera.Position.X == Camera.Target.X && Camera.Position.Y == Camera.Target.Y && Camera.Position.Z == Camera.Target.Z)
                errors.Add("Camera position and target must differ.");
            if (Participants == null || Items == null || TreasureUses == null)
            {
                errors.Add("Participant and item collections must be present.");
                return errors;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var slots = new HashSet<(BattleSide, BattleSlot)>();
            foreach (BattleParticipant actor in Participants)
            {
                if (actor == null)
                {
                    errors.Add("A participant is missing.");
                    continue;
                }
                string label = $"Actor {actor.ActorId} ({actor.InstanceId})";
                if (string.IsNullOrWhiteSpace(actor.InstanceId) || !ids.Add(actor.InstanceId))
                    errors.Add(label + ": instance ID must be present and unique.");
                if (actor.ActorId <= 0 || actor.Level < 1 || actor.Level > 100 || actor.Experience < 0 || actor.Love < 0 || actor.Love > 999)
                    errors.Add(label + ": invalid actor ID, level or experience.");
                if (!Enum.IsDefined(typeof(BattleAttributeSource), actor.AttributeSource))
                    errors.Add(label + ": invalid attribute source.");
                if (!Enum.IsDefined(typeof(BattleSide), actor.Side) || !Enum.IsDefined(typeof(BattleSlot), actor.Slot))
                    errors.Add(label + ": invalid side or slot.");
                else if (!slots.Add((actor.Side, actor.Slot)))
                    errors.Add(label + ": another participant occupies this slot.");
                if (actor.Attributes == null || actor.Attributes.Length != 12 || actor.Attributes.Any(value => value < 0))
                    errors.Add(label + ": provide 12 nonnegative explicit attributes.");
                if (actor.Vitals == null || actor.Vitals.Length != 3 || actor.Vitals.Any(value => value < 0) ||
                    actor.AttributeSource == BattleAttributeSource.ExplicitEffective && actor.Attributes is { Length: 12 } && actor.Vitals is { Length: 3 } &&
                    Enumerable.Range(0, 3).Any(index => actor.Vitals[index] > actor.Attributes[index]))
                    errors.Add(label + ": current vitals must be between zero and their explicit maxima.");
                var skillIds = new HashSet<int>();
                var stateIds = new HashSet<int>();
                foreach (BattleStatus state in actor.Statuses ?? new List<BattleStatus>())
                {
                    if (state == null)
                    {
                        errors.Add(label + ": a configured initial status is missing.");
                        continue;
                    }
                    if (!stateIds.Add(state.Id))
                        errors.Add(label + ": initial status IDs must be unique.");
                    if (!BattleStatusRules.SupportsInitialState((BattleState)state.Id))
                        errors.Add(label + $": status {state.Id} is not supported as an initial status.");
                }
                var magicSlots = new HashSet<(int Group, int Slot)>();
                foreach (BattleSkill skill in actor.Skills ?? new List<BattleSkill>())
                {
                    if (skill == null)
                    {
                        errors.Add(label + ": a configured initial skill is missing.");
                        continue;
                    }
                    if (!skillIds.Add(skill.Id))
                        errors.Add(label + ": skill IDs must be unique.");
                    bool specified = skill.NativeMagicGroup != -1 || skill.NativeMagicSlot != -1;
                    if (specified && (skill.NativeMagicGroup < 0 || skill.NativeMagicGroup >= 6 || skill.NativeMagicSlot < 0 ||
                        skill.NativeMagicSlot >= (Variant == "PAL3A" ? 9 : 8)))
                        errors.Add(label + ": native magic group and slot must both be specified within the original six-group layout.");
                    else if (specified && !magicSlots.Add((skill.NativeMagicGroup, skill.NativeMagicSlot)))
                        errors.Add(label + ": multiple skills occupy the same native magic slot.");
                }
                if (actor.MagicSword != null)
                {
                    if (Variant != "PAL3" || actor.Equipment is not { Length: > 0 } || actor.Equipment[0] != BattleMagicSwordRules.WeaponId)
                        errors.Add(label + ": a magic sword snapshot requires PAL3 weapon 6603.");
                    try
                    {
                        _ = actor.MagicSword.Capture();
                    }
                    catch (ArgumentException)
                    {
                        errors.Add(label + ": invalid magic sword cultivation or energy snapshot.");
                    }
                }
                else if (actor.Equipment is { Length: > 0 } && actor.Equipment[0] == BattleMagicSwordRules.WeaponId &&
                    !(BattleLoadoutRules.IsNativeDefaultWeapon(Variant, actor) && BattleLoadoutRules.PlayerIdentity(Variant, actor.ActorId) != 0))
                    errors.Add(label + ": weapon 6603 requires its captured cultivation and energy.");
                if (actor.UsesNativeDefaultWeapon && !BattleLoadoutRules.IsNativeDefaultWeapon(Variant, actor))
                    errors.Add(label + ": the native default weapon provenance does not match this actor or item.");
                if (!actor.Position.IsFinite)
                    errors.Add(label + ": position must be finite.");
                int maximumSkillUses = Variant == "PAL3A" ? ushort.MaxValue : byte.MaxValue;
                if (actor.Skills?.Any(skill => skill != null && skill.Uses > maximumSkillUses) == true)
                    errors.Add(label + $": initial skill use counts must not exceed {maximumSkillUses} for {Variant}.");
                if (actor.Skills == null || actor.Statuses == null || actor.Equipment == null ||
                    actor.Equipment.Any(id => id < 0) ||
                    actor.Skills?.Any(skill => skill == null || skill.Id <= 0 || skill.Level < 1 || skill.Level > 10 || skill.Uses < 0) == true ||
                    actor.Statuses?.Any(state => state == null || state.Id < 0 || state.Rounds < 1) == true)
                    errors.Add(label + ": invalid skills, statuses or equipment.");
            }
            if (!string.IsNullOrEmpty(EnemyAiIgnoredActor) && !Participants.Any(actor => actor != null && actor.InstanceId == EnemyAiIgnoredActor && actor.Side == BattleSide.Ally))
                errors.Add("The actor ignored by enemy AI must be an allied participant.");
            foreach (BattleSide side in Enum.GetValues(typeof(BattleSide)))
            {
                int count = Participants.Count(actor => actor != null && actor.Side == side);
                if (count == 0 || count > (side == BattleSide.Ally ? 4 : 6))
                    errors.Add(side + ": configure 1–" + (side == BattleSide.Ally ? 4 : 6) + " participants.");
            }
            if (Items.Any(item => item == null || item.Id <= 0 || item.Count < 1) ||
                Items.Where(item => item != null).GroupBy(item => item.Id).Any(group => group.Count() > 1))
                errors.Add("Items need unique positive IDs and positive quantities.");
            if (TreasureUses.Any(item => item == null || Variant is not ("PAL3" or "PAL3A") || !BattleSpecialRules.IsTreasure(Variant, item.Id) || item.Count < 0 || item.Count > byte.MaxValue) ||
                TreasureUses.Where(item => item != null).GroupBy(item => item.Id).Any(group => group.Count() > 1))
                errors.Add("Treasure use counters need unique valid treasure IDs and quantities between 0 and 255.");
            return errors;
        }
    }

    [Serializable]
    public sealed class BattleCamera
    {
        public bool Override;
        public BattlePoint Position;
        public BattlePoint Target;
        public float FieldOfView = 39;

        public BattleCamera Copy() => (BattleCamera)MemberwiseClone();
    }

    [Serializable]
    public struct BattlePoint
    {
        public float X;
        public float Y;
        public float Z;

        public bool IsFinite => !float.IsNaN(X) && !float.IsInfinity(X) &&
            !float.IsNaN(Y) && !float.IsInfinity(Y) && !float.IsNaN(Z) && !float.IsInfinity(Z);
    }

    [Serializable]
    public sealed class BattleParticipant
    {
        // Allocate once for a new participant. Copies keep this ID, even after a
        // slot swap, so inputs and replays can keep addressing the same actor.
        public string InstanceId = Guid.NewGuid().ToString("N");
        public int ActorId;
        public BattleSide Side;
        public BattleSlot Slot;
        public string Model = "";
        public int Level = 1;
        public int Experience;
        public int Love;
        public BattleAttributeSource AttributeSource;
        public int[] Attributes = new int[12];
        public int[] Vitals = new int[3];
        public int[] Equipment = new int[6];
        public bool UsesNativeDefaultWeapon;
        public BattleMagicSword MagicSword;
        public List<BattleSkill> Skills = new();
        public List<BattleStatus> Statuses = new();
        public bool OverridePosition;
        public BattlePoint Position;

        public BattleParticipant Copy()
        {
            var copy = (BattleParticipant)MemberwiseClone();
            copy.Attributes = Attributes?.ToArray();
            copy.Vitals = Vitals?.ToArray();
            copy.Equipment = Equipment?.ToArray();
            copy.MagicSword = MagicSword?.Copy();
            copy.Skills = Skills?.Select(skill => skill?.Copy()).ToList();
            copy.Statuses = Statuses?.Select(state => state?.Copy()).ToList();
            return copy;
        }
    }

    // Per-instance authoring input. The released game has one sword; test actors never share its mutable gauge.
    [Serializable]
    public sealed class BattleMagicSword
    {
        public int AttackBonus = 35;
        public BattleSwordPurificationElement SelectedElement = BattleSwordPurificationElement.Wind;
        public int PurificationCount;
        public int SkillId = 2224;
        public uint ChargeCoefficient = 10;
        public float Energy;

        public BattleMagicSwordSnapshot Capture()
        {
            if (SkillId != 0 && !BattleMagicSwordRules.IsSupportedSkill(SkillId))
                throw new ArgumentOutOfRangeException(nameof(SkillId));
            return new BattleMagicSwordSnapshot(AttackBonus, SelectedElement, PurificationCount, SkillId, ChargeCoefficient, Energy);
        }

        public BattleMagicSword Copy() => (BattleMagicSword)MemberwiseClone();
    }

    [Serializable]
    public sealed class BattleSkill
    {
        public int Id;
        public int Level = 1;
        public int Uses;
        public bool Mastered;
        public int NativeMagicGroup = -1;
        public int NativeMagicSlot = -1;

        public BattleSkill Copy() => (BattleSkill)MemberwiseClone();
    }

    [Serializable]
    public sealed class BattleStatus
    {
        public int Id;
        public int Rounds = 1;

        public BattleStatus Copy() => (BattleStatus)MemberwiseClone();
    }

    [Serializable]
    public sealed class BattleItem
    {
        public int Id;
        public int Count = 1;

        public BattleItem Copy() => (BattleItem)MemberwiseClone();
    }

    // Each start/reset gets its own deep copy. Nothing writable escapes the captured initial state.
    public sealed class BattleSetup
    {
        private readonly BattleScenario _initial;

        public BattleSetup(BattleScenario scenario)
        {
            if (scenario == null)
                throw new ArgumentNullException(nameof(scenario));
            List<string> errors = scenario.Validate();
            if (errors.Count > 0)
                throw new ArgumentException(string.Join("\n", errors), nameof(scenario));
            _initial = scenario.Copy();
        }

        public BattleScenario CreateInstance() => _initial.Copy();
    }
}
