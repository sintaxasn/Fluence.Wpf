# Demo App Visual Polish — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring all 17 gallery content pages into visual parity with `GalleryHomePage` (the manually-rewritten reference page), fix dark-mode swatch contrast on `GalleryColorsPage`, and fix the `DemoSampleControl` Expander border in dark mode.

**Architecture:** Three targeted changes in order of impact. (1) Standardize root StackPanel margins across all 17 content pages to match the reference Home page (28px L/R, 28px bottom). (2) Fix `GalleryColorsPage` — add missing page title, remove incorrect TextBlock Background attributes on swatch cells, and restore readable contrast in dark mode using `TextFillColorInverseBrush`. (3) Fix `DemoSampleControl` Expander border brush so it matches the Card border in both themes.

**Tech Stack:** WPF XAML, DynamicResource brushes, Fluence.Wpf controls (`ui:Card`, `fluence:Expander`), net472 + net10.0-windows, MSTest v3.2.

---

## Reference: What GalleryHomePage establishes

The manually-rewritten home page (`Fluence.Wpf.Demo/Pages/GalleryHomePage.xaml`) is the design authority. Key measurements from it:

- Root `StackPanel Margin="28,0,28,0"` — 28px left/right gutter, 0 top (hero section owns top spacing), 0 bottom (bug, fixed in Task 1)
- `ui:Card` with `Variant="{x:Static uicore:CardVariant.Default}"` and `Padding="16"` or `Padding="20"`
- `UniformGrid` with explicit column gutters via card `Margin` values (`0,0,6,12` / `6,0,6,12` / `6,0,0,12`)
- All text via `ui:TextBlockExtensions.Typography="..."` or shared styles from `DemoSharedStyles.xaml`
- All colors via `DynamicResource` — no hardcoded hex values

---

## File Map

| File | Line | Change |
|------|------|--------|
| `Fluence.Wpf.Demo/Pages/GalleryHomePage.xaml` | 13 | Add 28px bottom margin |
| `Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml` | 13 | `Margin="24"` → `Margin="28,24,28,28"` |
| `Fluence.Wpf.Demo/Pages/GallerySelectionPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryInputsPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryDataPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryTabsPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryNavigationPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryWindowPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryStatusPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryColorsPage.xaml` | 13 | same + title fix + swatch contrast |
| `Fluence.Wpf.Demo/Pages/GalleryGlyphsPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryAccessibilityPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryMenusPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryFormsPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryDataBindingPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryTypographyPage.xaml` | 12 | same + add `HorizontalAlignment="Stretch"` |
| `Fluence.Wpf.Demo/Pages/GalleryLayoutPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/GalleryTreesPage.xaml` | 13 | same |
| `Fluence.Wpf.Demo/Pages/DemoSampleControl.xaml` | 49 | Add `BorderBrush` to Expander |

No new files. No new resources. No test changes (visual-only changes). No logic changes.

---

## Task 1: Fix GalleryHomePage bottom padding

**Files:**
- Modify: `Fluence.Wpf.Demo/Pages/GalleryHomePage.xaml:13`

The root StackPanel has `Margin="28,0,28,0"` — 0 bottom padding. Content is flush with the window edge when scrolled to the bottom. All other pages will get 28px bottom (Task 2), so the Home page needs the same.

- [ ] **Step 1: Apply margin fix**

In `GalleryHomePage.xaml` line 13, change:
```xml
<StackPanel Margin="28,0,28,0" HorizontalAlignment="Stretch">
```
to:
```xml
<StackPanel Margin="28,0,28,28" HorizontalAlignment="Stretch">
```

- [ ] **Step 2: Build to confirm no regressions**

```powershell
dotnet build "F:/StagedMigration/Fluence.Wpf/Fluence.Wpf.sln" -c Debug
```
Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Commit**

```
git add Fluence.Wpf.Demo/Pages/GalleryHomePage.xaml
git commit -m "fix(demo): add 28px bottom padding to Home page container"
```

---

## Task 2: Standardize root StackPanel margin across all 17 content pages

**Files:** 17 pages listed in the file map above (every Gallery*.xaml except GalleryHomePage.xaml).

All 17 non-home content pages have `<StackPanel Margin="24"` as the direct child of `<ui:SmoothScrollViewer>`. The reference Home page uses 28px for left/right gutter. Aligning all pages to `Margin="28,24,28,28"` (28 L/R matching home, 24px top to clear the nav bar content area, 28px bottom) unifies the gutter and ensures content is not flush with the bottom.

