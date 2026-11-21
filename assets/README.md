# Fluence brand assets

`Fluence.Wpf.af` is the editable design file. The exported filenames use the visual role and target theme. `Light` has dark lettering for a light background; `Dark` has light lettering for a dark background. The icon mark itself has no theme variant.

| Role | Files |
| --- | --- |
| Color icon | `Fluence_Icon.svg`, `Fluence_Icon.png`, `Fluence_Icon_16.png`, `Fluence_Icon_32.png`, `Fluence_Icon_64.png`, `Fluence_Icon_128.png`, `Fluence_Icon_256.png` |
| Application icon | `Fluence_Icon.ico` |
| Plated icon | `Fluence_Icon_Background_Light.svg`, `Fluence_Icon_Background_Light_256.png`, `Fluence_Icon_Background_Light_512.png`, and the corresponding `Dark` files |
| Horizontal lockup | `Fluence_Lockup_Horizontal_{Light,Dark,Rainbow,Gradient}.{png,svg}` |
| Stacked lockup | `Fluence_Lockup_Stacked_{Light,Dark,Rainbow,Gradient}.{png,svg}` |
| Supplied tagline banner | `Fluence_Banner_{Light,Dark}.{png,svg}` |

The ICO contains the supplied 16, 32, 64, 128, and 256 pixel PNG frames byte-for-byte. The incoming `logo_*_horizontal` files are side-by-side lockups; the source files named `logoo_*_hoeizontal` are stacked layouts despite their spelling. `Rainbow` uses individual letter colors, while `Gradient` uses a continuous text gradient.

The Gallery Home backdrop uses the color mark from `Fluence_Icon.svg` as native WPF vector geometry. The product name, description, and runtime support are WPF text. The supplied banner exports remain reference artwork.

The library keeps its existing `FluenceIconBrandDrawingImage`, plated icon, and `FluenceHeaderLightDrawingImage` / `FluenceHeaderDarkDrawingImage` resource keys. The color icon DrawingImage uses native WPF geometry translated from `Fluence_Icon.svg`; header DrawingImages wrap the corresponding supplied PNG lockups.
