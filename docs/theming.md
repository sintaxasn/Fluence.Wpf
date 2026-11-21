# Theme resources

<span id="theme-resources-and-publication"></span>

This page describes the resource keys applications can use. For a first app, begin with the [Basic walkthrough](csharp/usage.md).

This page is the supported resource-key reference for Fluence.Wpf. Use it when styling application content or writing a control template. The complete, build-checked key inventory is [`PublicKeys.txt`](../Fluence.Wpf.Tests/Theming/golden/PublicKeys.txt); the families below explain how to use those keys.

## Initialize and consume resources

Call `ApplicationThemeManager.Apply` before showing the first window. It creates a resource dictionary with colors and brushes, then loads typography and control templates. Theme and accent updates replace the computed dictionary, so use `DynamicResource` for live values:

```xml
<Border Background="{DynamicResource CardBackgroundFillColorDefaultBrush}"
        BorderBrush="{DynamicResource CardStrokeColorDefaultBrush}"
        CornerRadius="{DynamicResource OverlayCornerRadius}" />
```

Each published color normally has a frozen `SolidColorBrush` sibling with `Brush` appended. Use the brush key for brush properties. Some special brush keys have no color sibling, and a few accent brushes intentionally differ from the same-named color's direct conversion. Consult the frozen inventory for exact spelling.

For theme resolution and transition behavior, see [Theme pipeline internals](explanation/theme-pipeline.md).

## Supported key families

| Family | Examples | Use |
| --- | --- | --- |
| Text | `TextFillColorPrimaryBrush`, `TextFillColorSecondaryBrush`, `TextOnAccentFillColorPrimaryBrush`, `AccentTextFillColorPrimaryBrush` | Body text, secondary text, text on accent, and accent text |
| Controls | `ControlFillColorDefaultBrush`, `ControlFillColorSecondaryBrush`, `ControlFillColorInputActiveBrush`, `ControlFillColorDisabledBrush` | Control surfaces in normal and active states |
| Alternate and subtle fills | `ControlAltFillColorSecondaryBrush`, `ControlAltFillColorTertiaryBrush`, `SubtleFillColorSecondaryBrush` | Tracks, passive fills, and low emphasis states |
| Shared accent fills | `AccentFillColorDefaultBrush`, `AccentFillColorSecondaryBrush`, `AccentFillColorTertiaryBrush`, `AccentFillColorDisabledBrush` | Accent surfaces in Light and Dark; neutral window surfaces in High Contrast |
| Button accent states | `AccentButtonBackgroundBrush`, `AccentButtonBackgroundPressedBrush`, `ToggleButtonBackgroundCheckedBrush`, `SplitButtonBackgroundCheckedBrush` | Button, drop down, split, and toggle split templates |
| Other control accent states | `CheckBoxCheckBackgroundFillCheckedBrush`, `CheckBoxCheckBackgroundStrokeIndeterminatePointerOverBrush`, `RadioButtonOuterEllipseCheckedFillBrush`, `RadioButtonOuterEllipseCheckedStrokeBrush`, `ToggleSwitchFillOnBrush`, `SliderTrackValueFillBrush`, `HyperlinkButtonForegroundBrush` | Checked, selected, and linked control states with their own High Contrast roles |
| Strokes and focus | `ControlStrokeColorDefaultBrush`, `ControlStrongStrokeColorDefaultBrush`, `CardStrokeColorDefaultBrush`, `SurfaceStrokeColorDefaultBrush`, `DividerStrokeColorDefaultBrush`, `FocusStrokeColorOuterBrush`, `FocusStrokeColorInnerBrush` | Edges, dividers, and focus visuals |
| Background and layers | `ApplicationBackgroundBrush`, `SolidBackgroundFillColorBaseBrush`, `LayerFillColorDefaultBrush`, `CardBackgroundFillColorDefaultBrush` | Page, layer, and card surfaces |
| Status | `SystemFillColorSuccessBrush`, `SystemFillColorCautionBrush`, `SystemFillColorCriticalBrush`, `SystemFillColorAttentionBrush`, `SystemFillColorNeutralBrush` | Feedback and severity surfaces |
| Accent ramp | `SystemAccentColor`, `SystemAccentColorLight1` through `Light3`, `SystemAccentColorDark1` through `Dark3` | Raw Windows or custom accent ramp |
| Theme-resolved accent | `SystemAccentColorPrimary`, `SystemAccentColorSecondary`, `SystemAccentColorTertiary`, and brush variants | Accent roles selected for the resolved theme |
| Chrome | `TitleBarActiveColor`, `TitleBarInactiveColor`, `WindowBorderColor`, and brush variants | WPF window frame |

