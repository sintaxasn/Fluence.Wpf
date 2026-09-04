# Test suite consolidation, design spec

Date: 2026-09-04. Branch: `fix/mica-composition-defects`. Worktree: `.claude/worktrees/mica-defects`.

Input: the consolidation audit at `research-C-tests.md`. Every claim reused below was re-verified
against the working tree. Corrections to the audit are marked **[correction]**.

---

## 1. Summary and goals

- Retire `partial class ControlTests` (64 files, 2750 lines in the root partial alone) in favour of
  one sealed class per subject, grouped into five folders.
- Collapse seven divergent resource-merge helpers into one with explicit semantics, so a control
  behaves identically no matter which file tests it.
- Delete 18 test cases that are strictly subsumed or are not tests, taking net10 from 1188 to 1170
  cases. Nothing else is removed.
- Make every test class start from a known application and theme state, so no test depends on what
  ran before it.
- Cut net10 wall clock by at least 20 percent by removing duplicated fixture work, not by removing
  assertions.

### Non-goals

- No new parallelism. `ParallelMode.None`, `xunit.runner.json` and `TestTfmsInParallel=false` stay.
- No second test assembly. `InternalsVisibleTo("Fluence.Wpf.Tests")` is unchanged.
- No change to what any surviving test asserts, beyond the two theory folds listed in section 4.
- No fix for the net472 whole-assembly abort, and no fix for the known TimePicker flyout flake.
- No change to the screenshot harness gate or to the committed PNGs.

---

## 2. Target layout

### 2.1 Folders

```text
Fluence.Wpf.Tests/
  Infrastructure/   WpfTestSta.cs, TestApp.cs, VisualTree.cs, BrushAssert.cs,
                    ThemeTestHelpers.cs, DemoTestHost.cs, SlopwatchSuppressAttribute.cs
  Control/          <Control>Tests.cs, one per control
  Control/Shared/   IconForegroundTests.cs, FocusVisualTests.cs, ReducedMotionTests.cs,
                    BackgroundParityTests.cs, AccessibilityNameTests.cs, AutomationPeerTests.cs,
                    FluentStrokeTests.cs, PopupCornerRadiusTests.cs
  Theming/          existing files plus ThemeManagerTests, ThemeMetricsTests, ThemeMarkupTests,
                    DictionaryStabilityTests, TypographyResourceContractTests, AccentTests
  Windowing/        WindowPolicyTests.cs, FluenceWindowTests.cs, TitleBarTests.cs,
                    CaptionButtonTests.cs, NativeMethodsTests.cs, SnapLayoutHelperTests.cs
  Gallery/          DemoShellTests.cs, DemoSampleContractTests.cs, Pages/Gallery<Page>Tests.cs
  Tools/            GalleryScreenshotHarness.cs
```

Four adjustments to the folder set in the brief, all forced by evidence. Every folder name is also a
namespace segment (section 2.2), so each one is chosen not to shadow a name the tests already use.

**`Windowing/` not `Window/`.** A folder named `Window` invites a namespace segment named `Window`,
which shadows `System.Windows.Window` inside every file in it. The tests declare local windows as
`Window w = new()` in hundreds of places.

**`Control/` not `Controls/`.** A namespace segment `Controls` shadows `Fluence.Wpf.Controls` at the
1208 sites that write the `Controls.ProgressBar` shorthand. The singular form collides with no
namespace. It does shadow the type `System.Windows.Controls.Control` at nine sites, which section
2.2 lists and qualifies.

**`Gallery/` not `Demo/`.** A namespace segment `Demo` shadows `Fluence.Wpf.Demo` at the
`Demo.MainWindow` and `Demo.Mvvm.MainWindow` shorthand sites in `GalleryScreenshotHarness.cs` and
`ControlTests.NavigationView.cs`. `Gallery` is what the demo application is, and it collides with
nothing.

**`Tools/` as a seventh folder.** `GalleryScreenshotHarness.cs` is not a test. It writes files that
are committed to `docs/screenshots/`. Isolating it makes the opt-in gate auditable at a glance.

`Control/Shared/` exists because eight files assert a rule across many controls at once and have no
single `<Control>Tests.cs` home. `ControlTests.PeerSetValueGuards.cs` and
`ControlTests.PeerValueChanged.cs` merge into `AutomationPeerTests.cs`.

### 2.2 Namespaces

**Decision: every test file declares the namespace that matches its folder. Folder names are chosen
so that no namespace segment shadows a name the tests already use.**

