#!/usr/bin/env python3
"""Check that the raw ABI layer matches the generated reference.

The reference is the generator's full output at the pin. The raw layer is
derived from it and ships inside Ghostty.Vt as Ghostty.Vt.Raw. This check
makes four claims:

1. every generated entry point is present exactly once, with the same
   signature;
2. the generated constants, class set and library name agree;
3. the entry points and the containing class are public, and the file has the
   expected namespace and header directives;
4. the committed file is reproducible from the reference.

Usage: build/check-native-coverage.py
"""

from __future__ import annotations

import re
import subprocess
import sys
import tempfile
from collections import Counter
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
REFERENCE = REPO / "build" / "bindings-reference" / "NativeMethods.pinned.cs"
COMPILED = REPO / "src" / "Ghostty.Vt" / "Raw" / "GhosttyVtNative.g.cs"
EMITTER = REPO / "build" / "emit-compiled-native.py"

EXTERN = re.compile(
    r"^\s*(?:public|internal)\s+static\s+extern\s+(?P<ret>.+?)\s+"
    r"(?P<name>ghostty_[a-z0-9_]+)\((?P<params>.*)\);\s*$"
)
CONSTANT = re.compile(
    r"^\s*(?:public|internal)\s+(?:static\s+readonly\s+|const\s+).*"
    r"\b(?P<name>GHOSTTY_[A-Z0-9_]+)\s*="
)
CLASS = re.compile(
    r"^\s*(?:public|internal)\s+(?:static\s+)?(?:unsafe\s+)?"
    r"(?:sealed\s+)?(?:partial\s+)?class\s+(?P<name>[A-Za-z_][A-Za-z0-9_]*)"
)
TYPE = re.compile(
    r"^\s*(?P<mods>(?:(?:public|internal|static|unsafe|sealed|partial|readonly)\s+)*)"
    r"(?P<kind>struct|enum|class)\s+(?P<name>[A-Za-z_][A-Za-z0-9_]*)"
)
INTERNAL_HELPERS = {"NativeTypeNameAttribute", "NativeAnnotationAttribute"}


def fail(message: str) -> None:
    print(f"::error::{message}", file=sys.stderr)
    raise SystemExit(1)


def read(path: Path) -> str:
    if not path.exists():
        fail(f"missing required file: {path}")
    return path.read_text(encoding="utf-8")


def extern_declarations(text: str, label: str) -> dict[str, str]:
    declarations: dict[str, str] = {}
    for line_number, line in enumerate(text.splitlines(), 1):
        if "static extern" not in line:
            continue
        match = EXTERN.match(line)
        if not match:
            fail(f"{label}:{line_number}: could not parse extern declaration")
        name = match.group("name")
        signature = " ".join(
            f"{match.group('ret')} {name}({match.group('params')})".split()
        )
        if name in declarations:
            fail(f"{label}:{line_number}: duplicate entry point {name}")
        declarations[name] = signature
    return declarations


def constants(text: str, label: str) -> Counter[str]:
    found: Counter[str] = Counter()
    for line_number, line in enumerate(text.splitlines(), 1):
        match = CONSTANT.match(line)
        if not match:
            continue
        normalized = re.sub(r"^\s*(?:public|internal)\s+", "", line).strip()
        found[normalized] += 1
    if not found:
        fail(f"{label}: no generated constants found")
    return found


def class_names(text: str, label: str) -> set[str]:
    names: set[str] = set()
    for line_number, line in enumerate(text.splitlines(), 1):
        match = CLASS.match(line)
        if match:
            names.add(match.group("name"))
    if not names:
        fail(f"{label}: no classes found")
    return names


def normalized_content(text: str, *, compiled: bool) -> list[str]:
    lines: list[str] = []
    for line in text.splitlines():
        stripped = line.strip()
        if not stripped:
            continue
        if stripped.startswith("//"):
            continue
        # Skip only pure top-level directives. A line that starts with `using`
        # but carries code after the semicolon is content and must be compared.
        if line == line.lstrip() and (
            re.fullmatch(r"using [^;]+;", stripped)
            or re.fullmatch(r"namespace [A-Za-z0-9_.]+;?", stripped)
        ):
            continue
        if stripped in {"{", "}"}:
            continue
        # Access differs by design: the reference is public, the compiled layer
        # is internal. Strip the modifier on both sides before comparing.
        stripped = re.sub(r"^(?:public|internal)\b", "", stripped).strip()
        if compiled:
            stripped = stripped.replace(
                'DllImport("ghostty-vt"', 'DllImport("libghostty-vt"'
            )
            stripped = stripped.replace("class GhosttyVtNative", "class NativeMethods")
            stripped = stripped.replace(
                "((ushort)(ansi ? 1 : 0) << 15)", "((ushort)(ansi) << 15)"
            )
            stripped = stripped.replace(
                "return (mode >> 15) != 0;", "return ((mode >> 15) != 0) != 0;"
            )
            stripped = stripped.replace(
                "return (ushort)(mode & 0x7FFF);", "return mode & 0x7FFF;"
            )
        lines.append(stripped)
    return lines


