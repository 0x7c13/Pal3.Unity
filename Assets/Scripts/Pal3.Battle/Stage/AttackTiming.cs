// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // Durations and markers are in logical ticks, compiled from the resource adapter before play.
    public sealed class AttackTiming
    {
        public int TravelTicks
        {
            get;
        }

        public int AnimationTicks
        {
            get;
        }

        public IReadOnlyList<int> HitTicks
        {
            get;
        }

        public int PreparationTicks
        {
            get;
        }

        public int MinimumEndTick
        {
            get;
        }

        public int BodyAnimationTicks
        {
            get;
        }

        public float? NativeWorkTimeSeconds
        {
            get;
        }

        public int EndTick => Math.Max(PreparationTicks + TravelTicks * 2 + AnimationTicks, MinimumEndTick);

        public AttackTiming(int travelTicks, int animationTicks, IEnumerable<int> hitTicks, int preparationTicks = 0, int minimumEndTick = 0, int? bodyAnimationTicks = null, float? nativeWorkTimeSeconds = null, bool allowEmptyHits = false)
        {
            int[] hits = hitTicks?.ToArray() ?? throw new ArgumentNullException(nameof(hitTicks));
            if (bodyAnimationTicks < 0 || bodyAnimationTicks > 30000 || nativeWorkTimeSeconds < 0 || nativeWorkTimeSeconds > 600 ||
                nativeWorkTimeSeconds.HasValue && !float.IsFinite(nativeWorkTimeSeconds.Value))
                throw new ArgumentException("Invalid body or native work time.");
            if (travelTicks < 0 || animationTicks < 1 || travelTicks > 30000 || animationTicks > 30000 || preparationTicks < 0 || preparationTicks > 30000 || minimumEndTick < 0 || minimumEndTick > 90000 ||
                !allowEmptyHits && hits.Length == 0 || hits.Length > 32 || hits.Any(tick => tick < 0 || tick > animationTicks) || !hits.SequenceEqual(hits.OrderBy(tick => tick)))
                throw new ArgumentException("Invalid normal attack timeline.");
            TravelTicks = travelTicks;
            AnimationTicks = animationTicks;
            PreparationTicks = preparationTicks;
            MinimumEndTick = minimumEndTick;
            BodyAnimationTicks = bodyAnimationTicks ?? animationTicks;
            NativeWorkTimeSeconds = nativeWorkTimeSeconds;
            HitTicks = Array.AsReadOnly(hits);
        }
    }

    // Presentation/resource knowledge is supplied at the composition boundary, never looked up by the loop.
    public interface IBattleCatalog
    {
        AttackTiming Attack(string source, string target);

        int GuardEntry(string actor);

        int Hurt(string actor);

        int SpImpact(string actor);

        int Priority(string actor);

        int DamageDivisor(string actor);
    }

    public interface IBattleAbilityTimings
    {
        AttackTiming Ability(string actor, BattleAbilityDefinition ability);
    }
}
