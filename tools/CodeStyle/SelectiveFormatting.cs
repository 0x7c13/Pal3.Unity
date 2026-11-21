// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle;

using System.Buffers;
using System.Text;

internal static class SelectiveFormatting
{
    private readonly record struct Token(int Start, int End, string Key, bool Protected);

    internal static byte[] Select(byte[]? baseline, byte[] original, byte[] formatted, CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        IReadOnlyList<ByteRange> sourceRanges = ProtectedText.Capture(original);
        IReadOnlyList<ByteRange> targetRanges = ProtectedText.Capture(formatted);
        Token[] targetTokens = Tokens(formatted, targetRanges);
        if (baseline == null)
            return SelectOnce(null, original, formatted, sourceRanges, targetTokens, "", cancellation).Item1;
        string newline = Newline(original, sourceRanges);
        byte[] selected = original;
        for (int pass = 0; pass < ChangedLines.Split(original).Length + 2; pass++)
        {
            cancellation.ThrowIfCancellationRequested();
            (byte[] result, IReadOnlyList<ByteRange> nextRanges) = SelectOnce(baseline, selected, formatted,
                sourceRanges, targetTokens, newline, cancellation);
            if (selected.AsSpan().SequenceEqual(result))
                return result;
            selected = result;
            sourceRanges = nextRanges;
        }
        throw new InvalidOperationException("Changed-line formatting did not converge.");
    }

    private static (byte[], IReadOnlyList<ByteRange>) SelectOnce(byte[]? baseline, byte[] original, byte[] formatted,
        IReadOnlyList<ByteRange> ranges, Token[] targetTokens, string newline, CancellationToken cancellation)
    {
        ChangedLines.Line[] lines = ChangedLines.Split(original);
        var (dirty, boundaries) = baseline == null ? (new HashSet<int>(), new HashSet<int>())
            : ChangedLines.Compare(ChangedLines.Split(baseline), lines, cancellation);
        Token[] sourceTokens = Tokens(original, ranges);
        if (!sourceTokens.Select(token => token.Key).SequenceEqual(targetTokens.Select(token => token.Key), StringComparer.Ordinal))
            throw new InvalidOperationException("The formatter changed non-whitespace content; refusing a partial rewrite.");
        int[] starts = lines.Select(line => line.Start).Append(original.Length).ToArray();
        var output = new ArrayBufferWriter<byte>();
        var nextRanges = new List<ByteRange>();
        int sourceEnd = 0, targetEnd = 0;
        for (int index = 0; index <= sourceTokens.Length; index++)
        {
            if (index % 256 == 0)
                cancellation.ThrowIfCancellationRequested();
            bool last = index == sourceTokens.Length;
            int sourceStart = last ? original.Length : sourceTokens[index].Start;
            int targetStart = last ? formatted.Length : targetTokens[index].Start;
            ReadOnlySpan<byte> gap = original.AsSpan(sourceEnd, sourceStart - sourceEnd);
            ReadOnlySpan<byte> targetGap = formatted.AsSpan(targetEnd, targetStart - targetEnd);
            if (baseline == null)
                output.Write(targetGap);
            else
            {
                int firstLine = Math.Min(lines.Length - 1, UpperBound(starts, sourceEnd) - 1);
                int lastLine = Math.Min(lines.Length - 1, UpperBound(starts, sourceStart) - 1);
                bool changed = boundaries.Any(boundary => sourceEnd <= starts[boundary] && starts[boundary] <= sourceStart);
                for (int line = firstLine; line <= lastLine && !changed; line++)
                    changed = dirty.Contains(line);
                if (changed)
                    WriteChangedGap(output, gap, targetGap, newline, dirty.Contains(firstLine), dirty.Contains(lastLine));
                else
                    output.Write(gap);
            }
            if (last)
                break;
            Token source = sourceTokens[index], target = targetTokens[index];
            if (source.Protected)
                nextRanges.Add(new(output.WrittenCount, output.WrittenCount + source.End - source.Start));
            output.Write(original.AsSpan(source.Start, source.End - source.Start));
            sourceEnd = source.End;
            targetEnd = target.End;
        }
        return (output.WrittenSpan.ToArray(), nextRanges);
    }

