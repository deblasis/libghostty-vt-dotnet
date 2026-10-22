#!/usr/bin/env bash
# Read and validate the top-level .version from a Zig build.zig.zon on stdin.
#
# This deliberately does not scan the whole file for the first `.version = "`
# substring: a comment or nested field could supply a decoy. It also validates
# the value before any workflow interpolates it, because the nightly publish
# job has repository write access and a NuGet key.
set -euo pipefail

VERSION=$(sed -nE 's/^[[:space:]]*\.version[[:space:]]*=[[:space:]]*"([^"]+)".*/\1/p' | head -1)
if [ -z "$VERSION" ]; then
  echo "::error::Could not parse top-level .version from build.zig.zon" >&2
  exit 1
fi

if ! [[ "$VERSION" =~ ^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$ ]]; then
  echo "::error::Upstream version is not a valid SemVer string: '$VERSION'" >&2
  exit 1
fi

printf '%s\n' "$VERSION"
