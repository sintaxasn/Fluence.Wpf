# Fluence.Wpf — agent handbook

Self-contained persistent memory for AI assistants (and new humans) working in this repository. Read top-to-bottom before touching code. **Do not** rely on out-of-repo agent bundles, external skill packs, or consumer-specific paths; everything you need to behave correctly in this codebase is written here or in `docs/`.

---

## 1. Project overview

- **Fluence.Wpf** is a WPF control library that recreates the **Windows 11 Fluent / WinUI 3** visual language and interaction patterns on WPF.
- **Target frameworks** (library + tests): `net472` (primary) and `net10.0-windows`. Demo is `net472`.
- **Language**: `C# 7.3` on `net472` (see `Directory.Build.props` conditional `LangVersion`). No `net472` sources may use C# 8+ features (nullable reference types, ranges, default-interface methods, `record`, `with`, range/index, raw strings, etc.).
- **License**: BSD 3-Clause. Every `.cs` file begins with the same 27-line header; copy it verbatim from any existing library file when adding new sources. Do not edit the copyright year unless the user asks.
- **OS**: Windows 10 1809+ baseline. Mica and rounded-corner extras light up on Windows 11.
- **XML namespace URI**: `http://schemas.fluencewpf.com` — suggested prefix `fluence`.

### Solution layout

```
Fluence.Wpf.sln
├── Fluence.Wpf/           Control library (multi-TFM)
├── Fluence.Wpf.Demo/      Gallery app (net472)
└── Fluence.Wpf.Tests/     MSTest v3.2 suite (multi-TFM)
```

### CLR namespaces

| Namespace | Contents |
|---|---|
| `Fluence.Wpf` | `ApplicationThemeManager`, `ApplicationAccentColorManager`, `SystemThemeWatcher`, `ThemeChangedEventArgs`, theme enums, `TabViewWidthMode` / `TabViewCloseButtonOverlayMode` |
| `Fluence.Wpf.Controls` | Custom controls (`TabView`, `TabViewItem`, `Card`, `NavigationView`, …), `FluenceWindow`, obsolete `FluentWindow`, `WindowPolicy`, navigation view family |
| `Fluence.Wpf.Enums` | `ApplicationTheme`, `BackdropType`, `CardVariant`, `InfoBarSeverity`, `FluentTypography`, etc. |
| `Fluence.Wpf.Helpers` | Internal helpers (`AcrylicNoiseHelper`, `HsvColorHelper`, `OsVersionHelper`, `RegistryHelper`) |
| `Fluence.Wpf.Native` | P/Invoke constants, structs, methods |

XAML themes are under `Fluence.Wpf/Themes/` and are **not** a CLR namespace.

---

## 2. Coding standards

### File header (required)

Every `.cs` file in the library, demo, and tests starts with the BSD 3-Clause header used by any existing source file (e.g. `Fluence.Wpf/ApplicationThemeManager.cs` lines 1–27). Never delete, shorten, or paraphrase it.

### Language features

- `net472`: **C# 7.3 only**. Prefer `out var`, tuples (`System.ValueTuple`), `is T name`, expression-bodied members, local functions, explicit interface methods.
- Multi-target: do not guard with `#if NET10_0_OR_GREATER` to gain features that would break `net472`.
- No nullable reference types in library or demo code.
- `public` API must have `///` XML doc comments. The library builds with `<DocumentationFile>` and does not suppress `CS1591` / `CS1574`; missing comments fail the build.

### Warnings and analyzers

- `TreatWarningsAsErrors` is **on** for the library. Fix root cause instead of suppressing.
- Prefer `EventArgs.Empty`, `nameof(...)`, explicit `readonly`, and immutable helpers.

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
- Animation timings: **~100–167 ms** typical transitions (WinUI `ControlFastAnimationDuration`, `ControlNormalAnimationDuration`). Easing curves consistent with existing templates (`{StaticResource ControlFastOutSlowInKeySpline}` where present).
- Focus visual: default WPF focus rectangles off; use FluentControl focus brush tokens instead, as in the existing Button / Card templates.

---

## 3. Theme architecture

### Merge slots

After `ApplicationThemeManager.Apply(...)` has run, `Application.Current.Resources.MergedDictionaries` looks like this:

| Slot | Dictionary | Lifecycle |
|---:|---|---|
| `[0]` | `Themes/Colors/Theme.{Light\|Dark\|HighContrast}.xaml` | **Swapped** on every theme change |
| `[1]` (optional) | `Themes/Compatibility/InkoreCompat.{Light\|Dark}.xaml` | Inserted when the iNKORE compatibility layer is enabled; swapped alongside the theme |
| `[1 + compatOffset]` | `Themes/Accent/Accent.xaml` | Loaded once; ramp color keys are **updated in place** |
| `[2 + compatOffset]` | `Themes/Brushes/Brushes.xaml` | Loaded once; never replaced |
| `[3 + compatOffset]` | `Themes/Typography/Typography.xaml` | Loaded once; never replaced |
| `[4 + compatOffset]` | `Themes/Generic.xaml` | Loaded once; never replaced |

