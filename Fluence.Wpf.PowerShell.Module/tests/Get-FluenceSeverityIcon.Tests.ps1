#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
    $script:Resolve = { param($icon) & (Get-Module Fluence.Wpf.PowerShell) { param($i) Get-FluenceSeverityIcon -Icon $i } $icon }
}

Describe 'Get-FluenceSeverityIcon' {
    It 'returns $null for None' {
        & $script:Resolve 'None' | Should -BeNullOrEmpty
    }
    It 'maps Info to the attention glyph and brush' {
        $r = & $script:Resolve 'Info'
        $r.Glyph | Should -Be ([string][char]0xE946)
        $r.BrushKey | Should -Be 'SystemFillColorAttentionBrush'
    }
    It 'maps Success/Warning/Error to their template glyphs' {
        (& $script:Resolve 'Success').Glyph  | Should -Be ([string][char]0xE73E)
        (& $script:Resolve 'Warning').Glyph  | Should -Be ([string][char]0xE7BA)
        (& $script:Resolve 'Error').Glyph    | Should -Be ([string][char]0xEA39)
        (& $script:Resolve 'Error').BrushKey | Should -Be 'SystemFillColorCriticalBrush'
    }
    It 'maps Question to the Help glyph with the attention brush' {
        $r = & $script:Resolve 'Question'
        $r.Glyph | Should -Be ([string][char]0xE897)
        $r.BrushKey | Should -Be 'SystemFillColorAttentionBrush'
    }
}
