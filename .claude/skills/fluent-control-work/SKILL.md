---
name: fluent-control-work
description: Use for Fluence.Wpf control, theme, demo, or test changes that must preserve WinUI parity, net472 compatibility, and repo verification gates.
---

# Fluent Control Work

Use this workflow for any change under `Fluence.Wpf/Controls`, `Fluence.Wpf/Themes`, `Fluence.Wpf.Demo`, or `Fluence.Wpf.Tests`.

## Read First

1. Read `CLAUDE.md` and the nearest existing implementation before editing.
2. Inspect current files if they already exist; continue from their current state.

## Reference Order

1. Prefer in-tree Fluence precedent in controls, theme dictionaries, tests, and docs.
2. For visual tokens, states, timings, and control visuals, compare against WinUI 3 CommonStyles. The canonical resource file is `https://raw.githubusercontent.com/microsoft/microsoft-ui-xaml/refs/heads/main/src/controls/dev/CommonStyles/Common_themeresources_any.xaml`.
3. For WPF-native chrome, dispatcher, registry, DWM, and .NET API behavior, use official Microsoft Docs or .NET WPF reference sources.
4. If a WinUI pattern cannot work on `net472`, use the closest WPF translation and document the gap.

## Implementation Rules

1. Keep shared library and demo code compatible with C# 7.3 unless the file is net10-only.
2. Use `#if NET10_0_OR_GREATER` only for APIs that do not exist on `net472`.
3. Keep public APIs documented with XML comments.
4. Use `DynamicResource` for theme, accent, high contrast, brush, color, typography, and corner-radius resources that must update at runtime.
5. Do not hard-code production template colors when a canonical WinUI-style key exists.
6. Do not introduce PSADT-specific paths, names, or behavior into Fluence library code.

## Test And Verification

1. Add or update MSTest coverage before implementation when behavior changes.
2. UI tests must run through `WpfTestSta`, `EnsureApplication`, `MergeGenericDictionary`, dispatcher draining, and explicit window cleanup.
3. For controls, cover default style, template parts, key dependency properties, state transitions, and a theme cycle when visuals are theme-sensitive.
4. Run focused tests first, then broader verification:

```powershell
$env:DOTNET_CLI_HOME = '.dotnet-cli-home'
$env:NUGET_PACKAGES = 'C:\Users\$(env:Username)\.nuget\packages'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'
$env:MSBuildEnableWorkloadResolver = 'false'
dotnet build Fluence.Wpf.sln -c Debug --no-restore -m:1
dotnet test Fluence.Wpf.Tests\Fluence.Wpf.Tests.csproj -c Debug --no-build
```

5. Update `CHANGELOG.md` for user-visible or public-surface changes. Update `docs/controls.md`, `docs/theming.md`, or `KNOWN_ISSUES.md` when the public catalog, theme contract, or known gap changes.
