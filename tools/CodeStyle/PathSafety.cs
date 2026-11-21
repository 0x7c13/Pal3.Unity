// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle;

using System.Runtime.InteropServices;

internal static class PathSafety
{
    // Darwin's _PC_CASE_SENSITIVE reports the volume semantics for this checkout.
    private const int MacCaseSensitive = 11;

    [DllImport("libSystem.B.dylib", EntryPoint = "pathconf")]
    private static extern nint MacPathConf([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int name);

    internal static StringComparison Comparison(string root) =>
        OperatingSystem.IsWindows() || (OperatingSystem.IsMacOS() && MacPathConf(root, MacCaseSensitive) == 0)
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    internal static string Physical(string path)
    {
        string absolute = Path.GetFullPath(path);
        string current = Path.GetPathRoot(absolute)!;
        foreach (string part in absolute[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            FileSystemInfo entry = Entry(current);
            if (entry.LinkTarget != null)
                current = entry.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? throw new IOException("Unresolved symbolic link: " + current);
        }
        return Path.TrimEndingDirectorySeparator(current);
    }

    internal static bool HasLink(string path)
    {
        string absolute = Path.GetFullPath(path);
        string current = Path.GetPathRoot(absolute)!;
        foreach (string part in absolute[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            if (Entry(current).LinkTarget != null)
                return true;
        }
        return false;
    }

    private static FileSystemInfo Entry(string path) => Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path);
}