    private static void WriteChangedGap(ArrayBufferWriter<byte> output, ReadOnlySpan<byte> original,
        ReadOnlySpan<byte> formatted, string newline, bool firstLineChanged, bool lastLineChanged)
    {
        string gapNewline = original.Contains((byte)10) ? Newline(original, []) : newline;
        int firstBreak = original.IndexOfAny((byte)'\r', (byte)'\n');
        int formattedBreak = formatted.IndexOfAny((byte)'\r', (byte)'\n');
        ReadOnlySpan<byte> indentation = [];
        if (firstBreak >= 0 && formattedBreak >= 0)
        {
            // A member gap can change even when the lines on its edges did not.
            // Keep trailing whitespace and indentation owned by those lines.
            if (!firstLineChanged)
            {
                output.Write(original[..firstBreak]);
                formatted = formatted[formattedBreak..];
            }
            if (!lastLineChanged)
            {
                indentation = original[(original.LastIndexOfAny((byte)'\r', (byte)'\n') + 1)..];
                formatted = formatted[..(formatted.LastIndexOfAny((byte)'\r', (byte)'\n') + 1)];
            }
        }
        string targetGap = ProtectedText.Utf8.GetString(formatted);
        output.Write(ProtectedText.Utf8.GetBytes(targetGap.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", gapNewline, StringComparison.Ordinal)));
        output.Write(indentation);
    }

    private static int UpperBound(int[] sorted, int value)
    {
        int start = 0, end = sorted.Length;
        while (start < end)
        {
            int middle = start + (end - start) / 2;
            if (sorted[middle] <= value)
                start = middle + 1;
            else
                end = middle;
        }
        return start;
    }

    private static string Newline(ReadOnlySpan<byte> source, IReadOnlyList<ByteRange> protectedRanges)
    {
        int start = 0, crlf = 0, lf = 0;
        foreach (ByteRange range in protectedRanges.Append(new(source.Length, source.Length)))
        {
            for (int i = start; i < range.Start; i++)
            {
                if (source[i] != 10)
                    continue;
                lf++;
                if (i > start && source[i - 1] == 13)
                    crlf++;
            }
            start = range.End;
        }
        return crlf > lf / 2d ? "\r\n" : "\n";
    }

    private static bool Word(byte value) => value is >= (byte)'a' and <= (byte)'z' or >= (byte)'A' and <= (byte)'Z' or
        >= (byte)'0' and <= (byte)'9' or (byte)'_';

    private static int WhitespaceLength(ReadOnlySpan<byte> source)
    {
        byte first = source[0];
        if (first < 128)
            return first is 9 or 10 or 11 or 12 or 13 or 32 ? 1 : 0;
        return Rune.DecodeFromUtf8(source, out Rune character, out int length) == OperationStatus.Done &&
            (character.Value is 0x85 or 0x2028 or 0x2029 ||
                Rune.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.SpaceSeparator) ? length : 0;
    }

    private static Token[] Tokens(byte[] source, IReadOnlyList<ByteRange> ranges)
    {
        var tokens = new List<Token>();
        int cursor = 0;
        for (int index = 0; index <= ranges.Count; index++)
        {
            int end = index < ranges.Count ? ranges[index].Start : source.Length;
            if (cursor > end)
                throw new InvalidOperationException("Invalid protected text span.");
            while (cursor < end)
            {
                int whitespace = WhitespaceLength(source.AsSpan(cursor, end - cursor));
                if (whitespace > 0)
                {
                    cursor += whitespace;
                    continue;
                }
                int start = cursor++;
                if (Word(source[start]))
                    while (cursor < end && Word(source[cursor]))
                        cursor++;
                tokens.Add(new(start, cursor, "code:" + Convert.ToHexString(source.AsSpan(start, cursor - start)), false));
            }
            if (index == ranges.Count)
                break;
            ByteRange range = ranges[index];
            if (range.Start >= range.End || range.End > source.Length)
                throw new InvalidOperationException("Invalid protected text span.");
            tokens.Add(new(range.Start, range.End, "literal:" + index, true));
            cursor = range.End;
        }
        return tokens.ToArray();
    }
}
