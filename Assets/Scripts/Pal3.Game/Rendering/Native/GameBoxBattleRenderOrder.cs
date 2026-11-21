// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Native
{
    /// <summary>Published scene-graph flush boundaries around combat shadows.</summary>
    public static class GameBoxBattleRenderOrder
    {
        public const int BeforeShadows = -1;
        public const int AfterShadows = 1;
        public const int DirectEffects = 2;

        public static int Actor(bool pal3A, bool ally, int actorId) =>
            pal3A && (ally || actorId == 7310) ? AfterShadows : BeforeShadows;

        public static int Effect(bool pal3A, bool model) =>
            model ? pal3A ? AfterShadows : BeforeShadows : DirectEffects;
    }
}
