# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **WinUI-style `NavigationView.ItemInvoked`** - item mouse, keyboard, and automation invocation now raises `ItemInvoked` before `SelectionChanged`, with `NavigationViewItemInvokedEventArgs` exposing the invoked data item, container, and settings flag.
- **Shell `TitleBar` control surface** - `TitleBar` now derives from `ContentControl` and provides back/pane-toggle buttons, icon/title/subtitle, left/right header slots, command properties, and `BackRequested` / `PaneToggleRequested` events for WPF shell integration.
- **`ProgressRingState` enum and `ProgressRing.ProgressState`** - `Normal`, `Paused`, and `Error` states color determinate and indeterminate ring arcs with accent, `SystemFillColorCautionBrush`, and `SystemFillColorCriticalBrush` respectively.
- **Folder-level README documentation** - added short component READMEs for the library, gallery demo, MVVM demo, PowerShell demo, tests, and reserved gallery folder so each `Fluence.*` directory explains its purpose, run/build entry points, and maintenance notes.
- **`Fluence.Wpf.Demo.PowerShell` folder** - Windows PowerShell 5.1 sample that copies the current `net472` `Fluence.Wpf.dll`, loads `MainWindow.xaml` via `XamlReader`, applies `ApplicationThemeManager` resources, enables `SystemThemeWatcher`, and shows a non-modal `FluenceWindow` with demo controls while logging lifecycle and UI events to the console.
- **`Fluence.Wpf.Demo.Mvvm` project** - minimal Task Manager application demonstrating `FluenceWindow` with CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand(CanExecute)]`, `ObservableCollection` filter-rebuild pattern). Targets `net10.0-windows`. Covers: filter `RadioButton` ↔ `FilterMode` enum via `EnumToBoolConverter`, `ProgressBar` + status footer, delete button via `RelativeSource AncestorType=Window` in `DataTemplate`, `SmoothScrollViewer` wrapping `ItemsControl`. Key MVVM correctness notes: `[NotifyPropertyChangedFor]` removed from `_activeFilter` to prevent stale derived-property reads; `App.xaml` has no manual `Generic.xaml` merge (avoids 6th-slot corruption of `ApplicationThemeManager`). Zero code-behind.
- **`ContextMenu` + `MenuItem` controls** (`Fluence.Wpf.Controls`) - Fluent-styled overlay menu surface. `ContextMenu`: `OverlayCornerRadius` (8 px) border, `SolidBackgroundFillColorTertiaryBrush` background, `FlyoutShadowEffect`, 2 px inner padding. `MenuItem`: 3-column layout (icon 20 px / header / shortcut), `SubtleFillColorSecondaryBrush` hover, `SubtleFillColorTertiaryBrush` pressed, checkmark glyph (`U+E73E`, `AccentFillColorDefaultBrush`), submenu-arrow glyph (`U+E76C`). Includes implicit `Separator` style (1 px `DividerStrokeColorDefaultBrush`, `Margin="4,2"`). Includes submenu slide animation (`TranslateTransform X=-8→0`, 167 ms `ControlFastOutSlowInKeySpline`). Authority: WinUI 3 `MenuFlyout_themeresources.xaml`, .NET 10 WPF `MenuItem.xaml`.
- **`Menu` control** (`Fluence.Wpf.Controls`) - top-level menu bar subclass. Transparent background, no border, re-uses `MenuItem` style from `ContextMenu.xaml`. Authority: WinUI 3 `MenuBar_themeresources.xaml`.
- **`ToolTip` control** (`Fluence.Wpf.Controls`) - Fluent overlay tooltip. `SolidBackgroundFillColorTertiaryBrush` background, `SurfaceStrokeColorFlyoutBrush` 1 px border, `OverlayCornerRadius` (8 px), `FlyoutShadowEffect`, `MaxWidth=320`, 9/6/9/8 px padding, 12 pt `FluentFontFamily`. Authority: WinUI 3 `ToolTip_themeresources.xaml`.
- **`TreeView` + `TreeViewItem` controls** (`Fluence.Wpf.Controls`) - hierarchical list surface. `TreeViewItem` template: per-level `Margin` indent via `LevelToIndentConverter`, chevron (`U+E76C`) rotates 90° on expand (100 ms `ControlFastOutSlowInKeySpline`), `SubtleFillColorSecondaryBrush` hover / `SubtleFillColorTertiaryBrush` pressed / `AccentFillColorDefaultBrush` selected. VSM groups: `CommonStates`, `SelectionStates`, `ExpansionStates`. Authority: WinUI 3 `TreeViewItem.xaml` + `TreeView_themeresources.xaml`.
- **`RepeatButton` + `ToggleButton` controls** (`Fluence.Wpf.Controls`) - Fluent-styled primitive buttons sharing the Button visual token set. `ToggleButton` gains `CheckedStates` VSM group (`Checked` / `Unchecked`) with `AccentFillColorDefaultBrush` checked background. Authority: WinUI 3 `RepeatButton_themeresources.xaml`, `ToggleButton_themeresources.xaml`.
- **`ControlCornerRadius` + `OverlayCornerRadius` tokens** in `Themes/Brushes/Brushes.xaml` - `CornerRadius(4)` and `CornerRadius(8)` respectively. All control templates bind corner radii via `{DynamicResource ControlCornerRadius}` / `{DynamicResource OverlayCornerRadius}` rather than hard-coded values. `FlyoutShadowEffect` (BlurRadius=18, ShadowDepth=4, Opacity=0.22) and `DefaultControlFocusVisualStyle` + `DefaultCollectionFocusVisualStyle` shared styles also added here.
- **`RatingControl` control** (`Fluence.Wpf.Controls`) - WinUI 3-style star rating surface. Public surface: `Value` (double, 0–`MaxRating`, coerced, `BindsTwoWayByDefault`), `MaxRating` (int, default 5), `IsReadOnly`, `Caption`. Stars are Segoe Fluent Icons U+E734 (empty) / U+E735 (filled); filled stars use `AccentFillColorDefaultBrush`, empty use `TextFillColorSecondaryBrush`, disabled use `TextFillColorDisabledBrush`. Hover preview fills stars up to the hovered index. Clicking the currently-set star clears the rating (WinUI 3 `IsClearEnabled` behaviour). `PART_Caption` collapses when `Caption` is empty. Authority: WinUI 3 `RatingControl.xaml` / `RatingControl_themeresources.xaml`.
- **`PersonPicture` control** (`Fluence.Wpf.Controls`) - WinUI 3-canonical circular avatar. Default 40×40 (`ControlAltFillColorQuarternaryBrush` background, `CardStrokeColorDefaultBrush` 1 px border). Public surface: `DisplayName` (derives initials from first + last word), `Initials` (explicit override), `ProfilePicture` (`ImageSource`), `IsGroup`, `BadgeNumber`, `BadgeGlyph`. VSM groups: `CommonStates` (Photo / Initials / NoPhotoOrInitials / Group) and `BadgeStates` (NoBadge / BadgeWithoutImageSource). Placeholder glyphs: contact U+E77B (no data) and people U+E716 (`IsGroup=true`). Badge: 16×16 `AccentFillColorDefaultBrush` ellipse at bottom-right corner. Authority: WinUI 3 `PersonPicture.xaml` / `PersonPicture_themeresources.xaml`.
- **`Separator` control** (`Fluence.Wpf.Controls`) - thin standalone 1 px horizontal divider for content areas. Background `DividerStrokeColorDefaultBrush`, `Height=1`, `Margin=0`, non-interactive. Distinct from the implicit `System.Windows.Controls.Separator` style in `ContextMenu.xaml` (which has `Margin="4,2"` for menu use). Authority: WinUI 3 `MenuFlyout_themeresources.xaml` separator token.
- **`DefaultCollectionFocusVisualStyle`** shared resource in `Themes/Brushes/Brushes.xaml` - inset single-stroke `Rectangle` (no margin, `RadiusX/Y=4`, `FocusStrokeColorOuterBrush`, 2 px) for collection item containers. Applied to `ListBoxItem`, `ListViewItem`, and `TreeViewItem` `FocusVisualStyle` setters. Distinct from `DefaultControlFocusVisualStyle` (which has `Margin=-3` double-border ring for button-type controls).
- **MenuItem submenu slide animation** in `Themes/Controls/ContextMenu.xaml` - `IsSubmenuOpen=True` animates `SubMenuBorder` via `TranslateTransform X=-8→0` over 167 ms with `SplineDoubleKeyFrame KeySpline="0,0,0,1"`, matching WinUI 3 `ControlFastOutSlowInKeySpline`. Authority: .NET 10 WPF `MenuItem.xaml` `IsSubmenuOpen` `EnterActions` pattern.
- **17 new MSTests**: `ControlTests.RatingControl.cs` (8 tests - default style, five-star generation, filled count, brush colours, caption show/hide, value coercion, theme cycle) and `ControlTests.PersonPicture.cs` (9 tests - default style, template parts, placeholder glyph, initials derivation, explicit initials override, IsGroup glyph, badge visibility, badge collapsed, default 40×40 size, theme cycle).
- **`GalleryFormsPage`** - cohesive sign-up form demo: `TextBox` (first/last name, multi-line notes, read-only), email `TextBox` with regex validation label, `PasswordBox` with live strength meter (DependencyPropertyDescriptor wiring), country `ComboBox`, tier `RadioButton` group (Free/Pro/Enterprise), terms/marketing `CheckBox` pair, `NumberBox` (quantity, header, range validation), submit `Button` gated on all-valid state, `InfoBar` confirmation on submit, cancel to reset.
- **`GalleryMenusPage`** - menus and flyout demo: `ContextMenu` on a `Card` (icons, checkable item, nested submenu, disabled item), rich `ToolTip` demo (simple + StackPanel title/body variants), `DropDownButton` sort flyout, `SplitButton` export flyout, full `Menu` bar (File/Edit/View/Help with submenus, shortcuts, checkable items).
- **`GalleryTreesPage`** - `TreeView` hierarchy demo: file-system tree (src/docs/tests/demo structure, nested folders), keyboard navigation reference card (↑↓ / → / ← / Space+Enter / Home+End), "Expand all / Collapse all" buttons wired to programmatic `IsExpanded` traversal.
- **`GalleryDataBindingPage`** - data-binding patterns demo: `ListView` bound to `ObservableCollection<DemoItem>` with add/remove UI and live item-count label; `DataTemplate` (FontIcon + Name Body + AddedAt Caption); `SelectionMode` picker (Single/Multiple/Extended) with selection-count feedback; `DataTemplate` pattern explainer card.
- **`GalleryAccessibilityPage`** - accessibility demo: focus ring walkthrough (Tab through Button×2, CheckBox, ToggleSwitch, TextBox, ComboBox, Slider, HyperlinkButton all with `AutomationProperties.Name`), explicit `TabIndex` reverse-order demo, High Contrast brush mapping table (12 Fluence key → Windows system colour rows, live swatch), icon-only buttons with `AutomationProperties.Name` for Narrator, RTL `FlowDirection` toggle on an Arabic-text card.
- **`MainWindow` navigation** - 5 new `NavigationViewItem` entries wired to new gallery pages; `ControlTests.MainWindow_NavigationView_HasSixteenNavItems` replaces the prior eleven-item assertion.

### Fixed

- **Demo startup resource load order** - `Fluence.Wpf.Demo` now loads `DemoSharedStyles.xaml` after `ApplicationThemeManager.Apply`, preventing early implicit theme-dictionary lookup from crashing startup with missing control BAML resources.
- **NavigationView title-bar alignment** - extended-title-bar demo chrome now uses the shared `TitleBar` control with 48 px rail slots so back/collapse glyphs align with left-mode item icons, title identity shifts right as glyphs appear, and non-extended left pane chrome uses a horizontal row above the item list without the previous extra spacer.
- **FluenceWindow title-bar metrics** - `FluenceWindow.MinWidth` remains caller-controlled instead of setting a library default, and default `TitleBarHeight` remains 68 px; the demo shell uses a compact 42 px title bar, the `NavigationView` header reservation matches that 42 px chrome, and title text now returns whenever it has search-box clearance.
- **Fluent typography propagation** - text-bearing controls now explicitly use the shared `FluentFontFamily` token and propagate it through content presenters, placeholders, headers, menu text, password reveal text, and list/tree/tab item content while leaving rendering policy to the window root.
- **Accent button elevation border** - accent `Button` fill now paints under the WinUI-style elevation border instead of rendering as a nested fill with a detached bottom/right shadow edge.
- **Demo title-bar alignment and source samples** - the title-bar search box sits 2 px lower for visual centering, the extended-title icon renders at 20 px, inline source expanders replace the owning sample card so their top edge attaches directly to the card bottom, and TextBox helper / validation text uses a 2 px top gap.
- **NavigationView compact back chrome** - closed LeftCompact panes now reserve both 48 px chrome slots when the enabled back button and pane toggle are visible, keeping the collapse button onscreen to the right of back.
- **Test suite warnings and runtime** - removed remaining `MSTEST0032` analyzer warnings by converting compile-time native-constant assertions into a reflection-backed contract test, avoided repeated logical/visual subtree walks in demo-shell tests, and split CI test output by target framework.
- **Demo follow-up polish** - fixed the initial Home page load, removed the ComboBox focus/accent underline entirely while keeping dropdown item selection indicators, made native `TabControl` / `TabItem` pick up the Fluent style implicitly, made the left navigation 48 px top gap conditional on an empty header, paused the paused `ProgressRing`, tightened Status/Inputs sample spacing, right-aligned caption button slots when buttons collapse, and aligned the Accessibility keyboard samples into stable rows and columns.
- **NavigationView and ProgressRing parity** - aligned `NavigationView` chrome with the WinUI ordering (`Back`, then pane toggle), added `NavigationView.IsPaneToggleButtonVisible`, collapsed disabled back buttons instead of showing an inactive slot, moved demo title-bar navigation chrome into the extended title bar while keeping search visible, and replaced the indeterminate `ProgressRing` motion with a 4 second linear keyframe arc.
- **Demo and control polish pass** - fixed title-bar search layout stability, compact `NavigationView` footer visibility, pane-toggle glyph alignment, left-aligned DropDownButton/SplitButton flyout content, focused-only TextBox validation underlines, ComboBox initial focus underline behavior, TreeView parent hover bleed, bound `ListView.AnimateRemove`, ProgressBar/ProgressRing paused/error demos, InfoBar action clipping, and the gallery navigation order.
- **Default Windows blue accent ramp** - `#0078D4` now uses the Windows accent palette values (`Light2=#4CC2FF`, `Dark1=#0067C0`) instead of the generated fallback ramp, so Dark theme accent fills resolve to the correct lighter blue shade across controls and the demo accent swatch.
- **NavigationView pane item focus ring clipping** - `PART_PaneItemsScrollViewer` padding changed from `"0,4"` to `"3,4"` on both Left and LeftCompact pane templates. The `DefaultControlFocusVisualStyle` uses `Margin="-3"` to extend 3 px outside item bounds; without horizontal padding the `ScrollViewer` clip rect was chopping the focus ring on both sides.
- **Slider focus ring invisible** - `FocusVisualStyle="{x:Null}"` replaced with `{DynamicResource DefaultControlFocusVisualStyle}` so keyboard-focused sliders now show the canonical two-ring Fluent focus indicator.
- **Card focus ring absent + no keyboard activation** - `IsClickable=True` trigger now also sets `Focusable="True"` and `FocusVisualStyle="{DynamicResource DefaultControlFocusVisualStyle}"` (via no-TargetName setters on the control itself). `Card.OnKeyDown` / `OnKeyUp` overrides added so Space/Enter fire the `Click` routed event in `IsClickable` mode.
- **NavigationViewItem Space/Enter navigation** - `OnKeyDown` override added to `NavigationViewItem`; pressing Space or Enter now sets `NavigationView.SelectedItem` and marks the event handled, matching the mouse-click path (`OnPreviewMouseLeftButtonDown`).
- **NumberBox spin buttons not focusable in Compact mode** - `NumberBoxSpinButton` style changed from `Focusable="False"` to `Focusable="True"` so Tab reaches each spin button. `IsTabStop="False"` added to the outer `NumberBox` shell to eliminate the Shift+Tab redirect loop (shell was stealing focus from PART_TextBox, immediately redirecting back, preventing Shift+Tab from escaping to the previous outer control). Up/Down arrow keys inside PART_TextBox now call `TryParseText()` + `OnUpClick()` / `OnDownClick()`.
- **ProgressRing indeterminate visual updated to a Fluent caterpillar arc** - the default template replaces the five-orb `DotHost` storyboard with `PART_IndeterminateArc`, a rounded `Path` rendered through `ArcSegment`. `ProgressRing` now animates private start/sweep angle dependency properties to grow, rotate, and shrink the indeterminate arc while preserving the determinate arc tween, `StrokeThickness`, accent foreground binding, and legacy `EllipseDiameter` / `EllipseOffset` template settings for custom templates.
- **NavigationView Left mode initial collapsed layout** - `PaneDisplayMode="Left"` with `IsPaneOpen="False"` now starts at the 48 px compact rail instead of rendering a 280 px pane with collapsed item text.
- **Disabled Accent buttons in Dark theme** - `Button` now publishes and applies the WinUI dark disabled accent fill (`#28FFFFFF`) plus the disabled on-accent text token, fixing the MVVM demo Add button when `AddCommand` is disabled.
- **`SplitButton` control** (`Fluence.Wpf.Controls`) - WinUI 3-canonical two-half button: a left primary half that fires `Click` / `Command` and a right chevron half that opens a flyout popup. Public surface: `Content`, `Command`, `CommandParameter`, `CommandTarget` (`ICommandSource`), `Flyout`, `FlyoutTemplate`, `CornerRadius`, `DropdownCornerRadius`, read-only `IsFlyoutOpen`, and a bubbling `Click` routed event. Template parts: `PART_PrimaryButton` (`Button`), `PART_SecondaryButton` (`ToggleButton`), `PART_Popup`. Default style in `Themes/Controls/SplitButton.xaml` merges a single rounded outline bisected by a 1 px divider, with per-half hover / pressed tints. `SplitButtonAutomationPeer` exposes the control as `AutomationControlType.SplitButton` with both `Invoke` (primary half) and `ExpandCollapse` (flyout) patterns. New `GalleryButtonsPage` section demonstrates menu-style, free-form, and disabled flyouts.
- **7 new MSTests** in `SplitButtonTests.cs` covering default dependency-property values, `IsFlyoutOpen` read-only enforcement, template parts (`PART_PrimaryButton` / `PART_SecondaryButton` / `PART_Popup` with `StaysOpen=false`), primary-half `Click` routed-event + `Command` execution via UI Automation, secondary `ToggleButton.IsChecked=true` → `Popup.IsOpen=true` + `IsFlyoutOpen` flip, and automation peer patterns (`Invoke` + `ExpandCollapse`).

