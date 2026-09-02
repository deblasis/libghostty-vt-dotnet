#!/usr/bin/env bash
#
# Run ClangSharpPInvokeGenerator over the ghostty vt headers.
#
# Composes three pieces:
#   1. build/generate-bindings.rsp   — the checked-in, path-free config
#   2. an optional machine-specific rsp (clang -resource-dir), from
#      .github/actions/setup-clangsharp
#   3. a --traverse entry per upstream header, generated here
#
# (3) is not optional and is why this script exists. ClangSharp only emits
# declarations from the file given to --file plus any file named by
# --traverse. `vt.h` is nothing but a list of #includes, so without a traverse
# list the generator parses everything, emits *nothing*, and still exits 0 —
# producing a ~2KB file containing only the generate-helper-types attributes
# and no bindings at all. That is what it did on its first successful run
# after the libclang fix in #46.
#
# The list is derived from the header tree rather than checked in, so headers
# upstream adds (search.h, snapshot.h and io.h all appeared in one release)
# are picked up without anyone remembering to edit a file.
#
# Exit codes: 0 success, 1 the generator could not produce trustworthy output.
#
# Usage:
#   build/generate-bindings.sh <generator> [extra-args-rsp] [include-root]

set -euo pipefail

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
# shellcheck source=build/ghostty-symbols.sh
source "$SCRIPT_DIR/ghostty-symbols.sh"

GENERATOR=${1:?usage: generate-bindings.sh <generator> [extra-args-rsp] [include-root]}
EXTRA_ARGS=${2:-}
INCLUDE_ROOT=${3:-ghostty-src/include/ghostty}
OUTPUT=${OUTPUT:-src/Ghostty.Vt/Native/NativeMethods.cs}
RSP=${RSP:-build/generate-bindings.rsp}

[ -d "$INCLUDE_ROOT" ] || { echo "::error::include root not found: $INCLUDE_ROOT" >&2; exit 1; }

TRAVERSE_RSP=$(mktemp)
LOG=$(mktemp)
trap 'rm -f "$TRAVERSE_RSP" "$LOG"' EXIT

while IFS= read -r header; do
  printf -- '--traverse\n%s\n' "$header" >> "$TRAVERSE_RSP"
done < <(find "$INCLUDE_ROOT" -name '*.h' | sort)

traverse_count=$(grep -c '^--traverse$' "$TRAVERSE_RSP" || true)
if [ "$traverse_count" -eq 0 ]; then
  echo "::error::No headers found under $INCLUDE_ROOT — refusing to run the generator over nothing" >&2
  exit 1
fi
echo "Traversing $traverse_count headers under $INCLUDE_ROOT"

# CRITICAL: the output path is a checked-in file that `actions/checkout` has
# already placed on disk. If the generator writes nothing — which is exactly
# how #46 failed for months — then a bare `[ -f "$OUTPUT" ]` passes and any
# count taken from it silently measures the hand-maintained bindings instead
# of generated output. That makes the gate report on a file the generator
# never touched. Delete it first so "produced nothing" is distinguishable
# from "produced something".
rm -f "$OUTPUT"

set +e
if [ -n "$EXTRA_ARGS" ]; then
  "$GENERATOR" @"$RSP" @"$EXTRA_ARGS" @"$TRAVERSE_RSP" 2>&1 | tee "$LOG"
else
  "$GENERATOR" @"$RSP" @"$TRAVERSE_RSP" 2>&1 | tee "$LOG"
fi
rc=${PIPESTATUS[0]}
set -e

# ClangSharp's exit code is its DIAGNOSTIC COUNT, not a success flag. A
# perfectly good run over these headers still exits ~177, one per warning for
# function-like macros (GHOSTTY_INIT_SIZED, GHOSTTY_COLOR_PALETTE_MASK_*) and
# unsupported visibility attributes, none of which affect the bindings. So
# `rc != 0` must not be read as failure — and, symmetrically, `rc == 0` must
# not be read as success: before --traverse was passed the generator emitted
# nothing at all and exited 0 precisely because it had produced no
# diagnostics. Judge the output instead.
warnings=$(grep -c 'Warning (Line' "$LOG" || true)
errors=$(grep -c 'Error (Line' "$LOG" || true)
fatals=$(grep -c 'fatal error:' "$LOG" || true)
# A .NET crash produces neither: the original #46 failure was an unhandled
# DllNotFoundException, which matches none of the diagnostic patterns above.
crashes=$(grep -cE 'Unhandled exception|DllNotFoundException' "$LOG" || true)

echo "Generator exit code $rc — $errors error(s), $fatals fatal(s), $crashes crash(es), $warnings warning(s)"

if [ "$crashes" -gt 0 ]; then
  echo "::error::Generator crashed (unhandled exception); bindings cannot be trusted" >&2
  exit 1
fi
if [ "$errors" -gt 0 ] || [ "$fatals" -gt 0 ]; then
  echo "::error::Generator reported $errors error(s) and $fatals fatal(s); bindings cannot be trusted" >&2
  exit 1
fi

if [ ! -f "$OUTPUT" ]; then
  echo "::error::Generator produced no $OUTPUT (exit code $rc). It wrote nothing at all — treat this as a broken generator, not as an empty diff." >&2
  exit 1
fi

# One attribute per bound function, whatever the declaration style.
emitted=$(ghostty_interop_attribute_count "$OUTPUT")

# The floor is derived from the headers actually being traversed, not a magic
# constant: a fixed 100 would have let the generator silently lose 41% of a
# 171-function surface, and would have drifted further from the truth with
# every upstream release. ClangSharp legitimately skips a few declarations
# (inline helpers, unsupported forms), so allow a margin rather than
# demanding parity.
exported=$(ghostty_exported_symbols "$INCLUDE_ROOT" | grep -c . || true)
MIN_RATIO=${MIN_RATIO:-85}
MIN_EXPECTED=${MIN_EXPECTED:-$(( exported * MIN_RATIO / 100 ))}

echo "Emitted P/Invoke declarations: $emitted (headers export $exported; floor $MIN_EXPECTED = ${MIN_RATIO}%)"

if [ "$emitted" -lt "$MIN_EXPECTED" ]; then
  echo "::error::Generator emitted only $emitted P/Invoke declarations against $exported exported functions (floor $MIN_EXPECTED). It produced nothing usable — treat this as a broken generator, not as an empty diff." >&2
  exit 1
fi
