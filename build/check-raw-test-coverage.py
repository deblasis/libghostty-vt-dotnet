#!/usr/bin/env python3
"""Require every public raw ABI entry point to appear in the test suite.

This is a source-level coverage gate, not a line-coverage claim: it catches a
raw function that ships without even a direct test reference. Behavior for the
referenced entry points still comes from the tests themselves.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
RAW = REPO / "src" / "Ghostty.Vt" / "Raw" / "GhosttyVtNative.g.cs"
TESTS = REPO / "tests"

EXTERN = re.compile(r"public static extern [^(]+\b(ghostty_[a-z0-9_]+)\(")
IDENTIFIER = re.compile(r"\b(ghostty_[a-z0-9_]+)\b")


def fail(message: str) -> None:
    print(f"::error::{message}", file=sys.stderr)
    raise SystemExit(1)


raw_text = RAW.read_text(encoding="utf-8")
raw_names = set(EXTERN.findall(raw_text))
if not raw_names:
    fail(f"could not parse any raw entry points from {RAW}")

test_names: set[str] = set()
for path in TESTS.rglob("*.cs"):
    test_names.update(IDENTIFIER.findall(path.read_text(encoding="utf-8")))

missing = sorted(raw_names - test_names)
if missing:
    print("Raw entry points with no test reference:", file=sys.stderr)
    for name in missing:
        print(f"  {name}", file=sys.stderr)
    fail(f"{len(missing)} of {len(raw_names)} raw entry points are unreferenced by tests")

print(f"All {len(raw_names)} raw entry points are referenced by the test suite.")
