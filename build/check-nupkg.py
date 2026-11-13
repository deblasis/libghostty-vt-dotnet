#!/usr/bin/env python3
"""Assert a packed .nupkg carries the Windows natives it claims.

A nupkg missing a runtime tree restores and tests cleanly on another OS, so
`dotnet pack` alone never proves the Windows assets are there. This opens the
produced package and requires, for every expected RID, a non-empty
`runtimes/<rid>/native/ghostty-vt.dll` and `ghostty-vt.pdb`, then runs
`check-windows-abi.py` on the extracted pair. The import table and the PDB
identity are therefore checked against the exact bytes that would publish.

A package that repeats an entry name is rejected before any of that: NuGet's
extractor writes the first copy of a name, while `zipfile.getinfo()` returns
the last, so the two disagree about which file the gate inspected. There is no
correct way to resolve that ambiguity, and a package assembled by anything
other than a single `dotnet pack` run can contain it.

It also enumerates every `runtimes/win*/native/` tree rather than trusting a
fixed list: an unexpected Windows RID, or an extra file inside a checked tree,
fails the gate. The embedded README must name every Windows RID the package
ships, so the package never advertises less than it contains. A RID that may be
absent must be worded as conditional, so it never advertises more than it
contains either.

The arch-neutral fallback trees (`win`, `any`, `base`) are rejected outright.
They are not in the RID graph for decoration: NuGet falls back from `win-x64`
to `win` to `any` to `base` when the specific tree is absent, so a native placed
there is resolved for a Windows RID while sitting outside every tree this gate
inspects. This package ships architecture-specific natives, so an arch-neutral
fallback is never a shape it should accept. Every other Windows-resolvable
spelling (`windows*`, `win<digit>*`, `win-*`) is enumerated and rejected unless
it is exactly `win-x64` or `win-arm64`; non-Windows trees (`linux-*`, `osx-*`)
are skipped because this gate is about Windows payload.

Entry names are classified case-insensitively and with backslashes treated as
separators, so a differently spelled tree cannot slip past the enumeration.
A non-canonical spelling is then rejected outright rather than accepted: NuGet
writes `runtimes/<rid>/native/<file>` in lowercase with forward slashes, and on
Windows a backslash is a path separator, so such an entry would land beside the
files this gate inspects while remaining invisible to an exact-name match.
Entry names with an absolute path, a drive prefix, a `.` or `..` segment, or
surrounding whitespace fail before classification, because an extractor can map
them somewhere other than the tree their name implies. `./runtimes/any/...` is
the worked example: NuGet normalizes the leading `./` and resolves the tree,
while a name-based sweep sees `"."` and would otherwise skip it.

By default both Windows RIDs are required. `--allow-missing-rid` relaxes one
with a warning, for the case where the ARM64 build leg failed and the x64
package must still ship. A partially present tree is still checked, so the
relaxation cannot hide a broken or wrong-architecture pair.

Usage: check-nupkg.py <nupkg> [--require-rid RID]... [--allow-missing-rid RID]...
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

DEFAULT_RIDS = ("win-x64", "win-arm64")
MACHINES = {"win-x64": "x64", "win-arm64": "arm64"}
NATIVE_FILES = ("ghostty-vt.dll", "ghostty-vt.pdb")
# NuGet's portable RID graph falls back from `win-x64`/`win-arm64` to `win`,
# `any`, and finally `base`. A native in any of those trees resolves on Windows
# without living in a tree this gate checks, so the package must not contain
# one.
FALLBACK_RIDS = ("win", "any", "base")
README = "README.md"
CHECKER = Path(__file__).with_name("check-windows-abi.py")


class PackageError(Exception):
    pass


def native_path(rid: str, name: str) -> str:
    return f"runtimes/{rid}/native/{name}"


def windows_rid(rid: str) -> bool:
    """Whether a RID is a Windows shape, known to this gate or not.

    `win-x64` and `win-arm64` are the trees this package ships. NuGet also
    accepts `win` and `any` (the fallbacks, rejected separately), versioned
    spellings such as `win10-x64`, and `windows*`. A gate that only looked at
    `win-` would skip a `win10-x64` tree, which NuGet resolves for a Windows
    10/11 consumer, without opening it.
    """
    return (
        rid in FALLBACK_RIDS
        or rid.startswith("win-")
        or rid.startswith("windows")
        or (rid.startswith("win") and rid[3:4].isdigit())
    )


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
    if name != name.strip():
        raise PackageError(f"package entry has surrounding whitespace: {name!r}")
    normalized = name.replace("\\", "/")
    # `runtimes/win-x64/` is a directory entry: the trailing separator is not an
    # empty segment. Any other empty segment is a path this gate cannot map.
    trimmed = normalized[:-1] if normalized.endswith("/") else normalized
    parts = trimmed.split("/")
    if normalized.startswith("/") or (
        len(parts[0]) == 2 and parts[0][1] == ":"
    ):
        raise PackageError(f"package entry has an absolute path: {name}")
    if ".." in parts:
        raise PackageError(f"package entry escapes its directory: {name}")
    if "." in parts:
        raise PackageError(
            "package entry uses a . path segment, which NuGet normalizes but a "
            f"name-based sweep does not: {name}"
        )
    if "" in parts:
        raise PackageError(f"package entry has an empty path segment: {name}")


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
        if rid in FALLBACK_RIDS:
            raise PackageError(
                "package puts a native under the arch-neutral RID fallback "
                f"runtimes/{rid}/native/, which NuGet resolves for a Windows "
                f"RID with no specific tree of its own: {name}"
            )
        if not windows_rid(rid):
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


README_OPTIONAL_MARKER = "is included when"
# The packaged README states the ARM64 caveat in prose and in a table row, and
# both phrasings are legitimate. What matters is that the clause naming the RID
# is conditional, not that it uses one exact sentence.
README_CONDITIONAL = (
    re.compile(r"is included when", re.IGNORECASE),
    re.compile(r"when (?:the|its) .*build succeeds", re.IGNORECASE),
)
# A RID that may be absent must not be described as unconditionally shipped.
README_ABSOLUTE = (
    re.compile(r"\b(?:are|is)\s+bundled\b", re.IGNORECASE),
    re.compile(r"\balways\b", re.IGNORECASE),
    re.compile(r"\ball four\b", re.IGNORECASE),
)
# A clause is about shipping when it names a native file or uses a shipping
# verb. Prose that only says ARM64 is tested does not make a shipping claim, so
# it does not have to be conditional.
README_SHIPPING = re.compile(
    r"\b(?:bundled|included|shipped|packaged|available|present|ships?)\b"
    r"|ghostty-vt\.(?:dll|pdb)"
    r"|\bin the package\b",
    re.IGNORECASE,
)


def readme_names(claim: str, rid: str) -> bool:
    """Whether a clause names one runtime.

    A RID reaches the README in more than one shape: backticked in prose, as a
    bare table cell, and as the machine name ("Windows ARM64"). Keying only on
    the backticked token let a row that dropped the backticks, or a sentence
    that said "Windows ARM64", describe the optional tree as unconditional.
    """
    arch = rid.rsplit("-", 1)[-1]
    return bool(
        re.search(rf"\b{re.escape(rid)}\b", claim, re.IGNORECASE)
        # "Windows ARM64" is a name for the runtime; the `-arm64` in
        # `osx-arm64` is a different runtime, so a preceding hyphen or word
        # character disqualifies the machine name.
        or re.search(rf"(?<![\w-]){re.escape(arch)}\b", claim, re.IGNORECASE)
    )


def check_readme(
    package: zipfile.ZipFile,
    names: set[str],
    present: list[str],
    optional: list[str],
) -> None:
    """Require the packaged README to match the package it is inside.

    Every RID the package ships must be named, or the package page advertises
    less than it carries. The reverse matters too: a RID that may be absent
    (the ARM64 leg failed, `--allow-missing-rid`) must be described as
    conditional, because the same README ships whether or not that tree was
    staged. A reader of an x64-only package must not be told ARM64 is bundled.

    Every clause that names the optional RID is checked, not just one line.
    A README can say the right thing once and contradict it in a table row or a
    following sentence; a single-line search treats that as conditional.
    """
    if README not in names:
        raise PackageError(f"package is missing {README}")
    text = read_member(package, README).decode("utf-8", errors="replace")
    missing = [rid for rid in present if rid not in text]
    if missing:
        raise PackageError(
            f"{README} does not name the packaged Windows runtime(s): "
            + ", ".join(missing)
        )
    lines = text.splitlines()
    for rid in optional:
        mentions = [line for line in lines if readme_names(line, rid)]
        if not mentions:
            raise PackageError(
                f"{README} does not mention {rid}, which this package may omit"
            )
        for line in mentions:
            named = False
            for claim in re.split(r"(?<=[.!?])\s+", line):
                names_rid = readme_names(claim, rid)
                conditional = any(
                    pattern.search(claim) for pattern in README_CONDITIONAL
                )
                absolute = any(pattern.search(claim) for pattern in README_ABSOLUTE)
                shipping = README_SHIPPING.search(claim)
                # A trailing sentence on the same line can carry the claim
                # without repeating the name ("All four natives are bundled.").
                anaphoric = named and not names_rid and bool(absolute)
                if absolute and (names_rid or anaphoric):
                    raise PackageError(
                        f"{README} describes {rid} as unconditional in a package "
                        f"that omits it: {claim.strip()!r}. Mark every clause that "
                        f"names it conditional, e.g. \"`{rid}` "
                        f'{README_OPTIONAL_MARKER} the ARM64 build succeeds."'
                    )
                if names_rid and shipping and not conditional:
                    raise PackageError(
                        f"{README} describes {rid} as unconditional in a package "
                        f"that omits it: {claim.strip()!r}. Mark every clause that "
                        f"names it conditional, e.g. \"`{rid}` "
                        f'{README_OPTIONAL_MARKER} the ARM64 build succeeds."'
                    )
                named = named or names_rid


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
    """Every entry name, after rejecting a name that appears more than once.

    `zipfile.getinfo()` returns the last entry with a given name, while NuGet's
    extractor writes the first. A package containing both would pass this gate
    on one copy and install the other, so the duplicate is a failure rather
    than something to deduplicate.
    """
    seen: set[str] = set()
    repeated: list[str] = []
    for info in package.infolist():
        if info.filename in seen:
            repeated.append(info.filename)
        seen.add(info.filename)
    if repeated:
        raise PackageError(
            "package repeats entry name(s), so the bytes checked here are not "
            "the bytes NuGet installs: " + ", ".join(sorted(set(repeated)))
        )
    return seen


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
                        "the packaged README must describe it as conditional"
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

            check_readme(
                package,
                names,
                present,
                [rid for rid in required if rid not in present],
            )
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
