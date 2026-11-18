// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle.Presentation
{
    using System;
    using System.Collections.Generic;

    public readonly struct BattleLineupFrame
    {
        public BattleSide Side
        {
            get;
        }

        public byte Alpha
        {
            get;
        }

        public BattleLineupFrame(BattleSide side, byte alpha)
        {
            Side = side;
            Alpha = alpha;
        }
    }

    public static class BattleLineupProjection
    {
        public static IReadOnlyList<BattleLineupFrame> Capture(BattleSession battle)
        {
            if (battle == null)
                throw new ArgumentNullException(nameof(battle));
            var frames = new List<BattleLineupFrame>();
            if (battle.Status != BattleSessionStatus.Running || battle.IsModalPresentation)
                return frames;
            double elapsed = BattleStageScheduler.TimeAt(battle.SimulationTick);
            // ProcDialog starts this non-blocking task alongside ProcControl.
            if (battle.Encounter != BattleEncounter.EnemyAmbush && elapsed <= 4)
            {
                byte alpha = Alpha(elapsed, 4);
                frames.Add(new BattleLineupFrame(BattleSide.Ally, alpha));
                frames.Add(new BattleLineupFrame(BattleSide.Enemy, alpha));
            }
            foreach (BattleAbilityPresentationSnapshot ability in battle.AbilityPresentations)
                if (ability.LineupVisible)
                    frames.Add(new BattleLineupFrame(ability.Side, Alpha(ability.LineupElapsedSeconds, ability.LineupDurationSeconds)));
            return frames;
        }

        public static byte Alpha(double elapsed, double duration)
        {
            if (duration <= 0 || elapsed < 0 || elapsed > duration)
                return 0;
            // ProcShowFiveLineup submits the previous pass's alpha before
            // calculating the next value. Repainting must not age this value.
            float previous = (float)Math.Max(0, elapsed - BattleSession.StepSeconds);
            float fadeIn = (float)duration / 4 * 3;
            float fadeOut = (float)duration / 4;
            float value = previous < fadeIn ? 255 * (previous / fadeIn) : 255 * (1 - (previous - fadeIn) / fadeOut);
            return (byte)Math.Clamp((int)Math.Round(value, MidpointRounding.ToEven), 0, 255);
        }

        public static float AllyRoll(float initial, int sceneElement)
        {
            int greater = BattleEffectRules.GreaterElement(sceneElement);
            int turns = greater switch
            {
                1 => 1,
                2 => 2,
                3 => 4,
                4 => 3,
                _ => 0
            };
            return initial + turns * ((360f / 5) * 3.14159f / 180f);
        }

        public static uint CenterColor(int sceneElement) => sceneElement switch
        {
            1 => 0x399aadu,
            2 => 0xde7531u,
            3 => 0x52b66bu,
            4 => 0xde75bdu,
            5 => 0xd6ae5au,
            _ => 0u
        };

        public static BattleMovementPoint Corner(BattleMovementPoint center, float radius, float roll, int index, bool centerTile)
        {
            if (index is < 0 or > 3)
                throw new ArgumentOutOfRangeException(nameof(index));
            float half = centerTile ? radius / 2 : radius;
            float x = index is 0 or 3 ? -half : half;
            float z = index < 2 ? -half : half;
            float sine = centerTile ? 0 : MathF.Sin(roll);
            float cosine = centerTile ? 1 : MathF.Cos(roll);
            return new BattleMovementPoint(center.X + x * cosine + z * sine,
                centerTile ? 0 : .5f, center.Z - x * sine + z * cosine);
        }
    }
}
