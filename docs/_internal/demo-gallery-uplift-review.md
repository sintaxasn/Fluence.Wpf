# Fluence.Wpf Demo Gallery Uplift Review

Date: 2026-05-02

Scope:

- Compare `Fluence.Wpf.Gallery` with the maintained `Fluence.Wpf.Demo`.
- Review `F:\StagedMigration\WinUIGallery` for transferable demo improvements.
- Investigate why dark-mode accent blue does not match the expected WinUI/system shade.

This is a read-only review of product code. No implementation changes were made.

## Executive Summary

`Fluence.Wpf.Demo` is already ahead of `Fluence.Wpf.Gallery`. The Gallery project is an untracked, stale side copy with the same general page/sample inventory but older API names, older project shape, and several regressions compared with Demo. Do not port Gallery wholesale into Demo.

The useful Gallery-to-Demo candidates are narrow:

- Restore or redesign local source-link behavior, if local source opening is still desired.
- Consider the Gallery page-reload subscription guard for `GalleryWindowPage`.
- Consider `ui:StackPanel Spacing` as a layout cleanup batch only if the team wants broad XAML churn.

Most future Demo uplift should come from `WinUIGallery` concepts, not from raw XAML ports. The best batches are page headers/source affordances, richer search, accessibility examples, fundamentals pages, and a better home/settings experience.

The dark-mode accent issue is not the WinUI dark/light mapping. Fluence maps dark accent fill to `SystemAccentColorLight2`, which matches WinUI. The problem is that the default/custom generated blue ramp is not the Windows accent palette. For default Windows blue, the local Windows palette reports `SystemAccentColorLight2 = #4CC2FF`; Fluence's generated ramp produces `#6BBFFF`, and `Accent.xaml` seeds `#6EB8F0`.

## Fluence.Wpf.Gallery vs Demo

Observed source inventory, excluding `bin`, `obj`, and `*.lscache`:

| State | Count | Meaning |
| --- | ---: | --- |
| Same | 113 | Byte-identical hand-authored files/resources. |
| Different | 208 | Mostly namespace/project-name drift, stale API names, and layout/style deltas. |
| Demo-only | 1 | `Fluence.Wpf.Demo.csproj`. |
| Gallery-only | 2 | `Fluence.Wpf.Gallery.csproj`, `Properties/AssemblyInfo.cs`. |

The meaningful grouped differences are below.

