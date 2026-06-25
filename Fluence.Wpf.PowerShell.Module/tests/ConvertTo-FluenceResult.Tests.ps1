BeforeAll {
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/ConvertTo-FluenceResult.ps1"
}
Describe 'ConvertTo-FluenceResult' {
    It 'projects the hashtable to a PSCustomObject with the same keys' {
        $h = @{ User = 'bob'; OK = $true; Cancel = $false; Cancelled = $false }
        $o = ConvertTo-FluenceResult -Result $h
        $o.User | Should -Be 'bob'
        $o.OK | Should -BeTrue
        $o.Cancelled | Should -BeFalse
    }
    It 'tags the object as Fluence.DialogResult' {
        (ConvertTo-FluenceResult -Result @{ Cancelled = $true }).PSObject.TypeNames[0] |
            Should -Be 'Fluence.DialogResult'
    }
}
