#!/usr/bin/env bash
#
# Self-test for the release workflow's version guards.
#
# The guards decide whether a tag may be published to nuget.org, and a
# nuget.org publish is IRREVERSIBLE -- a wrong version can only be unlisted,
# never replaced. They are also the kind of code whose working state is
# invisible: they exist to refuse, so a guard that has quietly become unable to
# refuse looks exactly like a guard that is passing. Same reasoning as
# selftest-binding-gates.sh.
#
# This does NOT reimplement the guards. It extracts the actual `run:` blocks
# out of .github/workflows/release.yml and executes them with injected inputs,
# because a test that carries its own copy of the logic stops testing the
# thing that ships the moment the two drift.
#
# Usage: build/selftest-release-guards.sh

set -uo pipefail

HERE=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
REPO=$(cd "$HERE/.." && pwd)
WORKFLOW="$REPO/.github/workflows/release.yml"

TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

# The guard parses build.zig.zon with `grep -oP`. Git Bash on Windows ships a
# GNU grep built without PCRE, where that matches nothing and prints nothing.
# Every "must refuse" case below then passes -- for the wrong reason, because
# the guard died before reaching the check under test -- while every "must
# allow" case fails. That is a suite reporting passes while testing nothing,
# so refuse to report rather than mislead. (Observed while writing this:
# 18 passed / 5 failed, and all five failures were the allow cases.)
if ! echo x | grep -oP 'x' >/dev/null 2>&1; then
  echo "::error::This grep has no -P (PCRE) support, which the guard depends on." >&2
  echo "Refusing to report: every refusal case would pass without exercising its check." >&2
  echo "Run this where GNU grep has PCRE (CI's ubuntu-latest does)." >&2
  exit 2
fi

pass=0
fail=0

check() { # check <name> <expected-rc> <actual-rc>
  if [ "$2" -eq "$3" ]; then
    printf '  ok    %-56s (rc=%s)\n' "$1" "$3"; pass=$((pass + 1))
  else
    printf '  FAIL  %-56s (expected rc=%s, got rc=%s)\n' "$1" "$2" "$3"; fail=$((fail + 1))
  fi
}

# Pull a step's `run:` script out of the workflow by step name.
extract() { # extract <job> <step-name-substring> <outfile>
  python3 - "$WORKFLOW" "$1" "$2" "$3" <<'PY'
import sys, io, yaml
wf, job, needle, out = sys.argv[1:5]
d = yaml.safe_load(io.open(wf, encoding='utf-8'))
for st in d['jobs'][job]['steps']:
    if needle.lower() in str(st.get('name', '')).lower() and st.get('run'):
        io.open(out, 'w', encoding='utf-8').write(st['run'])
        sys.exit(0)
sys.stderr.write(f"no step matching {needle!r} in job {job!r}\n")
sys.exit(1)
PY
}

extract preflight "Compute version" "$TMP/compute.sh" || exit 1
extract preflight "Guard 2"         "$TMP/guard2.sh"  || exit 1

# ---------------------------------------------------------------------------
# Guard 1 — tag shape and version decomposition.
#
# The block reads ${{ github.event_name }} and ${{ github.ref_name }} and
# writes to $GITHUB_OUTPUT. Substitute the expressions, point GITHUB_OUTPUT at
# a file, and read back what it decided.
# ---------------------------------------------------------------------------
run_compute() { # run_compute <tag>
  sed -e 's|\${{ github.event_name }}|push|g' \
      -e "s|\\\${{ github.ref_name }}|$1|g" \
      -e 's|\${{ github.run_id }}|0|g' \
      "$TMP/compute.sh" > "$TMP/compute.run.sh"
  : > "$TMP/out.env"
  ( GITHUB_OUTPUT="$TMP/out.env" bash "$TMP/compute.run.sh" ) >/dev/null 2>&1
  return $?
}

out_val() { grep -E "^$1=" "$TMP/out.env" | tail -1 | cut -d= -f2-; }

# A guard that exits non-zero for the wrong reason is indistinguishable from a
# working one, so every refusal asserts WHICH refusal fired. $LAST_ERR holds
# the stderr of the most recent run_guard2 call.
check_refusal() { # check_refusal <name> <actual-rc> <expected-message-substring>
  if [ "$2" -eq 0 ]; then
    printf '  FAIL  %-56s (expected refusal, got rc=0)\n' "$1"; fail=$((fail + 1))
  elif ! grep -qF "$3" "$LAST_ERR"; then
    printf '  FAIL  %-56s (refused, but not for the reason under test)\n' "$1"
    printf '        wanted: %s\n' "$3"
    printf '        got:    %s\n' "$(head -1 "$LAST_ERR")"
    fail=$((fail + 1))
  else
    printf '  ok    %-56s (refused correctly)\n' "$1"; pass=$((pass + 1))
  fi
}

