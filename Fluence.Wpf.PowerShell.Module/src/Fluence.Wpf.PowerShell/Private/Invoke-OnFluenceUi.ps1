function Invoke-OnFluenceUi
{
    <#
    .SYNOPSIS
        Runs a script on a UI (STA) thread that owns the single WPF Application.
    .DESCRIPTION
        Three cases: (1) a host application already runs its own pumped UI thread, so marshal
        onto its dispatcher; (2) this thread is STA and no application exists, so run inline and
        own the application here; (3) MTA host, so run on a persistent STA runspace we own.

        Returns the script's result as a bare value in every case. Only the runspace path wraps the
        output in a single-element PSDataCollection (PowerShell.Invoke); that is unwrapped here so
        callers never re-implement the unwrap.
    .NOTES
        Does not itself require a host application; it establishes one. State flag $script:OwnsApplication
        keeps routing stable once we have created our own application.
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

    if ($script:OwnsApplication -ne $true -and $null -ne [System.Windows.Application]::Current)
    {
        $app = [System.Windows.Application]::Current
        $func = [System.Func[object]] { & $Script @ArgumentList }
        return $app.Dispatcher.Invoke($func)
    }

    if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -eq [System.Threading.ApartmentState]::STA)
    {
        $script:OwnsApplication = $true
        return (& $Script @ArgumentList)
    }

    $script:OwnsApplication = $true
    $output = Invoke-InFluenceStaRunspace -Script $Script -ArgumentList $ArgumentList

    # Normalize the return shape so every case yields the bare value. PowerShell.Invoke wraps output
    # in a PSDataCollection; the script's real payload is a single object (the dialog/window result
    # hashtable), so unwrap a non-empty collection to its last element and an empty collection to
    # $null. This keeps callers free of a duplicated IList-unwrap and lets them null-check the result.
    if ($output -is [System.Collections.IList])
    {
        if ($output.Count -ge 1)
        {
            return $output[$output.Count - 1]
        }
        return $null
    }
    return $output
}
