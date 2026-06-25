function Invoke-InFluenceStaRunspace
{
    <#
    .SYNOPSIS
        Runs a script on a persistent module-owned STA runspace (for MTA hosts such as pwsh).
    .NOTES
        The runspace persists for the session so the WPF Application and dispatcher survive between
        dialogs. Each call runs synchronously; ShowDialog pumps its own modal loop on the STA thread.
    #>
    [CmdletBinding()]
    [OutputType([object])]
    param
    (
        [Parameter(Mandatory = $true)]
        [scriptblock]$Script,

        [Parameter()]
        [object[]]$ArgumentList = @()
    )

    if ($null -eq $script:StaRunspace -or $script:StaRunspace.RunspaceStateInfo.State -ne 'Opened')
    {
        $script:StaRunspace = [runspacefactory]::CreateRunspace()
        $script:StaRunspace.ApartmentState = 'STA'
        $script:StaRunspace.ThreadOptions  = 'ReuseThread'
        $script:StaRunspace.Open()

        $bootstrap = [powershell]::Create()
        $bootstrap.Runspace = $script:StaRunspace
        $null = $bootstrap.AddScript("Import-Module '$($script:ModuleManifestPath)' -Force").Invoke()

        # Capture import errors before disposing. A faulted bootstrap leaves an Opened-but-empty
        # runspace; reusing it would fail every later call with a confusing error for the rest of
        # the session, so tear it down here and rebuild on the next call.
        $bootstrapFailed = $bootstrap.HadErrors
        $bootstrapError = $null
        if ($bootstrap.Streams.Error.Count -gt 0)
        {
            $bootstrapFailed = $true
            $bootstrapError = $bootstrap.Streams.Error[0]
        }
        $bootstrap.Dispose()

        if ($bootstrapFailed)
        {
            $script:StaRunspace.Dispose()
            $script:StaRunspace = $null
            if ($null -ne $bootstrapError)
            {
                throw $bootstrapError
            }
            throw "Failed to import the Fluence.Wpf.PowerShell module into the STA runspace."
        }
    }

    # Transport the scriptblock by TEXT and re-create it inside the module's session state on the STA
    # runspace. PowerShell.AddScript has no ScriptBlock overload, so a [scriptblock] passed directly
    # would recompile at the runspace's global scope where module-PRIVATE functions are invisible.
    # Invoking the re-created block through the module object (& $module $inner @args) runs it in
    # module scope, so private helpers such as Invoke-FluenceWindow resolve.
    $ps = [powershell]::Create()
    $ps.Runspace = $script:StaRunspace
    $null = $ps.AddScript({
            param($ScriptText, $ScriptArgs)
            $module = Get-Module -Name 'Fluence.Wpf.PowerShell'
            $inner = [scriptblock]::Create($ScriptText)
            & $module $inner @ScriptArgs
        })
    $null = $ps.AddArgument($Script.ToString())
    $null = $ps.AddArgument([object[]]$ArgumentList)
    try
    {
        $output = $ps.Invoke()
        if ($ps.HadErrors -and $ps.Streams.Error.Count -gt 0)
        {
            throw $ps.Streams.Error[0]
        }
        return $output
    }
    finally
    {
        $ps.Dispose()
    }
}
