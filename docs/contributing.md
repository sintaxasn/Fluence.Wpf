## Build and test

```powershell
dotnet restore Fluence.Wpf.sln
dotnet build Fluence.Wpf.sln
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-not-trait "Category=Screenshots" --no-ansi --progress off
```

The suite runs on Microsoft Testing Platform, so run the built executable rather than `dotnet test`. On `net472` run it as two complementary lanes; see AGENTS.md section 6 and `KNOWN_ISSUES.md` for why a single-process whole-assembly run there aborts.

WPF tests share a single STA dispatcher (`WpfTestSta`), and the assembly carries `[assembly: Parallelization(Mode = ParallelMode.None)]` (with `xunit.runner.json` disabling runner parallelism) to avoid cross-thread resource issues.

## Language and style

- **Fluence.Wpf** library: `LangVersion=latest` and nullable reference types are enabled centrally. Use modern C# syntax on any target framework, but keep runtime APIs to ones that exist on .NET Framework 4.7.2 unless the code is already isolated to a newer target.
- Every `.cs` file starts with the standard BSD 3-Clause header used across the repo; match an existing file exactly.
- Public APIs carry `///` XML comments. The library builds with `<DocumentationFile>` and does **not** suppress `CS1591` / `CS1574` - a missing comment becomes a build error.
- XAML lives in `Fluence.Wpf/Themes/Controls/<ControlName>.xaml` and is merged from `Themes/Generic.xaml`.

## Visual changes

- Run **Fluence.Wpf.Demo** and exercise: theme (Light / Dark / High Contrast / Auto), accent swatches, backdrop, and representative controls per gallery section.
- Prefer `DynamicResource` for theme-bound properties in XAML.
- Use WinUI 3 CommonStyles as the visual reference for resource keys, states, and animation timing. For WPF-specific chrome or interop behavior, follow .NET WPF theme sources.

## Tests

- One sealed class per subject, in the folder that owns the concern: `Control/<Control>Tests.cs` for a control, `Control/Rules/` for a rule asserted across many controls, `Theming/`, `Windowing/`, `Gallery/` and `Gallery/Pages/` for those concerns, `Infrastructure/` for helpers, `Tools/` for the screenshot harness. Each folder is a namespace segment, so a file under `Control/` declares `namespace Fluence.Wpf.Tests.Control`; IDE0130 is an error, so this is not optional. Do not add a folder whose name matches the last segment of a `Fluence.Wpf.*` namespace the tests use by shorthand.
- Let the class own the reset: implement `IAsyncLifetime` and call `TestApp.EnsureLibraryTheme()` from `InitializeAsync` through `WpfTestSta.RunOnStaAsync`. Take `IClassFixture<LightThemeFixture>` instead if no test in the class applies a theme, changes the accent, or toggles reduced motion. `TestApp.EnsureDemoTheme()` is the explicit demo opt-in and belongs in `Gallery/`.
- Use the shared helpers rather than a private copy: `VisualTree` (with `using static Fluence.Wpf.Tests.Infrastructure.VisualTree;`), `BrushAssert`, `ThemeTestHelpers`, `DemoTestHost`.
- When adding a new public control, include at minimum:
  - A default-style and template smoke test.
  - A theme-cycle test if the control uses `DynamicResource` heavily (`ThemeTestHelpers.ApplyStandardThemeCycle`).
  - Interaction or state assertions for any public event or read-only DP the control exposes.
- `Control/Rules/FluentStrokeTests.cs` is the reference pattern for small template and behavior probes: show a minimal `Window`, `ApplyTemplate`, assert template parts and resolved brushes, then drain and close.

## Pull requests

- Keep changes focused; avoid unrelated refactors.
- If you add a public control or change a template, extend xUnit test coverage (template parts, theme cycle, or demo navigation smoke where appropriate).
- Update [CHANGELOG.md](../CHANGELOG.md) under **Unreleased** or the next version section.
- The library builds with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`; fix warnings rather than suppressing them.

## Documentation

- Public guides live in `docs/*.md`. Maintainer-only notes live under `docs/_internal/`; do not link them from `README.md` or public guides.
- AI-assisted edits should read [AGENTS.md](../AGENTS.md) for project standards and quality gates.

## Documentation site

There is currently **no** hosted documentation site; documentation lives entirely in the Markdown files under `docs/` and at the repository root. A published site is planned but not yet set up.

- Cross-doc links: prefer `[text](other-doc.md)` relative links so they resolve both on GitHub and in any future generated site.
