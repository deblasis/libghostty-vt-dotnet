#!/usr/bin/env python3
"""Reject Windows DLLs that depend on a non-system C/C++ runtime.

The Windows package must load on a clean machine. Zig's MSVC ABI pulls in
VCRUNTIME140.dll, while the GNU ABI links only the Windows system libraries
listed below. The ABI is easy to lose in a target-triple edit, so this reads
the PE import table and fails on anything outside the allowlist.

With --pdb, also check that the DLL's CodeView debug entry names that PDB and
that the file starts with the MSF signature, so a stale or unrelated PDB
cannot be shipped next to the DLL.

Usage: check-windows-abi.py <dll> --machine x64|arm64 [--pdb <pdb>]
"""

from __future__ import annotations

import argparse
import ntpath
import struct
import sys
from pathlib import Path

MSF_SIGNATURE = b"Microsoft C/C++ MSF 7.00\r\n\x1aDS\0\0\0"
IMAGE_DIRECTORY_ENTRY_DEBUG = 6
IMAGE_DEBUG_TYPE_CODEVIEW = 2
DEBUG_DIRECTORY_ENTRY_SIZE = 28
RSDS_HEADER_SIZE = 24  # "RSDS", 16-byte GUID, 4-byte age

MACHINES = {
    "x64": 0x8664,
    "arm64": 0xAA64,
}

# A clean libghostty-vt DLL imports ntdll, kernel32 and the Universal C
# Runtime API sets. Keep this list deliberately small: a new entry is a
# packaging decision, not an accident.
ALLOWED_DLLS = {"ntdll.dll", "kernel32.dll"}
ALLOWED_PREFIXES = ("api-ms-win-crt-",)


class PeError(Exception):
    pass


def read_u16(data: bytes, offset: int) -> int:
    return struct.unpack_from("<H", data, offset)[0]


def read_u32(data: bytes, offset: int) -> int:
    return struct.unpack_from("<I", data, offset)[0]


def rva_to_offset(data: bytes, sections: list[tuple[int, int, int]], rva: int) -> int:
    for virtual_address, virtual_size, raw_pointer, raw_size in sections:
        span = max(virtual_size, raw_size)
        if virtual_address <= rva < virtual_address + span:
            delta = rva - virtual_address
            if delta >= raw_size:
                raise PeError(f"RVA 0x{rva:x} points outside raw section data")
            return raw_pointer + delta
    raise PeError(f"RVA 0x{rva:x} is not mapped by any section")


def read_c_string(data: bytes, offset: int) -> str:
    end = data.find(b"\0", offset)
    if end < 0:
        raise PeError(f"unterminated string at file offset 0x{offset:x}")
    return data[offset:end].decode("ascii", errors="replace")


def imports(path: Path) -> tuple[int, list[str]]:
    data, machine, sections, data_directory, _ = pe_image(path)

    # Data directory entry 1 is the import table.
    import_rva = read_u32(data, data_directory + 8)
    import_size = read_u32(data, data_directory + 12)
    if import_rva == 0 or import_size == 0:
        raise PeError("PE has no import table")

    descriptor_offset = rva_to_offset(data, sections, import_rva)
    names: list[str] = []
    while True:
        descriptor = data[descriptor_offset : descriptor_offset + 20]
        if len(descriptor) < 20:
            raise PeError("truncated import descriptor")
        name_rva = read_u32(descriptor, 12)
        if name_rva == 0:
            break
        name_offset = rva_to_offset(data, sections, name_rva)
        names.append(read_c_string(data, name_offset).lower())
        descriptor_offset += 20

    return machine, names


def pe_image(
    path: Path,
) -> tuple[bytes, int, list[tuple[int, int, int, int]], int, int]:
    data = path.read_bytes()
    if len(data) < 0x40 or data[:2] != b"MZ":
        raise PeError("missing DOS header")

    pe_offset = read_u32(data, 0x3C)
    if data[pe_offset : pe_offset + 4] != b"PE\0\0":
        raise PeError("missing PE signature")

    coff = pe_offset + 4
    machine = read_u16(data, coff)
    section_count = read_u16(data, coff + 2)
    optional_size = read_u16(data, coff + 16)
    optional = coff + 20

    magic = read_u16(data, optional)
    if magic == 0x10B:
        data_directory = optional + 96
        directory_count = read_u32(data, optional + 92)
    elif magic == 0x20B:
        data_directory = optional + 112
        directory_count = read_u32(data, optional + 108)
    else:
        raise PeError(f"unknown optional-header magic 0x{magic:x}")

    sections = []
    section_table = optional + optional_size
    for index in range(section_count):
        offset = section_table + index * 40
        virtual_size = read_u32(data, offset + 8)
        virtual_address = read_u32(data, offset + 12)
        raw_size = read_u32(data, offset + 16)
        raw_pointer = read_u32(data, offset + 20)
        sections.append((virtual_address, virtual_size, raw_pointer, raw_size))

    # Callers only read directories that must exist in a real image; a header
    # that claims fewer entries has no debug or import data to find.
    return data, machine, sections, data_directory, directory_count