This is a measured outcome, not a preference. The flat namespace the earlier draft of this section
chose does not compile: moving one file into `Infrastructure/` with `namespace Fluence.Wpf.Tests`
left in place fails on both TFMs with `IDE0130: Namespace "Fluence.Wpf.Tests" does not match folder
structure, expected "Fluence.Wpf.Tests.Infrastructure"`. `.editorconfig:32` sets
`dotnet_analyzer_diagnostic.category-Style.severity = error`, which escalates IDE0130 repo-wide, and
`.editorconfig` may not be edited.

| Folder | Namespace |
| ------ | --------- |
| `Fluence.Wpf.Tests/` root, which only files awaiting a move still occupy | `Fluence.Wpf.Tests` |
| `Infrastructure/` | `Fluence.Wpf.Tests.Infrastructure` |
| `Control/` | `Fluence.Wpf.Tests.Control` |
| `Control/Shared/` | `Fluence.Wpf.Tests.Control.Shared` |
| `Theming/` | `Fluence.Wpf.Tests.Theming` |
| `Windowing/` | `Fluence.Wpf.Tests.Windowing` |
| `Gallery/` | `Fluence.Wpf.Tests.Gallery` |
| `Gallery/Pages/` | `Fluence.Wpf.Tests.Gallery.Pages` |
| `Tools/` | `Fluence.Wpf.Tests.Tools` |

**The folder-naming rule.** A folder name must not equal the last segment of any `Fluence.Wpf.*`
namespace the tests reference by shorthand. Those namespaces are `Fluence.Wpf.Controls`,
`Fluence.Wpf.Demo`, `Fluence.Wpf.Helpers`, `Fluence.Wpf.Native`, `Fluence.Wpf.Markup`,
`Fluence.Wpf.Automation` and `Fluence.Wpf.Theming`, so the forbidden folder names are `Controls`,
`Demo`, `Helpers`, `Native`, `Markup` and `Automation`. `Theming` is the accepted exception: five
files already sit under `Theming/` in `Fluence.Wpf.Tests.Theming` today and compile, because no test
reaches for a `Theming.X` shorthand.

Why the rule bites. A test writes `Controls.ProgressBar` at 1208 sites and lets the enclosing
`Fluence.Wpf` namespace resolve it; inside a namespace `Fluence.Wpf.Tests.Controls` the name
`Controls` binds to the test namespace instead, and every one of those sites fails to compile. The
same argument retires `Demo/`: `GalleryScreenshotHarness.cs` and `ControlTests.NavigationView.cs`
write `Demo.MainWindow` and `Demo.Mvvm.MainWindow`, which a `Fluence.Wpf.Tests.Demo` namespace would
capture. `Control/` and `Gallery/` are the singular and descriptive forms, and they collide with
nothing.

**One residual shadow, handled explicitly.** `Control` is also a type name,
`System.Windows.Controls.Control`, and inside any namespace nested under `Fluence.Wpf.Tests` the
bare name `Control` now binds to the `Fluence.Wpf.Tests.Control` namespace. Nine sites in six files
use the bare type and are written out as `System.Windows.Controls.Control` when their file moves:
`ControlTests.DemoParity.cs:316`, `ControlTests.IconForeground.cs:599`,
`ControlTests.NavigationView.cs:1565`, `FluenceWindowHardenTests.cs:908` and `:930`,
`GalleryScreenshotHarness.cs:197`, and `TextRenderingPolicyTests.cs:320`, `:343` and `:357`. No
other folder name in the table shadows anything: `Gallery`, `Infrastructure`, `Pages`, `Shared`,
`Tools` and `Windowing` appear in this tree only inside comments and string literals.

**How the helpers are reached.** Files in `Infrastructure/` are consumed with
`using Fluence.Wpf.Tests.Infrastructure;`, plus
`using static Fluence.Wpf.Tests.Infrastructure.VisualTree;` and
`using static Fluence.Wpf.Tests.Infrastructure.BrushAssert;` where the walkers and the brush
assertions are called unqualified, exactly as before. `WpfTestSta` alone is named in 94 of the
project's files, so that one `using` reaches nearly every file in the suite. Cross-folder references
are otherwise rare, and each one is named in the task that creates it. A name in `Gallery/Pages/`
resolves a name in `Gallery/` through the enclosing namespace with no `using` at all, which is what
lets a page class call `DemoShellTests.CreateShownMainWindow()`.

