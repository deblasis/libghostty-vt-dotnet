# Changelog

All notable changes to `DeBlasis.GhosttyVt`.

Versioning follows independent SemVer. `version.json` is the source of truth.
Upstream tracking is recorded in `ghostty-upstream.json` and the release notes;
the package version does not mirror the upstream version.

## Unreleased

### Breaking

- Curated `Style` and `StyleColor` now match the frozen native layouts.
  `Style.Bold`, `Italic`, `Faint`, `Blink`, `Inverse`, `Invisible`,
  `Strikethrough`, `Overline`, `Style.Underline`, and `StyleColor.Tag` changed
  from fields to properties; `Style.Size` was removed. `Style` is 16 bytes and
  `StyleColor` is 4 bytes. Compiled consumers must be rebuilt.
- Raw: `GhosttyFormatterTerminalOptions`, `GhosttyFormatterTerminalExtra`, and
  `GhosttyFormatterScreenExtra` were removed. `ghostty_formatter_terminal_new`
  no longer takes options; use `ghostty_formatter_set` and
  `ghostty_formatter_get`. `GhosttyStyle` is now a 16-byte
  `{ fg, bg, underline color, ushort flags, ushort reserved }` struct instead of
  per-attribute `_Bool`/`int` fields; read the flags through the
  `GHOSTTY_STYLE_FLAG_*` and `GHOSTTY_STYLE_UNDERLINE_*` macros.
  `GhosttyStyleColor` is 4 bytes with a `byte tag` and a 3-byte union.
  `GhosttyGridRef` and `GhosttySelection` lost their size fields.
  `GhosttyAllocatorVtable` gained a four-pointer `reserved` area.
  `ghostty_size_report_encode` now takes a pointer. Key, mouse, and encoder
  option setters return `GhosttyResult`.
- Raw: `GhosttyKittyPlacementLayer`, `GhosttyTerminalCursorStyle`,
  `GhosttyRenderStateCursorVisualStyle`, and `GhosttyRenderStateImageLayer`
  were removed. Use the `GhosttyImageLayer` masks with
  `GHOSTTY_KITTY_GRAPHICS_PLACEMENT_ITERATOR_OPT_LAYER`, and
  `GhosttyCursorStyle` for cursor visuals. `ghostty_terminal_selection_format_buf`
  and `ghostty_terminal_selection_format_alloc` were removed; format a
  selection with the formatter and `GHOSTTY_FORMATTER_OPT_SELECTION`.
  `ghostty_key_encoder_setopt*` and `ghostty_mouse_encoder_setopt*` are now
  `ghostty_*_encoder_set*`; `ghostty_search_select_next`/`prev` replace the
  removed search selection options; `ghostty_key_event_set_utf8` takes a
  `GhosttyString`; and `ghostty_osc_command_data`, mouse-event getters, and
  key/mouse-event setters return `GhosttyResult`.

### Changed

- Upstream pin moved from `cec8b92f` to `e95bed63` and then to `5813c946`
  (Ghostty `1.4.1-dev`, libghostty-vt `1.1.0-dev`). The curated
  `FormatterOptions` mapping keeps the previous all-off defaults despite the
  new upstream formatter defaults.
- Curated `KittyGraphicsAccessor.SetLayer` now translates its stable layer
  names to the upstream `GhosttyImageLayer` mask, and `SgrParser.SetParameters([])`
  still represents SGR 0 even though upstream requires a non-null parameter
  pointer. Render-state graphemes use the new `GhosttyCodepointBuffer`.
- Added raw coverage for `ghostty_search_select_next` and
  `ghostty_search_select_prev`.
- The curated key/mouse setters, grid-ref lookup, point lookup, and render-state
  style read now throw `GhosttyException` when the native call fails instead of
  returning a zeroed or stale value.
- Windows native builds use the `x86_64-windows-gnu` target, matching the
  upstream release. The packaged DLL no longer imports `VCRUNTIME140.dll`;
  `build/check-windows-abi.py` fails the build if a non-system runtime import
  comes back.
