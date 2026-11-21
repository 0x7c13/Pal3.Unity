// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle;

internal readonly record struct SourceFile(string Name, string? BaselineName);

internal sealed class Repository(string root, CancellationToken cancellation)
{
    internal string Root { get; } = PathSafety.Physical(root);

    private readonly StringComparison _pathComparison = PathSafety.Comparison(root);

    internal static async Task<Repository> Open(CancellationToken cancellation)
    {
        byte[] output = await Processes.Git(Environment.CurrentDirectory, cancellation, "rev-parse", "--show-toplevel");
        return new(Path.GetFullPath(ProtectedText.Utf8.GetString(output).TrimEnd('\r', '\n')), cancellation);
    }

    private async Task<string[]> GitFields(params string[] arguments)
    {
        byte[] output = await Processes.Git(Root, cancellation, arguments);
        return ProtectedText.Utf8.GetString(output).Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    internal string FullPath(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path, Root));

    internal async Task<string> ComparisonBase(string? reference)
    {
        byte[] output = await Processes.Git(Root, cancellation, "merge-base", "--", "HEAD", reference ?? "HEAD");
        return ProtectedText.Utf8.GetString(output).TrimEnd('\r', '\n');
    }

    private bool AtOrBelow(string path, string directory)
    {
        string prefix = Path.EndsInDirectorySeparator(directory) ? directory : directory + Path.DirectorySeparatorChar;
        return path.Equals(directory, _pathComparison) || path.StartsWith(prefix, _pathComparison);
    }

    private string ResolveRequestedPath(string path)
    {
        string workingDirectory = Environment.CurrentDirectory;
        string separators = OperatingSystem.IsWindows() ? "/\\" : "/";
        // An explicit link followed by ".." can resolve to a different file
        // on disk. Check the directory being removed before GetFullPath erases it.
        foreach (Range segment in path.AsSpan().SplitAny(separators))
        {
            if (path.AsSpan()[segment].SequenceEqual(".."))
            {
                int start = segment.Start.Value;
                string directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(start == 0 ? "." : path[..start], workingDirectory));
                if (new DirectoryInfo(directory).LinkTarget != null)
                    throw new ArgumentException("Symbolic-link paths are not formatted.");
            }
        }
        string absolute = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path, workingDirectory));
        if (AtOrBelow(absolute, Root))
            return absolute;
        // Canonicalize an alias above the repository (e.g. macOS /tmp). Leave
        // the suffix lexical so the link check below can still reject owned-file links.
        var prefixes = new Stack<DirectoryInfo>();
        for (DirectoryInfo? prefix = new(absolute); prefix != null; prefix = prefix.Parent)
            prefixes.Push(prefix);
        foreach (DirectoryInfo prefix in prefixes)
        {
            if (prefix.Exists && prefix.LinkTarget == null && prefix.Name.Equals(Path.GetFileName(Root), _pathComparison) &&
                PathSafety.Physical(prefix.FullName).Equals(Root, _pathComparison))
                return FullPath(Path.GetRelativePath(prefix.FullName, absolute));
        }
        return absolute;
    }

    internal async Task<SourceFile[]> Select(string[] requestedPaths, string? reference)
    {
        string[] paths = reference == null
            ? await GitFields("ls-files", "--cached", "--others", "--exclude-standard", "-z")
            : await GitFields("ls-files", "--others", "--exclude-standard", "-z");
        var files = paths.Distinct(StringComparer.Ordinal).Select(path => new SourceFile(path, null)).ToList();
        if (reference != null)
        {
            string[] fields = await GitFields("diff", "--name-status", "--find-renames", "--diff-filter=ACMR", "-z", reference, "--");
            for (int index = 0; index < fields.Length;)
            {
                char status = fields[index++][0];
                string firstPath = fields[index++];
                string name = status is 'R' or 'C' ? fields[index++] : firstPath;
                string? baselineName = status is 'M' or 'R' ? firstPath : null;
                files.Add(new(name, baselineName));
            }
        }
        string[] requested = requestedPaths.Select(ResolveRequestedPath).ToArray();
        if (requested.Any(path => !AtOrBelow(path, Root)))
            throw new ArgumentException("Paths must stay inside the repository.");
        if (requested.Any(PathSafety.HasLink))
            throw new ArgumentException("Symbolic-link paths are not formatted.");
        return files.Where(file =>
        {
            string name = file.Name;
            bool owned = name.StartsWith("Assets/Scripts/", StringComparison.Ordinal) || name.StartsWith("Assets/Tests/", StringComparison.Ordinal);
            string path = FullPath(name);
            return owned && name.EndsWith(".cs", StringComparison.Ordinal) && File.Exists(path) && !PathSafety.HasLink(path) &&
                (requested.Length == 0 || requested.Any(request => AtOrBelow(path, request)));
        }).OrderBy(file => file.Name, StringComparer.Ordinal).ToArray();
    }

    internal Task<byte[]> Baseline(string reference, string name) =>
        Processes.Git(Root, cancellation, "show", reference + ":" + name);
}
