function Get-FluenceRemoteHostPath
{
    <#
    .SYNOPSIS
        Resolves the path to the out-of-process Fluence UI host executable.
    .DESCRIPTION
        The host executable is launched with Process.Start and never loaded into the calling
        process, so the path is edition-independent (unlike Get-FluenceLibraryPath's TFM branch).
    .PARAMETER ModuleRoot
        The module root directory containing the lib folder.
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory = $true)]
        [string]$ModuleRoot
    )

    $exe = [System.IO.Path]::Combine($ModuleRoot, 'lib', 'host', 'Fluence.Wpf.RemoteHost.exe')
    if (-not (Test-Path -LiteralPath $exe))
    {
        throw "Fluence.Wpf.RemoteHost.exe not found at: $exe. Run build\Build-Module.ps1 to stage the remote host."
    }

    return $exe
}
