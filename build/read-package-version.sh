#!/usr/bin/env bash
# Read the package version from version.json.
#
# The package version is independent of upstream Ghostty. The value must be a
# plain canonical X.Y.Z; channel suffixes belong in the tag, not this file.
# `jq -e` plus an anchored bash regex rejects a multiline or non-string value
# instead of matching one line of it.
set -euo pipefail

if [ ! -f version.json ]; then
  echo "::error::version.json not found" >&2
  exit 1
fi

if ! VERSION=$(jq -er '.version | select(type == "string")' version.json 2>/dev/null); then
  echo "::error::version.json must contain a string version" >&2
  exit 1
fi

if ! [[ "$VERSION" =~ ^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$ ]]; then
  echo "::error::version.json must contain a plain canonical X.Y.Z version; got '$VERSION'" >&2
  exit 1
fi

printf '%s\n' "$VERSION"
