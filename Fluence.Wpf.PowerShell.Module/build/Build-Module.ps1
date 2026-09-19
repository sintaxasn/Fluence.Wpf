<#
.SYNOPSIS
    Stages the Fluence.Wpf assemblies for net472 and net8.0-windows10.0.26100.0 into the module's lib folder.
.DESCRIPTION
    Copies the library build output for the two editions the module loads (net472 for Windows
    PowerShell 5.1, net8.0-windows10.0.26100.0 for PowerShell 7, which rolls forward onto the .NET 9
    and 10 runtimes) from Fluence.Wpf/bin/<Configuration>/<tfm> into
    src/Fluence.Wpf.PowerShell/lib/<tfm>. Stale lib subfolders are removed first so an orphaned TFM
    folder from an earlier run never ships.

    The library is not built here unless -Build is passed: CI builds the solution once and this
    script stages what that build produced. A missing build output fails with the exact dotnet
    command that produces it.
.PARAMETER Configuration
    The MSBuild configuration to stage from. Defaults to Release.
.PARAMETER Build
    Build Fluence.Wpf for both target frameworks before staging.
.EXAMPLE
    pwsh -NoProfile -File build/Build-Module.ps1
.EXAMPLE
    pwsh -NoProfile -File build/Build-Module.ps1 -Configuration Debug -Build
.NOTES
    Run from any location. Does not require a host application.
#>
[CmdletBinding()]
param
(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [switch]$Build
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$lib = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\lib'
$project = Join-Path $repo 'Fluence.Wpf\Fluence.Wpf.csproj'
$targetFrameworks = @('net472', 'net8.0-windows10.0.26100.0')

if ($Build)
{
    foreach ($tfm in $targetFrameworks)
    {
        & dotnet build $project -c $Configuration -f $tfm
        if ($LASTEXITCODE -ne 0)
        {
            throw "dotnet build failed for $tfm ($Configuration)."
        }
    }
}

# Verify every source before touching the lib folder, so a partial build never half-stages.
$sources = @{}
foreach ($tfm in $targetFrameworks)
{
    $source = Join-Path $repo "Fluence.Wpf\bin\$Configuration\$tfm"
    $dll = Join-Path $source 'Fluence.Wpf.dll'
    if (-not (Test-Path -LiteralPath $dll))
    {
        throw "Fluence.Wpf build output not found: $dll. Build it first with: dotnet build `"$project`" -c $Configuration -f $tfm  (or pass -Build)."
    }
    $sources[$tfm] = $source
}

# Remove stale lib subfolders (for example a leftover net8.0-windows from an earlier target name).
if (Test-Path -LiteralPath $lib)
{
    foreach ($stale in (Get-ChildItem -LiteralPath $lib -Directory))
    {
        try
        {
            $stagingRoot = [System.IO.Path]::GetFullPath($lib).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
            $stalePath = [System.IO.Path]::GetFullPath($stale.FullName)
            if (-not $stalePath.StartsWith($stagingRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
                ($stale.Attributes -band [System.IO.FileAttributes]::ReparsePoint))
            {
                throw "Refusing to delete a staging directory outside '$stagingRoot' or through a reparse point: $stalePath"
            }
            [System.IO.Directory]::Delete($stalePath, $true)
        }
        catch
        {
            throw "Could not remove stale lib subfolder '$($stale.FullName)': $($_.Exception.Message)"
        }
    }
}

foreach ($tfm in $targetFrameworks)
{
    $destination = Join-Path $lib $tfm
    $null = New-Item -ItemType Directory -Path $destination -Force
    Get-ChildItem -LiteralPath $sources[$tfm] -Filter '*.dll' | Copy-Item -Destination $destination -Force
    $version = [System.Reflection.AssemblyName]::GetAssemblyName((Join-Path $destination 'Fluence.Wpf.dll')).Version
    Write-Output "Staged Fluence.Wpf $version ($Configuration, $tfm) into $destination"
}
