# Changelog

All notable changes to `DeBlasis.GhosttyVt`.

Versioning follows the strict-mirror rule in [RELEASING.md](RELEASING.md): a
package version names the upstream Ghostty version it binds. Interim
prereleases (`X.Y.Z-rc.N`) may ship ahead of that upstream release; see
[RELEASING.md](RELEASING.md#interim-prereleases).

## Unreleased

Vendored upstream moved from `c41c6b81` (2026-07-07, Ghostty `1.3.2-dev`) to
`349f0260` (2026-09-02, Ghostty `1.3.2-dev`). The daily sync had been unable to
build since 2026-07-08, so this is roughly two months of upstream change landed
as one reviewed step.

### Breaking

These are source- and binary-breaking for anyone consuming the package. All of
them are corrections: the previous values did not describe any libghostty API.

- **`TerminalMode` values are now packed mode identifiers.** libghostty encodes
  a mode as 15 bits of value plus an ANSI flag in bit 15; the enum previously
  stored bare numbers, so members that should have been ANSI modes selected the
  DEC private mode of the same number.
  - `Insert` `4` → `32772`. The bare `4` was DEC private "slow scroll", not
    ANSI 4 (IRM), despite a comment in `examples/Modes` asserting it was IRM.
  - `FocusEvent` `1007` → `1004`. `1007` is alternate scroll; focus reporting
    is `1004`.
  - `KittyKeyboard` (`2015`) **removed**. No such mode exists at any upstream
    version. Kitty keyboard state is read through
    `TerminalData.KittyKeyboardFlags`.
  - The enum now covers all 43 `GHOSTTY_MODE_*` macros. Every previously
    published member keeps its name.
- **`TerminalOption` rewritten from the header.** Its members matched no
  libghostty API at any version (`FontName = 0`, `ClipboardWrite = 14`, where
  upstream has `USERDATA = 0`, `COLOR_PALETTE = 14`). It had no callers, which
  is why it survived; it is now used by every `ghostty_terminal_set` call site.
- **`Terminal.MaxScrollbackLines` is `long?`**, was effectively unavailable.
  `null` means "unlimited", which upstream signals with `GHOSTTY_NO_VALUE`.
  A previous draft returned `int` and would have reported `0` for unlimited —
  the opposite meaning.
- **`Terminal.ModeGet` and `ModeSet` now throw `GhosttyException`** on an
  unrecognised mode. They previously discarded the native result code, so a get
  returned `false` and a set silently did nothing.

### Added

- `TerminalOptions.MaxScrollbackLines` (default `1000`, unchanged behaviour) —
  max scrollback moved out of the removed `GhosttyTerminalOptions` construction
  struct into a post-construction `ghostty_terminal_set`.
- `TerminalData` extended to 41 members, `TerminalOption` to 40,
  `RenderStateData` to 20 (`Cursor`, `Colors`).
- Interim prerelease support in the release workflow, with
  `build/selftest-release-guards.sh` covering it.

### Fixed

- **`ghostty_terminal_new` ABI break.** Upstream dropped the by-value
  `GhosttyTerminalOptions` struct; cols and rows are scalar parameters now. The
  symbol name did not change, so this linked cleanly and was wrong only at
  runtime: on SysV the 16-byte struct arrived in two integer registers, so the
  new `rows` parameter read the struct's `max_scrollback` field — which is why
  the suite reported 1000 rows. On Windows x64 the same struct passes by
  reference, so `cols` would be the low 16 bits of a pointer and `rows`
  uninitialised.
- `ghostty_terminal_mode_get` / `_set` and `ghostty_render_state_colors_get`
  were removed upstream and folded into the generic accessors.
- The binding-gate self-test was calibrated to the export count at the pin it
  was written at, so a larger upstream made the case describing *correct*
  behaviour fail. Both counts now derive from the headers.

### Known issues

- `ghostty_snapshot_*` (terminal snapshot encode/decode, added upstream
  2026-08-03) is not yet bound.
- Several hand-transcribed constants elsewhere in the binding still describe no
  libghostty API — see
  [#50](https://github.com/deblasis/libghostty-vt-dotnet/issues/50).
