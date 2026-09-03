# Known issues and follow-ups

This file tracks optional follow-ups and deliberate non-features. Filed bugs with
reproductions live on the issue tracker; this is the consolidated view for
maintainers.

## Current follow-ups (not defects)

- **Windows 10 legacy acrylic is unverified on real hardware** - the
  `SetWindowCompositionAttribute` acrylic path (`BackdropType.Acrylic` on
  Windows 10 build 17063+) is covered by pure policy tests only. The resolution
  rules, the plan values, and the `0xAABBGGRR` tint packing are pinned by
  `WindowPolicyTests` and `NativeMethodsTests`, but nothing in the suite can
  exercise the accent policy itself, because every CI and development machine
  here runs Windows 11, where the path is deliberately unreachable. Three things
  need a look on a real Windows 10 box before this is considered done: whether
  the tint alpha taken straight from `AcrylicBackgroundFillColorDefault` reads
  correctly (some reference implementations scale it by a further 0.8 to
  compensate for the weaker legacy blur); whether the drag downgrade to
  `ACCENT_ENABLE_BLURBEHIND` is enough on low-end hardware or whether the
  opaque `ACCENT_ENABLE_GRADIENT` fallback documented in
  `FluenceWindow.DowngradeLegacyAcrylicForDrag` is needed; and whether the
  `-1` glass frame thickness that `WindowPolicy.GetGlassFrameThickness` returns
  for a requested backdrop produces a visible artifact around an
  accent-blurred window, in which case that method should be driven by the
  effective backdrop instead of the requested one.

- **Peeking at a password materializes it as a managed string** - the Fluent
  `PasswordBox` chrome reads `PasswordBox.Password` in two places: to paint the
  peek overlay while the reveal button is held or toggled, and to score the
  strength meter after each change. Both allocate an immutable managed string
  that cannot be zeroed on demand, so a revealed or scored password is
  recoverable from a process dump until the garbage collector reclaims it. The
  native `SecurePassword` store remains authoritative and is never replaced; the
  overlay is emptied the moment the peek ends. An application handling
  high-value secrets can avoid the exposure by setting
  `PasswordBoxExtensions.RevealButtonEnabled` and
  `PasswordBoxExtensions.ShowPasswordStrength` to `False`, which is the default
  for the strength meter.

- **`TabView` drag-to-reorder** - `TabView` / `TabViewItem` ship with closable
  tabs, an add-tab button, per-tab icons, overflow scroll, and width / overlay
  modes. Drag-and-drop tab reordering (including cross-window tear-off) is **not**
  implemented; consumers that need it should handle `PreviewMouseMove` / drag-drop
  themselves. This is the main remaining gap vs. WinUI 3 `TabView`.
- **Navigation back-stack** - `NavigationView.IsBackButtonVisible` +
  `IsBackEnabled` + `BackRequested` are exposed, but the library does **not**
  track page history. The demo does not use the back button; consumers are
  expected to own their own back stack and route `BackRequested`.
- **Translucent layers over a DWM backdrop lose alpha precision on a 10 bpc
  display** - symptom: under `BackdropType.Mica` in Light the `NavigationView`
  content canvas (`LayerFillColorDefault`, `#80FFFFFF`) reads darker than the
  pane and title bar, which straight alpha compositing cannot produce. Measured
  on a desktop whose active display path reports `bitsPerColorChannel = 10`
  (`DisplayConfigGetDeviceInfo`, SDR, HDR off): a plain WPF window with
  `WindowChrome` and `DWMSBT_MAINWINDOW` composites its client alpha over the
  backdrop with the alpha rounded to two bits, {0, 1/3, 2/3, 1}, so
  `#80FFFFFF` (alpha 0.502 rounds to 2/3) lands at 128 + Mica/3 = 209 instead
  of the 249 the token specifies, `#24000000` is invisible, and `#60FFFFFF`
  saturates to white. Colour channels keep full precision. The same figures
  come out of iNKORE.UI.WPF.Modern on the same desktop, and the quantisation is
  absent under `BackdropType.None`, where WPF blends the layer itself. The
  likely mechanism is a `R10G10B10A2` composition surface when the output is
  10 bits per channel; this is inferred from the measurement, not documented
  by Microsoft, and is not yet verified against an 8 bpc display. Only
  `BackdropType.Mica` was measured quantising under
  `DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO` flags `0x4` (10 bpc, advanced color
  off); enabling the GPU driver's 10-bit pixel format (AMD Adrenalin, Gaming,
  Graphics) reported advanced color enabled (flags `0x7`), and under that
  state Mica, Acrylic, and Tabbed all read full precision. `Tabbed` is
  included in the fix below by DWM material-family inference, not
  measurement: it shares the same `DWMSBT_MAINWINDOW` / `DWMSBT_TABBEDWINDOW`
  family as Mica but was measured only under `0x7`. `Acrylic` is excluded:
  it too was measured only under `0x7`, where it read full precision, and was
  never measured under the quantising `0x4` flags, so there is no basis yet
  to include or exclude it. The library now applies an opaque pre-blend
  automatically when the window's display reports more than 8 bits per
  channel with advanced color off and the effective backdrop is Mica or
  Tabbed; an 8 bpc display, or a 10 bpc display with advanced color enabled,
  keeps the canonical translucent token.
