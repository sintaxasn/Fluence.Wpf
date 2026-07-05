function Close-FluenceUiRunspace
{
    <#
    .SYNOPSIS
        Disposes the module-owned STA UI runspace, if one is open.
    .NOTES
        Registered as the module's OnRemove handler so a stray STA thread + WPF Application never
        outlives the session. The runspace is idle between dialogs (each ShowDialog pumps its own
        modal loop synchronously), so Close() + Dispose() cleanly terminates it.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    param()

    if ($null -ne $script:StaRunspace)
    {
        try
        {
            $script:StaRunspace.Close()
            $script:StaRunspace.Dispose()
        }
        catch
        {
            # Best-effort teardown on module unload; a runspace already faulted/closed must not
            # throw out of OnRemove.
            Write-Verbose "Close-FluenceUiRunspace: $_"
        }
        finally
        {
            $script:StaRunspace = $null
        }
    }
}
