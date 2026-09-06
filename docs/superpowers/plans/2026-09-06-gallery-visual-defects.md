# Gallery Visual Defects Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the ten owner-reported Dark and Light theme parity defects in the Fluence.Wpf gallery, from the shared popup elevation bug through to the Colors and Icons page restructures, without touching the frozen 1.0 public surface.

**Architecture:** One branch, `fix/gallery-visual-defects`, cut at `6394606`. Eight tasks, one commit each. The popup family is one shared presenter fix plus one audit that applies the same mechanism to the other eight popups; then four control fixes; then the two gallery page restructures, which are the largest and go last. Research, including the four probes that proved the popup mechanism, is in the SDD workspace named below and is not committed.

**Tech Stack:** C# on `net472` / `net8.0-windows10.0.26100.0` / `net10.0-windows10.0.26100.0`, WPF, xunit.v3 4.0.0 under Microsoft Testing Platform, MSBuild with central package management and lock files, GitHub Actions.

**Spec:** [docs/superpowers/specs/2026-09-06-gallery-visual-defects-design.md](../specs/2026-09-06-gallery-visual-defects-design.md)

**Research:** `.superpowers/sdd/2026-09-06-gallery-visual-defects/research.md` (git-ignored). Read the section for your defect before starting a task: three of the spec's ten diagnoses were falsified by measurement, and the corrected diagnosis is what these tasks implement.

## Global Constraints

Every task's requirements implicitly include this section.

### Staging discipline

The tree is clean at the start of the plan; the owner's uncommitted experiments in `Theme.Dark.xaml` and `ScrollBar.xaml` were discarded by the owner before execution, so both files may be edited like any other. `git add` by name only, never `git add -A` or `git add .`, so nothing unrelated is swept into a commit.

### Branch and git preamble

Every task starts with these plain single git commands, run from the worktree root:

```
git branch --show-current
git log --oneline -1
git status --short
```

- The branch must print `fix/gallery-visual-defects`. If it prints anything else, stop and report.
- Verify the `git log --oneline -1` subject matches the previous task's commit before doing any work. If it does not, stop and report.
- `git status --short` must print nothing. If it shows anything, stop and report.
- No `git -C`, no compound shell lines mentioning git, no loops around git, no variables inside git commands. One plain invocation per command.
- Never push. Never `git stash`. Never rewrite history. Never `git checkout --` a file.
- Commits are the owner's: no `Co-Authored-By`, no `Claude-Session`, no trailer of any kind. Imperative subject line ending with a period, body wrapped at 80 columns explaining what and why and citing the WinUI reference, no em or en dashes.

### Frozen public surface

The 1.0 public surface is frozen. `PublicAPI.Unshipped.txt` (per target framework) and
`Fluence.Wpf.Tests/Theming/golden/PublicKeys.txt` must come out of this branch byte-identical.

- No new public CLR member, type, or enum value on any library type.
- **No new XAML resource key.** `ThemeParityTests.PublicKeyInventory_MatchesFrozenSetAsync`
  (`Fluence.Wpf.Tests/Theming/ThemeParityTests.cs:517`) walks every key in the published dictionary and
  fails on anything not in the frozen list. This is why the popup elevation gutter is a literal `16` in
  the templates rather than a `FlyoutShadowGutter` resource.
- No new library control. Demo pages build from existing controls or demo-local classes only.
- If a fix genuinely cannot be made without changing either file, stop and report rather than changing
  it.

### Build, analyzer and text policy (from AGENTS.md)

- `TreatWarningsAsErrors=True`, `WarningLevel=9999`, `AnalysisLevel=latest-all`,
  `EnforceCodeStyleInBuild=true`. Meziantou (`all-errors`), Roslynator, SonarAnalyzer and
  BannedApiAnalyzers all run as errors. **Fix root causes. Never suppress, never edit `.editorconfig`,
  never add `#pragma`.** If a diagnostic cannot be fixed without weakening a rule, stop and report.
- Common analyzer traps: discard unused return values with `_ =` (IDE0058, CA1806); explicit types, not
  `var`; target-typed `new()`; `is not null` patterns; `default` not `default(T)`; no redundant `using`;
  `string.IsNullOrEmpty` is banned (RS0030), use `string.IsNullOrWhiteSpace`; on `net472`
  `IsNullOrWhiteSpace` carries no `NotNullWhen`, so write
  `x is not null && !string.IsNullOrWhiteSpace(x)`; wrap Win32 bit-mask arithmetic in `unchecked`; a
  fully qualified name that can be simplified fails IDE0001 under `dotnet format`.
- Every new `.cs` file starts with the 27-line BSD 3-Clause header copied **verbatim** from
  `Fluence.Wpf.Tests/Control/Rules/PopupCornerRadiusTests.cs` lines 1 to 27. Do not retype it, do not
  change the year.
- Public API needs `///` XML documentation on every member. The library builds with
  `<DocumentationFile>` and suppresses neither `CS1591` nor `CS1574`.
- All `.cs`, `.xaml`, `.md`, `.csproj`, `.props`, `.yml` files: **UTF-8 with BOM, LF line endings, final
  newline, no trailing whitespace.** No em dash or en dash anywhere in `.cs` or `.md`; use a comma, a
  colon, or a new sentence, and do not substitute `" - "` where it would create a spurious Markdown list
  item, reflow the sentence instead.
- **No hard-coded hex colors under `Fluence.Wpf/Themes/Controls/**`.** Theme brushes are consumed with
  `DynamicResource` under canonical WinUI key names. Literal sizes, margins and offsets are fine and are
  already used throughout those templates.
- `net472` feasibility: `LangVersion=latest` compiles modern C# everywhere, but a runtime API absent
  from `net472` fails the separate `net472` lane. Verify before using one; do not guard with
  `#if NET10_0_OR_GREATER` to reach a newer BCL API.
- The redundant-publish gate: `ApplicationThemeManager.Apply` skips the rebuild when the resolved theme,
  the whole color map, the live `SystemColors` members `SpecialBrushes` reads, and the Settings
  transparency flag are all unchanged. A test that must observe `Changed` or `AccentColorChanged` has to
  make the apply a genuine transition.
- Do not subscribe a static manager (`ApplicationThemeManager.Changed`,
  `ApplicationAccentColorManager.AccentColorChanged`) from a constructor. Subscribe in `Loaded` and
  unsubscribe in `Unloaded`, or the page is pinned to the invocation list forever.

### Reference authority

Every visual or behavioural decision cites, in order: in-tree precedent, then the per-domain authority
(WinUI 3 CommonStyles at `F:\Consolidation\WInUI\controls\dev` for tokens and control visuals, the WinUI
Gallery at `F:\Consolidation\WinUI-Gallery\WinUIGallery` for gallery page layout, .NET 10 WPF Themes for
WPF-native window chrome), then published Windows 11 guidance as a tie-breaker. Each task below names
its reference with file and line. Reproduce that citation in the commit body.

### Test invocation

xunit.v3 under Microsoft Testing Platform. **Do not use `dotnet test`.** Run the built executable, one
target framework at a time, in the foreground:

```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class <FullName> --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --filter-class <FullName> --no-ansi --progress off
```

- `--filter-class` takes several space-separated class names after one flag.
- **Both target frameworks must pass for every task that touches code.**
- Never run the whole suite in the foreground: it overruns the tool timeout. Run the targeted filters
  each task names. On `net472` a single-process whole-assembly run also aborts non-deterministically
  (`KNOWN_ISSUES.md`).
- The known flaky animation-timing tests (ToggleSwitch pressed scale, BreadcrumbBar press scale,
  NavigationView pane width) pass in isolation and must not be "fixed".
- The regression floor is the HEAD-of-branch pass count. Add tests; never weaken the baseline.

### Test-count bookkeeping for any added or removed test

A task that adds or removes a test case must, in the same commit:

1. Regenerate all four baselines under `Fluence.Wpf.Tests/Baselines/`: `baseline.net10.txt`,
   `baseline.net472.txt`, `baseline.net10.methods.txt`, `baseline.net472.methods.txt`. Capture with
   `--list-tests` per target framework. Diff by method name, not by fully qualified name.
2. Update the CI lane sum-check constants at `.github/workflows/build.yml:88`, currently
   `$expected = @{ 'net472' = 1204; 'net10' = 1205 }`. Add the number of cases the task added to both
   values. The two differ by one because of a screenshot case, so the delta applied to each is the same.
3. Add a line under `## [Unreleased]` in `CHANGELOG.md` saying what changed and why.
4. Normalise the baseline files to LF with a UTF-8 BOM before committing.

### Per-task closing gate

Every task ends with, in this order:

```
dotnet build Fluence.Wpf.sln -c Debug
dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore
pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll
```

`dotnet build` must report `0 Warning(s)` and `0 Error(s)`. The `-NoProfile` on the text policy gate
matters: it hangs without it.

The text policy gate reports zero issues at the plan's starting commit (the design note's missing BOM
was fixed in the plan correction commit). Any issue it reports during a task is that task's to fix.

That gate scans tracked files only, so byte-check any new file yourself; the first three bytes must be
`EF BB BF`:

```
pwsh -NoProfile -Command "[System.IO.File]::ReadAllBytes('<path>')[0..2]"
```

### CHANGELOG

Every task adds at least one bullet under `## [Unreleased]` in `CHANGELOG.md`. The section is currently
empty; the first task that touches it adds a `### Fixed` heading under it. Use `### Fixed` for the
defect fixes and `### Changed` for the demo page restructures.

### Visual verification

The machine is in active use and the screen may lock. Any capture that comes back black or shows a lock
screen is a failed capture to redo, not evidence. Do not kill processes you did not start. If the demo
executable is already running from this worktree and the build cannot copy over it, report that and work
from source rather than terminating it.

Run the demo with:

```
dotnet run --project Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -f net10.0-windows10.0.26100.0
```

Save before and after captures under
`.superpowers/sdd/2026-09-06-gallery-visual-defects/captures/` (git-ignored, create it on first use).
This display is 10 bpc and quantises translucent layers, so compare relative structure and ordering, not
absolute translucent pixel values (`KNOWN_ISSUES.md`).

---

### Task 1: Popup elevation gutter for Flyout, ToolTip, TeachingTip and CommandBarFlyout

Defects 1 to 4. Read `.superpowers/sdd/2026-09-06-gallery-visual-defects/research.md`, section
"Defects 1 to 4", before starting. The short version: WPF sizes a popup HWND to exactly its child's
layout size, so the `DropShadowEffect` on the `ShadowCaster` is clipped away and survives only in the
four rounded-corner notches, where it reads as a dark square plate behind a rounded card. The comment
repeated in twelve places claiming "PopupRoot already sizes a popup window about 15 px larger per side
than its child" is false and must be corrected wherever it is touched.

**Files:**
- Modify: `Fluence.Wpf/Themes/Controls/FlyoutPresenter.xaml:16-19,37`
- Modify: `Fluence.Wpf/Themes/Controls/ToolTip.xaml:29-37,41,43-47`
- Modify: `Fluence.Wpf/Themes/Controls/TeachingTip.xaml:29-31,89,105-108`
- Modify: `Fluence.Wpf/Themes/Controls/CommandBarFlyout.xaml:13-17,258,259-263`
- Modify: `Fluence.Wpf/Controls/FlyoutBase.cs:288-303`
- Modify: `Fluence.Wpf/Controls/TeachingTip.cs:745-751`
- Create: `Fluence.Wpf.Tests/Control/Rules/PopupElevationTests.cs`
- Modify: `Fluence.Wpf.Tests/Baselines/baseline.net10.txt`,
  `Fluence.Wpf.Tests/Baselines/baseline.net472.txt`,
  `Fluence.Wpf.Tests/Baselines/baseline.net10.methods.txt`,
  `Fluence.Wpf.Tests/Baselines/baseline.net472.methods.txt`
- Modify: `.github/workflows/build.yml:88`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Produces: `private const double FlyoutBase.ShadowGutter = 16.0`, the single source of truth for the
  gutter on the code side. Task 2 reuses the same literal `16` in the eight remaining popup templates
  and must not introduce a second constant or a resource key.
- Produces: the corrected `FlyoutBase.GetEdgeCenteredPlacements(PlacementMode side, Size popupSize,
  Size targetSize, Point offset)` signature is unchanged; only the body changes.
  `TeachingTip.GetEdgePlacements` already delegates to it, so the targeted TeachingTip path is fixed for
  free.
- Produces: `Fluence.Wpf.Tests.Control.Rules.PopupElevationTests`, the home for popup elevation
  assertions. Task 2 adds cases to this same class rather than creating a second one.

- [ ] **Step 1: Write the failing test**

Create `Fluence.Wpf.Tests/Control/Rules/PopupElevationTests.cs`. Start the file with the 27-line BSD
header copied verbatim from `Fluence.Wpf.Tests/Control/Rules/PopupCornerRadiusTests.cs` lines 1 to 27,
then:

```csharp
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control.Rules
{
    /// <summary>
    /// Popup elevation rules. WPF sizes a popup HWND to exactly its child's layout size, with no
    /// gutter of any kind: PopupRoot, its Decorator and its AdornerDecorator all report the child's
    /// size, with no margin and no clip. A <see cref="DropShadowEffect"/> on a child that fills the
    /// popup is therefore clipped away entirely and survives only in the rounded-corner notches,
    /// where it reads as a dark square plate behind a rounded card. Every popup presenter reserves a
    /// 16px transparent margin so the effect has somewhere to render, and every host popup subtracts
    /// the same 16px from its offset so the plate lands where it did before. These tests pin both
    /// halves, because it is the layout gutter, not the effect, that was missing.
    /// </summary>
    public sealed class PopupElevationTests : IClassFixture<LightThemeFixture>
    {
        private const double ShadowGutter = 16.0;

        public PopupElevationTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public Task FlyoutPresenter_TemplateRoot_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                FlyoutPresenter presenter = new() { Content = "Quick note" };
                Window window = new() { Content = presenter, Width = 320, Height = 200 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Grid root = Assert.IsType<Grid>(FindVisualChild<Grid>(presenter), exactMatch: false);

                    Assert.Equal(new Thickness(ShadowGutter), root.Margin);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task FlyoutPresenter_ShadowCaster_CarriesTheFlyoutShadowEffectAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                FlyoutPresenter presenter = new() { Content = "Quick note" };
                Window window = new() { Content = presenter, Width = 320, Height = 200 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border caster = Assert.IsType<Border>(FindVisualChildByName<Border>(presenter, "ShadowCaster"), exactMatch: false);

                    _ = Assert.IsType<DropShadowEffect>(caster.Effect);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task FlyoutPresenter_DesiredSize_ExceedsThePlateByTwiceTheGutterAsync()
        {
            // This is the assertion that would have caught the original defect: the effect was
            // present all along, the layout gutter was not.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                FlyoutPresenter presenter = new() { Content = "Quick note" };
                Window window = new() { Content = presenter, Width = 320, Height = 200 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border surface = Assert.IsType<Border>(FindVisualChildByName<Border>(presenter, "PresenterSurface"), exactMatch: false);

                    Assert.Equal(surface.ActualWidth + (2 * ShadowGutter), presenter.ActualWidth, 0.01);
                    Assert.Equal(surface.ActualHeight + (2 * ShadowGutter), presenter.ActualHeight, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ToolTip_TemplateRoot_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ToolTip toolTip = new() { Content = "Save changes" };
                Window window = new() { Content = toolTip, Width = 320, Height = 200 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Grid root = Assert.IsType<Grid>(FindVisualChild<Grid>(toolTip), exactMatch: false);

                    Assert.Equal(new Thickness(ShadowGutter), root.Margin);
                    Assert.Equal(-ShadowGutter, toolTip.HorizontalOffset, 0.01);
                    Assert.Equal(-ShadowGutter, toolTip.VerticalOffset, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TeachingTip_TipRoot_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TeachingTip tip = new() { Title = "Pro tip", Subtitle = "A TeachingTip points at a target." };
                Window window = new() { Content = tip, Width = 420, Height = 300 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Grid root = Assert.IsType<Grid>(FindVisualChildByName<Grid>(tip, "TipRoot"), exactMatch: false);

                    Assert.Equal(new Thickness(ShadowGutter), root.Margin);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task CommandBarFlyoutPresenter_TemplateRoot_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                CommandBarFlyoutPresenter presenter = new();
                Window window = new() { Content = presenter, Width = 420, Height = 200 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Grid root = Assert.IsType<Grid>(FindVisualChild<Grid>(presenter), exactMatch: false);

                    Assert.Equal(new Thickness(ShadowGutter), root.Margin);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.Rules.PopupElevationTests --no-ansi --progress off
```

Expected: five of six fail on `Assert.Equal(Thickness(16), ...)` reporting `0,0,0,0`, and
`FlyoutPresenter_DesiredSize_ExceedsThePlateByTwiceTheGutterAsync` fails because the presenter and the
surface are the same size. `FlyoutPresenter_ShadowCaster_CarriesTheFlyoutShadowEffectAsync` passes
already; that is correct, the effect was never the problem.

- [ ] **Step 3: Add the gutter to `FlyoutPresenter.xaml`**

In `Fluence.Wpf/Themes/Controls/FlyoutPresenter.xaml`, replace the last three lines of the header
comment (lines 16 to 19, the sentence beginning "Elevation is cast by an empty ShadowCaster sibling")
with:

```
        Elevation is cast by an empty ShadowCaster sibling painted behind the surface, carrying
        FlyoutShadowEffect. WPF drops ClearType for every text run under an Effect, which is why the
        effect is not on the surface itself. The template root carries a 16px transparent gutter
        because WPF sizes a popup HWND to exactly its child's layout size, with no reserved space at
        all: without the gutter the effect is clipped away and survives only in the four rounded
        corner notches, where it reads as a dark square plate behind a rounded card. 16px covers the
        effect's full bleed (BlurRadius 18 gives 9px sideways and up, plus ShadowDepth 4 downward).
        FlyoutBase.ShadowGutter subtracts the same 16px from every placement candidate so the plate
        lands where it did before. Keep the two numbers in step.
```

Then change line 37 from `<Grid>` to:

```xml
                    <Grid Margin="16">
```

- [ ] **Step 4: Add the gutter to `ToolTip.xaml`**

In `Fluence.Wpf/Themes/Controls/ToolTip.xaml`, change line 41 from `<Grid>` to `<Grid Margin="16">`, and
add two setters to the style so the WPF-internal tooltip popup absorbs the gutter.
`System.Windows.Controls.ToolTip` exposes `HorizontalOffset` and `VerticalOffset` and forwards both to
the popup it creates internally, which is the only handle this control gives us. Insert them in
alphabetical order among the existing setters, that is immediately after the
`<Setter Property="Foreground" ... />` line at line 35:

```xml
        <Setter Property="HorizontalOffset" Value="-16" />
```

and immediately after the `<Setter Property="Template">` block's closing `</Setter>`:

```xml
        <Setter Property="VerticalOffset" Value="-16" />
```

