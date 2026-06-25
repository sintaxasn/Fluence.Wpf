function New-FluenceDialogWindow
{
    <#
    .SYNOPSIS
        Builds the FluenceWindow for a dialog specification and wires its buttons and validation.
    .DESCRIPTION
        Creates a FluenceWindow sized to its content, composes the optional message lines, each
        prompt's label and input control (via New-FluenceInputControl), an inline validation InfoBar,
        and a row of buttons. A non-cancel button validates the captured values with Test-FluenceInput
        and either opens the InfoBar (keeping the window open) or records its result and closes. A
        cancel button closes without validating. The Closed handler marks the result Cancelled when no
        button result is true. Returns the window; the caller assigns $State.Window before ShowDialog.
    .PARAMETER Spec
        The normalized dialog specification hashtable from Show-FluenceDialog.
    .PARAMETER State
        The shared @{ Result = @{}; Window = $null } hashtable that accumulates captured values.
    .OUTPUTS
        Fluence.Wpf.Controls.FluenceWindow
    .NOTES
        Must run on a UI (STA) thread; call it through Invoke-FluenceWindow. No hard-coded colors:
        every control resolves its own themed brushes from the seeded slots.
    #>
    [CmdletBinding()]
    [OutputType([Fluence.Wpf.Controls.FluenceWindow])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds a WPF window object in memory; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Spec,

        [Parameter(Mandatory = $true)]
        [hashtable]$State
    )

    $window = [Fluence.Wpf.Controls.FluenceWindow]::new()
    $window.Title = $Spec.Title
    $window.SystemBackdropType = [Fluence.Wpf.BackdropType]$Spec.Backdrop
    $window.SizeToContent = [System.Windows.SizeToContent]::WidthAndHeight
    $window.MinWidth = $Spec.MinWidth
    $window.MinHeight = 120
    $window.WindowStartupLocation = [System.Windows.WindowStartupLocation]::CenterScreen
    $window.Topmost = [bool]$Spec.Topmost
    if ($null -ne $Spec.ParentWindow)
    {
        $window.Owner = $Spec.ParentWindow
        $window.WindowStartupLocation = [System.Windows.WindowStartupLocation]::CenterOwner
    }

    # Root stack inside a padded border. Controls resolve their own themed brushes.
    $border = [System.Windows.Controls.Border]::new()
    $border.Padding = [System.Windows.Thickness]::new(24)

    $root = [System.Windows.Controls.StackPanel]::new()
    $root.Orientation = [System.Windows.Controls.Orientation]::Vertical
    $border.Child = $root

    # Message lines. When an Icon is specified (not None), render as a leading InfoBar so the
    # severity glyph and color appear. When Icon is None (or absent), render plain TextBlocks.
    $iconValue = $null
    if ($Spec.ContainsKey('Icon'))
    {
        $iconValue = $Spec.Icon
    }
    $useIconBar = (-not [string]::IsNullOrWhiteSpace($iconValue)) -and ($iconValue -ne 'None')

    if ($useIconBar -and $null -ne $Spec.Message)
    {
        $joinedMessage = [string]::Join(' ', $Spec.Message)
        $leadingBar = [Fluence.Wpf.Controls.InfoBar]::new()
        $leadingBar.IsOpen = $true
        $leadingBar.Message = $joinedMessage
        $leadingBar.Margin = [System.Windows.Thickness]::new(0, 0, 0, 8)

        $severity = [Fluence.Wpf.InfoBarSeverity]::Informational
        switch ($iconValue)
        {
            'Success'  { $severity = [Fluence.Wpf.InfoBarSeverity]::Success }
            'Warning'  { $severity = [Fluence.Wpf.InfoBarSeverity]::Warning }
            'Error'    { $severity = [Fluence.Wpf.InfoBarSeverity]::Error }
        }
        $leadingBar.Severity = $severity

        $null = $root.Children.Add($leadingBar)
    }
    elseif ($null -ne $Spec.Message)
    {
        foreach ($line in $Spec.Message)
        {
            $text = [System.Windows.Controls.TextBlock]::new()
            $text.Text = $line
            $text.TextWrapping = [System.Windows.TextWrapping]::Wrap
            $text.Margin = [System.Windows.Thickness]::new(0, 0, 0, 8)
            $null = $root.Children.Add($text)
        }
    }

    # Prompts: optional label + control.
    foreach ($prompt in $Spec.Prompts)
    {
        if (-not [string]::IsNullOrWhiteSpace($prompt.Message) -and $prompt.InputType -ne 'Checkbox' -and $prompt.InputType -ne 'Link')
        {
            $label = [System.Windows.Controls.TextBlock]::new()
            $label.Text = $prompt.Message
            $label.TextWrapping = [System.Windows.TextWrapping]::Wrap
            $label.Margin = [System.Windows.Thickness]::new(0, 8, 0, 4)
            $null = $root.Children.Add($label)
        }

        $control = New-FluenceInputControl -Prompt $prompt -State $State
        $control.HorizontalAlignment = [System.Windows.HorizontalAlignment]::Stretch
        $control.Margin = [System.Windows.Thickness]::new(0, 0, 0, 4)
        $null = $root.Children.Add($control)
    }

    # Inline validation InfoBar (hidden until a validation failure). Stashed on $State so the
    # button closures can open it.
    $infoBar = [Fluence.Wpf.Controls.InfoBar]::new()
    $infoBar.Severity = [Fluence.Wpf.InfoBarSeverity]::Error
    $infoBar.IsOpen = $false
    $infoBar.Margin = [System.Windows.Thickness]::new(0, 8, 0, 0)
    $State.InfoBar = $infoBar
    $null = $root.Children.Add($infoBar)

    # Button row.
    $buttonPanel = [System.Windows.Controls.StackPanel]::new()
    $buttonPanel.Orientation = [System.Windows.Controls.Orientation]::Horizontal
    $buttonPanel.HorizontalAlignment = [System.Windows.HorizontalAlignment]::Right
    $buttonPanel.Margin = [System.Windows.Thickness]::new(0, 16, 0, 0)

    # Initialize the result: Cancelled plus a false entry per button, before wiring.
    $State.Result['Cancelled'] = $false
    foreach ($button in $Spec.Buttons)
    {
        $State.Result[$button.Name] = $false
    }

    # Capture the validator as a variable so the button closures invoke it through a closed-over
    # reference. A GetNewClosure closure runs in a fresh anonymous scope that cannot resolve a
    # module-private function by name when the deferred WPF Click event fires, but it can call a
    # captured scriptblock.
    $validateInput = ${function:Test-FluenceInput}

    foreach ($button in $Spec.Buttons)
    {
        $wpfButton = [Fluence.Wpf.Controls.Button]::new()
        $wpfButton.Content = $button.Text
        $wpfButton.MinWidth = 88
        $wpfButton.Margin = [System.Windows.Thickness]::new(8, 0, 0, 0)
        $wpfButton.IsDefault = [bool]$button.IsDefault
        $wpfButton.IsCancel = [bool]$button.IsCancel

        if ($button.IsCancel)
        {
            $wpfButton.add_Click({
                # Cancel closes without validating; the Closed handler marks Cancelled.
                $State.Window.Close()
            }.GetNewClosure())
        }
        else
        {
            $wpfButton.add_Click({
                $validation = @{ IsValid = $true; Message = '' }
                if ($Spec.Prompts.Count -gt 0)
                {
                    $validation = & $validateInput -Prompts $Spec.Prompts -Values $State.Result
                }
                if (-not $validation.IsValid)
                {
                    $State.InfoBar.Message = $validation.Message
                    $State.InfoBar.Severity = [Fluence.Wpf.InfoBarSeverity]::Error
                    $State.InfoBar.IsOpen = $true
                    return
                }
                $State.Result[$button.Name] = $true
                $State.Window.Close()
            }.GetNewClosure())
        }

        $null = $buttonPanel.Children.Add($wpfButton)
    }

    $null = $root.Children.Add($buttonPanel)
    $window.Content = $border

    # When the window closes by any path, mark Cancelled unless a button reported success.
    $window.add_Closed({
        $anySuccess = $false
        foreach ($button in $Spec.Buttons)
        {
            if ($State.Result[$button.Name] -eq $true)
            {
                $anySuccess = $true
            }
        }
        if (-not $anySuccess)
        {
            $State.Result['Cancelled'] = $true
        }
    }.GetNewClosure())

    return $window
}
