Fluence.Wpf uses WinUI 3 naming and state behavior for theme resources, implemented as plain WPF. If you already work with WinUI keys, most token names and control roles will look familiar.

## Merge order (application resources)

`Application.Current.Resources.MergedDictionaries` uses a **stable 3-slot layout** after the first `ApplicationThemeManager.Apply`:

| Index | Content                            | On theme or accent change                                    |
| ----- | ---------------------------------- | ------------------------------------------------------------ |
| 0     | Computed colors and brushes        | **Replaced** with a freshly built dictionary on every change |
| 1     | Typography (`Typography.xaml`)     | Loaded once; never replaced                                  |
| 2     | Control templates (`Generic.xaml`) | Loaded once; never replaced                                  |

Slot 0 holds every canonical Color token and its frozen `SolidColorBrush` twin. It is built entirely in C# by `FluenceThemeEngine` each time `Apply` is called; replacing it causes all `DynamicResource` bindings to re-resolve with no promotion step. `Brushes.xaml` and `Accent.xaml` no longer exist; brushes are produced by `BrushFactory` (auto Color-to-Brush twins) and `SpecialBrushes` (gradient elevation borders, High Contrast SystemColors overrides, and brush-only exceptions). The per-theme XAML files (`Themes/Colors/Theme.*.xaml`) are Color-only tables read by C# at build time; they contain no brushes.

Repeated `Apply` calls must not accumulate extra theme dictionaries (`DictionaryStabilityTests` enforces this).

`Typography.xaml` defines the Fluent type ramp as named `TextBlock` styles: `BodyTextBlockStyle`, `BodyStrongTextBlockStyle`, `TitleLargeTextBlockStyle`, and so on. `TextBlockExtensions.Typography` is the compatibility API; it resolves those styles rather than duplicating font metrics in code.

## Rules for XAML and code

- Consume theme and accent brushes with **`DynamicResource`**, not `StaticResource`, so they track live updates.
- Do not hard-code theme colors in control templates; bind to shared keys.
- **High contrast**: brushes are built from live `SystemColors` snapshots in `SpecialBrushes.AddHighContrastBrushes` and published in slot 0 like any other theme. There is no promotion or `_promotedHighContrastBrushKeys` list. A `WM_SETTINGCHANGE` via `SystemThemeWatcher` triggers a re-Apply that refreshes the snapshot.

## Canonical token families

Fluence.Wpf defines the full WinUI 3 token ramp. These are the keys you will reference most often in custom templates:

- **Text**: `TextFillColorPrimary`, `TextFillColorSecondary`, `TextFillColorTertiary`, `TextFillColorDisabled`, `TextOnAccentFillColorPrimary` / `Secondary` / `Disabled`.
- **Fill**: `ControlFillColorDefault`, `ControlFillColorSecondary`, `ControlFillColorTertiary`, `ControlFillColorInputActive`, `ControlFillColorDisabled`, `ControlAltFillColorSecondary` / `Tertiary` / `Quarternary` (CheckBox / RadioButton / ToggleSwitch tracks, `Card` `Filled` variant), `AccentFillColorDefault` / `Secondary` / `Tertiary` / `Disabled`, `SubtleFillColorSecondary` / `Tertiary`, `LayerFillColorDefault`, `CardBackgroundFillColorDefault`.
- **Stroke**: `ControlStrokeColorDefault` / `Secondary`, **`ControlStrongStrokeColorDefault`** (radio / check-box rings), **`ControlStrongStrokeColorDisabled`**, `CardStrokeColorDefault`, `DividerStrokeColorDefault`, `FocusStrokeColorOuter` / `Inner`.
- **Background**: `SolidBackgroundFillColorBase`, `ApplicationBackgroundColor`.
- **Window controls**: `WindowCloseButtonBackgroundPointerOver`, `WindowCloseButtonBackgroundPressed`, `WindowCloseButtonForegroundPointerOver`.
- **High contrast aliases**: `SystemColorWindowTextColorBrush`, `SystemColorWindowColorBrush`, `SystemColorButtonFaceColorBrush`, `SystemColorButtonTextColorBrush`, `SystemColorHighlightColorBrush`, `SystemColorHighlightTextColorBrush`, `SystemColorHotlightColorBrush`, `SystemColorGrayTextColorBrush`. These brush-only aliases map directly to WPF `SystemColors`, so you can preview or bind Windows high contrast roles without hard-coding platform resources.

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