- **GalleryStatusPage ProgressRing demo condensed** - the duplicate "Indeterminate" and "Active (spinning)" columns (both `IsActive=True, IsIndeterminate=True`, visually identical after the indeterminate visual rewrite) are collapsed into a single `ProgressRing` (`x:Name="IndeterminateProgressRing"`) with a `ToggleSwitch` below it bound two-way to `IsActive` via `ElementName`. The `UniformGrid` column count drops from 4 to 3. The determinate arc and inactive columns are retained unchanged.

### Changed

- **FluenceWindow title-bar indentation contract removed** - `TitleBarLeftIndent` and the template spacer are gone; title identity offset for back/collapse glyphs is owned by the shell `TitleBar` / demo layout instead.
- **Window page accent swatches refreshed** - `GalleryColorsPage` retains the full theme brush catalogue; the Window page accent picker presents the seven bright logo rainbow swatches in a single row.
- **Demo gallery architecture simplified** - `Fluence.Wpf.Demo` now uses a flat `DemoNavigationCatalog`, direct `MainWindow` route-to-page navigation, concrete `Pages/Gallery*Page` controls, and page-local `XamlSource` / `CSharpSource` snippets. Removed generated category/control page shells, `Demo*Pages.cs` factories, copied `Samples/**` source files, and source-link actions. The iconography page now keeps the virtualized catalog outside the sample card, uses fixed row cards, and caches glyph rows.
- **Known-issue audit refresh** - validated current non-defect follow-ups with source/test evidence, added concrete next-step plans, and recorded the TreeView large-data virtualization tradeoff as a performance follow-up discovered during the WPF audit.
- **Code maintainability comments** - added targeted documentation around non-obvious WPF resource promotion, dispatcher/hook lifecycle, window hit-testing, demo navigation coordination, and template-part animation paths without changing runtime behavior.
- **Text rendering policy** - simplified typography to a type ramp only (font family, size, weight, line height, foreground). `FluenceWindow` now owns root `UseLayoutRounding`, `SnapsToDevicePixels`, and `RenderOptions.ClearTypeHint=Enabled`; control styles keep layout rounding but no longer set WPF `TextOptions` rendering policy.
- **Documentation and agent guidance refresh** - aligned public docs, `CLAUDE.md`, and `.github/copilot-instructions.md` with the five-slot theme lifecycle, the MVVM demo, current `SystemThemeWatcher.UnWatch` API spelling, and the canonical WinUI/.NET WPF reference policy.
- **Demo `MainWindow` NavigationView grouping** (Paradigm A) - the 11 gallery pages are now grouped under three WinUI 3 Gallery-style section headers (`NavigationViewItemHeader`): _Basic input_ (Buttons, Selection, Inputs), _Collections & navigation_ (Data, Tabs, Navigation), _Design & shell_ (Status, Colors, Glyphs, Window). "Home" stays above the groups. Existing search-driven `CollapseEmptySectionHeaders()` behavior hides headers when their section is fully filtered out - no new code path.
- **`GalleryHomePage` Featured controls tile grid** - a new "Featured controls" section below the category landing tiles displays a 3-column `UniformGrid` named `FeaturedControlsGrid` with six clickable `Card`s routing to Buttons, Selection, Inputs, Status, Collections, and Navigation. Uses `BodyStrong` + `Caption` typography so it reads as a distinct surface from the Subtitle + Body category tiles above.
- **Template-part contracts tightened across 10 controls** (WI-3 Batch A, uplift plan rows 37–46) - `ComboBox`, `DropDownButton`, `NumberBox`, `ProgressBar`, `ProgressRing`, `TextBox`, `Slider`, `SmoothScrollViewer`, `FontIcon`, and `TextBlock` now declare every `PART_*` they consume via `[TemplatePart]` attributes + `private const string PART_Whatever = "PART_Whatever"`, and use the constants in `OnApplyTemplate`/`GetTemplateChild` calls. No behaviour change; unblocks `[TemplateVisualState]` uplift work under row #1 RadioButton full-VSM port.

