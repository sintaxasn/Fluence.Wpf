# Fluence.Wpf — WinUI3 Control Coverage

This document maps [WinUI 3 controls](https://learn.microsoft.com/en-us/windows/apps/design/controls/) to **Fluence.Wpf** implementations. Use it to plan feature work and set expectations for API parity.

## Status Key

- **Implemented** — Styled control or equivalent shipped in this library
- **Partial** — Subset of WinUI behavior or different base type
- **Not implemented** — No Fluence.Wpf type yet
- **N/A** — Not applicable to WPF (platform-specific or replaced by WPF primitives)

**Difficulty (for future work):** Easy | Medium | Hard  
**Priority:** Typical line-of-business WPF app demand (High / Medium / Low)

---

## Basic Input

| WinUI3 Control | Fluence.Wpf Status | Notes |
|----------------|-------------------|-------|
| Button | Implemented | `Fluence.Wpf.Controls.Button` — Appearance, CornerRadius, Icon, IconPlacement |
| DropDownButton | Not implemented | Medium, High |
| HyperlinkButton | Implemented | `HyperlinkButton` — NavigateUri, Icon |
| RepeatButton | Not implemented | Easy, Medium |
| ToggleButton | Not implemented | Medium, High |
| ToggleSplitButton | Not implemented | Hard, Medium |
| SplitButton | Not implemented | Hard, Medium |
| CheckBox | Implemented | `CheckBox` — Description, three-state |
| RadioButton | Implemented | `RadioButton` — Description |
| ComboBox | Implemented | `ComboBox` — Placeholder, Icon |
| ToggleSwitch | Implemented | `ToggleSwitch` |
| Slider | Implemented | `Slider` — horizontal/vertical |
| RatingControl | Not implemented | Medium, Low |
| ColorPicker | Not implemented | Hard, Low |

---

## Text

| WinUI3 Control | Fluence.Wpf Status | Notes |
|----------------|-------------------|-------|
| TextBlock | N/A | Use WPF `TextBlock` + `TextBlockExtensions` (Typography, trimming, placeholder) |
| TextBox | Implemented | `TextBox` — validation, helper text, icon |
| RichEditBox | Not implemented | Hard, Medium |
| PasswordBox | Implemented | `PasswordBox` — reveal |
| AutoSuggestBox | Not implemented | Hard, High |
| NumberBox | Not implemented | Medium, High |

---

## Collections

| WinUI3 Control | Fluence.Wpf Status | Notes |
|----------------|-------------------|-------|
| ListView | Implemented | `ListView` — empty state, transitions |
| GridView | Not implemented | Medium, Medium |
| TreeView | Not implemented | Hard, High |
| DataGrid | Not implemented | Hard, High |
| ItemsRepeater | Not implemented | Hard, Medium |
| FlipView | Not implemented | Medium, Low |
| ListBox | Partial | WPF `ListBox` with default styles where merged |

---

## Date & Time

| WinUI3 Control | Fluence.Wpf Status | Notes |
|----------------|-------------------|-------|
| CalendarDatePicker | Not implemented | Medium, High |
| CalendarView | Not implemented | Hard, Medium |
| DatePicker | Not implemented | Medium, High |
| TimePicker | Not implemented | Medium, High |

---

## Menus & Toolbars

| WinUI3 Control | Fluence.Wpf Status | Notes |
|----------------|-------------------|-------|
| MenuBar | Not implemented | Medium, High |
| MenuFlyout | Not implemented | Medium, High |
| CommandBar | Not implemented | Medium, High |
| CommandBarFlyout | Not implemented | Hard, Medium |
| AppBarButton | Not implemented | Medium, Medium |
| AppBarToggleButton | Not implemented | Medium, Medium |
| AppBarSeparator | Not implemented | Easy, Medium |

---

## Dialogs & Flyouts

| WinUI3 Control | Fluence.Wpf Status | Notes |
|----------------|-------------------|-------|
| ContentDialog | Not implemented | Hard, High |
| Flyout | Not implemented | Medium, High |
| TeachingTip | Not implemented | Medium, Low |
| ToolTip | N/A | WPF `ToolTip` |

---

## Navigation

| WinUI3 Control | Fluence.Wpf Status | Notes |
|----------------|-------------------|-------|
| NavigationView | Implemented | `NavigationView` / `NavigationViewItem` — Top/Left modes |
| TabView | Not implemented | Partial: `TabControl` styling only |
| BreadcrumbBar | Not implemented | Medium, Medium |
| Pivot | Not implemented | Medium, Low |
| Hub | Not implemented | Not typical for WPF LOB |

---

## Status & Information

| WinUI3 Control | Fluence.Wpf Status | Notes |
|----------------|-------------------|-------|
| ProgressBar | Implemented | `ProgressBar` — determinate, indeterminate, step mode |
| ProgressRing | Implemented | `ProgressRing` |
| InfoBar | Implemented | `InfoBar` — severity, action |
| InfoBadge | Not implemented | Medium, Medium |
| Expander | Not implemented | Medium, High |
| Tooltip | N/A | WPF `ToolTip` |

---

## Layout

| WinUI3 Control | Fluence.Wpf Status | Notes |
|----------------|-------------------|-------|
| Border | Implemented | `Fluence.Wpf.Controls.Border` — variants |
| Canvas | N/A | WPF `Canvas` |
| Grid | N/A | WPF `Grid` |
| StackPanel | Implemented | `StackPanel` — spacing |
| RelativePanel | Not implemented | Medium, Medium |
| VariableSizedWrapGrid | Not implemented | Medium, Low |
| ViewBox | N/A | WPF `ViewBox` |
| ScrollViewer | Partial | `SmoothScrollViewer` optional smooth scrolling |
| SplitView | Not implemented | Medium, Medium |

---

## Media

| WinUI3 Control | Fluence.Wpf Status | Notes |
|----------------|-------------------|-------|
| Image | N/A | WPF `Image` |
| MediaPlayerElement | Not implemented | Hard, Low |
| PersonPicture | Not implemented | Medium, Medium |
| AnimatedIcon | Not implemented | Hard, Low |

---

## Windowing

| WinUI3 Control | Fluence.Wpf Status | Notes |
|----------------|-------------------|-------|
| Window | Implemented | `FluentWindow` — Mica/Acrylic/Tabbed, corners, caption overrides |
| TitleBar | Partial | Custom chrome via `FluentWindow` + caption button templates |

---

## Recommended Next Controls (priority order)

1. **NumberBox** — Common for settings and forms; Medium difficulty.
2. **ContentDialog** — Needed for modern confirmations; High priority for UX parity.
3. **MenuBar / MenuFlyout** — Essential for desktop apps; Medium difficulty each.
4. **AutoSuggestBox** — Search and pickers; High value, higher effort.
5. **TreeView** — Hierarchical data; High demand in enterprise tools.
6. **DataGrid** — Large tabular data; consider partnering with existing WPF DataGrid styling.
7. **AppBar / CommandBar** — Toolbar patterns for document apps.
8. **SplitView** — Master/detail and nav pane without full NavigationView.
9. **CalendarDatePicker / DatePicker / TimePicker** — Scheduling LOB scenarios.
10. **Expander** — Frequently used in settings panels.
11. **ToggleButton / RepeatButton** — Smaller gaps in basic input.
12. **InfoBadge** — Notifications on nav items.
13. **BreadcrumbBar** — Deep navigation hierarchy.
14. **RatingControl** — Niche but easy showcase control.
15. **DropDownButton / SplitButton** — Toolbar and form actions.

---

*Last updated with Fluence.Wpf initial open-source release.*
