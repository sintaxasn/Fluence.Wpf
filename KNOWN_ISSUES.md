# Known issues

These issues track limitations in Fluence.Wpf and gaps in WinUI behavior that need further investigation or implementation.

Last reviewed: 2026-11-19

Start with [AGENTS.md](AGENTS.md) and the nearest guidance for files you change. Open the technical brief for the assigned issue. Reproduce it with the stated runtime and settings, check the linked tests, and record whether your evidence came from source, automation, or the native desktop. Use [CONTRIBUTING.md](CONTRIBUTING.md) for verification commands. Update the status when the documented completion criteria are met.

Start at the top of the list when choosing work. **P1** covers credential handling, host stability, state correctness, and accessibility. **P2** covers shared rendering and layout work and prerequisites for reliable verification. **P3** covers targeted visual and interaction differences. Dependency links identify the work that must come first.

## Fluent password fields can put plaintext in managed memory

Priority: P1.

Summary: Routine Fluent decoration can read the plaintext password even when the reveal button and strength display are hidden.

Affects: Native WPF `PasswordBox` with Fluent decoration enabled.

Status: Investigating.

Workaround: Use a native WPF password field that opts out of Fluence decoration with `PasswordBoxExtensions.IsFluentDecorated="False"` before entry. Read `SecurePassword` instead of `Password`. See [Microsoft's password-handling guidance](https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.passwordbox.securepassword).

[Technical brief](docs/known-issues/reliability-and-accessibility.md#fluent-password-fields-can-put-plaintext-in-managed-memory)

Last updated: 2026-11-19.

## An embedded PowerShell UI host can fail during shutdown

Priority: P1.

Summary: Leaving an owned WPF dispatcher running during runspace disposal can trigger a shutdown assertion.

Affects: Inline STA, child-runspace, and custom PowerShell hosts.

Status: Investigating.

Workaround: Finish UI work and close owned windows. Shut down the dispatcher on the STA thread that owns it before disposing that runspace. Leave shared dispatcher shutdown to its owner.

[Technical brief](docs/known-issues/reliability-and-accessibility.md#an-embedded-powershell-ui-host-can-fail-during-shutdown)

Last updated: 2026-11-19.

## Checking a collapsed TreeView branch can skip its children

Priority: P1.

Summary: Multiple-selection cascading reaches existing containers, so unchecked descendants can appear when a collapsed data-bound branch expands.

Affects: Data-bound `TreeView` in `Multiple` selection mode.

Status: Investigating.

Workaround: Keep hierarchy-wide checked state in the data model. Bind the state of each item when its container is created.

Unblocks: [TreeView multiple selection can realize every root item](#treeview-multiple-selection-can-realize-every-root-item) needs selection state that can outlive a generated container.

[Technical brief](docs/known-issues/control-behavior.md#checking-a-collapsed-treeview-branch-can-skip-its-children)

Last updated: 2026-11-19.

## TreeView check state can be written back during template replacement

Priority: P1.

Summary: Automatic TwoWay binding can write a detached container's default checked state into the model when the template is replaced.

Affects: `TreeView` in `Multiple` mode with automatically updated checked-state bindings.

Status: Investigating.

Workaround: Own the state in the data model and set `UpdateSourceTrigger=Explicit` for checked-state bindings. Update the source on application-owned selection changes.

Unblocks: [TreeView multiple selection can realize every root item](#treeview-multiple-selection-can-realize-every-root-item) needs a stable binding contract when containers are recreated or recycled.

[Technical brief](docs/known-issues/control-behavior.md#treeview-check-state-can-be-written-back-during-template-replacement)

Last updated: 2026-11-19.

## DataGrid can briefly report a rejected selection in native collections

Priority: P1.

Summary: WPF can add a rejected native row or cell selection before Fluence clears it. The single-selection properties stay empty.

Affects: `DataGrid` with `IsItemSelectable=False`. Current-cell state and programmatic editing remain available.

Status: Investigating.

Workaround: Use the single-selection properties or application-owned state when selection is disabled. Avoid acting on transient changes to the native selection collections. Call `BeginEdit()` when the application owns the edit action.

[Technical brief](docs/known-issues/control-behavior.md#datagrid-can-briefly-report-a-rejected-selection-in-native-collections)

Last updated: 2026-11-19.

## A screen reader can announce ContentDialog as a window

Priority: P1.

Summary: The peer reports a Window role. The overlay contains tab focus and announces its title through a live region.

Affects: `ContentDialog` and assistive technology that uses the WPF peer's dialog metadata.

Status: Investigating.

Workaround: In an application that targets WPF with `AutomationProperties.IsDialog`, set it on the dialog. Use a descriptive dialog title on legacy hosts.

[Technical brief](docs/known-issues/reliability-and-accessibility.md#a-screen-reader-can-announce-contentdialog-as-a-window)

Last updated: 2026-11-19.

## Screen readers can miss structured item positions on older WPF

Priority: P1.

Summary: Legacy WPF can omit structured item position and count. A pager button's name can still contain an ordinal.

Affects: Framework 4.7.2 item hosts and the custom `PipsPager` button panel.

Status: Investigating.

Workaround: Use newer WPF for automatic metadata on standard item controls, or use the documented accessibility switches when a legacy-targeted app runs on .NET Framework 4.8. See [Microsoft's WPF accessibility guidance](https://learn.microsoft.com/en-us/dotnet/framework/whats-new/whats-new-in-accessibility#whats-new-in-accessibility-in-net-framework-48).

[Technical brief](docs/known-issues/reliability-and-accessibility.md#screen-readers-can-miss-structured-item-positions-on-older-wpf)

Last updated: 2026-11-19.

## TextBox and PasswordBox need a separate field label

Priority: P1.

Summary: The default templates have no header slot. Fluence `TextBox` supplies `HelperText`.

Affects: Fluence `TextBox` and decorated native WPF `PasswordBox` in labeled forms.

Status: Investigating.

Workaround: Add a visible label, connect it using `AutomationProperties.LabeledBy`, and use `HelperText` on Fluence `TextBox` or visible help with `AutomationProperties.HelpText` on native `PasswordBox`.

[Technical brief](docs/known-issues/reliability-and-accessibility.md#textbox-and-passwordbox-need-a-separate-field-label)

Last updated: 2026-11-19.

<a id="in-app-acrylic-source-and-rendering-limits"></a>

## Acrylic uses a solid fill with an unsupported source

Priority: P2.

Summary: Live blur requires an explicit source that is a separate visual subtree in the same WPF window.

Affects: In-app acrylic over a `Popup`, desktop, native `HwndHost`, detached or cyclic source, unsupported transform, or nonrectangular source clip.

Status: Investigating.

Workaround: Place the source and `AcrylicElement` in separate subtrees of the same WPF window. Use a solid fill for a separate popup. See the [in-app acrylic examples](docs/controls.md#acrylic-materials).

Verification prerequisite: establish source ownership and overlap before diagnosing [Large or competing acrylic surfaces can become opaque](#large-or-competing-acrylic-surfaces-can-become-opaque) or [Acrylic blur can look sharper at higher display scales](#acrylic-blur-can-look-sharper-at-higher-display-scales).

[Technical brief](docs/known-issues/material-and-windowing.md#acrylic-uses-a-solid-fill-with-an-unsupported-source)

Last updated: 2026-11-19.

## In-app acrylic can become opaque under rendering restrictions

Priority: P2.

Summary: Acrylic uses opaque fallback when rendering or system policy disallows live material. High Contrast uses the system Window color.

Affects: Software rendering, High Contrast, transparency settings, unreadable power status, missing blend resources, and device or shader limitations.

Status: Investigating.

Workaround: Use a hardware-rendered session with transparency effects enabled where appropriate. Choose an opaque `FallbackColor` in a custom or cloned recipe for modes other than High Contrast.

[Technical brief](docs/known-issues/material-and-windowing.md#in-app-acrylic-can-become-opaque-under-rendering-restrictions)

Last updated: 2026-11-19.

## Windows Energy Saver can leave in-app acrylic active

Priority: P2.

Summary: The policy listens to Battery Saver. A separate Energy Saver state may leave the blur active.

Affects: Windows systems that expose Energy Saver separately from Battery Saver.

Status: Investigating.

Workaround: Assign a custom or cloned recipe with `AlwaysUseFallback=True` when the application needs to disable live acrylic regardless of power mode.

[Technical brief](docs/known-issues/material-and-windowing.md#windows-energy-saver-can-leave-in-app-acrylic-active)

Last updated: 2026-11-19.

## Large or competing acrylic surfaces can become opaque

Priority: P2.

Summary: Two live surfaces share a padded physical-pixel budget. A wide blur can move a full-size pane over the limit.

Affects: Large or competing live acrylic surfaces, especially on scaled displays.

Status: Investigating.

Workaround: Reduce the acrylic area, use one live surface, or use an opaque fallback for larger surfaces.

Unblocks: [Acrylic blur can look sharper at higher display scales](#acrylic-blur-can-look-sharper-at-higher-display-scales) needs bounded allocation and timing measurements before changing blur stages.

[Technical brief](docs/known-issues/material-and-windowing.md#large-or-competing-acrylic-surfaces-can-become-opaque)

Last updated: 2026-11-19.

## Acrylic blur can look sharper at higher display scales

Priority: P2.

Summary: Both Gaussian stages can hit WPF's per-pass radius cap at high DPI.

Affects: In-app acrylic at higher display scales. The Default Light comparison at 150% met the color and cyan-edge tolerances.

Status: Investigating.

Workaround: Use the solid fallback where a consistent appearance matters across display scales.

[Technical brief](docs/known-issues/material-and-windowing.md#acrylic-blur-can-look-sharper-at-higher-display-scales)

Last updated: 2026-11-19.

## RenderTargetBitmap screenshots omit window backdrop effects

Priority: P2.

Summary: A visual-tree snapshot omits the DWM-composed material behind an HWND.

Affects: `RenderTargetBitmap` captures and the ordinary gallery screenshot route.

Status: Investigating.

Workaround: Capture the composed on-screen window when verifying a native window backdrop.

Verification prerequisite: use a composed-window capture for [Window acrylic uses a different effect on Windows 10](#window-acrylic-uses-a-different-effect-on-windows-10) and [Mica layer colors can vary by display path](#mica-layer-colors-can-vary-by-display-path).

[Technical brief](docs/known-issues/material-and-windowing.md#rendertargetbitmap-screenshots-omit-window-backdrop-effects)

Last updated: 2026-11-19.

## Window acrylic uses a different effect on Windows 10

Priority: P2.

Summary: Windows 10 can take the legacy accent route, which uses fixed alpha and simpler blur during window moves or resizes.

Affects: `FluenceWindow` on eligible Windows 10 builds. Native tint, drag, and glass-frame qualification is open.

Status: Investigating.

Workaround: Use `SystemBackdropType="None"` with a solid background when the window must look consistent across systems.

[Technical brief](docs/known-issues/material-and-windowing.md#window-acrylic-uses-a-different-effect-on-windows-10)

Last updated: 2026-11-19.

## Mica layer colors can vary by display path

Priority: P2.

Summary: An SDR 10-bit path with advanced color off showed coarse alpha blending over Mica. Results depend on the reported display path.

Affects: Window Mica and translucent content layers on affected Windows display paths.

Status: Investigating.

Workaround: Use a solid content layer or set `SystemBackdropType="None"` when exact layer colors matter.

[Technical brief](docs/known-issues/material-and-windowing.md#mica-layer-colors-can-vary-by-display-path)

Last updated: 2026-11-19.

## TreeView multiple selection can realize every root item

Priority: P2.

Summary: The stock Multiple template uses physical scrolling even when a virtualizing panel is requested.

Affects: Large `TreeView` data sets in `Multiple` selection mode.

Status: Investigating.

Workaround: Use `Single` or `None` with `VirtualizingPanel.IsVirtualizing=True` when virtualization is needed. Limit the loaded branches when multiple selection is needed.

Depends on: [Checking a collapsed TreeView branch can skip its children](#checking-a-collapsed-treeview-branch-can-skip-its-children) and [TreeView check state can be written back during template replacement](#treeview-check-state-can-be-written-back-during-template-replacement) before enabling virtualization for Multiple selection.

[Technical brief](docs/known-issues/control-behavior.md#treeview-multiple-selection-can-realize-every-root-item)

Last updated: 2026-11-19.

## Hidden NavigationView items can keep a stale width

Priority: P2.

Summary: The strip remeasures supported changes such as content replacement or direct TextBlock text. Hidden nested visuals can keep a stale cached width.

Affects: `NavigationView` Top mode with overflowed custom visual content.

Status: Investigating.

Workaround: Replace the hidden item's `Content` or update a direct `TextBlock.Text` when its label changes. Set `AutomationProperties.Name` when a custom visual needs a readable overflow label.

[Technical brief](docs/known-issues/control-behavior.md#hidden-navigationview-items-can-keep-a-stale-width)

Last updated: 2026-11-19.

## Adding a DataGrid group style after load can miss the first group level

Priority: P2.

Summary: A grid that loads without an application group style inserts Fluence's default first.

Affects: Grouped `DataGrid` when the first app-defined style is appended after load.

Status: Investigating.

Workaround: Supply the first group style in XAML or before the grid loads.

[Technical brief](docs/known-issues/control-behavior.md#adding-a-datagrid-group-style-after-load-can-miss-the-first-group-level)

Last updated: 2026-11-19.

## Grouping a DataGrid can realize every row

Priority: P2.

Summary: Grouping requires a virtualization opt-in to keep the realized row count bounded.

Affects: Large grouped `DataGrid` sources with standard WPF virtualization settings.

Status: Investigating.

Workaround: Enable row virtualization and set `VirtualizingPanel.IsVirtualizingWhenGrouping=True`.

[Technical brief](docs/known-issues/control-behavior.md#grouping-a-datagrid-can-realize-every-row)

Last updated: 2026-11-19.

## AutoSuggestBox raises TextChanged for each edit

Priority: P2.

Summary: The synchronous event can start multiple requests during a fast edit sequence.

Affects: `AutoSuggestBox.TextChanged` handlers that query a remote or slow provider.

Status: Investigating.

Workaround: Debounce the lookup in the handler and cancel or discard stale results.

[Technical brief](docs/known-issues/control-behavior.md#autosuggestbox-raises-textchanged-for-each-edit)

Last updated: 2026-11-19.

## TabView tabs cannot be reordered or torn off by dragging

Priority: P3.

Summary: Built-in tabs can be closed and added. Drag reordering and tear-out are missing.

Affects: Fluence `TabView` tab strip.

Status: Investigating.

Workaround: Provide application commands to reorder the tab collection or move tab content to another window. Provide custom drag handling if the workflow requires it.

[Technical brief](docs/known-issues/control-behavior.md#tabview-tabs-cannot-be-reordered-or-torn-off-by-dragging)

Last updated: 2026-11-19.

## Selected text can look different from WinUI

Priority: P3.

Summary: Fluence supplies the selection background and WPF draws the selected glyphs and opacity.

Affects: Fluence `TextBox`, native `PasswordBox`, and the `NumberBox` inner editor.

Status: Investigating.

Workaround: On WPF versions that support `SelectionTextBrush`, set it with non-adorner selection enabled. For `NumberBox`, style the inner editor. On .NET Framework 4.7.2, use the native selection appearance. See [Microsoft's WPF selection guidance](https://learn.microsoft.com/en-us/dotnet/framework/whats-new/whats-new-in-accessibility#whats-new-in-accessibility-in-net-framework-48).

[Technical brief](docs/known-issues/visual-parity.md#selected-text-can-look-different-from-winui)

Last updated: 2026-11-19.

## Picker text changes color as a whole row while scrolling

Priority: P3.

Summary: A whole picker row changes foreground as it crosses the accent band.

Affects: Scrolling `DatePicker` and `TimePicker` looping selectors.

Status: Investigating.

Workaround: No workaround.

[Technical brief](docs/known-issues/visual-parity.md#picker-text-changes-color-as-a-whole-row-while-scrolling)

Last updated: 2026-11-19.

## ColorPicker has one spectrum shape and layout

Priority: P3.

Summary: The picker uses a rectangular saturation-by-value spectrum and a hue slider.

Affects: ColorPicker layouts that need a ring, another orientation, component order, or custom range.

Status: Investigating.

Workaround: Use the supported channel inputs, or provide a custom picker when a different spectrum or constrained ranges are required.

[Technical brief](docs/known-issues/visual-parity.md#colorpicker-has-one-spectrum-shape-and-layout)

Last updated: 2026-11-19.

## ColorPicker hex edits apply when committed

Priority: P3.

Summary: Hex edits commit on Enter or focus loss. Hue text also accepts 360 as equivalent to zero.

Affects: `ColorPicker` text entry and app logic that expects live updates.

Status: Investigating.

Workaround: Use the spectrum or sliders for live updates. Press Enter to apply a hex edit while keeping focus.

[Technical brief](docs/known-issues/visual-parity.md#colorpicker-hex-edits-apply-when-committed)

Last updated: 2026-11-19.

## ContentDialog always dims surrounding content and uses bounded sizing

Priority: P3.

Summary: The stock overlay creates a smoke layer and uses bounded dialog sizing.

Affects: `ContentDialog` hosts that need transparent smoke or full-window layout.

Status: Investigating.

Workaround: Override `SmokeFillColorDefaultBrush` in the enclosing window or application resources to adjust dimming. Use a separate dialog host for full-window layout.

[Technical brief](docs/known-issues/visual-parity.md#contentdialog-always-dims-surrounding-content-and-uses-bounded-sizing)

Last updated: 2026-11-19.

## BreadcrumbBar overflow keeps the original trail scrollable

Priority: P3.

Summary: Fluence scrolls the trail and opens the full list from a separate overflow button.

Affects: Narrow `BreadcrumbBar` layouts.

Status: Investigating.

Workaround: Use the overflow button to reach any location in the trail.

[Technical brief](docs/known-issues/visual-parity.md#breadcrumbbar-overflow-keeps-the-original-trail-scrollable)

Last updated: 2026-11-19.

## PipsPager arrows do not shrink when pressed

Priority: P3.

Summary: The default previous and next glyphs change their colors when pressed and keep the same scale.

Affects: Stock `PipsPager` navigation buttons.

Status: Investigating.

Workaround: Customize the pager's navigation button template when that press animation is required.

[Technical brief](docs/known-issues/visual-parity.md#pipspager-arrows-do-not-shrink-when-pressed)

Last updated: 2026-11-19.

## Stock list, tree, and tab item backgrounds lack distinct press feedback

Priority: P3.

Summary: Stock item backgrounds have hover and selection styling but no distinct pressed background.

Affects: Default list box, list view, tree view, and tab item backgrounds.

Status: Investigating.

Workaround: Use an application item template and supply the pointer state for the pressed visual.

[Technical brief](docs/known-issues/visual-parity.md#stock-list-tree-and-tab-item-backgrounds-lack-distinct-press-feedback)

Last updated: 2026-11-19.

## NumberBox Compact shows spin buttons inline

Priority: P3.

Summary: Hover or keyboard focus reveals an inline spin panel.

Affects: `NumberBox` Compact mode.

Status: Investigating.

Workaround: Set `SpinButtonPlacementMode=Inline` if the spin buttons should stay visible.

[Technical brief](docs/known-issues/visual-parity.md#numberbox-compact-shows-spin-buttons-inline)

Last updated: 2026-11-19.

## TreeView rows can be taller than WinUI rows

Priority: P3.

Summary: The default Fluence row minimum is 32 DIPs. The comparable WinUI resource uses 28 DIPs.

Affects: Stock `TreeViewItem` geometry.

Status: Investigating.

Workaround: Apply an item style with the row height your layout requires.

[Technical brief](docs/known-issues/visual-parity.md#treeview-rows-can-be-taller-than-winui-rows)

Last updated: 2026-11-19.

## Selected TabView tabs have square lower corners

Priority: P3.

Summary: The default selected tab uses square lower corners at its join with the content pane.

Affects: Stock `TabViewItem` geometry.

Status: Investigating.

Workaround: Supply a custom tab item template if the curved join is required.

[Technical brief](docs/known-issues/visual-parity.md#selected-tabview-tabs-have-square-lower-corners)

Last updated: 2026-11-19.
