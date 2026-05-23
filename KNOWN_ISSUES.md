# Known issues and follow-ups

This file tracks optional follow-ups and deliberate non-features. Filed bugs with
reproductions live on the issue tracker; this is the consolidated view for
maintainers.

## Current follow-ups (not defects)

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
