function New-FluenceButton
{
    <#
    .SYNOPSIS
        Builds a single button specification for Show-FluenceDialog.
    .PARAMETER Text
        The button caption (and the default result key).
    .PARAMETER Name
        The result key; defaults to Text.
    .PARAMETER IsDefault
        Mark as the default button (activated by Enter).
    .PARAMETER IsCancel
        Mark as the cancel button (activated by Esc; skips input validation).
    .OUTPUTS
        Fluence.Button
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.Button')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds an in-memory specification object; changes no system state.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Text,

        [Parameter()]
        [string]$Name,

        [Parameter()]
        [switch]$IsDefault,

        [Parameter()]
        [switch]$IsCancel
    )

    if ([string]::IsNullOrWhiteSpace($Name))
    {
        $Name = $Text
    }

    return [pscustomobject]@{
        PSTypeName = 'Fluence.Button'
        Name       = $Name
        Text       = $Text
        IsDefault  = [bool]$IsDefault
        IsCancel   = [bool]$IsCancel
    }
}
