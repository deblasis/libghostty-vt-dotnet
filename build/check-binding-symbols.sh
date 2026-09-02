#!/usr/bin/env bash
#
# Verify that every native entry point declared in NativeMethods.cs still
# exists as an exported declaration in a given ghostty include tree.
#
# Why this and not a diff of the generated file:
#
#   NativeMethods.cs is not generator output and never has been. It is a
#   hand-maintained, curated subset — 97 of the ~183 exported functions —
#   written with LibraryImport and `internal` visibility, while
#   ClangSharpPInvokeGenerator emits DllImport, `public`, and every symbol it
#   can see. Diffing the two can therefore never come back clean, which made
#   the old "regenerate and diff" check incapable of ever reporting good news.
#
#   What actually matters is narrower and checkable: does every entry point we
#   P/Invoke still exist upstream? That is exactly the failure that cost 20
#   test failures when the pin went stale (EntryPointNotFoundException for
#   ghostty_terminal_mode_get/_set and ghostty_render_state_colors_get), and
#   it is a question with a correct answer rather than a permanent diff.
#
# Usage:
#   build/check-binding-symbols.sh <bindings.cs> <ghostty-include-dir>
#
# Exits non-zero, listing the offenders, if any declared entry point is gone.

set -euo pipefail

BINDINGS=${1:?usage: check-binding-symbols.sh <bindings.cs> <ghostty-include-dir>}
INCLUDE=${2:?usage: check-binding-symbols.sh <bindings.cs> <ghostty-include-dir>}

[ -f "$BINDINGS" ] || { echo "::error::bindings file not found: $BINDINGS" >&2; exit 1; }
[ -d "$INCLUDE" ]  || { echo "::error::include directory not found: $INCLUDE" >&2; exit 1; }

# LibraryImport uses the method name as the entry point; the file sets no
# explicit EntryPoint anywhere, so the declared method names are the symbols.
declared=$(grep -E 'static partial' "$BINDINGS" \
  | grep -oE 'ghostty_[a-z0-9_]+' | sort -u)

# Only real declarations: the name immediately before the opening paren of a
# GHOSTTY_API line. Matching bare "ghostty_x(" anywhere would also pick up
# prose in doc comments, which routinely name replacement functions and would
# therefore mask exactly the removals this is looking for. Taking the *last*
# ghostty_ token before the paren also handles declarations whose return type
# is itself snake_case (e.g. "GHOSTTY_API ghostty_info_s ghostty_info(").
available=$(find "$INCLUDE" -name '*.h' -exec cat {} + \
  | grep -hoE '^GHOSTTY_API[^(]*\(' \
  | sed -E 's/.*[^A-Za-z0-9_](ghostty_[a-z0-9_]+)[[:space:]]*\($/\1/' \
  | grep -E '^ghostty_[a-z0-9_]+$' | sort -u)

declared_n=$(printf '%s\n' "$declared" | grep -c . || true)
available_n=$(printf '%s\n' "$available" | grep -c . || true)

if [ "$available_n" -eq 0 ]; then
  echo "::error::No GHOSTTY_API declarations found under $INCLUDE — the header tree looks wrong, refusing to report a clean result" >&2
  exit 1
fi

missing=$(comm -23 <(printf '%s\n' "$declared") <(printf '%s\n' "$available") || true)

echo "Declared entry points in $(basename "$BINDINGS"): $declared_n"
echo "Exported functions in $INCLUDE:               $available_n"

if [ -z "$missing" ]; then
  echo "All $declared_n declared entry points are present upstream."
  exit 0
fi

missing_n=$(printf '%s\n' "$missing" | grep -c . || true)
echo
echo "$missing_n declared entry point(s) no longer exist upstream:"
printf '%s\n' "$missing" | sed 's/^/  - /'
exit 1
