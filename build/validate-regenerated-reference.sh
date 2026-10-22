#!/usr/bin/env bash
#
# Validate the regenerated binding artifact before it can become
# build/bindings-reference/NativeMethods.pinned.cs.
#
# The artifact is produced by the regenerate-bindings job in the same workflow
# run. Its metadata ties it to the upstream commit the pin is about to move to,
# so a re-run that resolves a newer HEAD but reuses an older artifact fails
# closed instead of committing a reference from the wrong commit.
#
# Usage: build/validate-regenerated-reference.sh <artifact-dir> <upstream-commit> [--stage]
#
# --stage copies the validated file over the pinned reference. The caller owns
# the git add/commit; this script never commits or pushes.
set -euo pipefail

dir=${1:?artifact directory}
commit=${2:?upstream commit}
stage=${3:-}

file="$dir/NativeMethods.cs"
meta="$dir/metadata.txt"

test -s "$file" || {
  echo "::error::missing or empty $file" >&2
  exit 1
}

test -f "$meta" || {
  echo "::error::missing $meta; refusing an artifact with unknown provenance" >&2
  exit 1
}

if ! grep -qx "upstream_commit=$commit" "$meta"; then
  echo "::error::artifact metadata does not match the pin target $commit:" >&2
  cat "$meta" >&2
  exit 1
fi

if ! grep -q 'partial class NativeMethods' "$file"; then
  echo "::error::$file does not look like the generated P/Invoke surface" >&2
  exit 1
fi

count=$(grep -c 'static extern' "$file" || true)
if [ "$count" -lt 100 ]; then
  echo "::error::$file has only $count extern declarations; refusing a degenerate artifact" >&2
  exit 1
fi

if [ "$stage" = "--stage" ]; then
  cp "$file" build/bindings-reference/NativeMethods.pinned.cs
  if git diff --quiet -- build/bindings-reference/NativeMethods.pinned.cs; then
    echo "reference_changed=false" >> "${GITHUB_OUTPUT:-/dev/null}"
  else
    echo "reference_changed=true" >> "${GITHUB_OUTPUT:-/dev/null}"
  fi
fi

echo "Validated $count declarations for upstream $commit"
