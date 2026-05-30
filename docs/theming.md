---
title: Theming
linkTitle: Theming
description: Dictionary slots, canonical token families, accent ramps, backdrops, and high contrast in Fluence.Wpf.
weight: 20
---

Fluence.Wpf uses WinUI 3 naming and state behavior for theme resources and is implemented as plain WPF. If you already work with WinUI keys, most token names and control roles will look familiar.

## Merge order (application resources)

`Application.Current.Resources.MergedDictionaries` uses a **stable 3-slot layout** after the first `ApplicationThemeManager.Apply`:

| Index | Content                             | On theme or accent change                                      |
|-------|-------------------------------------|----------------------------------------------------------------|
| 0     | Computed colors and brushes         | **Replaced** with a freshly built dictionary on every change   |
| 1     | Typography (`Typography.xaml`)      | Loaded once; never replaced                                    |
| 2     | Control templates (`Generic.xaml`)  | Loaded once; never replaced                                    |

Slot 0 holds every canonical Color token and its frozen `SolidColorBrush` twin. It is built entirely in C# by `FluenceThemeEngine` each time `Apply` is called; replacing it causes all `DynamicResource` bindings to re-resolve with no promotion step. `Brushes.xaml` and `Accent.xaml` no longer exist; brushes are produced by `BrushFactory` (auto Color-to-Brush twins) and `SpecialBrushes` (gradient elevation borders, High Contrast SystemColors overrides, and brush-only exceptions). The per-theme XAML files (`Themes/Colors/Theme.*.xaml`) are Color-only tables read by C# at build time; they contain no brushes.

Repeated `Apply` calls must not accumulate extra theme dictionaries (`DictionaryStabilityTests` enforces this).

`Typography.xaml` defines the Fluent type ramp as named `TextBlock` styles: `BodyTextBlockStyle`, `BodyStrongTextBlockStyle`, `TitleLargeTextBlockStyle`, and so on. `TextBlockExtensions.Typography` is the compatibility API; it resolves those styles rather than duplicating font metrics in code.

## Rules for XAML and code

- Consume theme and accent brushes with **`DynamicResource`**, not `StaticResource`, so they track live updates.
- Do not hard-code theme colors in control templates; bind to shared keys.
- **High contrast**: brushes are built from live `SystemColors` snapshots in `SpecialBrushes.AddHighContrastBrushes` and published in slot 0 like any other theme. There is no promotion or `_promotedHighContrastBrushKeys` list. A `WM_SETTINGCHANGE` via `SystemThemeWatcher` triggers a re-Apply that refreshes the snapshot.

## Canonical token families

Fluence.Wpf defines the full WinUI 3 token ramp. These are the keys most commonly referenced in custom templates:

- **Text**: `TextFillColorPrimary`, `TextFillColorSecondary`, `TextFillColorTertiary`, `TextFillColorDisabled`, `TextOnAccentFillColorPrimary` / `Secondary` / `Disabled`.
- **Fill**: `ControlFillColorDefault`, `ControlFillColorSecondary`, `ControlFillColorTertiary`, `ControlFillColorInputActive`, `ControlFillColorDisabled`, `AccentFillColorDefault` / `Secondary` / `Tertiary` / `Disabled`, `SubtleFillColorSecondary` / `Tertiary`, `LayerFillColorDefault`, `CardBackgroundFillColorDefault`.
- **Stroke**: `ControlStrokeColorDefault` / `Secondary`, **`ControlStrongStrokeColorDefault`** (radio / check-box rings), **`ControlStrongStrokeColorDisabled`**, `CardStrokeColorDefault`, `DividerStrokeColorDefault`, `FocusStrokeColorOuter` / `Inner`.
- **Background**: `SolidBackgroundFillColorBase`, `ApplicationBackgroundColor`.
- **Window controls**: `WindowCloseButtonBackgroundPointerOver`, `WindowCloseButtonBackgroundPressed`, `WindowCloseButtonForegroundPointerOver`.
- **High contrast aliases**: `SystemColorWindowTextColorBrush`, `SystemColorWindowColorBrush`, `SystemColorButtonFaceColorBrush`, `SystemColorButtonTextColorBrush`, `SystemColorHighlightColorBrush`, `SystemColorHighlightTextColorBrush`, `SystemColorHotlightColorBrush`, `SystemColorGrayTextColorBrush`. These brush-only aliases map directly to WPF `SystemColors` so consumers can preview or bind Windows high contrast roles without hard-coding platform resources.

Each color token has a matching `*Brush` frozen `SolidColorBrush` - for example `ControlStrongStrokeColorDefaultBrush` - produced by `BrushFactory`. Reference the brush keys from XAML, not the raw color keys.

## Accent

- `ApplicationAccentColorManager.ApplySystemAccent()` - sets the accent intent to System and re-runs the full pipeline, resolving the OS registry palette.
- `ApplicationAccentColorManager.ApplyCustomAccent(Color)` - sets the accent intent to a fixed color; the ramp is generated to WinUI-style keys (`SystemAccentColorPrimary` / `Secondary` / `Tertiary` plus the `AccentFillColor*` role tokens).
- `ApplicationThemeManager.Apply(theme)` alone uses the OS palette by default - no separate `ApplySystemAccent()` call is needed on startup.
- Accent changes re-run the full pipeline and replace slot [0]; `DynamicResource` consumers refresh automatically.

## Backdrop (`FluenceWindow`)

`BackdropType`: `None`, `Auto`, `Mica`, `Acrylic`, `Tabbed`.

Which backdrops work depends on OS support. Mica and Tabbed require Windows 11; Acrylic is available on Windows 10 1809+. Unsupported combinations fall back silently per `FluenceWindow` / `SystemBackdropType` logic.

## System theme watcher

`SystemThemeWatcher.Watch(window)` registers debounced Win32 settings hooks and notifies `ApplicationThemeManager` when the OS theme changes. One watched window per process is the normal setup; `ApplicationThemeManager.Changed` is the event to subscribe to.

## Design-time

`DesignTime.xaml` ships with Fluence and is merged under `d:DataContext` scenarios so the designer can resolve resource keys. It assumes Light theme with `#0078D4` accent. The XAML designer and runtime merge stacks are not identical - always check the result in the demo app.

## Testing

The test suite runs a full theme cycle (Light → Dark → High Contrast → Light → Auto) and asserts that critical brushes resolve at each step. See `ThemeTestHelpers.ApplyStandardThemeCycle` and `AssertKeyThemeBrushesResolve` in `Fluence.Wpf.Tests`. The `ControlStrongStrokeColor*` contract is covered by `ControlTests.FluentStroke.cs`.
