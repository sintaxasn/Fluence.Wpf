# WPF Accessibility Completeness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring Fluence.Wpf to complete Windows Narrator / UI Automation parity so a blind user can perform every task a sighted user can: every control has a correct role, an accessible name, the right UIA patterns, full keyboard operability, and dynamic state changes are announced.

**Architecture:** Three fix layers. (1) Template-level `AutomationProperties.Name` on icon-only buttons. (2) New / corrected `AutomationPeer` classes in `Fluence.Wpf/Automation/` for controls deriving from `Control`/`ContentControl` that lack adequate peers, plus keyboard handlers in the control code. (3) net472-safe live regions (`AutomationProperties.LiveSetting` + `peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged)`) for status/validation/progress changes. Each fix ships with an in-process MSTest peer test using the existing `WpfTestSta` harness.

**Tech Stack:** WPF (`System.Windows.Automation.*`, PresentationCore/PresentationFramework/UIAutomationTypes), C# (LangVersion=latest, nullable enabled), MSTest 4.2.2 via `WpfTestSta`. Multi-TFM: net472 + net10.0-windows10.0.26100.0.

## Global Constraints

- **TFMs:** every change MUST build and pass tests on BOTH `net472` AND `net10.0-windows10.0.26100.0`.
- **net472 live regions:** use `AutomationProperties.LiveSetting` + `peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged)`. **NEVER call `AutomationPeer.RaiseNotificationEvent`** — it is .NET Framework 4.8+ and does not exist on net472 (verified against the net472 reference assembly). Do not add `#if NET10_0_OR_GREATER` branches to gain it; the live-region path is the single cross-TFM implementation.
- **net472 unavailable (do NOT use):** `AutomationProperties.IsDialog`, `AutomationProperties.HeadingLevel`, automatic `PositionInSet`/`SizeOfSet`. Where needed, set position/size explicitly or omit and log in `KNOWN_ISSUES.md`.
- **BSD header:** every new `.cs` file begins with the verbatim 27-line BSD 3-Clause header copied from an existing library file (e.g. `Fluence.Wpf/Automation/DropDownButtonAutomationPeer.cs`). Do not change the copyright year.
- **Nullable-clean**, `public` API needs `///` XML docs (build fails otherwise), explicit types over `var`, target-typed `new()`, discard ignored returns with `_`, `string.IsNullOrWhiteSpace` never `IsNullOrEmpty` (RS0030 banned).
- **XAML:** no hard-coded hex in `Themes/Controls/**`; theme-bound values use `DynamicResource`; run `pwsh eng/Format-Xaml.ps1 -Path <file>` after editing any authored XAML. No em/en dashes in `.cs`/`.md`.
- **Peers are public** types in namespace `Fluence.Wpf.Automation`; mirror the structure of existing peers (`DatePickerAutomationPeer`, `NumberBoxAutomationPeer`).
- **Names match existing visible affordances:** reuse the exact ToolTip strings already in templates (e.g. "Minimize", "Back", "Close") as the `AutomationProperties.Name` so visual and spoken labels agree.
- **Test baseline:** HEAD-of-branch test count is the floor; add tests, never weaken. Each task adds tests in the matching `ControlTests.<Area>.cs` partial (create if absent) using `RunOnSta`, `MergeGenericDictionary`, `DrainDispatcher`.
- **Commit cadence:** commit per task with the message shown. No `Co-Authored-By` trailer (project convention). Do not push or tag unless the user asks.
- **Reference authority:** Microsoft Learn UIA docs + Win32 control-type pattern mapping (cited inline). Follow in-tree precedent first (the existing peers and the `GalleryAccessibilityPage`).

---

## File Structure

**New files (peers):**
- `Fluence.Wpf/Automation/RatingControlAutomationPeer.cs` — RangeValue peer for RatingControl.
- `Fluence.Wpf/Automation/PasswordBoxAutomationPeer.cs` — Edit/password peer for PasswordBox.
- `Fluence.Wpf/Automation/PersonPictureAutomationPeer.cs` — named image/group peer for PersonPicture.

**Modified controls (code-behind: peers, keyboard, live regions):**
- `Fluence.Wpf/Controls/RatingControl.cs`, `PasswordBox.cs`, `PersonPicture.cs`, `NumberBox.cs`, `HyperlinkButton.cs`, `InfoBar.cs`, `ProgressBar.cs`, `ProgressRing.cs`, `TeachingTip.cs`, `TextBox.cs`, `ColorPicker.cs`, `Card.cs`, `CheckBox.cs`, `RadioButton.cs`, `ToggleSwitch.cs`, `AutoSuggestBox.cs`, `AppBarButton.cs`.

**Modified templates (AutomationProperties.Name / LiveSetting / LabeledBy):**
- `Fluence.Wpf/Themes/Controls/FluenceWindow.xaml`, `TitleBar.xaml`, `DatePicker.xaml`, `TimePicker.xaml`, `NumberBox.xaml`, `AutoSuggestBox.xaml`, `TabView.xaml`, `InfoBar.xaml`, `TeachingTip.xaml`, `PipsPager.xaml`, `CommandBarFlyout.xaml`, `ToggleSwitch.xaml`.

**Modified automation peer (bug fix):**
- `Fluence.Wpf/Automation/NumberBoxAutomationPeer.cs`.

**Tests:** new/extended `Fluence.Wpf.Tests/ControlTests.Accessibility.cs` plus per-area partials (`ControlTests.RatingControl.cs`, `ControlTests.PasswordBox.cs`, `ControlTests.PersonPicture.cs`, etc.); extend `ControlTests.InfoBar.cs`, `ControlTests.NumberBox.cs`, `ControlTests.TextBox.cs` (create if missing).

**Docs:** `CHANGELOG.md`, `docs/controls.md`, `docs/theming.md` (HC note already exists), `KNOWN_ISSUES.md`, `Fluence.Wpf.Demo/Pages/GalleryAccessibilityPage.xaml(.cs)`.

---

## Phase 1 — Critical (a blind user is currently blocked)

### Task 1: Accessible names for window chrome and title-bar glyph buttons

**Files:**
- Modify: `Fluence.Wpf/Themes/Controls/FluenceWindow.xaml` (PART_MinimizeButton ~205, PART_MaximizeButton ~218, PART_RestoreButton ~231, PART_CloseButton ~245)
- Modify: `Fluence.Wpf/Themes/Controls/TitleBar.xaml` (PART_BackButton ~139, PART_PaneToggleButton ~153)
- Test: `Fluence.Wpf.Tests/TitleBarTests.cs` (extend) and `Fluence.Wpf.Tests/ControlTests.Accessibility.cs` (create)

