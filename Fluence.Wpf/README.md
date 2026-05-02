# Fluence.Wpf Library

This folder contains the reusable WPF control library. It targets `net472` and `net10.0-windows10.0.26100.0` and provides the Fluent/WinUI-style controls, theme resources, accent handling, window chrome, and native interop used by the demo applications.

## What Lives Here

- `Controls/` - public WPF controls such as `FluenceWindow`, `NavigationView`, `TabView`, `Card`, input controls, status controls, and layout helpers.
- `Themes/` - color, brush, typography, and control-template dictionaries loaded by `ApplicationThemeManager`.
- `Automation/` - UI Automation peers for custom controls.
- `Native/` and `Helpers/` - DWM, OS-version, registry, and rendering helpers.
- `ApplicationThemeManager`, `ApplicationAccentColorManager`, and `SystemThemeWatcher` - the theme/accent lifecycle surface consumers call at startup.

## Build

From the repository root:

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Debug
```

The `net472` target is constrained to C# 7.3. Keep public APIs documented with XML comments and keep `.cs`, `.xaml`, and `.csproj` files encoded as UTF-8 with BOM.

## Maintenance Notes

Use `ApplicationThemeManager.Apply(...)` to load the five managed resource-dictionary slots instead of hand-merging `Themes/Generic.xaml`. When changing templates, prefer canonical theme keys and `DynamicResource` for theme/accent-bound brushes. See the root [AGENTS.md](../AGENTS.md), [docs/theming.md](../docs/theming.md), and [docs/controls.md](../docs/controls.md) for the full contract.
