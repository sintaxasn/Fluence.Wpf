# Theming

Design reference: **WinUI 3** resource key names and state behaviour. Fluence.Wpf targets WPF, not WinUI, but keys, state tables, and brush roles mirror `Common_themeresources.xaml` so XAML you read in the WinUI source tree maps one-to-one.

## Merge order (application resources)

`Application.Current.Resources.MergedDictionaries` uses a **stable slot layout** after the first `ApplicationThemeManager.Apply`:

| Index | Content                                                            | On theme change                                       |
|-------|--------------------------------------------------------------------|-------------------------------------------------------|
| 0     | Theme colors (`Theme.Light` / `Theme.Dark` / `Theme.HighContrast`) | **Replaced** (the manager swaps only this dictionary) |
| 1     | Accent ramp (`Accent.xaml`)                                        | Loaded once; **values updated in place**              |
| 2     | Brushes (`Brushes.xaml`)                                           | Loaded once; reloaded and re-promoted on non-HC swaps |
| 3     | Typography (`Typography.xaml`)                                     | Loaded once                                           |
| 4     | Control templates (`Generic.xaml`)                                 | Loaded once                                           |

There must be **no accumulation** of extra theme dictionaries on repeated `Apply` calls (`DictionaryStabilityTests` enforces this). On a non-HighContrast theme swap, `ApplicationThemeManager` reloads `Brushes.xaml` and promotes those brush keys again so `DynamicResource` chains on `Freezable` values re-evaluate.

`Typography.xaml` owns the Fluent type ramp through named `TextBlock` styles such as `BodyTextBlockStyle`, `BodyStrongTextBlockStyle`, and `TitleLargeTextBlockStyle`. `TextBlockExtensions.Typography` remains the compatibility API, but it resolves those styles instead of duplicating font metrics in code.

## Rules for XAML and code

- Consume theme and accent brushes with **`DynamicResource`**, not `StaticResource`, so they track live updates.
- Do not hard-code theme colors in control templates; bind to shared keys.
- **High contrast**: the theme slot may promote certain brush keys so system colors take precedence over static fallbacks - see `ApplicationThemeManager` for the exact promotion behavior.

## Canonical token families

Fluence.Wpf defines the full WinUI 3 token ramp; these are the ones most commonly referenced in custom templates:

- **Text**: `TextFillColorPrimary`, `TextFillColorSecondary`, `TextFillColorTertiary`, `TextFillColorDisabled`, `TextOnAccentFillColorPrimary` / `Secondary` / `Disabled`.
- **Fill**: `ControlFillColorDefault`, `ControlFillColorSecondary`, `ControlFillColorTertiary`, `ControlFillColorInputActive`, `ControlFillColorDisabled`, `AccentFillColorDefault` / `Secondary` / `Tertiary` / `Disabled`, `SubtleFillColorSecondary` / `Tertiary`, `LayerFillColorDefault`, `CardBackgroundFillColorDefault`.
- **Stroke**: `ControlStrokeColorDefault` / `Secondary`, **`ControlStrongStrokeColorDefault`** (radio / check-box rings), **`ControlStrongStrokeColorDisabled`**, `CardStrokeColorDefault`, `DividerStrokeColorDefault`, `FocusStrokeColorOuter` / `Inner`.
- **Background**: `SolidBackgroundFillColorBase`, `ApplicationBackgroundColor`.
- **Window chrome**: `WindowCloseButtonBackgroundPointerOver`, `WindowCloseButtonBackgroundPressed`, `WindowCloseButtonForegroundPointerOver`.

Each color token has a matching `*Brush` resource in `Brushes.xaml` (for example `ControlStrongStrokeColorDefaultBrush`) that binds back to the color via `DynamicResource` - consume the brush keys from XAML, not the raw color keys.

## Accent

- `ApplicationAccentColorManager.ApplySystemAccent()` - follow Windows accent.
- `ApplicationAccentColorManager.ApplyCustomAccent(Color)` - custom base; the ramp is derived to WinUI-style keys (`SystemAccentColorPrimary` / `Secondary` / `Tertiary` plus the `AccentFillColor*` role tokens).
- Accent changes update the accent ramp **in place** (slot `[1]`); `DynamicResource` consumers refresh automatically.

## Backdrop (`FluenceWindow`)

`BackdropType`: `None`, `Auto`, `Mica`, `Acrylic`, `Tabbed`.

Behavior depends on OS support; unsupported combinations fall back per `FluenceWindow` / `SystemBackdropType` logic. Mica and Tabbed require Windows 11; `Acrylic` falls back on Windows 10 1809+.

## System theme watcher

`SystemThemeWatcher.Watch(window)` registers debounced Win32 settings hooks and coordinates with `ApplicationThemeManager` so resource updates stay coherent. Prefer **one** watched window per process unless you have a clear reason to register more; `ApplicationThemeManager.Changed` is the single event consumers should listen to.

## Design-time

`DesignTime.xaml` ships with Fluence and is merged under `d:DataContext` scenarios so the designer can resolve the same keys the runtime does. It assumes Light theme with `#0078D4` accent. Do **not** assume the XAML designer and runtime merge stacks resolve identically - always smoke-test in the demo.

## Testing

The test suite applies a **full theme cycle** (Light → Dark → High Contrast → Light → Auto) and asserts critical brushes resolve. See `ThemeTestHelpers.ApplyStandardThemeCycle` and `AssertKeyThemeBrushesResolve` in `Fluence.Wpf.Tests`. The `ControlStrongStrokeColor*` contract is covered by `ControlTests.FluentStroke.cs`.
