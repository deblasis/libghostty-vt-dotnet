#!/usr/bin/env python3
"""Fail a release or nightly when its version is not ahead of nuget.org.

The package version is independent of upstream Ghostty, so a missed
`version.json` bump can silently publish nightlies below the latest stable.
This script makes that a hard failure, and it also keeps RCs and respins from
being published where NuGet ordering makes them unreachable.

The index is the public flat-container document. `--index-file` exists so the
self-test can run the same comparison against fixtures without the network.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import urllib.error
import urllib.request
from functools import cmp_to_key

INDEX_URL = "https://api.nuget.org/v3-flatcontainer/deblasis.ghosttyvt/index.json"
CANONICAL = re.compile(r"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$")


def fail(message: str) -> int:
    print(f"::error::{message}", file=sys.stderr)
    return 1


def parse_version(raw: str):
    """Parse a NuGet version into ((major, minor, patch, revision), prerelease)."""
    raw = raw.strip()
    if not raw:
        raise ValueError("empty version")
    core = raw.split("+", 1)[0]
    main, sep, prerelease = core.partition("-")
    parts = main.split(".")
    if len(parts) not in (3, 4):
        raise ValueError(f"expected 3 or 4 numeric components: {raw}")
    if not all(part.isdigit() for part in parts):
        raise ValueError(f"non-numeric component: {raw}")
    numbers = [int(part) for part in parts]
    numbers += [0] * (4 - len(numbers))
    identifiers = tuple(prerelease.split(".")) if sep else None
    if identifiers is not None and any(not item for item in identifiers):
        raise ValueError(f"empty prerelease identifier: {raw}")
    return (tuple(numbers), identifiers)


def compare(left, right) -> int:
    if left[0] != right[0]:
        return -1 if left[0] < right[0] else 1
    if left[1] is None and right[1] is None:
        return 0
    if left[1] is None:
        return 1
    if right[1] is None:
        return -1
    for a, b in zip(left[1], right[1]):
        if a == b:
            continue
        a_num, b_num = a.isdigit(), b.isdigit()
        if a_num and b_num:
            return -1 if int(a) < int(b) else 1
        if a_num:
            return -1
        if b_num:
            return 1
        return -1 if a.lower() < b.lower() else 1
    if len(left[1]) == len(right[1]):
        return 0
    return -1 if len(left[1]) < len(right[1]) else 1


def require_canonical(raw: str, label: str) -> str:
    if not CANONICAL.fullmatch(raw):
        raise ValueError(f"{label} must be canonical X.Y.Z: {raw}")
    return raw


def classify(version: str) -> str:
    core = version.split("+", 1)[0].split("-", 1)[0]
    if "-rc." in version:
        if core.count(".") != 2:
            raise ValueError(f"combined respin/RC shape is not allowed: {version}")
        return "rc"
    if "-" in version:
        raise ValueError(f"unsupported prerelease label: {version}")
    return "respin" if core.count(".") == 3 else "stable"


def load_versions(index_file: str | None, index_url: str) -> list[str]:
    if index_file:
        with open(index_file, encoding="utf-8") as handle:
            data = json.load(handle)
    else:
        request = urllib.request.Request(
            index_url, headers={"User-Agent": "libghostty-vt-dotnet-release-guard"}
        )
        try:
            with urllib.request.urlopen(request, timeout=20) as response:
                data = json.load(response)
        except urllib.error.HTTPError as error:
            if error.code == 404:
                return []
            raise

    versions = data.get("versions")
    if not isinstance(versions, list):
        raise ValueError("NuGet index has no versions array")
    return [str(version) for version in versions]


def main() -> int:
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(dest="channel", required=True)

    nightly = subparsers.add_parser("nightly")
    nightly.add_argument("--version", required=True, help="nightly base version, X.Y.Z")

    release = subparsers.add_parser("release")
    release.add_argument("--version", required=True, help="exact release version")
    release.add_argument("--base", help="X.Y.Z base (required for rc/respin)")

    for command in (nightly, release):
        command.add_argument("--index-file", help="read a fixture instead of nuget.org")
        command.add_argument("--index-url", default=INDEX_URL)

    args = parser.parse_args()

    try:
        raw_versions = load_versions(args.index_file, args.index_url)
        versions = [parse_version(raw) for raw in raw_versions]
    except Exception as error:  # noqa: BLE001 - fail closed on any index problem
        return fail(f"Could not read NuGet versions: {error}")

    stable = [version for version in versions if version[1] is None]
    highest_stable = max(stable, key=cmp_to_key(compare)) if stable else None
    highest_any = max(versions, key=cmp_to_key(compare)) if versions else None

    try:
        requested = parse_version(args.version)
        if args.channel == "nightly":
            require_canonical(args.version, "nightly version")
            if highest_stable is not None and compare(requested, highest_stable) <= 0:
                return fail(
                    f"Nightly base {args.version} is not ahead of the latest published "
                    f"stable {format_key(highest_stable)}. Bump version.json before the next nightly."
                )
            print(f"Nightly base {args.version} is ahead of the latest stable.")
            return 0

        kind = classify(args.version)
        base = args.base
        if kind in ("rc", "respin"):
            if not base:
                return fail(f"--base is required for a {kind} release")
            require_canonical(base, "release base")
            core = args.version.split("+", 1)[0].split("-", 1)[0]
            expected_base = ".".join(core.split(".")[:3])
            if base != expected_base:
                return fail(f"Release base {base} does not match version {args.version}")
        else:
            base = args.version

        if kind == "stable":
            if highest_any is not None and compare(requested, highest_any) <= 0:
                return fail(
                    f"Stable {args.version} is not ahead of the highest published version "
                    f"{format_key(highest_any)}."
                )
        elif kind == "rc":
            if highest_stable is not None and compare(parse_version(base), highest_stable) <= 0:
                return fail(
                    f"RC base {base} is not ahead of the latest published stable "
                    f"{format_key(highest_stable)}. An RC below stable is unreachable."
                )
            if highest_any is not None and compare(requested, highest_any) <= 0:
                return fail(
                    f"RC {args.version} is not ahead of the highest published version "
                    f"{format_key(highest_any)}."
                )
        else:
            if highest_stable is None:
                return fail(f"Respin {args.version} requires a published stable release.")
            if parse_version(base)[0][:3] != highest_stable[0][:3]:
                return fail(
                    f"Respin base {base} is not the latest stable "
                    f"{format_key(highest_stable)}."
                )
            if compare(requested, highest_stable) <= 0:
                return fail(
                    f"Respin {args.version} is not ahead of the latest stable "
                    f"{format_key(highest_stable)}."
                )

        label = {"stable": "Stable", "rc": "RC", "respin": "Respin"}[kind]
        print(f"{label} {args.version} is ahead of the published versions.")
        return 0
    except ValueError as error:
        return fail(str(error))


def format_key(key) -> str:
    numbers, prerelease = key
    core = ".".join(str(number) for number in numbers[:3])
    if numbers[3]:
        core = f"{core}.{numbers[3]}"
    if prerelease:
        core = f"{core}-{'.'.join(prerelease)}"
    return core


if __name__ == "__main__":
    sys.exit(main())
