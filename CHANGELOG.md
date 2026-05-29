# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- `NavigationView`: the Settings (pane-footer) icon no longer drifts sideways while the left pane opens or collapses. The footer sits in a `ContentPresenter`, which sizes its child to content and centres it, whereas the menu items use a `StackPanel`, which stretches them. So the collapsed icon-only item slid across the pane as its width animated. Setting `HorizontalAlignment="Left"` on the footer `ContentPresenter` in the `Left` and `LeftCompact` templates keeps the icon in the same column as the menu icons. Covered by `DemoMainWindow_LeftPaneFooterIcon_StaysLeftAnchored_WhileCollapsed`.
- `FluenceWindow` no longer flashes a black client area for the first frame or two when it opens. WPF makes the window visible before its first frame is painted, and with the glass frame, a DWM backdrop, and native caption painting suppressed, DWM filled that gap with black. The window now cloaks itself (`DWMWA_CLOAK`) in `OnSourceInitialized` before the backdrop is applied and uncloaks after the first paint, on `ContentRendered`. A `DispatcherPriority.ContextIdle` fallback guarantees it can never stay hidden, and the whole thing is skipped when DWM composition is off. Adds `NativeMethods.SetWindowCloak` / `GetWindowCloakedState` and the `DWMWA_CLOAK` / `DWMWA_CLOAKED` constants.
- `FluenceWindow` immersive dark-mode now picks the right DWM attribute by OS build: attribute 19 (`DWMWA_USE_IMMERSIVE_DARK_MODE_OLD`) on Windows 10 builds 17763-18361 (1809), attribute 20 on build 18362+ (1903 and later, including Windows 11). The previous code used attribute 20 unconditionally, so the caption stayed light on early Windows 10 builds. Selection lives in the pure, testable `NativeMethods.GetImmersiveDarkModeAttribute(osBuild)`.
- `FluenceWindow` maximized windows now respect an auto-hidden taskbar. When a monitor's work area covers the full monitor (no reserved taskbar space) and a taskbar edge is set to auto-hide, the maximized rectangle is shifted 2 px on that edge in `WM_GETMINMAXINFO` so the taskbar still reveals on hover. New `NativeMethods.GetAutoHideTaskbarEdge` (via `SHAppBarMessage`) and the pure `NativeMethods.ApplyAutoHideTaskbarShift` back this.
- `FluenceWindow` no longer pins itself to the static theme managers when constructed but never shown. The `ApplicationThemeManager.Changed` and `ApplicationAccentColorManager.AccentColorChanged` subscriptions moved from the instance constructor to `OnSourceInitialized`, paired with the existing `OnClosed` unsubscribe. Only realised windows subscribe, and every subscription is released on close.
- `TreeView` outer border now clips to a `ControlCornerRadius` rounded corner (`ClipToBounds="True"`), so item hover highlights no longer paint past the rounded edge.
- `ProgressBar` indeterminate mode no longer renders square ends. The track now installs a rounded `RectangleGeometry` clip matching `CornerRadius`, kept in sync on size and layout changes, instead of relying on `ClipToBounds`, which clips only to the rectangular bounds. The translating indeterminate bars deliberately overshoot the track edges, so without the rounded clip their square mid-sections showed at the track boundary. They now follow the rounded track on every animation frame, matching the determinate fill.
- `NavigationView` left-pane item icons no longer shift horizontally when the pane collapses. The open-state `PART_PaneItemsScrollViewer` and `PaneFooterHost` left padding is now 0 (matching the collapsed state), so icons stay on a single vertical column - centered in the 48px compact rail - across open, collapsed, and compact, and are no longer clipped in the compact rail.
- `NavigationView` shared selection indicator now sits just inside the selected item's rounded border (horizontal offset 9, was 4) instead of floating in the pane to the left of the item, with no padding gap, in both expanded and compact states.
- `TitleBar` glyph buttons (back / pane toggle) no longer apply a legacy 4px rightward nudge to their glyph, so the extended-title-bar navigation chrome aligns with the NavigationView icon rail.

### Changed

- `NavigationView`: removed the divider line above the pane footer in the `Left` and `LeftCompact` templates, so the footer (e.g. the Settings item) sits directly in the pane. Also dropped the now-unused `BorderBrush` from `PaneFooterHost`.
- `WindowPolicy.CreateWindowChrome` no longer takes a `captionHeight` parameter. The caller always reset `CaptionHeight` to 0 immediately afterward, so the parameter never had effect; `CaptionHeight` is now hard-coded to 0 in the helper.

## [0.6.0-preview] - 2026-05-24

### Changed

