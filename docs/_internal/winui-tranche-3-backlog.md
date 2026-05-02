# WinUI Tranche-3 Backlog

> **Date:** 2026-04-26
> **Source scanned:** `F:\StagedMigration\microsoft-ui-xaml-main\src\controls\dev\`
> **Status:** Backlog only. Do not implement until user selects a tier/batch.
> **Supersedes:** The WI-6 row in the master plan (`wpf-control-designer-wpf-performance-op-swift-flurry.md`). The WI-6 row is retained in the master plan as a summary; this file is the detailed specification.

---

## 1. Controls

### Tier 1 — High return, small effort (S-size)

| # | Name | WinUI source path | Complexity | Feasibility | Value | Notes |
|---|------|-------------------|:----------:|:-----------:|-------|-------|
| T1.1 | `Repeater` | `dev/Repeater/` | S | HIGH | Functional | Pure-XAML virtualizing layout primitive. Subclass `ItemsControl`, override `ItemsPanel` to `VirtualizingStackPanel`. No WinUI-specific APIs. High reuse value as base for `SelectorBar`, `PagerControl`. |
| T1.2 | `PagerControl` | `dev/PagerControl/` | S | HIGH | Functional | Pagination bar (page numbers + previous/next). Build on `Repeater` or simple `UniformGrid` of `RadioButton`. Renders as a row of numbered pill-buttons. |
| T1.3 | `MenuBar` | `dev/MenuBar/` | S | HIGH | Functional | Horizontal `ItemsControl` wrapping existing `MenuItem` style. Adds `MenuBarItem` header + keyboard mnemonics. Reuses `ContextMenu.xaml` popup. |
| T1.4 | `ImageIcon` | `dev/ImageIcon/` | S | HIGH | Visual | `Image`-facade control for URL/pack-URI icon sources. Mirrors `FontIcon` API shape (`IconFontSize` → `Width`/`Height`, no `Glyph`). Enables icon parity in `NavigationViewItem` for bitmap assets. |
| T1.5 | `RadioButtons` | `dev/RadioButtons/` | S | HIGH | Functional | Horizontal/vertical `RadioButton` group with a `Header` label and `Items`/`ItemsSource`. Simplifies common "tier select" patterns. |
| T1.6 | `SelectorBar` | `dev/SelectorBar/` | S | HIGH | Visual | Pill-button segmented selector (replaces `TabView` for non-content pivots). 3–6 items max. Uses `ToggleButton` style internally. |

### Tier 2 — Valuable, medium effort (M–L-size)

| # | Name | WinUI source path | Complexity | Feasibility | Value | Notes |
|---|------|-------------------|:----------:|:-----------:|-------|-------|
| T2.1 | `AutoSuggestBox` | `dev/AutoSuggestBox/` | M | HIGH | Functional | `TextBox` + `Popup` with filtered suggestions. Requires `ItemsSource`, `TextChanged`/`SuggestionChosen` events. WPF `Popup` + `ListBox` inside; no WinRT dependency. |
| T2.2 | `CommandBarFlyout` | `dev/CommandBarFlyout/` | L | HIGH | Functional | Flyout-anchored toolbar (primary + secondary commands). Requires `Flyout` base (T2.4) and reuses `AppBarButton` style. Popup placement must handle screen-edge clipping. |
| T2.3 | `ItemsView` | `dev/ItemsView/` | L | MEDIUM | Functional | Modern virtualizing layout with pluggable `Layout` (Stack/Grid/Lin). Overlaps existing `ListView`. Recommend as an additive alternative, not a replacement. |
| T2.4 | `Flyout` / `MenuFlyout` | `dev/Flyout/` | M | HIGH | Functional | API shape: `FlyoutBase` → `Flyout` (arbitrary content) and `MenuFlyout` (wraps existing `ContextMenu`). Primarily an API-surface addition; visual already covered by WI-5A.2. |
| T2.5 | `AnnotatedScrollBar` | `dev/AnnotatedScrollBar/` | M | HIGH | Functional | `ScrollBar` with region labels/markers. Override WI-5A.3 `ScrollBar.xaml` to add `PART_Annotations` layer. Useful for long document navigation. |
| T2.6 | `TwoPaneView` | `dev/TwoPaneView/` | M | MEDIUM | Functional | Adaptive dual-pane layout. Can be implemented as `Grid` with two `ColumnDefinition`s that collapse below a `MinWideTrigger` via `AdaptiveTrigger`-equivalent (WPF: code-behind `SizeChanged`). |

### Tier 3 — Niche / blocked (do not schedule without explicit user decision)

| # | Name | Reason blocked / niche |
|---|------|------------------------|
| T3.1 | `ScrollView` | Snap-points, gesture physics; refactor risk against incumbent `ScrollViewer`. |
| T3.2 | `AnimatedIcon` | Lottie dependency — not viable on `net472` without forbidden third-party runtime. |
| T3.3 | `InkCanvas` / `InkToolBar` | WPF has native `InkCanvas`; toolbar requires Fluent restyling only (low-effort follow-on, not a porting task). |
| T3.4 | `SwipeControl` | Touch-gesture-only; non-standard desktop UX. |
| T3.5 | `ParallaxView` | Composition-API (`Visual`/`ExpressionAnimation`) — not available in WPF. |
| T3.6 | `RevealBrush` / `Lights` | Composition-API only. |
| T3.7 | `MapControl` | 3rd-party runtime dependency. |
| T3.8 | `WebView2` | Out of scope (separate SDK). |
| T3.9 | `PullToRefresh` | Touch-primary; desktop ergonomics poor. |

---

## 2. Theme keys to add

Source: `microsoft-ui-xaml-main/src/controls/dev/CommonStyles/Common_themeresources_any.xaml`

### 2.1 Accent fill — secondary/tertiary/quaternary

These three ramp-level brush keys exist in WinUI but are missing from Fluence's `Accent.xaml` / `Brushes.xaml`.

| Key | Light hex | Dark hex | HC value |
|-----|-----------|----------|----------|
| `AccentFillColorSecondaryBrush` | `#CC0078D4` (80 % alpha accent) | `#CC18A0FB` | `SystemHighlight` |
| `AccentFillColorTertiaryBrush` | `#990078D4` (60 % alpha) | `#9918A0FB` | `SystemHighlight` |
| `AccentFillColorQuaternaryBrush` | `#660078D4` (40 % alpha) | `#6618A0FB` | `SystemHighlight` |

