#!/usr/bin/env python3
"""Prove select-nupkg.py follows the packed version, not file timestamps.

1. two packages present, selecting the older version: must return that file
2. selecting the newer version: must return that file
3. a version with build metadata selects the file NuGet wrote without it
4. selecting a version that was never packed: must fail
5. a missing directory: must fail

Usage: selftest-select-nupkg.py
"""

from __future__ import annotations

import subprocess
import sys
import tempfile
from pathlib import Path

CHECKER = Path(__file__).with_name("select-nupkg.py")
PACKAGE_ID = "DeBlasis.GhosttyVt"


class SelftestError(Exception):
    pass


def run(directory: Path, version: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [
            sys.executable,
            str(CHECKER),
            "--directory",
            str(directory),
            "--version",
            version,
        ],
        capture_output=True,
        text=True,
    )


def expect(
    name: str,
    result: subprocess.CompletedProcess[str],
    accepted: bool,
    expected_name: str | None = None,
) -> None:
    passed = result.returncode == 0
    if passed != accepted:
        outcome = "pass" if passed else "fail"
        wanted = "pass" if accepted else "fail"
        raise SelftestError(
            f"{name}: selector did {outcome}, expected {wanted}\n"
            f"stdout: {result.stdout.strip()}\n"
            f"stderr: {result.stderr.strip()}"
        )
    if expected_name is not None and result.stdout.strip() != str(expected_name):
        raise SelftestError(
            f"{name}: selector returned {result.stdout.strip()!r}, "
            f"expected {str(expected_name)!r}"
        )


def main() -> int:
    try:
        with tempfile.TemporaryDirectory(prefix="select-nupkg-selftest-") as directory:
            root = Path(directory)
            older = root / f"{PACKAGE_ID}.0.0.1-stale.nupkg"
            newer = root / f"{PACKAGE_ID}.0.0.2-current.nupkg"
            older.write_bytes(b"older")
            newer.write_bytes(b"newer")
            # `dotnet pack` drops build metadata from the file name but the
            # nuspec keeps it, so the same version has two spellings.
            metadata = root / f"{PACKAGE_ID}.1.2.3.nupkg"
            metadata.write_bytes(b"metadata")
            # Make the older file the most recently written one, so any
            # timestamp-based selection would return the wrong package.
            newer.touch()
            older.touch()

            expect("older packed version", run(root, "0.0.1-stale"), True, older)
            expect("newer packed version", run(root, "0.0.2-current"), True, newer)
            expect("packed version with build metadata", run(root, "1.2.3+abc"), True, metadata)
            expect("version that was never packed", run(root, "0.0.3-missing"), False)
            expect("missing directory", run(root / "absent", "0.0.1-stale"), False)
    except (OSError, SelftestError) as error:
        print(f"::error::{error}", file=sys.stderr)
        return 1

    print("Package selection self-test passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
