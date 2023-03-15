// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using UnityEngine;
    using UnityEngine.UI;

    public enum ButtonType
    {
        Normal,
        Highlighted,
    }

    public static class UITheme
    {
        public static ColorBlock GetButtonColors(ButtonType buttonType = ButtonType.Normal)
        {
            #if PAL3
            if (buttonType == ButtonType.Normal)
            {
                return new ColorBlock()
                {
                    colorMultiplier = 1f,
                    normalColor = new Color(40f / 255f, 40f / 255f, 40f / 255f, 200f / 255f),
                    highlightedColor = new Color(30f / 255f, 75f / 255f, 140f / 255f, 200f / 255f),
                    pressedColor = new Color(20f / 255f, 50f / 255f, 100f / 255f, 200f / 255f),
                    selectedColor = new Color(30f / 255f, 75f / 255f, 140f / 255f, 200f / 255f),
                };
            }
            else
            {
                return new ColorBlock()
                {
                    colorMultiplier = 1f,
                    normalColor = new Color(200f / 255f, 120f / 255f, 0f / 255f, 200f / 255f),
                    highlightedColor = new Color(30f / 255f, 75f / 255f, 140f / 255f, 200f / 255f),
                    pressedColor = new Color(20f / 255f, 50f / 255f, 100f / 255f, 200f / 255f),
                    selectedColor = new Color(30f / 255f, 75f / 255f, 140f / 255f, 200f / 255f),
                };
            }
            #elif PAL3A
            if (buttonType == ButtonType.Normal)
            {
                return new ColorBlock()
                {
                    colorMultiplier = 1f,
                    normalColor = new Color(40f / 255f, 40f / 255f, 40f / 255f, 200f / 255f),
                    highlightedColor = new Color(160f / 255f, 40f / 255f, 110f / 255f, 200f / 255f),
                    pressedColor = new Color(110f / 255f, 25f / 255f, 75f / 255f, 200f / 255f),
                    selectedColor = new Color(160f / 255f, 40f / 255f, 110f / 255f, 200f / 255f),
                };
            }
            else
            {
                return new ColorBlock()
                {
                    colorMultiplier = 1f,
                    normalColor = new Color(200f / 255f, 120f / 255f, 0f / 255f, 200f / 255f),
                    highlightedColor = new Color(160f / 255f, 40f / 255f, 110f / 255f, 200f / 255f),
                    pressedColor = new Color(110f / 255f, 25f / 255f, 75f / 255f, 200f / 255f),
                    selectedColor = new Color(160f / 255f, 40f / 255f, 110f / 255f, 200f / 255f),
                };
            }
            #endif
        }
    }
}