#!/usr/bin/env bash
#
# Run build/selftest-binding-delta.sh and refuse a report that is not backed by
# executed assertions.
#
# The self-test prints its own pass count, which makes that count self-certified:
# a stub that prints "40 passed, 0 failed" satisfies a reader that trusts it.
# This script cross-checks the printed count against the number of `ok` lines
# the self-test actually emitted, and rejects any `FAIL` line. A stub that
# asserts nothing therefore fails even if it prints a plausible total.
#
# Both the CI job and the local regression suite run this script, so the check
# that guards the body builder has one implementation rather than two that can
# drift apart.
#
# Usage: build/check-binding-delta-selftest.sh
#   SELFTEST=<path>  run a different self-test (used to prove this gate rejects
#                    a stub); defaults to build/selftest-binding-delta.sh
#   MIN_CASES=<n>    minimum number of executed cases (default 30)

set -uo pipefail

HERE=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
SELFTEST=${SELFTEST:-$HERE/selftest-binding-delta.sh}
MIN_CASES=${MIN_CASES:-30}

if [ ! -f "$SELFTEST" ]; then
  echo "self-test not found: $SELFTEST"
  exit 1
fi

out=$(bash "$SELFTEST" 2>&1)
rc=$?
# Print the captured output in both directions: on failure this is the only
# diagnostic an author gets from the CI log.
printf '%s\n' "$out"

if [ "$rc" -ne 0 ]; then
  echo "self-test failed (rc=$rc) in $SELFTEST"
  exit 1
fi

reported=$(printf '%s\n' "$out" \
  | sed -n 's/.*selftest: \([0-9][0-9]*\) passed, 0 failed$/\1/p')
if [ -z "$reported" ]; then
  echo "the self-test did not report a clean 'N passed, 0 failed' line"
  exit 1
fi

failed=$(printf '%s\n' "$out" | grep -c '^  FAIL  ')
ran=$(printf '%s\n' "$out" | grep -c '^  ok    ')

if [ "$failed" -ne 0 ]; then
  echo "$failed self-test assertions reported FAIL"
  exit 1
fi
if [ "$ran" -ne "$reported" ]; then
  echo "the self-test reported $reported passes but executed $ran assertions"
  exit 1
fi
if [ "$ran" -lt "$MIN_CASES" ]; then
  echo "only $ran self-test cases ran; keep at least $MIN_CASES"
  exit 1
fi

echo "body-builder self-test: $ran cases, all executed"
