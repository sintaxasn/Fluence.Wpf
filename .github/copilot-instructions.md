# Fluence.Wpf - AI Coding Instructions

This file provides context for GitHub Copilot, Cursor, and other AI coding assistants working on this project.

## Project Overview

Fluence.Wpf is a WPF control library that recreates Windows 11 Fluent Design (WinUI 3) controls and theming for **.NET Framework 4.7.2** applications. It targets Windows 10 1809+ with enhanced features on Windows 11.

## Architecture

### Solution Structure

- `Fluence.Wpf/` - Core class library (multi-targets `net472` and `net10.0-windows`)
- `Fluence.Wpf.Demo/` - Control gallery (`net472`)
- `Fluence.Wpf.Demo.Mvvm/` - CommunityToolkit.Mvvm Task Manager demo (`net10.0-windows`)
- `Fluence.Wpf.Tests/` - MSTest suite (`Microsoft.NET.Test.Sdk`, MSTest 3.2; `net472` and `net10.0-windows`)

### Namespace Layout

- `Fluence.Wpf` - `ApplicationThemeManager`, `ApplicationAccentColorManager`, `SystemThemeWatcher`, theme enums
- `Fluence.Wpf.Controls` - Custom controls and `FluenceWindow`
- `Fluence.Wpf.Enums` - UI enums (card variant, validation, typography, etc.)
- `Fluence.Wpf.Helpers` - Internal helpers (acrylic noise, HSV, OS version, registry)
- `Fluence.Wpf.Native` - Internal P/Invoke and Win32 structures
- XAML themes live under `Fluence.Wpf/Themes/` (not a CLR namespace)

Mapped XML namespace (see `Properties/AssemblyInfo.cs`):

- URI: `http://schemas.fluencewpf.com`
- Suggested prefix: `fluence`

### Resource Dictionary Architecture

Merged dictionary order in `Application.Current.Resources` is stable:

1. `[0]` `Theme.{Light|Dark|HighContrast}.xaml` - color keys only; **swapped** on theme change
2. `[1]` `Accent.xaml` - accent ramp; keys updated in place
3. `[2]` `Brushes.xaml` - `SolidColorBrush` keys referencing colors via `DynamicResource`; reloaded/re-promoted on non-HighContrast theme swaps
4. `[3]` `Typography.xaml` - font resources and text styles
5. `[4]` `Generic.xaml` - merges per-control templates from `Themes/Controls/`

### Control Authoring Patterns

- Subclass the closest `System.Windows.Controls` type (or `Control` / `ContentControl`).
- Override `DefaultStyleKeyProperty` in the static constructor.
- Place templates in `Themes/Controls/<ControlName>.xaml` and merge from `Generic.xaml`.
- Theme-dependent visuals use `DynamicResource`; use `StaticResource` only for immutable template pieces.
- Avoid hardcoded RGB in templates; use WinUI-aligned resource key names.

### Resource Naming

Align with Windows 11 / WinUI theme resources, e.g. `TextFillColorPrimary` → `TextFillColorPrimaryBrush`.

## Coding Standards

### Language & Framework

- **C# 7.3** (no nullable reference types, no ranges, no default interface methods, etc.)
- **.NET Framework 4.7.2** and **.NET 10 Windows**, **WPF**

### License Header

Every `.cs` file must begin with the BSD 3-Clause block used in this repository (see any library source file).

### XML Documentation

Public APIs must have `///` comments; the library builds with `<DocumentationFile>` and **no** CS1591/CS1574 suppression. Use `<inheritdoc />` for overrides when appropriate; document dependency properties with `<see cref="..."/>` on the `*Property` field.

### File Organization

- One primary public type per file when practical.
- Control templates: one XAML file per control under `Themes/Controls/`.

## Common Tasks

### Adding a New Control

1. Add `Controls/<Name>.cs` with `DefaultStyleKeyProperty` and dependency properties.
2. Add `Themes/Controls/<Name>.xaml` and merge in `Themes/Generic.xaml`.
3. Add colors/brushes to Light, Dark, HighContrast (and design-time) dictionaries as needed.
4. Add demo section in `Fluence.Wpf.Demo`.
5. Add tests in `Fluence.Wpf.Tests`.
6. Update `docs/controls.md` (catalog / screenshots note) if the public inventory changes.

### Testing

- Tests use the shared **`WpfTestSta`** STA dispatcher, `Application` with `ShutdownMode.OnExplicitShutdown`, and `[assembly: DoNotParallelize]` on the test assembly.
- Theme tests reset merged dictionaries and call internal `ResetForTesting` helpers (`InternalsVisibleTo` tests assembly).

## Design References

- In-tree precedent in `Fluence.Wpf/Themes/**/*.xaml`, `Fluence.Wpf/Controls/*.cs`, and tests
- WinUI 3 CommonStyles for visual tokens, resource keys, state tables, and control visuals
- .NET WPF theme sources for WPF-native chrome, registry, and DWM interop patterns
- Windows design guidance on Microsoft Learn as a tie-breaker

## Inspirations

Design and implementation ideas are informed by the official WinUI theme resources. Prefer WinUI CommonStyles as the source of truth for token values when in doubt, while translating patterns through WPF primitives compatible with `net472`.