`--filter-class` arguments grow a folder segment. The section 8 name diff is unaffected, because it
keys on the method name alone.

The test project uses SDK globbing with no `<Compile>` items, so folder moves need no project-file
edit.

### 2.3 From partial to sealed classes

Each `ControlTests.<X>.cs` becomes `Control/<X>Tests.cs` holding `public sealed class <X>Tests`.
The class body is the file's existing members with three changes: the private helper copies are
deleted, a `using static` line brings the shared helpers into scope unqualified so call sites do not
change, and per-test setup moves to `IAsyncLifetime`. The stragglers listed in the audit's mapping
table move into the matching new class.

### 2.4 Shared helpers

Helpers live in `Infrastructure/` as small single-concern static classes. `WpfTestSta` keeps its
current role and signatures unchanged, per AGENTS.md section 6. The new classes forward to it rather
than duplicating tree walks.

| Class | Member |
| ----- | ------ |
| `TestApp` | `internal static Application EnsureLibraryTheme(ApplicationTheme theme = ApplicationTheme.Light, BackdropType backdrop = BackdropType.None)` |
| `TestApp` | `internal static Application EnsureDemoTheme(BackdropType backdrop = BackdropType.None)` |
| `TestApp` | `internal static ResourceDictionary GenericDictionary(Application application)` |
| `VisualTree` | `internal static T? FindVisualChild<T>(DependencyObject? root) where T : DependencyObject` |
| `VisualTree` | `internal static T? FindVisualChildByName<T>(DependencyObject? root, string name) where T : FrameworkElement` |
| `VisualTree` | `internal static DependencyObject? FindVisualChildByTypeName(DependencyObject? root, string typeName)` |
| `VisualTree` | `internal static IEnumerable<T> FindVisualChildren<T>(DependencyObject? root) where T : DependencyObject` |
| `VisualTree` | `internal static void CloseWindowAndDrain(Window window)` |
| `BrushAssert` | `internal static void AssertBrushColor(Brush? actual, string expectedResourceKey)` |
| `BrushAssert` | `internal static Color ResolvedColor(Application application, string resourceKey)` |

`VisualTree.FindVisualChildren` forwards to `WpfTestSta.FindVisualDescendants`. `DemoTestHost`
already forwards its own walker to `WpfTestSta.FindLogicalAndVisualDescendants` and keeps doing so.
Names are unchanged from the current private copies so the call-site diff stays mechanical: each
file gains `using Fluence.Wpf.Tests.Infrastructure;` and
`using static Fluence.Wpf.Tests.Infrastructure.VisualTree;`, and loses its private copy.

### 2.5 Method naming

Existing convention, codified: `Subject_Scenario_ExpectedOutcome`, with an `Async` suffix if and
only if the method returns `Task`. Examples in the tree: `ProgressBar_Track_FollowsBackgroundWith
HalfPixelCornerRadiusAsync` and the synchronous `BuildBackdropPlan_None_ReturnsOpaqueBackground`.
Method names do not repeat the class name where the class already names the subject, except where a
name is being preserved verbatim for the section 8 diff.

---

## 3. Helper unification

### 3.1 The seven copies

**[correction]** The audit lists six. There are seven. `ControlRenderingTests.cs:39
MergeThemeAndGeneric` was missed.

| Site | `Resources.Clear()` | Merges `DemoSharedStyles.xaml` | Returns |
| ---- | ------------------- | ------------------------------ | ------- |
| `ControlTests.cs:85` (61 partials) | yes | yes | last merged dictionary |
| `ControlRenderingTests.cs:39` | no | yes | void |
| `ListViewIsItemSelectableTests.cs:39` | no | yes | last merged dictionary |
| `TitleBarTests.cs:295` | yes | no | last merged dictionary |
| `TabViewTests.cs:44` | no | no | last merged dictionary |
| `ComboBoxTests.cs:40` (`MergeTheme`) | no | no | last merged dictionary |
| `FluenceWindowTitleBarTests.cs:50` (`MergeTheme`) | no | no | last merged dictionary |

### 3.2 The single semantics

`TestApp.EnsureLibraryTheme` does, in order: `WpfTestSta.EnsureApplication()`; close every open
window and null its content; `WpfTestSta.DrainDispatcher`; `ApplicationThemeManager.ResetForTesting()`;
`ApplicationAccentColorManager.ResetForTesting()`; `Resources.MergedDictionaries.Clear()`;
`Resources.Clear()`; `ApplicationThemeManager.Apply(theme, backdrop, updateAccent: true)`. It returns
the `Application`.

