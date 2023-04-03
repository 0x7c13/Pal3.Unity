// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using UnityEngine;
    using UnityEngine.UI;

    public static class UITheme
    {
        public static ColorBlock GetButtonColors()
        {
            #if PAL3
            return new ColorBlock()
            {
                colorMultiplier = 1f,
                normalColor = new Color(100f / 255f, 80f / 255f, 40f / 255f, 190f / 255f),
                highlightedColor = new Color(30f / 255f, 75f / 255f, 140f / 255f, 200f / 255f),
                pressedColor = new Color(20f / 255f, 50f / 255f, 100f / 255f, 200f / 255f),
                selectedColor = new Color(30f / 255f, 75f / 255f, 140f / 255f, 200f / 255f),
            };
            #elif PAL3A
            return new ColorBlock()
            {
                colorMultiplier = 1f,
                normalColor = new Color(100f / 255f, 80f / 255f, 40f / 255f, 190f / 255f),
                highlightedColor = new Color(160f / 255f, 40f / 255f, 110f / 255f, 200f / 255f),
                pressedColor = new Color(110f / 255f, 25f / 255f, 75f / 255f, 200f / 255f),
                selectedColor = new Color(160f / 255f, 40f / 255f, 110f / 255f, 200f / 255f),
            };
            #endif
        }
    }
}