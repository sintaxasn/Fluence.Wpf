# FluenceWindow public surface snapshot

Baseline captured 2026-04-20 against HEAD commit `46c4b93` for the WI-2 audit +
harden pass. This file is the diff reference used to prove zero public-API
regressions across WI-2. The caption-button and movement contract was updated
2026-05-01: new property-style names are canonical, while the old public names
remain as `[Obsolete]` pass-through aliases for source and binary consumers.
Any removal or signature change is a stop-and-ask event (PSADT consumes
these members via `ProjectReference`; see
`docs/_internal/wi1-verification.md` Stage 0.2 for the PSADT build
confirmation).

## Scope

The snapshot covers every public (or `protected` on a public class) member of
the window-chrome stack under `Fluence.Wpf.Controls`:

- [`FluenceWindow`](../Fluence.Wpf/Controls/FluenceWindow.cs) — the window.
- [`TitleBar`](../Fluence.Wpf/Controls/TitleBar.cs) — the standalone title-bar
  control (public, but not consumed by PSADT; PSADT uses
  `FluenceWindow.TitleBar` of type `UIElement`).

The following classes are **internal** and therefore excluded from the public
diff (changes are allowed, but call out any signature shift in the commit body
for test-assembly spot-review via `InternalsVisibleTo`):

- [`WindowPolicy`](../Fluence.Wpf/Controls/WindowPolicy.cs) — `internal static`.
- [`WindowCapabilities`](../Fluence.Wpf/Controls/WindowPolicy.cs) — `internal
  sealed`.
- [`BackdropPlan`](../Fluence.Wpf/Controls/WindowPolicy.cs) — `internal sealed`.
- [`FramePlan`](../Fluence.Wpf/Controls/WindowPolicy.cs) — `internal sealed`.
- [`CaptionButtonChrome`](../Fluence.Wpf/Controls/CaptionButtonChrome.cs) —
  `internal static`.

### Historical note: `FluentWindow`

