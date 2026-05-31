# Demo Projects Polish + PowerShell Examples Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Gallery and MVVM demos beginner-friendly and designer-correct, replace the PowerShell scripts with clear self-contained examples, and ship PowerShell-specific documentation.

**Architecture:** Four independent parts. Part A (Gallery) and Part B (MVVM) are documentation + design-time-resource fixes that must keep all existing tests green; Part A has an optional, separately-gated deep-restructure phase. Part C replaces the `.ps1` scripts (Windows PowerShell 5.1 / `net472`, self-contained, inline bootstrap) while preserving `MainWindow.xaml` (the screenshot harness depends on it). Part D dispatches the documentation agent to write `docs/powershell.md` and update the docs map.

**Tech Stack:** WPF (`net472` + `net10.0-windows10.0.26100.0`), C# `LangVersion=latest`, CommunityToolkit.Mvvm 8.4, Windows PowerShell 5.1, MSTest 4.2.2, Fluence.Wpf theming engine (`ApplicationThemeManager` / `FluenceWindow` / `DesignTime.{Light,Dark}.xaml`).

---

## Guardrails (read before any task)

- **BSD header**: every new/edited `.cs` file keeps the 27-line BSD 3-Clause header (copy verbatim from any existing library `.cs`). XAML files do not carry it (match existing demo XAML).
- **Encoding**: all `.cs`, `.xaml`, `.csproj`, `.md`, `.ps1` files are **UTF-8 with BOM** (`EF BB BF`). Verify after writing: `[System.IO.File]::ReadAllBytes($path)[0..2]` must be `239 187 191`. The Write tool does **not** add a BOM to `.cs`/`.ps1`/`.md` — prepend it (`printf '\xef\xbb\xbf' | cat - file > file.tmp && mv file.tmp file`) and re-verify.
- **Newlines**: repo is `eol=lf` (`.gitattributes`). Keep LF.
- **Demo projects' analyzer profile is looser than the library** — both demo `.csproj` set `<NoWarn>CS1591;...</NoWarn>` (missing-doc warnings suppressed) and `Demo.Mvvm` sets `<Nullable>disable</Nullable>`. **Do not enable nullable in `Demo.Mvvm`** — AGENTS.md documents that as deliberate. Do not "fix" it.
- **Tests are the floor.** Baseline before starting: `net10 = 689 passed / 5 skipped`, `net472 = 688 passed / 5 skipped`. No part may reduce the passed count. The demos are covered by `DemoMainWindowTests`, `ControlTests.DemoParity`, `ControlTests.DemoSamplePolish`, `DemoColorsPageTests`, `DemoResourceCleanupTests`, `DemoSamplePageWiringTests`, and `GalleryScreenshotHarness`.
- **`GalleryScreenshotHarness.CapturePowerShellDemoAt` loads `Fluence.Wpf.Demo.PowerShell\MainWindow.xaml`** (see `GalleryScreenshotHarness.cs:394`). Part C must keep that file present and loadable, or that test breaks and ~2 screenshots stop regenerating.
- **Design-time resources only affect the XAML designer** (they are merged via the `DesignTimeResources` Page item, never at runtime), so they cannot change runtime behavior or test outcomes. The library already ships `Fluence.Wpf/Properties/DesignTime.Light.xaml` and `DesignTime.Dark.xaml` (complete colors + brushes, default `#0078D4` accent).
- **Do not commit** until the user asks; each task ends with a commit step you run only after the user has confirmed they want commits (or batch them per the executing-plans checkpoint).
- **Build/test split by TFM** (per AGENTS.md §7):
  - `dotnet build Fluence.Wpf.sln -c Debug`
  - `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --no-build`
  - `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build`

---

## File Structure

**Part A — Gallery (`Fluence.Wpf.Demo`)**
- Modify: `Fluence.Wpf.Demo/Properties/DesignTimeResources.xaml` — repoint to the library's `DesignTime.Light.xaml` (colors **and** brushes).
- Create: `Fluence.Wpf.Demo/README.md` — beginner architecture overview of the gallery shell.
- Modify (doc pass): `App.xaml.cs`, `MainWindow.xaml(.cs)`, `Pages/DemoSampleControl.xaml.cs`, `Pages/DemoSamplePageWiring.cs`, `DemoNavigationCatalog.cs`, and a representative subset of `Pages/Gallery*Page.*` — add XML docs / beginner comments / section markers.
- **(Optional, gated)** deep-restructure files listed in Part A2.

**Part B — MVVM (`Fluence.Wpf.Demo.Mvvm`)**
- Create: `Fluence.Wpf.Demo.Mvvm/Properties/DesignTimeResources.xaml` — merges `DesignTime.Dark.xaml` (designer defaults to Dark).
- Modify: `Fluence.Wpf.Demo.Mvvm/Fluence.Wpf.Demo.Mvvm.csproj` — add the `DesignTimeResources` Page item.
- Modify: `ViewModels/MainViewModel.cs` — add a constructor that seeds sample tasks (better first-run UX + design-time data).
- Modify: `MainWindow.xaml` — make `d:DataContext` design-time-creatable so the designer shows sample rows.
- Modify: `README.md` — note the design-time Dark default and sample-data behavior.

**Part C — PowerShell (`Fluence.Wpf.Demo.PowerShell`)**
- Delete: `Show-ThemeDemo.ps1`, `Show-ControlsDemo.ps1`, `Show-ProgressDemo.ps1`.
- Create: `01-HelloWorld.ps1`, `02-ThemeAndAccent.ps1`, `03-ControlsTour.ps1`, `04-LoadXamlFile.ps1`.
- Keep: `MainWindow.xaml` (used by `04-LoadXamlFile.ps1` **and** the screenshot harness).
- Modify: `README.md` — describe the new scripts.

**Part D — Docs**
- Create: `docs/powershell.md` (via the documentation agent).
- Modify: `README.md` (repo), `docs/getting-started.md`, `AGENTS.md` §10 docs map, `CHANGELOG.md`.

---

## PART A — Gallery demo (`Fluence.Wpf.Demo`)

> **A1 is the always-do baseline** (design-time fix + documentation). **A2 is the optional deep restructure** the user may pivot away from. Do A1 fully, then **STOP at the A2 gate** and get an explicit decision.

### Task A1.1: Repoint the gallery design-time resources to include brushes

**Why:** The current file merges `Themes/Colors/Theme.Light.xaml`, which is a **colors-only** table — so `*Brush` keys don't resolve and controls render with fallback chrome in the designer. The library now ships `DesignTime.Light.xaml` (colors **and** brushes), matching the runtime computed slot.

**Files:**
- Modify: `Fluence.Wpf.Demo/Properties/DesignTimeResources.xaml`

- [ ] **Step 1: Replace the merged-dictionary list**

Current content (`Fluence.Wpf.Demo/Properties/DesignTimeResources.xaml`):

```xml
<ResourceDictionary.MergedDictionaries>
    <!--  Light theme colors for design-time preview. Brushes are built at runtime by FluenceThemeEngine.  -->
    <ResourceDictionary Source="pack://application:,,,/Fluence.Wpf;component/Themes/Colors/Theme.Light.xaml" />
    <ResourceDictionary Source="pack://application:,,,/Fluence.Wpf;component/Themes/Typography/Typography.xaml" />
    <ResourceDictionary Source="pack://application:,,,/Fluence.Wpf;component/Themes/Generic.xaml" />
    <ResourceDictionary Source="pack://application:,,,/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml" />
</ResourceDictionary.MergedDictionaries>
```

