# Demo Brush Comparison - 2026-05-12

## Scope

This report compares the demo-owned gallery brushes against the default Fluence.Wpf brushes they would most likely replace if the demo overrides were removed. It also evaluates a third variant: keep the current demo RGB values, but apply the alpha values from the original Fluence.Wpf default brush roles.

The current demo brush ownership is in:

- `Fluence.Wpf.Demo/Resources/DemoSharedStyles.xaml`
- `Fluence.Wpf.Demo/DemoThemeResources.cs`

## Current Demo Brush Roles

| Demo brush | Current Light | Current Dark | Default Fluence role if removed | Current usage |
| --- | --- | --- | --- | --- |
| `DemoPageBackgroundBrush` | `#FFF9F9F9` | `#FF272727` | `SolidBackgroundFillColorBaseBrush` | Gallery page root and scroll viewer backgrounds, Settings page background. |
| `DemoSampleCardBackgroundBrush` | `#FFF3F3F3` | `#FF202020` | `CardBackgroundFillColorDefaultBrush` | `DemoSampleControl` sample card, output region, copy button host. |
| `DemoSampleRightRailBackgroundBrush` | `#FFFBFBFB` | `#FF2B2B2B` | `CardBackgroundFillColorSecondaryBrush` | `DemoSampleControl` right rail/options pane. |
| `DemoSampleSourceHeaderBackgroundBrush` | `#FFFDFDFD` | `#FF323232` | `ControlFillColorDefaultBrush` | `DemoSampleControl` source expander header. |
| `DemoSampleSourceContentBackgroundBrush` | `#FFFFFFFF` | `#FF2E2E2E` | `SolidBackgroundFillColorBaseBrush` | `DemoSampleControl` source-code tab content and code viewer. |
| `DemoControlExampleBackgroundBrush` | `#FF373737` | `#FF373737` | `ControlFillColorDefaultBrush` | Requested ListView, TreeView, and first Menu sample surfaces. High Contrast maps to `ControlFillColorDefaultBrush`. |
| `DemoSettingsCardBrush` | Default card role | `#FF323232` | `CardBackgroundFillColorDefaultBrush` | Settings row cards. |
| `DemoSectionCardBrush` | Default card role | `#FF202020` | `CardBackgroundFillColorDefaultBrush` | Shared section card surfaces. |
| `DemoFieldLabelForegroundBrush` | Default secondary text role | Default secondary text role | `TextFillColorSecondaryBrush` | Settings field labels. This is not a surface override after theme refresh. |

## Default Fluence Brush Values

Relevant default color keys currently resolve as:

| Fluence color key | Light | Dark |
| --- | --- | --- |
| `SolidBackgroundFillColorBase` | `#FFF3F3F3` | `#FF202020` |
| `CardBackgroundFillColorDefault` | `#B3FFFFFF` | `#0DFFFFFF` |
| `CardBackgroundFillColorSecondary` | `#80F6F6F6` | `#08FFFFFF` |
| `ControlFillColorDefault` | `#B3FFFFFF` | `#0FFFFFFF` |
| `ControlFillColorSecondary` | `#80F9F9F9` | `#15FFFFFF` |
| `NavigationViewContentBackground` | `#A6FEFEFE` | `#AF2A2A2A` |
| `AcrylicBackgroundFillColorDefault` | `#F0F9F9F9` | `#F02C2C2C` |

The main difference is not only RGB. Fluence card and control defaults use partial alpha, especially in dark theme. The demo sample surfaces are intentionally opaque.

## Alpha-Adjusted Variant

The alpha-adjusted variant would keep demo RGB values and use the alpha from the matching Fluence default role:

| Demo brush | Alpha source | Light alpha-adjusted | Dark alpha-adjusted | Expected impact |
| --- | --- | --- | --- | --- |
| `DemoPageBackgroundBrush` | `SolidBackgroundFillColorBase` | `#FFF9F9F9` | `#FF272727` | No alpha change. Page background remains stable. |
| `DemoSampleCardBackgroundBrush` | `CardBackgroundFillColorDefault` | `#B3F3F3F3` | `#0D202020` | Dark cards would become nearly transparent over page/Mica. Sample card separation would mostly disappear. |
| `DemoSampleRightRailBackgroundBrush` | `CardBackgroundFillColorSecondary` | `#80FBFBFB` | `#082B2B2B` | Right rail would lose contrast against the sample card, especially in dark theme. |
| `DemoSampleSourceHeaderBackgroundBrush` | `ControlFillColorDefault` | `#B3FDFDFD` | `#0F323232` | Source header would no longer read as a separate attached row in dark theme. |
| `DemoSampleSourceContentBackgroundBrush` | `SolidBackgroundFillColorBase` | `#FFFFFFFF` | `#FF2E2E2E` | No alpha risk. This variant is visually safe. |
| `DemoControlExampleBackgroundBrush` | `ControlFillColorDefault` | `#B3373737` | `#0F373737` | ListView, TreeView, and Menu sample surfaces would become too dependent on backdrop and page content. High risk with Acrylic/Mica. |
| `DemoSettingsCardBrush` | `CardBackgroundFillColorDefault` | Default card role | `#0D323232` | Dark settings rows would flatten into the window background. |
| `DemoSectionCardBrush` | `CardBackgroundFillColorDefault` | Default card role | `#0D202020` | Dark section cards would lose visual grouping. |

## Expected Impact If Demo Brushes Are Removed

### Page Background

Removing `DemoPageBackgroundBrush` would use `SolidBackgroundFillColorBaseBrush`. In light theme the page would shift from `#F9F9F9` to `#F3F3F3`, making the page too close to the current sample card. In dark theme it would shift from `#272727` to `#202020`, making the page as dark as the current sample card and reducing the three-layer stack.

### Sample Cards And Right Rails

Default card brushes are translucent. In dark theme `CardBackgroundFillColorDefault` is `#0DFFFFFF` and `CardBackgroundFillColorSecondary` is `#08FFFFFF`. These are useful for general Fluent surfaces, but they do not preserve the opaque WinUI Gallery sample-card stack. With Mica or Acrylic active, the sample cards would depend more on the backdrop behind the window and could become lighter, noisier, or less distinct depending on the desktop.

### Source Expander

The source header uses the strongest intentional contrast in the current demo stack: dark `#323232` over a `#202020` sample card, with expanded content at `#2E2E2E`. Removing the demo source brushes would map the header to `ControlFillColorDefaultBrush`, which is translucent in both light and dark themes. The card-to-expander join would still exist geometrically, but the row separation would be weaker and more variable under Mica/Acrylic.

### ListView, TreeView, And Menu Sample Surfaces

The requested `#373737` background gives these controls a stable dark sample well. Using the default control fill role would introduce alpha and make those surfaces depend on the parent card and any backdrop effect. This is risky for list/tree/menu samples because their item rows, selection states, and focus visuals need a predictable contrast baseline.

### Navigation Surfaces

NavigationView content already has its own semi-opaque role: dark `#AF2A2A2A`, light `#A6FEFEFE`. The demo sample-card stack should not be replaced by NavigationView surface tokens. NavigationView samples can sit inside the demo card, but the demo wrapper needs its own opaque layers so the NavigationView composition remains legible.

## Recommendation

Keep the demo brush overrides for the gallery sample infrastructure.

Do not remove the opaque demo sample brushes wholesale. The Fluence defaults are correct for reusable controls, but the demo gallery is trying to reproduce WinUI Gallery control-example layering. The default card/control alpha values would weaken the three-tier stack and are most likely to become too light or visually noisy with Mica/Acrylic.

Recommended policy:

1. Keep `DemoPageBackgroundBrush`, `DemoSampleCardBackgroundBrush`, `DemoSampleRightRailBackgroundBrush`, `DemoSampleSourceHeaderBackgroundBrush`, and `DemoSampleSourceContentBackgroundBrush` as source-of-truth demo tokens.
2. Keep `DemoControlExampleBackgroundBrush` for the requested list/tree/menu sample wells, with High Contrast promoted to a system control fill role.
3. Consider future partial removal only for `DemoSettingsCardBrush` and `DemoSectionCardBrush`, because their light-theme behavior already promotes to default card roles.
4. Do not use the alpha-adjusted variant for sample cards, right rails, source headers, or list/tree/menu sample wells. It removes too much contrast in dark theme.

## Validation And Uncertainty

Automated resource and focused visual-structure tests were run during this pass. No screenshot diff harness for these individual pages was found or generated. The Mica/Acrylic assessment is based on the current brush alpha values and where those brushes are applied, not on side-by-side captured screenshots.

