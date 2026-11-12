#!/usr/bin/env python3
"""Reject Windows DLLs that depend on a non-system C/C++ runtime.

The Windows package must load on a clean machine. Zig's MSVC ABI pulls in
VCRUNTIME140.dll, while the GNU ABI links only the Windows system libraries
listed below. The ABI is easy to lose in a target-triple edit, so this reads
the PE import table (and the delay-load table, if any) and fails on anything
outside the allowlist.

With --pdb, also check that the DLL's CodeView debug entry names that PDB,
that the file is an MSF 7.00 PDB, and that the PDB info stream carries the
same GUID and age as the CodeView record. A PDB from another architecture or
another build therefore cannot be shipped next to the DLL.

Usage: check-windows-abi.py <dll> --machine x64|arm64 [--pdb <pdb>]
"""

from __future__ import annotations

import argparse
import ntpath
import struct
import sys
from pathlib import Path

MSF_SIGNATURE = b"Microsoft C/C++ MSF 7.00\r\n\x1aDS\0\0\0"
MSF_PAGE_SIZE_OFFSET = 32
MSF_DIRECTORY_BYTES_OFFSET = 44
MSF_BLOCK_MAP_OFFSET = 52
PDB_INFO_STREAM = 1
PDB_INFO_GUID_OFFSET = 12
PDB_INFO_AGE_OFFSET = 8
PDB_INFO_MIN_SIZE = 28

IMAGE_DIRECTORY_ENTRY_IMPORT = 1
IMAGE_DIRECTORY_ENTRY_DEBUG = 6
IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT = 13
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


def rva_to_offset(data: bytes, sections: list[tuple[int, int, int, int]], rva: int) -> int:
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


def directory_names(
    data: bytes,
    sections: list[tuple[int, int, int, int]],
    data_directory: int,
    directory_count: int,
    index: int,
    required: bool,
) -> list[str]:
    if directory_count <= index:
        if required:
            raise PeError(f"PE has no data directory {index}")
        return []

    entry = data_directory + index * 8
    rva = read_u32(data, entry)
    size = read_u32(data, entry + 4)
    if rva == 0 or size == 0:
        if required:
            raise PeError("PE has no import table")
        return []

    descriptor_offset = rva_to_offset(data, sections, rva)
    names: list[str] = []
    if index == IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT:
        # ImgDelayDescr is 32 bytes; the DLL-name RVA lives at offset 4. A
        # descriptor with grAttrs bit 0 clear uses virtual addresses, which a
        # file-backed check cannot resolve safely.
        step = 32
        name_offset = 4
        while True:
            descriptor = data[descriptor_offset : descriptor_offset + step]
            if len(descriptor) < step:
                raise PeError("truncated delay-load descriptor")
            name_rva = read_u32(descriptor, name_offset)
            # The table ends with an all-zero descriptor, which also has
            # grAttrs clear; stop before interpreting it as a VA-based entry.
            if name_rva == 0:
                break
            if not read_u32(descriptor, 0) & 1:
                raise PeError("delay-load descriptor does not use RVA addresses")
            offset = rva_to_offset(data, sections, name_rva)
            names.append(read_c_string(data, offset).lower())
            descriptor_offset += step
    else:
        # IMAGE_IMPORT_DESCRIPTOR is 20 bytes; the name RVA is at offset 12.
        step = 20
        name_offset = 12
        while True:
            descriptor = data[descriptor_offset : descriptor_offset + step]
            if len(descriptor) < step:
                raise PeError("truncated import descriptor")
            name_rva = read_u32(descriptor, name_offset)
            if name_rva == 0:
                break
            offset = rva_to_offset(data, sections, name_rva)
            names.append(read_c_string(data, offset).lower())
            descriptor_offset += step

    return names


def imports(path: Path) -> tuple[int, list[str]]:
    data, machine, sections, data_directory, directory_count = pe_image(path)
    names = directory_names(
        data,
        sections,
        data_directory,
        directory_count,
        IMAGE_DIRECTORY_ENTRY_IMPORT,
        required=True,
    )
    names.extend(
        directory_names(
            data,
            sections,
            data_directory,
            directory_count,
            IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT,
            required=False,
        )
    )
    return machine, names


def codeview_blob(path: Path) -> tuple[bytes, int]:
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
        return blob, pointer

    raise PeError("PE has no CodeView debug entry")


def codeview_record(path: Path) -> tuple[str, bytes, int]:
    blob, _ = codeview_blob(path)
    end = blob.find(b"\0", RSDS_HEADER_SIZE)
    if end < 0:
        raise PeError("CodeView PDB name is not terminated")
    name = blob[RSDS_HEADER_SIZE:end].decode("utf-8", errors="replace")
    if not name:
        raise PeError("CodeView PDB name is empty")
    return name, bytes(blob[4:20]), read_u32(blob, 20)


