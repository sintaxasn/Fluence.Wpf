---
title: Controls
linkTitle: Controls
description: Catalog of Fluence.Wpf controls aligned with the demo gallery, with XAML snippets and template contracts.
weight: 30
---

The **Fluence.Wpf.Demo** gallery is the easiest way to browse the control set: `FluenceWindow` chrome with a search box in the title bar, a left `NavigationView` (compact / expanded), and grouped `UserControl` pages under `Fluence.Wpf.Demo/Pages/`:

- Home (clickable hero cards)
- Icons (FontIcon and virtualized Segoe Fluent Icons catalog)
- Typography (Fluent type ramp and TextBlock usage)
- Accessibility (focus order, high contrast, automation, RTL)
- Buttons
- Selection (CheckBox, RadioButton, ToggleSwitch, RatingControl)
- Inputs (TextBox, PasswordBox, NumberBox, ComboBox, Slider)
- Forms (sign-in, checkout, settings)
- Data (Card, ListBox, ListView)
- Data Binding (ObservableCollection, selection modes, data templates)
- Trees (TreeView)
- Menus (Menu, ContextMenu, ToolTip, command buttons)
- Navigation (NavigationView modes)
- Tabs (TabControl, TabView)
- Layout (StackPanel, DockPanel, Border, Separator, Expander)
- Status (InfoBar, InfoBadge, ProgressBar, ProgressRing)
- Settings (theme, navigation style, colors, backdrop, caption buttons)

Most non-Home gallery pages render discrete examples through `DemoSampleControl`. Source tabs are backed by page-local `XamlSource` and optional `CSharpSource` strings so examples can be debugged directly with their page code-behind. Fixed XAML samples keep named live content in page-owned hidden slots and transfer it with the demo-only `DemoSamplePageWiring` helper. Direct reference pages such as Typography mirror WinUI Gallery catalog surfaces and do not add a trailing source expander.

**Fluence.Wpf.Demo.Mvvm** is a minimal Task Manager demonstrating `FluenceWindow` + Fluence controls with zero code-behind (CommunityToolkit.Mvvm). See [AGENTS.md](../AGENTS.md) for architecture notes.

## Namespaces

- `Fluence.Wpf` - theme, accent, title-bar and window-control helpers, and UI enums (`ApplicationTheme`, `BackdropType`, `CardVariant`, `NavigationViewPaneDisplayMode`, `TreeViewSelectionMode`, typography enums).
- `Fluence.Wpf.Controls` - styled controls, primitives, and `FluenceWindow`.

Example XML namespace declarations:

```xml
xmlns:fluence="http://schemas.fluencewpf.com"
<!-- or, fully qualified: -->
xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"
xmlns:uicore="clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf"
```

`http://schemas.fluencewpf.com` covers both the controls and the core namespaces in a single prefix.

## Catalog (summary)

| Area                | Types                                                                                                                              |
|---------------------|------------------------------------------------------------------------------------------------------------------------------------|
| Window              | `FluenceWindow`, `TitleBar`                                                                                                         |
| Basic actions       | `Button`, `HyperlinkButton`, `DropDownButton`, `SplitButton`, `RepeatButton`, `ToggleButton`                                       |
| Selection           | `CheckBox`, `RadioButton`, `ToggleSwitch`, `ComboBox`, `Slider`, `NumberBox`                                                       |
| Text                | `TextBox`, `PasswordBox`, `TextBlock` + `TextBlockExtensions`                                                                      |
| Data                | `ListView`, `ListBox`, `ListBoxItem`, `ListViewItem`                                                                               |
| Tabs                | `TabControl`, `TabItem`, `TabView`, `TabViewItem`                                                                                  |
| Feedback            | `ProgressBar`, `ProgressRing`, `InfoBar`, `InfoBadge`, `RatingControl`                                                             |
| Navigation          | `NavigationView`, `NavigationViewItem`, `NavigationViewItemHeader`, `NavigationViewItemSeparator`                                  |
| Menus & popups      | `ContextMenu`, `MenuItem`, `Menu`, `ToolTip`                                                                                       |
| Trees & collections | `TreeView`, `TreeViewItem`                                                                                                         |
| Layout / surfaces   | `Card`, `Expander`, `Border`, `StackPanel`, `DockPanel`, `SmoothScrollViewer`, `Separator`                                         |
| Person / social     | `PersonPicture`                                                                                                                    |
| Icons               | `FontIcon`                                                                                                                         |

