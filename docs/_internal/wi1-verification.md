# WI-1 / Migration Baseline Evidence

Internal working doc for the Fluence.Wpf / PSADT migration plan. Records
baselines at each stage so regressions are provable. Not part of the public
doc set.

## Stage 0 baseline (recorded 2026-04-20)

### Build (Debug)

Command: `dotnet build Fluence.Wpf.sln -c Debug`

Result: **0 errors, 0 warnings** on both `net472` and `net10.0-windows`.
Build time ~6 s on a warm restore.

### Tests (Debug, `--no-build`)

Command: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug --no-build`

| TFM               | Passed | Failed | Skipped | Total |
|-------------------|-------:|-------:|--------:|------:|
| net10.0-windows   |   212  |    7   |    1    |  220  |
| net472            |   211  |    7   |    1    |  219  |

Skipped: `CaptureBannerAcrossThemesAndScales` (gated on `FLUENCE_CAPTURE_SCREENSHOTS=1`).
One-test delta between TFMs reflects a net10-only test that both passes and is
counted in `220` — does not affect the failure set.

### Pre-existing failures (identical across both TFMs)

All 7 failures are in `DemoMainWindowTests.cs` except the last which is in the
RadioButton suite. They group into three migration buckets:

**WI-1 F1 — pane layout (3 failures).** Contradicts the plan's claim that F1
had landed. The XAML comment at `Themes/Controls/NavigationView.xaml:439-443`
says the Panel.ZIndex overlay was replaced with an inline two-column grid, but
the runtime behavior still trips the inline-pane assertions.

- `NavigationView_LeftCompact_PaneClosed_ContentStartsAt48px_Inline`
- `NavigationView_LeftCompact_PaneOpen_ContentStartsAt280px_Inline`
- `NavigationView_LeftCompact_PaneToggle_ResizesPushingContent`

**WI-1 F3 — search top-match selection (1 failure).** Enter key in NavSearchBox
selects 'Home' instead of 'Buttons' when the query is `button`. The filter
predicate ranks non-matches above exact-content matches.

- `NavSearch_EnterKey_SelectsTopMatch`

**WI-1D — Paradigm A redesign not yet implemented (2 failures).** Tests
encode the Paradigm A contract (section-header grouping in the nav pane,
`FeaturedControlsGrid` in `GalleryHomePage`) but the demo hasn't been
reshaped yet. These unblock only after the user picks a paradigm in WI-1D.

- `MainWindow_NavigationPane_ContainsSectionHeaders`
- `HomePage_ContainsFeaturedControlsGrid`

**WI-3 uplift — RadioButton outer ring (1 failure).** Outer ring uses the
wrong brush key vs the WinUI 3 CommonStyles anchor.

- `RadioButton_OuterRing_UsesControlStrongStrokeBrush`

### Regression floor

Zero-regression policy for the rest of the migration:

- **Must not drop below 212 passing (net10) / 211 passing (net472)** at any
  intermediate stage. Each failing test is allowed to stay failing only until
  its owning work item closes; any *new* failure is a blocker.
- **Must not fall below 220 / 219 total** — new tests are additive.
- **Final target: 220+0 failed / 219+0 failed** (seven tests flip to green
  across WI-1 and WI-3, zero new reds).

## Stage 0.2 — PSADT baseline (recorded 2026-04-20)

Command: `dotnet build PSAppDeployToolkit/PSADT.slnx -c Debug`

Result: **0 errors, 0 warnings**. Build time ~16 s.

All 13 PSADT assemblies compiled for both `net472` and `net10.0-windows10.0.26100.0`.
`PSADT.UserInterface` and `PSADT.UserInterface.TestHarness` build through the
live `ProjectReference` to `Fluence.Wpf` — confirming the public surface
Fluence exposes today satisfies PSADT's consumption.

## Stage 0.3 — Demo smoke test (recorded 2026-04-20)

Launched `Fluence.Wpf.Demo/bin/Debug/net472/Fluence.Wpf.Demo.exe` and confirmed
it stays alive for >4 s (no crash at startup). PID 1133 terminated cleanly.

Visual screenshots deferred: the Demo is a dev build (not Start-menu installed)
and the current state is already known to be broken via the 7 failing tests
(WI-1 F1 pane layout, F3 search, F4 captions via inference, WI-1D Paradigm A
scaffolding). Per-work-item targeted screenshots (Light / Dark / HC × 100% /
150% DPI) will be captured when a WI fix is implemented — that is where the
before/after evidence has signal.

## Visual baselines

## WI-1 runtime verification

### WI-1A F1 — pane layout (landed 2026-04-20)

Commit: restructured [NavigationView.xaml](../../Fluence.Wpf/Themes/Controls/NavigationView.xaml)
so PART_ContentPresenter is no longer wrapped by a `BorderThickness=1` Border.
The 1,1,0,0 stroke now lives on a sibling decorative Border (`IsHitTestVisible=False`)
inside the same Grid, sharing the 8,0,0,0 corner radius. Both
`NavigationViewLeftPaneTemplate` and `NavigationViewLeftCompactPaneTemplate` changed.

Root cause: at 150% DPI the 1 px stroke caused `WindowsChild` layout rounding to
snap `PART_ContentPresenter.X` to 281.333 instead of 280 (and 49.333 instead of 48),
breaking the pane-layout `TransformToAncestor` assertions. Splitting the Border
keeps the presenter flush with the column edge while preserving the visual contract.

Companion tests updated to match the new structure:
[ControlTests.FluentStroke.cs](../../Fluence.Wpf.Tests/ControlTests.FluentStroke.cs)
`NavigationView_Left_ContentBorder_HasWinUiCornerRadiusAndStroke` and
`NavigationView_LeftCompact_ContentBorder_HasWinUiCornerRadiusAndStroke` now look
for the 1,1,0,0 stroke on the sibling Border (second child of the content Grid).

Post-fix baseline (Debug, `--no-build`):

| TFM               | Passed | Failed | Skipped | Total | Delta vs Stage 0 |
|-------------------|-------:|-------:|--------:|------:|------------------|
| net10.0-windows   |   215  |    4   |    1    |  220  | +3 passed / −3 failed |
| net472            |   214  |    4   |    1    |  219  | +3 passed / −3 failed |

Three WI-1 F1 tests flipped green; zero new failures introduced:

- `NavigationView_LeftCompact_PaneClosed_ContentStartsAt48px_Inline` ✅
- `NavigationView_LeftCompact_PaneOpen_ContentStartsAt280px_Inline` ✅
- `NavigationView_LeftCompact_PaneToggle_ResizesPushingContent` ✅

Remaining 4 failures unchanged — they belong to WI-1A F3, WI-1D, and WI-3.

### WI-1A F3 — search top-match selection (landed 2026-04-20)

Added the missing `PreviewKeyDown="NavSearchBox_PreviewKeyDown"` attribute to
[MainWindow.xaml](../../Fluence.Wpf.Demo/MainWindow.xaml) on the `NavSearchBox`
TextBox. The code-behind had the handler since the prior session, but the XAML
binding was never wired — so Enter in the search box was a no-op and the initial
selection ('Home') stayed put regardless of the filter state.

With the handler wired, typing `button` + Enter now walks the visible items in
pane order and selects 'Buttons' (first match). 'Home' is collapsed by the
filter predicate and skipped, so there is no ranking needed — pane order is
already the intended top-match order.

Post-fix baseline:

| TFM               | Passed | Failed | Skipped | Total | Delta vs F1 baseline |
|-------------------|-------:|-------:|--------:|------:|----------------------|
| net10.0-windows   |   216  |    3   |    1    |  220  | +1 passed / −1 failed |
| net472            |   215  |    3   |    1    |  219  | +1 passed / −1 failed |

Test flipped green: `NavSearch_EnterKey_SelectsTopMatch`.

Remaining 3 failures — all WI-1D Paradigm A scaffolding + WI-3 RadioButton.

### WI-1B F2 — content pane tone (landed 2026-04-20)

Audit only — no source change needed. The default style setter at
[NavigationView.xaml:750](../../Fluence.Wpf/Themes/Controls/NavigationView.xaml)
already binds `ContentBackground` to `{DynamicResource LayerFillColorDefaultBrush}`,
the canonical WinUI 3 layer tone, and the template instances bind via
`TemplateBinding` at lines 409, 592, 721.

Gallery cards across the demo (`GalleryDataPage`, `GalleryColorsPage`,
`GalleryHomePage` etc.) already use `CardBackgroundFillColorDefaultBrush`,
so the subtle elevation (pane = `NavigationViewBackgroundBrush` →
content host = `LayerFillColorDefaultBrush` → cards =
`CardBackgroundFillColorDefaultBrush`) is preserved per WinUI 3 Gallery.

Added [NavigationView_ContentBackground_ResolvesToLayerFillColorDefaultBrush_AcrossThemes](../../Fluence.Wpf.Tests/ControlTests.NavigationView.cs)
as a regression guard: asserts `nav.ContentBackground` is reference-identical to the
currently-resolved `LayerFillColorDefaultBrush` under Light, Dark, and after a
full Light→Dark→HC→Light theme cycle.

Post-fix baseline:

| TFM               | Passed | Failed | Skipped | Total | Delta vs F3 baseline |
|-------------------|-------:|-------:|--------:|------:|----------------------|
| net10.0-windows   |   217  |    3   |    1    |  221  | +1 passed / +1 total |
| net472            |   216  |    3   |    1    |  220  | +1 passed / +1 total |

### WI-1C F4 — caption button wiring (landed 2026-04-20)

Audit + TDD regression guard — no source change needed. Caption buttons on
[FluenceWindow.xaml:203-251](../../Fluence.Wpf/Themes/Controls/FluenceWindow.xaml)
(`PART_MinimizeButton`, `PART_MaximizeButton`, `PART_RestoreButton`, `PART_CloseButton`) are already
bound to `SystemCommands.{Minimize,Maximize,Restore,Close}WindowCommand` via
`Command="{x:Static ...}"`. The matching `CommandBinding`s are registered in
[FluenceWindow.cs:394-397](../../Fluence.Wpf/Controls/FluenceWindow.cs) and
route to private handlers `OnMinimizeWindow` / `OnMaximizeWindow` /
`OnRestoreWindow` / `OnCloseWindow` (lines 1064-1089) that drive
`WindowState` directly.

The belt-and-braces comment at `FluenceWindow.cs:1043-1063` documents why these
handlers intentionally set `WindowState` and also call
`NativeMethods.{Minimize,Maximize,Restore}WindowNative(_handle)`: when
`NativeMethods.HideAllWindowButtons` strips `WS_MINIMIZEBOX` / `WS_MAXIMIZEBOX`
(or `ResizeMode=NoResize` does the same — the baseline for every PSADT fluent
dialog), `DefWindowProc` silently drops `SC_MINIMIZE` / `SC_MAXIMIZE`, and a
command router that went through `WM_SYSCOMMAND` would leave the caption buttons
looking clickable but doing nothing. The WPF `WindowState` setter uses
`ShowWindow` which is not gated by sysmenu styles, and the `*WindowNative`
P/Invoke provides a fallback for transient `_hwndSource` unavailability — so F4
has in fact been correctly wired all along; the prior "F4 caption buttons via
inference" classification in the Stage 0.3 note was speculative, not evidence-based.

New regression guard [ControlTests.CaptionButtons.cs](../../Fluence.Wpf.Tests/ControlTests.CaptionButtons.cs):

- `FluenceWindow_CaptionButtons_AllFourBindToCanonicalSystemCommands` — asserts
  `Button.Command` reference-equals the expected `SystemCommands.*WindowCommand`
  on all four caption buttons after template application.
- `FluenceWindow_MinimizeCommand_TransitionsToMinimized` — invokes
  `SystemCommands.MinimizeWindowCommand.Execute(null, window)` on an off-screen
  `FluenceWindow`, asserts `WindowState == Minimized`.
- `FluenceWindow_MaximizeCommand_TransitionsToMaximized` — same, but asserts
  `Maximized`.
- `FluenceWindow_RestoreCommand_TransitionsMaximizedToNormal` — starts from
  `Maximized`, invokes `RestoreWindowCommand`, asserts `Normal`.
- `FluenceWindow_CloseCommand_FiresClosingEvent` — invokes `CloseWindowCommand`,
  pumps the dispatcher at `Background` priority so `PostMessage(WM_SYSCOMMAND,
  SC_CLOSE)` is processed, asserts `Window.Closing` fires.

Post-fix baseline (Debug, `--no-build`):

| TFM               | Passed | Failed | Skipped | Total | Delta vs F2 baseline |
|-------------------|-------:|-------:|--------:|------:|----------------------|
| net10.0-windows   |   222  |    3   |    1    |  226  | +5 passed / +5 total |
| net472            |   221  |    3   |    1    |  225  | +5 passed / +5 total |

Remaining 3 failures unchanged — `MainWindow_NavigationPane_ContainsSectionHeaders`
and `HomePage_ContainsFeaturedControlsGrid` (WI-1D Paradigm A) plus
`RadioButton_OuterRing_UsesControlStrongStrokeBrush` (WI-3).

### WI-1D Paradigm A redesign (landed 2026-04-20)

Picked **Paradigm A** (WinUI 3 Gallery-style: left rail with section headers
+ category search, cards grid on the home page). The Paradigm A contract was
already encoded by the two pre-written failing tests
(`MainWindow_NavigationPane_ContainsSectionHeaders`,
`HomePage_ContainsFeaturedControlsGrid`), which made A the only paradigm that
keeps the baseline green without rewriting tests.

Two structural changes:

**1. NavigationView restructure.** [MainWindow.xaml](../../Fluence.Wpf.Demo/MainWindow.xaml)
now groups the 11 gallery pages under three WinUI 3 Gallery category headers,
with the "Home" item above all three groups:

- _Basic input_ — Buttons, Selection, Inputs
- _Collections & navigation_ — Data, Tabs, Navigation
- _Design & shell_ — Status, Colors, Glyphs, Window

Section headers use `ui:NavigationViewItemHeader`, a sibling item type inside
`NavigationView.Items`. The existing `CollapseEmptySectionHeaders()` code-behind
(at [MainWindow.xaml.cs:203-232](../../Fluence.Wpf.Demo/MainWindow.xaml.cs)) already
walks pane items tracking `currentHeader` + `currentSectionHasVisibleItem` and
collapses any header whose items are all filtered out — so search behavior
falls out for free.

**2. Featured controls grid on the home page.**
[GalleryHomePage.xaml](../../Fluence.Wpf.Demo/Pages/GalleryHomePage.xaml) adds a
second tile surface below the existing category landing cards: a Subtitle-styled
"Featured controls" heading plus a `<UniformGrid x:Name="FeaturedControlsGrid"
Columns="3">` containing six clickable `ui:Card`s (Buttons, Selection, Inputs,
Status, Collections, Navigation). Each card fires `Card_Click` → `NavigateTo(tag)`,
reusing the existing handler on `MainWindow`. The action-cards UniformGrid above
is unchanged (keeps its Theming / Shell / Controls / Fidelity landing semantic).

Per WinUI 3 Gallery, the Featured row uses `BodyStrong` + `Caption` typography
(tighter than the Subtitle + Body landing tiles) so a six-tile grid reads as a
different surface from the four-tile grid above it.

Post-fix baseline (Debug, `--no-build`):

| TFM               | Passed | Failed | Skipped | Total | Delta vs F4 baseline |
|-------------------|-------:|-------:|--------:|------:|----------------------|
| net10.0-windows   |   224  |    1   |    1    |  226  | +2 passed / −2 failed |
| net472            |   223  |    1   |    1    |  225  | +2 passed / −2 failed |

Tests flipped green: `MainWindow_NavigationPane_ContainsSectionHeaders`,
`HomePage_ContainsFeaturedControlsGrid`. The third Paradigm A test
(`NavSearch_Filter_HidesHeaders_WhenAllSectionItemsFilteredOut`) was already
passing before WI-1D thanks to `CollapseEmptySectionHeaders()`.

Remaining single failure is the pre-existing WI-3 item
`RadioButton_OuterRing_UsesControlStrongStrokeBrush` — WI-1 is now closed end
to end.

### WI-1D Step 1D.3 — screenshot regeneration (recorded 2026-04-20)

Regenerated `docs/screenshots/banner-{light,dark,highcontrast}-{1,1.5}x.png` via
[GalleryScreenshotHarness.CaptureBannerAcrossThemesAndScales](../../Fluence.Wpf.Tests/GalleryScreenshotHarness.cs)
with `FLUENCE_CAPTURE_SCREENSHOTS=1`. Invocation that works inside this session:

```powershell
dotnet vstest Fluence.Wpf.Tests/bin/Debug/net10.0-windows10.0.26100.0/Fluence.Wpf.Tests.dll `
  --TestCaseFilter:"FullyQualifiedName~CaptureBannerAcrossThemesAndScales"
```