**It does not merge the demo dictionary.** A library control test must assert the library's own
template and brushes. Merging `DemoSharedStyles.xaml` lets a demo style shadow a library brush, which
is exactly why the same control passes in one file and could fail in another today.

`TestApp.EnsureDemoTheme` is the explicit opt-in. It does everything `EnsureLibraryTheme` does, then
`ApplicationAccentColorManager.ApplySystemAccent()`, then adds `DemoSharedStyles.xaml`. This is the
behaviour `DemoTestHost.EnsureDemoTheme` already has, so `DemoTestHost.EnsureDemoTheme` becomes a
one-line forward to `TestApp.EnsureDemoTheme` and `DemoTestHost.AddDemoSharedStyles` is removed.

Demo tests request the demo dictionary by calling `EnsureDemoTheme`. Nothing else calls it. Any test
outside `Gallery/` that needs it must say so at its own call site with a comment naming the demo style
it depends on.

The `dictionaries[^1]` idiom disappears. Callers that need the Generic dictionary use
`TestApp.GenericDictionary(application)`, which asserts the three-slot invariant and returns slot
`[2]`. The `finally { MergedDictionaries.Remove(genericDictionary) }` blocks are deleted: the next
test's reset clears the collection anyway, and the removal is a source of ordering bugs.

### 3.3 Per-test isolation

Every test class whose tests touch `Application`, application resources, or a `Window` implements
`IAsyncLifetime`. `InitializeAsync` calls `TestApp.EnsureLibraryTheme()` or `EnsureDemoTheme()` on
the STA thread through `WpfTestSta.RunOnStaAsync`. `DisposeAsync` closes any window the class opened
and drains. Test bodies no longer call a merge helper themselves.

Classes with no such tests do not implement it. `WindowPolicyTests` (89 tests, 0.01 s total),
`CaptionButtonChromeTests`, `SnapLayoutHelperTests`, `NativeMethodsTests` and `AccentRampTests` are
pure logic and must stay free.

This is what removes ordering dependence. **[correction]** The audit's risk 3 overstates the current
problem: `DictionaryStabilityTests`, `ThemeManagerTests` and `Theming/RedundantPublishGateTests`
already reset in `InitializeAsync`. What they do not do is re-apply a theme, so their bodies apply
one first. Under the new rule they inherit a Light theme and their bodies keep their explicit applies.

`ControlTests.ReducedMotion.cs` mutates `MotionHelper.OverrideIsMotionEnabled`. That reset moves out
of each test's `finally` and into the class `DisposeAsync`, so an escaping exception cannot poison
later animation tests.

---

## 4. Deletions

All seven duplicate groups were re-read and confirmed against the tree.

| ID | Delete | Duplicates | Why no coverage is lost |
| --- | ------ | ---------- | ----------------------- |
| D1 | `ThemeMetricsTests.cs:56,68,80,96,108,120` (6) | `ThemeMetricsTests.cs:187` | The cycle test applies Light, Dark, HighContrast, Light and asserts `ControlCornerRadius == 4` and `OverlayCornerRadius == 8` at every step. Each deleted test asserts one of those pairs at one theme. |
| D2 | `ThemeMetricsTests.cs:169` (1) | `ControlTests.FocusVisual.cs:51` | Same loop over the three themes, same `Assert.IsType<Style>` on `DefaultControlFocusVisualStyle`. Only the reset helper differs. After section 3.2 both run without demo styles, so the survivor covers the same resource state. |
| D3 | `ControlTests.BackgroundParity.cs:114` (1) | `ControlTests.ProgressBar.cs:569` | Both resolve `PART_Track` on a 240x24 ProgressBar and compare its background to `ControlStrongStrokeColorDefaultBrush`. The survivor compares the brush instance, not just the colour, and also asserts `progressBar.Background` and `CornerRadius(0.5)`. |
| D4 | `ThemeManagerTests.cs:150`, `FluenceWindowTitleBarTests.cs:471` (2) | `DictionaryStabilityTests.cs:60` | All three assert `MergedDictionaries.Count` is unchanged after N Light/Dark applies. The survivor does 20 switches against 5. The three differ only in `updateAccent`, so the survivor becomes a `[Theory]` with `[InlineData(false)]` and `[InlineData(true)]`, keeping both flag values covered. Net case change is 3 to 2. |
| D5 | `FluenceWindowHardenTests.cs:223` (1) | `WindowPolicyTests.cs:379` | Identical `BuildBackdropPlan` call. The deleted copy asserts only `NotEqual(Transparent, plan.BackgroundColor)`; the survivor asserts that plus `EffectiveBackdrop`, the exact fallback colour, `CaptionColor` and `SystemBackdropType`. |
| D6 | `FluenceWindowHardenTests.cs:240` (1) | `WindowPolicyTests.cs:438` | Same capability shape and request. The deleted copy asserts only `Equal(Transparent, plan.BackgroundColor)`. |
| D7 | `ControlTests.cs:2056` (1) | `ControlTests.DemoSamplePolish.cs:379` | Both set `ProgressValueNumberBox` to 73 and assert `StandardProgressBar.Value == 73`. The survivor adds alignment, min/max, zero-value and toggle assertions. The deleted copy also builds the whole `MainWindow` and navigates, which `DemoMainWindowTests.cs:70` already covers. |
| D8 | `AccentPaletteRegenerationExperiment.cs` (3), `ImmersiveColorSetProbe.cs` (1) | none | Both are `[Fact(Explicit = true)]` probes whose own doc comments record the answer they were written to find, dated 2026-05-23. Never run in CI. Delete both files. |
| D9 | `AccentRampScoreboard.cs:153` (1) | none | Not a test. Its only assertion is `Assert.True(Fixtures.Length > 0)`. The captured OS ramp fixtures move verbatim into a comment block at the top of `Theming/AccentTests.cs`; the scoring code is deleted. |

