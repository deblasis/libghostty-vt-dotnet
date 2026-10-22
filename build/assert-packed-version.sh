#!/usr/bin/env bash
# Assert that the packed nupkg carries the version the workflow guarded.
# NuGet normalizes some version strings (for example 1.4.0.0 -> 1.4.0), so a
# matching filename is not enough.
set -euo pipefail

NUPKG=${1:?usage: assert-packed-version.sh <nupkg> <expected-version>}
EXPECTED=${2:?usage: assert-packed-version.sh <nupkg> <expected-version>}

if [ ! -f "$NUPKG" ]; then
  echo "::error::Package not found: $NUPKG" >&2
  exit 1
fi

PACKED=$(unzip -p "$NUPKG" '*.nuspec' \
  | grep -oE '<version>[^<]+</version>' \
  | head -1 \
  | sed -E 's:^<version>::; s:</version>$::')
if [ -z "$PACKED" ]; then
  echo "::error::Could not read <version> from $NUPKG" >&2
  exit 1
fi

if [ "$PACKED" != "$EXPECTED" ]; then
  echo "::error::Packed version '$PACKED' does not match guarded version '$EXPECTED'" >&2
  exit 1
fi

echo "Packed version: $PACKED"
