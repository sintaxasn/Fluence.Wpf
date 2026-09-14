# Permitted `--list-tests` method-name changes

Diff key: the method name alone, one line per discovered test case. Theory cases
repeat their method name once per `[InlineData]`. All 1155 method names in the
baseline are unique across classes, so no `class.method` disambiguation is used.

Baseline: 1197 cases on net10, 1195 on net472, both captured at
`950e779` and committed beside this file.

Nothing outside this list may appear in a `Compare-Object` result. Every entry
lands in Task 23 and in no other task; every earlier and later task must diff
empty against the baseline, or against the baseline plus this list once Task 23
has landed.

## Removals: 21 lines

Eighteen are deleted test cases; three are the method names retired by the
`DefaultCollectionFocusVisualStyle` theory fold.

| Line | Source | Reason |
| ---- | ------ | ------ |
| `ControlCornerRadius_PresentInLightThemeAsync` | `ThemeMetricsTests.cs:56` | D1 |
| `ControlCornerRadius_PresentInDarkThemeAsync` | `ThemeMetricsTests.cs:68` | D1 |
| `ControlCornerRadius_PresentInHighContrastThemeAsync` | `ThemeMetricsTests.cs:80` | D1 |
| `OverlayCornerRadius_PresentInLightThemeAsync` | `ThemeMetricsTests.cs:96` | D1 |
| `OverlayCornerRadius_PresentInDarkThemeAsync` | `ThemeMetricsTests.cs:108` | D1 |
| `OverlayCornerRadius_PresentInHighContrastThemeAsync` | `ThemeMetricsTests.cs:120` | D1 |
| `DefaultControlFocusVisualStyle_PresentInAllThemesAsync` | `ThemeMetricsTests.cs:169` | D2 |
| `ProgressBar_TrackBackground_UsesWinUiStrongStrokeRoleAsync` | `ControlTests.BackgroundParity.cs:114` | D3 |
| `FiveSwitches_DictionaryCountStableAsync` | `ThemeManagerTests.cs:150` | D4 |
| `MergedDictionaries_CountStableAfterMultipleSwitchesAsync` | `FluenceWindowTitleBarTests.cs:471` | D4 |
| `BuildBackdropPlan_None_ReturnsOpaqueBackground` | `FluenceWindowHardenTests.cs:223` | D5 |
| `BuildBackdropPlan_Mica_SupportedOs_ReturnsTransparent` | `FluenceWindowHardenTests.cs:240` | D6 |
| `MainWindow_ProgressNumberBox_UpdatesFirstProgressBarAsync` | `ControlTests.cs:2056` | D7 |
| `Experiment_WriteDwmAccentColor_DoesAccentPaletteRegenerateAsync` | `AccentPaletteRegenerationExperiment.cs` | D8 |
| `Experiment_WriteAllAccentValues_DoesAccentPaletteRegenerateAsync` | `AccentPaletteRegenerationExperiment.cs` | D8 |
| `Experiment_WriteAllAndBroadcast_DoesAccentPaletteRegenerateAsync` | `AccentPaletteRegenerationExperiment.cs` | D8 |
| `Probe_EnumerateColorSets_DumpsAllRamps` | `ImmersiveColorSetProbe.cs:73` | D8 |
| `Score_AllAlgorithms_AgainstCapturedFixtures` | `AccentRampScoreboard.cs:153` | D9 |
| `DefaultCollectionFocusVisualStyle_PresentInLightThemeAsync` | `ThemeMetricsTests.cs:210` | Fold 1 |
| `DefaultCollectionFocusVisualStyle_PresentInDarkThemeAsync` | `ThemeMetricsTests.cs:221` | Fold 1 |
| `DefaultCollectionFocusVisualStyle_PresentInHighContrastThemeAsync` | `ThemeMetricsTests.cs:232` | Fold 1 |

## Additions: 4 lines

| Line | Occurrences | Reason |
| ---- | ----------: | ------ |
| `DefaultCollectionFocusVisualStyle_PresentInThemeAsync` | 3 | Fold 1: one `[Theory]` with three `[InlineData]`. |
| `RepeatedThemeSwitches_NoDictionaryAccumulationAsync` | 1 extra, taking it from 1 line to 2 | Fold 2: the D4 survivor becomes a `[Theory]` with `[InlineData(false)]` and `[InlineData(true)]`. |

