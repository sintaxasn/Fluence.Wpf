# NavigationView + Titlebar Shell Rebuild Plan

## Summary

Rebuild the full shell surface: `NavigationView`, `NavigationViewItem` family, standalone `TitleBar`, `FluenceWindow` titlebar/caption integration, and `Fluence.Wpf.Demo` usage. Scope is WPFGallery parity, not full WinUI NavigationView: support the Demo shell and current Left / LeftCompact / Top samples; defer hierarchy, top overflow, and full settings-item machinery.

Use WinUI source as visual/behavior authority, iNKORE for WPF-translated NavigationView concepts, WPFUI/.NET 10 WPF for WPF-native chrome patterns. Do not add dependencies on iNKORE or WPFUI.

## Key Changes

- Replace old compatibility-first APIs with a WinUI-first shape where useful. Remove old demo crutches like `PageContent`, `SelectedContent`, and `NavSelectionChanged`; Demo owns navigation content.
- Keep WPF-required shell contracts where WinUI has no direct equivalent: `FluenceWindow` remains DWM/WindowChrome owner, with separate minimize/maximize/restore/close parts.
- Rebuild `TitleBar` as reusable shell chrome with back, pane-toggle, icon/title/subtitle/content slots, and commands/events used by Demo instead of duplicating titlebar navigation buttons in `MainWindow.xaml`.
- Rebuild `NavigationView` around explicit template parts, VSM pane states, 48 px compact / 280 px open pane metrics, one pane-level selection indicator, transparent pane surfaces, and content background only on the content area.
- Update Demo to use new `TitleBar` + `NavigationView.ItemInvoked`/selection flow, preserving search, tag-based navigation, theme/accent/backdrop demos, and no back-stack in the library.

## Implementation Plan

- Start with tests: replace or rewrite current NavigationView/titlebar tests that encode obsolete APIs, then add failing tests for new shell contracts before implementation.
- Recreate NavigationView C# and XAML in small slices: public DPs/events, item selection/invocation, pane state transitions, templates/resources, automation peers, then Demo sample page updates.
- Recreate TitleBar and FluenceWindow integration separately: standalone TitleBar template, command routing, caption hit testing, DWM/backdrop/caption buttons, then Demo shell cleanup.
- Update docs in the same change: `CHANGELOG.md`, `docs/controls.md`, `docs/getting-started.md`, `docs/theming.md` if theme tokens change, and `KNOWN_ISSUES.md` for any intentionally deferred WinUI gaps.
- Preserve UTF-8 BOM and C# 7.3 compatibility for shared `net472` sources.

## Test Plan

- Focused first: `ControlTests.NavigationView`, `FluenceWindowTitleBarTests`, `FluenceWindowHardenTests`, caption button tests, Demo main-window shell tests.
- Then split full suite by TFM: `net10.0-windows10.0.26100.0` and `net472`.
- Build gate: `dotnet build Fluence.Wpf.sln -c Debug -m:1`, zero errors/warnings.
- Visual gate: run Demo and check Light, Dark, High Contrast, accent swap, Mica/Acrylic/None, titlebar search, pane toggle, back button, caption buttons, Left/LeftCompact/Top NavigationView samples.

## Assumptions

- Breaking API changes are acceptable; source compatibility is secondary to WinUI-like behavior.
- WPFGallery parity means current Demo shell and samples, not full WinUI hierarchy/top-overflow.
- Existing PSADT-specific paths stay out of Fluence docs/source unless a later downstream task requests consumer propagation.
