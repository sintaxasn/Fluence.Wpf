# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- `NavigationView` now matches WinUI 3 canonical surface roles: the pane uses `AcrylicInAppFillColorDefaultBrush` (canonical `NavigationViewDefaultPaneBackground`) across Left / LeftCompact / Top templates instead of pure `Transparent`, and the content host uses `LayerFillColorDefault` values (dark `#4C3A3A3A`, light `#80FFFFFF`) - a translucent layer brush rather than the previous flat 65-69%-opaque Fluence-only tint. The pane now reads as a distinct surface vs the content area; Mica still passes through both as the translucent layer it is meant to be, giving cards composing on top the canonical Fluent "lift" they were missing.
- `FluenceWindow` no longer forces `RenderOptions.ClearTypeHint=Enabled` at the window root. The WPF default (`Auto`) lets the renderer select ClearType subpixel anti-aliasing on opaque surfaces and grayscale anti-aliasing on translucent surfaces (Mica / Acrylic, the `AccentFillBackdrop` layer, any other translucent compositing layer) per surface. Forcing `Enabled` overrode the fallback and produced visibly soft text at body / caption sizes whenever the parent surface was non-opaque - because ClearType subpixel rendering cannot blend correctly against a DWM-composited backdrop and degrades into a worse-than-grayscale fallback. .NET 10 WPF Fluent theme also leaves this at the default. `FluenceWindow_DefaultStyleOwnsCrispRootRenderingPolicy` updated to assert `ClearTypeHint.Auto`.
- `ProgressBar` template: removed the vestigial `BorderThickness` style setter that did not affect the template; corrected the unfilled-track `Background` from `ControlStrokeColorDefaultBrush` (a stroke role) to `ControlStrongStrokeColorDefaultBrush` (the canonical WinUI 3 fill role); changed the default `TrackHeight` from 4 px to 6 px and `CornerRadius` to 3 (a full pill at the new track height, matching the WinUI 3 Gallery visual). Resolves the two pre-existing failing `ProgressBar_*` tests; `ProgressBar_DefaultStyle_UsesThreePixelTrackHeight` renamed to `ProgressBar_DefaultStyle_UsesSixPixelTrackHeight`.

### Changed

- Extended the `AccentFillBackdrop` opaque sub-layer pattern from `ToggleSwitch` to every other control whose template applies an accent fill with sub-1.0 alpha (`AccentFillColorSecondary` 0.9, `AccentFillColorTertiary` 0.8, `AccentFillColorDisabled`): `Button`, `DropDownButton`, `ToggleButton`, `SplitButton` (per-half), `CheckBox`, `RadioButton`, and the `Slider` thumb. Hover / press / disabled accent fills now composite against a surface-matched solid (`AccentFillBackdropBrush`) instead of whatever translucent card or Mica surface sits beneath the control, matching the rendering Notepad and other native Windows 11 surfaces produce.

## [0.5.0] - 2026-05-21

- Initial release.
