# Visual parity investigation briefs

Use the entry linked from [Known issues](../../KNOWN_ISSUES.md). Compare the named control state at the same width, DPI, theme, and input state. Prefer geometry and state assertions for layout and triggers, then check rendered frames for glyph colors, clipping, or layering.

## Selected text can look different from WinUI

**Context and cause.** The Fluent styles assign `TextControlSelectionHighlightBrush` to `SelectionBrush`. WPF renders the selected run, so the fill and selected glyph foreground have different control paths. The legacy adorner draws over the text and defaults to 0.4 opacity. The non-adorner renderer draws behind the text and defaults to 1. Current selection behavior also depends on the application's WPF compatibility settings and any local brush or opacity overrides. See [TextBox](../../Fluence.Wpf/Themes/Controls/TextBox.xaml#L21), [PasswordBox](../../Fluence.Wpf/Themes/Controls/PasswordBox.xaml#L27), [NumberBox's inner editor](../../Fluence.Wpf/Themes/Controls/NumberBox.xaml#L125-L136), and [WPF selection properties](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Primitives/TextBoxBase.cs#L729-L766).

**Previous work.** The audit traced the shared background token and corrected the claim that all hosts use translucent selection. The styles currently supply the background token and leave glyph rendering to WPF.

**Investigate.** Select part of an editable field with keyboard and pointer input in TextBox, a masked PasswordBox using dummy text, and NumberBox. Capture Light, Dark, and High Contrast with the selection active and inactive. Inspect the effective brush and opacity on the editor itself. NumberBox hosts the editor in `PART_TextBox`. Check the availability of `SelectionTextBrush` at the compile target before attempting a template setter. The .NET Framework 4.7.2 reference assemblies predate the WPF API. An opaque adorner can cover the glyphs, so test both renderers in the hosts that support them. See [Microsoft's WPF accessibility changes](https://learn.microsoft.com/en-us/dotnet/framework/whats-new/whats-new-in-accessibility#whats-new-in-accessibility-in-net-framework-48).

**Done when.** Selected glyphs and background meet the intended contrast in each supported host, with caret movement and selected ranges preserved. Shared templates still load on .NET Framework 4.7.2 and the modern targets.

## Picker text changes color as a whole row while scrolling

**Context and cause.** DatePicker and TimePicker put a centered accent band behind their selector columns. The shared `LoopingSelectorList` container sets `PickerSelectedForegroundBrush` for the full selected item's content presenter. As a row crosses the band boundary its glyphs retain one color across both regions. WinUI clips a second rendering of each selector's foreground to the band. See the [DatePicker band](../../Fluence.Wpf/Themes/Controls/DatePicker.xaml#L205-L234), [TimePicker band](../../Fluence.Wpf/Themes/Controls/TimePicker.xaml#L205-L234), [shared row template](../../Fluence.Wpf/Themes/Controls/LoopingSelectorList.xaml#L42-L66), and [WinUI's clipped overlay](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/CommonStyles/DatePicker_themeresources.xaml#L289-L297).

**Previous work.** The existing band and selected-foreground state reproduce the resting row pattern. The visual audit confirmed the implementation difference from templates. Existing accept and cancel tests cover pending-value behavior and focus.

**Investigate.** Open either flyout, move one column, and capture a frame with a glyph crossing the top or bottom edge of the highlight. Trace the visible row and scroll offset in [LoopingSelectorList](../../Fluence.Wpf/Controls/LoopingSelectorList.cs). Try clipping a synchronized, noninteractive rendering of the glyph foreground to the stationary band in shared picker infrastructure. Keep the two glyph origins aligned through scrolling and layout rounding. Preserve one hit target and automation peer per row.

**Done when.** Glyphs render with the appropriate foreground on each side of the band during motion in both controls. Verify direction and theme changes, then run [DatePicker tests](../../Fluence.Wpf.Tests/Control/DatePickerTests.cs) and [TimePicker tests](../../Fluence.Wpf.Tests/Control/TimePickerTests.cs) for focus, accept, and cancel.

## ColorPicker has one spectrum shape and layout

**Context and cause.** `UpdateSpectrumBitmap` builds a square saturation/value map for the current hue and caches it by hue. Point-to-color conversion and thumb placement each embed that same axis mapping. The template provides a separate hue slider as the third channel. A shape, axis, or limits change must update rendering and both directions of the point-to-color mapping. See [spectrum bitmap](../../Fluence.Wpf/Controls/ColorPicker.cs#L701-L739), [thumb placement](../../Fluence.Wpf/Controls/ColorPicker.cs#L803-L832), [pointer mapping](../../Fluence.Wpf/Controls/ColorPicker.cs#L900-L927), and [template](../../Fluence.Wpf/Themes/Controls/ColorPicker.xaml).

**Previous work.** The current tests cover the 256 by 256 bitmap, saturation/value edits with hue preserved, and keyboard movement. The audit identified the fixed map as the remaining limitation. WinUI uses the chosen spectrum components to control which channel the separate slider edits. See [ColorPicker tests](../../Fluence.Wpf.Tests/Control/ColorPickerTests.cs#L112) and [WinUI's third-channel logic](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/ColorPicker/ColorPicker.cpp#L947-L1003).

**Investigate.** Drag across the spectrum and check that only saturation and value change. If adding an alternate layout, extract a single mapping used by bitmap generation, pointer and keyboard input, and thumb placement. Include shape, components, and active limits in bitmap invalidation. Test seams, bounds, resize, and retemplating. Treat range clamping as a color-value decision shared with sliders and editors.

**Done when.** Each supported mode keeps displayed pixels, thumb position, pointer input, keyboard input, and editable channels consistent. The default map, alpha handling, and focus behavior retain their tested behavior.

## ColorPicker hex edits apply when committed

**Context and cause.** The hex editor calls `CommitHexText` on Enter and lost keyboard focus. It publishes valid RGB or ARGB directly through the `Color` dependency property, normalizes the text, and restores the current value for invalid input. RGB and HSV channel editors use a separate live update path. The Hue editor accepts 360 because the model and slider use an inclusive upper bound. See [hex event handlers](../../Fluence.Wpf/Controls/ColorPicker.cs#L1049-L1086) and [HSV input](../../Fluence.Wpf/Controls/ColorPicker.cs#L1153-L1177).

**Previous work.** The hex-entry regression pins exact color on Enter and recovery from invalid text. The focused-editor and counter regressions cover external color updates, live theme and accent changes, and the present input length. See [commit test](../../Fluence.Wpf.Tests/Control/ColorPickerTests.cs#L185), [focused editor test](../../Fluence.Wpf.Tests/Control/ColorPickerTests.cs#L529), and [hex counter test](../../Fluence.Wpf.Tests/Control/ColorPickerTests.cs#L968).

**Investigate.** Enter a complete value without moving focus, then compare the swatch and `ColorChanged` before and after Enter or Tab. Repeat with incomplete text, invalid paste, and alpha enabled and disabled. If introducing live updates, separate valid value publication from final normalization so partially typed input and caret position survive. Reuse the existing feedback guards for control-originated text. Verify the reference's event timing before changing any default.

**Done when.** The specified edit timing is covered, including paste, focus loss, invalid input, and external color changes. The exact typed RGB survives, alpha rules hold, and one effective value change produces one event.

## ContentDialog always dims surrounding content and uses bounded sizing

**Context and cause.** `ShowAsync` mounts a dialog in the owner's overlay host or an adorner. `BuildOverlayRoot` creates a sibling smoke border before attaching the dialog and resolves its fill from `SmokeFillColorDefaultBrush`. The dialog style supplies centered alignment and default maximum dimensions of 548 by 756 DIPs. Investigate smoke in the overlay host and dimensions in the dialog style. See [host selection](../../Fluence.Wpf/Controls/ContentDialog.cs#L446-L492), [overlay construction](../../Fluence.Wpf/Controls/ContentDialog.cs#L1093-L1113), and [style sizing](../../Fluence.Wpf/Themes/Controls/ContentDialog.xaml#L33-L46).

**Previous work.** The max-height limit was added to bound long content. Tests cover overlay creation, smoke resource changes, owner isolation, modal input, and open and close behavior. A transparent smoke brush changes visual dimming while the modal overlay continues to block underlying input. See [ContentDialog tests](../../Fluence.Wpf.Tests/Control/ContentDialogTests.cs#L506) and [smoke and pointer tests](../../Fluence.Wpf.Tests/Control/ContentDialogTests.cs#L740).

**Investigate.** Open a large dialog over contrasting content in both FluenceWindow and a plain WPF Window. Use the visual tree to inspect the smoke sibling, host bounds, and effective sizes. If adding a mode, agree which combinations affect dimming, available layout size, and modal input. Apply it at the shared host/style boundary and compare [WinUI's dialog visual states](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/CommonStyles/ContentDialog_themeresources.xaml).

**Done when.** Tests distinguish the selected size and dimming modes and keep focus trapping, input blocking, one active dialog per owner, and cleanup on failed show and close.

## BreadcrumbBar overflow keeps the original trail scrollable

**Context and cause.** The template puts the trail in a horizontal viewport with a separate overflow button. The scroll handler compares extent to available width and brings the focused or current crumb into view when the extent or viewport changes. Opening the overflow menu rebuilds its trail from the source items. See [template layout](../../Fluence.Wpf/Themes/Controls/BreadcrumbBar.xaml#L211-L230) and [overflow logic](../../Fluence.Wpf/Controls/BreadcrumbBar.cs#L191-L283).

**Previous work.** The narrow-width fix kept the current location reachable, exposed the full trail in the menu, and preserved the originating item and index on invocation. Tests cover left-to-right and right-to-left resize, readable menu names, custom content, and detaching the previous menu on template replacement. See [BreadcrumbBar tests](../../Fluence.Wpf.Tests/Control/BreadcrumbBarTests.cs#L362).

**Investigate.** Load enough long crumbs to overflow, narrow and expand the owner, focus a crumb, and choose a menu item. If adopting the WinUI presentation, track which leading crumbs are omitted at the available width, keep original indices, and make the ellipsis reachable in keyboard order. Check that a UIElement used as a crumb is not reparented to build its menu representation. Use [WinUI BreadcrumbBar overflow](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/breadcrumbbar#breadcrumb-bar-ui) as the interaction reference.

**Done when.** Width changes elide and restore the intended leading segment, the current crumb and menu stay reachable in both directions, and item identity, labels, focus, and index reporting survive.

## PipsPager arrows do not shrink when pressed

**Context and cause.** `PipsPagerNavigationButtonStyle` handles `IsPressed` with a foreground change. The previous and next button templates use that style and have a centered glyph presenter. WinUI applies a scale of 0.875 to the glyph while pressed. See [WPF button style](../../Fluence.Wpf/Themes/Controls/PipsPager.xaml#L166-L198) and [WinUI pressed state](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/PipsPager/PipsPager_themeresources.xaml#L144-L162).

**Previous work.** The audit narrowed this to arrow press scale. The existing pip size morph and scroll behavior are separate features with their own regression cases. Navigation tests cover clicking and respecting first and last page bounds. See [navigation tests](../../Fluence.Wpf.Tests/Control/PipsPagerTests.cs#L193) and [pip morph test](../../Fluence.Wpf.Tests/Control/PipsPagerTests.cs#L796).

**Investigate.** Select a middle page and hold an enabled arrow, then release or drag outside. Inspect the navigation glyph's transform and layout slot. Apply a centered render transform to the content presenter for press feedback and keep the button's measured size and hit target intact. Check both directions, vertical orientation, keyboard press, disabled bounds, and reduced motion.

**Done when.** The glyph scales on press and returns to rest on release or cancellation. Layout dimensions, pip selection, and navigation remain stable.

## Stock list, tree, and tab item backgrounds lack distinct press feedback

**Context and cause.** The item templates combine hover, selection, and disabled visual states. The container base types do not supply the `ButtonBase.IsPressed` state available to actual buttons. Tab close, add, and scroll buttons already implement their own pressed states. See [ListBox](../../Fluence.Wpf/Themes/Controls/ListBox.xaml#L59), [ListView](../../Fluence.Wpf/Themes/Controls/ListView.xaml#L91), [TreeView](../../Fluence.Wpf/Themes/Controls/TreeView.xaml#L179), and [TabView](../../Fluence.Wpf/Themes/Controls/TabView.xaml#L152-L182).

**Previous work.** The template audit narrowed the gap to item background feedback. Existing tree and tab tests already cover selection, keyboard input, and container reuse or retemplating. The [BreadcrumbBar press tests](../../Fluence.Wpf.Tests/Control/BreadcrumbBarTests.cs#L250) show a local pattern for testing an explicit container press state.

**Investigate.** Hold a pointer on a selected and an unselected item, then release inside or outside. Compare the state transitions to the intended WinUI item presenter. Evaluate scoped item-level press tracking that clears on release, lost capture, unload, disable, and recycling. Decide event handling so normal selection, drag initiation, and nested buttons continue to receive the inputs they need.

**Done when.** Press feedback is visible for the intended states and clears along all exit paths. Selection, keyboard navigation, container reuse, and nested button clicks remain covered.

## NumberBox Compact shows spin buttons inline

**Context and cause.** The template has a single in-row spin panel shared by placement modes. Compact mode leaves the panel in layout, sets opacity to zero and disables hit testing at rest, and reveals it through hover or focus triggers. The WinUI Compact template uses a separate popup presentation. See [WPF placement triggers](../../Fluence.Wpf/Themes/Controls/NumberBox.xaml#L227-L248) and [WinUI NumberBox template](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/NumberBox/NumberBox.xaml#L115-L166).

**Previous work.** The current regression verifies that the compact panel reserves width and that invisible controls do not take hits, then exercises keyboard focus. Earlier focus-border work kept parsing and stepping on the inner editor. See [compact behavior test](../../Fluence.Wpf.Tests/Control/NumberBoxTests.cs#L270).

**Investigate.** Compare unfocused, hovered, and keyboard-focused states at a fixed field width. If adding a popup, explicitly decide the focus and capture path, close behavior, anchor placement, and whether the editor can reclaim reserved width without shifting on open. Validate both placement modes and check the [acrylic source requirements](../../KNOWN_ISSUES.md#acrylic-uses-a-solid-fill-with-an-unsupported-source) before designing popup appearance.

**Done when.** The intended Compact presentation works with mouse, keyboard, and focus transitions. Inline and Hidden modes, stepping, numeric edits, and disabled state pass their existing tests.

## TreeView rows can be taller than WinUI rows

**Context and cause.** The item style uses a 32 DIP minimum and template-binds it into the row border. The template comment attributes it to adaptation of WinUI's 28 DIP minimum for WPF padding. The upstream style has separate presenter margin and padding, so compare measured row pitch and content bounds as well as the nominal minimum. See [WPF row style](../../Fluence.Wpf/Themes/Controls/TreeView.xaml#L12-L38) and [WinUI row resources](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/TreeView/TreeView_themeresources.xaml#L36-L42).

**Previous work.** The WPF template already has the audited selection indicator geometry and the tests cover indicator alignment across depth and its removal in Multiple mode. Baseline snapshots should retain those working states. See [indicator template](../../Fluence.Wpf/Themes/Controls/TreeView.xaml#L132-L159) and [indicator tests](../../Fluence.Wpf.Tests/Control/TreeViewTests.cs#L254).

**Investigate.** Measure identical single-line roots and children in Single and Multiple modes. Compare the row plate, text, expansion glyph, and checkbox bounds, including layout rounding at 100% and 150% scaling. Larger content can raise the actual height. Decide which minimum and inset belong at the style level before updating baseline measurements.

**Done when.** The chosen row metrics match the documented reference states without clipping content or shifting indicator alignment across indentation levels. Confirm keyboard, selection, and opt-in virtualization regressions.

## Selected TabView tabs have square lower corners

**Context and cause.** The selected plate is a border with rounded top corners, a negative bottom margin, and a missing bottom stroke so it joins the content region. Its bottom corners are square. The WinUI tab uses a calculated path geometry for the lower join. See [selected WPF plate](../../Fluence.Wpf/Themes/Controls/TabView.xaml#L55-L68) and [WinUI tab geometry](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/TabView/TabViewItem.cpp#L75-L110).

**Previous work.** Header metrics and inter-tab separators are already covered. The separator is centered in the WPF gap and hidden next to the selected tab. Other regressions protect selected-header reveal, overflow scrolling, and right-to-left layout. See [header metrics](../../Fluence.Wpf.Tests/Control/TabViewTests.cs#L638), [separator tests](../../Fluence.Wpf.Tests/Control/TabViewTests.cs#L701), and [overflow tests](../../Fluence.Wpf.Tests/Control/TabViewTests.cs#L59).

**Investigate.** Select first, middle, and last tabs under light and dark themes. Inspect the selected plate, content border, and header viewport clip at constrained widths and in both flow directions. Prototype a width-aware join in the selected visual. Check overlap and z-order at the lower corners, including the seams beside neighboring tabs. Keep pointer and automation targets on their original containers.

**Done when.** Both lower joins render continuously with the content surface across widths and DPI. Separator rules, stroke thickness, hit testing, focus, and selected-header reveal stay intact.
