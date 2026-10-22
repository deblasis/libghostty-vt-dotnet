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
DAILY="$REPO/.github/workflows/daily-upstream.yml"
REAL_PYTHON=$(command -v python3)

TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

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
extract_daily() {
  python3 - "$DAILY" "publish" "Compute version" "$TMP/daily-compute.sh" <<'PY'
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
extract_daily || exit 1

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
run_compute() { # run_compute <tag> [event_name] [version]
  local version=${3:-1.4.0}
  mkdir -p "$TMP/work/build"
  cp "$REPO/build/read-package-version.sh" "$TMP/work/build/"
  printf '{"version":"%s"}\n' "$version" > "$TMP/work/version.json"
  sed -e 's|\${{ github.event_name }}|EVENT_PLACEHOLDER|g' \
      -e 's|\${{ github.ref_name }}|TAG_PLACEHOLDER|g' \
      -e 's|\${{ github.run_id }}|0|g' \
      "$TMP/compute.sh" > "$TMP/compute.run.sh"
  assert_fully_substituted "$TMP/compute.run.sh" "Compute version"
  : > "$TMP/out.env"
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  ( cd "$TMP/work" && GITHUB_OUTPUT="$TMP/out.env" EVENT_NAME="${2:-push}" TAG="$1" RUN_ID=0 \
      bash --noprofile --norc -e "$TMP/compute.run.sh" ) >"$LAST_LOG" 2>&1
  return $?
}

out_val() { grep -E "^$1=" "$TMP/out.env" | tail -1 | cut -d= -f2-; }

echo "== guard 1: tag shape =="

run_compute "v1.4.0"        push 1.4.0; check "stable tag accepted"                    0 $?
run_compute "v1.4.0.1"      push 1.4.0; check "packaging respin accepted"              0 $?
run_compute "v1.4.0-rc.1"   push 1.4.0; check "release candidate accepted"             0 $?
run_compute "v1.4.0.0"      push 1.4.0; check_refusal "respin zero refused"             $? "canonical"
run_compute "v1.4.0.01"     push 1.4.0; check_refusal "leading-zero respin refused"     $? "canonical"
run_compute "v01.4.0"       push 1.4.0; check_refusal "leading-zero base refused"       $? "canonical"
run_compute "v1.4.0-rc.01"  push 1.4.0; check_refusal "leading-zero RC refused"         $? "canonical"
run_compute "v1.4.0.1-rc.1" push 1.4.0; check_refusal "combined respin/RC refused"      $? "canonical"
run_compute "v1.2"          push 1.4.0; check_refusal "two-component tag refused"       $? "canonical"
run_compute "vfoo"          push 1.4.0; check_refusal "non-numeric tag refused"         $? "canonical"
run_compute "1.4.0"         push 1.4.0; check_refusal "tag without v refused"           $? "canonical"
run_compute "v1.0.0-"       push 1.0.0; check_refusal "empty prerelease label refused"  $? "canonical"
run_compute "v1.4.0-beta.2" push 1.4.0; check_refusal "beta label refused"              $? "canonical"
run_compute "v1.4.0-alpha.1" push 1.4.0; check_refusal "alpha label refused"            $? "canonical"
run_compute "v1.4.0-rc.x"   push 1.4.0; check_refusal "non-numeric rc ordinal refused"  $? "canonical"

echo "== guard 1: version decomposition and version.json match =="

run_compute "v1.4.0-rc.1" push 1.4.0
[ "$(out_val package_version)" = "1.4.0-rc.1" ] && ok "prerelease keeps its full version" || bad "prerelease keeps its full version"
[ "$(out_val base_version)" = "1.4.0" ]         && ok "prerelease base strips the suffix"  || bad "prerelease base strips the suffix"
[ "$(out_val is_prerelease)" = "true" ]         && ok "prerelease flagged"                 || bad "prerelease flagged"
[ "$(out_val is_respin)" = "false" ]            && ok "prerelease is not a respin"         || bad "prerelease is not a respin"

run_compute "v1.4.0" push 1.4.0
[ "$(out_val base_version)" = "1.4.0" ]  && ok "stable base is the version"      || bad "stable base is the version"
[ "$(out_val is_prerelease)" = "false" ] && ok "stable not flagged prerelease"   || bad "stable not flagged prerelease"
[ "$(out_val is_respin)" = "false" ]     && ok "stable is not a respin"          || bad "stable is not a respin"

run_compute "v1.4.0.4" push 1.4.0
[ "$(out_val base_version)" = "1.4.0" ]  && ok "respin base drops the 4th digit" || bad "respin base drops the 4th digit"
[ "$(out_val is_respin)" = "true" ]      && ok "respin flagged"                  || bad "respin flagged"

run_compute "v1.4.0.1" push 1.4.1
check "respin of the previous stable after a version bump is allowed" 0 $?
run_compute "v1.4.2.1" push 1.4.1
check_refusal "respin ahead of version.json refused" $? "ahead of version.json"

run_compute "v1.4.0" push 1.5.0
check_refusal "tag behind version.json refused"       $? "does not match version.json"
run_compute "v1.5.0" push 1.4.0
check_refusal "tag ahead of version.json refused"     $? "does not match version.json"
run_compute "v1.4.0" push 1.4.0-rc.1
check_refusal "non-plain version.json refused"        $? "plain canonical"

# The dry-run branch is the ONLY thing stopping a workflow_dispatch run from
# publishing 0.0.0-dryrun.<run_id> permanently, and nothing else in this suite
# reaches it.
run_compute "not-a-tag" workflow_dispatch 1.4.0
check "workflow_dispatch takes the dry-run branch" 0 $?
[ "$(out_val is_dry_run)" = "true" ] && ok "workflow_dispatch sets is_dry_run=true" || bad "workflow_dispatch sets is_dry_run=true"

# ---------------------------------------------------------------------------
# Guard 2 — record the upstream version at the pinned commit.
# The real block curls build.zig.zon; stub curl so upstream is an input.
# ---------------------------------------------------------------------------
mkdir -p "$TMP/bin"
cat > "$TMP/bin/curl" <<'STUB'
#!/usr/bin/env bash
printf '.{\n    .version = "%s",\n}\n' "$FAKE_UPSTREAM_VERSION"
STUB
chmod +x "$TMP/bin/curl"

run_guard2() { # run_guard2 <upstream_version>
  sed -e 's|\${{ steps.pin.outputs.upstream_commit }}|deadbeef|g' \
      "$TMP/guard2.sh" > "$TMP/guard2.run.sh"
  assert_fully_substituted "$TMP/guard2.run.sh" "Guard 2"
  : > "$TMP/out2.env"
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  ( PATH="$TMP/bin:$PATH" FAKE_UPSTREAM_VERSION="$1" GITHUB_OUTPUT="$TMP/out2.env" \
      bash --noprofile --norc -e "$TMP/guard2.run.sh" ) >"$LAST_LOG" 2>&1
  return $?
}

out2_val() { grep -E "^$1=" "$TMP/out2.env" | tail -1 | cut -d= -f2-; }

echo "== guard 2: upstream version is recorded, not compared =="

run_guard2 1.3.2-dev
check "upstream -dev version is recorded"              0 $?
[ "$(out2_val upstream_version)" = "1.3.2-dev" ] && ok "record contains the upstream version" || bad "record contains the upstream version"

run_guard2 1.3.2
check "upstream stable version is recorded"            0 $?
[ "$(out2_val upstream_version)" = "1.3.2" ] && ok "record contains the stable version" || bad "record contains the stable version"

run_guard2 ""
check_refusal "missing upstream version refused"       $? "Could not parse"

# ---------------------------------------------------------------------------
# Version readers and the NuGet ordering guard. These run outside the extracted
# workflow blocks, but the workflows call the same files.
# ---------------------------------------------------------------------------
echo "== version readers =="

run_pkg() { # run_pkg <json>
  mkdir -p "$TMP/pkg"
  printf '%s\n' "$1" > "$TMP/pkg/version.json"
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  ( cd "$TMP/pkg" && bash "$REPO/build/read-package-version.sh" ) >"$LAST_LOG" 2>&1
  return $?
}

run_pkg '{"version":"1.4.0"}'
check "canonical package version accepted" 0 $?
[ "$(cat "$TMP/last.log")" = "1.4.0" ] && ok "reader prints the package version" || bad "reader prints the package version"

run_pkg '{"version":"01.4.0"}'
check_refusal "leading-zero package version refused" $? "plain canonical"
run_pkg '{"version":"1.4.0.1"}'
check_refusal "four-part package version refused" $? "plain canonical"
run_pkg '{"version":"1.4.0\npackage_version=9.9.9"}'
check_refusal "multiline package version refused" $? "plain canonical"
run_pkg '{"version":1}'
check_refusal "non-string package version refused" $? "string version"

run_upstream() { # run_upstream <zon-text>
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  printf '%s' "$1" | bash "$REPO/build/read-upstream-version.sh" >"$LAST_LOG" 2>&1
  return $?
}

run_upstream '.version = "1.3.2-dev",'
check "upstream -dev version accepted" 0 $?
[ "$(cat "$TMP/last.log")" = "1.3.2-dev" ] && ok "reader prints the upstream version" || bad "reader prints the upstream version"

run_upstream '// .version = "9.9.9"
.version = "1.3.2-dev",'
check "commented decoy is ignored" 0 $?
[ "$(cat "$TMP/last.log")" = "1.3.2-dev" ] && ok "top-level version wins over a comment" || bad "top-level version wins over a comment"

run_upstream 'const version = "1.3.2";'
check_refusal "missing top-level version refused" $? "Could not parse"
run_upstream '.version = "$(touch /tmp/decoy)",'
check_refusal "shell payload in upstream version refused" $? "valid SemVer"

echo "== NuGet ordering =="

mkdir -p "$TMP/nuget"
printf '{"versions":["1.4.0-rc.1","1.4.0-rc.2"]}' > "$TMP/nuget/rc.json"
printf '{"versions":["1.4.0-rc.1","1.4.0-rc.2","1.4.0"]}' > "$TMP/nuget/stable.json"
printf '{"versions":["1.4.0","1.4.0.1"]}' > "$TMP/nuget/respin.json"
printf '{"versions":["1.4.0","1.4.1"]}' > "$TMP/nuget/next.json"

run_nuget() { # run_nuget <args...>
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  python3 "$REPO/build/check-nuget-version.py" "$@" >"$LAST_LOG" 2>&1
  return $?
}

run_nuget nightly --version 1.4.0 --index-file "$TMP/nuget/rc.json"
check "nightly above no stable is allowed" 0 $?
run_nuget nightly --version 1.4.0 --index-file "$TMP/nuget/stable.json"
check_refusal "nightly at the stable version refused" $? "Bump version.json"
run_nuget nightly --version 1.4.1 --index-file "$TMP/nuget/stable.json"
check "nightly above the stable version allowed" 0 $?

run_nuget release --version 1.4.0 --base 1.4.0 --index-file "$TMP/nuget/rc.json"
check "stable above its RCs allowed" 0 $?
run_nuget release --version 1.4.0 --base 1.4.0 --index-file "$TMP/nuget/stable.json"
check_refusal "duplicate stable refused" $? "not ahead"
run_nuget release --version 1.4.1-rc.1 --base 1.4.1 --index-file "$TMP/nuget/stable.json"
check "RC above the stable base allowed" 0 $?
run_nuget release --version 1.4.0-rc.3 --base 1.4.0 --index-file "$TMP/nuget/stable.json"
check_refusal "RC below the stable refused" $? "RC base"
run_nuget release --version 1.4.0.1 --base 1.4.0 --index-file "$TMP/nuget/stable.json"
check "respin above the stable allowed" 0 $?
run_nuget release --version 1.4.0.1 --base 1.4.0 --index-file "$TMP/nuget/respin.json"
check_refusal "duplicate respin refused" $? "not ahead"
run_nuget release --version 1.4.0.2 --base 1.4.0 --index-file "$TMP/nuget/respin.json"
check "next respin allowed" 0 $?
run_nuget release --version 1.4.1 --base 1.4.1 --index-file "$TMP/nuget/next.json"
check_refusal "already-published stable refused" $? "not ahead"

echo "== packed version assertion =="

mkdir -p "$TMP/pkgzip"
cat > "$TMP/pkgzip/Test.nuspec" <<'EOF'
<?xml version="1.0"?>
<package><metadata><version>1.4.0</version></metadata></package>
EOF
( cd "$TMP/pkgzip" && zip -q "$TMP/Test.1.4.0.nupkg" Test.nuspec )
run_assert() {
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  bash "$REPO/build/assert-packed-version.sh" "$@" >"$LAST_LOG" 2>&1
  return $?
}
run_assert "$TMP/Test.1.4.0.nupkg" 1.4.0
check "packed version match accepted" 0 $?
run_assert "$TMP/Test.1.4.0.nupkg" 1.4.1
check_refusal "packed version mismatch refused" $? "does not match guarded version"

echo "== nightly version =="

mkdir -p "$TMP/daily-bin"
cat > "$TMP/daily-bin/python3" <<SH
#!/usr/bin/env bash
for arg in "\$@"; do
  case "\$arg" in
    *build/check-nuget-version.py) exit 0 ;;
  esac
