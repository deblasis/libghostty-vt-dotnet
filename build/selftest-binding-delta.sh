#!/usr/bin/env bash
# shellcheck disable=SC2016
# assertions quote Markdown; the backticks are literal
#
# Self-test for build/summarise-binding-delta.sh.
#
# The bug this guards is not a crash: when upstream changed only the generated
# version constants, the tracker filed an issue whose signature section was
# empty, so the issue showed no delta at all. A body builder that silently
# drops the diff looks healthy in normal runs, which is why every case here
# asserts on the body it produces.
#
# Usage: build/selftest-binding-delta.sh

set -uo pipefail


HERE=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
SCRIPT="$HERE/summarise-binding-delta.sh"
COMMIT=0123456789abcdef0123456789abcdef01234567
RUN_URL=https://example.invalid/actions/runs/1

TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

pass=0
fail=0

ok() {
  printf '  ok    %s\n' "$1"
  pass=$((pass + 1))
}

bad() {
  printf '  FAIL  %s\n' "$1"
  fail=$((fail + 1))
}

check_contains() { # check_contains <name> <file> <needle>
  if grep -qF -- "$3" "$2"; then ok "$1"; else bad "$1 (missing: $3)"; fi
}

check_not_contains() { # check_not_contains <name> <file> <needle>
  if grep -qF -- "$3" "$2"; then bad "$1 (unexpected: $3)"; else ok "$1"; fi
}

check_rc() { # check_rc <name> <expected> <actual>
  if [ "$2" -eq "$3" ]; then ok "$1 (rc=$3)"; else bad "$1 (expected rc=$2, got rc=$3)"; fi
}

# --- The exact regression: a delta made only of version constants ----------
cat > "$TMP/version-only.diff" <<'EOF'
diff --git a/build/bindings-reference/NativeMethods.pinned.cs b/src/Ghostty.Vt/Native/NativeMethods.cs
--- a/build/bindings-reference/NativeMethods.pinned.cs
+++ b/src/Ghostty.Vt/Native/NativeMethods.cs
@@ -3150,12 +3150,12 @@
-        [NativeTypeName("#define GHOSTTY_VT_VERSION_MAJOR 1")]
-        public const int GHOSTTY_VT_VERSION_MAJOR = 1;
-        [NativeTypeName("#define GHOSTTY_VT_VERSION_MINOR 1")]
-        public const int GHOSTTY_VT_VERSION_MINOR = 1;
+        [NativeTypeName("#define GHOSTTY_VT_VERSION_MAJOR 1")]
+        public const int GHOSTTY_VT_VERSION_MAJOR = 1;
+        [NativeTypeName("#define GHOSTTY_VT_VERSION_MINOR 0")]
+        public const int GHOSTTY_VT_VERSION_MINOR = 0;
-        [NativeTypeName("#define GHOSTTY_VT_VERSION \"1.1.0\"")]
-        public static ReadOnlySpan<byte> GHOSTTY_VT_VERSION => "1.1.0"u8;
+        [NativeTypeName("#define GHOSTTY_VT_VERSION \"1.0.2\"")]
+        public static ReadOnlySpan<byte> GHOSTTY_VT_VERSION => "1.0.2"u8;
EOF

bash "$SCRIPT" "$TMP/version-only.diff" "$COMMIT" "$RUN_URL" > "$TMP/version-only.body"
check_rc "version-only delta renders" 0 "$?"
check_contains "version-only body names the changed constant" "$TMP/version-only.body" 'GHOSTTY_VT_VERSION_MINOR'
check_contains "version-only body shows the diff" "$TMP/version-only.body" '+        public const int GHOSTTY_VT_VERSION_MINOR = 0;'
check_contains "version-only body says no signature changed" "$TMP/version-only.body" 'No `static extern` signature lines changed.'
check_not_contains "version-only body has no signature section" "$TMP/version-only.body" 'Signature-level changes:'

