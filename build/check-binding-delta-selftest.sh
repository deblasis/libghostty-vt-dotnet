#!/usr/bin/env bash
#
# Run build/selftest-binding-delta.sh and refuse a report that is not backed by
# executed assertions.
#
# The self-test prints its own pass count, which makes that count
# self-certified: a stub that prints "40 passed, 0 failed" satisfies a reader
# that trusts it. This script cross-checks the printed count against the number
# of `ok` lines the self-test actually emitted, and rejects any `FAIL` line.
#
# That cross-check is still built from lines the self-test prints, so it is a
# secondary signal. The authoritative check is
# `build/check-binding-delta-behaviour.sh`, which runs the builder against
# fixtures and fails on the body rather than on a report about the body. It runs
# here so the CI job and the local regression suite both exercise it.
#
# Usage: build/check-binding-delta-selftest.sh
#   SELFTEST=<path>  run a different self-test (used to prove this gate rejects
#                    a stub); defaults to build/selftest-binding-delta.sh
#   MIN_CASES=<n>    minimum number of executed self-test cases (default 30)
#   MIN_BEHAVIOUR_CASES=<n>
#                    minimum number of executed behaviour cases (default 20)

set -uo pipefail

HERE=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
SELFTEST=${SELFTEST:-$HERE/selftest-binding-delta.sh}
MIN_CASES=${MIN_CASES:-30}
MIN_BEHAVIOUR_CASES=${MIN_BEHAVIOUR_CASES:-20}

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

# The count above comes from the self-test's own output. Check the builder
# directly as well: this fails on the body, so a self-test that prints a
# plausible count cannot stand in for the behaviour.
bout=$(bash "$HERE/check-binding-delta-behaviour.sh" 2>&1)
brc=$?
printf '%s\n' "$bout"
if [ "$brc" -ne 0 ]; then
  echo "the builder behaviour check failed; see the FAIL lines above"
  exit 1
fi

# Count the behaviour check's own result lines. Without this, deleting its
# fixtures would leave it green with fewer cases while every gate still
# reported a clean run.
breported=$(printf '%s\n' "$bout" \
  | sed -n 's/^builder behaviour: \([0-9][0-9]*\) checks, 0 failed$/\1/p')
bran=$(printf '%s\n' "$bout" | grep -c '^  ok    ')
bfailed=$(printf '%s\n' "$bout" | grep -c '^  FAIL  ')
if [ -z "$breported" ]; then
  echo "the builder behaviour check did not report a clean case count"
  exit 1
fi
if [ "$bfailed" -ne 0 ]; then
  echo "$bfailed builder behaviour cases reported FAIL"
  exit 1
fi
if [ "$bran" -ne "$breported" ]; then
  echo "the builder behaviour check reported $breported cases but emitted $bran"
  exit 1
fi
if [ "$bran" -lt "$MIN_BEHAVIOUR_CASES" ]; then
  echo "only $bran behaviour cases ran; keep at least $MIN_BEHAVIOUR_CASES"
  exit 1
fi

echo "body-builder self-test: $ran cases, all executed"
