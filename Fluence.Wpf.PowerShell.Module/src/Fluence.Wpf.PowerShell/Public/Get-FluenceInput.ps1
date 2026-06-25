function Get-FluenceInput
{
    <#
    .SYNOPSIS
        Shows a themed Fluent input dialog and returns the captured value, or $null on cancel.
    .DESCRIPTION
        Wraps a single-prompt Show-FluenceDialog with an OK (default) and Cancel button. Returns
        the captured input value when the user clicks OK, or $null when the user cancels.
    .PARAMETER Message
        The prompt label shown in the dialog (Mandatory).
    .PARAMETER Title
        The window title. Defaults to 'Fluence'.
    .PARAMETER DefaultValue
        The initial value pre-filled in the input control.
    .PARAMETER InputType
        The input control type. One of: Text (default), Multiline, Password, Number, Checkbox,
        Toggle, Choice, Date, Time, FileOpen, FileSave, FolderOpen, Link.
    .PARAMETER Theme
        Auto (default), Light, Dark, or HighContrast.
    .PARAMETER Backdrop
        Mica (default), Acrylic, Tabbed, None, or Auto.
    .EXAMPLE
        $name = Get-FluenceInput -Message 'Enter your name'
    .EXAMPLE
        $age = Get-FluenceInput -Message 'Enter your age' -InputType Number -DefaultValue 25
    .OUTPUTS
        System.Object
    .NOTES
        Establishes a WPF Application on a private STA thread when none exists; reuses a host
        application (for example PSADT) when one is already running. Blocks until the dialog closes.
    #>
    [CmdletBinding()]
    [OutputType([object])]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Message,

        [Parameter()]
        [string]$Title = 'Fluence',

        [Parameter()]
        [object]$DefaultValue,

        [Parameter()]
        [ValidateSet('Text', 'Multiline', 'Password', 'Number', 'Checkbox', 'Toggle',
            'Choice', 'Date', 'Time', 'FileOpen', 'FileSave', 'FolderOpen', 'Link')]
        [string]$InputType = 'Text',

        [Parameter()]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme,

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop
    )

    $promptParams = @{
        Name      = 'Input'
        Message   = $Message
        InputType = $InputType
    }

    if ($PSBoundParameters.ContainsKey('DefaultValue'))
    {
        $promptParams['DefaultValue'] = $DefaultValue
    }

    $prompt = New-FluencePrompt @promptParams

    $buttons = @(
        New-FluenceButton 'OK' -IsDefault
        New-FluenceButton 'Cancel' -IsCancel
    )

    $dialogParams = @{
        Title   = $Title
        Prompts = @($prompt)
        Buttons = $buttons
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

    if ($result.Cancelled)
    {
        return $null
    }

    return $result.Input
}
