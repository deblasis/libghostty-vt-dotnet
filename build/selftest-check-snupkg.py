#!/usr/bin/env python3
"""Prove check-snupkg.py rejects symbol packages nuget.org cannot index.

Builds throwaway `.snupkg` files and asserts:

1. a managed Portable PDB under lib/ only: must pass
2. the same package plus a native PDB under runtimes/: must fail
3. the same package plus a non-PDB native file under runtimes/: must fail
4. a package with no PDB at all: must fail
5. a lib/ PDB without the `BSJB` signature: must fail
6. an encrypted member: must fail with an annotated error, not a traceback
7. a backslash-separated native PDB: must fail (it is a path separator on Windows)
8. an uppercase `RUNTIMES/` path: must fail
9. an absolute native path: must fail
10. a drive-prefixed native path: must fail
11. a `..` segment: must fail
12. a repeated entry name: must fail (zipfile reads the last copy, NuGet pushes the first)

Usage: selftest-check-snupkg.py
"""

from __future__ import annotations

import struct
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

CHECKER = Path(__file__).with_name("check-snupkg.py")
MANAGED = "lib/net9.0/Ghostty.Vt.pdb"
NATIVE = "runtimes/win-x64/native/ghostty-vt.pdb"
PORTABLE = b"BSJB" + b"\0" * 60


class SelftestError(Exception):
    pass


def write_package(path: Path, entries: dict[str, bytes]) -> None:
    with zipfile.ZipFile(path, "w") as package:
        for name, data in entries.items():
            package.writestr(name, data)


def encrypt(path: Path) -> None:
    data = bytearray(path.read_bytes())
    for magic, offset in ((b"PK\x03\x04", 6), (b"PK\x01\x02", 8)):
        position = 0
        while (position := data.find(magic, position)) >= 0:
            field = struct.unpack_from("<H", data, position + offset)[0]
            struct.pack_into("<H", data, position + offset, field | 1)
            position += 4
    path.write_bytes(data)


def run(package: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(CHECKER), str(package)],
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
    try:
        with tempfile.TemporaryDirectory(prefix="check-snupkg-selftest-") as directory:
            root = Path(directory)

            managed = root / "managed.snupkg"
            write_package(managed, {MANAGED: PORTABLE})
            expect("managed Portable PDB only", run(managed), accepted=True)

            native_pdb = root / "native-pdb.snupkg"
            write_package(native_pdb, {MANAGED: PORTABLE, NATIVE: b"Microsoft C/C++ MSF 7.00\r\n\x1aDS\0\0\0"})
            expect("native PDB under runtimes/", run(native_pdb), accepted=False)

            for case, entry in (
                ("backslash native PDB", "runtimes\\win-x64\\native\\ghostty-vt.pdb"),
                ("uppercase runtimes", "RUNTIMES/win-x64/native/ghostty-vt.pdb"),
                ("absolute path", "/runtimes/win-x64/native/ghostty-vt.pdb"),
                ("drive prefix", "C:/runtimes/win-x64/native/ghostty-vt.pdb"),
                ("escaping path", "runtimes/win-x64/native/../../ghostty-vt.pdb"),
            ):
                mutated = root / f"{case.replace(' ', '-')}.snupkg"
                write_package(
                    mutated,
                    {MANAGED: PORTABLE, entry: b"Microsoft C/C++ MSF 7.00\r\n\x1aDS\0\0\0"},
                )
                expect(f"{case} spelling", run(mutated), accepted=False)

            native_file = root / "native-file.snupkg"
            write_package(
                native_file,
                {MANAGED: PORTABLE, "runtimes/win-x64/native/ghostty-vt.dll": b"MZ"},
            )
            expect("native payload under runtimes/", run(native_file), accepted=False)

            # A repeated name lets the gate read a Portable PDB while the push
            # carries a differently formatted one.
            duplicate = root / "duplicate-member.snupkg"
            write_package(duplicate, {MANAGED: PORTABLE})
            with zipfile.ZipFile(duplicate, "a") as archive:
                archive.writestr(MANAGED, b"Microsoft C/C++ MSF 7.00\r\n\x1aDS\0\0\0")
            result = run(duplicate)
            expect("repeated managed PDB entry", result, accepted=False)
            if MANAGED not in result.stderr:
                raise SelftestError(
                    f"the duplicate rejection did not name {MANAGED}\n"
                    f"stderr: {result.stderr.strip()}"
                )

            no_pdb = root / "no-pdb.snupkg"
            write_package(no_pdb, {"lib/net9.0/Ghostty.Vt.dll": b"MZ"})
            expect("no managed PDB", run(no_pdb), accepted=False)

            not_portable = root / "native-magic.snupkg"
            write_package(not_portable, {MANAGED: b"Microsoft C/C++ MSF 7.00\r\n\x1aDS\0\0\0"})
            expect("managed PDB without BSJB", run(not_portable), accepted=False)

            encrypted = root / "encrypted.snupkg"
            write_package(encrypted, {MANAGED: PORTABLE})
            encrypt(encrypted)
            result = run(encrypted)
            expect("encrypted member", result, accepted=False)
            if "Traceback" in result.stderr:
                raise SelftestError(
                    "encrypted member produced a traceback instead of an "
                    f"annotated error:\n{result.stderr.strip()}"
                )
    except (OSError, SelftestError) as error:
        print(f"::error::{error}", file=sys.stderr)
        return 1

    print("Symbol package content self-test passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
