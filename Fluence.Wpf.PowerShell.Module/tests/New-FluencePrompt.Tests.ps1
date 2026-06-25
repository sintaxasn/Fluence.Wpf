BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'New-FluencePrompt' {
    Context 'Output contract' {
        It 'tags the object as Fluence.Prompt' {
            $p = New-FluencePrompt -Name City -Message 'City?'
            $p.PSObject.TypeNames[0] | Should -Be 'Fluence.Prompt'
        }
        It 'defaults InputType to Text' {
            (New-FluencePrompt -Name City -Message 'City?').InputType | Should -Be 'Text'
        }
        It 'carries Name, Message and DefaultValue' {
            $p = New-FluencePrompt -Name City -Message 'City?' -DefaultValue 'Leeds'
            $p.Name | Should -Be 'City'
            $p.Message | Should -Be 'City?'
            $p.DefaultValue | Should -Be 'Leeds'
        }
    }
    Context 'Input validation' {
        It 'rejects an unknown InputType' {
            { New-FluencePrompt -Name X -Message 'x' -InputType Nope } | Should -Throw
        }
        It 'requires Choice prompts to supply a ValidateSet' {
            { New-FluencePrompt -Name X -Message 'x' -InputType Choice } | Should -Throw '*ValidateSet*'
        }
    }
}