**Interfaces:**
- Produces: nothing consumed by later tasks. These are pure template attribute additions.

**Context:** Each button currently has only `ToolTip="..."`. WPF maps ToolTip to UIA HelpText, not Name, so Narrator announces a nameless "button". Add `AutomationProperties.Name` matching the ToolTip text. Reference: https://learn.microsoft.com/accessibility-tools-docs/items/wpf/button_name

- [ ] **Step 1: Write the failing test** in `Fluence.Wpf.Tests/ControlTests.Accessibility.cs` (new file; copy BSD header from an existing test file; class is `public partial class ControlTests`).

```csharp
[TestMethod]
public void FluenceWindow_CaptionButtons_HaveAutomationNames()
{
    RunOnSta(() =>
    {
        MergeGenericDictionary(Application.Current.Resources);
        Controls.FluenceWindow window = new();
        window.Show();
        window.ApplyTemplate();
        DrainDispatcher();

        foreach ((string part, string expectedName) in new[]
        {
            ("PART_MinimizeButton", "Minimize"),
            ("PART_CloseButton", "Close"),
        })
        {
            FrameworkElement? button = FindVisualChildByName<FrameworkElement>(window, part);
            Assert.IsNotNull(button, $"{part} should exist in the FluenceWindow template.");
            Assert.AreEqual(expectedName, AutomationProperties.GetName(button),
                $"{part} must expose an accessible name for Narrator.");
        }

        window.Close();
    });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --filter "FullyQualifiedName~FluenceWindow_CaptionButtons_HaveAutomationNames"`
Expected: FAIL (GetName returns empty string).

- [ ] **Step 3: Add `AutomationProperties.Name` to each glyph button** in the two templates. For each `<ui:Button .../>` or chrome button add the attribute (XAML Styler will reflow). Example for FluenceWindow.xaml minimize button:

```xml
<Button
    x:Name="PART_MinimizeButton"
    AutomationProperties.Name="Minimize"
    ToolTip="Minimize"
    ... existing attributes ... />
```

Apply: PART_MinimizeButton -> "Minimize"; PART_MaximizeButton -> "Maximize"; PART_RestoreButton -> "Restore"; PART_CloseButton -> "Close". In TitleBar.xaml: PART_BackButton -> "Back"; PART_PaneToggleButton -> "Navigation". Use the exact existing ToolTip text so spoken and visual labels match.

- [ ] **Step 4: Format the edited XAML**

Run: `pwsh eng/Format-Xaml.ps1 -Path Fluence.Wpf/Themes/Controls/FluenceWindow.xaml` and `pwsh eng/Format-Xaml.ps1 -Path Fluence.Wpf/Themes/Controls/TitleBar.xaml`

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --filter "FullyQualifiedName~FluenceWindow_CaptionButtons_HaveAutomationNames"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add Fluence.Wpf/Themes/Controls/FluenceWindow.xaml Fluence.Wpf/Themes/Controls/TitleBar.xaml Fluence.Wpf.Tests/ControlTests.Accessibility.cs
git commit -m "a11y: name window chrome and title-bar glyph buttons for Narrator"
```

---

### Task 2: RatingControl automation peer and keyboard operability

**Files:**
- Create: `Fluence.Wpf/Automation/RatingControlAutomationPeer.cs`
- Modify: `Fluence.Wpf/Controls/RatingControl.cs` (class decl ~49, add OnCreateAutomationPeer, OnKeyDown, make focusable)
- Test: `Fluence.Wpf.Tests/ControlTests.RatingControl.cs` (create)

**Interfaces:**
- Consumes: `RatingControl.Value` (double), `RatingControl.MaxRating` or equivalent count property — inspect `RatingControl.cs` for the exact property names (`Value`, and the max-stars property; the audit referenced `Caption` at line 134 and star generation at 219). Use the real property names found in the file.
- Produces: `Fluence.Wpf.Automation.RatingControlAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider`.

**Context:** RatingControl derives from `Control`, has no peer and NO keyboard interaction (stars are mouse-only). It must be a focusable RangeValue control. Reference: https://learn.microsoft.com/dotnet/desktop/wpf/controls/ui-automation-of-a-wpf-custom-control and RangeValue pattern.

- [ ] **Step 1: Write the failing test** in `ControlTests.RatingControl.cs` (BSD header; `public partial class ControlTests`).

```csharp
[TestMethod]
public void RatingControl_AutomationPeer_ExposesRangeValueAndIsKeyboardSettable()
{
    RunOnSta(() =>
    {
        MergeGenericDictionary(Application.Current.Resources);
        Controls.RatingControl rating = new() { Value = 2 };
        Window window = new() { Content = rating };
        window.Show();
        rating.ApplyTemplate();
        DrainDispatcher();

        AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(rating);
        Assert.IsInstanceOfType(peer, typeof(Automation.RatingControlAutomationPeer));
        Assert.AreEqual(AutomationControlType.Slider, peer.GetAutomationControlType());

        IRangeValueProvider range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue);
        Assert.AreEqual(2.0, range.Value, 0.001);

        // Keyboard: Right arrow raises the rating.
        Assert.IsTrue(rating.Focus(), "RatingControl must be focusable.");
        rating.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(rating), 0, Key.Right)
        { RoutedEvent = Keyboard.KeyDownEvent });
        DrainDispatcher();
        Assert.AreEqual(3.0, rating.Value, 0.001, "Right arrow should increase the rating by one.");

        window.Close();
    });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --filter "FullyQualifiedName~RatingControl_AutomationPeer"`
Expected: FAIL (no peer type; arrow key does nothing).

- [ ] **Step 3: Create the peer** `Fluence.Wpf/Automation/RatingControlAutomationPeer.cs`:

```csharp
// (BSD header)
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>UI Automation peer for <see cref="RatingControl"/>, exposing the RangeValue pattern.</summary>
    public class RatingControlAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
    {
        /// <summary>Initializes a new instance of the <see cref="RatingControlAutomationPeer"/> class.</summary>
        /// <param name="owner">The rating control that owns this peer.</param>
        public RatingControlAutomationPeer(RatingControl owner) : base(owner)
        {
        }

        private RatingControl OwnerRating => (RatingControl)Owner;

        /// <inheritdoc/>
        protected override string GetClassNameCore() => nameof(RatingControl);

        /// <inheritdoc/>
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

        /// <inheritdoc/>
        public override object? GetPattern(PatternInterface patternInterface)
            => patternInterface == PatternInterface.RangeValue ? this : base.GetPattern(patternInterface);

        /// <inheritdoc/>
        public double Value => OwnerRating.Value;

        /// <inheritdoc/>
        public double Minimum => 0d;

        /// <inheritdoc/>
        public double Maximum => OwnerRating.MaxRating; // replace with the real max-stars property name

        /// <inheritdoc/>
        public double SmallChange => 1d;

        /// <inheritdoc/>
        public double LargeChange => 1d;

        /// <inheritdoc/>
        public bool IsReadOnly => !IsEnabled();

        /// <inheritdoc/>
        public void SetValue(double value) => OwnerRating.Value = value;
    }
}
```

- [ ] **Step 4: Wire the peer and keyboard into `RatingControl.cs`.** In the static constructor add focusability defaults; add the peer factory and key handling. Use the real `Value`/max property names.

```csharp
static RatingControl()
{
    FocusableProperty.OverrideMetadata(typeof(RatingControl), new FrameworkPropertyMetadata(true));
    IsTabStopProperty.OverrideMetadata(typeof(RatingControl), new FrameworkPropertyMetadata(true));
    // existing DefaultStyleKey override stays
}