`GalleryTypographyPage.xaml` line 12 also lacks `HorizontalAlignment="Stretch"` — add it.

- [ ] **Step 1: Apply to GalleryButtonsPage (first, verify pattern is correct)**

`Fluence.Wpf.Demo/Pages/GalleryButtonsPage.xaml` line 13:
```xml
<!-- BEFORE -->
<StackPanel Margin="24" HorizontalAlignment="Stretch">

<!-- AFTER -->
<StackPanel Margin="28,24,28,28" HorizontalAlignment="Stretch">
```

- [ ] **Step 2: Apply to the remaining 16 pages**

Apply the identical one-line change to the root StackPanel in every page listed below. In each file the target is the FIRST `<StackPanel Margin="24"` immediately inside `<ui:SmoothScrollViewer`. Do NOT change inner StackPanels (e.g., `Margin="16"` inside tab content, `Margin="0,0,0,12"` inside cards — leave those alone).

Pages to update (all have the root at line 13 unless noted):
- `GallerySelectionPage.xaml:13`
- `GalleryInputsPage.xaml:13`
- `GalleryDataPage.xaml:13`
- `GalleryTabsPage.xaml:13`
- `GalleryNavigationPage.xaml:13`
- `GalleryWindowPage.xaml:13`
- `GalleryStatusPage.xaml:13`
- `GalleryColorsPage.xaml:13` *(Task 3 will also edit this file — do both in the same edit)*
- `GalleryGlyphsPage.xaml:13`
- `GalleryAccessibilityPage.xaml:13`
- `GalleryMenusPage.xaml:13`
- `GalleryFormsPage.xaml:13`
- `GalleryDataBindingPage.xaml:13`
- `GalleryTypographyPage.xaml:12` *(also add `HorizontalAlignment="Stretch"` here)*
- `GalleryLayoutPage.xaml:13`
- `GalleryTreesPage.xaml:13`

For `GalleryTypographyPage.xaml` the target line is:
```xml
<!-- BEFORE -->
<StackPanel Margin="24">

<!-- AFTER -->
<StackPanel Margin="28,24,28,28" HorizontalAlignment="Stretch">
```

- [ ] **Step 3: Verify no stray root `Margin="24"` remain on content pages**

