# Fluence.Wpf <-> PSADT Narration Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the three concrete screen-reader (Narrator/NVDA) gaps that the Fluence.Wpf <-> PSADT accessibility audit found on or near the PSADT dialog surface, without regressing the already-working narration path.

**Architecture:** Two fixes land in the **Fluence.Wpf library** (canonical, per the repo rule "fix control gaps in the library, not consumer workarounds") and are then **mirrored** into PSADT's git-tracked copy at `psadt4\lib\Fluence.Wpf\Fluence.Wpf`; one fix is PSADT-dialog-specific. The audit already confirmed the PSADT progress flow narrates correctly (dialog-open notification + polite live regions on status/detail text + RangeValue progress), so this plan is targeted polish, not a rebuild.

**Tech Stack:** WPF (multi-target `net472` + `net10.0-windows10.0.26100.0`), UI Automation peers, MSTest 4.2.2 (Fluence tests), xUnit (PSADT tests), XAML Styler.

## Global Constraints

- **NO `git push` anywhere, under any circumstances** (Fluence repo and `psadt4` repo). All commits are **local only**. The user will handle any pushing.
- Both target frameworks must build and test clean: `net472` **and** `net10.0-windows10.0.26100.0`. `TreatWarningsAsErrors=True`, `WarningLevel=9999`, `AnalysisLevel=latest-all`, `EnforceCodeStyleInBuild=true` -- every analyzer/style diagnostic is a build error.
- `Nullable=enable` -- code must be nullable-clean.
- Every new `.cs` file starts with the verbatim 27-line BSD 3-Clause header (copy from `Fluence.Wpf\Controls\FontIcon.cs` lines 1-27).
- `public` API requires `///` XML doc comments (CS1591/CS1574 are errors).
- File encoding: UTF-8 **with BOM**; match each file's existing line endings (committed `.cs` are CRLF in this repo -- do not normalize EOL).
- Accessible names are literal strings in XAML matching in-tree precedent: back glyph `&#xE72B;` -> `"Back"`, pane-toggle glyph `&#xE700;` -> `"Navigation"` (precedent: `Fluence.Wpf\Themes\Controls\TitleBar.xaml:144,158`; intent: WinUI `NavigationView.xaml` `TogglePaneButton` `AutomationProperties.LandmarkType="Navigation"`).
- Authored XAML is auto-formatted by the post-tool hook (XAML Styler against `Settings.XamlStyler`); do not hand-fight attribute ordering -- run the formatter and commit its output. Generated `Properties/DesignTime.*.xaml` is excluded.
- **No Co-Authored-By / Claude-Session trailers** in commit messages (repo convention).
- Banned: `string.IsNullOrEmpty()` (use `string.IsNullOrWhiteSpace()`), `TextOptions.*`, inline `#pragma warning disable`, hard-coded hex in `Themes/Controls/**`, em/en dashes in `.cs`/`.md`.
- Test baseline is a floor: add tests, never weaken the count.
- The Fluence `Fluence.Wpf.csproj` is SDK-style (`Microsoft.NET.Sdk.WindowsDesktop`) and globs `**/*.cs` by default, so a new `.cs` file needs no `<Compile>` entry -- this also holds for the mirrored copy.

---

## Preconditions (do before Task 1)

- [ ] **P1: Isolate the unrelated working-tree change.** The Fluence working tree currently has an **uncommitted version bump to `0.8.6-preview`** (6 files: `Directory.Build.props`, `Fluence.Wpf\Fluence.Wpf.csproj`, `CHANGELOG.md`, `README.md`, `SECURITY.md`, `.github\ISSUE_TEMPLATE\bug_report.yml`). Confirm with `git -C F:/FRebuild/Fluence.Wpf status -sb`. Do **not** mix it into this work. Either commit it on its own local branch first, or `git stash` it. Decide with the user if unsure.

- [ ] **P2: Create local feature branches (no push).**

```bash
git -C F:/FRebuild/Fluence.Wpf checkout -b a11y/navigation-fonticon-narration
git -C F:/FRebuild/psadt4 checkout -b a11y/fluence-narration-mirror
```

Expected: both repos report "Switched to a new branch ...". Confirm neither command pushes.

