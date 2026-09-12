## Scope

`Fluence.Wpf` targets WPF applications on .NET Framework 4.7.2, .NET 8, and .NET 10 for Windows. It mirrors the Windows 11 Fluent / WinUI 3 visual language using WPF primitives, with no dependency on the Windows App SDK.

## Upgrading to 0.9.0-pre from a 0.8 preview

Every breaking change in 0.9.0-pre, the last preview before 1.0, is listed here. Some entries break only at compile time and need a recompile; some also break an already-compiled consumer binary at load time; and the resource key changes near the end fail silently instead, with no compile error at all.

### Types that became internal

Internalizing a type also breaks an already-compiled consumer binary, not only source that
recompiles, because the type becomes inaccessible at load time. The template part constants are
the one exception here: a `const` is inlined at the consumer's own compile site, so an
already-built binary keeps working and only a recompile against the new library breaks.

| Type | What to do |
| --- | --- |
| `Controls.LoopingSelectorList` | It was a `DatePicker` and `TimePicker` implementation detail. There is no replacement; use `DatePicker` or `TimePicker`. |
| `Helpers.CornerRadiusFilterConverter` | Write a one-property `IValueConverter` in your own assembly if you need the same corner filtering. |
| `Helpers.CornerRadiusFilterEdge` | Goes with `CornerRadiusFilterConverter`. |
| `Helpers.GridLengthAnimation` | Animate a `GridLength` in your own assembly with an equivalent `AnimationTimeline`, or animate `Width` on the column content instead. |
| The nine `NavigationView.Part*` constants | Template part names are not API. Read the name from the shipped template, or hard-code the string; this is the section's one source-only break. |

### Enums renamed

Values are unchanged in all three cases; rename the type at every use site.

| Was | Is |
| --- | --- |
| `SpinButtonPlacementMode` | `NumberBoxSpinButtonPlacementMode`. The property `NumberBox.SpinButtonPlacementMode` keeps its name, so XAML attributes do not change. |
| `BackdropType` | `WindowBackdropType`. `Auto` is unchanged. `FluenceWindow.SystemBackdropType` and `ApplicationThemeManager.CurrentBackdrop` keep their names and change type. |
| `CornerPreference` | `WindowCornerPreference`. `FluenceWindow.CornerStyle` keeps its name and changes type. |

### Events that gained typed args

Change the handler signature. In every case the second parameter's type changes and nothing else does, so a handler written as a `(_, _)` lambda already compiles.

| Event | New handler signature | What is new |
| --- | --- | --- |
| `ContentDialog.Opened` | `(object? sender, ContentDialogOpenedEventArgs e)` | nothing yet, the args type exists so data can be added additively |
| `ContentDialog.Closed` | `(object? sender, ContentDialogClosedEventArgs e)` | `e.Result` is the outcome. It was previously reachable only from the task `ShowAsync` returns, which a handler does not hold. |
| `InfoBar.Closed` | `(object? sender, InfoBarClosedEventArgs e)` | `e.Reason` says whether the user or code closed the bar. Setting `IsOpen` to `false` in code now raises `Closed` with `Programmatic`, which previously raised nothing. |
| `TeachingTip.Closed` | `(object? sender, TeachingTipClosedEventArgs e)` | `e.Reason` distinguishes the close button, a light dismiss, and a programmatic close. |
| `TabView.TabCloseRequested` | `(object? sender, TabViewTabCloseRequestedEventArgs e)` | the handler receives the args class directly instead of a base `RoutedEventArgs` that had to be cast. This retypes the registered handler delegate, so an already-compiled consumer binary breaks too, not only source that recompiles. `e.Tab` and `e.Item` are unchanged, and the event still bubbles. |
| `TabViewItem.CloseRequested` | `(object? sender, TabViewTabCloseRequestedEventArgs e)` | as `TabView.TabCloseRequested`, including the binary break. |

Before:

```csharp
private void OnTabCloseRequested(object sender, RoutedEventArgs e)
{
    if (e is not TabViewTabCloseRequestedEventArgs args)
    {
        return;
    }

    Tabs.Items.Remove(args.Tab);
}
```

After:

```csharp
private void OnTabCloseRequested(object sender, TabViewTabCloseRequestedEventArgs e)
{
    Tabs.Items.Remove(e.Tab);
}
```

`InfoBarClosingEventArgs` also gained `Reason`, which is additive: existing `Closing` handlers keep compiling. Its implicit parameterless constructor is gone, which matters only to code that constructed the args itself.

### Members removed

| Was | Is |
| --- | --- |
| `ApplicationThemeManager.Apply(theme, backdrop, updateAccent)` | `Apply(theme, backdrop)`. Delete the third argument. The pipeline always rebuilds using the current accent intent, so the flag had no effect since the single pipeline rewrite. |
| `ApplicationAccentColorManager.ApplyApplicationAccent()` | `ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4))`. |

### Resource keys removed