- **`RenderTargetBitmap` vs DWM backdrop** - DWM Mica / Acrylic is composed by
  the window manager and is **not** visible to `RenderTargetBitmap`. The
  screenshot harness hosts the gallery inside a plain `Window` with a solid
  `SolidBackgroundFillColorBaseBrush`. Automated capture of the full
  `FluenceWindow` chrome needs a different approach (e.g. `PrintWindow` /
  GDI screen capture).
- **`DatePicker` / `TimePicker` highlight band during a scroll** - the flyouts
  now use WinUI's looping selector columns under a centered accent highlight
  band (`DatePicker_themeresources.xaml` `HighlightRect`), but the band is a
  plain `Border` painted behind the columns and the row on it flips its whole
  foreground to `TextOnAccentFillColorPrimaryBrush`. WinUI instead draws the
  row glyphs through a `MonochromaticOverlayPresenter`, which splits a glyph at
  the band boundary so the part inside the band is painted on-accent and the
  part outside it is not. WPF has no equivalent presenter, so mid-scroll a row
  straddling the band edge flips as a whole glyph rather than pixel by pixel.
  At rest the two are indistinguishable.
- **`ColorPicker` spectrum permutations and layout options** - the picker now
  carries the WinUI gallery-default option surface (preview, color slider, hex,
  More/Less toggle, alpha slider/text, and the RGB/HSV channel text inputs),
  but `ColorSpectrumShape` (the Ring spectrum), the `ColorSpectrumComponents`
  permutations, `Orientation`, and the Min/Max channel range properties remain
  deliberately omitted; the spectrum is fixed to saturation (x) by value (y)
  with hue as the third-dimension slider. Two deviations from WinUI: the hex
  input commits on Enter / focus loss rather than live per keystroke, and the
  hue text input accepts 0-360 (WinUI caps at 359) because the picker's model
  and slider use 360 inclusive.
- **`ContentDialog` smoke layer and sizing** - the dialog always paints its
  smoke (dimming) layer; there is **no** WinUI `DialogShowingWithoutSmokeLayer`
  state, and **no** `FullDialogSizing` stretch mode. The entrance and the
  `DialogHidden` exit animations are both implemented.
- **`BreadcrumbBar` ellipsis overflow** - the bar does **not** collapse leading
  crumbs into an ellipsis (WinUI collapses them into an `E712` ellipsis item
  with a flyout). Long trails extend to their natural width and clip when
  constrained.
- **`PipsPager` edge-pip scale and nav-button scale** - the pager realizes every
  pip and scrolls them inside the stationary edge-scrolling viewport WinUI
  describes, but it does **not** scale the pips down as they approach the
  viewport edges, and the navigation buttons do **not** use WinUI's pressed
  `0.875` scale.
- **`AutoSuggestBox` raises `TextChanged` per keystroke** - WinUI defers the
  `TextChanged` event through a 150 ms `DispatcherTimer`
  (`AutoSuggestBox_Partial.cpp`, `s_textChangedEventTimerDuration`), coalescing a
  fast typing burst into one event. Fluence raises `TextChanged` synchronously on
  every edit. For in-memory filtering (the demo, most consumers) the difference
  is invisible; a consumer wiring `TextChanged` to an async or remote lookup gets
  per-keystroke churn WinUI would coalesce and should debounce in the handler.
  Adding the timer would change the event timing every existing `AutoSuggestBox`
  test observes, so it is deferred until a consumer needs it.
