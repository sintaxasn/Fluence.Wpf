# Repository Layout

This document explains what each project, folder, and important file category is for. It also calls out where the demo applications follow normal WPF structure and where they intentionally use custom gallery conventions.

## Standard WPF Baseline

Microsoft's WPF project model gives a normal application these core pieces:

- `App.xaml` / `App.xaml.cs` define the application object, startup behavior, and application-wide resources.
- `MainWindow.xaml` / `MainWindow.xaml.cs` define the main window and its code-behind.
- XAML windows, pages, user controls, and resource dictionaries are normally compiled as MSBuild `Page` items.
- Images and other always-available assets can be compiled as WPF `Resource` items.
- Loose files that must remain readable or updateable after build can be `Content` items copied to the output folder.

Official references:

- [Overview of WPF windows](https://learn.microsoft.com/dotnet/desktop/wpf/windows/)
- [WPF application resource, content, and data files](https://learn.microsoft.com/dotnet/desktop/wpf/app-development/wpf-application-resource-content-and-data-files)
- [Merged resource dictionaries](https://learn.microsoft.com/dotnet/desktop/wpf/systems/xaml-resources-merged-dictionaries)

## Architecture Summary

`Fluence.Wpf` is not a clean-architecture business application. It is a reusable WPF control library with demo and test projects around it.

The structure is therefore expected to look different from a repository-pattern or Clean Architecture WPF app:

- There is no `Domain`, `Application`, `Infrastructure`, or `Repositories` project because the library has no durable data store or business use cases.
- `Fluence.Wpf.Demo` is a gallery application. Its job is visual and interaction coverage, so it uses navigation metadata, concrete sample pages, and embedded source snippets instead of repositories and service layers.
- `Fluence.Wpf.Demo.Mvvm` is the small MVVM example. It demonstrates view models and commands, but still does not need repositories because its task data is in-memory demo data.
- `Fluence.Wpf.Tests` is intentionally broad because it verifies templates, theme dictionaries, demo shell behavior, and WPF dispatcher-sensitive interactions on both target frameworks.

## Solution Root

| Path | Purpose |
|------|---------|
| `Fluence.Wpf.sln` | Visual Studio solution containing the library, main gallery demo, MVVM demo, and test project. |
| `AGENTS.md` | Repository handbook and source of truth for coding rules, target frameworks, theme architecture, testing gates, and workflow. |
| `CLAUDE.md` | Same handbook content for tools that look for Claude-style repository instructions. |
| `README.md` | Public package overview, install instructions, and primary consumer-facing introduction. |
| `CHANGELOG.md` | Release and unreleased change history. |
| `KNOWN_ISSUES.md` | Tracked known gaps and pre-existing failures. |
| `LICENSE` | BSD 3-Clause license. |
| `.editorconfig` | Editor and analyzer formatting conventions. |
| `.gitattributes` | Git text normalization policy. |
| `.gitignore` | Generated file and build output exclusions. |
| `.mcp.json` | Local MCP/tool configuration. |
| `.github/` | GitHub-specific automation and assistant instructions. |
| `.claude/` | Local agent/tooling configuration, skills, and support prompts. Not part of the package surface. |
| `.pi/` | Local planning/task tooling state. Not part of the package surface. |
| `.vscode/` | Workspace editor settings. |
| `.history/` | Local backup/history artifacts. Do not treat as source of truth unless explicitly doing recovery work. |
| `.vs/` | Visual Studio local state. Ignored. |
| `assets/` | Repository-level branding images: `AppBanner_Dark.png`, `AppBanner_Light.png`, `AppIcon.png`, and editable `AppIcon.svg`. |
| `docs/` | Public documentation. `layout.md` belongs here because it explains the public repository layout. |
| `TestResults/` | Local test run output. Generated. |

## `Fluence.Wpf`

Role: reusable WPF control library. It targets `net472` and `net10.0-windows10.0.26100.0`.

### Project Files

| Path | Purpose |
|------|---------|
| `Fluence.Wpf.csproj` | Library project file. Enables WPF, multi-targets `net472` and `.NET 10`, turns warnings into errors, emits XML docs, and includes README/icon package content. |
| `README.md` | Library-folder quick reference for maintainers and consumers browsing the source tree. |

### Root Code Files

| Path | Purpose |
|------|---------|
| `ApplicationThemeManager.cs` | Owns theme application, five managed resource dictionary slots, theme key promotion, and `Changed` notifications. |
| `ApplicationAccentColorManager.cs` | Applies system, application, and custom accent colors and updates accent ramp resources in place. |
| `SystemThemeWatcher.cs` | Watches Windows theme/accent changes and routes them back through the theme manager. |
| `ThemeChangedEventArgs.cs` | Event args for theme change notifications. |

### Folders

| Folder | Purpose |
|--------|---------|
| `Automation/` | UI Automation peers for custom controls that need screen reader or automation behavior beyond WPF defaults. |
| `Controls/` | Public control classes and related event args. The matching default styles live under `Themes/Controls/`. |
| `Converters/` | Internal WPF value converters used by control templates. |
| `Enums/` | Public enums used by controls, theme APIs, and dependency properties. |
| `Helpers/` | Internal helpers for accent math, OS checks, animation helpers, registry reads, and rendering utilities. |
| `Native/` | Win32/DWM interop constants, structs, and P/Invoke declarations. |
| `Properties/` | Assembly metadata that is not generated by the SDK project. |
| `Themes/` | WPF resource dictionaries: colors, brushes, typography, generic control templates, and design-time resources. |

### Notable File Categories

| Pattern | Purpose |
|---------|---------|
| `Controls/<Control>.cs` | Control type, dependency properties, routed events, template-part wiring, and behavior. |
| `Controls/<Control>EventArgs.cs` | Event payload types for public control events. |
| `Themes/Controls/<Control>.xaml` | Default WPF style and `ControlTemplate` for a control. |
| `Themes/Colors/Theme.Light.xaml`, `Theme.Dark.xaml`, `Theme.HighContrast.xaml` | Theme-specific color tokens. |
| `Themes/Brushes/Brushes.xaml` | Brush resources derived from color tokens. |
| `Themes/Accent/Accent.xaml` | Accent ramp resources updated by `ApplicationAccentColorManager`. |
| `Themes/Typography/Typography.xaml` | Fluent typography styles and font metrics. |
| `Themes/Generic.xaml` | Library style entry point that merges all control dictionaries. |
| `Themes/Light.xaml`, `Dark.xaml`, `HighContrast.xaml` | Compatibility theme dictionaries. The managed theme slots use the newer `Themes/Colors/*` dictionaries. |
| `Themes/DesignTime.xaml` | Design-time preview resources. |

### Control Class Map

| Area | Files |
|------|-------|
| Window chrome | `FluenceWindow.cs`, `TitleBar.cs`, `CaptionButtonChrome.cs`, `WindowPolicy.cs` |
| Buttons and commands | `Button.cs`, `HyperlinkButton.cs`, `DropDownButton.cs`, `SplitButton.cs`, `RepeatButton.cs`, `ToggleButton.cs` |
| Selection and input | `CheckBox.cs`, `RadioButton.cs`, `ToggleSwitch.cs`, `ComboBox.cs`, `TextBox.cs`, `PasswordBox.cs`, `NumberBox.cs`, `Slider.cs`, `RatingControl.cs` |
| Data and collections | `ListView.cs`, `ListBox.cs`, `ListBoxItem.cs`, `TreeView.cs`, `TreeViewItem.cs` |
| Navigation and tabs | `NavigationView.cs`, `NavigationViewItem.cs`, `NavigationViewItemHeader.cs`, `NavigationViewItemSeparator.cs`, `NavigationViewBackRequestedEventArgs.cs`, `NavigationViewItemInvokedEventArgs.cs`, `TabView.cs`, `TabViewItem.cs`, `TabViewTabCloseRequestedEventArgs.cs` |
| Status and identity | `InfoBar.cs`, `InfoBarClosingEventArgs.cs`, `InfoBadge.cs`, `ProgressBar.cs`, `ProgressRing.cs`, `PersonPicture.cs` |
| Layout and surfaces | `Border.cs`, `Card.cs`, `DockPanel.cs`, `Expander.cs`, `Separator.cs`, `SmoothScrollViewer.cs`, `StackPanel.cs` |
| Menus and text | `ContextMenu.cs`, `Menu.cs`, `MenuItem.cs`, `ToolTip.cs`, `FontIcon.cs`, `TextBlock.cs`, `TextBlockExtensions.cs` |

## `Fluence.Wpf.Demo`

Role: primary gallery app and manual verification surface. It targets `net472` and `net10.0-windows10.0.26100.0`.

### Standard or Custom?

The demo follows the standard WPF app shape at the outer level:

- `App.xaml` / `App.xaml.cs` define the WPF application.
- `MainWindow.xaml` / `MainWindow.xaml.cs` define the main window.
- `Pages/*.xaml` / `Pages/*.xaml.cs` are normal compiled WPF `UserControl` pages.
- `Resources/*.png`, `Resources/ControlImages/*.png`, and `Resources/SegoeFluentIcons.tsv` are WPF `Resource` items.

The demo intentionally keeps its structure simple:

- `DemoNavigationCatalog.cs` is metadata only. `MainWindow` maps routes to concrete `Pages/Gallery*Page.xaml` controls with a direct C# switch.
- Sample source text lives beside the sample page in `const string` fields and is passed to `DemoSampleControl.XamlSource` / `DemoSampleControl.CSharpSource`. There is no copied `Samples/**` source tree.

This is a normal structure for a control-gallery demo, but not a standard line-of-business MVVM structure. Do not add repositories or service layers here unless the demo starts persisting or retrieving real data.

### Project Files

| Path | Purpose |
|------|---------|
| `Fluence.Wpf.Demo.csproj` | WPF executable project. References `Fluence.Wpf` and compiles app, page, and resource files. |
| `README.md` | Demo-specific run and maintenance notes. |
| `App.xaml` | WPF application definition. |
| `App.xaml.cs` | Startup code that applies Fluence theme resources before the main window runs. |
| `MainWindow.xaml` | Gallery shell layout: `FluenceWindow`, title bar, search box, and navigation host. |
| `MainWindow.xaml.cs` | Gallery orchestration: navigation catalog loading, search, theme/accent/backdrop controls, and direct page selection. |
| `DemoNavigationCatalog.cs` | Flat navigation metadata used by `MainWindow` to build the `NavigationView`. |

### `Pages/`

`Pages/` contains compiled WPF `UserControl` pages and reusable page controls. Gallery pages declare live examples directly in XAML where practical and keep the matching source snippets in their code-behind.

| File | Purpose |
|------|---------|
| `DemoSampleControl.xaml` / `.cs` | Reusable example host: sample surface plus collapsed inline source expander backed by `XamlSource` and optional `CSharpSource` strings. |
| `GalleryHomePage.xaml` / `.cs` | Landing page with banner and high-level navigation cards. |
| `GalleryAccessibilityPage.xaml` / `.cs` | Accessibility overview page. |
| `GalleryButtonsPage.xaml` / `.cs` | Button family gallery page. |
| `GalleryColorsPage.xaml` / `.cs` | Theme, accent, and brush gallery page. |
| `GalleryDataBindingPage.xaml` / `.cs` | Data binding examples. |
| `GalleryDataPage.xaml` / `.cs` | List/data control examples. |
| `GalleryFormsPage.xaml` / `.cs` | Form composition examples. |
| `GalleryGlyphsPage.xaml` / `.cs` | Fluent glyph browsing examples. |
| `GalleryInputsPage.xaml` / `.cs` | Text and numeric input examples. |
| `GalleryLayoutPage.xaml` / `.cs` | Layout and surface examples. |
| `GalleryMenusPage.xaml` / `.cs` | Menu, context menu, tooltip, and flyout examples. |
| `GalleryNavigationPage.xaml` / `.cs` | Navigation control examples. |
| `GallerySelectionPage.xaml` / `.cs` | CheckBox, RadioButton, ToggleSwitch, Slider, and Rating examples. |
| `GalleryStatusPage.xaml` / `.cs` | InfoBar, InfoBadge, ProgressBar, ProgressRing, and PersonPicture examples. |
| `GalleryTabsPage.xaml` / `.cs` | TabControl and TabView examples. |
| `GalleryTreesPage.xaml` / `.cs` | TreeView examples. |
| `GalleryTypographyPage.xaml` / `.cs` | Typography token examples. |
| `GalleryWindowPage.xaml` / `.cs` | Window, title-bar, backdrop, and caption-button examples. |

### `Resources/`

| Path | Purpose |
|------|---------|
| `AppIcon.png` | Demo and package icon. |
| `BannerLight.png` / `BannerDark.png` | Home page banner artwork for light and dark themes. |
| `DemoSharedStyles.xaml` | Gallery-only shared styles. |
| `SegoeFluentIcons.tsv` | Data source for glyph browsing. |
| `ControlImages/*.png` | WinUI Gallery-style control thumbnails. Filenames correspond to control or feature names, for example `Button.png`, `NavigationView.png`, `TabView.png`, `ProgressRing.png`, and `FluenceWindow`-adjacent windowing images. |

## `Fluence.Wpf.Demo.Mvvm`

Role: small MVVM Task Manager demo. It targets only `net10.0-windows10.0.26100.0` and uses `CommunityToolkit.Mvvm`.

This project is closer to a standard MVVM WPF app than the gallery demo, but it is intentionally small. It does not use a repository layer because it has no database, file store, or service boundary.

| Path | Purpose |
|------|---------|
| `Fluence.Wpf.Demo.Mvvm.csproj` | WPF executable project. References `Fluence.Wpf` and `CommunityToolkit.Mvvm`. |
| `README.md` | MVVM demo-specific notes. |
| `App.xaml` | WPF application definition. It intentionally does not merge Fluence dictionaries manually. |
| `App.xaml.cs` | Startup code that applies the Fluence theme manager. |
| `MainWindow.xaml` | Task Manager UI using Fluence controls and bindings. |
| `MainWindow.xaml.cs` | Minimal window code-behind. The demo behavior lives in view models. |
| `Resources/AppIcon.png` | App icon resource. |
| `Converters/EnumToBoolConverter.cs` | Converts `FilterMode` enum values to radio-button checked states. |
| `ViewModels/FilterMode.cs` | Filter enum for all/active/completed task views. |
| `ViewModels/MainViewModel.cs` | Main MVVM state: task collection, displayed task projection, add/delete/toggle/filter commands, status text, and progress. |
| `ViewModels/TaskItemViewModel.cs` | Per-task state and property-change notifications. |

## `Fluence.Wpf.Tests`

Role: MSTest suite for the library and demo. It targets `net472` and `net10.0-windows10.0.26100.0`.

### Project Files and Harness

| Path | Purpose |
|------|---------|
| `Fluence.Wpf.Tests.csproj` | MSTest project. References the library and gallery demo. Disables parallel TFM execution and adds analyzer suppressions specific to tests. |
| `README.md` | Test project notes. |
| `test.runsettings` | Local test run settings. |
| `DisableParallelization.cs` | Assembly-level MSTest setting that prevents unsafe WPF parallelization. |
| `WpfTestSta.cs` | Shared STA dispatcher harness used by WPF tests. |
| `ThemeTestHelpers.cs` | Shared helpers for applying themes, generic dictionaries, and theme cycles. |
| `ThemeTestHelpersTests.cs` | Tests for the helper behavior itself. |
| `Properties/AssemblyInfo.cs` | Test assembly metadata. |

### Theme and Resource Tests

| File | Purpose |
|------|---------|
| `AccentColorManagerTests.cs` | Accent ramp and accent manager behavior. |
| `DictionaryStabilityTests.cs` | Enforces the managed theme dictionary slot contract. |
| `ThemeManagerTests.cs` | Theme manager behavior and resource promotion. |
| `ThemeMetricsTests.cs` | Theme metrics and resource values. |
| `TypographyResourceContractTests.cs` | Typography resource contract. |
| `TextRenderingPolicyTests.cs` | Text rendering policy expectations. |

### Control Tests

| File | Purpose |
|------|---------|
| `ControlTests.cs` | Shared partial test class and broad control coverage. |
| `ControlTests.<Area>.cs` | Focused partial test files for individual controls or behavior areas. Existing areas include Button, CaptionButtons, Card, ComboBox, ContextMenu, Expander, FluentStroke, FocusVisual, InfoBadge, InfoBar, ListView, Menu, NavigationView, NumberBox, PersonPicture, PopupCornerRadius, ProgressRing, RatingControl, ScrollBar, Separator, Slider, SplitButton, TabView, TextBox, ToggleSwitch, ToolTip, and TreeView. |
| `AdditionalControlsTests.cs` | Additional smoke and regression coverage for controls not isolated elsewhere. |
| `ComboBoxTests.cs` | ComboBox-specific behavior tests. |
| `SplitButtonTests.cs` | SplitButton-specific behavior tests. |
| `TabViewTests.cs` | TabView-specific behavior tests. |
| `ListViewIsItemSelectableTests.cs` | ListView item selectability behavior. |
| `ControlRenderingTests.cs` | Rendering-oriented control checks. |

### Window and Demo Tests

| File | Purpose |
|------|---------|
| `DemoMainWindowTests.cs` | Gallery shell, navigation, sample source, category, and demo behavior tests. |
| `FluenceWindowHardenTests.cs` | FluenceWindow hardening and edge-case coverage. |
| `FluenceWindowTitleBarTests.cs` | Title bar API and behavior coverage. |
| `FluentCaptionButtonChromeTests.cs` | Caption button chrome tests. |
| `GalleryScreenshotHarness.cs` | Optional screenshot generation harness, gated by `FLUENCE_CAPTURE_SCREENSHOTS=1`. |
| `WindowPolicyTests.cs` | Window policy behavior tests. |

## Adjacent Non-Solution Folders

These folders are present in the repository but are not currently projects in `Fluence.Wpf.sln`.

| Path | Purpose |
|------|---------|
| `Fluence.Wpf.Demo.PowerShell/` | PowerShell-hosted proof/demo surface. Contains `Show-FluenceDemo.ps1`, `MainWindow.xaml`, a local `Fluence.Wpf.dll`, and a README. It is intended for Windows PowerShell 5.1 with `-STA`. |
| `Fluence.Wpf.Gallery/` | Placeholder/documentation folder with a README. It is not currently a `.csproj` in the solution. |

## Where To Put New Work

| Change | Location |
|--------|----------|
| New public control type | `Fluence.Wpf/Controls/<Control>.cs` |
| New control template | `Fluence.Wpf/Themes/Controls/<Control>.xaml`, then merge from `Themes/Generic.xaml` |
| New theme color or brush | Add colors to each `Themes/Colors/Theme.*.xaml`, then add brush aliases in `Themes/Brushes/Brushes.xaml` |
| New enum for public API | `Fluence.Wpf/Enums/` |
| New automation behavior | `Fluence.Wpf/Automation/` |
| New Win32/DWM interop | `Fluence.Wpf/Native/` plus helper wrapper if needed |
| New gallery category or nav item | `Fluence.Wpf.Demo/DemoNavigationCatalog.cs` |
| New gallery page | `Fluence.Wpf.Demo/Pages/Gallery<Area>Page.xaml` and `.xaml.cs`, then add a route in `MainWindow` |
| New inline source sample | Add live sample XAML to the page and put the display source in page-local `const string` fields passed to `DemoSampleControl` |
| New MVVM demo state | `Fluence.Wpf.Demo.Mvvm/ViewModels/` |
| New MVVM value converter | `Fluence.Wpf.Demo.Mvvm/Converters/` |
| New control tests | `Fluence.Wpf.Tests/ControlTests.<Area>.cs` or an existing matching partial file |
| New demo shell tests | `Fluence.Wpf.Tests/DemoMainWindowTests.cs` |
| New public documentation | `docs/*.md` |
| Maintainer-only notes | `docs/_internal/` if created; do not link from public docs |

## Demo App Structure Verdict

`Fluence.Wpf.Demo` is structurally reasonable for a control gallery:

- Its WPF entry points are standard.
- Its shell is intentionally code-behind heavy because it owns gallery navigation, shell state, and theme/accent/backdrop controls.
- Its gallery pages are concrete WPF user controls, so debugger and visual-tree inspection stay close to the sample being demonstrated.
- Its source snippets are embedded with the page that owns the live sample; there is no separate generated page or copied sample-file layer.

It would be non-standard only if judged as a business MVVM app. For this repository, that is the wrong comparison. The standard MVVM example is `Fluence.Wpf.Demo.Mvvm`; the main demo is a gallery and visual verification tool.
