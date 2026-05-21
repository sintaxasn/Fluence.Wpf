# Release Checklist

Use this checklist before publishing a package or tagging a release.

## Package Readiness

- Confirm `README.md`, `CHANGELOG.md`, and public docs under `docs/` describe the current public surface.
- Confirm every public API has XML documentation and that `Fluence.Wpf.xml` is included in package output.
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

When demo source samples change, also build the gallery:

```powershell
dotnet build Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -c Debug
```

## Pack Check

```powershell
dotnet pack Fluence.Wpf/Fluence.Wpf.csproj -c Release -o ./artifacts
```

Inspect the package for the assembly, XML documentation file, license, README, and theme resources.

## Docs Site

The hosted docs site is built and deployed by [`.github/workflows/docs.yml`](../.github/workflows/docs.yml):

- Conceptual docs are rendered by Hugo + the Hextra theme. Source markdown stays under [`docs/`](.) and is mounted into the site at build time.
- API reference is rendered by DocFX from `Fluence.Wpf.xml`.
- Hugo and DocFX outputs are merged into a single static artifact and published to GitHub Pages.

Release rules:

- A failing docs build does **not** block the existing build/test pipeline ([`.github/workflows/build.yml`](../.github/workflows/build.yml)). Treat the docs workflow as a follow-up signal, not a release gate, until intentionally promoted.
- When visual changes affect the gallery banner, regenerate `docs/screenshots/` and run `pwsh ./docs-site/scripts/build-docs.ps1` locally before tagging to make sure the published site has fresh assets.

See [`docs-site/README.md`](../docs-site/README.md) for local preview and customization guidance.
