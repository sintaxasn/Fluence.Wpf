function Get-FluenceLibraryPath
{
    <#
    .SYNOPSIS
        Resolves the path to Fluence.Wpf.dll for the running PowerShell edition.
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory = $true)]
        [string]$ModuleRoot,

        [Parameter()]
        [string]$Edition = $PSEdition
    )

    if ($Edition -eq 'Core')
    {
        $tfm = 'net8.0-windows10.0.26100.0'
    }
    else
    {
        $tfm = 'net472'
    }

    $dll = [System.IO.Path]::Combine($ModuleRoot, 'lib', $tfm, 'Fluence.Wpf.dll')
    if (-not (Test-Path -LiteralPath $dll))
    {
        throw "Fluence.Wpf.dll not found for edition '$Edition' at: $dll"
    }

    return $dll
}
