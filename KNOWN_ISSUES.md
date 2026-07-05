# Known issues and follow-ups

This file tracks optional follow-ups and deliberate non-features. Filed bugs with
reproductions live on the issue tracker; this is the consolidated view for
maintainers.

## Current follow-ups (not defects)

- **`TabView` drag-to-reorder** - `TabView` / `TabViewItem` ship with closable
  tabs, an add-tab button, per-tab icons, overflow scroll, and width / overlay
  modes. Drag-and-drop tab reordering (including cross-window tear-off) is **not**
  implemented; consumers that need it should handle `PreviewMouseMove` / drag-drop
  themselves. This is the main remaining gap vs. WinUI 3 `TabView`.
- **Navigation back-stack** - `NavigationView.IsBackButtonVisible` +
  `IsBackEnabled` + `BackRequested` are exposed, but the library does **not**
  track page history. The demo does not use the back button; consumers are
  expected to own their own back stack and route `BackRequested`.
- **`RenderTargetBitmap` vs DWM backdrop** - DWM Mica / Acrylic is composed by
  the window manager and is **not** visible to `RenderTargetBitmap`. The
  screenshot harness hosts the gallery inside a plain `Window` with a solid
  `SolidBackgroundFillColorBaseBrush`. Automated capture of the full
  `FluenceWindow` chrome needs a different approach (e.g. `PrintWindow` /
  GDI screen capture).
- **`DatePicker` / `TimePicker` selector flyouts** - the flyouts present plain
  scrollable selector lists. They do **not** implement WinUI's infinitely
  looping selectors, nor the WinUI centered accent highlight band with the
  foreground flip over the selected row (`DatePicker_themeresources.xaml`
  `HighlightRect` / `MonochromaticOverlayPresenter`). The highlight band is
  coupled to the looping-selector interaction model, so both are deferred
  together; the looping omission is already noted in code at `DatePicker.cs`
  (around line 606) and `TimePicker.cs` (around line 511).
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
- **`ContentDialog` smoke layer and motion** - the dialog always paints its
  smoke (dimming) layer; there is **no** WinUI `DialogShowingWithoutSmokeLayer`
  state. It also has **no** `FullDialogSizing` stretch mode and **no** exit
  (`DialogHidden`) reverse animation; the entrance motion is implemented.
- **`BreadcrumbBar` ellipsis overflow** - the bar does **not** collapse leading
  crumbs into an ellipsis (WinUI collapses them into an `E712` ellipsis item
  with a flyout). Long trails extend to their natural width and clip when
  constrained.
- **`PipsPager` scrolling and nav-button scale** - the pager uses a centered
  re-rendering window (already noted in code at `PipsPager.cs` around lines
  65-70). It does **not** implement WinUI's edge-pip scale-down or the
  stationary edge-scrolling viewport, and the navigation buttons do **not** use
  WinUI's pressed `0.875` scale.
- **Out-of-process dialog host: no parent-death detection** - the standalone UI
  host (`Fluence.Wpf.RemoteHost.exe`, launched by `Show-FluenceRemoteDialog`) is
  torn down cleanly on `Close-FluenceRemoteHost` and on module removal (the
  `.psm1` registers an `OnRemove` handler). It has **no** parent-process watch in
  v1: if the owning PowerShell process is killed hard (for example `Stop-Process
  -Force` or a crash) rather than exited cleanly, the child host can be orphaned
  until it is dismissed or the session's anonymous pipes are garbage-collected.
  The mitigation for scripts is to keep dialogs bounded with `-TimeoutSeconds` so
  an orphan self-dismisses. A future version could add a
  `Process.GetProcessById(parentPid)` exit watch on the host side if demanded.
- **Remote host: `Ping`-timeout drain fallback is verified by reasoning, not a
  dedicated test** - a non-destructive `Ping` timeout leaves the host's eventual
  reply unread, so `FluenceRemoteHostController` joins that orphaned read before the
  next read (`DrainOrphanedResponseRead`). The recovery branch that kills the host
  when a prior orphan still has not drained after a second full timeout window is a
  defensive liveness path covered by inspection + the passing suite, not a dedicated
  test: reproducing it needs a host stub that stays genuinely stuck across two full
  timeout windows, which the current `Fluence.Wpf.RemoteHost` test harness has no
  hook for. Behaviour is bounded (at most one orphan is ever outstanding). One
  consequence: this drain fallback kills the host whenever a prior orphan cannot be
  drained within the current call's timeout, regardless of that call's own
  non-destructive intent, so two `Ping` calls with timeouts shorter than the host's
  round-trip (a misuse) can terminate an alive host on the second call. Keep `Ping`
  timeouts comfortably above the expected round-trip.
- **Spec images: no decode-dimension cap (decompression-bomb residual)** -
  `SpecMaterializer.LoadImageSourceFromPath` and `LoadImageSourceFromBase64`
  restrict image sources to a scheme allow-list (`file` paths, UNC paths, and
  `pack://` application resources; remote schemes such as `http`, `https`, and
  `ftp` are rejected before WPF issues any request) and cap Base64 payloads at
  `MaxImageBytes` (64 MB) before decode. Neither loader sets
  `BitmapImage.DecodePixelWidth`, so a small, well-formed input that decodes to
  enormous pixel dimensions (a classic decompression bomb) can still allocate a
  large bitmap at decode time. This is accepted for the same-user v1 threat
  model: the spec author is the script runner materializing their own dialog,
  not an untrusted third party. If untrusted specs ever become a scenario (for
  example specs sourced from a network call or another user), a future
  hardening pass should set `DecodePixelWidth` (and/or inspect the encoded
  header's declared dimensions before allocating) to bound decoded memory.
- **Remote host: broad handle inheritance and post-exit-only stderr drain** - the
  controller launches `Fluence.Wpf.RemoteHost.exe` with the two anonymous-pipe
  handles marked inheritable via a plain `Process.Start` (no `STARTUPINFOEX`
  `PROC_THREAD_ATTRIBUTE_HANDLE_LIST`), so on Windows the child inherits **every**
  currently inheritable handle in the parent, not only the two pipes. This is a
  deliberate v1 simplification: the host is a trusted, same-user, same-machine
  child, so the broad inheritance is acceptable. Launching across a trust boundary
  (for example a SYSTEM-to-user handoff) would require an explicit inherited-handle
  list. Separately, the host's redirected stderr is only drained after the process
  exits (in the failure-diagnostic path); it never writes enough during normal
  operation to fill the buffer, but if per-frame host logging is added it must be
  drained asynchronously (`BeginErrorReadLine`) to avoid a full-buffer stall.

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
