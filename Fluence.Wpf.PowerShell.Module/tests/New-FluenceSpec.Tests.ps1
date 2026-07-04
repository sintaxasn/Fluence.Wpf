BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'New-FluenceSpec' {
    Context 'Output contract' {
        It 'builds a typed spec object' {
            $spec = New-FluenceSpec TextBox -Name Desk
            $spec.GetType().FullName | Should -Be 'Fluence.Wpf.Specs.TextBoxSpec'
            $spec.Name | Should -Be 'Desk'
        }
        It 'binds curated members as dynamic parameters' {
            $spec = New-FluenceSpec TextBox -Name Desk -PlaceholderText 'Desk number' -HelperText 'Level 3 desks start with 3'
            $spec.PlaceholderText | Should -Be 'Desk number'
            $spec.HelperText | Should -Be 'Level 3 desks start with 3'
        }
        It 'binds list members from arrays' {
            $spec = New-FluenceSpec ComboBox -Name Site -Items 'Sydney', 'Melbourne'
            $spec.Items.Count | Should -Be 2
            $spec.Items[1] | Should -Be 'Melbourne'
        }
        It 'binds mirrored enum members with typed values' {
            $spec = New-FluenceSpec StackPanel -Orientation Horizontal -Spacing 8
            "$($spec.Orientation)" | Should -Be 'Horizontal'
            $spec.Spacing | Should -Be 8
        }
        It 'nests container children built by New-FluenceSpec' {
            $spec = New-FluenceSpec StackPanel -Children @(
                New-FluenceSpec RadioButton -GroupName Fruit -Content 'Apple'
                New-FluenceSpec RadioButton -GroupName Fruit -Content 'Pear'
            )
            $spec.Children.Count | Should -Be 2
            $spec.Children[0].GetType().Name | Should -Be 'RadioButtonSpec'
        }
        It 'attaches rules from New-FluenceRule' {
            $spec = New-FluenceSpec TextBox -Name Desk -Rules (New-FluenceRule -NotEmpty -MaxLength 12)
            $spec.Rules.Count | Should -Be 2
        }
        It 'binds Image members including byte[] auto-encoding to Base64' {
            $bytes = [byte[]](1, 2, 3, 4)
            $spec = New-FluenceSpec Image -Source 'C:\brand\banner.png' -SourceBase64 $bytes -Stretch UniformToFill -CornerRadius '8'
            $spec.GetType().FullName | Should -Be 'Fluence.Wpf.Specs.ImageSpec'
            $spec.Source | Should -Be 'C:\brand\banner.png'
            $spec.SourceBase64 | Should -Be ([Convert]::ToBase64String($bytes))
            "$($spec.Stretch)" | Should -Be 'UniformToFill'
            $spec.CornerRadius | Should -Be '8'
        }
    }
    Context 'Input validation' {
        It 'rejects an unknown control type and lists the available ones' {
            { New-FluenceSpec -Type Bogus } | Should -Throw '*Available controls*TextBox*'
        }
        It 'constructs every curated control type' {
            $types = 'TextBlock', 'TextBox', 'PasswordBox', 'NumberBox', 'CheckBox', 'ToggleSwitch',
                'ComboBox', 'RadioButton', 'DatePicker', 'TimePicker', 'HyperlinkButton', 'InfoBar',
                'ProgressBar', 'ProgressRing', 'StackPanel', 'Border', 'Image'
            foreach ($type in $types)
            {
                (New-FluenceSpec -Type $type).GetType().Name | Should -Be "${type}Spec"
            }
        }
    }
}

Describe 'New-FluenceRule' {
    It 'requires at least one rule switch' {
        { New-FluenceRule } | Should -Throw '*at least one rule*'
    }
    It 'emits one rule per requested check in stable order' {
        $rules = New-FluenceRule -NotEmpty -Pattern '^\d+$' -MinLength 2 -Maximum 99
        $rules.Count | Should -Be 4
        $rules[0].GetType().Name | Should -Be 'NotEmptyRule'
        $rules[1].GetType().Name | Should -Be 'PatternRule'
        $rules[2].GetType().Name | Should -Be 'LengthRule'
        $rules[3].GetType().Name | Should -Be 'RangeRule'
    }
    It 'applies a custom error message to every rule it creates' {
        $rules = New-FluenceRule -NotEmpty -MaxLength 5 -ErrorMessage 'Nope.'
        foreach ($rule in $rules)
        {
            $rule.ErrorMessage | Should -Be 'Nope.'
        }
    }
}
