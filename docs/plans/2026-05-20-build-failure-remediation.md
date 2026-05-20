# Build Failure Remediation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Clear the 15 reproduced `net472` Fluence.Wpf test failures and keep the multi-TFM build/test gates green.

**Architecture:** The failures are layout and resource regressions in existing WPF templates, not new feature work. Fix the canonical templates and shared demo resources directly so the existing tests pass without weakening assertions.

**Tech Stack:** WPF XAML resource dictionaries, Fluence.Wpf controls, MSTest, `net472`, `net10.0-windows10.0.26100.0`.

---

### Task 1: NavigationView LeftCompact Content Metrics

**Files:**
- Modify: `Fluence.Wpf/Themes/Controls/NavigationView.xaml`
- Test: `Fluence.Wpf.Tests/ControlTests.NavigationView.cs`
- Test: `Fluence.Wpf.Tests/ControlTests.FluentStroke.cs`

**Steps:**
1. Make the LeftCompact content background match the Left template split-border pattern.
2. Remove the layout-affecting `BorderThickness` from the content background border.
3. Set the overlay stroke to `1,1,0,0`.
4. Run the five LeftCompact content-offset tests plus the LeftCompact stroke test.

### Task 2: NavigationView Top Strip Metrics

**Files:**
- Modify: `Fluence.Wpf/Themes/Controls/NavigationView.xaml`
- Modify if needed: `Fluence.Wpf/Controls/NavigationView.cs`
- Test: `Fluence.Wpf.Tests/ControlTests.NavigationViewTopParity.cs`

**Steps:**
1. Reconcile the top-item inter-item gap with the asserted 24px text-to-next-icon spacing.
2. Reconcile overflow measurement so a 220px top strip keeps the first two items visible and moves only the trailing item.
3. Run the two failing top-mode tests.

### Task 3: Control Resource Metrics

**Files:**
- Modify: `Fluence.Wpf/Themes/Controls/ProgressBar.xaml`
- Modify: `Fluence.Wpf/Themes/Controls/InfoBadge.xaml`
- Modify: `Fluence.Wpf/Themes/Typography/Typography.xaml`
- Test: `Fluence.Wpf.Tests/ControlTests.ProgressBar.cs`
- Test: `Fluence.Wpf.Tests/ControlTests.BackgroundParity.cs`
- Test: `Fluence.Wpf.Tests/ControlTests.InfoBadge.cs`
- Test: `Fluence.Wpf.Tests/TextRenderingPolicyTests.cs`

**Steps:**
1. Set ProgressBar default `TrackHeight` to 3.
2. Bind the ProgressBar track background to `ControlStrongStrokeColorDefaultBrush`.
3. Make InfoBadge pill padding symmetric while preserving the existing width and height.
4. Restore the documented Fluent typography line heights and `BlockLineHeight` stacking strategy.
5. Run the six failing resource/metric tests.

### Task 4: Demo Chrome and Icon Catalog Resources

**Files:**
- Modify: `Fluence.Wpf/Themes/Controls/TitleBar.xaml`
- Modify: `Fluence.Wpf.Demo/Pages/GalleryIconsPage.xaml`
- Test: `Fluence.Wpf.Tests/DemoMainWindowTests.cs`

**Steps:**
1. Align the title-bar back glyph rail with the NavigationView item glyph rail.
2. Set `IconCatalogCard` to the shared card background expected by demo tests.
3. Run the two failing demo tests.

### Task 5: Final Verification

**Files:**
- Verify all touched `.xaml` files remain UTF-8 with BOM.
- Verify whitespace with `git diff --check`.

**Commands:**
- `dotnet build .\Fluence.Wpf.sln -c Debug -m:1 -p:NuGetAudit=false /nr:false`
- `dotnet test .\Fluence.Wpf.Tests\Fluence.Wpf.Tests.csproj -c Debug -f net472 --no-build -p:NuGetAudit=false /nr:false`
- `dotnet test .\Fluence.Wpf.Tests\Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build -p:NuGetAudit=false /nr:false`
- `dotnet build F:\StagedMigration\PSAppDeployToolkit\PSADT.slnx -c Debug -m:1 -p:NuGetAudit=false /nr:false`
- `git diff --check`
