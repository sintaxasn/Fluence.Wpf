# Fluence.Wpf - developer handbook

Self-contained persistent memory for engineers (human and AI) working in this repository. Read top-to-bottom before touching code. This file is the single source of truth for conventions, architecture, reference authority, testing policy, and workflow; do **not** rely on out-of-repo agent bundles, external skill packs, or downstream-consumer-specific paths.

> **Portability rule** - everything in this handbook must remain usable by anyone consuming `Fluence.Wpf`, regardless of downstream product. Consumer-specific guidance (e.g. for a particular deployment toolkit) belongs in that consumer's own repo, not here.

---

## 1. Project overview

- **Fluence.Wpf** is a WPF control library that recreates the **Windows 11 Fluent / WinUI 3** visual language and interaction patterns on WPF.
- **Target frameworks** (library + tests): `net472` (primary) and `net10.0-windows`. Gallery demo (`Fluence.Wpf.Demo`) targets `net472`; MVVM demo (`Fluence.Wpf.Demo.Mvvm`) targets `net10.0-windows`.
- **Language**: `LangVersion=latest` across all TFMs, set centrally in `Directory.Build.props` — no per-TFM language restriction. `net472` still constrains **runtime API** availability (see §4.3); avoid APIs that don't ship in `net472`, but C# language features themselves are not restricted. Nullable reference types are **enabled** (`Nullable=enable` in `Directory.Build.props`); individual projects may override with `<Nullable>disable</Nullable>` (e.g. `Fluence.Wpf.Demo.Mvvm`).
- **License**: BSD 3-Clause. Every `.cs` file begins with the same 27-line header; copy it verbatim from any existing library file when adding new sources. Do not edit the copyright year unless the user asks.
- **OS**: Windows 10 1809+ baseline. Mica and rounded-corner extras light up on Windows 11.
- **XML namespace URI**: `http://schemas.fluencewpf.com` - suggested prefix `fluence`.

### Solution layout

```text
Fluence.Wpf.sln
├── Fluence.Wpf/             Control library (multi-TFM: net472 + net10.0-windows)
├── Fluence.Wpf.Demo/        Gallery app (net472) - visual verification for all controls
├── Fluence.Wpf.Demo.Mvvm/   MVVM Task Manager demo (net10.0-windows) - CommunityToolkit.Mvvm example
└── Fluence.Wpf.Tests/       MSTest v3.2 suite (multi-TFM)
```

### CLR namespaces

| Namespace              | Contents                                                                                                                                                                     |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Fluence.Wpf`          | `ApplicationThemeManager`, `ApplicationAccentColorManager`, `SystemThemeWatcher`, `ThemeChangedEventArgs`, theme enums, `TabViewWidthMode` / `TabViewCloseButtonOverlayMode` |
| `Fluence.Wpf.Controls` | Custom controls (`TabView`, `TabViewItem`, `Card`, `NavigationView`, …), `FluenceWindow`, `WindowPolicy`, navigation view family                                             |
| `Fluence.Wpf.Enums`    | `ApplicationTheme`, `BackdropType`, `CardVariant`, `InfoBarSeverity`, `FluentTypography`, etc.                                                                               |
| `Fluence.Wpf.Helpers`  | Internal helpers (`AcrylicNoiseHelper`, `HsvColorHelper`, `OsVersionHelper`, `RegistryHelper`)                                                                               |
| `Fluence.Wpf.Native`   | P/Invoke constants, structs, methods                                                                                                                                         |

XAML themes are under `Fluence.Wpf/Themes/` and are **not** a CLR namespace.

---

## 2. Coding standards

### File header (required)

Every `.cs` file in the library, demo, and tests starts with the BSD 3-Clause header used by any existing source file (e.g. `Fluence.Wpf/ApplicationThemeManager.cs` lines 1-27). Never delete, shorten, or paraphrase it.


### Language features

- Avoid the use of em dash (—) or en dash (–) anywhere in documentation or comments.
- All TFMs use `LangVersion=latest` (set in `Directory.Build.props`). Use modern C# features freely; verify any runtime API is available in `net472` before using it.
- Do not guard blocks with `#if NET10_0_OR_GREATER` to gain runtime APIs not present in `net472`; instead apply §4.3 guidance.
- Nullable reference types are **enabled** (`Nullable=enable` in `Directory.Build.props`). Library and test code must be nullable-clean — annotate parameters and returns with `?` only where genuinely nullable.
- `public` API must have `///` XML doc comments. The library builds with `<DocumentationFile>` and does not suppress `CS1591` / `CS1574`; missing comments fail the build.
- **File encoding**: All `.cs`, `.xaml`, and `.csproj` files must be saved as **UTF-8 with BOM** (EF BB BF). Never commit UTF-16 LE files - they produce spurious full-file diffs, break `grep`-based tooling, and may cause XML parser failures on some build agents. If your editor does not default to UTF-8 with BOM, configure it project-wide (Visual Studio: Tools → Advanced Save Options; VS Code: `"files.encoding": "utf8bom"`). Verify with `[System.IO.File]::ReadAllBytes($path)[0..2]` - must be `0xEF 0xBB 0xBF`.

### Warnings and analyzers

`Directory.Build.props` + `.editorconfig` harden the compiler to the maximum:

- **`TreatWarningsAsErrors=True`** and **`WarningLevel=9999`**: every diagnostic is a build error. Fix root cause; never suppress without an explicit entry.
- **`AnalysisLevel=latest-all`** + **`EnforceCodeStyleInBuild=true`**: all Roslyn analyzers and IDE style rules run as build-time errors across every project.
- **`CheckForOverflowUnderflow=True`**: arithmetic that overflows fails the build. Win32 bit-mask operations (HIWORD/LOWORD extractions from `lParam`) **must** be wrapped in `unchecked { }`. See `FluenceWindow.HitTestTitleBar` for the canonical pattern.
- **`Microsoft.CodeAnalysis.BannedApiAnalyzers`** (RS0030) reads `BannedSymbols.txt` at the solution root. **`string.IsNullOrEmpty()` is banned** — always use `string.IsNullOrWhiteSpace()`. Adding new banned symbols requires updating `BannedSymbols.txt`.
- **`Microsoft.Extensions.StaticAnalysis`** (SonarAnalyzer): Sxxx rules run as errors; see `.editorconfig` for the suppressed subset.

