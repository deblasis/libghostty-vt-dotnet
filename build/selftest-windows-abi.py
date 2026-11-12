#!/usr/bin/env python3
"""Prove that check-windows-abi.py rejects the regressions it exists to catch.

Runs the checker three times against copies of a real DLL:

1. unmodified: must pass
2. one allowed import renamed to vcruntime140.dll: must fail
3. machine field changed to ARM64: must fail the x64 check

CI runs this next to the green check, so the gate is never trusted just
because it printed success.

Usage: selftest-windows-abi.py <dll> --machine x64|arm64
"""

from __future__ import annotations

import argparse
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


def run(dll: Path, machine: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(CHECKER), str(dll), "--machine", machine],
        capture_output=True,
        text=True,
    )


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
    args = parser.parse_args()

    if not args.dll.is_file():
        print(f"::error::Windows DLL not found: {args.dll}", file=sys.stderr)
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
    except (OSError, SelftestError) as error:
        print(f"::error::{error}", file=sys.stderr)
        return 1

    print(f"Windows ABI check self-test passed: {args.dll}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