---

## Task 1: Name the NavigationView pane-toggle and back buttons (Fluence library)

**Why:** These four icon-only `Button`s (glyphs `&#xE700;` toggle, `&#xE72B;` back) carry no `AutomationProperties.Name`, so Narrator/NVDA announce them as a bare "Button" with no purpose. Highest-impact concrete library defect from the audit.

**Files:**
- Modify: `Fluence.Wpf\Themes\Controls\NavigationView.xaml` (two identical template blocks: back button at ~line 343 and ~593; pane-toggle at ~line 362 and ~612)
- Test: `Fluence.Wpf.Tests\ControlTests.Accessibility.cs` (add one `[TestMethod]` to the existing `partial class ControlTests`)

**Interfaces:**
- Consumes: nothing from other tasks.
- Produces: nothing other tasks depend on (Task 4 mirrors this file verbatim).

- [ ] **Step 1: Write the failing test.** Add this method inside the existing `partial class ControlTests` in `ControlTests.Accessibility.cs`, mirroring the existing `InfoBarAndPipsPager_GlyphButtons_HaveAutomationNames` pattern (same file):

```csharp
[TestMethod]
public void NavigationView_PaneToggleAndBackButtons_HaveAutomationNames()
{
    RunOnStaThread(static () =>
    {
        Application? application = EnsureApplication();
        ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

        try
        {
            // Default pane mode (Left) instantiates the template block that hosts both buttons.
            Controls.NavigationView nav = new() { Width = 320, Height = 240 };
            Window navWindow = new() { Content = nav, Width = 400, Height = 300 };

            try
            {
                navWindow.Show();
                _ = nav.ApplyTemplate();
                DrainDispatcher(navWindow.Dispatcher);

                ControlTemplate? navTemplate = nav.Template;
                Assert.IsNotNull(navTemplate, "NavigationView must receive its themed template.");

                foreach ((string part, string expectedName) in new[]
                {
                    ("PART_BackButton", "Back"),
                    ("PART_PaneToggleButton", "Navigation"),
                })
                {
                    FrameworkElement? btn = navTemplate.FindName(part, nav) as FrameworkElement;
                    Assert.IsNotNull(btn, $"{part} should exist in the NavigationView template.");
                    string actualName = AutomationProperties.GetName(btn);
                    Assert.IsTrue(
                        string.Equals(expectedName, actualName, System.StringComparison.Ordinal),
                        $"{part} must expose accessible name '{expectedName}' for Narrator. Actual: '{actualName}'.");
                }
            }
            finally
            {
                navWindow.Close();
            }
        }
        finally
        {
            _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
        }
    });
}
```

- [ ] **Step 2: Run the test to verify it fails.**

```bash
dotnet test F:/FRebuild/Fluence.Wpf/Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --filter "FullyQualifiedName~NavigationView_PaneToggleAndBackButtons_HaveAutomationNames"
```

Expected: FAIL -- `actualName` is empty string, so the `AssertIsTrue` for `"Back"` (or `"Navigation"`) fails. (If instead `PART_BackButton` is not found, the default pane mode used a different template block; set `nav.PaneDisplayMode = Fluence.Wpf.NavigationViewPaneDisplayMode.Left;` before `Show()` and re-run.)

- [ ] **Step 3: Add the accessible names in XAML.** In `Fluence.Wpf\Themes\Controls\NavigationView.xaml`, add one attribute to each of the four buttons. For **both** `<Button x:Name="PART_BackButton" ...>` occurrences, add `AutomationProperties.Name="Back"`. For **both** `<Button x:Name="PART_PaneToggleButton" ...>` occurrences, add `AutomationProperties.Name="Navigation"`. The back button becomes (apply identically to the second occurrence):

```xml
<Button
    x:Name="PART_BackButton"
    Width="48"
    Height="40"
    Margin="0"
    Padding="0"
    HorizontalAlignment="Center"
    VerticalAlignment="Center"
    HorizontalContentAlignment="Center"
    VerticalContentAlignment="Center"
    AutomationProperties.Name="Back"
    Background="Transparent"
    BorderThickness="0"
    Focusable="True"
    Template="{StaticResource NavigationViewBackButtonTemplate}"
    Visibility="Collapsed">
    <controls:FontIcon
        Foreground="{DynamicResource TextFillColorPrimaryBrush}"
        Glyph="&#xE72B;"
        IconFontSize="16" />
</Button>
```

