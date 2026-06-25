#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/Get-FluenceButtonPreset.ps1"
}
Describe 'Get-FluenceButtonPreset' {
    It 'maps OKCancel to two buttons with OK default and Cancel cancel' {
        $b = Get-FluenceButtonPreset -Preset OKCancel
        $b.Count | Should -Be 2
        ($b | Where-Object Text -eq 'OK').IsDefault | Should -BeTrue
        ($b | Where-Object Text -eq 'Cancel').IsCancel | Should -BeTrue
    }
    It 'maps YesNo to Yes/No' {
        (Get-FluenceButtonPreset -Preset YesNo | ForEach-Object Text) | Should -Be @('Yes', 'No')
    }
}
