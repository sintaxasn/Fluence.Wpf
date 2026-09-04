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