- Windows packages now include `ghostty-vt.pdb` beside `ghostty-vt.dll`. A
  build copies only the native tree for the RID being built, so
  `dotnet publish -r win-x64` copies the x64 DLL and PDB even when the source
  tree also holds `win-arm64`, and a Windows publish fails when the matching
  tree is not staged instead of producing an assembly that cannot load. The PDB
  adds 9.0 MiB unpacked and 2.4 MiB to the
  `.nupkg`. The build checks that the PDB is named by the DLL's CodeView debug
  entry, that it is an MSF 7.00 file, and that its GUID and age match the DLL.
  A second check reads the packed `.nupkg`, enumerates every `runtimes/win-*/`
  tree, rejects an unknown Windows RID or an extra file in a checked tree, and
  rejects the `win` and `any` fallback trees that NuGet's RID graph would
  otherwise resolve for a Windows RID whose own tree is absent. It requires the
  packaged README to name every Windows RID present, and applies
  the same ABI and PDB rules to each pair (it requires `win-arm64` whenever
  that build produced one). The symbol package carries only the managed
  Portable PDB; the native PDBs stay in the main package because nuget.org
  cannot index MSF symbols, and a third check fails the build if a
  `runtimes/` entry reappears in the `.snupkg`. The Windows package-consumer
  test checks that the PDB lands next to the native library the runtime loads.
- Added Windows ARM64 (`win-arm64`) native builds for `aarch64-windows-gnu`,
  with the same ABI check. The package ships
  `runtimes/win-arm64/native/ghostty-vt.dll` when that build succeeds. ARM64
  builds in their own job with a 30-minute timeout, so an ARM64 failure or hang
  still publishes the x64 package and is reported as a follow-up; runtime
  testing on ARM64 is a manual spot check.
- The daily workflow runs the package-consumer tests against the packed
  `.nupkg` on Windows x64 before `publish`. A package whose Windows natives do
  not load no longer reaches the feed.
- The workflows that pack for Windows are checked for the gates they must run.
  `build/check-package-gate-wiring.rb` fails when either chain stops invoking
  `check-nupkg.py`, `check-snupkg.py`, or their self-tests, when a gate step is
  conditional or `continue-on-error`, or when the gate command is piped or
  chained so a failure cannot reach the step. `ci.yml` runs it.
- `build/select-nupkg.py` matches the version `dotnet pack` writes, which has
  build metadata removed, so a version such as `1.2.3+abc` selects
  `…1.2.3.nupkg` instead of reporting the package as missing.
- Added raw coverage for `ghostty_sized_init`, `ghostty_version`,
  `ghostty_formatter_set`, and `ghostty_formatter_get`. The raw surface is now
  209 entry points.

### Fixed

- `OscParser.CommandData` no longer corrupts the caller's stack. The native
  `ghostty_osc_command_data` out parameter is a `GhosttyString` (pointer plus
  length); the wrapper passed an 8-byte pointer local, so the native side
  wrote 16 bytes over it. Reading a window title crashed the process on
  Windows x64.

## 1.4.1 (2026-10-22)

- The curated `Ghostty.Vt` API now forwards to the generated raw ABI layer.
  The 94 hand-written P/Invoke declarations were removed; the public surface is
  unchanged. The formatter now passes `GhosttyFormatterTerminalOptions` by
  value, matching the pinned header.
- Added raw-surface coverage for all 205 entry points. `258` tests pass, and
  `build/check-raw-test-coverage.py` fails if a raw entry point has no test
  reference.
- Corrected `OptimizeMode`, `RowSemanticPrompt`, `TerminalScreen`,
  `KittyPlacementLayer`, and `CursorStyle` to the upstream names and values.
  Old names remain as obsolete aliases where their meaning is preserved.
- Replaced the remaining raw call-site data literals with the generated enum
  constants.
- Added a member-level public API baseline hash. Any public type or member
  change now fails the test suite until the baseline is deliberately updated.