These nine Colors and their nine `*Brush` twins had no consumer and are gone: `WindowCloseFillColorHover`, `WindowCloseFillColorPressed`, `WindowCloseForegroundHover`, `WindowCloseForegroundPressed`, `ControlStrokeColorTertiary`, `SystemFillColorInformational`, `KeyboardFocusBorderColor`, `NavigationViewContentSeparator`, `TextPlaceholderColor`.

The caption button colours remain published under their WinUI names: `WindowCloseButtonBackgroundPointerOver`, `WindowCloseButtonBackgroundPressed` and `WindowCloseButtonForegroundPointerOver`. The close button template applies that single foreground key on both the pointer-over and the pressed trigger, so it is also the replacement for `WindowCloseForegroundPressed`, which has no separate pressed foreground key of its own. Use `SystemFillColorAttention` in place of `SystemFillColorInformational`, `FocusStrokeColorOuter` in place of `KeyboardFocusBorderColor`, `CardStrokeColorDefaultBrush` in place of `NavigationViewContentSeparatorBrush` (the pane/content seam the `NavigationView` template now paints with), and `TextFillColorSecondaryBrush` in place of `TextPlaceholderColorBrush` (the role `TextBox` and `PasswordBox` placeholders now follow).

**This fails silently**, like the rename below: a consumer's own XAML that still names one of the eighteen keeps building, and the target simply keeps its default value at runtime. Search your own XAML for these names as part of upgrading, not only your build output.

### Resource key renamed

`NavigationViewSelectionIndicatorBrush` is now `NavigationViewSelectionIndicatorForeground`, WinUI's own name for the role. Note the missing `Brush` suffix: it is a brush-only key and that is WinUI's spelling.

**This also fails silently.** A `DynamicResource` reference to a key that no longer exists produces no error and no build failure, the target simply keeps its default, so a selection indicator painted with the old key renders transparent. Search your XAML for the old name.

### Visual defaults changed

Four template changes alter how existing markup renders, with nothing to fix at build time.

`ContentDialog` no longer accents a button unless you ask for one. The primary button carried `Appearance="Accent"` unconditionally; it is now driven by `DefaultButton`, whose default is `ContentDialogButton.None`, so a dialog that does not set `DefaultButton` renders all three buttons standard. This is WinUI's own `DefaultButtonStates` behaviour. To keep the old look, set `DefaultButton="Primary"` on the dialog.

`CheckBox` has a `MinWidth` of 120, the WinUI `CheckBoxMinWidth`. A checkbox in a tight column, a `DataGrid` cell or an item template now reserves that width even when its content is narrower. Set `MinWidth="0"` on the instance or in a derived style to opt out; the library does exactly that for the `TreeView` item template.

The framework `Separator` style is keyed rather than implicit. It was an implicit `TargetType="{x:Type Separator}"` style, which WPF never applied inside a menu anyway; it is now keyed to `{x:Static MenuItem.SeparatorStyleKey}`, the key a `Menu` or `ContextMenu` actually looks up, so menu separators finally get the Fluent look. The consequence is that a bare `<Separator/>` outside a menu falls back to the WPF system theme. Use `fluence:Separator` for standalone separators, which is what the Fluence separator style targets.

Popup surfaces reserve a 16 px shadow gutter and compensate for it with `HorizontalOffset` and `VerticalOffset` of -16. WPF's own offset coercion returns the owner element's value whenever that value is not the property default, so an element that sets `ToolTipService.HorizontalOffset`, `ToolTipService.VerticalOffset`, `ContextMenuService.HorizontalOffset` or `ContextMenuService.VerticalOffset` replaces the compensation instead of adding to it, and its tooltip or context menu lands 16 px further right and down than the same markup used to place it. Subtract 16 from those offsets, or drop them and let the style place the popup.

### Resource keys added, nothing to do

`ContentControlThemeFontFamily` and `ApplicationPageBackgroundThemeBrush` are WinUI's names for `FluentFontFamily` and `ApplicationBackgroundBrush`. Both pairs ship, and both original names are supported for the life of 1.x, so nothing has to change. Prefer the WinUI names in new code.

## Basic Steps

1. Reference `Fluence.Wpf/Fluence.Wpf.csproj` or a local `Fluence.Wpf` package.
2. Add the XML namespace:

    ```xml
    xmlns:fluence="http://schemas.fluencewpf.com"
    ```

3. Initialize resources before showing the first window:

    ```csharp
    Fluence.Wpf.ApplicationThemeManager.Apply(
        Fluence.Wpf.ApplicationTheme.Auto,
        Fluence.Wpf.WindowBackdropType.Mica);
    Fluence.Wpf.ApplicationAccentColorManager.ApplySystemAccent();
    ```