Tab strip and scroll bar styling are provided via merged themes (see `Themes/Generic.xaml`).

## Control Screenshots and API

Each area below pairs Light and Dark gallery screenshots with the public API types used in that section. Captures live under `docs/screenshots/gallery/`, and API links point to `/api/` on the documentation site.

### Window and Shell

<div class="fluence-control-shot-pair" aria-label="Window and shell screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-home-light.png" alt="Gallery shell home page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-home-dark.png" alt="Gallery shell home page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.FluenceWindow.html">FluenceWindow</a>
  <a href="../../api/Fluence.Wpf.Controls.TitleBar.html">TitleBar</a>
  <a href="../../api/Fluence.Wpf.BackdropType.html">BackdropType</a>
  <a href="../../api/Fluence.Wpf.CornerPreference.html">CornerPreference</a>
</div>

Primary members include `SystemBackdropType`, `CornerStyle`, `ExtendsContentIntoTitleBar`, `TitleBar`, `TitleBarHeight`, caption-button visibility properties, and title-bar events for back and pane-toggle requests.

### Basic Actions

<div class="fluence-control-shot-pair" aria-label="Basic action control screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-buttons-light.png" alt="Buttons gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-buttons-dark.png" alt="Buttons gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.Button.html">Button</a>
  <a href="../../api/Fluence.Wpf.Controls.HyperlinkButton.html">HyperlinkButton</a>
  <a href="../../api/Fluence.Wpf.Controls.DropDownButton.html">DropDownButton</a>
  <a href="../../api/Fluence.Wpf.Controls.SplitButton.html">SplitButton</a>
  <a href="../../api/Fluence.Wpf.Controls.RepeatButton.html">RepeatButton</a>
  <a href="../../api/Fluence.Wpf.Controls.ToggleButton.html">ToggleButton</a>
  <a href="../../api/Fluence.Wpf.ControlAppearance.html">ControlAppearance</a>
</div>

The action controls expose standard WPF command, content, and click patterns with Fluent appearance roles. Use `Appearance="Accent"` for the page primary action and standard or subtle styling for lower-emphasis commands.

### Selection

<div class="fluence-control-shot-pair" aria-label="Selection control screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-selection-light.png" alt="Selection gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-selection-dark.png" alt="Selection gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.CheckBox.html">CheckBox</a>
  <a href="../../api/Fluence.Wpf.Controls.RadioButton.html">RadioButton</a>
  <a href="../../api/Fluence.Wpf.Controls.ToggleSwitch.html">ToggleSwitch</a>
  <a href="../../api/Fluence.Wpf.Controls.RatingControl.html">RatingControl</a>
</div>

Selection controls follow WPF checked-state APIs (`IsChecked`, groups, and selection) with Fluent focus, pressed, disabled, and checked-state visuals. `RatingControl` adds value-based selection for lightweight scoring UI.

### Inputs

<div class="fluence-control-shot-pair" aria-label="Input control screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-inputs-light.png" alt="Inputs gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-inputs-dark.png" alt="Inputs gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.ComboBox.html">ComboBox</a>
  <a href="../../api/Fluence.Wpf.Controls.Slider.html">Slider</a>
  <a href="../../api/Fluence.Wpf.Controls.NumberBox.html">NumberBox</a>
  <a href="../../api/Fluence.Wpf.Controls.TextBox.html">TextBox</a>
  <a href="../../api/Fluence.Wpf.Controls.PasswordBox.html">PasswordBox</a>
  <a href="../../api/Fluence.Wpf.SpinButtonPlacementMode.html">SpinButtonPlacementMode</a>
  <a href="../../api/Fluence.Wpf.NumberBoxValueChangedEventArgs.html">NumberBoxValueChangedEventArgs</a>