Implementation: add to `Accent.xaml` ramp generation (`ApplicationAccentColorManager`) and expose static `SolidColorBrush` keys in `Brushes.xaml` via `DynamicResource`.

### 2.2 Control-on-image fill

Used by controls rendered on top of images (e.g., media player controls on a thumbnail).

| Key | Light hex | Dark hex |
|-----|-----------|----------|
| `ControlOnImageFillColorDefaultBrush` | `#B3FFFFFF` | `#B3000000` |
| `ControlOnImageFillColorSecondaryBrush` | `#80FFFFFF` | `#80000000` |
| `ControlOnImageFillColorTertiaryBrush` | `#66FFFFFF` | `#66000000` |
| `ControlOnImageFillColorDisabledBrush` | `#33FFFFFF` | `#33000000` |

### 2.3 Layer-on-Mica-base alt fill

Used by `NavigationView` pane content sitting on a Mica backdrop (alt = slightly elevated).

| Key | Light hex | Dark hex | HC value |
|-----|-----------|----------|----------|
| `LayerOnMicaBaseAltFillColorDefaultBrush` | `#73FFFFFF` | `#0CFFFFFF` | `Window` |
| `LayerOnMicaBaseAltFillColorSecondaryBrush` | `#66FFFFFF` | `#19FFFFFF` | `Window` |
| `LayerOnMicaBaseAltFillColorTertiaryBrush` | `#4DFFFFFF` | `#26FFFFFF` | `Window` |
| `LayerOnMicaBaseAltFillColorTransparentBrush` | `#00FFFFFF` | `#00FFFFFF` | `Window` |

---

## 3. Animation primitives to add

Source: `WinUI CommonStyles Common_themeresources_any.xaml` + `AnimationConstants.h`

These `KeySpline` resources should be added to `Themes/Typography/Typography.xaml` alongside the existing `ControlFastOutSlowInKeySpline`.

