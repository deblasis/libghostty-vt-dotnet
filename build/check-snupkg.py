#!/usr/bin/env python3
"""Reject a symbol package that nuget.org cannot index.

nuget.org's symbol server accepts Portable PDBs, which begin with the `BSJB`
signature. The native Windows PDBs shipped beside the DLLs use the MSF format
(`Microsoft C/C++ MSF 7.00`) and are rejected. That rejection happens
asynchronously, after `dotnet nuget push` has already exited 0, so the failure
arrives as owner mail instead of a red job.

This opens the produced `.snupkg` and requires:

- the managed Portable PDB under `lib/`, starting with `BSJB`
- no native payload under `runtimes/` at all

Usage: check-snupkg.py <snupkg>
"""

from __future__ import annotations

import sys
import zipfile
from pathlib import Path

PORTABLE_PDB_MAGIC = b"BSJB"


class SymbolPackageError(Exception):
    pass


def check(path: Path) -> list[str]:
    with zipfile.ZipFile(path) as package:
        names = [info.filename for info in package.infolist()]

    native = sorted(name for name in names if name.startswith("runtimes/"))
    if native:
        raise SymbolPackageError(
            "symbol package contains native payload, which nuget.org cannot "
            "index: " + ", ".join(native)
        )

    managed = sorted(
        name
        for name in names
        if name.startswith("lib/") and name.lower().endswith(".pdb")
    )
    if not managed:
        raise SymbolPackageError(
            "symbol package has no managed PDB under lib/; there is nothing "
            "for the symbol server to index"
        )

    for name in managed:
        with zipfile.ZipFile(path) as package:
            data = package.read(name)
        if not data.startswith(PORTABLE_PDB_MAGIC):
            raise SymbolPackageError(
                f"{name} is not a Portable PDB (no {PORTABLE_PDB_MAGIC!r} "
                "signature); nuget.org would reject it"
            )

    return managed


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: check-snupkg.py <snupkg>", file=sys.stderr)
        return 2

    path = Path(sys.argv[1])
    if not path.is_file():
        print(f"::error::symbol package not found: {path}", file=sys.stderr)
        return 1

    try:
        managed = check(path)
    except (
        OSError,
        RuntimeError,
        NotImplementedError,
        zipfile.BadZipFile,
        SymbolPackageError,
    ) as error:
        print(f"::error::{path}: {error}", file=sys.stderr)
        return 1

    print(f"{path}: Portable symbol package with {', '.join(managed)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
