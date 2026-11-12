#!/usr/bin/env python3
"""Prove check-nupkg.py rejects incomplete or mismatched Windows packages.

Builds throwaway .nupkg files from real DLL/PDB fixtures and asserts:

1. no natives at all: must fail
2. win-x64 pair only: must fail (win-arm64 is required by default)
3. both pairs: must pass
4. both pairs, but the win-arm64 PDB replaced by the win-x64 PDB: must fail
5. both pairs, but the win-arm64 DLL replaced by the win-x64 DLL: must fail
6. win-x64 pair only, with win-arm64 allowed to be missing: must pass
7. win-arm64 DLL only, with win-arm64 allowed to be missing: must fail
   (a skipped RID must not hide a partial or wrong-architecture tree)
8. an extra Windows runtime tree (win-x86): must fail
9. an extra file inside a checked native directory: must fail
10. a README that omits a packaged Windows RID: must fail
11. the same RID both required and allowed to be missing: must fail
12. an encrypted member: must fail with an annotated error, not a traceback
13. a backslash-separated extra file: must fail (it is a path separator on Windows)
14. a backslash-separated unknown RID: must fail
15. a backslash-separated duplicate of a canonical native file: must fail
16. an uppercase RID: must fail
17. an uppercase native segment: must fail
18. an absolute path: must fail
19. a drive-prefixed path: must fail
20. a `..` segment: must fail
21. a repeated entry name: must fail (zipfile reads the last copy, NuGet writes the first)
22. a `win10-x64` runtime tree: must fail (a Windows RID the gate does not know)
23. a `base` runtime tree: must fail (the RID graph root fallback)
24. a `./runtimes/any/...` entry: must fail (NuGet normalizes the leading `./`)

Usage: selftest-check-nupkg.py --x64-dir DIR --arm64-dir DIR
"""

from __future__ import annotations

import argparse
import struct
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

CHECKER = Path(__file__).with_name("check-nupkg.py")
DLL = "ghostty-vt.dll"
PDB = "ghostty-vt.pdb"
README = "README.md"


class SelftestError(Exception):
    pass


def pair(directory: Path) -> dict[str, bytes]:
    dll = directory / DLL
    pdb = directory / PDB
    if not dll.is_file() or not pdb.is_file():
        raise SelftestError(f"{directory} must contain {DLL} and {PDB}")
    return {DLL: dll.read_bytes(), PDB: pdb.read_bytes()}


def write_package(path: Path, entries: dict[str, bytes]) -> None:
    with zipfile.ZipFile(path, "w") as package:
        for name, data in entries.items():
            package.writestr(name, data)


def compile_image(
    natives: dict[str, dict[str, bytes]],
    rids: list[str],
    readme: str | None = None,
) -> dict[str, bytes]:
    entries: dict[str, bytes] = {}
    for rid, files in natives.items():
        for name, data in files.items():
            entries[f"runtimes/{rid}/native/{name}"] = data
    if readme is None:
        present = [rid for rid in rids if rid in natives]
        absent = [rid for rid in rids if rid not in natives]
        listed = ", ".join(f"`{rid}`" for rid in present)
        lines = ["# Package", f"Native runtimes for {listed} are bundled."]
        for rid in absent:
            lines.append(
                f"`{rid}` is included when the ARM64 build succeeds."
            )
        readme = "\n".join(lines)
    entries[README] = readme.encode("utf-8")
    return entries


def encrypt(path: Path) -> None:
    data = bytearray(path.read_bytes())
    for magic, offset in ((b"PK\x03\x04", 6), (b"PK\x01\x02", 8)):
        position = 0
        while (position := data.find(magic, position)) >= 0:
            field = struct.unpack_from("<H", data, position + offset)[0]
            struct.pack_into("<H", data, position + offset, field | 1)
            position += 4
    path.write_bytes(data)


def run(package: Path, *args: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(CHECKER), str(package), *args],
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


