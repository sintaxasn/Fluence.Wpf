<#
.SYNOPSIS
    Packages the Fluence.Wpf.PowerShell module as a ZIP and a PowerShell Gallery .nupkg.
.DESCRIPTION
    Invokes Build-Module.ps1 to ensure assemblies are staged, then produces two
    distributable artifacts in the output directory:

      - Fluence.Wpf.PowerShell-<version>.zip   (drop-in for PSModulePath)
      - Fluence.Wpf.PowerShell.<version>.nupkg  (PowerShell Gallery format)

    The .nupkg is produced by Publish-Module targeting a temporary local file-based
    PSRepository. No network calls are made and nothing is published to PSGallery.
.PARAMETER Configuration
    MSBuild configuration passed to Build-Module.ps1. Defaults to 'Release'.
.PARAMETER OutputPath
    Directory that receives the zip and nupkg. Defaults to
    <repo>\Fluence.Wpf.PowerShell.Module\artifacts.
.NOTES
    Run from any location. Does not require a host application.
    Requires PowerShell 5.1 or 7+.
#>
[CmdletBinding()]
param
(
    [string]$Configuration = 'Release',
    [string]$OutputPath    = ''
)

$ErrorActionPreference = 'Stop'

$repo   = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$modSrc = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell'
$psd1   = Join-Path $modSrc 'Fluence.Wpf.PowerShell.psd1'

if ([string]::IsNullOrWhiteSpace($OutputPath))
{
    $OutputPath = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\artifacts'
}

# 1. Stage assemblies via Build-Module.ps1
$buildScript = Join-Path $PSScriptRoot 'Build-Module.ps1'
Write-Output "Invoking Build-Module.ps1 -Configuration $Configuration ..."
& $buildScript -Configuration $Configuration
if ($LASTEXITCODE -ne 0)
{
    throw "Build-Module.ps1 failed with exit code $LASTEXITCODE"
}

# 2. Read the module version from the manifest
$manifest = Import-PowerShellDataFile -Path $psd1
$version  = $manifest.ModuleVersion
Write-Output "Module version: $version"

# 3. Ensure the output directory exists
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# 4. Produce ZIP
# Compress-Archive with the module folder path includes the folder as the top-level
# entry, giving the required Fluence.Wpf.PowerShell\ root in the archive.
$zipName = "Fluence.Wpf.PowerShell-$version.zip"
$zipPath = Join-Path $OutputPath $zipName
Write-Output "Creating ZIP: $zipPath ..."
if (Test-Path $zipPath)
{
    [System.IO.File]::Delete($zipPath)
}
Compress-Archive -Path $modSrc -DestinationPath $zipPath
Write-Output "ZIP produced: $zipPath"

# 5. Produce NUPKG via Publish-Module targeting a temporary local PSRepository.
#    A unique repository name avoids collisions in concurrent builds.
$repoName    = "FluenceTempRepo_$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
$repoDir     = Join-Path ([System.IO.Path]::GetTempPath()) $repoName
$nupkgName   = "Fluence.Wpf.PowerShell.$version.nupkg"
$nupkgPath   = Join-Path $OutputPath $nupkgName

New-Item -ItemType Directory -Path $repoDir -Force | Out-Null

Write-Output "Creating NUPKG via Publish-Module to temp repository: $repoDir ..."

# Ensure NuGet provider is available (required for PSGet v2 publish)
Get-PackageProvider -Name NuGet -ForceBootstrap | Out-Null

Register-PSRepository -Name $repoName -SourceLocation $repoDir -PublishLocation $repoDir -InstallationPolicy Trusted

try
{
    Publish-Module -Path $modSrc -Repository $repoName -NuGetApiKey 'local' -Force

    # Find the nupkg Publish-Module deposited in the temp repo dir
    $produced = Get-ChildItem -Path $repoDir -Filter '*.nupkg' | Select-Object -First 1
    if ($null -eq $produced)
    {
        throw "Publish-Module completed but no .nupkg was found in: $repoDir"
    }

    if (Test-Path $nupkgPath)
    {
        [System.IO.File]::Delete($nupkgPath)
    }
    Copy-Item -LiteralPath $produced.FullName -Destination $nupkgPath -Force
    Write-Output "NUPKG produced: $nupkgPath"
}
finally
{
    Unregister-PSRepository -Name $repoName -ErrorAction SilentlyContinue
    if (Test-Path $repoDir)
    {
        [System.IO.Directory]::Delete($repoDir, $true)
    }
}

# 6. Report produced artifacts
$zipInfo   = Get-Item $zipPath
$nupkgInfo = Get-Item $nupkgPath
Write-Output 'Artifacts:'
Write-Output "  $($zipInfo.Name)  ($([int]($zipInfo.Length / 1KB)) KB)"
Write-Output "  $($nupkgInfo.Name)  ($([int]($nupkgInfo.Length / 1KB)) KB)"
