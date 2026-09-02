#!/usr/bin/env bash
#
# Shared symbol extraction, sourced by build/generate-bindings.sh and
# build/check-binding-symbols.sh so the two can never drift apart. It is only
# a function library; sourcing it has no side effects.

# ghostty_exported_symbols <include-dir>
#
# Prints, one per line, the names of the functions a ghostty include tree
# exports.
#
# Only real declarations: the name immediately before the opening paren of a
# GHOSTTY_API line. Matching a bare "ghostty_x(" anywhere would also pick up
# prose in doc comments, which routinely name replacement functions and would
# therefore mask exactly the removals this is used to find. Taking the *last*
# ghostty_ token before the paren also handles declarations whose return type
# is itself snake_case (e.g. "GHOSTTY_API ghostty_info_s ghostty_info(").
ghostty_exported_symbols() {
  local include=$1
  find "$include" -name '*.h' -exec cat {} + 2>/dev/null \
    | grep -hoE '^GHOSTTY_API[^(]*\(' \
    | sed -E 's/.*[^A-Za-z0-9_](ghostty_[a-z0-9_]+)[[:space:]]*\($/\1/' \
    | grep -E '^ghostty_[a-z0-9_]+$' \
    | sort -u || true
}

# ghostty_declared_entry_points <bindings.cs>
#
# Prints the native entry points a C# bindings file declares. LibraryImport
# and DllImport both default the entry point to the method name; callers must
# separately verify that no declaration overrides it with EntryPoint=, or this
# under-reports (see check-binding-symbols.sh).
ghostty_declared_entry_points() {
  local bindings=$1
  grep -E 'static (extern|partial)' "$bindings" 2>/dev/null \
    | grep -oE 'ghostty_[a-z0-9_]+' \
    | sort -u || true
}

# ghostty_interop_attribute_count <bindings.cs>
#
# Number of [LibraryImport]/[DllImport] attributes: exactly one per bound
# function, whatever the declaration style.
ghostty_interop_attribute_count() {
  local bindings=$1
  grep -cE '^[[:space:]]*\[(LibraryImport|DllImport)' "$bindings" 2>/dev/null || true
}