| Batch | Type | Gallery change or difference | Source evidence | Size | Effort | Portability / risk | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| G1 | Project shell | Separate `Fluence.Wpf.Gallery` app, `net472` project identity, explicit `Properties/AssemblyInfo.cs`. | `Fluence.Wpf.Gallery/Fluence.Wpf.Gallery.csproj`, `Fluence.Wpf.Gallery/Properties/AssemblyInfo.cs` | S | Low | Stale side project, not part of maintained solution flow. | Do not port. Treat Gallery as a comparison snapshot only. |
| G2 | Navigation coverage | Gallery has 42 catalog items. Demo has 45 and is richer: `Color contrast`, `CaptionButtonChrome`, `TitleBar`. | `Fluence.Wpf.Demo/DemoNavigationCatalog.cs`, `Fluence.Wpf.Gallery/DemoNavigationCatalog.cs` | S | Low | Copying Gallery would remove Demo coverage. | Do not port. If anything, update Gallery from Demo. |
| G3 | Windowing samples | Gallery collapses windowing into one `FluenceWindow` page and uses stale API names like `WindowBackdrop`; Demo uses `SystemBackdropType` and split windowing pages. | `Fluence.Wpf.Gallery/Pages/GalleryWindowPage.xaml.cs`, `Fluence.Wpf.Demo/Pages/GalleryWindowPage.xaml.cs` | M | Med | Gallery does not compile cleanly against current `FluenceWindow` without API updates. | Keep Demo's split-page model. |
| G4 | Caption visibility sample naming | Gallery uses `CaptionOverride` names and obsolete `MinimizeButtonVisibility`; Demo uses the current `IsMinimizeButtonVisible` style. | `Fluence.Wpf.Gallery/Samples/Window/BackdropAndCaptionButtons.xaml.cs`, `Fluence.Wpf/Controls/FluenceWindow.cs` | S | Low | Gallery follows obsolete wrapper properties in current code. | Do not port; keep current Demo API usage. |
| G5 | Source links | Gallery's `DemoSourceAction` can open local copied sample files or GitHub links via `GetSourceUri`; Demo currently routes source buttons to GitHub. | `Fluence.Wpf.Gallery/DemoSourceLinkSettings.cs`, `Fluence.Wpf.Gallery/Pages/DemoSourceAction.cs`, `Fluence.Wpf.Demo/Pages/DemoSourceAction.cs` | M | Med | Needs careful path handling for copied `Samples/**`, `siteoforigin` vs file URIs, and `Process.Start`. | Worth a focused batch if local source opening is wanted. |
| G6 | Source viewer robustness | Demo is ahead: clipboard retry handling, multi-target code paths, and more defensive source expansion. | `Fluence.Wpf.Demo/Pages/DemoSampleControl.xaml.cs`, `Fluence.Wpf.Gallery/Pages/DemoSampleControl.xaml.cs` | S | Low | Copying Gallery would regress clipboard reliability. | Do not port wholesale. Cherry-pick only local-link behavior. |
| G7 | Layout spacing | Gallery uses `ui:StackPanel Spacing` broadly; Demo often uses native `StackPanel` plus explicit margins. | Many `Fluence.Wpf.Gallery/Pages/*.xaml` and `Samples/**/*.xaml`; `Fluence.Wpf/Controls/StackPanel.cs` | M | Med | Broad XAML churn; may make examples depend more on Fluence layout controls. | Optional cleanup batch, not a functional uplift. |
| G8 | Home page | Gallery has a visible "WinUI Gallery features not shown here" callout; Demo has a tighter homepage. | `Fluence.Wpf.Gallery/Pages/GalleryHomePage.xaml`, `Fluence.Wpf.Demo/Pages/GalleryHomePage.xaml` | S | Low | In-app comparison text can feel like roadmap/unsupported API advertising. | Skip in UI. Put gap notes in docs/backlog instead. |
| G9 | Page reload subscription guard | Gallery keeps `ApplicationThemeManager.Changed` subscription state with `_isThemeManagerSubscribed`; Demo unsubscribes Loaded/Unloaded handlers after first use. | `Fluence.Wpf.Gallery/Pages/GalleryWindowPage.xaml.cs`, `Fluence.Wpf.Demo/Pages/GalleryWindowPage.xaml.cs` | S | Low | Useful only if pages can be detached and reattached. Must adapt to current Demo API names. | Consider as a small robustness patch. |
| G10 | Animation cleanup | Demo clears page entrance animations on completion; Gallery starts animations without cleanup. | `Fluence.Wpf.Demo/MainWindow.xaml.cs`, `Fluence.Wpf.Gallery/MainWindow.xaml.cs` | S | Low | Demo is better for long-running visual tree stability. | Keep Demo behavior. |
| G11 | Resource merge placement | Gallery adds `DemoSharedStyles.xaml` in `App.xaml.cs`; Demo merges it from `App.xaml`. | `Fluence.Wpf.Gallery/App.xaml.cs`, `Fluence.Wpf.Demo/App.xaml` | S | Low | Code merge is unnecessary and less declarative. | Keep Demo behavior. |

### Gallery Verdict

Recommended batches from Gallery:

1. **Source-link batch**: design whether source buttons should open local copied files, GitHub, or both. Bring only the behavior, not the stale project shape.
2. **Window page lifetime batch**: copy the subscription guard idea if page reloads matter.
3. **Optional XAML spacing cleanup**: evaluate `ui:StackPanel Spacing` in a narrow set of samples first. Do not mass-convert without visual review.

Everything else should be treated as stale or already superseded by Demo.

## WinUIGallery Opportunities

These are concepts to adapt to Fluence's existing `GalleryControlPage` and `DemoSampleControl` model. They are not direct WinUI XAML ports.

### Easy Batch

| Improvement | Type | Source | Demo target | Size | Effort | Dependencies | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Page header with API/source/docs affordances | Demo infrastructure | `WinUIGallery/Controls/PageHeader.xaml` | `Fluence.Wpf.Demo/Pages/GalleryControlPage.*`, catalog models | S | Low | Need metadata fields per page. | High value. Add API namespace, docs link, source link. |
| Better search ranking and no-results UI | Shell UX | `WinUIGallery/MainWindow.xaml.cs`, `SearchResultsPage.*` | `Fluence.Wpf.Demo/MainWindow.*` | S | Low | Demo already filters nav; improve scoring/display. | High value. Keep it demo-only. |
| Copy route/link actions | Demo workflow | WinUI nav item context actions | `DemoNavigationCatalog.cs`, `MainWindow.xaml.cs` | S | Low | Need stable route tags. | Useful for sharing sample locations. |
| Expand accessibility examples | Samples | `WinUIGallery/Samples/ControlPages/Accessibility/*` | `DemoAccessibilityPages.cs`, `Samples/Accessibility/*` | S/M | Low/Med | May reveal missing automation peer behavior. | Good batch after page-header work. |
| Add richer color/typography presentation controls | Design guidance | `WinUIGallery/Controls/DesignGuidance/*` | `GalleryColorsPage.*`, `GalleryTypographyPage.*` | S/M | Low/Med | Use Fluence resource keys, not WinUI-only keys. | Good visual polish batch. |

### Medium Batch