echo "== guard 1: tag shape =="

run_compute "v1.3.2";        check "stable tag accepted"                      0 $?
run_compute "v1.3.2.1";      check "packaging respin accepted"                0 $?
run_compute "v1.4.0-rc.1";   check "interim prerelease accepted"              0 $?
run_compute "v1.4.0-beta.2"; check "any prerelease label accepted"            0 $?
run_compute "v1.2";          check "two-component tag refused"                1 $?
run_compute "vfoo";          check "non-numeric tag refused"                  1 $?
run_compute "1.3.2";         check "tag without v refused"                    1 $?

# The decomposition is what Guard 2 then reasons about, so a wrong base is a
# wrong release rather than a failed one. Assert it explicitly.
echo "== guard 1: version decomposition =="

run_compute "v1.4.0-rc.1"
[ "$(out_val package_version)" = "1.4.0-rc.1" ]; check "prerelease keeps its full version"  0 $?
[ "$(out_val base_version)" = "1.4.0" ];         check "prerelease base strips the suffix"  0 $?
[ "$(out_val is_prerelease)" = "true" ];         check "prerelease flagged"                 0 $?

run_compute "v1.3.2"
[ "$(out_val base_version)" = "1.3.2" ];         check "stable base is the version"         0 $?
[ "$(out_val is_prerelease)" = "false" ];        check "stable not flagged prerelease"      0 $?

run_compute "v1.3.2.4"
[ "$(out_val base_version)" = "1.3.2" ];         check "respin base drops the 4th digit"    0 $?

# ---------------------------------------------------------------------------
# Guard 2 — tag base vs the upstream version at the pinned commit.
#
# The real block curls build.zig.zon from the pinned commit. Stub `curl` on
# PATH so the upstream version is an input rather than a network call.
# ---------------------------------------------------------------------------
mkdir -p "$TMP/bin"
cat > "$TMP/bin/curl" <<'STUB'
#!/usr/bin/env bash
# Enough of build.zig.zon for the guard's grep to find .version.
printf '.{\n    .version = "%s",\n}\n' "$FAKE_UPSTREAM_VERSION"
STUB
chmod +x "$TMP/bin/curl"

run_guard2() { # run_guard2 <base> <is_prerelease> <upstream_version>
  sed -e 's|\${{ steps.pin.outputs.upstream_commit }}|deadbeef|g' \
      -e "s|\\\${{ steps.compute.outputs.base_version }}|$1|g" \
      -e "s|\\\${{ steps.compute.outputs.is_prerelease }}|$2|g" \
      "$TMP/guard2.sh" > "$TMP/guard2.run.sh"
  : > "$TMP/out2.env"
  LAST_ERR="$TMP/guard2.err"
  ( PATH="$TMP/bin:$PATH" FAKE_UPSTREAM_VERSION="$3" GITHUB_OUTPUT="$TMP/out2.env" \
      bash "$TMP/guard2.run.sh" ) >/dev/null 2>"$LAST_ERR"
  return $?
}

echo "== guard 2: stable tags =="

run_guard2 1.3.2 false 1.3.2
check "stable matching a stable upstream is allowed"                 0 $?
run_guard2 1.4.0 false 1.3.2
check_refusal "stable NOT matching upstream is refused" $? "does not match upstream"
run_guard2 1.3.2 false 1.3.2-dev
check_refusal "stable against a -dev upstream is refused" $? "is a -dev build"
run_guard2 1.4.0 false 1.3.2-dev
check_refusal "stable ahead of a -dev upstream is refused" $? "is a -dev build"

echo "== guard 2: interim prereleases =="

# The case this change exists for: ship a validated surface before upstream
# cuts the release it mirrors.
run_guard2 1.4.0 true 1.3.2-dev
check "prerelease AHEAD of a -dev upstream is allowed"               0 $?
run_guard2 1.3.2 true 1.3.2-dev
check "prerelease matching the in-flight version is allowed"         0 $?
run_guard2 1.4.0 true 1.4.0-dev
check "prerelease of the version upstream is building is allowed"    0 $?

# Direction is the one thing still enforced. A prerelease naming a version
# upstream has already shipped would claim to mirror a release whose surface
# is settled and different.
run_guard2 1.2.0 true 1.3.2-dev
check_refusal "prerelease BEHIND upstream is refused" $? "is BEHIND upstream"
run_guard2 1.3.1 true 1.3.2
check_refusal "prerelease behind a stable upstream is refused" $? "is BEHIND upstream"

# Ordering must be numeric, not lexicographic: "1.10.0" > "1.9.0" is false
# under a string compare, which would refuse a legitimate tag.
run_guard2 1.10.0 true 1.9.0-dev
check "version compare is numeric, not lexicographic"                0 $?

echo
echo "$pass passed, $fail failed"
[ "$fail" -eq 0 ]