/// <inheritdoc/>
protected override AutomationPeer OnCreateAutomationPeer() => new Automation.RatingControlAutomationPeer(this);

/// <inheritdoc/>
protected override void OnKeyDown(KeyEventArgs e)
{
    base.OnKeyDown(e);
    if (e.Handled)
    {
        return;
    }

    double step = 1d;
    switch (e.Key)
    {
        case Key.Right:
        case Key.Up:
            Value = Math.Min(MaxRating, Value + step);
            e.Handled = true;
            break;
        case Key.Left:
        case Key.Down:
            Value = Math.Max(0d, Value - step);
            e.Handled = true;
            break;
        case Key.Home:
            Value = 0d;
            e.Handled = true;
            break;
        case Key.End:
            Value = MaxRating;
            e.Handled = true;
            break;
    }
}
```

When `Value` changes, raise the RangeValue property-changed event so Narrator speaks the new rating. In the existing `Value` change callback (find `OnValueChanged` or the property metadata callback) add:

```csharp
if (UIElementAutomationPeer.FromElement(this) is RatingControlAutomationPeer peer)
{
    peer.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --filter "FullyQualifiedName~RatingControl_AutomationPeer"`
Expected: PASS.

- [ ] **Step 6: Build both TFMs to confirm clean**

Run: `dotnet build Fluence.Wpf.sln -c Debug`
Expected: 0 warnings, 0 errors.

- [ ] **Step 7: Commit**

```bash
git add Fluence.Wpf/Automation/RatingControlAutomationPeer.cs Fluence.Wpf/Controls/RatingControl.cs Fluence.Wpf.Tests/ControlTests.RatingControl.cs
git commit -m "a11y: add RatingControl automation peer and keyboard operability"
```

---

### Task 3: PasswordBox automation peer and keyboard-accessible reveal

**Files:**
- Create: `Fluence.Wpf/Automation/PasswordBoxAutomationPeer.cs`
- Modify: `Fluence.Wpf/Controls/PasswordBox.cs` (class decl ~46; reveal handlers ~572-585)
- Modify: `Fluence.Wpf/Themes/Controls/PasswordBox.xaml` (reveal button: ensure it is a focusable Button with AutomationProperties.Name)
- Test: `Fluence.Wpf.Tests/ControlTests.PasswordBox.cs` (create)

**Interfaces:**
- Consumes: `PasswordBox` reveal state field/property (find the field backing `OnRevealButtonDown/Up`), the reveal button part name (inspect `PasswordBox.xaml`).
- Produces: `Fluence.Wpf.Automation.PasswordBoxAutomationPeer : FrameworkElementAutomationPeer` with `IsPasswordCore => true`.

**Context:** PasswordBox derives from `Control`, has no peer (so it reports as a bare element with no password semantics), and the reveal button is mouse press-and-hold only. Add a peer reporting `Edit` + `IsPassword`, and make reveal keyboard-operable as a toggle (Space/Enter) with an accessible name. Reference: https://learn.microsoft.com/accessibility-tools-docs/items/wpf/edit_name

- [ ] **Step 1: Write the failing test** in `ControlTests.PasswordBox.cs`:

```csharp
[TestMethod]
public void PasswordBox_AutomationPeer_ReportsPasswordEdit()
{
    RunOnSta(() =>
    {
        MergeGenericDictionary(Application.Current.Resources);
        Controls.PasswordBox box = new();
        Window window = new() { Content = box };
        window.Show();
        box.ApplyTemplate();
        DrainDispatcher();

        AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(box);
        Assert.IsInstanceOfType(peer, typeof(Automation.PasswordBoxAutomationPeer));
        Assert.AreEqual(AutomationControlType.Edit, peer.GetAutomationControlType());
        Assert.IsTrue(peer.IsPassword(), "PasswordBox peer must report IsPassword for Narrator to suppress reading the value.");

        window.Close();
    });
}

[TestMethod]
public void PasswordBox_RevealButton_IsKeyboardOperableAndNamed()
{
    RunOnSta(() =>
    {
        MergeGenericDictionary(Application.Current.Resources);
        Controls.PasswordBox box = new();
        Window window = new() { Content = box };
        window.Show();
        box.ApplyTemplate();
        DrainDispatcher();

        FrameworkElement? reveal = FindVisualChildByName<FrameworkElement>(box, "PART_RevealButton"); // confirm part name
        Assert.IsNotNull(reveal, "Reveal button part must exist.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(AutomationProperties.GetName(reveal)),
            "Reveal button must have an accessible name.");
        // Activating via Invoke (keyboard Space/Enter path) toggles reveal.
        AutomationPeer revealPeer = UIElementAutomationPeer.CreatePeerForElement(reveal);
        IInvokeProvider invoke = (IInvokeProvider)revealPeer.GetPattern(PatternInterface.Invoke);
        invoke.Invoke();
        DrainDispatcher();
        Assert.IsTrue(box.IsPasswordRevealed, "Invoking the reveal button should reveal the password."); // use real state property
        window.Close();
    });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --filter "FullyQualifiedName~PasswordBox_AutomationPeer|FullyQualifiedName~PasswordBox_RevealButton"`
Expected: FAIL.

- [ ] **Step 3: Create the peer** `Fluence.Wpf/Automation/PasswordBoxAutomationPeer.cs`:

```csharp
// (BSD header)
using System.Windows.Automation.Peers;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>UI Automation peer for <see cref="PasswordBox"/> reporting a password edit field.</summary>
    public class PasswordBoxAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>Initializes a new instance of the <see cref="PasswordBoxAutomationPeer"/> class.</summary>
        /// <param name="owner">The password box that owns this peer.</param>
        public PasswordBoxAutomationPeer(PasswordBox owner) : base(owner)
        {
        }

        /// <inheritdoc/>
        protected override string GetClassNameCore() => nameof(PasswordBox);

        /// <inheritdoc/>
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

        /// <inheritdoc/>
        protected override bool IsPasswordCore() => true;
    }
}
```

- [ ] **Step 4: Wire the peer in `PasswordBox.cs`:**

```csharp
/// <inheritdoc/>
protected override AutomationPeer OnCreateAutomationPeer() => new Automation.PasswordBoxAutomationPeer(this);
```

- [ ] **Step 5: Make reveal keyboard-operable.** In `PasswordBox.xaml` ensure the reveal element is a `ui:Button` (focusable) named `PART_RevealButton` with `AutomationProperties.Name="Show password"`. In `PasswordBox.cs`, in addition to the existing mouse press/hold handlers, handle the button `Click` (which fires for Space/Enter) to toggle the revealed state, and flip the name between "Show password"/"Hide password" via `AutomationProperties.SetName(revealButton, ...)` in `OnApplyTemplate` / on toggle. Keep mouse press-and-hold behavior intact for pointer users. Format the XAML: `pwsh eng/Format-Xaml.ps1 -Path Fluence.Wpf/Themes/Controls/PasswordBox.xaml`.

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --filter "FullyQualifiedName~PasswordBox_AutomationPeer|FullyQualifiedName~PasswordBox_RevealButton"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add Fluence.Wpf/Automation/PasswordBoxAutomationPeer.cs Fluence.Wpf/Controls/PasswordBox.cs Fluence.Wpf/Themes/Controls/PasswordBox.xaml Fluence.Wpf.Tests/ControlTests.PasswordBox.cs
git commit -m "a11y: add PasswordBox peer and keyboard-accessible reveal toggle"
```

