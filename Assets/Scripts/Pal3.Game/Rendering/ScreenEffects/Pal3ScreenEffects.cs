// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Rendering.ScreenEffects
{
    using UnityEngine;

    [DisallowMultipleComponent]
    internal sealed class Pal3ScreenEffects : MonoBehaviour
    {
        public bool Distortion { get; set; }
        public bool Snow { get; set; }
    }
}
