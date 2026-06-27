#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/Resolve-FluenceClickedButton.ps1"
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/ConvertTo-FluenceResult.ps1"
}

Describe 'Resolve-FluenceClickedButton' {

    It 'returns the name of the clicked non-cancel button when its flag is true' {
        $buttons = @(New-FluenceButton 'OK' -IsDefault)
        $result = @{ OK = $true; Cancelled = $false }
        Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'OK'
    }

    It 'returns the cancel button name when result is Cancelled and a cancel button exists' {
        $buttons = @(
            New-FluenceButton 'OK' -IsDefault
            New-FluenceButton 'Cancel' -IsCancel
        )
        $result = @{ OK = $false; Cancel = $false; Cancelled = $true }
        Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'Cancel'
    }

    It 'returns the name of the clicked button even when it is not the first button' {
        $buttons = @(
            New-FluenceButton 'Yes' -IsDefault
            New-FluenceButton 'No'
        )
        $result = @{ Yes = $false; No = $true; Cancelled = $false }
        Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'No'
    }

    It 'maps a dismissal to the last (safe) button when no cancel button exists' {
        $buttons = @(
            New-FluenceButton 'Yes' -IsDefault
            New-FluenceButton 'No'
        )
        $result = @{ Yes = $false; No = $false; Cancelled = $true }
        Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'No'
    }

    It 'maps a dismissal to OK for a single-button OK preset' {
        $buttons = @(New-FluenceButton 'OK' -IsDefault)
        $result = @{ OK = $false; Cancelled = $true }
        Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'OK'
    }

    # --- Runtime shape: [pscustomobject] / Fluence.DialogResult via ConvertTo-FluenceResult ---

    It 'returns the clicked button name when Result is a pscustomobject with the flag true' {
        $buttons = @(New-FluenceButton 'OK' -IsDefault)
        $result = ConvertTo-FluenceResult -Result @{ OK = $true; Cancelled = $false }
        Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'OK'
    }

    It 'returns the cancel button name when pscustomobject result has Cancelled true' {
        $buttons = @(
            New-FluenceButton 'Yes' -IsDefault
            New-FluenceButton 'Cancel' -IsCancel
        )
        $result = ConvertTo-FluenceResult -Result @{ Yes = $false; No = $false; Cancelled = $true }
        Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'Cancel'
    }

    It 'maps a dismissal to the last (safe) button for a pscustomobject result with no cancel button' {
        $buttons = @(
            New-FluenceButton 'Yes' -IsDefault
            New-FluenceButton 'No'
        )
        $result = ConvertTo-FluenceResult -Result @{ Yes = $false; No = $false; Cancelled = $true }
        Resolve-FluenceClickedButton -Result $result -Buttons $buttons | Should -Be 'No'
    }
}
