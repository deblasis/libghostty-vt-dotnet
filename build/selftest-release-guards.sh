#!/usr/bin/env bash
#
# Self-test for the release workflow's version guards.
#
# The guards decide what may be published to nuget.org, and a nuget.org publish
# is IRREVERSIBLE -- a wrong version can only be unlisted, never replaced. They
# are also the kind of code whose working state is invisible: they exist to
# refuse, so a guard that has quietly become unable to refuse looks exactly like
# a guard that is passing. Same reasoning as selftest-binding-gates.sh.
#
# This does NOT reimplement the guards. It extracts the actual `run:` blocks out
# of .github/workflows/release.yml and executes them with injected inputs,
# because a test carrying its own copy of the logic stops testing the thing that
# ships the moment the two drift.
#
# It also asserts the WIRING around those blocks -- the `if:` conditions and the
# job `outputs:` map -- because a review of the first version of this file
# demonstrated four edits to release.yml that break the guards for real while a
# run-block-only suite stays fully green. Dropping the quotes in
# `is_dry_run == 'false'` is the worst of them: GitHub then coerces the compare
# to numbers, it is never true, and Guard 2 simply never runs.
#
# Usage: build/selftest-release-guards.sh

set -uo pipefail

HERE=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
REPO=$(cd "$HERE/.." && pwd)
WORKFLOW="$REPO/.github/workflows/release.yml"

TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

# The guard parses build.zig.zon with `grep -oP`. Under some locales MSYS grep
# reports no PCRE support, and then the guard dies before reaching any check
# under test: every refusal case would "pass" without exercising anything.
# C.UTF-8 is enough to get PCRE on the environments seen so far; if it still is
# not available, refuse to report rather than mislead.
export LC_ALL=${LC_ALL:-C.UTF-8}
if ! echo x | grep -oP 'x' >/dev/null 2>&1; then
  echo "::error::This grep has no -P (PCRE) support, which the guard depends on." >&2
  echo "Refusing to report: refusal cases would pass without exercising their check." >&2
  exit 2
fi

pass=0
fail=0

ok()   { printf '  ok    %-58s\n' "$1"; pass=$((pass + 1)); }
bad()  { printf '  FAIL  %-58s %s\n' "$1" "${2:-}"; fail=$((fail + 1)); }

check() { # check <name> <expected-rc> <actual-rc>
  if [ "$2" -eq "$3" ]; then ok "$1"; else bad "$1" "(expected rc=$2, got rc=$3)"; fi
}

# A guard that exits non-zero for the wrong reason is indistinguishable from one
# that works. Every refusal names the message it expects.
#
# $LAST_LOG captures stdout AND stderr: the guards report through
# `echo "::error::..."` on STDOUT. The first version of this file captured only
# stderr, so every one of these assertions read an empty file and the whole
# refusal half of the suite was inert while reporting failures it could not
# explain.
check_refusal() { # check_refusal <name> <actual-rc> <expected-message-substring>
  if [ "$2" -eq 0 ]; then
    bad "$1" "(expected refusal, got rc=0)"
  elif ! grep -qF "$3" "$LAST_LOG"; then
    bad "$1" "(refused, but not for the reason under test)"
    printf '        wanted: %s\n' "$3"
    printf '        got:    %s\n' "$(head -1 "$LAST_LOG")"
  else
    ok "$1"
  fi
}

# ---------------------------------------------------------------------------
# Extraction. Requires exactly one matching step: a decoy step whose name also
# matches would otherwise be silently preferred by position.
# ---------------------------------------------------------------------------
extract() { # extract <job> <step-name-substring> <outfile>
  python3 - "$WORKFLOW" "$1" "$2" "$3" <<'PY'
import sys, io, yaml
wf, job, needle, out = sys.argv[1:5]
d = yaml.safe_load(io.open(wf, encoding='utf-8'))
hits = [st for st in d['jobs'][job]['steps']
        if needle.lower() in str(st.get('name', '')).lower() and st.get('run')]
if len(hits) != 1:
    sys.stderr.write(f"expected exactly 1 step matching {needle!r} in {job!r}, found {len(hits)}\n")
    sys.exit(1)
io.open(out, 'w', encoding='utf-8').write(hits[0]['run'])
PY
}

extract preflight "Compute version" "$TMP/compute.sh" || exit 1
extract preflight "Guard 2"         "$TMP/guard2.sh"  || exit 1

# Any ${{ }} the substitutions below do not cover would reach bash as a "bad
# substitution", which bash reports and then CONTINUES past, assigning empty.
# Without this the harness silently tests a mutilated copy of the script.
assert_fully_substituted() { # assert_fully_substituted <file> <label>
  if grep -q '\${{' "$1"; then
    echo "::error::$2 still contains an unsubstituted \${{ }} expression:" >&2
    grep -n '\${{' "$1" >&2
    echo "The harness would execute a mutilated copy. Extend the sed in this script." >&2
    exit 2
  fi
}

