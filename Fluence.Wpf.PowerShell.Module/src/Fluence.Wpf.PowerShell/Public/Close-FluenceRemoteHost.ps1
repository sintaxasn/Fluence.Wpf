function Close-FluenceRemoteHost
{
    <#
    .SYNOPSIS
        Shuts down the out-of-process Fluence UI host started by Show-FluenceRemoteDialog.
    .DESCRIPTION
        Requests a graceful host exit (killing the process after a short grace period if needed)
        and releases the pipes. Safe to call when no host was ever started; also runs
        automatically when the module is removed, so a stray child process never outlives the
        session.
    .EXAMPLE
        Close-FluenceRemoteHost
    .OUTPUTS
        None.
    .NOTES
        Does not require a host application. The next Show-FluenceRemoteDialog call starts a
        fresh host process.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    param()

    if ($null -ne $script:FluenceRemoteHost)
    {
        try
        {
            $script:FluenceRemoteHost.Dispose()
        }
        finally
        {
            $script:FluenceRemoteHost = $null
        }
    }
}
