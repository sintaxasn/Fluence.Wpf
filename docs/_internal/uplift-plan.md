# Fluence.Wpf Control Uplift Plan

> Internal working document. Not part of the public `docs/` set per CLAUDE.md §10.

**Status:** 21/21 complete — all items done  
**Verified:** 2026-04-25 scan against HEAD  
**Baseline (post WI-3):** net10 = 423 passed / net472 = 422 passed  
**Authority:** WinUI 3 CommonStyles → in-tree precedent → .NET 10 WPF Themes  

Priority tiers: HIGH → must fix before release | MEDIUM → fix in this cycle | LOW → defer

Legend: ✅ done (verified in code) | ❌ pending | ⚠️ partial

---

## Batch A — Quick visual fixes (S-size, single-file, ≤20 lines each)

| # | Status | Control | Finding | WinUI 3 anchor | Severity | Size |
|---|--------|---------|---------|----------------|----------|------|
| 1 | ✅ | Button | Per-control focus visual is duplicated; replace with `{DynamicResource DefaultControlFocusVisualStyle}` | Button_themeresources.xaml | visual | S |
| 2 | ✅ | CheckBox | Same focus-visual dedup as #1 | CheckBox_themeresources.xaml | visual | S |
| 3 | ✅ | RadioButton | Same focus-visual dedup | RadioButton_themeresources.xaml | visual | S |
| 4 | ✅ | ToggleButton | Same focus-visual dedup | ToggleButton_themeresources.xaml | visual | S |
| 5 | ✅ | ComboBox | Popup border missing `CornerRadius="{DynamicResource OverlayCornerRadius}"` | ComboBox_themeresources.xaml | visual | S |
| 6 | ✅ | DropDownButton | Popup border same CornerRadius gap | DropDownButton_themeresources.xaml | visual | S |
| 7 | ✅ | NumberBox | Spinner button layout margin vs WinUI spec off by 2px (current: `Margin="0,1,0,1"` on SpinPanel) | NumberBox_themeresources.xaml | visual | S |
| 8 | ✅ | HyperlinkButton | Missing `TextDecoration` on PointerOver (underline should appear) | HyperlinkButton_themeresources.xaml | visual | S |
| 9 | ✅ | ProgressBar | Indeterminate timing: Fluence uses 1.2s; WinUI canonical is 2.0s/2.4s stagger | ProgressBar_themeresources.xaml | motion | S |
| 10 | ✅ | InfoBadge | `DisplayKindStates` VSM group missing; uses triggers only | InfoBadge_themeresources.xaml | visual | S |

---

## Batch B — VSM ports (M-size, multi-state animations)

| # | Status | Control | Finding | WinUI 3 anchor | Severity | Size |
|---|--------|---------|---------|----------------|----------|------|
| 11 | ✅ | Slider | Thumb scale: Fluence 1.0→1.1; WinUI canonical 1.0→1.167 (hover) →0.86 (pressed) with ControlFastOutSlowIn easing | Slider_themeresources.xaml:263-350 | visual+motion | M |
| 12 | ✅ | ToggleSwitch | Knob translate storyboard missing SpringEase equivalent; uses LinearEase | ToggleSwitch_themeresources.xaml | motion | M |
| 13 | ✅ | Expander | Chevron rotate missing ControlFastOutSlowIn easing; uses instant flip | Expander_themeresources.xaml | motion | M |
| 14 | ✅ | InfoBar | `SeverityLevels` VSM group present but severity-swap has no cross-fade (0.1s) | InfoBar_themeresources.xaml | visual | M |
| 15 | ✅ | NavigationView | Pane header background should be `LayerFillColorAltBrush`; back-button not in VSM group | NavigationView_themeresources.xaml | visual | M |
| 16 | ✅ | TabView | Missing `PART_ScrollBackButton` + `PART_ScrollForwardButton` scroll controls per WinUI spec | TabView_themeresources.xaml | behavioral | M |
| 17 | ✅ | SplitButton | Primary/secondary divider stroke not using `ControlStrokeColorOnAccentSecondaryBrush` on accent state | SplitButton_themeresources.xaml | visual | M |

---

## Batch C — Large reworks (L-size, template restructure)

| # | Status | Control | Finding | WinUI 3 anchor | Severity | Size |
|---|--------|---------|---------|----------------|----------|------|
| 18 | ✅ | ComboBox | Full VSM port: `FocusedStates`, `EditableFocusedStates` missing (only WPF `FocusStates` group present) | ComboBox_themeresources.xaml | behavioral | L |
| 19 | ✅ | TextBox | `PlaceholderText` not using `TextFillColorTertiaryBrush`; no reveal-password variant | TextBox_themeresources.xaml | visual | L |
| 20 | ✅ | ListView / ListViewItem | Selection indicator (3×16 accent bar) missing | ListView_themeresources.xaml | visual | L |
| 21 | ✅ | Card | `CardVariant.Subtle` missing distinct elevation shadow; all variants look identical | WinUI: no Card, use InfoBar/Expander pattern | visual | L |

---

## Execution rules (per approved item)

1. Read target `.cs` + template `.xaml` in full.
2. Read the WinUI 3 CommonStyles counterpart for the control (`microsoft-ui-xaml/src/controls/dev/CommonStyles`) plus any matching `.NET WPF` theme source when WPF translation details matter.
3. TDD: add failing MSTest encoding the acceptance criterion.
4. Implement minimal change.
5. Build + test (net10 first, then net472).
6. Visual verification in Demo (Light / Dark / HC × Normal / PointerOver / Pressed / Focused / Disabled × 100% / 150% DPI).
7. Request a reviewer-agent audit or equivalent code review.
8. Commit per item. Do not batch.
