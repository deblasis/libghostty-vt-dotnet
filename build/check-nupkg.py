#!/usr/bin/env python3
"""Assert a packed .nupkg carries the Windows natives it claims.

A nupkg missing a runtime tree restores and tests cleanly on another OS, so
`dotnet pack` alone never proves the Windows assets are there. This opens the
produced package and requires, for every expected RID, a non-empty
`runtimes/<rid>/native/ghostty-vt.dll` and `ghostty-vt.pdb`, then runs
`check-windows-abi.py` on the extracted pair. The import table and the PDB
identity are therefore checked against the exact bytes that would publish.

It also enumerates every `runtimes/win-*/native/` tree rather than trusting a
fixed list: an unexpected Windows RID, or an extra file inside a checked tree,
fails the gate. The embedded README must name every Windows RID the package
ships, so the package never advertises less than it contains.

Entry names are classified case-insensitively and with backslashes treated as
separators, so a differently spelled tree cannot slip past the enumeration.
A non-canonical spelling is then rejected outright rather than accepted: NuGet
writes `runtimes/<rid>/native/<file>` in lowercase with forward slashes, and on
Windows a backslash is a path separator, so such an entry would land beside the
files this gate inspects while remaining invisible to an exact-name match.
Entry names with an absolute path, a drive prefix, or a `..` segment fail before
classification, because an extractor can map them somewhere other than the tree
their name implies.

By default both Windows RIDs are required. `--allow-missing-rid` relaxes one
with a warning, for the case where the ARM64 build leg failed and the x64
package must still ship. A partially present tree is still checked, so the
relaxation cannot hide a broken or wrong-architecture pair.

Usage: check-nupkg.py <nupkg> [--require-rid RID]... [--allow-missing-rid RID]...
"""

from __future__ import annotations

import argparse
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

DEFAULT_RIDS = ("win-x64", "win-arm64")
MACHINES = {"win-x64": "x64", "win-arm64": "arm64"}
NATIVE_FILES = ("ghostty-vt.dll", "ghostty-vt.pdb")
WINDOWS_PREFIX = "win-"
README = "README.md"
CHECKER = Path(__file__).with_name("check-windows-abi.py")


class PackageError(Exception):
    pass


def native_path(rid: str, name: str) -> str:
    return f"runtimes/{rid}/native/{name}"


def read_member(package: zipfile.ZipFile, name: str) -> bytes:
    try:
        info = package.getinfo(name)
    except KeyError as error:
        raise PackageError(f"package is missing {name}") from error
    if info.file_size == 0:
        raise PackageError(f"package contains an empty {name}")
    return package.read(info)


def check_entry_name(name: str) -> None:
    """Reject entry names no NuGet package should contain.

    A leading slash, a drive prefix, or a `..` segment can make an entry land
    outside the tree implied by its name once an extractor normalizes it. The
    gate must not print success for a package whose paths it cannot map.
    """
    normalized = name.replace("\\", "/")
    parts = normalized.split("/")
    if normalized.startswith("/") or (
        len(parts[0]) == 2 and parts[0][1] == ":"
    ):
        raise PackageError(f"package entry has an absolute path: {name}")
    if ".." in parts:
        raise PackageError(f"package entry escapes its directory: {name}")


def windows_trees(names: set[str]) -> dict[str, set[str]]:
    """Map every Windows runtime RID to the files under its native directory.

    Matching ignores case and treats backslashes as separators so no spelling
    escapes the enumeration, but every entry under runtimes/ must still use the
    canonical lowercase forward-slash form that NuGet writes. Anything else is
    payload the package gate cannot safely reason about, so it fails.
    """
    trees: dict[str, set[str]] = {}
    for name in names:
        check_entry_name(name)
        normalized = name.replace("\\", "/")
        parts = normalized.split("/")
        if not parts or parts[0].lower() != "runtimes":
            continue
        if name != normalized:
            raise PackageError(
                "package entry under runtimes/ uses a backslash path "
                f"separator: {name}"
            )
        if normalized in ("runtimes", "runtimes/"):
            continue
        if (
            parts[0] != "runtimes"
            or len(parts) < 4
            or parts[2] != "native"
            or parts[1] != parts[1].lower()
            or any(part in ("", ".", "..") for part in parts[3:])
        ):
            raise PackageError(
                "package entry under runtimes/ is not canonically named "
                f"runtimes/<rid>/native/<file>: {name}"
            )
        rid = parts[1]
        if not rid.startswith(WINDOWS_PREFIX):
            continue
        trees.setdefault(rid, set()).add("/".join(parts[3:]))
    return trees