</div>

Input controls keep standard WPF editing, selection, command, and binding behavior. `NumberBox` adds numeric parsing, range, increment, and spin-button placement support; text inputs expose Fluent placeholder, validation, and focus visuals through the shared templates.

### Forms

<div class="fluence-control-shot-pair" aria-label="Form screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-forms-light.png" alt="Forms gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-forms-dark.png" alt="Forms gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.TextBox.html">TextBox</a>
  <a href="../../api/Fluence.Wpf.Controls.PasswordBox.html">PasswordBox</a>
  <a href="../../api/Fluence.Wpf.Controls.ComboBox.html">ComboBox</a>
  <a href="../../api/Fluence.Wpf.Controls.Button.html">Button</a>
  <a href="../../api/Fluence.Wpf.ValidationState.html">ValidationState</a>
</div>

The form page composes the input controls with card surfaces, status text, validation states, and primary actions. Use it as the reference for sign-in, checkout, and settings forms.

### Data and Collections

<div class="fluence-control-shot-pair" aria-label="Data control screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-data-light.png" alt="Data gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-data-dark.png" alt="Data gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.Card.html">Card</a>
  <a href="../../api/Fluence.Wpf.Controls.ListBox.html">ListBox</a>
  <a href="../../api/Fluence.Wpf.Controls.ListBoxItem.html">ListBoxItem</a>
  <a href="../../api/Fluence.Wpf.Controls.ListView.html">ListView</a>
  <a href="../../api/Fluence.Wpf.Controls.PersonPicture.html">PersonPicture</a>
  <a href="../../api/Fluence.Wpf.CardVariant.html">CardVariant</a>
  <a href="../../api/Fluence.Wpf.ListViewState.html">ListViewState</a>
</div>

Collection controls keep WPF item-source and template behavior while applying Fluent selection, hover, empty-state, and card surface treatment. `Card` adds header, footer, icon, clickable, and pressed-state APIs.

### Data Binding

<div class="fluence-control-shot-pair" aria-label="Data binding screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-data-binding-light.png" alt="Data binding gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-data-binding-dark.png" alt="Data binding gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.ListView.html">ListView</a>
  <a href="../../api/Fluence.Wpf.Controls.ListBox.html">ListBox</a>
  <a href="../../api/Fluence.Wpf.Controls.TreeView.html">TreeView</a>
  <a href="../../api/Fluence.Wpf.Controls.Card.html">Card</a>
  <a href="../../api/Fluence.Wpf.ListViewState.html">ListViewState</a>
</div>

The data-binding page documents normal WPF `ItemsSource`, `SelectedItem`, `SelectedItems`, item-template, and command-binding patterns with Fluence controls. The control API stays WPF-native so view models do not need Fluence-specific base classes.

### Icons

<div class="fluence-control-shot-pair" aria-label="Icon screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-icons-light.png" alt="Icons gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-icons-dark.png" alt="Icons gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.FontIcon.html">FontIcon</a>
</div>

`FontIcon` wraps Segoe Fluent Symbols glyph rendering with icon-size, foreground, and alignment behavior that works inside buttons, navigation items, tab headers, cards, and standalone icon lists.

### Typography

<div class="fluence-control-shot-pair" aria-label="Typography screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-typography-light.png" alt="Typography gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-typography-dark.png" alt="Typography gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.TextBlock.html">TextBlock</a>
  <a href="../../api/Fluence.Wpf.Controls.TextBlockExtensions.html">TextBlockExtensions</a>
  <a href="../../api/Fluence.Wpf.FluentTypography.html">FluentTypography</a>
</div>

`TextBlockExtensions.Typography` maps text to the shared Fluent type ramp so app text and control templates stay on the same typography tokens.

### Navigation

