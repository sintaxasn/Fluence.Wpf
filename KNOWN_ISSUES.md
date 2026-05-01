# Known issues and follow-ups

This file tracks optional follow-ups and deliberate non-features. Filed bugs with reproductions live on the issue tracker; this is the consolidated view for maintainers.

## Current follow-ups (validated 2026-05-01)

- **`TabView` drag-to-reorder** - `TabView` / `TabViewItem` ship with closable tabs, an add-tab button, per-tab icons, overflow scroll, and width / overlay modes.
  - **Status:** Valid non-feature. Drag-and-drop tab reordering and cross-window tear-off are not implemented.
  - **Evidence:** `TabView.cs` exposes add/close/scroll events and container generation, but no `AllowDrop`, drag-event, or reorder logic exists in `Fluence.Wpf/Controls/TabView*.cs`; `TabViewTests.cs` covers close/add/visibility behavior only.
  - **Plan:** Add as a separate feature: define whether the API is item-reordering only or also tear-off; add routed reorder events and keyboard-accessible move commands; test item-source and direct-item modes; visually verify reorder insertion cues against WinUI 3 guidance.
- **Navigation back-stack** - `NavigationView.IsBackButtonVisible` + `IsBackEnabled` + `BackRequested` are exposed, but the library does **not** track page history.
  - **Status:** Valid deliberate design boundary. Consumers own page routing and history.
  - **Evidence:** `NavigationView.cs` raises `BackRequested`; the gallery `MainWindow` drives navigation from `DemoNavigationCatalog` and selected items without a journal/back-stack type.
  - **Plan:** Keep library behavior unchanged unless a future request asks for a history helper. If added, implement it as an optional demo/service layer component rather than coupling `NavigationView` to page lifetime.
- **Per-control screenshots** - `docs/screenshots/` contains `banner-{light|dark|highcontrast}-{1|1.5}x.png`, regenerated via the opt-in `GalleryScreenshotHarness` (`FLUENCE_CAPTURE_SCREENSHOTS=1`).
  - **Status:** Valid documentation backlog. Per-control captures at 100 % / 150 % are still pending.
  - **Evidence:** `GalleryScreenshotHarness.cs` only writes `banner-*` images; `docs/controls.md` documents the banner capture workflow and notes that per-control screenshots remain under `docs/images/`.
  - **Plan:** Extend the harness with named page/control capture targets, write deterministic filenames under `docs/images/`, and update `docs/controls.md` with capture commands and image references.
- **`RenderTargetBitmap` vs DWM backdrop** - DWM Mica / Acrylic is composed by the window manager and is **not** visible to `RenderTargetBitmap`.
  - **Status:** Valid technical limitation.
  - **Evidence:** `GalleryScreenshotHarness.cs` hosts `GalleryHomePage` in a plain `Window` with `SolidBackgroundFillColorBaseBrush`, and comments there document why `FluenceWindow` DWM backdrops are excluded.
  - **Plan:** Keep `RenderTargetBitmap` for control-surface captures. If full chrome/backdrop screenshots are required, add a separate Windows-only capture path using `PrintWindow`/GDI or a screen-capture helper and gate it behind an explicit environment variable.
- **TreeView large-data virtualization** - `TreeView` currently favors smooth pixel scrolling over container virtualization.
  - **Status:** Valid performance follow-up, not a correctness defect for current demo-scale trees.
  - **Evidence:** `Themes/Controls/TreeView.xaml` sets `CanContentScroll="False"` and does not enable `VirtualizingPanel.IsVirtualizing`; this is a known WPF virtualization breaker for large item counts. `ListView.xaml` and `ListBox.xaml` already enable recycling virtualization, so the gap is TreeView-specific.
  - **Plan:** Add a focused TreeView virtualization spike: test large hierarchical item counts, decide whether smooth scrolling or virtualization wins by default, and consider an opt-in style/resource key for virtualized trees if changing the default would alter existing scrolling behavior.

## Resolved (Unreleased)

- **WinUI `TabView` parity (MVP)** - `Fluence.Wpf.Controls.TabView` / `TabViewItem` now ship with WinUI 3 close buttons (`CloseRequested` → `TabCloseRequested` bubbling), add-tab button (`AddTabButtonClick`), per-tab icons, `TabWidthMode` (`SizeToContent` / `Equal` / `Compact`), `CloseButtonOverlayMode` (`Auto` / `OnPointerOver` / `Always`), and horizontal overflow scroll. A "Tabs" page in the demo gallery exercises both `TabControl` and `TabView`, and `TabViewTests.cs` covers the new public surface.

## Resolved (0.3.0)

- **Radio / checkbox ring visibility** - Outer ring now uses `ControlStrongStrokeColorDefaultBrush` (and `ControlStrongStrokeColorDisabledBrush` on `IsEnabled="False"`), matching WinUI 3 canonical values (#72000000 in Light, #8BFFFFFF in Dark).
- **NavigationView Left layout** - `Left` / `LeftCompact` templates center icons in a 48 px pane, stack the pane toggle above the back button, and the content region draws a 1 px top/left `CardStrokeColorDefault` border with an 8,0,0,0 corner radius that hugs the top-left - matching `Common_themeresources_any.xaml`.
- **Clickable cards** - `Fluence.Wpf.Controls.Card` exposes `IsClickable`, `IsPressed`, and a `Click` routed event; the demo home page drives navigation with it.
- **Search in title bar** - Demo `MainWindow` hosts the search box inside `FluenceWindow.TitleBar` and filters `NavigationView` items live; no per-page back-stack is kept.
- **Repo folder rename** - The repository root is now `Fluence.Wpf`; the earlier `New11` rename note has been retired.
- **XML documentation** - All public members in `Fluence.Wpf` have `///` comments; the csproj no longer suppresses `CS1591` / `CS1574`.
