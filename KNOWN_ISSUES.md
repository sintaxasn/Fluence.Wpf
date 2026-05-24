# Known issues and follow-ups

This file tracks optional follow-ups and deliberate non-features. Filed bugs with
reproductions live on the issue tracker; this is the consolidated view for
maintainers.

## Current follow-ups (not defects)

- **OS-transform modeling for the accent ramp** - The Candidate F HSV ramp
  algorithm in `HsvColorHelper` mirrors what Windows produces for
  `SystemAccentColor -> Light1..Dark3` on the verbatim base inputs we
  exercised. Boundary-probe captures showed that the OS aggressively
  projects arbitrary user input into a Fluent-compatible subspace
  (see `Fluence.Wpf.Tests/AccentRampScoreboard.cs` and the
  `accent-capture-*.txt` artifacts under `docs/_internal/theme-rewrite/`).
  We deliberately do not mirror that projection - we use the user-supplied
  base verbatim - which means `ApplyCustomAccent(Color)` outputs diverge
  from what the OS would produce if the same color were set as the system
  accent. Re-evaluating whether we want to mirror the OS transform (or
  expose a knob to opt in) is deferred until consumers report a divergence
  that matters in practice.
- **Full `Theme.*` canonical rewrite + `Themes/Shared.xaml` split** - The
  three theme dictionaries (`Theme.Light.xaml`, `Theme.Dark.xaml`,
  `Theme.HighContrast.xaml`) currently each repeat the full canonical key
  set with theme-specific values. Splitting the theme-independent keys
  (typography time spans, token aliases, etc.) into a shared
  `Themes/Shared.xaml` and trimming each theme dictionary to only the
  values that actually differ would reduce duplication and make canonical
  drift visible at review time. Deferred - the current duplication does
  not produce defects, just maintenance friction.
- **`TabView` drag-to-reorder** - `TabView` / `TabViewItem` ship with closable
  tabs, an add-tab button, per-tab icons, overflow scroll, and width / overlay
  modes. Drag-and-drop tab reordering (including cross-window tear-off) is **not**
  implemented; consumers that need it should handle `PreviewMouseMove` / drag-drop
  themselves. This is the main remaining gap vs. WinUI 3 `TabView`.
- **Navigation back-stack** - `NavigationView.IsBackButtonVisible` +
  `IsBackEnabled` + `BackRequested` are exposed, but the library does **not**
  track page history. The demo does not use the back button; consumers are
  expected to own their own back stack and route `BackRequested`.
- **Per-control screenshots** - `docs/screenshots/` now contains
  `banner-{light|dark|highcontrast}-{1|1.5}x.png`, regenerated via the opt-in
  `GalleryScreenshotHarness` (`FLUENCE_CAPTURE_SCREENSHOTS=1`). Per-control
  captures (buttons, inputs, navigation, etc.) at 100% / 150% are still pending
  and can reuse the same harness by pointing it at a different demo page.
- **`RenderTargetBitmap` vs DWM backdrop** - DWM Mica / Acrylic is composed by
  the window manager and is **not** visible to `RenderTargetBitmap`. The
  screenshot harness therefore hosts the gallery inside a plain `Window` with a
  solid `SolidBackgroundFillColorBaseBrush`. Any future automated capture of the
  full `FluenceWindow` chrome will need a different approach (e.g. `PrintWindow`
  / GDI screen capture).

## Resolved (Unreleased)

- **Theme & accent system rewrite (Units 4 - 9)** - Rebuilt the theme,
  accent, backdrop, and window-chrome machinery around canonical WinUI 3
  + .NET 10 WPF Fluent references:
  - `ApplicationThemeManager` ResolveTheme dual-fallback + sentinel
    idempotency.
  - `SystemThemeWatcher` filtered on `ImmersiveColorSet` (fires once per
    logical OS theme change, not per setting churn).
  - Candidate F HSV accent ramp in `HsvColorHelper`; removed the static
    `KnownAccentRamps` lookup and the opportunistic system-palette path.
  - Resource-dictionary refresh across `Theme.{Light,Dark,HighContrast}`
    + `Brushes.xaml` + `Accent.xaml` against
    `Common_themeresources_any.xaml`.
  - New `WindowBackdrop` orchestrator (`WindowPolicy.BuildBackdropPlan`)
    + Snap Layout coordination (`SnapLayoutHelper`); `FluenceWindow`
    consumes both.
  - `AccentFillBackdrop` opaque sub-layer: introduced on `ToggleSwitch`
    (Unit 9.1) then extended to `Button`, `DropDownButton`,
    `ToggleButton`, `SplitButton` (per-half), `CheckBox`, `RadioButton`,
    and the `Slider` thumb so accent fills composite predictably against
    translucent card / Mica surfaces.
