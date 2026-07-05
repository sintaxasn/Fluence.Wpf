function Show-FluenceRemoteDialog
{
    <#
    .SYNOPSIS
        Shows a composed dialog spec in a separate, same-user host process and returns the clicked
        button plus harvested values.
    .DESCRIPTION
        Serializes a Fluence.Wpf.Specs.DialogSpec through the versioned binary spec serializer and
        sends it over anonymous pipes to a standalone Fluence UI host (Fluence.Wpf.RemoteHost.exe),
        which materializes and shows the identical dialog the in-process Show-FluenceDialogSpec
        renders. The host starts lazily on the first call and is reused for the rest of the
        session, so repeated dialog cycles never risk the classic in-process second-ShowDialog
        hang. Close-FluenceRemoteHost (also run automatically on module removal) tears the host
        down.
    .PARAMETER Spec
        The dialog specification from New-FluenceDialogSpec.
    .PARAMETER Theme
        Auto (default), Light, Dark, or HighContrast.
    .PARAMETER Backdrop
        Mica (default), Acrylic, Tabbed, None, or Auto.
    .PARAMETER Accent
        Optional accent color (System.Windows.Media.Color or a parseable string). Defaults to
        system accent.
    .PARAMETER Topmost
        Show above other windows.
    .PARAMETER TimeoutSeconds
        Optional number of seconds after which the host closes the dialog by itself; the result
        then carries Button = 'Cancelled', the same as a user dismissal.
    .EXAMPLE
        $result = Show-FluenceRemoteDialog -Spec $dialog -TimeoutSeconds 120
        $result.Button; $result.Values.Desk
    .OUTPUTS
        Fluence.SpecDialogResult
    .NOTES
        The host process requires the .NET Desktop Runtime matching the staged host build. The
        calling PowerShell edition does not matter; the spec crosses the process boundary as an
        opaque versioned envelope.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.SpecDialogResult')]
    param
    (
        [Parameter(Mandatory = $true)]
        [object]$Spec,

        [Parameter()]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme = 'Auto',

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop = 'Mica',

        [Parameter()]
        [System.Windows.Media.Color]$Accent,

        [Parameter()]
        [switch]$Topmost,

        [Parameter()]
        [ValidateRange(1, 86400)]
        [int]$TimeoutSeconds
    )

    if ($Spec -isnot [Fluence.Wpf.Specs.DialogSpec])
    {
        throw "-Spec must be a Fluence.Wpf.Specs.DialogSpec from New-FluenceDialogSpec; got '$($Spec.GetType().FullName)'."
    }

    # Serialize on the caller side: validates the tree and produces the exact envelope the host
    # deserializes, mirroring the in-process path's round-trip discipline.
    $request = [Fluence.Wpf.Specs.RemoteDialogRequest]::new()
    $request.SpecBase64 = [Fluence.Wpf.Specs.SpecSerialization]::SerializeToBase64($Spec)
    $request.Theme = $Theme
    $request.Backdrop = $Backdrop
    $request.Topmost = [bool]$Topmost
    if ($PSBoundParameters.ContainsKey('Accent'))
    {
        $request.AccentColorText = $Accent.ToString()
    }

    # Without a timeout the transport waits forever (a dialog legitimately waits on a human).
    # With one, allow the host a generous transport margin beyond the dialog's own self-dismiss.
    $transportTimeout = [System.Threading.Timeout]::InfiniteTimeSpan
    if ($PSBoundParameters.ContainsKey('TimeoutSeconds'))
    {
        $request.TimeoutSeconds = $TimeoutSeconds
        $transportTimeout = [timespan]::FromSeconds($TimeoutSeconds + 30)
    }

    if ($null -eq $script:FluenceRemoteHost)
    {
        $script:FluenceRemoteHost = [Fluence.Wpf.Specs.FluenceRemoteHostController]::new()
    }
    $script:FluenceRemoteHost.EnsureRunning((Get-FluenceRemoteHostPath -ModuleRoot $script:ModuleRoot))

    $result = $script:FluenceRemoteHost.ShowDialog($request, $transportTimeout)

    $values = @{}
    foreach ($key in $result.Values.Keys)
    {
        $values[$key] = $result.Values[$key]
    }
    return [pscustomobject]@{
        PSTypeName = 'Fluence.SpecDialogResult'
        Button     = $result.Button
        Values     = $values
    }
}
