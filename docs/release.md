# Release Checklist

Use this checklist before publishing a package or tagging a release.

## Package Readiness

- Confirm `README.md`, `CHANGELOG.md`, and public docs under `docs/` describe the current public surface.
- Confirm every public API has XML documentation and that generated `Fluence.Wpf.xml` is included in package output.
- Confirm internal helpers such as `CaptionButtonChrome` and `WindowPolicy` are not documented as consumer-facing controls.
- Confirm screenshots under `docs/screenshots/` are current when visual changes affect the gallery banner.

## Local Gates

Run from the repository root:

```powershell
dotnet restore Fluence.Wpf.sln
dotnet build Fluence.Wpf.sln -c Debug
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug
slopwatch.exe analyze --no-baseline --exclude ".history/**, **/obj/**, **/bin/**"
```

When demo source samples change, also build the gallery on both supported target frameworks:

```powershell
dotnet build Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -c Debug -f net472
dotnet build Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -c Debug -f net10.0-windows10.0.26100.0
```

## Pack Check

```powershell
dotnet pack Fluence.Wpf/Fluence.Wpf.csproj -c Release -o ./artifacts
```

Inspect the package for the assembly, XML documentation file, license, README, and theme resources.

## Future Docs Site Direction

- Use Hextra for Hugo for conceptual docs.
- Use DocFX for API reference from XML documentation.
- Publish the combined site with GitHub Pages Actions.

Do not add a docs-site build to release gating until the repo has an explicit docs-site project and publishing workflow.
