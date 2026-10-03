# Releasing

This maintainer procedure covers the .NET package and the PowerShell module. Read the [documentation site](https://fluencewpf.com) for consumer guidance and the [roadmap](roadmap.md) for release policy.

A release starts by bumping the version, creating the matching tag, and pushing it. CI then creates the GitHub release, attaches the library and PowerShell module packages, and publishes to NuGet and PowerShell Gallery. Complete the final test round and get the owner's go-ahead before creating or pushing the tag. The configured GitHub release-environment reviewer then approves the publication job after the tag is pushed.

## Preconditions

Confirm all of these before tagging. CI enforces the first two; the rest are judgement.

1. **CI is green on `main`.** The `build` job runs the text policy check, restores in locked mode, builds Release, verifies formatting, and runs both target framework test lanes.
2. **`CHANGELOG.md` has a dated section for the version you are about to tag**, with nothing left under `Unreleased` that belongs in it. The release job slices that section for the release notes and fails if it is missing.
3. **`PublicAPI.Unshipped.txt` is empty.** One pair of baseline files under `Fluence.Wpf/PublicAPI/` serves all three target frameworks, because the surface is the same on each. Nothing in CI enforces this: `PublicApiAnalyzers` fails the build only when a public member is undeclared in both `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` (RS0016) or when a declared member has disappeared (RS0017). A member sitting in `Unshipped` satisfies that check just as well as one folded into `Shipped`, so a 1.0 tag can go out with additions never folded in unless you confirm this by hand:

   ```powershell
   (Get-Content Fluence.Wpf/PublicAPI/PublicAPI.Unshipped.txt).Count
   ```

   One line, `#nullable enable`, means empty. To fold the additions in, fold `PublicAPI.Unshipped.txt` into `PublicAPI.Shipped.txt`, sort the result, and reset the unshipped file to `#nullable enable`. Folding in is not a plain append. A `*REMOVED*` line is an instruction to delete the named member from `PublicAPI.Shipped.txt`, so apply it and drop the marker rather than carrying it across, or the shipped baseline ends up holding an entry form that does not belong in it. Both files also begin with `#nullable enable`, so keep one and drop the duplicate.
4. **Breaking changes are called out in `CHANGELOG.md`.** The release policy in [the roadmap](roadmap.md) requires a clear entry for each public API or XAML resource-key change. After 1.0 there should be no breaking changes in a minor release.
5. **Screenshots are current.** Before capture, select the Windows Light appearance for apps and the system, the native default-blue accent palette (`#0078D4` base), and enabled transparency effects. Turn off accent coloring on title bars and window borders. Record these OS settings with the capture; restore any setting changed only for the capture afterward. Capture scripts select Light and Dark explicitly for their labeled pairs, and named custom-accent examples intentionally use their stated colors.

   Regenerate `docs/screenshots/` with both gallery passes. The four-case pass writes the gallery routes, cards, and top-level images; the separate controls-only case writes the control images. Together they produce 378 PNGs plus `gallery/manifest.json`:

   ```powershell
   $env:FLUENCE_CAPTURE_SCREENSHOTS = '1'
   dotnet build Fluence.Wpf.sln -c Debug
   Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Tools.GalleryScreenshotHarness
   $env:FLUENCE_CAPTURE_CONTROLS_ONLY = '1'
   Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-method Fluence.Wpf.Tests.Tools.GalleryScreenshotHarness.CaptureGalleryCatalogAndCardsAsync
   Remove-Item Env:FLUENCE_CAPTURE_CONTROLS_ONLY
   Remove-Item Env:FLUENCE_CAPTURE_SCREENSHOTS
   ```

   The harness writes to `docs/screenshots/` by default. If another process has a screenshot mapped open, set `FLUENCE_CAPTURE_OUTPUT_DIRECTORY` to a fresh empty directory before both passes. Review the complete output there, then copy its contents into `docs/screenshots/` and compare file hashes. Clear the override afterward; do not import a partial or failed pass.

   Control reference images live in `docs/screenshots/controls/`: each captures the control visual without the gallery sample frame, with 64 pixels of space above and below and 32 pixels on each side at 96 DPI. Keep both light and dark captures current, including the documented state changes. Review each regenerated control image to confirm the control, theme, state, and padding are visible. For PowerShell dialogs and windows, build and stage the module, then run its documentation capture script:

   ```powershell
   dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release
   pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Build-Module.ps1 -Configuration Release
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File Fluence.Wpf.PowerShell.Module/build/Capture-Documentation.ps1
   ```

   Capture the live feature animations and the diagnostic theme sequence sequentially after module staging:

   ```powershell
   powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File Fluence.Wpf.PowerShell.Module/build/Capture-FeatureAnimations.ps1
   powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File Fluence.Wpf.PowerShell.Module/build/Capture-ThemeAnimation.ps1
   ```

   The PowerShell documentation and both live-screen scripts minimize other desktop windows by default and undo that minimization after each capture. Run one capture process at a time: the Shell undo action restores the state before its most recent minimize call. Review each script's desktop-state log and output frames before importing the eight feature GIFs and posters. The accent animations deliberately cycle labeled custom hues; other scenes begin with the Windows system accent. The theme sequence stays under ignored `artifacts/theme-animation/` for diagnosis.

   Import the reviewed captures into the separate [website repository](https://github.com/sintaxasn/Fluence.Wpf.Website), under its `docs/screenshots/`, `docs/powershell/images/`, and `website/static/images/features/` paths. Review the [gallery screenshot manifest](https://github.com/sintaxasn/Fluence.Wpf.Website/blob/main/docs/screenshots/gallery/manifest.json) and [PowerShell capture notes](https://github.com/sintaxasn/Fluence.Wpf.Website/blob/main/docs/powershell/images/CAPTURE.md) for render methods and limits. The gallery and PowerShell documentation captures render WPF offscreen and omit native DWM shadows or backdrops.
6. **The `NUGET_API_KEY` and `PSGALLERY_API_KEY` secrets are set.** The release job uses them to publish the library package to nuget.org and the module to PowerShell Gallery. It checks that both keys are present before creating the GitHub release. Verify that neither key has expired before tagging.
7. **The `release` environment has a required reviewer.** The release job runs in this GitHub Environment so creating the release and publishing either package waits for approval. Configure the rule in repository settings (Settings, Environments, `release`, Required reviewers). GitHub creates an environment on first use without rules; without a reviewer, the job runs unattended. Store publication keys as environment secrets where possible so only this job can read them.
8. **The C# API reference matches the library.** After building the current source, regenerate the checked-in pages and confirm there is no documentation drift:

   ```powershell
   dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Debug -f net10.0-windows10.0.26100.0
   pwsh -NoProfile -File tools/ApiDocs/Generate-ApiDocs.ps1
   pwsh -NoProfile -File tools/ApiDocs/Generate-ApiDocs.ps1 -Check
   ```

   Review the generated changes with the related control guides, then build the [documentation website](https://github.com/sintaxasn/Fluence.Wpf.Website/blob/main/README.md). The generator uses the shared public API surface; the three library target frameworks remain governed by the API baseline and build gates above.

## 1.0 approval checkpoint

The 0.9.1 stable release does not start the 1.0 API freeze. Before a future 1.0 version bump, complete the integration review. CI builds, tests, and packages the module in a separate `powershell` job after the .NET build succeeds. Require both jobs before merging. The tag workflow attaches the module ZIP and Gallery-format `.nupkg` to the GitHub release, publishes the library package to NuGet, and publishes the module to PowerShell Gallery. Do not create a 1.0 tag until the final test round passes and both publication credentials and the required reviewer are configured.

## Bump

Edit `VersionPrefix` and `VersionSuffix` in `Directory.Build.props`. The PowerShell module manifest is a separate version source and must be updated as described below:

```xml
<VersionPrefix>0.9.1</VersionPrefix>
<VersionSuffix></VersionSuffix>
```

An empty `VersionSuffix` is a stable release. A future prerelease can set it to `pre` or `rc.1`, producing versions such as `1.0.0-pre` or `1.0.0-rc.1`. The SDK derives `PackageVersion`, `AssemblyVersion`, `FileVersion` and `InformationalVersion` from these two; do not add them back, and never restate a version in a csproj, where it would win over this file.

The one version outside the solution is the PowerShell module manifest, `Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1`, which the SDK does not generate. Set `ModuleVersion` to the same `VersionPrefix` and `PSData.Prerelease` to the same `VersionSuffix` (with no leading hyphen), so `Package-Module.ps1` names its artifacts with the same string the library nupkg carries. `Build-Module.ps1` enforces exact agreement before building or touching staging, and packaging invokes that same guard. For a stable release, clear both suffixes; switching only one fails the gate.

Commit the bump, along with the `CHANGELOG.md` section, on `main`.

## Tag

```powershell
$version = dotnet msbuild Fluence.Wpf/Fluence.Wpf.csproj -getProperty:Version -p:TargetFramework=net472 -nologo
git tag "v$version"
git push origin "v$version"
```

## PowerShell module gates

The script module under `Fluence.Wpf.PowerShell.Module/` is outside the solution and has its own gate (see [AGENTS.md section 6](../AGENTS.md#6-testing) for the lane definitions). Install Pester 5.8.0 and PSScriptAnalyzer 1.25.0 in each edition before running the gate; the runner imports those exact versions. Follow the separate network setup commands in the [module README](../Fluence.Wpf.PowerShell.Module/README.md#run-the-module-gate). Windows PowerShell setup pins the NuGet provider to 2.8.5.208; PowerShell 7 uses its compatible bundled provider. Packaging requires an already installed compatible provider and does not bootstrap one.

Stage the Release assemblies first, then run the analyzer and the Pester logic lane on both editions; the render lane opens real windows, so run it once locally before tagging:

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Build-Module.ps1 -Configuration Release
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1
pwsh -NoProfile -MTA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1 -SkipAnalyzer
powershell.exe -NoProfile -STA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1 -IncludeUi
powershell.exe -NoProfile -STA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1 -IncludeUi
pwsh -NoProfile -MTA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1 -IncludeUi
```

Expected counts for the current suite (Pester reports them on the `Pester:` summary line):

| Lane | Total | Passed | Skipped | Not run |
| --- | ---: | ---: | ---: | ---: |
| Logic lane, `pwsh` or `powershell.exe` (STA) | 232 | 199 | 2 | 31 |
| Logic lane, `pwsh -MTA` | 232 | 201 | 0 | 31 |
| Render lane (`-IncludeUi`), STA hosts | 232 | 230 | 2 | 0 |
| Render lane (`-IncludeUi`), `pwsh -MTA` | 232 | 232 | 0 | 0 |

The two skipped cases on STA hosts are the MTA-only transport tests; the not-run cases are the UI-tagged render tests. PSScriptAnalyzer must report no findings. A change that adds or removes a case updates this table and says why in `CHANGELOG.md`.

Run each gate in its own process. Require both passing Pester results and process exit code 0. The test runner performs terminal cleanup of an inline dispatcher; the module handles its owned secondary dispatcher during primary ConsoleHost exit. Host ownership boundaries are recorded in [KNOWN_ISSUES.md](../KNOWN_ISSUES.md).

Then package and inspect the module artifacts locally. This creates files only; it does not publish them:

```powershell
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Package-Module.ps1 -Configuration Release
```

`Fluence.Wpf.PowerShell.Module/artifacts/` must hold `Fluence.Wpf.PowerShell-<version>.zip` and `Fluence.Wpf.PowerShell.<version>.nupkg`, where the version is `ModuleVersion` from the manifest with the `PSData.Prerelease` tag appended when present. The stable 0.9.1 manifest produces `Fluence.Wpf.PowerShell-0.9.1.zip` and `Fluence.Wpf.PowerShell.0.9.1.nupkg`. Inspect the ZIP contents, install the module from the ZIP in both supported PowerShell editions, and confirm the README and command references describe the current cmdlet set. The tag workflow attaches both artifacts to GitHub and publishes the module to PowerShell Gallery.

## Pack check

```powershell
dotnet msbuild Fluence.Wpf/Fluence.Wpf.csproj -getProperty:Version -p:TargetFramework=net472 -nologo
```

## What CI does

The `build` job owns the .NET restore, build, formatting, test, pack and artifact steps. A separate `powershell` job downloads its `fluence-wpf-release-dotnet472` and `fluence-wpf-release-dotnet8` artifacts into the corresponding Release output folders, stages the module, installs pinned tools in dedicated setup steps, and runs the PowerShell 7 STA, PowerShell 7 MTA and Windows PowerShell 5.1 STA logic lanes. It uploads `fluence-ps-module-package` and a separate `fluence-ps-module-test-results` artifact. The render lane stays local.

On a tag push, `release` waits for both the .NET build job and PowerShell module job to pass, then downloads their package artifacts. It then:

1. Checks the tag against the tree version and fails if they differ.
2. Zips the per-target-framework library binaries and the demo.
3. Slices the `CHANGELOG.md` section for the version into the release notes.
4. Creates the GitHub release with the per-TFM library ZIPs, demo ZIP, library `.nupkg` and `.snupkg`, plus the PowerShell module ZIP and Gallery-format `.nupkg`. It marks prerelease tags accordingly. If a release for the tag already exists, the step leaves it alone so a failed later publication can be retried.
5. Pushes the library `.nupkg` and sibling `.snupkg` to nuget.org using `NUGET_API_KEY`; `--skip-duplicate` makes a retry safe.
6. Publishes the packaged PowerShell module to PowerShell Gallery using `PSGALLERY_API_KEY`. It checks for the exact version first and skips an already-published version so a retry does not try to replace an immutable package.

## Afterwards

- The package page on nuget.org lists the symbol package.
- A consumer can step into a library source file from a Release build. Reference the published package from a scratch project, enable "Enable Source Link support" and "Enable source server support" and disable "Just My Code" in the debugger, put a breakpoint in a handler and step into `ApplicationThemeManager.Apply`.
- The GitHub release notes are the changelog section, not a commit list.

## A mistaken tag

A tag that fails the version guard has published nothing, so delete it, fix the version, and tag again. A tag that got past the guard has published to nuget.org, and **a published version can be unlisted but never replaced**. That is why the release candidate exists: tag `vX.Y.Z-rc.1` first and let it run the whole path before the stable tag.

An RC tag needs its own dated `## [X.Y.Z-rc.1]` section in `CHANGELOG.md`, distinct from the `## [X.Y.Z]` section the stable tag will use later. Precondition 2 above applies to whichever version you are tagging: without a matching section, the changelog slice step fails inside the release job, before anything is created or published.

## Strong naming

**The assembly is not strong named, deliberately.** There is no `SignAssembly` and no key file. The primary consumer references the library by project reference and does not need it, and a signing key is a release liability: it has to be stored, rotated and never lost, and every consumer is bound to it forever. If a consumer ever needs to load Fluence into a strong-named context, that is a major release decision, not a patch.