def msf_streams(path: Path) -> list[bytes]:
    data = path.read_bytes()
    if len(data) < MSF_BLOCK_MAP_OFFSET + 4 or data[: len(MSF_SIGNATURE)] != MSF_SIGNATURE:
        raise PeError(f"{path} is not an MSF 7.00 PDB")

    page_size = read_u32(data, MSF_PAGE_SIZE_OFFSET)
    directory_bytes = read_u32(data, MSF_DIRECTORY_BYTES_OFFSET)
    block_map = read_u32(data, MSF_BLOCK_MAP_OFFSET)
    if page_size < 0x200 or page_size > 0x10000 or page_size & (page_size - 1):
        raise PeError(f"{path} has an invalid MSF page size 0x{page_size:x}")
    directory_pages = (directory_bytes + page_size - 1) // page_size
    if directory_pages == 0:
        raise PeError(f"{path} has an empty MSF stream directory")

    def page(number: int) -> bytes:
        start = number * page_size
        if start + page_size > len(data):
            raise PeError(f"{path} references MSF page {number} past end of file")
        return data[start : start + page_size]

    map_offset = block_map * page_size
    if map_offset + directory_pages * 4 > len(data):
        raise PeError(f"{path} has a truncated MSF block map")
    directory = b"".join(
        page(read_u32(data, map_offset + index * 4)) for index in range(directory_pages)
    )[:directory_bytes]

    stream_count = read_u32(directory, 0)
    if stream_count == 0 or stream_count > 0x10000:
        raise PeError(f"{path} has an invalid MSF stream count {stream_count}")
    sizes_offset = 4
    if sizes_offset + stream_count * 4 > len(directory):
        raise PeError(f"{path} has a truncated MSF stream table")
    sizes = [read_u32(directory, sizes_offset + index * 4) for index in range(stream_count)]

    blocks_offset = sizes_offset + stream_count * 4
    streams = []
    for size in sizes:
        block_count = (size + page_size - 1) // page_size
        if blocks_offset + block_count * 4 > len(directory):
            raise PeError(f"{path} has a truncated MSF stream directory")
        numbers = [
            read_u32(directory, blocks_offset + index * 4)
            for index in range(block_count)
        ]
        blocks_offset += block_count * 4
        streams.append(b"".join(page(number) for number in numbers)[:size])
    return streams


def pdb_identity(path: Path) -> tuple[bytes, int]:
    streams = msf_streams(path)
    if len(streams) <= PDB_INFO_STREAM or len(streams[PDB_INFO_STREAM]) < PDB_INFO_MIN_SIZE:
        raise PeError(f"{path} has no PDB info stream")
    info = streams[PDB_INFO_STREAM]
    guid = bytes(info[PDB_INFO_GUID_OFFSET : PDB_INFO_GUID_OFFSET + 16])
    age = read_u32(info, PDB_INFO_AGE_OFFSET)
    if guid == b"\0" * 16:
        raise PeError(f"{path} has a zero PDB info GUID")
    return guid, age


def format_guid(guid: bytes) -> str:
    first, second, third = struct.unpack_from("<IHH", guid, 0)
    return f"{first:08x}-{second:04x}-{third:04x}-{guid[8:].hex()}"


def check_pdb(path: Path, pdb: Path) -> str:
    embedded, rsds_guid, rsds_age = codeview_record(path)
    expected = pdb.name
    if ntpath.basename(embedded) != expected:
        raise PeError(
            f"PE debug entry names {ntpath.basename(embedded)!r}, "
            f"but --pdb is {expected!r}"
        )
    if not pdb.is_file():
        raise PeError(f"PDB not found: {pdb}")
    guid, age = pdb_identity(pdb)
    if guid != rsds_guid or age != rsds_age:
        raise PeError(
            "PDB identity mismatch: DLL CodeView has GUID "
            f"{format_guid(rsds_guid)} age {rsds_age}, PDB info stream has "
            f"GUID {format_guid(guid)} age {age}"
        )
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
        help="PDB that must match the DLL's CodeView record and carry its GUID/age",
    )
    args = parser.parse_args()

    if not args.dll.is_file():
        print(f"::error::Windows DLL not found: {args.dll}", file=sys.stderr)
        return 1

    try:
        machine, imported = imports(args.dll)
    except (OSError, PeError, struct.error, ValueError) as error:
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
        except (OSError, PeError, struct.error, ValueError) as error:
            print(f"::error::{args.dll} + {args.pdb}: {error}", file=sys.stderr)
            return 1
        print(
            f"{args.pdb}: MSF PDB named by the DLL's CodeView entry "
            f"({embedded}) with matching GUID and age"
        )

    return 0


if __name__ == "__main__":
    sys.exit(main())
