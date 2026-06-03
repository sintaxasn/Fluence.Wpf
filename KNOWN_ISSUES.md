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
- **Screenshots regenerate on every full test run** - The
  `GalleryScreenshotHarness` capture tests are plain `[TestMethod]`s (no opt-in
  env var), so a normal full `dotnet test` overwrites `docs/screenshots/`:
  `banner-{light|dark|highcontrast}-{1|1.5}x.png` plus per-page `gallery/` and
  `apps/` captures (Light + Dark). Expect ~40 regenerated PNGs in the working
  tree after any full run; review and stage them deliberately. Per-control
  close-up captures (single buttons, inputs, etc.) at 100% / 150% are still
  pending and can reuse the same harness pointed at a narrower surface.
- **`RenderTargetBitmap` vs DWM backdrop** - DWM Mica / Acrylic is composed by
  the window manager and is **not** visible to `RenderTargetBitmap`. The
  screenshot harness hosts the gallery inside a plain `Window` with a solid
  `SolidBackgroundFillColorBaseBrush`. Automated capture of the full
  `FluenceWindow` chrome needs a different approach (e.g. `PrintWindow` /
  GDI screen capture).
