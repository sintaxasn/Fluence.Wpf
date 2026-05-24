# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.6.0-preview] - 2026-05-24

### Changed

- Widened the accent ramp spread in `HsvColorHelper.GenerateAccentRampWinaccent`. The previous Candidate F calibration produced near-base stops only 4-7 % away from the base on the L axis, leaving adjacent ramp rungs visually indistinguishable in control templates. New deltas are ~10-12 % per adjacent step (Light1 +12 %, Light2 +24 %, Light3 +36 %, Dark1 -10 %, Dark2 -20 %, Dark3 -30 %) so controls that reference different rungs for hover / pressed / focus states now show useful variation. The decision to use the user-supplied base verbatim instead of mirroring the Windows perceptual projection still stands; this is purely a spread adjustment, not an OS-transform model.
- Demo `DemoSampleSourceExpanderStyle` background switched to `SolidBackgroundFillColorQuarternaryBrush` so the collapsed "Source code" header strip reads as a distinct dark band beneath the sample card (matches the WinUI Gallery visual reference).

### Added

- `Themes/Shared.xaml` - a new merge slot (`[5]`, loaded once, never replaced) holding theme-independent Color tokens that are identical across Light, Dark, and HighContrast. Currently holds the three canonical Windows close-button brand reds (`WindowCloseButtonBackgroundPointerOver`, `...Pressed`, `WindowCloseButtonForegroundPointerOver`). Per-theme dictionaries no longer carry these keys. Slot count in `ApplicationThemeManager` is now 6; `DictionaryStabilityTests` updated accordingly.

### Changed

- `NavigationView` canonical sizing: open pane width 280 -> 320 px (canonical WinUI 3 `NavigationViewOpenPaneLength`); `NavigationViewItem` `FontSize` 13 -> 14 (canonical body type-ramp); `PaneFooter` slot gains a `DividerStrokeColorDefaultBrush` separator above its content in both Left and LeftCompact templates.
- `NavigationView` canonical surface roles: pane uses `AcrylicInAppFillColorDefaultBrush` (canonical `NavigationViewDefaultPaneBackground`); content host uses `LayerFillColorDefault` (dark `#4C3A3A3A`, light `#80FFFFFF`) - a translucent layer brush over the DWM backdrop instead of the previous flat 65-69%-opaque Fluence-only tint. Mica still passes through both as the translucent layer it is meant to be, giving cards composing on top the canonical Fluent "lift".
- `TitleBar` canonical sizing: app-title text moved from `CaptionTextBlockStyle` (12 pt) to `BodyTextBlockStyle` (14 pt); app icon shrunk from 24 x 24 to 20 x 20 with balanced 8 / 12 px margins (was 4 / 20).
- Extended the `AccentFillBackdrop` opaque sub-layer pattern from `ToggleSwitch` to every other control whose template applies an accent fill with sub-1.0 alpha (`AccentFillColorSecondary` 0.9, `AccentFillColorTertiary` 0.8, `AccentFillColorDisabled`): `Button`, `DropDownButton`, `ToggleButton`, `SplitButton` (per-half), `CheckBox`, `RadioButton`, and the `Slider` thumb. Hover / press / disabled accent fills now composite against a surface-matched solid (`AccentFillBackdropBrush`) instead of whatever translucent card or Mica surface sits beneath the control, matching the rendering Notepad and other native Windows 11 surfaces produce.
- Demo gallery home page (`GalleryHomePage.xaml`) cards rewritten to the standard `Card.Header` / `Card.Icon` contract (matching `GalleryDataPage`'s `CardVariant` samples) instead of the previous nested-`StackPanel` reimplementation; card glyphs use `AccentFillColorDefaultBrush` (the saturated solid-accent role) for vivid colour. The page-level `Background` setter on the hosting `SmoothScrollViewer` is removed so the canonical `NavigationView` layer / Mica composition reaches the page.
- `SettingsRowTitleStyle` -> `BodyStrongTextBlockStyle` (14 pt SemiBold); `SettingsRowDescriptionStyle` -> `CaptionTextBlockStyle` (12 pt). Matches canonical WinUI 3 `SettingsCard` text sizing.

### Fixed

- `ProgressBar` template: removed the vestigial `BorderThickness` style setter that did not affect the template; corrected the unfilled-track `Background` from `ControlStrokeColorDefaultBrush` (a stroke role) to `ControlStrongStrokeColorDefaultBrush` (the canonical WinUI 3 fill role); changed the default `TrackHeight` from 4 px to 6 px and `CornerRadius` to 3 (a full pill at the new track height, matching the WinUI 3 Gallery visual). Resolves the two pre-existing failing `ProgressBar_*` tests; `ProgressBar_DefaultStyle_UsesThreePixelTrackHeight` renamed to `ProgressBar_DefaultStyle_UsesSixPixelTrackHeight`.
- `FluenceWindow` no longer forces `RenderOptions.ClearTypeHint=Enabled` at the window root. The WPF default (`Auto`) lets the renderer select ClearType subpixel anti-aliasing on opaque surfaces and grayscale anti-aliasing on translucent surfaces (Mica / Acrylic, the `AccentFillBackdrop` layer, any other translucent compositing layer) per surface. Forcing `Enabled` overrode the fallback and produced visibly soft text at body / caption sizes whenever the parent surface was non-opaque - ClearType subpixel rendering cannot blend correctly against a DWM-composited backdrop. `.NET 10` WPF Fluent theme also leaves this at the default. `FluenceWindow_DefaultStyleOwnsCrispRootRenderingPolicy` updated to assert `ClearTypeHint.Auto`.

## [0.5.0] - 2026-05-21

- Initial release.
