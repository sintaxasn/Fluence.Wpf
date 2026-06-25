function New-FluencePrompt
{
    <#
    .SYNOPSIS
        Builds a single input-prompt specification for Show-FluenceDialog.
    .DESCRIPTION
        Returns a Fluence.Prompt object describing one input field: its name (the result key),
        message, input type, default value, and optional validation rules.
    .PARAMETER Name
        The result key under which the captured value is returned. Defaults to an auto name if omitted.
    .PARAMETER Message
        The label shown above (or beside) the input control.
    .PARAMETER InputType
        One of: Text, Multiline, Password, Number, Checkbox, Toggle, Choice, Date, Time,
        FileOpen, FileSave, FolderOpen, Link.
    .PARAMETER DefaultValue
        The initial value.
    .PARAMETER ValidateSet
        For Choice prompts, the allowed values. Required when InputType is Choice.
    .PARAMETER As
        For Choice prompts, how to render the set: Combo (default) or Radio.
    .PARAMETER ValidateNotEmpty
        Require a non-whitespace value before the dialog can close on a non-cancel button.
    .PARAMETER ValidatePattern
        A regular expression the value must match.
    .PARAMETER ValidateScript
        A scriptblock that receives the value and returns $true when valid.
    .EXAMPLE
        New-FluencePrompt -Name User -Message 'Account name' -ValidateNotEmpty
    .OUTPUTS
        Fluence.Prompt
    .NOTES
        Does not require a host application; this only builds a specification object.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.Prompt')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds an in-memory specification object; changes no system state.')]
    param
    (
        [Parameter()]
        [string]$Name,

        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Message,

        [Parameter()]
        [ValidateSet('Text', 'Multiline', 'Password', 'Number', 'Checkbox', 'Toggle',
            'Choice', 'Date', 'Time', 'FileOpen', 'FileSave', 'FolderOpen', 'Link')]
        [string]$InputType = 'Text',

        [Parameter()]
        [object]$DefaultValue,

        [Parameter()]
        [string[]]$ValidateSet,

        [Parameter()]
        [ValidateSet('Combo', 'Radio')]
        [string]$As = 'Combo',

        [Parameter()]
        [switch]$ValidateNotEmpty,

        [Parameter()]
        [string]$ValidatePattern,

        [Parameter()]
        [scriptblock]$ValidateScript
    )

    if ($InputType -eq 'Choice' -and ($null -eq $ValidateSet -or $ValidateSet.Count -eq 0))
    {
        throw "A Choice prompt requires -ValidateSet."
    }

    if ([string]::IsNullOrWhiteSpace($Name))
    {
        $Name = 'Input_' + [guid]::NewGuid().ToString('N').Substring(0, 8)
    }

    $prompt = [pscustomobject]@{
        PSTypeName       = 'Fluence.Prompt'
        Name             = $Name
        Message          = $Message
        InputType        = $InputType
        DefaultValue     = $DefaultValue
        ValidateSet      = $ValidateSet
        As               = $As
        ValidateNotEmpty = [bool]$ValidateNotEmpty
        ValidatePattern  = $ValidatePattern
        ValidateScript   = $ValidateScript
    }

    return $prompt
}
