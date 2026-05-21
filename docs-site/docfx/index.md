# Fluence.Wpf API Reference

This section is the authoritative type-by-type reference for the public surface of `Fluence.Wpf`. It is generated from the library's XML documentation comments by [DocFX](https://dotnet.github.io/docfx/).

For consumer-facing guidance, prefer the conceptual documentation on the [published site](https://sintaxasn.github.io/Fluence.Wpf/docs/).

## Browse by namespace

- **@"Fluence.Wpf"** - `ApplicationThemeManager`, `ApplicationAccentColorManager`, `SystemThemeWatcher`, theme/backdrop/typography enums (`ApplicationTheme`, `BackdropType`, `CardVariant`, `InfoBarSeverity`, `FluentTypography`, ...), and the event-arg types that surround them.
- **@"Fluence.Wpf.Controls"** - Custom controls (`Button`, `TabView`, `NavigationView`, `Card`, `PersonPicture`, ...), `FluenceWindow`, and the `TitleBar` host control.
- **@"Fluence.Wpf.Helpers"** - Public helper utilities surfaced to consumers (`GridLengthAnimation`, ...).
- **@"Fluence.Wpf.Automation"** - UI Automation peers for the custom controls.

## Conventions

- All types ship with `///` XML documentation. Missing comments are a build error in the library, so anything documented here matches the shipping API surface.
- Dependency properties are documented on their CLR wrapper property; the static `Property` field follows immediately below.
- `FluenceWindow.TitleBar` and similar nullable members declare null semantics in their summary.
- Internal helpers such as `CaptionButtonChrome` and `WindowPolicy` are intentionally excluded - see the conceptual [controls page](https://sintaxasn.github.io/Fluence.Wpf/docs/controls/) for the rationale.
