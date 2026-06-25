BeforeAll {
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/Test-FluenceInput.ps1"
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Public/New-FluencePrompt.ps1"
}

Describe 'Test-FluenceInput' {
    It 'fails an empty required value' {
        $p = New-FluencePrompt -Name U -Message 'u' -ValidateNotEmpty
        $r = Test-FluenceInput -Prompts @($p) -Values @{ U = '' }
        $r.IsValid | Should -BeFalse
        $r.Message | Should -Match 'U'
    }
    It 'passes a present required value' {
        $p = New-FluencePrompt -Name U -Message 'u' -ValidateNotEmpty
        (Test-FluenceInput -Prompts @($p) -Values @{ U = 'abc' }).IsValid | Should -BeTrue
    }
    It 'enforces a pattern' {
        $p = New-FluencePrompt -Name Code -Message 'c' -ValidatePattern '^\d{3}$'
        (Test-FluenceInput -Prompts @($p) -Values @{ Code = '12' }).IsValid | Should -BeFalse
        (Test-FluenceInput -Prompts @($p) -Values @{ Code = '123' }).IsValid | Should -BeTrue
    }
    It 'runs a ValidateScript' {
        $p = New-FluencePrompt -Name N -Message 'n' -ValidateScript { param($v) [int]$v -gt 5 }
        (Test-FluenceInput -Prompts @($p) -Values @{ N = '3' }).IsValid | Should -BeFalse
        (Test-FluenceInput -Prompts @($p) -Values @{ N = '9' }).IsValid | Should -BeTrue
    }
}
