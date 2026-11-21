// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Globalization;

    public readonly struct BattleMovementSound
    {
        public string Sound
        {
            get;
        }

        public bool Loop
        {
            get;
        }

        public BattleMovementSound(string sound, bool loop)
        {
            Sound = sound;
            Loop = loop;
        }
    }

    public readonly struct BattleBodySoundProfile
    {
        public int Attack
        {
            get;
        }

        public int Hurt
        {
            get;
        }

        public BattleBodySoundProfile(int attack, int hurt)
        {
            Attack = attack;
            Hurt = hurt;
        }
    }

    public interface IBattleBodyAudioCatalog
    {
        BattleBodySoundProfile BodySounds(string actor);
    }

    public interface IBattleMovementAudio
    {
        void PlayMovementSound(string actor, string sound, bool loop);

        void StopMovementSound(string actor);

        void StopBodySounds();
    }

    // Published ProcAttack / ProcBlock / movement bodies. Sound selection never
    // draws from the combat RNG and never manufactures a damage callback.
    public static class BattleBodyAudioRules
    {
        public static string Attack(string variant, int identity, int weapon, BattlePhysicalMarkerKind marker,
            int monsterSound = 0, bool magicSword = false, bool transformed = false)
        {
            Validate(variant, identity);
            if (marker > (variant == "PAL3" ? BattlePhysicalMarkerKind.Work1 : BattlePhysicalMarkerKind.Work3))
                return null;
            if (identity < 0)
                return Numbered("wd", monsterSound);
            if (variant == "PAL3")
                return identity switch
                {
                    0 => magicSword ? "wd023" : "wd025",
                    1 => "wd028",
                    2 => weapon == 7 ? "wd030" : marker == BattlePhysicalMarkerKind.Work1 ? "wd024" : null,
                    3 => weapon == 4 ? "wd027" : "wd026",
                    4 => weapon == 2 ? "wd025" : "wd029",
                    _ => null
                };
            return identity switch
            {
                0 when weapon == 2 && !transformed => null,
                0 => "wd025",
                1 => weapon == 3 ? "wd026" : "wd023",
                2 => weapon == 5 ? "wd027" : "wd028",
                3 when weapon == 10 => null,
                3 => "wd028",
                4 => weapon == 9 ? "wd031" : "wd030",
                _ => null
            };
        }

        public static string Hurt(string variant, int identity, bool guarded, int monsterSound = 0)
        {
            Validate(variant, identity);
            if (identity < 0)
                return Numbered("we", monsterSound);
            return guarded ? "we020" : Numbered("we", (variant == "PAL3" ? 15 : 2) + identity);
        }

        public static BattleMovementSound Movement(string variant, int identity, BattleMovementPlan plan)
        {
            Validate(variant, identity);
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            // Native early arrival does not activate a movement sound.
            if (BattleMovement.Distance(plan.From, plan.To) < (plan.Kind == BattleMovementKind.FastRun ? 4f : 2f))
                return default;
            bool fleeing = plan.To.X < -700f && plan.To.Z < -700f;
            if (plan.Kind == BattleMovementKind.Teleport)
                return new BattleMovementSound("we014", false);
            if (variant == "PAL3A")
                return identity < 0 || plan.Kind is not (BattleMovementKind.Run or BattleMovementKind.NpcFly) ? default :
                    new BattleMovementSound(identity is 1 or 2 ? "we001" : "we011", !fleeing);
            return plan.Kind switch
            {
                BattleMovementKind.Run or BattleMovementKind.NpcFly when identity == 0 => new BattleMovementSound("we011", !fleeing),
                BattleMovementKind.Run or BattleMovementKind.NpcFly when identity == 4 => new BattleMovementSound("we011b", !fleeing),
                BattleMovementKind.Fly when identity == 1 => new BattleMovementSound("we012", false),
                BattleMovementKind.Fly or BattleMovementKind.FlyRotate when identity == 3 => new BattleMovementSound("we013", false),
                _ => default
            };
        }

        private static string Numbered(string prefix, int number) => number == 0 ? null : prefix + number.ToString("D3", CultureInfo.InvariantCulture);

        private static void Validate(string variant, int identity)
        {
            if (variant is not ("PAL3" or "PAL3A") || identity is < -1 or > 4)
                throw new ArgumentException("Invalid original combat body sound identity.");
        }
    }
}
