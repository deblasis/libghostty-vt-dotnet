#!/usr/bin/env bash
# shellcheck disable=SC2016
# body is Markdown; the backticks are literal
#
# Render the body of the issue that tracks an upstream binding delta.
#
# The tracker is the durable record of what changed upstream when the pin
# moves. A previous revision passed only the `static extern` lines, so a delta
# made entirely of generated constants produced a body with an empty diff
# block: an issue that says "review the delta below" and shows nothing. This
# script always includes the diff, truncated on a line boundary, and says
# explicitly when no signature changed.
#
# Usage: build/summarise-binding-delta.sh <diff-file> <commit> <run-url>
#
# MAX_DIFF_BYTES may be overridden for testing; GitHub caps an issue body at
# 65536 characters, and the signature section is capped separately.

set -euo pipefail


MAX_DIFF_BYTES=${MAX_DIFF_BYTES:-20000}
MAX_SIGNATURE_LINES=${MAX_SIGNATURE_LINES:-50}

die() {
  echo "summarise-binding-delta: $*" >&2
  exit 2
}

if [ "$#" -ne 3 ]; then
  die "usage: summarise-binding-delta.sh <diff-file> <commit> <run-url>"
fi

diff_file=$1
commit=$2
run_url=$3

[ -f "$diff_file" ] || die "diff file not found: $diff_file"
[ -s "$diff_file" ] || die "diff file is empty: $diff_file"

signatures=$(grep -E '^[+-].*static extern' "$diff_file" \
  | head -n "$MAX_SIGNATURE_LINES" || true)
size=$(wc -c < "$diff_file" | tr -d ' ')

printf 'Upstream `%s` generates a different pinned binding reference — [run](%s).\n\n' \
  "$commit" "$run_url"
printf 'The pinned reference and raw ABI layer are refreshed automatically with the pin.\n\n'

if [ -n "$signatures" ]; then
  printf 'Signature-level changes:\n\n```diff\n%s\n```\n\n' "$signatures"
else
  printf 'No `static extern` signature lines changed. The delta is in generated constants, attributes, or other declarations. The full diff is the record; it follows.\n\n'
fi

if [ "$size" -le "$MAX_DIFF_BYTES" ]; then
  printf 'Full diff:\n\n```diff\n'
  cat "$diff_file"
  # A diff without a trailing newline would otherwise swallow the fence.
  if [ -n "$(tail -c 1 "$diff_file")" ]; then
    printf '\n'
  fi
  printf '```\n'
else
  printf 'Full diff (first lines within %s bytes of %s):\n\n```diff\n' \
    "$MAX_DIFF_BYTES" "$size"
  # Cut on a line boundary so the body stays valid text; byte-accurate
  # truncation can split a multi-byte character and invalidate the request.
  LC_ALL=C awk -v max="$MAX_DIFF_BYTES" '
    BEGIN { used = 0 }
    {
      width = length($0) + 1
      if (used + width > max) exit
      print
      used += width
    }
  ' "$diff_file"
  printf '```\n\n'
  printf 'The diff is truncated here. Download the `regenerated-bindings` artifact or run the generator to see the rest.\n'
fi