- **`NavigationView` canonical surface roles + sizing** - Pane background
  switched to `AcrylicInAppFillColorDefaultBrush`
  (canonical `NavigationViewDefaultPaneBackground`); content host uses
  `LayerFillColorDefault` (`#4C3A3A3A` dark / `#80FFFFFF` light) instead
  of the previous flat 65 - 69 %-opaque Fluence-only tint that was
  blocking Mica. Open pane width set to the canonical 320 px (was 280).
  `NavigationViewItem` font bumped to 14 pt (was 13). Footer slot draws a
  `DividerStrokeColorDefault` separator above it (matches WinUI 3 Gallery).
- **`ProgressBar` template** - Removed vestigial `BorderThickness` style
  setter; corrected the unfilled-track `Background` from
  `ControlStrokeColorDefaultBrush` (a stroke role) to
  `ControlStrongStrokeColorDefaultBrush` (the canonical fill role); track
  thickness 6 px with 3 px corner radius (full pill) to match the
  WinUI 3 Gallery visual.
- **Text-rendering policy** - `FluenceWindow` no longer forces
  `RenderOptions.ClearTypeHint=Enabled` at the window root. The WPF
  default (`Auto`) picks ClearType subpixel anti-aliasing on opaque
  surfaces and grayscale AA on translucent surfaces (Mica / Acrylic,
  `AccentFillBackdrop` layers) per surface, which is what .NET 10 WPF
  Fluent does. The `TextRenderingPolicyTests` invariants (no
  `TextOptions.*` in production sources, `SnapsToDevicePixels` only on
  `FluenceWindow.xaml`) are preserved and aligned to the new
  `ClearTypeHint.Auto` assertion.
- **`TitleBar` canonical sizing** - App-title text moved from
  `CaptionTextBlockStyle` (12 pt) to `BodyTextBlockStyle` (14 pt); app
  icon shrunk from 24 x 24 to 20 x 20 with balanced 8 / 12 px margins
  vs the previous 4 / 20.
- **Demo gallery home page** - Cards rewritten to the standard
  `Card.Header` / `Card.Icon` contract (matching `GalleryDataPage`'s
  Variant samples) instead of the previous nested-StackPanel
  reimplementation; card glyphs use `AccentFillColorDefaultBrush`
  (saturated accent). `SettingsRowTitleStyle` -> `BodyStrongTextBlockStyle`
  (14 pt SemiBold) and `SettingsRowDescriptionStyle` ->
  `CaptionTextBlockStyle` (12 pt) match canonical WinUI 3 `SettingsCard`
  text sizing.
- **WinUI `TabView` parity (MVP)** - `Fluence.Wpf.Controls.TabView` /
  `TabViewItem` now ship with WinUI 3 close buttons (`CloseRequested` ->
  `TabCloseRequested` bubbling), add-tab button (`AddTabButtonClick`), per-tab
  icons, `TabWidthMode` (`SizeToContent` / `Equal` / `Compact`),
  `CloseButtonOverlayMode` (`Auto` / `OnPointerOver` / `Always`), and horizontal
  overflow scroll. A "Tabs" page in the demo gallery exercises both `TabControl`
  and `TabView`, and `TabViewTests.cs` covers the new public surface.

## Resolved (0.3.0)

- **Radio / checkbox ring visibility** - Outer ring now uses
  `ControlStrongStrokeColorDefaultBrush` (and
  `ControlStrongStrokeColorDisabledBrush` on `IsEnabled="False"`), matching
  WinUI 3 canonical values (#72000000 in Light, #8BFFFFFF in Dark).
- **NavigationView Left layout** - `Left` / `LeftCompact` templates center icons
  in a 48 px pane, stack the pane toggle above the back button, and the content
  region draws a 1 px top/left `CardStrokeColorDefault` border with an 8,0,0,0
  corner radius that hugs the top-left - matching `Common_themeresources_any.xaml`.
- **Clickable cards** - `Fluence.Wpf.Controls.Card` exposes `IsClickable`,
  `IsPressed`, and a `Click` routed event; the demo home page drives navigation
  with it.
- **Search in title bar** - Demo `MainWindow` hosts the search box inside
  `FluenceWindow.TitleBar` and filters `NavigationView` items live; no per-page
  back-stack is kept.
- **Repo folder rename** - The repository root is now `Fluence.Wpf`; the earlier
  `New11` rename note has been retired.
- **XML documentation** - All public members in `Fluence.Wpf` have `///`
  comments; the csproj no longer suppresses `CS1591` / `CS1574`.
