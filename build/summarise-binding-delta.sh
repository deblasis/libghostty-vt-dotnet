#!/usr/bin/env bash
# shellcheck disable=SC2016
# body is Markdown; backticks are literal
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
# MAX_DIFF_BYTES and MAX_SIGNATURE_BYTES may be overridden for testing. GitHub
# caps an issue body at 65536 characters, so both payloads are bounded.
#
# The input diff must be valid UTF-8: the body is posted as UTF-8 and invalid
# bytes would make the request fail without a red build.

set -euo pipefail

MAX_DIFF_BYTES=${MAX_DIFF_BYTES:-20000}
MAX_SIGNATURE_BYTES=${MAX_SIGNATURE_BYTES:-10000}
MAX_SIGNATURE_LINES=${MAX_SIGNATURE_LINES:-50}

# A changed declaration, not any added or removed line that happens to mention
# `static extern`. A diff line that adds or removes a comment containing the
# words would otherwise be reported as a signature change and would suppress
# the honest "none changed" sentence. The marker must be followed by an
# optional accessibility modifier and then the declaration itself, so
# commented-out code and doc lines fail to match.
SIGNATURE_PATTERN='^[+-][[:space:]]*((public|internal|private|protected)[[:space:]]+)?static[[:space:]]+extern[[:space:]]'

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

# `-s` only proves the file is non-empty, so a file holding a single newline
# passes it and renders an empty code block: the exact shape this script
# exists to prevent. Require a non-blank line, and require the file to look
# like a diff of content rather than a bare header block. The tracker only
# ever receives the generated source diff, so anything else is a wrong input
# and must fail rather than file an issue that shows nothing.
LC_ALL=C grep -q '[^[:space:]]' "$diff_file" \
  || die "diff file has only whitespace: $diff_file"
LC_ALL=C grep -qE '^@@|^[+-][^+-]' "$diff_file" \
  || die "diff file has no hunk or changed line: $diff_file"

# The body is posted as UTF-8. Invalid bytes make the request invalid, and the
# tracker step runs with continue-on-error, so a rejection would leave no red
# build and no issue. Refuse the input here instead. The generator compares
# UTF-8 C# sources, so a genuine diff always passes.
iconv -f UTF-8 -t UTF-8 < "$diff_file" > /dev/null 2>&1 \
  || die "diff file is not valid UTF-8: $diff_file"

# Print complete lines until the byte budget is reached. Byte-accurate `head
# -c` truncation can split a multi-byte character, which makes the request
# body invalid UTF-8, so the cut happens between lines.
within_bytes_stream() { # within_bytes_stream <max> < input
  LC_ALL=C awk -v max="$1" '
    BEGIN { used = 0; printed = 0 }
    {
      width = length($0) + 1
      if (used + width > max) exit
      print
      used += width
      printed = 1
    }
    END { if (!printed) print "(the content has no complete line within the byte budget; download the artifact)" }
  '
}

within_bytes() { # within_bytes <file> <max>
  within_bytes_stream "$2" < "$1"
}

# A run of backticks in the diff can close a three-backtick code fence and
# change the rest of the issue into rendered Markdown. Use a fence longer than
# anything the payload contains, and never shorter than three. The fence is
# capped so a pathological run cannot make the fence itself enormous; runs at
# or above the fence length are neutralised in the payload.
FENCE_CAP=10

longest_backtick_run() {
  LC_ALL=C awk '
    {
      line = $0
      while (match(line, /`+/)) {
        if (RLENGTH > max) max = RLENGTH
        line = substr(line, RSTART + RLENGTH)
      }
    }
    END { print max + 0 }
  ' "$1"
}

fence_length_for() { # fence_length_for <file>
  local n
  n=$(longest_backtick_run "$1")
  n=$((n + 1))
  if [ "$n" -lt 3 ]; then
    n=3
  fi
  if [ "$n" -gt "$FENCE_CAP" ]; then
    n=$FENCE_CAP
  fi
  printf '%s\n' "$n"
}

fence_of_length() { # fence_of_length <n>
  printf '%*s' "$1" '' | tr ' ' '`'
}

neutralise_backtick_runs() { # neutralise_backtick_runs <fence-length>
  LC_ALL=C awk -v n="$1" '
    {
      out = ""
      line = $0
      while (match(line, /`+/)) {
        out = out substr(line, 1, RSTART - 1)
        if (RLENGTH >= n) {
          out = out "[backticks removed]"
        } else {
          out = out substr(line, RSTART, RLENGTH)
        }
        line = substr(line, RSTART + RLENGTH)
      }
      print out line
    }
  '
}

size=$(wc -c < "$diff_file" | tr -d ' ')
fence_len=$(fence_length_for "$diff_file")
fence=$(fence_of_length "$fence_len")

signature_total=$(grep -cE "$SIGNATURE_PATTERN" "$diff_file" || true)
signatures=$(grep -E "$SIGNATURE_PATTERN" "$diff_file" \
  | head -n "$MAX_SIGNATURE_LINES" || true)
signature_shown=0
if [ -n "$signatures" ]; then
  signature_payload=$(printf '%s\n' "$signatures" \
    | within_bytes_stream "$MAX_SIGNATURE_BYTES" \
    | neutralise_backtick_runs "$fence_len")
  if [ -n "$signature_payload" ]; then
    signature_shown=$(printf '%s\n' "$signature_payload" | wc -l | tr -d ' ')
  fi
fi

printf 'Upstream `%s` generates a different pinned binding reference — [run](%s).\n\n' \
  "$commit" "$run_url"
printf 'The pinned reference and raw ABI layer are refreshed automatically with the pin.\n\n'

if [ "$signature_total" -gt 0 ]; then
  if [ -n "${signature_payload:-}" ]; then
    printf 'Signature-level changes:\n\n%sdiff\n%s\n%s\n\n' \
      "$fence" "$signature_payload" "$fence"
  else
    printf 'Signature-level changes: the changed lines are longer than the %s-byte summary budget, so they are shown only in the full diff below.\n\n' \
      "$MAX_SIGNATURE_BYTES"
  fi
  # The section is a subset whenever a cap bites. Say so instead of letting a
  # reader assume the list is complete.
  if [ "$signature_shown" -lt "$signature_total" ]; then
    printf 'Showing the first %s of %s changed signature lines; the rest are in the full diff below or in the `regenerated-bindings` artifact.\n\n' \
      "$signature_shown" "$signature_total"
  fi
else
  printf 'No `static extern` signature lines changed. The delta is in generated constants, attributes, or other declarations. The full diff is the record; it follows.\n\n'
fi

if [ "$size" -le "$MAX_DIFF_BYTES" ]; then
  printf 'Full diff:\n\n%sdiff\n' "$fence"
  # The sanitiser prints one newline per line, including the last, so the
  # closing fence always starts on its own line.
  neutralise_backtick_runs "$fence_len" < "$diff_file"
  printf '%s\n' "$fence"
else
  printf 'Full diff (complete lines within %s bytes of %s):\n\n%sdiff\n' \
    "$MAX_DIFF_BYTES" "$size" "$fence"
  within_bytes "$diff_file" "$MAX_DIFF_BYTES" \
    | neutralise_backtick_runs "$fence_len"
  printf '\n%s\n\n' "$fence"
  printf 'The diff is truncated here. Download the `regenerated-bindings` artifact or run the generator to see the rest.\n'
fi