- **`NavigationView` Top-overflow fitting deviations** - the overflow pass
  (`NavigationView.UpdateTopOverflow`) no longer forces a layout pass, caches each
  item's measured width on the item, and applies a 5px recovery grace before an
  item returns from the menu. Three deliberate deviations from WinUI's
  `NavigationView` remain. The available width is the width
  `PART_TopItemsHost` was last arranged at rather than a width computed inside a
  measure override: the host is the star-sized column of the pane header grid, so
  item visibility cannot change it, but a pass that runs before the host's first
  arrange reads zero and bails, leaving the work to the pass that the following
  arrange schedules. The cached width is evicted only on a non-zero
  `SizeChanged` (a zero size is this control collapsing the item into the menu,
  not a content change), so a content change made while an item is hidden in the
  menu is not picked up until the item is shown again. Fitting stays first-fit
  greedy, so a narrow item after a wide one keeps its place on the strip instead
  of being pushed into the menu with everything that follows it.
- **`TitleBar` has no deactivated family** - WinUI's `TitleBar` dims eight
  regions (back button, pane toggle, left header, icon, title text, subtitle
  text, content presenter, right header) when the window loses activation,
  most to `TitleBarDeactivatedOpacity` (0.5) and the text elements to
  `TextFillColorTertiaryBrush` (`TitleBar.xaml:25-133`,
  `TitleBar_themeresources.xaml:86` under WinUI CommonStyles). `FluenceWindow`
  dims only its own title text; the icon, subtitle, and title-bar content
  slots stay at full opacity on deactivation. See `docs/winui-parity.md`
  section 8.
- **High contrast interaction states collapse into one** - Fluence's high
  contrast table resolves both `SubtleFillColorSecondary` and
  `SubtleFillColorTertiary` to `SystemColors.Control`, so hover, pressed, and
  selected visuals on menu items, list rows, and `NavigationView` items are
  indistinguishable from each other and from the resting surface. WinUI keeps
  these states visually distinct in high contrast by mapping pointer-over and
  pressed to two different System accent-derived brushes
  (`NavigationView_themeresources.xaml:142-143`). See `docs/winui-parity.md`
  section 7.
- **Pressed states are absent on stock WPF item containers** -
  `ListViewItem`, `ListBoxItem`, `TreeViewItem`, and `TabViewItem` derive from
  WPF base classes with no `IsPressed` concept the way `ButtonBase` has, so
  none of them draw a pressed visual state, unlike their WinUI counterparts.
  See `docs/winui-parity.md` section 8.
- **`NumberBox` `Compact` mode is a hover-reveal panel, not a popup** - WinUI's
  `Compact` spin-button mode opens an acrylic popup with 36px increment and
  decrement buttons; Fluence instead reveals an inline panel on hover. See
  `docs/winui-parity.md` sections 4 and 5.
- **Text controls have no `Header` or `Description` template parts** -
  `TextBox` and `PasswordBox` carry neither slot, so a consumer wanting a
  label or helper text above or below the field must add its own
  `TextBlock`; `NumberBox`'s header also does not dim when the control is
  disabled, unlike WinUI's. See `docs/winui-parity.md` section 5.
- **`TreeView`, `TabView`, and `PipsPager` geometry gaps** - `TreeView` and
  `TabView` selection and separator metrics, and `PipsPager` pip pitch and
  hover sizing, were not part of the current colour and role fix pass and
  still differ from their WinUI CommonStyles sources (`TabView` draws no
  inter-tab separator or corner fillets; `PipsPager` does not scale pips or
  use a pressed scale on its nav buttons). See `docs/winui-parity.md`
  section 6 for the full geometry table.

## net472 accessibility API gaps

The following Windows Presentation Foundation accessibility APIs were introduced
in .NET Framework 4.8 and are **not available on the `net472` TFM** this library
supports. Each entry documents the chosen fallback and why the gap is acceptable.
Reference: <https://learn.microsoft.com/dotnet/framework/whats-new/whats-new-in-accessibility>