done
exec "$REAL_PYTHON" "\$@"
SH
chmod +x "$TMP/daily-bin/python3"

run_daily_compute() { # run_daily_compute <version> <commit>
  mkdir -p "$TMP/daily-work/build"
  cp "$REPO/build/read-package-version.sh" "$TMP/daily-work/build/"
  printf '{"version":"%s"}\n' "$1" > "$TMP/daily-work/version.json"
  : > "$TMP/out-daily.env"
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  ( cd "$TMP/daily-work" && PATH="$TMP/daily-bin:$PATH" \
      GITHUB_OUTPUT="$TMP/out-daily.env" NEW_COMMIT="$2" \
      bash --noprofile --norc -e "$TMP/daily-compute.sh" ) >"$LAST_LOG" 2>&1
  return $?
}

out_daily_val() { grep -E "^$1=" "$TMP/out-daily.env" | tail -1 | cut -d= -f2-; }

run_daily_compute 1.4.1 0123456789abcdef0123456789abcdef01234567
check "nightly compute accepted" 0 $?
[ "$(out_daily_val package_version)" != "" ] && ok "nightly compute emits a package version" || bad "nightly compute emits a package version"
if [[ "$(out_daily_val package_version)" =~ ^1\.4\.1-ci\.[0-9]{12}\.g01234567$ ]]; then
  ok "nightly SHA suffix is a non-numeric identifier"
