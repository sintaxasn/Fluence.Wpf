# Migration Notes

## Typography attached property

`TextBlockExtensions.Typography` is still supported and keeps the same `FluentTypography` enum values. It now resolves the matching named `TextBlock` style from `Themes/Typography/Typography.xaml` instead of duplicating font metrics in code.

Applications that already call `ApplicationThemeManager.Apply(...)` or merge the Fluence resource dictionaries keep the same type-ramp sizing. If an application used the attached property without loading Fluence resources, load the Fluence theme dictionaries before relying on typography metrics.