- **`AutomationPeer.RaiseNotificationEvent`** (available from .NET Framework 4.8) - this
  API pushes an ad-hoc text announcement to assistive technologies without a
  corresponding UI Automation element. All live-region controls in this library
  (`InfoBar`, `ProgressBar`, `ProgressRing`, `TeachingTip`, and `TextBox`
  validation) use the net472-safe substitute: the element sets
  `AutomationProperties.LiveSetting` to `Polite` or `Assertive` in its template
  or peer constructor, and the peer calls
  `RaiseAutomationEvent(AutomationEvents.LiveRegionChanged)` when state changes.
  Screen readers that honour `LiveRegionChanged` (Narrator, NVDA, JAWS) announce
  the current `GetNameCore` text of the peer on that event, which is equivalent
  for the controlled-status use cases in this library.

- **`AutomationProperties.IsDialog`** (available from .NET Framework 4.8) - this
  property marks an element as a modal dialog surface so screen readers announce
  it as such when focus enters. `ContentDialog` does not set this property on
  net472. The fallback used is: the `ContentDialogAutomationPeer` returns
  `AutomationControlType.Window` from `GetAutomationControlTypeCore`, the dialog
  traps Tab focus inside its bounds during `ShowAsync`, and on open it declares
  an assertive UI Automation live region (`AutomationProperties.LiveSetting`) and
  raises `LiveRegionChanged` so Narrator, NVDA, and JAWS read the dialog `Title`
  as it appears. Assistive technologies therefore observe a Window-role boundary,
  focus containment, and an explicit open announcement, which together characterise
  a modal dialog. The behaviour gap is limited to the literal "dialog" role phrase
  that Narrator and JAWS emit when `IsDialog=true`; the structural, focus, and
  announcement semantics are present. Without the live region the overlay-hosted
  dialog (not a separate HWND) raised no event for assistive technologies to act
  on, so it was not read on open.

- **`AutomationProperties.HeadingLevel`** (available from .NET Framework 4.8) - this
  property allows elements to be reported as heading levels H1-H9 to assistive
  technologies, enabling document-style navigation with Narrator's heading-scan
  mode. Fluence controls do not use heading levels internally; applications
  consuming the library on net10.0-windows10.0.26100.0 may set this property
  freely. On net472 the property is absent and any XAML that references it will
  fail to compile unless guarded. The gap is acceptable because Fluence is a
  controls library, not a document renderer; section headings in consuming
  applications are app-layer concerns.

- **Automatic `PositionInSet` and `SizeOfSet` for `ItemsControl`** (available from
  .NET Framework 4.8) - on 4.8+ WPF automatically computes and exposes
  `PositionInSet` and `SizeOfSet` UI Automation properties for items inside an
  `ItemsControl`, so screen readers can announce "item 2 of 5" without explicit
  annotation. On net472 these values are not computed automatically. Fluence's
  automation peers do not currently override `GetPositionInSetCore` /
  `GetSizeOfSetCore`, so set position is not annotated explicitly on either TFM;
  on net472, controls such as `NavigationViewItem` inside a `NavigationView`,
  `TabViewItem` inside a `TabView`, and `PipsPager` dots therefore do not
  announce set position, and the application-item controls (`ListBox`,
  `ListView`, `TreeView`, `ComboBox`) rely solely on the 4.8+ automatic
  computation. Applications that require position announcements on net472 (or for
  any control) should set `AutomationProperties.PositionInSet` and
  `AutomationProperties.SizeOfSet` explicitly on each item in XAML or code.

## Deferred runtime test coverage

The following accessibility items are XAML-verified (the names and parts exist in
the committed templates) but do not have automated runtime interaction tests
because their rendering depends on host shell state that is difficult to
reproduce in the headless test harness:

- **`TeachingTip` `PART_AlternateCloseButton`** - the alternate close button lives
  inside a `Popup` subtree that is only in the visual tree while the tip is
  open and the primary close button is hidden. Its `AutomationProperties.Name`
  is verified by inspection of `TeachingTip.xaml`; an automated test would
  require the popup to be open, the primary close hidden, and Narrator focus
  routed into the popup subtree.

- **`TabView` scroll buttons** (`PART_ScrollDecreaseButton`, `PART_ScrollIncreaseButton`) -
  these buttons appear only when the tab strip overflows its container. Their
  `AutomationProperties.Name` values are verified by inspection of `TabView.xaml`;
  an automated test would require a `TabView` with enough tabs to trigger
  overflow in a measured layout pass, which the current STA test infrastructure
  does not size windows to guarantee.