`compatOffset` is `0` normally and `1` when compatibility is active (see `ApplicationThemeManager.CompatOffset`). Constants live at the top of `ApplicationThemeManager.cs`; change code only, never the comment drift.

High-contrast promotion: when the active theme is `HighContrast`, a set of brush keys is copied from the theme dictionary directly into `Application.Resources` so they win over `Brushes.xaml`. The list is maintained in `ApplicationThemeManager`; follow the existing promotion pattern if you add new HC brushes.

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

- `ApplicationThemeManager.Apply(ApplicationTheme theme, BackdropType backdrop = BackdropType.Auto, bool updateAccent = true)` — first call initializes, later calls swap `[0]` (and compat slot if active).
- `ApplicationThemeManager.CurrentTheme` / `CurrentBackdrop` / `IsInkoreCompatibilityEnabled` — read-only state.
- `ApplicationThemeManager.Changed` — `EventHandler<ThemeChangedEventArgs>`, raised once per applied change.
- `ApplicationAccentColorManager.ApplySystemAccent()` / `ApplyApplicationAccent(Color)` / `ApplyCustomAccent(Color)` — ramp generation + in-place key updates. Subscribe to `AccentColorChanged` for post-apply hooks.
- `SystemThemeWatcher.Watch(Window)` / `Unwatch(Window)` — Win32 settings-change hooks with debounce; fires `Changed` (via `ApplicationThemeManager`) once per logical OS change. **Do not assume more than one `Changed` per user action in tests.**
- `FluenceWindow` is the canonical WPF window with DWM backdrop, rounded corners, caption extension, and an optional title-bar content slot. `FluentWindow` is retained as a thin `[Obsolete]` subclass that inherits the same default style (`Themes/Controls/FluenceWindow.xaml`) so existing consumer XAML still binds; new code must subclass or reference `FluenceWindow`.

---

## 4. Control authoring checklist

When adding a new control or materially changing an existing one:

1. **CLR type**
    - Subclass the closest `System.Windows.Controls.*` (or `Control` / `ContentControl`).
    - In the static constructor: `DefaultStyleKeyProperty.OverrideMetadata(typeof(MyControl), new FrameworkPropertyMetadata(typeof(MyControl)));`.
    - Expose dependency properties; use `RegisterReadOnly` for state-only DPs (`IsPressed`, `IsValid`).
2. **Template**
    - Add `Themes/Controls/MyControl.xaml` as a standalone `ResourceDictionary` and merge it from `Themes/Generic.xaml`.
    - Mark template parts with `[TemplatePart]` attributes and wire them in `OnApplyTemplate`.
    - Wire up `VisualStateManager` groups (`CommonStates`, `FocusStates`, `CheckStates`, …) with short Fluent timings.
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

## 5. Testing

- **Framework**: MSTest v3.2 via `Microsoft.NET.Test.Sdk`.
- **TFMs**: `net472` **and** `net10.0-windows`; both must pass.
- **Parallelization**: `[assembly: DoNotParallelize]` (`DisableParallelization.cs`). WPF's shared `ResourceDictionary` / storyboard sealing is not thread-safe across parallel fixtures.
- **STA**: `WpfTestSta` in the test project owns a single STA thread + `Dispatcher`. All UI-touching work goes through `WpfTestSta.Invoke(...)` / `RunOnStaThread(...)`.
- **Application**: `WpfTestSta.EnsureApplication()` creates an `Application` with `ShutdownMode.OnExplicitShutdown` so tests do not tear it down.
- **Theme helpers**: `ThemeTestHelpers.ApplyStandardThemeCycle` (Light→Dark→HighContrast→Light); `AssertKeyThemeBrushesResolve` for canonical key sanity.
- **Tests for controls** typically:
    1. Merge `Themes/Generic.xaml` via `MergeGenericDictionary(Application.Current.Resources)`.
    2. Create a minimal `Window`, attach the control, call `Window.Show()` so `ApplyTemplate` runs.
    3. Drive the control (simulate mouse/keyboard by invoking protected `OnMouse…` via a small probe subclass if needed; see `ClickableCardProbe` in `ControlTests.FluentStroke.cs`).
    4. Assert via `VisualTreeHelper` / `FindVisualChildByName` and `TryFindResource`.
    5. Drain the dispatcher with `DrainDispatcher()` and close the window.
- **InternalsVisibleTo**: the test assembly sees library internals; theme tests can call `ApplicationThemeManager.ResetForTesting()` to isolate fixtures.
- **Baseline** today: the green suite now sits around **210** tests (trimmed after consolidating `FluentWindow` onto `FluenceWindow` and removing `FluentWindowTitleBarTests`, and grown by the new `TabViewTests`). Treat the current branch test count as the floor — add tests, don't weaken it.
- **Screenshot harness**: `Fluence.Wpf.Tests/GalleryScreenshotHarness.cs` regenerates `docs/screenshots/banner-{theme}-{scale}x.png` via `RenderTargetBitmap`. The test is gated on `FLUENCE_CAPTURE_SCREENSHOTS=1`; without it, it reports `Inconclusive` so ordinary CI runs never overwrite committed images. DWM backdrops (Mica / Acrylic) are *not* captured by `RenderTargetBitmap`, so the harness hosts `GalleryHomePage` inside a plain `Window` with a solid `SolidBackgroundFillColorBaseBrush`.

