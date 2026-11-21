// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Battle;
    using Battle.Presentation;
    using UnityEngine;

    public sealed partial class BattlePreview
    {
        private BattleSession _numberOwner;
        private BattleNumberProjection _numbers;
        private BattleNumberFont _numberFont;
        private readonly Dictionary<char, Texture2D> _numberGlyphs = new();

        public IReadOnlyList<BattleNumberFrame> NumberFrames => _numbers?.Sample() ?? Array.Empty<BattleNumberFrame>();

        public BattleDamageNumberColorMode DamageNumberColorMode
        {
            get;
            set;
        } = BattleDamageNumberColorMode.Original;

        public void ApplyNumbers(BattleSession combat)
        {
            if (_numberOwner != combat)
            {
                _numberOwner = combat;
                _numbers = new BattleNumberProjection(_data.Resources.Codepage == 950 ? "蠱" : "蛊");
            }
            if (combat.Status != BattleSessionStatus.Running)
            {
                _numbers.Clear();
                return;
            }
            _numbers.Synchronize(combat.Events, combat.SimulationTick);
        }

        private Texture2D NumberGlyph(char character)
        {
            if (_numberGlyphs.TryGetValue(character, out Texture2D texture))
                return texture;
            if (_numberFont == null)
            {
                const string path = "basedata.cpk\\gbdata\\unicode20.tft";
                var entry = _data.Resources.Find(path) ?? throw new FileNotFoundException("Missing original battle font: " + path);
                _numberFont = new BattleNumberFont(_data.Resources.Read(entry));
            }
            byte[] alpha = _numberFont.GlyphAlpha(character);
            var pixels = new Color32[alpha.Length];
            for (int y = 0; y < BattleNumberFont.Size; y++)
                for (int x = 0; x < BattleNumberFont.Size; x++)
                    pixels[(BattleNumberFont.Size - 1 - y) * BattleNumberFont.Size + x] =
                        new Color32(255, 255, 255, alpha[y * BattleNumberFont.Size + x]);
            texture = new Texture2D(BattleNumberFont.Size, BattleNumberFont.Size, TextureFormat.RGBA32, false)
            {
                name = "Battle number " + (int)character,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            _numberGlyphs.Add(character, texture);
            return texture;
        }

        public static Rect NumberBounds(BattleNumberFrame frame, Vector2 projected, BattleHudViewport viewport)
        {
            int width = 0;
            foreach (char character in frame.Text)
                width += BattleNumberFont.Advance(character);
            return new Rect(projected.x + frame.OffsetX * viewport.Scale,
                projected.y + (frame.OffsetY - BattleNumberFont.Size) * viewport.Scale,
                width * viewport.Scale, BattleNumberFont.Size * viewport.Scale);
        }

        public void DrawBattleNumbers(BattleSession combat, Rect viewport, Vector4 safeInsets = default)
        {
            if (combat == null || combat.IsModalPresentation || combat.Status != BattleSessionStatus.Running ||
                Event.current.type != EventType.Repaint || viewport.width < 1 || viewport.height < 1)
                return;
            ApplyNumbers(combat);
            View.SetViewportContext(viewport);
            var context = new BattleHudViewport(viewport, safeInsets);
            Color previous = GUI.color;
            GUI.BeginGroup(context.SafeRect);
            try
            {
                foreach (BattleNumberFrame frame in NumberFrames)
                {
                    if (frame.Alpha == 0 || !View.Project(BattleAnimationCatalog.UnityPosition(frame.Position), viewport, out Vector2 point))
                        continue;
                    Rect bounds = NumberBounds(frame, point, context);
                    bounds.position -= context.SafeRect.position;
                    uint color = BattleDamageNumberColors.Resolve(frame.Color, DamageNumberColorMode);
                    GUI.color = new Color32((byte)(color >> 16), (byte)(color >> 8), (byte)color, frame.Alpha);
                    float x = bounds.x;
                    foreach (char character in frame.Text)
                    {
                        int advance = BattleNumberFont.Advance(character);
                        const float inset = .4875f / BattleNumberFont.Size;
                        bool ascii = advance == BattleNumberFont.Size / 2;
                        Rect uv = new((ascii ? .25f : 0) + inset, inset,
                            (ascii ? .5f : 1) - 2 * inset, 1 - 2 * inset);
                        GUI.DrawTextureWithTexCoords(new Rect(x, bounds.y, advance * context.Scale, bounds.height), NumberGlyph(character), uv, true);
                        x += advance * context.Scale;
                    }
                }
            }
            finally
            {
                GUI.EndGroup();
                GUI.color = previous;
            }
        }

        private void DisposeNumbers()
        {
            foreach (Texture2D texture in _numberGlyphs.Values)
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
            _numberGlyphs.Clear();
            _numberFont = null;
            _numbers?.Clear();
            _numberOwner = null;
        }
    }
}