<div class="fluence-control-shot-pair" aria-label="Navigation control screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-navigation-light.png" alt="Navigation gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-navigation-dark.png" alt="Navigation gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.NavigationView.html">NavigationView</a>
  <a href="../../api/Fluence.Wpf.Controls.NavigationViewItem.html">NavigationViewItem</a>
  <a href="../../api/Fluence.Wpf.Controls.NavigationViewItemHeader.html">NavigationViewItemHeader</a>
  <a href="../../api/Fluence.Wpf.Controls.NavigationViewItemSeparator.html">NavigationViewItemSeparator</a>
  <a href="../../api/Fluence.Wpf.NavigationViewPaneDisplayMode.html">NavigationViewPaneDisplayMode</a>
  <a href="../../api/Fluence.Wpf.NavigationViewBackRequestedEventArgs.html">NavigationViewBackRequestedEventArgs</a>
  <a href="../../api/Fluence.Wpf.NavigationViewItemInvokedEventArgs.html">NavigationViewItemInvokedEventArgs</a>
</div>

`NavigationView` owns pane layout, selection, back-button state, top overflow, and item invocation events. Application route history remains app-owned.

### Tabs

<div class="fluence-control-shot-pair" aria-label="Tab control screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-tabs-light.png" alt="Tabs gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-tabs-dark.png" alt="Tabs gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.TabView.html">TabView</a>
  <a href="../../api/Fluence.Wpf.Controls.TabViewItem.html">TabViewItem</a>
  <a href="../../api/Fluence.Wpf.TabViewWidthMode.html">TabViewWidthMode</a>
  <a href="../../api/Fluence.Wpf.TabViewCloseButtonOverlayMode.html">TabViewCloseButtonOverlayMode</a>
  <a href="../../api/Fluence.Wpf.TabViewTabCloseRequestedEventArgs.html">TabViewTabCloseRequestedEventArgs</a>
</div>

`TabView` adds add-tab, close-request, width-mode, and close-button overlay APIs. Standard `TabControl` and `TabItem` pick up Fluent styling through the merged control templates.

### Layout and Surfaces

<div class="fluence-control-shot-pair" aria-label="Layout control screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-layout-light.png" alt="Layout gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-layout-dark.png" alt="Layout gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.Border.html">Border</a>
  <a href="../../api/Fluence.Wpf.Controls.StackPanel.html">StackPanel</a>
  <a href="../../api/Fluence.Wpf.Controls.DockPanel.html">DockPanel</a>
  <a href="../../api/Fluence.Wpf.Controls.Expander.html">Expander</a>
  <a href="../../api/Fluence.Wpf.Controls.Separator.html">Separator</a>
  <a href="../../api/Fluence.Wpf.Controls.SmoothScrollViewer.html">SmoothScrollViewer</a>
  <a href="../../api/Fluence.Wpf.BorderVariant.html">BorderVariant</a>
</div>

Layout primitives preserve WPF layout behavior while applying Fluent radii, strokes, dividers, and scroll chrome. `SmoothScrollViewer` is the preferred scroll host in the demo gallery.

### Menus and Popups

<div class="fluence-control-shot-pair" aria-label="Menu and popup screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-menus-light.png" alt="Menus gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-menus-dark.png" alt="Menus gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.Menu.html">Menu</a>
  <a href="../../api/Fluence.Wpf.Controls.MenuItem.html">MenuItem</a>
  <a href="../../api/Fluence.Wpf.Controls.ContextMenu.html">ContextMenu</a>
  <a href="../../api/Fluence.Wpf.Controls.ToolTip.html">ToolTip</a>
</div>

Menu and popup controls map WPF menu semantics to WinUI-style flyout visuals, including command text, separators, nested menu items, context menus, and tooltips.

### Trees

<div class="fluence-control-shot-pair" aria-label="Tree control screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-trees-light.png" alt="Trees gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-trees-dark.png" alt="Trees gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.TreeView.html">TreeView</a>
  <a href="../../api/Fluence.Wpf.Controls.TreeViewItem.html">TreeViewItem</a>
  <a href="../../api/Fluence.Wpf.TreeViewSelectionMode.html">TreeViewSelectionMode</a>
</div>

`TreeView` supports single and multiple selection modes, a live `SelectedItems` list, expandable hierarchy, and tri-state item selection through `TreeViewItem.IsSelectionChecked`.