def codeview_pdb_name(path: Path) -> str:
    data, _, sections, data_directory, directory_count = pe_image(path)
    if directory_count <= IMAGE_DIRECTORY_ENTRY_DEBUG:
        raise PeError("PE optional header has no debug data directory")
    debug_entry = data_directory + IMAGE_DIRECTORY_ENTRY_DEBUG * 8
    debug_rva = read_u32(data, debug_entry)
    debug_size = read_u32(data, debug_entry + 4)
    if debug_rva == 0 or debug_size == 0:
        raise PeError("PE has no debug directory")
    if debug_size < DEBUG_DIRECTORY_ENTRY_SIZE:
        raise PeError("PE debug directory is truncated")

    debug_offset = rva_to_offset(data, sections, debug_rva)
    for index in range(debug_size // DEBUG_DIRECTORY_ENTRY_SIZE):
        entry = debug_offset + index * DEBUG_DIRECTORY_ENTRY_SIZE
        if entry + DEBUG_DIRECTORY_ENTRY_SIZE > len(data):
            raise PeError("PE debug directory runs past end of file")
        debug_type = read_u32(data, entry + 12)
        if debug_type != IMAGE_DEBUG_TYPE_CODEVIEW:
            continue

        size = read_u32(data, entry + 16)
        pointer = read_u32(data, entry + 24)
        if size <= RSDS_HEADER_SIZE or pointer == 0:
            raise PeError("CodeView debug entry has no file data")
        blob = data[pointer : pointer + size]
        if len(blob) < size:
            raise PeError("CodeView debug data runs past end of file")
        if blob[:4] != b"RSDS":
            raise PeError("CodeView debug entry is not RSDS")
        end = blob.find(b"\0", RSDS_HEADER_SIZE)
        if end < 0:
            raise PeError("CodeView PDB name is not terminated")
        name = blob[RSDS_HEADER_SIZE:end].decode("utf-8", errors="replace")
        if not name:
            raise PeError("CodeView PDB name is empty")
        return name

    raise PeError("PE has no CodeView debug entry")


def check_pdb(path: Path, pdb: Path) -> str:
    embedded = codeview_pdb_name(path)
    expected = pdb.name
    if ntpath.basename(embedded) != expected:
        raise PeError(
            f"PE debug entry names {ntpath.basename(embedded)!r}, "
            f"but --pdb is {expected!r}"
        )
    if not pdb.is_file():
        raise PeError(f"PDB not found: {pdb}")
    with pdb.open("rb") as stream:
        signature = stream.read(len(MSF_SIGNATURE))
    if signature != MSF_SIGNATURE:
        raise PeError(f"{pdb} is not an MSF 7.00 PDB")
    return embedded


def allowed(name: str) -> bool:
    return name in ALLOWED_DLLS or name.startswith(ALLOWED_PREFIXES)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("dll", type=Path)
    parser.add_argument("--machine", choices=sorted(MACHINES), required=True)
    parser.add_argument(
        "--pdb",
        type=Path,
        help="PDB that must match the DLL's CodeView entry and carry the MSF signature",
    )
    args = parser.parse_args()

    if not args.dll.is_file():
        print(f"::error::Windows DLL not found: {args.dll}", file=sys.stderr)
        return 1

    try:
        machine, imported = imports(args.dll)
    except (OSError, PeError) as error:
        print(f"::error::{args.dll}: {error}", file=sys.stderr)
        return 1

    expected = MACHINES[args.machine]
    if machine != expected:
        print(
            f"::error::{args.dll} is machine 0x{machine:04x}, expected "
            f"0x{expected:04x} for --machine {args.machine}",
            file=sys.stderr,
        )
        return 1

    rejected = [name for name in imported if not allowed(name)]
    if rejected:
        print(
            f"::error::{args.dll} imports a non-system runtime: "
            + ", ".join(sorted(set(rejected))),
            file=sys.stderr,
        )
        return 1

    print(f"{args.dll}: {args.machine} PE, system imports only")
    for name in sorted(imported):
        print(f"  {name}")

    if args.pdb is not None:
        try:
            embedded = check_pdb(args.dll, args.pdb)
        except (OSError, PeError) as error:
            print(f"::error::{args.dll} + {args.pdb}: {error}", file=sys.stderr)
            return 1
        print(f"{args.pdb}: MSF PDB named by the DLL's CodeView entry ({embedded})")

    return 0


if __name__ == "__main__":
    sys.exit(main())