Replace the `<ResourceDictionary.MergedDictionaries>` block with:

```xml
<ResourceDictionary.MergedDictionaries>
    <!--
        Design-time-only resources for the XAML designer / Blend (never merged at runtime).
        Slot order mirrors what FluenceThemeEngine publishes at runtime:
          [0] DesignTime.Light.xaml - the COMPLETE computed Light palette: every Color token
              AND its SolidColorBrush twin, for the default #0078D4 accent. (The old
              Theme.Light.xaml is colors-only, so brushes fell back to defaults in the designer.)
          [1] Typography.xaml       - text styles.
          [2] Generic.xaml          - control templates.
        Then the gallery's own shared styles on top.
        To preview Dark in the designer, add a d:-namespace merge of DesignTime.Dark.xaml
        on the specific page you are editing.
    -->
    <ResourceDictionary Source="pack://application:,,,/Fluence.Wpf;component/Properties/DesignTime.Light.xaml" />
    <ResourceDictionary Source="pack://application:,,,/Fluence.Wpf;component/Themes/Typography/Typography.xaml" />
    <ResourceDictionary Source="pack://application:,,,/Fluence.Wpf;component/Themes/Generic.xaml" />
    <ResourceDictionary Source="pack://application:,,,/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml" />
</ResourceDictionary.MergedDictionaries>
```

- [ ] **Step 2: Build to confirm the pack URIs resolve**

Run: `dotnet build Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -c Debug -f net10.0-windows10.0.26100.0`
Expected: Build succeeded, 0 warnings, 0 errors. (The `DesignTimeResources` Page item compiles only under the design-time condition, but a clean solution build via `dotnet build Fluence.Wpf.sln` exercises it — see Step 3.)

- [ ] **Step 3: Full solution build (design-time page compiles here)**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: Build succeeded, 0/0. (The CLI sets `SolutionPath`, which satisfies the `DesignTimeResources.xaml` Page condition in the csproj, so a bad pack URI would fail here.)

- [ ] **Step 4: Manual designer check (record result)**

Open `Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml` in the VS/Blend designer. Confirm buttons render with real Fluent fills/strokes (not bare system defaults). Note the outcome in the commit message. (No automated test covers the designer surface; this is a visual confirmation.)

- [ ] **Step 5: Commit**

```bash
git add Fluence.Wpf.Demo/Properties/DesignTimeResources.xaml
git commit -m "demo(gallery): design-time resources include computed brushes (DesignTime.Light.xaml)"
```

---

### Task A1.2: Add a beginner architecture README for the gallery

**Files:**
- Create: `Fluence.Wpf.Demo/README.md`

- [ ] **Step 1: Write the README**

Create `Fluence.Wpf.Demo/README.md` with this content (UTF-8 BOM):

```markdown
# Fluence.Wpf Control Gallery (beginner guide)

This app is a tour of every Fluence.Wpf control. It is also a worked example of how to
build a modern, themed WPF desktop app. Read this before diving into the code.

## Run it

```powershell
dotnet run --project Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -f net10.0-windows10.0.26100.0
# or -f net472
```

## The 60-second mental model

1. **`App.xaml.cs` -> `OnStartup`** turns the theme engine on *before* any window exists:
   `ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica)` then
   `ApplicationAccentColorManager.ApplySystemAccent()`. This publishes all the brushes the
   controls bind to. Then it merges the gallery's own `Resources/DemoSharedStyles.xaml` and
   shows `MainWindow`.
2. **`MainWindow`** is a `FluenceWindow` (a `Window` with Fluent chrome). It hosts a
   `NavigationView` (the left menu) and a `TitleBar` (icon, title, search box). Each menu
   entry has a `Tag`; `NavigateTo(tag)` swaps the content frame to the matching `Gallery*Page`.
3. **Each page** is a `UserControl` under `Pages/`. Most pages are a `SmoothScrollViewer` over
   a `StackPanel` of `DemoSampleControl` cards.
4. **`DemoSampleControl`** is the reusable "sample card": a description, the live control(s),
   an optional options rail, and an expandable XAML/C# source viewer.

## How a page wires its samples (the one piece of "magic")

Named controls cannot be declared *inside* `DemoSampleControl` property elements (WPF raises
`MC3093`). So each page declares hidden `ContentControl` "slots" named
`DemoSampleSlotNNDemoContentHost` / `...OutputContentHost` / `...RightRailContentHost` (NN =
01-based sample index). In the page constructor, `DemoSamplePageWiring.Apply(...)` moves each
slot's content into the matching `DemoSampleControl` and attaches the source-code strings. See
`Pages/DemoSamplePageWiring.cs` for the full contract and `Pages/GalleryButtonsPage.xaml(.cs)`
for a complete example.

## Theming at design time

`Properties/DesignTimeResources.xaml` merges the library's `DesignTime.Light.xaml` (complete
computed colors + brushes for the default accent) so the XAML designer renders controls
correctly. It is design-time only and never merged at runtime.

## Where to look next

| You want to... | Open |
| --- | --- |
| See app startup + theme setup | `App.xaml.cs` |
| See the shell (window, nav, search) | `MainWindow.xaml` / `MainWindow.xaml.cs` |
| See the sample-card control | `Pages/DemoSampleControl.xaml(.cs)` |
| See a typical page | `Pages/GalleryButtonsPage.xaml(.cs)` |
| See shared spacing/styles | `Resources/DemoSharedStyles.xaml` |
```

- [ ] **Step 2: Verify BOM**

Run: `powershell -Command "[System.IO.File]::ReadAllBytes('Fluence.Wpf.Demo/README.md')[0..2]"`
Expected: `239 187 191`. If not, prepend the BOM and re-check.

- [ ] **Step 3: Commit**

```bash
git add Fluence.Wpf.Demo/README.md
git commit -m "demo(gallery): add beginner architecture README"
```

---

### Task A1.3: Document the shell and sample infrastructure (XML docs + beginner comments)

**Goal:** Add `///` XML docs to public types/members and beginner-oriented inline comments to the *non-obvious* code, without changing behavior. Apply the comment standard below; do **not** restate the obvious.

**Comment standard (apply consistently):**
- Every public class: a `<summary>` saying what it is and its role in the app.
- Every public method/property whose purpose isn't obvious from its name: `<summary>` (+ `<param>` where it clarifies).
- Each non-trivial private method: a one-line intent comment ("// why", not "// what").
- File-level "what is this and how does it fit" comment at the top of the infrastructure files.

**Files (in priority order):**
- Modify: `Fluence.Wpf.Demo/Pages/DemoSamplePageWiring.cs`
- Modify: `Fluence.Wpf.Demo/Pages/DemoSampleControl.xaml.cs`
- Modify: `Fluence.Wpf.Demo/MainWindow.xaml.cs`
- Modify: `Fluence.Wpf.Demo/App.xaml.cs`
- Modify: `Fluence.Wpf.Demo/DemoNavigationCatalog.cs`

- [ ] **Step 1: Document `DemoSamplePageWiring.cs` (the slot contract)**

At the top of the class, add a `<summary>` that explains the slot-naming convention and the `MC3093` reason it exists. Worked example to add immediately above the `Apply` method:

```csharp
/// <summary>
/// Wires every <see cref="DemoSampleControl"/> on a page to its live content and source code.
/// </summary>
/// <remarks>
/// WPF will not let you give an <c>x:Name</c> to a control declared directly inside a
/// <see cref="DemoSampleControl"/> property element (error MC3093). So pages instead declare
/// hidden <see cref="System.Windows.Controls.ContentControl"/> "slots" named
/// <c>DemoSampleSlotNNDemoContentHost</c> / <c>...OutputContentHost</c> /
/// <c>...RightRailContentHost</c> (NN is the 1-based sample index). This method finds each
/// slot, transfers its child into the matching sample card, attaches the XAML/C# source, and
/// clears the slot. The Nth <see cref="DemoSampleSource"/> argument supplies the Nth card's
/// source text, so the source count must equal the sample count.
/// </remarks>
/// <param name="root">The page's content root to search.</param>
/// <param name="sources">Source-code entries, one per sample, in document order.</param>
public static void Apply(DependencyObject root, params DemoSampleSource[] sources)
```

Then add one-line intent comments on the tree-walk and the slot-match/validation branches (explain the "silently fails if slot count != source count" failure mode the wiring guards against).

- [ ] **Step 2: Document `DemoSampleControl.xaml.cs`**

Add a class `<summary>` (the four zones: description / live demo / options rail / source expander) and `<summary>` on each dependency property (`SampleDescription`, `DemoContent`, `OutputContent`, `RightRailContent`, `XamlSource`, `CSharpSource`). Above the syntax-highlighting helpers (`AddXamlLine`, `AddCSharpLine`), add a comment explaining *why* there's a hand-rolled tokenizer (no external highlighter dependency; it colorizes the read-only source preview) so a beginner isn't intimidated.

- [ ] **Step 3: Document `MainWindow.xaml.cs`**

Add `<summary>` to the public surface (`NavigateTo`) and to the navigation fields (what `_pageByContainer` caches, what `_navigationBackStack` is for). Add intent comments to the visual-tree helpers `TryGetVisual*` / `IsVisualAncestorOf` and the title/search collision-detection block (one sentence: "hide the title text when the centered search box would overlap it").

- [ ] **Step 4: Document `App.xaml.cs`**

Add a `<summary>` on `OnStartup` describing the start sequence (theme on -> accent -> shared styles -> show). Add a clear comment above the `--smoke-test` branch: "Headless self-test used by CI/screenshots; not part of the normal app flow - safe to ignore when learning."

- [ ] **Step 5: Document `DemoNavigationCatalog.cs`**

Add a `<summary>` explaining it's the single list that drives the left menu (route tag, label, glyph, search keywords) and that adding a page means adding an entry here plus a case in `MainWindow.CreatePageForRoute`.

- [ ] **Step 6: Build + test (no behavior change)**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Then: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build --filter "FullyQualifiedName~Demo"`
Expected: Build 0/0; all `Demo*` tests pass (no count regression).

- [ ] **Step 7: Commit**

```bash
git add Fluence.Wpf.Demo/Pages/DemoSamplePageWiring.cs Fluence.Wpf.Demo/Pages/DemoSampleControl.xaml.cs Fluence.Wpf.Demo/MainWindow.xaml.cs Fluence.Wpf.Demo/App.xaml.cs Fluence.Wpf.Demo/DemoNavigationCatalog.cs
git commit -m "demo(gallery): document shell + sample infrastructure for beginners"
```

---

### Task A1.4: Add page-level guidance comments to representative pages

**Goal:** A beginner opening any page should see a top comment explaining the page's shape and the slot/wiring pattern. Apply to the three reference pages; the pattern then transfers to the rest.

**Files:**
- Modify: `Fluence.Wpf.Demo/Pages/GalleryHomePage.xaml(.cs)`
- Modify: `Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml(.cs)`
- Modify: `Fluence.Wpf.Demo/Pages/GalleryDataBindingPage.xaml(.cs)`

- [ ] **Step 1: Add a top-of-XAML comment to each page**

Immediately inside each page's root element, add a comment of this shape (tailor the wording per page). Example for `GalleryButtonsPage.xaml`:

```xml
<!--
    Buttons gallery page. Pattern used by most pages:
      * A SmoothScrollViewer over a StackPanel of DemoSampleControl "cards".
      * Live controls for each card live in hidden ContentControl slots named
        DemoSampleSlotNN...Host; the code-behind constructor calls
        DemoSamplePageWiring.Apply(...) to move them into the cards and attach source.
    See Fluence.Wpf.Demo/README.md and Pages/DemoSamplePageWiring.cs.
-->
```

- [ ] **Step 2: Comment the `DemoSamplePageWiring.Apply` call in each code-behind**

Above the `Apply(...)` call in each constructor, add:

```csharp
// Move each hidden slot's control into its DemoSampleControl card and attach the
// XAML/C# source shown in the expander. The Nth source maps to DemoSampleSlot{N}. See
// DemoSamplePageWiring for the slot-naming contract.
```

For `GalleryButtonsPage.xaml.cs`, also add a one-line comment on `_repeatButtonClickCount` / `RepeatCounterButton_Click` explaining it's the interactive demo's click counter.

- [ ] **Step 3: Build + targeted test**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Then: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build --filter "FullyQualifiedName~Demo"`
Expected: 0/0 build; Demo tests pass.

- [ ] **Step 4: Commit**

```bash
git add Fluence.Wpf.Demo/Pages/GalleryHomePage.xaml Fluence.Wpf.Demo/Pages/GalleryHomePage.xaml.cs Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml.cs Fluence.Wpf.Demo/Pages/GalleryDataBindingPage.xaml Fluence.Wpf.Demo/Pages/GalleryDataBindingPage.xaml.cs
git commit -m "demo(gallery): add page-level guidance comments to reference pages"
```

---

### Task A1.5: Full both-TFM test gate for Part A baseline

- [ ] **Step 1: Build**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: 0 warnings / 0 errors.

- [ ] **Step 2: Test net472**

Run: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --no-build`
Expected: `Passed: 688, Skipped: 5` (no regression).

- [ ] **Step 3: Test net10**

Run: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build`
Expected: `Passed: 689, Skipped: 5` (the full run also regenerates `docs/screenshots/*.png` - expected, not noise).

---

### A2 GATE — STOP

> **Do not start A2 without an explicit decision.** Present the user three choices:
> 1. **Skip A2** (ship Part A as document-only — A1 already delivers the design-time fix + beginner docs).
> 2. **Light refactor subset** — do only A2.1 (extract embedded source strings to files).
> 3. **Full deep restructure** — A2.1 + A2.2 + A2.3.
>
> A2 changes touch tested code (`DemoMainWindowTests`, `ControlTests.DemoParity/DemoSamplePolish`, `DemoSamplePageWiringTests`, `GalleryScreenshotHarness`) and carry regression risk. Re-run the full both-TFM gate after **every** A2 task.

### Task A2.1 (OPTIONAL): Move embedded sample source out of page code-behind

**Why:** Pages like `GalleryButtonsPage.xaml.cs` hold ~14 multi-hundred-line string constants of demo source, which buries the actual page logic. Move them to plain text resource files loaded at construction.

**Files (per page that embeds source; start with Buttons as the template):**
- Create: `Fluence.Wpf.Demo/Pages/Sources/GalleryButtons.{NN}.xaml.txt` and `.cs.txt` (embedded resources)
- Modify: `Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj` (add `EmbeddedResource` glob for `Pages/Sources/**`)
- Modify: `Fluence.Wpf.Demo/Pages/DemoSamplePageWiring.cs` (add a helper to read a named embedded source) or `GalleryButtonsPage.xaml.cs`

- [ ] **Step 1: Add the embedded-resource glob to the csproj**

