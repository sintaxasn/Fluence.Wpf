# Control behavior issue briefs

## TreeView check state can be written back during template replacement

**Context and cause.** In `Multiple` mode, generated `TreeViewItem` containers hold `IsSelectionChecked`. During full template replacement, an automatic two-way binding can write the retiring container's default state to the model. Identify the write before changing selection callbacks. A later binding refresh cannot recover a model value that has already been overwritten. [TreeView.cs](../../Fluence.Wpf/Controls/TreeView.cs) · [TreeViewItem.cs](../../Fluence.Wpf/Controls/TreeViewItem.cs)

**Previous work.** `OnContainerStatusChanged` schedules `RefreshBoundSelection` after the generator reports `ContainersGenerated`. The refresh calls `UpdateTarget` on generated root bindings and rebuilds the existing `SelectedItems` list. `TreeView_ModeSwitch_ReconcilesFallbackWithoutSelectionLossAsync` uses explicit source updates and checks the regenerated checkbox, selection list identity, and removal behavior. Its explicit update trigger bypasses the write-back described here. [TreeViewTests.cs](../../Fluence.Wpf.Tests/Control/TreeViewTests.cs)

**Investigate.** Start with the mode-switch fixture. Change the checked-state binding to an automatic source trigger, check an item, then clear and reapply the template. Record model property writes, old and new container identity, and generator status throughout the transition. Set `NotifyOnSourceUpdated=True` and enable `PresentationTraceSources.TraceLevel=High` on the binding to distinguish a source write from a regenerated target display. Protect legitimate user writes and model updates if introducing a teardown guard.

**Done when.** The affected binding preserves its model value, the new container reflects that value, and the same live `SelectedItems` list has the correct entries after regeneration. A subsequent user toggle still updates the model, and removing a checked item removes it from the list.

## Checking a collapsed TreeView branch can skip its children

**Context and cause.** `UpdateSelectionFromItem` calls `ApplySelectionStateToDescendants` for definite check values. The descent and `AddCheckedItems` resolve generated containers, or use explicitly supplied `TreeViewItem` objects. An unrealized data item has neither a usable container nor stored selection intent in that traversal. `GetChildSelectionState` preserves a nonempty parent's current value when no child container exists, and clears it when the data collection becomes empty. [TreeView.cs](../../Fluence.Wpf/Controls/TreeView.cs)

**Previous work.** Collection reconciliation now removes detached selections, retains a checked collapsed parent through changes to nonempty child data, and clears it after the final child goes away. `TreeView_MultipleSelection_CollapsedBoundChildrenPreserveParentUntilEmptyAsync` asserts that the child containers remain absent during those changes. The existing cascade test exercises generated containers. [TreeViewTests.cs](../../Fluence.Wpf.Tests/Control/TreeViewTests.cs)

**Investigate.** Bind a parent to an observable collection through a `HierarchicalDataTemplate`. Verify `ContainerFromIndex(0)` is null before checking the parent. Capture `IsSelectionChecked` and `SelectedItems` before and after expansion, then repeat with remove, replace, and reset. Use a data-bound branch because explicit `TreeViewItem` children participate even while collapsed. Define item identity, tri-state aggregation, and how inherited check intent applies to later additions before choosing a container-independent model.

**Done when.** Checked intent reaches the affected data items without forcing realization, freshly generated descendants display it, and removals and resets reconcile the live selection. Container-independent state is a prerequisite for enabling virtualization in Multiple mode.

## TreeView multiple selection can realize every root item

**Context and cause.** The Multiple-mode template forces `PART_ScrollViewer.CanContentScroll=False`, even if a caller supplies a `VirtualizingStackPanel` and requests logical scrolling. The opt-in style triggers are limited to `Single` and `None`. The physical-scrolling fallback keeps root containers available to the current container-based checked-state logic. [TreeView.xaml](../../Fluence.Wpf/Themes/Controls/TreeView.xaml) · [TreeView.cs](../../Fluence.Wpf/Controls/TreeView.cs)

**Previous work.** `TreeView_OptIn_BoundsRealizedRootContainersAsync` covers Single and None. `TreeView_MultipleOptIn_UsesPhysicalFallbackAndPreservesSelectionAsync` deliberately requests logical scrolling and a virtualizing panel, then checks that all 1,000 roots are realized and that selection stays consistent. The mode-switch test also exercises explicit checked-state binding through retemplating. [TreeViewTests.cs](../../Fluence.Wpf.Tests/Control/TreeViewTests.cs)