And the pane-toggle button becomes (apply identically to the second occurrence):

```xml
<Button
    x:Name="PART_PaneToggleButton"
    Width="48"
    Height="40"
    Margin="0"
    Padding="0"
    HorizontalAlignment="Center"
    VerticalAlignment="Center"
    HorizontalContentAlignment="Center"
    VerticalContentAlignment="Center"
    AutomationProperties.Name="Navigation"
    Background="Transparent"
    BorderThickness="0"
    Focusable="True"
    Template="{StaticResource NavigationViewBackButtonTemplate}">
    <controls:FontIcon
        x:Name="PaneToggleGlyph"
        Margin="2,0,0,0"
        Foreground="{DynamicResource TextFillColorPrimaryBrush}"
        Glyph="&#xE700;"
        IconFontSize="16" />
</Button>
```

(Attribute ordering shown is XAML-Styler-sorted; if you insert it elsewhere the post-tool hook will re-sort it. That is fine.)

- [ ] **Step 4: Format the XAML.**

```bash
pwsh F:/FRebuild/Fluence.Wpf/.claude/hooks/Format-Xaml.ps1 -Path F:/FRebuild/Fluence.Wpf/Fluence.Wpf/Themes/Controls/NavigationView.xaml
```

Expected: exits 0; file conforms (LF + single UTF-8 BOM per the reference style).

- [ ] **Step 5: Run the test to verify it passes (both TFMs).**

```bash
dotnet build F:/FRebuild/Fluence.Wpf/Fluence.Wpf.sln -c Debug
dotnet test F:/FRebuild/Fluence.Wpf/Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --no-build --filter "FullyQualifiedName~NavigationView_PaneToggleAndBackButtons_HaveAutomationNames"
dotnet test F:/FRebuild/Fluence.Wpf/Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build --filter "FullyQualifiedName~NavigationView_PaneToggleAndBackButtons_HaveAutomationNames"
```

Expected: build clean (0 warnings/errors); the new test PASSES on both TFMs.

- [ ] **Step 6: Add a CHANGELOG entry.** In `F:/FRebuild/Fluence.Wpf/CHANGELOG.md`, under the `## [Unreleased]` section (add an `### Fixed` subsection if absent):

```markdown
- NavigationView pane-toggle (hamburger) and back buttons now expose accessible
  names ("Navigation" and "Back") so Windows Narrator and NVDA announce their
  purpose instead of a bare "Button".
```

- [ ] **Step 7: Commit locally (NO push).**

```bash
git -C F:/FRebuild/Fluence.Wpf add Fluence.Wpf/Themes/Controls/NavigationView.xaml Fluence.Wpf.Tests/ControlTests.Accessibility.cs CHANGELOG.md
git -C F:/FRebuild/Fluence.Wpf commit -m "Add accessible names to NavigationView pane-toggle and back buttons"
```

Expected: one local commit; do not run `git push`.

---

## Task 2: Exclude decorative FontIcon glyphs from the UI Automation tree (Fluence library)

