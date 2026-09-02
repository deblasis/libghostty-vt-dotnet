#!/usr/bin/env bash
#
# Self-test for the two binding gates.
#
# These gates exist to fail. That makes them the one kind of code whose
# working state is invisible in normal operation: while everything is healthy,
# a gate that has silently become incapable of failing looks exactly like a
# gate that is passing. #46 was precisely that — a drift detector that had
# never once run, reporting success for months — and the first fix for it
# reintroduced the same defect in a new place, because the emitted-count check
# measured the checked-in file that `actions/checkout` had left at the output
# path.
#
# So the gates get tested with deliberately broken stub generators, in CI, on
# every run. Nothing here touches the repository's real bindings.
#
# Usage: build/selftest-binding-gates.sh <ghostty-include-dir>

set -uo pipefail

INCLUDE=${1:?usage: selftest-binding-gates.sh <ghostty-include-dir>}
HERE=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
REPO=$(cd "$HERE/.." && pwd)

# shellcheck source=build/ghostty-symbols.sh
source "$HERE/ghostty-symbols.sh"

TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

# How many functions the headers under test actually export. The gate's floor
# is a percentage of this (see generate-bindings.sh), so every stub meant to
# look HEALTHY has to be sized from the same number.
#
# These counts used to be the literal 171 -- the export count at the pin where
# this file was written. A literal silently turns the self-test into a broken
# one the moment upstream exports more functions: the floor rises past 171, the
# "healthy" stub is rejected, and the suite fails on a case that describes
# correct behaviour. Moving the pin to 349f0260 did exactly that. Worse, the
# regression guard below would still have reported "ok" -- for the wrong
# reason, because its pre-existing file fell under the floor rather than
# because the generator's stale output was correctly discarded.
HEALTHY_COUNT=$(ghostty_exported_symbols "$INCLUDE" | grep -c . || true)
if [ "${HEALTHY_COUNT:-0}" -lt 1 ]; then
  echo "::error::Could not count exported symbols under $INCLUDE; the self-test cannot size its stubs." >&2
  exit 1
fi

pass=0
fail=0

check() { # check <name> <expected-rc> <actual-rc>
  if [ "$2" -eq "$3" ]; then
    printf '  ok    %-52s (rc=%s)\n' "$1" "$3"; pass=$((pass + 1))
  else
    printf '  FAIL  %-52s (expected rc=%s, got rc=%s)\n' "$1" "$2" "$3"; fail=$((fail + 1))
  fi
}

emit_attributes() { # emit_attributes <file> <count>
  : > "$1"
  for _ in $(seq 1 "$2"); do
    printf '        [DllImport("libghostty-vt")]\n        public static extern int ghostty_x();\n' >> "$1"
  done
}

mkstub() { # mkstub <path> <body>
  printf '#!/usr/bin/env bash\n%s\n' "$2" > "$1"
  chmod +x "$1"
}

run_gen() { # run_gen <stub> [include]
  ( cd "$REPO" && OUTPUT="$TMP/out.cs" RSP=build/generate-bindings.rsp \
      HEALTHY_COUNT="$HEALTHY_COUNT" \
      bash build/generate-bindings.sh "$1" "" "${2:-$INCLUDE}" ) >"$TMP/log" 2>&1
  return $?
}

echo "== generator gate =="

# The failure that started #46: an unhandled .NET exception. It matches none
# of ClangSharp's diagnostic patterns, so only an explicit crash check or the
# absence of output can catch it.
mkstub "$TMP/crash.sh" 'echo "Unhandled exception: System.DllNotFoundException: Unable to load shared library '\''libclang'\''"; exit 134'
run_gen "$TMP/crash.sh"; check "crash writing nothing must fail" 1 $?

# THE REGRESSION GUARD. A stub that writes nothing while a healthy-looking
# file already sits at the output path — which is what `actions/checkout`
# leaves there. Without the pre-run removal, the gate counts the committed
# hand-maintained bindings and reports them as freshly generated.
emit_attributes "$TMP/out.cs" "$HEALTHY_COUNT"
mkstub "$TMP/silent.sh" 'exit 0'
run_gen "$TMP/silent.sh"; check "silent stub must not inherit the committed file" 1 $?

