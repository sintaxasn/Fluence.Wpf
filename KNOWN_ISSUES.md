# Known Issues

Validated follow-ups for the first Fluence.Wpf release. This list tracks current limitations only; resolved migration notes are intentionally omitted.

## Current Follow-Ups

- **TabView drag reorder**
  - Status: Not implemented.
  - Evidence: `TabView` and `TabViewItem` support add, close, icons, width modes, close-button overlay modes, and overflow scrolling, but there is no drag-event or reorder API in `Fluence.Wpf/Controls/TabView*.cs`.
  - Next: Define the reorder API, add keyboard-accessible move commands, and test direct-item and item-source modes.

- **TreeView large-data virtualization**
  - Status: Performance follow-up.
  - Evidence: `Fluence.Wpf/Themes/Controls/TreeView.xaml` uses smooth pixel scrolling and does not enable recycling virtualization. This is fine for the current demo trees, but not ideal for very large hierarchies.
  - Next: Prototype a virtualized TreeView style and decide whether it should be default or opt-in.

- **Per-control documentation screenshots**
  - Status: Documentation backlog.
  - Evidence: `GalleryScreenshotHarness` currently generates the home banner screenshots only.
  - Next: Add deterministic capture targets for individual controls at 100 percent and 150 percent scale.

- **DWM backdrop capture**
  - Status: Technical limitation.
  - Evidence: Mica and Acrylic are composed by DWM and are not captured by `RenderTargetBitmap`.
  - Next: Keep bitmap captures focused on control surfaces. Add a separate Windows screen-capture path only if full chrome screenshots become required.

- **Library-owned navigation history**
  - Status: Deliberate non-feature.
  - Evidence: `NavigationView` raises back and item events, while page routing and history remain consumer-owned. The demo shell has its own simple history for gallery navigation.
  - Next: Keep the library decoupled from page lifetime unless a future request asks for an optional history helper.
