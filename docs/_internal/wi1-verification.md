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

## Stage 0.2 — PSADT baseline

_(recorded after WI-4 spot-check)_

## Visual baselines

_(captured at Stage 0.3)_

## WI-1 runtime verification

_(filled by WI-1A / WI-1B / WI-1C / WI-1D)_

## WI-2 API snapshot diff

_(see `docs/fluencewindow-api-snapshot.md` for the enumeration; this section
summarizes the before/after diff)_

## Final theme-cycle soak

_(Stage F)_