**Suppressions in `.editorconfig`** — do not re-enable without discussion:
- `IDE0056` / `IDE0057` — index/range operators (net472 runtime gap)
- `CA1307` / `CA1310` / `CA1847` / `CA1866` — string ordinal/span overloads (net472 API gap)
- SonarAnalyzer: `S103`, `S104`, `S107`, `S109`, `S1067`, `S1121`, `S1449`, `S1659`, `S3358`, `S3458`, `S3532`, `S3869`

**Per-library suppressions** (in `Fluence.Wpf.csproj` `<NoWarn>`):
- `SYSLIB1045` - regex source generator (not available on `net472`)
- `IDE0330` - `using` alias preference (style override)
- `S1244` - floating-point equality (necessary for pixel math)
- `VSTHRD001` - task/thread analyzer (WPF dispatcher pattern conflict)

Prefer `EventArgs.Empty`, `nameof(...)`, explicit `readonly`, and immutable helpers. **Never** use inline `#pragma warning disable` except in exceptional third-party interop cases.

### C# style conventions

`EnforceCodeStyleInBuild=true` + `AnalysisLevel=latest-all` make the following patterns **mandatory** (violations are build errors):

- **Explicit types over `var`**: `Color customColor = ...` not `var customColor = ...`. Exception: anonymous types have no explicit form.
- **Target-typed `new()`**: `MainWindow mainWindow = new()` not `var mainWindow = new MainWindow()` - use when the type is clear from the declaration.
- **Discard ignored returns with `_`**: methods that return a value must have the return consumed or explicitly discarded. `_ = Dispatcher.BeginInvoke(...)`, `_ = list.ApplyTemplate()`.
- **`default` not `default(T)`**: `Assert.AreNotEqual(default, value)` not `Assert.AreNotEqual(default(Color), value)`.
- **`is not` for null pattern checks**: `if (x is not FrameworkElement fe) throw ...` instead of `x as T; if (x is null) throw ...`.
- **`??` throw expressions**: `FindVisualChildByName<T>(...) ?? throw new InvalidOperationException(...)` instead of a separate null-check + throw block.
- **`const` for compile-time-known locals**: `const FrameworkPropertyMetadataOptions flags = ...` when a local's value is statically determined.
- **Auto-properties over manual backing fields**: `public static Color SystemAccentColor { get; private set; }` instead of a `private static Color _systemAccentColor` field plus an expression-bodied getter.
- **Remove redundant `using` directives**: unused imports are `error` (IDE0005).

### Naming

- Dependency properties: `public static readonly DependencyProperty FooProperty = DependencyProperty.Register(...)` with a CLR wrapper `public T Foo { get; set; }` and, when relevant, `OnFooChanged` static callback.
- Readonly DPs end with `...PropertyKey` private field + public `...Property = ...PropertyKey.DependencyProperty`.
- Template parts: `const string PART_Whatever = "PART_Whatever"`; annotate the class with `[TemplatePart(Name = PART_..., Type = typeof(T))]`.
- Visual states: `[TemplateVisualState(GroupName = "CommonStates", Name = "Normal|PointerOver|Pressed|Disabled")]`.

### XAML

- Keep templates in `Fluence.Wpf/Themes/Controls/<ControlName>.xaml`, one file per control, merged from `Themes/Generic.xaml`.
- Use `DynamicResource` for any brush, color, corner radius, or typography value that must react to theme, accent, or high contrast at runtime.
- Use `StaticResource` only for immutable assets (glyphs, fixed icon paths, constant geometries).
- Never inline hard-coded hex colors in production templates; always go through a canonical WinUI-style key.
- Animation timings: **~100-167 ms** typical transitions (WinUI `ControlFastAnimationDuration`, `ControlNormalAnimationDuration`). Easing curves consistent with existing templates (`{StaticResource ControlFastOutSlowInKeySpline}` where present).
- Focus visual: default WPF focus rectangles off; use FluentControl focus brush tokens instead, as in the existing Button / Card templates.

---

## 3. Theme architecture

### Merge slots

After `ApplicationThemeManager.Apply(...)` has run, `Application.Current.Resources.MergedDictionaries` always contains exactly **five** dictionaries in this fixed order:

|  Slot | Dictionary                                             | Lifecycle                                                |
| ----: | ------------------------------------------------------ | -------------------------------------------------------- |
| `[0]` | `Themes/Colors/Theme.{Light\|Dark\|HighContrast}.xaml` | **Swapped** on every theme change                        |
| `[1]` | `Themes/Accent/Accent.xaml`                            | Loaded once; ramp color keys are **updated in place**    |
| `[2]` | `Themes/Brushes/Brushes.xaml`                          | Loaded once; reloaded on non-HC theme swap to re-promote |
| `[3]` | `Themes/Typography/Typography.xaml`                    | Loaded once; never replaced                              |
| `[4]` | `Themes/Generic.xaml`                                  | Loaded once; never replaced                              |

The slot layout is enforced by `DictionaryStabilityTests` - any change to count or ordering breaks those tests and must be accompanied by a conscious update to both sides. Slot constants live at the top of `ApplicationThemeManager.cs`; change code only, never the comment drift.

**Key promotion.** After a theme swap the active theme dictionary's keys are copied into top-level `Application.Resources` so that `DynamicResource` bindings on `Freezable` properties (e.g. `SolidColorBrush.Color`) reliably re-evaluate. The `Brushes.xaml` dictionary is reloaded and re-promoted on every non-HighContrast swap for the same reason.

**High-contrast promotion.** When the active theme is `HighContrast`, a set of brush keys is copied from the theme dictionary directly into `Application.Resources` so they win over `Brushes.xaml`. The list is maintained in `ApplicationThemeManager._promotedHighContrastBrushKeys`; follow the existing promotion pattern if you add new HC brushes.