In `Fluence.Wpf.Demo.csproj`, inside an `<ItemGroup>`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Pages\Sources\**\*.txt" />
</ItemGroup>
```

- [ ] **Step 2: Add a source-loading helper**

Add to `DemoSamplePageWiring.cs`:

```csharp
/// <summary>
/// Reads an embedded demo-source text file (build action: EmbeddedResource) by its
/// logical resource name suffix, e.g. "Pages.Sources.GalleryButtons.01.xaml.txt".
/// </summary>
public static string ReadSource(string resourceNameSuffix)
{
    System.Reflection.Assembly asm = typeof(DemoSamplePageWiring).Assembly;
    string fullName = Array.Find(asm.GetManifestResourceNames(),
        n => n.EndsWith(resourceNameSuffix, StringComparison.Ordinal))
        ?? throw new InvalidOperationException("Embedded demo source not found: " + resourceNameSuffix);
    using System.IO.Stream stream = asm.GetManifestResourceStream(fullName)
        ?? throw new InvalidOperationException("Could not open embedded demo source: " + fullName);
    using System.IO.StreamReader reader = new(stream);
    return reader.ReadToEnd();
}
```

- [ ] **Step 3: Move one page's constants to files, swap to `ReadSource`**

For `GalleryButtonsPage`: create `Pages/Sources/GalleryButtons.01.xaml.txt` … `.06.cs.txt` containing the current string-constant bodies verbatim, delete the constants, and change the `DemoSamplePageWiring.Apply(...)` call to use `DemoSamplePageWiring.ReadSource("Pages.Sources.GalleryButtons.01.xaml.txt")` etc.

- [ ] **Step 4: Build + the wiring/polish tests**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Then: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build --filter "FullyQualifiedName~DemoSample|FullyQualifiedName~DemoParity"`
Expected: 0/0 build; tests pass. If `ControlTests.DemoSamplePolish` asserts on specific source text, update those expectations to read the same embedded files (keep them in lockstep).

- [ ] **Step 5: Commit**

```bash
git add Fluence.Wpf.Demo/Pages/Sources Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml.cs Fluence.Wpf.Demo/Pages/DemoSamplePageWiring.cs Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj
git commit -m "demo(gallery): move Buttons sample source to embedded text files"
```

- [ ] **Step 6: Repeat Steps 3-5 for the remaining source-heavy pages**, one page per commit, re-running the gate each time.

### Task A2.2 (OPTIONAL): Type-safe page navigation

**Files:**
- Modify: `Fluence.Wpf.Demo/DemoNavigationCatalog.cs` (add a `Func<UserControl>` factory per item)
- Modify: `Fluence.Wpf.Demo/MainWindow.xaml.cs` (replace the `CreatePageForRoute` string switch with the catalog factory)

- [ ] **Step 1: Add a factory delegate to each catalog item; route through it in `EnsurePageContent`; delete the string switch. Keep `NavigateTo(string tag)` working (look the tag up in the catalog).**
- [ ] **Step 2: Build + `DemoMainWindowTests` + screenshot harness.** Run the full both-TFM gate. Expected: no regression (screenshots still capture every route).
- [ ] **Step 3: Commit** `demo(gallery): catalog-driven, type-safe page navigation`.

### Task A2.3 (OPTIONAL): Extract the smoke-test out of `App.xaml.cs`

**Files:**
- Create: `Fluence.Wpf.Demo/GallerySmokeTest.cs` (move the `--smoke-test` logic here)
- Modify: `Fluence.Wpf.Demo/App.xaml.cs` (call `GallerySmokeTest.Run(...)` from the branch; keep behavior identical)

- [ ] **Step 1: Move the methods, keep the entrypoint behavior byte-for-byte. Step 2: full both-TFM gate. Step 3: commit** `demo(gallery): extract smoke-test harness from App`.

---

## PART B — MVVM demo (`Fluence.Wpf.Demo.Mvvm`)

> The MVVM project is already well-documented and MVVM-clean. Scope here: make the **designer default to Dark with sample data**, add the small clarity comments the review flagged, and keep tests green. No nullable change (handbook-deliberate).

### Task B.1: Create the Dark design-time resources file

**Files:**
- Create: `Fluence.Wpf.Demo.Mvvm/Properties/DesignTimeResources.xaml`

- [ ] **Step 1: Create the file**