**Investigate.** First resolve the state dependency described in [collapsed branch selection](#checking-a-collapsed-treeview-branch-can-skip-its-children). Measure realized root containers in a height-constrained large tree using the existing fixture. Check and scroll items out of view, recycle containers, expand and collapse descendants, change the collection, and switch selection modes. Track selection identity independently from container lifetime.

**Done when.** Multiple mode with virtualization enabled realizes a bounded set of roots while preserving checks, partial ancestors, realized state after recycling, and the live `SelectedItems` list. The Single and None opt-in cases still pass.

## DataGrid can briefly report a rejected selection in native collections

**Context and cause.** The native grid can update `SelectedItems` or `SelectedCells` before the Fluence override receives the rejected selection. `OnSelectionChanged` and `OnSelectedCellsChanged` suppress rejected public events. `ScheduleClearSelection` enqueues a `DispatcherPriority.Send` cleanup because native selection bookkeeping is not re-entrant. Fluence separately coerces `SelectedItem`, `SelectedIndex`, and `SelectedValue` while selection is disabled. [DataGrid.cs](../../Fluence.Wpf/Controls/DataGrid.cs)

**Previous work.** Existing tests add a row or cell with selection disabled, drain the dispatcher, and check the empty collections and suppressed consumer events. The rapid re-enable tests protect valid selection that follows a rejected add. A current cell can still enter edit mode through `BeginEdit()`. The November 19 audit verified those paths in source and left physical mouse and keyboard routes for targeted interaction verification. [DataGridTests.cs](../../Fluence.Wpf.Tests/Control/DataGridTests.cs)

**Investigate.** Reproduce separately with full-row and cell selection. Record the collection during its notification, immediately after a direct `Add`, and after the dispatcher drains. Record the three single-selection properties and public event counts alongside the collection. Verify existing selection removal when disabling selection and repeat with immediate re-enable before the queued clear. Determine which originating paths can reject a change before WPF updates the collection. Check the closest native input and editing tests before attempting a synchronous clear.

**Done when.** The fixed path stops exposing rejected entries to observers while keeping single-selection properties empty and rejected events suppressed. Existing selection removal, subsequent valid selection, current-cell navigation, and editing retain their behavior. Report which direct collection and native input paths were verified.

## Hidden NavigationView items can keep a stale width

**Context and cause.** Top-mode overflow stores each item's natural width in `TopOverflowItemWidthProperty`. A collapsed item has no ordinary `SizeChanged` signal. `NavigationViewItem.OnPropertyChanged` handles content replacement and properties marked `AffectsMeasure`. Its hidden-content observer watches only a direct `TextBlock.Text` property, so changes deeper in a visual can leave the cached width stale. `AutomationProperties.Name` supplies the overflow label independently of width. [NavigationViewItem.cs](../../Fluence.Wpf/Controls/NavigationViewItem.cs) · [NavigationView.cs](../../Fluence.Wpf/Controls/NavigationView.cs)

**Previous work.** The corrected paths evict the width and schedule Top overflow reflow. `NavigationView_TopMode_ReplacingHiddenContentRecoversItemAsync` and `NavigationView_TopMode_ChangingHiddenChildTextRecoversItemAsync` cover replacement and direct text. `NavigationView_TopMode_ExactFitBoundaryDoesNotFlapLastItemAsync` protects the recovery margin from oscillation. [NavigationViewTopModeTests.cs](../../Fluence.Wpf.Tests/Control/NavigationViewTopModeTests.cs)

**Investigate.** Use a fixed-width Top host. Give the last item a panel containing a long label and let the item overflow. Shorten the nested label in place, leaving `Content` itself untouched. Record the cached width, visible strip membership, and overflow menu entries. Compare with the direct-text and replacement fixtures. Follow `InvalidateTopItemWidth` and the width lookup near the natural-measure routine. A fix needs bounded remeasurement and should release any observers when the item is replaced, removed, made visible, or leaves Top mode.

**Done when.** An in-place nested size change updates the overflow decision without resizing the host. The menu keeps current content and accessibility labels, and exact-fit recovery, invocation, and observer cleanup still work.

## AutoSuggestBox raises TextChanged for each edit

**Context and cause.** The template editor's `OnTextBoxTextChanged` assigns the public `Text` under the `UserInput` reason. `OnTextPropertyChanged` synchronizes the editor and calls `RaiseTextChanged` synchronously. That method captures the current text for `CheckCurrent()` before notifying subscribers. WinUI's reference implementation has a 150 ms TextChanged timer. [AutoSuggestBox.cs](../../Fluence.Wpf/Controls/AutoSuggestBox.cs) · [WinUI implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/main/dxaml/xcp/dxaml/lib/AutoSuggestBox_Partial.cpp)

**Previous work.** The user-edit and programmatic-edit tests assert their distinct reasons. Commitment runs `SuggestionChosen`, updates text if `UpdateTextOnSelect` is enabled, and then runs `QuerySubmitted`. Keyboard preview restores typed text on cancellation, so it is a separate path from an edit burst. The audit found the existing immediate notification contract. [AutoSuggestBoxTests.cs](../../Fluence.Wpf.Tests/Control/AutoSuggestBoxTests.cs)

**Investigate.** Record reason, text, event order, and lookup start for several quick edits followed by Enter. Compare with a programmatic update and a committed suggestion. For app-side throttling, cancel the previous lookup or assign a request generation, and use `CheckCurrent()` before applying results. A generation also handles typing back to the same text. For a control change, agree on opt-in coalescing before changing defaults and define cancellation on unload and template replacement. Prefer controllable scheduling to fixed sleep delays in tests.

**Done when.** The chosen policy limits lookups for a typing burst, displays results only for the newest request, and preserves notification reasons and commitment ordering.

## Adding a DataGrid group style after load can miss the first group level

**Context and cause.** `OnDataGridLoaded` calls `EnsureDefaultGroupStyle`. If `GroupStyle` is empty, the grid appends the Fluent `DataGridGroupItemStyle` as entry zero. WPF tries `GroupStyleSelector` first and falls back to indexing the `GroupStyle` collection by grouping level. If the collection fallback applies, a later append leaves the default at level zero. [DataGrid.cs](../../Fluence.Wpf/Controls/DataGrid.cs) · [WPF group-style selection](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/ItemsControl.cs#L1317-L1344)

**Previous work.** `DataGrid_Grouping_AddsDefaultGroupStyleAndTogglesAllGroupsAsync` asserts a single default style after load and exercises expand and collapse. The insertion guard preserves styles supplied before load. The reviewed tests establish default insertion and group behavior. [DataGridTests.cs](../../Fluence.Wpf.Tests/Control/DataGridTests.cs)

**Investigate.** Create a grouped `ListCollectionView` and show the grid with no group style. Compare a distinct style supplied before load, appended after load, and supplied through `GroupStyleSelector`. Use two grouping levels to see which generated `GroupItem` receives which style. If changing insertion ownership, track the exact default instance so application styles and reloads remain predictable.

**Done when.** A fix supports the specified late customization path at the intended level, preserves caller-owned styles and selectors, and keeps the Fluent default available when no override is supplied. Check two grouping levels and a reload, plus the related virtualization header test.

## Grouping a DataGrid can realize every row

**Context and cause.** The theme sets recycling for ordinary rows. Its Aero2-style trigger sets `ScrollViewer.CanContentScroll=False` when grouping is active and `VirtualizingPanel.IsVirtualizingWhenGrouping=False`. Grouped rows therefore need the WPF opt-in to continue using the virtualizing layout path. Check the actual items panel and scroll viewer before attributing realization to the group style. [DataGrid.xaml](../../Fluence.Wpf/Themes/Controls/DataGrid.xaml) · [WPF grouped virtualization setting](https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.virtualizingpanel.isvirtualizingwhengrouping)

**Previous work.** `DataGrid_TenThousandRows_RealizesOnlyVisibleRowsAsync` limits the ungrouped baseline to 40 realized rows. `DataGrid_GroupHeader_ReportsItsSizeToGroupVirtualizationAsync` enables grouping virtualization on a small fixture and checks that the header contributes its height to the extent. These exercise different contracts. [DataGridTests.cs](../../Fluence.Wpf.Tests/Control/DataGridTests.cs)

**Investigate.** Use a height-constrained grid with a large grouped source. Count realized `DataGridRow` and `GroupItem` containers and record `CanContentScroll`, the items panel, and the extent before and after the opt-in. Expand and collapse groups, then scroll across boundaries. If row count stays high after opt-in, check the caller's items panel, unconstrained parent scrolling, and whether its group template exposes the header site. Reuse the existing header-size test when changing the default.

**Done when.** Grouped realization stays bounded in the supported configuration and the extent correctly represents headers as groups expand, collapse, and recycle. Document the verified settings and retain the ungrouped realization baseline.

## TabView tabs cannot be reordered or torn off by dragging

**Context and cause.** `TabView` extends WPF `TabControl` and wires tab creation, close requests, strip scrolling, and keyboard focus. The template and control currently lack a drag threshold, drop-index calculation, and transfer lifecycle. Same-strip reordering is a collection operation. Tear-out also needs application-owned window creation, transfer, and cancellation. [TabView.cs](../../Fluence.Wpf/Controls/TabView.cs) · [TabViewItem.cs](../../Fluence.Wpf/Controls/TabViewItem.cs) · [WinUI TabView interactions](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/tab-view#tab-tear-out)

**Previous work.** `TabView_CollectionChanges_KeepSelectedHeaderVisibleAsync`, the retemplate test, and the left-to-right and right-to-left overflow tests establish selection and scrolling behavior around non-drag collection changes. Prior fixes covered keyboard entry with no available header, oversized header alignment in right-to-left layout, and overflow button glyph direction. Preserve those behaviors and the application-owned add and close requests when adding a gesture. [TabViewTests.cs](../../Fluence.Wpf.Tests/Control/TabViewTests.cs)

**Investigate.** Start with a narrow, overflowing strip with stable item identities. Compare explicitly declared tabs and tabs backed by a mutable `ItemsSource`. Drag a header across both physical edges and past another header. Track model order, selection, focus, and viewport offset. Establish same-strip moves and cancellation before adding cross-window transfers. Transfer item data or detach it from its original owner before reusing a visual in another window.

**Done when.** A verified same-strip drag updates order exactly once, cancellation preserves order, and selected-header visibility and keyboard focus survive in both flow directions. For tear-out, verify source ownership, destination acceptance or refusal, close handling, and selection after transfer in two actual test windows.
