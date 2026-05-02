# Migration guide

This guide is **generic**: it describes moving from any older Fluent-style WPF theming stack to Fluence.Wpf **without** naming specific third-party libraries.

## 1. Resource keys

- Map consumer `ResourceDictionary` keys to **WinUI 3 canonical** names used by this library (for example `TextFillColorPrimaryBrush`, `AccentFillColorDefaultBrush`, `CardStrokeColorDefaultBrush`, `ControlStrongStrokeColorDefaultBrush`).
- Prefer removing duplicate brush definitions and binding to Fluence keys via `DynamicResource`.
- When a brush was previously the subtle `ControlStrokeColorDefaultBrush` on ring-style controls (radio / check box), switch to `ControlStrongStrokeColorDefaultBrush` to match the 0x72 alpha visibility used by WinUI 3.

## 2. Static vs dynamic

- **Problem**: `StaticResource` to a brush that lives in the theme slot (or any accent / high-contrast-tracking brush) will **not** update when the theme changes.
- **Fix**: use `DynamicResource` for any brush, color, or margin that must track theme, accent, or high contrast. Reserve `StaticResource` for immutable assets (icon glyphs, fixed margins, static paths).

## 3. Merge order

- Fluence expects a **single** swappable theme dictionary at slot `[0]`; do not stack multiple full theme dictionaries.
- After calling `ApplicationThemeManager.Apply`, avoid manually clearing `MergedDictionaries` unless you re-run the documented initialization sequence (see [theming.md](theming.md)).
- If you previously held brushes in `App.xaml`, move them into your application resources under a dedicated merged dictionary *after* the Fluence slots so they can reference Fluence color keys with `DynamicResource`.

## 4. Window and backdrop

- If you used custom chrome or unofficial backdrop wrappers, compare behavior with `FluenceWindow` (`Backdrop`, caption visibility, rounded corners, `ExtendsContentIntoTitleBar`).
- `FluenceWindow` exposes a `TitleBar` content slot - migrate any search boxes, breadcrumbs, or workspace pickers you previously crammed into a custom header into this slot.
- Test **High Contrast** and **accent** changes with `SystemThemeWatcher` enabled; if your old shell relied on `SystemParameters` poll-based hooks, retire them and subscribe to `ApplicationThemeManager.Changed` instead.

## 5. NavigationView layout

- The default `PaneDisplayMode` is **`Left`**. If you previously pinned `LeftCompact` to work around indicator gaps, you can drop that override - both templates now share a single animated `PART_SelectionIndicator` and the same content-region border.
- The content region draws a 1 px top/left `CardStrokeColorDefault` border with `CornerRadius="8,0,0,0"`. If your consumer styles painted their own outer frame, remove that frame so the Fluence border is visible.
- Back button and pane toggle live in a 48 px rail on the left; custom pane headers / footers go through `PaneHeader`, `PaneFooter`, and `Header` as before.

## 6. Clickable cards

- `Card.IsClickable` / `Card.Click` replaces pattern-driven "wrap card in a transparent button" workarounds. Migrate those to `<fluence:Card IsClickable="True" Click="..." />` to get `IsPressed` styling and keyboard accessibility semantics that match WinUI.

## 7. Control mapping

- Many controls subclass standard WPF types with a new `DefaultStyleKey`; swap XML prefixes from your old assembly to `Fluence.Wpf.Controls` (or use the `http://schemas.fluencewpf.com` URI).
- Where APIs differ (attached properties, corner radius, flyouts), align to Fluence's surface as shown in **Fluence.Wpf.Demo**.

## 8. Validation checklist

- [ ] No `StaticResource` on keys that must track theme, accent, or high contrast.
- [ ] `ApplicationThemeManager.Apply` runs at startup; accent manager initialized.
- [ ] `SystemThemeWatcher.Watch(MainWindow)` registered if you want live OS-theme reactions.
- [ ] Visual pass: Light, Dark, High Contrast, accent change, Mica / Acrylic / Tabbed backdrops.
- [ ] `dotnet test` on `net472` and `net10.0-windows` if you mirror the repo matrix.