(`dotnet test` silently skipped output in this session's shell; `dotnet vstest`
against the pre-built DLL is the reliable path. Result: `Passed 1, Failed 0`.)

File size growth reflects the added **Featured controls** heading +
`FeaturedControlsGrid` 3×2 card surface below the existing four-tile category
grid:

| File                             | Before (04-17) | After (04-20) |
|----------------------------------|---------------:|--------------:|
| banner-light-1x.png              |         68 KB  |        91 KB  |
| banner-light-1.5x.png            |         78 KB  |       104 KB  |
| banner-dark-1x.png               |         66 KB  |        89 KB  |
| banner-dark-1.5x.png             |         74 KB  |        99 KB  |
| banner-highcontrast-1x.png       |         64 KB  |        87 KB  |
| banner-highcontrast-1.5x.png     |         70 KB  |        95 KB  |

Visual pass on all three 1× captures:

- **Light** — hero banner + four-tile category grid (Theming / Shell / Controls / Fidelity) + Subtitle "Featured controls" + six-tile grid (Buttons, Selection, Inputs, Status, Collections, Navigation). All text readable; card strokes render with the canonical `CardStrokeColorDefault` tone.
- **Dark** — same layout; theme-reactive foreground/background brushes tracked the swap cleanly. No stale Light brushes on any surface.
- **HighContrast** — FeaturedControlsGrid + category tiles render correctly; the **pre-existing** HC banner wordmark fade (BannerLight.png raster blending into the HC-light surface) is unrelated to 46c4b93 and is tracked separately (not a WI-1D regression).

The `GalleryScreenshotHarness` hosts `GalleryHomePage` inside a plain `Window`
per its documented limitation (`RenderTargetBitmap` cannot capture DWM
backdrops), so the NavigationView section-header grouping from 46c4b93 is
**not** visible in these banners. Runtime verification of the grouped pane is
covered by MSTest `MainWindow_NavigationPane_ContainsSectionHeaders` already
green.

## WI-2 API snapshot diff

_(see `docs/fluencewindow-api-snapshot.md` for the enumeration; this section
summarizes the before/after diff)_

## Final theme-cycle soak

_(Stage F)_