`FluenceWindowHardenTests.cs:256` `BuildBackdropPlan_Acrylic_FallsBackToMica_...` has no equivalent
in `WindowPolicyTests.cs`. It moves, it is not deleted.

Total deleted: 18 cases. Net10 goes from 1188 to 1170 cases, and from 1146 to 1128 test methods
before the theory folds. `NotRunnable` goes from 5 to 1; the surviving Explicit case is
`Theming/DesignTimeResourceTests.cs:118`, the maintainer-only DesignTime writer, which stays.
`NotExecuted` stays at 3 (the screenshot harness).

### Theory folds

Two folds land with the deletions. Both preserve case count.

| Fold | Sites | Result |
| ---- | ----- | ------ |
| `DefaultCollectionFocusVisualStyle_PresentIn{Light,Dark,HighContrast}ThemeAsync` | `ThemeMetricsTests.cs:210,221,232` | one `[Theory]`, three `[InlineData]`, 3 cases |
| D4 survivor accent flag | `DictionaryStabilityTests.cs:60` | one `[Theory]`, two `[InlineData]`, 2 cases |

The two larger folds the audit proposes (the 19 `Stage3_*` DP defaults and the 19 theme-cycle smoke
tests) are **out of scope**. They change method names at 38 sites, which makes the section 8 name
diff unreadable, and they save no wall clock. They are recorded as a follow-up.

---

## 5. Non-test files

| File | Decision | Reason |
| ---- | -------- | ------ |
| `AccentPaletteRegenerationExperiment.cs` | Delete | Dead experiment. Its own doc comment at line 102 records the answer, dated 2026-05-23. Mutates the user's system accent. |
| `AccentRampScoreboard.cs` | Delete the code, keep the fixtures | The scoring harness compares four candidate ramp algorithms and cannot fail. The eight captured OS ramp fixtures are real measurements and are worth keeping, so they move into a comment block in `Theming/AccentTests.cs`. |
| `ImmersiveColorSetProbe.cs` | Delete | Dead probe. Answer recorded in its doc comment at line 72. |
| `GalleryScreenshotHarness.cs` | Move to `Tools/`; only the namespace line, one added `using Fluence.Wpf.Tests.Infrastructure;` and the qualified `System.Windows.Controls.Control` at line 197 change | Nothing touches the three `[Fact(SkipUnless = nameof(ScreenshotCaptureEnabled))]` attributes or the class-level `[Trait("Category", "Screenshots")]`. Verified by asserting the run still reports 3 `NotExecuted`. |
| `DemoTestHost.cs` | Move to `Infrastructure/`, namespace changes, two members change | `EnsureDemoTheme` forwards to `TestApp.EnsureDemoTheme`; `AddDemoSharedStyles` is removed. The window helpers and repo file readers are unchanged. |
| `WpfTestSta.cs` | Move to `Infrastructure/`, only the namespace line changes | It is the canonical STA fixture named in AGENTS.md section 6. Changing its members during a layout refactor would make every failure ambiguous. Its 93 consumers gain `using Fluence.Wpf.Tests.Infrastructure;`. |
| `ThemeTestHelpers.cs` | Move to `Infrastructure/`, only the namespace line changes | `ApplyStandardThemeCycle` has 19 call sites and `AssertKeyThemeBrushesResolve` has 2. Both stay. |
| `ThemeTestHelpersTests.cs` | Move to `Theming/`, namespace becomes `Fluence.Wpf.Tests.Theming` | Two tests that cover a helper. They belong beside the theming tests, not at the project root. |
| `SlopwatchSuppressAttribute.cs` | Move to `Infrastructure/`, only the namespace line changes | One use, at `Theming/DesignTimeResourceTests.cs:117`, which gains the `using`. |
| `Theming/DesignTimeResourceWriter.cs` | Stays where it is | Already correctly placed. |
| `Fluence.Wpf.Tests/README.md` | Rewrite in the final phase | Section 9. |

