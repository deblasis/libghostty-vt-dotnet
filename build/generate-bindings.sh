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
# Usage:
#   build/generate-bindings.sh <generator> [extra-args-rsp] [include-root]

set -euo pipefail

GENERATOR=${1:?usage: generate-bindings.sh <generator> [extra-args-rsp] [include-root]}
EXTRA_ARGS=${2:-}
INCLUDE_ROOT=${3:-ghostty-src/include/ghostty}
OUTPUT=${OUTPUT:-src/Ghostty.Vt/Native/NativeMethods.cs}

[ -d "$INCLUDE_ROOT" ] || { echo "::error::include root not found: $INCLUDE_ROOT" >&2; exit 1; }

TRAVERSE_RSP=$(mktemp)
trap 'rm -f "$TRAVERSE_RSP"' EXIT

while IFS= read -r header; do
  printf -- '--traverse\n%s\n' "$header" >> "$TRAVERSE_RSP"
done < <(find "$INCLUDE_ROOT" -name '*.h' | sort)

traverse_count=$(grep -c '^--traverse$' "$TRAVERSE_RSP" || true)
if [ "$traverse_count" -eq 0 ]; then
  echo "::error::No headers found under $INCLUDE_ROOT — refusing to run the generator over nothing" >&2
  exit 1
fi
echo "Traversing $traverse_count headers under $INCLUDE_ROOT"

LOG=$(mktemp)
trap 'rm -f "$TRAVERSE_RSP" "$LOG"' EXIT

set +e
if [ -n "$EXTRA_ARGS" ]; then
  "$GENERATOR" @build/generate-bindings.rsp @"$EXTRA_ARGS" @"$TRAVERSE_RSP" 2>&1 | tee "$LOG"
else
  "$GENERATOR" @build/generate-bindings.rsp @"$TRAVERSE_RSP" 2>&1 | tee "$LOG"
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

echo "Generator exit code $rc — $errors error(s), $fatals fatal(s), $warnings warning(s)"

if [ "$errors" -gt 0 ] || [ "$fatals" -gt 0 ]; then
  echo "::error::Generator reported $errors error(s) and $fatals fatal(s); bindings cannot be trusted" >&2
  exit 1
fi

[ -f "$OUTPUT" ] || { echo "::error::generator produced no $OUTPUT" >&2; exit 1; }

# Count the interop attribute, exactly one per bound function. Matching the
# `extern` line as well would double-count, and a detector that reports a
# number twice the truth is not one anybody should trust.
emitted=$(grep -cE '^[[:space:]]*\[(DllImport|LibraryImport)' "$OUTPUT" || true)
MIN_EXPECTED=${MIN_EXPECTED:-100}
echo "Emitted P/Invoke declarations: $emitted (minimum expected: $MIN_EXPECTED)"

if [ "$emitted" -lt "$MIN_EXPECTED" ]; then
  echo "::error::Generator emitted only $emitted P/Invoke declarations (expected at least $MIN_EXPECTED). It exited successfully but produced nothing usable — treat this as a broken generator, not as an empty diff." >&2
  exit 1
fi