---

## Phase 2 — High (names for icon-only buttons in templates)

### Task 4: Accessible names for picker, spinner, and search glyph buttons

**Files:**
- Modify: `Fluence.Wpf/Themes/Controls/DatePicker.xaml` (PART_AcceptButton ~194, PART_CancelButton ~202)
- Modify: `Fluence.Wpf/Themes/Controls/TimePicker.xaml` (PART_AcceptButton ~195, PART_CancelButton ~203)
- Modify: `Fluence.Wpf/Themes/Controls/NumberBox.xaml` (PART_UpButton ~124, PART_DownButton ~135)
- Modify: `Fluence.Wpf/Themes/Controls/AutoSuggestBox.xaml` (PART_QueryButton ~113)
- Test: `Fluence.Wpf.Tests/ControlTests.Accessibility.cs` (extend)

**Interfaces:** none consumed/produced. Pure attribute additions.

- [ ] **Step 1: Write the failing test** (extend `ControlTests.Accessibility.cs`):

```csharp
[TestMethod]
public void GlyphButtons_InPickersAndSpinners_HaveAutomationNames()
{
    RunOnSta(() =>
    {
        MergeGenericDictionary(Application.Current.Resources);

        Controls.NumberBox numberBox = new();
        Window window = new() { Content = numberBox };
        window.Show();
        numberBox.ApplyTemplate();
        DrainDispatcher();

        FrameworkElement? up = FindVisualChildByName<FrameworkElement>(numberBox, "PART_UpButton");
        FrameworkElement? down = FindVisualChildByName<FrameworkElement>(numberBox, "PART_DownButton");
        Assert.IsNotNull(up);
        Assert.IsNotNull(down);
        Assert.AreEqual("Increase", AutomationProperties.GetName(up));
        Assert.AreEqual("Decrease", AutomationProperties.GetName(down));

        window.Close();
    });
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --filter "FullyQualifiedName~GlyphButtons_InPickersAndSpinners"`
Expected: FAIL.

- [ ] **Step 3: Add `AutomationProperties.Name` to each glyph button** (also add a matching `ToolTip` where none exists, for sighted-hover parity):
  - DatePicker/TimePicker `PART_AcceptButton` -> "Accept"; `PART_CancelButton` -> "Cancel".
  - NumberBox `PART_UpButton` -> "Increase"; `PART_DownButton` -> "Decrease".
  - AutoSuggestBox `PART_QueryButton` -> "Search".

- [ ] **Step 4: Format XAML**

Run: `pwsh eng/Format-Xaml.ps1 -Path Fluence.Wpf/Themes/Controls/DatePicker.xaml` (repeat for TimePicker.xaml, NumberBox.xaml, AutoSuggestBox.xaml)

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --filter "FullyQualifiedName~GlyphButtons_InPickersAndSpinners"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add Fluence.Wpf/Themes/Controls/DatePicker.xaml Fluence.Wpf/Themes/Controls/TimePicker.xaml Fluence.Wpf/Themes/Controls/NumberBox.xaml Fluence.Wpf/Themes/Controls/AutoSuggestBox.xaml Fluence.Wpf.Tests/ControlTests.Accessibility.cs
git commit -m "a11y: name picker, spinner, and search glyph buttons"
```

---

### Task 5: Accessible names for tab, info-bar, teaching-tip, and pager glyph buttons

**Files:**
- Modify: `Fluence.Wpf/Themes/Controls/TabView.xaml` (PART_CloseButton ~74, PART_ScrollBackButton ~252, PART_ScrollForwardButton ~318)
- Modify: `Fluence.Wpf/Themes/Controls/InfoBar.xaml` (PART_CloseButton ~136)
- Modify: `Fluence.Wpf/Themes/Controls/TeachingTip.xaml` (PART_AlternateCloseButton ~202)
- Modify: `Fluence.Wpf/Themes/Controls/PipsPager.xaml` (PART_PreviousButton ~206, PART_NextButton ~220)
- Test: `Fluence.Wpf.Tests/ControlTests.Accessibility.cs` (extend)

**Interfaces:** none.

- [ ] **Step 1: Write the failing test** asserting `AutomationProperties.GetName` on InfoBar `PART_CloseButton` equals "Close" (instantiate `Controls.InfoBar`, show, ApplyTemplate, find part). Mirror the Task 4 test shape.

- [ ] **Step 2: Run test to verify it fails.** Run the focused filter; Expected: FAIL.

- [ ] **Step 3: Add names:** TabView `PART_CloseButton` -> "Close tab"; `PART_ScrollBackButton` -> "Scroll tabs backward"; `PART_ScrollForwardButton` -> "Scroll tabs forward". InfoBar `PART_CloseButton` -> "Close". TeachingTip `PART_AlternateCloseButton` -> "Close". PipsPager `PART_PreviousButton` -> "Previous page"; `PART_NextButton` -> "Next page". Add ToolTips where missing.

- [ ] **Step 4: Format XAML** for each edited file via `eng/Format-Xaml.ps1 -Path ...`.

- [ ] **Step 5: Run test to verify it passes.** Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add Fluence.Wpf/Themes/Controls/TabView.xaml Fluence.Wpf/Themes/Controls/InfoBar.xaml Fluence.Wpf/Themes/Controls/TeachingTip.xaml Fluence.Wpf/Themes/Controls/PipsPager.xaml Fluence.Wpf.Tests/ControlTests.Accessibility.cs
git commit -m "a11y: name tab, info-bar, teaching-tip, and pager glyph buttons"
```