Replace the comment at lines 43 to 47 (the block beginning "Elevation caster: an empty sibling behind
the surface") with:

```
                            Elevation caster: an empty sibling behind the surface, because WPF
                            drops ClearType for text under an Effect and the surface opts into it.
                            The root's 16px margin is the popup's shadow gutter: WPF sizes a popup
                            HWND to exactly its child's layout size, so without it the effect is
                            clipped to the plate and only the corner notches survive. The style's
                            HorizontalOffset and VerticalOffset of -16 pull the tooltip back so the
                            plate keeps its position.
```

- [ ] **Step 5: Add the gutter to `TeachingTip.xaml` and `CommandBarFlyout.xaml`**

`Fluence.Wpf/Themes/Controls/TeachingTip.xaml`, line 89:

```xml
                    <Grid x:Name="TipRoot" Margin="16" UseLayoutRounding="True">
```

`Fluence.Wpf/Themes/Controls/CommandBarFlyout.xaml`, line 258:

```xml
                    <Grid Margin="16">
```

In both files, replace the false sentence "no layout gutter is needed because PopupRoot already sizes a
popup window about 15 px larger per side than its child" (and the "No layout gutter is needed, PopupRoot
already reserves about 15 px per side for popup shadows" variant) with:

```
        the template root carries a 16px transparent gutter, because WPF sizes a popup HWND to
        exactly its child's layout size and the effect would otherwise be clipped to the plate
```

- [ ] **Step 6: Subtract the gutter in `FlyoutBase.cs`**

In `Fluence.Wpf/Controls/FlyoutBase.cs`, add the constant next to the other private fields and replace
`GetEdgeCenteredPlacements` (lines 288 to 303) with:

```csharp
        /// <summary>
        /// The transparent margin every flyout presenter template reserves on all four sides so its
        /// ShadowCaster's DropShadowEffect has somewhere to render. WPF sizes a popup HWND to exactly
        /// its child's layout size, so without the gutter the effect is clipped to the plate and only
        /// the rounded corner notches survive. Placement subtracts it again so the plate lands where
        /// it did before. Keep in step with the Margin in the presenter templates.
        /// </summary>
        private const double ShadowGutter = 16.0;

        internal static CustomPopupPlacement[] GetEdgeCenteredPlacements(
            PlacementMode side,
            Size popupSize,
            Size targetSize,
            Point offset)
        {
            // popupSize includes the gutter on all four sides, so the plate is inset by ShadowGutter
            // inside it. Center on the plate rather than on the popup, and pull every candidate back
            // by the gutter on the axis it docks to.
            double plateWidth = popupSize.Width - (2 * ShadowGutter);
            double plateHeight = popupSize.Height - (2 * ShadowGutter);
            double centeredX = ((targetSize.Width - plateWidth) / 2.0) + offset.X - ShadowGutter;
            double centeredY = ((targetSize.Height - plateHeight) / 2.0) + offset.Y - ShadowGutter;
            CustomPopupPlacement above = new(new Point(centeredX, -popupSize.Height + offset.Y + ShadowGutter), PopupPrimaryAxis.Horizontal);
            CustomPopupPlacement below = new(new Point(centeredX, targetSize.Height + offset.Y - ShadowGutter), PopupPrimaryAxis.Horizontal);
            CustomPopupPlacement leftOf = new(new Point(-popupSize.Width + offset.X + ShadowGutter, centeredY), PopupPrimaryAxis.Vertical);
            CustomPopupPlacement rightOf = new(new Point(targetSize.Width + offset.X - ShadowGutter, centeredY), PopupPrimaryAxis.Vertical);
            return side is PlacementMode.Top
                ? [above, below]
                : side is PlacementMode.Left
                    ? [leftOf, rightOf]
                    : side is PlacementMode.Right ? [rightOf, leftOf] : [below, above];
        }
```

Keep the existing accessibility of the method exactly as it is on disk; `TeachingTip.GetEdgePlacements`
calls it, so it stays visible to that call site and the targeted TeachingTip path is fixed for free.

- [ ] **Step 7: Subtract the gutter in `TeachingTip.cs`**

`TeachingTip` reaches its popup three ways. `PlacementMode.Center` needs no change, because the gutter is
symmetric and a symmetric grow keeps the plate centred. `GetEdgePlacements` delegates to the method Step
6 fixed. Only the untargeted bottom-right dock needs its own adjustment. Replace
`GetBottomRightPlacements` (lines 745 to 751) with:

```csharp
        private static CustomPopupPlacement[] GetBottomRightPlacements(Size popupSize, Size targetSize, Point offset)
        {
            // popupSize includes the presenter's 16px shadow gutter on all four sides, so add it back
            // on both axes to keep the plate's bottom-right corner docked where it was.
            const double shadowGutter = 16.0;
            Point bottomRight = new(
                targetSize.Width - popupSize.Width + offset.X + shadowGutter,
                targetSize.Height - popupSize.Height + offset.Y + shadowGutter);
            return [new CustomPopupPlacement(bottomRight, PopupPrimaryAxis.Horizontal)];
        }
```

- [ ] **Step 8: Run the tests to verify they pass**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.Rules.PopupElevationTests Fluence.Wpf.Tests.Control.Rules.PopupCornerRadiusTests Fluence.Wpf.Tests.Control.FlyoutTests Fluence.Wpf.Tests.Control.ToolTipTests Fluence.Wpf.Tests.Control.TeachingTipTests Fluence.Wpf.Tests.Control.CommandBarFlyoutTests --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.Rules.PopupElevationTests Fluence.Wpf.Tests.Control.Rules.PopupCornerRadiusTests Fluence.Wpf.Tests.Control.FlyoutTests Fluence.Wpf.Tests.Control.ToolTipTests Fluence.Wpf.Tests.Control.TeachingTipTests Fluence.Wpf.Tests.Control.CommandBarFlyoutTests --no-ansi --progress off
```

Expected: all pass on both target frameworks. Any existing test in those four control classes that
asserted a template-root margin, a presenter size, or a popup offset will now fail; that is a real
consequence of the fix, so update the assertion and say so in the commit body rather than reverting.

- [ ] **Step 9: Visual verification**

Run the demo, switch to Dark, go to Menus, and exercise `Show flyout`, `Show teaching tip`,
`Show command bar`, and hover `Save` for the tooltip. Save a capture of each under
`.superpowers/sdd/2026-09-06-gallery-visual-defects/captures/`.

What to look for:
- A soft graded dark band below and to the sides of each plate, strongest below.
- **No square-cornered step at the four corners.** That artifact is the whole defect.
- The plate itself has not moved: the flyout still opens directly under the button's left edge, the
  teaching tip's beak still points at its target, the command bar still sits over its anchor.
- The CommandBarFlyout's light 1 px rim now reads as a lit top edge on an elevated card rather than a
  halo. Do not change the border brush: `ControlStrokeColorDefaultBrush` is exactly what WinUI
  specifies (`CommandBarFlyout_themeresources.xaml:7`).
- Click just outside a flyout's plate but inside its 16 px gutter. The flyout must dismiss. If the
  click is swallowed, stop and report: the gutter is capturing hit tests and the fix needs
  `IsHitTestVisible="False"` on the gutter.

Repeat the whole pass in Light.

- [ ] **Step 10: Update baselines, the CI sum-check and the CHANGELOG**

Capture the new test lists and update all four files under `Fluence.Wpf.Tests/Baselines/`:

```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --list-tests --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --list-tests --no-ansi --progress off
```

This task adds six cases, so `.github/workflows/build.yml:88` becomes:

```yaml
          $expected = @{ 'net472' = 1210; 'net10' = 1211 }
```

Verify that arithmetic against the actual `--list-tests` totals rather than trusting it; if the counts
disagree, the totals from the run win and you should say so in the commit body.

Add under `## [Unreleased]` in `CHANGELOG.md`:

```markdown
### Fixed

- Flyout, ToolTip, TeachingTip and CommandBarFlyout now cast a real drop shadow. WPF sizes a popup
  window to exactly its child's layout size, so the shadow effect was clipped away and survived only in
  the rounded corner notches, where it read as a dark square plate behind a rounded card. Each presenter
  now reserves a 16 px transparent gutter and each popup subtracts the same 16 px from its placement.
```

- [ ] **Step 11: Closing gate**

```
dotnet build Fluence.Wpf.sln -c Debug
dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore
pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll
pwsh -NoProfile -Command "[System.IO.File]::ReadAllBytes('Fluence.Wpf.Tests/Control/Rules/PopupElevationTests.cs')[0..2]"
```

The byte check must print `239 187 191`, and the text policy gate must now report zero issues.

- [ ] **Step 12: Commit**

```
git add Fluence.Wpf/Themes/Controls/FlyoutPresenter.xaml
git add Fluence.Wpf/Themes/Controls/ToolTip.xaml
git add Fluence.Wpf/Themes/Controls/TeachingTip.xaml
git add Fluence.Wpf/Themes/Controls/CommandBarFlyout.xaml
git add Fluence.Wpf/Controls/FlyoutBase.cs
git add Fluence.Wpf/Controls/TeachingTip.cs
git add Fluence.Wpf.Tests/Control/Rules/PopupElevationTests.cs
git add Fluence.Wpf.Tests/Baselines/baseline.net10.txt
git add Fluence.Wpf.Tests/Baselines/baseline.net472.txt
git add Fluence.Wpf.Tests/Baselines/baseline.net10.methods.txt
git add Fluence.Wpf.Tests/Baselines/baseline.net472.methods.txt
git add .github/workflows/build.yml
git add CHANGELOG.md
git status --short
```

`git status --short` must show only those files as staged and nothing unstaged. Then commit with subject:

```
Give popup presenters a shadow gutter so elevation renders.
```

and a body covering: WPF sizes a popup HWND to exactly its child's layout size, proven by walking
PopupRoot, its Decorator and its AdornerDecorator, all of which report the child's size with no margin
and no clip; the effect was therefore clipped to the plate and survived only in the rounded corner
notches, which is the dark square plate the owner saw and also why there was no shadow; the fix is a
16 px transparent margin on each presenter root plus the matching subtraction in placement; the
CommandBarFlyout light halo was the same defect, its `ControlStrokeColorDefaultBrush` border being
exactly what WinUI specifies at `CommandBarFlyout_themeresources.xaml:7`; WinUI expresses the same
elevation as a `ThemeShadow` at `CommandBarFlyout_themeresources.xaml:109` with `Translation="0,0,32"`
at `:1063`, which the existing `FlyoutShadowEffect` already translates faithfully.

---

### Task 2: Apply the elevation gutter to the remaining eight popups

The same defect, in every other presenter that hosts a `ShadowCaster` inside a `Popup`. Task 1 proved
the mechanism and set the constant; this task applies it. `ContentDialog` is deliberately excluded: it is
an in-window overlay, not a popup, so its shadow already renders.

**Files:**
- Modify: `Fluence.Wpf/Themes/Controls/ContextMenu.xaml` (root at `:32-37`, submenu at `:99-111`)
- Modify: `Fluence.Wpf/Themes/Controls/ComboBox.xaml:212-252`
- Modify: `Fluence.Wpf/Themes/Controls/AutoSuggestBox.xaml:125-142`
- Modify: `Fluence.Wpf/Themes/Controls/DatePicker.xaml:135-151`
- Modify: `Fluence.Wpf/Themes/Controls/TimePicker.xaml:136-152`
- Modify: `Fluence.Wpf/Themes/Controls/DropDownButton.xaml:79-99`
- Modify: `Fluence.Wpf/Themes/Controls/SplitButton.xaml:174-194`
- Modify: `Fluence.Wpf/Themes/Controls/ToggleSplitButton.xaml:178-198`
- Modify: `Fluence.Wpf.Tests/Control/Rules/PopupElevationTests.cs`
- Modify: the four `Fluence.Wpf.Tests/Baselines/` files, `.github/workflows/build.yml:88`, `CHANGELOG.md`

**Interfaces:**
- Consumes: the 16 px gutter convention and `Fluence.Wpf.Tests.Control.Rules.PopupElevationTests` from
  Task 1. Do not introduce a second constant, a resource key, or a second test class.
- Produces: nothing new. After this task every `ShadowCaster` in the library is inside a popup that
  reserves its gutter, except `ContentDialog`, which does not need one.

- [ ] **Step 1: Write the failing tests**

Append to `Fluence.Wpf.Tests/Control/Rules/PopupElevationTests.cs`, inside the class, one case per
popup-owning control. Two representative shapes; write all eight, following these two exactly.

For a control whose popup is a named template part:

```csharp
        [Fact]
        public Task ComboBox_DropdownPopup_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ComboBox comboBox = new();
                Window window = new() { Content = comboBox, Width = 320, Height = 120 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = comboBox.ApplyTemplate();

                    Popup popup = Assert.IsType<Popup>(comboBox.Template.FindName("PART_Popup", comboBox));

                    Assert.Equal(-ShadowGutter, popup.HorizontalOffset, 0.01);
                    Assert.Equal(-ShadowGutter, popup.VerticalOffset, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
```

For the ContextMenu, whose popup is created by WPF rather than by the template:

```csharp
        [Fact]
        public Task ContextMenu_TemplateRoot_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ContextMenu menu = new();
                menu.Items.Add(new Controls.MenuItem { Header = "Cut" });
                Border host = new();
                host.ContextMenu = menu;
                Window window = new() { Content = host, Width = 320, Height = 200 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    menu.IsOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Grid root = Assert.IsType<Grid>(FindVisualChild<Grid>(menu), exactMatch: false);

                    Assert.Equal(new Thickness(ShadowGutter), root.Margin);
                    Assert.Equal(-ShadowGutter, menu.HorizontalOffset, 0.01);
                    Assert.Equal(-ShadowGutter, menu.VerticalOffset, 0.01);

                    menu.IsOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
```

The other six follow the `ComboBox` shape with these substitutions: `AutoSuggestBox` with part name
`PART_SuggestionsPopup`; `DatePicker`, `TimePicker`, `DropDownButton`, `SplitButton` and
`ToggleSplitButton` all with part name `PART_Popup`. Each also asserts the presenter root's
`Margin` where the template owns a named root; where it does not, assert the popup offsets only.

- [ ] **Step 2: Run the tests to verify they fail**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.Rules.PopupElevationTests --no-ansi --progress off
```

Expected: the eight new cases fail on `Assert.Equal(-16, ..., 0.01)` reporting `0`. The six Task 1 cases
still pass.

- [ ] **Step 3: Add the gutter to the eight templates**

For each of the eight, wrap or annotate the presenter content root with `Margin="16"` and add the two
offsets to the popup. Concretely, in `Fluence.Wpf/Themes/Controls/ComboBox.xaml` the popup at lines 212
to 219 becomes:

```xml
                        <Popup
                            x:Name="PART_Popup"
                            AllowsTransparency="True"
                            HorizontalOffset="-16"
                            VerticalOffset="-16"
```

with the remaining existing attributes unchanged below, and the `Grid` that parents `ShadowCaster` at
line 252 gains `Margin="16"`. Apply the identical pair of edits to `AutoSuggestBox.xaml` (`:125`,
`:142`), `DatePicker.xaml` (`:135`, `:151`), `TimePicker.xaml` (`:136`, `:152`),
`DropDownButton.xaml` (`:79`, `:99`), `SplitButton.xaml` (`:174`, `:194`) and
`ToggleSplitButton.xaml` (`:178`, `:198`).

`ContextMenu.xaml` needs three edits: the root menu's `Grid` at `:36` gains `Margin="16"`, the style
gains `<Setter Property="HorizontalOffset" Value="-16" />` and
`<Setter Property="VerticalOffset" Value="-16" />` because WPF owns the root context menu popup, and the
submenu `PART_Popup` at `:99` to `:106` gains the same two attributes with the submenu `Grid` at `:108`
gaining `Margin="16"`.

In every file, replace the false "PopupRoot already reserves about 15 px per side for popup shadows"
sentence with the corrected wording from Task 1 Step 5.

- [ ] **Step 4: Run the tests to verify they pass**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.Rules.PopupElevationTests Fluence.Wpf.Tests.Control.ComboBoxTests Fluence.Wpf.Tests.Control.AutoSuggestBoxTests Fluence.Wpf.Tests.Control.DatePickerTests Fluence.Wpf.Tests.Control.TimePickerTests Fluence.Wpf.Tests.Control.DropDownButtonTests Fluence.Wpf.Tests.Control.SplitButtonTests Fluence.Wpf.Tests.Control.ToggleSplitButtonTests Fluence.Wpf.Tests.Control.ContextMenuTests Fluence.Wpf.Tests.Control.MenuTests --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.Rules.PopupElevationTests Fluence.Wpf.Tests.Control.ComboBoxTests Fluence.Wpf.Tests.Control.AutoSuggestBoxTests Fluence.Wpf.Tests.Control.DatePickerTests Fluence.Wpf.Tests.Control.TimePickerTests Fluence.Wpf.Tests.Control.DropDownButtonTests Fluence.Wpf.Tests.Control.SplitButtonTests Fluence.Wpf.Tests.Control.ToggleSplitButtonTests Fluence.Wpf.Tests.Control.ContextMenuTests Fluence.Wpf.Tests.Control.MenuTests --no-ansi --progress off
```

`KNOWN_ISSUES.md` records a `net472` TimePicker flyout flake. If `TimePickerTests` fails on `net472`
only, rerun that one class alone before treating it as a regression.

- [ ] **Step 5: Visual verification**

Dark then Light. Selection page: open the `ComboBox` and the `With icon` combo. Forms page: open the
`DatePicker` and `TimePicker` flyouts. Buttons page: open the `DropDownButton`, `SplitButton` and
`ToggleSplitButton` menus. Menus page: right-click `Right-click this note` for the `ContextMenu`, then
open the `File` menu and hover into a submenu. Inputs page: type into `Search fruit` so the
`AutoSuggestBox` suggestion list opens.

What to look for: the same graded shadow, no square corner step, and, critically, **no drift**. A
dropdown must still align with the left edge of its owner and open flush under it. A submenu must still
sit flush against the parent item. Drift means an offset was missed or double-applied.

- [ ] **Step 6: Update baselines, the CI sum-check and the CHANGELOG**

Eight new cases, so `.github/workflows/build.yml:88` goes from `1210` and `1211` to `1218` and `1219`,
verified against `--list-tests`. Extend the `### Fixed` bullet from Task 1 with a second bullet:

```markdown
- ContextMenu and its submenus, ComboBox, AutoSuggestBox, DatePicker, TimePicker, DropDownButton,
  SplitButton and ToggleSplitButton popups reserve the same 16 px elevation gutter, so every popup
  surface in the library now casts the shadow it was already configured for.
```

- [ ] **Step 7: Closing gate and commit**

```
dotnet build Fluence.Wpf.sln -c Debug
dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore
pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll
```

Stage each of the eleven changed files by name, run `git status --short`, and commit with subject:

```
Extend the popup shadow gutter to the remaining popups.
```

Body: the same clipping mechanism proven in the previous commit applies to every presenter whose
`ShadowCaster` lives inside a `Popup`; lists the eight controls; records that `ContentDialog` is excluded
because it is an in-window overlay whose shadow already renders; cites
`CommandBarFlyout_themeresources.xaml:109` and `:1063` for the WinUI flyout elevation the effect
translates.

---

### Task 3: Slider thumb capsule and exclusive scale states

Defect 10. Read the "Defect 10" section of the research first. The inner dot's absolute size and all four
scale values already match WinUI exactly. Two things are wrong: the outer capsule is 20 where WinUI's is
22, so the dot occupies 0.516 of the thumb instead of 0.469; and the `IsDragging` `ExitActions` animate
back to the hover value, so the dot can stick at 14 px after a drag.

**Files:**
- Modify: `Fluence.Wpf/Themes/Controls/Slider.xaml:8,19-25,41-137,143`
- Modify: `Fluence.Wpf.Tests/Control/SliderTests.cs`
- Modify: `docs/winui-parity.md:96-97`
- Modify: the four `Fluence.Wpf.Tests/Baselines/` files, `.github/workflows/build.yml:88`, `CHANGELOG.md`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: the template part names `ThumbEllipse`, `ThumbInnerDot` and `ThumbScale` are unchanged. No
  public member and no resource key changes.

- [ ] **Step 1: Write the failing test**

Add to `Fluence.Wpf.Tests/Control/SliderTests.cs`, inside
`public sealed class SliderTests : IClassFixture<LightThemeFixture>`:

```csharp
        [Fact]
        public Task Slider_Thumb_MatchesWinUiCapsuleAndInnerDotGeometryAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Slider slider = new() { Value = 50, Minimum = 0, Maximum = 100 };
                Window w = new() { Content = slider, Width = 300, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    // WinUI's thumb is an 18x18 Thumb whose template root Border carries Margin="-2"
                    // (Slider_themeresources.xaml:169-170, :198), so the painted capsule is 22x22. The
                    // inner accent dot is a 12x12 ellipse scaled 0.86 at rest (:166, :173, :210), that
                    // is 10.32px, which is 0.469 of the capsule and not the 0.516 a 20px capsule gives.
                    // WPF centres an Ellipse Stroke on the geometry, so ThumbEllipse is 21 inside the
                    // 22 Thumb: 21 plus the centred 1px stroke is the same 22px painted extent that
                    // WinUI gets from a Border whose BorderThickness draws inside the 22 box.
                    Thumb thumb = Assert.IsType<Thumb>(FindVisualChild<Thumb>(slider), exactMatch: false);
                    Ellipse capsule = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(thumb, "ThumbEllipse"), exactMatch: false);
                    Ellipse innerDot = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(thumb, "ThumbInnerDot"), exactMatch: false);
                    ScaleTransform scale = Assert.IsType<ScaleTransform>(innerDot.RenderTransform);

                    Assert.Equal(22.0, thumb.Width, 0.01);
                    Assert.Equal(22.0, thumb.Height, 0.01);
                    Assert.Equal(21.0, capsule.Width, 0.01);
                    Assert.Equal(21.0, capsule.Height, 0.01);
                    Assert.Equal(12.0, innerDot.Width, 0.01);
                    Assert.Equal(12.0, innerDot.Height, 0.01);
                    Assert.Equal(10.32, innerDot.Width * scale.ScaleX, 0.01);
                    Assert.Equal(10.32, innerDot.Height * scale.ScaleY, 0.01);
                }
                finally
                {
                    w.Close();
                }
            });
        }
```

- [ ] **Step 2: Run the test to verify it fails**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.SliderTests --filter-method "*MatchesWinUiCapsule*" --no-ansi --progress off
```

Expected: FAIL, `Assert.Equal() Failure: Expected 22.0, Actual 20.0`.

- [ ] **Step 3: Widen the capsule**

`Fluence.Wpf/Themes/Controls/Slider.xaml:8`:

```xml
        <Setter Property="Height" Value="22" />
```

`Fluence.Wpf/Themes/Controls/Slider.xaml:143`:

```xml
        <Setter Property="Width" Value="22" />
```

`Fluence.Wpf/Themes/Controls/Slider.xaml:19-25`:

```xml
                        <!--
                            WinUI's thumb is an 18x18 Thumb whose template root Border carries
                            Margin="-2" (Slider_themeresources.xaml:169-170, :198), so the painted
                            capsule is 22x22 with the 1px stroke drawn inside it. WPF centres an
                            Ellipse Stroke on the geometry, so a 22 ellipse would paint to 23; 21
                            inside the 22 Thumb gives the same 22px painted extent. WinUI's
                            SliderThumbCornerRadius of 10 on a 22 box is a slight squircle; the
                            Ellipse keeps a true circle here, an intentional deviation.
                        -->
                        <Ellipse
                            x:Name="ThumbEllipse"
                            Width="21"
                            Height="21"
                            Fill="{DynamicResource ControlSolidFillColorDefaultBrush}"
                            Stroke="{DynamicResource ControlElevationBorderBrush}"
                            StrokeThickness="1" />
```

The 21 in the ellipse against the 22 on the `Thumb` is deliberate and is what Step 1's test already
asserts: WPF centres the stroke on the geometry, so 21 plus a centred 1 px stroke paints to the same
22 px extent WinUI gets from a `Border` whose `BorderThickness` draws inside the 22 box.

- [ ] **Step 4: Make the scale states mutually exclusive**

Replace the three trigger blocks at `Fluence.Wpf/Themes/Controls/Slider.xaml:41-137` with
`EnterActions`-only triggers whose conditions cannot overlap. WinUI's visual state manager re-evaluates
the whole state on exit; a hard-coded WPF `ExitAction` cannot, which is why the drag exit currently
animates to the hover value 1.167 and can leave the dot stuck at 14 px:

```xml
                        <MultiTrigger>
                            <MultiTrigger.Conditions>
                                <Condition Property="IsMouseOver" Value="False" />
                                <Condition Property="IsDragging" Value="False" />
                            </MultiTrigger.Conditions>
                            <MultiTrigger.EnterActions>
                                <BeginStoryboard>
                                    <Storyboard>
                                        <DoubleAnimationUsingKeyFrames Storyboard.TargetName="ThumbScale" Storyboard.TargetProperty="ScaleX">
                                            <SplineDoubleKeyFrame
                                                KeySpline="0,0,0,1"
                                                KeyTime="0:0:0.1"
                                                Value="0.86" />
                                        </DoubleAnimationUsingKeyFrames>
                                        <DoubleAnimationUsingKeyFrames Storyboard.TargetName="ThumbScale" Storyboard.TargetProperty="ScaleY">
                                            <SplineDoubleKeyFrame
                                                KeySpline="0,0,0,1"
                                                KeyTime="0:0:0.1"
                                                Value="0.86" />
                                        </DoubleAnimationUsingKeyFrames>
                                    </Storyboard>
                                </BeginStoryboard>
                            </MultiTrigger.EnterActions>
                        </MultiTrigger>
                        <MultiTrigger>
                            <MultiTrigger.Conditions>
                                <Condition Property="IsMouseOver" Value="True" />
                                <Condition Property="IsDragging" Value="False" />
                            </MultiTrigger.Conditions>
                            <MultiTrigger.EnterActions>
                                <BeginStoryboard>
                                    <Storyboard>
                                        <DoubleAnimationUsingKeyFrames Storyboard.TargetName="ThumbScale" Storyboard.TargetProperty="ScaleX">
                                            <SplineDoubleKeyFrame
                                                KeySpline="0,0,0,1"
                                                KeyTime="0:0:0.1"
                                                Value="1.167" />
                                        </DoubleAnimationUsingKeyFrames>
                                        <DoubleAnimationUsingKeyFrames Storyboard.TargetName="ThumbScale" Storyboard.TargetProperty="ScaleY">
                                            <SplineDoubleKeyFrame
                                                KeySpline="0,0,0,1"
                                                KeyTime="0:0:0.1"
                                                Value="1.167" />
                                        </DoubleAnimationUsingKeyFrames>
                                    </Storyboard>
                                </BeginStoryboard>
                            </MultiTrigger.EnterActions>
                        </MultiTrigger>
                        <Trigger Property="IsDragging" Value="True">
                            <Trigger.EnterActions>
                                <BeginStoryboard>
                                    <Storyboard>
                                        <DoubleAnimationUsingKeyFrames Storyboard.TargetName="ThumbScale" Storyboard.TargetProperty="ScaleX">
                                            <SplineDoubleKeyFrame
                                                KeySpline="0,0,0,1"
                                                KeyTime="0:0:0.1"
                                                Value="0.71" />
                                        </DoubleAnimationUsingKeyFrames>
                                        <DoubleAnimationUsingKeyFrames Storyboard.TargetName="ThumbScale" Storyboard.TargetProperty="ScaleY">
                                            <SplineDoubleKeyFrame
                                                KeySpline="0,0,0,1"
                                                KeyTime="0:0:0.1"
                                                Value="0.71" />
                                        </DoubleAnimationUsingKeyFrames>
                                    </Storyboard>
                                </BeginStoryboard>
                            </Trigger.EnterActions>
                        </Trigger>
```

Leave the existing `IsEnabled="False"` trigger at `:119-137` exactly as it is; WinUI's `Disabled` state
is 1.167 (`Slider_themeresources.xaml:244-251`) and it already matches.

Trigger order matters in WPF: later triggers win, so the `IsDragging` trigger must come last, as written
above.

- [ ] **Step 5: Run the tests to verify they pass**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.SliderTests Fluence.Wpf.Tests.Control.Rules.ReducedMotionTests Fluence.Wpf.Tests.Control.Rules.FluentStrokeTests --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.SliderTests Fluence.Wpf.Tests.Control.Rules.ReducedMotionTests Fluence.Wpf.Tests.Control.Rules.FluentStrokeTests --no-ansi --progress off
```

`SliderTests:77-105` (`Slider_DefaultState_ThumbInnerDotScaleIsRestValueAsync`) must still pass: the rest
scale is still 0.86.

- [ ] **Step 6: Update `docs/winui-parity.md`**

Replace the two rows at `docs/winui-parity.md:96-97` with:

```markdown
| `Slider` thumb | 22 | `SliderHorizontalThumbWidth` and `Height` 18 plus `Margin="-2"` on the thumb template root, giving a 22 px painted capsule | `CommonStyles\Slider_themeresources.xaml:169-170,198` |
| `Slider` inner dot | 12 base, scaled 0.86 rest, 1.167 hover, 0.71 pressed, so 10.32, 14 and 8.52 inside the 22 px capsule | the same | `CommonStyles\Slider_themeresources.xaml:166,173,208-251` |
```

Match the surrounding table's column count exactly; read the header row before editing.

- [ ] **Step 7: Visual verification**

Inputs page, then the Settings page sliders, Dark and Light, at 100 and 150 percent DPI. Save before and
after captures.

What to look for: the accent dot sits inside a white capsule with a visible ring of plate all round it,
matching the WinUI Gallery Slider page side by side. Hover and the dot grows to 14. Press and drag and
it shrinks to 8.52. **Release with the pointer moved off the thumb: it must settle back to 10.32, not
stay at 14.** That last one is the exclusive-state fix. Confirm the 22 px thumb is not clipped by the
32 DIP `TrackHost` (`Slider.xaml:215`) in either orientation.

- [ ] **Step 8: Bookkeeping, gate and commit**

One new case, so `.github/workflows/build.yml:88` increments both values by 1 from whatever Task 2 left
them at, verified against `--list-tests`. Regenerate all four baselines. Add to `CHANGELOG.md` under
`### Fixed`:

```markdown
- The Slider thumb's accent dot no longer reads oversized. The outer capsule is 22 px, matching WinUI's
  18 px thumb plus the `Margin="-2"` its template root carries, and the hover, pressed and rest scale
  states are now mutually exclusive so releasing a drag away from the thumb returns the dot to its rest
  size instead of leaving it at the hover size.
```

Closing gate, then stage `Fluence.Wpf/Themes/Controls/Slider.xaml`,
`Fluence.Wpf.Tests/Control/SliderTests.cs`, `docs/winui-parity.md`, the four baselines,
`.github/workflows/build.yml` and `CHANGELOG.md` by name. Commit subject:

```
Match the WinUI slider thumb capsule and scale states.
```

Body: cites `Slider_themeresources.xaml:169-170` for the 18 px thumb, `:198` for the `Margin="-2"` that
makes the painted capsule 22, `:166` and `:173` for the 12 px inner dot, and `:208-251` for the four
scale states; explains that the dot was already the right absolute size and it was the capsule that was
2 px small, giving a 0.516 ratio against WinUI's 0.469; explains that the drag exit action animated to
the hover value because a WPF `ExitAction` cannot re-evaluate state the way WinUI's VSM does, and that
the triggers are now `EnterActions`-only with non-overlapping conditions.

---

### Task 4: NumberBox spin button chrome and input validation

Defect 9, both halves. Read the "Defect 9" section of the research first. The spin buttons draw a real
`ControlElevationBorderBrush` outline where WinUI's resolve to a fully transparent brush, and the control
keeps unparseable text as a value where WinUI overwrites it.

**Files:**
- Modify: `Fluence.Wpf/Themes/Controls/NumberBox.xaml:13-55`
- Modify: `Fluence.Wpf/Controls/NumberBox.cs:435-469`
- Modify: `Fluence.Wpf.Tests/Control/NumberBoxTests.cs`
- Modify: the four `Fluence.Wpf.Tests/Baselines/` files, `.github/workflows/build.yml:88`, `CHANGELOG.md`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `private void NumberBox.ValidateInput()`. It is private, so nothing outside the class sees
  it. The public `TryParseText()` keeps its signature and its documented contract unchanged, and no
  `ValidationMode` property is added, because the public surface is frozen. The behaviour Fluence gains
  is WinUI's default, `NumberBoxValidationMode.InvalidInputOverwritten`, hard-wired.

- [ ] **Step 1: Write the failing tests**

Add to `Fluence.Wpf.Tests/Control/NumberBoxTests.cs`. Add `using System.Windows.Media;` after
`using System.Windows.Input;` and
`using static Fluence.Wpf.Tests.Infrastructure.InputSimulation;` before the existing
`using static Fluence.Wpf.Tests.Infrastructure.VisualTree;`.

```csharp
        [Fact]
        public Task NumberBox_SpinButtons_DrawNoChromeAtRestAsync()
        {
            // WinUI retargets the whole RepeatButton brush family on the NumberBox template root
            // (NumberBox.xaml:61-102), so the rest fill resolves to TextControlButtonBackground
            // (transparent) and every border state, disabled included (:74), resolves to
            // TextControlButtonBorderBrush, that is ControlFillColorTransparent. The canonical
            // "0,1,1,1" thickness (NumberBox_themeresources.xaml:29) reserves layout and paints
            // nothing. Both brushes must therefore have zero alpha.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
                    {
                        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    RepeatButton upButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_UpButton", numberBox));
                    RepeatButton downButton = Assert.IsType<RepeatButton>(numberBox.Template.FindName("PART_DownButton", numberBox));
                    RepeatButton[] spinButtons = [upButton, downButton];

                    foreach (RepeatButton spinButton in spinButtons)
                    {
                        SolidColorBrush border = Assert.IsType<SolidColorBrush>(spinButton.BorderBrush);
                        Assert.Equal(0, border.Color.A);

                        SolidColorBrush background = Assert.IsType<SolidColorBrush>(spinButton.Background);
                        Assert.Equal(0, background.Color.A);
                    }
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NumberBox_NonNumericText_IsOverwrittenOnEnterAsync()
        {
            // WinUI defaults ValidationMode to NumberBoxValidationMode.InvalidInputOverwritten
            // (NumberBox.idl:15-19, member 0), so NumberBox::ValidateInput (NumberBox.cpp:478-521)
            // discards text that does not parse and rewrites it from the committed Value. WinUI runs
            // that on Enter (NumberBox.cpp:560-568).
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
                    {
                        Minimum = 0,
                        Maximum = 100,
                        Value = 50,
                        Width = 160,
                    };
                    window.Content = numberBox;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    TextBox textBox = Assert.IsType<TextBox>(numberBox.Template.FindName("PART_TextBox", numberBox));
                    _ = textBox.Focus();
                    _ = Keyboard.Focus(textBox);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    textBox.Text = "abc";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    RaiseKeyEvent(textBox, Key.Enter, Keyboard.KeyDownEvent);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(50.0, numberBox.Value);
                    Assert.Equal("50", textBox.Text, StringComparer.Ordinal);
                    Assert.Equal("50", numberBox.Text, StringComparer.Ordinal);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NumberBox_NonNumericText_IsOverwrittenOnLostFocusAsync()
        {
            // Companion to the Enter case: WinUI validates on lost focus too
            // (NumberBox::OnNumberBoxLostFocus, NumberBox.cpp:431-439). Focus moves to a sibling so
            // PART_TextBox raises LostKeyboardFocus for real.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NumberBox numberBox = new()
                    {
                        Minimum = 0,
                        Maximum = 100,
                        Value = 7,
                        Width = 160,
                    };
                    Button sibling = new() { Content = "Elsewhere", Width = 80 };
                    StackPanel root = new();
                    _ = root.Children.Add(numberBox);
                    _ = root.Children.Add(sibling);
                    window.Content = root;
                    window.Width = 240;
                    window.Height = 160;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = numberBox.ApplyTemplate();
                    TextBox textBox = Assert.IsType<TextBox>(numberBox.Template.FindName("PART_TextBox", numberBox));
                    _ = textBox.Focus();
                    _ = Keyboard.Focus(textBox);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    textBox.Text = "12abc";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    _ = sibling.Focus();
                    _ = Keyboard.Focus(sibling);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(7.0, numberBox.Value);
                    Assert.Equal("7", textBox.Text, StringComparer.Ordinal);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.NumberBoxTests --no-ansi --progress off
```

Expected: the chrome test fails with `Assert.IsType` on a `LinearGradientBrush` (that is
`ControlElevationBorderBrush`), and the two input tests fail with `Assert.Equal("50", "abc")` and
`Assert.Equal(7.0, 12.0)` or similar.

- [ ] **Step 3: Replace the spin button style**

Replace `Fluence.Wpf/Themes/Controls/NumberBox.xaml:13-55` with:

```xml
    <!--
        Matches the WinUI 3 NumberBoxSpinButtonStyle (NumberBox.xaml:113-123, BasedOn
        DefaultRepeatButtonStyle). The NumberBox template root retargets the whole RepeatButton
        brush family (NumberBox.xaml:61-102), so the spin buttons resolve the text-control button
        ramp, not the ordinary control-fill ramp:
            rest      Background  TextControlButtonBackground            transparent
                      BorderBrush TextControlButtonBorderBrush           ControlFillColorTransparent
                      Foreground  TextControlButtonForeground            TextFillColorSecondaryBrush
            hover     Background  TextControlButtonBackgroundPointerOver SubtleFillColorSecondaryBrush
            pressed   Background  TextControlButtonBackgroundPressed     SubtleFillColorTertiaryBrush
                      Foreground  TextControlButtonForegroundPressed     TextFillColorTertiaryBrush
            disabled  Background  RepeatButtonBackgroundDisabled         ControlFillColorDisabledBrush
                      Foreground  RepeatButtonForegroundDisabled         TextFillColorDisabledBrush
        Every border state, disabled included (NumberBox.xaml:74), resolves to
        ControlFillColorTransparent, so the canonical NumberBoxSpinButtonBorderThickness of
        "0,1,1,1" (NumberBox_themeresources.xaml:29) reserves layout and paints nothing. Fluence
        previously drew that thickness with ControlElevationBorderBrush over a
        ControlFillColorDefaultBrush plate, which is what boxed each spin button inside the input.
    -->
    <Style x:Key="NumberBoxSpinButton" TargetType="{x:Type RepeatButton}">
        <Setter Property="Background" Value="{DynamicResource ControlFillColorTransparentBrush}" />
        <Setter Property="BorderBrush" Value="{DynamicResource ControlFillColorTransparentBrush}" />
        <Setter Property="BorderThickness" Value="0,1,1,1" />
        <!--  Fire Click on MouseDown so a quick press-release registers; the default Release mode  -->
        <!--  waits for the internal repeat timer's first tick.  -->
        <Setter Property="ClickMode" Value="Press" />
        <Setter Property="Focusable" Value="True" />
        <Setter Property="Foreground" Value="{DynamicResource TextFillColorSecondaryBrush}" />
        <Setter Property="Height" Value="30" />
        <Setter Property="IsTabStop" Value="False" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="{x:Type RepeatButton}">
                    <Border
                        x:Name="SpinBorder"
                        Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="{DynamicResource ControlCornerRadius}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="SpinBorder" Property="Background" Value="{DynamicResource SubtleFillColorSecondaryBrush}" />
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="SpinBorder" Property="Background" Value="{DynamicResource SubtleFillColorTertiaryBrush}" />
                            <Setter Property="Foreground" Value="{DynamicResource TextFillColorTertiaryBrush}" />
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter TargetName="SpinBorder" Property="Background" Value="{DynamicResource ControlFillColorDisabledBrush}" />
                            <Setter Property="Foreground" Value="{DynamicResource TextFillColorDisabledBrush}" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Setter Property="UseLayoutRounding" Value="True" />
        <Setter Property="Width" Value="32" />
    </Style>
```

Before writing it, read the existing block on disk and carry over verbatim any setter present there that
is not listed above, so nothing is silently dropped. `ControlFillColorTransparentBrush` is a real frozen
`SolidColorBrush` with `A=0`, not `null`, so the buttons stay hit-testable. There is no hard-coded hex,
so the text policy gate on `Themes/Controls/**` stays green.

- [ ] **Step 4: Add the validation helper and route the four handlers through it**

In `Fluence.Wpf/Controls/NumberBox.cs`, add immediately before `OnPartTextBoxKeyDown` (line 435):

```csharp
        /// <summary>
        /// Applies the WinUI <c>NumberBoxValidationMode.InvalidInputOverwritten</c> contract, which
        /// is the WinUI default, to whatever the inner text box currently holds: text that parses is
        /// committed to <see cref="Value"/> and then re-formatted from it, and text that does not
        /// parse is discarded and overwritten with the committed value. Mirrors
        /// NumberBox::ValidateInput (NumberBox.cpp lines 478 to 521), which WinUI runs on lost focus
        /// (lines 431 to 439), on Enter (lines 560 to 568), and at the top of StepValue (line 602).
        /// Keystrokes are deliberately not filtered: WinUI accepts any character and rejects the
        /// value at commit time, which is what keeps expression input working.
        /// </summary>
        private void ValidateInput()
        {
            // TryParseText commits Value on success. UpdateTextFromValue then normalises the text in
            // both directions: it re-formats "007" to "7" after a successful parse whose value did
            // not move, and it restores the committed value after a failed one.
            _ = TryParseText();
            UpdateTextFromValue();
        }
```

Then replace the four call sites so each runs `ValidateInput()` in place of `_ = TryParseText();`:

```csharp
        private void OnPartTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter)
            {
                ValidateInput();
                e.Handled = true;
            }
            else if (e.Key is Key.Up)
            {
                ValidateInput();
                OnUpClick();
                e.Handled = true;
            }
            else if (e.Key is Key.Down)
            {
                ValidateInput();
                OnDownClick();
                e.Handled = true;
            }
        }

        private void OnPartTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ValidateInput();
        }

        private void OnPartUpButtonClick(object sender, RoutedEventArgs e)
        {
            ValidateInput();
            OnUpClick();
        }

        private void OnPartDownButtonClick(object sender, RoutedEventArgs e)
        {
            ValidateInput();
            OnDownClick();
        }
```

Keep each method's existing XML documentation comment; only the bodies change. Analyzer note: the two
handlers take parameters they do not use, which is already the case on disk, so no new diagnostic
appears.

- [ ] **Step 5: Run the tests to verify they pass**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.NumberBoxTests Fluence.Wpf.Tests.Control.Rules.FluentStrokeTests Fluence.Wpf.Tests.Gallery.Pages.GalleryInputsPageTests --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.NumberBoxTests Fluence.Wpf.Tests.Control.Rules.FluentStrokeTests Fluence.Wpf.Tests.Gallery.Pages.GalleryInputsPageTests --no-ansi --progress off
```

The four existing spin-click tests (`NumberBoxTests.cs:52`, `:96`, `:137`, `:335`) all start from a
`Value` whose text already matches, so the added `ValidateInput()` in the click handlers is inert for
them and they must stay green.

- [ ] **Step 6: Visual verification**

Inputs page, NumberBox sample (host slot at `Fluence.Wpf.Demo/Pages/GalleryInputsPage.xaml:121`), Light
and Dark. No demo change is needed.

What to look for: the `Inline` and `Compact` spin buttons show no outline at rest, a subtle plate on
hover and a subtler one on press, and the glyphs read secondary rather than primary. Type `abc` into
`Keyboard only` and press Enter: the box snaps back to `50`. Type `12abc` and click elsewhere: it snaps
back. Type `12` and press Enter: it commits `12`. Confirm the `Disabled` instance still shows the
disabled fill.

- [ ] **Step 7: Bookkeeping, gate and commit**

Three new cases, so increment both `.github/workflows/build.yml:88` values by 3, verified against
`--list-tests`. Regenerate all four baselines. Add to `CHANGELOG.md` under `### Fixed`:

```markdown
- NumberBox spin buttons no longer draw a border inside the input. WinUI retargets the RepeatButton
  brush family on the NumberBox template root so every spin button border state resolves to a fully
  transparent brush; Fluence was painting the canonical border thickness with the elevation gradient.
- NumberBox now discards text that does not parse, on Enter, on lost focus and before a spin step,
  matching WinUI's default `NumberBoxValidationMode.InvalidInputOverwritten`. It previously kept
  alphanumeric text as if it were a value.
```

Closing gate, then stage `Fluence.Wpf/Themes/Controls/NumberBox.xaml`,
`Fluence.Wpf/Controls/NumberBox.cs`, `Fluence.Wpf.Tests/Control/NumberBoxTests.cs`, the four baselines,
`.github/workflows/build.yml` and `CHANGELOG.md` by name. Commit subject:

```
Flatten the NumberBox spin buttons and reject non-numeric input.
```

Body: cites `NumberBox\NumberBox.xaml:61-102` for the RepeatButton brush retarget, `:74` for the
disabled border alias, `NumberBox_themeresources.xaml:29` for the border thickness that paints nothing,
`CommonStyles\TextBox_themeresources.xaml:40-47` for the text-control button ramp,
`NumberBox\NumberBox.idl:15-19` for `NumberBoxValidationMode` and its default member, and
`NumberBox\NumberBox.cpp:478-521` for `ValidateInput` with `:431-439`, `:560-568` and `:599-602` for its
three call sites. Records that no public member was added because the 1.0 surface is frozen, so the
WinUI default is hard-wired and a `ValidationMode` property can be added additively after 1.0.

---

### Task 5: Stop the button MinWidth floor from stretching the graphical sample

Defect 7. Read the "Defect 7" section of the research first, because the spec's diagnosis is falsified:
the stroke on the pie button is byte-identical to the standard button's, top row 229 and bottom row 204
on both, and `ControlElevationBorderBrush` does not degenerate with control height. What is actually
wrong is the size. `Fluence.Wpf/Themes/Controls/Button.xaml:18` sets `MinWidth = 110` and WPF clamps
`Width` up to `MinWidth`, so the sample authored at 50 x 50 renders 110 x 50 and the image is stretched
into a rectangle. There is also no RGB or image button on the Menus page; the only `Slices.png` use in
the repo is `GalleryButtonsPage.xaml:48`, and what the Menus page shows is the same floor making every
text button 110 DIP wide.

**Files:**
- Modify: `Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml:41-49`
- Modify: `Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml.cs:62-74`
- Modify: `Fluence.Wpf.Tests/Control/ButtonTests.cs`
- Modify: the four `Fluence.Wpf.Tests/Baselines/` files, `.github/workflows/build.yml:88`, `CHANGELOG.md`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: nothing. `Fluence.Wpf/Themes/Controls/Button.xaml` is deliberately **not** modified. Deleting
  the `MinWidth` floor would change every button in the gallery and contradict the deviation documented
  at `docs/winui-parity.md:70`; if the owner wants that instead, it is a separate change.

- [ ] **Step 1: Write the failing test**

Add to `Fluence.Wpf.Tests/Control/ButtonTests.cs`, inside
`public sealed class ButtonTests : IAsyncLifetime`:

```csharp
        [Fact]
        public Task Button_ExplicitSquareSize_IsNotWidenedByTheMinWidthFloorAsync()
        {
            // WinUI Gallery's "Button with graphical content" sample
            // (Samples\Button\ButtonPage.xaml:49-56) is a 50x50 square, and WinUI's
            // DefaultButtonStyle sets no MinWidth. Fluence keeps a 110 dp floor
            // (Themes/Controls/Button.xaml:18, recorded in docs/winui-parity.md:70) and WPF clamps
            // Width up to MinWidth, so an authored square must lower MinWidth and MinHeight as well
            // as setting Width and Height.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.Button square = new()
                {
                    Width = 50,
                    Height = 50,
                    MinWidth = 50,
                    MinHeight = 50,
                    Content = new Border { Width = 24, Height = 24 },
                };

                try
                {
                    window.Content = square;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(50.0, square.ActualWidth, 0.01);
                    Assert.Equal(50.0, square.ActualHeight, 0.01);
                }
                finally
                {
                    window.Close();
                }
            });
        }
```

Also add, to `Fluence.Wpf.Tests/Gallery/Pages/GalleryButtonsPageTests.cs` if that class exists (check by
listing `Fluence.Wpf.Tests/Gallery/Pages/`; if it does not exist, skip this second test and say so in
the commit body), a regression lock on the sample itself, matching that class's existing lifecycle and
helpers rather than the shape below if they differ:

```csharp
        [Fact]
        public Task GalleryButtonsPage_GraphicalButton_RendersAsAFiftyPixelSquareAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.Button graphical = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(window, "GraphicalButton"), exactMatch: false);

                Assert.Equal(50.0, graphical.ActualWidth, 0.01);
                Assert.Equal(50.0, graphical.ActualHeight, 0.01);
            });
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.ButtonTests --filter-method "*MinWidthFloor*" --no-ansi --progress off
```

Expected: FAIL, `Assert.Equal() Failure: Expected 50.0, Actual 110.0`.

- [ ] **Step 3: Lower the floor on the sample**

`Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml:42-49` becomes:

```xml
                <fluence:Button
                    x:Name="GraphicalButton"
                    Width="50"
                    Height="50"
                    MinWidth="50"
                    MinHeight="50"
                    AutomationProperties.Name="Pie"
                    Click="GraphicalButton_Click">
                    <Image AutomationProperties.Name="Slice" Source="pack://application:,,,/Fluence.Wpf.Demo;component/Resources/SampleMedia/Slices.png" />
                </fluence:Button>
```

- [ ] **Step 4: Keep the displayed source in lockstep**

The Source code tab must show what the sample actually is. In
`Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml.cs`, in `ButtonGraphicalContentXamlSource`, insert two
lines immediately after the `"            Height=\"50\"\n" +` line:

```csharp
                                                                "            MinWidth=\"50\"\n" +
                                                                "            MinHeight=\"50\"\n" +
```

- [ ] **Step 5: Run the tests to verify they pass**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.ButtonTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.ButtonTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests --no-ansi --progress off
```

`DemoSampleContractTests` matters here because it parses every `XamlSource` as XML and checks the root
and `x:Class`; a typo in the two inserted lines breaks it.

- [ ] **Step 6: Visual verification**

Buttons page, Light and Dark, 100 and 150 percent DPI. Save before and after captures.

What to look for: the pie button is a 50 x 50 square with the image at its natural aspect, not a 110 x 50
rectangle with a stretched pie. Its bottom border is visibly darker than its top and sides, matching the
standard button beside it, which it already was. Open the Source code tab and confirm the shown XAML now
carries `MinWidth="50"` and `MinHeight="50"`.

- [ ] **Step 7: Bookkeeping, gate and commit**

One or two new cases depending on whether `GalleryButtonsPageTests` exists. Increment both
`.github/workflows/build.yml:88` values by the actual count from `--list-tests`. Regenerate all four
baselines. Add to `CHANGELOG.md` under `### Fixed`:

```markdown
- Demo: the Buttons page graphical-content sample renders as the 50 px square the WinUI Gallery sample
  is, instead of being stretched to 110 px wide by the library's `MinWidth` floor.
```

Closing gate, then stage by name and commit with subject:

```
Render the graphical button sample at its authored square size.
```

Body: records that the reported stroke difference was measured and falsified, both buttons sampling 229
on the top and sides and 204 on the bottom band, and that `ControlElevationBorderBrush` holds its 3 DIP
band at any height because `MappingMode="Absolute"` plus the `ScaleTransform(ScaleY=-1, CenterY=0.5)`
mirrors about `height / 2`, matching
`CommonStyles\Common_themeresources_any.xaml:186-191` and `:382-390`; the real cause is
`Themes/Controls/Button.xaml:18` `MinWidth = 110` clamping the authored `Width="50"` from
`WinUIGallery\Samples\Button\ButtonPage.xaml:49-56`; the floor itself is left alone because it is a
documented deviation at `docs/winui-parity.md:70` and removing it would move every button in the
gallery.

---

### Task 6: Surface the three ToggleButton states in the demo and lock the parity

Defect 8. Read the "Defect 8" section of the research first, because the spec's premise does not hold:
WinUI 3 aliases every `Indeterminate*` key to its unchecked counterpart, so Indeterminate and Unchecked
are **deliberately** indistinguishable, and Fluence already implements that correctly. There is no
template change to make. The real gap is that the demo gives the visitor no way to tell which of the
three states is current.

**Files:**
- Modify: `Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml:271-289`
- Modify: `Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml.cs:400-442,585-592`
- Modify: `Fluence.Wpf.Tests/Control/ToggleButtonTests.cs`
- Modify: the four `Fluence.Wpf.Tests/Baselines/` files, `.github/workflows/build.yml:88`, `CHANGELOG.md`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: the named demo element `ThreeStateToggleButtonStateText` and the handler
  `GalleryButtonsPage.ThreeStateToggleButton_StateChanged(object, RoutedEventArgs)`. No library change,
  so no public surface movement.

- [ ] **Step 1: Write the failing test**

Add to `Fluence.Wpf.Tests/Control/ToggleButtonTests.cs`, using the class's existing
`RunToggleButtonTestAsync<T>` helper (`:72-96`) and its `SolidColor` and `ResolvedColor` imports:

```csharp
        [Fact]
        public Task ToggleButton_Indeterminate_MatchesUncheckedRestVisualAsync()
        {
            // WinUI aliases every Indeterminate* key to its unchecked counterpart
            // (ToggleButton_themeresources.xaml:15-18, :27-30, :39-42 in the Default block; :131-134,
            // :143-146, :155-158 in Light), so Indeterminate and Unchecked are deliberately
            // indistinguishable. Lock that in so a future "make null look different" change has to be
            // a conscious deviation.
            return RunToggleButtonTestAsync(
                static () => new Controls.ToggleButton
                {
                    Content = "Toggle",
                    IsThreeState = true,
                    IsChecked = null,
                    IsHitTestVisible = false,
                },
                static (application, toggleButton) =>
                {
                    Border restFill = Assert.IsType<Border>(FindVisualChildByName<Border>(toggleButton, "RestFill"), exactMatch: false);
                    Border outerBorder = Assert.IsType<Border>(FindVisualChildByName<Border>(toggleButton, "OuterBorder"), exactMatch: false);

                    Color indeterminateFill = SolidColor(restFill.Background);
                    Color indeterminateForeground = SolidColor(toggleButton.Foreground);
                    Brush indeterminateStroke = outerBorder.BorderBrush;

                    toggleButton.IsChecked = false;
                    WpfTestSta.DrainDispatcher(toggleButton.Dispatcher);
                    toggleButton.UpdateLayout();

                    Assert.Equal(indeterminateFill, SolidColor(restFill.Background));
                    Assert.Equal(indeterminateForeground, SolidColor(toggleButton.Foreground));
                    Assert.Same(indeterminateStroke, outerBorder.BorderBrush);
                    Assert.Equal(ResolvedColor(application, "ControlFillColorDefaultBrush"), indeterminateFill);
                    Assert.Same(application.Resources["ControlElevationBorderBrush"], indeterminateStroke);
                });
        }
```

Before writing it, read `RunToggleButtonTestAsync`'s actual signature on disk and match the delegate
shapes exactly; the class already contains three Indeterminate tests at `:181`, `:203` and `:228` that
show the calling convention.

- [ ] **Step 2: Run the test to verify it passes immediately**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.ToggleButtonTests --no-ansi --progress off
```

Expected: PASS. **This is deliberate and is not a plan error.** The template is already correct, so this
test is a parity lock, not a red-to-green cycle. To prove it locks something real, temporarily add
`<Setter TargetName="RestFill" Property="Background" Value="{DynamicResource AccentFillColorDefaultBrush}" />`
inside a `MultiTrigger` matching `IsChecked` `{x:Null}` in
`Fluence.Wpf/Themes/Controls/ToggleButton.xaml`, rerun and confirm the test fails, then revert that
temporary edit with a plain `git checkout` of that one file **only if it is otherwise unmodified in this
task**, which it is. Confirm with `git status --short` afterwards.

- [ ] **Step 3: Add the state readout to the demo**

`Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml:271-275` becomes:

```xml
                    <fluence:ToggleButton
                        x:Name="ThreeStateToggleButton"
                        Margin="{DynamicResource DemoControlGroupItemMargin}"
                        Checked="ThreeStateToggleButton_StateChanged"
                        Content="Three-state"
                        Indeterminate="ThreeStateToggleButton_StateChanged"
                        IsThreeState="True"
                        Unchecked="ThreeStateToggleButton_StateChanged" />
```

and the output host at `:283-288` becomes a two-line stack:

```xml
            <ContentControl x:Name="DemoSampleSlot09OutputContentHost" Visibility="Collapsed">
                <StackPanel>
                    <TextBlock
                        x:Name="ToggleButtonStateText"
                        Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                        Text="Wrap text: Off" />
                    <TextBlock
                        x:Name="ThreeStateToggleButtonStateText"
                        Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                        Text="Three-state: Off" />
                </StackPanel>
            </ContentControl>
```

- [ ] **Step 4: Add the handler and keep the displayed source in lockstep**

In `Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml.cs`, add next to the existing
`WrapToggleButton_CheckedChanged` handler near line 585:

```csharp
        private void ThreeStateToggleButton_StateChanged(object sender, RoutedEventArgs e)
        {
            ThreeStateToggleButtonStateText.Text = ThreeStateToggleButton.IsChecked switch
            {
                true => "Three-state: On",
                false => "Three-state: Off",
                _ => "Three-state: Indeterminate",
            };
        }
```

Then update the two source strings so the Source code tab matches the live sample. In the XAML source
string around lines 407 to 420, the three-state button block gains the three event attributes and the
output block becomes the two-line stack, mirroring Step 3 exactly. In the C# source string around lines
435 to 441, add the new handler after `WrapToggleButton_CheckedChanged`, written with the same
escaping style as its neighbours and with no `var`, because
`DemoSampleContractTests.GallerySamplePages_CSharpSourcesUseReleaseReadySnippetStyleAsync` rejects `var`
as a whole word in a displayed C# snippet.

- [ ] **Step 5: Run the tests to verify they pass**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.ToggleButtonTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Gallery.Pages.GalleryButtonsPageTests --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.ToggleButtonTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Gallery.Pages.GalleryButtonsPageTests --no-ansi --progress off
```

If `GalleryButtonsPageTests` does not exist, drop it from both commands.

- [ ] **Step 6: Visual verification**

Buttons page, Light and Dark. Click Three-state repeatedly through Off, On, Indeterminate.

What to look for: only On changes appearance, taking the accent fill and the `TextOnAccent` label. Off
and Indeterminate are pixel-identical, which is correct. The readout line is the only differentiator and
cycles through all three labels. Confirm the Wrap text readout still updates independently.

- [ ] **Step 7: Bookkeeping, gate and commit**

One new case. Increment both `.github/workflows/build.yml:88` values by 1, verified against
`--list-tests`. Regenerate all four baselines. Add to `CHANGELOG.md` under `### Changed`, adding that
heading under `## [Unreleased]` if it is not there yet:

```markdown
- Demo: the Buttons page three-state ToggleButton reports its current state. WinUI renders
  Indeterminate identically to Unchecked by design, so a readout is the only way to tell the two apart.
```

Closing gate, then stage by name and commit with subject:

```
Report the three-state toggle button state in the demo.
```

Body: records that WinUI aliases every `Indeterminate*` key to its unchecked counterpart at
`CommonStyles\ToggleButton_themeresources.xaml:15-18`, `:27-30` and `:39-42` in the Default block and
`:131-134`, `:143-146` and `:155-158` in Light, so two of the three states rendering identically is
correct rather than a defect, and that WinUI Gallery ships no three-state ToggleButton sample at all;
Fluence's `MultiTrigger`s on `IsChecked` `{x:Null}` already match, with rest deliberately untriggered
because the style's base setters are the Indeterminate palette; the change is therefore a demo readout
plus a parity-lock test.

---

### Task 7: Restructure the Colors page to the WinUI Gallery layout

Defect 5. Read the "Defect 5" section of the research first. Three facts shape the work: the page's tab
strip is already right (Fluence's implicit `TabItemStyle` gives the accent underline), the source viewer
already syntax-highlights XAML and the Colors page simply never routes through it, and the two
"demo sample controls" the spec names are actually two hand-rolled `fluence:Border` cards.

**Files:**
- Modify: `Fluence.Wpf.Demo/Pages/GalleryColorsPage.xaml:18-124`
- Modify: `Fluence.Wpf.Demo/Pages/GalleryColorsPage.xaml.cs:41-65,219-527,602-618`
- Modify: `Fluence.Wpf.Tests/Gallery/Pages/GalleryColorsPageTests.cs:136,145-169`
- Modify: the four `Fluence.Wpf.Tests/Baselines/` files, `.github/workflows/build.yml:88`, `CHANGELOG.md`
- Do not modify: `Fluence.Wpf.Demo/Pages/HcBrushEntry.cs`. It belongs to the Accessibility page.
- Do not modify: `Fluence.Wpf.Demo/Pages/DemoSampleControl.xaml.cs`. Highlighting already works.

**Interfaces:**
- Consumes: `DemoSamplePageWiring.Apply(FrameworkElement root, params DemoSampleSource[] sources)` and
  `DemoSampleSource(int slot, string xamlSource, string csharpSource)`
  (`Fluence.Wpf.Demo/Pages/DemoSamplePageWiring.cs:88-114`,
  `Fluence.Wpf.Demo/Pages/DemoSampleSource.cs:49-77`), and
  `DemoSampleXaml.UserControl(string className, string body)`
  (`Fluence.Wpf.Demo/Pages/DemoSampleXaml.cs:46-55`).
- Produces: the private helper `private static Brush TileForeground(Color background)` on
  `GalleryColorsPage`. The existing private records `ColorSection` and `ColorToken`
  (`GalleryColorsPage.xaml.cs:620-650`) keep their names and shapes. No public surface movement.

**Required brushes (from `.claude/skills/demo-sample-page/SPEC.md:64-68`):**

| Surface | Brush or token |
| --- | --- |
| Token row container | `SolidBackgroundFillColorBaseBrush` |
| Token row outline | `CardStrokeColorDefaultBrush`, 1 px |
| Token row corner | `OverlayCornerRadius` |
| Group example card | `SolidBackgroundFillColorQuarternaryBrush`, same outline |
| Accent group example card | `AccentFillColorDefaultBrush` |

- [ ] **Step 1: Update the two tests the restructure inverts, so they fail**

In `Fluence.Wpf.Tests/Gallery/Pages/GalleryColorsPageTests.cs`:

Line 136 currently asserts the page has no `DemoSampleControl`. Change it to:

```csharp
                _ = Assert.Single(DemoTestHost.FindVisualChildren<DemoSampleControl>(page));
```

Then add a new test to the same class, matching its `IAsyncLifetime` and `TestApp.EnsureDemoTheme()`
lifecycle at `:60-68`:

```csharp
        [Fact]
        public Task GalleryColorsPage_SwatchTilesAreFilledWithTheirOwnTokenAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryColorsPage(), static window =>
            {
                Application application = WpfTestSta.EnsureApplication();
                UniformGrid row = Assert.IsType<UniformGrid>(
                    DemoTestHost.FindVisualChildren<UniformGrid>(window).FirstOrDefault(), exactMatch: false);

                foreach (Border tile in row.Children.OfType<Border>())
                {
                    string key = Assert.IsType<string>(tile.Tag);
                    SolidColorBrush expected = Assert.IsType<SolidColorBrush>(application.FindResource(key));
                    SolidColorBrush actual = Assert.IsType<SolidColorBrush>(tile.Background);

                    // The tile IS the colour, as in the WinUI Gallery ColorTile
                    // (Controls\DesignGuidance\ColorTile.xaml:24-38), not a strip inside a card.
                    Assert.Equal(expected.Color, actual.Color);
                    Assert.Equal(new Thickness(0), tile.Margin);
                }
            });
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.Pages.GalleryColorsPageTests --no-ansi --progress off
```

Expected: the amended `Assert.Single` fails with 0 matches, and the new tile test fails on
`Assert.Equal(new Thickness(0), tile.Margin)` reporting `2,2,2,2`.

- [ ] **Step 3: Rebuild the group and tile construction in code-behind**

In `Fluence.Wpf.Demo/Pages/GalleryColorsPage.xaml.cs`, restructure so each group emits a card followed by
a gapless tile row, rather than nesting the rows inside the card. Target shape, per group:

```
StackPanel (child spacing 4, the WinUI ColorSectionSpacing from App.xaml:58)
├── Controls.Border  group card
│     Margin 0,36,0,8   Padding 12                       ColorPageExample.xaml:13-20
│     Background SolidBackgroundFillColorQuarternaryBrush (Text On Accent: AccentFillColorDefaultBrush)
│     BorderBrush CardStrokeColorDefaultBrush, 1          CornerRadius OverlayCornerRadius
│   └── StackPanel
│         ├── TextBlock  SubtitleTextBlockStyle   Tag "ColorExampleTitle"
│         ├── TextBlock  CaptionTextBlockStyle    Opacity 0.8
│         └── TextBlock  "Aa"  FontSize 42  SemiBold  centred  Margin 0,8,0,0
└── Controls.Border  tile row
      Tag "ColorTokenRow"   ClipToBounds true
      Background SolidBackgroundFillColorBaseBrush
      BorderBrush CardStrokeColorDefaultBrush, 1   CornerRadius OverlayCornerRadius
    └── UniformGrid  Rows 1  Columns N (N <= 4)
        └── N x Controls.Border  swatch tile
              Margin 0   Padding 12   Tag = token.ResourceKey
              Background = the token's own brush, via SetResourceReference
            └── Grid  rows Auto / Auto / (* MinHeight 30) / Auto,  columns * / Auto
                  ├── [0,0]   TextBlock  BodyStrongTextBlockStyle   e.g. "Text / Primary"
                  ├── [0..3,1] Controls.Button  subtle, FontIcon E8C8, RowSpan 4, top right,
                  │            Tag = resource key, ToolTip "Copy " + resource key
                  ├── [1,0]   TextBlock  CaptionTextBlockStyle  Opacity 0.8  e.g. "Rest or Hover"
                  └── [3,0..1] TextBlock  CaptionTextBlockStyle  DemoMonospaceFontFamily  = the key
```

Group and row shapes for the Text tab, matching `ColorSections\TextSection.xaml`:

| Group | Card fill | Row layout |
| --- | --- | --- |
| `Text` | `SolidBackgroundFillColorQuarternaryBrush` | one 4-column row: Primary, Secondary, Tertiary, Disabled |
| `Accent Text` | `SolidBackgroundFillColorQuarternaryBrush`, `Aa` in `AccentTextFillColorPrimaryBrush` | one 4-column row: Primary, Secondary, Tertiary, Disabled |
| `Text On Accent` | `AccentFillColorDefaultBrush`, `Aa` in `TextOnAccentFillColorPrimaryBrush` | two 2-column rows: Primary and Secondary, then Disabled and Selected Text |

Usage-note strings come from `WinUIGallery\Samples\Color\brushes.json`: `Rest or Hover`,
`Pressed only (not accessible)`, `Disabled only (not accessible)`,
`For highlighted text in text entry experiences`.

Do not copy the shipped WinUI Gallery bug at `TextSection.xaml:155` and `:179`, where the second tile of
a pair is placed at `Grid.Column="2"` in a two-column grid and collapses into column 1.

- [ ] **Step 4: Choose the tile foreground in code, never in XAML**

`GalleryColorsPageTests.cs:238-240` bans the literals `Foreground="Black"`, `Foreground="White"` and
`Foreground="#` anywhere in this page's source, so the polarity must be computed. There is no luminance
logic in the WinUI Gallery Color page to copy (every `ColorTile.Foreground` there is hard-coded per
tile), so mirror the in-tree rule at `Fluence.Wpf/Helpers/HsvColorHelper.cs:184-187`, which is `internal`
and therefore invisible to the demo assembly:

```csharp
        /// <summary>
        /// Picks black or white tile text by the weighted formula Windows uses, mirroring
        /// Fluence.Wpf.Helpers.HsvColorHelper.ShouldUseWhiteText, which is internal and so not
        /// visible to the demo assembly. Several tokens are translucent, so the caller composites
        /// the token over SolidBackgroundFillColorBaseBrush first: applying the threshold to the
        /// raw token colour picks the wrong polarity on the disabled and tertiary tiles.
        /// </summary>
        private static Brush TileForeground(Color background)
        {
            return ((5 * background.G) + (2 * background.R) + background.B) <= 1024
                ? Brushes.White
                : Brushes.Black;
        }

        /// <summary>
        /// Composites a possibly translucent token colour over the row surface so the luminance
        /// threshold sees what the viewer sees.
        /// </summary>
        private static Color CompositeOverRowSurface(Color token, Color rowSurface)
        {
            double alpha = token.A / 255.0;
            return Color.FromRgb(
                (byte)((token.R * alpha) + (rowSurface.R * (1 - alpha))),
                (byte)((token.G * alpha) + (rowSurface.G * (1 - alpha))),
                (byte)((token.B * alpha) + (rowSurface.B * (1 - alpha))));
        }
```

The swatch colours are theme and accent reactive, so recompute every tile's foreground after
`ApplicationThemeManager.Changed` and `ApplicationAccentColorManager.AccentColorChanged`. Subscribe in
`Loaded` and unsubscribe in `Unloaded`, never in the constructor: those managers are static and a
constructor subscription pins the page forever (AGENTS.md section 9).

- [ ] **Step 5: Collapse the two theme-dictionary cards into one `DemoSampleControl`**

Replace `Fluence.Wpf.Demo/Pages/GalleryColorsPage.xaml:59-124` with a hidden slot host plus one card:

```xml
            <ContentControl x:Name="DemoSampleSlot01DemoContentHost" Visibility="Collapsed">
                <StackPanel>
                    <!--  the ThemeDictionary resources and the live sample border move here verbatim from the old card at lines 59 to 95  -->
                </StackPanel>
            </ContentControl>
            <local:DemoSampleControl SampleDescription="ThemeDictionary and ThemeResource swap a value per theme without code." />
```

and wire it from the constructor with:

```csharp
            DemoSamplePageWiring.Apply(this, new DemoSampleSource(1, ThemeDictionaryXamlSource, ThemeDictionaryCSharpSource));
```

`ThemeDictionaryXamlSource` (currently `GalleryColorsPage.xaml.cs:41-65`) must become well-formed XML,
because `DemoSampleContractTests.cs:557-589` parses every `XamlSource` with `XDocument.Parse` and
requires a `UserControl` root carrying `x:Class`. Wrap the body with
`DemoSampleXaml.UserControl("Fluence.Wpf.Demo.Pages.Colors.ThemeDictionarySample", body)`. That wrapper
declares only the default, `x:` and `fluence:` namespaces
(`Fluence.Wpf.Demo/Pages/DemoSampleXaml.cs:48-52`), so the current snippet's `sys:` prefix has to go:
either declare `xmlns:sys` on the snippet's own root element or drop the `sys:String` entries in favour
of brush-only tables. Add a matching `ThemeDictionaryCSharpSource` containing `InitializeComponent();`
with no `var` as a whole word, both of which that same test file checks.

Routing the snippet through `DemoSampleControl` is what gives the page syntax highlighting: the
tokenizer at `DemoSampleControl.xaml.cs:561-619` already colours comments, quoted attribute values,
punctuation, attribute names and element names, all through `SetResourceReference` so it follows the
theme. Nothing needs adding to the shared viewer.

For the intro `{ThemeResource TextFillColorPrimaryBrush}` snippet at
`GalleryColorsPage.xaml:18-49`, keep the existing card but note in a comment that its one line stays a
plain `TextBlock` because the tokenizer methods are `private static` on `DemoSampleControl`. If the owner
wants that line highlighted too, that is a separate change to the shared control.

- [ ] **Step 6: Run the tests to verify they pass**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.Pages.GalleryColorsPageTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Gallery.DemoShellTests --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.Pages.GalleryColorsPageTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Gallery.DemoShellTests --no-ansi --progress off
```

These must keep passing unchanged and are the guard rails for the restructure:

- `:121-128` a `TabControl` named `ColorSectionTabs` with exactly `Text`, `Fill`, `Stroke`,
  `Background`, `Signal`, `High Contrast` in that order.
- `:130-133` the `"ColorExampleTitle"` tagged titles are exactly `Text`, `Accent Text`,
  `Text On Accent`.
- `:135` no `WrapPanel` anywhere on the page.
- `:145-156` every token row is a `UniformGrid` whose parent `Border` is tagged `"ColorTokenRow"`, with
  `SolidBackgroundFillColorBaseBrush` and `BorderThickness` 1.
- `:158-169` row child count equals `Columns`, `Columns` at most 4, each tile's `Tag` is its resource
  key.
- `:173-175` at least 90 tiles, including `SystemColorWindowTextColorBrush` and
  `AccentFillColorDefaultBrush`.
- `:197-218` at least 90 keys, all resolving in Light, Dark and High Contrast.
- `:234-244` the forbidden-string list.

Keep the current token counts so the 90 floors hold: Fill 28, Stroke 15, Background 23, Signal 13,
High Contrast 8 is 87, plus the Text tab's 12 group tiles and `TextFillColorInverseBrush` makes 100.

`GalleryColorsPage` is deliberately absent from `DemoSampleContractTests.SamplePageFactories`
(`:60-76`). Adding one `DemoSampleControl` does not require adding the page to that list, and it should
not be added.

- [ ] **Step 7: Visual verification**

Colors page, Dark and Light, each of the six tabs. Save a capture per tab per theme.

What to look for: tiles touch each other with no gap and no gap to the card above; the row's rounded
outline clips the outermost tiles; tile text is legible on every swatch including the translucent
disabled and tertiary ones; the `Text On Accent` group card is painted in the accent with its `Aa` in
`TextOnAccentFillColorPrimaryBrush`; the theme dictionary section is one card with one Source code
expander whose XAML tab is syntax highlighted; the selected tab carries the accent underline. Switch
theme while the page is open and confirm every tile's text polarity re-resolves. Switch the accent and
confirm the accent tiles follow.

- [ ] **Step 8: Bookkeeping, gate and commit**

One new case. Increment both `.github/workflows/build.yml:88` values by 1, verified against
`--list-tests`. Regenerate all four baselines. Add to `CHANGELOG.md` under `### Changed`:

```markdown
- Demo: the Colors page follows the WinUI Gallery Color page layout. Each group is a card on the
  quarternary surface above a gapless full-width row of swatch tiles that are filled with the colour
  itself, and the theme dictionary section is one sample card with one syntax-highlighted source view
  instead of two hand-rolled cards.
```

Closing gate, then stage by name and commit with subject:

```
Rebuild the Colors page on the WinUI Gallery layout.
```

Body: cites `WinUIGallery\Controls\DesignGuidance\ColorPageExample.xaml:13-20` for the group card's
`0,36,0,8` margin, 12 padding and `OverlayCornerRadius`; `:27-36` for the subtitle and caption styles;
`WinUIGallery\Styles\Grid.xaml:4-9` with `App.xaml:37-38` for the tile row surface resolving to
`SolidBackgroundFillColorBaseBrush` over `CardStrokeColorDefaultBrush`;
`Controls\DesignGuidance\ColorTile.xaml:24-38` for the tile grid and 12 padding, `:39-44` for the name,
`:46-64` for the copy button and `:75-81` for the key row; `Samples\Color\ColorPage.xaml:39-52` for the
six-item selector; `Samples\Color\brushes.json` for the usage notes; and `App.xaml:58` for the 4 unit
section spacing. Records that the WinUI Gallery has no luminance logic to copy, so the tile polarity
mirrors `Fluence.Wpf/Helpers/HsvColorHelper.cs:184-187` locally because that helper is internal, and
that the token is composited over the row surface first because several tokens are translucent.

---

### Task 8: Close the Icons page divergences from the WinUI Iconography page

Defect 6. Read the "Defect 6" section of the research first, because the spec is out of date here: the
two-column Iconography restructure already landed in commit `b6184f6`, and
`Fluence.Wpf.Tests/Gallery/Pages/GalleryIconsPageTests.cs` already locks its shape with five tests. What
remains is a list of measurable divergences, two of which the existing tests currently pin to the wrong
value.

**Files:**
- Modify: `Fluence.Wpf.Demo/Pages/GalleryIconsPage.xaml:18-83,110,112-123,125-135,207-230`
- Modify: `Fluence.Wpf.Demo/Pages/GalleryIconsPage.xaml.cs:50-51,206-228`
- Modify: `Fluence.Wpf.Tests/Gallery/Pages/GalleryIconsPageTests.cs:47,157`
- Modify: the four `Fluence.Wpf.Tests/Baselines/` files, `.github/workflows/build.yml:88`, `CHANGELOG.md`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: nothing new. The named parts `IconSearchBox`, `IconCatalogList`, `IconCatalogCard`,
  `IconDetailsPanel`, `IconPreviewGlyph`, `IconCatalogEmptyText`, the five value `TextBlock` names and
  the five copy `Button` names all keep their names, because `GalleryIconsPageTests` and
  `DemoShellTests` find them by name.

**Divergences to close, in this order:**

| # | Concern | Now | Target | WinUI citation |
| --- | --- | --- | --- | --- |
| 1 | Card surface brush | `:128` `CardBackgroundFillColorTertiaryBrush` | `SolidBackgroundFillColorBaseBrush` | `Styles\Grid.xaml:5` with `App.xaml:37` |
| 2 | Card radius | `:131` literal `CornerRadius="8"` | `{DynamicResource OverlayCornerRadius}` | `Styles\Grid.xaml:8` |
| 3 | Search box width | `:116` `Width="420"` | `MinWidth="304"` `MaxWidth="320"` | `IconographyPage.xaml:100-101` |
| 4 | Description | `:110` "Segoe Fluent Icons, the standard icon font on Windows 11." | "The icons below use Segoe Fluent Icons on Windows 11 and Segoe MDL2 Assets on Windows 10." | `SampleSupport\Data\ControlInfoData.json:309` |
| 5 | Tile size | 115 x 110, 12 px gutter (`:82`, `:21`, `.xaml.cs:50-51`) | 96 x 96, 8 px | `IconographyPage.xaml:45-46,125-126` |
| 6 | Glyph size | `:50` `FontSize="30"` | a 28 x 28 `Viewbox` with `Margin="0,0,0,16"` | `IconographyPage.xaml:57-60` |
| 7 | Selection ring | `:29-30` 2 px, radius 6 | 3 px at `ControlCornerRadius`, `AccentFillColorDefaultBrush` | WindowsAppSDK `generic.xaml:22737,22649,2750` |
| 8 | Details pane width | `:135` 361 | 334 | `IconographyPage.xaml:114` |
| 9 | Details glyph tile | `:216-229` fixed 90 x 90, `IconFontSize="40"` | auto size, `Padding="8"`, `IconFontSize="48"` | `IconographyPage.xaml:148-157` |
| 10 | Tag pills inert | `.xaml.cs:206-228` a `Border` | clicking a tag sets the search text | `IconographyPage.xaml:238`, `.xaml.cs:167-173` |

Divergence 1 is also a `.claude/skills/demo-sample-page/SPEC.md:68` violation, which is why it leads.

**Explicitly out of scope for this task:** curated tag data. `Fluence.Wpf.Demo/Resources/SegoeFluentIcons.tsv`
(1437 rows, declared at `Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj:25`) has three tab-separated columns
and no tags, so tags are derived from the PascalCase name at `GalleryIconsPage.xaml.cs:322-349` and
`Wifi` yields only `["wifi"]` against WinUI's eight. Porting `IconsData.json`'s tags is a data change
larger than the rest of this task combined, and search already matches on name and code. Also out of
scope: the `SymbolIcon XAML` and `SymbolIcon C#` detail rows (`IconographyPage.xaml:208-225`), because
WPF has no `Symbol` enum, and the `IsSegoeFluentOnly` caution row (`:159-172`), because the TSV carries
no such flag.

- [ ] **Step 1: Update the two tests that pin the wrong values, and add one**

In `Fluence.Wpf.Tests/Gallery/Pages/GalleryIconsPageTests.cs`:

Line 47, `Assert.Equal(420.0, search.Width, 0.1)`, becomes two assertions, because `Width` becomes `NaN`:

```csharp
                    Assert.Equal(304.0, search.MinWidth, 0.1);
                    Assert.Equal(320.0, search.MaxWidth, 0.1);
```

Line 157, the card background assertion, becomes:

```csharp
                    AssertBrushColor(catalogCard.Background, "SolidBackgroundFillColorBaseBrush");
```

Then add, matching the class's `CreateHostWindow` plus `try` / `finally` shape shown at `:141-188`:

```csharp
        [Fact]
        public Task GalleryIconsPage_TileGeometryMatchesWinUiIconographyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryIconsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Controls.ListView list = Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(page, "IconCatalogList"), exactMatch: false);
                    Button tile = Assert.IsType<Button>(FindVisualChildren<Button>(list).First(static b => b.DataContext is GalleryIconsPage.IconCatalogItem), exactMatch: false);

                    // WinUI's Iconography tile is a 96x96 ItemContainer on an 8px UniformGridLayout
                    // (IconographyPage.xaml:45-46, :125-126), with a 3px accent selection ring at
                    // ControlCornerRadius (WindowsAppSDK generic.xaml:22737, :22649).
                    Assert.Equal(96.0, tile.Width, 0.01);
                    Assert.Equal(96.0, tile.Height, 0.01);

                    Border ring = Assert.IsType<Border>(FindVisualChildByName<Border>(tile, "SelectionRing"), exactMatch: false);
                    Assert.Equal(new Thickness(3), ring.BorderThickness);

                    TextBlock description = Assert.IsType<TextBlock>(
                        FindVisualChildren<TextBlock>(page).First(static t => t.Text.StartsWith("The icons below", StringComparison.Ordinal)),
                        exactMatch: false);
                    Assert.Equal(
                        "The icons below use Segoe Fluent Icons on Windows 11 and Segoe MDL2 Assets on Windows 10.",
                        description.Text,
                        StringComparer.Ordinal);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.Pages.GalleryIconsPageTests --no-ansi --progress off
```

Expected: the amended search-box assertions fail (`MinWidth` is 0), the card background assertion fails
against the tertiary brush, and the new test fails on `Assert.Equal(96.0, 115.0)`.

- [ ] **Step 3: Fix the card surface, the search box and the description**

`Fluence.Wpf.Demo/Pages/GalleryIconsPage.xaml:125-135`:

```xml
            <Border
                x:Name="IconCatalogCard"
                Grid.Row="3"
                Background="{DynamicResource SolidBackgroundFillColorBaseBrush}"
                BorderBrush="{DynamicResource CardStrokeColorDefaultBrush}"
                BorderThickness="1"
                CornerRadius="{DynamicResource OverlayCornerRadius}">
```

`:112-123`, replace `Width="420"` with:

```xml
                MinWidth="304"
                MaxWidth="320"
                HorizontalAlignment="Left"
```

`:107-110`, the description text becomes:

```
The icons below use Segoe Fluent Icons on Windows 11 and Segoe MDL2 Assets on Windows 10.
```

`:135`, the details column width becomes `334`, and the details panel's `CornerRadius` becomes
`0,7,7,0` so it tucks inside the card's 8 px outer radius minus the 1 px stroke.

- [ ] **Step 4: Fix the tile geometry and the selection ring**

`Fluence.Wpf.Demo/Pages/GalleryIconsPage.xaml:18-83`, `IconTileButtonStyle`: `Width` 96, `Height` 96,
`Margin "0,0,8,8"`. Wrap the tile body in an outer `Border x:Name="SelectionRing"` with
`BorderBrush="Transparent"`, `BorderThickness="3"` and
`CornerRadius="{DynamicResource ControlCornerRadius}"`, flipped to
`{DynamicResource AccentFillColorDefaultBrush}` by the existing `IsSelected` `DataTrigger`. Inside it,
the existing card border keeps `CardBackgroundFillColorDefaultBrush` over
`CardStrokeColorDefaultBrush` at `ControlCornerRadius`, and the glyph moves into a
`Viewbox Width="28" Height="28" Margin="0,0,0,16"` with the caption below it at `Margin="8,0,8,8"`,
`CaptionTextBlockStyle`, `TextFillColorSecondaryBrush`, `TextTrimming="CharacterEllipsis"` and
`TextWrapping="NoWrap"`.

Match the layout constants in `Fluence.Wpf.Demo/Pages/GalleryIconsPage.xaml.cs:50-51`:

```csharp
        private const double TileWidth = 96.0;
        private const double TileGapWidth = 8.0;
```

`:216-229`, the details glyph tile drops its fixed 90 x 90 for `Padding="8"`,
`HorizontalAlignment="Left"` and `IconFontSize="48"`.

**Do not touch the virtualization.** The row batching at `.xaml.cs:256-271`, the
`VirtualizingStackPanel` at `:171-175`, the `IsVirtualizing`, `VirtualizationMode="Recycling"` and
`ScrollUnit="Pixel"` at `:149-151`, the stripped `ListViewItem` template at `:152-169` and
`KeyboardNavigation.TabNavigation="Once"` at `:144` are all load-bearing for a 1400-tile catalogue and
are pinned by `GalleryIconsPageTests.cs:166-181`. WPF ships no virtualizing wrap panel on either target
framework and AGENTS.md section 4.3 bars adding a dependency for one, so the row-batching workaround
stays. The column count recomputation at `.xaml.cs:115-124` must be re-checked against the new
`TileWidth` and `TileGapWidth`.

**Preserve `GalleryIconsPage.xaml.cs:173` verbatim:** `_ = _selectedIcon?.IsSelected = false;` is the
null-conditional-assignment-discard form the owner's Visual Studio analyzer configuration requires.

- [ ] **Step 5: Make the tag pills clickable**

WinUI's tags re-run the search (`IconographyPage.xaml:238`, `.xaml.cs:167-173`). Keep the code-built
`Border` pill at `Fluence.Wpf.Demo/Pages/GalleryIconsPage.xaml.cs:206-228` and its
`CornerRadius 12`, `MinHeight 24`, `Padding 10,2,10,3`, `Margin 0,0,4,4`; add
`Cursor = Cursors.Hand` and a `MouseLeftButtonUp` handler that sets `IconSearchBox.Text` to the tag:

```csharp
        private void IconTagPill_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement pill && pill.Tag is string tag)
            {
                IconSearchBox.Text = tag;
            }
        }
```

Set `pill.Tag = tag;` where the pill is built. This is lower risk than converting the pills to
`HyperlinkButton`s, which would need a keyed style to recover the 12 px chip look.

- [ ] **Step 6: Run the tests to verify they pass**

```
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.Pages.GalleryIconsPageTests Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.Pages.GalleryIconsPageTests Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests --no-ansi --progress off
```

Guard rails that must keep passing unchanged:

- `GalleryIconsPageTests.cs:43-48` one `"Iconography"` title, the exact search placeholder
  `Search icons by name, code, or tags`, and **zero** `DemoSampleControl` on the page. Icons is a direct
  catalog page (`.claude/skills/demo-sample-page/SPEC.md:66`).
- `:65` `IconCatalogList` is a `Controls.ListView` over `IEnumerable<IconCatalogRow>`.
- `:96-97` tiles are `System.Windows.Controls.Button` whose `DataContext` is an `IconCatalogItem`.
- `:112` `IconPreviewGlyph` is a `Controls.FontIcon`.
- `:130-135` the five value `TextBlock` names, the five copy `Button` names and their exact strings,
  including `<fluence:FontIcon Glyph="&#xE71F;" />`.
- `:161-164` `IconDetailsPanel` keeps `BorderThickness` `1,0,0,0` with
  `CardBackgroundFillColorDefaultBrush` and `DividerStrokeColorDefaultBrush`.
- `:166-181` the virtualization assertions.
- `DemoShellTests.cs:1282-1293` the Icons-only branch: a `Grid` named `PageRoot` with a null
  `Background`, and a `Grid` named `PageContent` whose `Style` is the same instance as
  `GalleryPageContentGridStyle`, with `Margin` `36,24,36,48`, `MaxWidth` `PositiveInfinity` and
  `HorizontalAlignment` `Stretch`.
- `DemoSampleContractTests.cs:60-76` deliberately omits `GalleryIconsPage`. Do not add it.

- [ ] **Step 7: Visual verification**

Icons page, Dark and Light. Save before and after captures.

What to look for: tiles are 96 x 96 on an 8 px grid, so more fit per row than before; the selected tile
carries a 3 px accent ring at the small corner radius; the card surface reads one step above the page
rather than as a tertiary card; the details pane is 334 wide with the glyph tile auto-sized around a
48 px glyph; the search box is 304 to 320 wide and left aligned; clicking a tag chip re-runs the search
for that tag. Scroll the catalogue hard and confirm it is still smooth: a virtualization regression here
is a multi-second hang, not a subtle one. Resize the window narrow and wide and confirm the column count
tracks the new 96 plus 8 metric with no clipped final column.

- [ ] **Step 8: Bookkeeping, gate and commit**

One new case. Increment both `.github/workflows/build.yml:88` values by 1, verified against
`--list-tests`. Regenerate all four baselines. Add to `CHANGELOG.md` under `### Changed`:

```markdown
- Demo: the Icons page matches the WinUI Gallery Iconography page's metrics. The catalogue sits on the
  base surface at the overlay corner radius, tiles are 96 px on an 8 px grid with a 3 px accent
  selection ring, the details pane is 334 px with a 48 px glyph, and tag chips re-run the search.
```

Closing gate, then stage by name and commit with subject:

```
Align the Icons page metrics with the WinUI Iconography page.
```

Body: records that the two-column restructure already landed in `b6184f6` and this closes the measured
divergences; cites `WinUIGallery\Styles\Grid.xaml:4-9` with `App.xaml:37-38` for the card surface,
`Samples\Iconography\IconographyPage.xaml:100-101` for the search box width, `:45-46` and `:125-126` for
the 96 px tile on an 8 px grid, `:57-60` for the 28 px glyph viewbox, `:114` for the 334 px details
column, `:148-157` for the details glyph tile, `:238` with `IconographyPage.xaml.cs:167-173` for the
clickable tags, `SampleSupport\Data\ControlInfoData.json:309` for the description, and WindowsAppSDK
`generic.xaml:22737` with `:22649` for the 3 px accent selection ring; records that curated tag data,
the `SymbolIcon` rows and the `IsSegoeFluentOnly` caution row are deliberately out of scope and why.

---

## Closing checklist for the owner

These are owner actions, not executor tasks.

- [ ] Confirm the four questions in `.superpowers/sdd/2026-09-06-gallery-visual-defects/research.md`,
      under "Questions only the owner can answer". Each has a recommended default that the tasks above
      already implement, so the run does not stall waiting on them.
- [ ] Confirm `PublicAPI.Unshipped.txt` and `Fluence.Wpf.Tests/Theming/golden/PublicKeys.txt` are
      unchanged across the whole branch:
      `git diff --stat 6394606 -- Fluence.Wpf/PublicAPI Fluence.Wpf.Tests/Theming/golden` must be empty.
- [ ] Run the full two-lane suite on both target frameworks before merging, per AGENTS.md section 6.
      Do not run it in the foreground of an agent session; it overruns the tool timeout.
- [ ] Review the Dark and Light captures under
      `.superpowers/sdd/2026-09-06-gallery-visual-defects/captures/`.