def check_trees(trees: dict[str, set[str]]) -> None:
    unknown = sorted(rid for rid in trees if rid not in MACHINES)
    if unknown:
        raise PackageError(
            "package ships Windows runtime trees this gate does not know: "
            + ", ".join(unknown)
        )
    for rid, files in sorted(trees.items()):
        extra = sorted(files - set(NATIVE_FILES))
        if extra:
            raise PackageError(
                f"package has unexpected files under {rid}/native: "
                + ", ".join(extra)
            )


def check_readme(package: zipfile.ZipFile, names: set[str], rids: list[str]) -> None:
    if README not in names:
        raise PackageError(f"package is missing {README}")
    text = read_member(package, README).decode("utf-8", errors="replace")
    missing = [rid for rid in rids if rid not in text]
    if missing:
        raise PackageError(
            f"{README} does not name the packaged Windows runtime(s): "
            + ", ".join(missing)
        )


def check_rid(package: zipfile.ZipFile, rid: str, machine: str, root: Path) -> None:
    dll = read_member(package, native_path(rid, "ghostty-vt.dll"))
    pdb = read_member(package, native_path(rid, "ghostty-vt.pdb"))

    directory = root / rid
    directory.mkdir(parents=True, exist_ok=True)
    dll_path = directory / "ghostty-vt.dll"
    pdb_path = directory / "ghostty-vt.pdb"
    dll_path.write_bytes(dll)
    pdb_path.write_bytes(pdb)

    result = subprocess.run(
        [
            sys.executable,
            str(CHECKER),
            str(dll_path),
            "--machine",
            machine,
            "--pdb",
            str(pdb_path),
        ],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        detail = (result.stderr or result.stdout).strip()
        raise PackageError(f"{rid} native pair failed the ABI/PDB check: {detail}")
    print(result.stdout.strip())


def member_names(package: zipfile.ZipFile) -> set[str]:
    return {info.filename for info in package.infolist()}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("nupkg", type=Path)
    parser.add_argument(
        "--require-rid",
        action="append",
        default=[],
        metavar="RID",
        help="extra Windows RID that must be present (default: win-x64 and win-arm64)",
    )
    parser.add_argument(
        "--allow-missing-rid",
        action="append",
        default=[],
        metavar="RID",
        help="Windows RID that may be absent, e.g. when its build leg failed",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()

    if not args.nupkg.is_file():
        print(f"::error::package not found: {args.nupkg}", file=sys.stderr)
        return 1

    required = list(DEFAULT_RIDS)
    for rid in args.require_rid:
        if rid not in required:
            required.append(rid)
    allowed_missing = set(args.allow_missing_rid)

    unknown = [rid for rid in required if rid not in MACHINES]
    unknown.extend(rid for rid in allowed_missing if rid not in MACHINES)
    if unknown:
        print(
            f"::error::unknown Windows RID(s): {', '.join(sorted(set(unknown)))}",
            file=sys.stderr,
        )
        return 1

    overlap = sorted(set(args.require_rid) & allowed_missing)
    if overlap:
        print(
            "::error::RID(s) both required and allowed to be missing: "
            + ", ".join(overlap),
            file=sys.stderr,
        )
        return 1

    try:
        with zipfile.ZipFile(args.nupkg) as package, tempfile.TemporaryDirectory(
            prefix="check-nupkg-"
        ) as directory:
            root = Path(directory)
            names = member_names(package)
            trees = windows_trees(names)
            check_trees(trees)

            checked = []
            present = []
            for rid in required:
                if rid in trees:
                    # A partially present tree is still checked: skipping the
                    # RID must not turn a broken or wrong-architecture pair into
                    # a silent pass.
                    check_rid(package, rid, MACHINES[rid], root)
                    checked.append(rid)
                    present.append(rid)
                elif rid in allowed_missing:
                    print(
                        f"::warning::{rid} is allowed to be missing; "
                        "the package will not claim that architecture"
                    )
                else:
                    raise PackageError(
                        f"package is missing the {rid} native tree "
                        f"({native_path(rid, 'ghostty-vt.dll')})"
                    )

            if not checked:
                raise PackageError(
                    "no Windows native tree was checked; the gate proved nothing"
                )

            check_readme(package, names, present)
    except (
        OSError,
        RuntimeError,
        NotImplementedError,
        zipfile.BadZipFile,
        PackageError,
    ) as error:
        print(f"::error::{args.nupkg}: {error}", file=sys.stderr)
        return 1

    print(f"{args.nupkg}: Windows natives verified for {', '.join(checked)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