The plan document references `[Obsolete] FluentWindow` as an alias to
preserve. The alias no longer exists; commit `2728efe` (_"Refactor:
Consolidate FluentWindow and remove compatibility layer"_, 2026-04-20) deleted
`Fluence.Wpf/Controls/FluentWindow.cs` entirely. PSADT's
[FluentDialog.xaml](../../PSAppDeployToolkit/src/PSADT/PSADT.UserInterface/Interfaces/Fluent/FluentDialog.xaml)
and [FluentDialog.xaml.cs](../../PSAppDeployToolkit/src/PSADT/PSADT.UserInterface/Interfaces/Fluent/FluentDialog.xaml.cs)
already bind to `FluenceWindow`. No WI-2 action required.

---

## `Fluence.Wpf.Controls.FluenceWindow`

```csharp
[TemplatePart(Name = "PART_MinimizeButton", Type = typeof(System.Windows.Controls.Button))]
[TemplatePart(Name = "PART_MaximizeButton", Type = typeof(System.Windows.Controls.Button))]
[TemplatePart(Name = "PART_RestoreButton", Type = typeof(System.Windows.Controls.Button))]
[TemplatePart(Name = "PART_CloseButton", Type = typeof(System.Windows.Controls.Button))]
public class FluenceWindow : Window
```

### Public static fields (non-DP)

| Member                                | Type             | Notes                                                  |
|---------------------------------------|------------------|--------------------------------------------------------|
| `IsNotNullConverter`                  | `IValueConverter`| Singleton `IsNotNullValueConverter`; used by template. |

### Public dependency properties (canonical registered contract)

> Updated 2026-05-01: `IsMoveable` and the three `Is*ButtonVisible` visibility properties are canonical. Old names remain as obsolete aliases.
> Updated 2026-05-03: `TitleBarLeftIndent` was removed; shell title spacing is owned by `TitleBar` and consumer layout.

| DP field                               | CLR wrapper                   | Type              | Default                      | Metadata callback                         |
|----------------------------------------|-------------------------------|-------------------|------------------------------|-------------------------------------------|
| `SystemBackdropTypeProperty`               | `SystemBackdropType`              | `BackdropType`    | `BackdropType.Auto`          | `OnSystemBackdropTypeChanged`                 |
| `CornerStyleProperty`                | `CornerStyle`               | `CornerPreference`| `CornerPreference.Round`     | `OnCornerStyleChanged`                  |
| `MarginMaximizedProperty`              | `MarginMaximized`             | `Thickness`       | `new Thickness(0)`           | _(none)_                                  |
| `ExtendsContentIntoTitleBarProperty`   | `ExtendsContentIntoTitleBar`  | `bool`            | `false`                      | `OnExtendsContentIntoTitleBarChanged`     |
| `TitleBarProperty`                     | `TitleBar`                    | `UIElement`       | `null`                       | _(none)_                                  |
| `TitleBarHeightProperty`               | `TitleBarHeight`              | `double`          | `DefaultTitleBarHeight` (68) | `OnTitleBarHeightChanged`                 |
| `ShowIconProperty`                     | `ShowIcon`                    | `bool`            | `true`                       | _(none)_                                  |
| `ShowTitleProperty`                    | `ShowTitle`                   | `bool`            | `true`                       | _(none)_                                  |
| `IsMinimizeButtonVisibleProperty`      | `IsMinimizeButtonVisible`     | `Visibility`      | `Visibility.Visible`         | `OnCaptionButtonChromeOverrideChanged`    |
| `IsMaximizeButtonVisibleProperty`      | `IsMaximizeButtonVisible`     | `Visibility`      | `Visibility.Visible`         | `OnCaptionButtonChromeOverrideChanged`    |
| `IsCloseButtonVisibleProperty`         | `IsCloseButtonVisible`        | `Visibility`      | `Visibility.Visible`         | `OnCaptionButtonChromeOverrideChanged`    |
| `IsMinimizableProperty`                | `IsMinimizable`               | `bool`            | `true`                       | `OnCaptionButtonChromeOverrideChanged`    |
| `IsMaximizableProperty`                | `IsMaximizable`               | `bool`            | `true`                       | `OnCaptionButtonChromeOverrideChanged`    |
| `IsClosableProperty`                   | `IsClosable`                  | `bool`            | `true`                       | `OnCaptionButtonChromeOverrideChanged`    |
| `IsMoveableProperty`                   | `IsMoveable`                  | `bool`            | `true`                       | _(none)_                                  |
| `HasShadowProperty`                    | `HasShadow`                   | `bool`            | `true`                       | `OnHasShadowChanged`                      |

All registered via `DependencyProperty.Register`; none read-only; none
attached.

### Obsolete compatibility aliases

| Obsolete member                         | Replacement                     | Notes                                      |
|-----------------------------------------|----------------------------------|--------------------------------------------|
| `CanMoveProperty` / `CanMove`           | `IsMoveableProperty` / `IsMoveable` | Alias to the same registered DP.        |
| `MinimizeButtonVisibilityProperty` / `MinimizeButtonVisibility` | `IsMinimizeButtonVisibleProperty` / `IsMinimizeButtonVisible` | Alias to the same registered DP. |
| `MaximizeButtonVisibilityProperty` / `MaximizeButtonVisibility` | `IsMaximizeButtonVisibleProperty` / `IsMaximizeButtonVisible` | Alias to the same registered DP. |
| `CloseButtonVisibilityProperty` / `CloseButtonVisibility` | `IsCloseButtonVisibleProperty` / `IsCloseButtonVisible` | Alias to the same registered DP. |
| `SetMinimizeButtonVisibility(Visibility)` | `IsMinimizeButtonVisible`      | Pass-through method retained obsolete.     |
| `SetMaximizeButtonVisibility(Visibility)` | `IsMaximizeButtonVisible`      | Pass-through method retained obsolete.     |
| `SetCloseButtonVisibility(Visibility)` | `IsCloseButtonVisible`          | Pass-through method retained obsolete.     |

### Public instance members

| Member                                 | Kind        | Signature                                                               |
|----------------------------------------|-------------|-------------------------------------------------------------------------|
| `FluenceWindow()`                      | constructor | `public FluenceWindow()`                                                |
| `SetTitleBar`                          | method      | `public void SetTitleBar(UIElement titleBar)`                           |
| `SetMinimizeButtonVisibility`          | obsolete method | `public void SetMinimizeButtonVisibility(Visibility visibility)`     |
| `SetMaximizeButtonVisibility`          | obsolete method | `public void SetMaximizeButtonVisibility(Visibility visibility)`     |
| `SetCloseButtonVisibility`             | obsolete method | `public void SetCloseButtonVisibility(Visibility visibility)`        |
| `OnApplyTemplate`                      | override    | `public override void OnApplyTemplate()`                                |

### Protected overrides

| Member                                 | Signature                                                                            |
|----------------------------------------|--------------------------------------------------------------------------------------|
| `OnSourceInitialized`                  | `protected override void OnSourceInitialized(EventArgs e)`                           |
| `OnStateChanged`                       | `protected override void OnStateChanged(EventArgs e)`                                |
| `OnActivated`                          | `protected override void OnActivated(EventArgs e)`                                   |
| `OnDeactivated`                        | `protected override void OnDeactivated(EventArgs e)`                                 |
| `OnPropertyChanged`                    | `protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)`    |
| `OnClosed`                             | `protected override void OnClosed(EventArgs e)`                                      |

### Events

No events declared directly on `FluenceWindow`. All eventing is inherited from
`System.Windows.Window` (`Activated`, `Deactivated`, `Closing`, `Closed`,
`StateChanged`, `SourceInitialized`, …). Theme / accent changes are routed
through `ApplicationThemeManager.Changed` and
`ApplicationAccentColorManager.AccentColorChanged`, which the window subscribes
to at construction and unsubscribes in `OnClosed`.

### Attached properties

None declared on `FluenceWindow`.

### TemplatePart attributes

`FluenceWindow` declares four caption-button template parts:

| TemplatePart name       | Expected type                    |
|-------------------------|----------------------------------|
| `PART_MinimizeButton`   | `System.Windows.Controls.Button` |
| `PART_MaximizeButton`   | `System.Windows.Controls.Button` |
| `PART_RestoreButton`    | `System.Windows.Controls.Button` |
| `PART_CloseButton`      | `System.Windows.Controls.Button` |

### Template contract

The `Themes/Controls/FluenceWindow.xaml` default style names four caption
buttons that `OnApplyTemplate` looks up by name:

| Template element name | Expected type                   | Used by                           |
|-----------------------|---------------------------------|-----------------------------------|
| `PART_MinimizeButton` | `System.Windows.Controls.Button`| `UpdateCaptionButtons()`          |
| `PART_MaximizeButton` | `System.Windows.Controls.Button`| `UpdateCaptionButtons()`          |
| `PART_RestoreButton`  | `System.Windows.Controls.Button`| `UpdateCaptionButtons()`          |
| `PART_CloseButton`    | `System.Windows.Controls.Button`| `UpdateCaptionButtons()`          |

Each button binds to a `SystemCommands.*WindowCommand`; `FluenceWindow`
registers matching `CommandBindings` in its constructor (see
`FluenceWindow.cs:394-397`).

### Command bindings (registered in ctor)

| Command                                         | Executed handler      | CanExecute handler       |
|-------------------------------------------------|-----------------------|--------------------------|
| `SystemCommands.CloseWindowCommand`             | `OnCloseWindow`       | _(none — always true)_   |
| `SystemCommands.MaximizeWindowCommand`          | `OnMaximizeWindow`    | `OnCanResizeWindow`      |
| `SystemCommands.MinimizeWindowCommand`          | `OnMinimizeWindow`    | `OnCanMinimizeWindow`    |
| `SystemCommands.RestoreWindowCommand`           | `OnRestoreWindow`     | `OnCanResizeWindow`      |

---

## `Fluence.Wpf.Controls.TitleBar`

```csharp
public class TitleBar : Control
```

Standalone title-bar control. Intended for demo / consumer usage inside
`FluenceWindow.TitleBar`; PSADT does not consume this type directly (it
composes its own title-bar layout inside a `Grid`).

### Public dependency properties (7)

| DP field                        | CLR wrapper             | Type       | Default             | Metadata callback |
|---------------------------------|-------------------------|------------|---------------------|-------------------|
| `TitleProperty`                 | `Title`                 | `string`   | `string.Empty`      | _(none)_          |
| `IconProperty`                  | `Icon`                  | `object`   | `null`              | _(none)_          |
| `IsBackButtonVisibleProperty`   | `IsBackButtonVisible`   | `bool`     | `false`             | _(none)_          |
| `IsCompactProperty`             | `IsCompact`             | `bool`     | `false`             | _(none)_          |
| `CustomContentProperty`         | `CustomContent`         | `object`   | `null`              | _(none)_          |
| `BackCommandProperty`           | `BackCommand`           | `ICommand` | `null`              | _(none)_          |
| `BackCommandParameterProperty`  | `BackCommandParameter`  | `object`   | `null`              | _(none)_          |

All registered via `DependencyProperty.Register`; none read-only; none
attached.

### Public instance members

Constructor `public TitleBar()` is compiler-generated. The static constructor
overrides `DefaultStyleKey`; no other public methods are declared.

### Protected overrides

None beyond the `Control` defaults.

### Events

None declared directly.

### Template contract

`Themes/Controls/TitleBar.xaml` names a single back-button part (`PART_…`
naming convention), visible only when `IsBackButtonVisible="True"`, which
routes to `BackCommand` with `BackCommandParameter`.

---

## PSADT consumption cross-check (frozen reference)

From Phase 1.C discovery, locked in at this snapshot. Every member below MUST
survive WI-2 unchanged in name and semantic.

**From [`FluentDialog.xaml.cs`](../../PSAppDeployToolkit/src/PSADT/PSADT.UserInterface/Interfaces/Fluent/FluentDialog.xaml.cs):**

- Inheritance: `FluentDialog : FluenceWindow`.
- Theme manager: `ApplicationThemeManager.Apply(…)`, `.Changed`,
  `.CurrentTheme`.
- Accent manager: `ApplicationAccentColorManager.ApplyCustomAccent(Color)`.
- Dependency properties touched: `IsMinimizeButtonVisible`, `IsMinimizable`, `IsMoveable`.

**From [`FluentDialog.xaml`](../../PSAppDeployToolkit/src/PSADT/PSADT.UserInterface/Interfaces/Fluent/FluentDialog.xaml):**

- Root element: `ui:FluenceWindow`.
- Attributes set: `ExtendsContentIntoTitleBar`, `SystemBackdropType="Acrylic"`,
  `CornerStyle="Round"`, `TitleBarHeight`, `IsMinimizeButtonVisible`,
  `IsMaximizeButtonVisible`, `IsCloseButtonVisible`.

**Resource keys referenced (canonical WinUI 3; not this snapshot's
responsibility but recorded for WI-2 cross-check):**

`TextFillColorPrimaryBrush`, `AccentFillColorDefaultBrush`,
`AccentFillColorTertiaryBrush`, `AccentTextFillColorPrimaryBrush`,
`ControlSolidFillColorDefaultBrush`, `SurfaceStrokeColorFlyoutBrush`,
`SystemFillColorCautionBrush`, `SystemFillColorCriticalBrush`.

---

## Post-WI-2 diff verification

After WI-2 closes (Step 2.8), regenerate this document and diff against this
baseline. Acceptance:

- **Zero removals.** No member name or signature from the tables above may
  disappear.
- **Zero renames.** No member may be renamed without a compatibility shim.
- **Zero type changes.** No DP may change its type, default, or add required
  metadata callbacks (adding optional callbacks is fine; removing them is
  not).
- **Additions allowed.** New DPs, new methods, new overrides are acceptable
  provided they do not break the existing contract.

Any violation stops WI-2 and escalates to the user before the next commit.
