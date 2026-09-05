# Test Suite Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retire `partial class ControlTests`, collapse the seven divergent resource-merge helpers into one, delete 18 subsumed test cases, and make every test class start from a known application and theme state, without changing what any surviving test asserts.

**Architecture:** Seven folders under `Fluence.Wpf.Tests` (`Infrastructure`, `Control`, `Control/Rules`, `Theming`, `Windowing`, `Gallery`, `Tools`), each one a namespace segment, because IDE0130 is an error in this repository and a flat namespace under a subfolder does not compile. Three new single-concern static helper classes (`TestApp`, `VisualTree`, `BrushAssert`) replace every private copy, and each test class that touches WPF resets through `IAsyncLifetime`. The work lands in eight phases, each verified by a `--list-tests` method-name multiset diff against a committed baseline.

**Tech Stack:** C# with `LangVersion=latest`, xunit.v3 4.0.0 on Microsoft Testing Platform (no `dotnet test`), WPF on `net472` and `net10.0-windows10.0.26100.0`, PowerShell 7 for tooling.

**Spec:** `docs/superpowers/specs/2026-09-04-test-suite-consolidation-design.md`

---

## Global Constraints

Every task's requirements implicitly include this section. Read it before Step 1 of any task.

### Executor preamble (Step 1 of every task except Task 1 and Task 29)

Executors are subagents whose worktree is created from `main` by the harness. Run these four commands, one per tool call, as plain single invocations. Never use `git -C`. Never write a compound shell line that mentions git. Never put a loop or a shell variable inside a git command. Never push. Never add a commit trailer.

```
git branch --show-current
```
Expected: `main`.

```
git reset --hard HEAD
```
Expected: `HEAD is now at <sha> <subject>`. This discards any harness scaffolding in the fresh worktree.

```
git checkout refactor/test-suite-consolidation
```
Expected: `Switched to branch 'refactor/test-suite-consolidation'`.

```
git log --oneline -1
```
Expected: the commit named in that task's **Starting commit** line. If it does not match, stop and report; do not proceed.

Task 1 creates the branch, so its Step 1 differs. Task 29 runs in the primary worktree `F:\Consolidation\Fluence.Wpf\.claude\worktrees\mica-defects`, which is already on `fix/mica-composition-defects`, so its Step 1 differs too. Both are written out in full in their own task.

### Repository rules copied from AGENTS.md

- **Analyzers are errors.** `TreatWarningsAsErrors=True`, `WarningLevel=9999`, `AnalysisLevel=latest-all`, `EnforceCodeStyleInBuild=true`, plus SonarAnalyzer, Meziantou (`all-errors`), Roslynator and `BannedApiAnalyzers`. Fix root causes. Never add `#pragma warning disable`. Never edit `.editorconfig`, `Directory.Build.props` or `BannedSymbols.txt`.
- **Banned API.** `string.IsNullOrEmpty` is banned (RS0030); use `string.IsNullOrWhiteSpace`.
- **Style rules that bite in this suite.** Explicit types, not `var`. Target-typed `new()`. Discard ignored return values with `_ =` (CA1806 / IDE0058). `default`, not `default(T)`. `is not null` patterns. No redundant `using` directives, because IDE0005 is an error, so do not add a `using static` that a file does not use.
- **File header.** Every `.cs` file starts with the same 27-line BSD 3-Clause header. Copy it verbatim from `Fluence.Wpf.Tests/WpfTestSta.cs` lines 1 to 27.
- **Text policy.** All `.cs`, `.md`, `.csproj`, `.props`, `.yml` files: UTF-8 with BOM (first three bytes `EF BB BF`), LF line endings, final newline, no trailing whitespace. No em dash or en dash anywhere in `.cs` or `.md`. Do not substitute a spaced hyphen where it would create a spurious Markdown list item; reflow the sentence instead.
- **Build.** `dotnet build Fluence.Wpf.sln -c Debug` must report `0 Warning(s)` and `0 Error(s)`.
- **Format.** `dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore` must pass.
- **Text policy gate.** `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll` must pass. The `-NoProfile` matters; it hangs without it. It scans tracked files only, so byte-check any new file yourself before committing.
- **Commits.** Imperative subject line ending with a period. Body explains what and why, wrapped at 80 columns. No em or en dashes. No `Co-Authored-By`, no `Claude-Session`, no other trailer. Do not push.

### Folders and namespaces

`IDE0130` is an error here: `.editorconfig:32` sets `dotnet_analyzer_diagnostic.category-Style.severity = error`, which escalates it repo-wide, and `.editorconfig` may not be edited. Every file therefore declares the namespace that matches its folder.

| Folder | Namespace |
| ------ | --------- |
| `Fluence.Wpf.Tests/` root, which only files awaiting a move still occupy | `Fluence.Wpf.Tests` |
| `Infrastructure/` | `Fluence.Wpf.Tests.Infrastructure` |
| `Control/` | `Fluence.Wpf.Tests.Control` |
| `Control/Rules/` | `Fluence.Wpf.Tests.Control.Rules` |
| `Theming/` | `Fluence.Wpf.Tests.Theming` |
| `Windowing/` | `Fluence.Wpf.Tests.Windowing` |
| `Gallery/` | `Fluence.Wpf.Tests.Gallery` |
| `Gallery/Pages/` | `Fluence.Wpf.Tests.Gallery.Pages` |
| `Tools/` | `Fluence.Wpf.Tests.Tools` |

Four consequences every task depends on.

- **Do not invent a folder name.** A folder name must not equal the last segment of any `Fluence.Wpf.*` namespace the tests reach for by shorthand, so `Controls`, `Demo`, `Helpers`, `Native`, `Markup` and `Automation` are all forbidden. `Theming` is the one accepted exception, because five files already sit under it and compile, and no test writes a `Theming.X` shorthand. This is why the folders are `Control/` and `Gallery/`: a `Fluence.Wpf.Tests.Controls` namespace would capture the `Controls.ProgressBar` shorthand at 1208 sites, and a `Fluence.Wpf.Tests.Demo` namespace would capture `Demo.MainWindow` and `Demo.Mvvm.MainWindow` in `GalleryScreenshotHarness.cs` and `ControlTests.NavigationView.cs`.
- **`using Fluence.Wpf.Tests.Infrastructure;` is needed wherever a helper is named.** `WpfTestSta`, `TestApp`, `VisualTree`, `BrushAssert`, `ThemeTestHelpers`, `DemoTestHost`, `LightThemeFixture` and `SlopwatchSuppressAttribute` all live there. `WpfTestSta` alone is named in 94 of the project's files. A file that calls the walkers or the brush assertions unqualified also needs `using static Fluence.Wpf.Tests.Infrastructure.VisualTree;` or `using static Fluence.Wpf.Tests.Infrastructure.BrushAssert;`. Add only what a file actually uses: IDE0005 is a build error.
- **Qualify the bare `Control` type.** Inside any namespace nested under `Fluence.Wpf.Tests`, the name `Control` binds to the `Fluence.Wpf.Tests.Control` namespace, not to `System.Windows.Controls.Control`. Nine sites in six files use the bare type and must be written out as `System.Windows.Controls.Control` by the task that moves their file: `ControlTests.NavigationView.cs:1565` (Task 14), `ControlTests.IconForeground.cs:599` (Task 16), `ControlTests.DemoParity.cs:316` (Task 17), `TextRenderingPolicyTests.cs:320`, `:343` and `:357` (Task 19), `FluenceWindowHardenTests.cs:908` and `:930` (Task 20), and `GalleryScreenshotHarness.cs:197` (Task 22). No other folder name shadows anything: `Gallery`, `Infrastructure`, `Pages`, `Shared`, `Tools` and `Windowing` occur in this tree only inside comments and string literals.
- **`--filter-class` arguments carry the folder segment.** `Fluence.Wpf.Tests.Control.ButtonTests`, not `Fluence.Wpf.Tests.ButtonTests`. A class whose file has not moved yet still carries the root namespace, so every task writes its filter out in full rather than deriving it. The `--list-tests` name diff is unaffected, because it keys on the method name alone, which the extraction one-liner takes as the last dot-separated segment of each line.

### Test invocation

xunit.v3 runs on Microsoft Testing Platform. **Do not use `dotnet test`.** Run the built executable:

```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class <FullName> --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --filter-class <FullName> --no-ansi --progress off
```

`--filter-class` and `--filter-not-class` accept several space-separated fully qualified class names after a single flag. Verified at this branch tip: `--filter-class Fluence.Wpf.Tests.WindowPolicyTests Fluence.Wpf.Tests.NativeMethodsTests` discovers 111 cases and the matching `--filter-not-class` discovers 1086, summing to the whole-assembly 1197.

- **Never run the whole assembly in the foreground.** It overruns the tool timeout. Use targeted `--filter-class` while iterating, and the two-lane run at phase ends, backgrounded.
- **`ParallelMode.None`.** `Fluence.Wpf.Tests/Properties/AssemblyInfo.cs:31` carries `[assembly: Parallelization(Mode = ParallelMode.None)]`, `xunit.runner.json` disables runner parallelism, and the project sets `<TestTfmsInParallel>false</TestTfmsInParallel>`. Do not change any of these.
- **Single STA thread.** `WpfTestSta` owns one STA thread and one `Dispatcher`. Every UI-touching line runs inside `WpfTestSta.RunOnStaAsync`.
- **Known flaky tests. Do not "fix" them and do not read them as regressions.** `TimePicker_Cancel_RevertsPendingSelectionAsync` fails on `net472` and passes on `net10` (flyout timing). The ToggleSwitch pressed-scale, BreadcrumbBar press-scale and NavigationView pane-width animation-timing tests pass in isolation and can fail in a suite run.
- **net472 whole-assembly abort.** A single-process run of the whole `net472` assembly aborts with exit `-1` at a non-deterministic point. This plan does not fix it; the two lanes below are exact complements, so their union is the whole assembly.
- **Screenshots stay opt-in.** `GalleryScreenshotHarness` facts use `[Fact(SkipUnless = nameof(ScreenshotCaptureEnabled))]` gated on `FLUENCE_CAPTURE_SCREENSHOTS`. Every phase must still report 3 `NotExecuted`. Do not set that variable.

### The two lanes

Lane A is the seven costliest classes after consolidation. Lane B is the exact complement.

```
Fluence.Wpf.Tests\bin\Debug\<tfm>\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --report-xunit-trx --report-xunit-trx-filename laneA.trx --results-directory TestResults\<tfm> --no-ansi --progress off

Fluence.Wpf.Tests\bin\Debug\<tfm>\Fluence.Wpf.Tests.exe --filter-not-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --report-xunit-trx --report-xunit-trx-filename laneB.trx --results-directory TestResults\<tfm> --no-ansi --progress off
```

Those seven class names do not all exist until Task 26 completes, and a class changes its namespace when its file moves. Four splits are therefore used over the life of the plan, and each task states which:

- **Tasks 1 to 17**, the pre-consolidation split the audit proved: `--filter-class Fluence.Wpf.Tests.ControlTests` and `--filter-not-class Fluence.Wpf.Tests.ControlTests`.
- **Tasks 18 to 20**, the first interim nine-class split, defined in full just after Task 18.
- **Tasks 21 to 25**, the second interim nine-class split, which differs from the first only in the two demo classes Task 21 moves into `Gallery/`.
- **Tasks 26 to 29**, the seven-class split above.

### Baselines and the name diff

Measured on `fix/mica-composition-defects` at commit `950e779`, with the exe built from that tree:

| Lane | Discovered cases at `950e779` | After Task 23 |
| ---- | ----------------------------: | ------------: |
| `net10.0-windows10.0.26100.0` | **1197** | **1180** |
| `net472` | **1195** | **1178** |

Test *methods* at `950e779`: **1155**, and all 1155 method names are unique across all classes. Verified: stripping the class prefix from the 1197 discovered `net10` names yields 1155 distinct method names with no duplicates, so the diff key is the method name alone and the spec's `class.method` fallback for duplicate names is not needed.

The spec's 1188 and 1170 predate this branch tip. `GalleryPageHeaderTests.cs` (5 methods) and further additions landed after the audit was measured, taking the baseline to 1197. Use the measured numbers above.

The 1197 to 1180 arithmetic is **18 cases deleted and 1 case added**, a net of 17. The added case is the second `[InlineData]` of the D4 theory fold. The spec's section 4 reports the deletion count as 18 while its fold table claims both folds "preserve case count"; those two statements are only consistent if D4 is counted as 2 deletions plus 1 addition, which is how this plan counts it.

`--list-tests` prints one indented, fully qualified line per case, with theory arguments in parentheses, for example:

```
  Fluence.Wpf.Tests.AccentRampTests.GenerateAccentRampWinaccent_IsDeterministic(r: 0, g: 120, b: 212)
```

Capture per TFM:

```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --list-tests --no-ansi > Fluence.Wpf.Tests\Baselines\<phase>.net10.txt
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --list-tests --no-ansi > Fluence.Wpf.Tests\Baselines\<phase>.net472.txt
```

Extract and sort the method-name multiset with this PowerShell one-liner, run once per file:

```powershell
Get-Content Fluence.Wpf.Tests\Baselines\<phase>.net10.txt | Where-Object { $_ -match '^\s+Fluence\.Wpf\.Tests\.' } | ForEach-Object { (($_ -replace '\(.*$', '').Trim() -split '\.')[-1] } | Sort-Object | Set-Content Fluence.Wpf.Tests\Baselines\<phase>.net10.methods.txt
```

Diff against the committed baseline:

```powershell
Compare-Object (Get-Content Fluence.Wpf.Tests\Baselines\baseline.net10.methods.txt) (Get-Content Fluence.Wpf.Tests\Baselines\<phase>.net10.methods.txt)
```

An empty result means the multiset is unchanged. `<=` rows are removals, `=>` rows are additions. Every task below states the expected result.

Committed artefacts live in `Fluence.Wpf.Tests/Baselines/` as `.txt` and `.md`, which the SDK compile glob ignores. Only `baseline.net10.txt`, `baseline.net472.txt`, `baseline.net10.methods.txt`, `baseline.net472.methods.txt` and `allowlist.md` are committed; per-phase captures are scratch and are not committed.

---

## File Structure

New files, and what each is responsible for.

| File | Responsibility |
| ---- | -------------- |
| `Fluence.Wpf.Tests/Infrastructure/TestApp.cs` | The one application and theme reset. `EnsureLibraryTheme`, `EnsureDemoTheme`, `GenericDictionary`. Nothing else resets `Application.Resources`. |
| `Fluence.Wpf.Tests/Infrastructure/VisualTree.cs` | The one set of visual-tree walkers and the one window teardown. Forwards enumeration to `WpfTestSta.FindVisualDescendants`. |
| `Fluence.Wpf.Tests/Infrastructure/BrushAssert.cs` | Resolve a theme brush key and compare colours. Two members, no state. |
| `Fluence.Wpf.Tests/Baselines/*.txt`, `allowlist.md` | The committed `--list-tests` baseline per TFM and the allowlist of permitted name changes. |
| `Fluence.Wpf.Tests/Infrastructure/LightThemeFixture.cs` | Task 24. A class fixture that pays one reset plus one Light apply per class instead of per test. |

Moved files keep one responsibility each: `Infrastructure/` holds only helpers, `Control/<X>Tests.cs` holds exactly one control's tests, `Control/Rules/` holds the eight rules asserted across many controls, `Windowing/`, `Theming/`, `Gallery/` and `Tools/` hold their named concerns.

Every folder name is also a namespace segment, and each was picked so that it shadows nothing the tests already use. `Windowing` is spelled with the `ing` deliberately: a folder named `Window` would invite a namespace segment that shadows `System.Windows.Window`, which hundreds of test bodies declare as `Window w = new()`. `Control` is singular so that it does not shadow `Fluence.Wpf.Controls` at the 1208 `Controls.ProgressBar` shorthand sites, and `Gallery` replaces `Demo` so that it does not shadow `Fluence.Wpf.Demo` at the `Demo.MainWindow` and `Demo.Mvvm.MainWindow` sites. The five files already under `Theming/` keep the namespace they have today; Task 19 gives the seven files moving in the same one. See the Global Constraints section "Folders and namespaces" for the full table and the three rules that follow from it.

The test project uses SDK globbing with no `<Compile>` items, so folder moves need no project-file edit.

---
## Phase 0: branch, baseline, namespace probe

### Task 1: Create the branch, commit the baseline, and land the first folder-matching namespace

**Files:**
- Create: `Fluence.Wpf.Tests/Baselines/baseline.net10.txt`
- Create: `Fluence.Wpf.Tests/Baselines/baseline.net472.txt`
- Create: `Fluence.Wpf.Tests/Baselines/baseline.net10.methods.txt`
- Create: `Fluence.Wpf.Tests/Baselines/baseline.net472.methods.txt`
- Create: `Fluence.Wpf.Tests/Baselines/allowlist.md`
- Move: `Fluence.Wpf.Tests/WpfTestSta.cs` to `Fluence.Wpf.Tests/Infrastructure/WpfTestSta.cs`, changing its namespace line
- Modify: every other file that names `WpfTestSta`, which is 93 of them, to add `using Fluence.Wpf.Tests.Infrastructure;`

**Starting commit:** `950e779 Apply the owner review edits to the two design specs.` (the tip of `fix/mica-composition-defects`).

**Interfaces:**
- Consumes: nothing.
- Produces: the branch `refactor/test-suite-consolidation`; the four baseline text files and `allowlist.md` under `Fluence.Wpf.Tests/Baselines/`; `Fluence.Wpf.Tests/Infrastructure/WpfTestSta.cs` holding `internal static class WpfTestSta`, members unchanged, in the namespace `Fluence.Wpf.Tests.Infrastructure`; and `using Fluence.Wpf.Tests.Infrastructure;` in its 93 consumers, which is the `using` every later task assumes is already present.

