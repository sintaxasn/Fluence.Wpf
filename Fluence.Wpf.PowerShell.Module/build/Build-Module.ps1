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

Write-Output 'Staged Fluence.Wpf assemblies into the module lib folder.'
