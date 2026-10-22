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
extract preflight "Resolve upstream pin" "$TMP/pin.sh" || exit 1
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

extract_daily_job() { # extract_daily_job <job> <needle> <outfile>
  python3 - "$DAILY" "$1" "$2" "$3" <<'PY'
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

extract preflight "Guard 3" "$TMP/guard3.sh" || exit 1
extract preflight "Guard 5" "$TMP/guard5.sh" || exit 1
extract publish "Verify version is unpublished" "$TMP/unpublished.sh" || exit 1
extract_daily_job publish "Prepare pin update" "$TMP/prepare-pin.sh" || exit 1
extract_daily_job check-upstream "Check for upstream changes" "$TMP/check-upstream.sh" || exit 1

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

echo "== upstream pin =="

run_pin() { # run_pin <is_dry_run> <pinned_commit> <dry_run_commit>
  mkdir -p "$TMP/pin-work"
  printf '{"commit":"%s"}\n' "$2" > "$TMP/pin-work/ghostty-upstream.json"
  : > "$TMP/out-pin.env"
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  ( cd "$TMP/pin-work" && GITHUB_OUTPUT="$TMP/out-pin.env" \
      IS_DRY_RUN="$1" DRY_RUN_COMMIT="$3" \
      bash --noprofile --norc -e "$TMP/pin.sh" ) >"$LAST_LOG" 2>&1
  return $?
}

out_pin_val() { grep -E "^$1=" "$TMP/out-pin.env" | tail -1 | cut -d= -f2-; }

run_pin false 0123456789abcdef0123456789abcdef01234567 ""
check "full pin SHA accepted" 0 $?
[ "$(out_pin_val upstream_commit)" = "0123456789abcdef0123456789abcdef01234567" ] && ok "pin SHA emitted" || bad "pin SHA emitted"

run_pin false deadbeef ""
check_refusal "short pin SHA refused" $? "40-hex"
run_pin false '$(touch /tmp/pin-decoy)' ""
check_refusal "shell payload in pin refused" $? "40-hex"

run_pin true ignored 0123456789abcdef0123456789abcdef01234567
check "dry-run commit accepted" 0 $?
[ "$(out_pin_val upstream_commit)" = "0123456789abcdef0123456789abcdef01234567" ] && ok "dry-run commit emitted" || bad "dry-run commit emitted"
run_pin true ignored not-a-sha
check_refusal "invalid dry-run commit refused" $? "40-hex"

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
  printf '%s' "$1" | python3 "$REPO/build/read-upstream-version.py" >"$LAST_LOG" 2>&1
  return $?
}

run_upstream '.{
    .version = "1.3.2-dev",
}'
check "upstream -dev version accepted" 0 $?
[ "$(cat "$TMP/last.log")" = "1.3.2-dev" ] && ok "reader prints the upstream version" || bad "reader prints the upstream version"

run_upstream '.{
    // .version = "9.9.9"
    .version = "1.3.2-dev",
}'
check "commented decoy is ignored" 0 $?
[ "$(cat "$TMP/last.log")" = "1.3.2-dev" ] && ok "top-level version wins over a comment" || bad "top-level version wins over a comment"

run_upstream 'const version = "1.3.2";'
check_refusal "missing top-level version refused" $? "Could not parse"
run_upstream '.{
    .version = "$(touch /tmp/decoy)",
}'
check_refusal "shell payload in upstream version refused" $? "valid SemVer"

run_upstream '.{
    .dependencies = .{
        .foo = .{
            .version = "9.9.9",
        },
    },
    .version = "1.3.2-dev",
}'
check "nested version decoy is ignored" 0 $?
[ "$(cat "$TMP/last.log")" = "1.3.2-dev" ] && ok "top-level version wins over a nested field" || bad "top-level version wins over a nested field"

