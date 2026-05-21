---
title: Migration guide
linkTitle: Migration guide
description: Move an existing WPF application from standard WPF controls or another Fluent-style library to Fluence.Wpf.
weight: 40
---

Use this guide when moving an existing WPF app from stock controls or another Fluent-style library to `Fluence.Wpf`.

## Scope

`Fluence.Wpf` supports WPF applications on .NET Framework 4.7.2 and .NET 10 for Windows. It mirrors the Windows 11 Fluent / WinUI 3 visual language while staying in WPF primitives. It does not require the Windows App SDK.

## Basic Steps

1. Reference `Fluence.Wpf/Fluence.Wpf.csproj` or a local `Fluence.Wpf` package.
2. Add the XML namespace:

    ```xml
    xmlns:fluence="http://schemas.fluencewpf.com"
    ```

3. Initialize resources before showing the first window:

    ```csharp
    Fluence.Wpf.ApplicationThemeManager.Apply(
        Fluence.Wpf.ApplicationTheme.Auto,
        Fluence.Wpf.BackdropType.Mica,
        updateAccent: true);
    Fluence.Wpf.ApplicationAccentColorManager.ApplySystemAccent();
    ```

4. Replace shell windows with `fluence:FluenceWindow` where you need Fluent caption buttons, DWM backdrop, rounded corners, or title-bar content.
5. Replace controls incrementally. Start with leaf controls such as `Button`, `TextBox`, `ComboBox`, `ListView`, `InfoBar`, and `ProgressBar`, then move larger shell surfaces such as `NavigationView` and `TabView`.

## Resource Rules

- Use `DynamicResource` for Fluence brushes, colors, typography, corner radii, and theme-bound values.
- Do not manually merge `Themes/Generic.xaml` when using `ApplicationThemeManager.Apply`; the manager owns the fixed resource dictionary slots.
- Consume brush resources such as `TextFillColorPrimaryBrush` and `ControlFillColorDefaultBrush`, not raw color resources, from control templates and application XAML.

## Title bar and window controls

`FluenceWindow` owns DWM and caption-button behavior. Use its public properties such as `SystemBackdropType`, `CornerStyle`, `ExtendsContentIntoTitleBar`, `TitleBar`, and caption button visibility properties. Internal helpers such as `CaptionButtonChrome` and `WindowPolicy` are implementation details and should not be referenced by applications.

## Verification

After migrating a page or shell surface, run the gallery and exercise Light, Dark, High Contrast, accent changes, and the target backdrop mode. For source builds, run:

```powershell
dotnet build Fluence.Wpf.sln -c Debug
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug
```