def package(
    root: Path,
    name: str,
    natives: dict[str, dict[str, bytes]],
    rids: list[str] | None = None,
    readme: str | None = None,
) -> Path:
    path = root / name
    write_package(
        path,
        compile_image(
            natives,
            rids if rids is not None else list(natives),
            readme,
        ),
    )
    return path


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--x64-dir", type=Path, required=True)
    parser.add_argument("--arm64-dir", type=Path, required=True)
    args = parser.parse_args()

    try:
        x64 = pair(args.x64_dir)
        arm64 = pair(args.arm64_dir)
    except (OSError, SelftestError) as error:
        print(f"::error::{error}", file=sys.stderr)
        return 1

    try:
        with tempfile.TemporaryDirectory(prefix="check-nupkg-selftest-") as directory:
            root = Path(directory)

            empty = root / "empty.nupkg"
            write_package(empty, {README: b"# Package"})
            expect("no natives", run(empty), accepted=False)

            expect(
                "missing win-arm64",
                run(package(root, "x64-only.nupkg", {"win-x64": x64})),
                accepted=False,
            )

            complete = package(root, "complete.nupkg", {"win-x64": x64, "win-arm64": arm64})
            expect("complete package", run(complete), accepted=True)

            wrong_pdb = package(
                root,
                "wrong-pdb.nupkg",
                {
                    "win-x64": x64,
                    "win-arm64": {DLL: arm64[DLL], PDB: x64[PDB]},
                },
            )
            expect("foreign win-arm64 PDB", run(wrong_pdb), accepted=False)

            wrong_dll = package(
                root,
                "wrong-dll.nupkg",
                {
                    "win-x64": x64,
                    "win-arm64": {DLL: x64[DLL], PDB: arm64[PDB]},
                },
            )
            expect("wrong win-arm64 machine", run(wrong_dll), accepted=False)

            x64_only = package(
                root,
                "optional-x64.nupkg",
                {"win-x64": x64},
                rids=["win-x64", "win-arm64"],
            )
            optional = ("--allow-missing-rid", "win-arm64")
            expect(
                "arm64 absent and allowed",
                run(x64_only, *optional),
                accepted=True,
            )

            # The same README ships whether or not the ARM64 tree was staged,
            # so a package that omits it must not describe it as bundled.
            bundled = (
                "# Package\nNative runtimes for `win-x64` and `win-arm64` "
                "are bundled.\n"
            )
            claims_arm64 = package(
                root,
                "claims-absent.nupkg",
                {"win-x64": x64},
                readme=bundled,
            )
            result = run(claims_arm64, *optional)
            expect("README claims an absent RID is bundled", result, accepted=False)
            if "win-arm64" not in result.stderr:
                raise SelftestError(
                    "the absent-RID rejection did not name win-arm64\n"
                    f"stderr: {result.stderr.strip()}"
                )

            conditional_readme = (
                "# Package\nNative runtimes for `win-x64` are bundled. "
                "`win-arm64` is included when the ARM64 build succeeds.\n"
            )
            expect(
                "README marks the optional RID conditional",
                run(
                    package(
                        root,
                        "conditional-arm64.nupkg",
                        {"win-x64": x64},
                        readme=conditional_readme,
                    ),
                    *optional,
                ),
                accepted=True,
            )

            # One correct sentence does not excuse a contradicting one. The
            # same RID is conditional in the prose and absolute in the table
            # row, which is how a README edit usually goes wrong.
            mixed = (
                "# Package\nNative runtimes for `win-x64` are bundled. "
                "`win-arm64` is included when the ARM64 build succeeds.\n\n"
                "| Platform | RID |\n| --- | --- |\n"
                "| Windows ARM64 | `win-arm64` | always bundled |\n"
            )
            result = run(
                package(
                    root,
                    "mixed-arm64.nupkg",
                    {"win-x64": x64},
                    readme=mixed,
                ),
                *optional,
            )
            expect("README contradicts itself about an absent RID", result, accepted=False)
            if "win-arm64" not in result.stderr:
                raise SelftestError(
                    "the contradicting-README rejection did not name win-arm64\n"
                    f"stderr: {result.stderr.strip()}"
                )

            # The absolute words the reviewers named, one at a time.
            for claim in ("always bundled", "all four runtimes", "are bundled"):
                text = (
                    "# Package\nNative runtimes for `win-x64` are bundled. "
                    f"`win-arm64` is {claim}.\n"
                )
                result = run(
                    package(
                        root,
                        f"absolute-{claim.split()[0]}.nupkg",
                        {"win-x64": x64},
                        readme=text,
                    ),
                    *optional,
                )
                expect(
                    f"README calls an absent RID {claim!r}", result, accepted=False
                )

            partial = package(
                root, "partial-arm64.nupkg", {"win-x64": x64, "win-arm64": {DLL: arm64[DLL]}}
            )
            expect(
                "arm64 DLL without its PDB, even when optional",
                run(partial, *optional),
                accepted=False,
            )

            stray = package(
                root,
                "stray-tree.nupkg",
                {"win-x64": x64, "win-arm64": arm64, "win-x86": x64},
            )
            expect("unknown Windows runtime tree", run(stray), accepted=False)

            # `win`, `any` and `base` are not shipped RIDs; they are the RID
            # graph fallbacks NuGet uses when a specific tree is absent. A
            # native there would be selected on Windows without ever entering
            # the trees this gate inspects, so the package must not contain one.
            for case, entry in (
                ("win fallback tree", "runtimes/win/native/" + DLL),
                ("any fallback tree", "runtimes/any/native/" + DLL),
                ("base fallback tree", "runtimes/base/native/" + DLL),
            ):
                fallback = package(
                    root,
                    f"fallback-{case.split()[0]}.nupkg",
                    {"win-x64": x64, "win-arm64": arm64},
                )
                with zipfile.ZipFile(fallback, "a") as archive:
                    archive.writestr(entry, x64[DLL])
                result = run(fallback)
                expect(case, result, accepted=False)
                if entry not in result.stderr:
                    raise SelftestError(
                        f"{case}: rejection did not name {entry}\n"
                        f"stderr: {result.stderr.strip()}"
                    )

            extra_file = package(root, "extra-file.nupkg", {"win-x64": x64, "win-arm64": arm64})
            with zipfile.ZipFile(extra_file, "a") as archive:
                archive.writestr("runtimes/win-x64/native/vcruntime140.dll", b"MZ")
            expect("extra file in a checked tree", run(extra_file), accepted=False)

            # NuGet's extractor writes the first copy of a repeated name while
            # `zipfile.getinfo()` returns the last. Accepting the package would
            # mean the gate checked bytes the consumer never installs.
            duplicate = package(
                root, "duplicate-member.nupkg", {"win-x64": x64, "win-arm64": arm64}
            )
            repeated = "runtimes/win-x64/native/" + DLL
            with zipfile.ZipFile(duplicate, "a") as archive:
                archive.writestr(repeated, x64[DLL])
            result = run(duplicate)
            expect("repeated native entry", result, accepted=False)
            if repeated not in result.stderr:
                raise SelftestError(
                    f"the duplicate rejection did not name {repeated}\n"
                    f"stderr: {result.stderr.strip()}"
                )

            # `win10-x64` is a Windows RID NuGet resolves, so the gate must not
            # skip it for the sole reason that it does not start with `win-`.
            versioned = package(
                root, "win10-tree.nupkg", {"win-x64": x64, "win-arm64": arm64}
            )
            with zipfile.ZipFile(versioned, "a") as archive:
                archive.writestr("runtimes/win10-x64/native/" + DLL, x64[DLL])
            result = run(versioned)
            expect("versioned Windows RID tree", result, accepted=False)
            if "win10-x64" not in result.stderr:
                raise SelftestError(
                    "the versioned-RID rejection did not name win10-x64\n"
                    f"stderr: {result.stderr.strip()}"
                )

            # NuGet normalizes a leading `./`, so this entry lands under the
            # `any` fallback; a sweep that splits the name sees `"."` and skips
            # it. The gate has to reject the spelling, not normalize it.
            dot_relative = package(
                root, "dot-relative.nupkg", {"win-x64": x64, "win-arm64": arm64}
            )
            dotted = "./runtimes/any/native/" + DLL
            with zipfile.ZipFile(dot_relative, "a") as archive:
                archive.writestr(dotted, x64[DLL])
            result = run(dot_relative)
            expect("dot-relative fallback tree", result, accepted=False)
            if dotted not in result.stderr:
                raise SelftestError(
                    f"the dot-relative rejection did not name {dotted}\n"
                    f"stderr: {result.stderr.strip()}"
                )

            for case, entry in (
                ("backslash extra file", "runtimes\\win-x64\\native\\vcruntime140.dll"),
                ("backslash unknown RID", "runtimes\\win-x86\\native\\" + DLL),
                ("backslash duplicate", "runtimes\\win-x64\\native\\" + DLL),
                ("uppercase RID", "runtimes/WIN-X86/native/" + DLL),
                ("uppercase native segment", "runtimes/win-x64/Native/vcruntime140.dll"),
                ("absolute path", "/runtimes/win-x64/native/vcruntime140.dll"),
                ("drive prefix", "C:/runtimes/win-x64/native/vcruntime140.dll"),
                ("escaping path", "runtimes/win-x64/native/../../vcruntime140.dll"),
            ):
                mutated = package(
                    root,
                    f"spelling-{case.replace(' ', '-')}.nupkg",
                    {"win-x64": x64, "win-arm64": arm64},
                )
                with zipfile.ZipFile(mutated, "a") as archive:
                    archive.writestr(entry, b"MZ")
                expect(f"{case} spelling", run(mutated), accepted=False)

            quiet_readme = package(
                root,
                "quiet-readme.nupkg",
                {"win-x64": x64, "win-arm64": arm64},
                rids=["win-x64"],
            )
            expect("README omits a packaged RID", run(quiet_readme), accepted=False)

            overlap = ("--require-rid", "win-arm64", "--allow-missing-rid", "win-arm64")
            expect("RID both required and optional", run(complete, *overlap), accepted=False)

            encrypted = package(root, "encrypted.nupkg", {"win-x64": x64, "win-arm64": arm64})
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

    print("Windows package content self-test passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
