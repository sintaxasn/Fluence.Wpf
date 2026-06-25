function ConvertTo-FluenceResult
{
    <#
    .SYNOPSIS
        Converts a raw result hashtable into a typed Fluence.DialogResult PSCustomObject.
    .DESCRIPTION
        Seeds an ordered hashtable with PSTypeName = 'Fluence.DialogResult', copies every
        key from the input hashtable, then casts to [pscustomobject] and returns it.
    .PARAMETER Result
        The hashtable of collected values from the dialog renderer.
    .OUTPUTS
        Fluence.DialogResult
    .NOTES
        Private helper. Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.DialogResult')]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Result
    )

    $ordered = [ordered]@{ PSTypeName = 'Fluence.DialogResult' }
    foreach ($key in $Result.Keys)
    {
        $ordered[$key] = $Result[$key]
    }
    return [pscustomobject]$ordered
}
