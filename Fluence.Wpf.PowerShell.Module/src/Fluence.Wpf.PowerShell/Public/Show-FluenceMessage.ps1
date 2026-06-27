function Show-FluenceMessage
{
    <#
    .SYNOPSIS
        Shows a themed Fluent message dialog and returns the name of the button the user clicked.
    .DESCRIPTION
        A thin wrapper around Show-FluenceDialog that maps a named button preset (OK, OKCancel,
        YesNo, YesNoCancel) to the correct button objects, renders the message as wrapping
        TextBlocks with an optional leading severity FontIcon, and returns the clicked button name
        as a string instead of the full DialogResult. For cancel-less presets (OK, YesNo), closing
        the dialog with the title-bar X or Esc returns the safe button name (OK or No) rather than
        $null, so a guard such as `if ($answer -ne 'No')` does not run the affirmative branch on a
        dismiss.
    .PARAMETER Message
        One or more message lines displayed in the dialog.
    .PARAMETER Title
        The window title. Defaults to 'Fluence'.
    .PARAMETER Icon
        The severity icon to display to the left of the message text.
        Info (default), Success, Warning, Error, or Question. Each severity maps to a distinct
        Segoe Fluent glyph and themed brush: Success, Warning, and Error use the InfoBar severity
        glyphs; Info uses the Informational glyph; Question uses the Segoe Fluent Help glyph with
        the neutral brush. The message renders as wrapping TextBlocks - not inside an InfoBar.
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