---

## Phase 3 — High (input labeling: headers become accessible names)

### Task 6: Wire control Headers as accessible labels

**Files:**
- Modify: `Fluence.Wpf/Controls/NumberBox.cs` (Header ~131-250), `AutoSuggestBox.cs` (Header ~249), `ToggleSwitch.cs` (HeaderContent ~153), `AppBarButton.cs` (Label ~80)
- Test: extend `ControlTests.NumberBox.cs`, add cases for AutoSuggestBox/ToggleSwitch.

**Interfaces:**
- Consumes: each control's existing `Header`/`HeaderContent`/`Label` property.
- Produces: peer `GetNameCore` (or `LabeledBy` propagation) returns the header text when no explicit `AutomationProperties.Name` is set.

**Context:** Header is visual-only. The simplest robust fix that works for both string and object headers is to override the peer's `GetNameCore` to fall back to the header text. Two of these controls already have peers (NumberBox, AutoSuggestBox); ToggleSwitch has one too. AppBarButton derives from stock Button (no custom peer) so wire its Label via `AutomationProperties.Name` set in code when Label changes. Prefer `LabeledBy` only when the header is a discrete visual `TextBlock` part the peer can resolve; otherwise return the string. Reference: https://learn.microsoft.com/accessibility-tools-docs/items/wpf/edit_name

- [ ] **Step 1: Write the failing test** (extend `ControlTests.NumberBox.cs`):

```csharp
[TestMethod]
public void NumberBox_Header_BecomesAccessibleName()
{
    RunOnSta(() =>
    {
        MergeGenericDictionary(Application.Current.Resources);
        Controls.NumberBox box = new() { Header = "Quantity" };
        Window window = new() { Content = box };
        window.Show();
        box.ApplyTemplate();
        DrainDispatcher();

        AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(box);
        Assert.AreEqual("Quantity", peer.GetName(),
            "NumberBox Header must be the accessible name when no explicit AutomationProperties.Name is set.");

        box.SetValue(AutomationProperties.NameProperty, "Explicit");
        Assert.AreEqual("Explicit", peer.GetName(), "Explicit AutomationProperties.Name must win over Header.");

        window.Close();
    });
}
```

- [ ] **Step 2: Run test to verify it fails.** Filter `~NumberBox_Header_BecomesAccessibleName`. Expected: FAIL.

- [ ] **Step 3: Override `GetNameCore` in the NumberBox/AutoSuggestBox/ToggleSwitch peers** to honor explicit name first, then header text:

```csharp
/// <inheritdoc/>
protected override string GetNameCore()
{
    string baseName = base.GetNameCore();
    if (!string.IsNullOrWhiteSpace(baseName))
    {
        return baseName; // honors AutomationProperties.Name / LabeledBy
    }

    return OwnerControl.Header?.ToString() ?? string.Empty; // ToggleSwitch: HeaderContent; AppBarButton handled in code
}
```

(For AppBarButton, instead set `AutomationProperties.SetName(this, Label?.ToString())` whenever Label changes, in its Label property-changed callback, only when no explicit name is present.)

- [ ] **Step 4: Run test to verify it passes.** Expected: PASS.

- [ ] **Step 5: Build both TFMs.** `dotnet build Fluence.Wpf.sln -c Debug` — 0/0.

- [ ] **Step 6: Commit**

```bash
git add Fluence.Wpf/Automation/NumberBoxAutomationPeer.cs Fluence.Wpf/Automation/AutoSuggestBoxAutomationPeer.cs Fluence.Wpf/Automation/ToggleSwitchAutomationPeer.cs Fluence.Wpf/Controls/AppBarButton.cs Fluence.Wpf.Tests/ControlTests.NumberBox.cs
git commit -m "a11y: expose control Headers and AppBarButton Label as accessible names"
```

---

## Phase 4 — High (live regions and dynamic announcements, net472-safe)

### Task 7: InfoBar live-region announcements

**Files:**
- Modify: `Fluence.Wpf/Controls/InfoBar.cs` (event pipeline ~238-299)
- Test: extend `Fluence.Wpf.Tests/ControlTests.InfoBar.cs`

**Interfaces:**
- Consumes: InfoBar `IsOpen`/`Severity`/`Title`/`Message` change notifications (find the property callbacks).
- Produces: a private helper `AnnounceLiveRegion()` raising `LiveRegionChanged`.

**Context:** An InfoBar appearing or changing severity must be announced without moving focus. net472-safe path only. Reference: https://learn.microsoft.com/dotnet/api/system.windows.automation.peers.automationevents

- [ ] **Step 1: Write the failing test** in `ControlTests.InfoBar.cs`:

```csharp
[TestMethod]
public void InfoBar_DeclaresPoliteLiveSetting()
{
    RunOnSta(() =>
    {
        MergeGenericDictionary(Application.Current.Resources);
        Controls.InfoBar bar = new() { Title = "Saved", IsOpen = true };
        Window window = new() { Content = bar };
        window.Show();
        bar.ApplyTemplate();
        DrainDispatcher();

        Assert.AreEqual(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(bar),
            "InfoBar must declare a polite live region so Narrator announces it without stealing focus.");
        window.Close();
    });
}
```

