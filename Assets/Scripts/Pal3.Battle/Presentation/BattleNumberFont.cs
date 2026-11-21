// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Battle.Presentation
{
    using System;
    using System.IO;

    // GB_FONT_UNICODE20 is a one-bit BMP-indexed font, including the centered
    // half-width ASCII used by ShowDamage and ShowMiss in both released games.
    public sealed class BattleNumberFont
    {
        public const int Size = 20;
        private const int BytesPerGlyph = Size * Size / 8;
        private readonly byte[] _glyphs;
        private readonly int _count;

        public BattleNumberFont(byte[] source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            using var reader = new BinaryReader(new MemoryStream(source, false));
            if (source.Length < 20)
                throw new InvalidDataException("The native battle font header is truncated.");
            reader.ReadUInt32(); // Engine does not interpret the file's magic.
            uint version = reader.ReadUInt32();
            uint size = reader.ReadUInt32();
            uint bits = reader.ReadUInt32();
            _count = reader.ReadInt32();
            if (version != 1 || size != Size || bits != 1 || _count <= 0 || _count > 65536 ||
                source.Length != 20 + _count * BytesPerGlyph)
                throw new InvalidDataException("Expected an original unicode20.tft one-bit font.");
            _glyphs = reader.ReadBytes(_count * BytesPerGlyph);
        }

        public static int Advance(char character) => character is >= ' ' and <= '~' ? Size / 2 : Size;

        public byte[] GlyphAlpha(char character)
        {
            if (character >= _count)
                throw new InvalidDataException("The native battle font has no glyph for " + (int)character + ".");
            var alpha = new byte[Size * Size];
            int start = character * BytesPerGlyph;
            for (int pixel = 0; pixel < alpha.Length; pixel++)
                alpha[pixel] = (_glyphs[start + pixel / 8] & (1 << (pixel % 8))) != 0 ? byte.MaxValue : (byte)0;
            return alpha;
        }
    }
}
