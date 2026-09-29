# Documentation walkthrough sample

This runnable WPF app presents the seven C# tutorial scenarios documented in the [website repository](https://github.com/sintaxasn/Fluence.Wpf.Website/tree/main/docs/csharp) and its how-to guides. It uses the library project directly, so a package release is not needed to inspect the examples.

```powershell
dotnet run --project Fluence.Wpf.Docs.Walkthroughs/Fluence.Wpf.Docs.Walkthroughs.csproj
```

Use the scenario selector to explore each page. To refresh the light and dark tutorial images, run the same command with `-- --capture-all`. The app writes 14 PNGs to `docs/screenshots/tutorials/` after each scenario has rendered; import those captures into the separate website repository. It uses WPF `RenderTargetBitmap`, the same technique as the gallery documentation captures. Capture mode selects the opaque window fallback so the controls and theme colors remain legible. Native DWM Mica pixels, rounded window edges, and exterior shadows are composited outside WPF and therefore do not appear in these images. Run the app interactively to inspect those effects.
