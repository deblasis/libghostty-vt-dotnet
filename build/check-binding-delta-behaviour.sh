#!/usr/bin/env bash
# shellcheck disable=SC2016
# the fixtures and assertions contain literal backticks
#
# Check the issue-body builder against fixtures and fail on the body it wrote.
#
# `build/selftest-binding-delta.sh` prints its own pass count, so a gate that
# reads only that report can be satisfied by a script that asserts nothing.
# This check runs `build/summarise-binding-delta.sh` and inspects the output:
# a constants-only delta keeps the diff and says no signature changed, a
# header-only diff is refused, truncation keeps early complete lines and drops
# late ones, a budget too small for one line says so instead of claiming a
# shown count, a backtick run gets a longer fence, and invalid or blank input is
# refused.
#
# CI runs this script, and the local regression suite also feeds it deliberately
# broken builders to prove it can fail.
#
# Usage: build/check-binding-delta-behaviour.sh
#   BUILDER=<path>  run a different builder (used to prove this check can fail)

set -uo pipefail

HERE=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
BUILDER=${BUILDER:-$HERE/summarise-binding-delta.sh}
COMMIT=c0ffee0123456789
RUN_URL=https://example.invalid/actions/runs/1

if [ ! -f "$BUILDER" ]; then
  echo "builder not found: $BUILDER"
  exit 1
fi

tmp=$(mktemp -d) || exit 1
trap 'rm -rf "$tmp"' EXIT

checks=0
failures=0

ok() {
  checks=$((checks + 1))
  printf '  ok    %s\n' "$1"
}

bad() {
  checks=$((checks + 1))
  failures=$((failures + 1))
  printf '  FAIL  %s\n' "$1"
}

build() { # build <diff> <body> [env assignment...]
  local diff=$1 body=$2
  shift 2
  env "$@" bash "$BUILDER" "$diff" "$COMMIT" "$RUN_URL" > "$body" 2> "$body.err"
}

contains() { # contains <body> <text> <case>
  if grep -qF -- "$2" "$1"; then
    ok "$3"
  else
    bad "$3"
  fi
}

absent() { # absent <body> <text> <case>
  if grep -qF -- "$2" "$1"; then
    bad "$3"
  else
    ok "$3"
  fi
}

count_is() { # count_is <body> <text> <n> <case>
  local seen
  seen=$(grep -cF -- "$2" "$1" || true)
  if [ "$seen" -eq "$3" ]; then
    ok "$4"
  else
    bad "$4 ($seen occurrences, expected $3)"
  fi
}

expect_rc() { # expect_rc <want> <got> <case>
  if [ "$2" -eq "$1" ]; then
    ok "$3"
  else
    bad "$3 (exit $2, expected $1)"
  fi
}

# A delta that touched only generated constants is the reason this builder
# exists: the previous version rendered an empty code block for it.
cat > "$tmp/version.diff" <<'DIFF'
diff --git a/src/Ghostty.Vt/Raw/X.g.cs b/src/Ghostty.Vt/Raw/X.g.cs
--- a/src/Ghostty.Vt/Raw/X.g.cs
+++ b/src/Ghostty.Vt/Raw/X.g.cs
@@ -1,2 +1,2 @@
-        public const int GHOSTTY_VT_VERSION_MINOR = 1;
+        public const int GHOSTTY_VT_VERSION_MINOR = 0;
DIFF
if build "$tmp/version.diff" "$tmp/version.md"; then
  ok 'a constants-only delta is accepted'
else
  bad 'a constants-only delta is accepted'
fi
contains "$tmp/version.md" 'GHOSTTY_VT_VERSION_MINOR = 0' \
  'a constants-only body still carries the diff'
contains "$tmp/version.md" 'No `static extern` signature lines changed.' \
  'a constants-only body says that no signature changed'

# The declaration shapes a generator emits are not all `public static extern`.
# A body that says "no signature changed" while the diff adds a P/Invoke tells
# the reader the opposite of what happened.
cat > "$tmp/shapes.diff" <<'DIFF'
diff --git a/x b/x
--- a/x
+++ b/x
@@ -1,6 +1,6 @@
+    protected internal static extern int ghostty_protected();
+    public static unsafe extern int ghostty_unsafe();
+    [DllImport("libghostty-vt")] public static extern int ghostty_attribute();
+    [LibraryImport("libghostty-vt")] public static partial int ghostty_partial();
+    public static extern
-    // this line used to read: static extern int ghostty_old();
DIFF
build "$tmp/shapes.diff" "$tmp/shapes.md"
contains "$tmp/shapes.md" 'Signature-level changes:' \
  'the body has a signature section when declarations changed'
