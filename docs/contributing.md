# Contributing

## Build and test

```powershell
dotnet restore Fluence.Wpf.sln
dotnet build Fluence.Wpf.sln
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj
```

Test project targets **net472** and **net10.0-windows**; both must pass. WPF tests run on a shared STA dispatcher (`WpfTestSta`); the assembly uses `[assembly: DoNotParallelize]` to avoid cross-thread resource issues. The branch's current test count is the floor: add coverage for new behavior and do not remove tests without documenting the replacement rationale.

## Language and style

- **Fluence.Wpf** library: **C# 7.3** on `net472` (no `default` interface members, no nullable reference types, no ranges). `net10.0-windows` may use `latest` via the `LangVersion` conditional.
- Every `.cs` file starts with the standard BSD 3-Clause header used across the repo; match an existing file exactly.
- Public APIs carry `///` XML comments. The library builds with `<DocumentationFile>` and does **not** suppress `CS1591` / `CS1574` - a missing comment becomes a build error.
- XAML lives in `Fluence.Wpf/Themes/Controls/<ControlName>.xaml` and is merged from `Themes/Generic.xaml`.

## Visual changes

- Run **Fluence.Wpf.Demo** and exercise: theme (Light / Dark / High Contrast / Auto), accent swatches, backdrop, and representative controls per gallery section.
- Prefer `DynamicResource` for theme-bound properties in XAML.
- Reference the authoritative WinUI 3 CommonStyles sources when choosing resource keys, state tables, or animation timings; use .NET WPF theme sources for WPF-native chrome and interop patterns.

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

- Public guides live in `docs/*.md`. Maintainer-only notes live under `docs/_internal/`; do not link them from `README.md` or public guides.
- AI-assisted edits should read [CLAUDE.md](../CLAUDE.md) and [.github/copilot-instructions.md](../.github/copilot-instructions.md) for project standards and quality gates.
