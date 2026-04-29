# Migration Notes

## Typography attached property

`TextBlockExtensions.Typography` is still supported and keeps the same `FluentTypography` enum values. It now resolves the matching named `TextBlock` style from `Themes/Typography/Typography.xaml` instead of duplicating font metrics in code.

Applications that already call `ApplicationThemeManager.Apply(...)` or merge the Fluence resource dictionaries keep the same type-ramp sizing. If an application used the attached property without loading Fluence resources, load the Fluence theme dictionaries before relying on typography metrics.

## NavigationView child indicators

`NavigationViewItem.IsChildItem` is an optional dependency property for entries that are visually nested under a section header. It defaults to `false`.

Set `IsChildItem="True"` on child entries when a left-pane `NavigationView` should align the selected indicator with the child content column. Iconless top-level entries are no longer treated as child entries only because `Icon` is unset; set `IsChildItem="True"` if an existing iconless entry intentionally relied on the child indicator position.