ZON_CHAR=$(cat <<'EOF'
.{
    .foo = .{
        .x = '}',
        .version = "9.9.9",
    },
    .version = "1.3.2-dev",
}
EOF
)
run_upstream "$ZON_CHAR"
check "brace in a char literal does not shift depth" 0 $?
[ "$(cat "$TMP/last.log")" = "1.3.2-dev" ] && ok "char literal decoy ignored" || bad "char literal decoy ignored"

ZON_MULTILINE=$(cat <<'EOF'
.{
    .paths = .{
        \\}
    },
    .dependencies = .{
        .version = "9.9.9",
    },
    .version = "1.3.2-dev",
}
EOF
)
run_upstream "$ZON_MULTILINE"
check "multiline string content does not shift depth" 0 $?
[ "$(cat "$TMP/last.log")" = "1.3.2-dev" ] && ok "multiline string decoy ignored" || bad "multiline string decoy ignored"

echo "== NuGet ordering =="

mkdir -p "$TMP/nuget"
printf '{"versions":["1.4.0-rc.1","1.4.0-rc.2"]}' > "$TMP/nuget/rc.json"
printf '{"versions":["1.4.0-rc.1","1.4.0-rc.2","1.4.0"]}' > "$TMP/nuget/stable.json"
printf '{"versions":["1.4.0","1.4.0.1"]}' > "$TMP/nuget/respin.json"
printf '{"versions":["1.4.0","1.4.1"]}' > "$TMP/nuget/next.json"
printf '{"versions":["1.4.0-RC.2"]}' > "$TMP/nuget/case.json"
printf '{"versions":["1.4.0-rc.01"]}' > "$TMP/nuget/zero.json"
printf '{"versions":["1.3.2","1.4.1-rc.1"]}' > "$TMP/nuget/higher-rc.json"

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
run_nuget release --version 1.4.0.1 --base 1.4.0 --index-file "$TMP/nuget/next.json"
check_refusal "respin of an older stable refused" $? "not the latest stable"
run_nuget release --version 1.4.0-rc.1 --base 1.4.0 --index-file "$TMP/nuget/case.json"
check_refusal "case-insensitive RC ordering enforced" $? "not ahead"
run_nuget release --version 1.4.0-rc.1 --base 1.4.0 --index-file "$TMP/nuget/zero.json"
check_refusal "leading-zero RC ordering enforced" $? "not ahead"
run_nuget release --version 1.4.0 --base 1.4.0 --index-file "$TMP/nuget/higher-rc.json"
check "stable above a higher RC is allowed" 0 $?

if python3 - "$REPO/build/check-nuget-version.py" <<'PY'
import importlib.util, sys, urllib.error
spec = importlib.util.spec_from_file_location("check_nuget_version", sys.argv[1])
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

def missing(*args, **kwargs):
    raise urllib.error.HTTPError("https://example.invalid", 404, "Not Found", {}, None)

module.urllib.request.urlopen = missing
try:
    module.load_versions(None, "https://example.invalid")
except ValueError as error:
    if "404" in str(error):
        sys.exit(0)
sys.exit(1)
PY
then ok "NuGet 404 fails closed"; else bad "NuGet 404 fails closed"; fi

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

echo "== guard 3 =="

assert_fully_substituted "$TMP/guard3.sh" "Guard 3"
mkdir -p "$TMP/guard3-bin"
cat > "$TMP/guard3-bin/python3" <<SH
#!/usr/bin/env bash
case " \$* " in
  *check-nuget-version.py*) exit 1 ;;
esac
exec "$REAL_PYTHON" "\$@"
SH
chmod +x "$TMP/guard3-bin/python3"

run_guard3() {
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  ( PATH="$TMP/guard3-bin:$PATH" PKG_VERSION=1.4.0 BASE_VERSION=1.4.0 \
      bash --noprofile --norc -e "$TMP/guard3.sh" ) >"$LAST_LOG" 2>&1
  return $?
}
run_guard3
check "Guard 3 failure propagates" 1 $?

echo "== guard 5 =="

