#!/usr/bin/env python3
# ------------------------------------------------------------------------
#  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
#  See LICENSE file in the project root for license information.
# ------------------------------------------------------------------------

"""Format owned C# changes with Roslyn and targeted StyleCop layout rules."""

import argparse
import difflib
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
OWNED = ("Assets/Scripts/", "Assets/Tests/")
LAYOUT_RULES = ("SA1107", "SA1502", "SA1507", "SA1516")


def git_files(*args):
    result = subprocess.check_output(["git", "-C", str(ROOT), *args, "-z"])
    return {os.fsdecode(name) for name in result.split(b"\0") if name}


def select_formatting(baseline, original, formatted):
    """Keep formatter edits touching changed lines or a deletion boundary."""
    if baseline is None:
        return formatted
    # Existing files may predate the current CRLF policy. Keep their line-ending
    # convention instead of converting the whole file or introducing mixed EOLs.
    newline = b"\r\n" if original.count(b"\r\n") > original.count(b"\n") / 2 else b"\n"
    formatted = formatted.replace(b"\r\n", b"\n").replace(b"\n", newline)
    before = original.splitlines(keepends=True)
    after = formatted.splitlines(keepends=True)
    content = lambda lines: [line.rstrip(b"\r\n") for line in lines]
    old_source = baseline.splitlines(keepends=True)
    old_lines = content(old_source)
    current_lines = content(before)
    dirty = set()
    boundaries = set()
    for tag, old_start, _, start, end in difflib.SequenceMatcher(
            None, old_lines, current_lines, autojunk=False).get_opcodes():
        if tag == "equal":
            dirty.update(index for index in range(start, end)
                         if before[index].endswith(b"\n") !=
                         old_source[old_start + index - start].endswith(b"\n"))
        else:
            dirty.update(range(start, end))
            if start == end:
                boundaries.add(start)
    result = []
    # Match surviving lines independently of spacing so an inserted member gap
    # does not pull an adjacent, inherited indentation fix into the same edit.
    tokens = lambda lines: [b"".join(line.split()) for line in lines]
    for tag, start, end, target_start, target_end in difflib.SequenceMatcher(
            None, tokens(before), tokens(after), autojunk=False).get_opcodes():
        if tag == "equal":
            result.extend(after[target_start + index - start] if index in dirty else before[index]
                          for index in range(start, end))
            continue
        touches_change = any(index in dirty for index in range(start, end))
        touches_change |= any(start <= boundary < end or start == end == boundary
                              for boundary in boundaries)
        if start == end:
            touches_change |= start in dirty or start - 1 in dirty
        result.extend(after[target_start:target_end] if touches_change else before[start:end])
    return b"".join(result)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Fail instead of modifying files")
    parser.add_argument("--verbose", action="store_true", help="Show detailed Roslyn/MSBuild diagnostics")
    selection = parser.add_mutually_exclusive_group()
    selection.add_argument("--base", help="Include C# changes since this Git ref")
    selection.add_argument("--all", action="store_true", help="Include all owned C# sources")
    parser.add_argument("paths", nargs="*", help="Format whole files at these repo-relative paths")
    args = parser.parse_args()
    if args.paths and (args.base or args.all):
        parser.error("Explicit paths cannot be combined with --base or --all")

    if args.all or args.paths:
        files = git_files("ls-files", "--cached", "--others", "--exclude-standard")
    else:
        files = git_files("diff", "--name-only", "--diff-filter=ACMR", args.base or "HEAD")
        files |= git_files("ls-files", "--others", "--exclude-standard")
    files = sorted(name for name in files
                   if name.startswith(OWNED) and name.endswith(".cs") and (ROOT / name).is_file()
                   and (ROOT / name).resolve().is_relative_to(ROOT))
    if args.paths:
        requested = [(ROOT / path).resolve() for path in args.paths]
        if any(not path.is_relative_to(ROOT) for path in requested):
            parser.error("Paths must stay inside the repository")
        files = [name for name in files if any(
            (ROOT / name).resolve() == path or path in (ROOT / name).resolve().parents
            for path in requested)]
    if not files:
        print("No owned C# changes to format.")
        return 0

    originals = {name: (ROOT / name).read_bytes() for name in files}
    baselines = {}
    if not args.all and not args.paths:
        for name in files:
            result = subprocess.run(["git", "-C", str(ROOT), "show", f"{args.base or 'HEAD'}:{name}"],
                                    capture_output=True)
            baselines[name] = result.stdout if result.returncode == 0 else None

    # A tiny format-only project avoids Unity-generated csproj files and needs
    # no Unity install, game data or runtime third-party assemblies. Parsing
    # both variants also checks whitespace inside #if PAL3 / #if PAL3A blocks.
    workspace = ROOT.parent / "workspace"
    temp_root = workspace / "Tooling" / "CodeStyle" if workspace.is_dir() else None
    if temp_root is not None:
        temp_root.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="pal3-format-", dir=temp_root) as temporary:
        # Format copies so --check is read-only and failures cannot leave partial edits.
        source_root = Path(temporary) / "sources"
        source_root.mkdir()
        shutil.copy2(ROOT / ".editorconfig", source_root / ".editorconfig")
        for name, source in originals.items():
            path = source_root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(source)
        project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
        properties = ET.SubElement(project, "PropertyGroup")
        for name, value in {
            "TargetFramework": "net10.0",
            "EnableDefaultCompileItems": "false",
            "GenerateAssemblyInfo": "false",
            "NuGetAudit": "false",
            "DefineConstants": "$(PAL3_FORMAT_VARIANT);UNITY_EDITOR",
        }.items():
            ET.SubElement(properties, name).text = value
        items = ET.SubElement(project, "ItemGroup")
        ET.SubElement(items, "PackageReference", Include="StyleCop.Analyzers", Version="1.3.180", PrivateAssets="all")
        # The SDK discovers .editorconfig from each source path automatically.
        # Adding it explicitly would register it twice in the Roslyn workspace.
        for name in files:
            ET.SubElement(items, "Compile", Include=str(source_root / name))
        project_path = Path(temporary) / "Formatting.csproj"
        ET.ElementTree(project).write(project_path, encoding="utf-8", xml_declaration=True)
        print(f"{'Checking' if args.check else 'Formatting'} {len(files)} C# files (PAL3 and PAL3A).", flush=True)
        subprocess.run(["dotnet", "restore", str(project_path), "--verbosity", "quiet"], cwd=ROOT, check=True)
        failed = False
        for variant in ("PAL3", "PAL3A"):
            for subcommand, extra in (("analyzers", ["--diagnostics", *LAYOUT_RULES]), ("whitespace", [])):
                verbosity = "diagnostic" if args.verbose else "minimal"
                command = ["dotnet", "format", subcommand, str(project_path), "--no-restore", "--verbosity", verbosity, *extra]
                result = subprocess.run(command, cwd=ROOT, env={**os.environ, "PAL3_FORMAT_VARIANT": variant})
                failed |= result.returncode != 0
        if failed:
            return 1
        changed = False
        for name, original in originals.items():
            formatted = (source_root / name).read_bytes()
            selected = select_formatting(baselines.get(name), original, formatted)
            if selected == original:
                continue
            changed = True
            if args.check:
                print(f"Formatting required: {name}")
            else:
                (ROOT / name).write_bytes(selected)
        return 1 if args.check and changed else 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, subprocess.CalledProcessError) as error:
        raise SystemExit(str(error)) from error