---

## 6. Wall clock

### 6.1 What can be shared

Under `ParallelMode.None` with one STA thread, xunit.v3 fixtures are safe but their constructors run
on the runner thread, so every fixture that touches WPF must route through `WpfTestSta.RunOnStaAsync`.

**Shareable, via `IClassFixture<LightThemeFixture>`.** A class qualifies if no test in it applies a
theme, changes the accent, or toggles `MotionHelper.OverrideIsMotionEnabled`. Those classes pay one
reset and one Light apply per class instead of one per test. Most `Control/` classes qualify.

**Shareable, via one host window per class.** The `Gallery/Pages/Gallery<Page>Tests.cs` classes build
their page once in `InitializeAsync` and reuse it, because their assertions read layout and brushes
rather than mutating the tree. Where a page test does mutate (the Status page NumberBox drive), that
test rebuilds the page itself.

**Not shareable.** Any class that applies a theme, mutates the accent intent, toggles reduced motion,
or drives input keeps per-test `IAsyncLifetime`. That is all of `Theming/`, `Control/Shared/
ReducedMotionTests.cs`, and the interaction-heavy control classes (ContentDialog, DatePicker,
TimePicker, ColorPicker, CommandBarFlyout, TeachingTip, PipsPager).

**Not available at all.** Parallelism. Restructuring cannot buy it, and the spec does not try.

The largest single win is not a fixture at all: today each gallery page is constructed and shown by
two different source files (`ControlTests.DemoParity.cs` or `ControlTests.DemoSamplePolish.cs`, and
`DemoMainWindowTests.cs` or `DemoColorsPageTests.cs`). One file per page halves that.

Target: at least 20 percent off the net10 wall clock of 3 min 29 s. The audit's 30 to 40 percent is
the optimistic end. Phase 7 measures; a phase that increases wall clock is treated as a defect.

### 6.2 How the suite is invoked

The net472 whole-assembly run aborts with exit -1 at a non-deterministic point, twice reproduced.
This spec does not fix it and does not depend on it being fixed. It gets a `KNOWN_ISSUES.md` entry
with both reproductions.

**Decision: both TFMs run in two complementary lanes, using only `--filter-class` and
`--filter-not-class`, which the audit proved work.** The lanes are complements of the same explicit
class list, so their union is provably the whole assembly and a newly added class lands in lane B
automatically rather than going unrun.

Lane A is the heavy set. After consolidation the current split on `ControlTests` no longer exists,
so the list is the seven costliest classes, each named with the folder-matching namespace of section
2.2: `Fluence.Wpf.Tests.Gallery.DemoShellTests`,
`Fluence.Wpf.Tests.Gallery.DemoSampleContractTests`,
`Fluence.Wpf.Tests.Control.NavigationViewTests`, `Fluence.Wpf.Tests.Control.ProgressBarTests`,
`Fluence.Wpf.Tests.Control.ContentDialogTests`, `Fluence.Wpf.Tests.Control.ColorPickerTests`,
`Fluence.Wpf.Tests.Control.TimePickerTests`.

```pwsh
Fluence.Wpf.Tests\bin\Debug\<tfm>\Fluence.Wpf.Tests.exe ^
  --filter-class Fluence.Wpf.Tests.Gallery.DemoShellTests ... (seven) ^
  --report-xunit-trx --results-directory <dir> --no-ansi --progress off

Fluence.Wpf.Tests\bin\Debug\<tfm>\Fluence.Wpf.Tests.exe ^
  --filter-not-class Fluence.Wpf.Tests.Gallery.DemoShellTests ... (the same seven) ^
  --report-xunit-trx --results-directory <dir> --no-ansi --progress off
```