`SystemThemeWatcher.Watch(window)` registers debounced Win32 settings hooks and notifies `ApplicationThemeManager` when the OS theme changes. One watched window per process is the normal setup; `ApplicationThemeManager.Changed` is the event to subscribe to. The same watcher also reacts to a desktop wallpaper change (invalidating the cached wallpaper average and re-running the pipeline) so `NavigationViewContentBackground` (below) stays current without a separate watcher.

## NavigationView content layer (wallpaper-aware)

`NavigationViewContentBackground` is an **opaque** pre-blend, not the translucent WinUI 3 canonical value - WPF cannot honor that translucency because DWM composites it incorrectly above a Mica/Acrylic frame (see `KNOWN_ISSUES.md`, "DWM glass-blend distorts translucent overlays above Mica"). To still track the desktop the way WinUI's Mica-backed content layer does, `ColorMap.Build` recomputes this key at every `Apply` from the desktop wallpaper's average color (`Fluence.Wpf.Helpers.WallpaperTintHelper`):

- `WallpaperTintHelper.GetWallpaperAverageColor()` reads the wallpaper file path (or, for a solid-color desktop, the `HKCU\Control Panel\Colors\Background` value), decodes and averages a small downsample of it, and caches the result until the file changes or the cache is explicitly invalidated.
- `WallpaperTintHelper.EstimateMicaColor` and `PreBlendContent` model how a Mica pane would tint toward that average and pre-blend the theme's translucent content-layer color over it, fitted against measured WinUI 3 Gallery pixel values (see the constants comment in `WallpaperTintHelper.cs`).
- A neutral (achromatic) wallpaper, or a failed read, resolves to exactly the values in `Theme.Light.xaml` / `Theme.Dark.xaml` (`#F9F9F9` / `#272727`) - those XAML values are the fallback, not the runtime value.
- High Contrast is never tinted; `SpecialBrushes.AddHighContrastBrushes` continues to bind this key to the live `SystemColors.Window`.

`WallpaperTintHelper.OverrideAverageColor` is the internal test seam that keeps this deterministic in this repo's own suite (mirrors `MotionHelper.OverrideIsMotionEnabled`): `WpfTestSta.EnsureApplication()` pins it to a neutral color so every existing fixture sees the fallback `#F9F9F9` / `#272727` regardless of the host machine's desktop, and the wallpaper-tint tests override it locally to specific colors and restore it afterward. It is `internal` (visible to `Fluence.Wpf.Tests` only), not a public API a downstream consumer's own test project can set.

## Design-time

`FluenceThemeEngine` computes the full Fluence color and brush set in C# at runtime and publishes it at `MergedDictionaries[0]`. None of those brushes exist as authored XAML, so the XAML designer and Blend cannot resolve `*Brush` keys on their own. To fix the preview, Fluence ships two generated, design-time-only dictionaries that hold the computed palette for the default `#0078D4` accent:

- `Properties/DesignTime.Light.xaml`
- `Properties/DesignTime.Dark.xaml`

The project-wide preview file `Properties/DesignTimeResources.xaml` merges the Light one (plus Typography and Generic), mirroring the runtime 3-slot model so the whole library previews correctly in Light. These files are compiled into the assembly (`Page` build action) and are referenceable at design time by pack URI, for example `pack://application:,,,/Fluence.Wpf;component/Properties/DesignTime.Dark.xaml`. Nothing merges them at runtime.

To preview **Dark**, add a design-time-only merge of `DesignTime.Dark.xaml` (under `mc:Ignorable="d"` / the `d:` namespace) to the specific window or page you are previewing.

These files are a serialized snapshot of the engine output, kept honest by a unit test: `DesignTimeResources_AreCurrent` regenerates each file in memory and fails CI if the committed file drifts. After an intentional engine change that affects colors or brushes, run the (normally `[Ignore]`d) `RegenerateDesignTimeResources` test to rewrite both files, then re-commit. The snapshot is deterministic and machine-independent: it forces the default accent through the HSV ramp generator (no registry / DWM read), uses the default theme title-bar colors for the window-chrome tokens, and omits the live-`SystemColors` `SystemColor*` aliases, the runtime-only `AcrylicNoiseBrush`, the flyout shadow effect, and the focus-visual styles. High contrast is out of scope for design-time previews. The XAML designer and runtime merge stacks are not identical - always check the result in the demo app.

## Testing

The test suite runs a full theme cycle (Light → Dark → High Contrast → Light → Auto) and asserts that critical brushes resolve at each step. See `ThemeTestHelpers.ApplyStandardThemeCycle` and `AssertKeyThemeBrushesResolve` in `Fluence.Wpf.Tests`. The `ControlStrongStrokeColor*` contract is covered by `ControlTests.FluentStroke.cs`.