`Fluence.Wpf.Demo.Mvvm/Properties/DesignTimeResources.xaml` (UTF-8 BOM):

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!--
        Design-time-only resources for the XAML designer / Blend (never merged at runtime;
        App.xaml.cs calls ApplicationThemeManager.Apply at startup for the running app).
        This demo previews in DARK by default to match its intended look. Slot order mirrors
        the runtime model:
          [0] DesignTime.Dark.xaml - complete computed Dark colors + brushes (default #0078D4 accent)
          [1] Typography.xaml
          [2] Generic.xaml
    -->
    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="pack://application:,,,/Fluence.Wpf;component/Properties/DesignTime.Dark.xaml" />
        <ResourceDictionary Source="pack://application:,,,/Fluence.Wpf;component/Themes/Typography/Typography.xaml" />
        <ResourceDictionary Source="pack://application:,,,/Fluence.Wpf;component/Themes/Generic.xaml" />
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

- [ ] **Step 2: Verify BOM** (`...ReadAllBytes(...)[0..2]` = `239 187 191`).

### Task B.2: Register the design-time resources in the MVVM csproj

**Files:**
- Modify: `Fluence.Wpf.Demo.Mvvm/Fluence.Wpf.Demo.Mvvm.csproj`

- [ ] **Step 1: Add the Page item**

After the existing `<ItemGroup>` that holds the `ProjectReference`, add (mirrors the gallery csproj item):

```xml
<ItemGroup>
  <Page Update="Properties\DesignTimeResources.xaml" Condition="'$(DesignTime)'=='true' OR ('$(SolutionPath)'!='' AND Exists('$(SolutionPath)') AND '$(BuildingInsideVisualStudio)'!='true' AND '$(BuildingInsideExpressionBlend)'!='true')">
    <Generator>MSBuild:Compile</Generator>
    <SubType>Designer</SubType>
    <ContainsDesignTimeResources>true</ContainsDesignTimeResources>
  </Page>
</ItemGroup>
```

- [ ] **Step 2: Solution build (compiles the design-time page)**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: 0/0. A bad pack URI or malformed page fails here.

### Task B.3: Seed sample tasks for first-run UX and design-time data

**Files:**
- Modify: `Fluence.Wpf.Demo.Mvvm/ViewModels/MainViewModel.cs`

**Context:** `MainViewModel` currently starts empty (no constructor seeds `_allTasks`), so both the running app and the designer show a blank list. Add a constructor that seeds a few sample tasks via the existing `Add`-style path so completion toggles still refresh the filter.

- [ ] **Step 1: Add the constructor**

Add to `MainViewModel` (just above the `Add` command, after the derived properties). Use the existing subscribe-then-add pattern so `OnItemPropertyChanged` is wired:

```csharp
/// <summary>
/// Seeds a few sample tasks so the app is useful on first run and the XAML designer shows
/// realistic rows. New items are wired to <see cref="OnItemPropertyChanged"/> exactly like
/// <see cref="Add"/> does, so toggling completion refreshes the filtered view and the footer.
/// </summary>
public MainViewModel()
{
    Seed("Try the Fluence.Wpf MVVM demo", isCompleted: true);
    Seed("Toggle a task to see the strikethrough + progress update", isCompleted: false);
    Seed("Filter by All / Pending / Completed", isCompleted: false);
    Seed("Add your own task below", isCompleted: false);
    Refresh();
}

private void Seed(string title, bool isCompleted)
{
    TaskItemViewModel item = new(title) { IsCompleted = isCompleted };
    item.PropertyChanged += OnItemPropertyChanged;
    _allTasks.Add(item);
}
```

- [ ] **Step 2: Build**

Run: `dotnet build Fluence.Wpf.Demo.Mvvm/Fluence.Wpf.Demo.Mvvm.csproj -c Debug`
Expected: 0/0. (Confirms `Seed`/ctor compile against the existing members.)

### Task B.4: Make the designer show the seeded data

**Files:**
- Modify: `Fluence.Wpf.Demo.Mvvm/MainWindow.xaml`

- [ ] **Step 1: Make `d:DataContext` design-time-creatable**

In `MainWindow.xaml`, change line 15:

```xml
d:DataContext="{d:DesignInstance vm:MainViewModel}"
```

to:

```xml
d:DataContext="{d:DesignInstance vm:MainViewModel, IsDesignTimeCreatable=True}"
```

This makes the designer instantiate the real `MainViewModel` (now seeded), so the task list, status text, and progress bar render with sample data in Dark.

- [ ] **Step 2: Solution build**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: 0/0.

- [ ] **Step 3: Manual designer check (record result)**

Open `Fluence.Wpf.Demo.Mvvm/MainWindow.xaml` in the designer. Confirm: Dark surface, four sample rows (one struck-through/completed), footer progress reflects 1/4. Note outcome in the commit message.

### Task B.5: Add the two clarity comments the review flagged + README note

**Files:**
- Modify: `Fluence.Wpf.Demo.Mvvm/ViewModels/MainViewModel.cs`
- Modify: `Fluence.Wpf.Demo.Mvvm/README.md`

- [ ] **Step 1: Comment the `Refresh()` consolidation**

Above `private void Refresh()`, add:

```csharp
// Single rebuild path: Add/Delete/ClearCompleted and any IsCompleted toggle all funnel here.
// We rebuild DisplayedTasks from _allTasks rather than mutating it in place so the filtered
// view is always consistent in one step, then raise the derived StatusText/ProgressValue.
```

- [ ] **Step 2: Add a README note about design-time theme + sample data**

Append a short section to `Fluence.Wpf.Demo.Mvvm/README.md`:

```markdown
## Design-time preview

`Properties/DesignTimeResources.xaml` merges the library's `DesignTime.Dark.xaml`, so the XAML
designer renders this window in **Dark** (the running app uses `ApplicationTheme.Auto`).
`MainWindow.xaml` uses `d:DataContext="{d:DesignInstance vm:MainViewModel, IsDesignTimeCreatable=True}"`,
so the designer instantiates the real (seeded) `MainViewModel` and shows sample rows.
```

- [ ] **Step 3: Build + MVVM-relevant tests**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Then: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build --filter "FullyQualifiedName~Mvvm|FullyQualifiedName~Demo"`
Expected: 0/0 build; tests pass. **Note:** `GalleryScreenshotHarness.CaptureMvvmDemoAt` seeds its own tasks then captures - confirm the MVVM screenshot still renders (it tolerates pre-seeded rows because it adds more). If any MVVM test asserts an empty initial list, update it to expect the four seeded rows.

- [ ] **Step 4: Commit**

```bash
git add Fluence.Wpf.Demo.Mvvm/Properties/DesignTimeResources.xaml Fluence.Wpf.Demo.Mvvm/Fluence.Wpf.Demo.Mvvm.csproj Fluence.Wpf.Demo.Mvvm/ViewModels/MainViewModel.cs Fluence.Wpf.Demo.Mvvm/MainWindow.xaml Fluence.Wpf.Demo.Mvvm/README.md
git commit -m "demo(mvvm): Dark design-time resources, seeded sample data, clarity comments"
```

---

## PART C — PowerShell example scripts (`Fluence.Wpf.Demo.PowerShell`)

> Windows PowerShell 5.1 / `net472`. Each script is **self-contained** (inline bootstrap) so a beginner can read one file top-to-bottom. The canonical pattern every script follows: **(1)** relaunch STA if needed, **(2)** locate/build the `net472` DLL, **(3)** `Add-Type` the WPF + Fluence assemblies, **(4)** `new System.Windows.Application` (required - `ApplicationThemeManager.Apply` no-ops if `Application.Current` is null), **(5)** `Apply` theme+backdrop, **(6)** parse XAML, wire handlers, **(7)** `$app.Run($window)`.

### Task C.1: Delete the three old scripts

**Files:**
- Delete: `Fluence.Wpf.Demo.PowerShell/Show-ThemeDemo.ps1`, `Show-ControlsDemo.ps1`, `Show-ProgressDemo.ps1`

- [ ] **Step 1: Delete**

```bash
git rm Fluence.Wpf.Demo.PowerShell/Show-ThemeDemo.ps1 Fluence.Wpf.Demo.PowerShell/Show-ControlsDemo.ps1 Fluence.Wpf.Demo.PowerShell/Show-ProgressDemo.ps1
```

(Keep `MainWindow.xaml` - it is reused by `04-LoadXamlFile.ps1` and the screenshot harness.)

### Task C.2: Create `01-HelloWorld.ps1` (the required script)

**Files:**
- Create: `Fluence.Wpf.Demo.PowerShell/01-HelloWorld.ps1`

- [ ] **Step 1: Write the script**

```powershell
#Requires -Version 5.1
<#
.SYNOPSIS
    Smallest possible Fluence.Wpf window from PowerShell: a Mica window with a button that
    cycles the backdrop (Mica -> Acrylic -> Tabbed -> None) and a label that rotates through
    "Hello, World!" greetings.
.NOTES
    Run with:  powershell.exe -STA -File .\01-HelloWorld.ps1
    WPF requires a single-threaded apartment (STA); the script relaunches itself if needed.
#>

# --- 1. WPF needs STA. Relaunch ourselves in STA if we are not already there. ---
if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA') {
    powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File $PSCommandPath @args
    return
}

$ErrorActionPreference = 'Stop'

# --- 2. Find the net472 build of Fluence.Wpf.dll; build it once if missing. ---
$dll = Join-Path $PSScriptRoot '..\Fluence.Wpf\bin\Release\net472\Fluence.Wpf.dll'
if (-not (Test-Path -LiteralPath $dll)) {
    Write-Host 'Building Fluence.Wpf (net472, Release) - first run only...'
    dotnet build (Join-Path $PSScriptRoot '..\Fluence.Wpf\Fluence.Wpf.csproj') -c Release -f net472 --nologo -v q
}

# --- 3. Load WPF + Fluence. ---
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Xaml
Add-Type -Path $dll

# --- 4. A WPF Application must exist BEFORE theming: ApplicationThemeManager.Apply publishes
#        brushes into Application.Current.Resources and silently no-ops if there is no app. ---
$app = New-Object System.Windows.Application

# --- 5. Turn the theme engine on. Auto = follow the Windows light/dark setting. ---
[Fluence.Wpf.ApplicationThemeManager]::Apply(
    [Fluence.Wpf.ApplicationTheme]::Auto,
    [Fluence.Wpf.BackdropType]::Mica,
    $true)
[Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()

# --- 6. Build the window from inline XAML. ui: = the Fluence.Wpf.Controls namespace. ---
$xaml = @'
<ui:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"
    Title="Fluence.Wpf - PowerShell Hello World"
    Width="520" Height="340"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
        <TextBlock x:Name="HelloLabel"
                   Text="Hello, World!"
                   HorizontalAlignment="Center"
                   ui:TextBlockExtensions.Typography="Title"
                   Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <TextBlock x:Name="BackdropLabel"
                   Text="Backdrop: Mica"
                   Margin="0,8,0,20"
                   HorizontalAlignment="Center"
                   Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
        <ui:Button x:Name="CycleButton"
                   Content="Next backdrop + greeting"
                   Appearance="Accent"
                   HorizontalAlignment="Center" />
    </StackPanel>
</ui:FluenceWindow>
'@

$window = [System.Windows.Markup.XamlReader]::Parse($xaml)

# --- 7. Wire the button. Each click advances the backdrop and the greeting. ---
$backdrops = @('Mica', 'Acrylic', 'Tabbed', 'None')
$greetings = @('Hello, World!', 'Hej, varlden!', 'Hola, mundo!', 'Bonjour, le monde!', 'Ola, mundo!', 'Ciao, mondo!')
$script:tick = 0

$helloLabel    = $window.FindName('HelloLabel')
$backdropLabel = $window.FindName('BackdropLabel')
$cycleButton   = $window.FindName('CycleButton')

$cycleButton.add_Click({
    $script:tick++
    $name = $backdrops[$script:tick % $backdrops.Count]
    # Setting the window's SystemBackdropType re-applies the DWM backdrop live.
    $window.SystemBackdropType = [Enum]::Parse([Fluence.Wpf.BackdropType], $name)
    $backdropLabel.Text = "Backdrop: $name"
    $helloLabel.Text    = $greetings[$script:tick % $greetings.Count]
})

# Follow OS light/dark changes while open; stop watching on close.
[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({ [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window) })

# --- 8. Show the window and pump the WPF message loop until it closes. ---
[void]$app.Run($window)
```

- [ ] **Step 2: Verify it parses (syntax check)**

Run: `powershell.exe -NoProfile -STA -Command "$null = [scriptblock]::Create((Get-Content -Raw '.\Fluence.Wpf.Demo.PowerShell\01-HelloWorld.ps1')); 'parsed ok'"`
Expected: `parsed ok` (no parse errors). This validates PowerShell syntax without launching the GUI.

- [ ] **Step 3: Manual run (record result)**

Run: `powershell.exe -STA -File .\Fluence.Wpf.Demo.PowerShell\01-HelloWorld.ps1`
Expected: a Mica window opens; clicking the button cycles Mica -> Acrylic -> Tabbed -> None and the greeting changes. Close the window. Note outcome.

- [ ] **Step 4: Verify BOM, commit**

```bash
git add Fluence.Wpf.Demo.PowerShell/01-HelloWorld.ps1
git commit -m "demo(powershell): add 01-HelloWorld backdrop-cycle example"
```

### Task C.3: Create `02-ThemeAndAccent.ps1`

**Files:**
- Create: `Fluence.Wpf.Demo.PowerShell/02-ThemeAndAccent.ps1`

- [ ] **Step 1: Write the script** (same bootstrap as Steps 1-5 of C.2; differing window + handlers below)

Reuse the identical `#Requires`/STA-relaunch/DLL-locate/`Add-Type`/`$app = New-Object ...`/`Apply`/`ApplySystemAccent` bootstrap from `01-HelloWorld.ps1`, then this window and wiring:

```powershell
$xaml = @'
<ui:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"
    Title="Fluence.Wpf - Theme and Accent"
    Width="560" Height="360"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <StackPanel Margin="24" VerticalAlignment="Center">
        <TextBlock Text="Theme" ui:TextBlockExtensions.Typography="Subtitle"
                   Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <StackPanel Orientation="Horizontal" Margin="0,8,0,16">
            <ui:Button x:Name="LightBtn"  Content="Light"  Margin="0,0,8,0" />
            <ui:Button x:Name="DarkBtn"   Content="Dark"   Margin="0,0,8,0" />
            <ui:Button x:Name="AutoBtn"   Content="Auto (follow Windows)" />
        </StackPanel>
        <TextBlock Text="Accent" ui:TextBlockExtensions.Typography="Subtitle"
                   Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <StackPanel Orientation="Horizontal" Margin="0,8,0,16">
            <ui:Button x:Name="AccentBtn"       Content="Cycle custom accent" Appearance="Accent" Margin="0,0,8,0" />
            <ui:Button x:Name="SystemAccentBtn" Content="Use system accent" />
        </StackPanel>
        <ui:InfoBar x:Name="StatusBar" IsOpen="True" IsClosable="False"
                    Severity="Informational"
                    Title="Tip"
                    Message="Change the Windows theme while this is open - Auto follows it live." />
    </StackPanel>
</ui:FluenceWindow>
'@

$window = [System.Windows.Markup.XamlReader]::Parse($xaml)

$window.FindName('LightBtn').add_Click({ [Fluence.Wpf.ApplicationThemeManager]::Apply([Fluence.Wpf.ApplicationTheme]::Light, [Fluence.Wpf.BackdropType]::Mica, $true) })
$window.FindName('DarkBtn').add_Click({  [Fluence.Wpf.ApplicationThemeManager]::Apply([Fluence.Wpf.ApplicationTheme]::Dark,  [Fluence.Wpf.BackdropType]::Mica, $true) })
$window.FindName('AutoBtn').add_Click({  [Fluence.Wpf.ApplicationThemeManager]::Apply([Fluence.Wpf.ApplicationTheme]::Auto,  [Fluence.Wpf.BackdropType]::Mica, $true) })

# A small palette to cycle through with ApplyCustomAccent(Color).
$accents = @(
    [System.Windows.Media.Color]::FromRgb(0x00, 0x78, 0xD4),  # blue
    [System.Windows.Media.Color]::FromRgb(0x10, 0x89, 0x3E),  # green
    [System.Windows.Media.Color]::FromRgb(0xC4, 0x2B, 0x1C),  # red
    [System.Windows.Media.Color]::FromRgb(0x74, 0x37, 0xC9)   # purple
)
$script:accentIndex = 0
$window.FindName('AccentBtn').add_Click({
    $color = $accents[$script:accentIndex % $accents.Count]
    $script:accentIndex++
    [Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent($color)
})
$window.FindName('SystemAccentBtn').add_Click({ [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent() })

[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({ [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window) })

[void]$app.Run($window)
```

- [ ] **Step 2: Parse-check (as C.2 Step 2), manual run, BOM, commit** `demo(powershell): add 02-ThemeAndAccent example`.

### Task C.4: Create `03-ControlsTour.ps1`

**Files:**
- Create: `Fluence.Wpf.Demo.PowerShell/03-ControlsTour.ps1`

- [ ] **Step 1: Write the script** (same bootstrap; window showcases common controls inside a scrolling card with one live interaction)

After the shared bootstrap, use:

```powershell
$xaml = @'
<ui:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"
    Title="Fluence.Wpf - Controls tour"
    Width="620" Height="560"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <ui:SmoothScrollViewer>
        <StackPanel Margin="24">
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Buttons" ui:TextBlockExtensions.Typography="Subtitle"
                               Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <StackPanel Orientation="Horizontal">
                        <ui:Button Content="Standard" Margin="0,0,8,0" />
                        <ui:Button Content="Accent" Appearance="Accent" Margin="0,0,8,0" />
                        <ui:Button Content="Disabled" IsEnabled="False" />
                    </StackPanel>
                </StackPanel>
            </ui:Card>
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Selection" ui:TextBlockExtensions.Typography="Subtitle"
                               Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <ui:ToggleSwitch x:Name="DemoToggle" OnContent="On" OffContent="Off" Margin="0,0,0,8" />
                    <ui:CheckBox Content="I am a checkbox" Margin="0,0,0,8" />
                    <ui:RadioButton Content="Option A" GroupName="Demo" Margin="0,0,0,4" />
                    <ui:RadioButton Content="Option B" GroupName="Demo" />
                </StackPanel>
            </ui:Card>
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Text input" ui:TextBlockExtensions.Typography="Subtitle"
                               Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <ui:TextBox PlaceholderText="Type here" Margin="0,0,0,8" />
                    <ui:NumberBox Header="A number" Minimum="0" Maximum="100" SpinButtonPlacementMode="Compact" />
                </StackPanel>
            </ui:Card>
            <ui:InfoBar x:Name="StatusBar" IsOpen="True" IsClosable="False"
                        Severity="Informational" Title="Toggle state"
                        Message="Flip the switch above to update this message from PowerShell." />
        </StackPanel>
    </ui:SmoothScrollViewer>
</ui:FluenceWindow>
'@

$window = [System.Windows.Markup.XamlReader]::Parse($xaml)

# One live interaction: the toggle drives the InfoBar text via a PowerShell handler.
$bar = $window.FindName('StatusBar')
$toggle = $window.FindName('DemoToggle')
$toggle.add_Checked({   $bar.Message = 'The switch is ON (handled in PowerShell).' })
$toggle.add_Unchecked({ $bar.Message = 'The switch is OFF (handled in PowerShell).' })

[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({ [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window) })

[void]$app.Run($window)
```

- [ ] **Step 2: Parse-check, manual run, BOM, commit** `demo(powershell): add 03-ControlsTour example`.

### Task C.5: Create `04-LoadXamlFile.ps1` (loads `MainWindow.xaml` from disk)

**Why:** Teaches the more maintainable pattern of keeping XAML in its own file, and keeps `MainWindow.xaml` meaningful (it is also the screenshot-harness template).

**Files:**
- Create: `Fluence.Wpf.Demo.PowerShell/04-LoadXamlFile.ps1`
- Verify: `Fluence.Wpf.Demo.PowerShell/MainWindow.xaml` still has the named controls the script wires (`ThemeComboBox`, `AccentButton`, `SystemAccentButton`).

- [ ] **Step 1: Confirm the named controls exist**

Read `Fluence.Wpf.Demo.PowerShell/MainWindow.xaml`; confirm the `x:Name` values referenced below exist (`ThemeComboBox`, `AccentButton`, `SystemAccentButton`). If a name differs, adjust the script's `FindName` calls to match (do not rename the XAML - the screenshot harness loads this file by content, names don't matter to it, but keep it valid).

- [ ] **Step 2: Write the script** (shared bootstrap; then load the file instead of an inline string)

After the shared bootstrap (Steps 1-5 of C.2), use:

```powershell
# Load XAML from a file instead of an inline string (XamlReader.Load over a file stream).
$xamlPath = Join-Path $PSScriptRoot 'MainWindow.xaml'
$window = $null
$stream = [System.IO.File]::OpenRead($xamlPath)
try {
    $reader = [System.Xml.XmlReader]::Create($stream)
    try { $window = [System.Windows.Markup.XamlReader]::Load($reader) }
    finally { $reader.Dispose() }
}
finally { $stream.Dispose() }

# Wire the controls that MainWindow.xaml exposes by name.
$themeCombo = $window.FindName('ThemeComboBox')
if ($null -ne $themeCombo) {
    $themeCombo.add_SelectionChanged({
        param($s, $e)
        # The ComboBox items map (by index) to Auto / Light / Dark / HighContrast.
        $theme = switch ($s.SelectedIndex) {
            1       { [Fluence.Wpf.ApplicationTheme]::Light }
            2       { [Fluence.Wpf.ApplicationTheme]::Dark }
            3       { [Fluence.Wpf.ApplicationTheme]::HighContrast }
            default { [Fluence.Wpf.ApplicationTheme]::Auto }
        }
        [Fluence.Wpf.ApplicationThemeManager]::Apply($theme, [Fluence.Wpf.BackdropType]::Mica, $true)
    })
}
$accentBtn = $window.FindName('AccentButton')
if ($null -ne $accentBtn) { $accentBtn.add_Click({ [Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent([System.Windows.Media.Color]::FromRgb(0x74, 0x37, 0xC9)) }) }
$sysAccentBtn = $window.FindName('SystemAccentButton')
if ($null -ne $sysAccentBtn) { $sysAccentBtn.add_Click({ [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent() }) }

[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({ [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window) })

[void]$app.Run($window)
```

- [ ] **Step 3: Parse-check, manual run, BOM, commit** `demo(powershell): add 04-LoadXamlFile example (external XAML)`.

### Task C.6: Rewrite the PowerShell README

**Files:**
- Modify: `Fluence.Wpf.Demo.PowerShell/README.md`

- [ ] **Step 1: Replace the README body** (UTF-8 BOM) describing the new lineup:

```markdown
# Fluence.Wpf - PowerShell examples

Self-contained Windows PowerShell 5.1 scripts that build a themed Fluent window with no
project, no compilation step of your own - just the Fluence.Wpf DLL loaded at runtime.

## Requirements

- Windows PowerShell 5.1 (built into Windows) run in STA mode. Each script relaunches itself
  in STA automatically.
- The .NET SDK on PATH (`dotnet`). On first run a script builds the `net472` Fluence.Wpf.dll
  if it is not already present.

## Run

```powershell
powershell.exe -STA -File .\01-HelloWorld.ps1
```

| Script | Shows |
| --- | --- |
| `01-HelloWorld.ps1` | The smallest example: a Mica window, a button that cycles Mica/Acrylic/Tabbed/None, and a rotating "Hello, World!" label. |
| `02-ThemeAndAccent.ps1` | Switching Light/Dark/Auto, cycling a custom accent, returning to the system accent, and following OS theme changes with `SystemThemeWatcher`. |
| `03-ControlsTour.ps1` | Common controls (buttons, toggle, checkbox, radio, text box, number box) in cards, with a toggle that updates an `InfoBar` from PowerShell. |
| `04-LoadXamlFile.ps1` | Loading the UI from `MainWindow.xaml` on disk instead of an inline string, then wiring its named controls. |

## The pattern every script uses

1. Relaunch in STA (WPF requirement).
2. Locate `..\Fluence.Wpf\bin\Release\net472\Fluence.Wpf.dll`; `dotnet build` it once if missing.
3. `Add-Type` the WPF assemblies + the Fluence DLL.
4. Create a `System.Windows.Application` **before** theming (otherwise the theme brushes have
   nowhere to publish).
5. `ApplicationThemeManager.Apply(theme, backdrop, updateAccent)`.
6. Parse XAML (`XamlReader.Parse` for a string, `XamlReader.Load` for a file), wire handlers
   with `$control.add_Click({ ... })`.
7. `$app.Run($window)` to show the window and run the message loop.

See `../docs/powershell.md` for the full guide.
```

- [ ] **Step 2: Verify BOM, commit** `demo(powershell): rewrite README for the new examples`.

### Task C.7: Confirm the screenshot harness still passes

- [ ] **Step 1: Build + run the screenshot harness test**

Run: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --filter "FullyQualifiedName~GalleryScreenshotHarness"`
Expected: passes (it loads `Fluence.Wpf.Demo.PowerShell/MainWindow.xaml`, which we kept). If it fails because `MainWindow.xaml` changed, restore it to a loadable state.

---

## PART D — PowerShell documentation (documentation agent)

### Task D.1: Generate `docs/powershell.md` via the documentation agent

**Files:**
- Create: `docs/powershell.md`

- [ ] **Step 1: Dispatch the documentation agent**

Use the Agent tool with `subagent_type: documentation-updater` and this brief:

> Write `F:\FRebuild\Fluence.Wpf\docs\powershell.md`: a comprehensive guide to using Fluence.Wpf from Windows PowerShell 5.1. Audience: PowerShell scripters new to WPF. Match the tone/format of the existing `docs/getting-started.md` and `docs/theming.md`. Save as UTF-8 with BOM, LF newlines, no em/en dashes (repo style rule).
>
> Cover, with copy-pasteable snippets verified against the example scripts in `Fluence.Wpf.Demo.PowerShell/01-HelloWorld.ps1` … `04-LoadXamlFile.ps1`:
> 1. **Why/when** - theming WPF PowerShell GUIs with one DLL, no project.
> 2. **Prerequisites** - Windows PowerShell 5.1, STA mode (and the self-relaunch snippet), the .NET SDK, the `net472` build of `Fluence.Wpf.dll` and where it lives (`Fluence.Wpf/bin/Release/net472/`).
> 3. **The canonical bootstrap** - STA relaunch -> locate/build DLL -> `Add-Type` -> create `System.Windows.Application` (explain it MUST exist before `ApplicationThemeManager.Apply` or brushes don't publish) -> `Apply` -> XAML -> `$app.Run($window)`.
> 4. **Theme API from PowerShell** - the exact static calls: `[Fluence.Wpf.ApplicationThemeManager]::Apply([Fluence.Wpf.ApplicationTheme]::Auto, [Fluence.Wpf.BackdropType]::Mica, $true)`; `ApplicationTheme` values (Light/Dark/HighContrast/Auto); `BackdropType` values (None/Auto/Mica/Acrylic/Tabbed); `[Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()` and `::ApplyCustomAccent([System.Windows.Media.Color]::FromRgb(r,g,b))`; `[Fluence.Wpf.SystemThemeWatcher]::Watch($window)` / `::UnWatch($window)`.
> 5. **`FluenceWindow` from XAML** - the `xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"` declaration, `SystemBackdropType`, `ExtendsContentIntoTitleBar`, `CornerStyle`; changing `$window.SystemBackdropType` live; binding control colors with `{DynamicResource TextFillColorPrimaryBrush}` (point to `docs/theming.md` for the token list).
> 6. **Wiring events** - `$window.FindName('X')` + `$control.add_Click({ ... })`; the `$script:` scope gotcha for handler state.
> 7. **A walkthrough of each of the four example scripts** (one short subsection each, linking to the file).
> 8. **Troubleshooting** - "window opens unstyled/black" (no `Application` created, or `Apply` not called); "STA" errors; "type not found" (DLL TFM mismatch - this targets PS 5.1 / net472, not pwsh 7); building the DLL manually.
>
> Do not invent API members - everything above is confirmed in the source. Output only the file.

- [ ] **Step 2: Review the generated doc**

Read `docs/powershell.md`. Verify every code snippet matches the actual scripts and the API names are exact (cross-check against `Fluence.Wpf/ApplicationThemeManager.cs`, `ApplicationAccentColorManager.cs`, `SystemThemeWatcher.cs`, `Controls/FluenceWindow.cs`). Fix any drift. Verify BOM.

- [ ] **Step 3: Commit**

```bash
git add docs/powershell.md
git commit -m "docs: add PowerShell usage guide"
```

### Task D.2: Link the new doc into the docs map

**Files:**
- Modify: `README.md` (repo root) — add `docs/powershell.md` to the documentation list.
- Modify: `docs/getting-started.md` — add a one-line "Using Fluence.Wpf from PowerShell -> see docs/powershell.md" pointer.
- Modify: `AGENTS.md` §10 (Documentation map) — add `[docs/powershell.md](docs/powershell.md)` to the public doc set list.

- [ ] **Step 1: Add the link in each of the three files** (one bullet/line each, matching surrounding formatting).

- [ ] **Step 2: Commit**

```bash
git add README.md docs/getting-started.md AGENTS.md
git commit -m "docs: link PowerShell guide into the docs map"
```

### Task D.3: CHANGELOG entry

**Files:**
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Add entries under `[Unreleased]`**

Under `### Added`:

```markdown
- PowerShell examples: `Fluence.Wpf.Demo.PowerShell` now ships four self-contained Windows PowerShell 5.1 scripts (`01-HelloWorld.ps1` backdrop-cycle, `02-ThemeAndAccent.ps1`, `03-ControlsTour.ps1`, `04-LoadXamlFile.ps1`) plus a new `docs/powershell.md` guide, replacing the previous three demo scripts.
- Beginner documentation: `Fluence.Wpf.Demo` gains a README and inline/XML-doc comments across the shell and sample infrastructure; the gallery design-time resources now merge the computed `DesignTime.Light.xaml` (colors + brushes) so the XAML designer renders controls correctly.
- `Fluence.Wpf.Demo.Mvvm` now ships `Properties/DesignTimeResources.xaml` (designer defaults to Dark via `DesignTime.Dark.xaml`), a design-time-creatable `d:DataContext`, and seeded sample tasks so both the running app and the designer show realistic data.
```

- [ ] **Step 2: Commit** `docs: changelog for demo polish + PowerShell examples`.

---

## Final acceptance gate (run after all parts)

- [ ] **Build:** `dotnet build Fluence.Wpf.sln -c Debug` -> 0 warnings / 0 errors.
- [ ] **Test net472:** `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --no-build` -> `Passed >= 688, Skipped 5` (plus any tests you updated for A2.1/B.5).
- [ ] **Test net10:** `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build` -> `Passed >= 689, Skipped 5`.
- [ ] **Manual visual:** Gallery renders correctly in the designer (Light); MVVM renders Dark + sample rows in the designer; all four PowerShell scripts launch and behave.
- [ ] **Screenshots:** the full net10 run regenerated `docs/screenshots/*.png` (expected). Confirm `apps/powershell-*.png` and `mvvm-*.png` look right.
- [ ] **Docs synced:** `docs/powershell.md` exists and is linked from README/getting-started/AGENTS; CHANGELOG updated.
- [ ] **Working tree** is "git-clean minus the intended diff"; **do not commit/push until the user asks** (or per the executing-plans checkpoint policy).

---

## Self-review notes (for the implementer)

- **Spec coverage:** Gallery review = A1 (+ optional A2); MVVM review + dark design-time = B; PowerShell scripts = C (one Hello World + three topic scripts, self-contained inline bootstrap, PS 5.1/net472); PowerShell docs via the documentation agent = D. Design-time resources: Gallery -> `DesignTime.Light.xaml` (A1.1), MVVM -> `DesignTime.Dark.xaml` (B.1-B.4).
- **Known risk (Part C):** `MainWindow.xaml` is a screenshot-harness dependency; C keeps and reuses it (C.5/C.7).
- **Known risk (Part B):** seeding tasks changes the MVVM app's initial state; B.5 Step 3 calls out updating any test that assumed an empty initial list.
- **Deferred decision:** the A2 gate lets the user ship document-only, a light refactor, or the full restructure for the gallery.
```
