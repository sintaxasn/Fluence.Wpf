# WI-2 audit findings (Steps 2.3 - 2.5)

> Internal working document. Not part of the public `docs/` set per CLAUDE.md §10.

## Scope

- [Fluence.Wpf/Controls/WindowPolicy.cs](../../Fluence.Wpf/Controls/WindowPolicy.cs) (300 lines) — **Step 2.3**
- [Fluence.Wpf/Themes/Controls/FluenceWindow.xaml](../../Fluence.Wpf/Themes/Controls/FluenceWindow.xaml) (293 lines) — **Step 2.4**
- [Fluence.Wpf/Controls/CaptionButtonChrome.cs](../../Fluence.Wpf/Controls/CaptionButtonChrome.cs) (99 lines) — **Step 2.5**
- [Fluence.Wpf/Controls/TitleBar.cs](../../Fluence.Wpf/Controls/TitleBar.cs) (129 lines) — **Step 2.5**
- [Fluence.Wpf/Controls/FluenceWindow.cs](../../Fluence.Wpf/Controls/FluenceWindow.cs) (1093 lines) — **Step 2.5** (WndProc hit-test correctness)

## Summary

**No functional defects.** The audit surfaced four convention-level findings; none alter observable behaviour, none break the PSADT consumer contract (`ApplicationThemeManager.Apply`, `.Changed`, `.CurrentTheme`; `ApplicationAccentColorManager.ApplyCustomAccent`; `IsMinimizeButtonVisible`, `IsMinimizable`, `IsMoveable`, `SystemBackdropType`, `CornerStyle`, `TitleBarHeight`, `ExtendsContentIntoTitleBar`, `ShowIcon`, `ShowTitle`). The older `MinimizeButtonVisibility` and `CanMove` names are retained as obsolete FluenceWindow aliases.

**Remediated 2026-05-02.** Findings 1-3 are now landed in `FluenceWindow.xaml`, `FluenceWindow.cs`, the managed theme dictionaries, and regression tests. Finding 4 remains an accepted WPF-specific divergence.

---

## Step 2.3 — WindowPolicy.cs

**Clean.** All DWM values flow through `Fluence.Wpf.Native.NativeConstants.*` — no magic numbers in the policy layer. `DWMWA_USE_IMMERSIVE_DARK_MODE` (20), `DWMWA_SYSTEMBACKDROP_TYPE` (38), `DWMWA_WINDOW_CORNER_PREFERENCE` (33), `DWMSBT_*` (0-4), and `DWMWCP_*` (0-3) are all referenced by symbol.

`ResolveEffectiveBackdrop` correctly downgrades when `WindowCapabilities` reports the OS cannot support the requested backdrop — Mica on pre-22H2, Acrylic/Tabbed on pre-1809 — so policy compiles even against Windows 10 1809 baseline without runtime failure.

`CreateWindowChrome` emits `NonClientFrameEdges.None`, `ResizeBorderThickness = new Thickness(4)`, `UseAeroCaptionButtons = false`. Matches .NET 10 WPF's `FluentWindow` chrome shape.

