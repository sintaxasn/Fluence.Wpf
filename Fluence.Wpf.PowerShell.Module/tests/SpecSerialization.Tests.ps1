BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

    $script:BuildUserFlowDialog = {
        New-FluenceDialogSpec -Title 'Contoso IT' -Content @(
            New-FluenceSpec TextBlock -Text 'Before we upgrade, tell us where you sit.'
            New-FluenceSpec TextBox -Name Desk -PlaceholderText 'Desk number' -Rules (New-FluenceRule -NotEmpty)
            New-FluenceSpec ComboBox -Name Site -Items 'Sydney', 'Melbourne', 'Auckland'
            New-FluenceSpec CheckBox -Name Vpn -Content 'I use VPN daily'
        ) -Buttons (New-FluenceButton -Text 'Continue' -IsDefault), 'Defer'
    }
}

Describe 'Spec serialization from PowerShell' {
    It 'round-trips the user-flow dialog byte-stably through the versioned envelope' {
        $dialog = & $script:BuildUserFlowDialog
        $first = [Fluence.Wpf.Specs.SpecSerialization]::Serialize($dialog)
        $back = [Fluence.Wpf.Specs.SpecSerialization]::Deserialize($first)
        $second = [Fluence.Wpf.Specs.SpecSerialization]::Serialize($back)

        [System.Convert]::ToBase64String($first) | Should -BeExactly ([System.Convert]::ToBase64String($second))
        $back.Title | Should -Be 'Contoso IT'
        $back.Content.Count | Should -Be 4
        $back.Content[1].Rules.Count | Should -Be 1
        ($back.Content[2].Items -join ',') | Should -Be 'Sydney,Melbourne,Auckland'
    }
    It 'round-trips through the Base64 string form' {
        $dialog = & $script:BuildUserFlowDialog
        $envelope = [Fluence.Wpf.Specs.SpecSerialization]::SerializeToBase64($dialog)
        ([Fluence.Wpf.Specs.SpecSerialization]::DeserializeFromBase64($envelope)).Buttons.Count | Should -Be 2
    }
}

Describe 'Spec input-type parity (PRD success metric)' {
    # Every non-file New-FluencePrompt InputType must be composable via the spec API:
    # Text, Multiline, Password, Number, Checkbox, Toggle, Choice, Date, Time, Link.
    It 'composes Text as a TextBox spec' {
        (New-FluenceSpec TextBox -Name X).GetType().Name | Should -Be 'TextBoxSpec'
    }
    It 'composes Multiline as a TextBox spec with AcceptsReturn and MinLines' {
        $spec = New-FluenceSpec TextBox -Name X -AcceptsReturn $true -MinLines 3 -TextWrapping Wrap
        $spec.AcceptsReturn | Should -BeTrue
        $spec.MinLines | Should -Be 3
    }
    It 'composes Password as a PasswordBox spec' {
        (New-FluenceSpec PasswordBox -Name X).GetType().Name | Should -Be 'PasswordBoxSpec'
    }
    It 'composes Number as a NumberBox spec with range members' {
        $spec = New-FluenceSpec NumberBox -Name X -Minimum 1 -Maximum 9 -Value 5
        $spec.Minimum | Should -Be 1
        $spec.Maximum | Should -Be 9
    }
    It 'composes Checkbox as a CheckBox spec' {
        (New-FluenceSpec CheckBox -Name X -Content 'Tick me').Content | Should -Be 'Tick me'
    }
    It 'composes Toggle as a ToggleSwitch spec' {
        $spec = New-FluenceSpec ToggleSwitch -Name X -OnContent 'On' -OffContent 'Off'
        $spec.OnContent | Should -Be 'On'
    }
    It 'composes Choice as a ComboBox spec or a RadioButton group' {
        (New-FluenceSpec ComboBox -Name X -Items 'A', 'B').Items.Count | Should -Be 2
        $radios = New-FluenceSpec StackPanel -Children @(
            New-FluenceSpec RadioButton -GroupName X -Content 'A'
            New-FluenceSpec RadioButton -GroupName X -Content 'B'
        )
        $radios.Children.Count | Should -Be 2
    }
    It 'composes Date as a DatePicker spec' {
        $spec = New-FluenceSpec DatePicker -Name X -SelectedDate ([datetime]'2026-07-04')
        $spec.SelectedDate.Year | Should -Be 2026
    }
    It 'composes Time as a TimePicker spec' {
        $spec = New-FluenceSpec TimePicker -Name X -SelectedTime ([timespan]'09:30')
        $spec.SelectedTime.TotalMinutes | Should -Be 570
    }
    It 'composes Link as a HyperlinkButton spec' {
        $spec = New-FluenceSpec HyperlinkButton -Content 'Docs' -NavigateUri 'https://example.test/docs'
        $spec.NavigateUri | Should -Be 'https://example.test/docs'
    }
}
