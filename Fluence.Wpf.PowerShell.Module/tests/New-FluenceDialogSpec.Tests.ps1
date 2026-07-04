BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'New-FluenceDialogSpec' {
    Context 'Composition' {
        It 'builds the PRD user-flow dialog' {
            $dialog = New-FluenceDialogSpec -Title 'Contoso IT' -Content @(
                New-FluenceSpec TextBlock -Text 'Before we upgrade, tell us where you sit.'
                New-FluenceSpec TextBox -Name Desk -PlaceholderText 'Desk number' -Rules (New-FluenceRule -NotEmpty)
                New-FluenceSpec ComboBox -Name Site -Items 'Sydney', 'Melbourne', 'Auckland'
                New-FluenceSpec CheckBox -Name Vpn -Content 'I use VPN daily'
            ) -Buttons (New-FluenceButton -Text 'Continue' -IsDefault), 'Defer'

            $dialog.GetType().FullName | Should -Be 'Fluence.Wpf.Specs.DialogSpec'
            $dialog.Title | Should -Be 'Contoso IT'
            $dialog.Content.Count | Should -Be 4
            $dialog.Buttons.Count | Should -Be 2
        }
        It 'converts Fluence.Button objects, preserving default and cancel roles' {
            $dialog = New-FluenceDialogSpec -Buttons (New-FluenceButton -Name Go -Text 'Go' -IsDefault), (New-FluenceButton -Text 'Stop' -IsCancel)
            $dialog.Buttons[0].Name | Should -Be 'Go'
            $dialog.Buttons[0].IsDefault | Should -BeTrue
            $dialog.Buttons[1].IsCancel | Should -BeTrue
        }
        It 'treats a bare Cancel string as the cancel affordance' {
            $dialog = New-FluenceDialogSpec -Buttons 'OK', 'Cancel'
            $dialog.Buttons[1].IsCancel | Should -BeTrue
            $dialog.Buttons[0].IsCancel | Should -BeFalse
        }
        It 'defaults to a single OK button' {
            (New-FluenceDialogSpec).Buttons[0].Text | Should -Be 'OK'
        }
    }
    Context 'Fail-fast validation' {
        It 'rejects non-spec content' {
            { New-FluenceDialogSpec -Content @('just a string') } | Should -Throw '*New-FluenceSpec*'
        }
        It 'rejects duplicate input names at build time' {
            {
                New-FluenceDialogSpec -Content @(
                    New-FluenceSpec TextBox -Name Desk
                    New-FluenceSpec TextBox -Name desk
                )
            } | Should -Throw '*Duplicate input name*'
        }
        It 'rejects rules on unnamed elements at build time' {
            {
                New-FluenceDialogSpec -Content @(
                    New-FluenceSpec TextBox -Rules (New-FluenceRule -NotEmpty)
                )
            } | Should -Throw '*no Name*'
        }
    }
}
