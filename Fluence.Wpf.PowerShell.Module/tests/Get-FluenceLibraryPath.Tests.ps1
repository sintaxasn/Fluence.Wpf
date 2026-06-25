BeforeAll {
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/Get-FluenceLibraryPath.ps1"
    $script:root = Join-Path $TestDrive 'modroot'
    New-Item -ItemType Directory -Path (Join-Path $script:root 'lib/net472') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $script:root 'lib/net8.0-windows') -Force | Out-Null
    New-Item -ItemType File -Path (Join-Path $script:root 'lib/net472/Fluence.Wpf.dll') -Force | Out-Null
    New-Item -ItemType File -Path (Join-Path $script:root 'lib/net8.0-windows/Fluence.Wpf.dll') -Force | Out-Null
}

Describe 'Get-FluenceLibraryPath' {
    Context 'Edition selection' {
        It 'returns the net8.0-windows path for Core' {
            $p = Get-FluenceLibraryPath -ModuleRoot $script:root -Edition 'Core'
            $p | Should -Match 'net8\.0-windows'
        }
        It 'returns the net472 path for Desktop' {
            $p = Get-FluenceLibraryPath -ModuleRoot $script:root -Edition 'Desktop'
            $p | Should -Match 'net472'
        }
    }
    Context 'Missing assembly' {
        It 'throws when the dll is absent' {
            $empty = Join-Path $TestDrive 'empty'
            New-Item -ItemType Directory -Path $empty -Force | Out-Null
            { Get-FluenceLibraryPath -ModuleRoot $empty -Edition 'Core' } | Should -Throw '*not found*'
        }
    }
}
