#!/usr/bin/env python3
"""Prove that check-windows-abi.py rejects the regressions it exists to catch.

Runs the checker against copies of a real DLL:

1. unmodified: must pass
2. one allowed import renamed to vcruntime140.dll: must fail
3. machine field changed to ARM64: must fail the x64 check
4. delay-import descriptor walk: a normal entry plus the zero terminator must
   parse, and a VA-based descriptor must be rejected

With --pdb, also proves the PDB gate:

5. unmodified DLL with its matching PDB: must pass
6. missing PDB: must fail
7. PDB with a different basename: must fail
8. file with the right basename but no MSF signature: must fail
9. PDB with the right basename but a different GUID: must fail
10. DLL whose CodeView age differs from the PDB: must fail

CI runs this next to the green check, so the gate is never trusted just
because it printed success.

Usage: selftest-windows-abi.py <dll> --machine x64|arm64 [--pdb <pdb>]
"""

from __future__ import annotations

import argparse
import importlib.util
import shutil
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

CHECKER = Path(__file__).with_name("check-windows-abi.py")
FAKE_RUNTIME = b"vcruntime140.dll"
MACHINE_VALUES = {"x64": 0x8664, "arm64": 0xAA64}


class SelftestError(Exception):
    pass


def run(
    dll: Path, machine: str, pdb: Path | None = None
) -> subprocess.CompletedProcess[str]:
    command = [sys.executable, str(CHECKER), str(dll), "--machine", machine]
    if pdb is not None:
        command.extend(["--pdb", str(pdb)])
    return subprocess.run(command, capture_output=True, text=True)


def with_fake_runtime(source: Path, destination: Path) -> None:
    data = bytearray(source.read_bytes())
    marker = b"api-ms-win-crt-"
    start = data.find(marker)
    if start < 0:
        raise SelftestError("DLL has no api-ms-win-crt-* import to rewrite")
    end = data.find(b"\0", start)
    if end < 0:
        raise SelftestError("unterminated import name")
    if end - start < len(FAKE_RUNTIME):
        raise SelftestError("import name is too short for the fake runtime")
    data[start : start + len(FAKE_RUNTIME)] = FAKE_RUNTIME
    data[start + len(FAKE_RUNTIME)] = 0
    destination.write_bytes(data)


def with_machine(source: Path, destination: Path, machine: int) -> None:
    data = bytearray(source.read_bytes())
    if data[:2] != b"MZ":
        raise SelftestError("missing DOS header")
    pe_offset = struct.unpack_from("<I", data, 0x3C)[0]
    if data[pe_offset : pe_offset + 4] != b"PE\0\0":
        raise SelftestError("missing PE signature")
    struct.pack_into("<H", data, pe_offset + 4, machine)
    destination.write_bytes(data)


def with_codeview_change(source: Path, destination: Path, offset: int) -> None:
    module = load_checker()
    try:
        _, start = module.codeview_blob(source)
    except Exception as error:  # checker errors are not selftest failures
        raise SelftestError(f"could not locate the CodeView record: {error}") from error

    data = bytearray(source.read_bytes())
    field = start + offset
    if field >= len(data):
        raise SelftestError("CodeView record is truncated")
    data[field] ^= 0xFF
    destination.write_bytes(data)


def with_fake_msf(destination: Path) -> None:
    signature = b"Microsoft C/C++ MSF 7.00\r\n\x1aDS\0\0\0"
    destination.write_bytes(signature + b"\0" * 64)


def load_checker():
    spec = importlib.util.spec_from_file_location("check_windows_abi", CHECKER)
    if spec is None or spec.loader is None:
        raise SelftestError("could not load the ABI checker")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def delay_table(attributes: int) -> bytes:
    """Build a PE image fragment with one delay descriptor and a terminator."""
    data = bytearray(0x500)
    data_directory = 0x200
    struct.pack_into("<II", data, data_directory + 13 * 8, 0x1000, 64)
    # Descriptor: grAttrs, rvaDLLName, ...
    struct.pack_into("<I", data, 0x300, attributes)
    struct.pack_into("<I", data, 0x304, 0x1100)
    # rvaDLLName points into the section at file offset 0x400.
    data[0x400:0x408] = b"foo.dll\0"
    # The all-zero terminator is a valid end even though grAttrs is clear.
    return bytes(data)


