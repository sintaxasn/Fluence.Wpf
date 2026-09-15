# Releasing

A release is one action: bump the version, tag, push the tag. Everything after that is CI. This page is the preconditions and what to check afterwards.

## Preconditions

Confirm all of these before tagging. CI enforces the first two; the rest are judgement.

1. **CI is green on `main`.** The `build` job runs the text policy check, restores in locked mode, builds Release, verifies formatting, and runs both target framework test lanes.
2. **`CHANGELOG.md` has a dated section for the version you are about to tag**, with nothing left under `Unreleased` that belongs in it. The release job slices that section for the release notes and fails if it is missing.
3. **`PublicAPI.Unshipped.txt` is empty for every target framework.** Nothing in CI enforces this: `PublicApiAnalyzers` fails the build only when a public member is undeclared in both `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` (RS0016) or when a declared member has disappeared (RS0017). A member sitting in `Unshipped` satisfies that check just as well as one folded into `Shipped`, so a 1.0 tag can go out with additions never folded in unless you confirm this by hand:

   ```powershell
   Get-ChildItem -Recurse -Filter PublicAPI.Unshipped.txt Fluence.Wpf/PublicAPI |
       ForEach-Object { '{0}: {1} lines' -f $_.Directory.Name, (Get-Content $_.FullName).Count }
   ```

   One line, `#nullable enable`, means empty. To fold the additions in, fold each `PublicAPI.Unshipped.txt` into its sibling `PublicAPI.Shipped.txt`, sort the result, and reset the unshipped file to `#nullable enable`. Folding in is not a plain append. A `*REMOVED*` line is an instruction to delete the named member from `PublicAPI.Shipped.txt`, so apply it and drop the marker rather than carrying it across, or the shipped baseline ends up holding an entry form that does not belong in it. Both files also begin with `#nullable enable`, so keep one and drop the duplicate.
4. **`docs/migration-guide.md` has an entry for every breaking change in the section.** The release policy in [the roadmap](roadmap.md) promises this. After 1.0 there should be none in a minor release.
5. **Screenshots are current.** If gallery visuals changed, regenerate `docs/screenshots/` before tagging:

   ```powershell
   $env:FLUENCE_CAPTURE_SCREENSHOTS = '1'
   dotnet build Fluence.Wpf.sln -c Debug
   Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Tools.GalleryScreenshotHarness
   ```
6. **The `NUGET_API_KEY` repository secret is set.** The `release` job's last step pushes to nuget.org using it, and nothing prompts you to create it before the first tag. A missing or expired key fails only at that last step, after the GitHub release has already been created and the zips and packages already attached.

## Bump

Edit `VersionPrefix` and `VersionSuffix` in `Directory.Build.props`. Nothing else in the tree carries a version:

```xml
<VersionPrefix>0.9.0</VersionPrefix>
<VersionSuffix>pre</VersionSuffix>
```

An empty `VersionSuffix` is a stable release. A prerelease sets it, for example `pre`, which produces `0.9.0-pre`, or `rc.1`, which produces `1.0.0-rc.1`. The SDK derives `PackageVersion`, `AssemblyVersion`, `FileVersion` and `InformationalVersion` from these two; do not add them back, and never restate a version in a csproj, where it would win over this file.

Commit the bump, along with the `CHANGELOG.md` section, on `main`.

## Tag

```powershell
git tag v0.9.0-pre
git push origin v0.9.0-pre
```

The tag must be exactly `v` plus the version the tree resolves to. CI checks it and fails the release before publishing anything if it does not match. To confirm before tagging:

```powershell
dotnet msbuild Fluence.Wpf/Fluence.Wpf.csproj -getProperty:Version -p:TargetFramework=net472 -nologo
```

## What CI does

On the tag push, the `build` job runs everything it runs for `main`, then packs. The `release` job then:

1. Checks the tag against the tree version and fails if they differ.
2. Zips the per-target-framework library binaries and the demo.
3. Slices the `CHANGELOG.md` section for the version into the release notes.
4. Creates the GitHub release with those assets, the `.nupkg` and the `.snupkg` attached, marking it a prerelease when the tag carries a SemVer prerelease identifier. If a release for the tag already exists, this step leaves it alone instead of recreating it, so re-running the job after a later step failed does not touch a release that already published correctly.
5. Pushes the `.nupkg` to nuget.org from the `NUGET_API_KEY` secret; `dotnet nuget push` pushes the sibling `.snupkg` from the same folder automatically. `--skip-duplicate` means a re-run whose package already reached nuget.org does not fail on that account.

## Afterwards

- The package page on nuget.org lists the symbol package.
- A consumer can step into a library source file from a Release build. Reference the published package from a scratch project, enable "Enable Source Link support" and "Enable source server support" and disable "Just My Code" in the debugger, put a breakpoint in a handler and step into `ApplicationThemeManager.Apply`.
- The GitHub release notes are the changelog section, not a commit list.

## A mistaken tag

A tag that fails the version guard has published nothing, so delete it, fix the version, and tag again. A tag that got past the guard has published to nuget.org, and **a published version can be unlisted but never replaced**. That is why the release candidate exists: tag `vX.Y.Z-rc.1` first and let it run the whole path before the stable tag.

An RC tag needs its own dated `## [X.Y.Z-rc.1]` section in `CHANGELOG.md`, distinct from the `## [X.Y.Z]` section the stable tag will use later. Precondition 2 above applies to whichever version you are tagging: without a matching section, the changelog slice step fails inside the release job, before anything is created or published.

## Strong naming

**The assembly is not strong named, deliberately.** There is no `SignAssembly` and no key file. The primary consumer references the library by project reference and does not need it, and a signing key is a release liability: it has to be stored, rotated and never lost, and every consumer is bound to it forever. If a consumer ever needs to load Fluence into a strong-named context, that is a major release decision, not a patch.
