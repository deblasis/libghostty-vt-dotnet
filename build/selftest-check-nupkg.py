#!/usr/bin/env python3
"""Prove check-nupkg.py rejects incomplete or mismatched Windows packages.

Builds throwaway .nupkg files from real DLL/PDB fixtures and asserts:

1. no natives at all: must fail
2. win-x64 pair only: must fail (win-arm64 is required by default)
3. both pairs: must pass
4. both pairs, but the win-arm64 PDB replaced by the win-x64 PDB: must fail
5. both pairs, but the win-arm64 DLL replaced by the win-x64 DLL: must fail
6. win-x64 pair only, with win-arm64 allowed to be missing: must pass
7. win-arm64 DLL only, with win-arm64 allowed to be missing: must fail
   (a skipped RID must not hide a partial or wrong-architecture tree)

Usage: selftest-check-nupkg.py --x64-dir DIR --arm64-dir DIR
"""

from __future__ import annotations

import argparse
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

CHECKER = Path(__file__).with_name("check-nupkg.py")
DLL = "ghostty-vt.dll"
PDB = "ghostty-vt.pdb"


class SelftestError(Exception):
    pass


def pair(directory: Path) -> dict[str, bytes]:
    dll = directory / DLL
    pdb = directory / PDB
    if not dll.is_file() or not pdb.is_file():
        raise SelftestError(f"{directory} must contain {DLL} and {PDB}")
    return {DLL: dll.read_bytes(), PDB: pdb.read_bytes()}


def write_package(path: Path, natives: dict[str, dict[str, bytes]]) -> None:
    with zipfile.ZipFile(path, "w") as package:
        for rid, files in natives.items():
            for name, data in files.items():
                package.writestr(f"runtimes/{rid}/native/{name}", data)


def run(package: Path, *args: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(CHECKER), str(package), *args],
        capture_output=True,
        text=True,
    )


def expect(name: str, result: subprocess.CompletedProcess[str], accepted: bool) -> None:
    passed = result.returncode == 0
    if passed == accepted:
        return
    outcome = "pass" if passed else "fail"
    wanted = "pass" if accepted else "fail"
    raise SelftestError(
        f"{name}: checker did {outcome}, expected {wanted}\n"
        f"stdout: {result.stdout.strip()}\n"
        f"stderr: {result.stderr.strip()}"
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--x64-dir", type=Path, required=True)
    parser.add_argument("--arm64-dir", type=Path, required=True)
    args = parser.parse_args()

    try:
        x64 = pair(args.x64_dir)
        arm64 = pair(args.arm64_dir)
    except (OSError, SelftestError) as error:
        print(f"::error::{error}", file=sys.stderr)
        return 1

    try:
        with tempfile.TemporaryDirectory(prefix="check-nupkg-selftest-") as directory:
            root = Path(directory)

            empty = root / "empty.nupkg"
            write_package(empty, {})
            expect("no natives", run(empty), accepted=False)

            x64_only = root / "x64-only.nupkg"
            write_package(x64_only, {"win-x64": x64})
            expect("missing win-arm64", run(x64_only), accepted=False)

            complete = root / "complete.nupkg"
            write_package(complete, {"win-x64": x64, "win-arm64": arm64})
            expect("complete package", run(complete), accepted=True)

            wrong_pdb = root / "wrong-pdb.nupkg"
            write_package(
                wrong_pdb,
                {
                    "win-x64": x64,
                    "win-arm64": {DLL: arm64[DLL], PDB: x64[PDB]},
                },
            )
            expect("foreign win-arm64 PDB", run(wrong_pdb), accepted=False)

            wrong_dll = root / "wrong-dll.nupkg"
            write_package(
                wrong_dll,
                {
                    "win-x64": x64,
                    "win-arm64": {DLL: x64[DLL], PDB: arm64[PDB]},
                },
            )
            expect("wrong win-arm64 machine", run(wrong_dll), accepted=False)

            optional = ("--allow-missing-rid", "win-arm64")
            expect(
                "arm64 absent and allowed",
                run(x64_only, *optional),
                accepted=True,
            )

            partial = root / "partial-arm64.nupkg"
            write_package(partial, {"win-x64": x64, "win-arm64": {DLL: arm64[DLL]}})
            expect(
                "arm64 DLL without its PDB, even when optional",
                run(partial, *optional),
                accepted=False,
            )
    except (OSError, SelftestError) as error:
        print(f"::error::{error}", file=sys.stderr)
        return 1

    print("Windows package content self-test passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