assert_fully_substituted "$TMP/guard5.sh" "Guard 5"
git init --bare -q "$TMP/guard5-origin.git"
git clone -q "$TMP/guard5-origin.git" "$TMP/guard5-work"
git -C "$TMP/guard5-work" config user.name selftest
git -C "$TMP/guard5-work" config user.email selftest@example.invalid
printf 'A\n' > "$TMP/guard5-work/marker"
git -C "$TMP/guard5-work" add marker
git -C "$TMP/guard5-work" commit -q -m A
git -C "$TMP/guard5-work" branch -M main
git -C "$TMP/guard5-work" tag -a v1.0.0 -m v1.0.0
git -C "$TMP/guard5-work" push -q -u origin main
git -C "$TMP/guard5-work" push -q origin v1.0.0
A_SHA=$(git -C "$TMP/guard5-work" rev-parse HEAD)
git -C "$TMP/guard5-work" checkout -q -b side
printf 'B\n' > "$TMP/guard5-work/marker"
git -C "$TMP/guard5-work" add marker
git -C "$TMP/guard5-work" commit -q -m B
git -C "$TMP/guard5-work" tag -a v1.0.1 -m v1.0.1
git -C "$TMP/guard5-work" push -q origin side
git -C "$TMP/guard5-work" push -q origin v1.0.1
B_SHA=$(git -C "$TMP/guard5-work" rev-parse HEAD)
git -C "$TMP/guard5-work" checkout -q main
printf 'C\n' > "$TMP/guard5-work/marker"
git -C "$TMP/guard5-work" add marker
git -C "$TMP/guard5-work" commit -q -m C
git -C "$TMP/guard5-work" push -q

run_guard5() { # run_guard5 <sha> <tag>
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  ( cd "$TMP/guard5-work" && GITHUB_SHA="$1" TAG="$2" \
      bash --noprofile --norc -e "$TMP/guard5.sh" ) >"$LAST_LOG" 2>&1
  return $?
}
run_guard5 "$A_SHA" v1.0.0
check "tag on main accepted" 0 $?
run_guard5 "$B_SHA" v1.0.1
check_refusal "side-branch tag refused" $? "not on origin/main"
run_guard5 "$A_SHA" v1.0.1
check_refusal "moved tag refused" $? "has moved"

echo "== unpublished check =="

assert_fully_substituted "$TMP/unpublished.sh" "Verify version is unpublished"
mkdir -p "$TMP/curl-bin"
cat > "$TMP/curl-bin/curl" <<'SH'
#!/usr/bin/env bash
printf '%s' "${FAKE_HTTP:-404}"
SH
chmod +x "$TMP/curl-bin/curl"

run_unpublished() { # run_unpublished <http-status>
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  ( PATH="$TMP/curl-bin:$PATH" FAKE_HTTP="$1" PKG_VERSION=1.4.0 \
      bash --noprofile --norc -e "$TMP/unpublished.sh" ) >"$LAST_LOG" 2>&1
  return $?
}
run_unpublished 404
check "unpublished version accepted" 0 $?
run_unpublished 200
check_refusal "published version refused" $? "already published"
run_unpublished 503
check_refusal "unknown registry state refused" $? "Could not confirm"

echo "== pin commit preparation =="

assert_fully_substituted "$TMP/prepare-pin.sh" "Prepare pin update"
mkdir -p "$TMP/pin-repo"
git -C "$TMP/pin-repo" init -q
git -C "$TMP/pin-repo" config user.name selftest
git -C "$TMP/pin-repo" config user.email selftest@example.invalid
printf '{"repo":"https://github.com/ghostty-org/ghostty.git","commit":"0000000000000000000000000000000000000000"}\n' > "$TMP/pin-repo/ghostty-upstream.json"
mkdir -p "$TMP/pin-repo/build/bindings-reference" "$TMP/pin-repo/src/Ghostty.Vt/Raw"
printf 'old\n' > "$TMP/pin-repo/build/bindings-reference/NativeMethods.pinned.cs"
printf 'old\n' > "$TMP/pin-repo/src/Ghostty.Vt/Raw/GhosttyVtNative.g.cs"
git -C "$TMP/pin-repo" add ghostty-upstream.json \
  build/bindings-reference/NativeMethods.pinned.cs \
  src/Ghostty.Vt/Raw/GhosttyVtNative.g.cs
