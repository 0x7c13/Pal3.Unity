// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Collections.Generic;
    using Battle.Presentation;
    using UnityEngine;

    public readonly struct BattleHudTextGlyph
    {
        public string Text
        {
            get;
        }

        public int Column
        {
            get;
        }

        public int Row
        {
            get;
        }

        public BattleHudTextGlyph(string text, int column, int row)
        {
            Text = text;
            Column = column;
            Row = row;
        }
    }

    public static class BattleHudText
    {
        // ProcSkillList / ProcMagicList / ProcItemList pass only the suffix
        // after the final '$' to UIStatic. '#' is ordinary text here.
        public static string Description(string value)
        {
            int separator = value?.LastIndexOf('$') ?? -1;
            return separator < 0 ? string.Empty : value[(separator + 1)..];
        }

        // Match the compact native strip without the larger Editor font metrics.
        public const int DescriptionFontSize = 14;

        public static Rect AbilityPanelBounds(BattleHudPage page, bool pal3A) => page switch
        {
            BattleHudPage.Skills => new Rect(10, 237, 256, 256),
            BattleHudPage.Magic => new Rect(10, pal3A ? 258 : 276, 249, pal3A ? 226 : 206),
            BattleHudPage.Items => new Rect(10, pal3A ? 258 : 278, 249, pal3A ? 226 : 206),
            _ => throw new ArgumentOutOfRangeException(nameof(page))
        };

        // Effective UIStatic text rectangles include its native eight-pixel
        // border. PAL3A uses a separate Skill description child and taller lists.
        public static Rect DescriptionBounds(BattleHudPage page, bool pal3A) => page switch
        {
            BattleHudPage.Skills => pal3A ? new Rect(47, 233, 192, 23) : new Rect(47, 227, 201, 21),
            BattleHudPage.Magic => pal3A ? new Rect(48, 202, 193, 16) : new Rect(48, 178, 193, 20),
            BattleHudPage.Items => pal3A ? new Rect(45, 204, 184, 24) : new Rect(45, 178, 184, 24),
            _ => throw new ArgumentOutOfRangeException(nameof(page))
        };

        public static Rect ListBounds(BattleHudPage page, bool pal3A) => page switch
        {
            BattleHudPage.Skills => pal3A ? new Rect(39, 92, 208, 136) : new Rect(39, 110, 202, 116),
            BattleHudPage.Magic => pal3A ? new Rect(39, 78, 214, 119) : new Rect(39, 78, 202, 97),
            BattleHudPage.Items => new Rect(39, 78, 202, pal3A ? 118 : 97),
            _ => throw new ArgumentOutOfRangeException(nameof(page))
        };

        // UIDrawText uses half-width ASCII cells, full-width Chinese cells,
        // literal backslash-n/backslash-t escapes and a font-height bottom gate.
        public static IReadOnlyList<BattleHudTextGlyph> Layout(string value, float width, float height, int fontSize)
        {
            if (fontSize <= 0 || !float.IsFinite(width) || !float.IsFinite(height))
                throw new ArgumentOutOfRangeException(nameof(fontSize));
            var result = new List<BattleHudTextGlyph>();
            if (string.IsNullOrEmpty(value) || width <= 0 || height < fontSize)
                return result;
            int column = 0, row = 0;
            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];
                if (current == '\\')
                {
                    if (++index == value.Length)
                        break;
                    if (value[index] == 'n')
                    {
                        row++;
                        column = 0;
                    }
                    else if (value[index] == 't')
                        column = (column + 8) / 8 * 8;
                    continue;
                }
                if (current == '\0')
                    break;
                int cells = current < 127 ? 1 : 2;
                if ((column + cells) * fontSize / 2f > width)
                {
                    column = 0;
                    row++;
                }
                if ((row + 1) * fontSize > height)
                    break;
                // Actual ASCII control bytes are not the backslash escapes.
                // They consume one native cell but add no visible glyph ink.
                if (current < 32)
                {
                    column += cells;
                    continue;
                }
                string glyph = current.ToString();
                if (char.IsHighSurrogate(current) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                    glyph += value[++index];
                result.Add(new BattleHudTextGlyph(glyph, column, row));
                column += cells;
            }
            return result;
        }
    }
}
