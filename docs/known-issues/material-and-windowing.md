# Material and windowing investigations

Start with the matching issue in [Known issues](../../KNOWN_ISSUES.md). In-app acrylic samples a WPF visual. Window backdrops come from Windows composition. Use the [gallery acrylic sample](../../Fluence.Wpf.Demo/Pages/GalleryAcrylicPage.xaml) as a known in-app setup and [CONTRIBUTING.md](../../CONTRIBUTING.md) for the test commands.

## Acrylic uses a solid fill with an unsupported source

**Context and cause.** [AcrylicSourceMap.Evaluate](../../Fluence.Wpf/Helpers/AcrylicSourceMap.cs#L61) requires loaded, disjoint source and target elements in one live WPF HWND. It rejects recursion through visual ancestry and brush references, native child HWNDs, transforms other than translation or positive axis-aligned scale, and unsupported source clips. A valid map also requires visible overlap. A WPF popup creates its own window even if it has a placement target in the owner's tree. [WPF popup behavior](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/popup)

**Previous work.** The source-map tests cover accepted siblings, popup rejection, nested brush cycles, rounded clips, and scrolling viewport clips. [Lifetime tests](../../Fluence.Wpf.Tests/Control/AcrylicMaterialLifetimeTests.cs) cover source retargeting, unload, and reference release. These checks establish the source contract.

**Investigate.** Start with two overlapping siblings and a patterned source. Compare the fallback reason with the same fixture after moving the source to an ancestor, another window, or a rounded clip. At the source-map breakpoint record both HWNDs, the effective transform and clip, the target crop, and any traversed brush references. For a live mutation, trace how the [controller](../../Fluence.Wpf/Helpers/AcrylicMaterialController.cs#L288) updates its reference watcher and queues the next evaluation. [Source map tests](../../Fluence.Wpf.Tests/Control/AcrylicSourceMapTests.cs) · [Gallery fallback tests](../../Fluence.Wpf.Tests/Gallery/Pages/GalleryAcrylicPageTests.cs)

**Done when.** The supported configuration recovers on load, valid retargeting, and viewport changes. Invalid scenarios return their specific fallback reasons without sampling their own output. Establish this baseline before changing rendering policy, recipes, or budgets.

## In-app acrylic can become opaque under rendering restrictions

**Context and cause.** The [material policy](../../Fluence.Wpf/Helpers/AcrylicMaterialPolicy.cs#L36) evaluates High Contrast first, then design mode, the forced-fallback recipe, transparency, power, source validity, and graphics or blend capability. The [renderer](../../Fluence.Wpf/Helpers/AcrylicMaterialRenderer.cs#L155) also requires tier 2, pixel shader 3 support, and hardware at process and window level. High Contrast fallback uses the live system Window color.

**Previous work.** Policy tests inject competing conditions and assert precedence. Renderer tests load the packaged shader and compare CPU blend goldens. Environment tests check recovery after a simulated graphics mode change on the same HWND. Real device loss, remote desktop, and cross-monitor hardware recovery need separate evidence. [Policy tests](../../Fluence.Wpf.Tests/Theming/AcrylicMaterialPolicyTests.cs) · [Renderer tests](../../Fluence.Wpf.Tests/Control/AcrylicMaterialRendererTests.cs) · [Environment tests](../../Fluence.Wpf.Tests/Windowing/AcrylicEnvironmentMonitorTests.cs)

**Investigate.** Read the fallback reason alongside `IsUsingFallback`. Log the app and OS High Contrast flags, the transparency setting, power state, both render-mode settings, WPF rendering tier, and pixel shader support. Test one condition at a time from an eligible sibling scene. Check shader resource load if hardware is available and the renderer reports `BlendUnavailable`. Use the dispatcher to observe both activation and recovery. Policy tests can inject `DeviceLost`. A native reproduction is required to establish which driver events the application receives.

**Done when.** The reported reason explains the observed opaque pixels, priority rules remain covered, and clearing a recoverable condition restores the material while the original host remains loaded. Record the OS, session type, GPU, and native capture conditions for hardware changes.

## Windows Energy Saver can leave in-app acrylic active

**Context and cause.** [NativeMethods.TryGetPowerSaving](../../Fluence.Wpf/Native/NativeMethods.cs#L125) queries the Battery Saver flag from `GetSystemPowerStatus`. The [environment monitor](../../Fluence.Wpf/Helpers/AcrylicEnvironmentMonitor.cs#L44) observes `GUID_POWER_SAVING_STATUS` on each HWND and accepts a 4-byte payload with value 0 or 1. Registration or query failure yields an unknown state and a solid fallback. Win32 documents a separate Energy Saver setting and notification. [System power status](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-system_power_status) · [Power setting GUIDs](https://learn.microsoft.com/en-us/windows/win32/power/power-setting-guids)

**Previous work.** Environment tests use a fake native adapter to verify malformed payload rejection, registration ownership, resume requery, and failed-read behavior. Physical Battery Saver qualification remains open. [Environment tests](../../Fluence.Wpf.Tests/Windowing/AcrylicEnvironmentMonitorTests.cs)

**Investigate.** On applicable Windows builds, capture timestamped native signals while switching the setting on and off, changing AC power where available, and resuming the same window. Record the build, observed Battery Saver flag, notification GUID, payload and fallback reason. Compare the documented Energy Saver notification and its state values before expanding the policy. Cover initialization, events before or after the first query, unknown state, OS-version behavior, and one subscription per HWND in the fake adapter.

**Done when.** Supported power transitions have a documented signal and demonstrate fallback and recovery on the same host. Failure to determine state still selects the conservative fallback. Subscription and shutdown behavior remain covered.

## Large or competing acrylic surfaces can become opaque

**Context and cause.** The [budget](../../Fluence.Wpf/Helpers/AcrylicMaterialBudget.cs#L35) accounts for the source map's padded target in physical pixels, with ceilings applied to each dimension before multiplication. It admits two live surfaces at most, up to 2,146,176 pixels per surface and 2,172,672 pixels in aggregate. Blur padding can make the reservation much larger than the visible bounds. A release queues a refresh on the waiting owner's dispatcher.

**Previous work.** Budget tests cover caps, opaque output without a noise visual, weak ownership, competition across windows, resize and shrink recovery, and cross-dispatcher release. The recent renderer change retained the 90-DIP padding and passed a single controlled native comparison. Wider-blur GPU cost and budget recovery across scales remain open. [Budget tests](../../Fluence.Wpf.Tests/Control/AcrylicMaterialBudgetTests.cs) · [Budget refresh](../../Fluence.Wpf/Helpers/AcrylicMaterialController.cs#L208)

**Investigate.** Start with one eligible surface, then resize it across the limit and add a second and third host, including one in a different window. Record the target crop, padded rectangle, scale, requested pixel count, actual fallback reason, release trigger, and dispatcher progress. Close or shrink a competing host and wait for a full reevaluation. For a proposed cap change, measure frame timing and memory with representative scrolling and overlapping hosts at the affected DPI.

**Done when.** Budget acceptance is explained by the measured padded size and active reservations, rejected targets fall back, and remaining hosts recover after a release. Treat the [blur footprint and display scale](#acrylic-blur-can-look-sharper-at-higher-display-scales) as inputs when qualifying any change to the caps.

## Acrylic blur can look sharper at higher display scales

**Context and cause.** [`BuildBlurredSource`](../../Fluence.Wpf/Helpers/AcrylicMaterialRenderer.cs#L235) uses the smaller device scale to decide when to split a 90-DIP Gaussian into two 64-DIP passes. The passes approximate the desired Gaussian variance while bounding rendering work. WPF caps each pass at 100 physical pixels, so both can reach the cap at higher scale. The source map reserves the 90-DIP blur footprint. [WPF native blur code](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/WpfGfx/core/resources/BlurEffect.cpp#L214-L340)

**Previous work.** Commit 39605424 followed a 144-DPI Default Light comparison where one pass missed the cyan-edge tolerance by 19.08%. Two passes reduced the error to 4.97%, with a mean color difference of 0.59 ΔE00 and a 95th percentile of 1.13 against WinUI Gallery 3.0.4.0. Native window-content captures were checked against guarded screen references before use. Those results cover that recipe, DPI, device, and fixture.

**Investigate.** First qualify source geometry, policy, budget, and owned-pixel capture. Compare a matched cyan-edge fixture against WinUI at affected display scales using physical pixel widths and repeated captures. Log DPI, blur-pass radii, source crop, material reason, and timing. The prior comparison used blue-minus-red edge contrast because luminance alone could not reliably measure the cyan edge. Reuse its 10% width tolerance and color limits of 3 mean and 8 at the 95th percentile when repeating that scene.

**Done when.** Native results cover the affected scales with repeatable dimensions and an identified render path. Record any change in color, edge width, and budget recovery alongside GPU timing. The retained renderer tests should continue to exercise single and split blur paths.

## Window acrylic uses a different effect on Windows 10

**Context and cause.** [OS capability detection](../../Fluence.Wpf/Helpers/OsVersionHelper.cs#L82) enables the legacy accent path on Windows 10 builds 17063 and later. [WindowPolicy.BuildBackdropPlan](../../Fluence.Wpf/Controls/WindowPolicy.cs#L371) selects that path after capability, transparency, and theme checks. [FluenceWindow](../../Fluence.Wpf/Controls/FluenceWindow.cs#L1228) packs a themed tint with fixed alpha 0xF0 and switches to simpler blur during native move or resize. The initial glass thickness also depends on the requested backdrop and window shadow setting.

**Previous work.** Policy and native tests pin the Windows 10 plan, fallback, Windows 11 route, and color packing. Existing Windows 11 captures exercise different window composition and the in-app material. The legacy tint, drag transition, and glass-frame appearance remain hardware investigations. [Window policy tests](../../Fluence.Wpf.Tests/Windowing/WindowPolicyTests.cs) · [Native method tests](../../Fluence.Wpf.Tests/Windowing/NativeMethodsTests.cs) · [Documented DWM backdrop types](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwm_systembackdrop_type)

**Investigate.** On a qualifying Windows 10 build, compare two otherwise identical FluenceWindow fixtures with Acrylic and `None`. Check requested versus effective backdrop, native call results, packed tint before and after a theme change, and the mouse move or resize start and end messages. Capture the frame edges at rest, during drag, and after restoration. Record which observations support a native tint, policy, or glass change.

**Done when.** Repeated captures on the target build show the expected tint, move or resize transition, and restoration, with reproducible frame behavior. Establish [composited capture](#rendertargetbitmap-screenshots-omit-window-backdrop-effects) before claiming native visual qualification.

## Mica layer colors can vary by display path

**Context and cause.** A historical 10-bit SDR path with advanced color off showed effective alpha steps of 0, 1/3, 2/3, and 1 over Light Mica, including in a plain WPF WindowChrome fixture. The observed result was specific to that display path. [WindowPolicy.ShouldApplyContentLayerPreBlend](../../Fluence.Wpf/Controls/WindowPolicy.cs#L557) gates an opaque pre-blend on color depth, advanced color, theme, and effective backdrop. Dark Mica and High Contrast are excluded. Tabbed eligibility came from material-family inference.

**Previous work.** The historical control with backdrop `None` showed ordinary WPF blending. A later run on an advanced-color-enabled path showed full precision. Those observations motivate the gate, with current [policy tests](../../Fluence.Wpf.Tests/Windowing/WindowPolicyTests.cs#L1038) pinning its behavior. The fallback is applied by [FluenceWindow](../../Fluence.Wpf/Controls/FluenceWindow.cs#L1157), including refreshes when the display or window monitor changes.

**Investigate.** Record OS and GPU driver versions, HDR, advanced color flags, bits per channel, the active monitor, effective backdrop, theme, and whether pre-blending applied. Compare matched solid and alpha swatches over the target backdrop and over `None`. Include Light and Dark and repeat across the display settings under review. Inspect [native display information](../../Fluence.Wpf/Native/NativeMethods.cs#L848) with the screen captures. Treat a vendor or surface-format explanation as a hypothesis until the recorded display configuration and swatch results distinguish it.

**Done when.** A fix reproduces the effect on the recorded display path and proves that its own compensation is limited to the affected combinations. Verify theme and monitor changes, fallback cleanup, and the caller's window-scoped override. Use [composited capture](#rendertargetbitmap-screenshots-omit-window-backdrop-effects) for the alpha measurements.

## RenderTargetBitmap screenshots omit window backdrop effects

**Context and cause.** `RenderTargetBitmap` renders the WPF visual tree. Windows composites a native Mica or Acrylic backdrop and transparent window shadows outside that tree. The [gallery screenshot harness](../../Fluence.Wpf.Tests/Tools/GalleryScreenshotHarness.cs#L53) uses the visual path for ordinary images and has separate opt-in capture for native in-app Acrylic. [RenderTargetBitmap contract](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.imaging.rendertargetbitmap)

**Previous work.** The in-app Acrylic comparison qualified PrintWindow captures against guarded screen pixels for its Light fixture. The ordinary harness also produces software fallback images. Those paths establish different claims. Native window backdrops require the composed visible window.

**Investigate.** Use two otherwise identical FluenceWindow scenes with `None` and the desired `SystemBackdropType`. Compare the ordinary WPF output with the owned, visible window's composed screen pixels. Check window identity, clip to its actual display bounds, and exclude overlapping HWNDs and focus or capture changes. Bind each result to the capture method, application build, OS, DPI, theme, effective backdrop, and device settings. Scope candidates and provenance to the repository's [capture and validation workflow](../../CONTRIBUTING.md).

**Done when.** The test produces repeatable composed pixels for the intended visible HWND and distinguishes native effects from the WPF visual-only images. This proof is needed for the [Windows 10 Acrylic](#window-acrylic-uses-a-different-effect-on-windows-10) and [Mica alpha](#mica-layer-colors-can-vary-by-display-path) investigations.
