# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- **Demo `MainWindow` NavigationView grouping** (Paradigm A) - the 11 gallery pages are now grouped under three WinUI 3 Gallery-style section headers (`NavigationViewItemHeader`): _Basic input_ (Buttons, Selection, Inputs), _Collections & navigation_ (Data, Tabs, Navigation), _Design & shell_ (Status, Colors, Glyphs, Window). "Home" stays above the groups. Existing search-driven `CollapseEmptySectionHeaders()` behavior hides headers when their section is fully filtered out — no new code path.
- **`GalleryHomePage` Featured controls tile grid** - a new "Featured controls" section below the category landing tiles displays a 3-column `UniformGrid` named `FeaturedControlsGrid` with six clickable `Card`s routing to Buttons, Selection, Inputs, Status, Collections, and Navigation. Uses `BodyStrong` + `Caption` typography so it reads as a distinct surface from the Subtitle + Body category tiles above.

### Added

- **`TabView` / `TabViewItem` controls** (`Fluence.Wpf.Controls`) - WinUI 3-styled multi-document surface built on top of `TabControl` / `TabItem`. Public surface: `TabViewItem.IsClosable`, `TabViewItem.Icon`, `TabViewItem.CloseRequested` routed event; `TabView.IsAddTabButtonVisible`, `TabView.TabWidthMode` (`SizeToContent` / `Equal` / `Compact`), `TabView.CloseButtonOverlayMode` (`Auto` / `OnPointerOver` / `Always`), plus `TabView.AddTabButtonClick` and `TabView.TabCloseRequested` routed events. Template parts: `PART_AddTabButton`, `PART_CloseButton`. Default style in `Themes/Controls/TabView.xaml`.
- **`TabViewWidthMode`** and **`TabViewCloseButtonOverlayMode`** enums in `Fluence.Wpf` (namespace intentionally flat to match the rest of the public enums).
- **`TabViewTabCloseRequestedEventArgs`** routed event args carrying `Tab` (the originating `TabViewItem`) and `Item` (the bound data item).
- **`Fluence.Wpf.Demo/Pages/GalleryTabsPage`** - new "Tabs" entry in the demo `NavigationView`; shows `TabControl` and `TabView` side-by-side, wires up add-tab and close-tab handlers, and demonstrates `IsClosable="False"` for pinned tabs.
- **`GalleryScreenshotHarness`** (`Fluence.Wpf.Tests`) - MSTest-driven `RenderTargetBitmap` capture of the gallery home surface across Light / Dark / High Contrast at 1.0× and 1.5× DPI. Opt-in: set `FLUENCE_CAPTURE_SCREENSHOTS=1` and run the test to regenerate `docs/screenshots/banner-{theme}-{scale}x.png`.
- **`docs/screenshots/`** - committed banner captures (`banner-light-1x.png`, `banner-dark-1x.png`, `banner-highcontrast-1x.png`, and 1.5× counterparts) for documentation and README use.
- **13 new MSTests** in `TabViewTests.cs` covering default dependency-property values, container generation, template parts, add-tab invoke → `AddTabButtonClick`, close-button invoke → `CloseRequested` → `TabView.TabCloseRequested` bubbling, `IsClosable="False"` hides the close button, and `IsAddTabButtonVisible="False"` hides the add button.

### Changed

- **Demo `MainWindow` navigation** now exposes 11 pages (was 10) - the new "Tabs" entry sits between "Data" and "Glyphs". The existing `MainWindow_NavigationView_HasTenNavItems` test was renamed to `MainWindow_NavigationView_HasElevenNavItems` and updated to assert 11.

## [0.3.0] - 2026-04-17

### Added

- **`Card.Click` routed event** plus `IsClickable` / `IsPressed` dependency properties. The demo home page now uses clickable cards to route into the gallery (see `Fluence.Wpf.Demo/Pages/GalleryHomePage.xaml`).
- **`ControlStrongStrokeColorDefault`** and **`ControlStrongStrokeColorDisabled`** color tokens and matching `*Brush` keys in every theme (Light `#72000000` / Dark `#8BFFFFFF` / High Contrast `#FFFFFFFF`), aligned to WinUI 3 `Common_themeresources.xaml`.
- **7 new MSTests** in `ControlTests.FluentStroke.cs` covering the `RadioButton` outer ring, disabled ring swap, `Card.Click` press/release semantics, and the `NavigationView` Left / LeftCompact content-border corner radius + stroke contract.
- **Theme-aware demo banner** in `GalleryHomePage` - `BannerLight.png` / `BannerDark.png` swap in response to `ApplicationThemeManager.Changed` without a page reload.

