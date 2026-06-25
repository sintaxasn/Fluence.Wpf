function Test-FluenceInput
{
    <#
    .SYNOPSIS
        Validates captured dialog values against their prompt rules.
    .OUTPUTS
        A hashtable with IsValid (bool) and Message (string).
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [object[]]$Prompts,

        [Parameter(Mandatory = $true)]
        [hashtable]$Values
    )

    foreach ($p in $Prompts)
    {
        $value = $Values[$p.Name]
        $asText = [string]$value

        if ($p.ValidateNotEmpty -and [string]::IsNullOrWhiteSpace($asText))
        {
            return @{ IsValid = $false; Message = "'$($p.Name)' is required." }
        }

        if (-not [string]::IsNullOrWhiteSpace($p.ValidatePattern) -and -not [string]::IsNullOrWhiteSpace($asText))
        {
            if ($asText -notmatch $p.ValidatePattern)
            {
                return @{ IsValid = $false; Message = "'$($p.Name)' does not match the required format." }
            }
        }

        if ($null -ne $p.ValidateScript)
        {
            $ok = $false
            try
            {
                $ok = [bool](& $p.ValidateScript $value)
            }
            catch
            {
                $ok = $false
            }
            if (-not $ok)
            {
                return @{ IsValid = $false; Message = "'$($p.Name)' failed validation." }
            }
        }
    }

    return @{ IsValid = $true; Message = '' }
}