- Widened the accent ramp spread in `HsvColorHelper.GenerateAccentRampWinaccent`. The previous Candidate F calibration produced near-base stops only 4-7 % away from the base on the L axis, so adjacent ramp rungs were hard to tell apart in control templates. New deltas are ~10-12 % per adjacent step (Light1 +12 %, Light2 +24 %, Light3 +36 %, Dark1 -10 %, Dark2 -20 %, Dark3 -30 %), so controls that reference different rungs for hover / pressed / focus states now read as distinct. The decision to use the user-supplied base verbatim instead of mirroring the Windows perceptual projection still stands; this is purely a spread adjustment, not an OS-transform model.
- Demo `DemoSampleSourceExpanderStyle` background switched to `SolidBackgroundFillColorQuarternaryBrush` so the collapsed "Source code" header strip reads as a distinct dark band beneath the sample card (matches the WinUI Gallery visual reference).

### Added

- `Themes/Shared.xaml` - a new merge slot (`[5]`, loaded once, never replaced) holding theme-independent Color tokens that are identical across Light, Dark, and HighContrast. Currently holds the three Windows close-button brand reds (`WindowCloseButtonBackgroundPointerOver`, `...Pressed`, `WindowCloseButtonForegroundPointerOver`). Per-theme dictionaries no longer carry these keys. Slot count in `ApplicationThemeManager` is now 6; `DictionaryStabilityTests` updated accordingly.

### Changed

- `NavigationView` sizing brought in line with WinUI 3: open pane width 280 -> 320 px (the WinUI 3 `NavigationViewOpenPaneLength`); `NavigationViewItem` `FontSize` 13 -> 14 (the body type-ramp value); `PaneFooter` slot gains a `DividerStrokeColorDefaultBrush` separator above its content in both Left and LeftCompact templates.
- `NavigationView` surface roles realigned to WinUI 3: the pane uses `AcrylicInAppFillColorDefaultBrush` (the `NavigationViewDefaultPaneBackground` value); the content host uses `LayerFillColorDefault` (dark `#4C3A3A3A`, light `#80FFFFFF`), a translucent layer brush over the DWM backdrop instead of the previous flat 65-69%-opaque Fluence-only tint. Mica still passes through both as the translucent layer they are meant to be, so cards composing on top sit above the surface as Fluent intends.
- `TitleBar` sizing: app-title text moved from `CaptionTextBlockStyle` (12 pt) to `BodyTextBlockStyle` (14 pt); app icon shrunk from 24 x 24 to 20 x 20 with 8 / 12 px margins (was 4 / 20).
- Extended the `AccentFillBackdrop` opaque sub-layer pattern from `ToggleSwitch` to every other control whose template applies an accent fill with sub-1.0 alpha (`AccentFillColorSecondary` 0.9, `AccentFillColorTertiary` 0.8, `AccentFillColorDisabled`): `Button`, `DropDownButton`, `ToggleButton`, `SplitButton` (per-half), `CheckBox`, `RadioButton`, and the `Slider` thumb. Hover / press / disabled accent fills now composite against a surface-matched solid (`AccentFillBackdropBrush`) instead of whatever translucent card or Mica surface sits beneath the control. This matches how Notepad and other native Windows 11 surfaces render.
- Demo gallery home page (`GalleryHomePage.xaml`) cards rewritten to the standard `Card.Header` / `Card.Icon` contract (matching `GalleryDataPage`'s `CardVariant` samples) instead of the previous nested-`StackPanel` reimplementation; card glyphs use `AccentFillColorDefaultBrush` (the saturated solid-accent role). The page-level `Background` setter on the hosting `SmoothScrollViewer` is removed so the `NavigationView` layer / Mica composition reaches the page.
- `SettingsRowTitleStyle` -> `BodyStrongTextBlockStyle` (14 pt SemiBold); `SettingsRowDescriptionStyle` -> `CaptionTextBlockStyle` (12 pt). Matches WinUI 3 `SettingsCard` text sizing.

### Fixed

- `ProgressBar` template: removed the dead `BorderThickness` style setter that did not affect the template; corrected the unfilled-track `Background` from `ControlStrokeColorDefaultBrush` (a stroke role) to `ControlStrongStrokeColorDefaultBrush` (the WinUI 3 fill role); changed the default `TrackHeight` from 4 px to 6 px and `CornerRadius` to 3 (a full pill at the new track height, matching the WinUI 3 Gallery). Resolves the two pre-existing failing `ProgressBar_*` tests; `ProgressBar_DefaultStyle_UsesThreePixelTrackHeight` renamed to `ProgressBar_DefaultStyle_UsesSixPixelTrackHeight`.
- `FluenceWindow` no longer forces `RenderOptions.ClearTypeHint=Enabled` at the window root. The WPF default (`Auto`) lets the renderer pick per surface: ClearType subpixel anti-aliasing on opaque surfaces, grayscale anti-aliasing on translucent ones (Mica / Acrylic, the `AccentFillBackdrop` layer, any other translucent compositing layer). Forcing `Enabled` overrode that fallback and produced visibly soft text at body / caption sizes whenever the parent surface was non-opaque, because ClearType subpixel rendering cannot blend correctly against a DWM-composited backdrop. The `.NET 10` WPF Fluent theme also leaves this at the default. `FluenceWindow_DefaultStyleOwnsCrispRootRenderingPolicy` updated to assert `ClearTypeHint.Auto`.

## [0.5.0] - 2026-05-21

- Initial release.