- Added direct mapping tests for every `FormatterOptions` flag, including the
  `Extra*` options, `IncludeStyle`, `Unwrap`, and `Trim`.
- Tightened raw-only kitty-placement assertions and asserted the empty
  render-state image iterator behavior explicitly.
- The raw layer now uses source-generated `LibraryImport` with explicit Cdecl
  calling conventions, making the native surface AOT-compatible. A NativeAOT
  publish-and-run smoke app is part of CI.
- Upstream pin moved to `f54340ad`. `ghostty_terminal_vt_write_until_ground`
  was replaced by `ghostty_terminal_vt_write_until` with explicit boundary and
  stop-result types; the raw layer and tests follow the new API.

## 1.4.0 (2026-10-22)

Vendored upstream moved from `c41c6b81` (2026-07-07, Ghostty `1.3.2-dev`) to
`67edd20a` (2026-10-21, Ghostty `1.3.2-dev`). The daily sync had been unable to
build since 2026-07-08, so this is roughly three months of upstream change
landed as one reviewed step.

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
- **`OscCommandType` and `OscCommandData` now mirror the upstream enums.**
  The previous members were a guessed taxonomy: value `3` was `SetWindowIcon`
  where `GhosttyOscCommandType` defines `SEMANTIC_PROMPT`, and `Unknown = 0`
  where upstream uses `INVALID = 0` with `UNKNOWN = 27`. `OscParser.End()` and
  `CommandType` now return the upstream names (`ChangeWindowTitle`,
  `ChangeWindowIcon`, `SemanticPrompt`, `ReportPwd`, `HyperlinkStart`, ...).
  `OscCommandData` gained the three unknown-sequence selectors and moved to
  `Ghostty.Vt.Enums`. `OscCommandType.Unknown` (27) is only produced when the
  unknown-sequence limit is enabled; the wrapper does not expose that option
  yet, so the API returns `Invalid` for unimplemented sequences. The
  `*_MAX_VALUE` sentinels are not exposed, consistent with the other enums.
  This is source-breaking for `OscCommandType` (enum constants are inlined,
  but names change) and binary-breaking for `OscCommandData` (the type moved
  to `Ghostty.Vt.Enums`).

### Added

- `TerminalOptions.MaxScrollbackLines` (default `1000`, unchanged behaviour) —
  max scrollback moved out of the removed `GhosttyTerminalOptions` construction
  struct into a post-construction `ghostty_terminal_set`.
- `TerminalData` extended to 41 members, `TerminalOption` to 40,
  `RenderStateData` to 20 (`Cursor`, `Colors`).
- **Independent package versioning.** `version.json` is the package version
  source; the upstream Ghostty pin remains in `ghostty-upstream.json` and is
  named in the release notes. Nightlies use `<version>-ci.<timestamp>.g<sha>`,
  release candidates use `<version>-rc.<n>`, and stable releases use
  `<version>`. The release guards enforce canonical tags, `version.json`
  matching, NuGet ordering, packed-version equality, and tag ancestry, with
  `build/selftest-release-guards.sh` covering the refusal paths.
- **Full C ABI under `Ghostty.Vt.Raw`.** `GhosttyVtNative` exposes the 205
  entry points declared in the public headers plus the generated structs,
  enums and constants. The curated `Ghostty.Vt` API is unchanged. The raw
  layer is regenerated with the upstream pin and gated for signatures,
  constants, library, content, public visibility and reproducibility.

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
  2026-08-03) is available through `Ghostty.Vt.Raw` but is not wrapped in the
  curated API.
- The raw `Ghostty.Vt.Raw` layer uses `DllImport`, returns pointers and leaves
  lifetimes to the caller. The curated API forwards to this layer.
- The generated raw layer does not carry per-function XML docs; function-level
  documentation lives in the public C headers.
- Several hand-transcribed constants elsewhere in the binding still describe no
  libghostty API — see
  [#50](https://github.com/deblasis/libghostty-vt-dotnet/issues/50).
