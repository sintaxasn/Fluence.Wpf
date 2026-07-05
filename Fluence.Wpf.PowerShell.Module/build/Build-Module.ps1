<#
.SYNOPSIS
    Builds Fluence.Wpf for net472 and net8.0-windows10.0.26100.0 and stages the assemblies into the module.
.NOTES
    Run from any location. Does not require a host application.
#>
[CmdletBinding()]
param
(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$lib  = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\lib'
$proj = Join-Path $repo 'Fluence.Wpf\Fluence.Wpf.csproj'

$map = @{
    'net472'                     = 'net472'
    'net8.0-windows10.0.26100.0' = 'net8.0-windows10.0.26100.0'
}

# Remove any stale lib subfolders before staging to prevent orphaned TFM directories
# (e.g., a leftover net8.0-windows from a prior run) that would cause NU1012.
if (Test-Path $lib)
{
    foreach ($stale in (Get-ChildItem -Path $lib -Directory))
    {
        try
        {
            [System.IO.Directory]::Delete($stale.FullName, $true)
        }
        catch
        {
            throw "Could not remove stale lib subfolder '$($stale.FullName)': $_"
        }
    }
}

foreach ($dest in $map.Keys)
{
    $tfm = $map[$dest]
    & dotnet build $proj -c $Configuration -f $tfm
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $tfm" }

    $src = Join-Path $repo "Fluence.Wpf\bin\$Configuration\$tfm"
    $out = Join-Path $lib $dest
    New-Item -ItemType Directory -Path $out -Force | Out-Null
    Get-ChildItem -Path $src -Filter '*.dll' | Copy-Item -Destination $out -Force
}

# Stage the out-of-process dialog host. It is launched with Process.Start and never loaded
# in-process, so it ships as a single modern TFM regardless of the caller's PowerShell edition.
# The stale-cleanup loop above deletes every lib subfolder, so lib\host is recreated each run.
$hostTfm  = 'net8.0-windows10.0.26100.0'
$hostProj = Join-Path $repo 'Fluence.Wpf.RemoteHost\Fluence.Wpf.RemoteHost.csproj'
& dotnet build $hostProj -c $Configuration -f $hostTfm
if ($LASTEXITCODE -ne 0) { throw 'Build failed for Fluence.Wpf.RemoteHost' }

$hostSrc = Join-Path $repo "Fluence.Wpf.RemoteHost\bin\$Configuration\$hostTfm"
$hostOut = Join-Path $lib 'host'
New-Item -ItemType Directory -Path $hostOut -Force | Out-Null
Get-ChildItem -Path $hostSrc -File |
    Where-Object { $_.Extension -in '.dll', '.exe', '.json' } |
    Copy-Item -Destination $hostOut -Force

Write-Output 'Staged Fluence.Wpf assemblies and the remote host into the module lib folder.'