### Canonical color/brush keys

Names align with WinUI 3. Families currently used:

- **Text**: `TextFillColorPrimary|Secondary|Tertiary|Disabled` (+ `Brush` suffix).
- **Accent text**: `AccentTextFillColorPrimary|Secondary|Tertiary|Disabled`.
- **Control fill**: `ControlFillColorDefault|Secondary|Tertiary|Disabled|InputActive|Transparent`.
- **Control stroke**: `ControlStrokeColorDefault|Secondary|OnAccentDefault|OnAccentSecondary|OnAccentTertiary|OnAccentDisabled`.
- **Strong stroke** (ring-style selection / focus): `ControlStrongStrokeColorDefault|Disabled`.
- **Card**: `CardBackgroundFillColorDefault|Secondary`, `CardStrokeColorDefault|DefaultSolid`.
- **Background / layer**: `SolidBackgroundFillColorBase|Secondary|Tertiary|Quarternary`, `LayerFillColorDefault|Alt`.
- **Accent fill**: `AccentFillColorDefault|Secondary|Tertiary|Disabled|SelectedTextBackground`.
- **System**: `SystemFillColorSuccess|Caution|Critical|Neutral|NeutralBackground|SolidNeutral|SolidAttentionBackground`.
- **Accent ramp**: `SystemAccentColor`, `SystemAccentColorPrimary|Secondary|Tertiary`, and `…Brush` pairs.

Every color key generally has a sibling `…Brush` `SolidColorBrush`; template bindings almost always target the `Brush` version via `DynamicResource`.

### Theme API surface

- `ApplicationThemeManager.Apply(ApplicationTheme theme, BackdropType backdrop = BackdropType.Auto, bool updateAccent = true)` - first call initializes all five slots, later calls swap `[0]`, re-promote, and reload `[2]` on non-HC swaps.
- `ApplicationThemeManager.CurrentTheme` / `CurrentBackdrop` - read-only state.
- `ApplicationThemeManager.Changed` - `EventHandler<ThemeChangedEventArgs>`, raised once per applied change.
- `ApplicationAccentColorManager.ApplySystemAccent()` / `ApplyApplicationAccent(Color)` / `ApplyCustomAccent(Color)` - ramp generation + in-place key updates. Subscribe to `AccentColorChanged` for post-apply hooks.
- `SystemThemeWatcher.Watch(Window)` / `UnWatch(Window)` - Win32 settings-change hooks with debounce; fires `Changed` (via `ApplicationThemeManager`) once per logical OS change. **Do not assume more than one `Changed` per user action in tests.**
- `FluenceWindow` is the canonical WPF window with DWM backdrop, rounded corners, caption extension, and an optional title-bar content slot.

---

## 4. Reference priority

When a question arises about _"how should this look, behave, or be implemented?"_ - resolve it in this order. Never fabricate Fluent semantics from imagination; always cite an authoritative source.

### 4.1 General priority (applies to every question)

1. **In-tree precedent.** If a pattern already exists in `Fluence.Wpf/Themes/**/*.xaml`, `Fluence.Wpf/Controls/*.cs`, or `Fluence.Wpf.Tests/ThemeTestHelpers.cs`, follow it. Consistency with the shipped surface trumps outside sources.
2. **Per-domain reference (see §4.2).** Select the correct authority for the concern at hand.
3. **Published Windows 11 design guidance** on Microsoft Learn (Fluent Design docs, Windows App SDK docs). Use as a tie-breaker only, never as the primary spec.

Undocumented "looks right" choices are not acceptable in a PR. If nothing in the three layers above covers a specific case, raise it and get explicit guidance before implementing.

### 4.2 Per-domain authority

