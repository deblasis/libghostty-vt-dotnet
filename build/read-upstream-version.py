#!/usr/bin/env python3
"""Read and validate the top-level .version from a Zig build.zig.zon.

A regex over the whole file can pick a comment or a nested dependency's
.version. This walks the document instead: it skips comments and strings,
tracks brace depth, and only accepts `.version` at the top level. The value is
validated before any workflow interpolates it, because the nightly publish job
has repository write access and a NuGet key.
"""

from __future__ import annotations

import re
import sys

SEMVER = re.compile(
    r"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)"
    r"(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$"
)


def fail(message: str) -> int:
    print(f"::error::{message}", file=sys.stderr)
    return 1


def read_version(text: str) -> str | None:
    index = 0
    depth = 0
    in_string = False
    escaped = False

    while index < len(text):
        char = text[index]

        if in_string:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            index += 1
            continue

        if char == '"':
            in_string = True
            index += 1
            continue

        if char == "/" and index + 1 < len(text) and text[index + 1] == "/":
            newline = text.find("\n", index)
            index = len(text) if newline < 0 else newline
            continue

        if char == "{":
            depth += 1
            index += 1
            continue

        if char == "}":
            depth -= 1
            index += 1
            continue

        if (
            depth == 1
            and text.startswith(".version", index)
            and (index == 0 or not (text[index - 1].isalnum() or text[index - 1] == "_"))
        ):
            cursor = index + len(".version")
            while cursor < len(text) and text[cursor].isspace():
                cursor += 1
            if cursor < len(text) and text[cursor] == "=":
                cursor += 1
                while cursor < len(text) and text[cursor].isspace():
                    cursor += 1
                if cursor < len(text) and text[cursor] == '"':
                    cursor += 1
                    value = []
                    escaped = False
                    while cursor < len(text):
                        current = text[cursor]
                        if escaped:
                            value.append(current)
                            escaped = False
                        elif current == "\\":
                            escaped = True
                        elif current == '"':
                            return "".join(value)
                        else:
                            value.append(current)
                        cursor += 1
            index = cursor
            continue

        index += 1

    return None


def main() -> int:
    version = read_version(sys.stdin.read())
    if not version:
        return fail("Could not parse top-level .version from build.zig.zon")
    if not SEMVER.fullmatch(version):
        return fail(f"Upstream version is not a valid SemVer string: {version!r}")
    print(version)
    return 0


if __name__ == "__main__":
    sys.exit(main())
