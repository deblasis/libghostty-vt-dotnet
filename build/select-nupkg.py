#!/usr/bin/env python3
"""Select the package a validation run just produced, by exact version.

`ls -t artifacts/*.nupkg | head -n 1` picks by modification time, while the
package consumer resolves by version. Two packages in the feed can therefore
disagree, and a good package can certify a bad one. This selects the single
file named after the version the caller packed, and fails if it is not there.

Usage: select-nupkg.py --directory DIR --version VERSION [--package-id ID]
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path


class SelectionError(Exception):
    pass


def select(directory: Path, package_id: str, version: str) -> Path:
    if not directory.is_dir():
        raise SelectionError(f"package directory not found: {directory}")

    # `dotnet pack -p:Version=1.2.3+abc` writes `…1.2.3.nupkg`: NuGet drops the
    # build metadata from the file name while the nuspec keeps it. Matching the
    # raw version would report the package as missing while it sits in the
    # directory.
    name = f"{package_id}.{version.split('+', 1)[0]}.nupkg"
    path = directory / name
    if path.is_file():
        return path

    available = sorted(entry.name for entry in directory.glob("*.nupkg"))
    detail = ", ".join(available) if available else "none"
    raise SelectionError(
        f"no package for version {version} in {directory} (found: {detail})"
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--package-id", default="DeBlasis.GhosttyVt")
    args = parser.parse_args()

    try:
        path = select(args.directory, args.package_id, args.version)
    except SelectionError as error:
        print(f"::error::{error}", file=sys.stderr)
        return 1

    print(path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