No `StaticResource` usage in this file (it is pure C#, no XAML).

---

## Step 2.4 — FluenceWindow.xaml

**Clean on theme reactivity.** Every theme-reactive brush in the default template is bound via `DynamicResource`; no stale brushes survive a theme swap. Checked all 40+ brush references.

**Four convention-level findings:**

### Finding 1 — close button hover/pressed hex is hard-coded

Original audit observation (`WindowCloseButtonStyle`): `#C42B1C` (hover), `#B4271C` (pressed), `#FFFFFF` (foreground on hover/pressed) were inlined in the control template.

**Assessment:** The CommonStyles counterpart is `TitleBarCloseButtonBackgroundPointerOver` (which is also always that same red across Light/Dark/HighContrast because the system convention is "close button is always red on hover"). The values are correct, but CLAUDE.md §2 bans hard-coded hex in production templates.

**Recommendation:** Add three brush keys (`WindowCloseButtonBackgroundPointerOver`, `WindowCloseButtonBackgroundPressed`, `WindowCloseButtonForegroundPointerOver`) to `Themes/Brushes/Brushes.xaml` and `Themes/Colors/Theme.{Light|Dark|HighContrast}.xaml`, swap the hard-coded hex for `DynamicResource` references. Preserve the existing red values across all three themes.

**Status:** Remediated. The template now consumes `WindowCloseButtonBackgroundPointerOverBrush`, `WindowCloseButtonBackgroundPressedBrush`, and `WindowCloseButtonForegroundPointerOverBrush`; color values live in the three managed theme dictionaries.

**Severity:** S (small, single template + 4 dictionary edits).

### Finding 2 — caption buttons lack `PART_` prefix on `x:Name`

Original audit observation: the caption buttons were named `MinimizeButton`, `MaximizeButton`, `RestoreButton`, `CloseButton`.

**Assessment:** CLAUDE.md §2 mandates `PART_Whatever` naming for template parts wired up in `OnApplyTemplate`. The old `x:Name` values were referenced directly from `FluenceWindow.OnApplyTemplate` via `GetTemplateChild("MinimizeButton")` etc.; the lookup worked, but the naming convention drifted from the rest of the library.

**Recommendation:** Rename to `PART_MinimizeButton`, `PART_MaximizeButton`, `PART_RestoreButton`, `PART_CloseButton`. Update `FluenceWindow.OnApplyTemplate` accordingly. Add `[TemplatePart(Name = "PART_MinimizeButton", Type = typeof(Button))]` etc. to the class — see Finding 3.

**Status:** Remediated. The default template now names all four caption buttons with the `PART_` prefix and tests locate those names.

**Severity:** S (rename in two files).

### Finding 3 — no `[TemplatePart]` attributes on FluenceWindow class

Original audit observation: [FluenceWindow.cs](../../Fluence.Wpf/Controls/FluenceWindow.cs) had no `[TemplatePart]` attributes.

**Assessment:** CLAUDE.md §2: "Template parts: `const string PART_Whatever = "PART_Whatever"`; annotate the class with `[TemplatePart(Name = PART_..., Type = typeof(T))]`."

**Recommendation:** Co-land with Finding 2. Emit `const string PART_MinimizeButton = "PART_MinimizeButton"` etc. and attribute the class.

**Status:** Remediated. `FluenceWindow` declares caption-button template-part constants and `[TemplatePart]` metadata for all four buttons.

**Severity:** S (co-land with Finding 2).

### Finding 4 — Maximize and Restore are separate Button elements

Rather than a single `PART_MaximizeRestoreButton` that swaps glyph via visual state (WinUI 3 idiom), the template has two Buttons (`PART_MaximizeButton` + `PART_RestoreButton`) that toggle `Visibility` via a Boolean-to-Visibility converter bound to `IsMaximized`.

**Assessment:** WPF-idiomatic. Two separate buttons avoid the need for a visual state on glyph content (which is awkward in WPF Path-based templates), and command routing is cleaner because each button binds to its own `SystemCommands.Maximize` / `.Restore`. `CaptionButtonChrome.GetMaximizeRestoreChrome` returns the visibility pair.

**Decision:** **Keep as-is.** This is an approved divergence from WinUI 3. The convention is documented in `CaptionButtonChrome.cs`.

**Severity:** N/A — accepted divergence.

---

## Step 2.5 — CaptionButtonChrome.cs, TitleBar.cs, FluenceWindow.cs WndProc

### CaptionButtonChrome.cs

**Clean.** `internal static` with three pure functions (`GetMinimizeChrome`, `GetCloseChrome`, `GetMaximizeRestoreChrome`) that return `(Visibility, bool isEnabled)` computed from `ResizeMode` + `WindowState` + explicit DP overrides. No state, no mutation, no allocations per frame.

### TitleBar.cs

**Clean.** `public class TitleBar : Control` with 7 DPs (`Title`, `Icon`, `IsBackButtonVisible`, `IsCompact`, `CustomContent`, `BackCommand`, `BackCommandParameter`). Not on the PSADT consumption surface — PSADT uses `FluenceWindow.TitleBar` (the `UIElement` DP) to inject custom content directly, not this child control.

### FluenceWindow.cs WndProc (lines 785-913)

**Clean.** Verified message handling:

| Msg                  | Handling                                                                                           |
|----------------------|----------------------------------------------------------------------------------------------------|
| `WM_NCHITTEST`       | `HitTestTitleBar` returns `HTMAXBUTTON` over maximize/restore (Win11 snap), `0` over minimize/close (fall through to WPF `Button.Click`), `HTCAPTION` elsewhere in title bar — drag-to-move works, hover-to-show-snap-flyout works. |
| `WM_NCMOUSELEAVE`    | `ClearSnapHover` reverts button background/foreground. No sticky highlight on mouse-out.          |
| `WM_NCLBUTTONUP`     | `HTMAXBUTTON` clicks routed to `SystemCommands.Maximize/Restore` based on current `WindowState`.  |
| `WM_GETMINMAXINFO`   | Respects `MaxWidth` / `MaxHeight` / `MinWidth` / `MinHeight` with DPI-correct Screen → Window unit conversion. Prevents maximized window from overflowing taskbar on multi-monitor Win11 with per-monitor DPI. |

`SetSnapHover` uses canonical `ControlStrongFillColorDefaultBrush` + `TextFillColorInverseBrush` via `TryFindResource`, with a sensible fallback to `Brushes.Transparent` when keys are unresolved. No hard-coded colors.

`OnMaximizeWindow` / `OnMinimizeWindow` / `OnRestoreWindow` (lines 1064-1089) use the belt-and-braces pattern (`WindowState = ...; NativeMethods.*WindowNative(_handle);`) documented in the inline comment at lines 1043-1063. The rationale for avoiding `SystemCommands.*Window` (WM_SYSCOMMAND is gated by WS_SYSMENU / WS_MINIMIZEBOX / WS_MAXIMIZEBOX, which are intentionally stripped) is sound and correct.

---

## Gate for Step 2.6

The audit uncovered **zero** functional defects. Step 2.6 (TDD harden) therefore proceeds on the base assumption that the existing behaviour is correct; every new MSTest is a regression floor, not a bug repro.

Test plan for Step 2.6:

1. **Backdrop swap** — `SystemBackdropType=None` → `Acrylic` → `Mica` → `Tabbed` → `None`; assert `WindowPolicy.ResolveEffectiveBackdrop` downgrades on capability failure; assert no exception when window is shown.
2. **Theme swap while shown** — Light → Dark → HighContrast → Light; assert caption-button brush keys resolve to fresh brushes each swap (not stale references from previous theme).
3. **Caption button routing** — synthesize `Click` on `PART_MinimizeButton` / `PART_MaximizeButton` / `PART_RestoreButton` / `PART_CloseButton`; assert `WindowState` transitions correctly. (`F4` from WI-1C is landed here.)
4. **DPI change** — raise `DpiChanged`; assert caption height DP stays canonical and glyph container scales proportionally.
5. **Win11 snap hit-test** — call `HitTestTitleBar` with points inside `PART_MaximizeButton` bounds; assert returns `HTMAXBUTTON`; inside `PART_MinimizeButton` bounds; assert returns `0`.

Findings 1-3 are landed and covered by `FluenceWindowHardenTests` plus caption-button routing tests. Finding 4 stays as-is.