### Status and Feedback

<div class="fluence-control-shot-pair" aria-label="Status and feedback screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-status-light.png" alt="Status gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-status-dark.png" alt="Status gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Controls.InfoBar.html">InfoBar</a>
  <a href="../../api/Fluence.Wpf.Controls.InfoBadge.html">InfoBadge</a>
  <a href="../../api/Fluence.Wpf.Controls.ProgressBar.html">ProgressBar</a>
  <a href="../../api/Fluence.Wpf.Controls.ProgressRing.html">ProgressRing</a>
  <a href="../../api/Fluence.Wpf.Controls.RatingControl.html">RatingControl</a>
  <a href="../../api/Fluence.Wpf.InfoBarSeverity.html">InfoBarSeverity</a>
  <a href="../../api/Fluence.Wpf.InfoBadgeStyle.html">InfoBadgeStyle</a>
  <a href="../../api/Fluence.Wpf.ProgressBarMode.html">ProgressBarMode</a>
  <a href="../../api/Fluence.Wpf.ProgressRingState.html">ProgressRingState</a>
</div>

Status controls expose severity, closable state, determinate and indeterminate progress, paused and error states, and badge styling for count or attention indicators.

### Accessibility

<div class="fluence-control-shot-pair" aria-label="Accessibility screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-accessibility-light.png" alt="Accessibility gallery page in light theme.">
    <figcaption>Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-accessibility-dark.png" alt="Accessibility gallery page in dark theme.">
    <figcaption>Dark</figcaption>
  </figure>
</div>

Key API:

<div class="fluence-api-list">
  <a href="../../api/Fluence.Wpf.Automation.html">Automation namespace</a>
  <a href="../../api/Fluence.Wpf.Automation.NavigationViewAutomationPeer.html">NavigationViewAutomationPeer</a>
  <a href="../../api/Fluence.Wpf.Automation.ToggleSwitchAutomationPeer.html">ToggleSwitchAutomationPeer</a>
  <a href="../../api/Fluence.Wpf.Automation.InfoBarAutomationPeer.html">InfoBarAutomationPeer</a>
</div>

Accessibility coverage includes focus visuals, high contrast resources, automation peers, keyboard navigation, and right-to-left layout checks.

## FluenceWindow

`FluenceWindow` provides Fluent title-bar styling, caption buttons, backdrop support, and a title-bar content slot. `MinWidth` is caller-controlled and remains unset by default; the default title bar height is 68 px. When `ExtendsContentIntoTitleBar="True"`, app content can render behind the title bar; NavigationView left panes reserve title-bar height before their first item when no explicit header is provided.

`CaptionButtonChrome` and `WindowPolicy` are internal implementation details behind `FluenceWindow` caption button and DWM policy decisions. They are covered by tests, but they are not normal consumer controls.

`TitleBar` is the reusable shell title-bar control used by the gallery. It provides back and pane-toggle buttons (`BackRequested`, `PaneToggleRequested`, and matching command properties), icon/title/subtitle presentation, and left/right/content slots. Interactive template buttons opt into `WindowChrome.IsHitTestVisibleInChrome`; app-specific content such as search boxes should do the same.

## NavigationView

Three pane display modes are supported out of the box:

| `PaneDisplayMode` | Rail                                            | Labels                         | Template                                |
|-------------------|-------------------------------------------------|--------------------------------|-----------------------------------------|
| `Left` (default)  | 48 / 280 px                                     | Shown when `IsPaneOpen="True"` | `NavigationViewLeftPaneTemplate`        |
| `LeftCompact`     | 48 px (overlay 280 px when `IsPaneOpen="True"`) | Overlay only                   | `NavigationViewLeftCompactPaneTemplate` |
| `Top`             | 48 px horizontal strip                          | Always shown                   | `NavigationViewTopPaneTemplate`         |

Left and LeftCompact share the same visual contract:

