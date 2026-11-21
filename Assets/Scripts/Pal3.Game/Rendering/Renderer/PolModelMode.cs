// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.Renderer
{
    public enum PolModelMode
    {
        // Apply the game's animated-water convention where requested by texture name.
        Game,

        // Retain file blend flags and vertex colors without substituting water
        // materials, and preserve the full source mesh bounds. Scene lighting
        // remains the material manager's responsibility.
        Authored,
    }
}
