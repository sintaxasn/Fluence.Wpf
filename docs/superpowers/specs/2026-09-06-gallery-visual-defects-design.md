# Gallery visual defects against WinUI parity

Owner-reported defects from a Dark and Light theme pass over the gallery on 2026-09-06, with the
WinUI 3 Gallery 2.9.3 as the reference. The owner supplied screenshots; they are described here in
words because the implementation agents cannot see them. Every fix cites its WinUI 3 CommonStyles
source (`F:\Consolidation\WInUI\controls\dev`) or WinUI Gallery source (`F:\Consolidation\WinUI-Gallery`)
by file and line, per AGENTS.md section 4. Nothing here changes the public CLR or XAML surface
frozen at 1.0: `PublicAPI.Unshipped.txt` and `Theming/golden/PublicKeys.txt` must not change,
except where a fix adds a key and the owner is told.

## Popups: TeachingTip, Flyout, ToolTip, CommandBarFlyout

1. **TeachingTip.** Dark theme. The `Got it` action button shows a faint horizontal line through
   its top edge, as if a border or a clipped element overlaps the button. The tip has no shadow;
   WinUI's TeachingTip has a `ThemeShadow` with the flyout elevation.
2. **Flyout.** Dark theme. The rounded corner border reads oversized and there is a dark
   square-cornered layer visible outside the rounded border, so the popup shows a square dark
   plate behind a rounded card. No shadow. WinUI's FlyoutPresenter has an 8 px `OverlayCornerRadius`
   border with the popup surface clipped to it and a flyout shadow.
3. **ToolTip.** Dark theme. Same symptom as the Flyout: oversized border, a dark square layer
   outside the rounded corners, and no shadow. WinUI ToolTip uses `OverlayCornerRadius` with the
   tooltip shadow.
4. **CommandBarFlyout.** Dark theme. The shadow renders as a light halo around the bar instead of a
   dark drop shadow. Compare the shadow colour and opacity WinUI uses for its flyout elevation in
   dark theme; a shadow must never be lighter than the surface it falls on.

The common cause to check first: WPF `Popup` with `AllowsTransparency` and a `DropShadowEffect` or
a plate element that is not clipped to the presenter's `CornerRadius`. Fix the shared presenter
pattern once (Flyout, ToolTip, TeachingTip, CommandBarFlyout, and audit MenuFlyout, ContextMenu,
ComboBox and DatePicker or TimePicker popups for the same symptom) rather than four times.

## Gallery pages