CI sums the two TRX case counts per TFM and fails if the total is not the expected number. Both
lanes keep the existing `Screenshots` category exclusion. `.github/workflows/build.yml` replaces its
two `dotnet test` steps with four exe invocations plus the sum check.

---

## 7. Migration approach

One folder or concern per commit. The build, `dotnet format --verify-no-changes --severity info
--no-restore`, and `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll` must pass at the end
of every phase. Every phase runs both TFMs in the two lanes of section 6.2 and compares case counts
against the expected number for that phase.

| Phase | Content | Expected net10 cases | Verification |
| ----- | ------- | -------------------: | ------------ |
| 0 | Capture the `--list-tests` baseline per TFM. Move one file into `Infrastructure/` with the matching namespace `Fluence.Wpf.Tests.Infrastructure`, adding the `using` its consumers need. | 1188 | Build clean, format clean. Confirms the section 2.2 namespace decision on the real tree. |
| 1 | Add `Infrastructure/TestApp.cs`, `VisualTree.cs`, `BrushAssert.cs`. Delete the seven merge copies and the private walkers. Add `using static` per file. No file moves, no class changes. | 1188 | Name diff empty. |
| 2 | Switch the 61 former demo-styles callers to `EnsureLibraryTheme`. Land alone. | 1188 | Name diff empty. Any new failure is listed in the commit message with its cause. |
| 3 | Retire the partial. One commit per control family: `ControlTests.<X>.cs` becomes `Control/<X>Tests.cs`, `sealed`, in `Fluence.Wpf.Tests.Control`, with `IAsyncLifetime`. `ControlTests.cs` is emptied last. | 1188 | Name diff empty, class map reviewed. |
| 4 | `git mv` the remaining files into `Theming/`, `Windowing/`, `Gallery/`, `Tools/`. Content unchanged except each file's namespace line, the `using Fluence.Wpf.Tests.Infrastructure;` it then needs, and the qualified `System.Windows.Controls.Control`. One folder per commit. | 1188 | Name diff empty. Screenshot skips still 3. |
| 5 | The 18 deletions and the two theory folds. One commit. `CHANGELOG.md` entry in the same commit. | 1170 | Name diff equals exactly the allowlist in section 8. |
| 6 | `LightThemeFixture` and the per-page host windows. | 1170 | Name diff empty. Wall clock measured. |
| 7 | Docs, `KNOWN_ISSUES.md`, CI workflow. | 1170 | Full two-lane pass on both TFMs. |

Phase 1 must precede every move, because the partial's private helpers are what the split would
otherwise break: `MergeGenericDictionary` (61 consumers), `FindVisualChildByName` (40),
`CloseWindowAndDrain` (17, defined in the NavigationView file) and `RunDemoPageTestAsync` (22
consumers, defined in a different file from all of them).

---

## 8. Testing the tests

Before phase 0 and after each phase, per TFM:

```pwsh
Fluence.Wpf.Tests\bin\Debug\<tfm>\Fluence.Wpf.Tests.exe --list-tests --no-ansi > <phase>.<tfm>.txt
```

The diff key is the **method name alone**, not the fully qualified name. Classes are merged,
renamed, and given folder-matching namespaces by this work, so fully qualified names change by
design; method names do not. Class renames therefore stay allowed at every phase, and only a changed
method-name multiset can fail one.
Extract them with a sort and a suffix strip, and compare the multisets.

Two artefacts are committed under `Fluence.Wpf.Tests/Baselines/` (text files only, so the SDK compile glob ignores them): the baseline list per TFM, and an allowlist of
the method names that are permitted to disappear or appear. The allowlist has exactly 18 removals
(section 4), plus 6 removals and 2 additions for the two theory folds, and nothing else. Any other
difference fails the phase.

Case counts are read from the TRX per lane and summed. Expected totals: 1188 for phases 0 to 4, 1170
for phases 5 to 7, on net10. On net472 the same method-name multiset must hold; its case count is
compared lane by lane rather than as a single whole-assembly number, because the whole-assembly run
aborts.

Duplicate method names across classes are handled by keying on `class.method` for the subset of
names that are not unique, and by listing those pairs in the allowlist header.

---

## 9. Documentation updates