else
  bad "nightly SHA suffix is a non-numeric identifier" "$(out_daily_val package_version)"
fi

run_daily_compute '1.4.0\npackage_version=9.9.9' 0123456789abcdef0123456789abcdef01234567
check_refusal "nightly multiline version refused" $? "plain canonical"
run_daily_compute 1.4.1 not-a-sha
check_refusal "nightly invalid commit refused" $? "40-hex"

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

# The output map and the pack/release steps are part of the guard: a green
# compute with a remapped package_version still publishes the wrong package.
if python3 - "$WORKFLOW" "$DAILY" <<'PY' >"$TMP/wiring.log" 2>&1
import sys, io, yaml
release = yaml.safe_load(io.open(sys.argv[1], encoding='utf-8'))
daily = yaml.safe_load(io.open(sys.argv[2], encoding='utf-8'))

pre = release['jobs']['preflight']
expected = {
    'package_version': '${{ steps.compute.outputs.package_version }}',
    'base_version': '${{ steps.compute.outputs.base_version }}',
    'upstream_commit': '${{ steps.pin.outputs.upstream_commit }}',
    'upstream_version': '${{ steps.verify.outputs.upstream_version }}',
    'is_dry_run': '${{ steps.compute.outputs.is_dry_run }}',
    'is_prerelease': '${{ steps.compute.outputs.is_prerelease }}',
    'is_respin': '${{ steps.compute.outputs.is_respin }}',
}
for key, value in expected.items():
    assert pre['outputs'][key] == value, (key, pre['outputs'][key])