Net: 21 removed, 4 added, 17 fewer cases. 1197 to 1180 on net10, 1195 to 1178 on net472.

## Status

Consumed. The baseline files beside this one were refreshed to the post-deletion
state in the same commit, so every task after this one diffs empty against them.

## Task 38: final branch-close regeneration

Baseline files were regenerated once, at the end of the branch, per Ruling R4.
This is the only other task allowed to touch this folder. No commit before
this one touched this folder, so the regenerated capture reflects every case
added anywhere on the branch since the Task 23 point above: 1208 cases on
net10 (was 1180), 1206 on net472 (was 1178), a rise of 28 on each TFM.

### Renames: 1 line

| Old name | New name | Commit | Reason |
| ---- | ---- | ---- | ------ |
| `ApplyApplicationAccent_RaisesAccentColorChangedOnceAsync` | `ApplyCustomAccent_WindowsBlue_RaisesAccentColorChangedOnceAsync` | `9451f89` | `ApplyApplicationAccent` was removed as a one-line alias for a Windows blue `ApplyCustomAccent` call, so the test that exercised it now calls `ApplyCustomAccent` directly with the Windows blue color and is named to match. |

### Additions: 28 lines, none colliding with an existing name

No other method name changed; every other new line is a genuinely new test case
with a name that never appeared in the Task 23 baseline, so none needed an
allowlist entry of its own. For the record, grouped by source file:

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `Opened_WhenShown_RaisesWithOpenedEventArgsAsync` | 1 | `ContentDialogTests.cs` |
| `Closed_EachClosePath_ReportsMatchingResultAsync` | 4 | `ContentDialogTests.cs` |
| `Closed_CloseButtonClicked_ReportsCloseButtonReasonAsync` | 1 | `InfoBarTests.cs` |
| `Closed_IsOpenSetFalse_ReportsProgrammaticReasonOnceAsync` | 1 | `InfoBarTests.cs` |
| `Closing_CloseButtonClicked_ReportsCloseButtonReasonAsync` | 1 | `InfoBarTests.cs` |
| `Closed_CloseButtonClicked_ReportsCloseButtonReasonAsync` | 1 | `TeachingTipTests.cs` |
| `Closed_EscapeKeyDismissal_ReportsLightDismissReasonAsync` | 1 | `TeachingTipTests.cs` |
| `Closed_IsOpenSetFalse_ReportsProgrammaticReasonAsync` | 1 | `TeachingTipTests.cs` |
| `Closed_PopupClosedOutsideIsOpen_ReportsLightDismissReasonAsync` | 1 | `TeachingTipTests.cs` |
| `CloseRequested_TypedHandler_ReceivesArgsWithoutCastAsync` | 1 | `TabViewTests.cs` |
| `TabCloseRequested_StillBubblesToAParentHandlerAsync` | 1 | `TabViewTests.cs` |
| `RemovedKeys_DoNotResolveInAnyThemeAsync` | 3 | `ResourceAliasTests.cs` |
| `BackgroundBrushAlias_ResolvesToTheSameBrushAsync` | 3 | `ResourceAliasTests.cs` |
| `FontFamilyAlias_ResolvesToTheSameFamilyAsync` | 1 | `ResourceAliasTests.cs` |
| `TitleBar_AutomationPeer_PrefersExplicitAutomationNameAsync` | 1 | `Control/Rules/AutomationPeerTests.cs` |
| `TitleBar_AutomationPeer_ReportsTitleBarControlTypeAndTitleAsync` | 1 | `Control/Rules/AutomationPeerTests.cs` |
| `InfoBadge_AutomationPeer_ReportsValueAsNameAsync` | 3 | `Control/Rules/AutomationPeerTests.cs` |
| `FlyoutPresenter_AutomationPeer_ReportsGroupControlTypeAsync` | 1 | `Control/Rules/AutomationPeerTests.cs` |
| `PublicKeyInventory_MatchesFrozenSetAsync` | 1 | `ThemeParityTests.cs` |

Note the task-38 dispatch's own case tally (27 new cases: 1 ContentDialog.Opened,
4 ContentDialog.Closed, 2 InfoBar.Closed, 3 TeachingTip.Closed, 2 TabView, 3
RemovedKeys, 4 aliases, 2 TitleBar peer, 3 InfoBadge peer, 1 FlyoutPresenter
peer, 1 InfoBar.Closing reason, 1 PublicKeyInventory) undercounts
`TeachingTipTests.cs` by one: that file added four new `Closed_`-prefixed facts,
not three, which is confirmed above by both the `Compare-Object` multiset diff
and direct inspection of the file. The measured total is 28 new cases, not 27;
this table is the corrected record.

