// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Battle;
    using Battle.Presentation;
    using UnityEngine;

    public sealed partial class BattlePreview
    {
        private Dictionary<string, string> _battleMessageLanguage;
        private BattleSession _messageOwner;
        private readonly BattleStageMessageProjection _messageProjection = new();

        public void ApplyMessages(BattleSession combat)
        {
            if (_messageOwner != combat)
            {
                _messageOwner = combat;
                _messageProjection.Clear();
            }
            _messageProjection.Synchronize(combat.StageMessageReferences, Trail.Enabled);
        }

        public string BattleMessageText(BattleStageMessageSnapshot message)
        {
            if (message.LanguageKey == null)
                return message.Text ?? string.Empty;
            if (_battleMessageLanguage == null)
            {
                _battleMessageLanguage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string source = Encoding.GetEncoding(_data.Resources.Codepage).GetString(_data.Bytes("basedata.cpk\\datascript\\lang.txt"));
                foreach (string line in source.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int separator = line.IndexOf('$');
                    if (separator < 0 || line.TrimStart().StartsWith(";", StringComparison.Ordinal))
                        continue;
                    int end = line.IndexOf('&', separator + 1);
                    if (end < 0)
                        continue;
                    _battleMessageLanguage[line[..separator].Trim()] = line[(separator + 1)..end].Trim();
                }
            }
            return _battleMessageLanguage.TryGetValue(message.LanguageKey, out string text) ? text :
                throw new InvalidOperationException("Missing original battle message: " + message.LanguageKey);
        }

        public static Rect BattleMessageBounds(bool pal3A, float canvasWidth)
        {
            // PAL3 ships a fixed x=300. PAL3A centers this same 161x57 control
            // using integer ClientWidth arithmetic. Keep the PAL3 composition
            // in the centered original canvas when extra viewport width exists.
            float x = pal3A ? ((int)canvasWidth - 161) / 2 : 300 + (canvasWidth - 800) / 2;
            return new Rect(x, 30, 161, 57);
        }

        public void DrawBattleMessages(BattleSession combat, Rect viewport, Vector4 safeInsets = default)
        {
            if (combat == null || combat.IsModalPresentation || combat.Status != BattleSessionStatus.Running ||
                Event.current.type != EventType.Repaint || viewport.width < 1 || viewport.height < 1)
                return;
            var context = new BattleHudViewport(viewport, safeInsets);
            ApplyMessages(combat);
            using (new BattleHudCanvas(context))
            {
                Rect bounds = BattleMessageBounds(BattleData.Variant == "PAL3A", context.CanvasSize.x);
                foreach (BattleStageMessageSnapshot message in _messageProjection.Messages)
                {
                    GUI.color = Color.white;
                    string background = message.Side == BattleSide.Ally ? "TricknameBack" : "TricknameBack2";
                    Texture2D texture = View.GetTexture("basedata.cpk\\ui\\combat\\" + background + ".tga");
                    GUI.DrawTextureWithTexCoords(bounds, texture, BattleHudCanvas.SpriteUv(texture, bounds.size), true);
                    DrawBattleMessageText(BattleMessageText(message), bounds);
                }
            }
        }

        private void DrawBattleMessageText(string text, Rect bounds)
        {
            // UIStatic uses an eight-pixel border and UIDrawText's integer
            // single-line centering. SetFont(TRUE) selects unicode20.tft.
            int width = 0;
            foreach (char character in text)
                width += BattleNumberFont.Advance(character);
            float x = bounds.x + 8 + (145 - width) / 2;
            float y = bounds.y + 8 + (41 - BattleNumberFont.Size) / 2;
            foreach (char character in text)
            {
                int advance = BattleNumberFont.Advance(character);
                const float inset = .4875f / BattleNumberFont.Size;
                bool ascii = advance == BattleNumberFont.Size / 2;
                Rect uv = new((ascii ? .25f : 0) + inset, inset,
                    (ascii ? .5f : 1) - 2 * inset, 1 - 2 * inset);
                GUI.DrawTextureWithTexCoords(new Rect(x, y, advance, BattleNumberFont.Size), NumberGlyph(character), uv, true);
                x += advance;
            }
        }
    }
}
