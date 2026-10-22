# Releasing DeBlasis.GhosttyVt

This document is for maintainers. Day-to-day users should see the [README](README.md).

## Versioning rule

Package versions are **independent SemVer** for this library:

- MAJOR for a breaking public API change.
- MINOR for a new public API.
- PATCH for fixes and internal work.
- Packaging-only respins use a fourth digit, `X.Y.Z.N`, which NuGet permits.

`version.json` at the repository root is the source of truth. The package version does **not** mirror the upstream Ghostty version.

Upstream tracking lives in `ghostty-upstream.json`: it pins the exact Ghostty commit each build uses. Release notes name that commit and the upstream version it reports. The package version and the upstream version are deliberately separate.

The release channels are:

| Channel | Version shape | Example |
|---|---|---|
| Nightly | `<version>-ci.<timestamp>.<sha>` | `1.4.1-ci.202610221530.67edd20a` |
| Release candidate | `<version>-rc.<n>` | `1.4.0-rc.2` |
| Stable | `<version>` | `1.4.0` |
| Packaging respin | `<version>.<n>` | `1.4.0.1` |

Nightly builds sort below the release candidate and stable versions for the same base. A stable release supersedes its candidates.

After a stable release, update `version.json` to the next version before the next nightly. For example, after `1.4.0`, set `version.json` to `1.4.1` for internal fixes, or `1.5.0` for new public API work.

## Preconditions

Before tagging:

- `version.json` contains a plain `X.Y.Z` version matching the tag base.
- `ghostty-upstream.json` points at a commit that exists upstream. The upstream version may be a `-dev` build; it is recorded for the release notes, not compared to the package version.
- The working tree is clean and the tag is created from current `main`.

Check the inputs:

```bash
jq -r '.version' version.json
jq -r '.commit, .upstreamVersion' ghostty-upstream.json
```

## One-time setup

Before the first release:

1. Repo → **Settings → Environments → New environment** → `nuget-release`.
2. Under **Deployment protection rules → Required reviewers**, add yourself (`deblasis`).
3. No deployment branch restrictions are needed. Tag refs are not branches, and the workflow's tag-shape guard covers correctness.

The `NUGET_API_KEY` secret is configured and glob-scoped to `DeBlasis.GhosttyVt*`.

## Cutting a release

1. **Update `version.json` and the changelog.** The tag base must match `version.json`.
2. **Commit and push `main`.** Tag from that commit.
3. **Create an annotated tag.** The message shows up on the GitHub Release.
   ```bash
   git tag -a v1.4.0 -m "DeBlasis.GhosttyVt 1.4.0"
   ```
4. **Push the tag.**
   ```bash
   git push origin v1.4.0
   ```
5. **Approve the deployment.** Go to **Actions → Release → (run) → Review deployments**, verify the computed package version and the upstream commit in the preflight logs, then approve.
6. **Confirm the publish.** After approval the workflow will:
   - push `DeBlasis.GhosttyVt.1.4.0.nupkg` and its `.snupkg` to nuget.org with `--skip-duplicate`;
   - create GitHub Release `v1.4.0`, marked stable or prerelease as appropriate;
   - attach the package assets to the release.
7. **Bump `version.json`** to the next release version and commit it, so the next nightly builds above the release.

## Dry run

Exercise the full build, test, pack and consumer validation without publishing:

```bash
gh workflow run release.yml -f dry_run_commit=$(jq -r '.commit' ghostty-upstream.json)
```

This runs preflight (with tag guards skipped), the native matrix, tests, pack and the package-consumer validation. The `publish` job is skipped.

## Guards

`build/selftest-release-guards.sh` executes the real `run:` blocks from `release.yml` and asserts both the refusals and their reasons. It covers:

- tag shape (`vX.Y.Z`, `vX.Y.Z.N`, `vX.Y.Z-rc.N`);
- tag base matching `version.json`;
- plain `X.Y.Z` in `version.json`;
- the dry-run branch;
- the de-duplicated prerelease label rules;
- the upstream-version record.

It runs in CI. The guards are the only thing between a wrong version and an irreversible nuget.org publish, so do not weaken them without updating the self-test.

## Failure modes

| Symptom | Meaning | Fix |
|---|---|---|
| `preflight` fails: tag does not match `v<X.Y.Z>...` | Malformed tag. | Delete the tag, re-tag correctly. |
| `preflight` fails: tag base does not match `version.json` | The version file was not updated for this tag. | Update `version.json`, commit, retag. |
| `preflight` fails: `version.json` is not plain `X.Y.Z` | The version file has a suffix or is malformed. | Use a plain version; put channel suffixes in the tag only. |
| `preflight` fails: pinned commit not found upstream | Bad or unreachable pin. | Fix `ghostty-upstream.json`. |
| `pack` fails at tests or consumer validation | A regression or packaging problem. | Fix on `main` first, then retag. |
| `publish` pauses at Review deployments | Environment protection. | Approve when ready. |
| `dotnet nuget push` auth error | Expired or revoked API key. | Rotate the key and re-run. |

## Rolling back

nuget.org publishes are irreversible. You can only unlist a version; it remains resolvable for existing references. Cannot re-push the same version number.

1. Unlist the version at https://www.nuget.org/packages/DeBlasis.GhosttyVt → **Manage package → Listing → Unlist**.
2. Ship the fix as the next patch version (`X.Y.Z+1`), or a four-digit respin for packaging-only issues.
