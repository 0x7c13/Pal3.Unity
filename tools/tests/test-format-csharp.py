#!/usr/bin/env python3
# ------------------------------------------------------------------------
#  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
#  See LICENSE file in the project root for license information.
# ------------------------------------------------------------------------

"""Exercise the actual formatter with missing member gaps in both variants."""

import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile


ROOT = Path(__file__).resolve().parents[2]


def run(command, cwd, success=True):
    result = subprocess.run(command, cwd=cwd, capture_output=True, text=True, timeout=180,
                            env={**os.environ, "TMPDIR": str(cwd)})
    if (result.returncode == 0) != success:
        raise AssertionError(f"Unexpected exit {result.returncode}: {command}\n{result.stdout}\n{result.stderr}")
    return result.stdout + result.stderr


def main():
    workspace = ROOT.parent / "workspace"
    temporary_root = workspace / "Tooling" / "CodeStyle" if workspace.is_dir() else None
    if temporary_root is not None:
        temporary_root.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="pal3-format-test-", dir=temporary_root) as directory:
        fixture = Path(directory)
        (fixture / "tools").mkdir()
        for name in (".editorconfig", "global.json", "tools/format-csharp.py"):
            shutil.copy2(ROOT / name, fixture / name)
        source = fixture / "Assets/Scripts/Effect layout.cs"
        source.parent.mkdir(parents=True)
        good = ("internal sealed class Layout\n{\n#if PAL3\n"
                "    public int AOne() => 1;\n\n    public int ATwo() => 2;\n"
                "#elif PAL3A\n    public int BOne() => 1;\n\n    public int BTwo() => 2;\n#endif\n}\n")
        source.write_bytes(good.replace("\n", "\r\n").encode())
        plugin = fixture / "Assets/Plugins/Keep.cs"
        plugin.parent.mkdir()
        plugin.write_bytes(b"class Plugin{void One(){}void Two(){}}")
        plugin_before = plugin.read_bytes()
        run(["git", "init", "--quiet"], fixture)
        run(["git", "add", "."], fixture)
        run(["git", "-c", "user.name=Format fixture", "-c", "user.email=fixture@localhost",
             "-c", "commit.gpgsign=false", "commit", "--quiet", "-m", "Fixture baseline"], fixture)
        base = run(["git", "rev-parse", "HEAD"], fixture).strip()
        command = [sys.executable, "tools/format-csharp.py"]
        source.write_bytes(good.replace(";\n\n", ";\n").replace("\n", "\r\n").encode())
        before = source.read_bytes()
        output = run([*command, "--check"], fixture, success=False)
        assert "SA1516" in output, output
        assert source.read_bytes() == before, "Check mode modified the source"
        run(command, fixture)
        fixed = source.read_bytes()
        assert b"AOne() => 1;\r\n\r\n" in fixed
        assert b"BOne() => 1;\r\n\r\n" in fixed
        run([*command, "--check", "--all"], fixture)
        assert source.read_bytes() == fixed, "Formatting is not stable"
        # A violation only in PAL3A must fail the same --base mode as PR CI.
        source.write_bytes(fixed.replace(b"BOne() => 1;\r\n\r\n", b"BOne() => 1;\r\n"))
        output = run([*command, "--check", "--base", base], fixture, success=False)
        assert "SA1516" in output, output
        assert plugin.read_bytes() == plugin_before, "Third-party source was modified"
    print("Passed: missing member gaps fail, both variants auto-fix, check is read-only and stable, --base checks PAL3A, plugins stay untouched.")


if __name__ == "__main__":
    main()