- [ ] **Step 2: Run test to verify it fails.** Filter `~InfoBar_DeclaresPoliteLiveSetting`. Expected: FAIL.

- [ ] **Step 3: Set LiveSetting in the static constructor and announce on change** in `InfoBar.cs`:

```csharp
static InfoBar()
{
    AutomationProperties.LiveSettingProperty.OverrideMetadata(
        typeof(InfoBar), new FrameworkPropertyMetadata(AutomationLiveSetting.Polite));
    // existing DefaultStyleKey override stays
}

private void AnnounceLiveRegion()
{
    if (!AutomationPeer.ListenerExists(AutomationEvents.LiveRegionChanged))
    {
        return;
    }

    AutomationPeer? peer = UIElementAutomationPeer.FromElement(this) ?? UIElementAutomationPeer.CreatePeerForElement(this);
    peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
}
```

Call `AnnounceLiveRegion()` from the `IsOpen` -> true transition and from `Severity`/`Title`/`Message` change callbacks while open.

- [ ] **Step 4: Run test to verify it passes.** Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Fluence.Wpf/Controls/InfoBar.cs Fluence.Wpf.Tests/ControlTests.InfoBar.cs
git commit -m "a11y: announce InfoBar open and severity changes via live region"
```

---

### Task 8: ProgressBar and ProgressRing state announcements

**Files:**
- Modify: `Fluence.Wpf/Controls/ProgressBar.cs`, `Fluence.Wpf/Controls/ProgressRing.cs`
- Modify: `Fluence.Wpf/Automation/ProgressRingAutomationPeer.cs` (raise ValueProperty changes)
- Test: extend an existing ProgressBar/ProgressRing test partial (create `ControlTests.ProgressRing.cs` if needed)

**Interfaces:**
- Consumes: `ShowError`/`ShowPaused`/`IsIndeterminate`/`Value` (per AGENTS.md these exist on both).
- Produces: live-region announcement on error/paused/indeterminate-completion state change.

**Context:** Determinate value is already RangeValue (ProgressRing peer). Add (a) RangeValue ValueProperty change events so Narrator can read progress on demand, and (b) a polite live region announcement when `ShowError`/`ShowPaused` toggles, since those are away-from-focus status changes.

- [ ] **Step 1: Write the failing test** asserting `AutomationProperties.GetLiveSetting(progressRing) == AutomationLiveSetting.Polite`. Mirror the InfoBar test.

- [ ] **Step 2: Run test to verify it fails.** Expected: FAIL.

- [ ] **Step 3: Implement** — set `AutomationProperties.LiveSettingProperty` metadata to `Polite` on both controls' static constructors; add the same `AnnounceLiveRegion()` helper and call it from `ShowError`/`ShowPaused` change callbacks. In `ProgressRingAutomationPeer` (and add a ProgressBar peer only if one does not already exist; ProgressBar inherits the stock peer which already exposes RangeValue), raise `RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, old, new)` when Value changes. Reference: https://learn.microsoft.com/dotnet/desktop/wpf/controls/ui-automation-of-a-wpf-custom-control

- [ ] **Step 4: Run test to verify it passes.** Expected: PASS.

- [ ] **Step 5: Build both TFMs.** 0/0.

- [ ] **Step 6: Commit**

```bash
git add Fluence.Wpf/Controls/ProgressBar.cs Fluence.Wpf/Controls/ProgressRing.cs Fluence.Wpf/Automation/ProgressRingAutomationPeer.cs Fluence.Wpf.Tests/ControlTests.ProgressRing.cs
git commit -m "a11y: announce progress error/paused state and value changes"
```

---

### Task 9: TeachingTip open announcement and validation-message announcements

**Files:**
- Modify: `Fluence.Wpf/Controls/TeachingTip.cs` (IsOpen ~), `Fluence.Wpf/Controls/TextBox.cs` (ValidationState/ValidationMessage ~203-359), `Fluence.Wpf/Controls/NumberBox.cs` (parse-error surface ~301-314)
- Test: extend `ControlTests.TextBox.cs` (create if missing)

**Interfaces:**
- Consumes: `TextBox.ValidationState`/`ValidationMessage`, `TeachingTip.IsOpen`.
- Produces: live-region announcement of the validation message and the teaching-tip body.

**Context:** When validation transitions to Error/Warning, Narrator must speak the message. Set the validation message text element as a polite live region (or announce via the control peer with the message in `ItemStatus`/`HelpText`). Approach: when `ValidationState` becomes Error/Warning, set `AutomationProperties.SetHelpText(this, ValidationMessage)` and raise `LiveRegionChanged` on the validation message presenter (mark `PART_ValidationText` or equivalent with `AutomationProperties.LiveSetting=Assertive` in the template, since validation errors are higher priority). For TeachingTip, set polite LiveSetting and announce on open.

- [ ] **Step 1: Write the failing test** asserting that after setting `ValidationState = Error` with a message, `AutomationProperties.GetHelpText(textBox)` equals the message. Use the real enum member names from `TextBox.cs`.

- [ ] **Step 2: Run test to verify it fails.** Expected: FAIL.

- [ ] **Step 3: Implement** the HelpText sync + live-region raise in the `UpdateHelperText`/validation callback; mark the validation text part with `AutomationProperties.LiveSetting="Assertive"` in `TextBox.xaml` (and NumberBox.xaml for parse errors); set TeachingTip LiveSetting=Polite and announce on open. Format edited XAML.

- [ ] **Step 4: Run test to verify it passes.** Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Fluence.Wpf/Controls/TeachingTip.cs Fluence.Wpf/Controls/TextBox.cs Fluence.Wpf/Controls/NumberBox.cs Fluence.Wpf/Themes/Controls/TextBox.xaml Fluence.Wpf/Themes/Controls/NumberBox.xaml Fluence.Wpf.Tests/ControlTests.TextBox.cs
git commit -m "a11y: announce validation messages and teaching-tip content via live regions"
```

---

## Phase 5 — High (peer correctness and missing peer)

### Task 10: Fix NumberBox peer LargeChange and report HyperlinkButton as a hyperlink

**Files:**
- Modify: `Fluence.Wpf/Automation/NumberBoxAutomationPeer.cs` (line ~75: `LargeChange` returns `SmallChange`)
- Modify: `Fluence.Wpf/Controls/HyperlinkButton.cs` (add peer override) and create `Fluence.Wpf/Automation/HyperlinkButtonAutomationPeer.cs`
- Test: extend `ControlTests.NumberBox.cs`; add `ControlTests.HyperlinkButton.cs`

