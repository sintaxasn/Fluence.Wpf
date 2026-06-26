#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Opt-in UI tests for the Task 1 theming helpers. Every It is tagged UI and skips unless
# FLUENCE_PS_UI=1, mirroring the library's opt-in Screenshots pattern and the render-test gate.
# These exercise the process-wide theme statics through the public helpers, so they touch
# AppDomain-wide state and must run on a host that owns (or can establish) a WPF Application.

BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force
}

Describe 'Fluence theming helpers' -Tag UI {

    It 'Set-FluenceTheme changes CurrentTheme and preserves an omitted backdrop' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        Set-FluenceTheme -Theme Dark -Backdrop None

        $afterDark = Get-FluenceTheme
        $afterDark.CurrentTheme | Should -Be ([Fluence.Wpf.ApplicationTheme]::Dark)
        $afterDark.CurrentBackdrop | Should -Be ([Fluence.Wpf.BackdropType]::None)

        Set-FluenceTheme -Theme Light

        $afterLight = Get-FluenceTheme
        $afterLight.CurrentTheme | Should -Be ([Fluence.Wpf.ApplicationTheme]::Light)
        $afterLight.CurrentBackdrop | Should -Be ([Fluence.Wpf.BackdropType]::None)
    }

    It 'Set-FluenceAccent round-trips a custom color and -System does not throw' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $red = [System.Windows.Media.Color]::FromRgb(255, 0, 0)
        Set-FluenceAccent -Color $red

        $accent = [Fluence.Wpf.ApplicationAccentColorManager]::SystemAccentColor
        $accent.R | Should -Be 255
        $accent.G | Should -Be 0
        $accent.B | Should -Be 0

        { Set-FluenceAccent -System } | Should -Not -Throw
    }
}
