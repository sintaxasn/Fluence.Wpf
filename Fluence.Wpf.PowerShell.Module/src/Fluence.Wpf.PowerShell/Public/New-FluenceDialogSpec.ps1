function New-FluenceDialogSpec
{
    <#
    .SYNOPSIS
        Builds a complete, serializable dialog specification from typed spec elements and buttons.
    .DESCRIPTION
        Composes a Fluence.Wpf.Specs.DialogSpec: a title, a vertical flow of elements from
        New-FluenceSpec, and a button row. Buttons accept New-FluenceButton objects, bare strings
        (a bare 'Cancel' acts as the cancel button), or Fluence.Wpf.Specs.ButtonSpec instances.
        The spec is validated fail-fast (at least one button, unique input names, strict tree
        shape) and is ready for Show-FluenceDialogSpec or serialization.
    .PARAMETER Title
        The dialog window title.
    .PARAMETER Content
        One or more spec elements from New-FluenceSpec, rendered top to bottom.
    .PARAMETER Buttons
        One or more buttons: New-FluenceButton objects, ButtonSpec instances, or bare strings.
        Defaults to a single OK button.
    .EXAMPLE
        $dialog = New-FluenceDialogSpec -Title 'Contoso IT' -Content @(
            New-FluenceSpec TextBox -Name Desk -PlaceholderText 'Desk number' -Rules (New-FluenceRule -NotEmpty)
        ) -Buttons (New-FluenceButton -Text 'Continue' -IsDefault), 'Defer'
    .OUTPUTS
        Fluence.Wpf.Specs.DialogSpec
    .NOTES
        Does not require a host application; this only builds a specification object.
    #>
    [CmdletBinding()]
    [OutputType([object])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds an in-memory specification object; changes no system state.')]
    param
    (
        [Parameter()]
        [string]$Title = 'Fluence',

        [Parameter()]
        [object[]]$Content,

        [Parameter()]
        [object[]]$Buttons = @('OK')
    )

    $dialog = [Fluence.Wpf.Specs.DialogSpec]::new()
    $dialog.Title = $Title

    foreach ($node in @($Content))
    {
        if ($null -eq $node)
        {
            continue
        }
        if ($node -isnot [Fluence.Wpf.Specs.SpecNode])
        {
            throw "Content items must be spec elements from New-FluenceSpec; got '$($node.GetType().FullName)'."
        }
        $dialog.Content.Add($node)
    }

    foreach ($button in @($Buttons))
    {
        if ($null -eq $button)
        {
            continue
        }
        if ($button -is [Fluence.Wpf.Specs.ButtonSpec])
        {
            $dialog.Buttons.Add($button)
            continue
        }
        if ($button -is [string])
        {
            # Mirror Show-FluenceDialog: a bare 'Cancel' string is the Esc/cancel affordance.
            $spec = [Fluence.Wpf.Specs.ButtonSpec]::new()
            $spec.Name = $button
            $spec.Text = $button
            $spec.IsCancel = $button -eq 'Cancel'
            $dialog.Buttons.Add($spec)
            continue
        }
        if ($null -ne $button.PSObject.Properties['Text'])
        {
            # A Fluence.Button object from New-FluenceButton.
            $spec = [Fluence.Wpf.Specs.ButtonSpec]::new()
            $spec.Name = [string]$button.Name
            $spec.Text = [string]$button.Text
            $spec.IsDefault = [bool]$button.IsDefault
            $spec.IsCancel = [bool]$button.IsCancel
            $dialog.Buttons.Add($spec)
            continue
        }
        throw "Buttons must be strings, New-FluenceButton objects, or ButtonSpec instances; got '$($button.GetType().FullName)'."
    }

    # Fail fast at build time with the validator's message (button presence, unique input names,
    # strict tree shape); Show-FluenceDialogSpec and serialization revalidate.
    [Fluence.Wpf.Specs.SpecTreeValidator]::Validate($dialog)

    return $dialog
}