# GitHub runs these as `bash -e {0}`; match that, or a failing command mid-script
# is invisible here and fatal there.
run_block() { # run_block <script> [env assignments...]
  LAST_LOG="$TMP/last.log"
  : > "$LAST_LOG"
  ( set -e; "$@" ) >"$LAST_LOG" 2>&1
  return $?
}

# ---------------------------------------------------------------------------
# Guard 1 — tag shape and version decomposition.
# ---------------------------------------------------------------------------
run_compute() { # run_compute <tag> [event_name]
  sed -e 's|\${{ github.event_name }}|EVENT_PLACEHOLDER|g' \
      -e 's|\${{ github.ref_name }}|TAG_PLACEHOLDER|g' \
      -e 's|\${{ github.run_id }}|0|g' \
      "$TMP/compute.sh" > "$TMP/compute.run.sh"
  assert_fully_substituted "$TMP/compute.run.sh" "Compute version"
  : > "$TMP/out.env"
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  ( GITHUB_OUTPUT="$TMP/out.env" EVENT_NAME="${2:-push}" TAG="$1" RUN_ID=0 \
      bash --noprofile --norc -e "$TMP/compute.run.sh" ) >"$LAST_LOG" 2>&1
  return $?
}

out_val() { grep -E "^$1=" "$TMP/out.env" | tail -1 | cut -d= -f2-; }

echo "== guard 1: tag shape =="

run_compute "v1.3.2";        check "stable tag accepted"                   0 $?
run_compute "v1.3.2.1";      check "packaging respin accepted"             0 $?
run_compute "v1.4.0-rc.1";   check "interim prerelease accepted"           0 $?
run_compute "v1.2";          check_refusal "two-component tag refused"     $? "does not match"
run_compute "vfoo";          check_refusal "non-numeric tag refused"       $? "does not match"
run_compute "1.3.2";         check_refusal "tag without v refused"         $? "does not match"
run_compute "v1.0.0-";       check_refusal "empty prerelease label refused" $? "does not match"

# The label alphabet is load-bearing, not cosmetic. The daily sync publishes
# `<upstream>-ci.<ts>.<sha>` to the SAME nuget package, and SemVer orders
# prerelease identifiers alphabetically: alpha < beta < ci < dev < rc. Anything
# below `ci` would sort under every nightly already published and never resolve
# for a --prerelease consumer -- permanently, since nuget cannot withdraw.
run_compute "v1.3.2-beta.2"
check_refusal "beta label refused (would sort below the -ci.* stream)"  $? "does not match"
run_compute "v1.3.2-alpha.1"
check_refusal "alpha label refused (same reason)"                       $? "does not match"
run_compute "v1.3.2-rc.x"
check_refusal "non-numeric rc ordinal refused"                          $? "does not match"

echo "== guard 1: version decomposition =="

run_compute "v1.4.0-rc.1"
[ "$(out_val package_version)" = "1.4.0-rc.1" ] && ok "prerelease keeps its full version" || bad "prerelease keeps its full version"
[ "$(out_val base_version)" = "1.4.0" ]         && ok "prerelease base strips the suffix"  || bad "prerelease base strips the suffix"
[ "$(out_val is_prerelease)" = "true" ]         && ok "prerelease flagged"                 || bad "prerelease flagged"

run_compute "v1.3.2"
[ "$(out_val base_version)" = "1.3.2" ]  && ok "stable base is the version"      || bad "stable base is the version"
[ "$(out_val is_prerelease)" = "false" ] && ok "stable not flagged prerelease"   || bad "stable not flagged prerelease"

run_compute "v1.3.2.4"
[ "$(out_val base_version)" = "1.3.2" ]  && ok "respin base drops the 4th digit" || bad "respin base drops the 4th digit"

# The dry-run branch is the ONLY thing stopping a workflow_dispatch run from
# publishing 0.0.0-dryrun.<run_id> permanently, and nothing else in this suite
# reaches it.
run_compute "not-a-tag" "workflow_dispatch"
check "workflow_dispatch takes the dry-run branch" 0 $?
[ "$(out_val is_dry_run)" = "true" ] && ok "workflow_dispatch sets is_dry_run=true" || bad "workflow_dispatch sets is_dry_run=true"

# ---------------------------------------------------------------------------
# Guard 2 — tag base vs the upstream version at the pinned commit.
# The real block curls build.zig.zon; stub curl so upstream is an input.
# ---------------------------------------------------------------------------
mkdir -p "$TMP/bin"
cat > "$TMP/bin/curl" <<'STUB'
#!/usr/bin/env bash
printf '.{\n    .version = "%s",\n}\n' "$FAKE_UPSTREAM_VERSION"
STUB
chmod +x "$TMP/bin/curl"