| Concern                                                                           | Primary authority                                                                                                                                                                                                                                     | Rationale                                                                            |
| --------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| Visual tokens (colors, brushes, typography, spacing, corner radii, timing curves) | [**WinUI 3 CommonStyles**](https://github.com/microsoft/microsoft-ui-xaml/tree/main/src/controls/dev/CommonStyles)                                                                                                                                    | Canonical Microsoft-owned Fluent design tokens and control visuals.                  |
| WPF-native window chrome (`WindowChrome`, DWM extension, caption buttons)         | [**.NET 10 WPF Themes**](https://github.com/dotnet/wpf/tree/main/src/Microsoft.DotNet.Wpf/src/Themes)                                                                                                                                                 | WPF-specific idioms that WinUI 3 does not express; known to work on `net472`.        |
| Navigation patterns (`NavigationView` layout, selection indicator, pane modes)    | [**WinUI 3 CommonStyles**](https://github.com/microsoft/microsoft-ui-xaml/tree/main/src/controls/dev/CommonStyles) (visual) + [**.NET 10 WPF Themes**](https://github.com/dotnet/wpf/tree/main/src/Microsoft.DotNet.Wpf/src/Themes) (WPF translation) | Visuals are Fluent-canonical; composition must respect WPF templating constraints.   |
| Accent ramp generation and HSV tint math                                          | [**.NET 10 WPF Themes**](https://github.com/dotnet/wpf/tree/main/src/Microsoft.DotNet.Wpf/src/Themes)                                                                                                                                                 | Includes a proven WPF implementation of the Windows accent ramp.                     |
| System theme detection (Light/Dark/HighContrast)                                  | [**.NET 10 WPF Themes**](https://github.com/dotnet/wpf/tree/main/src/Microsoft.DotNet.Wpf/src/Themes)                                                                                                                                                 | WPF-compatible registry reads and `WM_SETTINGCHANGE` handling suitable for `net472`. |
| Individual controls (Button, CheckBox, RadioButton, ComboBox, ToggleSwitch, …)    | [**WinUI 3 CommonStyles**](https://github.com/microsoft/microsoft-ui-xaml/tree/main/src/controls/dev/CommonStyles)                                                                                                                                    | Canonical Fluent templates and visual states.                                        |
| Acrylic / Mica backdrops, rounded corners                                         | [**.NET 10 WPF Themes**](https://github.com/dotnet/wpf/tree/main/src/Microsoft.DotNet.Wpf/src/Themes) + [DWM API docs on Microsoft Learn](https://learn.microsoft.com/windows/win32/api/dwmapi/)                                                      | DWM interop is the mechanism; .NET 10 WPF demonstrates the WPF hook.                 |
| Accessibility / automation peers                                                  | WinUI 3 CommonStyles + Windows UI Automation docs on Microsoft Learn                                                                                                                                                                                  | Behavioural contract, not visual.                                                    |

### 4.3 Feasibility test for `net472`

When a reference pattern depends on an API that is not available on `net472` (CsWinRT, WinUI runtime types, `System.Text.Json` source generators, `record`, etc.):

1. **Prefer** the closest idiomatic WPF translation using `System.Windows.*` primitives - this is why .NET 10 WPF is listed as the primary authority for WPF-native concerns.
2. **Document** the gap in `KNOWN_ISSUES.md` with the specific API that is unavailable and what the chosen fallback gives up.
3. **Never** add a new third-party runtime dependency to close a `net472` gap without explicit user approval.

---

## 5. Control authoring checklist

When adding a new control or materially changing an existing one:

1. **CLR type**
   - Subclass the closest `System.Windows.Controls.*` (or `Control` / `ContentControl`).
   - In the static constructor: `DefaultStyleKeyProperty.OverrideMetadata(typeof(MyControl), new FrameworkPropertyMetadata(typeof(MyControl)));`.
   - Expose dependency properties; use `RegisterReadOnly` for state-only DPs (`IsPressed`, `IsValid`).
2. **Template**
   - Add `Themes/Controls/MyControl.xaml` as a standalone `ResourceDictionary` and merge it from `Themes/Generic.xaml`.
   - Mark template parts with `[TemplatePart]` attributes and wire them in `OnApplyTemplate`.
   - Wire up `VisualStateManager` groups (`CommonStates`, `FocusStates`, `CheckStates`, …) with Fluent timings (~100-167 ms).
3. **Resources**
   - Reuse canonical WinUI keys. If a concept is new (e.g. a brand-specific state), add a **color** to each `Themes/Colors/Theme.*.xaml`, **then** add the `SolidColorBrush` to `Themes/Brushes/Brushes.xaml` binding via `DynamicResource`.
   - Add a design-time preview entry in `Themes/DesignTime.xaml` assuming Light + `#0078D4`.
4. **Demo**
   - Add or extend a gallery page under `Fluence.Wpf.Demo/Pages/Gallery*.xaml`. Register the page in `MainWindow.NavigateTo(string tag)` if it should be navigable from the `NavigationView`.
5. **Tests (mandatory)**
   - Add a partial `ControlTests.MyArea.cs` in `Fluence.Wpf.Tests`. Use `RunOnStaThread`, `EnsureApplication`, `MergeGenericDictionary`, and `FindVisualChild*` helpers.
   - Cover at minimum: default style applies, key template parts found, critical DP/state transitions, and (if theme-sensitive) one theme cycle via `ThemeTestHelpers.ApplyStandardThemeCycle`.
6. **Docs**
   - Append to `docs/controls.md` when the public catalogue changes.
   - Note new brush families in `docs/theming.md`.
   - Add a one-line entry under the current CHANGELOG section.

---

## 6. Testing

- **Framework**: MSTest v3.2 via `Microsoft.NET.Test.Sdk`.
- **TFMs**: `net472` **and** `net10.0-windows`; both must pass.
- **Parallelization**: `[assembly: DoNotParallelize]` (`DisableParallelization.cs`). WPF's shared `ResourceDictionary` / storyboard sealing is not thread-safe across parallel fixtures.
- **STA**: `WpfTestSta` in the test project owns a single STA thread + `Dispatcher`. All UI-touching work goes through `WpfTestSta.Invoke(...)` / `RunOnStaThread(...)`.
- **Application**: `WpfTestSta.EnsureApplication()` creates an `Application` with `ShutdownMode.OnExplicitShutdown` so tests do not tear it down.
- **Theme helpers**: `ThemeTestHelpers.ApplyStandardThemeCycle` (Light→Dark→HighContrast→Light); `AssertKeyThemeBrushesResolve` for canonical key sanity.
- **Tests for controls** typically:
  1. Merge `Themes/Generic.xaml` via `MergeGenericDictionary(Application.Current.Resources)` (this also calls `Apply(Light)` to seed canonical keys).
  2. Create a minimal `Window`, attach the control, call `Window.Show()` so `ApplyTemplate` runs.
  3. Drive the control (simulate mouse/keyboard by invoking protected `OnMouse…` via a small probe subclass if needed; see `ClickableCardProbe` in `ControlTests.FluentStroke.cs`).
  4. Assert via `VisualTreeHelper` / `FindVisualChildByName` and `TryFindResource`.
  5. Drain the dispatcher with `DrainDispatcher()` and close the window.
- **InternalsVisibleTo**: the test assembly sees library internals; theme tests can call `ApplicationThemeManager.ResetForTesting()` to isolate fixtures.
- **Baseline policy**: the HEAD-of-branch test count is the floor. Add tests, do not weaken it. If a test is legitimately obsoleted by a design change, remove the whole file in the same commit that supersedes it, record the rationale in `CHANGELOG.md`, and update this handbook if the testing pattern itself changed.
- **Known pre-existing failures**: any currently-failing test must be tracked in `KNOWN_ISSUES.md` with a reproduction and the intended fix. A green local run is `total - skipped - known-failures = passed`; do not merge if your own changes add to the known-failure count.
- **Screenshot harness**: `Fluence.Wpf.Tests/GalleryScreenshotHarness.cs` regenerates `docs/screenshots/banner-{theme}-{scale}x.png` via `RenderTargetBitmap` during normal full test runs. DWM backdrops (Mica / Acrylic) are _not_ captured by `RenderTargetBitmap`, so the harness hosts `GalleryHomePage` inside a plain `Window` with a solid `SolidBackgroundFillColorBaseBrush`.

---

## 7. Build and run

```powershell
# from repo root
dotnet restore Fluence.Wpf.sln
dotnet build   Fluence.Wpf.sln -c Debug
dotnet test    Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug
```

- Zero errors, zero warnings - the library is `TreatWarningsAsErrors`.
- The demo is run with `dotnet run -p Fluence.Wpf.Demo` (net472, Windows).
- For visual verification: exercise Light / Dark / High Contrast / Auto, a couple of accent swatches, Mica / Acrylic / Tabbed / None backdrops, and at least one control per gallery page.

---

## 8. Demo applications

### Fluence.Wpf.Demo (gallery, net472)

- `MainWindow` is a `FluenceWindow` with `ExtendsContentIntoTitleBar="True"`; the title bar hosts the app icon, title, a `TextBox` **search** bound to filter menu items, and caption buttons.
- `NavigationView` named `DemoNav`: default `PaneDisplayMode="Left"` in source (demo currently opens in `LeftCompact` with `IsPaneOpen="True"` to showcase expansion - verify at review time).
- Menu items carry `Tag` strings; `MainWindow.NavigateTo(string tag)` does a switch to the matching `Gallery*Page` inside the content frame. The back stack has been intentionally removed; navigation is tag-driven.
- `GalleryHomePage` shows a theme-aware hero banner (`BannerLight.png` / `BannerDark.png`) and four large **clickable `Card`** tiles that route to Buttons, Selection, Navigation, and Window pages via the same `NavigateTo` helper.
- 11 gallery pages: Home, Buttons, Selection, Inputs, Data, Tabs, Navigation, Window, Status, Colors, Glyphs - grouped under three `NavigationViewItemHeader` sections.
- Run: `dotnet run -p Fluence.Wpf.Demo` (net472, Windows).

### Fluence.Wpf.Demo.Mvvm (MVVM Task Manager, net10.0-windows)

- Minimal Task Manager demonstrating `FluenceWindow` + Fluence controls with **zero code-behind**.
- Uses **CommunityToolkit.Mvvm** 8.4: `[ObservableProperty]`, `[RelayCommand(CanExecute=nameof(CanAdd))]`, `partial void OnXxxChanged` source-generated callbacks.
- `MainViewModel` owns an unfiltered `ObservableCollection<TaskItemViewModel>` and rebuilds `DisplayedTasks` on every filter or completion change. `StatusText` and `ProgressValue` are derived and notified after the rebuild - do **not** add `[NotifyPropertyChangedFor]` on `_activeFilter`; that would fire notifications before `DisplayedTasks` is rebuilt (stale read).
- Filter radio buttons use `EnumToBoolConverter` with `ConverterParameter={x:Static vm:FilterMode.*}`.
- Delete button inside `DataTemplate` reaches `MainViewModel.DeleteCommand` via `RelativeSource AncestorType=Window`; this is deliberate - keeps `TaskItemViewModel` free of parent references.
- `App.xaml` contains **no `MergedDictionaries`**; `ApplicationThemeManager.Apply` (called from `App.xaml.cs`) seeds all five slots. A manual `Generic.xaml` merge would become a sixth stale entry and corrupt slot indices.
- Run: `dotnet run -p Fluence.Wpf.Demo.Mvvm`.

---

## 9. Common pitfalls

- **`StaticResource` on a theme- or accent-bound brush** ⇒ stale colors after the first theme switch. Fix: change to `DynamicResource`.
- **Clearing `Application.Current.Resources.MergedDictionaries`** directly, then adding your own, without going through `ApplicationThemeManager.Apply` ⇒ broken `DynamicResource` chains and missing templates. Fix: always go through the manager; the first call initializes all slots.
- **Creating `FrameworkElement` instances on a worker thread** in tests ⇒ `InvalidOperationException`. Fix: route through `WpfTestSta.Invoke`.
- **Skipping `[assembly: DoNotParallelize]`** on a new test project / renaming the file ⇒ intermittent `ResourceReferenceExpression` / sealed-storyboard failures.
- **Assuming the old "subtle stroke" for selection rings** ⇒ RadioButton / CheckBox rings disappear in light theme. Fix: use `ControlStrongStrokeColorDefaultBrush` (and `…Disabled` for disabled state).
- **Hard-coding caption metrics or backdrop flags in child controls** ⇒ breaks on Windows 10 / unsupported DWM builds. Fix: read `OsVersionHelper` and honour `FluenceWindow` policy.
- **Navigating via an external back-stack** in the demo ⇒ divergence with the current tag-based `NavigateTo`. The back stack is intentionally not wired up.
- **Holding designer-only brushes as immutable resources** ⇒ designer no longer matches runtime after a theme change. Fix: keep `DesignTime.xaml` minimal and aligned with Light + `#0078D4`.
- **Relying on a previous test's theme state leaking into yours** ⇒ intermittent color-alpha mismatches when tests run as a suite but pass in isolation. Fix: always call `MergeGenericDictionary(Application.Current)` (which resets managers, clears dictionaries, and applies a known theme) as the first step of any control test body.
- **Using `string.IsNullOrEmpty()`** ⇒ build error RS0030 (banned via `BannedApiAnalyzers` + `BannedSymbols.txt`). Fix: always use `string.IsNullOrWhiteSpace()`.
- **Win32 bit-mask arithmetic without `unchecked`** ⇒ `OverflowException` at runtime; caught as a build error because `CheckForOverflowUnderflow=True`. Fix: wrap HIWORD/LOWORD extractions in `unchecked { }`. See `FluenceWindow.HitTestTitleBar` for the canonical pattern.
- **Ignoring a return value from a non-void method** ⇒ build error CA1806. Fix: discard with `_ = method()`.

---

## 10. Documentation map

Public documentation (ship with the package):

- [README.md](README.md)
- [CHANGELOG.md](CHANGELOG.md)
- [docs/getting-started.md](docs/getting-started.md)
- [docs/theming.md](docs/theming.md)
- [docs/controls.md](docs/controls.md)
- [docs/migration-guide.md](docs/migration-guide.md)
- [docs/contributing.md](docs/contributing.md)
- [KNOWN_ISSUES.md](KNOWN_ISSUES.md)

Maintainer / AI context (this file and its siblings):

- [AGENTS.md](AGENTS.md) - this handbook
- [.github/copilot-instructions.md](.github/copilot-instructions.md) - condensed instructions for Copilot-class assistants

Anything under `docs/_internal/` is not part of the public doc set. Do not link it from `README.md` or `docs/*.md`.

---

## 11. Role definition and quality gates

When you are editing this repository, you are acting as a **senior C#/.NET WPF engineer and Windows-theme specialist**. Every change must honour the following gates:

1. **Standards respected**: BSD header, `LangVersion=latest` with nullable-clean code, XML docs on public API, `DynamicResource` for theme-bound values, no hard-coded RGB, canonical WinUI key names, no banned APIs (`string.IsNullOrEmpty` etc.).
2. **Reference authority followed**: any visual or behavioural decision is backed by §4 (in-tree precedent → per-domain authority → Windows 11 docs). Fabricated design choices do not pass review.
3. **Build clean**: `dotnet build Fluence.Wpf.sln` with **zero** errors and **zero** warnings after your change on every TFM.
4. **Tests green, and extended**: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj` passes on every TFM; every new control, public API, or behaviour change ships with an MSTest that exercises it, including a theme cycle where relevant. No regressions against the HEAD-of-branch baseline (see §6).
5. **Visual parity**: any template / XAML change is confirmed in `Fluence.Wpf.Demo` across Light, Dark, High Contrast, accent swap, and at least one backdrop. Capture screenshots (100% and 150% DPI) when visuals change materially.
6. **Docs synced**: public changes update `CHANGELOG.md`, and any of `README.md` / `docs/controls.md` / `docs/theming.md` that a consumer would rely on.
7. **Scope discipline**: do not touch unrelated files or rename things unless explicitly asked; do not commit without the user's explicit request.

---

## 12. Exclusions (apply to _this_ handbook)

- No filesystem paths, build steps, or deployment artifacts specific to a downstream consumer product.
- No endorsement of, or dependency on, any particular third-party WPF library; keep comparisons, migration notes, and naming advice generic.
- No references to external agent bundles, skill packs, or remote tooling that are not already part of this repository.
- No speculative roadmap items; everything in this file must reflect code that exists on the current branch.

---

## 13. Templated prompts

Two canonical task templates. Copy the relevant block, fill in the `TASK` line, and execute end-to-end.

### 13.1 Generic Fluence.Wpf development workflow

```text
ROLE: Senior WPF engineer maintaining Fluence.Wpf, a Windows 11 Fluent control library for .NET Framework 4.7.2 and .NET 10+.

CONTEXT (read before touching code):
- Fluence.Wpf/AGENTS.md - this handbook (authoritative)
- Fluence.Wpf/docs/controls.md - public control catalogue
- Fluence.Wpf/docs/theming.md - canonical brush/color families
- Fluence.Wpf/docs/contributing.md - contribution notes
- Fluence.Wpf/KNOWN_ISSUES.md - known gaps and pre-existing regressions
- Fluence.Wpf/CHANGELOG.md - recent scope

Reference authority (see §4):
  1. In-tree precedent (XAML, controls, tests)
  2. Per-domain authority:
     - WinUI 3 CommonStyles - https://github.com/microsoft/microsoft-ui-xaml/tree/main/src/controls/dev/CommonStyles
     - .NET 10 WPF Themes - https://github.com/dotnet/wpf/tree/main/src/Microsoft.DotNet.Wpf/src/Themes
  3. Windows 11 design guidance on Microsoft Learn (tie-breaker only)

TASK: <one sentence describing the concrete change>

WORKFLOW:
 1. Re-read the relevant section(s) of AGENTS.md (§3 Theme architecture, §4 Reference priority, §5 Control authoring, §6 Testing).
 2. Enumerate files and regions you plan to touch. Keep the diff minimal and name the slot/layer each file belongs to.
 3. If the change is visual or behavioural, cite the authority from §4 that justifies it.
 4. For any new control, follow §5 (Control authoring checklist) exactly.
 5. For any theme / brush change, update the matching entries in Theme.{Light|Dark|HighContrast}.xaml AND Brushes.xaml together; never one without the other.
 6. TDD: add or extend an MSTest before writing implementation. Run just that test on net10 first (fast feedback), then both TFMs.
 7. dotnet build Fluence.Wpf.sln -c Debug - 0 errors / 0 warnings on net472 + net10.0-windows.
 8. dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug - all tests pass on both TFMs; pre-existing KNOWN_ISSUES.md failures unchanged.
 9. Update docs: CHANGELOG.md (always), docs/*.md (when the public surface changes), KNOWN_ISSUES.md (when a gap is opened or closed).
10. Stage changes; show diffs; wait for the user's explicit commit instruction.

ACCEPTANCE:
- Build: 0/0 on both TFMs
- Tests: no new regressions; net count +N for N new tests you added
- Docs: synced with the change
- No new third-party WPF runtime dependency introduced; no external agent bundles referenced
- Every visual / behavioural choice has a cited authority from §4

STOP CONDITION: working tree is "git-clean minus your intended diff"; wait for explicit user approval before committing.
```

### 13.2 PSADT-integrated workflow (downstream consumer)

```text
ROLE: Senior WPF engineer working on Fluence.Wpf with PSADT (PSAppDeployToolkit) as the primary downstream consumer. PSADT references Fluence.Wpf via ProjectReference; the consumer-side test harness is PSADT.UserInterface.TestHarness.

CONTEXT (read before touching code):
- Fluence.Wpf/AGENTS.md - this handbook (authoritative for library concerns)
- Fluence.Wpf/docs/controls.md, docs/theming.md, KNOWN_ISSUES.md, CHANGELOG.md
- PSAppDeployToolkit/AGENTS.md - consumer-side rules and migration state
- PSAppDeployToolkit/MIGRATION_PLAN.md - consumer migration checklist

Reference authority (see §4 of this handbook): in-tree precedent → WinUI 3 CommonStyles / .NET 10 WPF Themes (per domain) → Windows 11 docs.

POLICY:
- Fluence.Wpf may be modified when a change benefits both projects. Flag any such cross-project change in the commit body so it can be spot-reviewed.
- Never introduce PSADT-specific paths, constants, or assumptions into Fluence.Wpf source, XAML, or public API. Consumer-specific integration belongs on the consumer side.
- The TestHarness is the authoritative visual-verification surface for PSADT consumption; all dialogs / screens used by PSADT must render correctly there on both TFMs.

TASK: <one sentence describing the concrete change>

WORKFLOW:
 1. Re-read the relevant section(s) of AGENTS.md (§3 Theme architecture, §4 Reference priority, §5 Control authoring, §6 Testing, §12 Exclusions).
 2. Decide the lowest layer that resolves the issue for both consumers:
    - Fluence.Wpf (when the fix generalises)
    - PSADT.UserInterface (when the fix is consumer-specific behaviour)
    - PSADT.UserInterface.TestHarness (when only the harness itself is affected)
 3. TDD:
    - Fluence.Wpf changes: add / extend MSTests in Fluence.Wpf.Tests.
    - PSADT.UserInterface changes: add / extend the consumer-side tests where applicable.
    - Visual parity: plan a TestHarness session that exercises every dialog the change touches, on both TFMs.
 4. Build order (both must be 0 errors / 0 warnings on net472 + net10.0-windows):
      dotnet build Fluence.Wpf.sln -c Debug
      dotnet build PSAppDeployToolkit/PSADT.slnx -c Debug
 5. Run Fluence.Wpf tests: dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug - both TFMs green.
 6. Run the TestHarness (net10 first, then net472):
      dotnet run --project PSAppDeployToolkit/src/PSADT/PSADT.UserInterface.TestHarness
    Exercise: Light / Dark / High Contrast swap, each affected dialog, each supported backdrop (Mica / Acrylic / None). Capture screenshots if the visual contract changes.
    If the harness reaches the restart dialog, terminate the TestHarness process immediately. Do not click `Restart Now`; automatic restart behavior is not part of this visual verification.
 7. Update docs on both sides:
    - Fluence.Wpf: CHANGELOG.md (always), docs/*.md (public surface), KNOWN_ISSUES.md (gaps).
    - PSADT: MIGRATION_PLAN.md (when a migration checkpoint moves), consumer docs if the visible contract changes.
 8. Stage changes; show diffs grouped by repo; wait for the user's explicit commit instruction.

ACCEPTANCE:
- Fluence.Wpf build: 0/0 on both TFMs
- PSADT.slnx build: 0/0 on both TFMs
- Fluence.Wpf tests: no new regressions; any pre-existing KNOWN_ISSUES.md failures unchanged
- TestHarness launches cleanly and renders correctly on both TFMs, across theme / backdrop combinations touched by the change
- No new third-party WPF runtime dependency introduced on either side; every visual / behavioural choice has a cited authority from §4
- Any Fluence.Wpf edits driven by PSADT needs are flagged in the commit body for spot review

STOP CONDITION: both working trees are "git-clean minus intended diff"; wait for explicit user approval before committing.
```

<claude-mem-context>
# Memory Context

# [Fluence.Wpf] recent context, 2026-05-09 11:30pm EDT

Legend: 🎯session 🔴bugfix 🟣feature 🔄refactor ✅change 🔵discovery ⚖️decision 🚨security_alert 🔐security_note
Format: ID TIME TYPE TITLE
Fetch details: get_observations([IDs]) | Search: mem-search skill

Stats: 50 obs (11,351t read) | 87,194t work | 87% savings

### May 8, 2026
S72 User acknowledged that a pull request will be created once the remote session completes its planned work (May 8, 8:27 PM)
S71 User requested to review a plan from a remote ultraplan session for approval before implementation (May 8, 8:27 PM)
S73 Check if the PR is ready for Fluence.Wpf repository (May 8, 8:28 PM)
S74 Implement fixes and improvements to WinUI/WPF controls: ToggleStatus button state switching, ValidationMessage/Icon positioning, Progress Ring/Bar accent color updates, InfoBar accent color updates, and review controls against WinUI 3 and WPFGallery references for improvement opportunities. (May 8, 9:24 PM)
### May 9, 2026
S75 Fix ToggleStatus button state switching, adjust ValidationMessage/Icon positioning, fix paused Progress Ring and Progress Bar accent color updates, fix Infobar Informational accent color updates, and review controls in WinUI references and current implementation for improvements. (May 9, 2:08 PM)
S76 Fix four control issues in Fluence.Wpf: ToggleStatus button state not switching on release, ValidationMessage/Icon positioning, paused Progress Ring/Bar not updating to accent color, and InfoBar Informational colors not updating to accent color. Review WinUI 3 and WinUIGallery references for implementation guidance while respecting CLAUDE.md project doctrine. (May 9, 3:11 PM)
S77 Continue WinUI Gallery brush token parity verification for Fluence.Wpf control library - verify all control background colors match between WinUI Gallery reference and Fluence.Wpf demo app, identify deviations, and document custom brush usage (May 9, 5:10 PM)
1896 11:22p 🔵 PaneFooter toggle in demo uses StackPanel with ToggleSwitch and label for mode switching
1897 " ✅ ToggleSwitch footer layout updated for Left/Compact mode centering and text hiding
1898 " ✅ Patch applied to MainWindow.xaml for responsive toggle footer layout
1899 " ✅ TextBlock style BasedOn attribute removed from PaneModeToggleLabel
1900 " ✅ Patch applied simplifying PaneModeToggleLabel TextBlock style
1901 11:23p 🔵 NavigationView.xaml contains two PaneFooterHost instances for Left and Compact mode templates
1902 " 🔵 Compact pane template structure shows 48px fixed pane width with content in Grid.Row="2"
1903 " ✅ PaneFooterHost padding reduced in Compact mode to accommodate ToggleSwitch
1904 " ✅ Patch applied reducing PaneFooterHost padding in Compact mode
1905 " 🔵 Patch applied to Compact mode only; Left mode PaneFooterHost still has original padding
1906 " ✅ Compact mode PaneFooterHost padding reverted from 4,0,4,8 back to 8,0,8,8
1907 " ✅ Patch applied reverting Compact PaneFooterHost padding to 8,0,8,8
1908 " ✅ Compact mode PaneFooterHost padding reduced again to 4,0,4,8
1909 " ✅ Patch applied reducing Compact mode PaneFooterHost padding to 4,0,4,8
1910 " 🔵 Padding changes confirmed: Compact 4,0,4,8 and Left 8,0,8,8 are now asymmetric
1911 " 🔵 Left mode pane footer now has reduced padding 4,0,4,8 while Compact retains 8,0,8,8
1912 " ✅ Left mode PaneFooterHost padding reverted to 8,0,8,8
1913 " ✅ Patch applied reverting Left mode PaneFooterHost padding to 8,0,8,8
1914 11:24p ✅ Compact mode PaneFooterHost padding reduced to 4,0,4,8
1915 " ✅ Patch applied reducing Compact mode footer padding to 4,0,4,8
1916 " ✅ Test assertions added for Left and LeftCompact mode toggle layout behavior
1917 " ✅ Patch applied adding test assertions for Left and LeftCompact mode toggle layout
1918 " 🔵 Existing test validates LeftCompact footer visibility tied to IsPaneOpen state
1919 " 🔵 Left and Compact pane templates have different header and chrome heights
1920 " 🔵 Compact pane template includes IsPaneOpen trigger that collapses PaneFooterHost
1921 " ✅ PaneModeToggleLabel DataTrigger added to hide label when compact pane is closed
1922 " ✅ Patch applied adding IsPaneOpen trigger to PaneModeToggleLabel
1923 " ✅ IsPaneOpen trigger added to set PaneFooterHost padding in Compact template
1924 11:25p ✅ Patch applied adding IsPaneOpen trigger to Compact pane template
1925 " 🔵 Final padding configuration confirmed: Left 8,0,8,8 and Compact 4,0,4,8
1926 " ✅ Test assertions added for closed Left pane mode toggle layout
1927 " ✅ Patch applied adding closed Left pane mode test assertions
1929 " ✅ Complete diff summary: ToggleSwitch footer layout implemented across three files
1933 11:28p 🔵 NavigationView control bindings confirm PaneDisplayMode property drives template selection and animations
1935 " 🔵 MainWindow.xaml.cs implements PaneModeToggle_CheckedChanged to sync DemoNav.PaneDisplayMode
1936 " 🔵 NavigationView.cs implements comprehensive pane mode lifecycle and animation orchestration
1937 " 🔵 NavigationView.xaml templates define three-pane layouts with PaneFooter content positioning
1938 " ✅ MainWindow.xaml.cs updated with pane mode transition animation constants and state flags
1939 " ✅ Patch applied adding animation timing and transition state management to MainWindow.xaml.cs
1940 " 🟣 Pane mode transition animations implemented for Left-to-Top mode switch
1941 " 🟣 Patch applied implementing pane mode transition animations in MainWindow.xaml.cs
1942 " 🟣 Animation helper methods added for pane transition animations
1943 " 🟣 Patch applied adding animation helper methods to MainWindow.xaml.cs
1944 11:29p 🟣 FindVisualChildByName helper method added for visual tree traversal
1948 " 🟣 Patch applied successfully adding FindVisualChildByName helper method to MainWindow.xaml.cs
**1949** " 🟣 **Patch confirmed successfully applied to MainWindow.xaml.cs**
Successfully confirmed patch application adding FindVisualChildByName helper method to MainWindow.xaml.cs. The method is now integrated and available for visual tree element lookup operations used by pane mode transition animations.
~141t 🛠️ 982

**1950** " ✅ **CHANGELOG.md updated documenting pane mode transition animations feature**
Updated CHANGELOG.md to document the pane mode transition animation feature. The changelog entry now describes the smooth visual transitions when switching from Left to Top pane mode, including the left pane slide-out and top strip/content slide-in animations.
~206t 🛠️ 879

**1951** " ✅ **Patch applied successfully updating CHANGELOG.md with animation feature documentation**
Successfully applied patch updating CHANGELOG.md to document the pane mode transition animation feature, completing the changelog documentation for the responsive ToggleSwitch footer and animation system.
~139t 🛠️ 906

**1953** " 🔵 **Test infrastructure includes WaitForAnimationAndDrain utility for animation timing**
Test infrastructure includes WaitForAnimationAndDrain utility that uses DispatcherTimer for waiting during animations. The new MainWindow_PaneModeToggle_SwitchesBetweenTopAndLeft test was added but uses Drain() instead of WaitForAnimationAndDrain, meaning it doesn't wait for the 167ms pane mode transition animations to complete. The test may need updates to call WaitForAnimationAndDrain(window.Dispatcher, 167) after pane mode transitions to account for slide animations.
~322t 🔍 1,879

**1955** " 🔵 **WaitForAnimationAndDrain utility explicitly documents 167ms LeftCompact pane animation timing**
The WaitForAnimationAndDrain test utility is explicitly designed to handle 167ms animations (matching LeftCompact pane column animation). The comment documents this timing. The pane mode transition animations added to MainWindow.xaml.cs also use 167ms duration, making them compatible with this test infrastructure. Tests calling WaitForAnimationAndDrain(dispatcher, 167) or similar would properly account for pane mode transition completion.
~302t 🔍 1,209


Access 87k tokens of past work via get_observations([IDs]) or mem-search skill.
</claude-mem-context>