---

## 6. Build and run

```powershell
# from repo root
dotnet restore Fluence.Wpf.sln
dotnet build   Fluence.Wpf.sln -c Debug
dotnet test    Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug
```

- Zero errors, zero warnings — the library is `TreatWarningsAsErrors`.
- The demo is run with `dotnet run -p Fluence.Wpf.Demo` (net472, Windows).
- For visual verification: exercise Light / Dark / High Contrast / Auto, a couple of accent swatches, Mica / Acrylic / Tabbed / None backdrops, and at least one control per gallery page.

---

## 7. Demo application

- `MainWindow` is a `FluenceWindow` with `ExtendsContentIntoTitleBar="True"`; the title bar hosts the app icon, title, a `TextBox` **search** bound to filter menu items, and caption buttons.
- `NavigationView` named `DemoNav`: default `PaneDisplayMode="Left"` in source (demo currently opens in `LeftCompact` with `IsPaneOpen="True"` to showcase expansion — verify at review time).
- Menu items carry `Tag` strings; `MainWindow.NavigateTo(string tag)` does a switch to the matching `Gallery*Page` inside the content frame. The back stack has been intentionally removed; navigation is tag-driven.
- `GalleryHomePage` shows a theme-aware hero banner (`BannerLight.png` / `BannerDark.png`) and four large **clickable `Card`** tiles that route to Buttons, Selection, Navigation, and Window pages via the same `NavigateTo` helper.

---

## 8. Common pitfalls

- **`StaticResource` on a theme- or accent-bound brush** ⇒ stale colors after the first theme switch. Fix: change to `DynamicResource`.
- **Clearing `Application.Current.Resources.MergedDictionaries`** directly, then adding your own, without going through `ApplicationThemeManager.Apply` ⇒ broken `DynamicResource` chains and missing templates. Fix: always go through the manager; the first call initializes all slots.
- **Creating `FrameworkElement` instances on a worker thread** in tests ⇒ `InvalidOperationException`. Fix: route through `WpfTestSta.Invoke`.
- **Skipping `[assembly: DoNotParallelize]`** on a new test project / renaming the file ⇒ intermittent `ResourceReferenceExpression` / sealed-storyboard failures.
- **Assuming the old "subtle stroke" for selection rings** ⇒ RadioButton / CheckBox rings disappear in light theme. Fix: use `ControlStrongStrokeColorDefaultBrush` (and `…Disabled` for disabled state).
- **Hard-coding caption metrics or backdrop flags in child controls** ⇒ breaks on Windows 10 / unsupported DWM builds. Fix: read `OsVersionHelper` and honour `FluenceWindow` policy.
- **Navigating via an external back-stack** in the demo ⇒ divergence with the current tag-based `NavigateTo`. The back stack is intentionally not wired up.
- **Holding designer-only brushes as immutable resources** ⇒ designer no longer matches runtime after a theme change. Fix: keep `DesignTime.xaml` minimal and aligned with Light + `#0078D4`.

---

## 9. Documentation map

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

- [CLAUDE.md](CLAUDE.md) — this handbook
- [.github/copilot-instructions.md](.github/copilot-instructions.md) — condensed instructions for Copilot-class assistants

Anything under `docs/_internal/` is not part of the public doc set. Do not link it from `README.md` or `docs/*.md`.

---

## 10. Role definition and quality gates (condensed)

When you are editing this repository as an AI assistant, you are acting as a **senior C#/.NET WPF migration engineer and Windows-theme specialist**. Every change must honour the following gates:

1. **Standards respected**: BSD header, C# 7.3 on `net472`, XML docs on public API, `DynamicResource` for theme-bound values, no hard-coded RGB, canonical WinUI key names.
2. **Build clean**: `dotnet build Fluence.Wpf.sln` with **zero** errors and **zero** warnings after your change.
3. **Tests green, and extended**: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj` passes on every TFM; every new control, public API, or behavior change ships with an MSTest that exercises it, including a theme cycle where relevant. No regressions in the existing 230-test baseline.
4. **Visual parity**: any template / XAML change is confirmed in `Fluence.Wpf.Demo` across Light, Dark, High Contrast, accent swap, and at least one backdrop. Capture screenshots (100% and 150% DPI) when visuals change materially.
5. **Docs synced**: public changes update `CHANGELOG.md`, and any of `README.md` / `docs/controls.md` / `docs/theming.md` that a consumer would rely on.
6. **Scope discipline**: do not touch unrelated files or rename things unless explicitly asked; do not commit without the user's explicit request.

---

## 11. Exclusions (apply to *this* handbook)

- No filesystem paths or artifacts specific to a downstream consumer product.
- No endorsement of or dependency on any particular third-party WPF library; keep comparisons, migration notes, and naming advice generic.
- No references to external agent bundles, skill packs, or remote tooling that are not already part of this repository.
- No speculative roadmap items; everything in this file must reflect code that exists on the current branch.