The named families contain more keys than the examples shown. The public inventory also includes the `ControlOnImageFillColor*`, `LayerOnMicaBaseAltFillColor*`, acrylic, card, background, and InfoBadge foreground families. `AcrylicBackgroundFillColorDefaultBrush` and `AcrylicBackgroundFillColorBaseBrush` provide painted fallbacks for transient surfaces. `AccentAcrylicBackgroundFillColor*` supplies accent tinted variants. These legacy solid brush keys describe flat surfaces; they do not themselves blur an element's backdrop. For an explicit same-window source, use the separate [in-app acrylic material](controls.md#acrylic-materials).

### Acrylic recipes

After `ApplicationThemeManager.Apply`, the computed dictionary publishes these frozen `AcrylicMaterialRecipe` values. Each host supplies its own same-window source.

| Material | Default | Base |
| --- | --- | --- |
| Neutral | `AcrylicInAppDefaultRecipe` | `AcrylicInAppBaseRecipe` |
| Accent | `AcrylicInAppAccentDefaultRecipe` | `AcrylicInAppAccentBaseRecipe` |

Accent tint and opaque fallback use the resolved accent ramp. In Light, both recipes use Light3. In Dark, Default uses Dark1 and Base uses Dark2. All four recipes use the system Window color as their solid fallback in High Contrast.

```xml
<fluence:AcrylicElement BackdropSource="{Binding ElementName=BackdropRoot}"
                        Recipe="{DynamicResource AcrylicInAppDefaultRecipe}" />
```

Use `DynamicResource` on the host's `Recipe` dependency property to follow theme and accent changes. The Colors gallery shows the accent pair under **Background > Accent Acrylic Background**. For a direct `Border.Background` or supported shape fill, pass a fixed `StaticResource` recipe to `{fluence:AcrylicBrush ...}`. The direct markup extension cannot reevaluate a `DynamicResource` nested in its CLR property. The [rooted example and source rules](controls.md#acrylic-materials) show both patterns. Existing `AcrylicBackground*Brush` keys, `AcrylicNoiseBrush`, popup paints, and window DWM backdrops retain their own behavior.