- Pane toggle (`PART_PaneToggleButton`, glyph `E700`) and back button (`PART_BackButton`, glyph `E72B`) appear in WinUI order at the top of a 48 px rail, each 48×40 px.
- When a closed compact-left pane shows both an enabled back button and pane toggle, the pane reserves two 48 px chrome slots so the pane toggle remains visible to the right of back.
- Selection indicator (`PART_SelectionIndicator`) is a single `Border` that animates between items - 3 × 16 px vertical in `Left` / `LeftCompact`, 16 × 3 px horizontal in `Top`.
- Content region is a `Border` with `CornerRadius="8,0,0,0"`, `BorderThickness="1,1,0,0"`, and `BorderBrush="{DynamicResource CardStrokeColorDefaultBrush}"`, wrapping `PART_ContentPresenter`.
- Back button visibility and enabled state are driven by `IsBackButtonVisible` / `IsBackEnabled`. The back button is visible only when both are `true`; a disabled back route collapses the button and does not reserve a glyph slot. Consumers route the `BackRequested` event to their own history stack.
- Pane toggle visibility is controlled by `IsPaneToggleButtonVisible`. It defaults to `true` for left pane modes and is not shown in top mode.
- When hosted inside a `FluenceWindow`, `Left` mode sets `ExtendsContentIntoTitleBar=True`; `Top` mode sets it `False`.
- `Top` mode coerces `IsPaneOpen=True` and `IsPaneToggleButtonVisible=False`, keeps item icons and text visible, lays items out horizontally without scrolling, and moves overflowed items to the `PART_TopOverflowButton` menu.
- Item invocation raises `ItemInvoked` before WPF `SelectionChanged`, matching WinUI ordering. Navigation content belongs to the app layer: set `NavigationView.Content` or route through your own frame/service when handling `ItemInvoked`.

## Cards

`Card` is a `ContentControl` with optional `Header`, `Footer`, and `Icon` slots. Opt into click semantics with `IsClickable="True"`:

```xml
<fluence:Card Padding="16"
              IsClickable="True"
              Click="OnCardClicked"
              Variant="{x:Static uicore:CardVariant.Default}">
    <fluence:Button Content="Accent" Appearance="Accent" />
</fluence:Card>
```

When `IsClickable` is true:

- The read-only `IsPressed` dependency property mirrors the left-button press state.
- A left-button press inside the card followed by a matching release raises the `Click` routed event (`RoutingStrategy.Bubble`).
- `OnMouseLeave` and `OnLostMouseCapture` cancel the pending press without raising `Click`, matching WinUI button semantics.

## Typography

`TextBlockExtensions` exposes attached properties for the WinUI type ramp:

```xml
<TextBlock fluence:TextBlockExtensions.Typography="TitleLarge"
           Text="Fluence.Wpf" />
```

Supported values: `Caption`, `Body`, `BodyStrong`, `BodyLarge`, `Subtitle`, `Title`, `TitleLarge`, `Display` (see `Fluence.Wpf/FluentTypography.cs`).

The attached property applies the corresponding named style from `Themes/Typography/Typography.xaml` (`BodyTextBlockStyle`, `TitleTextBlockStyle`, and so on). Keep type-ramp metrics in that dictionary so code, templates, and consumers share one source of truth.

## Tabs

`TabControl` / `TabItem` receive WinUI 3 styling automatically via `Themes/Generic.xaml` - animated selection indicator, typography, and strip padding match the rest of the library.

`TabView` / `TabViewItem` add multi-document features on top of the standard `TabControl` contract:

```xml
<ui:TabView AddTabButtonClick="OnAddTabButtonClick"
            CloseButtonOverlayMode="Auto"
            TabCloseRequested="OnTabCloseRequested">
    <ui:TabViewItem Header="Document 1" IsSelected="True">
        <ui:TabViewItem.Icon>
            <ui:FontIcon Glyph="" IconFontSize="16" />
        </ui:TabViewItem.Icon>
        <!-- tab body -->
    </ui:TabViewItem>

    <ui:TabViewItem Header="Welcome" IsClosable="False">
        <!-- pinned tab: close button hidden -->
    </ui:TabViewItem>
</ui:TabView>
```

