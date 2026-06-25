<#
.SYNOPSIS
    Builds Fluence.Wpf for net472 and net8.0-windows and stages the assemblies into the module.
.NOTES
    Run from any location. Does not require a host application.
#>
[CmdletBinding()]
param
(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repo = 'F:\FRebuild\Fluence.Wpf'
$lib  = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\lib'
$proj = Join-Path $repo 'Fluence.Wpf\Fluence.Wpf.csproj'

$map = @{
    'net472'          = 'net472'
    'net8.0-windows'  = 'net8.0-windows10.0.26100.0'
}

foreach ($dest in $map.Keys)
{
    $tfm = $map[$dest]
    & dotnet build $proj -c $Configuration -f $tfm
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $tfm" }

    $src = Join-Path $repo "Fluence.Wpf\bin\$Configuration\$tfm"
    $out = Join-Path $lib $dest
    New-Item -ItemType Directory -Path $out -Force | Out-Null
    Get-ChildItem -Path $out -Filter '*.dll' | Remove-Item -Force
    Get-ChildItem -Path $src -Filter '*.dll' | Copy-Item -Destination $out -Force
}

Write-Host 'Staged Fluence.Wpf assemblies into the module lib folder.'
