# Contributing

## Build and test

```powershell
dotnet restore Fluence.Wpf.sln
dotnet build Fluence.Wpf.sln
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj
```

Test project targets **net472** and **net10.0-windows**; both must pass. WPF tests run on a shared STA dispatcher (`WpfTestSta`); the assembly uses `[assembly: DoNotParallelize]` to avoid cross-thread resource issues. Current baseline is **230** tests, duration ~16 seconds on `net472`.

## Language and style

- **Fluence.Wpf** library: **C# 7.3** on `net472` (no `default` interface members, no nullable reference types, no ranges). `net10.0-windows` may use `latest` via the `LangVersion` conditional.
- Every `.cs` file starts with the standard BSD 3-Clause header used across the repo; match an existing file exactly.
- Public APIs carry `///` XML comments. The library builds with `<DocumentationFile>` and does **not** suppress `CS1591` / `CS1574` - a missing comment becomes a build error.
- XAML lives in `Fluence.Wpf/Themes/Controls/<ControlName>.xaml` and is merged from `Themes/Generic.xaml`.

## Visual changes

- Run **Fluence.Wpf.Demo** and exercise: theme (Light / Dark / High Contrast / Auto), accent swatches, backdrop, and representative controls per gallery section.
- Prefer `DynamicResource` for theme-bound properties in XAML.
- Reference the WinUI 3 source (`WinUI_XAML/Controls/*_themeresources.xaml` in the repo root) when choosing resource keys, state tables, or animation timings.

## Tests

- Drop new test files alongside existing ones (`ControlTests.<Area>.cs`) as partial extensions of `public partial class ControlTests` so they share the `RunOnStaThread`, `EnsureApplication`, `MergeGenericDictionary`, and `FindVisualChild*` helpers.
- When adding a new public control, include at minimum:
  - A default-style / template smoke test.
  - A theme-cycle test if the control uses `DynamicResource` heavily (`ThemeTestHelpers.ApplyStandardThemeCycle`).
  - Interaction or state assertions for any public event / read-only DP the control exposes.
- `ControlTests.FluentStroke.cs` is the reference pattern for small template/behavior probes: apply the generic dictionary, show a minimal `Window`, `ApplyTemplate`, assert template parts and resolved brushes, then drain and close.

## Pull requests

- Keep changes focused; avoid unrelated refactors.
- If you add a public control or change a template, extend MSTest coverage (template parts, theme cycle, or demo navigation smoke where appropriate).
- Update [CHANGELOG.md](../CHANGELOG.md) under **Unreleased** or the next version section.
- The library builds with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`; fix warnings rather than suppressing them.

## Documentation

- Public guides live in `docs/*.md`. Archive maintainer-only notes under `docs/_internal/` only if/when that folder is reintroduced.
- AI-assisted edits should read [CLAUDE.md](../CLAUDE.md) and [.github/copilot-instructions.md](../.github/copilot-instructions.md) for project standards and quality gates.