assert '-ci.' in pre['if']
assert pre['steps'][0]['with']['fetch-depth'] == 0
for name in ('Guard 3', 'Guard 5'):
    step = next(s for s in pre['steps'] if name in s.get('name', ''))
    assert "is_dry_run == 'false'" in step['if'], name

pack = next(s for s in release['jobs']['pack']['steps'] if s.get('name') == 'Pack')
assert pack['env']['PKG_VERSION'] == '${{ needs.preflight.outputs.package_version }}'
assert '-p:Version="$PKG_VERSION"' in pack['run']
assert '${{' not in pack['run']
assert any(s.get('name') == 'Assert packed version' for s in release['jobs']['pack']['steps'])
assert 'validate-pack' in release['jobs']['publish']['needs']
assert any(s.get('name') == 'Verify version is unpublished' for s in release['jobs']['publish']['steps'])

notes = next(s for s in release['jobs']['publish']['steps'] if s.get('name') == 'Create GitHub Release')
assert notes['env']['PKG_VERSION'] == '${{ needs.preflight.outputs.package_version }}'
assert notes['env']['UPSTREAM_VERSION'] == '${{ needs.preflight.outputs.upstream_version }}'
assert '${{' not in notes['run']

pub = daily['jobs']['publish']
assert 'NUGET_API_KEY' not in pub.get('env', {})
assert any(s.get('id') == 'nuget' for s in pub['steps'])
compute = next(s for s in pub['steps'] if s.get('name') == 'Compute version')
assert compute['env']['NEW_COMMIT'] == '${{ needs.check-upstream.outputs.new_commit }}'
assert '${{' not in compute['run']
assert 'g${NEW_COMMIT:0:8}' in compute['run']
assert 'check-nuget-version.py nightly' in compute['run']
release_step = next(s for s in pub['steps'] if s.get('name') == 'Create GitHub Release')
assert release_step['env']['PKG_VERSION'] == '${{ steps.version.outputs.package_version }}'
assert release_step['env']['UPSTREAM_VERSION'] == '${{ needs.check-upstream.outputs.new_version }}'
assert '--prerelease' in release_step['run']
assert '--target "$PIN_COMMIT"' in release_step['run']
assert '${{' not in release_step['run']

names = [s.get('name') for s in pub['steps']]
assert names.index('Prepare pin update') < names.index('Publish to NuGet') < names.index('Push pin update') < names.index('Create GitHub Release')
publish_step = next(s for s in pub['steps'] if s.get('name') == 'Publish to NuGet')
assert publish_step['env']['NUGET_API_KEY'] == '${{ secrets.NUGET_API_KEY }}'
PY
then ok "release and nightly output/pack/release wiring is exact"; else bad "release and nightly output/pack/release wiring is exact"; head -5 "$TMP/wiring.log"; fi

echo
echo "$pass passed, $fail failed"
[ "$fail" -eq 0 ]