# The state the generator was actually in after the libclang fix: exit 0,
# helper types only, zero bindings.
mkstub "$TMP/empty.sh" 'printf "internal sealed partial class NativeTypeNameAttribute {}\n" > "$OUTPUT"; exit 0'
OUTPUT="$TMP/out.cs" run_gen "$TMP/empty.sh"; check "exit 0 with no bindings must fail" 1 $?

mkstub "$TMP/fatal.sh" 'echo "/usr/include/limits.h:124:16: fatal error: '\''limits.h'\'' file not found"; exit 255'
run_gen "$TMP/fatal.sh"; check "fatal diagnostic must fail" 1 $?

# A healthy run: many warnings, non-zero exit, real output. Must PASS — the
# exit code is a diagnostic count, not a verdict.
mkstub "$TMP/good.sh" '
for i in $(seq 1 177); do echo "    Warning (Line $i, Column 9 in x.h): Function like macro definition records are not supported"; done
: > "$OUTPUT"
for i in $(seq 1 "$HEALTHY_COUNT"); do printf "        [DllImport(\"libghostty-vt\")]\n        public static extern int ghostty_x$i();\n" >> "$OUTPUT"; done
exit 177'
run_gen "$TMP/good.sh"; check "warnings + non-zero exit + real output passes" 0 $?

# Too few declarations relative to what the headers export.
mkstub "$TMP/thin.sh" '
: > "$OUTPUT"
for i in $(seq 1 20); do printf "        [DllImport(\"libghostty-vt\")]\n        public static extern int ghostty_x$i();\n" >> "$OUTPUT"; done
exit 0'
run_gen "$TMP/thin.sh"; check "output far below the header-derived floor fails" 1 $?

mkdir -p "$TMP/noheaders"
run_gen "$TMP/good.sh" "$TMP/noheaders"; check "empty header tree must fail" 1 $?

echo "== symbol gate =="

BINDINGS="$REPO/src/Ghostty.Vt/Native/NativeMethods.cs"
CHECK="$REPO/build/check-binding-symbols.sh"

bash "$CHECK" "$BINDINGS" "$INCLUDE" >/dev/null 2>&1; check "real bindings vs their own headers" 0 $?

# Delete a symbol the bindings declare: that is drift, exit 1.
cp -r "$INCLUDE" "$TMP/hdr"
grep -rl 'ghostty_terminal_free' "$TMP/hdr" | while read -r f; do
  sed -i 's/^GHOSTTY_API[^(]*ghostty_terminal_free[[:space:]]*(/GHOSTTY_API void ghostty_REMOVED_free(/' "$f"
done
bash "$CHECK" "$BINDINGS" "$TMP/hdr" >/dev/null 2>&1; check "removed symbol reports drift" 1 $?

# An EntryPoint= override hides a declaration from name-based extraction. It
# must refuse to report, not quietly check one fewer symbol.
sed 's/\[LibraryImport(LibraryName)\]\n/X/' "$BINDINGS" > "$TMP/ep.cs"
python3 - "$BINDINGS" "$TMP/ep.cs" <<'PY' 2>/dev/null || cp "$BINDINGS" "$TMP/ep.cs"
import re, sys
src = open(sys.argv[1], encoding='utf-8').read()
src = src.replace('[LibraryImport(LibraryName)]\n    internal static partial void ghostty_terminal_free(nint terminal);',
                  '[LibraryImport(LibraryName, EntryPoint = "ghostty_terminal_free")]\n    internal static partial void TerminalFree(nint terminal);', 1)
open(sys.argv[2], 'w', encoding='utf-8').write(src)
PY
bash "$CHECK" "$TMP/ep.cs" "$INCLUDE" >/dev/null 2>&1; check "EntryPoint= override must refuse to report" 2 $?

printf 'internal static class X { }\n' > "$TMP/empty-bindings.cs"
bash "$CHECK" "$TMP/empty-bindings.cs" "$INCLUDE" >/dev/null 2>&1; check "bindings with no entry points must refuse" 2 $?

mkdir -p "$TMP/emptyhdr"
bash "$CHECK" "$BINDINGS" "$TMP/emptyhdr" >/dev/null 2>&1; check "empty header tree must refuse" 2 $?

bash "$CHECK" "$TMP/does-not-exist.cs" "$INCLUDE" >/dev/null 2>&1; check "missing bindings file must refuse" 2 $?

echo
echo "$pass passed, $fail failed"
[ "$fail" -eq 0 ]
