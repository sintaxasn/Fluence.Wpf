function Invoke-OnFluenceUi
{
    <#
    .SYNOPSIS
        Runs a script on a UI (STA) thread that owns the single WPF Application.
    .DESCRIPTION
        Three cases: (1) a host application already runs its own pumped UI thread, so marshal
        onto its dispatcher; (2) this thread is STA and no application exists, so run inline and
        own the application here; (3) MTA host, so run on a persistent STA runspace we own.
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
    return (Invoke-InFluenceStaRunspace -Script $Script -ArgumentList $ArgumentList)
}