**Interfaces:**
- Produces: `HyperlinkButtonAutomationPeer : ButtonAutomationPeer` with `GetAutomationControlTypeCore => AutomationControlType.Hyperlink`.

- [ ] **Step 1: Write failing tests** — (a) NumberBox peer `IRangeValueProvider.LargeChange` equals `NumberBox.LargeChange` (set distinct Small/Large values); (b) HyperlinkButton peer control type is `Hyperlink`.

```csharp
[TestMethod]
public void NumberBox_Peer_LargeChange_MatchesControl()
{
    RunOnSta(() =>
    {
        MergeGenericDictionary(Application.Current.Resources);
        Controls.NumberBox box = new() { SmallChange = 1, LargeChange = 10 };
        Window window = new() { Content = box };
        window.Show(); box.ApplyTemplate(); DrainDispatcher();
        AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(box);
        IRangeValueProvider range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue);
        Assert.AreEqual(10.0, range.LargeChange, 0.001);
        Assert.AreEqual(1.0, range.SmallChange, 0.001);
        window.Close();
    });
}
```

- [ ] **Step 2: Run tests to verify they fail.** Expected: FAIL (LargeChange returns 1).

- [ ] **Step 3: Fix** `NumberBoxAutomationPeer` line ~75 to return `OwnerNumberBox.LargeChange`. Create `HyperlinkButtonAutomationPeer : ButtonAutomationPeer` overriding `GetAutomationControlTypeCore => AutomationControlType.Hyperlink` and `GetClassNameCore => nameof(HyperlinkButton)`; wire `OnCreateAutomationPeer` in `HyperlinkButton.cs`.

- [ ] **Step 4: Run tests to verify they pass.** Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Fluence.Wpf/Automation/NumberBoxAutomationPeer.cs Fluence.Wpf/Automation/HyperlinkButtonAutomationPeer.cs Fluence.Wpf/Controls/HyperlinkButton.cs Fluence.Wpf.Tests/ControlTests.NumberBox.cs Fluence.Wpf.Tests/ControlTests.HyperlinkButton.cs
git commit -m "a11y: fix NumberBox peer LargeChange and report HyperlinkButton as Hyperlink"
```

---

### Task 11: PersonPicture automation peer with accessible name

**Files:**
- Create: `Fluence.Wpf/Automation/PersonPictureAutomationPeer.cs`
- Modify: `Fluence.Wpf/Controls/PersonPicture.cs` (visual-state updates ~253-319)
- Test: `Fluence.Wpf.Tests/ControlTests.PersonPicture.cs` (create)

**Interfaces:**
- Consumes: `PersonPicture.DisplayName` and/or `Initials` (use the real property names from the file).
- Produces: `PersonPictureAutomationPeer : FrameworkElementAutomationPeer` returning control type `Image` (or `Text`) and a name from DisplayName/Initials.

**Context:** A person picture conveys identity; Narrator currently reads nothing. Reference: https://learn.microsoft.com/accessibility-tools-docs/items/wpf/customcontrol_name

- [ ] **Step 1: Write the failing test** — set `DisplayName = "Ada Lovelace"`, assert peer is `PersonPictureAutomationPeer`, control type is `Image`, and `GetName()` returns "Ada Lovelace"; explicit `AutomationProperties.Name` wins.

- [ ] **Step 2: Run test to verify it fails.** Expected: FAIL.

- [ ] **Step 3: Create the peer** (mirror `RatingControlAutomationPeer` structure): `GetAutomationControlTypeCore => AutomationControlType.Image`, `GetClassNameCore => nameof(PersonPicture)`, `GetNameCore` returns explicit name else `DisplayName` else `Initials`. Wire `OnCreateAutomationPeer` in `PersonPicture.cs`.

- [ ] **Step 4: Run test to verify it passes.** Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Fluence.Wpf/Automation/PersonPictureAutomationPeer.cs Fluence.Wpf/Controls/PersonPicture.cs Fluence.Wpf.Tests/ControlTests.PersonPicture.cs
git commit -m "a11y: add PersonPicture peer with name from display name or initials"
```

---

## Phase 6 — Medium (remaining gaps)

### Task 12: ColorPicker spectrum keyboard operability

**Files:**
- Modify: `Fluence.Wpf/Controls/ColorPicker.cs` (spectrum drag ~903-945; add key handling)
- Test: extend `Fluence.Wpf.Tests/ControlTests.ColorPicker.cs`

**Interfaces:**
- Consumes: the internal HSV source-of-truth fields (saturation/value) and the spectrum part.
- Produces: arrow-key adjustment of saturation (Left/Right) and value (Up/Down) on the focused spectrum element, with an accessible name ("Color spectrum") and focusability.

**Context:** The hue and alpha sliders are already keyboard-operable; only the 2D spectrum is mouse-only. Make the spectrum thumb/surface focusable with `AutomationProperties.Name="Color spectrum"` and handle arrow keys to step saturation/value (e.g. 1% per press, larger with Page keys), updating the same HSV state the mouse path uses so there is no RGB round-trip drift.

- [ ] **Step 1: Write the failing test** — focus the spectrum part, send Key.Right, assert saturation increased (read via `ColorPicker.Color` change or an internal test hook). Use the real part name and a verifiable observable.

- [ ] **Step 2: Run test to verify it fails.** Expected: FAIL.

- [ ] **Step 3: Implement** focusable spectrum + arrow/Page key handling in `ColorPicker.cs`, routing through the existing HSV update method. Add `AutomationProperties.Name` and `IsTabStop` on the spectrum part in `ColorPicker.xaml`; format XAML.