# Each declaration appears twice when it is summarised: once in the signature
# section and once in the full diff. Counting the occurrences keeps the case
# from passing on another line's summary.
count_is "$tmp/shapes.md" 'ghostty_protected' 2 \
  'a declaration behind a compound modifier is shown in the summary'
count_is "$tmp/shapes.md" 'ghostty_unsafe' 2 \
  'an unsafe declaration is shown in the summary'
count_is "$tmp/shapes.md" 'ghostty_attribute' 2 \
  'a declaration with an attribute is shown in the summary'
count_is "$tmp/shapes.md" 'ghostty_partial' 2 \
  'a static partial declaration is shown in the summary'
count_is "$tmp/shapes.md" 'this line used to read' 1 \
  'a comment that mentions static extern is only in the full diff'

# NUL is valid UTF-8, so the encoding check accepts it, but `grep` would then
# print a runner path instead of the declaration.
printf 'diff --git a/x b/x\n--- a/x\n+++ b/x\n@@ -1,2 +1,3 @@\n+before\0after\n+    public static extern int ghostty_nul();\n' \
  > "$tmp/nul.diff"
build "$tmp/nul.diff" "$tmp/nul.md"
expect_rc 2 "$?" 'a diff containing a NUL byte is refused'

# A diff of nothing but file headers has no content to show.
cat > "$tmp/header.diff" <<'DIFF'
diff --git a/old b/new
similarity index 100%
rename from old
rename to new
DIFF
build "$tmp/header.diff" "$tmp/header.md"
expect_rc 2 "$?" 'a header-only diff is refused'

# 400 lines of about 200 bytes each exceed the 20000-byte full-diff budget.
{
  printf 'diff --git a/x b/x\n--- a/x\n+++ b/x\n@@ -0,0 +1,400 @@\n'
  printf '+EARLY_COMPLETE_LINE\n'
  filler=$(printf 'x%.0s' $(seq 1 200))
  i=0
  while [ "$i" -lt 398 ]; do
    printf '+%s\n' "$filler"
    i=$((i + 1))
  done
  printf '+LATE_COMPLETE_LINE\n'
} > "$tmp/long.diff"
build "$tmp/long.diff" "$tmp/long.md"
contains "$tmp/long.md" 'EARLY_COMPLETE_LINE' \
  'a long diff keeps an early complete line'
absent "$tmp/long.md" 'LATE_COMPLETE_LINE' \
  'a long diff drops a line beyond the budget'
contains "$tmp/long.md" 'The diff is truncated here.' \
  'a truncated body says it is truncated'

# A budget smaller than a single line has nothing to show. The placeholder that
# says so must not be counted as a changed signature line.
cat > "$tmp/signatures.diff" <<'DIFF'
diff --git a/x b/x
--- a/x
+++ b/x
@@ -1,3 +1,3 @@
+    public static extern int Alpha();
+    public static extern int Beta();
+    public static extern int Gamma();
DIFF
build "$tmp/signatures.diff" "$tmp/nofit.md" MAX_SIGNATURE_BYTES=1
contains "$tmp/nofit.md" 'no changed signature line fits the 1-byte summary budget' \
  'a budget too small for one line says nothing fits'
absent "$tmp/nofit.md" 'Showing the first' \
  'a no-fit summary does not claim a shown count'

# A backtick run in the diff must not be able to close the code fence.
cat > "$tmp/backticks.diff" <<'DIFF'
diff --git a/x b/x
--- a/x
+++ b/x
@@ -1 +1 @@
-a
+code ```` four backticks
DIFF
build "$tmp/backticks.diff" "$tmp/backticks.md"
contains "$tmp/backticks.md" '`````diff' \
  'a backtick run opens a longer fence'
contains "$tmp/backticks.md" 'code ```` four backticks' \
  'a safe backtick run is left in place'

# The body is posted as UTF-8, and a blank diff would render an empty block.
printf 'diff --git a/x b/x\n--- a/x\n+++ b/x\n@@ -1 +1 @@\n-a\n+caf\351 latin1\n' \
  > "$tmp/invalid.diff"
build "$tmp/invalid.diff" "$tmp/invalid.md"
expect_rc 2 "$?" 'invalid UTF-8 is refused'

printf '  \n\n\t\n' > "$tmp/blank.diff"
build "$tmp/blank.diff" "$tmp/blank.md"
expect_rc 2 "$?" 'whitespace-only input is refused'

printf '\nbuilder behaviour: %s checks, %s failed\n' "$checks" "$failures"
[ "$failures" -eq 0 ]