def check_delay_walk() -> None:
    module = load_checker()
    sections = [(0x1000, 0x1000, 0x300, 0x1000)]
    names = module.directory_names(
        delay_table(1), sections, 0x200, 16, 13, required=False
    )
    if names != ["foo.dll"]:
        raise SelftestError(f"delay-import walk returned {names!r}, expected ['foo.dll']")

    try:
        module.directory_names(
            delay_table(0), sections, 0x200, 16, 13, required=False
        )
    except module.PeError:
        return
    raise SelftestError("delay-import walk accepted a VA-based descriptor")


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
    parser.add_argument("dll", type=Path)
    parser.add_argument("--machine", choices=sorted(MACHINE_VALUES), required=True)
    parser.add_argument(
        "--pdb",
        type=Path,
        help="matching PDB; also proves the PDB gate rejects bad inputs",
    )
    args = parser.parse_args()

    if not args.dll.is_file():
        print(f"::error::Windows DLL not found: {args.dll}", file=sys.stderr)
        return 1

    if args.pdb is not None and not args.pdb.is_file():
        print(f"::error::PDB not found: {args.pdb}", file=sys.stderr)
        return 1

    try:
        with tempfile.TemporaryDirectory(prefix="windows-abi-") as directory:
            temp = Path(directory)

            expect("unmodified DLL", run(args.dll, args.machine), accepted=True)

            fake = temp / "fake-runtime.dll"
            with_fake_runtime(args.dll, fake)
            expect("non-system runtime", run(fake, args.machine), accepted=False)

            wrong = temp / "wrong-machine.dll"
            other = 0xAA64 if args.machine == "x64" else 0x8664
            with_machine(args.dll, wrong, other)
            expect("wrong machine", run(wrong, args.machine), accepted=False)

            check_delay_walk()

            if args.pdb is not None:
                expect(
                    "matching PDB",
                    run(args.dll, args.machine, args.pdb),
                    accepted=True,
                )

                missing = temp / "missing" / args.pdb.name
                expect(
                    "missing PDB",
                    run(args.dll, args.machine, missing),
                    accepted=False,
                )

                mismatched = temp / f"different-{args.pdb.name}"
                shutil.copyfile(args.pdb, mismatched)
                expect(
                    "mismatched PDB name",
                    run(args.dll, args.machine, mismatched),
                    accepted=False,
                )

                corrupt = temp / "corrupt" / args.pdb.name
                corrupt.parent.mkdir()
                corrupt.write_bytes(b"not a PDB")
                expect(
                    "PDB without MSF signature",
                    run(args.dll, args.machine, corrupt),
                    accepted=False,
                )

                signature_only = temp / "signature-only" / args.pdb.name
                signature_only.parent.mkdir()
                with_fake_msf(signature_only)
                expect(
                    "MSF signature without an info stream",
                    run(args.dll, args.machine, signature_only),
                    accepted=False,
                )

                wrong_guid = temp / "wrong-guid.dll"
                with_codeview_change(args.dll, wrong_guid, 4)
                expect(
                    "CodeView GUID does not match the PDB",
                    run(wrong_guid, args.machine, args.pdb),
                    accepted=False,
                )

                wrong_age = temp / "wrong-age.dll"
                with_codeview_change(args.dll, wrong_age, 20)
                expect(
                    "CodeView age does not match the PDB",
                    run(wrong_age, args.machine, args.pdb),
                    accepted=False,
                )
    except (OSError, SelftestError) as error:
        print(f"::error::{error}", file=sys.stderr)
        return 1

    print(f"Windows ABI check self-test passed: {args.dll}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
