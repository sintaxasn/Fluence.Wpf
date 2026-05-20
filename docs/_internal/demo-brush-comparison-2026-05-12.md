# Demo Brush Comparison - 2026-05-12

## Current Status

This note is retained as historical context for the May 2026 demo brush cleanup. The original recommendation kept a demo-owned brush promotion layer for sample cards, source headers, settings cards, and page backgrounds. That layer has since been removed.

The current policy is:

- `Fluence.Wpf.Demo/Resources/DemoSharedStyles.xaml` remains the single demo style entry point.
- Demo layout resources stay demo-owned: margins, padding, spacing, row styles, tile density, and sample-page composition.
- Demo surfaces use native Fluence brush roles directly through `DynamicResource`.
- Do not reintroduce `DemoThemeResources.cs` or any app-level brush refresh layer.
- Do not add demo aliases such as `GalleryBackgroundBrush`, `GalleryBorderBrush`, `DemoSampleCardBackgroundBrush`, or `DemoSettingsCardBrush`.

## Current Surface Mapping

| Demo surface | Current Fluence role |
| --- | --- |
| Gallery page root / scroll host | Control default host surface unless a page has a specific reason to override it |
| Sample card surface | `CardBackgroundFillColorDefaultBrush` |
| Sample card stroke | `CardStrokeColorDefaultBrush` |
| Right rail / options pane | `CardBackgroundFillColorSecondaryBrush` |
| Source expander header | `ControlFillColorDefaultBrush` |
| Source expander content | `SolidBackgroundFillColorBaseBrush` |
| Settings row cards | `CardBackgroundFillColorDefaultBrush` plus `CardStrokeColorDefaultBrush` |
| Secondary labels | `TextFillColorSecondaryBrush` |

## Rationale

The demo should show the library's resource system, not maintain a parallel resource system. Native Fluence roles now provide the card, stroke, background, foreground, and high-contrast behavior the demo needs. Demo-specific resources remain appropriate for layout and catalog presentation.

If a future page needs a new semantic color role, add it to `Fluence.Wpf` only when it is broadly useful to consumers. Keep page-only presentation resources in the demo.

## Validation Expectations

Resource cleanup should be backed by tests that:

- Load `DemoSharedStyles.xaml` without shadowing native Fluence resource keys.
- Confirm stale demo aliases stay absent.
- Confirm `ApplicationThemeManager` still owns the first five merged dictionaries and demo styles are appended after those slots.
