// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using UnityEngine;

    public readonly struct BattleHudInteractionProfile
    {
        public bool Touch
        {
            get;
        }

        public float MenuScale
        {
            get;
        }

        public float HitPadding
        {
            get;
        }

        public BattleHudInteractionProfile(bool touch, float menuScale = 1.2f, float hitPadding = 6)
        {
            if (!float.IsFinite(menuScale) || menuScale < 1 || menuScale > 2 || !float.IsFinite(hitPadding) || hitPadding < 0 || hitPadding > 32)
                throw new ArgumentException("Interaction dimensions must be finite and bounded.");
            Touch = touch;
            MenuScale = menuScale;
            HitPadding = hitPadding;
        }

        public float ScaleFor(BattleHudViewport viewport) => !Touch ? 1 : Math.Max(1,
            Math.Min(MenuScale, (viewport.SafeRect.height - 109 * viewport.Scale - 16) / (322 * viewport.Scale)));
    }
}
