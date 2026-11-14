// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser
{
    using System;
    using System.Text;
    using System.IO;
    using UnityEngine;

    // Survives assembly reload through the owning window's serialized reference.
    public sealed class ResourceTextDraft : ScriptableObject
    {
        public string Text, Original, SourcePath, PhysicalPath, SourceRoot;
        public int Codepage;
        public bool HasBom;

        public bool IsDirty => Text != Original;
        public bool Editable => Original != null && Original.Length <= 60000;

        public void Load(byte[] bytes, int codepage)
        {
            int offset = 0;
            Codepage = codepage;
            HasBom = false;
            if (bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
            {
                Codepage = 65001;
                offset = 3;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe)
            {
                Codepage = 1200;
                offset = 2;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xfe && bytes[1] == 0xff)
            {
                Codepage = 1201;
                offset = 2;
            }
            HasBom = offset > 0;
            Original = Text = StrictEncoding(Codepage).GetString(bytes, offset, bytes.Length - offset);
        }

        public bool CanReadFrom(string root) => !string.IsNullOrEmpty(SourceRoot) &&
            Path.GetFullPath(SourceRoot).TrimEnd(Path.DirectorySeparatorChar).Equals(
                Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

        public static string ContextAt(string text, int match)
        {
            int start = Math.Clamp(match - 40, 0, text.Length);
            return text.Substring(start, Math.Min(180, text.Length - start));
        }

        public byte[] Encode(bool utf8)
        {
            Encoding encoding = StrictEncoding(utf8 ? 65001 : Codepage);
            byte[] content = encoding.GetBytes(Text);
            byte[] bom = !utf8 && HasBom ? encoding.GetPreamble() : Array.Empty<byte>();
            if (bom.Length == 0)
                return content;
            var result = new byte[bom.Length + content.Length];
            Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
            Buffer.BlockCopy(content, 0, result, bom.Length, content.Length);
            return result;
        }

        private static Encoding StrictEncoding(int codepage) => Encoding.GetEncoding(codepage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }
}
