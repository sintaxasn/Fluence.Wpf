BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/ConvertTo-FluencePromptList.ps1"
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/ConvertTo-FluenceButtonList.ps1"
}

Describe 'ConvertTo-FluencePromptList' {
    It 'turns a bare string into a Text prompt' {
        $list = ConvertTo-FluencePromptList -InputObject @('Your name?')
        $list[0].PSObject.TypeNames[0] | Should -Be 'Fluence.Prompt'
        $list[0].InputType | Should -Be 'Text'
        $list[0].Message | Should -Be 'Your name?'
    }
    It 'passes a Fluence.Prompt through' {
        $p = New-FluencePrompt -Name A -Message 'a'
        (ConvertTo-FluencePromptList -InputObject @($p))[0].Name | Should -Be 'A'
    }
}

Describe 'ConvertTo-FluenceButtonList' {
    It 'turns a bare string into a button' {
        $list = ConvertTo-FluenceButtonList -InputObject @('OK')
        $list[0].PSObject.TypeNames[0] | Should -Be 'Fluence.Button'
        $list[0].Text | Should -Be 'OK'
    }
}
