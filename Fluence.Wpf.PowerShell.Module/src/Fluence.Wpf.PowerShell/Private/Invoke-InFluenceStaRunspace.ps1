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
        $bootstrap.Dispose()
    }

    $ps = [powershell]::Create()
    $ps.Runspace = $script:StaRunspace
    $null = $ps.AddScript($Script)
    foreach ($arg in $ArgumentList)
    {
        $null = $ps.AddArgument($arg)
    }
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