git -C "$TMP/pin-repo" commit -q -m initial
printf 'new\n' > "$TMP/pin-repo/build/bindings-reference/NativeMethods.pinned.cs"
printf 'new\n' > "$TMP/pin-repo/src/Ghostty.Vt/Raw/GhosttyVtNative.g.cs"
PREV_HEAD=$(git -C "$TMP/pin-repo" rev-parse HEAD)

out_pin_prepare() { grep -E "^$1=" "$TMP/out-pin-prepare.env" | tail -1 | cut -d= -f2-; }
run_prepare_pin() { # run_prepare_pin <version>
  : > "$TMP/out-pin-prepare.env"
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  ( cd "$TMP/pin-repo" && GITHUB_OUTPUT="$TMP/out-pin-prepare.env" \
      NEW_COMMIT=0123456789abcdef0123456789abcdef01234567 \
      NEW_VERSION="$1" bash --noprofile --norc -e "$TMP/prepare-pin.sh" ) >"$LAST_LOG" 2>&1
  return $?
}
PAYLOAD='1.3.2$(touch '"$TMP"'/pin-inject)'
run_prepare_pin "$PAYLOAD"
check "pin commit prepared" 0 $?
[ ! -e "$TMP/pin-inject" ] && ok "pin version is not shell-expanded" || bad "pin version is not shell-expanded"
[ "$(jq -r '.upstreamVersion' "$TMP/pin-repo/ghostty-upstream.json")" = "$PAYLOAD" ]   && ok "pin version is stored literally" || bad "pin version is stored literally"
[ "$(out_pin_prepare pin_commit)" = "$(git -C "$TMP/pin-repo" rev-parse HEAD)" ]   && ok "pin commit SHA emitted" || bad "pin commit SHA emitted"
[ "$(git -C "$TMP/pin-repo" rev-parse HEAD)" != "$PREV_HEAD" ]   && ok "pin commit advances HEAD" || bad "pin commit advances HEAD"
[ "$(git -C "$TMP/pin-repo" show HEAD:ghostty-upstream.json | jq -r '.upstreamVersion')" = "$PAYLOAD" ]   && ok "new pin is in the commit" || bad "new pin is in the commit"
git -C "$TMP/pin-repo" cat-file -e HEAD:build/bindings-reference/NativeMethods.pinned.cs   && git -C "$TMP/pin-repo" cat-file -e HEAD:src/Ghostty.Vt/Raw/GhosttyVtNative.g.cs   && ok "reference and raw layer are in the commit" || bad "reference and raw layer are in the commit"

echo "== upstream check =="

assert_fully_substituted "$TMP/check-upstream.sh" "Check for upstream changes"
mkdir -p "$TMP/check-work/build" "$TMP/check-bin"
cp "$REPO/build/read-upstream-version.py" "$TMP/check-work/build/"
printf '{"commit":"0000000000000000000000000000000000000000"}\n' > "$TMP/check-work/ghostty-upstream.json"
REAL_GIT=$(command -v git)
cat > "$TMP/check-bin/git" <<SH
#!/usr/bin/env bash
if [ "\$1" = "ls-remote" ]; then
  printf '%s\tHEAD\n' "\${FAKE_LATEST:-}"
  exit 0
fi
exec "$REAL_GIT" "\$@"
SH
chmod +x "$TMP/check-bin/git"
cat > "$TMP/check-bin/curl" <<'ZON'
#!/usr/bin/env bash
cat <<'EOF'
.{
    // .version = "9.9.9"
    .dependencies = .{ .foo = .{ .version = "8.8.8", }, },
    .version = "1.3.2-dev",
}
EOF
ZON
chmod +x "$TMP/check-bin/curl"