| Resource key | Value | Used by | Notes |
|---|---|---|---|
| `ControlEmphasizedMotionKeySpline` | `0.1, 0.9, 0.2, 1.0` | Large content transitions (page nav, flyout open/close, drawer slide) | "Spring-like" feel; more pronounced deceleration than fast-out-slow-in. |
| `ControlDecelerateKeySpline` | `0.0, 0.0, 0.2, 1.0` | `AutoSuggestBox` dropdown open, `ComboBox` popup enter | Softer decelerate, distinct from `ControlFastOutSlowInKeySpline` (`0.8,0,0,1`) which is more aggressive. |
| `ControlAccelerateKeySpline` | `0.8, 0.0, 1.0, 0.0` | Dismiss / exit animations (flyout close, popup hide) | Mirror of decelerate for exit; fast departure, slow start. |

Also add duration constants (as `Duration` resources):

| Resource key | Value | Notes |
|---|---|---|
| `ControlFastAnimationDuration` | `0:0:0.083` (83 ms) | Micro-interactions: hover color, ripple |
| `ControlNormalAnimationDuration` | `0:0:0.167` (167 ms) | Standard transitions (already used as literal; formalize as resource) |
| `ControlSlowAnimationDuration` | `0:0:0.333` (333 ms) | Large content transitions, emphasized motion |

---

## 4. Backdrop / material gaps

Source: `dev/Materials/DesktopAcrylicBackdrop/`, `dev/Materials/MicaBackdrop/`

Currently `WindowPolicy` exposes `BackdropType` (None/Auto/Mica/Acrylic/Tabbed) but does not expose per-material tuning parameters available in WinUI.

### 4.1 Mica tuning

WinUI `MicaBackdrop` exposes:
- `Kind` = `Base | Alt` (Alt is slightly more opaque, used for floating panels)
- `LuminosityOpacity` (0.0–1.0, default 1.0)
- `TintColor` (ARGB override, default transparent → OS-managed)

**Proposed:** Add `MicaKind` enum (`Base` / `Alt`) to `WindowPolicy` and a `LuminosityOpacity` double DP on `FluenceWindow`. Propagate via `DwmSetWindowAttribute(DWMWA_SYSTEMBACKDROP_TYPE, 2/4)` (2 = Mica Base, 4 = Mica Alt on Win11 22H2+). Win10 fallback: ignore (no DWM support).

### 4.2 Acrylic tuning

WinUI `DesktopAcrylicBackdrop` exposes:
- `Kind` = `Base | Thin` (Thin = 70 % luminosity, Base = 85 %)
- `LuminosityOpacity`
- `TintColor`
- `TintOpacity`

**Proposed:** Add `AcrylicKind` enum (`Base` / `Thin`) and surface `TintColor` / `TintOpacity` DPs on `FluenceWindow`. On Win10 (no `DWMWA_SYSTEMBACKDROP_TYPE`): blend a semi-transparent `SolidColorBrush` over the fallback background using the tint values. On Win11: set the DWM attribute and pass tint via `DwmSetWindowAttribute(DWMWA_MICA_EFFECT)` or compositor-side (if available).

### 4.3 Integration steps

1. Add `MicaKind`, `AcrylicKind` enums to `Fluence.Wpf.Enums`.
2. Add DPs to `FluenceWindow`: `MicaKind`, `AcrylicKind`, `BackdropTintColor`, `BackdropLuminosityOpacity`.
3. Extend `WindowPolicy.BuildBackdropPlan` to accept and apply tint/luminosity parameters.
4. Expose in Demo: `GalleryWindowPage` — add sliders for `LuminosityOpacity` (0.0→1.0) and a `ColorPicker` for `TintColor`.

---

## 5. Behaviors, converters, and automation

### 5.1 `CornerRadiusFilterConverter`

**WinUI usage:** `CornerRadiusFilterConverter` is a 5-line `IValueConverter` used throughout WinUI templates to extract a single corner from a `CornerRadius` (e.g., only bottom corners for a `ComboBox` popup that attaches below its button).

