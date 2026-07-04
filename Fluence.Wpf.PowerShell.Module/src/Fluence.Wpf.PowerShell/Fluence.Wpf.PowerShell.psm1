$script:ModuleRoot = $PSScriptRoot
$script:ModuleManifestPath = Join-Path $PSScriptRoot 'Fluence.Wpf.PowerShell.psd1'

# The cached out-of-process dialog host controller; Show-FluenceRemoteDialog creates it lazily
# and Close-FluenceRemoteHost (or module removal) tears it down.
$script:FluenceRemoteHost = $null

# Load the WPF framework assemblies before dot-sourcing functions, so that System.Windows.* types
# resolve both for the host router and for public function parameter types that are bound at
# dot-source time (for example Show-FluenceDialog's -Accent and -ParentWindow).
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

# Dot-source private then public functions.
$private = @(Get-ChildItem -Path (Join-Path $script:ModuleRoot 'Private') -Filter '*.ps1' -ErrorAction SilentlyContinue)
$public  = @(Get-ChildItem -Path (Join-Path $script:ModuleRoot 'Public')  -Filter '*.ps1' -ErrorAction SilentlyContinue)
foreach ($file in @($private + $public))
{
    . $file.FullName
}

# Load the Fluence.Wpf assembly for this edition (idempotent across runspaces).
Import-FluenceLibrary -ModuleRoot $script:ModuleRoot

# Tear the out-of-process dialog host down with the module, so a stray child process never
# outlives the session.
$MyInvocation.MyCommand.ScriptBlock.Module.OnRemove = {
    Close-FluenceRemoteHost -ErrorAction SilentlyContinue
}

Export-ModuleMember -Function @($public | ForEach-Object { $_.BaseName })