| Improvement | Type | Source | Demo target | Size | Effort | Dependencies | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Fundamentals section | Educational content | `WinUIGallery/Samples/ControlPages/Fundamentals/*` | New Demo pages/samples | M | Med | Adapt to WPF: resources, styles, binding, templates, custom controls. | Worth doing. Skip WinUI-only XAML conditionals or recast them. |
| Richer `DemoSampleControl` options | Demo infrastructure | `WinUIGallery/Controls/ControlExample.xaml` | `DemoSampleControl.*`, sample pages | M | Med | Optional controls, live code substitutions, tests. | Useful once page content stabilizes. |
| Home "recent/new" surface | Shell UX | `WinUIGallery/Pages/HomePage.xaml` | `GalleryHomePage.*`, catalog metadata | M | Med | "Recently added" is easy; favorites/recents need local settings. | Start with "new/updated" only. |
| Settings-like theme/accent/backdrop page | Shell UX | `WinUIGallery/Pages/SettingsPage.xaml` | `GalleryWindowPage.*` or new Settings page | M | Med | Avoid duplicating Window samples. | Good if Demo grows beyond a pure gallery. |
| Catalog metadata enrichment | Infrastructure | `WinUIGallery/Samples/Data/ControlInfoData.json` | Static catalog classes or future metadata file | M/L | Med/High | Could become a broad refactor. | Add fields to existing classes first; avoid full JSON migration initially. |

### Large Batch

| Improvement | Type | Source | Demo target | Size | Effort | Dependencies | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Missing control pages where Fluence has no API | Control/library parity | Many `WinUIGallery/Samples/ControlPages/*` | Library + Demo + tests | L/XL | High | Requires new controls before demo pages. | Do one control family at a time. |
| Data-driven catalog | Infrastructure | `ControlInfoData.json`, `ItemsPageBase.cs` | Catalog, search, source/docs links | L | High | Refactors navigation and tests. | Defer until content stabilizes. |
| Live XAML scratch pad | Tooling/demo | `ScratchPadPage.*` | New Demo page | L | High | WPF `XamlReader` sandboxing, namespaces, resources, security. | Defer. High risk for a gallery. |
| Motion/media/system samples | Samples/library | Connected animation, media, notifications, app/window APIs | Library + Demo | L/XL | High | Many WinUI-only APIs have no WPF equivalent. | Only after explicit library scope decisions. |

### Missing Control Families Before Direct WinUIGallery Page Ports

Fluence already has enough surface for pages around Button, DropDownButton, HyperlinkButton, RepeatButton, ToggleButton, SplitButton, CheckBox, ComboBox, RadioButton, RatingControl, Slider, ToggleSwitch, Card, ListBox, ListView, TreeView, Menu, ContextMenu, ToolTip, NavigationView, TabView, InfoBadge, InfoBar, ProgressBar, ProgressRing, PersonPicture, Border, DockPanel, Expander, Separator, StackPanel, TextBlock, TextBox, PasswordBox, NumberBox, FluenceWindow, TitleBar, and CaptionButtonChrome.

Do not add direct pages yet for these WinUI controls unless the library work is approved first:

- AutoSuggestBox, BreadcrumbBar, ColorPicker, ContentDialog, Flyout, Popup, TeachingTip.
- CommandBar, AppBarButton, AppBarToggleButton, AppBarSeparator, ToggleSplitButton.
- GridView, FlipView, ItemsRepeater, ItemsView, PullToRefresh.
- DatePicker, TimePicker, CalendarDatePicker, CalendarView, PipsPager, Pivot, SelectorBar.
- ScrollView, SemanticZoom, RelativePanel, SplitView, WrapPanel, Viewbox, VariableSizedWrapGrid.
- RichEditBox, RichTextBlock, media controls, notification/system/shell pages, map/web/storage picker pages.

Recommended WinUIGallery order:

1. Page header/source/docs affordances and richer search.
2. Accessibility page expansion and design-guidance page polish.
3. Fundamentals pages.
4. Home "new/updated" surface.
5. Missing control pages only where Fluence already has the API.
6. New library-backed controls last, one family at a time with tests.

## Dark Mode Accent Blue Investigation

### Reference Behavior

WinUI `Common_themeresources_any.xaml` uses different accent ramp entries by theme:

| Theme | `AccentFillColorDefaultBrush` reference |
| --- | --- |
| Default / dark | `SystemAccentColorLight2` |
| Light | `SystemAccentColorDark1` |
| HighContrast | System high-contrast colors |

Local reference:

- `F:\StagedMigration\WinUI_XAML\Controls\Common_themeresources_any.xaml:125`
- `F:\StagedMigration\WinUI_XAML\Controls\Common_themeresources_any.xaml:329`

Fluence runtime mapping matches that reference:

