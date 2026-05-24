# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Extended the `AccentFillBackdrop` opaque sub-layer pattern from `ToggleSwitch` to every other control whose template applies an accent fill with sub-1.0 alpha (`AccentFillColorSecondary` 0.9, `AccentFillColorTertiary` 0.8, `AccentFillColorDisabled`): `Button`, `DropDownButton`, `ToggleButton`, `SplitButton` (per-half), `CheckBox`, `RadioButton`, and the `Slider` thumb. Hover / press / disabled accent fills now composite against a surface-matched solid (`AccentFillBackdropBrush`) instead of whatever translucent card or Mica surface sits beneath the control, matching the rendering Notepad and other native Windows 11 surfaces produce.

## [0.5.0] - 2026-05-21

- Initial release.
