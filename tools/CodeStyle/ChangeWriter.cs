// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle;

internal sealed record FileChange
{
    internal string Path
    {
        get;
    }

    internal byte[] Original
    {
        get;
    }

    internal byte[] Formatted
    {
        get;
    }

    internal FileChange(string path, byte[] original, byte[] formatted)
    {
        // Repository.Root is canonicalized before reading inputs. Keep its
        // lexical child path: resolving a parent again after formatting would
        // hide a directory that was replaced with a link while the SDK ran.
        Path = System.IO.Path.GetFullPath(path);
        Original = original;
        Formatted = formatted;
    }
}

internal static class ChangeWriter
{
    internal static void Verify(IReadOnlyList<FileChange> changes)
    {
        foreach (FileChange change in changes)
            if (PathSafety.HasLink(change.Path) || !File.ReadAllBytes(change.Path).AsSpan().SequenceEqual(change.Original))
                throw new IOException($"Source changed while formatting: {change.Path}; no output was applied.");
    }

    internal static void Apply(IReadOnlyList<FileChange> changes, CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        Verify(changes);
        var staged = new List<(FileChange Change, string Temporary, string Backup)>();
        var applied = new List<(FileChange Change, string Backup)>();
        var retained = new HashSet<string>();
        try
        {
            foreach (FileChange change in changes.Where(change => !change.Original.AsSpan().SequenceEqual(change.Formatted)))
            {
                cancellation.ThrowIfCancellationRequested();
                if ((File.GetAttributes(change.Path) & FileAttributes.ReadOnly) != 0)
                    throw new UnauthorizedAccessException("Cannot format read-only source: " + change.Path);
                string temporary = change.Path + ".pal3-format-" + Guid.NewGuid().ToString("N") + ".tmp";
                string backup = temporary + ".bak";
                staged.Add((change, temporary, backup));
                // Preserve permissions when replacing an existing file.
                File.Copy(change.Path, temporary);
                File.WriteAllBytes(temporary, change.Formatted);
            }
            Verify(changes);
            foreach (var entry in staged)
            {
                cancellation.ThrowIfCancellationRequested();
                Verify([entry.Change]);
                File.Replace(entry.Temporary, entry.Change.Path, entry.Backup);
                applied.Add((entry.Change, entry.Backup));
            }
        }
        catch
        {
            foreach (var entry in applied.AsEnumerable().Reverse())
            {
                try
                {
                    if (PathSafety.HasLink(entry.Change.Path) ||
                        !File.ReadAllBytes(entry.Change.Path).AsSpan().SequenceEqual(entry.Change.Formatted))
                        throw new IOException("Source changed during formatting.");
                    File.Move(entry.Backup, entry.Change.Path, overwrite: true);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    retained.Add(entry.Backup);
                    Console.Error.WriteLine($"Could not restore {entry.Change.Path}. Backup path: {entry.Backup}. {error.Message}");
                }
            }
            throw;
        }
        finally
        {
            foreach (var entry in staged)
            {
                TryDelete(entry.Temporary);
                if (!retained.Contains(entry.Backup))
                    TryDelete(entry.Backup);
            }
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (!PathSafety.HasLink(path))
                File.Delete(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Could not remove formatter artifact {path}: {error.Message}");
        }
    }
}
