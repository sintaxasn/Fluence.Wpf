#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
BeforeAll { Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force }

Describe 'Close-FluenceUiRunspace' {
    It 'disposes and nulls an open STA runspace, idempotently' {
        InModuleScope Fluence.Wpf.PowerShell {
            $script:StaRunspace = [runspacefactory]::CreateRunspace()
            $script:StaRunspace.ApartmentState = 'STA'
            $script:StaRunspace.Open()
            Close-FluenceUiRunspace
            $script:StaRunspace | Should -BeNullOrEmpty
            { Close-FluenceUiRunspace } | Should -Not -Throw
        }
    }
    It 'is a no-op when no runspace was ever opened' {
        InModuleScope Fluence.Wpf.PowerShell {
            $script:StaRunspace = $null
            { Close-FluenceUiRunspace } | Should -Not -Throw
        }
    }
}
