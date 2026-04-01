# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-03-31

### Added

- Initial open-source release as **Fluence.Wpf** (extracted and rebranded from an internal WPF interop library).
- `ApplicationThemeManager` — Light / Dark / High Contrast / Auto theme switching with stable merged dictionary indices.
- `ApplicationAccentColorManager` — System accent palette and custom accent ramps mapped to WinUI-aligned resource keys.
- `SystemThemeWatcher` — Live reaction to Windows theme and accent settings while the app runs.
- `FluentWindow` — DWM Mica, Acrylic, and Tabbed backdrops; rounded corners; caption button visibility overrides.
- Fluent-styled controls: Button, HyperlinkButton, CheckBox, RadioButton, ToggleSwitch, TextBox, PasswordBox, ComboBox, Slider, ProgressBar, ProgressRing, ListView, Card, InfoBar, NavigationView, FontIcon, Border, StackPanel, DockPanel, SmoothScrollViewer; tab and scroll bar themes.
- Layered resource dictionaries (theme colors, brushes, accent ramp, typography, control templates).
- Demo gallery application and MSTest suite (theme stability, accent, window policy, control templates).
- GitHub Actions CI (build + test), documentation, and contributor guidelines.

[0.1.0]: https://github.com/dancunningham/fluence-wpf/releases/tag/v0.1.0
