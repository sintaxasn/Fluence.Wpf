function Resolve-FluenceClickedButton
{
    <#
    .SYNOPSIS
        Returns the name of the button that caused the dialog to close.
    .DESCRIPTION
        Given a dialog result hashtable and the button list, resolves which button was clicked.
        If a non-cancel button's flag is true in Result, returns that button's Name.
        If Result.Cancelled is true and a cancel button exists in the set, returns its Name.
        Otherwise returns $null (window closed without a recognized action).
    .PARAMETER Result
        The raw result hashtable (or Fluence.DialogResult) from Show-FluenceDialog.
    .PARAMETER Buttons
        The array of Fluence.Button objects that were passed to Show-FluenceDialog.
    .OUTPUTS
        System.String
    .NOTES
        Does not require a host application; pure logic helper for Show-FluenceMessage.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory = $true)]
        [object]$Result,

        [Parameter(Mandatory = $true)]
        [object[]]$Buttons
    )

    # Normalize Result to a plain hashtable so the same logic works for both
    # a raw [hashtable] (unit tests) and a [pscustomobject] / Fluence.DialogResult
    # (the runtime shape returned by ConvertTo-FluenceResult).
    $values = @{}
    if ($Result -is [System.Collections.IDictionary])
    {
        foreach ($key in $Result.Keys)
        {
            $values[$key] = $Result[$key]
        }
    }
    else
    {
        foreach ($property in $Result.PSObject.Properties)
        {
            $values[$property.Name] = $property.Value
        }
    }

    # Check non-cancel buttons first - if any flag is true, that button was clicked.
    foreach ($button in $Buttons)
    {
        if (-not $button.IsCancel)
        {
            $flag = $values[$button.Name]
            if ($flag -eq $true)
            {
                return $button.Name
            }
        }
    }

    # Check for cancel path.
    $isCancelled = $values['Cancelled']
    if ($isCancelled -eq $true)
    {
        $cancelButton = $Buttons | Where-Object { $_.IsCancel -eq $true } | Select-Object -First 1
        if ($null -ne $cancelButton)
        {
            return $cancelButton.Name
        }
    }

    return $null
}
