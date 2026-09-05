# Contributing

Thanks for helping improve Fluence.Wpf, a Windows 11 Fluent / WinUI 3 control library for WPF on .NET Framework 4.7.2, .NET 8, and .NET 10.

The authoritative contributor guide is [docs/contributing.md](docs/contributing.md), and the full engineering handbook (conventions, theme architecture, reference authority, and quality gates) is [AGENTS.md](AGENTS.md). Read both before a non-trivial change. For AI-assisted work, read [AGENTS.md](AGENTS.md) first - it is the single source of truth.

This file is the short version.

## Build and test

```powershell
dotnet restore Fluence.Wpf.sln
dotnet build   Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-not-trait "Category=Screenshots" --no-ansi --progress off
```

The suite runs on Microsoft Testing Platform, so run the built executable rather than `dotnet test` (the SDK 10 VSTest bridge is gone). On `net472`, a single-process whole-assembly run aborts non-deterministically, so run it as two complementary lanes instead; see AGENTS.md section 6 and `KNOWN_ISSUES.md`.

Both target frameworks (`net472` and `net10.0-windows10.0.26100.0`) must build and test green. The library builds with `TreatWarningsAsErrors=true`, `WarningLevel=9999`, and `AnalysisLevel=latest-all`: fix warnings at the root cause, do not suppress them. `string.IsNullOrEmpty()` is banned (use `string.IsNullOrWhiteSpace()`); public API needs `///` XML docs or the build fails.

## Formatting and text policy

XAML style (4-space indent, UTF-8 with BOM, LF line endings, final newline) is governed by `.editorconfig`, applied to `.xaml` like every other source file; there is no separate XAML formatter tool. Encoding and text policy are enforced by a repo hook and in CI:

```powershell
pwsh .claude/hooks/post-tool-util.ps1 -CheckAll   # CI gate: UTF-8 BOM, LF, banned APIs, hard-coded hex, em/en dashes
```

All source and text files are UTF-8 with BOM and LF; `string.IsNullOrEmpty()` is banned (use `string.IsNullOrWhiteSpace()`); do not inline hex colors in `Themes/Controls/**` or use em/en dashes in `.cs` / `.md`. Generated XAML (`Properties/DesignTime.*.xaml`) is excluded from the repo-wide check.

## Adding or changing a control

extend the gallery demo; and add xUnit test coverage.

## Reference authority

Every visual or behavioral decision must be grounded, in this order (see [AGENTS.md](AGENTS.md) Section 4):

1. In-tree precedent (existing XAML, controls, tests).
2. Per-domain authority - WinUI 3 CommonStyles for visual tokens and control templates; .NET 10 WPF Themes for WPF-native chrome, accent ramp math, theme detection, and backdrops.
3. Published Windows 11 design guidance on Microsoft Learn (tie-breaker only).

"Looks right" is not an acceptable justification.

## Tests

- One sealed class per subject, in the folder that owns the concern (`Control/<Control>Tests.cs`, `Control/Rules/`, `Theming/`, `Windowing/`, `Gallery/`). The class owns the reset through `IAsyncLifetime` calling `TestApp.EnsureLibraryTheme()`, or `IClassFixture<LightThemeFixture>` when no test mutates theme state. See AGENTS.md section 6 for the full layout and the shared `VisualTree` / `BrushAssert` helpers.
- Cover at minimum: default style applies, key template parts resolve, critical DP/state transitions, and one theme cycle (`ThemeTestHelpers.ApplyStandardThemeCycle`) for theme-sensitive controls.
- The HEAD-of-branch test count is the floor. Add tests; do not weaken the baseline. If a test is legitimately obsoleted, remove the whole file in the same change and record the rationale in `CHANGELOG.md`.

## Visual verification

Run `Fluence.Wpf.Demo` and exercise Light / Dark / High Contrast / Auto, a couple of accent swatches, Mica / Acrylic / Tabbed / None backdrops, and at least one control per gallery page. Capture 100% and 150% DPI screenshots when visuals change materially.

## Pull requests

- Keep changes focused; no unrelated refactors or renames.
- Update [CHANGELOG.md](CHANGELOG.md) under **Unreleased** (Keep a Changelog format, SemVer).
- Update public docs ([README.md](README.md), [docs/controls.md](docs/controls.md), [docs/theming.md](docs/theming.md)) when consumer-visible behavior changes.
- The PR template encodes the build / test / visual / docs gates; fill it in.
- Do not link `docs/_internal/` from any public doc.
