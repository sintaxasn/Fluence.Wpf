# Fluence.Wpf

Windows 11 Fluent Design controls and theming for WPF applications targeting **.NET Framework 4.7.2** and **Windows 10** (1809+) with enhanced visuals on **Windows 11**.

![Fluence.Wpf](docs/images/banner.svg)

## Features

- **Theme pipeline** — Light, Dark, High Contrast, and Auto (follow Windows app theme) with stable `MergedDictionaries` ordering.
- **Accent colors** — System accent palette and custom accent ramps mapped to WinUI-style resource keys.
- **System theme watcher** — Refresh resources when the user changes Windows theme or accent at runtime.
- **FluentWindow** — DWM **Mica**, **Acrylic**, and **Tabbed** backdrops; rounded corners; minimize / maximize / close button overrides; optional title and icon.
- **Controls** — Fluent-styled Button, HyperlinkButton, CheckBox, RadioButton, ToggleSwitch, TextBox, PasswordBox, ComboBox, Slider, ProgressBar, ProgressRing, ListView, Card, InfoBar, NavigationView, FontIcon, Border, StackPanel, DockPanel, SmoothScrollViewer, plus TabControl / ScrollBar themes.
- **Typography** — Attached properties for a WinUI-like type ramp on `TextBlock`.
- **Demo app** — Full gallery for visual verification.
- **Tests** — MSTest coverage for theme stability, accent, window policy, and control templates.

## Quick Start

1. Add a project reference to `Fluence.Wpf` (or reference the built `Fluence.Wpf.dll`).
2. In `App.xaml.cs` (before showing the main window):

```csharp
Fluence.Wpf.ApplicationThemeManager.Apply(
    Fluence.Wpf.ApplicationTheme.Auto,
    Fluence.Wpf.BackdropType.Mica,
    updateAccent: true);
Fluence.Wpf.ApplicationAccentColorManager.ApplySystemAccent();
```

3. Use `Fluence.Wpf.Controls.FluentWindow` (or merge `Generic.xaml` and use styled controls in a standard `Window`).

Optional XML namespace mapping:

```xml
xmlns:fluence="http://schemas.fluencewpf.com"
```

## Screenshots

_Screenshots will be added in a future update._ Run the **Fluence.Wpf.Demo** project locally to preview all controls.

## Controls

| Area | Types |
|------|--------|
| Window | `FluentWindow` |
| Basic input | `Button`, `HyperlinkButton`, `CheckBox`, `RadioButton`, `ToggleSwitch`, `ComboBox`, `Slider` |
| Text | `TextBox`, `PasswordBox`, `TextBlockExtensions` |
| Collections | `ListView` |
| Feedback | `ProgressBar`, `ProgressRing`, `InfoBar` |
| Navigation | `NavigationView`, `NavigationViewItem` |
| Layout | `Card`, `Border`, `StackPanel`, `DockPanel`, `SmoothScrollViewer` |
| Icons | `FontIcon` |

## Theming

- **ApplicationTheme**: `Light`, `Dark`, `HighContrast`, `Auto`.
- **BackdropType**: `None`, `Auto`, `Mica`, `Acrylic`, `Tabbed` (for `FluentWindow`).
- **Accent**: `ApplicationAccentColorManager.ApplySystemAccent()`, `ApplyApplicationAccent()`, or `ApplyCustomAccent(Color)`.
- **Live OS changes**: `SystemThemeWatcher.Watch(window)` / `UnWatch(window)`.

## Installation

Clone or submodule this repository and add a **project reference** to `Fluence.Wpf/Fluence.Wpf.csproj`. NuGet package publication may follow in a later release.

## Requirements

- .NET Framework 4.7.2
- Windows 10 version 1809 or later
- Windows 11 recommended for full Mica / Acrylic / Tabbed backdrop support

## Building from Source

Prerequisites: [.NET SDK](https://dotnet.microsoft.com/download) (includes MSBuild), Windows.

```powershell
dotnet restore Fluence.Wpf.sln
dotnet build Fluence.Wpf.sln -c Release
dotnet test Fluence.Wpf.sln -c Release
```

## Running the Demo

```powershell
dotnet run --project Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -c Release
```

Or set **Fluence.Wpf.Demo** as the startup project in Visual Studio and press F5.

## Contributing

Pull requests are welcome. Please keep **C# 7.3** compatibility and match existing patterns for themes and controls. For AI-assisted edits, read [.github/copilot-instructions.md](.github/copilot-instructions.md).

## Control Coverage

See [docs/CONTROL_COVERAGE.md](docs/CONTROL_COVERAGE.md) for a WinUI 3 control mapping and roadmap.

## Used By

- [PSAppDeployToolkit](https://psappdeploytoolkit.com) — Enterprise application deployment toolkit

## License

Licensed under the [BSD 3-Clause License](LICENSE).

Copyright © 2026 Dan Cunningham.