```powershell
Select-String -Path "F:/StagedMigration/Fluence.Wpf/Fluence.Wpf.Demo/Pages/Gallery*.xaml" -Pattern '<StackPanel Margin="24"'
```
Expected: 0 matches. (Inner containers use different margin values and won't match this exact pattern.)

- [ ] **Step 4: Build**

```powershell
dotnet build "F:/StagedMigration/Fluence.Wpf/Fluence.Wpf.sln" -c Debug
```
Expected: 0 errors, 0 warnings.

- [ ] **Step 5: Commit**

```
git add Fluence.Wpf.Demo/Pages/
git commit -m "fix(demo): standardize page container margin to 28,24,28,28 across all gallery pages"
```

---

## Task 3: Fix GalleryColorsPage — title header and swatch dark-mode contrast

**Files:**
- Modify: `Fluence.Wpf.Demo/Pages/GalleryColorsPage.xaml`

**Two distinct issues:**

**Issue A — Missing page title.** Lines 14–19 use a `<Grid>` + `SectionHeaderStyle` (20px SemiBold) as the page heading instead of the standard `Title` typography (28px SemiBold) used by every other content page. This makes the Colors page look visually smaller/thinner than the rest.

**Issue B — Dark-mode swatch text invisible.** Inside the `Text` tab (and parts of other tabs), swatch cells set `Background` directly on child `TextBlock` elements. In dark mode `TextFillColorPrimary` resolves to near-white (`#E4FFFFFF`), and the labels use `Foreground="{DynamicResource TextOnAccentFillColorPrimaryBrush}"` (also near-white), making text invisible against the white TextBlock background. Fix: remove all `Background` attributes from `TextBlock` elements inside swatch cells and change `Foreground` to `TextFillColorInverseBrush`, which resolves to near-white in light theme (readable on dark text-fill backgrounds) and near-black in dark theme (readable on light text-fill backgrounds).

- [ ] **Step 1: Replace the page header block (lines 12–20)**

Current state of lines 12–20 in `GalleryColorsPage.xaml`:
```xml
    <ui:SmoothScrollViewer HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="24" HorizontalAlignment="Stretch">
            <Grid Margin="0,0,0,16">
                <TextBlock
                    Style="{StaticResource SectionHeaderStyle}"
                    Text="Color" />
            </Grid>

            <TabControl
```

Replace with (margin update from Task 2 is included here):
```xml
    <ui:SmoothScrollViewer HorizontalScrollBarVisibility="Disabled" VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="28,24,28,28" HorizontalAlignment="Stretch">
            <TextBlock
                Margin="0,0,0,4"
                ui:TextBlockExtensions.Typography="Title"
                Foreground="{DynamicResource TextFillColorPrimaryBrush}"
                Text="Color" />
            <TextBlock
                Margin="0,0,0,24"
                Style="{StaticResource SectionDescriptionStyle}"
                Text="WinUI 3 semantic brush tokens organized by purpose. Each swatch shows the brush key and its intended use state." />

            <TabControl
```

- [ ] **Step 2: Find all TextBlock Background attributes inside swatch cells**

```powershell
Select-String -Path "F:/StagedMigration/Fluence.Wpf/Fluence.Wpf.Demo/Pages/GalleryColorsPage.xaml" -Pattern "Background=" | Where-Object { $_.Line -match "TextBlock" }
```

Review the output. Every matching line will look like:
```xml
                            <TextBlock
                                Background="{DynamicResource SomeBrush}"
                                Foreground="{DynamicResource TextOnAccentFillColorPrimaryBrush}"
                                Style="{StaticResource SwatchNameStyle}"
                                Text="..." />
```

There are approximately 30–40 such TextBlocks across the Text, Fill, Stroke, Background, and Signal tabs.

- [ ] **Step 3: Remove TextBlock Background attributes and fix Foreground**

For every TextBlock inside a `<Border Style="{StaticResource ColorSwatchCellStyle}">`, apply this transformation:

```xml
<!-- BEFORE (example from Text tab, line ~49) -->
<TextBlock
    Background="{DynamicResource TextFillColorPrimaryBrush}"
    Foreground="{DynamicResource TextOnAccentFillColorPrimaryBrush}"
    Style="{StaticResource SwatchNameStyle}"
    Text="Text / Primary" />
<TextBlock
    Background="{DynamicResource TextFillColorSecondaryBrush}"
    Foreground="{DynamicResource TextOnAccentFillColorPrimaryBrush}"
    Style="{StaticResource SwatchUsageStyle}"
    Text="Rest or hover" />
<TextBlock
    Background="{DynamicResource TextFillColorTertiaryBrush}"
    Foreground="{DynamicResource TextOnAccentFillColorPrimaryBrush}"
    Style="{StaticResource SwatchKeyStyle}"
    Text="TextFillColorPrimaryBrush" />

<!-- AFTER -->
<TextBlock
    Foreground="{DynamicResource TextFillColorInverseBrush}"
    Style="{StaticResource SwatchNameStyle}"
    Text="Text / Primary" />
<TextBlock
    Foreground="{DynamicResource TextFillColorInverseBrush}"
    Style="{StaticResource SwatchUsageStyle}"
    Text="Rest or hover" />
<TextBlock
    Foreground="{DynamicResource TextFillColorInverseBrush}"
    Style="{StaticResource SwatchKeyStyle}"
    Text="TextFillColorPrimaryBrush" />
```

**Exception:** The High Contrast tab (last tab, around line 1189) uses hardcoded `Foreground="White"` on labels for HC system colors. Those are intentional — leave them as-is. Only change `Foreground="{DynamicResource TextOnAccentFillColorPrimaryBrush}"` + `Background="{DynamicResource ...}"` pairs.

- [ ] **Step 4: Verify no stray TextBlock Background attributes remain in swatch cells**

```powershell
Select-String -Path "F:/StagedMigration/Fluence.Wpf/Fluence.Wpf.Demo/Pages/GalleryColorsPage.xaml" -Pattern "Background=" | Where-Object { $_.Line -match "TextBlock" }
```
Expected: 0 matches.

- [ ] **Step 5: Build**

```powershell
dotnet build "F:/StagedMigration/Fluence.Wpf/Fluence.Wpf.sln" -c Debug
```
Expected: 0 errors, 0 warnings.

- [ ] **Step 6: Commit**

```
git add Fluence.Wpf.Demo/Pages/GalleryColorsPage.xaml
git commit -m "fix(demo): add Title to Colors page; fix swatch text contrast for dark mode"
```

---

## Task 4: Fix DemoSampleControl Expander border brush

**Files:**
- Modify: `Fluence.Wpf.Demo/Pages/DemoSampleControl.xaml:49`

The `fluence:Expander` at line 49 has `BorderThickness="1,0,1,1"` but no `BorderBrush`. It inherits the Expander control's default border which may differ from `CardStrokeColorDefaultBrush` used by the Card above it. In dark mode this creates a visible color seam between the Card and Expander.

- [ ] **Step 1: Add BorderBrush to the Expander**

`DemoSampleControl.xaml` lines 49–62, current state:
```xml
        <fluence:Expander
            x:Name="SourceExpander"
            Grid.Row="1"
            Margin="0"
            Background="{DynamicResource CardBackgroundFillColorSecondaryBrush}"
            BorderThickness="1,0,1,1"
            CornerRadius="0,0,8,8"
            Expanded="SourceExpander_Expanded"
            Header="Source code">
            <fluence:TabView
                x:Name="SourceTabs"
                Margin="16,8,16,16"
                IsAddTabButtonVisible="False" />
        </fluence:Expander>
```

Change to:
```xml
        <fluence:Expander
            x:Name="SourceExpander"
            Grid.Row="1"
            Margin="0"
            Background="{DynamicResource CardBackgroundFillColorSecondaryBrush}"
            BorderBrush="{DynamicResource CardStrokeColorDefaultBrush}"
            BorderThickness="1,0,1,1"
            CornerRadius="0,0,8,8"
            Expanded="SourceExpander_Expanded"
            Header="Source code">
            <fluence:TabView
                x:Name="SourceTabs"
                Margin="16,8,16,16"
                IsAddTabButtonVisible="False" />
        </fluence:Expander>
```

- [ ] **Step 2: Build**

```powershell
dotnet build "F:/StagedMigration/Fluence.Wpf/Fluence.Wpf.sln" -c Debug
```
Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Commit**

```
git add Fluence.Wpf.Demo/Pages/DemoSampleControl.xaml
git commit -m "fix(demo): set explicit CardStrokeColorDefaultBrush on DemoSampleControl Expander border"
```

---

## Task 5: Full build, tests, and visual spot-check

- [ ] **Step 1: Full solution build (both TFMs)**

```powershell
dotnet build "F:/StagedMigration/Fluence.Wpf/Fluence.Wpf.sln" -c Debug
```
Expected: 0 errors, 0 warnings on net472 and net10.0-windows.

- [ ] **Step 2: Run full test suite**

```powershell
dotnet test "F:/StagedMigration/Fluence.Wpf/Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj" -c Debug
```
Expected: all tests pass; no new failures relative to the HEAD baseline in KNOWN_ISSUES.md.

- [ ] **Step 3: Run demo and perform visual spot-check**

```powershell
dotnet run --project "F:/StagedMigration/Fluence.Wpf/Fluence.Wpf.Demo"
```

Work through this checklist in Light theme first, then Dark theme:

| Check | Pass? |
|-------|-------|
| Home page: scrolling to bottom shows 28px breathing room before window edge | |
| Buttons page: left/right gutter is 28px (visually matches Home page gutter) | |
| Buttons page: 24px gap visible between NavigationView content area top and "Buttons" title | |
| Colors page: "Color" title appears in large 28px SemiBold at top of page | |
| Colors page (Light): swatch labels readable on all colored backgrounds | |
| Colors page (Dark): swatch labels readable — no white-on-white invisible text | |
| Switch to Dark theme (Window page): Colors page swatch labels still readable | |
| DemoSampleControl (if any page shows it): Expander border matches the Card border above it | |
| Toggle pane mode Top → Left → LeftCompact: page content left/right gutter remains 28px in all modes | |
| Accent color change: Colors page swatches update correctly | |

- [ ] **Step 4: Commit any visual-verification corrections**

If the spot-check reveals small issues (e.g., one page still at Margin="24", one swatch label still invisible), fix them, then:

```
git add Fluence.Wpf.Demo/Pages/
git commit -m "fix(demo): visual spot-check corrections after demo run"
```

---

## Appendix: Spacing system reference

For reference when reading existing pages or adding new sections:

| Element | Style | Effective spacing |
|---------|-------|-------------------|
| Page container (non-home) | `Margin="28,24,28,28"` | 28px L/R gutter, 24px top, 28px bottom |
| Page title TextBlock | `Margin="0,0,0,4"` + `Typography="Title"` | 4px below title |
| Page description TextBlock | `Margin="0,0,0,24"` + `SectionDescriptionStyle` | 24px below desc |
| First section header (after desc) | `Margin="0,0,0,4"` + `SectionHeaderStyle` | Overrides style top margin to 0 for tighter gap after desc |
| Subsequent section headers | `SectionHeaderStyle` (no override) | 36px top / 4px bottom |
| Card | `Margin="0,0,0,16"` + `Padding="16"` | 16px gap between cards |
| Last card on page | No bottom margin needed — container bottom handles it |
