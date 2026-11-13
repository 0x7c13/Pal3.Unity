#!/usr/bin/env python3
# ------------------------------------------------------------------------
#  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
#  See LICENSE file in the project root for license information.
# ------------------------------------------------------------------------

"""Format owned C# changes with Roslyn and targeted StyleCop layout rules."""

import argparse
import os
from pathlib import Path
import subprocess
import tempfile
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
OWNED = ("Assets/Scripts/", "Assets/Tests/")
LAYOUT_RULES = ("SA1107", "SA1502", "SA1507", "SA1516")


def git_files(*args):
    result = subprocess.check_output(["git", "-C", str(ROOT), *args, "-z"])
    return {os.fsdecode(name) for name in result.split(b"\0") if name}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Fail instead of modifying files")
    parser.add_argument("--verbose", action="store_true", help="Show detailed Roslyn/MSBuild diagnostics")
    selection = parser.add_mutually_exclusive_group()
    selection.add_argument("--base", help="Include C# changes since this Git ref")
    selection.add_argument("--all", action="store_true", help="Include all owned C# sources")
    parser.add_argument("paths", nargs="*", help="Optional repo-relative files or directories")
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

    # A tiny format-only project avoids Unity-generated csproj files and needs
    # no Unity install, game data or runtime third-party assemblies. Parsing
    # both variants also checks whitespace inside #if PAL3 / #if PAL3A blocks.
    workspace = ROOT.parent / "workspace"
    temp_root = workspace / "Tooling" / "CodeStyle" if workspace.is_dir() else None
    if temp_root is not None:
        temp_root.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="pal3-format-", dir=temp_root) as temporary:
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
            ET.SubElement(items, "Compile", Include=str(ROOT / name))
        project_path = Path(temporary) / "Formatting.csproj"
        ET.ElementTree(project).write(project_path, encoding="utf-8", xml_declaration=True)
        print(f"{'Checking' if args.check else 'Formatting'} {len(files)} C# files (PAL3 and PAL3A).", flush=True)
        subprocess.run(["dotnet", "restore", str(project_path), "--verbosity", "quiet"], cwd=ROOT, check=True)
        failed = False
        for variant in ("PAL3", "PAL3A"):
            for subcommand, extra in (("analyzers", ["--diagnostics", *LAYOUT_RULES]), ("whitespace", [])):
                verbosity = "diagnostic" if args.verbose else "minimal"
                command = ["dotnet", "format", subcommand, str(project_path), "--no-restore", "--verbosity", verbosity, *extra]
                if args.check:
                    command.append("--verify-no-changes")
                result = subprocess.run(command, cwd=ROOT, env={**os.environ, "PAL3_FORMAT_VARIANT": variant})
                failed |= result.returncode != 0
        return 1 if failed else 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, subprocess.CalledProcessError) as error:
        raise SystemExit(str(error)) from error
