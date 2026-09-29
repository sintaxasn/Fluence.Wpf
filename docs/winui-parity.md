# WinUI 3 Parity and WPF differences

Fluence.Wpf follows the Windows 11 Fluent visual language and many WinUI 3 control names. It remains a WPF library. This page helps when moving a WinUI example into a WPF application or deciding whether a familiar WinUI pattern is available.

## What maps directly

- Core control names and roles include `Button`, `NavigationView`, `TabView`, `ContentDialog`, `InfoBar`, `TeachingTip`, `AutoSuggestBox`, `NumberBox`, `DatePicker`, `TimePicker`, and `ColorPicker`.
- Common color and brush names follow WinUI-style families such as `TextFillColor*`, `ControlFillColor*`, `AccentFillColor*`, and `CardBackgroundFillColor*`.
- Light, Dark, and High Contrast are supported, with live theme and accent updates through published WPF resources.
- `ThemeResourceExtension` and `ThemeDictionary` provide WPF equivalents of theme-aware markup and per-theme values.

The [control catalog](controls.md) and [theme reference](theming.md) define the actual shipped surface. The public API and resource-key baselines in the repository are the exact inventories.

## Differences to account for

| Concern | Fluence.Wpf behavior |
| --- | --- |
| Runtime | Controls are WPF types and templates. A consuming app does not need the Windows App SDK. |
| XAML namespace | Use `xmlns:fluence="http://schemas.fluencewpf.com"`; write `{fluence:ThemeResource Key}` with the prefix. |
| App resources | `ApplicationThemeManager.Apply` publishes a WPF merged-dictionary stack. Use `DynamicResource` or `ThemeResource` for live values. |
| Per-control keys | The library publishes application-wide roles, but no WinUI-style key per control state. Replace a specific control template to restyle that state. |
| Acrylic surfaces | WPF has no equivalent per-element backdrop brush, so acrylic resource roles are painted fallback colors. Window backdrop requests use the available Windows DWM or legacy path. |
| Navigation | `NavigationView` derives from WPF `Selector`; its page host belongs in `NavigationView.Content`, and the app owns navigation history. |
| Date picker | The Fluence picker has its own selected-date API and is not a subclass of WPF `DatePicker`. |
| Text and images | Fluence `TextBlock` and `Image` are templated controls rather than subclasses of the native WPF elements. |
| Password entry | Native WPF `PasswordBox` is styled implicitly and extended with attached properties because it is sealed. |

## Visual and platform boundaries

The control templates use Fluent colors, geometry, elevation, focus, and animation resources. Window chrome is translated through WPF `WindowChrome` and Windows interop. Windows 11 can supply Mica, Tabbed, Acrylic, and rounded-corner behavior; Windows 10 and high contrast use documented fallbacks. See [compatibility](reference/compatibility.md).

Where WinUI uses a compositor feature that WPF does not expose, the library chooses a WPF behavior and records accepted limitations in [known issues](../KNOWN_ISSUES.md). For a specific control, inspect the [gallery example](../Fluence.Wpf.Demo/Pages/) beside the corresponding [control source](../Fluence.Wpf/Controls/) and [template](../Fluence.Wpf/Themes/Controls/). This is the most reliable way to assess parity for an interaction you plan to use.
