// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using Battle;
    using UnityEngine;

    public sealed partial class BattlePreview
    {
        // Editor layout uses a stable 800x600 canvas. These released button
        // bounds retain the original wheel's core/UI element ordering. Water's
        // animated top is fixed at 28 for this authoring view.
        public static BattleSpiritRect SpiritWheelTarget(int coreElement) => coreElement switch
        {
            0 => new BattleSpiritRect(722, 48, 750, 93),
            1 => new BattleSpiritRect(732, 22, 764, 71),
            2 => new BattleSpiritRect(753, 54, 799, 86),
            3 => new BattleSpiritRect(742, 32, 788, 78),
            4 => new BattleSpiritRect(755, 72, 802, 101),
            _ => throw new ArgumentOutOfRangeException(nameof(coreElement))
        };

        private Texture2D SpiritTexture(int coreElement)
        {
            int ui = coreElement switch
            {
                2 => 3,
                3 => 2,
                _ => coreElement
            };
            return View.GetTexture($"basedata.cpk\\ui\\combat\\control\\ImbibeNimbus{ui}.tga");
        }

        public void DrawBattleOverlay(BattleSession combat, Rect viewport, Vector4 safeInsets = default, bool wheelControls = true, string focusedControl = null)
        {
            if (combat.Definitions.Variant != "PAL3A" || combat.IsModalPresentation || combat.Status != BattleSessionStatus.Running)
                return;
            var context = new BattleHudViewport(viewport, safeInsets);
            Rect CanvasRect(float x, float y, float width, float height)
            {
                Vector2 center = context.NativeToViewport(new Vector2(x + width / 2, y + height / 2));
                Vector2 size = new(width * context.Scale, height * context.Scale);
                return new Rect(center - size / 2, size);
            }
            void Sprite(string name, float x, float y, float width, float height)
            {
                Texture2D texture = View.GetTexture("basedata.cpk\\ui\\combat\\control\\" + name + ".tga");
                GUI.DrawTextureWithTexCoords(CanvasRect(x, y, width, height), texture,
                    BattleHudCanvas.SpriteUv(texture, new Vector2(width, height)), true);
            }
            // Released UI 0x50b63f / 0x513941: a 90x140 padded backdrop,
            // followed by five cropped pieces. Charge tiers use unwtN0..2;
            // the full tier uses wtN0. Native core order swaps wind/thunder.
            Sprite("wll", 710, 20, 90, 140);
            if (wheelControls)
            {
                for (int element = 0; element < 5; element++)
                {
                    BattleSpiritRect target = SpiritWheelTarget(element);
                    int ui = element switch
                    {
                        2 => 3,
                        3 => 2,
                        _ => element
                    };
                    int tier = combat.Pal3AState.SpiritWheel.Charges[element] / 3;
                    string image = (tier < 3 ? "unwt" : "wt") + (4 - ui) + (tier < 3 ? tier : 0);
                    int skill = element switch
                    {
                        2 => 267,
                        3 => 266,
                        _ => 264 + element
                    };
                    Color previous = GUI.color;
                    try
                    {
                        if (focusedControl == "wheel:" + skill)
                        {
                            float pulse = .65f + .35f * MathF.Sin((float)UnityEditor.EditorApplication.timeSinceStartup * 7.8f);
                            GUI.color *= new Color(pulse, pulse, pulse, 1);
                        }
                        Sprite(image, target.Left, target.Top, target.Right - target.Left, target.Bottom - target.Top);
                    }
                    finally
                    {
                        GUI.color = previous;
                    }
                }
                Sprite("unbs", 739, 28, 45, 37);
            }
            foreach (BattleSpiritOrbSnapshot orb in combat.SpiritOrbs)
            {
                Vector2 point = context.SpiritOrbPoint(orb, View.ReferenceProjectionScale(viewport));
                float size = 32 * context.Scale;
                GUI.DrawTexture(new Rect(point.x - size / 2, point.y - size / 2, size, size), SpiritTexture(orb.Element), ScaleMode.ScaleToFit, true);
            }
        }
    }
}