- [ ] **Step 4: Run test to verify it passes.** Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Fluence.Wpf/Controls/ColorPicker.cs Fluence.Wpf/Themes/Controls/ColorPicker.xaml Fluence.Wpf.Tests/ControlTests.ColorPicker.cs
git commit -m "a11y: make ColorPicker spectrum keyboard operable"
```

---

### Task 13: Clickable Card peer plus CheckBox/RadioButton description help text

**Files:**
- Modify: `Fluence.Wpf/Controls/Card.cs` (clickable path ~250-301), create `Fluence.Wpf/Automation/CardAutomationPeer.cs`
- Modify: `Fluence.Wpf/Controls/CheckBox.cs` (Description ~73), `Fluence.Wpf/Controls/RadioButton.cs` (Description ~55)
- Test: `Fluence.Wpf.Tests/ControlTests.Card.cs` (create or extend); extend selection tests

**Interfaces:**
- Produces: `CardAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider` used only when `IsClickable` is true (control type `Button`); otherwise base behavior. Description text surfaced as `AutomationProperties.HelpText`.

**Context:** A clickable Card already handles Enter/Space but reports as generic content. When `IsClickable`, expose a Button role + Invoke and ensure `IsTabStop`/`Focusable`. CheckBox/RadioButton `Description` is supplemental, so map it to HelpText (read on demand), not Name. Reference: https://learn.microsoft.com/windows/apps/design/accessibility/basic-accessibility-information

- [ ] **Step 1: Write failing tests** — (a) clickable Card peer is `CardAutomationPeer`, control type `Button`, exposes Invoke that raises Click; non-clickable Card uses base peer. (b) `AutomationProperties.GetHelpText(checkBox)` equals the Description after it is set.

- [ ] **Step 2: Run tests to verify they fail.** Expected: FAIL.

- [ ] **Step 3: Implement** the CardAutomationPeer (Invoke calls the existing click-raising method) returned from `OnCreateAutomationPeer` only when `IsClickable`; set `Focusable`/`IsTabStop` true in that mode. In CheckBox/RadioButton, in the `Description` property-changed callback call `AutomationProperties.SetHelpText(this, description)`.

- [ ] **Step 4: Run tests to verify they pass.** Expected: PASS.

- [ ] **Step 5: Build both TFMs.** 0/0.

- [ ] **Step 6: Commit**

```bash
git add Fluence.Wpf/Automation/CardAutomationPeer.cs Fluence.Wpf/Controls/Card.cs Fluence.Wpf/Controls/CheckBox.cs Fluence.Wpf/Controls/RadioButton.cs Fluence.Wpf.Tests/ControlTests.Card.cs
git commit -m "a11y: clickable Card Button peer and CheckBox/RadioButton description help text"
```

---

## Phase 7 — Documentation, demo coverage, and final verification

### Task 14: Document net472 gaps, update docs and demo, run the full gate

**Files:**
- Modify: `KNOWN_ISSUES.md`, `CHANGELOG.md`, `docs/controls.md`, `Fluence.Wpf.Demo/Pages/GalleryAccessibilityPage.xaml(.cs)`

**Interfaces:** none.

- [ ] **Step 1: Record the net472 accessibility gaps in `KNOWN_ISSUES.md`** — `AutomationPeer.RaiseNotificationEvent`, `AutomationProperties.IsDialog`, `AutomationProperties.HeadingLevel`, and automatic `PositionInSet`/`SizeOfSet` are .NET Framework 4.8+ and unavailable on net472. State the chosen fallback for each (live region for notifications; ControlType Window + manual focus management for dialogs; explicit set position where needed; HeadingLevel omitted). Cite https://learn.microsoft.com/dotnet/framework/whats-new/whats-new-in-accessibility.

- [ ] **Step 2: Add a CHANGELOG `[Unreleased]` entry** summarizing the accessibility pass (named glyph buttons; RatingControl/PasswordBox/PersonPicture/HyperlinkButton/Card peers; header labeling; live regions for InfoBar/Progress/validation/TeachingTip; NumberBox peer LargeChange fix; ColorPicker spectrum keyboard).

- [ ] **Step 3: Add an accessibility section to `docs/controls.md`** documenting the per-control roles, names, patterns, and the net472 live-region approach.

- [ ] **Step 4: Extend `GalleryAccessibilityPage`** with a short live-region demo (a button that opens an InfoBar / triggers a validation error) so the announcement path is visually demonstrable, plus a RatingControl keyboard example. Keep the page's existing four sections. Format the XAML.

- [ ] **Step 5: Run the full gate on both TFMs.**

Run:
```
pwsh eng/Format-Xaml.ps1 -Check
dotnet build Fluence.Wpf.sln -c Debug
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --no-build
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build
```
Expected: XAML check passes; build 0 warnings / 0 errors; both test lanes green with the new tests; test count strictly above the prior baseline.

- [ ] **Step 6: Manual Narrator pass (record results in the PR/commit message).** With Narrator on: tab the gallery shell and each control page; confirm every control speaks name + role + value; confirm InfoBar / validation / progress changes are announced; confirm RatingControl, ColorPicker spectrum, and PasswordBox reveal are fully keyboard-operable; repeat once in a High Contrast theme. Reference: https://learn.microsoft.com/windows/apps/design/accessibility/accessibility-testing

- [ ] **Step 7: Commit**

```bash
git add KNOWN_ISSUES.md CHANGELOG.md docs/controls.md Fluence.Wpf.Demo/Pages/GalleryAccessibilityPage.xaml Fluence.Wpf.Demo/Pages/GalleryAccessibilityPage.xaml.cs
git commit -m "a11y: document net472 gaps, extend accessibility demo, finalize docs"
```

---

## Self-Review

**Spec coverage** — every Critical/High gap from the audit maps to a task: window/title-bar names (T1), RatingControl (T2), PasswordBox (T3), picker/spinner/search names (T4), tab/info/tip/pager names (T5), header labeling + AppBarButton (T6), InfoBar live region (T7), progress announcements (T8), validation + teaching-tip announcements (T9), NumberBox LargeChange + HyperlinkButton role (T10), PersonPicture (T11), ColorPicker spectrum keyboard (T12), Card peer + CheckBox/RadioButton description (T13), docs/demo/verification (T14). Medium items not separately tasked (HyperlinkButton handled in T10; FontIcon standalone is acceptable when parented by a labeled control, noted in docs).

**Placeholder scan** — property and part names marked "use the real name from the file" are deliberate: the executing agent must confirm exact identifiers (`MaxRating`, reveal state property, `PART_RevealButton`, validation enum members) against the source before coding. Every code step shows real, compilable structure mirroring existing peers.

**Type consistency** — all new peers derive from `FrameworkElementAutomationPeer` (or `ButtonAutomationPeer` for HyperlinkButton) and live in `Fluence.Wpf.Automation`; the `AnnounceLiveRegion()` helper signature is identical across InfoBar/Progress/TeachingTip; `RaiseAutomationEvent(AutomationEvents.LiveRegionChanged)` and `AutomationProperties.LiveSettingProperty` metadata are the single net472-safe live-region mechanism used throughout.

**net472 gate** — no task uses `RaiseNotificationEvent`, `IsDialog`, `HeadingLevel`, or auto position/size; all are explicitly substituted and logged (T14).
