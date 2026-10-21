#!/usr/bin/env python3
"""Check that the compiled native layer covers the generated reference.

The reference is the generator's full output at the pin. The compiled layer is
derived from it and ships inside Ghostty.Vt. This check makes three claims:

1. the committed compiled file is reproducible from the reference;
2. every generated P/Invoke entry point is present exactly once;
3. no declaration leaked back to public visibility.

Usage: build/check-native-coverage.py
"""

from __future__ import annotations

import re
import subprocess
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
REFERENCE = REPO / "build" / "bindings-reference" / "NativeMethods.pinned.cs"
COMPILED = REPO / "src" / "Ghostty.Vt" / "Native" / "Generated" / "LibGhosttyVt.g.cs"
EMITTER = REPO / "build" / "emit-compiled-native.py"

NAME = re.compile(r"\b(ghostty_[a-z0-9_]+)\s*\(")


def fail(message: str) -> None:
    print(f"::error::{message}", file=sys.stderr)
    raise SystemExit(1)


def extern_names(path: Path) -> set[str]:
    names: set[str] = set()
    for line in path.read_text(encoding="utf-8").splitlines():
        if "static extern" in line:
            names.update(NAME.findall(line))
    return names


def main() -> None:
    for path in (REFERENCE, COMPILED, EMITTER):
        if not path.exists():
            fail(f"missing required file: {path}")

    expected = extern_names(REFERENCE)
    actual = extern_names(COMPILED)
    if not expected:
        fail(f"no extern declarations found in {REFERENCE}")

    missing = sorted(expected - actual)
    extra = sorted(actual - expected)
    if missing or extra:
        for name in missing:
            print(f"  missing from compiled layer: {name}", file=sys.stderr)
        for name in extra:
            print(f"  not in generated reference: {name}", file=sys.stderr)
        fail(
            f"compiled layer does not match the reference "
            f"({len(actual)} vs {len(expected)} entry points)"
        )

    public = [
        f"{line_number}: {line.strip()}"
        for line_number, line in enumerate(
            COMPILED.read_text(encoding="utf-8").splitlines(), 1
        )
        if re.match(r"^\s*public\s", line)
    ]
    if public:
        print("\n".join(public[:20]), file=sys.stderr)
        fail("compiled layer contains public declarations; keep it internal")

    if "namespace Ghostty.Vt.Native.Generated;" not in COMPILED.read_text(encoding="utf-8"):
        fail("compiled layer does not use the Generated namespace")
    if "class LibGhosttyVt" not in COMPILED.read_text(encoding="utf-8"):
        fail("compiled layer does not define LibGhosttyVt")

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
