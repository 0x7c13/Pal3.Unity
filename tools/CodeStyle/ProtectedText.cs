// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle;

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal readonly record struct ByteRange(int Start, int End);

internal static class ProtectedText
{
    internal static readonly UTF8Encoding Utf8 = new(false, true);

    internal static IReadOnlyList<ByteRange> Capture(byte[] source)
    {
        string text = Utf8.GetString(source);
        var ranges = new List<ByteRange>();
        var inactive = new List<ByteRange>[2];
        int context = 0;
        foreach (string variant in new[] { "PAL3", "PAL3A" })
        {
            SyntaxNode root = CSharpSyntaxTree.ParseText(text,
                new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: [variant, "UNITY_EDITOR"])).GetRoot();
            inactive[context++] = root.DescendantTrivia(descendIntoTrivia: true)
                .Where(trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia))
                .Select(trivia => new ByteRange(trivia.SpanStart, trivia.Span.End)).ToList();
            foreach (SyntaxNode node in root.DescendantNodes())
            {
                switch (node)
                {
                    case InterpolatedStringExpressionSyntax interpolation:
                        ranges.Add(new(interpolation.StringStartToken.SpanStart, interpolation.StringStartToken.Span.End));
                        ranges.Add(new(interpolation.StringEndToken.SpanStart, interpolation.StringEndToken.Span.End));
                        break;
                    case InterpolatedStringTextSyntax part:
                        ranges.Add(new(part.SpanStart, part.Span.End));
                        break;
                    case InterpolationFormatClauseSyntax format:
                        ranges.Add(new(format.FormatStringToken.SpanStart, format.FormatStringToken.Span.End));
                        break;
                }
            }
            foreach (SyntaxToken token in root.DescendantTokens(descendIntoTrivia: true))
            {
                if (token.Kind() is SyntaxKind.StringLiteralToken or SyntaxKind.CharacterLiteralToken or
                    SyntaxKind.Utf8StringLiteralToken or SyntaxKind.SingleLineRawStringLiteralToken or
                    SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.Utf8SingleLineRawStringLiteralToken or
                    SyntaxKind.Utf8MultiLineRawStringLiteralToken)
                    ranges.Add(new(token.SpanStart, token.Span.End));
            }
        }
        // Text inactive in both formatting contexts is opaque as well: its
        // player-only literals must retain their exact original bytes.
        for (int left = 0, right = 0; left < inactive[0].Count && right < inactive[1].Count;)
        {
            ByteRange first = inactive[0][left], second = inactive[1][right];
            int start = Math.Max(first.Start, second.Start), end = Math.Min(first.End, second.End);
            if (start < end)
                ranges.Add(new(start, end));
            if (first.End <= second.End)
                left++;
            else
                right++;
        }
        var merged = new List<ByteRange>();
        foreach (ByteRange range in ranges.OrderBy(range => range.Start).ThenByDescending(range => range.End))
        {
            if (range.Start == range.End)
                continue;
            if (merged.Count > 0 && range.Start < merged[^1].End)
                merged[^1] = merged[^1] with
                {
                    End = Math.Max(merged[^1].End, range.End)
                };
            else
                merged.Add(range);
        }
        return merged.Select(range => new ByteRange(Utf8.GetByteCount(text.AsSpan(0, range.Start)),
            Utf8.GetByteCount(text.AsSpan(0, range.End)))).ToArray();
    }
}