run_guard2() { # run_guard2 <base> <is_prerelease> <upstream_version>
  sed -e 's|\${{ steps.pin.outputs.upstream_commit }}|deadbeef|g' \
      -e "s|\\\${{ steps.compute.outputs.base_version }}|$1|g" \
      -e "s|\\\${{ steps.compute.outputs.is_prerelease }}|$2|g" \
      "$TMP/guard2.sh" > "$TMP/guard2.run.sh"
  assert_fully_substituted "$TMP/guard2.run.sh" "Guard 2"
  : > "$TMP/out2.env"
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  ( PATH="$TMP/bin:$PATH" FAKE_UPSTREAM_VERSION="$3" GITHUB_OUTPUT="$TMP/out2.env" \
      bash --noprofile --norc -e "$TMP/guard2.run.sh" ) >"$LAST_LOG" 2>&1
  return $?
}

out2_val() { grep -E "^$1=" "$TMP/out2.env" | tail -1 | cut -d= -f2-; }

echo "== guard 2: stable tags =="

run_guard2 1.3.2 false 1.3.2
check "stable matching a stable upstream is allowed" 0 $?
# An allow that writes nothing is still a broken release: the notes read
# "(Ghostty )" and the mirror claim becomes unverifiable.
[ "$(out2_val upstream_version)" = "1.3.2" ] && ok "allow records the upstream version" || bad "allow records the upstream version"

run_guard2 1.4.0 false 1.3.2
check_refusal "stable NOT matching upstream is refused"    $? "does not match upstream"
run_guard2 1.3.2 false 1.3.2-dev
check_refusal "stable against a -dev upstream is refused"  $? "is a -dev build"
run_guard2 1.4.0 false 1.3.2-dev
check_refusal "stable ahead of a -dev upstream is refused" $? "is a -dev build"

echo "== guard 2: interim prereleases =="

run_guard2 1.4.0 true 1.3.2-dev
check "prerelease AHEAD of a -dev upstream is allowed"            0 $?
run_guard2 1.3.2 true 1.3.2-dev
check "prerelease matching the in-flight version is allowed"      0 $?
run_guard2 1.4.0 true 1.4.0-dev
check "prerelease of the version upstream is building is allowed" 0 $?

run_guard2 1.2.0 true 1.3.2-dev
check_refusal "prerelease BEHIND upstream is refused"             $? "is BEHIND upstream"
run_guard2 1.3.1 true 1.3.2
check_refusal "prerelease behind a stable upstream is refused"    $? "is BEHIND upstream"

# A lexicographic compare puts 1.10.0 below 1.9.0 and would refuse a legitimate
# tag.
run_guard2 1.10.0 true 1.9.0-dev
check "version compare is numeric, not lexicographic"             0 $?

# ---------------------------------------------------------------------------
# Wiring. None of the above can see an `if:` condition or the outputs map, and
# both decide whether the guards run at all.
# ---------------------------------------------------------------------------
echo "== wiring =="

wiring() { # wiring <name> <python-expression-over-d>
  if python3 - "$WORKFLOW" "$2" <<'PY' >/dev/null 2>&1
import sys, io, yaml
d = yaml.safe_load(io.open(sys.argv[1], encoding='utf-8'))
sys.exit(0 if eval(sys.argv[2]) else 1)
PY
  then ok "$1"; else bad "$1"; fi
}

# GitHub coerces a string-vs-boolean compare to numbers, so `== false` (unquoted)
# is never true and the guarded step silently never runs.
wiring "Guard 2 is gated on the QUOTED is_dry_run == 'false'" \
  "any(\"is_dry_run == 'false'\" in str(s.get('if','')) for s in d['jobs']['preflight']['steps'] if 'Guard 2' in str(s.get('name','')))"
wiring "publish is gated on the QUOTED is_dry_run == 'false'" \
  "\"is_dry_run == 'false'\" in str(d['jobs']['publish'].get('if',''))"
wiring "preflight exports is_prerelease" \
  "'is_prerelease' in d['jobs']['preflight']['outputs']"
wiring "preflight exports is_dry_run" \
  "'is_dry_run' in d['jobs']['preflight']['outputs']"
wiring "preflight exports package_version" \
  "'package_version' in d['jobs']['preflight']['outputs']"
wiring "publish still requires the nuget-release environment approval" \
  "d['jobs']['publish'].get('environment') == 'nuget-release'"
# Interpolating github.ref_name into bash is a command-substitution site: a git
# ref may legally contain a backtick, and it would run before Guard 1 looks at
# the tag.
wiring "the tag reaches the script through env, not interpolation" \
  "all('github.ref_name' not in str(s.get('run','')) for s in d['jobs']['preflight']['steps'])"

echo
echo "$pass passed, $fail failed"
[ "$fail" -eq 0 ]