### Changed

- **`NavigationView` layout** redesigned to match the WinUI 3 reference:
  - Pane toggle sits above the back button, both 40×40, centered in a 48 px pane column.
  - Selection indicator is now a single `PART_SelectionIndicator` that animates between items (3×16 vertical / 16×3 horizontal).
  - Content region draws a 1 px top/left `CardStrokeColorDefault` border with `CornerRadius="8,0,0,0"` in both `Left` and `LeftCompact` templates so the content visually hugs the top-left.
  - Background defaults to `Transparent`; content surface defaults to `LayerFillColorDefaultBrush`.
- **`RadioButton` / `CheckBox` unchecked rings** switched from the subtle `ControlStrokeColorDefaultBrush` to `ControlStrongStrokeColorDefaultBrush`, fixing visibility against light backgrounds (reported as *"radio buttons barely visible"*).
- **Demo `MainWindow`** - search box moved into `FluenceWindow.TitleBar`; filter handler now toggles `NavigationViewItem.Visibility` instead of repopulating the items collection. Back-stack plumbing was removed in favour of `NavigateTo(tag)` + selection-driven navigation.
- **`docs/getting-started.md`**, **`docs/theming.md`**, and **`docs/controls.md`** refreshed for the new Left-mode defaults and the `ControlStrongStroke*` tokens.
- **`CLAUDE.md`** rewritten as a single self-contained maintainer handbook - project overview, architecture, coding standards, control-authoring checklist, theme architecture, testing, pitfalls, and quality gates.

### Removed

- Stale `Themes/Light copy.xaml` and `Themes/Dark copy.xaml` (unused duplicates from an earlier migration).
- `MIGRATION_TRACKING.md` (root and `PSAppDeployToolkit/lib/Fluence.Wpf/`) - the migration is complete, so the log has been archived in git history rather than in the repository.
- The repo-folder-rename note in `KNOWN_ISSUES.md` - the root is now `Fluence.Wpf`.

## [0.2.0] - 2026-04-14

### Added

- Demo gallery restructured: `NavigationView` (**LeftCompact**), search, and split `Pages/*.xaml` user controls.
- Public documentation set: `docs/getting-started.md`, `theming.md`, `controls.md`, `migration-guide.md`, `contributing.md`.
- Test infrastructure: shared `WpfTestSta` STA dispatcher, `[assembly: DoNotParallelize]`, `ThemeTestHelpers`, DPI assertion on `net10`, and coverage for `NumberBox`, `Expander`, `DropDownButton`, `InfoBadge`, `ListBox` container override.

### Changed

- README installation guidance (project reference + local `dotnet pack`); documentation links updated.
- Internal session and migration notes moved under `docs/_internal/`.
- `CLAUDE.md` expanded into a self-contained maintainer handbook (generic comparisons only).

## [0.1.0] - 2026-04-02

### Added

- Initial release of **Fluence.Wpf**:
  - `ApplicationThemeManager` - Light / Dark / High Contrast / Auto theme switching with stable merged dictionary indices.
  - `ApplicationAccentColorManager` - System accent palette and custom accent ramps mapped to WinUI-aligned resource keys.
  - `SystemThemeWatcher` - Live reaction to Windows theme and accent settings while the app runs.
  - `FluenceWindow` - DWM Mica, Acrylic, and Tabbed backdrops; rounded corners; caption button visibility overrides.
  - Fluent-styled controls: Button, HyperlinkButton, CheckBox, RadioButton, ToggleSwitch, TextBox, PasswordBox, ComboBox, Slider, ProgressBar, ProgressRing, ListView, Card, InfoBar, NavigationView, FontIcon, Border, StackPanel, DockPanel, SmoothScrollViewer; tab and scroll bar themes.
  - Layered resource dictionaries (theme colors, brushes, accent ramp, typography, control templates).
  - Demo gallery application and MSTest suite (theme stability, accent, window policy, control templates).
  - GitHub Actions CI (build + test), documentation, and contributor guidelines.

[Unreleased]: https://github.com/sintaxasn/fluence-wpf/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/sintaxasn/fluence-wpf/releases/tag/v0.3.0
[0.2.0]: https://github.com/sintaxasn/fluence-wpf/releases/tag/v0.2.0
[0.1.0]: https://github.com/sintaxasn/fluence-wpf/releases/tag/v0.1.0