Key members:

| Member                           | Type                                               | Notes                                                                  |
| -------------------------------- | -------------------------------------------------- | ---------------------------------------------------------------------- |
| `TabView.IsAddTabButtonVisible`  | `bool`                                             | Toggles the trailing `+` button (`PART_AddTabButton`). Default `true`. |
| `TabView.TabWidthMode`           | `TabViewWidthMode`                                 | `SizeToContent` (default), `Equal`, or `Compact`.                      |
| `TabView.CloseButtonOverlayMode` | `TabViewCloseButtonOverlayMode`                    | `Auto` (default), `OnPointerOver`, or `Always`.                        |
| `TabView.AddTabButtonClick`      | Routed event                                       | Raised when the trailing `+` button is invoked.                        |
| `TabView.TabCloseRequested`      | Routed event (`TabViewTabCloseRequestedEventArgs`) | Bubbled from the originating `TabViewItem.CloseRequested`.             |
| `TabViewItem.IsClosable`         | `bool`                                             | Default `true`. Set `false` to pin a tab and hide its close button.    |
| `TabViewItem.Icon`               | `object`                                           | Any visual (typically `FontIcon`); rendered to the left of `Header`.   |
| `TabViewItem.CloseRequested`     | Routed event (`RoutingStrategy.Bubble`)            | Raised by the per-tab close button (`PART_CloseButton`).               |

Consumers remove the tab from the source collection themselves - the control does not auto-remove items. See `Fluence.Wpf.Demo/Pages/GalleryTabsPage.xaml(.cs)` for a reference implementation.

## Feedback

`ProgressBar` supports determinate, indeterminate, step, paused, and error modes through `ProgressMode`. Paused and error modes use the system caution and critical brushes.

```xml
<ui:ProgressBar Value="62" ProgressMode="Paused" />
<ui:ProgressBar Value="78" ProgressMode="Error" />
```

`ProgressRing` supports the same normal/caution/critical visual language through `ProgressState`:

```xml
<ui:ProgressRing IsActive="True" IsIndeterminate="True" />
<ui:ProgressRing ProgressState="Paused"
                 IsActive="True"
                 IsIndeterminate="True" />
<ui:ProgressRing ProgressState="Error"
                 IsActive="True"
                 IsIndeterminate="False"
                 Value="70" />
```

`ProgressRingState` values are `Normal`, `Paused`, and `Error`. `Normal` uses the accent brush, `Paused` uses `SystemFillColorCautionBrush`, and `Error` uses `SystemFillColorCriticalBrush`.

## Menus & Popups

`ContextMenu`, `MenuItem`, and `Menu` use the WinUI 3 MenuFlyout visual vocabulary.

```xml
<!-- Attach a Fluent ContextMenu to any element -->
<ui:Button Content="Right-click me">
    <ui:Button.ContextMenu>
        <ui:ContextMenu>
            <ui:MenuItem Header="Cut"  InputGestureText="Ctrl+X" />
            <ui:MenuItem Header="Copy" InputGestureText="Ctrl+C" />
            <Separator />
            <ui:MenuItem Header="Paste" InputGestureText="Ctrl+V" />
        </ui:ContextMenu>
    </ui:Button.ContextMenu>
</ui:Button>

<!-- Top-level menu bar -->
<ui:Menu>
    <ui:MenuItem Header="_File">
        <ui:MenuItem Header="_New"  InputGestureText="Ctrl+N" />
        <ui:MenuItem Header="_Open" InputGestureText="Ctrl+O" />
        <Separator />
        <ui:MenuItem Header="E_xit" />
    </ui:MenuItem>
</ui:Menu>
```

`ToolTip` is applied automatically to any element with a `ToolTipService.ToolTip` property when the `ToolTip` style is merged:

```xml
<ui:Button Content="Save" ToolTipService.ToolTip="Save the document" />
```

## Trees

`TreeView` and `TreeViewItem` provide a hierarchical list matching the WinUI 3 `TreeView` visual contract.