# An empty diff block is the defect; assert the fence never closes on nothing.
# Python keeps the check portable (BSD grep has no -P).
if python3 - "$TMP/version-only.body" <<'PY'
import sys

body = open(sys.argv[1], encoding="utf-8").read()
raise SystemExit(0 if "```diff\n\n```" in body else 1)
PY
then
  bad "version-only body never contains an empty diff block"
else
  ok "version-only body never contains an empty diff block"
fi

# --- A real signature delta keeps both the summary and the full diff --------
cat > "$TMP/signature.diff" <<'EOF'
diff --git a/build/bindings-reference/NativeMethods.pinned.cs b/src/Ghostty.Vt/Native/NativeMethods.cs
--- a/build/bindings-reference/NativeMethods.pinned.cs
+++ b/src/Ghostty.Vt/Native/NativeMethods.cs
@@ -100,6 +100,7 @@
-        public static extern int ghostty_old_entry();
+        public static extern int ghostty_new_entry();
+        // MARKER_NON_SIGNATURE_LINE
         [NativeTypeName("GhosttyTerminal *")]
         public static extern int ghostty_unchanged_entry();
EOF

bash "$SCRIPT" "$TMP/signature.diff" "$COMMIT" "$RUN_URL" > "$TMP/signature.body"
check_rc "signature delta renders" 0 "$?"
check_contains "signature body has the summary" "$TMP/signature.body" 'Signature-level changes:'
check_contains "signature body shows the added entry point" "$TMP/signature.body" '+        public static extern int ghostty_new_entry();'
check_contains "signature body shows the removed entry point" "$TMP/signature.body" '-        public static extern int ghostty_old_entry();'
check_contains "signature body still carries the full diff" "$TMP/signature.body" '+        // MARKER_NON_SIGNATURE_LINE'

# --- A comment that mentions `static extern` is not a signature change -----
cat > "$TMP/comment-static.diff" <<'EOF'
diff --git a/x b/x
--- a/x
+++ b/x
@@ -1,1 +1,2 @@
+        // see public static extern int ghostty_x(); for details
+        public const int GHOSTTY_VT_VERSION_MINOR = 0;
EOF

bash "$SCRIPT" "$TMP/comment-static.diff" "$COMMIT" "$RUN_URL" \
  > "$TMP/comment-static.body"
check_rc "comment-only delta renders" 0 "$?"
check_contains "comment-only delta says no signature changed" \
  "$TMP/comment-static.body" 'No `static extern` signature lines changed.'
check_not_contains "comment-only delta has no signature section" \
  "$TMP/comment-static.body" 'Signature-level changes:'

# --- A capped signature summary says how much it is showing ---------------
awk 'BEGIN {
  print "diff --git a/x b/x"
  print "--- a/x"
  print "+++ b/x"
  print "@@ -1,120 +1,120 @@"
  for (i = 1; i <= 120; i++) {
    printf "+        public static extern int ghostty_sig_%03d();\n", i
  }
}' > "$TMP/many-signatures.diff"

bash "$SCRIPT" "$TMP/many-signatures.diff" "$COMMIT" "$RUN_URL" \
  > "$TMP/many-signatures.body"
check_rc "many signatures render" 0 "$?"
check_contains "line-capped summary reports the omitted count" \
  "$TMP/many-signatures.body" 'Showing the first 50 of 120 changed signature lines'

# The byte cap binds before the line cap when the lines are long.
MAX_SIGNATURE_BYTES=400 bash "$SCRIPT" "$TMP/many-signatures.diff" "$COMMIT" "$RUN_URL" \
  > "$TMP/byte-capped.body"
check_rc "byte-capped summary renders" 0 "$?"
if grep -qE 'Showing the first [0-9]+ of 120 changed signature lines' \
  "$TMP/byte-capped.body"; then
  ok "byte-capped summary reports the omitted count"
else
  bad "byte-capped summary reports the omitted count"