def main() -> None:
    reference = read(REFERENCE)
    compiled = read(COMPILED)
    if not EMITTER.exists():
        fail(f"missing required file: {EMITTER}")

    expected = extern_declarations(reference, "reference")
    actual = extern_declarations(compiled, "compiled")
    if not expected:
        fail("reference has no extern declarations")
    if expected != actual:
        missing = sorted(set(expected) - set(actual))
        extra = sorted(set(actual) - set(expected))
        changed = sorted(
            name for name in set(expected) & set(actual) if expected[name] != actual[name]
        )
        for name in missing:
            print(f"  missing from compiled layer: {name}", file=sys.stderr)
        for name in extra:
            print(f"  not in generated reference: {name}", file=sys.stderr)
        for name in changed:
            print(f"  signature changed: {name}", file=sys.stderr)
        fail(
            f"compiled layer does not match the reference "
            f"({len(actual)} vs {len(expected)} entry points)"
        )

    reference_constants = constants(reference, "reference")
    compiled_constants = constants(compiled, "compiled")
    if reference_constants != compiled_constants:
        for line in sorted((reference_constants - compiled_constants).elements()):
            print(f"  missing constant: {line}", file=sys.stderr)
        for line in sorted((compiled_constants - reference_constants).elements()):
            print(f"  extra constant: {line}", file=sys.stderr)
        fail("generated constants do not match the reference")

    reference_classes = class_names(reference, "reference")
    compiled_classes = class_names(compiled, "compiled")
    expected_classes = {"NativeMethods", "NativeTypeNameAttribute", "NativeAnnotationAttribute"}
    if reference_classes != expected_classes:
        fail(f"unexpected class set in reference: {sorted(reference_classes)}")
    if compiled_classes != {
        "GhosttyVtNative",
        "NativeTypeNameAttribute",
        "NativeAnnotationAttribute",
    }:
        fail(f"unexpected class set in compiled layer: {sorted(compiled_classes)}")

    text = read(COMPILED)
    if not re.search(
        r"(?m)^\s*public\s+static\s+unsafe\s+partial\s+class\s+GhosttyVtNative\b",
        text,
    ):
        fail("the raw entry-point class must be public")
    for line_number, line in enumerate(text.splitlines(), 1):
        if "static extern" in line and not line.lstrip().startswith(
            "public static extern"
        ):
            fail(f"{COMPILED}:{line_number}: raw entry points must be public")

        type_match = TYPE.match(line)
        if type_match:
            modifiers = type_match.group("mods").split()
            name = type_match.group("name")
            if "public" not in modifiers and "internal" not in modifiers:
                fail(
                    f"{COMPILED}:{line_number}: generated type {name} has no "
                    "access modifier; raw types must be explicitly public"
                )
            if "internal" in modifiers and name not in INTERNAL_HELPERS:
                fail(
                    f"{COMPILED}:{line_number}: generated type {name} is "
                    "internal; raw types must be public"
                )
            if "public" in modifiers and name in INTERNAL_HELPERS:
                fail(
                    f"{COMPILED}:{line_number}: helper type {name} must stay "
                    "internal"
                )

    imports = [
        line
        for line in text.splitlines()
        if "DllImport(" in line
    ]
    if len(imports) != len(actual):
        fail(
            f"expected {len(actual)} DllImport attributes, found {len(imports)}"
        )
    for line in imports:
        if 'DllImport("ghostty-vt"' not in line:
            fail(f"unexpected DllImport library: {line.strip()}")
        if "CallingConvention.Cdecl" not in line or "ExactSpelling = true" not in line:
            fail(f"DllImport is missing Cdecl/ExactSpelling: {line.strip()}")

    expected_lines = normalized_content(reference, compiled=False)
    actual_lines = normalized_content(compiled, compiled=True)
    if expected_lines != actual_lines:
        for index, (expected_line, actual_line) in enumerate(
            zip(expected_lines, actual_lines)
        ):
            if expected_line != actual_line:
                print(f"  first difference at content line {index + 1}:", file=sys.stderr)
                print(f"    reference: {expected_line}", file=sys.stderr)
                print(f"    compiled:  {actual_line}", file=sys.stderr)
                break
        if len(expected_lines) != len(actual_lines):
            print(
                f"  content line count differs: {len(expected_lines)} vs {len(actual_lines)}",
                file=sys.stderr,
            )
        fail("compiled layer content differs from the reference beyond the documented rewrites")

    namespace_lines = [
        line.strip()
        for line in compiled.splitlines()
        if line.startswith("namespace ")
    ]
    if namespace_lines != ["namespace Ghostty.Vt.Raw;"]:
        fail(f"unexpected output namespace: {namespace_lines}")

    header_usings = [
        line for line in compiled.splitlines() if line.startswith("using ")
    ]
    expected_usings = [
        "using System;",
        "using System.Diagnostics;",
        "using System.Runtime.CompilerServices;",
        "using System.Runtime.InteropServices;",
    ]
    if header_usings != expected_usings:
        fail(f"unexpected header using directives: {header_usings}")

    with tempfile.TemporaryDirectory() as tmp:
        regenerated = Path(tmp) / "GhosttyVtNative.g.cs"
        subprocess.run(
            [sys.executable, str(EMITTER), str(REFERENCE), str(regenerated)],
            check=True,
        )
        if regenerated.read_bytes() != COMPILED.read_bytes():
            fail(
                "compiled layer is not reproducible from the reference; "
                "run build/emit-compiled-native.py and commit the result"
            )

    print(f"compiled native layer matches {len(expected)} generated entry points")


if __name__ == "__main__":
    main()