```xml
<ui:TreeView SelectionMode="{x:Static uicore:TreeViewSelectionMode.Multiple}">
    <ui:TreeViewItem Header="Documents" IsExpanded="True">
        <ui:TreeViewItem Header="Reports">
            <ui:TreeViewItem Header="Q1.xlsx" />
            <ui:TreeViewItem Header="Q2.xlsx" />
        </ui:TreeViewItem>
        <ui:TreeViewItem Header="Presentations" />
    </ui:TreeViewItem>
    <ui:TreeViewItem Header="Pictures" />
</ui:TreeView>
```

Selection members:

| Member                         | Type                    | Notes                                                           |
| ------------------------------ | ----------------------- | --------------------------------------------------------------- |
| `TreeView.SelectionMode`       | `TreeViewSelectionMode` | `Single` by default; use `Multiple` for checkbox selection.     |
| `TreeView.SelectedItems`       | `IList`                 | Live selected item list.                                        |
| `TreeViewItem.IsSelectionChecked` | `bool?`               | Mirrors checked, unchecked, and indeterminate checkbox state.    |

Visual contract:
- Per-level indent via `LevelToIndentConverter` (16 px per level).
- Chevron (`U+E76C`) rotates 90° on expand - 100 ms `ControlFastOutSlowInKeySpline` easing.
- `SubtleFillColorSecondaryBrush` on hover, `SubtleFillColorTertiaryBrush` on press, `AccentFillColorDefaultBrush` when selected.
- VSM groups: `CommonStates`, `SelectionStates`, `ExpansionStates`.

## Screenshots

Reference captures live under `docs/screenshots/`:

- `gallery/gallery-*-light.png` and `gallery/gallery-*-dark.png` cover every gallery page, including Settings.
- `apps/mvvm-light.png` / `apps/mvvm-dark.png` cover the MVVM Task Manager demo.
- `apps/powershell-light.png` / `apps/powershell-dark.png` cover the PowerShell-hosted XAML demo.
- `banner-{light,dark,highcontrast}-{1x,1.5x}.png` remain available for compact banner references.

<div class="fluence-screenshot-row" aria-label="Representative demo screenshots">
  <figure>
    <img src="../../images/screenshots/gallery/gallery-home-light.png" alt="Gallery home page capture in light theme.">
    <figcaption>Gallery home, Light</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/gallery/gallery-buttons-dark.png" alt="Buttons gallery page capture in dark theme.">
    <figcaption>Buttons, Dark</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/apps/mvvm-dark.png" alt="MVVM task manager capture in dark theme.">
    <figcaption>MVVM, Dark</figcaption>
  </figure>
  <figure>
    <img src="../../images/screenshots/apps/powershell-light.png" alt="PowerShell demo host capture in light theme.">
    <figcaption>PowerShell, Light</figcaption>
  </figure>
</div>

The <a href="/Fluence.Wpf/screenshots/">full screenshot set</a> is published on the Hugo site. If you need to refresh captures locally, run the screenshot harness directly:

```powershell
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug --filter "FullyQualifiedName~GalleryScreenshotHarness"
```

The harness uses `RenderTargetBitmap` and flattens transparent WPF layers over `SolidBackgroundFillColorBaseBrush`. DWM Mica / Acrylic are still not captured because they are composed outside WPF, so the screenshots document the WPF control and shell surfaces. `FluenceWindow` caption styling is verified by `FluenceWindowTitleBarTests`.

Marketing images live under `docs/images/` (for example `docs/images/Banner.png`). Capture control screenshots at 100 % and 150 % scaling and document the reference OS build, theme, and accent when adding them.

## Tests

MSTest exercises templates, theme stability, and control behavior on .NET Framework 4.7.2 and .NET 10 for Windows. Adding a new public control should include at least:

- A default-style / template smoke test (the control applies the expected template).
- A theme-cycle pass if the control uses `DynamicResource` heavily (`ThemeTestHelpers.ApplyStandardThemeCycle`).
- Interaction or state assertions where the control exposes behavior (see `ControlTests.NavigationView.cs` and `ControlTests.FluentStroke.cs` for representative patterns).
