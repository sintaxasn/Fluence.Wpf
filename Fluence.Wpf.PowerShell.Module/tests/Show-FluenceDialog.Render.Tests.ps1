#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Opt-in render test for the Show-FluenceDialog UI path. Every It is tagged UI and skips unless
# FLUENCE_PS_UI=1, mirroring the library's opt-in Screenshots pattern. The dialog is driven and
# closed by a DispatcherTimer created ON the UI thread (inside the Invoke-OnFluenceUi scriptblock),
# which is the only pattern that works on every host shape: inline-STA (powershell, pwsh) and the
# module-owned STA runspace used by an MTA host (pwsh -mta).
#
# The harness scriptblock is fully self-contained: its only inputs are the spec hashtable and a
# bool, and it resolves the private renderer helpers through the module object (& $module { ... }),
# so it behaves identically whether Invoke-OnFluenceUi runs it inline or transports it by text to
# the STA runspace (where only public functions sit at top level).

BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force

    # Self-contained UI-thread harness. Ensures the Application + theme slots, builds the dialog
    # window via the private builder (through the module object), and attaches a DispatcherTimer
    # that optionally clicks the first button then closes. Returns the collected result hashtable.
    $script:RenderHarness = {
        param($spec, $clickFirstButton)

        if ($null -eq [System.Windows.Application]::Current)
        {
            $app = [System.Windows.Application]::new()
            $app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown
        }

        $theme = [Fluence.Wpf.ApplicationTheme]$spec.Theme
        $backdrop = [Fluence.Wpf.BackdropType]$spec.Backdrop
        [Fluence.Wpf.ApplicationThemeManager]::Apply($theme, $backdrop, $true)
        [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()

        $module = Get-Module Fluence.Wpf.PowerShell
        $state = @{ Result = @{}; Window = $null }
        $window = & $module {
            param($s, $st)
            New-FluenceDialogWindow -Spec $s -State $st
        } $spec $state
        $state.Window = $window

        $timer = [System.Windows.Threading.DispatcherTimer]::new()
        $timer.Interval = [timespan]::FromSeconds(2)
        $timer.add_Tick({
            $timer.Stop()
            try
            {
                if ($clickFirstButton)
                {
                    # Iterative depth-first walk of the visual tree (no nested function, which a
                    # deferred dispatcher callback cannot resolve) to find the first Fluence Button.
                    $button = $null
                    $stack = [System.Collections.Generic.Stack[System.Windows.DependencyObject]]::new()
                    $stack.Push($window)
                    while ($stack.Count -gt 0 -and $null -eq $button)
                    {
                        $node = $stack.Pop()
                        $childCount = [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($node)
                        for ($i = 0; $i -lt $childCount; $i++)
                        {
                            $child = [System.Windows.Media.VisualTreeHelper]::GetChild($node, $i)
                            if ($child -is [Fluence.Wpf.Controls.Button])
                            {
                                $button = $child
                                break
                            }
                            $stack.Push($child)
                        }
                    }
                    if ($null -ne $button)
                    {
                        $button.RaiseEvent([System.Windows.RoutedEventArgs]::new([System.Windows.Controls.Primitives.ButtonBase]::ClickEvent))
                    }
                }
            }
            finally
            {
                if ($window.IsVisible)
                {
                    $window.Close()
                }
            }
        }.GetNewClosure())
        $timer.Start()

        $null = $window.ShowDialog()
        return $state.Result
    }

    # Runs the harness on the UI thread through the private Invoke-OnFluenceUi router (via the module
    # object), so the test body does not need module-private functions in its own scope.
    function script:RunHarness
    {
        param([scriptblock]$Harness, [object[]]$Arguments)
        return (& (Get-Module Fluence.Wpf.PowerShell) {
                param($h, $a)
                Invoke-OnFluenceUi -Script $h -ArgumentList $a
            } $Harness $Arguments)
    }

    # Projects the raw result hashtable through the private converter (via the module object).
    function script:ConvertResult
    {
        param([hashtable]$Hash)
        return (& (Get-Module Fluence.Wpf.PowerShell) {
                param($h)
                ConvertTo-FluenceResult -Result $h
            } $Hash)
    }

    # Unwraps the Invoke-OnFluenceUi return (a collection from the STA runspace) to the hashtable.
    function script:Unwrap
    {
        param($Raw)
        if ($Raw -is [System.Collections.IList] -and $Raw.Count -ge 1)
        {
            return $Raw[$Raw.Count - 1]
        }
        return $Raw
    }
}

Describe 'Show-FluenceDialog render' -Tag UI {

    It 'returns the prompt value and OK flag on the success path' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $spec = @{
            Title        = 'Render Success'
            Message      = @('Enter your city.')
            Prompts      = @(New-FluencePrompt -Name City -Message 'City' -DefaultValue 'Leeds')
            Buttons      = @(New-FluenceButton -Text 'OK' -IsDefault)
            Theme        = 'Light'
            Backdrop     = 'None'
            AccentColor  = $null
            MinWidth     = 360
            Topmost      = $false
            ParentWindow = $null
        }

        $raw = script:RunHarness -Harness $script:RenderHarness -Arguments @($spec, $true)
        $result = script:ConvertResult -Hash (script:Unwrap -Raw $raw)

        $result.City | Should -Be 'Leeds'
        $result.OK | Should -BeTrue
        $result.Cancelled | Should -BeFalse
    }

    It 'constructs every input type and returns a key per prompt' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $prompts = @(
            New-FluencePrompt -Name P_Text -Message 'Text' -InputType Text -DefaultValue 'abc'
            New-FluencePrompt -Name P_Multiline -Message 'Multiline' -InputType Multiline -DefaultValue "a`nb"
            New-FluencePrompt -Name P_Password -Message 'Password' -InputType Password -DefaultValue 'secret'
            New-FluencePrompt -Name P_Number -Message 'Number' -InputType Number -DefaultValue 42
            New-FluencePrompt -Name P_Checkbox -Message 'Checkbox' -InputType Checkbox -DefaultValue $true
            New-FluencePrompt -Name P_Toggle -Message 'Toggle' -InputType Toggle -DefaultValue $true
            New-FluencePrompt -Name P_Combo -Message 'Combo' -InputType Choice -ValidateSet 'One', 'Two' -As Combo -DefaultValue 'One'
            New-FluencePrompt -Name P_Radio -Message 'Radio' -InputType Choice -ValidateSet 'Red', 'Green' -As Radio -DefaultValue 'Red'
            New-FluencePrompt -Name P_Date -Message 'Date' -InputType Date -DefaultValue ([datetime]'2026-01-01')
            New-FluencePrompt -Name P_Time -Message 'Time' -InputType Time -DefaultValue ([timespan]'09:30:00')
            New-FluencePrompt -Name P_FileOpen -Message 'FileOpen' -InputType FileOpen -DefaultValue 'C:\file.txt'
            New-FluencePrompt -Name P_FileSave -Message 'FileSave' -InputType FileSave -DefaultValue 'C:\out.txt'
            New-FluencePrompt -Name P_FolderOpen -Message 'FolderOpen' -InputType FolderOpen -DefaultValue 'C:\folder'
            New-FluencePrompt -Name P_Link -Message 'Link' -InputType Link -DefaultValue 'https://example.com'
        )

        $spec = @{
            Title        = 'Render All Types'
            Message      = @('Every input type.')
            Prompts      = $prompts
            Buttons      = @(New-FluenceButton -Text 'OK' -IsDefault)
            Theme        = 'Light'
            Backdrop     = 'None'
            AccentColor  = $null
            MinWidth     = 420
            Topmost      = $false
            ParentWindow = $null
        }

        # Timer just closes the window (no button click) so no value capture is exercised.
        $raw = script:RunHarness -Harness $script:RenderHarness -Arguments @($spec, $false)
        $result = script:ConvertResult -Hash (script:Unwrap -Raw $raw)

        foreach ($prompt in $prompts)
        {
            $result.PSObject.Properties.Name | Should -Contain $prompt.Name
        }
        $result.Cancelled | Should -BeTrue
    }
}