### Fixed

- **ComboBox popup open animation** (WI-3 B2, uplift row #29) - duration raised from 0.15 s to the canonical `ControlFastAnimationDuration` (0.167 s) and easing swapped from `CubicEaseOut` to a `SplineDoubleKeyFrame` with KeySpline `0.8,0,0,1`, matching WinUI 3 `ControlFastOutSlowInKeySpline` motion.
- **TabViewItem close-button glyph** (WI-3 B3, uplift row #30) - `StrokeThickness` changed from `1` to `1.5` to match the WinUI 3 canonical close-glyph visual weight.
- **DropDownButton + ComboBox chevrons** (WI-3 B4, uplift row #31) - replaced the inline `Path` (filled triangle on DropDownButton) and raw `TextBlock` glyph (ComboBox) with `controls:FontIcon Glyph="&#xE70D;" IconFontSize="12"` (Segoe Fluent Icons `ChevronDown`) for consistent foreground / opacity plumbing and canonical rendering.

### Added

- **`TabView` / `TabViewItem` controls** (`Fluence.Wpf.Controls`) - WinUI 3-styled multi-document surface built on top of `TabControl` / `TabItem`. Public surface: `TabViewItem.IsClosable`, `TabViewItem.Icon`, `TabViewItem.CloseRequested` routed event; `TabView.IsAddTabButtonVisible`, `TabView.TabWidthMode` (`SizeToContent` / `Equal` / `Compact`), `TabView.CloseButtonOverlayMode` (`Auto` / `OnPointerOver` / `Always`), plus `TabView.AddTabButtonClick` and `TabView.TabCloseRequested` routed events. Template parts: `PART_AddTabButton`, `PART_CloseButton`. Default style in `Themes/Controls/TabView.xaml`.
- **`TabViewWidthMode`** and **`TabViewCloseButtonOverlayMode`** enums in `Fluence.Wpf` (namespace intentionally flat to match the rest of the public enums).
- **`TabViewTabCloseRequestedEventArgs`** routed event args carrying `Tab` (the originating `TabViewItem`) and `Item` (the bound data item).
- **`Fluence.Wpf.Demo/Pages/GalleryTabsPage`** - new "Tabs" entry in the demo `NavigationView`; shows `TabControl` and `TabView` side-by-side, wires up add-tab and close-tab handlers, and demonstrates `IsClosable="False"` for pinned tabs.
- **`GalleryScreenshotHarness`** (`Fluence.Wpf.Tests`) - MSTest-driven `RenderTargetBitmap` capture of the gallery home surface across Light / Dark / High Contrast at 1.0× and 1.5× DPI. Opt-in: set `FLUENCE_CAPTURE_SCREENSHOTS=1` and run the test to regenerate `docs/screenshots/banner-{theme}-{scale}x.png`.
- **`docs/screenshots/`** - committed banner captures (`banner-light-1x.png`, `banner-dark-1x.png`, `banner-highcontrast-1x.png`, and 1.5× counterparts) for documentation and README use.
- **13 new MSTests** in `TabViewTests.cs` covering default dependency-property values, container generation, template parts, add-tab invoke → `AddTabButtonClick`, close-button invoke → `CloseRequested` → `TabView.TabCloseRequested` bubbling, `IsClosable="False"` hides the close button, and `IsAddTabButtonVisible="False"` hides the add button.

### Changed

- **Demo `MainWindow` navigation** now exposes 11 pages (was 10) - the new "Tabs" entry sits between "Data" and "Glyphs". The existing `MainWindow_NavigationView_HasTenNavItems` test was renamed to `MainWindow_NavigationView_HasElevenNavItems` and updated to assert 11.

## [0.3.0] - 2026-04-17

### Added

- **`Card.Click` routed event** plus `IsClickable` / `IsPressed` dependency properties. The demo home page now uses clickable cards to route into the gallery (see `Fluence.Wpf.Demo/Pages/GalleryHomePage.xaml`).
- **`ControlStrongStrokeColorDefault`** and **`ControlStrongStrokeColorDisabled`** color tokens and matching `*Brush` keys in every theme (Light `#72000000` / Dark `#8BFFFFFF` / High Contrast `#FFFFFFFF`), aligned to WinUI 3 `Common_themeresources.xaml`.
- **7 new MSTests** in `ControlTests.FluentStroke.cs` covering the `RadioButton` outer ring, disabled ring swap, `Card.Click` press/release semantics, and the `NavigationView` Left / LeftCompact content-border corner radius + stroke contract.
- **Theme-aware demo banner** in `GalleryHomePage` - `BannerLight.png` / `BannerDark.png` swap in response to `ApplicationThemeManager.Changed` without a page reload.

### Changed

- **`NavigationView` layout** redesigned to match the WinUI 3 reference:
  - Pane toggle sits above the back button, both 40×40, centered in a 48 px pane column.
  - Selection indicator is now a single `PART_SelectionIndicator` that animates between items (3×16 vertical / 16×3 horizontal).
  - Content region draws a 1 px top/left `CardStrokeColorDefault` border with `CornerRadius="8,0,0,0"` in both `Left` and `LeftCompact` templates so the content visually hugs the top-left.
  - Background defaults to `Transparent`; content surface defaults to `LayerFillColorDefaultBrush`.
- **`RadioButton` / `CheckBox` unchecked rings** switched from the subtle `ControlStrokeColorDefaultBrush` to `ControlStrongStrokeColorDefaultBrush`, fixing visibility against light backgrounds (reported as _"radio buttons barely visible"_).
- **Demo `MainWindow`** - search box moved into `FluenceWindow.TitleBar`; filter handler now toggles `NavigationViewItem.Visibility` instead of repopulating the items collection. Back-stack plumbing was removed in favour of `NavigateTo(tag)` + selection-driven navigation.
- **`docs/getting-started.md`**, **`docs/theming.md`**, and **`docs/controls.md`** refreshed for the new Left-mode defaults and the `ControlStrongStroke*` tokens.
- **`CLAUDE.md`** rewritten as a single self-contained maintainer handbook - project overview, architecture, coding standards, control-authoring checklist, theme architecture, testing, pitfalls, and quality gates.

### Removed

- Stale `Themes/Light copy.xaml` and `Themes/Dark copy.xaml` (unused duplicates from an earlier migration).
- `MIGRATION_TRACKING.md` (root and `PSAppDeployToolkit/lib/Fluence.Wpf/`) - the migration is complete, so the log has been archived in git history rather than in the repository.
- The repo-folder-rename note in `KNOWN_ISSUES.md` - the root is now `Fluence.Wpf`.

## [0.2.0] - 2026-04-14

### Added

- Demo gallery restructured: `NavigationView` (**LeftCompact**), search, and split `Pages/*.xaml` user controls.
- Public documentation set: `docs/getting-started.md`, `theming.md`, `controls.md`, `migration-guide.md`, `contributing.md`.
- Test infrastructure: shared `WpfTestSta` STA dispatcher, `[assembly: DoNotParallelize]`, `ThemeTestHelpers`, DPI assertion on `net10`, and coverage for `NumberBox`, `Expander`, `DropDownButton`, `InfoBadge`, `ListBox` container override.

### Changed

- README installation guidance (project reference + local `dotnet pack`); documentation links updated.
- Internal session and migration notes moved under `docs/_internal/`.
- `CLAUDE.md` expanded into a self-contained maintainer handbook (generic comparisons only).

## [0.1.0] - 2026-04-02

### Added

- Initial release of **Fluence.Wpf**:
  - `ApplicationThemeManager` - Light / Dark / High Contrast / Auto theme switching with stable merged dictionary indices.
  - `ApplicationAccentColorManager` - System accent palette and custom accent ramps mapped to WinUI-aligned resource keys.
  - `SystemThemeWatcher` - Live reaction to Windows theme and accent settings while the app runs.
  - `FluenceWindow` - DWM Mica, Acrylic, and Tabbed backdrops; rounded corners; caption button visibility overrides.
  - Fluent-styled controls: Button, HyperlinkButton, CheckBox, RadioButton, ToggleSwitch, TextBox, PasswordBox, ComboBox, Slider, ProgressBar, ProgressRing, ListView, Card, InfoBar, NavigationView, FontIcon, Border, StackPanel, DockPanel, SmoothScrollViewer; tab and scroll bar themes.
  - Layered resource dictionaries (theme colors, brushes, accent ramp, typography, control templates).
  - Demo gallery application and MSTest suite (theme stability, accent, window policy, control templates).
  - GitHub Actions CI (build + test), documentation, and contributor guidelines.

[Unreleased]: https://github.com/sintaxasn/fluence-wpf/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/sintaxasn/fluence-wpf/releases/tag/v0.3.0
[0.2.0]: https://github.com/sintaxasn/fluence-wpf/releases/tag/v0.2.0
[0.1.0]: https://github.com/sintaxasn/fluence-wpf/releases/tag/v0.1.0