| File | Change |
| ---- | ------ |
| `AGENTS.md` section 6 | Replace the `MergeGenericDictionary(Application.Current.Resources)` step with `TestApp.EnsureLibraryTheme()` and the demo opt-in. Describe the folder layout and the folder-matching namespaces. Replace the `ControlTests.FluentStroke.cs` reference-pattern pointer with `Control/Shared/FluentStrokeTests.cs`. Add the two-lane invocation and the net472 abort. |
| `AGENTS.md` section 6, parallelization bullet | **[correction]** It states `[assembly: CollectionBehavior(DisableTestParallelization = true)]`. The file actually carries `[assembly: Parallelization(Mode = ParallelMode.None)]` at `Properties/AssemblyInfo.cs:32`. Fix the text. |
| `AGENTS.md` section 9 | Update the "relying on a previous test's theme state" pitfall: the fix is now the class `IAsyncLifetime`, not a call in the test body. |
| `AGENTS.md` section 13.2 | If `demo-sample-page/SPEC.md` names test file paths, update them. |
| `docs/contributing.md` line 9 | Same `CollectionBehavior` correction. |
| `docs/contributing.md` lines 24 to 31 | Replace "drop new test files alongside existing ones as partial extensions of `public partial class ControlTests`" with the one sealed class per control rule and the folder map. Update the `ControlTests.FluentStroke.cs` pointer. |
| `docs/contributing.md` line 6 | Replace `dotnet test` with the exe invocation, matching the memory note that the VSTest bridge is gone. |
| `Fluence.Wpf.Tests/README.md` | Rewrite the "What Lives Here" list against the new folders. Same `CollectionBehavior` correction. Replace both `dotnet test` commands. |
| `.github/PULL_REQUEST_TEMPLATE.md` lines 8 and 9 | Replace the two `dotnet test` lines with the four lane invocations. Line 15's baseline sentence stays. |
| `CHANGELOG.md` | One entry in phase 5 listing all 18 deletions with their justification, as AGENTS.md section 6 requires. |
| `KNOWN_ISSUES.md` | The net472 whole-assembly abort, with both reproductions, and the TimePicker net472 flyout flake. |

---

## 10. Risks and mitigations

| Risk | Mitigation |
| ---- | ---------- |
| Splitting the partial breaks 140 helper call sites at once. | Phase 1 lifts every shared helper first, with names unchanged and `using static` at each call site, so phase 3 is a pure class-shape change. |
| Dropping demo styles from library tests surfaces new failures in 61 files. | Phase 2 lands alone. Each failure is triaged as either a real library brush shadowed by a demo style, which is a bug worth having found, or a test that genuinely needs the demo dictionary, which moves to `EnsureDemoTheme` with a comment. Both outcomes are listed in the commit message. |
| IDE0130 rejected the flat namespace, so every folder name became a namespace segment. | Recorded outcome, not an open risk. The phase 0 probe moved `WpfTestSta.cs` into `Infrastructure/` with the namespace left flat, and both TFMs failed with IDE0130, which `.editorconfig:32` escalates to an error. Section 2.2 now specifies folder-matching namespaces plus a folder-naming rule that keeps the `Controls.X` and `Demo.X` shorthands resolving, and it qualifies the nine bare `Control` sites the `Control/` folder would otherwise shadow. |
| Deleting a test that happened to seed global state for a later test. | Section 3.3 makes every stateful class reset in `InitializeAsync`, and that lands in phases 1 to 3, before the phase 5 deletions. |
| net472 lanes hide a regression because the whole assembly never runs. | The two lanes are exact complements, so their union is the whole assembly, and CI sums and checks the case counts. |
| Screenshot harness starts running and overwrites committed PNGs. | The move is `git mv` only. Every phase asserts the run still reports 3 `NotExecuted`. |
| Case-count drift goes unnoticed across seven phases. | Section 8 runs the name diff at every phase against a committed allowlist, not just at the end. |
| The TimePicker net472 flake is read as a consolidation regression. | It is recorded in `KNOWN_ISSUES.md` in phase 7 and named in every phase report. |
| The demo project stays a test dependency, so `Gallery/` is not an isolation boundary. | Accepted. A library-only lane would need a second assembly and a second `InternalsVisibleTo`, which is a non-goal. |
| Fixture sharing in phase 6 introduces cross-test bleed. | A class qualifies for `LightThemeFixture` only if no test in it applies a theme, changes the accent, or toggles reduced motion. The qualifying list is reviewed per class, and phase 6 is reverted wholesale if the name diff or counts move. |
