function Get-FluenceInput
{
    <#
    .SYNOPSIS
        Shows a themed Fluent input dialog and returns the captured value, or $null on cancel.
    .DESCRIPTION
        Wraps a single-prompt Show-FluenceDialog with an OK (default) and Cancel button. Returns
        the captured input value when the user clicks OK, or $null when the user cancels or the
        dialog times out.
    .PARAMETER Message
        The prompt label shown in the dialog (Mandatory).
    .PARAMETER Title
        The window title. Defaults to 'Fluence'.
    .PARAMETER DefaultValue
        The initial value pre-filled in the input control.
    .PARAMETER InputType
        The input control type. One of: Text (default), Multiline, Password, Number, Checkbox,
        Toggle, Choice, Date, Time, FileOpen, FileSave, FolderOpen, Link. A List prompt is not
        offered here because it picks from a set rather than capturing a typed value; use
        Show-FluenceListSelection for that.
    .PARAMETER ValidateSet
        The allowed values for a Choice prompt. Required when -InputType is Choice.
    .PARAMETER As
        How a Choice prompt renders its values: Combo (default) or Radio.
    .PARAMETER Timeout
        Seconds after which the dialog closes on its own and $null is returned. Between 1 and 86400.
    .PARAMETER Countdown
        Show the remaining seconds in the OK button's caption. Requires -Timeout.
    .PARAMETER Theme
        Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process;
        the first Fluence call in a process applies Auto.
    .PARAMETER Backdrop
        Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the
        process; the first Fluence call in a process applies Mica.
    .EXAMPLE
        $name = Get-FluenceInput -Message 'Enter your name'
    .EXAMPLE
        $age = Get-FluenceInput -Message 'Enter your age' -InputType Number -DefaultValue 25
    .EXAMPLE
        $edition = Get-FluenceInput -Message 'Edition' -InputType Choice -ValidateSet 'Standard', 'Pro' -As Radio
    .EXAMPLE
        $server = Get-FluenceInput -Message 'Server name' -DefaultValue 'localhost' -Timeout 20 -Countdown
        if ($null -eq $server) { $server = 'localhost' }

        Falls back to the default after twenty unattended seconds.
    .OUTPUTS
        System.Object
    .NOTES
        Uses the calling STA thread or a module-owned STA runspace. An existing host application
        is supported only when the command already runs on its dispatcher thread. Blocks until the dialog closes.
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
        [string[]]$ValidateSet,

        [Parameter()]
        [ValidateSet('Combo', 'Radio')]
        [string]$As,

        [Parameter()]
        [ValidateRange(1, 86400)]
        [int]$Timeout,

        [Parameter()]
        [switch]$Countdown,

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

    foreach ($name in @('DefaultValue', 'ValidateSet', 'As'))
    {
        if ($PSBoundParameters.ContainsKey($name))
        {
            $promptParams[$name] = $PSBoundParameters[$name]
        }
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

    foreach ($name in @('Theme', 'Backdrop', 'Timeout', 'Countdown'))
    {
        if ($PSBoundParameters.ContainsKey($name))
        {
            $dialogParams[$name] = $PSBoundParameters[$name]
        }
    }

    $result = Show-FluenceDialog @dialogParams

    if ($result.Cancelled -or $result.TimedOut)
    {
        return $null
    }

    return $result.Input
}
