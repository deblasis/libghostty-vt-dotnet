#!/usr/bin/env python3
"""Check that the compiled native layer matches the generated reference.

The reference is the generator's full output at the pin. The compiled layer is
derived from it and ships inside Ghostty.Vt. This check makes four claims:

1. every generated entry point is present exactly once, with the same
   signature;
2. the generated constants, class set and library name agree;
3. no declaration leaked back to public visibility;
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
COMPILED = REPO / "src" / "Ghostty.Vt" / "Native" / "Generated" / "LibGhosttyVt.g.cs"
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


def fail(message: str) -> None:
    print(f"::error::{message}", file=sys.stderr)
    raise SystemExit(1)


def read(path: Path) -> str:
    if not path.exists():
        fail(f"missing required file: {path}")
    return path.read_text(encoding="utf-8")


def code_only(line: str) -> str:
    line = re.sub(r"//.*", "", line)
    return re.sub(r'"(?:\\.|[^"\\])*"', '""', line)


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


def normalized_content(text: str, *, compiled: bool) -> Counter[str]:
    lines: Counter[str] = Counter()
    for line in text.splitlines():
        stripped = line.strip()
        if not stripped:
            continue
        if stripped.startswith("//"):
            continue
        if stripped.startswith("using ") or stripped.startswith("namespace "):
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
            stripped = stripped.replace("class LibGhosttyVt", "class NativeMethods")
            stripped = stripped.replace(
                "((ushort)(ansi ? 1 : 0) << 15)", "((ushort)(ansi) << 15)"
            )
            stripped = stripped.replace(
                "return (mode >> 15) != 0;", "return ((mode >> 15) != 0) != 0;"
            )
            stripped = stripped.replace(
                "return (ushort)(mode & 0x7FFF);", "return mode & 0x7FFF;"
            )
        lines[stripped] += 1
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
    actual_classes = {
        "LibGhosttyVt" if name == "LibGhosttyVt" else name for name in compiled_classes
    }
    if actual_classes != {
        "LibGhosttyVt",
        "NativeTypeNameAttribute",
        "NativeAnnotationAttribute",
    }:
        fail(f"unexpected class set in compiled layer: {sorted(compiled_classes)}")

    text = read(COMPILED)
    for line_number, line in enumerate(text.splitlines(), 1):
        if re.search(r"\bpublic\b", code_only(line)):
            fail(f"{COMPILED}:{line_number}: public declaration in the internal layer")

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

    if normalized_content(reference, compiled=False) != normalized_content(
        compiled, compiled=True
    ):
        expected_lines = normalized_content(reference, compiled=False)
        actual_lines = normalized_content(compiled, compiled=True)
        for line in sorted((expected_lines - actual_lines).elements())[:20]:
            print(f"  missing content: {line}", file=sys.stderr)
        for line in sorted((actual_lines - expected_lines).elements())[:20]:
            print(f"  extra content: {line}", file=sys.stderr)
        fail("compiled layer content differs from the reference beyond the documented rewrites")

    with tempfile.TemporaryDirectory() as tmp:
        regenerated = Path(tmp) / "LibGhosttyVt.g.cs"
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