**Why:** `FontIcon : Control` (`Fluence.Wpf\Controls\FontIcon.cs:42`) has no automation peer override, so every decorative glyph appears in the screen-reader control tree as an unnamed generic element (e.g., the four `ui:FontIcon` glyphs in PSADT's `FluentDialog.xaml`). WinUI's `FontIcon` is `AccessibilityView=Raw`; the WPF equivalent is an automation peer that returns `IsControlElementCore() == false` (and `IsContentElementCore() == false`), removing it from the control/content views Narrator and NVDA navigate. Glyph buttons keep their names because the **parent** `Button` carries `AutomationProperties.Name` (Task 1 closes the last gap).

**Files:**
- Create: `Fluence.Wpf\Automation\FontIconAutomationPeer.cs`
- Modify: `Fluence.Wpf\Controls\FontIcon.cs` (add `OnCreateAutomationPeer` override + `using`)
- Test: `Fluence.Wpf.Tests\ControlTests.Accessibility.cs` (add one `[TestMethod]`)

**Interfaces:**
- Consumes: `Fluence.Wpf.Controls.FontIcon` (existing).
- Produces: `Fluence.Wpf.Automation.FontIconAutomationPeer` (new public type, ctor `FontIconAutomationPeer(FontIcon owner)`). Task 4 mirrors both files verbatim.

- [ ] **Step 1: Write the failing test.** Add inside `partial class ControlTests` in `ControlTests.Accessibility.cs`:

```csharp
[TestMethod]
public void FontIcon_AutomationPeer_IsExcludedFromControlTree()
{
    RunOnStaThread(static () =>
    {
        Application? application = EnsureApplication();
        ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

        try
        {
            Controls.FontIcon icon = new() { Glyph = "" };
            System.Windows.Automation.Peers.AutomationPeer peer =
                System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(icon);

            Assert.IsNotNull(peer, "FontIcon must create an automation peer.");
            Assert.IsInstanceOfType(peer, typeof(Automation.FontIconAutomationPeer));
            Assert.IsFalse(
                peer.IsControlElement(),
                "Decorative FontIcon must be excluded from the UI Automation control view (AccessibilityView=Raw equivalent).");
            Assert.IsFalse(
                peer.IsContentElement(),
                "Decorative FontIcon must be excluded from the UI Automation content view.");
        }
        finally
        {
            _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
        }
    });
}
```

- [ ] **Step 2: Run the test to verify it fails.**

```bash
dotnet test F:/FRebuild/Fluence.Wpf/Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --filter "FullyQualifiedName~FontIcon_AutomationPeer_IsExcludedFromControlTree"
```

Expected: FAIL at compile (`Automation.FontIconAutomationPeer` does not exist) or, once it compiles, at `IsInstanceOfType` (the default `FrameworkElementAutomationPeer` is returned and `IsControlElement()` is `true`).

- [ ] **Step 3: Create the peer.** Write `Fluence.Wpf\Automation\FontIconAutomationPeer.cs` (BSD header copied verbatim from `Fluence.Wpf\Controls\FontIcon.cs:1-27`, then):

```csharp
using System.Windows.Automation.Peers;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Automation peer for <see cref="FontIcon"/> that excludes the purely decorative glyph from the
    /// UI Automation control and content views, matching WinUI's <c>AccessibilityView="Raw"</c> behavior.
    /// The glyph carries no meaning of its own; the labelled parent control (for example a
    /// <see cref="System.Windows.Controls.Button"/> with an <c>AutomationProperties.Name</c>) is what
    /// screen readers announce.
    /// </summary>
    public class FontIconAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FontIconAutomationPeer"/> class.
        /// </summary>
        /// <param name="owner">The <see cref="FontIcon"/> that owns this peer.</param>
        public FontIconAutomationPeer(FontIcon owner)
            : base(owner)
        {
        }

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(FontIcon);

        /// <inheritdoc />
        protected override bool IsControlElementCore() => false;

        /// <inheritdoc />
        protected override bool IsContentElementCore() => false;
    }
}
```

- [ ] **Step 4: Wire the peer into FontIcon.** In `Fluence.Wpf\Controls\FontIcon.cs`, add `using System.Windows.Automation.Peers;` to the using block (after `using System.Windows.Media.Animation;`), and add this override (place it just after `OnApplyTemplate` / before `OnPropertyChanged`):

```csharp
        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new Fluence.Wpf.Automation.FontIconAutomationPeer(this);
        }
```

- [ ] **Step 5: Build and run the test (both TFMs).**

```bash
dotnet build F:/FRebuild/Fluence.Wpf/Fluence.Wpf.sln -c Debug
dotnet test F:/FRebuild/Fluence.Wpf/Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --no-build --filter "FullyQualifiedName~FontIcon_AutomationPeer_IsExcludedFromControlTree"
dotnet test F:/FRebuild/Fluence.Wpf/Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build --filter "FullyQualifiedName~FontIcon_AutomationPeer_IsExcludedFromControlTree"
```

Expected: build clean; new test PASSES on both TFMs.

- [ ] **Step 6: Run the FULL test suite (regression gate).** Excluding a type from the control view can disturb a test that walked the tree expecting a FontIcon node, or a glyph button that implicitly relied on glyph-derived naming.

```bash
dotnet test F:/FRebuild/Fluence.Wpf/Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --no-build
dotnet test F:/FRebuild/Fluence.Wpf/Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build
```

Expected: `total - skipped = passed` on both TFMs, with no new failures vs. the HEAD-of-branch baseline. If a tree-walk test now misses a FontIcon, evaluate whether the test was asserting the decorative node (update the test) or a real regression (revert and reconsider).

- [ ] **Step 7: CHANGELOG entry.** Append under `## [Unreleased]` -> `### Fixed`:

```markdown
- Decorative FontIcon glyphs are now excluded from the UI Automation tree
  (AccessibilityView=Raw equivalent), so screen readers no longer stop on
  unnamed icon nodes; the labelled parent control is announced instead.
```

- [ ] **Step 8: Commit locally (NO push).**

```bash
git -C F:/FRebuild/Fluence.Wpf add Fluence.Wpf/Automation/FontIconAutomationPeer.cs Fluence.Wpf/Controls/FontIcon.cs Fluence.Wpf.Tests/ControlTests.Accessibility.cs CHANGELOG.md
git -C F:/FRebuild/Fluence.Wpf commit -m "Exclude decorative FontIcon glyphs from the UI Automation tree"
```

Expected: one local commit; no push.

---

## Task 3: Give the PSADT ProgressDialog an explicit initial focus element

**Why:** `ProgressDialog` (no action buttons) does not override `GetInitialFocusElement`, so the base returns `null` (`FluentDialog.xaml.cs:1395-1398`) and initial keyboard/AT focus relies on the WPF default. The base comment states "Screen readers begin reading from this element." Landing focus on the progress message gives a deterministic reading start that complements the existing open-notification.

**Files:**
- Modify: `psadt4\src\PSADT\PSADT.UserInterface.Interfaces\Fluent\ProgressDialog.cs`

**Interfaces:**
- Consumes: `FluentDialog.MessageTextBlock` (existing base member, already referenced at `ProgressDialog.cs:60`), `FluentDialog.GetInitialFocusElement()` (existing `private protected virtual`, `FluentDialog.xaml.cs:1395`).
- Produces: nothing other tasks depend on.

- [ ] **Step 1: Make the message text a focus target.** In `ProgressDialog.cs`, in the constructor, immediately after `ProgressStackPanel.Visibility = Visibility.Visible;` (line 27), add:

```csharp
            // The progress message is the natural place for a screen reader to begin reading when the
            // dialog opens. Make it focusable for assistive technology without inserting it into the tab
            // cycle (the progress dialog has no interactive controls to tab between).
            MessageTextBlock.Focusable = true;
            MessageTextBlock.IsTabStop = false;
```

- [ ] **Step 2: Override the initial-focus element.** Add this override to `ProgressDialog` (place it just after the `GetOpenAnnouncement` override, before the `_lastAnnouncedMessage` field):

```csharp
        /// <inheritdoc />
        private protected override FrameworkElement? GetInitialFocusElement()
        {
            return MessageTextBlock;
        }
```

(`System.Windows` is already imported at `ProgressDialog.cs:3`, so `FrameworkElement` resolves.)

- [ ] **Step 3: Build the PSADT UI project (both TFMs).** Per repo memory, the top-level `build.ps1` fails on an unrelated PSScriptAnalyzer step; verify by building the project directly.

```bash
dotnet build F:/FRebuild/psadt4/src/PSADT/PSADT.UserInterface.Interfaces/PSADT.UserInterface.Interfaces.csproj -c Debug
```

Expected: build clean (this project targets `net472`; if the UI build also produces the net10 lane, both succeed). Zero warnings/errors.

- [ ] **Step 4: Run the PSADT UI logic tests (regression gate).**

```bash
dotnet test F:/FRebuild/psadt4/src/PSADT/PSADT.UserInterface.Interfaces.Tests/PSADT.UserInterface.Interfaces.Tests.csproj -c Debug
```

Expected: all existing xUnit tests in `AccessibilityLogicTests` still pass (this change is runtime focus behavior; the logic tests must remain green).

- [ ] **Step 5: Manual AT verification (the repo's own convention for runtime focus).** The PSADT test harness is pure-logic xUnit and does not instantiate live WPF dialogs, and the repo's `GalleryAccessibilityPage` explicitly directs runtime AT checks via Narrator/Inspect.exe. Launch the PSADT progress dialog test harness (`PSADT.UserInterface.TestHarness` or `PSADT.UserInterface.Interfaces.TestHarness`), open the progress dialog, and with **Inspect.exe** (Windows SDK) confirm the focused element on open is the progress `MessageTextBlock` (its Name equals the progress message). With **Narrator** running, confirm it begins reading at the progress message. Record the result (pass/fail + what was observed) in the task report.

- [ ] **Step 6: Commit locally in psadt4 (NO push).**

```bash
git -C F:/FRebuild/psadt4 add src/PSADT/PSADT.UserInterface.Interfaces/Fluent/ProgressDialog.cs
git -C F:/FRebuild/psadt4 commit -m "Set explicit initial focus on the Fluent ProgressDialog message for screen readers"
```

Expected: one local commit in `psadt4`; no push.

---

## Task 4: Mirror the Fluence library fixes into PSADT and verify the integration

**Why:** PSADT compiles against the git-tracked copy at `psadt4\lib\Fluence.Wpf\Fluence.Wpf` (`PSADT.UserInterface.Interfaces.csproj:16` -> `..\..\..\lib\Fluence.Wpf\Fluence.Wpf\Fluence.Wpf.csproj`), **not** the canonical repo. Tasks 1 and 2 only take effect for PSADT once mirrored. This task makes the two libraries byte-identical for the changed files and proves PSADT still builds and that its dialogs pick up the fixes.

**Files (copy canonical -> mirror, overwriting):**
- `Fluence.Wpf\Themes\Controls\NavigationView.xaml`
- `Fluence.Wpf\Controls\FontIcon.cs`
- `Fluence.Wpf\Automation\FontIconAutomationPeer.cs` (new)

**Interfaces:**
- Consumes: the committed outputs of Task 1 and Task 2.
- Produces: an updated mirror that PSADT references.

- [ ] **Step 1: Copy the three changed files into the mirror.** Source root `F:/FRebuild/Fluence.Wpf/Fluence.Wpf`, destination root `F:/FRebuild/psadt4/lib/Fluence.Wpf/Fluence.Wpf` (preserve bytes so encoding/BOM/EOL match the canonical files):

```bash
cp -f "F:/FRebuild/Fluence.Wpf/Fluence.Wpf/Themes/Controls/NavigationView.xaml" "F:/FRebuild/psadt4/lib/Fluence.Wpf/Fluence.Wpf/Themes/Controls/NavigationView.xaml"
cp -f "F:/FRebuild/Fluence.Wpf/Fluence.Wpf/Controls/FontIcon.cs"               "F:/FRebuild/psadt4/lib/Fluence.Wpf/Fluence.Wpf/Controls/FontIcon.cs"
cp -f "F:/FRebuild/Fluence.Wpf/Fluence.Wpf/Automation/FontIconAutomationPeer.cs" "F:/FRebuild/psadt4/lib/Fluence.Wpf/Fluence.Wpf/Automation/FontIconAutomationPeer.cs"
```

- [ ] **Step 2: Verify the mirror matches the canonical files.**

```bash
diff "F:/FRebuild/Fluence.Wpf/Fluence.Wpf/Themes/Controls/NavigationView.xaml" "F:/FRebuild/psadt4/lib/Fluence.Wpf/Fluence.Wpf/Themes/Controls/NavigationView.xaml"
diff "F:/FRebuild/Fluence.Wpf/Fluence.Wpf/Controls/FontIcon.cs" "F:/FRebuild/psadt4/lib/Fluence.Wpf/Fluence.Wpf/Controls/FontIcon.cs"
diff "F:/FRebuild/Fluence.Wpf/Fluence.Wpf/Automation/FontIconAutomationPeer.cs" "F:/FRebuild/psadt4/lib/Fluence.Wpf/Fluence.Wpf/Automation/FontIconAutomationPeer.cs"
```

Expected: all three `diff`s produce **no output** (identical). The new `FontIconAutomationPeer.cs` is auto-included by the mirror's SDK-style csproj glob -- no csproj edit needed.

- [ ] **Step 3: Build PSADT against the updated mirror (the real integration check).**

```bash
dotnet build F:/FRebuild/psadt4/src/PSADT/PSADT.UserInterface.Interfaces/PSADT.UserInterface.Interfaces.csproj -c Debug
```

Expected: build clean. (This also recompiles the mirrored Fluence project; the new peer file compiles and the NavigationView template change loads.)

- [ ] **Step 4: Confirm the PSADT dialogs pick up the fixes (manual AT spot-check).** In the PSADT UI test harness with **Inspect.exe**: (a) the four decorative `ui:FontIcon` glyphs in the dialogs (`FluentDialog.xaml:381,418,456,491`) no longer appear as stop points in the control view; (b) if any dialog surface shows a NavigationView, its toggle/back buttons announce "Navigation"/"Back". Record observations in the task report.

- [ ] **Step 5: Commit the mirror locally in psadt4 (NO push).**

```bash
git -C F:/FRebuild/psadt4 add lib/Fluence.Wpf/Fluence.Wpf/Themes/Controls/NavigationView.xaml lib/Fluence.Wpf/Fluence.Wpf/Controls/FontIcon.cs lib/Fluence.Wpf/Fluence.Wpf/Automation/FontIconAutomationPeer.cs
git -C F:/FRebuild/psadt4 commit -m "Mirror Fluence.Wpf narration fixes (NavigationView names, FontIcon automation peer)"
```

Expected: one local commit in `psadt4`; no push.

---

## Final verification (whole plan)

- [ ] **V1: Fluence full suite, both TFMs, clean build.**

```bash
dotnet build F:/FRebuild/Fluence.Wpf/Fluence.Wpf.sln -c Debug
dotnet test F:/FRebuild/Fluence.Wpf/Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --no-build
dotnet test F:/FRebuild/Fluence.Wpf/Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build
```

Expected: zero warnings/errors; test count = baseline + 2 new tests; no failures.

- [ ] **V2: XAML format check (CI gate).**

```bash
pwsh F:/FRebuild/Fluence.Wpf/.claude/hooks/Format-Xaml.ps1 -Check
```

Expected: exits 0 (all authored XAML conformant).

- [ ] **V3: PSADT UI build + logic tests green** (Task 3 Step 3-4 and Task 4 Step 3 re-confirmed together).

- [ ] **V4: Confirm no pushes happened.** `git -C F:/FRebuild/Fluence.Wpf log --oneline origin/main..HEAD` and the same in `psadt4` should show the new local commits ahead of the remote, and `git -C <repo> status` should show the branch is ahead of its upstream / has no upstream -- never pushed.

- [ ] **V5: theme-slot-auditor sanity (optional).** None of these changes touch the theme pipeline or slot layout, so no `theme-slot-auditor` pass is required. The `winui-parity-reviewer` lane is satisfied by the in-tree precedent + WinUI references cited in Global Constraints.

---

## Out of scope (recorded findings, not in this plan)

Per the approved "targeted fixes" scope, these audit findings are **deliberately excluded** (library-general, not on the PSADT narration path, or design-ambiguous):

- **NumberBox validation announcement** -- `NumberBox` sets no `LiveSetting` and wraps a plain WPF `TextBox`, so validation is silent to screen readers. Real library gap, but PSADT's dialogs do not use `NumberBox`. Needs a `ValidationState` surface first (brainstorm before planning).
- **`IsRequiredForForm` / `LabeledBy`** not used anywhere in the library.
- **Behavioral live-region test** -- no test asserts `LiveRegionChanged` actually fires (documented WPF in-process UIA-bus limitation; would need an out-of-process/peer-shim harness).
- **ProgressBar percentage push** -- intentionally RangeValue-only (WinUI parity); not a defect.
- **InputDialog disabled-button reason** -- no spoken explanation when Continue is disabled (design choice; field is labelled via `LabeledBy`).
