#!/usr/bin/env bash
#
# Verify that every native entry point declared in NativeMethods.cs still
# exists as an exported declaration in a given ghostty include tree.
#
# This is a NAME-EXISTENCE check and nothing more. It deliberately cannot see
# a signature change: when ghostty_terminal_new lost its by-value options
# struct upstream the name was untouched, so this reports a clean 97/97 while
# the ABI silently corrupts. Catching that is the job of the generated-vs-
# reference diff in ci.yml, not of this script. Neither check subsumes the
# other and both are wired up.
#
# Exit codes are distinct so callers can treat the two outcomes differently:
#   0  every declared entry point is present
#   1  DRIFT — one or more declared entry points no longer exist upstream
#   2  CANNOT RUN — bad inputs; the question was never actually asked
#
# Usage:
#   build/check-binding-symbols.sh <bindings.cs> <ghostty-include-dir>

set -euo pipefail

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
# shellcheck source=build/ghostty-symbols.sh
source "$SCRIPT_DIR/ghostty-symbols.sh"

BINDINGS=${1:-}
INCLUDE=${2:-}

if [ -z "$BINDINGS" ] || [ -z "$INCLUDE" ]; then
  echo "::error::usage: check-binding-symbols.sh <bindings.cs> <ghostty-include-dir>" >&2
  exit 2
fi
[ -f "$BINDINGS" ] || { echo "::error::bindings file not found: $BINDINGS" >&2; exit 2; }
[ -d "$INCLUDE" ]  || { echo "::error::include directory not found: $INCLUDE" >&2; exit 2; }

# Both helpers end in `|| true`, so an empty result reaches the guards below
# instead of killing the script under `set -e`. An earlier revision of this
# file put the guards after bare pipelines, which meant `set -euo pipefail`
# aborted on the failing grep and the guards were unreachable dead code — the
# script exited 1 with completely empty output.
declared=$(ghostty_declared_entry_points "$BINDINGS")
available=$(ghostty_exported_symbols "$INCLUDE")

declared_n=$(printf '%s\n' "$declared" | grep -c . || true)
available_n=$(printf '%s\n' "$available" | grep -c . || true)

# An empty set on either side makes the comparison trivially pass while
# verifying nothing — the same failure shape as the job this replaces.
if [ "$available_n" -eq 0 ]; then
  echo "::error::No GHOSTTY_API declarations found under $INCLUDE — the header tree looks wrong, refusing to report a clean result" >&2
  exit 2
fi
if [ "$declared_n" -eq 0 ]; then
  echo "::error::No P/Invoke entry points found in $BINDINGS — wrong file, or the declaration style changed; refusing to report a clean result" >&2
  exit 2
fi

# Entry points are taken from the method names, which is only valid while no
# declaration overrides them with EntryPoint=. Nothing enforced that, so a
# single `[LibraryImport(Lib, EntryPoint = "ghostty_x")] static partial Foo()`
# would drop silently out of the checked set: the count would slip from 97 to
# 96 and the run would stay green while the binary threw
# EntryPointNotFoundException. Assert the two counts agree instead of
# asserting it in a comment.
attributes_n=$(ghostty_interop_attribute_count "$BINDINGS")
if [ "$declared_n" -ne "$attributes_n" ]; then
  echo "::error::$BINDINGS has $attributes_n interop attributes but $declared_n resolvable entry-point names. A declaration is probably using EntryPoint= or an unrecognised form, which would silently shrink the checked set. Refusing to report a result." >&2
  exit 2
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
