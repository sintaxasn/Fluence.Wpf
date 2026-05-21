#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the combined Fluence.Wpf documentation site (Hugo + DocFX) into
    docs-site/public/, ready for GitHub Pages deploy.

.DESCRIPTION
    Runs the full pipeline end-to-end so the local artifact matches what the
    docs.yml workflow publishes:

      1. dotnet build Fluence.Wpf (Release, net10.0-windows10.0.26100.0)
         -> regenerates Fluence.Wpf.xml so DocFX has the latest API surface.
      2. docfx ./docs-site/docfx/docfx.json
         -> renders the API reference into ./docs-site/docfx/_site/.
      3. hugo --minify --source ./docs-site
         -> renders the conceptual site into ./docs-site/public/.
      4. Robocopy ./docs-site/docfx/_site/ into ./docs-site/public/api/.

    The combined ./docs-site/public/ directory is the artifact that
    upload-pages-artifact ships to deploy-pages in CI.

.PARAMETER SkipLibraryBuild
    Skip step 1 and reuse the existing Fluence.Wpf.xml file. Useful when
    iterating on docs only.

.PARAMETER SkipDocFx
    Skip the DocFX build entirely. The /api/ folder under public/ will be
    missing or stale, but the conceptual site still builds.

.PARAMETER Configuration
    .NET configuration for the library build. Defaults to Release for CI parity.

.PARAMETER Output
    Final output directory. Defaults to ./docs-site/public.

.EXAMPLE
    pwsh ./docs-site/scripts/build-docs.ps1

.EXAMPLE
    pwsh ./docs-site/scripts/build-docs.ps1 -SkipLibraryBuild -SkipDocFx
#>
[CmdletBinding()]
param(
    [switch]$SkipLibraryBuild,
    [switch]$SkipDocFx,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$Output
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Assert-Command {
    param([string]$Name, [string]$Hint)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' is not on PATH. $Hint"
    }
}

# Resolve key paths relative to the script so it works from any cwd.
$ScriptDir = Split-Path -Parent $PSCommandPath
$DocsSite = Resolve-Path (Join-Path $ScriptDir '..')
$RepoRoot = Resolve-Path (Join-Path $DocsSite '..')
$LibProject = Join-Path $RepoRoot 'Fluence.Wpf/Fluence.Wpf.csproj'
$DocfxConfig = Join-Path $DocsSite 'docfx/docfx.json'
$DocfxSite = Join-Path $DocsSite 'docfx/_site'
$HugoOutput = if ($Output) { (Resolve-Path -LiteralPath $Output -ErrorAction SilentlyContinue) } else { $null }
if (-not $HugoOutput) {
    $HugoOutput = Join-Path $DocsSite 'public'
}

Write-Host "Fluence.Wpf docs build" -ForegroundColor Magenta
Write-Host "  Repository    : $RepoRoot"
Write-Host "  docs-site root: $DocsSite"
Write-Host "  Output        : $HugoOutput"

Assert-Command -Name 'hugo' -Hint 'Install Hugo Extended >= 0.140 from https://gohugo.io/installation/.'

if (-not $SkipDocFx) {
    Assert-Command -Name 'docfx' -Hint "Install DocFX globally: dotnet tool install -g docfx"
}

if (-not $SkipLibraryBuild) {
    Assert-Command -Name 'dotnet' -Hint 'Install the .NET SDK from https://dotnet.microsoft.com/download.'
}

# --- 1. Build the library to refresh Fluence.Wpf.xml ------------------------
if (-not $SkipLibraryBuild) {
    Write-Step "Building Fluence.Wpf ($Configuration, net10.0-windows10.0.26100.0)"
    & dotnet build $LibProject `
        --configuration $Configuration `
        --framework 'net10.0-windows10.0.26100.0' `
        --verbosity minimal `
        --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }
} else {
    Write-Step "Skipping library build (--SkipLibraryBuild)"
}

# --- 2. Run DocFX -----------------------------------------------------------
if (-not $SkipDocFx) {
    Write-Step "Running DocFX -> $DocfxSite"
    # DocFX does not always purge stale output between runs. If the
    # docfx.json layout shifts (e.g. when we moved metadata to obj/api
    # and stopped emitting an api/ subdirectory), the old output stays
    # behind and gets merged into Hugo's /api/. Wipe it deterministically
    # before each build to guarantee parity with the docfx.json contract.
    if (Test-Path -LiteralPath $DocfxSite) {
        Remove-Item -LiteralPath $DocfxSite -Recurse -Force
    }
    Push-Location (Split-Path -Parent $DocfxConfig)
    try {
        & docfx (Split-Path -Leaf $DocfxConfig) --warningsAsErrors:false
        if ($LASTEXITCODE -ne 0) {
            throw "docfx failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
} else {
    Write-Step "Skipping DocFX build (--SkipDocFx)"
}

# --- 3. Run Hugo ------------------------------------------------------------
Write-Step "Running Hugo -> $HugoOutput"
if (Test-Path -LiteralPath $HugoOutput) {
    Remove-Item -LiteralPath $HugoOutput -Recurse -Force
}
Push-Location $DocsSite
try {
    & hugo --minify --gc --destination $HugoOutput --logLevel info
    if ($LASTEXITCODE -ne 0) {
        throw "hugo build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

# --- 4. Merge DocFX output under /api/ -------------------------------------
if (-not $SkipDocFx) {
    if (-not (Test-Path -LiteralPath $DocfxSite)) {
        throw "DocFX output directory not found at $DocfxSite. Did docfx succeed?"
    }
    $ApiTarget = Join-Path $HugoOutput 'api'
    Write-Step "Merging DocFX output -> $ApiTarget"
    New-Item -ItemType Directory -Force -Path $ApiTarget | Out-Null
    if ($IsWindows -or $env:OS -eq 'Windows_NT') {
        # Use robocopy on Windows: deterministic mirror copy with /MIR and
        # exit code 0-7 = success per robocopy convention.
        & robocopy $DocfxSite $ApiTarget /MIR /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
        $robocopyExit = $LASTEXITCODE
        if ($robocopyExit -ge 8) {
            throw "robocopy failed with exit code $robocopyExit."
        }
        # Reset $LASTEXITCODE so subsequent steps see success.
        $global:LASTEXITCODE = 0
    } else {
        # Cross-platform fallback: use Copy-Item.
        Copy-Item -Path (Join-Path $DocfxSite '*') -Destination $ApiTarget -Recurse -Force
    }
} else {
    Write-Step "Skipping /api/ merge (--SkipDocFx)"
}

Write-Step "Done"
Write-Host "Static site is ready at: $HugoOutput" -ForegroundColor Green
Write-Host ""
Write-Host "Preview locally:" -ForegroundColor Yellow
Write-Host "  cd `"$DocsSite`""
Write-Host "  hugo server --source . --renderToMemory --buildDrafts"
