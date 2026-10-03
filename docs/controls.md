# Control catalog

This catalog lists the public controls in `Fluence.Wpf.Controls`. Add `xmlns:fluence="http://schemas.fluencewpf.com"` in XAML and call `ApplicationThemeManager.Apply` before creating the first window. The [gallery](../Fluence.Wpf.Demo/README.md) groups live examples by task. The [C# API reference](api/index.md) documents each type's constructors, properties, methods, and events. Follow the [Basic walkthrough](https://fluencewpf.com/docs/csharp/usage) for a runnable XAML example.

## Window and application shell

| Type | Use it for | Start here |
| --- | --- | --- |
| [FluenceWindow](controls/fluence-window.md) | WPF window with a Fluent title bar, caption controls, backdrop policy, and corner preference | [Window guide](how-to/window-and-title-bar.md) |
| [TitleBar](controls/title-bar.md) | Title, back request, pane toggle request, and custom title-area content | [Gallery shell](../Fluence.Wpf.Demo/MainWindow.xaml) |

[FluenceWindow](controls/fluence-window.md) extends `Window`. `SystemBackdropType` requests Mica, Acrylic, Tabbed, Auto, or None. Its [TitleBar](controls/title-bar.md) property accepts a UI element; the [TitleBar](controls/title-bar.md) control raises `BackRequested` and `PaneToggleRequested`.

## Actions and selection

| Types | Use them for | Main behavior |
| --- | --- | --- |
| [Button](controls/button.md), [HyperlinkButton](controls/hyperlink-button.md), [AppBarButton](controls/app-bar-button.md), [RepeatButton](controls/repeat-button.md) | Commands and repeated actions | WPF command and click patterns |
| [ToggleButton](controls/toggle-button.md), [ToggleSwitch](controls/toggle-switch.md) | On/off and optional indeterminate state | Checked state; [ToggleSwitch](controls/toggle-switch.md) exposes a switch presentation |
| [DropDownButton](controls/drop-down-button.md), [SplitButton](controls/split-button.md), [ToggleSplitButton](controls/toggle-split-button.md) | Primary action with a flyout | Flyout opening; toggle split button also reports `IsCheckedChanged` |
| [CheckBox](controls/check-box.md), [RadioButton](controls/radio-button.md) | Independent or exclusive choices | Standard WPF checked state |
| [Slider](controls/slider.md), [RatingControl](controls/rating-control.md) | Continuous or discrete ratings | Value and change events |

`ControlAppearance` selects the visual emphasis for supported controls. The gallery's Buttons and Selection pages show the variants. For a primary command, `Appearance="Accent"` is the common choice.

## Text, forms, and pickers

| Types | Use them for | Key detail |
| --- | --- | --- |
| [TextBox](controls/text-box.md), [AutoSuggestBox](controls/auto-suggest-box.md) | Text entry and suggestions | `PlaceholderText`; query and suggestion events on [AutoSuggestBox](controls/auto-suggest-box.md) |
| Native WPF `PasswordBox` with [PasswordBoxExtensions](controls/password-box-extensions.md) | Password entry | The native sealed control gets an implicit Fluence style and attached properties |
| [NumberBox](controls/number-box.md) | Numeric entry with spin behavior | `Value`, `ValueChanged`, spin placement options |
| [ComboBox](controls/combo-box.md) | Choose from a drop-down list | WPF items and selection APIs, plus placeholder support |
| [DatePicker](controls/date-picker.md), [TimePicker](controls/time-picker.md) | Choose a date or time | Dedicated selected-value properties and changed events |
| [ColorPicker](controls/color-picker.md) | Choose a color | `Color`, `ColorChanged` |

The Fluence [DatePicker](controls/date-picker.md) is a `Control` with a three-column picker, not a subclass of the native WPF calendar picker. Use its own `SelectedDate` API. See [inputs and data](how-to/inputs-and-data.md) and the gallery's Inputs and Forms pages.

## Navigation and tabs

| Types | Use them for | Main behavior |
| --- | --- | --- |
| [NavigationView](controls/navigation-view.md), [NavigationViewItem](controls/navigation-view-item.md), [NavigationViewItemHeader](controls/navigation-view-item-header.md), [NavigationViewItemSeparator](controls/navigation-view-item-separator.md) | Application destinations in a left or top pane | `ItemInvoked`, `BackRequested`, `PaneDisplayMode`, `FooterMenuItems` |
| [BreadcrumbBar](controls/breadcrumb-bar.md), [BreadcrumbBarItem](controls/breadcrumb-bar-item.md) | Location hierarchy | `ItemClicked` |
| [PipsPager](controls/pips-pager.md) | A compact sequence of pages | `SelectedIndexChanged` |
| [SelectorBar](controls/selector-bar.md), [SelectorBarItem](controls/selector-bar-item.md) | A compact choice among views | WPF selection model |
| [SlideNavigationPresenter](controls/slide-navigation-presenter.md) | Transition between navigation content | Slide transition effect |
| [TabView](controls/tab-view.md), [TabViewItem](controls/tab-view-item.md) | Document-like tabs | Add and close requests; `TabWidthMode` |

[NavigationView](controls/navigation-view.md) derives from `Selector`. Put its page host in `NavigationView.Content`, because an unnamed child enters the item collection. The application controls its navigation history. [TabView](controls/tab-view.md) extends WPF `TabControl`; a close request does not remove the item for you. See [navigation and tabs](how-to/navigation-and-tabs.md).

## Data, collections, and people

| Types | Use them for | Key detail |
| --- | --- | --- |
| [ListBox](controls/list-box.md), [ListBoxItem](controls/list-box-item.md), [ListView](controls/list-view.md) | Data-bound item lists | Familiar WPF items, templates, and selection; `ListView.ItemsLayout` chooses layout |
| [TreeView](controls/tree-view.md), [TreeViewItem](controls/tree-view-item.md) | Hierarchical collections | Expansion and selection; `TreeView.SelectedItems` for multiple selection |
| [Card](controls/card.md) | Passive or clickable content panel | `IsClickable` enables its `Click` event |
| [PersonPicture](controls/person-picture.md) | Initials or avatar imagery | Person representation |

The theme also styles some native framework elements used inside these controls. A `ListViewItem` is the WPF container, not a separate public Fluence class.

## Menus, flyouts, and dialogs

| Types | Use them for | Main behavior |
| --- | --- | --- |
| [Menu](controls/menu.md), [MenuItem](controls/menu-item.md), [ContextMenu](controls/context-menu.md), [ToolTip](controls/tool-tip.md) | Conventional menus and hints | WPF menu and popup interactions |
| [FlyoutBase](controls/flyout-base.md), [Flyout](controls/flyout.md), [FlyoutPresenter](controls/flyout-presenter.md) | Attached or programmatic anchored content | `ShowAt`, `Hide`, opening and closing events |
| [CommandBarFlyout](controls/command-bar-flyout.md), [CommandBarFlyoutPresenter](controls/command-bar-flyout-presenter.md) | A flyout with command actions | Provides command buttons in a popup |
| [TeachingTip](controls/teaching-tip.md) | Contextual instruction anchored to UI | `IsOpen`, action and close events |
| [ContentDialog](controls/content-dialog.md) | Modal decision inside an owner window | `ShowAsync`, result enum, button click events |

[FlyoutBase](controls/flyout-base.md) is an abstract support type. `ContentDialog.ShowAsync` needs an active owner and must run on the UI dispatcher. See [dialogs and feedback](how-to/dialogs-and-feedback.md).

## Status and layout

| Types | Use them for | Main behavior |
| --- | --- | --- |
| [InfoBar](controls/info-bar.md), [InfoBadge](controls/info-badge.md) | Inline message or compact status | Severity/style and open or close behavior |
| [ProgressBar](controls/progress-bar.md), [ProgressRing](controls/progress-ring.md) | Determinate or indeterminate progress | Progress value or ring state |
| [Border](controls/border.md), [StackPanel](controls/stack-panel.md), [DockPanel](controls/dock-panel.md), [Separator](controls/separator.md), [Expander](controls/expander.md) | Layout and grouped content | WPF layout patterns with Fluent defaults |
| [SmoothScrollViewer](controls/smooth-scroll-viewer.md), [ScrollBarExtensions](controls/scroll-bar-extensions.md) | Scrolling and native scroll bar behavior | Smooth scrolling and attached extensions |

## Text, icons, and images

| Types | Use them for | Key detail |
| --- | --- | --- |
| [TextBlock](controls/text-block.md), [TextBlockExtensions](controls/text-block-extensions.md) | Fluent typography | Fluence [TextBlock](controls/text-block.md) is a templated `ContentControl`; extensions also serve native text |
| [FontIcon](controls/font-icon.md) | Segoe Fluent icon glyphs | Set `Glyph` and optional icon font size |
| [Image](controls/image.md) | Images with templated corner clipping | Not a subclass of native WPF [Image](controls/image.md) |

Typography styles such as `BodyTextBlockStyle` and `TitleLargeTextBlockStyle` are application resources. See [theming](theming.md).

## Accessibility and extensibility

Custom controls expose UI Automation peers under `Fluence.Wpf.Automation`. The gallery includes focus, keyboard, high contrast, and right-to-left examples. Use the [accessibility reference](reference/accessibility.md) when checking an interaction. `Fluence.Wpf.Markup` adds `ThemeResourceExtension` and theme dictionaries for app-authored theme values; see the [theme guide](theming.md#theme-aware-markup).

The `Fluence.Wpf` namespace also contains theme managers, enums, and typed event arguments used by these controls. The [API reference map](reference/api.md) explains where to look them up.
