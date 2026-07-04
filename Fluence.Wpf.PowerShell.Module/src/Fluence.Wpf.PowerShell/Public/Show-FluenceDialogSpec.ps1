function Show-FluenceDialogSpec
{
    <#
    .SYNOPSIS
        Shows a composed dialog spec in-process and returns the clicked button plus harvested values.
    .DESCRIPTION
        Materializes a Fluence.Wpf.Specs.DialogSpec into a themed FluenceWindow on the Fluence UI
        thread and blocks until it closes. Before showing, the spec is round-tripped through the
        versioned binary spec serializer, so the in-process path exercises the exact wire format the
        out-of-process transports use. A non-cancel button validates the declarative rules inline
        (an error InfoBar keeps the dialog open); a cancel button, Esc, or closing the window
        returns Button = 'Cancelled' semantics without validating.
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
    .EXAMPLE
        $result = Show-FluenceDialogSpec -Spec $dialog
        $result.Button; $result.Values.Desk
    .OUTPUTS
        Fluence.SpecDialogResult
    .NOTES
        Establishes a WPF Application on a private STA thread when none exists; reuses a host
        application when one is already running. Blocks until the dialog closes.
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
        [switch]$Topmost
    )

    if ($Spec -isnot [Fluence.Wpf.Specs.DialogSpec])
    {
        throw "-Spec must be a Fluence.Wpf.Specs.DialogSpec from New-FluenceDialogSpec; got '$($Spec.GetType().FullName)'."
    }

    # Round-trip on the caller thread: validates the tree and proves the exact serialized form the
    # remote transports will carry (Phase 3-4 of the out-of-process design).
    $envelope = [Fluence.Wpf.Specs.SpecSerialization]::Serialize($Spec)

    $accentColor = $null
    if ($PSBoundParameters.ContainsKey('Accent'))
    {
        $accentColor = $Accent
    }

    $uiSpec = @{
        Envelope    = $envelope
        Theme       = $Theme
        Backdrop    = $Backdrop
        AccentColor = $accentColor
        Topmost     = [bool]$Topmost
    }

    $result = Invoke-OnFluenceUi -Script {
        param($s)

        # Ensure the Application exists and seed the three theme slots and accent. Mandatory
        # before showing a FluenceWindow; shared with the window host.
        Initialize-FluenceApplication -Theme $s.Theme -Backdrop $s.Backdrop -Accent $s.AccentColor

        $dialogSpec = [Fluence.Wpf.Specs.SpecSerialization]::Deserialize($s.Envelope)
        $window = [Fluence.Wpf.Specs.SpecMaterializer]::Materialize($dialogSpec)
        $window.Topmost = [bool]$s.Topmost
        $window.ShowAndCollect()
    } -ArgumentList @($uiSpec)

    if ($null -eq $result)
    {
        return [pscustomobject]@{
            PSTypeName = 'Fluence.SpecDialogResult'
            Button     = 'Cancelled'
            Values     = @{}
        }
    }

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