- `Fluence.Wpf/ApplicationAccentColorManager.cs:247-251`: dark -> `Light2`, `Light1`, base.
- `Fluence.Wpf/ApplicationAccentColorManager.cs:253-257`: light -> `Dark1`, `Dark2`, `Dark3`.
- `Fluence.Wpf/ApplicationAccentColorManager.cs:411-417`: `AccentFillColorDefault/Secondary/Tertiary` are emitted from the theme-adaptive primary color.

### Actual Windows Blue Palette On This Machine

Registry path: `HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Accent`.

For the current default Windows blue palette, bytes read as RGBA groups:

| Ramp key | Registry value |
| --- | --- |
| `SystemAccentColorLight3` | `#99EBFF` |
| `SystemAccentColorLight2` | `#4CC2FF` |
| `SystemAccentColorLight1` | `#0091F8` |
| `SystemAccentColor` | `#0078D4` |
| `SystemAccentColorDark1` | `#0067C0` |
| `SystemAccentColorDark2` | `#003E92` |
| `SystemAccentColorDark3` | `#001A68` |

Therefore, in dark mode, WinUI-style `AccentFillColorDefaultBrush` for default blue should resolve to `#4CC2FF`.

### Fluence Mismatch

There are two mismatching blue sources:

| Source | Values | Problem |
| --- | --- | --- |
| `Fluence.Wpf/Themes/Accent/Accent.xaml:13-19` | `Light2 = #6EB8F0`, `Dark1 = #005A9E`, etc. | Hard-coded defaults are not the current Windows default blue palette. |
| `HsvColorHelper.GenerateAccentRampWinaccent(#0078D4)` | `Light2 = #6BBFFF`, `Dark1 = #005A9F`, `Dark2 = #003C6A`, `Dark3 = #001E35` | Custom/fallback generation does not match the registry palette (`Light2 = #4CC2FF`). |

This is visible when:

- `ApplySystemAccent()` cannot read `AccentPalette` and falls back to generated colors.
- `ApplyApplicationAccent()` calls `ApplyCustomAccent(#0078D4)`.
- Demo swatches call `ApplyCustomAccent((Color)converted)` for `#0078D4`.
- Resources resolve from `Accent.xaml` before runtime accent refresh.

Demo sources:

- `Fluence.Wpf.Demo/Samples/Window/ThemeAndAccent.xaml.cs:89-93`
- `Fluence.Wpf.Demo/Pages/GalleryWindowPage.xaml.cs:205-212`

### Root Cause

The dark-mode selection of `Light2` is correct. The shade is wrong because Fluence's default/custom blue ramp is not the same palette as Windows/WinUI. The generated and XAML-seeded `Light2` colors are too pale/desaturated compared with the system palette `#4CC2FF`.

### Candidate Fix Batch

| Fix | Size | Effort | Risk | Notes |
| --- | --- | --- | --- | --- |
| Add exact tests for default Windows blue palette constants and dark/light adaptive mapping. | S | Low | Low | Extend `AccentColorManagerTests`; existing tests only assert non-default and relative mapping. |
| Update `Accent.xaml` seeded blue ramp to the current Windows blue palette. | S | Low | Low | Use `Light3 #99EBFF`, `Light2 #4CC2FF`, `Light1 #0091F8`, base `#0078D4`, `Dark1 #0067C0`, `Dark2 #003E92`, `Dark3 #001A68`. |
| Make `ApplyApplicationAccent()` use the known Windows blue palette instead of `ApplyCustomAccent(#0078D4)`. | S | Low | Low/Med | Avoids the most visible wrong shade when choosing the default app accent. |
| Decide what `ApplyCustomAccent(#0078D4)` should mean. | S/M | Med | Med | Either special-case default blue or document it as synthetic custom ramp. |
| Revisit generated custom ramp algorithm against .NET WPF or Windows palette behavior. | M | Med/High | Med | The current method claims it matches Windows AccentPalette, but local evidence shows it does not for `#0078D4`. |
| Consider using brush opacity for `AccentFillColorSecondary/TertiaryBrush` instead of alpha-baked colors. | M | Med | Med | WinUI uses same RGB plus opacity `0.9` / `0.8`; WPF resource semantics need tests. |

Recommended fix order:

1. Add focused tests for default blue palette and dark/light mapping.
2. Update `Accent.xaml` constants and `ApplyApplicationAccent()` to use the known Windows blue palette.
3. Update Demo's default blue swatch behavior so "Windows blue" does not take the synthetic custom-ramp path.
4. Separately evaluate the custom-ramp algorithm for non-blue custom colors.

## Notes

- `git diff --no-index` emitted `.gitattributes` warnings: lines 27 and 28 are currently parsed as invalid attributes. This review did not change `.gitattributes`, but the warning can obscure future diff output.
- `Fluence.Wpf.Gallery` is currently untracked in this working tree. Keep any cleanup or removal as a separate decision.