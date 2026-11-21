// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public readonly struct BattleHudCanvas : IDisposable
    {
        private static readonly Dictionary<(bool ProSkin, int Size, Color Color, bool Tight, bool Wrap, TextAnchor? Alignment), GUIStyle> Labels = new();

        private readonly Matrix4x4 _matrix;
        private readonly Color _color;

        public BattleHudCanvas(BattleHudViewport viewport) : this(viewport.SafeRect, viewport.Scale)
        {
        }

        public BattleHudCanvas(Rect area, float scale)
        {
            _matrix = GUI.matrix;
            _color = GUI.color;
            // Clip bounds must be established in the scaled coordinate system.
            // Scaling after BeginGroup leaves a physical-sized clip over a
            // reference-sized canvas and cuts the lower/right-hand controls.
            GUI.matrix = _matrix * Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUI.BeginGroup(new Rect(area.position / scale, area.size / scale));
        }

        public void Dispose()
        {
            GUI.EndGroup();
            GUI.color = _color;
            GUI.matrix = _matrix;
        }

        public static void DrawVitalNumber(Rect track, int value, string kind)
        {
            // CCBDisplayChain::ShowRoleState uses the current value. Both
            // state/result callers publish these offsets and RGBQUAD colors.
            // Pixel2FloatY and gbNumFont::Flush make +5 the lower baseline;
            // the native number glyph extends upward by 12 * 1.3 pixels.
            int x = kind == "GP" ? 31 : kind == "MP" ? 23 : 22;
            Color color = kind == "GP" ? new Color32(251, 224, 162, 255) : kind == "MP" ? new Color32(250, 168, 213, 255) : new Color32(160, 188, 250, 255);
            GUI.Label(new Rect(track.x + x, track.y + 5 - 15.6f, track.width - x, 15.6f),
                value.ToString(), LabelStyle(12, color, tight: true));
        }

        internal static GUIStyle LabelStyle(int size, Color color, bool tight = false, bool wrap = false, TextAnchor? alignment = null)
        {
            var key = (EditorGUIUtility.isProSkin, size, color, tight, wrap, alignment);
            if (Labels.TryGetValue(key, out GUIStyle cached))
                return cached;
            var style = new GUIStyle(EditorStyles.label)
            {
                fontSize = size,
                clipping = TextClipping.Clip,
                richText = false,
                wordWrap = wrap
            };
            if (alignment.HasValue)
                style.alignment = alignment.Value;
            if (tight)
            {
                style.padding = new RectOffset();
                style.alignment = TextAnchor.UpperLeft;
            }
            // Keep the native text color when Unity uses a hover or keyboard state.
            foreach (GUIStyleState state in new[] { style.normal, style.hover, style.active, style.focused,
                style.onNormal, style.onHover, style.onActive, style.onFocused })
            {
                state.textColor = color;
                state.background = null;
            }
            return Labels[key] = style;
        }

        public static Rect ProgressFill(Rect track, float fraction, bool result)
        {
            // UIProgressBar::Render plus released state/result radius settings.
            float inset = result ? 1 : 0;
            float width = result ? 81 : 76;
            return new Rect(track.x + inset, track.y + 2, width * Mathf.Clamp01(fraction), 7);
        }

        // Original UI images have power-of-two padding. TGA row zero enters
        // Unity at v=0, so the native top-left crop uses the lower UV range.
        public static Rect SpriteUv(Texture2D texture, Vector2 nativeSize) => new(0, 0,
            Math.Min(nativeSize.x, texture.width) / texture.width,
            Math.Min(nativeSize.y, texture.height) / texture.height);
    }
}