4. Replace shell windows with `fluence:FluenceWindow` where you need Fluent caption buttons, a DWM backdrop, rounded corners, or a title-bar content slot.
5. Replace controls incrementally. Start with leaf controls (`Button`, `TextBox`, `ComboBox`, `ListView`, `InfoBar`, `ProgressBar`), then move larger shell surfaces like `NavigationView` and `TabView`.

## Resource Rules

- Use `DynamicResource` for Fluence brushes, colors, typography, corner radii, and theme-bound values.
- Do not manually merge `Themes/Generic.xaml` when using `ApplicationThemeManager.Apply`; the manager owns the fixed resource dictionary slots.
- Bind to brush resources such as `TextFillColorPrimaryBrush` and `ControlFillColorDefaultBrush` from control templates and application XAML, not to raw color resources.

## PasswordBox

`System.Windows.Controls.PasswordBox` is `sealed`, so the library cannot ship a derived Fluent
control the way it does for `TextBox`. It styles the native control instead. Use the WPF
`PasswordBox` directly; merging the Fluence theme applies the Fluent template implicitly, and the
extras live on the `PasswordBoxExtensions` attached properties.

```xml
<!-- before -->
<fluence:PasswordBox
    PlaceholderText="Password"
    RevealButtonEnabled="True"
    ShowCapsLockIndicator="True"
    ShowPasswordStrength="True" />

<!-- after -->
<PasswordBox
    fluence:PasswordBoxExtensions.PlaceholderText="Password"
    fluence:PasswordBoxExtensions.RevealButtonEnabled="True"
    fluence:PasswordBoxExtensions.ShowCapsLockIndicator="True"
    fluence:PasswordBoxExtensions.ShowPasswordStrength="True" />
```

The old control carried a two-way bindable `Password` dependency property. The native control does
not. `Password` is a plain CLR property by design, and `SecurePassword` is the authoritative store.
Watch for changes with the native `PasswordChanged` routed event.

```csharp
// before
DependencyPropertyDescriptor
    .FromProperty(Fluence.Wpf.Controls.PasswordBox.PasswordProperty, typeof(Fluence.Wpf.Controls.PasswordBox))
    .AddValueChanged(passwordBox, OnPasswordChanged);

// after
passwordBox.PasswordChanged += OnPasswordChanged;
```

Other differences worth knowing:

- `MaxLength` and `PasswordChar` are the native dependency properties now; drop the Fluence copies.
- Reveal is a peek. A read-only, non-focusable overlay paints the plaintext while the real field
    keeps focus and keystrokes, so there is no second editable field and no second tab stop.
- `Fluence.Wpf.Automation.PasswordBoxAutomationPeer` is gone. The native
    `System.Windows.Automation.Peers.PasswordBoxAutomationPeer` already reports an Edit control with
    `IsPassword`, and it sits on the element that actually takes focus.
- To keep the native look for a specific box, clear the decoration with
    `fluence:PasswordBoxExtensions.IsFluentDecorated="False"` and give it your own style.

## Title bar and window controls

`FluenceWindow` owns DWM and caption-button behavior. Use its public properties: `SystemBackdropType`, `CornerStyle`, `ExtendsContentIntoTitleBar`, `TitleBar`, and the caption-button visibility properties. `CaptionButtonChrome` and `WindowPolicy` are internal helpers; do not reference them from application code.

## WinUI-canonical title-bar and caption metrics (visual-only change, no API impact)

The `FluenceWindow` and `TitleBar` controls were re-authored to WinUI-canonical metrics. **There are no public API, dependency property, event, or template-part changes** -- this is a drop-in update; existing XAML and code-behind compile and run unchanged.

Visual changes to be aware of:

- `FluenceWindow.TitleBarHeight` default changed from 68 to 48 px (the WinUI 3 canonical expanded title-bar height). Any explicit `TitleBarHeight="42"` (or other explicit value) in your XAML is unaffected.
- Minimize, maximize, and restore caption buttons are now 46 px wide (was approximately 64 px) and stretch to the full title-bar height instead of a fixed 32 px with top alignment. The close button is unchanged.
- Caption-button hover and press fills changed from a strong inverted fill (`ControlStrongFillColorDefaultBrush` background, `TextFillColorInverseBrush` glyph) to WinUI-canonical subtle fills (`SubtleFillColorSecondaryBrush` hover, `SubtleFillColorTertiaryBrush` press; glyph keeps its normal `TextFillColorPrimaryBrush` color). The close button hover/press colors are unchanged.
- `TitleBar` back and pane-toggle button slot width changed from 42 to 40 px, matching the WinUI 3 canonical hit-area width.

If your application relied on the exact 68 px default title-bar height or the previous caption-button sizing, set `TitleBarHeight` and caption-button widths explicitly in your `FluenceWindow` XAML.

## Verification

After migrating a page or shell surface, run the gallery and check Light, Dark, High Contrast, accent changes, and the target backdrop mode. For source builds, run:

```powershell
dotnet build Fluence.Wpf.sln -c Debug
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-not-trait "Category=Screenshots" --no-ansi --progress off
```
