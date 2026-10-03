# Fluence brand assets

`Fluence.Wpf.af` is the editable design file. The exported filenames use the visual role and target theme. `Light` has dark lettering for a light background; `Dark` has light lettering for a dark background. The icon mark itself has no theme variant.

| Role | Files |
| --- | --- |
| Color icon | `Fluence_Icon.svg`, `Fluence_Icon.png`, `Fluence_Icon_16.png`, `Fluence_Icon_32.png`, `Fluence_Icon_64.png`, `Fluence_Icon_128.png`, `Fluence_Icon_256.png` |
| Application icon | `Fluence_Icon.ico` |
| Horizontal lockup | `Fluence_Lockup_Horizontal_Light.png`, `Fluence_Lockup_Horizontal_Light.svg`, `Fluence_Lockup_Horizontal_Dark.png`, `Fluence_Lockup_Horizontal_Dark.eps` |
| Stacked lockup | `Fluence_Lockup_Stacked_Light.png`, `Fluence_Lockup_Stacked_Light.svg`, `Fluence_Lockup_Stacked_Dark.png`, `Fluence_Lockup_Stacked_Dark.svg`, `Fluence_Lockup_Stacked_Gradient.png` |
| Supplied banner | `Fluence_Banner_Light.png` |

The ICO contains the supplied 16, 32, 64, 128, and 256 pixel PNGs as exact frames, without resizing. The horizontal light SVG uses live text and an embedded bitmap; the supplied dark horizontal artwork is an EPS, so there is no matching dark SVG. The gallery uses the supplied PNG pair to keep both themes consistent. The banner contains version-specific text and is kept as an export, not used as a current runtime or documentation header.

The library keeps its existing `FluenceIconBrandDrawingImage`, plated icon, and `FluenceHeaderLightDrawingImage` / `FluenceHeaderDarkDrawingImage` resource keys. The color icon DrawingImage uses native WPF geometry translated from `Fluence_Icon.svg`; header DrawingImages wrap the corresponding supplied PNG lockups.