fi

# When no signature line fits the budget at all, the block holds a placeholder
# rather than a changed line. The notice must not count it as one.
MAX_SIGNATURE_BYTES=1 bash "$SCRIPT" "$TMP/many-signatures.diff" \
  "$COMMIT" "$RUN_URL" > "$TMP/no-fit.body"
check_rc "no-fit signature summary renders" 0 "$?"
check_contains "no-fit summary says nothing fits" "$TMP/no-fit.body" \
  'no changed signature line fits the 1-byte summary budget'
check_not_contains "no-fit summary does not claim to show a line" \
  "$TMP/no-fit.body" 'Showing the first'

# --- A large diff is truncated on a line boundary, not mid-character -------
awk 'BEGIN {
  print "diff --git a/x b/x"
  print "--- a/x"
  print "+++ b/x"
  print "@@ -1,400 +1,400 @@"
  for (i = 1; i <= 400; i++) printf "+LINE_%04d padding padding padding padding\n", i
}' > "$TMP/large.diff"

MAX_DIFF_BYTES=500 bash "$SCRIPT" "$TMP/large.diff" "$COMMIT" "$RUN_URL" > "$TMP/large.body"
check_rc "truncated delta renders" 0 "$?"
check_contains "truncated body says so" "$TMP/large.body" 'The diff is truncated here.'
check_contains "truncated body keeps early lines" "$TMP/large.body" '+LINE_0001 padding'
check_not_contains "truncated body drops late lines" "$TMP/large.body" '+LINE_0400 padding'
if grep -qE '^\+LINE_[0-9]{1,3}([^0-9]|$)' "$TMP/large.body"; then
  bad "truncation keeps whole lines"
else
  ok "truncation keeps whole lines"
fi

# --- The body stays inside GitHub's 65536-character issue limit -----------
awk 'BEGIN {
  print "diff --git a/x b/x"
  print "--- a/x"
  print "+++ b/x"
  for (i = 1; i <= 4000; i++) printf "+%04d aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n", i
}' > "$TMP/huge.diff"
bash "$SCRIPT" "$TMP/huge.diff" "$COMMIT" "$RUN_URL" > "$TMP/huge.body"
size=$(wc -c < "$TMP/huge.body" | tr -d ' ')
if [ "$size" -lt 65536 ]; then
  ok "huge delta stays under the issue limit (${size} bytes)"
else
  bad "huge delta stays under the issue limit (${size} bytes)"
fi

# --- A single enormous line cannot produce an empty block or blow the cap --
awk 'BEGIN {
  print "diff --git a/x b/x"
  print "--- a/x"
  print "+++ b/x"
  printf "+"
  for (i = 0; i < 200000; i++) printf "a"
  printf "\n"
}' > "$TMP/one-line.diff"
bash "$SCRIPT" "$TMP/one-line.diff" "$COMMIT" "$RUN_URL" > "$TMP/one-line.body"
check_rc "single oversized line renders" 0 "$?"
check_contains "single oversized line is marked truncated" "$TMP/one-line.body" 'The diff is truncated here.'
size=$(wc -c < "$TMP/one-line.body" | tr -d ' ')
if [ "$size" -lt 65536 ]; then
  ok "single oversized line stays under the issue limit (${size} bytes)"
else
  bad "single oversized line stays under the issue limit (${size} bytes)"
fi

# If even the first line exceeds the budget the block must say why it is
# empty rather than rendering as an empty fence.
awk 'BEGIN {
  printf "+"
  for (i = 0; i < 200000; i++) printf "a"
  printf "\n"
}' > "$TMP/first-line-huge.diff"
MAX_DIFF_BYTES=100 bash "$SCRIPT" "$TMP/first-line-huge.diff" "$COMMIT" "$RUN_URL" > "$TMP/first-line-huge.body"
check_rc "first oversized line renders" 0 "$?"
check_contains "first oversized line is explained" "$TMP/first-line-huge.body" 'no complete line within the byte budget'

