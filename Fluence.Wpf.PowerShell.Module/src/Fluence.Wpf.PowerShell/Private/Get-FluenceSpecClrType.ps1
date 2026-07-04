function Get-FluenceSpecClrType
{
    <#
    .SYNOPSIS
        Resolves a spec control name (for example 'TextBox') to its Fluence.Wpf.Specs CLR type.
    .DESCRIPTION
        Maps a friendly control name onto the generated Fluence.Wpf.Specs.<Name>Spec class,
        case-insensitively. With -ListAvailable, returns the friendly names of every available
        spec control instead.
    .PARAMETER TypeName
        The friendly control name (the spec class name without the 'Spec' suffix).
    .PARAMETER ListAvailable
        Return the sorted friendly names of all available spec controls.
    .OUTPUTS
        System.Type or System.String[]
    .NOTES
        Requires Import-FluenceLibrary to have loaded Fluence.Wpf.Specs.dll. Does not require a
        host application.
    #>
    [CmdletBinding()]
    [OutputType([System.Type], [string[]])]
    param
    (
        [Parameter()]
        [string]$TypeName,

        [Parameter()]
        [switch]$ListAvailable
    )

    $assembly = [System.AppDomain]::CurrentDomain.GetAssemblies() |
        Where-Object { $_.GetName().Name -eq 'Fluence.Wpf.Specs' } |
        Select-Object -First 1
    if ($null -eq $assembly)
    {
        throw 'Fluence.Wpf.Specs.dll is not loaded; import the Fluence.Wpf.PowerShell module first.'
    }

    $nodeBase = $assembly.GetType('Fluence.Wpf.Specs.SpecNode', $true)

    if ($ListAvailable)
    {
        return @($assembly.GetTypes() |
                Where-Object { $_.IsPublic -and -not $_.IsAbstract -and $nodeBase.IsAssignableFrom($_) } |
                ForEach-Object { $_.Name -replace 'Spec$', '' } |
                Sort-Object)
    }

    if ([string]::IsNullOrWhiteSpace($TypeName))
    {
        return $null
    }

    $resolved = $assembly.GetType("Fluence.Wpf.Specs.${TypeName}Spec", $false, $true)
    if ($null -ne $resolved -and $resolved.IsPublic -and -not $resolved.IsAbstract -and $nodeBase.IsAssignableFrom($resolved))
    {
        return $resolved
    }
    return $null
}