**Implementation:**
```csharp
public class CornerRadiusFilterConverter : IValueConverter
{
    public CornerRadiusFilterMode Filter { get; set; } = CornerRadiusFilterMode.All;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var cr = (CornerRadius)value;
        return Filter switch
        {
            CornerRadiusFilterMode.Top    => new CornerRadius(cr.TopLeft, cr.TopRight, 0, 0),
            CornerRadiusFilterMode.Bottom => new CornerRadius(0, 0, cr.BottomRight, cr.BottomLeft),
            CornerRadiusFilterMode.Left   => new CornerRadius(cr.TopLeft, 0, 0, cr.BottomLeft),
            CornerRadiusFilterMode.Right  => new CornerRadius(0, cr.TopRight, cr.BottomRight, 0),
            _                             => cr,
        };
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => value;
}
public enum CornerRadiusFilterMode { All, Top, Bottom, Left, Right }
```

**Place in:** `Fluence.Wpf/Helpers/CornerRadiusFilterConverter.cs`. Register in `Generic.xaml` resources.

### 5.2 `EnumToBoolConverter` — promote to `Fluence.Wpf`

Currently lives in `Fluence.Wpf.Demo.Mvvm/Converters/EnumToBoolConverter.cs` (demo-only). Should be promoted to `Fluence.Wpf/Helpers/EnumToBoolConverter.cs` so any consumer can use it without referencing the demo project.

**Migration:** Copy to `Fluence.Wpf`, add BSD header, register in `Generic.xaml`, update Demo.Mvvm reference. Zero breaking changes (Demo.Mvvm already has its own copy; after promotion, Demo.Mvvm should reference the library's version).

### 5.3 Focus-ring delay

WinUI hides the focus ring on mouse interaction and shows it only after keyboard navigation (`FocusEngagementStates`). Fluence currently always shows focus rings. Consider adding a `FocusManager.GotKeyboardFocus` + `LostKeyboardFocus` behavior class that toggles a `FocusVisible` attached property — templates can then bind `Visibility` of the focus ring rectangle to `{Binding Path=(helpers:FocusVisibility.IsKeyboardFocused), ...}`.

### 5.4 Automation peer fill-ins

Two controls lack `AutomationPeer` overrides:
- `Card` — should report `AutomationControlType.ListItem` when `IsClickable=True`, `AutomationControlType.Group` otherwise.
- `InfoBar` — should report `AutomationControlType.StatusBar`; expose `IsOpen` as `IsOffscreen` inverse.

Both are small additions to their respective `.cs` files.

---

## 6. Recommended schedule

Three sprint-sized batches (user picks which to approve):

### Sprint A — Theme keys + converters + animation primitives (2–3 days)

Low risk, zero WPF template changes. Pure additions.

1. Add `AccentFillColorSecondary|Tertiary|Quaternary` brush keys to `Accent.xaml` + `Brushes.xaml` (with tests).
2. Add `LayerOnMicaBaseAltFillColor*` and `ControlOnImageFillColor*` keys to Light/Dark/HC `Theme.*.xaml` + `Brushes.xaml`.
3. Add `ControlEmphasizedMotionKeySpline`, `ControlDecelerateKeySpline`, `ControlAccelerateKeySpline` to `Typography.xaml`.
4. Add duration constants (`ControlFastAnimationDuration`, `ControlNormalAnimationDuration`, `ControlSlowAnimationDuration`) to `Typography.xaml`.
5. Add `CornerRadiusFilterConverter` to `Fluence.Wpf/Helpers/`.
6. Promote `EnumToBoolConverter` from Demo.Mvvm to `Fluence.Wpf`.
7. Add automation peers for `Card` + `InfoBar`.

### Sprint B — Tier 1 controls (1 week)

Highest ROI new controls.

1. `ImageIcon` (S)
2. `RadioButtons` (S)
3. `SelectorBar` (S)
4. `MenuBar` + `MenuBarItem` (S, reuses existing `MenuItem`)
5. `PagerControl` (S)

### Sprint C — Tier 2 controls + backdrop tuning (2 weeks)

1. `Flyout` / `MenuFlyout` API shape (M)
2. `AutoSuggestBox` (M)
3. Mica `Kind` (`Base`/`Alt`) + `LuminosityOpacity` on `FluenceWindow` (M)
4. Acrylic `Kind` + `TintColor` / `TintOpacity` on `FluenceWindow` (M)
5. `TwoPaneView` (M)
6. `AnnotatedScrollBar` (M, extends WI-5A.3 ScrollBar)

---

*Items in Tier 3 are explicitly out of scope unless user requests a specific control after evaluating the blocking constraint.*