Recipe, theme, and policy refreshes switch directly between acrylic material and its opaque fallback. The 167 ms `ControlFastAnimationDuration` token does not animate this material. The [material limits](controls.md#acrylic-materials) apply to direct brushes and hosts together across windows in the process.

When a `TitleBar` is hosted by `FluenceWindow`, it follows the window's active state. Built-in title text uses the tertiary text role while inactive, and its title-area content slots use 0.5 opacity.

### Control state keys

| Key family | Light and Dark role | High Contrast role |
| --- | --- | --- |
| `RadioButtonOuterEllipseCheckedStroke*` | Checked ring accent states | Highlight, ControlText, Control, and GrayText by state |
| `TextControlElevationBorderFocusedBrush` | Focused editor border | Uses the active system contrast roles |
| `CheckBoxCheckBackgroundFillIndeterminate*`, `CheckBoxCheckGlyphForegroundIndeterminate*` | Accent plate and on-accent dash | State-specific Highlight, HighlightText, GrayText, and Window roles |
| `NavigationViewItemPointerOver*Brush`, `NavigationViewItemPressed*Brush` | Existing subtle fill and text roles for nonselected items | Paired system Highlight and HighlightText colors for both interaction states |

### High contrast roles

These brush-only keys map to live WPF `SystemColors` when high contrast resources are published:

`SystemColorWindowTextColorBrush`, `SystemColorWindowColorBrush`, `SystemColorButtonFaceColorBrush`, `SystemColorButtonTextColorBrush`, `SystemColorHighlightColorBrush`, `SystemColorHighlightTextColorBrush`, `SystemColorHotlightColorBrush`, and `SystemColorGrayTextColorBrush`.

The InfoBadge foreground family also changes with high contrast. Use the published role keys instead of hard-coded colors. A settings broadcast causes the theme engine to read a new `SystemColors` snapshot.

In high contrast, WinUI's shared `AccentFillColor*` brushes use the live window color, while `AccentTextFillColor*` and `TextOnAccentFillColor*` use window text; disabled text uses gray text. Interactive controls use separate state resources for highlight, hover, pressed, and disabled visuals. Brush-only roles such as `SystemControlHighlightAccentBrush` and `TextControlSelectionHighlightBrush` also follow the live highlight color. Fluence publishes these resources in the computed dictionary so existing controls and custom templates update when the Windows contrast scheme changes. The raw `SystemAccentColor` ramp remains available for inspection and custom accent intent, but does not determine high-contrast control states.

### Typography, geometry, and motion

`Themes/Typography/Typography.xaml` publishes `CaptionTextBlockStyle`, `BodyTextBlockStyle`, `BodyStrongTextBlockStyle`, `BodyLargeTextBlockStyle`, `SubtitleTextBlockStyle`, `TitleTextBlockStyle`, `TitleLargeTextBlockStyle`, and `DisplayTextBlockStyle`. It also publishes `ContentControlThemeFontFamily`.

Fluence `TextBlock.Typography` selects the named font-size ramp, including Body (14), Title (28), TitleLarge (40), and Display (68). An explicit `FontSize` set locally, by a binding, or by an application style takes precedence. Removing that override restores the named size. The font family continues to follow `ContentControlThemeFontFamily` through theme changes.

Supported radii include `ControlCornerRadius` (4), `OverlayCornerRadius` (8), `ComboBoxItemCornerRadius` (3), and `PopupCornerRadius`. Motion tokens include `ControlFasterAnimationDuration` (83 ms), `ControlFastAnimationDuration` (167 ms), `ControlNormalAnimationDuration` (250 ms), and `ControlFastOutSlowInKeySpline`. `ControlPressAnimationDuration` (100 ms) and `ControlSlowAnimationDuration` (333 ms) are Fluence extensions of the naming pattern.

`FlyoutShadowEffect` is a shared WPF fallback for transient surfaces. It uses a fixed black shadow with blur radius 18, opacity 0.22, direction 270 degrees, and depth 4. WinUI's `ThemeShadow` varies with theme and semantic elevation; WPF's `DropShadowEffect` does not reproduce that compositor behavior. Treat this value as an approximation. In custom templates, place it on an empty sibling behind the text surface so WPF text rendering is not affected by an `Effect` ancestor. See [parity boundaries](winui-parity.md#color-alpha-and-elevation-fidelity).

### Supported aliases

`FluentFontFamily` aliases `ContentControlThemeFontFamily`. `ApplicationPageBackgroundThemeBrush` aliases `ApplicationBackgroundBrush`. Both names in each pair are supported; prefer the WinUI-style name in new code where it conveys the same intent. The eight `SystemColor*Brush` values above are platform role aliases rather than aliases of another Fluence key.

## How theme publication works

See [Theme pipeline internals](explanation/theme-pipeline.md) for resolution, publication, and transition behavior.

## Resource ownership and lookup

After initialization, `Application.Current.Resources.MergedDictionaries` has three Fluence slots:

| Slot | Content | Update behavior |
| --- | --- | --- |
| `[0]` | Computed colors, brushes, and special brush roles | Replaced when published output changes |
| `[1]` | `Themes/Typography/Typography.xaml` | Reused while the owned resource stack remains installed |
| `[2]` | `Themes/Generic.xaml` | Reused while the owned resource stack remains installed |

The engine owns slots `[0]` through `[2]`. If application resources are replaced or those slots are cleared, reordered, or replaced, the next apply repairs the owned stack even when the requested theme has not changed. Other application dictionaries are retained after those slots in their existing relative order. Do not treat an exact total dictionary count as part of the contract.

The per-theme `Themes/Colors/Theme.*.xaml` files are color tables read by the engine, not dictionaries to merge into an application. Never merge `Generic.xaml` manually after calling the theme manager. The [pipeline explanation](explanation/theme-pipeline.md) traces resolution and publication.

Only the computed keys, typography keys, and the two focus styles `DefaultControlFocusVisualStyle` and `DefaultCollectionFocusVisualStyle` are supported app resources. `Generic.xaml` merges control-template dictionaries whose other `x:Key` entries are implementation details. The computed control accent state keys describe WinUI roles used by Fluence templates; changing one globally affects every consumer of that key. Replace a template to restyle only one control.

The color keys `SystemAccentColorPrimary`, `Secondary`, and `Tertiary` express theme-resolved roles. Their brush names are not always direct color twins: the engine deliberately publishes different ramp shades for some brush roles. Bind the specific resource whose value type and role you need.

## Theme-aware markup

`{fluence:ThemeResource Key}` is a WPF `DynamicResource`-based way to make WinUI-style theme intent explicit. It refreshes when the engine publishes new resources. App-authored per-theme values can be placed in `ThemeDictionary` with `ThemeResourceDictionary` tables keyed `Light`, `Dark`, or `HighContrast`. Use `DynamicResource` or `ThemeResource` to consume those values. See the XML documentation in [`ThemeDictionary.cs`](../Fluence.Wpf/Markup/ThemeDictionary.cs) for an XAML example and fallback behavior.

## Accent, backdrop, and design time

For accent selection and exact colors, see [Theme and accent](how-to/theme-and-accent.md#pin-a-brand-accent) and [accent resolution](explanation/theme-pipeline.md#accent-resolution).

The XAML designer cannot run the full theme engine. The library includes generated Light and Dark design-time resource dictionaries under `Fluence.Wpf/Properties/`, plus `DesignTimeResources.xaml` for default preview. These are snapshots for the designer and are not merged at runtime. Check the final appearance in the gallery, especially high contrast and DWM surfaces.

## Gallery appearance examples

See the [theme and accent guide](how-to/theme-and-accent.md#gallery-appearance-examples) for gallery captures.
