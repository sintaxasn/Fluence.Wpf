$script:ModuleRoot = $PSScriptRoot
$script:ModuleManifestPath = Join-Path $PSScriptRoot 'Fluence.Wpf.PowerShell.psd1'

# Dot-source private then public functions.
$private = @(Get-ChildItem -Path (Join-Path $script:ModuleRoot 'Private') -Filter '*.ps1' -ErrorAction SilentlyContinue)
$public  = @(Get-ChildItem -Path (Join-Path $script:ModuleRoot 'Public')  -Filter '*.ps1' -ErrorAction SilentlyContinue)
foreach ($file in @($private + $public))
{
    . $file.FullName
}

# Load the Fluence.Wpf assembly for this edition (idempotent across runspaces).
Import-FluenceLibrary -ModuleRoot $script:ModuleRoot

Export-ModuleMember -Function @($public | ForEach-Object { $_.BaseName })