out_check_upstream() { grep -E "^$1=" "$TMP/out-check.env" | tail -1 | cut -d= -f2-; }
run_check_upstream() { # run_check_upstream <latest>
  : > "$TMP/out-check.env"
  LAST_LOG="$TMP/last.log"; : > "$LAST_LOG"
  ( cd "$TMP/check-work" && PATH="$TMP/check-bin:$PATH" FAKE_LATEST="$1" \
      GITHUB_OUTPUT="$TMP/out-check.env" \
      bash --noprofile --norc -e "$TMP/check-upstream.sh" ) >"$LAST_LOG" 2>&1
  return $?
}
run_check_upstream 0123456789abcdef0123456789abcdef01234567
check "upstream check accepted" 0 $?
[ "$(out_check_upstream commit)" = "0123456789abcdef0123456789abcdef01234567" ]   && ok "upstream commit emitted" || bad "upstream commit emitted"
[ "$(out_check_upstream version)" = "1.3.2-dev" ]   && ok "top-level upstream version emitted" || bad "top-level upstream version emitted"
run_check_upstream not-a-sha
check_refusal "invalid upstream SHA refused" $? "40-hex"

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
assert any('Guard 4' in s.get('name', '') for s in pre['steps'])

compute_step = next(s for s in pre['steps'] if s.get('name') == 'Compute version and dry-run flag')
assert 'echo "Package version:' in compute_step['run']
assert 'echo "Base version:' in compute_step['run']
pin_step = next(s for s in pre['steps'] if s.get('name') == 'Resolve upstream pin')
assert 'echo "Upstream commit:' in pin_step['run']
guard2_step = next(s for s in pre['steps'] if 'Guard 2' in s.get('name', ''))
assert 'echo "Upstream version:' in guard2_step['run']

pack = next(s for s in release['jobs']['pack']['steps'] if s.get('name') == 'Pack')
assert pack['env']['PKG_VERSION'] == '${{ needs.preflight.outputs.package_version }}'
assert '-p:Version="$PKG_VERSION"' in pack['run']
assert '${{' not in pack['run']
assert any(s.get('name') == 'Assert packed version' for s in release['jobs']['pack']['steps'])
assert 'validate-pack' in release['jobs']['publish']['needs']

assert release['concurrency']['group'] == 'release'
for job in release['jobs'].values():
    for step in job.get('steps', []):
        if str(step.get('uses', '')).startswith('actions/checkout'):
            assert step.get('with', {}).get('ref') == '${{ github.sha }}', job

guard3 = next(s for s in pre['steps'] if 'Guard 3' in s.get('name', ''))
assert 'check-nuget-version.py release' in guard3['run']
assert '--version "$PKG_VERSION"' in guard3['run']
assert '--base "$BASE_VERSION"' in guard3['run']
assert '|| true' not in guard3['run']
guard5 = next(s for s in pre['steps'] if 'Guard 5' in s.get('name', ''))
assert 'merge-base --is-ancestor' in guard5['run']
assert '^{commit}' in guard5['run']
assert '|| true' not in guard5['run']
recheck_release = next(s for s in release['jobs']['publish']['steps'] if s.get('name') == 'Re-check published version ordering')
assert 'check-nuget-version.py release' in recheck_release['run']
assert '--version "$PKG_VERSION"' in recheck_release['run']
assert '--base "$BASE_VERSION"' in recheck_release['run']
tag_target = next(s for s in release['jobs']['publish']['steps'] if s.get('name') == 'Verify tag target')
assert '^{commit}' in tag_target['run']
assert 'TARGET_SHA' in tag_target['env']
assert 'if [ "$TAG_SHA" != "$TARGET_SHA" ]' in tag_target['run']
assert 'has moved' in tag_target['run']
assert 'git fetch --tags --force' in tag_target['run']
unpublished = next(s for s in release['jobs']['publish']['steps'] if s.get('name') == 'Verify version is unpublished')
assert 'HTTP' in unpublished['run'] and '404' in unpublished['run']
for name in ('Re-check published version ordering', 'Verify tag target', 'Verify version is unpublished'):
    step = next(s for s in release['jobs']['publish']['steps'] if s.get('name') == name)
    assert 'if' not in step, name
