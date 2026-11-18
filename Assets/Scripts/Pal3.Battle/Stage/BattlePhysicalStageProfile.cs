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

    public enum BattlePhysicalBodyKind
    {
        Run, Fly, FlyRotate, NpcFly, Teleport, Turn, TurnToPosition, TurnToRole,
        Attack, Protect, FastRun, ThrownWeapon, StartGuard, RestoreThrownWeapon, FleeWait, EnemyFlee
    }

    public enum BattlePhysicalMarkerKind
    {
        Work, Work1, Work2, Work3, Over, FlyUp, FlyDown, Start, Other, Catched, Throwed
    }

    public readonly struct BattlePhysicalMarker
    {
        public int Tick
        {
            get;
        }

        public BattlePhysicalMarkerKind Kind
        {
            get;
        }

        public uint GameBoxTick
        {
            get;
        }

        public string NativeName
        {
            get;
        }

        public BattlePhysicalMarker(int tick, BattlePhysicalMarkerKind kind, uint? gameBoxTick = null, string nativeName = null)
        {
            if (tick < 0 || !Enum.IsDefined(typeof(BattlePhysicalMarkerKind), kind))
                throw new ArgumentException("Invalid physical animation marker.");
            Tick = tick;
            Kind = kind;
            GameBoxTick = gameBoxTick ?? checked((uint)tick * 96);
            NativeName = nativeName;
        }
    }

    public sealed class BattlePhysicalAnimation
    {
        public string Clip
        {
            get;
        }

        public int DurationTicks
        {
            get;
        }

        public float HalfDepth
        {
            get;
        }

        public IReadOnlyList<BattlePhysicalMarker> Markers
        {
            get;
        }

        public uint GameBoxDuration
        {
            get;
        }

        public BattlePhysicalAnimation(string clip, int durationTicks, float halfDepth, IEnumerable<BattlePhysicalMarker> markers = null, uint? gameBoxDuration = null)
        {
            if (string.IsNullOrWhiteSpace(clip) || durationTicks < 1 || !float.IsFinite(halfDepth) || halfDepth < 0)
                throw new ArgumentException("Invalid original physical animation.");
            Clip = clip;
            DurationTicks = durationTicks;
            HalfDepth = halfDepth;
            GameBoxDuration = gameBoxDuration ?? checked((uint)durationTicks * 96);
            if (GameBoxDuration == 0)
                throw new ArgumentException("The original actor animation duration must be nonzero.");
            Markers = Array.AsReadOnly((markers ?? Array.Empty<BattlePhysicalMarker>()).OrderBy(marker => marker.GameBoxTick).ToArray());
        }
    }

    // The current clip's merged original AABB supplies the native OBB depth.
    // Actor identity, weapon/lian and animation metadata are captured together.
    public sealed class BattlePhysicalStageProfile
    {
        public string Variant
        {
            get;
        }

        public int ResourceId
        {
            get;
        }

        public int Identity
        {
            get;
        }

        public int WeaponType
        {
            get;
        }

        public int AttackCount
        {
            get;
        }

        public bool Transformed
        {
            get;
        }

        public BattlePhysicalAnimation ThrownStart
        {
            get;
        }

        public BattlePhysicalAnimation ThrownReturn
        {
            get;
        }

        public bool IsAlly => Identity >= 0;

        public IReadOnlyDictionary<BattlePose, BattlePhysicalAnimation> Animations
        {
            get;
        }

        public BattlePhysicalStageProfile(string variant, int resourceId, int identity, int weaponType, int attackCount,
            IReadOnlyDictionary<BattlePose, BattlePhysicalAnimation> animations, bool transformed = false, BattlePhysicalAnimation thrownStart = null, BattlePhysicalAnimation thrownReturn = null)
        {
            if (variant is not ("PAL3" or "PAL3A") || identity is < -1 or > 4 || weaponType < 0 || attackCount < 0 || animations == null ||
                !animations.TryGetValue(BattlePose.Idle, out BattlePhysicalAnimation idle) || idle == null)
                throw new ArgumentException("Incomplete original physical profile.");
            Variant = variant;
            ResourceId = resourceId;
            Identity = identity;
            WeaponType = weaponType;
            AttackCount = attackCount;
            Transformed = transformed;
            ThrownStart = thrownStart;
            ThrownReturn = thrownReturn;
            Animations = new ReadOnlyDictionary<BattlePose, BattlePhysicalAnimation>(animations.ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        public BattlePhysicalAnimation Animation(BattlePose pose) => Animations.GetValueOrDefault(pose);

        public int NativeAction(BattlePose pose) => pose switch
        {
            BattlePose.Idle => IsAlly ? 32 : 50,
            BattlePose.Moving or BattlePose.Returning or BattlePose.Repositioning => IsAlly ? 31 : 52,
            BattlePose.Attacking => !IsAlly ? 53 :
                (Variant == "PAL3" ? Identity == 2 && WeaponType == 7 || Identity == 3 && WeaponType == 4 || Identity == 4 && WeaponType == 2
                    : Identity == 1 && WeaponType == 4 || Identity == 2 && WeaponType == 6 || Identity == 4 && WeaponType == 9) ? 30 : 29,
            BattlePose.Hurt => IsAlly ? 33 : 54,
            BattlePose.GuardHurt => 43,
            BattlePose.Dodging => 44,
            BattlePose.GuardEntry => 41,
            BattlePose.Guard => 42,
            BattlePose.Dead => 47,
            BattlePose.Dying => 46,
            BattlePose.Sleeping => 45,
            BattlePose.Casting => IsAlly ? 34 : 55,
            BattlePose.Fleeing => 48,
            _ => throw new ArgumentException("This body pose requires its command's native action index.", nameof(pose))
        };

        public float Radius(BattlePose pose) => (Animation(pose) ?? Animations[BattlePose.Idle]).HalfDepth - (Identity == (Variant == "PAL3" ? 1 : 3) ? 10f : 0f);

        public bool UsesThrownWeapon => UsesThrownWeaponFor(Transformed);

        public bool UsesThrownWeaponFor(bool transformed) => Variant == "PAL3A" && (Identity == 0 && WeaponType == 2 && !transformed || Identity == 3 && WeaponType == 10);

        public int DamageCount(BattlePhysicalMarkerKind marker, bool? transformed = null)
        {
            if (UsesThrownWeaponFor(transformed ?? Transformed) || marker > (Variant == "PAL3A" ? BattlePhysicalMarkerKind.Work3 : BattlePhysicalMarkerKind.Work1))
                return 0;
            if (!IsAlly)
                return Variant == "PAL3A" || marker == BattlePhysicalMarkerKind.Work ? 1 : 0;
            int repeatedIdentity = Variant == "PAL3" ? 2 : 4;
            if (Identity != repeatedIdentity)
                return 1;
            if (WeaponType == 5 && marker == BattlePhysicalMarkerKind.Work)
                return 0;
            return Math.Max(1, AttackCount);
        }

        public bool TurnsAtAttack => IsAlly && Identity == (Variant == "PAL3" ? 2 : 4) && WeaponType != (Variant == "PAL3" ? 7 : 9);

        public IReadOnlyList<BattlePhysicalBodyKind> Approach(bool? transformed = null)
        {
            bool shape = transformed ?? Transformed;
            if (IsAlly && (WeaponType == 5 || Variant == "PAL3A" && WeaponType == 8) || Variant == "PAL3" && ResourceId == 7366)
                return Array.AsReadOnly(new[] { BattlePhysicalBodyKind.TurnToRole });
            if (Variant == "PAL3")
                return Identity switch
                {
                    1 => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.Fly }),
                    2 => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.Teleport, BattlePhysicalBodyKind.Run }),
                    3 => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.TurnToPosition, BattlePhysicalBodyKind.FlyRotate, BattlePhysicalBodyKind.Turn }),
                    -1 when ResourceId == 720 => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.Teleport }),
                    -1 when ResourceId is 738 or 739 => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.NpcFly }),
                    _ => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.Run })
                };
            if (UsesThrownWeaponFor(shape))
                return Array.AsReadOnly(new[] { BattlePhysicalBodyKind.TurnToPosition, BattlePhysicalBodyKind.ThrownWeapon });
            return Identity switch
            {
                0 when shape => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.Teleport }),
                2 => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.Teleport }),
                4 => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.Run, BattlePhysicalBodyKind.Run }),
                -1 when ResourceId == 719 => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.Fly, BattlePhysicalBodyKind.Run }),
                -1 when ResourceId == 713 => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.Teleport, BattlePhysicalBodyKind.Run }),
                -1 when ResourceId == 720 => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.Teleport }),
                -1 when ResourceId is 738 or 739 => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.NpcFly }),
                _ => Array.AsReadOnly(new[] { BattlePhysicalBodyKind.Run })
            };
        }

        public IReadOnlyList<BattlePhysicalBodyKind> Return(bool? transformed = null)
        {
            bool shape = transformed ?? Transformed;
            var result = new List<BattlePhysicalBodyKind>();
            if (Variant == "PAL3")
                result.Add(Identity switch
                {
                    1 => BattlePhysicalBodyKind.Fly,
                    2 => BattlePhysicalBodyKind.Teleport,
                    3 => BattlePhysicalBodyKind.FlyRotate,
                    -1 when ResourceId == 720 => BattlePhysicalBodyKind.Teleport,
                    -1 when ResourceId is 738 or 739 => BattlePhysicalBodyKind.NpcFly,
                    _ => BattlePhysicalBodyKind.Run
                });
            else if (!IsAlly)
            {
                if (ResourceId == 719)
                    result.Add(BattlePhysicalBodyKind.Fly);
                else if (ResourceId == 713)
                    result.Add(BattlePhysicalBodyKind.Teleport);
                result.Add(ResourceId switch
                {
                    720 => BattlePhysicalBodyKind.Teleport,
                    738 or 739 => BattlePhysicalBodyKind.NpcFly,
                    _ => BattlePhysicalBodyKind.Run
                });
            }
            else if (Identity == 2 && WeaponType == 6 || Identity == 0 && shape)
                result.Add(BattlePhysicalBodyKind.Teleport);
            else if (Identity == 0 && WeaponType != 2 || Identity == 1 && WeaponType is not (2 or 10) || Identity == 3 && WeaponType != 10 || Identity == 4 && WeaponType == 9)
                result.Add(BattlePhysicalBodyKind.Run);
            result.Add(BattlePhysicalBodyKind.Turn);
            return result.AsReadOnly();
        }
    }

    public interface IBattlePhysicalTimings
    {
        BattlePhysicalStageProfile Physical(string actor);
    }
}
