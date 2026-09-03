# Releasing DeBlasis.GhosttyVt

This document is for maintainers. Day-to-day users should see the [README](README.md).

## Versioning rule

`DeBlasis.GhosttyVt` uses **strict mirror** versioning against upstream Ghostty:

- Package `1.3.2` corresponds to upstream Ghostty `v1.3.2`, always.
- Packaging-only respins (e.g. a bad `.snupkg`, a missing native artifact) ship as a 4-digit version: `1.3.2.1`, `1.3.2.2`, etc. SemVer permits this.
- There are no independent version bumps. If our .NET surface changes in a breaking way without an upstream bump, we do **not** invent a version of our own — see interim prereleases below.

### Interim prereleases

A tag may carry an `-rc.<n>` suffix — `v1.4.0-rc.1` — to ship a validated
binding surface **before** upstream cuts the release it mirrors.

The label is deliberately restricted to `rc`, not an open alphabet. The daily
sync publishes `<upstream>-ci.<timestamp>.<sha>` to the same package, and SemVer
compares prerelease identifiers alphabetically: `alpha < beta < ci < dev < rc`.
A `v1.3.2-beta.2` would therefore sort *below* every nightly already on
nuget.org and never resolve for a `--prerelease` consumer — the opposite of the
point — and it could not be withdrawn afterwards.

This does not break the mirror; it extends it forward. The tag still names the
upstream version the binding is *for*, and SemVer orders `1.4.0-rc.1` below
`1.4.0`, so the eventual stable supersedes it without a version collision. It
also sorts above every `1.3.2-ci.*` build from the daily stream, so
`--prerelease` consumers get the newer surface.

The guards differ from a stable release in exactly one way: a `-dev` upstream
is expected rather than fatal, because the version being named has not been cut
yet. What is still enforced is **direction** — a prerelease may name the
upstream version in flight or a later one, never an earlier one, which would
claim to mirror a release that has already shipped with a settled, different
surface.

Use this when the binding has changed in a way consumers need and upstream's
next stable has not landed. Prefer waiting when it has not.

**The version you name is a bet.** If you tag `v1.4.0-rc.1` and upstream's next
stable turns out to be `1.3.3`, the prerelease is stranded — nuget.org publishes
cannot be deleted, only unlisted. Name a version you have reason to believe is
coming.

`build/selftest-release-guards.sh` covers both guards and runs in CI. It
executes the workflow's real `run:` blocks rather than a copy of them, and every
refusal case asserts *which* refusal fired — a guard that exits non-zero for an
unrelated reason otherwise looks identical to one that works.

This keeps the story on nuget.org one sentence long: *"install `1.3.2` to get the VT parser from Ghostty `v1.3.2`."*

## Preconditions

Before tagging, `ghostty-upstream.json` must point at an upstream commit whose `build.zig.zon` reports a **non-`-dev`** version matching the tag you intend to push.

Check with:

```bash
jq -r '.upstreamVersion' ghostty-upstream.json
```

If it ends in `-dev` (e.g. `1.3.2-dev`), upstream has not cut a stable yet — wait, or manually merge the upstream-bump PR from the daily sync workflow once one lands.

## One-time setup

Before the first release:

1. Repo → **Settings → Environments → New environment** → `nuget-release`.
2. Under **Deployment protection rules → Required reviewers**, add yourself (`deblasis`).
3. No deployment branch restrictions are needed. Tag refs are not branches, and the workflow's tag-shape guard covers correctness.

The `NUGET_API_KEY` secret is already configured and glob-scoped to `DeBlasis.GhosttyVt*` (covers stable versions).

## Cutting a release

1. **Verify preconditions.**
   ```bash
   jq -r '.upstreamVersion, .commit' ghostty-upstream.json
   ```
   The version must match the tag you are about to push, and must not end in `-dev`.

2. **Create an annotated tag.** The message shows up on the GitHub Release.
   ```bash
   git tag -a v1.3.2 -m "DeBlasis.GhosttyVt 1.3.2 (Ghostty 1.3.2)"
   ```

3. **Push the tag.**
   ```bash
   git push origin v1.3.2
   ```

4. **Approve the deployment.** GitHub will email / notify you. Go to **Actions → Release → (run) → Review deployments**, verify the computed version and the upstream commit shown in the `preflight` logs, then **Approve**.

5. **Confirm the publish.** After approval the workflow will:
   - Push `DeBlasis.GhosttyVt.1.3.2.nupkg` (and `.snupkg`) to nuget.org via `--skip-duplicate`.
   - Create GitHub Release `v1.3.2` with auto-generated changelog and the package link.
   - Attach the `.nupkg` and `.snupkg` files to the GitHub Release.

   Verify at https://www.nuget.org/packages/DeBlasis.GhosttyVt/1.3.2 (allow a few minutes for indexing).

## Dry run

Before pushing a real tag, you can exercise the full build + test + pack pipeline without publishing anything:

```bash
gh workflow run release.yml -f dry_run_commit=$(jq -r '.commit' ghostty-upstream.json)
```

This runs `preflight` (with Guard 2 skipped — there is no tag to compare), the full native build matrix, `dotnet test`, and `dotnet pack`. The resulting `.nupkg` is uploaded as a workflow artifact (`nuget-package`) and can be downloaded for inspection. The `publish` job is skipped entirely (`if: needs.preflight.outputs.is_dry_run == 'false'`).

## Failure modes

| Symptom | Meaning | Fix |
|---------|---------|-----|
| `preflight` fails: "Tag X does not match v<X.Y.Z>..." | Tag is malformed. | Delete the tag (`git push origin :v...` + `git tag -d`), re-tag correctly. |
| `preflight` fails: "Tag base version X does not match upstream Y" | Upstream pin is for a different version than the tag. | Check `ghostty-upstream.json`; either retag to match or merge an upstream bump first. |
| `preflight` fails: "Upstream version X-dev is a -dev build" | Upstream has not cut a stable tag yet. | Wait for upstream. Do not force. |
| `preflight` fails: "Pinned commit X not found upstream" | The pin was hand-edited with a typo, or points at a commit that never reached GitHub. | Fix `ghostty-upstream.json`, commit, retag. |
| `pack` fails at `dotnet test` | A test regression. | Fix the test or the underlying bug on `main` first; retag. |
| `publish` pauses at "Review deployments" | Environment protection is doing its job. | Approve when ready. |
| `dotnet nuget push` fails with auth error | `NUGET_API_KEY` is expired or revoked. | Rotate the API key at nuget.org, update the repo secret, re-run `publish`. |

## Rolling back a release

**nuget.org publishes are irreversible.** You can only *unlist* a version, which hides it from search but keeps it resolvable for anyone already referencing it. You cannot re-push the same version number.

1. Unlist at https://www.nuget.org/packages/DeBlasis.GhosttyVt → **Manage package → Listing → Unlist**.
2. Ship the fix as `X.Y.Z.1` (or the next respin digit) using the normal release flow. Note: the nuget.org listing for the unlisted version stays visible to tooling — the fix version replaces it in practice, not in the registry.
