// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tools.CodeStyle;

internal sealed record Options(bool Check, bool Verbose, bool All, string? Base, string[] Paths, bool Help)
{
    internal const string Usage = """
        Format owned C# changes with the pinned .NET SDK and StyleCop layout rules.

        dotnet run --project tools/CodeStyle -- [--check] [--verbose] [--base REF | --all] [PATH ...]

        --check      Report required formatting without changing source files.
        --base REF   Include changes since the merge base of HEAD and this Git commit.
        --all        Format all owned C# files.
        PATH         Format whole files or folders under Assets/Scripts or Assets/Tests.
        --verbose    Show detailed Roslyn/MSBuild diagnostics.
        --help       Show this help.

        With no selection flags, format current tracked/untracked C# changes.
        Explicit paths cannot be combined with --base or --all.
        Both PAL3 and PAL3A branches are formatted. Unity and game data are not required.
        """;

    internal static Options Parse(string[] arguments)
    {
        bool check = false, verbose = false, all = false, help = false, pathsOnly = false;
        string? baseline = null;
        var paths = new List<string>();
        for (int index = 0; index < arguments.Length; index++)
        {
            string argument = arguments[index];
            if (pathsOnly)
            {
                paths.Add(argument);
                continue;
            }
            switch (argument)
            {
                case "--":
                    pathsOnly = true;
                    break;
                case "--check":
                    check = true;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--all":
                    all = true;
                    break;
                case "--help" or "-h":
                    help = true;
                    break;
                case "--base":
                    if (baseline != null || ++index >= arguments.Length ||
                        string.IsNullOrWhiteSpace(arguments[index]) || arguments[index].StartsWith('-'))
                        throw new ArgumentException("--base requires one Git commit.");
                    baseline = arguments[index];
                    break;
                default:
                    if (argument.StartsWith("-", StringComparison.Ordinal))
                        throw new ArgumentException("Unknown option: " + argument);
                    paths.Add(argument);
                    break;
            }
        }
        if ((all && baseline != null) || (paths.Count > 0 && (all || baseline != null)))
            throw new ArgumentException("Explicit paths, --base and --all cannot be combined.");
        return new(check, verbose, all, baseline, paths.ToArray(), help);
    }
}