# --- Backticks in the diff cannot close the fence early --------------------
cat > "$TMP/injection.diff" <<'EOF'
diff --git a/x b/x
--- a/x
+++ b/x
@@ -1,2 +1,2 @@
+        // ```uh oh```
+        public static extern int ghostty_new_entry();
EOF
bash "$SCRIPT" "$TMP/injection.diff" "$COMMIT" "$RUN_URL" > "$TMP/injection.body"
check_contains "diff keeps the backtick line" "$TMP/injection.body" '// ```uh oh```'
if grep -qx '````diff' "$TMP/injection.body" && grep -qx '````' "$TMP/injection.body"; then
  ok "fence grows past the backticks in the diff"
else
  bad "fence grows past the backticks in the diff"
fi

# A pathological run cannot stretch the fence past the cap; the run itself is
# replaced so the fence still closes where the body says it does.
awk 'BEGIN {
  print "diff --git a/x b/x"
  print "--- a/x"
  print "+++ b/x"
  printf "+"
  for (i = 0; i < 5000; i++) printf "`"
  printf " end\n"
}' > "$TMP/huge-run.diff"
bash "$SCRIPT" "$TMP/huge-run.diff" "$COMMIT" "$RUN_URL" > "$TMP/huge-run.body"
check_contains "long backtick run is neutralised" "$TMP/huge-run.body" '[backticks removed]'
if grep -qx '``````````' "$TMP/huge-run.body"; then
  ok "fence stops at the cap"
else
  bad "fence stops at the cap"
fi
size=$(wc -c < "$TMP/huge-run.body" | tr -d ' ')
if [ "$size" -lt 65536 ]; then
  ok "long backtick run stays under the issue limit (${size} bytes)"
else
  bad "long backtick run stays under the issue limit (${size} bytes)"
fi

# --- Bad input fails loudly instead of filing a broken issue --------------
bash "$SCRIPT" "$TMP/missing.diff" "$COMMIT" "$RUN_URL" >/dev/null 2>&1
check_rc "missing diff file is refused" 2 "$?"

: > "$TMP/empty.diff"
bash "$SCRIPT" "$TMP/empty.diff" "$COMMIT" "$RUN_URL" >/dev/null 2>&1
check_rc "empty diff file is refused" 2 "$?"

# `-s` accepts a newline-only file, which would render an empty block.
printf '\n' > "$TMP/blank.diff"
bash "$SCRIPT" "$TMP/blank.diff" "$COMMIT" "$RUN_URL" >/dev/null 2>&1
check_rc "whitespace-only diff file is refused" 2 "$?"

# A bare header block has no content to show; refusing it beats filing an
# issue whose diff section is empty.
cat > "$TMP/header-only.diff" <<'EOF'
diff --git a/old b/new
similarity index 100%
rename from old
rename to new
EOF
bash "$SCRIPT" "$TMP/header-only.diff" "$COMMIT" "$RUN_URL" >/dev/null 2>&1
check_rc "diff with no hunk or changed line is refused" 2 "$?"

# The body is posted as UTF-8; a byte that cannot decode is refused here
# rather than failing the request after the step went green.
printf 'diff --git a/x b/x\n--- a/x\n+++ b/x\n@@ -1 +1 @@\n+caf\351 latin1\n' \
  > "$TMP/invalid-utf8.diff"
bash "$SCRIPT" "$TMP/invalid-utf8.diff" "$COMMIT" "$RUN_URL" >/dev/null 2>&1
check_rc "diff that is not valid UTF-8 is refused" 2 "$?"

bash "$SCRIPT" >/dev/null 2>&1
check_rc "missing arguments are refused" 2 "$?"

printf '\nsummarise-binding-delta selftest: %s passed, %s failed\n' "$pass" "$fail"
[ "$fail" -eq 0 ]
