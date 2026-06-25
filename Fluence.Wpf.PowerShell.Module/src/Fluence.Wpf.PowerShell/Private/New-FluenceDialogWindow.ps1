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

    # Initialize the result: Cancelled and TimedOut flags plus a false entry per button, before
    # wiring. TimedOut is carried for forward compatibility with the design's result contract; the
    # -Timeout/-Countdown features that would set it are deferred to a later phase.
    $State.Result['Cancelled'] = $false
    $State.Result['TimedOut'] = $false
    foreach ($button in $Spec.Buttons)
    {
        $State.Result[$button.Name] = $false
    }

    # Capture the validator as a variable so the button closures invoke it through a closed-over
    # reference. A GetNewClosure closure runs in a fresh anonymous scope that cannot resolve a
    # module-private function by name when the deferred WPF Click event fires, but it can call a
    # captured scriptblock.
    $validateInput = ${function:Test-FluenceInput}

    # Pull-at-commit refresh for controls with no usable change event (PasswordBox). Reading the
    # live value here at each click avoids a persistent DependencyPropertyDescriptor subscription
    # (which would leak the control + $State for the life of the reused STA Application).
    $refreshPullControls = {
        if ($null -ne $State.PullControls)
        {
            foreach ($pull in $State.PullControls)
            {
                $State.Result[$pull.Name] = $pull.Control.Password
            }
        }
    }

    # Button row: equal-fill Grid mirroring ContentDialog CommandSpace.
    # Layout order: IsDefault left, neither-default-nor-cancel in the middle (preserving $Spec.Buttons
    # order among them), IsCancel right. $Spec.Buttons itself is not reordered; the Closed handler and
    # Result init above continue to iterate it. The ordered list is layout-only.
    $orderedButtons = @($Spec.Buttons | Where-Object { $_.IsDefault }) `
        + @($Spec.Buttons | Where-Object { -not $_.IsDefault -and -not $_.IsCancel }) `
        + @($Spec.Buttons | Where-Object { $_.IsCancel -and -not $_.IsDefault })

    $grid = [System.Windows.Controls.Grid]::new()
    $grid.HorizontalAlignment = [System.Windows.HorizontalAlignment]::Stretch
    $grid.Margin = [System.Windows.Thickness]::new(0, 16, 0, 0)

    $n = $orderedButtons.Count

    # A single button is sized as if there were two (half width) and pinned to the right column,
    # leaving the left column empty. For 2+ buttons, one equal Star column per button. The single
    # button therefore needs an extra empty leading column.
    $columnCount = if ($n -eq 1) { 2 } else { $n }
    $starWidth = [System.Windows.GridLength]::new(1, [System.Windows.GridUnitType]::Star)
    for ($c = 0; $c -lt $columnCount; $c++)
    {
        $col = [System.Windows.Controls.ColumnDefinition]::new()
        $col.Width = $starWidth
        $null = $grid.ColumnDefinitions.Add($col)
    }

    for ($i = 0; $i -lt $n; $i++)
    {
        $button = $orderedButtons[$i]
        $wpfButton = [Fluence.Wpf.Controls.Button]::new()
        $wpfButton.Content = $button.Text
        $wpfButton.HorizontalAlignment = [System.Windows.HorizontalAlignment]::Stretch
        $wpfButton.MinWidth = 0

        # 4px half-margins produce 8px gaps between adjacent buttons, 0 at the outer edges,
        # matching ContentDialog CommandSpace: left (0,0,4,0), middle (4,0,4,0), right (4,0,0,0).
        # A lone button sits in the right column, so it takes the right-most margin (4,0,0,0).
        if ($n -eq 1)
        {
            $wpfButton.Margin = [System.Windows.Thickness]::new(4, 0, 0, 0)
        }
        elseif ($i -eq 0)
        {
            $wpfButton.Margin = [System.Windows.Thickness]::new(0, 0, 4, 0)
        }
        elseif ($i -eq ($n - 1))
        {
            $wpfButton.Margin = [System.Windows.Thickness]::new(4, 0, 0, 0)
        }
        else
        {
            $wpfButton.Margin = [System.Windows.Thickness]::new(4, 0, 4, 0)
        }

        $wpfButton.IsDefault = [bool]$button.IsDefault
        $wpfButton.IsCancel = [bool]$button.IsCancel

        if ($button.IsDefault)
        {
            $wpfButton.Appearance = [Fluence.Wpf.ControlAppearance]::Accent
        }

        # Lone button -> right column (index 1 of 2); otherwise its position in the ordered list.
        $targetColumn = if ($n -eq 1) { 1 } else { $i }
        [System.Windows.Controls.Grid]::SetColumn($wpfButton, $targetColumn)

        if ($button.IsCancel)
        {
            $wpfButton.add_Click({
                # Cancel closes without validating; the Closed handler marks Cancelled. Pull anyway
                # so the captured result reflects the latest values regardless of close path.
                & $refreshPullControls
                $State.Window.Close()
            }.GetNewClosure())
        }
        else
        {
            $wpfButton.add_Click({
                # Capture the latest pull-only values (PasswordBox) before validating and closing.
                & $refreshPullControls
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

        $null = $grid.Children.Add($wpfButton)
    }

    $null = $root.Children.Add($grid)
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
