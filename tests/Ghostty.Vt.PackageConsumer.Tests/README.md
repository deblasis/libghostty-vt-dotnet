# Ghostty.Vt.PackageConsumer.Tests

Validates the packed `DeBlasis.GhosttyVt` NuGet package the way a real consumer
sees it: `<PackageReference>` against a local feed, with the floating `*-*`
version resolving to whatever `just pack` produced.

**This project is deliberately not part of `Ghostty.Vt.sln`.** Every flow that
runs `dotnet test Ghostty.Vt.sln` (inner loop, CI push, release pack) does so
*before* `dotnet pack` — so including it in the solution would try to restore a
nupkg that doesn't exist yet. Invoke explicitly by project path.

## Run locally

```bash
just pack
just validate-pack
```

## If you see odd restore behaviour

`artifacts/` may contain multiple `DeBlasis.GhosttyVt.*.nupkg` from prior runs.
The floating `*-*` version picks the highest SemVer; if two runs happen to
share a version string, results are ambiguous.

Clean slate:

```bash
rm -rf artifacts/ packages/
just pack
just validate-pack
```

## What each test covers

| File | Targets |
|------|---------|
| `BuildInfoTests.cs` | `BuildInfo.Query()` — **canary** for native lib load |
| `TerminalTests.cs` | `Terminal.VTWrite` + `RenderState` grid read |
| `FormatterTests.cs` | `Formatter` plain-text + HTML output |
| `EncoderTests.cs` | `KeyEncoder` + `MouseEncoder` |
| `ParserTests.cs` | `SgrParser` + `OscParser` |
| `LifetimeTests.cs` | Construct / dispose / reconstruct without native crash |
