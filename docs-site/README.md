# Fluence.Wpf documentation site

Source for the static documentation site published to GitHub Pages at
[https://sintaxasn.github.io/Fluence.Wpf/](https://sintaxasn.github.io/Fluence.Wpf/).

## Stack

- **Hugo** static site generator with the **[Hextra](https://imfing.github.io/hextra/)** theme for conceptual docs.
- **DocFX** for API reference generated from the library's XML documentation comments.
- **GitHub Pages** via GitHub Actions for hosting (see `../.github/workflows/docs.yml`).

The repository-level markdown under `../docs/` and the brand assets under `../assets/` are the source of truth. They are mounted into Hugo at build time (see `hugo.toml`) so the docs site never duplicates that content.

## Prerequisites

Install the following once per dev machine:

- **Hugo Extended >= 0.161** ([install instructions](https://gohugo.io/installation/)). Confirm with `hugo version`.
- **Go >= 1.22** ([install instructions](https://go.dev/doc/install)). Required only for Hugo Modules. Confirm with `go version`.
- **.NET SDK 10** (already required by the main repo). Confirm with `dotnet --version`.
- **DocFX** as a global .NET tool:

  ```powershell
  dotnet tool install -g docfx
  ```

## Local build

From the repository root:

```powershell
pwsh ./docs-site/scripts/build-docs.ps1
```

That script:

1. Builds `Fluence.Wpf` in Release on `net10.0-windows10.0.26100.0` so DocFX has fresh XML documentation.
2. Runs `docfx ./docs-site/docfx/docfx.json` to render the API site under `./docs-site/docfx/_site/`.
3. Runs `hugo --minify --source ./docs-site --destination ./public` to render the conceptual site.
4. Merges the DocFX output into `./docs-site/public/api/`.

Open `./docs-site/public/index.html` (or any other file under `public/`) to inspect the build. Alternatively, run Hugo in watch mode for fast iteration on conceptual docs only:

```powershell
hugo server --source ./docs-site --renderToMemory --buildDrafts
```

## What gets published

| Path under public site | Source |
|------------------------|--------|
| `/`                    | `docs-site/content/_index.md` |
| `/screenshots/`        | `docs-site/content/screenshots/_index.md` plus `../docs/screenshots/` static assets |
| `/docs/`               | Selected `../docs/*.md` files mounted in `hugo.toml` |
| `/docs/known-issues/`  | `../KNOWN_ISSUES.md` (inlined via shortcode) |
| `/changelog/`          | `../CHANGELOG.md` (inlined via shortcode) |
| `/api/`                | `docs-site/docfx/_site/` after DocFX build |

`docs-site/content/api/_index.md` only documents the landing page used to bridge the Hugo navigation into the DocFX section.

## Editing content

- Edit conceptual docs at the source: `../docs/getting-started.md`, `../docs/theming.md`, `../docs/controls.md`, `../docs/migration-guide.md`, `../docs/contributing.md`.
- Update `../KNOWN_ISSUES.md` or `../CHANGELOG.md` and the synced pages refresh automatically on the next build.
- Place new repo-wide guides under `../docs/` (without `_internal/` or `plans/`), then add an explicit mount in `hugo.toml` before linking them from the docs section.
- API reference content is regenerated from XML doc comments on the library; the docs site never edits those files directly.

## Branding

Brand tokens (colors, typography, radii) and assets are kept aligned with the runtime library:

- Color and brush tokens are derived from `../Fluence.Wpf/Themes/Colors/*.xaml`, `../Fluence.Wpf/Themes/Accent/Accent.xaml`, and `../Fluence.Wpf/Themes/Typography/Typography.xaml` and mirrored into `assets/css/custom.css`.
- The rainbow Fluence wordmark logos live in `../assets/fluence-wpf-banner-{light,dark}.svg` and are surfaced as the site navbar logo through Hugo's static mount.
- Theme-aware gallery screenshots are sourced from `../docs/screenshots/` and exposed under `/images/screenshots/`.

## Visual QA checklist

After a substantive change, exercise the published site (or a local preview) against this checklist before declaring the docs healthy:

- **Theme cycle.** Toggle Light / Dark from the navbar control. Both modes should show readable text on every page, the navbar logo should swap to the dark or light banner SVG, and code blocks should retain contrast.
- **Accent integrity.** Buttons, focus rings, sidebar active items, and link hover states should resolve to the Fluence accent (`#0078D4` in light, `#4FA3E8` in dark). The horizontal rainbow accent strip under the API navbar should match the runtime gallery wordmark.
- **High-contrast respect.** If your OS is set to a high-contrast theme (or `prefers-contrast: more` is enabled), borders should sharpen and focus rings should grow to 3px without breaking layout.
- **Navigation.** The sidebar should list Documentation > Getting started -> Theming -> Controls -> Migration guide -> Contributing -> Known issues in that order. API Reference should link to `/api/` and load the DocFX site. Changelog should render the contents of `../CHANGELOG.md` inline.
- **Cross-doc links.** From `/docs/getting-started/`, click through to `theming` and `controls`. Both must resolve via clean URLs (no `.md` suffix). Repo-root references (`../AGENTS.md`, `../docs-site/README.md`) should open `github.com/sintaxasn/Fluence.Wpf/blob/main/...` in a new tab.
- **API reference parity.** `/api/` should render with the Fluence-styled DocFX template (rainbow accent strip under the navbar, Segoe UI, Fluent corner radii). A namespace navigation and a member page should both load.
- **Screenshots.** `/screenshots/` should render paired Light / Dark gallery captures plus MVVM and PowerShell app captures. `/docs/controls/#screenshots` should show the representative capture row.
- **Mobile.** Test the home page and a docs page at 375x812 viewport. Navbar should collapse, sidebar should hide behind a toggle, and feature cards should stack.

## Troubleshooting

- **`module github.com/imfing/hextra: ... unknown`** - run `hugo mod get -u ./...` once from `docs-site/` to pull the module.
- **`Error: failed to mount`** - confirm the mount sources exist relative to `docs-site/` (Hugo resolves them from this directory, not the repo root).
- **DocFX warnings about missing XML** - rebuild the library with `dotnet build ../Fluence.Wpf/Fluence.Wpf.csproj -c Release -f net10.0-windows10.0.26100.0` before running `docfx`.
- **`/api/` 404 after deploy** - check the GitHub Actions log for the build job. The merge step prints `Merging DocFX output -> .../public/api`; if missing, DocFX likely failed before the merge ran.
- **`/changelog/` is empty** - the `readroot` shortcode is silent on missing files; confirm `../CHANGELOG.md` is still listed in the `assets/_root` mount in `hugo.toml`.
