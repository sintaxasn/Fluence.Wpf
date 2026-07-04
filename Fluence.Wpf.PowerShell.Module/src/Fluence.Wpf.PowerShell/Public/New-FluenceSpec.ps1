function New-FluenceSpec
{
    <#
    .SYNOPSIS
        Builds one typed, serializable dialog-spec element mirroring a Fluence control.
    .DESCRIPTION
        Creates an instance of the generated Fluence.Wpf.Specs.<Type>Spec class. The element's
        curated members surface as dynamic parameters of this command (for example -Text and
        -PlaceholderText for TextBox, -Items for ComboBox, -Children for StackPanel), so specs are
        composed from typed objects with tab completion and never from XAML. Specs are plain data:
        they serialize through New-FluenceDialogSpec/Show-FluenceDialogSpec and later across
        process boundaries.
    .PARAMETER Type
        The spec control name: TextBlock, TextBox, PasswordBox, NumberBox, CheckBox, ToggleSwitch,
        ComboBox, RadioButton, DatePicker, TimePicker, HyperlinkButton, InfoBar, ProgressBar,
        ProgressRing, StackPanel, Border, or Image.
    .PARAMETER Name
        The result key for value-bearing elements; must be unique within the dialog.
    .PARAMETER Rules
        Declarative validation rules from New-FluenceRule, evaluated when the dialog commits.
    .EXAMPLE
        New-FluenceSpec TextBox -Name Desk -PlaceholderText 'Desk number' -Rules (New-FluenceRule -NotEmpty)
    .EXAMPLE
        New-FluenceSpec ComboBox -Name Site -Items 'Sydney','Melbourne','Auckland'
    .OUTPUTS
        Fluence.Wpf.Specs.SpecNode
    .NOTES
        Does not require a host application; this only builds a specification object.
    #>
    [CmdletBinding()]
    [OutputType([object])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds an in-memory specification object; changes no system state.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ArgumentCompleter({
            try
            {
                $assembly = [System.AppDomain]::CurrentDomain.GetAssemblies() |
                    Where-Object { $_.GetName().Name -eq 'Fluence.Wpf.Specs' } |
                    Select-Object -First 1
                if ($null -eq $assembly) { return @() }
                $nodeBase = $assembly.GetType('Fluence.Wpf.Specs.SpecNode', $false)
                $assembly.GetTypes() |
                    Where-Object { $_.IsPublic -and -not $_.IsAbstract -and $nodeBase.IsAssignableFrom($_) } |
                    ForEach-Object { $_.Name -replace 'Spec$', '' } |
                    Sort-Object
            }
            catch
            {
                @()
            }
        })]
        [string]$Type,

        [Parameter()]
        [string]$Name,

        [Parameter()]
        [object[]]$Rules
    )

    dynamicparam
    {
        $dictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()
        $resolved = $null
        if ($PSBoundParameters.ContainsKey('Type'))
        {
            try
            {
                $resolved = Get-FluenceSpecClrType -TypeName ([string]$PSBoundParameters['Type'])
            }
            catch
            {
                $resolved = $null
            }
        }
        if ($null -eq $resolved)
        {
            return $dictionary
        }
        foreach ($property in $resolved.GetProperties(([System.Reflection.BindingFlags]'Public,Instance')))
        {
            if ($property.Name -in @('Name', 'Rules'))
            {
                continue
            }
            $parameterType = $null
            if ($property.CanWrite -and $null -ne $property.SetMethod -and $property.SetMethod.IsPublic)
            {
                $parameterType = $property.PropertyType
                $isBase64Member = @($property.GetCustomAttributes($true) |
                        Where-Object { $_.GetType().Name -eq 'SpecBase64Attribute' }).Count -gt 0
                if ($isBase64Member)
                {
                    # Base64-carrying members also accept a raw byte array; the spec constructor
                    # auto-encodes it (SpecValueConverter.ToBase64Text), so bind loosely.
                    $parameterType = [object]
                }
            }
            elseif ($property.PropertyType.IsGenericType -and $property.PropertyType.GetGenericTypeDefinition() -eq [System.Collections.Generic.IList`1])
            {
                # Read-only IList members (Items, Children) accept one-or-more values at build time.
                $itemType = $property.PropertyType.GetGenericArguments()[0]
                $parameterType = if ($itemType -eq [string]) { [string[]] } else { [object[]] }
            }
            if ($null -eq $parameterType)
            {
                continue
            }
            $attributes = [System.Collections.ObjectModel.Collection[System.Attribute]]::new()
            $attributes.Add([System.Management.Automation.ParameterAttribute]::new())
            $dictionary.Add($property.Name, [System.Management.Automation.RuntimeDefinedParameter]::new($property.Name, $parameterType, $attributes))
        }
        return $dictionary
    }

    process
    {
        $clrType = Get-FluenceSpecClrType -TypeName $Type
        if ($null -eq $clrType)
        {
            $available = (Get-FluenceSpecClrType -ListAvailable) -join ', '
            throw "Unknown spec control '$Type'. Available controls: $available."
        }

        $properties = @{}
        if ($PSBoundParameters.ContainsKey('Name'))
        {
            $properties['Name'] = $Name
        }
        if ($PSBoundParameters.ContainsKey('Rules'))
        {
            $properties['Rules'] = $Rules
        }
        $static = @('Type', 'Name', 'Rules')
        foreach ($key in @($PSBoundParameters.Keys))
        {
            if ($key -in $static -or [System.Management.Automation.Cmdlet]::CommonParameters.Contains($key))
            {
                continue
            }
            $properties[$key] = $PSBoundParameters[$key]
        }

        return New-Object -TypeName $clrType.FullName -ArgumentList @(, $properties)
    }
}
