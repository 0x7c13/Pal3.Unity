// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Linq;
    using Battle;
    using Battle.Presentation;
    using UnityEngine;

    public sealed class BattleHudLayout
    {
        public bool IsPal3A
        {
            get;
        }

        public float AttackSequenceX => IsPal3A ? 410 : 483;

        public float AttackSequenceY => IsPal3A ? 14 : 10;

        public float ListHeight => IsPal3A ? 226 : 206;

        public BattleHudLayout(string variant)
        {
            if (variant is not ("PAL3" or "PAL3A"))
                throw new ArgumentException("Unknown battle HUD variant.", nameof(variant));
            IsPal3A = variant == "PAL3A";
        }

        public static Rect ContentRect(Rect available)
        {
            float scale = Math.Max(0, Math.Min(available.width / 800, available.height / 600));
            Vector2 size = new(800 * scale, 600 * scale);
            return new Rect(available.center - size / 2, size);
        }

        public static Vector2 ToCanvas(Vector2 point, Rect content) => new((point.x - content.x) * 800 / content.width,
            (point.y - content.y) * 600 / content.height);

        public static Vector2 ToViewport(Vector2 point, Rect content) => new(content.x + point.x * content.width / 800,
            content.y + point.y * content.height / 600);

        public static bool Hit(Rect canvasRect, Vector2 point, Rect content) => content.width > 0 && content.height > 0 &&
            content.Contains(point) && canvasRect.Contains(ToCanvas(point, content));

        public static Rect StateCard(int index, int page = 0) => new(new[] { 0f, 207, 403, 600 }[index - page * 4], 491, 200, 106);

        public Vector2 AttackPortrait(int gauge, int castGauge, bool casting)
        {
            float x = AttackSequenceX - 3 - 20;
            x += casting ? 199 + Mathf.Clamp01(castGauge / (float)BattleSession.FullCastGauge) * 90 :
                Mathf.Clamp01(gauge / (float)BattleSession.FullGauge) * 197;
            return new Vector2(x, AttackSequenceY - 2);
        }

        public string StateFace(int identity) => IsPal3A ? "state/tx" + (identity + 1) : "state/" + Pal3Name(identity);

        public string StateName(int identity) => IsPal3A ? "state/wz_" + new[] { "ngh", "wh", "px", "xx", "lyg" }[identity] :
            "state/" + Pal3Name(identity) + "name";

        public string FormationFace(int identity, bool disabled) => IsPal3A ? "control/Lineup/" +
            new[] { "zngh", "zwh", "zpx", "zxx", "zlyg" }[identity] + (disabled ? "02" : "01") :
            "control/Lineup/" + Pal3Name(identity) + (disabled ? "_d" : "");

        public string AiName(int identity) => "control/AI/" + (IsPal3A ? new[] { "w2", "w3", "w4", "w5", "w6" }[identity] : Pal3Name(identity));

        public string ProtectionFace(int identity) => IsPal3A ? identity switch
        {
            1 => "control/Protect/yhwh",
            2 => "control/Protect/yhpx",
            _ => null
        } : identity switch
        {
            1 => "control/Protect/xuejian",
            2 => "control/Protect/longkui",
            3 => "control/Protect/zixuan",
            _ => null
        };

        public static Vector2 FormationPosition(BattleSlot slot) => slot switch
        {
            BattleSlot.Water => new Vector2(104, 188),
            BattleSlot.Fire => new Vector2(46, 148),
            BattleSlot.Wind => new Vector2(136, 87),
            BattleSlot.Thunder => new Vector2(65, 87),
            BattleSlot.Earth => new Vector2(156, 148),
            BattleSlot.Center => new Vector2(104, 131),
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };

        // PAL3 CBUIPos.h and PAL3A released 0x5ebf5c..0x5ebfbc
        // publish the same status rectangles inside each native state card.
        public static Rect? StatusRect(BattleState state) => state switch
        {
            BattleState.AttackIncrease or BattleState.AttackDecrease => new Rect(15, 85, 22, 23),
            BattleState.DefenseIncrease or BattleState.DefenseDecrease => new Rect(39, 85, 22, 23),
            BattleState.SpeedIncrease or BattleState.SpeedDecrease => new Rect(63, 85, 22, 23),
            BattleState.LuckIncrease or BattleState.LuckDecrease => new Rect(87, 85, 22, 23),
            BattleState.Paralysis or BattleState.Seal or BattleState.Forbidden or BattleState.Sleep or BattleState.Chaos or BattleState.Madness => new Rect(111, 88, 16, 16),
            BattleState.Reflection => new Rect(111, 88, 20, 17),
            BattleState.Evade => new Rect(111, 88, 18, 18),
            BattleState.Barrier => new Rect(111, 88, 19, 17),
            BattleState.Invisible => new Rect(111, 88, 18, 17),
            BattleState.PoisonResist => new Rect(131, 88, 37, 17),
            _ => null
        };

        public static float StatusLuminance(double seconds) => (200 + Mathf.RoundToInt(MathF.Sin((float)seconds * 12) * 50)) / 255f;

        public static Color FaceTint(BattleActorSnapshot actor, bool darkDead = true)
        {
            uint value = darkDead && actor.Hp <= 0 ? 0xff505050 : actor.States.Any(state => state.LatentRounds == 0 && state.State == BattleState.PoisonWater) ? 0xff399aad :
                actor.States.Any(state => state.LatentRounds == 0 && state.State == BattleState.PoisonFire) ? 0xffde7531 :
                actor.States.Any(state => state.LatentRounds == 0 && state.State == BattleState.PoisonThunder) ? 0xffa466ea :
                actor.States.Any(state => state.LatentRounds == 0 && state.State == BattleState.PoisonWind) ? 0xff52b66b :
                actor.States.Any(state => state.LatentRounds == 0 && state.State == BattleState.PoisonEarth) ? 0xffd6ae5a : 0xffffffff;
            return new Color((value >> 16 & 255) / 255f, (value >> 8 & 255) / 255f, (value & 255) / 255f, 1);
        }

        private static string Pal3Name(int identity) => new[] { "jingtian", "xuejian", "longkui", "zixuan", "changqing" }[identity];
    }
}
