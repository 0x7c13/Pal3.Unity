// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle;

// Find stable longest contiguous matches (earliest old/new position on ties).
// Recurse only into unmatched intervals, then merge adjacent blocks. This keeps
// repeated blank lines deterministic without marking inherited members dirty.
internal static class ChangedLines
{
    internal readonly record struct Line(string Content, int Start, int End, bool HasNewline);

    private readonly record struct Match(int Old, int Current, int Count);

    internal static Line[] Split(byte[] source)
    {
        var lines = new List<Line>();
        int start = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] is not (10 or 13))
                continue;
            int contentEnd = i;
            if (source[i] == 13 && i + 1 < source.Length && source[i + 1] == 10)
                i++;
            int end = i + 1;
            lines.Add(new(Convert.ToHexString(source.AsSpan(start, contentEnd - start)), start, end, source[end - 1] == 10));
            start = end;
        }
        if (start < source.Length)
            lines.Add(new(Convert.ToHexString(source.AsSpan(start)), start, source.Length, false));
        return lines.ToArray();
    }

    internal static (HashSet<int> Dirty, HashSet<int> Boundaries) Compare(Line[] baseline, Line[] current, CancellationToken cancellation = default)
    {
        var positions = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (int i = 0; i < current.Length; i++)
        {
            if (!positions.TryGetValue(current[i].Content, out List<int>? indices))
                positions.Add(current[i].Content, indices = []);
            indices.Add(i);
        }
        var matches = new List<Match>();
        var pending = new Stack<(int OldStart, int OldEnd, int Start, int End)>();
        pending.Push((0, baseline.Length, 0, current.Length));
        while (pending.TryPop(out var area))
        {
            cancellation.ThrowIfCancellationRequested();
            var best = new Match(area.OldStart, area.Start, 0);
            var lengths = new Dictionary<int, int>();
            for (int old = area.OldStart; old < area.OldEnd; old++)
            {
                if (old % 64 == 0)
                    cancellation.ThrowIfCancellationRequested();
                var next = new Dictionary<int, int>();
                if (positions.TryGetValue(baseline[old].Content, out List<int>? indices))
                {
                    foreach (int index in indices)
                    {
                        if (index < area.Start)
                            continue;
                        if (index >= area.End)
                            break;
                        int count = lengths.GetValueOrDefault(index - 1) + 1;
                        next[index] = count;
                        if (count > best.Count)
                            best = new(old - count + 1, index - count + 1, count);
                    }
                }
                lengths = next;
            }
            if (best.Count == 0)
                continue;
            matches.Add(best);
            if (best.Old > area.OldStart && best.Current > area.Start)
                pending.Push((area.OldStart, best.Old, area.Start, best.Current));
            if (best.Old + best.Count < area.OldEnd && best.Current + best.Count < area.End)
                pending.Push((best.Old + best.Count, area.OldEnd, best.Current + best.Count, area.End));
        }
        var blocks = new List<Match>();
        foreach (Match match in matches.OrderBy(match => match.Old).ThenBy(match => match.Current))
        {
            if (blocks.Count > 0 && blocks[^1].Old + blocks[^1].Count == match.Old && blocks[^1].Current + blocks[^1].Count == match.Current)
                blocks[^1] = blocks[^1] with
                {
                    Count = blocks[^1].Count + match.Count
                };
            else
                blocks.Add(match);
        }
        blocks.Add(new(baseline.Length, current.Length, 0));
        var dirty = new HashSet<int>();
        var boundaries = new HashSet<int>();
        int oldEnd = 0, currentEnd = 0;
        foreach (Match match in blocks)
        {
            for (int line = currentEnd; line < match.Current; line++)
                dirty.Add(line);
            if (match.Current == currentEnd && match.Old > oldEnd)
                boundaries.Add(currentEnd);
            for (int line = 0; line < match.Count; line++)
                if (baseline[match.Old + line].HasNewline != current[match.Current + line].HasNewline)
                    dirty.Add(match.Current + line);
            oldEnd = match.Old + match.Count;
            currentEnd = match.Current + match.Count;
        }
        return (dirty, boundaries);
    }
}
