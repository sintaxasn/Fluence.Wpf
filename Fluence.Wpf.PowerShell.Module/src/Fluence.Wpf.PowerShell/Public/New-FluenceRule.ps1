function New-FluenceRule
{
    <#
    .SYNOPSIS
        Builds declarative validation rules for spec input elements.
    .DESCRIPTION
        Creates serializable Fluence.Wpf.Specs rule objects evaluated by the dialog host when a
        non-cancel button commits. Declarative rules replace live ValidateScript scriptblocks,
        which cannot cross a process boundary. Combining switches emits one rule per requested
        check, in NotEmpty, Pattern, Length, Range order.
    .PARAMETER NotEmpty
        Require a non-whitespace value.
    .PARAMETER Pattern
        A .NET regular expression the value's string form must match.
    .PARAMETER MinLength
        The inclusive minimum string length.
    .PARAMETER MaxLength
        The inclusive maximum string length.
    .PARAMETER Minimum
        The inclusive numeric minimum.
    .PARAMETER Maximum
        The inclusive numeric maximum.
    .PARAMETER ErrorMessage
        A custom failure message applied to every rule this call creates; defaults to each rule's
        built-in message.
    .EXAMPLE
        New-FluenceSpec TextBox -Name Desk -Rules (New-FluenceRule -NotEmpty -MaxLength 12)
    .OUTPUTS
        Fluence.Wpf.Specs.SpecRule
    .NOTES
        Does not require a host application; this only builds specification objects.
    #>
    [CmdletBinding()]
    [OutputType([object])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds in-memory specification objects; changes no system state.')]
    param
    (
        [Parameter()]
        [switch]$NotEmpty,

        [Parameter()]
        [string]$Pattern,

        [Parameter()]
        [int]$MinLength,

        [Parameter()]
        [int]$MaxLength,

        [Parameter()]
        [double]$Minimum,

        [Parameter()]
        [double]$Maximum,

        [Parameter()]
        [string]$ErrorMessage
    )

    $rules = @()

    if ($NotEmpty)
    {
        $rules += [Fluence.Wpf.Specs.NotEmptyRule]::new()
    }
    if (-not [string]::IsNullOrWhiteSpace($Pattern))
    {
        $rule = [Fluence.Wpf.Specs.PatternRule]::new()
        $rule.Pattern = $Pattern
        $rules += $rule
    }
    if ($PSBoundParameters.ContainsKey('MinLength') -or $PSBoundParameters.ContainsKey('MaxLength'))
    {
        $rule = [Fluence.Wpf.Specs.LengthRule]::new()
        if ($PSBoundParameters.ContainsKey('MinLength'))
        {
            $rule.MinLength = $MinLength
        }
        if ($PSBoundParameters.ContainsKey('MaxLength'))
        {
            $rule.MaxLength = $MaxLength
        }
        $rules += $rule
    }
    if ($PSBoundParameters.ContainsKey('Minimum') -or $PSBoundParameters.ContainsKey('Maximum'))
    {
        $rule = [Fluence.Wpf.Specs.RangeRule]::new()
        if ($PSBoundParameters.ContainsKey('Minimum'))
        {
            $rule.Minimum = $Minimum
        }
        if ($PSBoundParameters.ContainsKey('Maximum'))
        {
            $rule.Maximum = $Maximum
        }
        $rules += $rule
    }

    if ($rules.Count -eq 0)
    {
        throw 'Specify at least one rule: -NotEmpty, -Pattern, -MinLength/-MaxLength, or -Minimum/-Maximum.'
    }

    if (-not [string]::IsNullOrWhiteSpace($ErrorMessage))
    {
        foreach ($rule in $rules)
        {
            $rule.ErrorMessage = $ErrorMessage
        }
    }

    return $rules
}
