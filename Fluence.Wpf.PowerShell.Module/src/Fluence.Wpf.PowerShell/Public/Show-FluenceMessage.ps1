function Show-FluenceMessage
{
    <#
    .SYNOPSIS
        Shows a themed Fluent message dialog and returns the name of the button the user clicked.
    .DESCRIPTION
        A thin wrapper around Show-FluenceDialog that maps a named button preset (OK, OKCancel,
        YesNo, YesNoCancel) to the correct button objects, passes an optional icon severity to
        render a leading InfoBar, and returns the clicked button name as a string instead of the
        full DialogResult.
    .PARAMETER Message
        One or more message lines displayed in the dialog.
    .PARAMETER Title
        The window title. Defaults to 'Fluence'.
    .PARAMETER Icon
        The icon and color to display in the leading InfoBar.
        Info (default), Success, Warning, Error, or Question. InfoBarSeverity has no Question or Info
        member, so Info and Question both render as the Informational severity (no distinct question
        glyph); use the button set (for example -Buttons YesNo) to convey a confirmation.
    .PARAMETER Buttons
        Named button set: OK (default), OKCancel, YesNo, or YesNoCancel.
    .PARAMETER Theme
        Auto (default), Light, Dark, or HighContrast.
    .PARAMETER Backdrop
        Mica (default), Acrylic, Tabbed, None, or Auto.
    .EXAMPLE
        Show-FluenceMessage -Message 'Proceed?' -Icon Question -Buttons YesNo
    .EXAMPLE
        $answer = Show-FluenceMessage -Message 'Save changes?' -Buttons OKCancel -Icon Warning
        if ($answer -eq 'OK') { Save-Data }
    .OUTPUTS
        System.String
    .NOTES
        Establishes a WPF Application on a private STA thread when none exists; reuses a host
        application (for example PSADT) when one is already running. Blocks until the dialog closes.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [string[]]$Message,

        [Parameter()]
        [string]$Title = 'Fluence',

        [Parameter()]
        [ValidateSet('Info', 'Success', 'Warning', 'Error', 'Question')]
        [string]$Icon = 'Info',

        [Parameter()]
        [ValidateSet('OK', 'OKCancel', 'YesNo', 'YesNoCancel')]
        [string]$Buttons = 'OK',

        [Parameter()]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme,

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop
    )

    $presetButtons = Get-FluenceButtonPreset -Preset $Buttons

    $dialogParams = @{
        Message = $Message
        Title   = $Title
        Icon    = $Icon
        Buttons = $presetButtons
    }

    if ($PSBoundParameters.ContainsKey('Theme'))
    {
        $dialogParams['Theme'] = $Theme
    }

    if ($PSBoundParameters.ContainsKey('Backdrop'))
    {
        $dialogParams['Backdrop'] = $Backdrop
    }

    $result = Show-FluenceDialog @dialogParams

    return Resolve-FluenceClickedButton -Result $result -Buttons $presetButtons
}