Zero pure deletions on this stretch of the branch: the only line that left the
multiset is the rename source above.

### Status

Consumed. This is the branch's last baseline regeneration; there is no further
task after this one that touches `Baselines/`.

## Branch `fix/gallery-visual-defects`: NavigationView pane chrome regeneration

The baseline files were regenerated again on this branch, for the two regression
tests the pane chrome fixes brought with them. Both are new names that never
appeared in an earlier capture, so neither needed an entry of its own; this
section is the record of the regeneration.

### Additions: 22 lines

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `NavigationView_NestedInContent_KeepsItsOwnPaneChromeAsync` | 1 | `Control/NavigationViewTests.cs` |
| `NavigationView_CompactRailWidth_DoesNotFollowTheBackButtonAsync` | 1 | `Control/NavigationViewTests.cs` |
| `NavigationViewItem_InfoBadge_StaysOnAClosedPaneAsync` | 1 | `Control/NavigationViewTests.cs` |
| `SlideNavigationPresenter_OutgoingContent_KeepsTheContentTemplateAsync` | 1 | `Control/SlideNavigationPresenterTests.cs` |
| `SelectorBar_EmptiedSelection_PutsThePreviousItemBackAsync` | 1 | `Control/SelectorBarTests.cs` |
| `InfoBar_CloseButton_RaisesClickAndRunsTheCommandBeforeClosingAsync` | 1 | `Control/InfoBarTests.cs` |
| `InfoBar_CloseButtonStyle_ReachesTheButtonAndRestoresOnClearAsync` | 1 | `Control/InfoBarTests.cs` |
| `InfoBar_Content_RendersUnderTheBannerAndTakesItWhenThereIsNoneAsync` | 1 | `Control/InfoBarTests.cs` |
| `InfoBadge_DisplayKind_PrefersValueOverIconAndFallsBackAsync` | 1 | `Control/InfoBadgeTests.cs` |
| `InfoBar_Banner_LaysOutOnOneLineUntilItStopsFittingAsync` | 1 | `Control/InfoBarTests.cs` |
| `InfoBadge_ValueBadge_IsNeverNarrowerThanItIsTallAsync` | 1 | `Control/InfoBadgeTests.cs` |
| `NavigationViewItem_ClosedPane_GivesTheIconItsFullColumnAsync` | 2 | `Control/NavigationViewTests.cs`, one `[Theory]` with `[InlineData]` for Left and LeftCompact |
| `InfoBar_Opened_RaisesOnTheOpenTransitionButNotOnACancelledCloseRevertAsync` | 1 | `Control/InfoBarTests.cs` |
| `InfoBar_CustomIcon_IsClampedToTheIconBoxAsync` | 1 | `Control/InfoBarTests.cs` |
| `InfoBadge_Value_RejectsAnythingBelowMinusOneAsync` | 1 | `Control/InfoBadgeTests.cs` |
| `InfoBadge_GetStyleGlyph_ReturnsWinUiGlyphPerSeverity` | 1 | `Control/InfoBadgeTests.cs` |
| `InfoBadge_SeverityWithAValue_ShowsTheValueNotAGlyphAsync` | 1 | `Control/InfoBadgeTests.cs` |
| `TabViewItem_LeadingSeparator_SitsInTheMiddleOfTheGapAsync` | 1 | `Control/TabViewTests.cs` |
| `ComboBoxItem_CornerRadius_ComesFromTheKeyedResourceAsync` | 1 | `Control/ComboBoxTests.cs` |
| `InfoBadge_ValueText_FitsThePillAndIsCentredAsync` | 1 | `Control/InfoBadgeTests.cs` |
| `MainWindow_TitleBarSearch_IsNotClippedAsync` | 1 | `Gallery/DemoShellTests.cs` |
| `Slider_ThumbScale_TakesTheDurationOfTheStateItEntersAsync` | 1 | `Control/SliderTests.cs` |

Zero removals and zero renames. 1275 cases to 1298 on net10, 1273 to 1296 on
net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.
