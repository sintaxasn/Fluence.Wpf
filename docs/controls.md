# Controls

The **Fluence.Wpf.Demo** gallery is the live inventory: `FluenceWindow` chrome with a search box in the title bar, a left `NavigationView` (compact / expanded), and grouped `UserControl` pages under `Fluence.Wpf.Demo/Pages/`:

- Home (clickable hero cards)
- Colors (accent ramp, theme brush swatches)
- Iconography (FontIcon and virtualized Segoe Fluent Icons catalog)
- Typography (Fluent type ramp and TextBlock usage)
- Accessibility (focus order, high contrast, automation, RTL)
- Buttons
- Selection (CheckBox, RadioButton, ToggleSwitch, Slider)
- Inputs (TextBox, PasswordBox, NumberBox, ComboBox)
- Forms (sign-in, checkout, settings)
- Data (Card, ListBox, ListView)
- Data Binding (ObservableCollection, selection modes, data templates)
- Trees (TreeView)
- Menus (Menu, ContextMenu, ToolTip, command buttons)
- Navigation (NavigationView modes)
- Tabs (TabControl, TabView)
- Layout (StackPanel, DockPanel, Border, Separator, Expander)
- Status (InfoBar, InfoBadge, ProgressBar, ProgressRing)
- Windowing (backdrop, caption, theme)

Each non-Home gallery page renders examples inline and exposes source through `DemoSampleControl`. Source tabs are backed by page-local `XamlSource` and optional `CSharpSource` strings so examples can be debugged directly with their page code-behind.

**Fluence.Wpf.Demo.Mvvm** is a minimal Task Manager demonstrating `FluenceWindow` + Fluence controls with zero code-behind (CommunityToolkit.Mvvm). See [CLAUDE.md §8](../CLAUDE.md) for architecture notes.

## Namespaces

- `Fluence.Wpf` - theme, accent, window chrome helpers, and UI enums (`ApplicationTheme`, `BackdropType`, `CardVariant`, `NavigationViewPaneDisplayMode`, typography enums).
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
| Window              | `FluenceWindow`, `TitleBar`, `CaptionButtonChrome`, `WindowPolicy`                                                                 |
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

## FluenceWindow

`FluenceWindow` provides the Fluent window chrome, caption buttons, backdrop, and title-bar content slot. `MinWidth` is caller-controlled and remains unset by default; the default title bar height is 68 px. When `ExtendsContentIntoTitleBar="True"`, app content can render behind the title bar; NavigationView left panes reserve title-bar height before their first item when no explicit header is provided.

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

Supported values: `Caption`, `Body`, `BodyStrong`, `BodyLarge`, `Subtitle`, `Title`, `TitleLarge`, `Display` (see `Fluence.Wpf/Enums/FluentTypography.cs`).

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
<ui:TreeView>
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

Visual contract:
- Per-level indent via `LevelToIndentConverter` (16 px per level).
- Chevron (`U+E76C`) rotates 90° on expand — 100 ms `ControlFastOutSlowInKeySpline` easing.
- `SubtleFillColorSecondaryBrush` on hover, `SubtleFillColorTertiaryBrush` on press, `AccentFillColorDefaultBrush` when selected.
- VSM groups: `CommonStates`, `SelectionStates`, `ExpansionStates`.

## Screenshots

Reference captures live under `docs/screenshots/`:

- `banner-light-1x.png` / `banner-light-1.5x.png`
- `banner-dark-1x.png` / `banner-dark-1.5x.png`
- `banner-highcontrast-1x.png` / `banner-highcontrast-1.5x.png`

They are regenerated by `Fluence.Wpf.Tests/GalleryScreenshotHarness.CaptureBannerAcrossThemesAndScales`. The test is opt-in: set `FLUENCE_CAPTURE_SCREENSHOTS=1` and run

```powershell
dotnet test Fluence.Wpf.Tests -f net472 --filter "FullyQualifiedName~GalleryScreenshotHarness"
```

Without the environment variable the test reports `Inconclusive` so routine CI / developer runs don't overwrite the committed images. The harness hosts `GalleryHomePage` inside a plain `Window` with a solid `SolidBackgroundFillColorBaseBrush` backdrop (DWM Mica / Acrylic cannot be captured by `RenderTargetBitmap`), so the screenshots document the control surface - not the window chrome itself. `FluenceWindow` caption styling is verified by `FluenceWindowTitleBarTests` instead.

Marketing images live under `docs/images/` (for example `docs/images/Banner.png`). Capture control screenshots at 100 % and 150 % scaling and document the reference OS build, theme, and accent when adding them.

## Tests

MSTest exercises templates, theme stability, and control behavior across `net472` and `net10.0-windows`. Adding a new public control should include at least:

- A default-style / template smoke test (the control applies the expected template).
- A theme-cycle pass if the control uses `DynamicResource` heavily (`ThemeTestHelpers.ApplyStandardThemeCycle`).
- Interaction or state assertions where the control exposes behavior (see `ControlTests.NavigationView.cs` and `ControlTests.FluentStroke.cs` for representative patterns).