- [ ] **Step 1: Create the branch (this task's preamble, which differs from every other task)**

Run each as a separate command.

```
git branch --show-current
```
Expected: `main`.

```
git reset --hard HEAD
```

```
git checkout -b refactor/test-suite-consolidation fix/mica-composition-defects
```
Expected: `Switched to a new branch 'refactor/test-suite-consolidation'`. Creating a branch from another branch's tip does not check that branch out, so this does not collide with the primary worktree.

```
git log --oneline -1
```
Expected: `950e779 Apply the owner review edits to the two design specs.`

- [ ] **Step 2: Build both TFMs**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 3: Capture the raw baseline per TFM**

```
mkdir Fluence.Wpf.Tests\Baselines
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --list-tests --no-ansi > Fluence.Wpf.Tests\Baselines\baseline.net10.txt
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --list-tests --no-ansi > Fluence.Wpf.Tests\Baselines\baseline.net472.txt
```

Expected tail of `baseline.net10.txt`: `Test discovery summary: found 1197 test(s)`.
Expected tail of `baseline.net472.txt`: `Test discovery summary: found 1195 test(s)`.

If either number differs, stop and report. The rest of the plan is keyed to 1197 and 1195.

- [ ] **Step 4: Extract the sorted method-name multisets**

```powershell
Get-Content Fluence.Wpf.Tests\Baselines\baseline.net10.txt | Where-Object { $_ -match '^\s+Fluence\.Wpf\.Tests\.' } | ForEach-Object { (($_ -replace '\(.*$', '').Trim() -split '\.')[-1] } | Sort-Object | Set-Content Fluence.Wpf.Tests\Baselines\baseline.net10.methods.txt
```

```powershell
Get-Content Fluence.Wpf.Tests\Baselines\baseline.net472.txt | Where-Object { $_ -match '^\s+Fluence\.Wpf\.Tests\.' } | ForEach-Object { (($_ -replace '\(.*$', '').Trim() -split '\.')[-1] } | Sort-Object | Set-Content Fluence.Wpf.Tests\Baselines\baseline.net472.methods.txt
```

Verify the line counts:

```powershell
(Get-Content Fluence.Wpf.Tests\Baselines\baseline.net10.methods.txt).Count
(Get-Content Fluence.Wpf.Tests\Baselines\baseline.net472.methods.txt).Count
```
Expected: `1197` and `1195`.

Verify there are no duplicate method names across classes, which is what makes the method name a safe diff key:

```powershell
((Get-Content Fluence.Wpf.Tests\Baselines\baseline.net10.methods.txt) | Sort-Object -Unique).Count
```
Expected: `1155`. All 42 repeats are theory cases of one method, not two methods sharing a name.

- [ ] **Step 5: Write the allowlist**

Create `Fluence.Wpf.Tests/Baselines/allowlist.md` with exactly this content (UTF-8 with BOM, LF, final newline):

```markdown
# Permitted `--list-tests` method-name changes

Diff key: the method name alone, one line per discovered test case. Theory cases
repeat their method name once per `[InlineData]`. All 1155 method names in the
baseline are unique across classes, so no `class.method` disambiguation is used.

Baseline: 1197 cases on net10, 1195 on net472, both captured at
`950e779` and committed beside this file.

Nothing outside this list may appear in a `Compare-Object` result. Every entry
lands in Task 23 and in no other task; every earlier and later task must diff
empty against the baseline, or against the baseline plus this list once Task 23
has landed.

## Removals: 21 lines

Eighteen are deleted test cases; three are the method names retired by the
`DefaultCollectionFocusVisualStyle` theory fold.

| Line | Source | Reason |
| ---- | ------ | ------ |
| `ControlCornerRadius_PresentInLightThemeAsync` | `ThemeMetricsTests.cs:56` | D1 |
| `ControlCornerRadius_PresentInDarkThemeAsync` | `ThemeMetricsTests.cs:68` | D1 |
| `ControlCornerRadius_PresentInHighContrastThemeAsync` | `ThemeMetricsTests.cs:80` | D1 |
| `OverlayCornerRadius_PresentInLightThemeAsync` | `ThemeMetricsTests.cs:96` | D1 |
| `OverlayCornerRadius_PresentInDarkThemeAsync` | `ThemeMetricsTests.cs:108` | D1 |
| `OverlayCornerRadius_PresentInHighContrastThemeAsync` | `ThemeMetricsTests.cs:120` | D1 |
| `DefaultControlFocusVisualStyle_PresentInAllThemesAsync` | `ThemeMetricsTests.cs:169` | D2 |
| `ProgressBar_TrackBackground_UsesWinUiStrongStrokeRoleAsync` | `ControlTests.BackgroundParity.cs:114` | D3 |
| `FiveSwitches_DictionaryCountStableAsync` | `ThemeManagerTests.cs:150` | D4 |
| `MergedDictionaries_CountStableAfterMultipleSwitchesAsync` | `FluenceWindowTitleBarTests.cs:471` | D4 |
| `BuildBackdropPlan_None_ReturnsOpaqueBackground` | `FluenceWindowHardenTests.cs:223` | D5 |
| `BuildBackdropPlan_Mica_SupportedOs_ReturnsTransparent` | `FluenceWindowHardenTests.cs:240` | D6 |
| `MainWindow_ProgressNumberBox_UpdatesFirstProgressBarAsync` | `ControlTests.cs:2056` | D7 |
| `Experiment_WriteDwmAccentColor_DoesAccentPaletteRegenerateAsync` | `AccentPaletteRegenerationExperiment.cs` | D8 |
| `Experiment_WriteAllAccentValues_DoesAccentPaletteRegenerateAsync` | `AccentPaletteRegenerationExperiment.cs` | D8 |
| `Experiment_WriteAllAndBroadcast_DoesAccentPaletteRegenerateAsync` | `AccentPaletteRegenerationExperiment.cs` | D8 |
| `Probe_EnumerateColorSets_DumpsAllRamps` | `ImmersiveColorSetProbe.cs:73` | D8 |
| `Score_AllAlgorithms_AgainstCapturedFixtures` | `AccentRampScoreboard.cs:153` | D9 |
| `DefaultCollectionFocusVisualStyle_PresentInLightThemeAsync` | `ThemeMetricsTests.cs:210` | Fold 1 |
| `DefaultCollectionFocusVisualStyle_PresentInDarkThemeAsync` | `ThemeMetricsTests.cs:221` | Fold 1 |
| `DefaultCollectionFocusVisualStyle_PresentInHighContrastThemeAsync` | `ThemeMetricsTests.cs:232` | Fold 1 |

## Additions: 4 lines

| Line | Occurrences | Reason |
| ---- | ----------: | ------ |
| `DefaultCollectionFocusVisualStyle_PresentInThemeAsync` | 3 | Fold 1: one `[Theory]` with three `[InlineData]`. |
| `RepeatedThemeSwitches_NoDictionaryAccumulationAsync` | 1 extra, taking it from 1 line to 2 | Fold 2: the D4 survivor becomes a `[Theory]` with `[InlineData(false)]` and `[InlineData(true)]`. |

Net: 21 removed, 4 added, 17 fewer cases. 1197 to 1180 on net10, 1195 to 1178 on net472.
```

- [ ] **Step 6: Move `WpfTestSta.cs` into `Infrastructure/` with the matching namespace**

```
mkdir Fluence.Wpf.Tests\Infrastructure
```

```
git mv Fluence.Wpf.Tests/WpfTestSta.cs Fluence.Wpf.Tests/Infrastructure/WpfTestSta.cs
```

Change exactly one line in the moved file: `namespace Fluence.Wpf.Tests` becomes `namespace Fluence.Wpf.Tests.Infrastructure`. Every member keeps its name, signature and body, per the spec's section 5 ruling that `WpfTestSta` is the canonical STA fixture and must not change during a layout refactor.

Then add `using Fluence.Wpf.Tests.Infrastructure;` to every other file that names `WpfTestSta`. List them with

```
git grep -l WpfTestSta -- Fluence.Wpf.Tests
```

Expected: 94 files, one of which is `WpfTestSta.cs` itself, so 93 gain the `using`. Place it in the file's `using` block in alphabetical order among the non-static directives, above any `using static` lines; an out-of-order block fails IDE0055 under `dotnet format`. Do not add it to a file that does not name a helper: IDE0005 is a build error.

- [ ] **Step 7: Build and confirm the namespace decision holds**

Run: `dotnet build Fluence.Wpf.sln -c Debug`

Expected: `0 Warning(s)`, `0 Error(s)`, and no `IDE0130`.

The three failures to expect while converging, and what each means: `IDE0130` means the moved file's namespace line still disagrees with its folder; `CS0246` on `WpfTestSta` means a consumer is missing the `using`; `IDE0005` on `using Fluence.Wpf.Tests.Infrastructure;` means that file does not name a helper and the `using` comes back out.

Do **not** try a flat `namespace Fluence.Wpf.Tests` under `Infrastructure/`. It has already been measured and it fails on both TFMs with `IDE0130: Namespace "Fluence.Wpf.Tests" does not match folder structure, expected "Fluence.Wpf.Tests.Infrastructure"`, escalated to an error by `.editorconfig:32`, which may not be edited. The folder-matching layout in Global Constraints is the settled answer.

- [ ] **Step 8: Verify format and text policy**

Run: `dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore`
Expected: no output, exit 0.

Run: `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll`
Expected: pass.

The five new files under `Baselines/` are untracked when `-CheckAll` runs, and it scans tracked files only. Byte-check them yourself:

```powershell
'baseline.net10.txt','baseline.net472.txt','baseline.net10.methods.txt','baseline.net472.methods.txt','allowlist.md' | ForEach-Object { $p = "Fluence.Wpf.Tests\Baselines\$_"; "$_ : " + (([System.IO.File]::ReadAllBytes($p)[0..2] | ForEach-Object { $_.ToString('X2') }) -join ' ') }
```
Expected: every line ends `EF BB BF`. If a file lacks the BOM, rewrite it with `Set-Content -Encoding utf8BOM`.

- [ ] **Step 9: Re-list and confirm the diff is empty**

```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --list-tests --no-ansi > Fluence.Wpf.Tests\Baselines\phase0.net10.txt
```

```powershell
Get-Content Fluence.Wpf.Tests\Baselines\phase0.net10.txt | Where-Object { $_ -match '^\s+Fluence\.Wpf\.Tests\.' } | ForEach-Object { (($_ -replace '\(.*$', '').Trim() -split '\.')[-1] } | Sort-Object | Set-Content Fluence.Wpf.Tests\Baselines\phase0.net10.methods.txt
```

```powershell
Compare-Object (Get-Content Fluence.Wpf.Tests\Baselines\baseline.net10.methods.txt) (Get-Content Fluence.Wpf.Tests\Baselines\phase0.net10.methods.txt)
```
Expected: no output.

Delete the two scratch files:

```
del Fluence.Wpf.Tests\Baselines\phase0.net10.txt
```
```
del Fluence.Wpf.Tests\Baselines\phase0.net10.methods.txt
```

- [ ] **Step 10: Run a smoke lane to prove the moved file still hosts the STA fixture**

Run: `Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.WindowPolicyTests Fluence.Wpf.Tests.SnapLayoutHelperTests Fluence.Wpf.Tests.ThemeMetricsTests --no-ansi --progress off`
Expected: 105 passed, 0 failed. (89 + 3 + 13.)

Run the same filter on `Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe`.
Expected: 105 passed, 0 failed.

- [ ] **Step 11: Commit**

```
git add -A
```

```
git commit -m "Commit the test discovery baseline and move WpfTestSta into Infrastructure."
```

Commit body to include:

```
Capture --list-tests per TFM at the branch point so every later phase of the
consolidation can be diffed as a method-name multiset rather than by eye:
1197 cases on net10 and 1195 on net472, over 1155 uniquely named methods.

Move the first file into Infrastructure/ with the folder-matching namespace
Fluence.Wpf.Tests.Infrastructure. A flat namespace under a subfolder does not
compile here: IDE0130 is escalated to an error repo-wide by .editorconfig, so
every folder this refactor adds is also a namespace segment. Ninety-three
consumers gain the one using directive that the rest of the plan assumes.
```

---

## Phase 1: lift the shared helpers

Phase 1 must precede every move. The partial's private helpers are what a class split would otherwise break: `MergeGenericDictionary` (618 call sites in 65 files), `FindVisualChildByName` (455 sites), `CloseWindowAndDrain` (128 sites, defined in the NavigationView file) and `RunDemoPageTestAsync` (defined in `ControlTests.DemoParity.cs`, called 22 times from `ControlTests.DemoSamplePolish.cs`).

**Phase 1 preserves behaviour exactly.** `TestApp` ships both entry points in Task 2, and every caller is pointed at whichever one matches what it does today. The behavioural change, dropping the demo dictionary from library tests, lands alone in Task 6.

### Task 2: Add `Infrastructure/TestApp.cs` and retire the seven merge helpers

**Files:**
- Create: `Fluence.Wpf.Tests/Infrastructure/TestApp.cs`
- Modify: `Fluence.Wpf.Tests/ControlTests.cs:85-102` (delete `MergeGenericDictionary`)
- Modify: `Fluence.Wpf.Tests/ControlRenderingTests.cs:39` (delete `MergeThemeAndGeneric`)
- Modify: `Fluence.Wpf.Tests/ListViewIsItemSelectableTests.cs:39` (delete `MergeGenericDictionary`)
- Modify: `Fluence.Wpf.Tests/TitleBarTests.cs:295` (delete `MergeGenericDictionary`)
- Modify: `Fluence.Wpf.Tests/TabViewTests.cs:44` (delete `MergeGenericDictionary`)
- Modify: `Fluence.Wpf.Tests/ComboBoxTests.cs:40` (delete `MergeTheme`)
- Modify: `Fluence.Wpf.Tests/FluenceWindowTitleBarTests.cs:50` (delete `MergeTheme`)
- Modify: all 65 files that call any of the above

**Starting commit:** the Task 1 commit.

**Interfaces:**
- Consumes: `WpfTestSta.EnsureApplication()`, `WpfTestSta.DrainDispatcher(Dispatcher)` from `Infrastructure/WpfTestSta.cs`.
- Produces:
  - `internal static Application TestApp.EnsureLibraryTheme(ApplicationTheme theme = ApplicationTheme.Light, BackdropType backdrop = BackdropType.None)`
  - `internal static Application TestApp.EnsureDemoTheme(BackdropType backdrop = BackdropType.None)`
  - `internal static ResourceDictionary TestApp.GenericDictionary(Application application)`

- [ ] **Step 1: Executor preamble**

Run the four commands from Global Constraints. Expected `git log --oneline -1`: the Task 1 commit, subject `Commit the test discovery baseline and move WpfTestSta into Infrastructure.`

- [ ] **Step 2: Write `Fluence.Wpf.Tests/Infrastructure/TestApp.cs`**

```csharp
/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Xunit;

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// The single application and theme reset for the suite. Every test class that touches
    /// <see cref="Application"/>, application resources, or a <see cref="Window"/> starts from
    /// one of these two entry points, so no test inherits state from the test before it.
    /// </summary>
    internal static class TestApp
    {
        private static readonly Uri DemoSharedStylesUri = new(
            "/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml",
            UriKind.Relative);

        /// <summary>
        /// Resets the application and applies a library theme. The demo resource dictionary is
        /// deliberately not merged: a library control test asserts the library's own template and
        /// brushes, and a demo style merged on top can shadow a library brush.
        /// </summary>
        /// <param name="theme">The theme to apply after the reset.</param>
        /// <param name="backdrop">The backdrop to apply with it.</param>
        internal static Application EnsureLibraryTheme(
            ApplicationTheme theme = ApplicationTheme.Light,
            BackdropType backdrop = BackdropType.None)
        {
            Application application = WpfTestSta.EnsureApplication();
            Reset(application);
            ApplicationThemeManager.Apply(theme, backdrop, updateAccent: true);
            return application;
        }

        /// <summary>
        /// Resets the application, applies the Light theme and the system accent, then merges the
        /// demo shared styles. This is the explicit opt-in for tests whose subject is the demo
        /// gallery. A library test that calls it must say at its own call site which demo style it
        /// depends on.
        /// </summary>
        /// <param name="backdrop">The backdrop to apply with the Light theme.</param>
        internal static Application EnsureDemoTheme(BackdropType backdrop = BackdropType.None)
        {
            Application application = EnsureLibraryTheme(ApplicationTheme.Light, backdrop);
            ApplicationAccentColorManager.ApplySystemAccent();
            application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = DemoSharedStylesUri });
            return application;
        }

        /// <summary>
        /// Returns the merged <c>Themes/Generic.xaml</c> dictionary, slot [2] of the three-slot
        /// layout described in AGENTS.md section 3, and asserts that the layout is intact. A demo
        /// application has a fourth slot on top; slot [2] is still Generic.
        /// </summary>
        /// <param name="application">The application whose merged dictionaries to read.</param>
        internal static ResourceDictionary GenericDictionary(Application application)
        {
            Collection<ResourceDictionary> dictionaries = application.Resources.MergedDictionaries;
            Assert.True(
                dictionaries.Count >= 3,
                $"Expected at least the three theme slots, found {dictionaries.Count}.");
            return dictionaries[2];
        }

        private static void Reset(Application application)
        {
            Keyboard.ClearFocus();

            foreach (Window window in (Window[])[.. application.Windows.Cast<Window>()])
            {
                window.Content = null;
                window.Close();
            }

            // A single ApplicationIdle drain subsumes the higher Loaded and ContextIdle
            // priorities: Invoke blocks until the queue has been processed down to and
            // including the requested priority.
            WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);

            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            application.Resources.Clear();
        }
    }
}
```

Every consumer already carries `using Fluence.Wpf.Tests.Infrastructure;` from Task 1, because every one of them also names `WpfTestSta`. Add it to any that does not.

- [ ] **Step 3: Rewrite the 618 call sites of `MergeGenericDictionary`**

There are exactly two call-site forms in the tree, verified by counting: 383 discards and 235 assignments, all textually identical.

Form 1, the discard (383 sites):

```csharp
_ = MergeGenericDictionary(app);
```
becomes
```csharp
_ = TestApp.EnsureDemoTheme();
```

The local is sometimes named `application` rather than `app`; match whatever the file uses.

Form 2, the assignment (235 sites, all reading exactly `ResourceDictionary? genericDictionary = MergeGenericDictionary(application);`):

```csharp
ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
```
becomes
```csharp
_ = TestApp.EnsureDemoTheme();
```

and the matching teardown, which appears 222 times as

```csharp
if (genericDictionary is not null)
{
    _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
}
```
or unguarded as
```csharp
_ = application.Resources.MergedDictionaries.Remove(genericDictionary);
```

is **deleted outright**. The next test's `EnsureLibraryTheme` or `EnsureDemoTheme` clears the whole collection, so the removal is dead work and a source of ordering bugs. If deleting the body leaves an empty `finally { }`, delete the `try`/`finally` and dedent, unless the `finally` also closes a window.

- [ ] **Step 4: Fix the PasswordBox teardown helper**

`ControlTests.PasswordBox.cs` passes the dictionary into a private helper at 14 sites. Change

```csharp
private static void ClosePasswordBoxTest(Window window, Application application, ResourceDictionary? genericDictionary)
```
to
```csharp
private static void ClosePasswordBoxTest(Window window)
```

deleting the two parameters and the dictionary-removal statement from its body, and update all 14 call sites to `ClosePasswordBoxTest(window);`.

- [ ] **Step 5: Verify no `genericDictionary` identifier survives**

Run: `git grep -c genericDictionary -- Fluence.Wpf.Tests`
Expected: no output (no matches).

- [ ] **Step 6: Rewrite the other six merge helpers and their call sites**

Each helper's body is deleted, and its callers move to the entry point that matches what the old helper did.

| Helper | Old behaviour | Replacement at every call site |
| ------ | ------------- | ------------------------------ |
| `ControlRenderingTests.cs:39 MergeThemeAndGeneric(Application app)`, 3 calls at `:63,:77,:91` | merged demo styles, did not clear | `_ = TestApp.EnsureDemoTheme();` |
| `ListViewIsItemSelectableTests.cs:39 MergeGenericDictionary` | merged demo styles, did not clear | `_ = TestApp.EnsureDemoTheme();` |
| `TitleBarTests.cs:295 MergeGenericDictionary` | no demo styles, cleared | `_ = TestApp.EnsureLibraryTheme();` |
| `TabViewTests.cs:44 MergeGenericDictionary` | no demo styles, did not clear | `_ = TestApp.EnsureLibraryTheme();` |
| `ComboBoxTests.cs:40 MergeTheme`, 1 call at `:185` | no demo styles, did not clear | `_ = TestApp.EnsureLibraryTheme();` |
| `FluenceWindowTitleBarTests.cs:50 MergeTheme`, 12 calls | no demo styles, did not clear | `_ = TestApp.EnsureLibraryTheme();` |

The four "did not clear" helpers now clear. That is a behaviour change in the direction of more isolation, and it is the only one in this task; if it surfaces a failure, that failure is a real ordering dependency and is triaged, not papered over. `TitleBarTests.cs:306 ResetSharedWpfStateAsync` becomes redundant once the class resets through `TestApp`; delete it and its calls.

`FluenceWindowTitleBarTests.cs` assigns the return at 12 sites (`ResourceDictionary? dict = MergeTheme(app);`). Where `dict` is only used for a teardown removal, delete both. Where it is genuinely read, use `ResourceDictionary generic = TestApp.GenericDictionary(TestApp.EnsureLibraryTheme());`.

- [ ] **Step 7: Build**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: `0 Warning(s)`, `0 Error(s)`.

Expect to fix, in this order, if they appear: IDE0005 for a `using System.Collections.ObjectModel;` left behind in a file that no longer declares a `Collection<ResourceDictionary>`; CA1806 for an undiscarded `TestApp.EnsureDemoTheme()`; IDE0059 for a now-unread local.

- [ ] **Step 8: Run the two pre-consolidation lanes on net10**

Run in the background, one at a time:

```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.ControlTests --report-xunit-trx --report-xunit-trx-filename laneA.trx --results-directory TestResults\net10 --no-ansi --progress off
```
```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-not-class Fluence.Wpf.Tests.ControlTests --report-xunit-trx --report-xunit-trx-filename laneB.trx --results-directory TestResults\net10 --no-ansi --progress off
```

Expected: the two case counts sum to **1197**, 0 failed, 5 `NotRunnable`, 3 `NotExecuted`.

Repeat both on `Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe`. Expected sum: **1195**, 0 failed apart from the known `TimePicker_Cancel_RevertsPendingSelectionAsync` flake.

- [ ] **Step 9: Name diff**

Capture, extract and `Compare-Object` as described in Global Constraints, for both TFMs.
Expected: **no output** for both. This task moves no test and renames none.

- [ ] **Step 10: Format and text policy**

Run: `dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore`
Run: `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll`

Byte-check the new file:
```powershell
([System.IO.File]::ReadAllBytes("Fluence.Wpf.Tests\Infrastructure\TestApp.cs")[0..2] | ForEach-Object { $_.ToString('X2') }) -join ' '
```
Expected: `EF BB BF`.

- [ ] **Step 11: Commit**

```
git add -A
```
```
git commit -m "Collapse the seven test resource-merge helpers into TestApp."
```

Commit body to include:

```
Seven private helpers reset the application differently: three cleared
Application.Resources and four did not, and two merged the demo shared styles
while five did not. A control tested through one of them could pass where the
same control tested through another would fail.

TestApp.EnsureLibraryTheme and TestApp.EnsureDemoTheme are the only two shapes
now. Every caller is pointed at whichever one matches what it did before this
commit, so behaviour is unchanged except that the four helpers that skipped
Resources.Clear now clear. Switching the library tests off the demo dictionary
lands separately.

The finally blocks that removed the Generic dictionary from MergedDictionaries
are deleted: the next test's reset clears the collection anyway.
```
### Task 3: Add `Infrastructure/VisualTree.cs` and retire the private tree walkers

**Files:**
- Create: `Fluence.Wpf.Tests/Infrastructure/VisualTree.cs`
- Modify: `Fluence.Wpf.Tests/ControlTests.cs:104-185` (delete `FindVisualChild`, `FindVisualChildren`, `FindVisualChildByTypeName`, `FindVisualChildByName`)
- Modify: `Fluence.Wpf.Tests/ControlTests.NavigationView.cs:48-54` (delete `CloseWindowAndDrain`)
- Modify: `Fluence.Wpf.Tests/TitleBarTests.cs:265` (delete `FindVisualChild`)
- Modify: `Fluence.Wpf.Tests/DemoColorsPageTests.cs:313,320,326,332,342` (delete `CloseWindowAndDrain`, `FindByName`, `FindVisualChild`, two `FindVisualChildren` overloads)
- Modify: `Fluence.Wpf.Tests/DemoMainWindowTests.cs:2160,2168,2178,2231` (delete `FindByName`, two `FindAllVisualChildren` overloads, `FindVisualChild`)
- Modify: every file that calls any of the above

**Starting commit:** the Task 2 commit.

**Interfaces:**
- Consumes: `WpfTestSta.FindVisualDescendants<T>(DependencyObject?)`, `WpfTestSta.DrainDispatcher(Dispatcher)`, `WpfTestSta.Dispatcher`.
- Produces:
  - `internal static T? VisualTree.FindVisualChild<T>(DependencyObject? root) where T : DependencyObject`
  - `internal static T? VisualTree.FindVisualChildByName<T>(DependencyObject? root, string name) where T : FrameworkElement`
  - `internal static DependencyObject? VisualTree.FindVisualChildByTypeName(DependencyObject? root, string typeName)`
  - `internal static IEnumerable<T> VisualTree.FindVisualChildren<T>(DependencyObject? root) where T : DependencyObject`
  - `internal static void VisualTree.CloseWindowAndDrain(Window window)`
  - The `using static Fluence.Wpf.Tests.Infrastructure.VisualTree;` idiom that every later task relies on.

- [ ] **Step 1: Executor preamble**

Run the four commands from Global Constraints. Expected `git log --oneline -1`: the Task 2 commit, subject `Collapse the seven test resource-merge helpers into TestApp.`

- [ ] **Step 2: Write `Fluence.Wpf.Tests/Infrastructure/VisualTree.cs`**

Start with the same 27-line BSD header, copied verbatim from `Fluence.Wpf.Tests/Infrastructure/WpfTestSta.cs` lines 1 to 27, then:

```csharp
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// The one set of visual-tree walkers and the one window teardown for the suite. Every test
    /// class brings these into scope with <c>using static Fluence.Wpf.Tests.Infrastructure.VisualTree;</c> so the
    /// call sites read exactly as they did when each partial carried its own private copy.
    /// </summary>
    internal static class VisualTree
    {
        /// <summary>
        /// Returns the first descendant of <paramref name="root"/> of type <typeparamref name="T"/>
        /// in the visual tree, depth-first, pre-order, or <see langword="null"/>.
        /// </summary>
        /// <typeparam name="T">The descendant type to find.</typeparam>
        /// <param name="root">The element to search below.</param>
        internal static T? FindVisualChild<T>(DependencyObject? root)
            where T : DependencyObject
        {
            if (root is null)
            {
                return null;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int index = 0; index < childCount; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, index);
                if (child is T match)
                {
                    return match;
                }

                if (FindVisualChild<T>(child) is T visual)
                {
                    return visual;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the first descendant of <paramref name="root"/> of type <typeparamref name="T"/>
        /// whose <see cref="FrameworkElement.Name"/> is <paramref name="name"/>, or
        /// <see langword="null"/>.
        /// </summary>
        /// <typeparam name="T">The descendant type to find.</typeparam>
        /// <param name="root">The element to search below.</param>
        /// <param name="name">The template part name to match, ordinally.</param>
        internal static T? FindVisualChildByName<T>(DependencyObject? root, string name)
            where T : FrameworkElement
        {
            if (root is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int index = 0; index < childCount; index++)
            {
                if (VisualTreeHelper.GetChild(root, index) is FrameworkElement child
                    && string.Equals(child.Name, name, StringComparison.Ordinal)
                    && child is T match)
                {
                    return match;
                }

                T? found = FindVisualChildByName<T>(VisualTreeHelper.GetChild(root, index), name);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the first node at or below <paramref name="root"/> whose runtime type name is
        /// <paramref name="typeName"/>, or <see langword="null"/>. Used where the type is internal
        /// to the library and cannot be named from the test assembly.
        /// </summary>
        /// <param name="root">The element to search at and below.</param>
        /// <param name="typeName">The simple type name to match, ordinally.</param>
        internal static DependencyObject? FindVisualChildByTypeName(DependencyObject? root, string typeName)
        {
            if (root is null)
            {
                return null;
            }

            if (string.Equals(root.GetType().Name, typeName, StringComparison.Ordinal))
            {
                return root;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int index = 0; index < childCount; index++)
            {
                DependencyObject? found = FindVisualChildByTypeName(VisualTreeHelper.GetChild(root, index), typeName);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Enumerates every visual descendant of <paramref name="root"/> of type
        /// <typeparamref name="T"/>. Forwards to the canonical
        /// <see cref="WpfTestSta.FindVisualDescendants{T}(DependencyObject?)"/>; the broader
        /// logical-and-visual walk lives there too, as
        /// <see cref="WpfTestSta.FindLogicalAndVisualDescendants{T}(DependencyObject?)"/>, and is
        /// what the demo host uses.
        /// </summary>
        /// <typeparam name="T">The descendant type to enumerate.</typeparam>
        /// <param name="root">The element to search below.</param>
        internal static IEnumerable<T> FindVisualChildren<T>(DependencyObject? root)
            where T : DependencyObject
        {
            return WpfTestSta.FindVisualDescendants<T>(root);
        }

        /// <summary>
        /// Detaches, closes and drains a test window. Test bodies call this in a
        /// <c>finally</c> so an assertion failure still tears the window down.
        /// </summary>
        /// <param name="window">The window to close.</param>
        internal static void CloseWindowAndDrain(Window window)
        {
            window.Content = null;
            window.UpdateLayout();
            window.Close();
            WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
        }
    }
}
```

- [ ] **Step 3: Delete the private copies and add the `using static`**

Delete these definitions and nothing else:

| File | Lines | Members deleted |
| ---- | ----- | --------------- |
| `ControlTests.cs` | 104 to 185 | `FindVisualChild`, `FindVisualChildren`, `FindVisualChildByTypeName`, `FindVisualChildByName` |
| `ControlTests.NavigationView.cs` | 48 to 54 | `CloseWindowAndDrain` |
| `TitleBarTests.cs` | 265 | `FindVisualChild` |
| `DemoColorsPageTests.cs` | 313, 320, 326, 332, 342 | `CloseWindowAndDrain`, `FindByName`, `FindVisualChild`, both `FindVisualChildren` overloads |
| `DemoMainWindowTests.cs` | 2160, 2168, 2178, 2231 | `FindByName`, both `FindAllVisualChildren` overloads, `FindVisualChild` |

Add, to every file whose test bodies call any of the five produced members and which no longer defines them, this single line in the `using` block, alphabetically after the other `using static` lines if any:

```csharp
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;
```

Do not add it to a file that does not call one of them: IDE0005 is a build error.

`FindByName<T>` and `FindAllVisualChildren<T>` are **not** produced by `VisualTree`. `FindByName` already exists as `DemoTestHost.FindByName<T>`; point the `DemoColorsPageTests` and `DemoMainWindowTests` call sites at that. `FindAllVisualChildren<T>(root)` becomes `FindVisualChildren<T>(root)`, and the second overload `FindAllVisualChildren<T>(root, name)` becomes `FindVisualChildren<T>(root).Where(item => string.Equals(item.Name, name, StringComparison.Ordinal))` inlined at its call sites; check whether the file already has `using System.Linq;` and add it only if it does not.

- [ ] **Step 4: Build**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: `0 Warning(s)`, `0 Error(s)`.

The likely fixes: a call site passing a non-nullable `DependencyObject` into a `DependencyObject?` parameter is fine; a call site that relied on `FindVisualChild<T>(root)` throwing on a null root did not exist, because the old copy also returned null. `RCS1224` and `MA0018` do not apply to a static class of static methods.

- [ ] **Step 5: Verify the private copies are gone**

Run: `git grep -n "private static .*FindVisualChild" -- Fluence.Wpf.Tests`
Expected: no output.

Run: `git grep -n "private static void CloseWindowAndDrain" -- Fluence.Wpf.Tests`
Expected: no output.

Run: `git grep -c "FindAllVisualChildren" -- Fluence.Wpf.Tests`
Expected: no output.

- [ ] **Step 6: Targeted test run**

Run on both TFMs:
```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.TitleBarTests Fluence.Wpf.Tests.DemoColorsPageTests Fluence.Wpf.Tests.DemoMainWindowTests --no-ansi --progress off
```
Expected: 56 passed, 0 failed on net10 (5 + 4 + 47).

Then the two pre-consolidation lanes on both TFMs, backgrounded, as in Task 2 Step 8. Expected sums: 1197 on net10, 1195 on net472.

- [ ] **Step 7: Name diff**

Expected: **no output** on both TFMs.

- [ ] **Step 8: Format, text policy, BOM check on the new file**

Run: `dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore`
Run: `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll`
Byte-check `Fluence.Wpf.Tests\Infrastructure\VisualTree.cs` for `EF BB BF`.

- [ ] **Step 9: Commit**

```
git add -A
```
```
git commit -m "Lift the test visual-tree walkers into Infrastructure/VisualTree."
```

Commit body to include:

```
Five walkers and one window teardown were copied across ControlTests.cs,
ControlTests.NavigationView.cs, TitleBarTests.cs, DemoColorsPageTests.cs and
DemoMainWindowTests.cs, with the teardown living in the NavigationView file
while seventeen unrelated partials called it.

VisualTree now owns all six, with the member names unchanged, so each file
gains one using static line and no call site changes. FindVisualChildren
forwards to WpfTestSta.FindVisualDescendants rather than re-walking the tree.
```

---

### Task 4: Add `Infrastructure/BrushAssert.cs`

**Files:**
- Create: `Fluence.Wpf.Tests/Infrastructure/BrushAssert.cs`
- Modify: `Fluence.Wpf.Tests/ControlTests.BackgroundParity.cs:550` (delete `AssertBrushColor`)
- Modify: `Fluence.Wpf.Tests/ControlTests.ToggleButton.cs:57,63` (delete `GetResolvedBrushColor`, `GetSolidColor`)
- Modify: `Fluence.Wpf.Tests/ControlTests.IconForeground.cs:599-618` (delete the four resolve-and-compare variants)
- Modify: `Fluence.Wpf.Tests/ThemeMarkupTests.cs:305` (delete the open-coded resolver)

**Starting commit:** the Task 3 commit.

**Interfaces:**
- Consumes: nothing beyond `Application` and `Xunit`.
- Produces:
  - `internal static void BrushAssert.AssertBrushColor(Brush? actual, string expectedResourceKey)`
  - `internal static Color BrushAssert.ResolvedColor(Application application, string resourceKey)`

- [ ] **Step 1: Executor preamble**

Expected `git log --oneline -1`: the Task 3 commit, subject `Lift the test visual-tree walkers into Infrastructure/VisualTree.`

- [ ] **Step 2: Write `Fluence.Wpf.Tests/Infrastructure/BrushAssert.cs`**

Start with the same 27-line BSD header, copied verbatim from `Fluence.Wpf.Tests/Infrastructure/WpfTestSta.cs` lines 1 to 27, then:

```csharp
using System.Windows;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// Resolves a canonical theme brush key and compares colours. The suite asserts brush roles by
    /// colour rather than by instance, because the theme engine rebuilds every brush on each apply.
    /// </summary>
    internal static class BrushAssert
    {
        /// <summary>
        /// Asserts that <paramref name="actual"/> is a <see cref="SolidColorBrush"/> whose colour
        /// equals the colour of the brush the current application resolves for
        /// <paramref name="expectedResourceKey"/>.
        /// </summary>
        /// <param name="actual">The brush read off the element under test.</param>
        /// <param name="expectedResourceKey">The canonical WinUI-style brush key, for example
        /// <c>ControlStrongStrokeColorDefaultBrush</c>.</param>
        internal static void AssertBrushColor(Brush? actual, string expectedResourceKey)
        {
            SolidColorBrush actualBrush = Assert.IsType<SolidColorBrush>(actual);
            SolidColorBrush expected = Assert.IsType<SolidColorBrush>(
                Application.Current?.TryFindResource(expectedResourceKey));

            Assert.Equal(expected.Color, actualBrush.Color);
        }

        /// <summary>
        /// Returns the colour of the <see cref="SolidColorBrush"/> that
        /// <paramref name="application"/> resolves for <paramref name="resourceKey"/>, failing the
        /// test if the key does not resolve to one.
        /// </summary>
        /// <param name="application">The application whose resources to read.</param>
        /// <param name="resourceKey">The canonical WinUI-style brush key.</param>
        internal static Color ResolvedColor(Application application, string resourceKey)
        {
            SolidColorBrush brush = Assert.IsType<SolidColorBrush>(application.TryFindResource(resourceKey));
            return brush.Color;
        }
    }
}
```

- [ ] **Step 3: Delete the private copies and rewire their 23 plus call sites**

- `ControlTests.BackgroundParity.cs:550` `AssertBrushColor` is deleted; its 23 call sites are unchanged because the name and parameter order are identical. Add `using static Fluence.Wpf.Tests.Infrastructure.BrushAssert;` to that file.
- `ControlTests.BackgroundParity.cs:559` `AssertBrushResolves(string)` is **kept** as a private helper. It asserts existence, not equality, and has no `BrushAssert` counterpart.
- `ControlTests.ToggleButton.cs:57` `GetResolvedBrushColor(Application, string)` becomes `ResolvedColor(application, key)`; `:63` `GetSolidColor(Brush)` call sites become `Assert.IsType<SolidColorBrush>(brush).Color`. Add the `using static`.
- `ControlTests.IconForeground.cs:599 to 618` holds four variants of the same resolve-and-compare. Replace each call with `AssertBrushColor(actual, key)` where the variant compared a brush to a key, and with `ResolvedColor(application, key)` where it returned a colour. Add the `using static`.
- `ThemeMarkupTests.cs:305` open-coded resolver becomes `ResolvedColor(application, key)`. Add the `using static`.

- [ ] **Step 4: Build**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 5: Targeted test run**

Run on both TFMs:
```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.ThemeMarkupTests --no-ansi --progress off
```
Expected: 6 passed, 0 failed.

```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-method "*IconForeground*" --no-ansi --progress off
```
Expected: 11 passed, 0 failed.

Then the two pre-consolidation lanes on both TFMs, backgrounded. Expected sums: 1197 and 1195.

- [ ] **Step 6: Name diff**

Expected: **no output** on both TFMs.

- [ ] **Step 7: Format, text policy, BOM check**

As in Task 3 Step 8, for `Fluence.Wpf.Tests\Infrastructure\BrushAssert.cs`.

- [ ] **Step 8: Commit**

```
git add -A
```
```
git commit -m "Lift the brush colour assertions into Infrastructure/BrushAssert."
```

Commit body to include:

```
The three-line resolve, cast and compare sequence appeared open-coded in
ControlTests.ToggleButton.cs, ControlTests.IconForeground.cs and
ThemeMarkupTests.cs alongside the one real helper in
ControlTests.BackgroundParity.cs. BrushAssert owns it now, with the same member
name and parameter order so the twenty-three existing call sites are untouched.
```

---

### Task 5: Move the remaining helpers into `Infrastructure/` and fold `DemoTestHost` onto `TestApp`

**Files:**
- Move: `Fluence.Wpf.Tests/ThemeTestHelpers.cs` to `Fluence.Wpf.Tests/Infrastructure/ThemeTestHelpers.cs`
- Move: `Fluence.Wpf.Tests/DemoTestHost.cs` to `Fluence.Wpf.Tests/Infrastructure/DemoTestHost.cs`
- Move: `Fluence.Wpf.Tests/SlopwatchSuppressAttribute.cs` to `Fluence.Wpf.Tests/Infrastructure/SlopwatchSuppressAttribute.cs`
- Modify: `Fluence.Wpf.Tests/Infrastructure/DemoTestHost.cs:51-64,122-135` (forward `EnsureDemoTheme`, delete `AddDemoSharedStyles` and `ResetApplication`)

**Starting commit:** the Task 4 commit.

**Interfaces:**
- Consumes: `TestApp.EnsureDemoTheme(BackdropType)` from Task 2.
- Produces: `Infrastructure/` holding all six helper files. `DemoTestHost.EnsureDemoTheme(BackdropType backdrop = BackdropType.None)` keeps its signature and its eight callers; `DemoTestHost.AddDemoSharedStyles` no longer exists.

- [ ] **Step 1: Executor preamble**

Expected `git log --oneline -1`: the Task 4 commit, subject `Lift the brush colour assertions into Infrastructure/BrushAssert.`

- [ ] **Step 2: Move the three files**

```
git mv Fluence.Wpf.Tests/ThemeTestHelpers.cs Fluence.Wpf.Tests/Infrastructure/ThemeTestHelpers.cs
```
```
git mv Fluence.Wpf.Tests/DemoTestHost.cs Fluence.Wpf.Tests/Infrastructure/DemoTestHost.cs
```
```
git mv Fluence.Wpf.Tests/SlopwatchSuppressAttribute.cs Fluence.Wpf.Tests/Infrastructure/SlopwatchSuppressAttribute.cs
```

`ThemeTestHelpers.cs` and `SlopwatchSuppressAttribute.cs` change one line each: `namespace Fluence.Wpf.Tests` becomes `namespace Fluence.Wpf.Tests.Infrastructure`. Every member keeps its name and body. `ApplyStandardThemeCycle` has 19 call sites and `AssertKeyThemeBrushesResolve` has 2; both stay. `DemoTestHost.cs` takes the same namespace line.

All three sets of consumers already carry `using Fluence.Wpf.Tests.Infrastructure;` from Task 1, with one class of exception to check by hand: a file that names `ThemeTestHelpers`, `DemoTestHost` or `SlopwatchSuppress` but never names `WpfTestSta` did not get the `using` in Task 1. Find them with `git grep -l -E "ThemeTestHelpers|DemoTestHost|SlopwatchSuppress" -- Fluence.Wpf.Tests` and add the `using` where the build asks for it. `Theming/DesignTimeResourceTests.cs:117` is the known one for `SlopwatchSuppress`.

- [ ] **Step 3: Forward `DemoTestHost.EnsureDemoTheme` to `TestApp`**

Replace lines 51 to 64 of `Fluence.Wpf.Tests/Infrastructure/DemoTestHost.cs`, which currently read

```csharp
internal static Application EnsureDemoTheme(BackdropType backdrop = BackdropType.None)
{
    Application application = WpfTestSta.EnsureApplication() ?? throw new InvalidOperationException("WPF application was not created.");
    ResetApplication(application);
    ApplicationThemeManager.Apply(ApplicationTheme.Light, backdrop, updateAccent: true);
    ApplicationAccentColorManager.ApplySystemAccent();
    AddDemoSharedStyles(application);
    return application;
}

internal static void AddDemoSharedStyles(Application application)
{
    application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = DemoSharedStylesUri });
}
```

with

```csharp
internal static Application EnsureDemoTheme(BackdropType backdrop = BackdropType.None)
{
    return TestApp.EnsureDemoTheme(backdrop);
}
```

Then delete the now-unused `private static void ResetApplication(Application)` at lines 122 to 135 and the `private static readonly Uri DemoSharedStylesUri` field at lines 42 to 44, and prune the `using` directives that become unused: `System.Linq` and `System.Windows.Threading` are used only by `ResetApplication`, and `System` is used only by `DemoSharedStylesUri` and `FindByName`'s `StringComparison.Ordinal`, so keep `System`.

- [ ] **Step 4: Confirm nothing else called the removed members**

Run: `git grep -n "AddDemoSharedStyles" -- Fluence.Wpf.Tests`
Expected: no output.

- [ ] **Step 5: Build**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 6: Targeted test run**

The eight `DemoTestHost` consumers:
```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.DemoMainWindowTests Fluence.Wpf.Tests.DemoColorsPageTests Fluence.Wpf.Tests.DemoSamplePageWiringTests Fluence.Wpf.Tests.DemoResourceCleanupTests Fluence.Wpf.Tests.GalleryPageHeaderTests --no-ansi --progress off
```
Expected: 68 passed, 0 failed (47 + 4 + 10 + 2 + 5).

Then the two pre-consolidation lanes on both TFMs, backgrounded. Expected sums: 1197 and 1195.

- [ ] **Step 7: Name diff**

Expected: **no output** on both TFMs.

- [ ] **Step 8: Format and text policy**

Run: `dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore`
Run: `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll`

- [ ] **Step 9: Commit**

```
git add -A
```
```
git commit -m "Move the remaining test helpers into Infrastructure."
```

Commit body to include:

```
ThemeTestHelpers, DemoTestHost and SlopwatchSuppressAttribute join WpfTestSta,
TestApp, VisualTree and BrushAssert under Infrastructure/. The first and third
move byte for byte.

DemoTestHost.EnsureDemoTheme is now a one-line forward to TestApp, so the demo
opt-in has one implementation rather than two that had drifted only by luck.
AddDemoSharedStyles had no caller outside that method and is deleted.
```

---

## Phase 2: drop the demo dictionary from the library tests

### Task 6: Switch the former demo-styles callers to `EnsureLibraryTheme`

**Files:**
- Modify: every file that Task 2 pointed at `TestApp.EnsureDemoTheme()` and whose subject is a library control rather than the demo gallery. That is the 61 `ControlTests.*.cs` partials minus the four demo partials, plus `ControlRenderingTests.cs` and `ListViewIsItemSelectableTests.cs`.

**Starting commit:** the Task 5 commit.

**Interfaces:**
- Consumes: `TestApp.EnsureLibraryTheme(ApplicationTheme, BackdropType)` and `TestApp.EnsureDemoTheme(BackdropType)` from Task 2.
- Produces: no new API. After this task, `TestApp.EnsureDemoTheme` has callers only in files whose subject is the demo gallery.

**Why this lands alone:** every library control test currently runs with `Fluence.Wpf.Demo/Resources/DemoSharedStyles.xaml` merged on top of the theme slots. A demo style can shadow a library brush, which is exactly why the same control could pass in one file and fail in another. Dropping it may surface real failures. Each one is either a library brush that a demo style was masking, which is a bug worth having found, or a test that genuinely needs the demo dictionary, which stays on `EnsureDemoTheme` with a comment naming the style. Both outcomes go in the commit message.

- [ ] **Step 1: Executor preamble**

Expected `git log --oneline -1`: the Task 5 commit, subject `Move the remaining test helpers into Infrastructure.`

- [ ] **Step 2: List the files to switch and the four to leave alone**

Switch every `_ = TestApp.EnsureDemoTheme();` to `_ = TestApp.EnsureLibraryTheme();` in every file **except** these four, whose subject is the demo gallery and which keep `EnsureDemoTheme`:

- `ControlTests.DemoParity.cs`
- `ControlTests.DemoSamplePolish.cs`
- `ControlTests.BackgroundParity.cs` (only the three PowerShell and XAML source-lint tests at `:382,:441,:457` read demo assets; the other seven switch)
- `ControlTests.cs` (only the 15 `MainWindow_*` and `DemoMainWindow_*` tests keep it; the other 63 switch)

`ControlRenderingTests.cs` and `ListViewIsItemSelectableTests.cs` switch in full: their subjects are `Button`, `TextBox` and `ListView` templates.

`DemoMainWindowTests.cs`, `DemoColorsPageTests.cs`, `DemoSamplePageWiringTests.cs`, `DemoResourceCleanupTests.cs` and `GalleryPageHeaderTests.cs` go through `DemoTestHost.EnsureDemoTheme` and are untouched.

- [ ] **Step 3: Apply the switch**

Run: `git grep -c "TestApp.EnsureDemoTheme()" -- Fluence.Wpf.Tests`
Record the per-file counts before the edit so Step 7 can show the expected residue.

Edit each file. The replacement is one token: `EnsureDemoTheme` becomes `EnsureLibraryTheme`.

- [ ] **Step 4: Build**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: `0 Warning(s)`, `0 Error(s)`. No signature changes, so a build failure here means an unrelated mistake.

- [ ] **Step 5: Run the two pre-consolidation lanes on net10 and triage every new failure**

```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.ControlTests --report-xunit-trx --report-xunit-trx-filename laneA.trx --results-directory TestResults\net10 --no-ansi --progress off
```
```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-not-class Fluence.Wpf.Tests.ControlTests --report-xunit-trx --report-xunit-trx-filename laneB.trx --results-directory TestResults\net10 --no-ansi --progress off
```

For each failure, decide one of two things and write the decision down:

1. **The library brush was being shadowed.** The test now sees the library's own value. If the library value is the correct WinUI role, update the test's expectation and record the demo style that was masking it. If the library value is wrong, stop and report: that is a library defect, not a test defect, and it is out of this plan's scope.
2. **The test genuinely needs a demo style.** Revert that one file to `TestApp.EnsureDemoTheme()` and add a comment on the line naming the style it depends on, in this form:

```csharp
// Demo opt-in: this test asserts the DemoSampleCardStyle plate, which lives in
// Fluence.Wpf.Demo/Resources/DemoSharedStyles.xaml, not in the library theme.
_ = TestApp.EnsureDemoTheme();
```

Do not resolve a failure by weakening an assertion.

- [ ] **Step 6: Repeat both lanes on net472**

Expected: the same outcome, plus the known `TimePicker_Cancel_RevertsPendingSelectionAsync` flake.

- [ ] **Step 7: Confirm the residue**

Run: `git grep -c "TestApp.EnsureDemoTheme()" -- Fluence.Wpf.Tests`
Expected: matches only in `ControlTests.cs`, `ControlTests.DemoParity.cs`, `ControlTests.DemoSamplePolish.cs`, `ControlTests.BackgroundParity.cs`, and any file Step 5 case 2 reverted. Every one of those must carry the demo-opt-in comment or be one of the four demo-subject files.

- [ ] **Step 8: Name diff**

Expected: **no output** on both TFMs. Case counts: 1197 and 1195.

- [ ] **Step 9: Format and text policy**

Run: `dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore`
Run: `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll`

- [ ] **Step 10: Commit**

```
git add -A
```
```
git commit -m "Run the library control tests without the demo resource dictionary."
```

The commit body must list, one line each, every failure Step 5 surfaced and which of the two outcomes it took. If there were none, say so explicitly:

```
Every library control test ran with Fluence.Wpf.Demo/Resources/DemoSharedStyles.xaml
merged on top of the three theme slots, because the shared merge helper always
added it. A demo style could therefore shadow a library brush, and the same
control tested from a file that did not merge it would see a different value.

Library tests now call TestApp.EnsureLibraryTheme, which merges only the theme.
The demo dictionary is an explicit opt-in through TestApp.EnsureDemoTheme.

Failures surfaced by the switch, and their resolution:
<one line per failure, or "None.">
```
## Phase 3: retire the partial

Twelve tasks, one commit each. Every task takes a set of `ControlTests.<X>.cs` partials (and, where a matching standalone class already exists, that class too), and produces one sealed class per subject in its destination folder. `ControlTests.cs` is emptied last, in Task 18.

### The Phase 3 conversion recipe

Apply this to every file in a task's table. It is mechanical; nothing about what a test asserts changes.

1. `git mv` the file to its destination path.
1a. Change the namespace line to the one that matches the destination folder: `Fluence.Wpf.Tests.Control` for `Control/`, `Fluence.Wpf.Tests.Control.Rules` for `Control/Rules/`, `Fluence.Wpf.Tests.Windowing` for `Windowing/`, `Fluence.Wpf.Tests.Gallery.Pages` for `Gallery/Pages/`. IDE0130 is an error, so this is not optional.
1b. Confirm the file still carries `using Fluence.Wpf.Tests.Infrastructure;` from Task 1. Every one of these files names `WpfTestSta`, so it should already be there.
1c. If the file uses the bare type `Control`, write it out as `System.Windows.Controls.Control`. Inside a namespace nested under `Fluence.Wpf.Tests`, `Control` now binds to the `Fluence.Wpf.Tests.Control` namespace. Global Constraints lists the three files in this phase that are affected.
2. Change `public sealed partial class ControlTests : IAsyncLifetime` or `public partial class ControlTests` to `public sealed class <NewClassName> : IAsyncLifetime`.
3. Give the class the two `IAsyncLifetime` members shown below. `InitializeAsync` uses `EnsureLibraryTheme()` unless the task's table says `EnsureDemoTheme()`.
4. Delete the per-test reset line from every test body: `_ = TestApp.EnsureLibraryTheme();` or `_ = TestApp.EnsureDemoTheme();`. Keep `Application app = WpfTestSta.EnsureApplication();` where the body still reads `app`.
5. Add the `using static` lines the file needs, and only those it needs, because IDE0005 is a build error:
   - `using static Fluence.Wpf.Tests.Infrastructure.VisualTree;` if the body calls `FindVisualChild`, `FindVisualChildByName`, `FindVisualChildByTypeName`, `FindVisualChildren` or `CloseWindowAndDrain`.
   - `using static Fluence.Wpf.Tests.Infrastructure.BrushAssert;` if the body calls `AssertBrushColor` or `ResolvedColor`.
6. Move any private helper the file defines into the new class unchanged, unless the task's table says another class needs it too, in which case the table names the owner and the other class calls it through that owner.
7. Update the class XML doc summary to name the new class rather than "Gap-audit tests".

The two `IAsyncLifetime` members, verbatim:

```csharp
public ValueTask InitializeAsync()
{
    return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
}

public ValueTask DisposeAsync()
{
    return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
}
```

`DisposeAsync` calls the same entry point on purpose. `TestApp.EnsureLibraryTheme` closes every open window and drains before it applies, which is exactly the teardown the spec asks for, and it uses no member outside the three the spec defines. The extra apply costs one theme build per class disposal.

For a demo-subject class, both members call `TestApp.EnsureDemoTheme()` instead.

For a class whose tests are pure logic and touch no `Application`, no resources and no `Window` (`WindowPolicyTests`, `CaptionButtonChromeTests`, `SnapLayoutHelperTests`, `NativeMethodsTests`, `AccentRampTests`), **do not** add `IAsyncLifetime`. Those classes stay free.

### Worked example: `ControlTests.Separator.cs`

**Before** (the file as it stands after Task 6, lines 29 to 126; the 27-line BSD header above it is unchanged and is omitted here):

```csharp
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Gap-audit tests: Fluent <see cref="Controls.Separator"/> control.
    /// Authority: .NET 10 WPF PresentationFramework.Fluent/Styles/Separator.xaml.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // Separator
        // ---------------------------------------------------------------------------

        [Fact]
        public Task Separator_DefaultStyle_AppliesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = TestApp.EnsureLibraryTheme();

                Controls.Separator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Template applied - Border is the root of the template
                Border border = Assert.IsType<Border>(FindVisualChild<Border>(sep), exactMatch: false);
                w.Close();
            });
        }

        [Fact]
        public Task Separator_Height_IsOneAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = TestApp.EnsureLibraryTheme();

                Controls.Separator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(1.0, sep.Height);
                w.Close();
            });
        }

        [Fact]
        public Task Separator_Background_UsesDividerStrokeColorDefaultBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = TestApp.EnsureLibraryTheme();

                Controls.Separator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                SolidColorBrush bg = Assert.IsType<SolidColorBrush>(sep.Background);
                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("DividerStrokeColorDefaultBrush"));

                Assert.Equal(expected.Color, bg.Color);
                w.Close();
            });
        }

        [Fact]
        public Task Separator_ThemeCycle_StyleRemainsAppliedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = TestApp.EnsureLibraryTheme();

                Controls.Separator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Border border = Assert.IsType<Border>(FindVisualChild<Border>(sep), exactMatch: false);
                w.Close();
            });
        }
    }
}
```

**After**, at `Fluence.Wpf.Tests/Control/SeparatorTests.cs` (same 27-line BSD header above it, unchanged):

```csharp
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="Controls.Separator"/> control.
    /// Authority: .NET 10 WPF PresentationFramework.Fluent/Styles/Separator.xaml.
    /// </summary>
    public sealed class SeparatorTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        [Fact]
        public Task Separator_DefaultStyle_AppliesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Separator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Template applied - Border is the root of the template
                Border border = Assert.IsType<Border>(FindVisualChild<Border>(sep), exactMatch: false);
                w.Close();
            });
        }

        [Fact]
        public Task Separator_Height_IsOneAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Separator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Assert.Equal(1.0, sep.Height);
                w.Close();
            });
        }

        [Fact]
        public Task Separator_Background_UsesDividerStrokeColorDefaultBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Controls.Separator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                SolidColorBrush bg = Assert.IsType<SolidColorBrush>(sep.Background);
                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("DividerStrokeColorDefaultBrush"));

                Assert.Equal(expected.Color, bg.Color);
                w.Close();
            });
        }

        [Fact]
        public Task Separator_ThemeCycle_StyleRemainsAppliedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Separator sep = new();
                Window w = new() { Content = sep, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Border border = Assert.IsType<Border>(FindVisualChild<Border>(sep), exactMatch: false);
                w.Close();
            });
        }
    }
}
```

Eight differences and no others: the path, the namespace line, the class declaration, the two lifetime members, the `using Fluence.Wpf.Tests.Infrastructure;` Task 1 added, the `using static`, the deleted reset lines, and the deleted `Application app` local in the three tests that no longer read it. `SeparatorTests` uses no bare `Control`, so step 1c does not apply to it. `Separator_ThemeCycle_...` applies themes, so `SeparatorTests` does **not** qualify for the Task 24 fixture.

### Phase 3 verification, applied identically to every task in the phase

- [ ] Build: `dotnet build Fluence.Wpf.sln -c Debug`, expect `0 Warning(s)` / `0 Error(s)`.
- [ ] Run each new class on both TFMs: `Fluence.Wpf.Tests\bin\Debug\<tfm>\Fluence.Wpf.Tests.exe --filter-class <the new class names, space separated> --no-ansi --progress off`. Expect the case count in the task's table, 0 failed.
- [ ] Run the two lanes on both TFMs, backgrounded, using `--filter-class Fluence.Wpf.Tests.ControlTests` and its complement until Task 18 empties that class. Expect the sums 1197 and 1195.
- [ ] Name diff on both TFMs: expect **no output**.
- [ ] `dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore`.
- [ ] `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll`.
- [ ] Commit with `git add -A` then a single `git commit -m "<subject>."`.

---

### Task 7: Buttons

**Starting commit:** the Task 6 commit, subject `Run the library control tests without the demo resource dictionary.`

**Interfaces:**
- Consumes: `TestApp`, `VisualTree`, `BrushAssert` from Tasks 2 to 4.
- Produces: `ButtonTests`, `HyperlinkButtonTests`, `ToggleButtonTests`, `SplitButtonTests`, `ToggleSplitButtonTests`.

| Source | Destination | New class | `using static` added | Cases |
| ------ | ----------- | --------- | -------------------- | ----: |
| `ControlTests.Button.cs` | `Control/ButtonTests.cs` | `ButtonTests` | `VisualTree` | 7 |
| `ControlTests.HyperlinkButton.cs` | `Control/HyperlinkButtonTests.cs` | `HyperlinkButtonTests` | `VisualTree` | 2 |
| `ControlTests.ToggleButton.cs` | `Control/ToggleButtonTests.cs` | `ToggleButtonTests` | `VisualTree`, `BrushAssert` | 11 |
| `ControlTests.ToggleSplitButton.cs` | `Control/ToggleSplitButtonTests.cs` | `ToggleSplitButtonTests` | `VisualTree` | 17 |
| `ControlTests.SplitButton.cs` **merged into** `SplitButtonTests.cs` | `Control/SplitButtonTests.cs` | `SplitButtonTests` | `VisualTree` | 12 (5 + 7) |

The merge in the last row: `git mv SplitButtonTests.cs Control/SplitButtonTests.cs`, convert it to `IAsyncLifetime` per the recipe, then move the five test methods out of `ControlTests.SplitButton.cs` into it and `git rm` that file. `ToggleButtonTests` needs `BrushAssert` because Task 4 rewired its `GetResolvedBrushColor` call sites to `ResolvedColor`.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 6 commit.
- [ ] **Step 2: Apply the Phase 3 conversion recipe to each row of the table.**
- [ ] **Step 3: Run the Phase 3 verification checklist.** Targeted filter: `--filter-class Fluence.Wpf.Tests.Control.ButtonTests Fluence.Wpf.Tests.Control.HyperlinkButtonTests Fluence.Wpf.Tests.Control.ToggleButtonTests Fluence.Wpf.Tests.Control.ToggleSplitButtonTests Fluence.Wpf.Tests.Control.SplitButtonTests`, expect 49 passed.
- [ ] **Step 4: Commit** with subject `Split the button control tests out of the ControlTests partial.`

---

### Task 8: Text entry and ComboBox

**Starting commit:** the Task 7 commit.

**Interfaces:**
- Produces: `TextBoxTests`, `PasswordBoxTests`, `NumberBoxTests`, `AutoSuggestBoxTests`, `ComboBoxTests`.

| Source | Destination | New class | `using static` added | Cases |
| ------ | ----------- | --------- | -------------------- | ----: |
| `ControlTests.TextBox.cs` | `Control/TextBoxTests.cs` | `TextBoxTests` | `VisualTree` | 10 |
| `ControlTests.PasswordBox.cs` | `Control/PasswordBoxTests.cs` | `PasswordBoxTests` | `VisualTree` | 15 |
| `ControlTests.NumberBox.cs` | `Control/NumberBoxTests.cs` | `NumberBoxTests` | `VisualTree` | 11 |
| `ControlTests.AutoSuggestBox.cs` | `Control/AutoSuggestBoxTests.cs` | `AutoSuggestBoxTests` | `VisualTree` | 12 |
| `ControlTests.ComboBox.cs` **merged into** `ComboBoxTests.cs` | `Control/ComboBoxTests.cs` | `ComboBoxTests` | `VisualTree` | 17 (6 + 11) |

`PasswordBoxTests` owns the `ClosePasswordBoxTest(Window)` helper Task 2 reduced to one parameter.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 7 commit.
- [ ] **Step 2: Apply the Phase 3 conversion recipe to each row.**
- [ ] **Step 3: Run the Phase 3 verification checklist.** Targeted filter: the five class names, each prefixed `Fluence.Wpf.Tests.Control.`, expect 65 passed.
- [ ] **Step 4: Commit** with subject `Split the text entry and ComboBox tests out of the ControlTests partial.`

---

### Task 9: Selection and list controls

**Starting commit:** the Task 8 commit.

**Interfaces:**
- Produces: `ToggleSwitchTests`, `SliderTests`, `RatingControlTests`, `ListBoxTests`, `ListViewTests`.

| Source | Destination | New class | `using static` added | Cases |
| ------ | ----------- | --------- | -------------------- | ----: |
| `ControlTests.ToggleSwitch.cs` | `Control/ToggleSwitchTests.cs` | `ToggleSwitchTests` | `VisualTree` | 10 |
| `ControlTests.Slider.cs` | `Control/SliderTests.cs` | `SliderTests` | `VisualTree` | 3 |
| `ControlTests.RatingControl.cs` | `Control/RatingControlTests.cs` | `RatingControlTests` | `VisualTree` | 13 |
| `ControlTests.ListBox.cs` | `Control/ListBoxTests.cs` | `ListBoxTests` | `VisualTree` | 2 |
| `ControlTests.ListView.cs` **merged with** `ListViewIsItemSelectableTests.cs` | `Control/ListViewTests.cs` | `ListViewTests` | `VisualTree` | 11 (5 + 6) |

`ToggleSwitchTests` carries the known flaky pressed-scale animation test. Do not change it.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 8 commit.
- [ ] **Step 2: Apply the Phase 3 conversion recipe to each row.**
- [ ] **Step 3: Run the Phase 3 verification checklist.** Targeted filter: the five class names, each prefixed `Fluence.Wpf.Tests.Control.`, expect 39 passed.
- [ ] **Step 4: Commit** with subject `Split the selection and list control tests out of the ControlTests partial.`

---

### Task 10: Cards, surfaces and media

**Starting commit:** the Task 9 commit.

**Interfaces:**
- Produces: `CardTests`, `CheckBoxTests`, `RadioButtonTests`, `ExpanderTests`, `SeparatorTests`, `ImageTests`, `PersonPictureTests`.

| Source | Destination | New class | `using static` added | Cases |
| ------ | ----------- | --------- | -------------------- | ----: |
| `ControlTests.Card.cs` plus `ControlTests.CardAutomation.cs:52-343` (the eight `ClickableCard_*` and `NonClickableCard_*` tests) | `Control/CardTests.cs` | `CardTests` | `VisualTree` | 17 (9 + 8) |
| `ControlTests.CardAutomation.cs:344-441` (`CheckBox_Description_SetsAutomationHelpTextAsync`, `CheckBox_DescriptionChanges_UpdatesAutomationHelpTextAsync`, `CheckBox_NullDescription_ClearsAutomationHelpTextAsync`) | `Control/CheckBoxTests.cs` | `CheckBoxTests` | `VisualTree` | 3 |
| `ControlTests.CardAutomation.cs:443-537` (`RadioButton_Description_SetsAutomationHelpTextAsync`, `RadioButton_DescriptionChanges_UpdatesAutomationHelpTextAsync`, `RadioButton_NullDescription_ClearsAutomationHelpTextAsync`) | `Control/RadioButtonTests.cs` | `RadioButtonTests` | `VisualTree` | 3 |
| `ControlTests.Expander.cs` | `Control/ExpanderTests.cs` | `ExpanderTests` | `VisualTree` | 13 |
| `ControlTests.Separator.cs` | `Control/SeparatorTests.cs` | `SeparatorTests` | `VisualTree` | 4 |
| `ControlTests.Image.cs` | `Control/ImageTests.cs` | `ImageTests` | `VisualTree` | 8 |
| `ControlTests.PersonPicture.cs` | `Control/PersonPictureTests.cs` | `PersonPictureTests` | `VisualTree` | 16 |

`ControlTests.CardAutomation.cs` is `git rm`d once its 14 tests are split three ways. Its name never described its contents.

`Control/CheckBoxTests.cs` and `Control/RadioButtonTests.cs` gain more members in Task 18; create them here with only the rows above.

`SeparatorTests` is the worked example above. Copy it verbatim.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 9 commit.
- [ ] **Step 2: Apply the Phase 3 conversion recipe to each row.**
- [ ] **Step 3: Run the Phase 3 verification checklist.** Targeted filter: the seven class names, each prefixed `Fluence.Wpf.Tests.Control.`, expect 64 passed.
- [ ] **Step 4: Commit** with subject `Split the card, surface and media tests out of the ControlTests partial.`

---

### Task 11: Progress and status

**Starting commit:** the Task 10 commit.

**Interfaces:**
- Produces: `ProgressBarTests`, `ProgressRingTests`, `InfoBarTests`, `InfoBadgeTests`, `PipsPagerTests`. `ProgressBarTests` is one of the seven Lane A classes from Task 26 onward.

| Source | Destination | New class | `using static` added | Cases |
| ------ | ----------- | --------- | -------------------- | ----: |
| `ControlTests.ProgressBar.cs` | `Control/ProgressBarTests.cs` | `ProgressBarTests` | `VisualTree` | 20 |
| `ControlTests.ProgressRing.cs` | `Control/ProgressRingTests.cs` | `ProgressRingTests` | `VisualTree` | 22 |
| `ControlTests.InfoBar.cs` **merged with** `ControlTests.InfoBarSeverityIcon.cs` | `Control/InfoBarTests.cs` | `InfoBarTests` | `VisualTree` | 10 (8 + 2) |
| `ControlTests.InfoBadge.cs` | `Control/InfoBadgeTests.cs` | `InfoBadgeTests` | `VisualTree` | 6 |
| `ControlTests.PipsPager.cs` | `Control/PipsPagerTests.cs` | `PipsPagerTests` | `VisualTree` | 18 |

`ControlTests.InfoBarSeverityIcon.cs` is 69 lines for two tests on the same subject; it folds in and is `git rm`d.

`PipsPagerTests` drives input and does not qualify for the Task 24 fixture.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 10 commit.
- [ ] **Step 2: Apply the Phase 3 conversion recipe to each row.**
- [ ] **Step 3: Run the Phase 3 verification checklist.** Targeted filter: the five class names, each prefixed `Fluence.Wpf.Tests.Control.`, expect 76 passed.
- [ ] **Step 4: Commit** with subject `Split the progress and status tests out of the ControlTests partial.`

---

### Task 12: Flyouts, dialogs and menus

**Starting commit:** the Task 11 commit.

**Interfaces:**
- Produces: `ContentDialogTests`, `FlyoutTests`, `CommandBarFlyoutTests`, `TeachingTipTests`, `ToolTipTests`, `ContextMenuTests`, `MenuTests`. `ContentDialogTests` is one of the seven Lane A classes.

| Source | Destination | New class | `using static` added | Cases |
| ------ | ----------- | --------- | -------------------- | ----: |
| `ControlTests.ContentDialog.cs` | `Control/ContentDialogTests.cs` | `ContentDialogTests` | `VisualTree` | 19 |
| `ControlTests.Flyout.cs` | `Control/FlyoutTests.cs` | `FlyoutTests` | `VisualTree` | 12 |
| `ControlTests.CommandBarFlyout.cs` | `Control/CommandBarFlyoutTests.cs` | `CommandBarFlyoutTests` | `VisualTree` | 8 |
| `ControlTests.TeachingTip.cs` | `Control/TeachingTipTests.cs` | `TeachingTipTests` | `VisualTree` | 17 |
| `ControlTests.ToolTip.cs` | `Control/ToolTipTests.cs` | `ToolTipTests` | `VisualTree` | 6 |
| `ControlTests.ContextMenu.cs` | `Control/ContextMenuTests.cs` | `ContextMenuTests` | `VisualTree` | 8 |
| `ControlTests.Menu.cs` | `Control/MenuTests.cs` | `MenuTests` | `VisualTree` | 4 |

`ContentDialogTests`, `CommandBarFlyoutTests` and `TeachingTipTests` drive input and do not qualify for the Task 24 fixture.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 11 commit.
- [ ] **Step 2: Apply the Phase 3 conversion recipe to each row.**
- [ ] **Step 3: Run the Phase 3 verification checklist.** Targeted filter: the seven class names, each prefixed `Fluence.Wpf.Tests.Control.`, expect 74 passed.
- [ ] **Step 4: Commit** with subject `Split the flyout, dialog and menu tests out of the ControlTests partial.`

---

### Task 13: Pickers

**Starting commit:** the Task 12 commit.

**Interfaces:**
- Produces: `DatePickerTests`, `TimePickerTests`, `ColorPickerTests`, `LoopingSelectorTests`. `TimePickerTests` and `ColorPickerTests` are two of the seven Lane A classes.

| Source | Destination | New class | `using static` added | Cases |
| ------ | ----------- | --------- | -------------------- | ----: |
| `ControlTests.DatePicker.cs` | `Control/DatePickerTests.cs` | `DatePickerTests` | `VisualTree` | 13 |
| `ControlTests.TimePicker.cs` | `Control/TimePickerTests.cs` | `TimePickerTests` | `VisualTree` | 15 |
| `ControlTests.ColorPicker.cs` | `Control/ColorPickerTests.cs` | `ColorPickerTests` | `VisualTree` | 27 |
| `ControlTests.LoopingSelector.cs` | `Control/LoopingSelectorTests.cs` | `LoopingSelectorTests` | `VisualTree` | 12 |

`TimePicker_Cancel_RevertsPendingSelectionAsync` is the known `net472` flyout-timing flake. It moves verbatim. A `net472` failure in this task is expected and is not a regression; say so in the task report.

All four classes drive input and do not qualify for the Task 24 fixture.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 12 commit.
- [ ] **Step 2: Apply the Phase 3 conversion recipe to each row.**
- [ ] **Step 3: Run the Phase 3 verification checklist.** Targeted filter: the four class names, each prefixed `Fluence.Wpf.Tests.Control.`, expect 67 passed on net10, and 66 passed with the one known flake on net472.
- [ ] **Step 4: Commit** with subject `Split the picker tests out of the ControlTests partial.`

---

### Task 14: NavigationView

**Starting commit:** the Task 13 commit.

**Interfaces:**
- Produces: `NavigationViewTests`, `NavigationViewTopModeTests`. `NavigationViewTests` is one of the seven Lane A classes.

| Source | Destination | New class | `using static` added | Cases |
| ------ | ----------- | --------- | -------------------- | ----: |
| `ControlTests.NavigationView.cs` plus `ControlTests.NavigationViewFooter.cs` plus `ControlTests.NavigationViewReload.cs` | `Control/NavigationViewTests.cs` | `NavigationViewTests` | `VisualTree` | 60 (52 + 7 + 1) |
| `ControlTests.NavigationViewTopFooter.cs` plus `ControlTests.NavigationViewTopParity.cs` | `Control/NavigationViewTopModeTests.cs` | `NavigationViewTopModeTests` | `VisualTree` | 14 (5 + 9) |

Two classes, not one: 74 tests in a single file is unreadable, and the top-mode overflow suite is a coherent subject on its own. The split point is the `PaneDisplayMode` the tests set, so `NavigationViewTopModeTests` holds the five `NavigationView_TopFooter*` and `NavigationView_TopMainItem_KeepsLabelAsync` tests plus the nine `NavigationView_InFluenceWindow_*` and `NavigationView_TopMode_*` tests.

`ControlTests.NavigationView.cs:48` used to define `CloseWindowAndDrain` for seventeen other partials; Task 3 already moved it to `VisualTree`, so nothing here re-exports it.

`ControlTests.NavigationView.cs:1565` reads `setter.Property == Control.TemplateProperty`. Inside `Fluence.Wpf.Tests.Control` that `Control` binds to the namespace, so write it out as `System.Windows.Controls.Control.TemplateProperty`.

Private helpers in `ControlTests.NavigationView.cs` that both new classes need, notably `GetNavigationViewItemsHostPanel` (currently at `ControlTests.cs:187`) and `WaitForAnimationAndDrainAsync` (currently at `ControlTests.NavigationView.cs:59`): put both in `NavigationViewTests` as `internal static` members and have `NavigationViewTopModeTests` call them as `NavigationViewTests.GetNavigationViewItemsHostPanel(nav)`. Do not duplicate them.

`NavigationViewTests` carries the known flaky pane-width animation test. Do not change it. Neither class qualifies for the Task 24 fixture.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 13 commit.
- [ ] **Step 2: Apply the Phase 3 conversion recipe to each row.**
- [ ] **Step 3: Run the Phase 3 verification checklist.** Targeted filter: `--filter-class Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.NavigationViewTopModeTests`, expect 74 passed.
- [ ] **Step 4: Commit** with subject `Split the NavigationView tests out of the ControlTests partial.`

---

### Task 15: Collections, strips and scrolling

**Starting commit:** the Task 14 commit.

**Interfaces:**
- Produces: `BreadcrumbBarTests`, `TabViewTests`, `TreeViewTests`, `ScrollBarTests`.

| Source | Destination | New class | `using static` added | Cases |
| ------ | ----------- | --------- | -------------------- | ----: |
| `ControlTests.BreadcrumbBar.cs` | `Control/BreadcrumbBarTests.cs` | `BreadcrumbBarTests` | `VisualTree` | 8 |
| `ControlTests.TabView.cs` **merged into** `TabViewTests.cs` | `Control/TabViewTests.cs` | `TabViewTests` | `VisualTree` | 17 (4 + 13) |
| `ControlTests.TreeView.cs` **merged with** `ControlTests.TreeViewSelectionMode.cs` | `Control/TreeViewTests.cs` | `TreeViewTests` | `VisualTree` | 15 (9 + 6) |
| `ControlTests.ScrollBar.cs` | `Control/ScrollBarTests.cs` | `ScrollBarTests` | `VisualTree` | 14 |

`BreadcrumbBarTests` carries the known flaky press-scale animation test. Do not change it.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 14 commit.
- [ ] **Step 2: Apply the Phase 3 conversion recipe to each row.**
- [ ] **Step 3: Run the Phase 3 verification checklist.** Targeted filter: the four class names, each prefixed `Fluence.Wpf.Tests.Control.`, expect 54 passed.
- [ ] **Step 4: Commit** with subject `Split the collection and strip control tests out of the ControlTests partial.`

---

### Task 16: The cross-control rules

**Starting commit:** the Task 15 commit.

**Interfaces:**
- Produces: `AccessibilityNameTests`, `IconForegroundTests`, `FocusVisualTests`, `ReducedMotionTests`, `BackgroundParityTests`, `FluentStrokeTests`, `PopupCornerRadiusTests`, `AutomationPeerTests`, `CrispRenderingTests`. `Control/Rules/FluentStrokeTests.cs` becomes the reference pattern that AGENTS.md and `docs/contributing.md` point at in Task 27.

| Source | Destination | New class | `using static` added | Cases |
| ------ | ----------- | --------- | -------------------- | ----: |
| `ControlTests.Accessibility.cs` | `Control/Rules/AccessibilityNameTests.cs` | `AccessibilityNameTests` | `VisualTree` | 11 |
| `ControlTests.IconForeground.cs` | `Control/Rules/IconForegroundTests.cs` | `IconForegroundTests` | `VisualTree`, `BrushAssert` | 11 |
| `ControlTests.FocusVisual.cs` | `Control/Rules/FocusVisualTests.cs` | `FocusVisualTests` | `VisualTree` | 10 |
| `ControlTests.ReducedMotion.cs` | `Control/Rules/ReducedMotionTests.cs` | `ReducedMotionTests` | `VisualTree` | 11 |
| `ControlTests.BackgroundParity.cs` | `Control/Rules/BackgroundParityTests.cs` | `BackgroundParityTests` | `VisualTree`, `BrushAssert` | 10 |
| `ControlTests.FluentStroke.cs` | `Control/Rules/FluentStrokeTests.cs` | `FluentStrokeTests` | `VisualTree` | 10 |
| `ControlTests.PopupCornerRadius.cs` | `Control/Rules/PopupCornerRadiusTests.cs` | `PopupCornerRadiusTests` | `VisualTree` | 4 |
| `ControlTests.PeerSetValueGuards.cs` **merged with** `ControlTests.PeerValueChanged.cs` | `Control/Rules/AutomationPeerTests.cs` | `AutomationPeerTests` | `VisualTree` | 6 (4 + 2) |
| `ControlRenderingTests.cs` | `Control/Rules/CrispRenderingTests.cs` | `CrispRenderingTests` | `VisualTree` | 3 |

Two special cases:

**`ReducedMotionTests` moves the `MotionHelper.OverrideIsMotionEnabled` reset out of each test's `finally` and into `DisposeAsync`.** Its lifetime members are:

```csharp
public ValueTask InitializeAsync()
{
    return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
}

public ValueTask DisposeAsync()
{
    return new ValueTask(WpfTestSta.RunOnStaAsync(static () =>
    {
        MotionHelper.OverrideIsMotionEnabled = null;
        _ = TestApp.EnsureLibraryTheme();
    }));
}
```

Delete the `MotionHelper.OverrideIsMotionEnabled = null;` line from each test's `finally` block, and delete the `try`/`finally` where that was all it did. An exception escaping a test body can then no longer poison the later animation tests. The file needs `using Fluence.Wpf.Helpers;` for `MotionHelper`; check whether it already has it.

**`BackgroundParityTests` keeps its private `AssertBrushResolves(string)` and `GetNativeDemoSurfaceBrushKeys()` helpers**, and keeps `TestApp.EnsureDemoTheme()` in the three source-lint tests at `:382,:441,:457` with the demo-opt-in comment Task 6 added. Its class lifetime uses `EnsureLibraryTheme`; those three tests call `EnsureDemoTheme` in their own bodies on top of it.

`ControlRenderingTests.cs` is renamed because "control rendering" described nothing; the three tests assert `UseLayoutRounding` and `SnapsToDevicePixels` setters survive a theme switch.

`ControlTests.IconForeground.cs:599` declares `private static Color GetControlForegroundColor(Control control)`. Inside `Fluence.Wpf.Tests.Control.Rules` that `Control` binds to the namespace, so write the parameter type out as `System.Windows.Controls.Control`.

`ReducedMotionTests` does not qualify for the Task 24 fixture.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 15 commit.
- [ ] **Step 2: Apply the Phase 3 conversion recipe to each row, with the two special cases above.**
- [ ] **Step 3: Run the Phase 3 verification checklist.** Targeted filter: the nine class names, each prefixed `Fluence.Wpf.Tests.Control.Rules.`, expect 76 passed.
- [ ] **Step 4: Commit** with subject `Split the cross-control rule tests into Control/Rules.`

---

### Task 17: Demo and window partials

**Starting commit:** the Task 16 commit.

**Interfaces:**
- Produces: `GalleryPageTests` (a temporary home, replaced by the per-page classes in Task 26), `CaptionButtonTests`, `WindowIconTests`. `RunDemoPageTestAsync` moves with `GalleryPageTests` and keeps its 22 callers inside one class for the first time.

| Source | Destination | New class | `using static` added | Cases |
| ------ | ----------- | --------- | -------------------- | ----: |
| `ControlTests.DemoParity.cs` **merged with** `ControlTests.DemoSamplePolish.cs` | `Gallery/Pages/GalleryPageTests.cs` | `GalleryPageTests` | `VisualTree` | 33 (12 + 21) |
| `ControlTests.CaptionButtons.cs` | `Windowing/CaptionButtonTests.cs` | `CaptionButtonTests` | `VisualTree` | 6 |
| `ControlTests.WindowIcon.cs` | `Windowing/WindowIconTests.cs` | `WindowIconTests` | `VisualTree` | 3 |

`GalleryPageTests` is a demo-subject class: both lifetime members call `TestApp.EnsureDemoTheme()`. It exists so that `RunDemoPageTestAsync`, defined at `ControlTests.DemoParity.cs:356` and called 22 times from `ControlTests.DemoSamplePolish.cs`, stops being a cross-file dependency inside a partial. Task 26 splits it per gallery page; do not attempt that split here.

`CaptionButtonTests` and `WindowIconTests` land in `Windowing/`, namespace `Fluence.Wpf.Tests.Windowing`, which Task 20 fills out with the rest of that family. Create the folder here if it does not exist yet.

`ControlTests.DemoParity.cs:316` declares `private static void AssertControlHasThemedBorder(Control control)`. Inside `Fluence.Wpf.Tests.Gallery.Pages` that `Control` binds to the `Fluence.Wpf.Tests.Control` namespace, so write the parameter type out as `System.Windows.Controls.Control`.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 16 commit.
- [ ] **Step 2: Apply the Phase 3 conversion recipe to each row.**
- [ ] **Step 3: Run the Phase 3 verification checklist.** Targeted filter: `--filter-class Fluence.Wpf.Tests.Gallery.Pages.GalleryPageTests Fluence.Wpf.Tests.Windowing.CaptionButtonTests Fluence.Wpf.Tests.Windowing.WindowIconTests`, expect 42 passed.
- [ ] **Step 4: Commit** with subject `Split the demo parity and window chrome tests out of the ControlTests partial.`

---

### Task 18: Empty `ControlTests.cs` and `AdditionalControlsTests.cs`

**Starting commit:** the Task 17 commit.

**Interfaces:**
- Consumes: every class Tasks 7 to 17 produced.
- Produces: no `partial class ControlTests` anywhere in the tree, and the new classes `FontIconTests`, `TextBlockExtensionsTests`, `LayoutPrimitiveTests`, `TabControlTests` and `DropDownButtonTests` in `Fluence.Wpf.Tests.Control`, plus `DemoShellTests` in `Fluence.Wpf.Tests.Gallery`. `DemoShellTests` is one of the seven Lane A classes, but three of the other six do not exist until Task 26, so from this commit the lanes switch to the first interim nine-class split defined just after this task.

`ControlTests.cs` holds 78 tests over seventeen subjects. Every one has a destination class that already exists, except the six new ones. Distribute exactly as follows; the line numbers are those in the file at the branch point.

| Destination class | File | Methods moved (line at branch point) |
| ----------------- | ---- | ------------------------------------ |
| `FontIconTests` (new) | `Control/FontIconTests.cs` | `FontIcon_DefaultFontFamily_IsSegoeFluentAsync` (200), `FontIcon_GlyphProperty_RoundtripsAsync` (211), `Stage3_FontIcon_Rotation_RoundtripsAsync` (1821), `Stage3_FontIcon_IsSpinning_RoundtripsAsync` (1831), `Stage3_FontIcon_Spin_PausesWhenCollapsed_ResumesWhenVisibleAsync` (1841), `Stage3_FontIcon_Spin_StopsWhenUnloadedAsync` (1883), `Stage3_FontIcon_EnableTransitions_DefaultTrueAsync` (1921) |
| `ButtonTests` | `Control/ButtonTests.cs` | 225, 236, 581, 621, 657, 694, 732 |
| `TextBoxTests` | `Control/TextBoxTests.cs` | 250, 264, 277, 314, 533, 1789, 1799, 1931 |
| `ListViewTests` | `Control/ListViewTests.cs` | 352, 363, 374, 411, 450, 1811, 1965 |
| `TextBlockExtensionsTests` (new) | `Control/TextBlockExtensionsTests.cs` | `TextBlockExtensions_Typography_SetsCorrectFontSizeAsync` (506) |
| `TabControlTests` (new) | `Control/TabControlTests.cs` | `FluentTabControl_SelectedTabUsesFluentCardSurfaceAsync` (984), `FluentTabControl_SelectedHeaderUsesSequentialPanelAndCenteredIndicatorAsync` (1033), `FluentTabControl_LeftPlacement_SeparatesHeadersAndContentAsync` (1085), `FluentTabControl_BottomPlacement_LeavesBorderBreathingRoomAsync` (1129) |
| `CardTests` | `Control/CardTests.cs` | `Stage3_Card_DefaultVariant_IsDefaultAsync` (1405), `Stage3_Card_IsClickable_ExposesIsPressedAsync` (1415) |
| `CheckBoxTests` | `Control/CheckBoxTests.cs` | `Stage3_CheckBox_Content_RoundtripsAsync` (1425) |
| `ComboBoxTests` | `Control/ComboBoxTests.cs` | 1435, 1445, 1482, 1521, 1583, 1623, 1670, 1705 |
| `ProgressBarTests` | `Control/ProgressBarTests.cs` | `Stage3_ProgressBar_ProgressMode_DefaultIsStandardAsync` (1749), `Stage3_ProgressBar_Template_HasTrackAndFillAsync` (1999) |
| `LayoutPrimitiveTests` (new) | `Control/LayoutPrimitiveTests.cs` | `Stage3_Border_Variant_DefaultIsNoneAsync` (1759), `Stage3_StackPanel_Spacing_DefaultZeroAsync` (1769), `Stage3_DockPanel_LastChildFill_DefaultTrueAsync` (1779) |
| `SliderTests` | `Control/SliderTests.cs` | `Slider_Template_HasTrackAsync` (2028) |
| `HyperlinkButtonTests` | `Control/HyperlinkButtonTests.cs` | 2166, 2200 |
| `InfoBarTests` | `Control/InfoBarTests.cs` | 2236, 2274, 2315 |
| `RadioButtonTests` | `Control/RadioButtonTests.cs` | 2358, 2397, 2437 |
| `ToggleSwitchTests` | `Control/ToggleSwitchTests.cs` | 2477, 2520 |
| `ProgressRingTests` | `Control/ProgressRingTests.cs` | 2553, 2591 |
| `DemoShellTests` (new) | `Gallery/DemoShellTests.cs` | the fourteen `MainWindow_*` at 787, 835, 869, 909, 943, 1170, 1213, 1261, 1293, 1334, 1371, 2056, 2092, 2128, plus `DemoMainWindow_SelectingNavPage_DoesNotThrowAsync` at 2636 |

That is 7 + 7 + 8 + 7 + 1 + 4 + 2 + 1 + 8 + 2 + 3 + 1 + 2 + 3 + 3 + 2 + 2 + 15, which is 78.

`AdditionalControlsTests.cs` (11 tests, named for nothing) distributes the same way:

| Destination class | Methods moved |
| ----------------- | ------------- |
| `NumberBoxTests` | `NumberBox_DefaultStyle_LoadsPartsAsync` (53), `NumberBox_Value_RoundtripsAsync` (77) |
| `ExpanderTests` | `Expander_CornerRadius_DefaultAsync` (87), `Expander_Template_AppliesAsync` (97) |
| `DropDownButtonTests` (new, `Control/DropDownButtonTests.cs`) | `DropDownButton_Template_HasFlyoutPresenterNameAsync` (121), `DropDownButton_CloseFlyout_ClosesOpenPopupAsync` (145), `DropDownButton_FlyoutPresenter_StretchesForLeftAlignedItemsAsync` (185) |
| `SplitButtonTests` | `SplitButton_FlyoutPresenter_StretchesForLeftAlignedItemsAsync` (211) |
| `InfoBadgeTests` | `InfoBadge_Value_RoundtripsAsync` (237), `InfoBadge_Template_AppliesAsync` (247) |
| `ListBoxTests` | `ListBox_GetContainerForItemOverride_ReturnsFluentListBoxItemAsync` (271) |

- [ ] **Step 1: Executor preamble.** Expected head: the Task 17 commit.

- [ ] **Step 2: Create the six new classes**

`Control/FontIconTests.cs`, `Control/TextBlockExtensionsTests.cs`, `Control/LayoutPrimitiveTests.cs`, `Control/TabControlTests.cs`, `Control/DropDownButtonTests.cs`, `Gallery/DemoShellTests.cs`. Each starts with the 27-line BSD header, then `using Fluence.Wpf.Tests.Infrastructure;` in its `using` block, then the namespace that matches its folder (`Fluence.Wpf.Tests.Control` for the five, `Fluence.Wpf.Tests.Gallery` for `DemoShellTests`), then the two `IAsyncLifetime` members from the recipe. `DemoShellTests` uses `TestApp.EnsureDemoTheme()` in both members.

- [ ] **Step 3: Move all 78 methods and all 11 methods, verbatim**

Move each method body byte for byte apart from the deleted `_ = TestApp.EnsureLibraryTheme();` or `_ = TestApp.EnsureDemoTheme();` line, per the recipe. Do not rename a single method: the name diff must stay empty.

Also move the private helper `GetNavigationViewItemsHostPanel` (`ControlTests.cs:187`) into `NavigationViewTests` as an `internal static` member if Task 14 has not already claimed it, and update `NavigationViewTopModeTests` accordingly.

- [ ] **Step 4: Delete the two emptied files**

```
git rm Fluence.Wpf.Tests/ControlTests.cs
```
```
git rm Fluence.Wpf.Tests/AdditionalControlsTests.cs
```

- [ ] **Step 5: Prove the partial is gone**

Run: `git grep -n "class ControlTests" -- Fluence.Wpf.Tests`
Expected: no output.

Run: `git grep -l "ControlTests\." -- Fluence.Wpf.Tests`
Expected: no output.

- [ ] **Step 6: Build**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 7: Run the two lanes, now with the interim nine-class split, on both TFMs**

`ControlTests` no longer exists, so the pre-consolidation split is dead from this commit. Three of the seven final Lane A names are not settled until Task 26, so use the first interim nine-class split defined in the section immediately after this task, substituting those nine names into the Lane A and Lane B commands from Global Constraints.

Expected: the two case counts sum to **1197** on net10 and **1195** on net472. 0 failed apart from the known `net472` TimePicker flake. 5 `NotRunnable`, 3 `NotExecuted`.

- [ ] **Step 8: Name diff on both TFMs**

Expected: **no output**. This is the single most important check in the phase: 89 methods moved between classes and none was renamed.

- [ ] **Step 9: Format and text policy**

Run: `dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore`
Run: `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll`
Byte-check the six new files for `EF BB BF`.

- [ ] **Step 10: Commit**

```
git add -A
```
```
git commit -m "Retire the ControlTests partial."
```

Commit body to include:

```
ControlTests.cs was 2749 lines and 78 tests across seventeen unrelated
subjects, and it owned the helpers that sixty other partials of the same class
depended on. AdditionalControlsTests.cs was eleven tests across six controls
and was named for nothing.

Both are distributed to the class that owns their subject. Six new classes are
created for subjects that had no home: FontIconTests, TextBlockExtensionsTests,
LayoutPrimitiveTests, TabControlTests, DropDownButtonTests and DemoShellTests.

No method is renamed and no assertion changes, so the --list-tests method-name
multiset is identical to the branch-point baseline.
```
### Interim lanes, Tasks 18 to 25

`ControlTests` no longer exists after Task 18, and three of the seven final Lane A class names are not settled until Task 26. Tasks 18 to 25 therefore use a nine-class interim split. It comes in two forms, because Task 21 moves `DemoMainWindowTests` and `DemoSamplePageWiringTests` from the project root into `Gallery/` and their namespace changes with them. Substitute the matching list into the Lane A and Lane B commands from Global Constraints.

**Tasks 18 to 20:**

```
Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.DemoMainWindowTests Fluence.Wpf.Tests.DemoSamplePageWiringTests Fluence.Wpf.Tests.Gallery.Pages.GalleryPageTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests
```

**Tasks 21 to 25**, the same nine classes with the two demo classes now under `Gallery/`:

```
Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoMainWindowTests Fluence.Wpf.Tests.Gallery.DemoSamplePageWiringTests Fluence.Wpf.Tests.Gallery.Pages.GalleryPageTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests
```

A lane filter that names a class which does not exist discovers fewer cases than expected, and the lane-sum check fails in the same task. That is the intended safety net; do not relax it.

From Task 26 onward, use the seven-class list in Global Constraints.

---

## Phase 4: move the remaining files into their folders

One folder per task. Contents are unchanged apart from three things: each moved file's namespace line, which must match its new folder because IDE0130 is an error; the bare `Control` references Global Constraints lists, which are written out as `System.Windows.Controls.Control`; and the four class merges the spec's section 2.1 layout requires, which are called out row by row.

### Task 19: `Theming/`

**Starting commit:** the Task 18 commit, subject `Retire the ControlTests partial.`

**Interfaces:**
- Produces: `Theming/AccentTests.cs` holding `public sealed class AccentTests`, which Task 23 adds the captured OS ramp fixtures to. Every file under `Theming/` in the namespace `Fluence.Wpf.Tests.Theming`.

| Source | Destination | Change |
| ------ | ----------- | ------ |
| `Theming/DesignTimeResourceTests.cs` | same path | no change; its namespace already matches its folder |
| `Theming/DesignTimeResourceWriter.cs` | same path | no change |
| `Theming/RedundantPublishGateTests.cs` | same path | no change |
| `Theming/ThemeEngineUnitTests.cs` | same path | no change |
| `Theming/ThemeParityTests.cs` | same path | no change |
| `ThemeManagerTests.cs` | `Theming/ThemeManagerTests.cs` | `git mv`, then `namespace Fluence.Wpf.Tests` becomes `namespace Fluence.Wpf.Tests.Theming` |
| `ThemeMetricsTests.cs` | `Theming/ThemeMetricsTests.cs` | `git mv` plus the same namespace line |
| `ThemeMarkupTests.cs` | `Theming/ThemeMarkupTests.cs` | `git mv` plus the same namespace line |
| `DictionaryStabilityTests.cs` | `Theming/DictionaryStabilityTests.cs` | `git mv` plus the same namespace line |
| `TypographyResourceContractTests.cs` | `Theming/TypographyResourceContractTests.cs` | `git mv` plus the same namespace line |
| `ThemeTestHelpersTests.cs` | `Theming/ThemeTestHelpersTests.cs` | `git mv` plus the same namespace line |
| `TextRenderingPolicyTests.cs` | `Theming/TextRenderingPolicyTests.cs` | `git mv`, the same namespace line, and the three bare `Control` parameters at `:320`, `:343` and `:357` written out as `System.Windows.Controls.Control` |
| `AccentColorManagerTests.cs` **merged with** `AccentRampTests.cs` | `Theming/AccentTests.cs`, class `AccentTests`, namespace `Fluence.Wpf.Tests.Theming` | 12 methods, **40 cases** (7 methods and 7 cases from `AccentColorManagerTests`; 5 methods and 33 cases from `AccentRampTests`, four of which are theories) |

The merged `AccentTests` keeps `AccentColorManagerTests`'s `IAsyncLifetime` members, which the five pure ramp-math tests then also pay. Their measured cost is 0 s, so the isolation is free.

The five files already under `Theming/` keep their namespace line exactly as it is: it already matches the folder, and flattening it is what IDE0130 forbids. The seven files moving in take that namespace, and each keeps the `using Fluence.Wpf.Tests.Infrastructure;` Task 1 gave it.

Nothing outside `Theming/` references a type declared in `Fluence.Wpf.Tests.Theming`. Confirm with `git grep -n "Fluence.Wpf.Tests.Theming" -- Fluence.Wpf.Tests`, whose only hits should be the namespace lines themselves, so no `using` needs adding anywhere else and none needs removing.

`AccentPaletteRegenerationExperiment.cs`, `AccentRampScoreboard.cs` and `ImmersiveColorSetProbe.cs` stay at the project root. Task 23 deletes them; moving them first would only make that diff harder to read.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 18 commit.
- [ ] **Step 2: `git mv` the seven files and set each one's namespace to `Fluence.Wpf.Tests.Theming`. Leave the five files already under `Theming/` untouched. Qualify the three bare `Control` parameters in `TextRenderingPolicyTests.cs`.**
- [ ] **Step 3: Merge `AccentColorManagerTests` and `AccentRampTests` into `Theming/AccentTests.cs`, then `git rm` both sources.**
- [ ] **Step 4: Build.** `dotnet build Fluence.Wpf.sln -c Debug`, expect `0 Warning(s)` / `0 Error(s)`.
- [ ] **Step 5: Targeted run on both TFMs.** `--filter-class Fluence.Wpf.Tests.Theming.AccentTests Fluence.Wpf.Tests.Theming.ThemeManagerTests Fluence.Wpf.Tests.Theming.ThemeMetricsTests Fluence.Wpf.Tests.Theming.ThemeMarkupTests Fluence.Wpf.Tests.Theming.DictionaryStabilityTests Fluence.Wpf.Tests.Theming.TypographyResourceContractTests Fluence.Wpf.Tests.Theming.ThemeTestHelpersTests Fluence.Wpf.Tests.Theming.TextRenderingPolicyTests Fluence.Wpf.Tests.Theming.DesignTimeResourceTests Fluence.Wpf.Tests.Theming.RedundantPublishGateTests Fluence.Wpf.Tests.Theming.ThemeEngineUnitTests Fluence.Wpf.Tests.Theming.ThemeParityTests`. Expect 0 failed, 1 `NotRunnable` (the maintainer-only DesignTime writer at `Theming/DesignTimeResourceTests.cs:118`).
- [ ] **Step 6: Interim lanes on both TFMs**, using the Tasks 18 to 20 list. Expect the sums 1197 and 1195.
- [ ] **Step 7: Name diff on both TFMs.** Expected: **no output**.
- [ ] **Step 8: Format and text policy.**
- [ ] **Step 9: Commit** with subject `Gather the theming tests under Theming.`

---

### Task 20: `Windowing/`

**Starting commit:** the Task 19 commit.

**Interfaces:**
- Produces: `Windowing/WindowPolicyTests.cs`, `Windowing/FluenceWindowTests.cs`, `Windowing/TitleBarTests.cs`, `Windowing/CaptionButtonTests.cs`, `Windowing/NativeMethodsTests.cs`, `Windowing/SnapLayoutHelperTests.cs`, `Windowing/WindowIconTests.cs` (already there from Task 17), every one of them in the namespace `Fluence.Wpf.Tests.Windowing`.

| Source | Destination | Class | Cases |
| ------ | ----------- | ----- | ----: |
| `WindowPolicyTests.cs` plus `FluenceWindowHardenTests.cs:256` (`BuildBackdropPlan_Acrylic_FallsBackToMica_WhenMicaEffectButNoSystemBackdrop`) | `Windowing/WindowPolicyTests.cs` | `WindowPolicyTests` | 90 (89 + 1) |
| `FluenceWindowHardenTests.cs` (minus `:223`, `:240`, `:256`) **merged with** `FluenceWindowSizeToContentTests.cs` | `Windowing/FluenceWindowTests.cs` | `FluenceWindowTests` | 28 cases (28 minus the 3 moved out, plus 3), before the Task 23 deletions. The file's 26 methods expand to 28 cases because `FluenceWindow_BackdropNone_BackgroundMatchesApplicationBackgroundBrushAsync` at `:527` is a `[Theory]` with three `[InlineData]`; the three methods moving out are all `[Fact]`. |
| `FluenceWindowTitleBarTests.cs` (minus `:1262`, `:1294`, `:1302`, `:1311`) **merged with** `TitleBarTests.cs` | `Windowing/TitleBarTests.cs` | `TitleBarTests` | 54 (49 + 5) |
| `CaptionButtonChromeTests.cs` **merged into** `Windowing/CaptionButtonTests.cs` (from Task 17) | `Windowing/CaptionButtonTests.cs` | `CaptionButtonTests` | 12 (6 + 6) |
| `NativeMethodsTests.cs` plus `FluenceWindowTitleBarTests.cs:1294,1302,1311` (the three `MINMAXINFO` and `MONITORINFO` struct-layout tests) | `Windowing/NativeMethodsTests.cs` | `NativeMethodsTests` | 25 cases (22 + 3). The 20 methods expand to 22 cases because `ComputeMaximizedFrameMargin_NonPositiveScale_TreatedAsUnscaled` at `:183` is a `[Theory]` with three `[InlineData]`. |
| `SnapLayoutHelperTests.cs` | `Windowing/SnapLayoutHelperTests.cs` | `SnapLayoutHelperTests` | 3 |
| `FluenceWindowTitleBarTests.cs:1262` (`PasswordBox_SelectAll_DoesNotThrowWithoutTemplateAsync`) | `Control/PasswordBoxTests.cs` | `PasswordBoxTests` | 16 (15 + 1) |

Every file in the table takes `namespace Fluence.Wpf.Tests.Windowing`, except the one row that lands in `Control/PasswordBoxTests.cs`, which is already in `Fluence.Wpf.Tests.Control` from Task 8. Each keeps the `using Fluence.Wpf.Tests.Infrastructure;` Task 1 gave it.

`FluenceWindowHardenTests.cs:908` and `:930` read `Control.BorderBrushProperty`. Inside `Fluence.Wpf.Tests.Windowing` that `Control` binds to the `Fluence.Wpf.Tests.Control` namespace, so write both out as `System.Windows.Controls.Control.BorderBrushProperty`.

`FluenceWindowHardenTests.cs:223` and `:240` are **not** deleted here. They move into `Windowing/FluenceWindowTests.cs` with everything else and are deleted in Task 23, so that every deletion sits in one reviewable commit.

`WindowPolicyTests`, `CaptionButtonTests` (the `CaptionButtonChrome` half), `SnapLayoutHelperTests` and `NativeMethodsTests` are pure logic. Per the Phase 3 recipe they take **no** `IAsyncLifetime`. `CaptionButtonTests` already has one from Task 17 because its `ControlTests.CaptionButtons.cs` half shows a `FluenceWindow`; keep it, and accept that the six pure-logic tests pay a reset each. Their combined cost is 0 s.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 19 commit.
- [ ] **Step 2: Perform the seven moves and merges, then `git rm` the emptied sources: `FluenceWindowHardenTests.cs`, `FluenceWindowSizeToContentTests.cs`, `FluenceWindowTitleBarTests.cs`, `TitleBarTests.cs`, `CaptionButtonChromeTests.cs`, `NativeMethodsTests.cs`, `SnapLayoutHelperTests.cs`, `WindowPolicyTests.cs`.**
- [ ] **Step 3: Build.** Expect `0 Warning(s)` / `0 Error(s)`.
- [ ] **Step 4: Targeted run on both TFMs.** `--filter-class Fluence.Wpf.Tests.Windowing.WindowPolicyTests Fluence.Wpf.Tests.Windowing.FluenceWindowTests Fluence.Wpf.Tests.Windowing.TitleBarTests Fluence.Wpf.Tests.Windowing.CaptionButtonTests Fluence.Wpf.Tests.Windowing.NativeMethodsTests Fluence.Wpf.Tests.Windowing.SnapLayoutHelperTests Fluence.Wpf.Tests.Windowing.WindowIconTests Fluence.Wpf.Tests.Control.PasswordBoxTests`. Expect 231 passed, 0 failed (90 + 28 + 54 + 12 + 25 + 3 + 3 + 16).
- [ ] **Step 5: Interim lanes on both TFMs**, using the Tasks 18 to 20 list. Expect the sums 1197 and 1195.
- [ ] **Step 6: Name diff on both TFMs.** Expected: **no output**.
- [ ] **Step 7: Format and text policy.**
- [ ] **Step 8: Commit** with subject `Gather the window chrome tests under Windowing.`

Commit body to include:

```
Four tests were filed under a name that did not describe them: a PasswordBox
SelectAll guard and three native struct-layout checks lived in
FluenceWindowTitleBarTests, and three WindowPolicy.BuildBackdropPlan unit tests
lived in FluenceWindowHardenTests beside twenty-one siblings in
WindowPolicyTests. Each moves to the class that owns its subject.

The folder is Windowing, not Window: a namespace segment named Window would
shadow System.Windows.Window in every file that declares a test window. For the
same reason the two bare Control.BorderBrushProperty references are written out
as System.Windows.Controls.Control.BorderBrushProperty, because Control is now a
namespace segment too.
```

---

### Task 21: `Gallery/`

**Starting commit:** the Task 20 commit.

**Interfaces:**
- Produces: every demo test file under `Gallery/` or `Gallery/Pages/`, in `Fluence.Wpf.Tests.Gallery` and `Fluence.Wpf.Tests.Gallery.Pages`. Class names are unchanged in this task; Task 26 renames and redistributes them. From this commit the interim lanes switch to the Tasks 21 to 25 list.

| Source | Destination | Change |
| ------ | ----------- | ------ |
| `DemoMainWindowTests.cs` | `Gallery/DemoMainWindowTests.cs` | `git mv`, then `namespace Fluence.Wpf.Tests` becomes `namespace Fluence.Wpf.Tests.Gallery` |
| `DemoSamplePageWiringTests.cs` | `Gallery/DemoSamplePageWiringTests.cs` | `git mv` plus the same namespace line |
| `DemoResourceCleanupTests.cs` | `Gallery/DemoResourceCleanupTests.cs` | `git mv` plus the same namespace line |
| `DemoColorsPageTests.cs` | `Gallery/Pages/DemoColorsPageTests.cs` | `git mv`, then the namespace becomes `Fluence.Wpf.Tests.Gallery.Pages` |
| `GalleryPageHeaderTests.cs` | `Gallery/Pages/GalleryPageHeaderTests.cs` | `git mv` plus the same `Gallery.Pages` namespace line |

`Gallery/DemoShellTests.cs` and `Gallery/Pages/GalleryPageTests.cs` are already in place, with the right namespaces, from Tasks 18 and 17.

All five files keep the `using Fluence.Wpf.Tests.Infrastructure;` Task 1 gave them, which is what keeps `DemoTestHost`, `WpfTestSta` and `TestApp` resolving. None of them uses the bare `Control` type, so no qualification is needed here. The `Demo.MainWindow` and `Demo.Mvvm` shorthands elsewhere in the suite keep resolving to `Fluence.Wpf.Demo` precisely because this folder is `Gallery` and not `Demo`.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 20 commit.
- [ ] **Step 2: `git mv` the five files and change one line in each, the namespace, to the one its destination folder requires. Change nothing else.**
- [ ] **Step 3: Build.** Expect `0 Warning(s)` / `0 Error(s)`.
- [ ] **Step 4: Targeted run on both TFMs.** `--filter-class Fluence.Wpf.Tests.Gallery.DemoMainWindowTests Fluence.Wpf.Tests.Gallery.DemoSamplePageWiringTests Fluence.Wpf.Tests.Gallery.DemoResourceCleanupTests Fluence.Wpf.Tests.Gallery.Pages.DemoColorsPageTests Fluence.Wpf.Tests.Gallery.Pages.GalleryPageHeaderTests Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.Pages.GalleryPageTests`. Expect 116 passed, 0 failed (47 + 10 + 2 + 4 + 5 + 15 + 33).
- [ ] **Step 5: Interim lanes on both TFMs.** Switch to the Tasks 21 to 25 list from this task onward. Expect the sums 1197 and 1195.
- [ ] **Step 6: Name diff on both TFMs.** Expected: **no output**.
- [ ] **Step 7: Format and text policy.**
- [ ] **Step 8: Commit** with subject `Gather the demo tests under Gallery.`

---

### Task 22: `Tools/`

**Starting commit:** the Task 21 commit.

**Interfaces:**
- Produces: `Tools/GalleryScreenshotHarness.cs` in the namespace `Fluence.Wpf.Tests.Tools`, with only two lines changed.

| Source | Destination | Change |
| ------ | ----------- | ------ |
| `GalleryScreenshotHarness.cs` | `Tools/GalleryScreenshotHarness.cs` | `git mv`, then `namespace Fluence.Wpf.Tests` becomes `namespace Fluence.Wpf.Tests.Tools`, and `Control.BackgroundProperty` at `:197` becomes `System.Windows.Controls.Control.BackgroundProperty` |

The harness is not a test. It writes the ten PNGs committed under `docs/screenshots/`, gated on `FLUENCE_CAPTURE_SCREENSHOTS` through three `[Fact(SkipUnless = nameof(ScreenshotCaptureEnabled))]` attributes and a class-level `[Trait("Category", "Screenshots")]`. Neither the move nor the two line edits touches any of those, and the verification below proves it.

Two details specific to this file. It keeps the `using Fluence.Wpf.Tests.Infrastructure;` Task 1 gave it, for `WpfTestSta`. And it writes `Demo.MainWindow` and `Demo.Mvvm.MainWindow`, which resolve to `Fluence.Wpf.Demo` only because the demo test folder is named `Gallery`; if a `Fluence.Wpf.Tests.Demo` namespace ever appears, this file stops compiling.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 21 commit.
- [ ] **Step 2: `git mv Fluence.Wpf.Tests/GalleryScreenshotHarness.cs Fluence.Wpf.Tests/Tools/GalleryScreenshotHarness.cs`, then change the namespace line and qualify `Control.BackgroundProperty` at `:197`. Change nothing else.**
- [ ] **Step 3: Build.** Expect `0 Warning(s)` / `0 Error(s)`.
- [ ] **Step 4: Prove the gate is intact.**

Run: `Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Tools.GalleryScreenshotHarness --no-ansi --progress off`
Expected: 3 tests, 0 passed, 0 failed, 3 skipped. Do **not** set `FLUENCE_CAPTURE_SCREENSHOTS`.

Run: `git status --porcelain docs/screenshots`
Expected: no output. No PNG changed.

- [ ] **Step 5: Interim lanes on both TFMs**, using the Tasks 21 to 25 list. Expect the sums 1197 and 1195, and 3 `NotExecuted` in each run's summary.
- [ ] **Step 6: Name diff on both TFMs.** Expected: **no output**.
- [ ] **Step 7: Format and text policy.**
- [ ] **Step 8: Commit** with subject `Move the screenshot harness under Tools.`

Commit body to include:

```
GalleryScreenshotHarness is not a test. It regenerates the ten PNGs committed
under docs/screenshots and is opt-in behind FLUENCE_CAPTURE_SCREENSHOTS.
Isolating it in Tools makes that gate auditable at a glance instead of leaving
it among ninety files that do assert something.

Only the namespace line and one qualified Control reference change with the
move; the run still reports three skipped and docs/screenshots is untouched.
```

---

## Phase 5: the deletions and the two theory folds

### Task 23: Delete the 18 subsumed cases and fold the two theories

**Starting commit:** the Task 22 commit, subject `Move the screenshot harness under Tools.`

**Files:**
- Modify: `Fluence.Wpf.Tests/Theming/ThemeMetricsTests.cs`
- Modify: `Fluence.Wpf.Tests/Theming/ThemeManagerTests.cs`
- Modify: `Fluence.Wpf.Tests/Theming/DictionaryStabilityTests.cs`
- Modify: `Fluence.Wpf.Tests/Theming/AccentTests.cs`
- Modify: `Fluence.Wpf.Tests/Control/Rules/BackgroundParityTests.cs`
- Modify: `Fluence.Wpf.Tests/Windowing/FluenceWindowTests.cs`
- Modify: `Fluence.Wpf.Tests/Gallery/DemoShellTests.cs`
- Delete: `Fluence.Wpf.Tests/AccentPaletteRegenerationExperiment.cs`
- Delete: `Fluence.Wpf.Tests/ImmersiveColorSetProbe.cs`
- Delete: `Fluence.Wpf.Tests/AccentRampScoreboard.cs`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: `Theming/AccentTests.cs` from Task 19, which receives the ramp fixtures.
- Produces: the post-deletion baseline, 1180 cases on net10 and 1178 on net472. This is the only task in the plan whose name diff is non-empty.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 22 commit.

- [ ] **Step 2: D1. Delete six per-theme corner-radius tests**

In `Theming/ThemeMetricsTests.cs`, delete `ControlCornerRadius_PresentInLightThemeAsync`, `ControlCornerRadius_PresentInDarkThemeAsync`, `ControlCornerRadius_PresentInHighContrastThemeAsync`, `OverlayCornerRadius_PresentInLightThemeAsync`, `OverlayCornerRadius_PresentInDarkThemeAsync` and `OverlayCornerRadius_PresentInHighContrastThemeAsync` (branch-point lines 56, 68, 80, 96, 108, 120).

Survivor: `CornerRadiusTokens_SurviveFullThemeCycleAsync` (line 187) applies Light, Dark, HighContrast, Light and asserts `ControlCornerRadius == new CornerRadius(4)` and `OverlayCornerRadius == new CornerRadius(8)` at every step. Each deleted test asserts one of those two pairs at one theme.

- [ ] **Step 3: D2. Delete the duplicated focus-visual test**

In `Theming/ThemeMetricsTests.cs`, delete `DefaultControlFocusVisualStyle_PresentInAllThemesAsync` (line 169).

Survivor: `Control/Rules/FocusVisualTests.FocusVisual_DefaultControlFocusVisualStyle_ResolvesInAllThemesAsync`. Same loop over the three themes, same `Assert.IsType<Style>` on `DefaultControlFocusVisualStyle`. The two differed only in which reset helper they used, and Tasks 2 and 6 made both run without demo styles.

- [ ] **Step 4: D3. Delete the duplicated ProgressBar track test**

In `Control/Rules/BackgroundParityTests.cs`, delete `ProgressBar_TrackBackground_UsesWinUiStrongStrokeRoleAsync` (branch-point line 114).

Survivor: `Control/ProgressBarTests.ProgressBar_Track_FollowsBackgroundWithHalfPixelCornerRadiusAsync`. Both resolve `PART_Track` on a 240 by 24 ProgressBar and compare its background to `ControlStrongStrokeColorDefaultBrush`; the survivor also asserts `progressBar.Background` and `CornerRadius(0.5)`.

- [ ] **Step 5: D4 and fold 2. Delete two dictionary-count tests and make the survivor a theory**

Delete `Theming/ThemeManagerTests.FiveSwitches_DictionaryCountStableAsync` (line 150) and `Windowing/TitleBarTests.MergedDictionaries_CountStableAfterMultipleSwitchesAsync` (from `FluenceWindowTitleBarTests.cs:471`).

In `Theming/DictionaryStabilityTests.cs`, change `RepeatedThemeSwitches_NoDictionaryAccumulationAsync` (line 60) from a `[Fact]` to a `[Theory]` over the accent flag, so both values the three tests covered survive:

```csharp
[Theory]
[InlineData(false)]
[InlineData(true)]
public Task RepeatedThemeSwitches_NoDictionaryAccumulationAsync(bool updateAccent)
{
    return WpfTestSta.RunOnStaAsync(() =>
    {
        Application application = TestApp.EnsureLibraryTheme();
        int baselineCount = application.Resources.MergedDictionaries.Count;

        for (int index = 0; index < 10; index++)
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent);
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent);
        }

        Assert.Equal(baselineCount, application.Resources.MergedDictionaries.Count);
    });
}
```

Keep whatever the existing body already asserts; the shape above is the required change, which is the parameter, the two `[InlineData]` attributes, and threading `updateAccent` through the twenty applies in place of the literal. Note the lambda loses `static` because it now closes over the parameter.

Net case change for D4: 3 cases become 2.

- [ ] **Step 6: D5 and D6. Delete two subsumed backdrop-plan tests**

In `Windowing/FluenceWindowTests.cs`, delete `BuildBackdropPlan_None_ReturnsOpaqueBackground` (from `FluenceWindowHardenTests.cs:223`) and `BuildBackdropPlan_Mica_SupportedOs_ReturnsTransparent` (from `:240`).

Survivors: `Windowing/WindowPolicyTests.BuildBackdropPlan_None_UsesFallbackBackground_EmitsDwmsbtNone` (line 379) and `BuildBackdropPlan_Mica_Win22H2_UsesDwmSystemBackdropType_NotLegacy` (line 438). Same call, same capability shape; the deleted copies asserted only one field of the plan each, and the survivors assert `EffectiveBackdrop`, the exact fallback colour, `CaptionColor` and `SystemBackdropType` as well.

`BuildBackdropPlan_Acrylic_FallsBackToMica_WhenMicaEffectButNoSystemBackdrop` has no equivalent and Task 20 already moved it into `Windowing/WindowPolicyTests.cs`. Do not delete it.

- [ ] **Step 7: D7. Delete the duplicated demo progress test**

In `Gallery/DemoShellTests.cs`, delete `MainWindow_ProgressNumberBox_UpdatesFirstProgressBarAsync` (from `ControlTests.cs:2056`).

Survivor: `GalleryStatusPage_NumberBoxDrivesFirstProgressBarAsync`, in `Gallery/Pages/GalleryPageTests.cs` after Tasks 17 and 21. Both set `ProgressValueNumberBox` to 73 and assert `StandardProgressBar.Value == 73`; the survivor adds alignment, min and max, zero-value and toggle assertions. The deleted copy also built the whole `MainWindow` and navigated, which `MainWindow_DirectNavigation_LoadsConcretePagesAsync` already covers.

- [ ] **Step 8: D8. Delete the two dead probe files**

```
git rm Fluence.Wpf.Tests/AccentPaletteRegenerationExperiment.cs
```
```
git rm Fluence.Wpf.Tests/ImmersiveColorSetProbe.cs
```

Both are `[Fact(Explicit = true)]` probes whose own doc comments record the answer they were written to find, dated 2026-05-23: the accent palette does not regenerate, and only the active accent is exposed. Neither has ever run in CI, and the first mutates the user's system accent.

- [ ] **Step 9: D9. Delete the scoreboard, keeping its fixtures**

Before deleting, copy the eight captured OS ramp fixtures out of `AccentRampScoreboard.cs` and paste them into a comment block at the top of `Theming/AccentTests.cs`, immediately below the `using` directives and above the `namespace`, in this form:

```csharp
// Captured Windows accent ramps, measured 2026-05-23 from the OS palette on this
// hardware. Kept as data after AccentRampScoreboard was deleted: the scoring
// harness compared four candidate algorithms and could not fail, but these eight
// measurements are real and are the reference any future ramp change is judged
// against.
//
// <the eight fixture rows, copied verbatim from the Fixtures table in
//  AccentRampScoreboard.cs, one per line>
```

Copy the rows exactly as they appear, converting the C# array initialiser syntax into plain comment text. Do not paraphrase a value.

Then:

```
git rm Fluence.Wpf.Tests/AccentRampScoreboard.cs
```

Its single `[Fact]` at line 153 ended with `Assert.True(Fixtures.Length > 0)`, a tautology. It ran on every pass and could never fail.

- [ ] **Step 10: Fold 1. Collapse the three collection focus-visual tests**

In `Theming/ThemeMetricsTests.cs`, replace `DefaultCollectionFocusVisualStyle_PresentInLightThemeAsync`, `DefaultCollectionFocusVisualStyle_PresentInDarkThemeAsync` and `DefaultCollectionFocusVisualStyle_PresentInHighContrastThemeAsync` (lines 210, 221, 232) with one theory. The bodies are identical apart from the enum value:

```csharp
[Theory]
[InlineData(ApplicationTheme.Light)]
[InlineData(ApplicationTheme.Dark)]
[InlineData(ApplicationTheme.HighContrast)]
public Task DefaultCollectionFocusVisualStyle_PresentInThemeAsync(ApplicationTheme theme)
{
    return WpfTestSta.RunOnStaAsync(() =>
    {
        Application application = TestApp.EnsureLibraryTheme(theme);

        _ = Assert.IsType<Style>(application.TryFindResource("DefaultCollectionFocusVisualStyle"));
    });
}
```

Keep whatever the three bodies already assert; the shape above is the required change. Three cases before, three cases after.

- [ ] **Step 11: Add the CHANGELOG entry**

Add this under `## [Unreleased]` in `CHANGELOG.md`, in a `### Changed` section (create the section if `Unreleased` does not have one, placing it after `### Added` and before `### Fixed`):

```markdown
- Tests: the suite is reorganised into `Infrastructure/`, `Control/`, `Control/Rules/`, `Theming/`, `Windowing/`, `Gallery/` and `Tools/`, each folder also a namespace segment, with one sealed class per subject in place of the 62-file `partial class ControlTests`, and a single application and theme reset (`TestApp.EnsureLibraryTheme`, with `TestApp.EnsureDemoTheme` as the explicit demo opt-in) in place of seven divergent private merge helpers. Library control tests no longer run with the demo resource dictionary merged over the theme slots, so a demo style can no longer shadow a library brush. Eighteen test cases are deleted as strictly subsumed or as not being tests, and one case is added by a theory fold, taking net10 from 1197 to 1180 and net472 from 1195 to 1178. No surviving test's assertions changed. The deletions, with the survivor that covers each: six per-theme corner-radius assertions in `ThemeMetricsTests`, covered by `CornerRadiusTokens_SurviveFullThemeCycleAsync`, which asserts both pairs at all four steps of the cycle; `DefaultControlFocusVisualStyle_PresentInAllThemesAsync`, byte-for-byte identical to `FocusVisualTests.FocusVisual_DefaultControlFocusVisualStyle_ResolvesInAllThemesAsync`; `ProgressBar_TrackBackground_UsesWinUiStrongStrokeRoleAsync`, covered by `ProgressBarTests.ProgressBar_Track_FollowsBackgroundWithHalfPixelCornerRadiusAsync`, which also asserts the control background and the half-pixel corner radius; `FiveSwitches_DictionaryCountStableAsync` and `MergedDictionaries_CountStableAfterMultipleSwitchesAsync`, both covered by `RepeatedThemeSwitches_NoDictionaryAccumulationAsync`, which does twenty switches against five and now runs as a theory over both `updateAccent` values; `BuildBackdropPlan_None_ReturnsOpaqueBackground` and `BuildBackdropPlan_Mica_SupportedOs_ReturnsTransparent`, each asserting one field of a plan that `WindowPolicyTests` asserts in full on the same call; `MainWindow_ProgressNumberBox_UpdatesFirstProgressBarAsync`, covered by `GalleryStatusPage_NumberBoxDrivesFirstProgressBarAsync` plus `MainWindow_DirectNavigation_LoadsConcretePagesAsync`; the three `AccentPaletteRegenerationExperiment` probes and the one `ImmersiveColorSetProbe` probe, whose own doc comments record the answers they were written to find and which never ran in CI; and `AccentRampScoreboard.Score_AllAlgorithms_AgainstCapturedFixtures`, whose only assertion was `Assert.True(Fixtures.Length > 0)` and whose eight captured OS ramp fixtures are preserved as a comment block in `Theming/AccentTests.cs`.
```

- [ ] **Step 12: Build**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 13: Interim lanes on both TFMs**, using the Tasks 21 to 25 list

Expected sums: **1180** on net10 and **1178** on net472. `NotRunnable` drops from 5 to **1** (the maintainer-only DesignTime writer at `Theming/DesignTimeResourceTests.cs:118`). `NotExecuted` stays at **3**.

- [ ] **Step 14: Name diff, and check it against the allowlist**

Run the capture, extraction and `Compare-Object` for both TFMs.

Expected `Compare-Object` output: exactly the 21 `<=` rows and 4 `=>` rows listed in `Fluence.Wpf.Tests/Baselines/allowlist.md`, and nothing else. Compare row by row against that file. A single unexpected row fails the task.

- [ ] **Step 15: Refresh the committed baseline**

The allowlist has now been consumed, so later tasks diff against the new state. Overwrite the four baseline files with the post-deletion capture:

```
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --list-tests --no-ansi > Fluence.Wpf.Tests\Baselines\baseline.net10.txt
```
```
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --list-tests --no-ansi > Fluence.Wpf.Tests\Baselines\baseline.net472.txt
```

Re-run the extraction one-liner for both to regenerate `baseline.net10.methods.txt` (1180 lines) and `baseline.net472.methods.txt` (1178 lines).

Append this note to the end of `Fluence.Wpf.Tests/Baselines/allowlist.md`:

```markdown
## Status

Consumed. The baseline files beside this one were refreshed to the post-deletion
state in the same commit, so every task after this one diffs empty against them.
```

- [ ] **Step 16: Format and text policy**

Run: `dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore`
Run: `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll`

The CHANGELOG entry is long; check it for em and en dashes before running the gate.

- [ ] **Step 17: Commit**

```
git add -A
```
```
git commit -m "Delete eighteen subsumed test cases and fold two theories."
```

Commit body: the same deletion list as the CHANGELOG entry, reflowed to 80 columns.
---

## Phase 6: share what can be shared

Three tasks. Each one is reverted wholesale if the name diff or the case counts move.

### The qualification rule

A class may drop its per-test `IAsyncLifetime` in favour of `IClassFixture<LightThemeFixture>` only if **no test in it** applies a theme, changes the accent intent, or toggles `MotionHelper.OverrideIsMotionEnabled`. Derive the list mechanically rather than by eye:

```
git grep -l -E "ApplyStandardThemeCycle|ApplicationThemeManager\.Apply|ApplicationAccentColorManager\.|OverrideIsMotionEnabled|AssertKeyThemeBrushesResolve" -- Fluence.Wpf.Tests/Control Fluence.Wpf.Tests/Windowing
```

Every file that command lists is **disqualified**. Every file under those folders that it does not list qualifies.

One preparatory edit changes the answer for several classes, and it is part of Task 24. Many test bodies carry a redundant

```csharp
ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
```

which re-applies exactly the theme the class lifetime already applied. Delete those. Do **not** delete an `Apply` that requests a different theme or backdrop, and do **not** delete an `ApplicationAccentColorManager.ApplyCustomAccent` or `ApplySystemAccent`: those pin state the fixture cannot provide, and the class stays disqualified.

### Task 24: `LightThemeFixture` and the qualifying `Control/` classes

**Starting commit:** the Task 23 commit, subject `Delete eighteen subsumed test cases and fold two theories.`

**Files:**
- Create: `Fluence.Wpf.Tests/Infrastructure/LightThemeFixture.cs`
- Modify: the qualifying classes under `Fluence.Wpf.Tests/Control/`

**Interfaces:**
- Consumes: `TestApp.EnsureLibraryTheme()`, `WpfTestSta.RunOnStaAsync(Action)`.
- Produces: `public sealed class LightThemeFixture : IAsyncLifetime`, consumed as `IClassFixture<LightThemeFixture>`.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 23 commit.

- [ ] **Step 2: Measure the net10 wall clock before the change**

Run both interim lanes on net10, backgrounded, and record each run's reported wall clock. The sum is the "before" number. Task 26 compares against it. A phase that increases wall clock is a defect.

- [ ] **Step 3: Write `Fluence.Wpf.Tests/Infrastructure/LightThemeFixture.cs`**

Start with the same 27-line BSD header, copied verbatim from `Fluence.Wpf.Tests/Infrastructure/WpfTestSta.cs` lines 1 to 27, then:

```csharp
using System.Threading.Tasks;
using Xunit;

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// One reset and one Light apply per test class, for classes in which no test applies a theme,
    /// changes the accent intent, or toggles reduced motion. Those classes pay the fixture cost
    /// once instead of once per test. A class that mutates any of that state keeps its own
    /// per-test <see cref="IAsyncLifetime"/> and must not take this fixture.
    /// </summary>
    public sealed class LightThemeFixture : IAsyncLifetime
    {
        /// <summary>
        /// Resets the application and applies the Light theme on the shared STA thread, once for
        /// the whole class. xunit constructs fixtures on the runner thread, so the work is
        /// marshalled through <see cref="WpfTestSta.RunOnStaAsync(System.Action)"/>.
        /// </summary>
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        /// <summary>
        /// Closes any window the class left open and returns the application to a known Light
        /// state so the next class does not inherit this one's tree.
        /// </summary>
        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }
    }
}
```

- [ ] **Step 4: Delete the redundant same-theme applies under `Control/`**

Run: `git grep -n "ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);" -- Fluence.Wpf.Tests/Control`

Delete each occurrence that sits inside a test body, because the class lifetime already applied exactly that. Leave any occurrence inside a loop over several themes, and leave any `Apply` naming a different theme or backdrop.

Build after this step alone and run the affected classes; a test that was silently relying on a second apply to settle a `DynamicResource` will fail here, and that failure belongs to this step, not to the fixture change.

- [ ] **Step 5: Re-derive the qualifying list**

Run the `git grep -l` command from the qualification rule above, restricted to `Fluence.Wpf.Tests/Control`. Every `Control/*.cs` file it does **not** list qualifies.

Derived from the tree at planning time, and expected to be the answer after Step 4, the qualifying `Control/` classes are: `CardTests`, `CheckBoxTests`, `ExpanderTests`, `HyperlinkButtonTests`, `InfoBadgeTests`, `ListBoxTests`, `NumberBoxTests`, `SliderTests`, `SplitButtonTests`, `TabViewTests`, `ToggleSwitchTests`, `FontIconTests`, `TextBlockExtensionsTests`, `LayoutPrimitiveTests`, `TabControlTests`, `DropDownButtonTests`.

If the grep disagrees with that list, the grep wins. Record the difference in the task report.

Explicitly **not** qualifying, and why, so the list is not silently widened: `ListViewTests` and `RadioButtonTests` pin the accent with `ApplyCustomAccent(#0078D4)`; `TextBoxTests`, `ProgressBarTests`, `ProgressRingTests`, `ComboBoxTests`, `AutoSuggestBoxTests`, `ImageTests`, `MenuTests`, `PersonPictureTests`, `RatingControlTests`, `SeparatorTests`, `ToggleButtonTests`, `ToggleSplitButtonTests`, `TreeViewTests`, `LoopingSelectorTests`, `ScrollBarTests`, `BreadcrumbBarTests`, `ContextMenuTests`, `FlyoutTests`, `InfoBarTests`, `ToolTipTests`, `ColorPickerTests`, `DatePickerTests`, `TimePickerTests`, `ContentDialogTests`, `CommandBarFlyoutTests`, `TeachingTipTests`, `PipsPagerTests`, `PasswordBoxTests`, `NavigationViewTests` and `NavigationViewTopModeTests` all cycle themes, drive input, or both.

- [ ] **Step 6: Convert each qualifying class**

For each, delete the two `IAsyncLifetime` members and the `IAsyncLifetime` base, and take the fixture instead:

```csharp
public sealed class CardTests : IClassFixture<LightThemeFixture>
```

Do not add a constructor parameter: none of these classes reads anything off the fixture. Confirm that every test in the class still closes the window it opened, through `CloseWindowAndDrain(window)` or `w.Close()`; if one does not, add the close rather than keeping the per-test reset.

- [ ] **Step 7: Build**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: `0 Warning(s)`, `0 Error(s)`.

`xUnit1041` (fixture argument not used) does not fire when the class takes no constructor. If any analyzer objects to an unused fixture type parameter, stop and report rather than suppressing.

- [ ] **Step 8: Run every converted class on both TFMs, twice**

Run the sixteen class names, each prefixed `Fluence.Wpf.Tests.Control.`, in one `--filter-class` invocation, then run them again in reverse order using two invocations that split the list, to shake out order dependence introduced by sharing. Expect the same pass count both times, 0 failed.

- [ ] **Step 9: Interim lanes on both TFMs**, using the Tasks 21 to 25 list

Expected sums: **1180** and **1178**. Record the net10 wall clock and compare with Step 2.

- [ ] **Step 10: Name diff on both TFMs**

Expected: **no output**.

- [ ] **Step 11: Format and text policy.** Byte-check `Fluence.Wpf.Tests\Infrastructure\LightThemeFixture.cs`.

- [ ] **Step 12: Commit** with subject `Share one Light theme apply per class where no test mutates the theme.`

Commit body to include:

```
Sixteen Controls classes contain no test that applies a theme, changes the
accent intent or toggles reduced motion. They now pay one reset and one Light
apply per class through IClassFixture<LightThemeFixture> instead of one per
test.

Redundant in-body applies of the theme the lifetime had already applied are
deleted in the same commit; they were the reason several of these classes
looked as though they mutated the theme.

Wall clock, net10, two lanes summed: <before> to <after>.
```

---

### Task 25: The qualifying `Control/Rules/` and `Windowing/` classes

**Starting commit:** the Task 24 commit.

**Files:**
- Modify: the qualifying classes under `Fluence.Wpf.Tests/Control/Rules/` and `Fluence.Wpf.Tests/Windowing/`

**Interfaces:**
- Consumes: `LightThemeFixture` from Task 24.
- Produces: no new API.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 24 commit.

- [ ] **Step 2: Delete the redundant same-theme applies under `Control/Rules/` and `Windowing/`**

Same edit as Task 24 Step 4, over those two folders.

- [ ] **Step 3: Re-derive the qualifying list**

Run the `git grep -l` command from the qualification rule over `Fluence.Wpf.Tests/Control/Rules` and `Fluence.Wpf.Tests/Windowing`.

Derived at planning time, the qualifying classes are: `AccessibilityNameTests`, `FluentStrokeTests`, `PopupCornerRadiusTests`, `AutomationPeerTests`, `CaptionButtonTests`, `WindowIconTests`. If the grep disagrees, the grep wins.

Explicitly not qualifying: `FocusVisualTests`, `IconForegroundTests`, `BackgroundParityTests`, `ReducedMotionTests` and `CrispRenderingTests` all loop over themes or toggle motion. `TitleBarTests` and `FluenceWindowTests` create and destroy real `FluenceWindow` instances per test and keep their per-test lifetime, whatever the grep says: a shared application across a window-lifetime suite is exactly the state the suite is testing.

`WindowPolicyTests`, `NativeMethodsTests` and `SnapLayoutHelperTests` have no lifetime at all and take no fixture. Leave them alone.

- [ ] **Step 4: Convert each qualifying class** as in Task 24 Step 6.

- [ ] **Step 5: Build.** Expect `0 Warning(s)` / `0 Error(s)`.

- [ ] **Step 6: Run every converted class on both TFMs, twice, in two different orders.** Expect the same pass count both times, 0 failed.

- [ ] **Step 7: Interim lanes on both TFMs**, using the Tasks 21 to 25 list. Expected sums: **1180** and **1178**.

- [ ] **Step 8: Name diff on both TFMs.** Expected: **no output**.

- [ ] **Step 9: Format and text policy.**

- [ ] **Step 10: Commit** with subject `Share the class theme apply across the cross-control and window chrome suites.`

---

### Task 26: One host window per gallery page

**Starting commit:** the Task 25 commit.

**Files:**
- Create: `Fluence.Wpf.Tests/Gallery/Pages/GalleryHomePageTests.cs`, `GalleryIconsPageTests.cs`, `GalleryTypographyPageTests.cs`, `GalleryStatusPageTests.cs`, `GalleryNavigationPageTests.cs`, `GalleryTabsPageTests.cs`, `GalleryAccessibilityPageTests.cs`, `GallerySettingsPageTests.cs`
- Rename: `Fluence.Wpf.Tests/Gallery/Pages/DemoColorsPageTests.cs` to `GalleryColorsPageTests.cs`, class `GalleryColorsPageTests`
- Rename: `Fluence.Wpf.Tests/Gallery/DemoSamplePageWiringTests.cs` to `DemoSampleContractTests.cs`, class `DemoSampleContractTests`, namespace unchanged at `Fluence.Wpf.Tests.Gallery`
- Delete: `Fluence.Wpf.Tests/Gallery/Pages/GalleryPageTests.cs` (emptied)
- Delete: `Fluence.Wpf.Tests/Gallery/DemoMainWindowTests.cs` (emptied)
- Modify: `Fluence.Wpf.Tests/Gallery/DemoShellTests.cs`

**Interfaces:**
- Consumes: `DemoTestHost.CreateHostWindow`, `DemoTestHost.CloseWindow`, `DemoTestHost.FindByName`, `TestApp.EnsureDemoTheme`.
- Produces: the seven final Lane A class names, so from this commit the lanes switch to the seven-class list in Global Constraints.

**Why:** each gallery page is currently constructed and shown by two different source files, `GalleryPageTests` (from `ControlTests.DemoParity.cs` plus `ControlTests.DemoSamplePolish.cs`) and `DemoMainWindowTests`. One file per page halves that, and each page class then builds its page once in `InitializeAsync` and reuses it. This is the single largest wall-clock item in the plan.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 25 commit.

- [ ] **Step 2: Split `Gallery/DemoMainWindowTests.cs` by subject**

Its 47 tests divide in two. The page tests, identified by their `Gallery<Page>_` prefix and located at these branch-point lines, move to the matching new page class:

| New class | Tests moved from `DemoMainWindowTests.cs` |
| --------- | ----------------------------------------- |
| `GalleryStatusPageTests` | `:1369`, `:1400`, `:1435`, `:1473` |
| `GallerySettingsPageTests` | `:762`, `:847`, `:897`, `:1637`, `:1677`, `:1718`, `:1761` |
| `GalleryTypographyPageTests` | `:1580`, `:1607` |
| `GalleryNavigationPageTests` | `:964`, `:1512` |
| `GalleryTabsPageTests` | `:1541` |
| `GalleryAccessibilityPageTests` | `:1792` |
| `GalleryIconsPageTests` | `:1853` |
| `GalleryHomePageTests` | `:121`, `:168` |
| `DemoSampleContractTests` | `:1224`, `:1266`, `:1302` (the three `DemoSampleControl_*`) |

Everything else in the file, 24 tests whose subject is the shell, its title bar, its navigation or its theme switching, moves into `Gallery/DemoShellTests.cs` beside the fifteen Task 18 put there. `git rm Fluence.Wpf.Tests/Gallery/DemoMainWindowTests.cs` once it is empty.

Move the file's private helpers with the classes that use them: `CreateShownMainWindow` (branch-point `:1966`) and `CreateHostWindow` (`:1984`) go to `DemoShellTests` as `internal static` members, and the page classes call them as `DemoShellTests.CreateShownMainWindow()` where they need the shell. A page class that only needs its own page uses `DemoTestHost.CreateHostWindow` instead.

- [ ] **Step 3: Split `Gallery/Pages/GalleryPageTests.cs` by page**

Its 33 tests (12 from `ControlTests.DemoParity.cs`, 21 from `ControlTests.DemoSamplePolish.cs`) move into the same eight page classes by the page each one drives: Icons, Navigation, Status, Tabs, Accessibility and the rest, matching the `Gallery<Page>_` prefix each test name already carries. `DemoSamplePolish`'s `DemoSampleControl_SourceExpander` test (branch-point `ControlTests.DemoSamplePolish.cs:73`) goes to `DemoSampleContractTests`.

`RunDemoPageTestAsync` (branch-point `ControlTests.DemoParity.cs:356`) is the shared page host. Move it to `Infrastructure/DemoTestHost.cs` as `internal static Task RunDemoPageTestAsync(...)`, keeping its signature, so all eight page classes reach it through the one helper class rather than one of them re-exporting it. It lands in `Fluence.Wpf.Tests.Infrastructure`, which every page class already imports.

Two cross-namespace notes for this task. The eight page classes are in `Fluence.Wpf.Tests.Gallery.Pages` and call `DemoShellTests.CreateShownMainWindow()`, which is in `Fluence.Wpf.Tests.Gallery`; that resolves through the enclosing namespace with no `using`, because `Gallery.Pages` is nested inside `Gallery` by name. The bare `Control` parameter that Task 17 qualified in `AssertControlHasThemedBorder` moves with whichever page class takes it and stays written out as `System.Windows.Controls.Control`.

`git rm Fluence.Wpf.Tests/Gallery/Pages/GalleryPageTests.cs` once it is empty.

- [ ] **Step 4: Give each page class one host window**

Each of the eight new page classes plus `GalleryColorsPageTests` takes this shape. The page type and the class name change per file; nothing else does.

```csharp
// In Gallery/Pages/GalleryIconsPageTests.cs, namespace Fluence.Wpf.Tests.Gallery.Pages.
public sealed class GalleryIconsPageTests : IAsyncLifetime
{
    private Window? _host;
    private GalleryIconsPage? _page;

    public ValueTask InitializeAsync()
    {
        return new ValueTask(WpfTestSta.RunOnStaAsync(() =>
        {
            _ = TestApp.EnsureDemoTheme();
            _page = new GalleryIconsPage();
            _host = DemoTestHost.CreateHostWindow(_page);
        }));
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(WpfTestSta.RunOnStaAsync(() =>
        {
            if (_host is not null)
            {
                DemoTestHost.CloseWindow(_host);
                _host = null;
            }

            _page = null;
        }));
    }
}
```

Each test body then reads `_page` and `_host` instead of building its own. Where a test **mutates** the page, notably the Status page's NumberBox drive, that test builds and closes its own page inside its body and leaves `_page` alone; add a comment on that test saying so:

```csharp
// This test drives the page's NumberBox, so it builds its own instance rather
// than mutating the one the class shares.
```

The class fields are instance state, so these classes take `IAsyncLifetime`, not `IClassFixture<LightThemeFixture>`.

- [ ] **Step 5: Rename the two classes**

```
git mv Fluence.Wpf.Tests/Gallery/Pages/DemoColorsPageTests.cs Fluence.Wpf.Tests/Gallery/Pages/GalleryColorsPageTests.cs
```
```
git mv Fluence.Wpf.Tests/Gallery/DemoSamplePageWiringTests.cs Fluence.Wpf.Tests/Gallery/DemoSampleContractTests.cs
```

Rename the class inside each to match the file, and give `GalleryColorsPageTests` the one-host-window shape from Step 4. Neither file changes namespace: `GalleryColorsPageTests` stays in `Fluence.Wpf.Tests.Gallery.Pages` and `DemoSampleContractTests` stays in `Fluence.Wpf.Tests.Gallery`.

- [ ] **Step 6: Build.** Expect `0 Warning(s)` / `0 Error(s)`.

- [ ] **Step 7: Run every demo class on both TFMs, twice, in two different orders**

`--filter-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Gallery.DemoResourceCleanupTests Fluence.Wpf.Tests.Gallery.Pages.GalleryPageHeaderTests Fluence.Wpf.Tests.Gallery.Pages.GalleryHomePageTests Fluence.Wpf.Tests.Gallery.Pages.GalleryColorsPageTests Fluence.Wpf.Tests.Gallery.Pages.GalleryIconsPageTests Fluence.Wpf.Tests.Gallery.Pages.GalleryTypographyPageTests Fluence.Wpf.Tests.Gallery.Pages.GalleryStatusPageTests Fluence.Wpf.Tests.Gallery.Pages.GalleryNavigationPageTests Fluence.Wpf.Tests.Gallery.Pages.GalleryTabsPageTests Fluence.Wpf.Tests.Gallery.Pages.GalleryAccessibilityPageTests Fluence.Wpf.Tests.Gallery.Pages.GallerySettingsPageTests`

Expect the same pass count both times, 0 failed. A shared page that one test mutated shows up here as an order-dependent failure.

- [ ] **Step 8: Switch to the final lanes and run them on both TFMs**

Use the Lane A and Lane B commands from Global Constraints verbatim, with the seven-class list. All seven names now exist.

Expected sums: **1180** on net10 and **1178** on net472.

- [ ] **Step 9: Name diff on both TFMs.** Expected: **no output**. Roughly 80 methods moved between classes and none was renamed.

- [ ] **Step 10: Measure and report the wall clock**

Record the summed net10 two-lane wall clock and compare it with the number Task 24 Step 2 captured and with the pre-consolidation 3 min 29 s. The spec's target is at least 20 percent off. If the number is higher than Task 24's, treat it as a defect and report before committing.

- [ ] **Step 11: Format and text policy.** Byte-check the eight new files.

- [ ] **Step 12: Commit** with subject `Host each gallery page once per test class.`

Commit body to include:

```
Every gallery page was constructed and shown by two different source files, one
descended from ControlTests.DemoParity or ControlTests.DemoSamplePolish and one
from DemoMainWindowTests. There is now one class per page, each building its
page once in InitializeAsync and reusing it; the two tests that mutate their
page build their own instance and say so.

DemoMainWindowTests and GalleryPageTests are emptied and removed.
DemoSamplePageWiringTests becomes DemoSampleContractTests and absorbs the four
DemoSampleControl tests that were scattered across two files.

Wall clock, net10, two lanes summed: <before> to <after>.
```

---

## Phase 7: documentation, CI, and the merge back

### Task 27: Update the handbook and the contributor docs

**Starting commit:** the Task 26 commit, subject `Host each gallery page once per test class.`

**Files:**
- Modify: `AGENTS.md` section 6, section 9, section 13.2
- Modify: `docs/contributing.md` lines 3 to 9, 24 to 31
- Modify: `Fluence.Wpf.Tests/README.md`
- Modify: `.github/PULL_REQUEST_TEMPLATE.md` lines 8 and 9

**Interfaces:**
- Consumes: the folder layout and helper names every earlier task produced.
- Produces: no code.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 26 commit.

- [ ] **Step 2: Replace `AGENTS.md` section 6**

Replace the whole of section 6 (`## 6. Testing`, from its heading down to the line before `## 7. Build and run`) with:

```markdown
## 6. Testing

- **Framework**: xunit.v3 4.0.0 (`xunit.v3` / `xunit.runner.visualstudio`) via `Microsoft.NET.Test.Sdk` 18.8.1, running on Microsoft Testing Platform.
- **TFMs**: `net472` **and** `net10.0-windows10.0.26100.0`; both must pass.
- **Invocation**: run the built executable, not `dotnet test`. The SDK 10 VSTest bridge is gone.

  ```powershell
  Fluence.Wpf.Tests\bin\Debug\<tfm>\Fluence.Wpf.Tests.exe --filter-class <FullName> --no-ansi --progress off
  ```

  `--filter-class` and `--filter-not-class` take several space-separated class names after one flag.
- **The two lanes.** A single-process run of the whole `net472` assembly aborts with exit `-1` at a non-deterministic point (see `KNOWN_ISSUES.md`). Both TFMs therefore run as two complementary lanes whose union is provably the whole assembly, so a newly added class lands in lane B automatically rather than going unrun. Lane A is the seven costliest classes: `Fluence.Wpf.Tests.Gallery.DemoShellTests`, `Fluence.Wpf.Tests.Gallery.DemoSampleContractTests`, `Fluence.Wpf.Tests.Control.NavigationViewTests`, `Fluence.Wpf.Tests.Control.ProgressBarTests`, `Fluence.Wpf.Tests.Control.ContentDialogTests`, `Fluence.Wpf.Tests.Control.ColorPickerTests`, `Fluence.Wpf.Tests.Control.TimePickerTests`. Lane B is `--filter-not-class` over the same seven. Both lanes carry `--filter-not-trait "Category=Screenshots"`. Sum the two case counts per TFM and compare against the expected total.
- **Parallelization**: `[assembly: Parallelization(Mode = ParallelMode.None)]` lives in `Fluence.Wpf.Tests/Properties/AssemblyInfo.cs`, `xunit.runner.json` disables assembly and collection parallelism, and the test project sets `<TestTfmsInParallel>false</TestTfmsInParallel>`. WPF's shared `ResourceDictionary` and storyboard sealing is not thread-safe across parallel fixtures or target-framework lanes.
- **STA**: `WpfTestSta` in `Fluence.Wpf.Tests/Infrastructure/` owns a single STA thread plus `Dispatcher`. All UI-touching work goes through `WpfTestSta.RunOnStaAsync(...)`.
- **Layout**: one sealed class per subject, in the folder that owns the concern. Every folder is also a namespace segment, because `IDE0130` is an error here. Folder names are chosen so that no segment shadows a name the tests use: `Control/` rather than `Controls/`, because a segment `Controls` would shadow `Fluence.Wpf.Controls` at the 1200-plus `Controls.X` shorthand sites; `Gallery/` rather than `Demo/`, because a segment `Demo` would shadow `Fluence.Wpf.Demo`; and `Windowing/` rather than `Window/`, because a segment `Window` would shadow `System.Windows.Window`. The one residual shadow is the type `System.Windows.Controls.Control`, which is written out in full at the handful of sites that use it bare.

  | Folder | Contents |
  | ------ | -------- |
  | `Infrastructure/` | `WpfTestSta.cs`, `TestApp.cs`, `VisualTree.cs`, `BrushAssert.cs`, `LightThemeFixture.cs`, `ThemeTestHelpers.cs`, `DemoTestHost.cs`, `SlopwatchSuppressAttribute.cs` |
  | `Control/` | `<Control>Tests.cs`, one per control |
  | `Control/Rules/` | the eight rules asserted across many controls at once |
  | `Theming/` | theme engine, dictionary stability, accent, markup, metrics, parity, design-time |
  | `Windowing/` | `WindowPolicyTests`, `FluenceWindowTests`, `TitleBarTests`, `CaptionButtonTests`, `NativeMethodsTests`, `SnapLayoutHelperTests`, `WindowIconTests` |
  | `Gallery/`, `Gallery/Pages/` | the demo gallery shell, the sample contracts, and one class per gallery page |
  | `Tools/` | `GalleryScreenshotHarness.cs`, which is not a test |
  | `Baselines/` | the committed `--list-tests` baseline per TFM and the name-change allowlist |

- **Application and theme setup**: `TestApp.EnsureLibraryTheme()` resets the application, closes every open window, resets both managers, clears the resources, and applies a theme. It does **not** merge the demo dictionary. `TestApp.EnsureDemoTheme()` is the explicit opt-in that adds `DemoSharedStyles.xaml`, and only `Gallery/` uses it; a test elsewhere that needs it says so in a comment at its own call site, naming the demo style it depends on. `TestApp.GenericDictionary(application)` returns slot `[2]`.
- **Per-test isolation**: every class whose tests touch `Application`, application resources, or a `Window` implements `IAsyncLifetime` and calls `TestApp.EnsureLibraryTheme()` (or `EnsureDemoTheme()`) from `InitializeAsync` on the STA thread. Test bodies do not call a setup helper themselves. A class in which no test applies a theme, changes the accent, or toggles reduced motion takes `IClassFixture<LightThemeFixture>` instead and pays that cost once. Pure-logic classes (`WindowPolicyTests`, `NativeMethodsTests`, `SnapLayoutHelperTests`) take neither.
- **Shared helpers**: `WpfTestSta` (`RunOnStaAsync`, `DrainDispatcher`, `FindVisualDescendants`, `FindLogicalAndVisualDescendants`), `VisualTree` (`FindVisualChild`, `FindVisualChildByName`, `FindVisualChildByTypeName`, `FindVisualChildren`, `CloseWindowAndDrain`, brought in with `using static Fluence.Wpf.Tests.Infrastructure.VisualTree;`), `BrushAssert` (`AssertBrushColor`, `ResolvedColor`), `ThemeTestHelpers` (`ApplyStandardThemeCycle`, `AssertKeyThemeBrushesResolve`) and `DemoTestHost`. Do not reintroduce a private copy of any of them. Prefer the condition-based `WaitUntil(dispatcher, timeoutMs, predicate)` over a fixed delay.
- **Tests for controls** typically:
  1. Let the class `IAsyncLifetime` or `LightThemeFixture` do the reset; the body starts with the control.
  2. Create a minimal `Window`, attach the control, call `Window.Show()` so `ApplyTemplate` runs.
  3. Drive the control (simulate mouse or keyboard by invoking protected `OnMouse*` members via a small probe subclass if needed; see `ClickableCardProbe` in `Control/Rules/FluentStrokeTests.cs`).
  4. Assert via `VisualTree` helpers and `TryFindResource`.
  5. Drain with `WpfTestSta.DrainDispatcher` and close through `CloseWindowAndDrain(window)` in a `finally`.
- **InternalsVisibleTo**: the test assembly sees library internals; theme tests can call `ApplicationThemeManager.ResetForTesting()` to isolate fixtures.
- **Baseline policy**: the HEAD-of-branch case count is the floor. `Fluence.Wpf.Tests/Baselines/` holds the `--list-tests` capture per TFM; a change that adds or removes a test case must update it and say why in `CHANGELOG.md`. Diff by method name, not by fully qualified name: classes get renamed, method names do not.
- **Known failures**: `KNOWN_ISSUES.md` records the `net472` whole-assembly abort and the `net472` TimePicker flyout flake. A green local run is `total - skipped - known-failures = passed`; do not merge if your own changes add to the known-failure count.
- **Screenshot harness**: `Fluence.Wpf.Tests/Tools/GalleryScreenshotHarness.cs` writes the ten documentation PNGs under `docs/screenshots/`. Capture is **opt-in**: the tests are `[Trait("Category", "Screenshots")]` and skip unless `FLUENCE_CAPTURE_SCREENSHOTS=1`, so an ordinary run never overwrites the committed images. DWM backdrops are not captured by `RenderTargetBitmap`, so each surface is hosted in a plain off-screen `Window` over a solid `SolidBackgroundFillColorBaseBrush`.
```

- [ ] **Step 3: Fix the `AGENTS.md` section 9 pitfall**

Replace the bullet that currently reads

```markdown
- **Relying on a previous test's theme state leaking into yours** -> intermittent color-alpha mismatches when tests run as a suite but pass in isolation. Fix: always call `MergeGenericDictionary(Application.Current)` (which resets managers, clears dictionaries, and applies a known theme) as the first step of any control test body.
```

with

```markdown
- **Relying on a previous test's theme state leaking into yours** -> intermittent color-alpha mismatches when tests run as a suite but pass in isolation. Fix: the class, not the test body, owns the reset. Implement `IAsyncLifetime` and call `TestApp.EnsureLibraryTheme()` from `InitializeAsync` through `WpfTestSta.RunOnStaAsync`, or take `IClassFixture<LightThemeFixture>` if no test in the class applies a theme, changes the accent, or toggles reduced motion. Do not call a setup helper from inside a test body.
```

Also replace, in the parallelization pitfall, `[assembly: CollectionBehavior(DisableTestParallelization = true)]` with `[assembly: Parallelization(Mode = ParallelMode.None)]`, which is what `Properties/AssemblyInfo.cs:31` actually carries.

Also replace, in the section 5 control-authoring checklist item 5, `Add a partial `ControlTests.MyArea.cs` in `Fluence.Wpf.Tests`. Use `RunOnStaThread`, `EnsureApplication`, `MergeGenericDictionary`, and `FindVisualChild*` helpers.` with `Add `Fluence.Wpf.Tests/Control/MyControlTests.cs` holding one sealed class. Use `WpfTestSta.RunOnStaAsync`, `TestApp.EnsureLibraryTheme` from `IAsyncLifetime`, and the `VisualTree` and `BrushAssert` helpers.`

- [ ] **Step 4: Check `AGENTS.md` section 13.2 and `.claude/skills/demo-sample-page/SPEC.md`**

Run: `git grep -n "ControlTests" -- .claude AGENTS.md docs`
Every remaining hit is a stale path. Update each to the new location, using the folder table above. If `demo-sample-page/SPEC.md` names a test file path, point it at `Fluence.Wpf.Tests/Gallery/Pages/Gallery<Page>Tests.cs`.

- [ ] **Step 5: Update `docs/contributing.md`**

Replace lines 3 to 9, currently

````markdown
```powershell
dotnet restore Fluence.Wpf.sln
dotnet build Fluence.Wpf.sln
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj
```

WPF tests share a single STA dispatcher (`WpfTestSta`), and the assembly carries `[assembly: CollectionBehavior(DisableTestParallelization = true)]` (with `xunit.runner.json` disabling runner parallelism) to avoid cross-thread resource issues.
````

with

````markdown
```powershell
dotnet restore Fluence.Wpf.sln
dotnet build Fluence.Wpf.sln
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-not-trait "Category=Screenshots" --no-ansi --progress off
```

The suite runs on Microsoft Testing Platform, so run the built executable rather than `dotnet test`. On `net472` run it as two complementary lanes; see AGENTS.md section 6 and `KNOWN_ISSUES.md` for why a single-process whole-assembly run there aborts.

WPF tests share a single STA dispatcher (`WpfTestSta`), and the assembly carries `[assembly: Parallelization(Mode = ParallelMode.None)]` (with `xunit.runner.json` disabling runner parallelism) to avoid cross-thread resource issues.
````

Replace lines 24 to 31, the `## Tests` bullets, with:

```markdown
- One sealed class per subject, in the folder that owns the concern: `Control/<Control>Tests.cs` for a control, `Control/Rules/` for a rule asserted across many controls, `Theming/`, `Windowing/`, `Gallery/` and `Gallery/Pages/` for those concerns, `Infrastructure/` for helpers, `Tools/` for the screenshot harness. Each folder is a namespace segment, so a file under `Control/` declares `namespace Fluence.Wpf.Tests.Control`; IDE0130 is an error, so this is not optional. Do not add a folder whose name matches the last segment of a `Fluence.Wpf.*` namespace the tests use by shorthand.
- Let the class own the reset: implement `IAsyncLifetime` and call `TestApp.EnsureLibraryTheme()` from `InitializeAsync` through `WpfTestSta.RunOnStaAsync`. Take `IClassFixture<LightThemeFixture>` instead if no test in the class applies a theme, changes the accent, or toggles reduced motion. `TestApp.EnsureDemoTheme()` is the explicit demo opt-in and belongs in `Gallery/`.
- Use the shared helpers rather than a private copy: `VisualTree` (with `using static Fluence.Wpf.Tests.Infrastructure.VisualTree;`), `BrushAssert`, `ThemeTestHelpers`, `DemoTestHost`.
- When adding a new public control, include at minimum:
  - A default-style and template smoke test.
  - A theme-cycle test if the control uses `DynamicResource` heavily (`ThemeTestHelpers.ApplyStandardThemeCycle`).
  - Interaction or state assertions for any public event or read-only DP the control exposes.
- `Control/Rules/FluentStrokeTests.cs` is the reference pattern for small template and behavior probes: show a minimal `Window`, `ApplyTemplate`, assert template parts and resolved brushes, then drain and close.
```

- [ ] **Step 6: Rewrite `Fluence.Wpf.Tests/README.md`**

Replace the "What Lives Here" list and the "Run" section with:

````markdown
## What Lives Here

- `Infrastructure/WpfTestSta.cs` - the single STA-thread dispatcher used by every UI-touching test.
- `Infrastructure/TestApp.cs` - the single application and theme reset. `EnsureLibraryTheme` for library tests, `EnsureDemoTheme` for the demo opt-in, `GenericDictionary` for slot `[2]`.
- `Infrastructure/VisualTree.cs` - the tree walkers and `CloseWindowAndDrain`, brought into scope with `using static Fluence.Wpf.Tests.Infrastructure.VisualTree;`.
- `Infrastructure/BrushAssert.cs` - resolve a theme brush key and compare colours.
- `Infrastructure/LightThemeFixture.cs` - one reset and one Light apply per class, for classes that do not mutate the theme.
- `Infrastructure/ThemeTestHelpers.cs`, `DemoTestHost.cs`, `SlopwatchSuppressAttribute.cs` - theme cycle assertions, demo hosting, and the one analyzer-suppression marker.
- `Control/` - one sealed class per control.
- `Control/Rules/` - the rules asserted across many controls at once: icon foreground, focus visuals, reduced motion, background parity, accessibility names, automation peers, stroke compositing, popup corner radius, crisp rendering.
- `Theming/` - theme engine, dictionary stability, accent, markup, metrics, WinUI parity, design-time drift.
- `Windowing/` - window policy, `FluenceWindow`, title bar, caption buttons, native structs, snap layout, window icon.
- `Gallery/` and `Gallery/Pages/` - the gallery shell, the sample contracts, and one class per gallery page.
- `Tools/GalleryScreenshotHarness.cs` - documentation screenshot regeneration. Not a test.
- `Baselines/` - the committed `--list-tests` capture per TFM and the name-change allowlist.
- Namespaces match folders: `Fluence.Wpf.Tests.Infrastructure`, `.Control`, `.Control.Rules`, `.Theming`, `.Windowing`, `.Gallery`, `.Gallery.Pages`, `.Tools`. IDE0130 is an error, so a file's namespace and its folder never disagree.
- `Properties/AssemblyInfo.cs` - carries `[assembly: Parallelization(Mode = ParallelMode.None)]` so WPF resource and template work stays serial (the project also ships `xunit.runner.json` and sets `<TestTfmsInParallel>false</TestTfmsInParallel>`).

## Run

The project runs on Microsoft Testing Platform (`UseMicrosoftTestingPlatformRunner`). Run the built executable directly; the SDK 10 VSTest bridge is gone.

```powershell
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-not-trait "Category=Screenshots" --no-ansi --progress off
```

A single-process whole-assembly run on `net472` aborts non-deterministically, so run that TFM as two complementary lanes. Lane A is `--filter-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests`; lane B is `--filter-not-class` over the same seven. Sum the two case counts.

To regenerate documentation screenshots, opt in through the environment variable that gates the declaratively skipped harness facts:

```powershell
$env:FLUENCE_CAPTURE_SCREENSHOTS = "1"
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Tools.GalleryScreenshotHarness --no-ansi --progress off
```
````

In "Maintenance Notes", replace "Control tests should call the shared application/resource helpers before creating WPF elements" with "The class owns the reset, through `IAsyncLifetime` or `IClassFixture<LightThemeFixture>`; test bodies do not call a setup helper."

- [ ] **Step 7: Update `.github/PULL_REQUEST_TEMPLATE.md`**

Replace lines 8 and 9, currently

```markdown
- [ ] `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Release -f net472 --no-build`
- [ ] `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Release -f net10.0-windows10.0.26100.0 --no-build`
```

with the four lane invocations:

```markdown
- [ ] `Fluence.Wpf.Tests\bin\Release\net472\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --no-ansi --progress off`
- [ ] `Fluence.Wpf.Tests\bin\Release\net472\Fluence.Wpf.Tests.exe --filter-not-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --no-ansi --progress off`
- [ ] `Fluence.Wpf.Tests\bin\Release\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --no-ansi --progress off`
- [ ] `Fluence.Wpf.Tests\bin\Release\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-not-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --no-ansi --progress off`
```

Line 15's baseline sentence stays as it is.

- [ ] **Step 8: Verify no stale reference survives**

Run: `git grep -n "MergeGenericDictionary\|partial class ControlTests\|ControlTests.FluentStroke\|CollectionBehavior(DisableTestParallelization" -- AGENTS.md docs .github .claude Fluence.Wpf.Tests`
Expected: no output.

- [ ] **Step 9: Text policy**

Run: `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll`
The new prose is long; check for em and en dashes first.

- [ ] **Step 10: Commit** with subject `Update the handbook and contributor docs for the test layout.`

---

### Task 28: Two-lane CI and the `KNOWN_ISSUES.md` entries

**Starting commit:** the Task 27 commit.

**Files:**
- Modify: `.github/workflows/build.yml` lines 46 to 57
- Modify: `KNOWN_ISSUES.md`

**Interfaces:**
- Consumes: the seven Lane A class names.
- Produces: no code.

- [ ] **Step 1: Executor preamble.** Expected head: the Task 27 commit.

- [ ] **Step 2: Replace the two `dotnet test` steps in `.github/workflows/build.yml`**

Replace lines 46 to 57, which currently hold the comment block and the two `dotnet test` steps, with:

```yaml
      # Screenshots-category tests are PNG regenerators (no pass/fail assertion beyond
      # "rendered without throwing"); they run in full local test runs to refresh
      # docs/screenshots but are skipped here to keep CI fast.
      #
      # The suite runs on Microsoft Testing Platform, so these steps invoke the built test
      # executable rather than dotnet test: the VSTest target the previous steps used is
      # rejected outright by MTP on the .NET 10 SDK. Trait filtering is by the xunit trait
      # name (Category), not TestCategory.
      #
      # Each TFM runs as two complementary lanes. A single-process whole-assembly run on
      # net472 aborts with exit -1 at a non-deterministic point (see KNOWN_ISSUES.md). The
      # lanes are exact complements of one explicit class list, so their union is provably
      # the whole assembly and a newly added class lands in lane B automatically. The sum
      # check below is what turns that into a guarantee.
      - name: Test (.NET Framework 4.7.2, lane A)
        run: Fluence.Wpf.Tests\bin\Release\net472\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --report-xunit-trx --report-xunit-trx-filename net472.laneA.trx --results-directory TestResults/net472 --no-ansi --progress off
        shell: cmd

      - name: Test (.NET Framework 4.7.2, lane B)
        run: Fluence.Wpf.Tests\bin\Release\net472\Fluence.Wpf.Tests.exe --filter-not-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --report-xunit-trx --report-xunit-trx-filename net472.laneB.trx --results-directory TestResults/net472 --no-ansi --progress off
        shell: cmd

      - name: Test (.NET 10, lane A)
        run: Fluence.Wpf.Tests\bin\Release\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --report-xunit-trx --report-xunit-trx-filename net10.laneA.trx --results-directory TestResults/net10 --no-ansi --progress off
        shell: cmd

      - name: Test (.NET 10, lane B)
        run: Fluence.Wpf.Tests\bin\Release\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-not-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --report-xunit-trx --report-xunit-trx-filename net10.laneB.trx --results-directory TestResults/net10 --no-ansi --progress off
        shell: cmd

      # The lanes are complements, so their case counts must sum to the whole assembly. A
      # filter typo that silently matches nothing shows up here and nowhere else.
      - name: Check lane case-count sums
        shell: pwsh
        run: |
          $expected = @{ 'net472' = 1178; 'net10' = 1180 }
          $failed = $false
          foreach ($tfm in $expected.Keys) {
            $total = 0
            foreach ($lane in 'laneA', 'laneB') {
              $path = "TestResults/$tfm/$tfm.$lane.trx"
              if (-not (Test-Path $path)) { Write-Error "Missing $path"; $failed = $true; continue }
              [xml]$trx = Get-Content $path
              $total += [int]$trx.TestRun.ResultSummary.Counters.total
            }
            if ($total -ne $expected[$tfm]) {
              Write-Error "$tfm : lanes summed to $total, expected $($expected[$tfm])"
              $failed = $true
            } else {
              Write-Host "$tfm : $total cases across both lanes."
            }
          }
          if ($failed) { exit 1 }
```

The `shell: cmd` on the four test steps matters: PowerShell would parse the bare `.exe` path with backslashes differently, and `cmd` runs the executable directly.

The two expected totals in the sum check are the Task 23 numbers minus nothing: 1180 on net10 and 1178 on net472. Both counts exclude the three `Screenshots` cases, which the `--filter-not-trait` removes before discovery, so subtract 3 from each if the TRX totals come back at 1177 and 1175; verify against a real run before committing rather than guessing, and set the constants to what the run reports.

- [ ] **Step 3: Verify the sum check against a real local run**

Build Release: `dotnet build Fluence.Wpf.sln -c Release`

Run all four lane commands locally against `bin\Release\`, backgrounded, then run the sum-check script body against the four TRX files it produced. Set the two constants in the workflow to the numbers the run actually reports, and record those numbers in the task report.

- [ ] **Step 4: Add the `KNOWN_ISSUES.md` entries**

Add both under `## Current follow-ups (not defects)`:

```markdown
- **A single-process `net472` run of the whole test assembly aborts** - two
  attempts at running `Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe`
  with no class filter both died with exit `-1`, no failure output, no crash
  dump and no Application event-log entry. The first died after 48 seconds; the
  second after 1 minute 56 seconds, about 591 tests in. The abort point moved
  between runs, so it is non-deterministic, and neither run named a test. Class
  filtering is unaffected: the same assembly completes when it is split, and the
  two complementary lanes AGENTS.md section 6 describes cover all cases in about
  2 minutes 57 seconds. CI and the pull-request template therefore run both TFMs
  as two lanes and sum the case counts, which keeps the union provably equal to
  the whole assembly. Nothing here is known to be a product defect; the
  suspicion is a WPF or dispatcher resource exhaustion late in a very long
  single-process `net472` run, and confirming that needs a dump captured at the
  abort, which no attempt has produced yet.

- **`TimePicker_Cancel_RevertsPendingSelectionAsync` is flaky on `net472`** -
  `Fluence.Wpf.Tests/Control/TimePickerTests.cs` fails there with "The selector
  flyout must open before the cancel scenario" after about 2.45 seconds, and
  passes on `net10`. It passes in isolation. This is flyout-open timing, not a
  product defect: the test asserts the flyout is open before it clicks Cancel,
  and on `net472` the popup occasionally has not composited by the time the
  dispatcher drain returns. Do not treat a failure of this test alone as a
  regression, and do not add a fixed delay to hide it; the fix is a
  condition-based wait on the popup's `IsOpen`, which is a follow-up.
```

- [ ] **Step 5: Text policy**

Run: `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll`

- [ ] **Step 6: Commit** with subject `Run both test lanes in CI and record the net472 abort.`

---

### Task 29: Final verification and merge back

**This task runs in the primary worktree, `F:\Consolidation\Fluence.Wpf\.claude\worktrees\mica-defects`, not in a fresh harness worktree.** It is the only task that touches `fix/mica-composition-defects`.

**Starting commit:** `fix/mica-composition-defects` at `950e779`, with `refactor/test-suite-consolidation` 28 commits ahead of it.

- [ ] **Step 1: Confirm the working tree and branch**

```
git branch --show-current
```
Expected: `fix/mica-composition-defects`.

```
git status --porcelain
```
Expected: no output. If there is uncommitted work, stop and report. Never discard changes you did not make.

- [ ] **Step 2: Confirm no PNG-producing file moved**

```
git diff --stat fix/mica-composition-defects refactor/test-suite-consolidation -- docs/screenshots
```
Expected: no output.

```
git diff --stat fix/mica-composition-defects refactor/test-suite-consolidation -- Fluence.Wpf.Demo Fluence.Wpf.Demo.Mvvm Fluence.Wpf
```
Expected: no output. The whole plan touches only `Fluence.Wpf.Tests`, the docs, and CI. If any of those three directories shows a change, stop and report.

Because nothing that produces a PNG moved, **do not regenerate the screenshots**. If, contrary to expectation, `Tools/GalleryScreenshotHarness.cs` changed by more than its path, or a `Fluence.Wpf.Demo` file moved, regenerate them with `$env:FLUENCE_CAPTURE_SCREENSHOTS='1'` and `--filter-class Fluence.Wpf.Tests.Tools.GalleryScreenshotHarness` on the net10 exe, then check each PNG: a black or lock-screen capture is a failure and must be redone.

- [ ] **Step 3: Full verification on the branch tip, before merging**

```
git checkout refactor/test-suite-consolidation
```

Wait: the branch is checked out in another worktree if a task worktree still exists. Confirm with `git worktree list` first and remove any stale task worktree with `git worktree remove <path>` before this step. If the branch cannot be checked out here, stop and report.

Then, on the branch:

Run: `dotnet build Fluence.Wpf.sln -c Debug`, expect `0 Warning(s)` / `0 Error(s)`.
Run: `dotnet build Fluence.Wpf.sln -c Release`, expect `0 Warning(s)` / `0 Error(s)`.
Run: `dotnet format Fluence.Wpf.sln --verify-no-changes --severity info --no-restore`.
Run: `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll`.
Run all four lane commands from Global Constraints, backgrounded, on the Debug binaries.

Expected: sums of **1180** on net10 and **1178** on net472; 0 failed apart from the known `net472` TimePicker flake; 1 `NotRunnable`; 3 `NotExecuted`.

Name diff on both TFMs against the refreshed baseline from Task 23: expected **no output**.

- [ ] **Step 4: Merge back with `--no-ff`**

```
git checkout fix/mica-composition-defects
```

```
git merge --no-ff refactor/test-suite-consolidation
```

Expected: a merge commit, no conflicts. `fix/mica-composition-defects` has not moved since `950e779`, so a conflict means someone else committed to it; if so, stop and report rather than resolving.

Merge commit message:

```
Merge branch 'refactor/test-suite-consolidation' into fix/mica-composition-defects.

Retire the ControlTests partial in favour of one sealed class per subject
across seven folders, collapse seven divergent resource-merge helpers into
TestApp, and give every WPF-touching class its own reset. Eighteen subsumed
test cases are deleted and one is added by a theory fold, taking net10 from
1197 to 1180 and net472 from 1195 to 1178.
```

- [ ] **Step 5: Re-verify after the merge**

Run: `dotnet build Fluence.Wpf.sln -c Debug`, expect `0 Warning(s)` / `0 Error(s)`.
Run the four lane commands again on the merged tree. Expected: the same sums.

- [ ] **Step 6: Delete the branch**

```
git branch -d refactor/test-suite-consolidation
```

Expected: `Deleted branch refactor/test-suite-consolidation`. Use `-d`, never `-D`: `-d` refuses if the branch is not fully merged, which is the check.

- [ ] **Step 7: Do not push.** The user pushes and opens pull requests themselves.

---

## Self-review notes

Recorded for the reviewer, not as work items.

**Spec coverage.** Section 2.1 folders: Tasks 1, 5, 17, 19 to 22. Section 2.2 folder-matching namespaces: Task 1 lands the first one and the rule, and every move task after it sets the namespace of the files it moves. Section 2.3 partial to sealed: Tasks 7 to 18. Section 2.4 helper table: Tasks 2 to 4. Section 2.5 method naming: honoured by never renaming a method, which is what keeps the name diff empty. Section 3.1 and 3.2 helper unification: Task 2. Section 3.3 per-test isolation: the Phase 3 recipe, plus Task 16 for the `MotionHelper` reset. Section 4 deletions D1 to D9 and both folds: Task 23. Section 5 non-test files: Tasks 5, 19, 22, 23. Section 6.1 fixture sharing: Tasks 24 to 26. Section 6.2 lane invocation: Global Constraints and Task 28. Section 7 phase table: the eight phase groups. Section 8 name diff: Task 1 and the per-task verification. Section 9 documentation: Tasks 27 and 28. Section 10 risks: each mitigation is a named step.

**Three places where this plan departs from the spec as first written, all deliberate and all flagged in the text where they occur.**

1. The case counts. The spec's 1188 and 1170 predate this branch tip; the measured numbers are 1197 and 1180 on net10, and 1195 and 1178 on net472. The spec's own arithmetic is also internally inconsistent by one case, because it counts D4 as two deletions in one table and as case-count-preserving in another. This plan counts 18 deletions plus 1 addition.
2. Phase 4's "content unchanged". The spec's section 2.1 target layout requires four class merges that phase 4's own description would forbid (`AccentTests`, `FluenceWindowTests`, `TitleBarTests`, `NativeMethodsTests`). Tasks 19 and 20 perform them, row by row, and the name diff still comes out empty because no method is renamed.
3. Namespaces. The spec's first draft of section 2.2 chose a flat `Fluence.Wpf.Tests` namespace in every folder. The Task 1 probe measured that and it does not compile: IDE0130 is escalated to an error by `.editorconfig:32`. Both documents now specify folder-matching namespaces, folder names that shadow nothing (`Control/`, `Gallery/`, `Windowing/`), and the nine bare `Control` sites written out as `System.Windows.Controls.Control`. Nothing else about the plan's shape changed: the phases, the task numbering and the name-diff gate are as they were.

**One spec item this plan does not implement**, because the spec places it out of scope: the two larger theory folds the audit proposed, the 19 `Stage3_*` DP-default tests and the 19 per-control theme-cycle smoke tests. They would rename 38 methods and make the name diff unreadable, and they save no wall clock. They remain a follow-up.