5. **Colors page.** Restructure to the WinUI Gallery Color page layout. Reference screenshot,
   Dark theme:
   - Page title `Color`, then a one-line description with an inline XAML snippet showing
     `{ThemeResource TextFillColorPrimaryBrush}` usage.
   - A horizontal tab strip: `Text`, `Fill`, `Stroke`, `Background`, `Signal`, `High Contrast`,
     with an accent underline on the selected tab.
   - Under the selected tab, a vertical stack of groups. Each group is one large card on the
     card surface (`SolidBackgroundFillColorQuarternary` in WinUI Gallery's `ColorPageExample`)
     with the group name in a subtitle weight (`Text`, `Accent Text`, `Text On Accent`), a
     one-line usage note beneath it, and a centred large `Aa` sample rendered in the group's
     primary brush. `Text On Accent` paints the whole card in the accent fill with black `Aa`.
   - Directly under each group card, a full-width row of swatch tiles with no gaps between them
     and no gap to the card above: each tile is filled with its colour, shows the tile name
     (`Text / Primary`), a usage note (`Rest or Hover`, `Pressed only (not accessible)`,
     `Disabled only (not accessible)`), a copy button at the top right, and the brush key name
     (`TextFillColorPrimaryBrush`) at the bottom. Tile text is black or white by luminance.
     Rows have four tiles (Text, Accent Text) or two per row (Text On Accent has five tiles in
     two rows: Primary and Secondary, then Disabled and Selected Text).
   - The colour layering is the point: the page background is the layer fill, the group cards
     sit on the quarternary card surface, the swatch tiles are the colours themselves. Match the
     backgrounds and layers, not just the arrangement. Measure the WinUI Gallery on this machine
     via `winui3gallery://item/Color` and cite `WinUIGallery/Samples/Color/ColorPage.xaml` in the
     Gallery source.
   - The theme dictionary section at the bottom of the current Fluence page is split across two
     demo sample controls; it becomes one `DemoSampleControl` with one source view.
   - XAML shown in source views on this page is syntax highlighted. If the `DemoSampleControl`
     source viewer already highlights XAML elsewhere, the Colors page is not using it; if it
     highlights nowhere, add highlighting to the shared viewer, which fixes every page.
6. **Icons page.** Restructure to the WinUI Gallery Iconography page layout. Reference screenshot,
   Dark theme:
   - Page title `Iconography`, a one-line description (Segoe Fluent Icons on Windows 11, Segoe
     MDL2 Assets on Windows 10), then a search box `Search icons by name, code, or tags`.
   - Below, a two-column layout on one card surface: left, a wrapping grid of square tiles, each
     tile a subtle card with the glyph centred at about 24 px and the icon name beneath, the
     selected tile outlined in the accent colour; right, a details pane for the selected icon
     showing the glyph large in a tile, then labelled rows `Icon name`, `Text glyph` (`&#xE700;`),
     `Code glyph` (`\uE700`), `FontIcon XAML` (`<FontIcon Glyph="&#xE700;" />`), `FontIcon C#`
     (two lines), each with a copy button, and a `Tags` row of pill chips.
   - Match the layer colours: page background, the card surface behind the grid and details
     pane, the tile fill, the selected outline. Measure via `winui3gallery://item/Iconography`
     and cite the iconography page under `WinUIGallery/Samples/` in the Gallery source.

## Controls

7. **Button with graphical content.** Light theme. The Buttons page sample whose content is the
   `Slices.png` pie image renders its border incorrectly compared with the standard Button beside
   it: the stroke reads as a uniform flat outline rather than the WinUI control stroke (subtle
   top and sides, darker bottom edge from `ControlElevationBorderBrush`). The same defect is
   visible on the Menus page on the oversized RGB button. Reproduce both, then determine whether
   the image content, the button size, or the content margin triggers it, and fix the template so
   any content shape gets the standard stroke.
8. **Three-state ToggleButton.** Two of the three states (`IsChecked` true, false, null) render
   identically. WinUI's ToggleButton has distinct Checked, Unchecked and Indeterminate visuals
   (`ToggleButton_themeresources.xaml`, the `Indeterminate` state group). Fix the template and add
   a test that resolves the three background brushes and asserts all three differ.
9. **NumberBox.** Inputs page, first sample. Light theme screenshot shows `Inline` and `Compact`
   with spin buttons drawn as two separately outlined boxes inside the input, where WinUI draws
   them flat with no border inside the text box (`NumberBox_themeresources.xaml`, the
   `SpinButtonPlacementMode` states; Compact shows them in a flyout on hover or focus). `Keyboard
   only` accepts alphanumeric text; WinUI rejects non-numeric input on validation and the box
   must not accept letters as a value. Fix both, with tests for the input rejection and for the
   spin button chrome.
10. **Slider thumb.** The thumb's inner accent dot is too large. WinUI's thumb is a 20 px
    white circle (`SliderThumbWidth`, `SliderThumbHeight`) with an inner accent ellipse of
    `SliderInnerThumbWidth` and `SliderInnerThumbHeight` (12 px at rest), growing on hover and
    shrinking on press per `Slider_themeresources.xaml`. Match the rest, hover and pressed sizes
    and add a test on the rest size.

## Definition of done

- Each fix cites its WinUI reference in the commit body.
- Each fix has an xunit test where a value can be asserted (brush, size, state, input rejection).
- Visual verification in the demo in Light and Dark at 100 percent DPI with a before and after
  capture per defect saved under the SDD workspace, not committed.
- `PublicAPI.Unshipped.txt` and `Theming/golden/PublicKeys.txt` unchanged, or the change named to
  the owner in the final report.
- Build 0 warnings 0 errors on all TFMs, `dotnet format` clean, text policy gate clean, the two
  test lanes green on both TFMs, CI sum-check constants and `Baselines/` updated for any added test.
- `CHANGELOG.md` entries under `Unreleased`.