release_publish_names = [s.get('name') for s in release['jobs']['publish']['steps']]
assert release_publish_names.index('Re-check published version ordering') < release_publish_names.index('Verify tag target')
assert release_publish_names.index('Verify tag target') < release_publish_names.index('Verify version is unpublished')
assert release_publish_names.index('Verify version is unpublished') < release_publish_names.index('Publish to NuGet')

notes = next(s for s in release['jobs']['publish']['steps'] if s.get('name') == 'Create GitHub Release')
assert notes['env']['PKG_VERSION'] == '${{ needs.preflight.outputs.package_version }}'
assert notes['env']['UPSTREAM_VERSION'] == '${{ needs.preflight.outputs.upstream_version }}'
assert notes['env']['IS_PRERELEASE'] == '${{ needs.preflight.outputs.is_prerelease }}'
assert 'LATEST_FLAG=(--latest)' in notes['run']
assert 'LATEST_FLAG=(--latest=false)' in notes['run']
assert '${{' not in notes['run']

pub = daily['jobs']['publish']
assert 'NUGET_API_KEY' not in pub.get('env', {})
key_step = next(s for s in pub['steps'] if s.get('id') == 'nuget')
assert key_step['if'] == "github.ref == 'refs/heads/main'"
for name in ('Prepare pin update', 'Publish to NuGet', 'Push pin update', 'Create GitHub Release'):
    step = next(s for s in pub['steps'] if s.get('name') == name)
    assert "github.ref == 'refs/heads/main'" in step['if'], name
    assert '||' not in step['if'], name
for name in ('Publish to NuGet', 'Push pin update', 'Create GitHub Release'):
    step = next(s for s in pub['steps'] if s.get('name') == name)
    assert 'GITHUB_REF' in step['run'], name
recheck = next(s for s in pub['steps'] if s.get('name') == 'Re-check nightly ordering')
assert 'check-nuget-version.py nightly' in recheck['run']
assert 'BASE="${PKG_VERSION%%-ci.*}"' in recheck['run']
assert '--version "$BASE"' in recheck['run']
assert '|| true' not in recheck['run']
pub_names = [s.get('name') for s in pub['steps']]
assert pub_names.index('Re-check nightly ordering') < pub_names.index('Publish to NuGet')
assert '--version "$VERSION_BASE"' in next(s for s in pub['steps'] if s.get('name') == 'Compute version')['run']

check_upstream = next(s for s in daily['jobs']['check-upstream']['steps'] if s.get('name') == 'Check for upstream changes')
assert 'read-upstream-version.py' in check_upstream['run']
assert '[0-9a-f]{40}' in check_upstream['run']
assert 'grep -oP' not in check_upstream['run']

prepare = next(s for s in pub['steps'] if s.get('name') == 'Prepare pin update')
assert 'jq -n' in prepare['run'] and '--arg' in prepare['run']
assert 'cat > ghostty-upstream.json <<' not in prepare['run']
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
assert names.index('Prepare pin update') < names.index('Build') < names.index('Pack')
assert names.index('Pack') < names.index('Publish to NuGet') < names.index('Push pin update') < names.index('Create GitHub Release')
publish_step = next(s for s in pub['steps'] if s.get('name') == 'Publish to NuGet')
assert publish_step['env']['NUGET_API_KEY'] == '${{ secrets.NUGET_API_KEY }}'
assert '--prerelease' in release_step['run']
assert '--latest=false' in release_step['run']
assert '--target "$PIN_COMMIT"' in release_step['run']
PY
then ok "release and nightly output/pack/release wiring is exact"; else bad "release and nightly output/pack/release wiring is exact"; head -5 "$TMP/wiring.log"; fi

echo
echo "$pass passed, $fail failed"
[ "$fail" -eq 0 ]
