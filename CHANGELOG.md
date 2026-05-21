# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.5.0] - 2026-05-21

### Added

- Documentation site project under `docs-site/` that publishes the conceptual docs in `docs/` together with a DocFX API reference to GitHub Pages at [sintaxasn.github.io/Fluence.Wpf](https://sintaxasn.github.io/Fluence.Wpf/). The Hugo Hextra theme is themed to match the Fluence Win11 visual identity and the deploy workflow `.github/workflows/docs.yml` runs independently of the existing build pipeline.

### Fixed

- Accent application now raises `AccentColorChanged` once per apply after accent-dependent resources are updated.
- `PasswordBox` now stops Caps Lock polling when unloaded.
- `TitleBar` now detaches command `CanExecuteChanged` handlers while unloaded.
- Colors gallery now follows the WinUI Gallery Design > Colors structure with reusable live resource tiles, resolving brush keys, and theme-aware foregrounds.
- PowerShell demo scripts now declare their strict-mode smoke-test parameters and use current `FluenceWindow` backdrop/corner properties.
- Typography and Settings demo pages now keep source snippets and compact settings layouts aligned with the live UI.
- Status and Navigation demo source snippets now match the live ProgressBar, ProgressRing, and compact navigation samples.
- Data page PersonPicture source now uses a self-contained `UserControl` XAML root that matches its displayed C# code-behind.
- Demo sample source validation now catches displayed XAML/C# namespace, class, and snippet hygiene regressions.
- XML documentation now clarifies theme-apply behavior, title-bar null clearing, accent ramp naming, automation peer owners, and attached-property accessors.
- Public docs now resolve the migration and release checklist links and keep internal window-chrome helpers out of the consumer control catalog.
- `NavigationView` now keeps its pane collapse button visible in Left and LeftCompact modes without duplicating shell title-bar chrome.
- Removed tracked C# Dev Kit `.lscache` files and ignored them.

## [0.1.0] - 2026-05-12

### Added

- Initial Fluence.Wpf control library for WPF applications that want Windows 11 Fluent and WinUI 3 style controls.
- Theme system with stable Light, Dark, High Contrast, and Auto dictionaries, WinUI-style color and brush keys, typography resources, and dynamic accent ramp updates.
- Fluent window shell support through `FluenceWindow`, including DWM backdrop selection, rounded corners, title-bar extension, caption controls, and title-bar content slots.
- Core control set covering buttons, selection controls, text input, combo boxes, sliders, progress controls, cards, borders, panels, list and tree views, menus, tooltips, tabs, navigation, status controls, icons, and smooth scrolling.
- Demo gallery with concrete pages for Home, Colors, Icons, Typography, Accessibility, Buttons, Selection, Inputs, Forms, Data, Data binding, Trees, Menus, Navigation, Tabs, Layout, and Status.
- Demo Settings page with theme, navigation style, accent colors, backdrop, caption-button customization, version, and repository actions.
- MVVM demo application that shows Fluence controls in a CommunityToolkit.Mvvm task manager sample.
- MSTest suite covering theme stability, accent behavior, focus visuals, window policy, control templates, demo navigation, page layout, and representative interaction states.

### Changed

- Gallery pages now use WinUI Gallery-style composition: dark page background `#272727`, sample surfaces `#202020`, source-code surfaces `#323232`, max-width content, shared margins, and 48 px bottom scroll breathing room.
- `DemoSampleControl` now matches the WinUI Gallery control-example pattern with attached sample and source surfaces, a darker source area, and an overlaid copy action.
- Demo navigation moved Settings into a selectable footer gear item. Navigation style switching, colors, backdrop, and window chrome customization now live on the Settings page instead of a separate Windowing page.
- Focus visuals now behave like accessibility focus cues: keyboard navigation shows focus chrome, while pointer selection does not leave focus rectangles behind.
- `TabControl`, `TabView`, and `NavigationView` spacing now reserves room for focus rectangles so right-side focus visuals are not clipped.

### Fixed

- NavigationView item pointer selection no longer forces keyboard focus onto the clicked item.
- Tab item focus no longer uses a pointer-sticky custom ring.
- TabControl selected headers no longer lose their right-side rounded corners or shift the accent selection bar off center.
- Demo top navigation overflow now accounts for the Settings footer item instead of the removed pane-mode toggle.
- Demo resource brushes now update with `ApplicationThemeManager.Changed`, using exact dark-mode Gallery colors and readable Light and High Contrast fallbacks.

### Known Limitations

- `TabView` does not yet support drag reorder or tear-off tabs.
- `TreeView` prioritizes smooth pixel scrolling over large-data virtualization.
- Generated screenshots cover the gallery banner only; per-control screenshots are still a documentation follow-up.
- Mica and Acrylic are not captured by `RenderTargetBitmap`.
- `NavigationView` exposes routing events and back events, but library-owned page history remains consumer-owned by design.

[Unreleased]: https://github.com/sintaxasn/fluence.wpf/compare/v0.5.0...HEAD
[0.5.0]: https://github.com/sintaxasn/fluence.wpf/compare/v0.1.0...v0.5.0
[0.1.0]: https://github.com/sintaxasn/fluence.wpf/releases/tag/v0.1.0
