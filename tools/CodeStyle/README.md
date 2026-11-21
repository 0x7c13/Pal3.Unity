# C# code style

This .NET CLI formats the repository's owned C# sources under `Assets/Scripts/`
and `Assets/Tests/`. It calls the SDK's `dotnet format` with `.editorconfig` and
StyleCop 1.3.180 layout rules SA1107, SA1502, SA1507 and SA1516. Roslyn identifies
the C# text that must be preserved during incremental formatting. It checks both
PAL3 and PAL3A conditional branches without Unity or original game data. The
formatter and its tests need no Python; use the .NET SDK pinned in the
repository's `global.json`.

Run commands from the repository root. The first run restores the tool's .NET
dependencies. StyleCop is used only by the development tool and is not added to
the Unity project.

## Commands

Format current tracked changes and untracked owned C# files, then verify them:

```sh
dotnet run --project tools/CodeStyle --
dotnet run --project tools/CodeStyle -- --check
```

Check changes since the common ancestor of HEAD and a Git ref, including current
working tree changes, as the pull request workflow does:

```sh
dotnet run --project tools/CodeStyle -- --check --base origin/main
```

Format whole files by selecting explicit repository-relative files or directories:

```sh
dotnet run --project tools/CodeStyle -- Assets/Scripts/Pal3.Core
```

Check every owned C# source, or show detailed formatter diagnostics:

```sh
dotnet run --project tools/CodeStyle -- --check --all
dotnet run --project tools/CodeStyle -- --check --verbose
```

Run the formatter's .NET regression tests:

```sh
dotnet test tools/CodeStyle.Tests
```

After the test command restores both projects, verify their own source whitespace
with the same SDK checks used by CI:

```sh
dotnet format whitespace tools/CodeStyle --no-restore --verify-no-changes
dotnet format whitespace tools/CodeStyle.Tests --no-restore --verify-no-changes
```

## Scope and preservation

Default and `--base` runs apply formatting only to changed lines and deletion
boundaries. Git-detected renames use the original path for comparison, so a
pure rename preserves the file byte for byte. Newly added and untracked files,
explicit paths and `--all` use whole-file formatting. Third-party
sources outside the owned directories are excluded. `--base` and `--all` are
mutually exclusive, and neither can be combined with explicit paths.

Files with symbolic links in any component below the repository root are skipped
by default and with `--all`. Explicit paths through symbolic links are rejected.
The tool does not follow links into other source trees or replace links with
ordinary files.

Check mode formats temporary copies and exits unsuccessfully if formatting is
required; it does not rewrite source files. Formatting preserves each source
file's BOM. Incremental formatting also preserves the existing line ending
convention and protects C# literal contents and inactive conditional text.
Whole-file formatting uses the complete SDK result and the `.editorconfig`
line ending setting.

These checks cover whitespace and member layout. Run the relevant Unity tests
after formatting to verify compilation and behavior. ShaderLab/HLSL formatting
and rendering checks remain separate.
