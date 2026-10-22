#!/usr/bin/env python3
"""Keep native interop declarations inside the generated raw layer."""

from __future__ import annotations

import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
SRC = REPO / "src" / "Ghostty.Vt"
ALLOWED = SRC / "Raw" / "GhosttyVtNative.g.cs"
INTEROP = re.compile(r"\[(?:DllImport|LibraryImport)\b")

offenders = []
for path in SRC.rglob("*.cs"):
    if path == ALLOWED:
        continue
    if INTEROP.search(path.read_text(encoding="utf-8")):
        offenders.append(path.relative_to(REPO))

if offenders:
    print("Native interop declarations outside the generated raw layer:", file=sys.stderr)
    for path in offenders:
        print(f"  {path}", file=sys.stderr)
    raise SystemExit(1)

print("Native interop declarations are confined to the generated raw layer.")
