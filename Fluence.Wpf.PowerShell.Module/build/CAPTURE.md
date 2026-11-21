# PowerShell screenshot capture

These PNGs show actual WPF controls rendered from the current PowerShell module by
`Fluence.Wpf.PowerShell.Module/build/Capture-Documentation.ps1`. The script loads the staged module
assemblies and opens each scene in a separate Windows PowerShell STA process.

The published captures use WPF `RenderTargetBitmap`. The module's Mica window surface is
transparent to offscreen rendering, so the script composites the window over the library's
`ApplicationBackgroundBrush`. The images show the control templates, title bar text and icon,
theme colors, accent colors, and selected states. They do not include the desktop, native DWM
backdrop, or a DWM shadow. The PNG outside the WPF window has no transparent shadow region.
The Windows screen capture API returned `The handle is invalid` in the capture host, including
when invoked through the standard screenshot helper. A desktop capture on an interactive Windows
session can use the script's `-CaptureMode Desktop` to record the real shadow if DWM draws it.

To regenerate all images after building and staging the module:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Fluence.Wpf.PowerShell.Module/build/Capture-Documentation.ps1
```

Scenes include message (normal and warning), form, image dialog, single and multiple list
selection, determinate and indeterminate progress, restart prompt, XAML hosted window, and purple
and green custom accents. Light and dark pairs are provided where the control benefits from a
direct comparison. The file name identifies the scene and theme.

## Controls Tour

`Capture-ControlsTour.ps1` is the source for the ten-page Controls Tour screenshots. It opens
the actual `examples/06-ControlsTour.ps1` window and runs its `-Initialize` block, so the data
grid contains the example's five PowerShell objects and category switching uses the script's
own handlers. Run it after staging the module:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File Fluence.Wpf.PowerShell.Module/build/Capture-ControlsTour.ps1
```

The default run creates 20 Light and Dark PNGs in ignored `artifacts/docs/screenshots/`.
`powershell-light.png` and `powershell-dark.png` show Inputs. The other categories use
`powershell-{color|selection|commands|data|navigation|status|surfaces|acrylic|layout}-{light|dark}.png`.
For a focused capture, pass `-Theme Dark -Category Color`; `-OutputDirectory` chooses another
candidate location. A fresh STA process is used for each theme, and each PNG replaces its
previous candidate atomically. The companion `powershell-controls-tour-*.json` files record
the source script, capture script, staged library hashes, Git HEAD and tracked dirty state.
Each capture also records the page viewport and direct content extent in device-independent
pixels, and the renderer fails when content exceeds the page viewport. To check the declared
minimum window, use `-WindowWidth 1000 -WindowHeight 880` with a separate `-OutputDirectory`
so the minimum-size images and receipts do not replace the default 1120 x 900 candidates.

These images are opaque WPF `RenderTargetBitmap` output composited over
`ApplicationBackgroundBrush`. They show the WPF control surface and theme resources, not
native DWM Mica, caption pixels, or the desktop shadow. Review the candidates visually before
copying them to the website. The opt-in `GalleryScreenshotHarness` delegates its Controls
Tour IDs to this same capture script, so it cannot produce an XAML-only replacement.

`Capture-Documentation.ps1` remains the producer for the separate PowerShell command scenes
described above; it does not capture the Controls Tour categories.
