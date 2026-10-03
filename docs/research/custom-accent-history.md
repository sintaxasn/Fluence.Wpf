# Custom accent and Mica history

This note records the source investigation completed on 2026-10-02. It separates accent palette calculations from the nearby Mica layer work, because they affect different pixels. It describes source behavior; the PSADT report still needs visual validation against a chosen target color.

## What changed

| Date | Change | Effect |
| --- | --- | --- |
| 2026-05-24 | [b102d0c7](https://github.com/sintaxasn/Fluence.Wpf/commit/b102d0c7) | Added the Windows accent palette and generated custom ramp machinery. |
| 2026-05-24 | [d522daa4](https://github.com/sintaxasn/Fluence.Wpf/commit/d522daa4) | Widened the generated shade spread so neighboring control states are distinguishable. The input seed remains the raw `SystemAccentColor`. |
| 2026-05-30 | [6a46acca](https://github.com/sintaxasn/Fluence.Wpf/commit/6a46acca) | Mapped the visible primary accent role and `AccentFillColorDefault` to generated `Dark1` in Light or `Light2` in Dark. This is the mapping in the current default path. |
| 2026-08-09 | [5fd0cebb](https://github.com/sintaxasn/Fluence.Wpf/commit/5fd0cebb) | Added sticky per-theme custom seeds with `ApplyCustomAccent(light, dark)`. Each seed still feeds the generated ramp; supplying both never meant the visible fills were exact seed colors. |
| 2026-09-01 to 2026-09-02 | [1ad179a8](https://github.com/sintaxasn/Fluence.Wpf/commit/1ad179a8), [58293f34](https://github.com/sintaxasn/Fluence.Wpf/commit/58293f34), [1285bdb5](https://github.com/sintaxasn/Fluence.Wpf/commit/1285bdb5), [f20aec39](https://github.com/sintaxasn/Fluence.Wpf/commit/f20aec39), [77d16c95](https://github.com/sintaxasn/Fluence.Wpf/commit/77d16c95) | Aligned NavigationView pane/content layers with WinUI tokens, corrected the Light solid base and recorded DWM 10-bpc alpha quantization. The last commit pre-blends the NavigationView content surface over Mica on affected displays. |

The September Mica fix is still present in `FluenceWindow` and `WindowPolicy`. It is a window-scoped pre-blend of the NavigationView content background under Mica or Tabbed backdrops on displays using more than 8 bits per color channel with advanced color off. It does not select `AccentFillColorDefault`, button fills, or the custom accent ramp. The history therefore does not show a later revert of the Mica work causing the custom accent report. The accent shade mapping predates August 20 and persisted separately.

For the example custom seed `#FF87ABC8`, the generated fallback path produces approximately `#FF1D9AFF` as Light `AccentFillColorDefault` and `#FFCAE8FF` as Dark `AccentFillColorDefault`. This applies when Windows is using a different accent base or the OS palette cannot be read. It demonstrates why a configured brand seed can disagree visibly with the primary accent fill, even while the raw `SystemAccentColor` still equals the seed. The generated ramp is an approximation for arbitrary custom colors; these source changes do not establish an exact Windows/WinUI transform for all input colors.

## Windows Settings observations

Five user-supplied Windows Settings captures provide more concrete targets. The capture state may include hover, so these displayed pixels are observations, not confirmed resting-state resource colors:

| Selected base | Light displayed | Dark displayed |
| --- | --- | --- |
| Blue `#0078D4` | `#1A76C6` | `#48B2EB` |
| Gold `#FFB900` | `#E4A71A` | `#E9C32B` |
| Rust `#DA3B01` | `#CB471A` | `#E87534` |
| Plum `#881798` | `#882C97` | `#C755D5` |
| Green `#107C10` | `#267C26` | `#42D233` |

For blue, plum, and green, the Light displayed pixels equal a 90% Windows `Dark1` shade over 10% white. The Dark pixels are consistent with a 90% `Light2` shade over a tinted Mica surface, but that backdrop varies. These relationships suggest compositing is material to the visible result; they do not justify replacing opaque resource colors with the sampled pixels. The live system palette also varies from historical captured examples: the current registry green snapshot has `Light2=#45E532` and `Dark1=#0E6D0E`, while older blue and plum captures recorded different rungs. The current operating-system palette is therefore the strongest source when the custom input exactly matches its selected base; an unrelated arbitrary custom color still needs a generated approximation.

## PSADT 4.2.0-rc2 integration

PSADT [issue #2315](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues/2315) reports the original mismatch, and [issue #2368](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues/2368) reports it again in RC2. In the official [`4.2.0-rc2` source](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/tree/4.2.0-rc2), the prompt passes both configured colors through [Show-ADTInstallationPrompt.ps1](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/4.2.0-rc2/modules/PSAppDeployToolkit/Public/Show-ADTInstallationPrompt.ps1) into [BaseDialogOptions.cs](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/4.2.0-rc2/src/PSADT.UserInterface/DialogOptions/BaseDialogOptions.cs). [FluentDialog.xaml.cs](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/4.2.0-rc2/src/PSADT.UserInterface.Interfaces/Fluent/FluentDialog.xaml.cs) converts ARGB bytes correctly and selects the light or dark configured color by resolved theme, but `SetDialogAccent` invokes only the one-color `ApplyCustomAccent(accentColor)` overload. The two-color overload is present in the vendored library but unused by this call path.

The sidebar in [FluentDialog.xaml](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/4.2.0-rc2/src/PSADT.UserInterface.Interfaces/Fluent/FluentDialog.xaml) binds `AccentFillColorDefaultBrush`. The accent button in the [vendored Button.xaml](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/4.2.0-rc2/vendor/Fluence.Wpf/Fluence.Wpf/Themes/Controls/Button.xaml) also uses that fill in its default state, with generated secondary and tertiary accent fills for hover and pressed states. The vendored [ColorMap.cs](https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/4.2.0-rc2/vendor/Fluence.Wpf/Fluence.Wpf/Theming/ColorMap.cs) maps that visible default fill to `Dark1` in Light and `Light2` in Dark. This explains how PSADT can preserve a configured seed yet display a different color.

Fluence now offers `ApplyCustomAccentExact(light)` and `ApplyCustomAccentExact(light, dark)` as opt-in APIs for an exact visible primary fill. The existing `ApplyCustomAccent` overloads keep their theme-selected shade behavior: when a supplied seed equals the current Windows accent base, they pin a snapshot of the seven OS shades; otherwise, they use the prior generated approximation. A pinned snapshot does not follow later Windows accent changes, while `ApplySystemAccent` does. For PSADT to obtain exact configured fills for arbitrary colors, its integration must call the new exact API from a Fluence version that includes it; changing Fluence alone does not alter the RC2 vendored call site. If only a light color is configured, the one-color exact overload keeps that light fill and derives the dark primary from the resolved palette's `Light2` tint (captured Windows shade for a matching seed, generated fallback otherwise). If both are configured, the two-color exact overload treats each as the intended visible fill. High Contrast control roles continue to follow live Windows system colors.
