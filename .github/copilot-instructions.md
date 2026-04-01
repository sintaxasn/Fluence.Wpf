# Fluence.Wpf — AI Coding Instructions

This file provides context for GitHub Copilot, Cursor, and other AI coding assistants working on this project.

## Project Overview

Fluence.Wpf is a WPF control library that recreates Windows 11 Fluent Design (WinUI 3) controls and theming for **.NET Framework 4.7.2** applications. It targets Windows 10 1809+ with enhanced features on Windows 11.

## Architecture

### Solution Structure

- `Fluence.Wpf/` — Core class library (WPF)
- `Fluence.Wpf.Demo/` — Control gallery (WPF executable)
- `Fluence.Wpf.Tests/` — MSTest suite (`Microsoft.NET.Test.Sdk`, MSTest 3.2)

### Namespace Layout

- `Fluence.Wpf` — `ApplicationThemeManager`, `ApplicationAccentColorManager`, `SystemThemeWatcher`, theme enums
- `Fluence.Wpf.Controls` — Custom controls and `FluentWindow`
- `Fluence.Wpf.Enums` — UI enums (card variant, validation, typography, etc.)
- `Fluence.Wpf.Helpers` — Internal helpers (acrylic noise, HSV, OS version, registry)
- `Fluence.Wpf.Native` — Internal P/Invoke and Win32 structures
- XAML themes live under `Fluence.Wpf/Themes/` (not a CLR namespace)

Mapped XML namespace (see `Properties/AssemblyInfo.cs`):

- URI: `http://schemas.fluencewpf.com`
- Suggested prefix: `fluence`

### Resource Dictionary Architecture

Merged dictionary order in `Application.Current.Resources` is stable:

1. `[0]` `Theme.{Light|Dark|HighContrast}.xaml` — color keys only; **swapped** on theme change
2. `[1]` `Accent.xaml` — accent ramp; keys updated in place
3. `[2]` `Brushes.xaml` — `SolidColorBrush` keys referencing colors via `DynamicResource`
4. `[3]` `Typography.xaml` — font resources and text styles
5. `[4]` `Generic.xaml` — merges per-control templates from `Themes/Controls/`

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
- **.NET Framework 4.7.2**, **WPF**

### License Header

Every `.cs` file must begin with the BSD 3-Clause block used in this repository (see any library source file).

### XML Documentation

Public APIs should have `///` comments. The project may suppress CS1591 until coverage is complete — prefer adding real summaries over permanent suppression.

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
6. Update `docs/CONTROL_COVERAGE.md`.

### Testing

- Tests use STA threads and `Application` with `ShutdownMode.OnExplicitShutdown`.
- Theme tests reset merged dictionaries and call internal `ResetForTesting` helpers (`InternalsVisibleTo` tests assembly).

## Design References

- [WinUI 3 controls](https://learn.microsoft.com/en-us/windows/apps/design/controls/)
- [Windows 11 color](https://learn.microsoft.com/en-us/windows/apps/design/style/color)

## Inspirations

Design and implementation ideas are informed by community projects such as [WPF-UI](https://github.com/lepoco/wpfui), [MicaWPF](https://github.com/Jeremyforever/MicaWPF), and official WinUI theme resources. Prefer WinUI’s `themeresources.xaml` as the source of truth for token values when in doubt.
