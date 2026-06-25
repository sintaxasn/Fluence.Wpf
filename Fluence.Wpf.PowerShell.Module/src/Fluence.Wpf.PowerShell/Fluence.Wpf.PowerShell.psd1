@{
    RootModule           = 'Fluence.Wpf.PowerShell.psm1'
    ModuleVersion        = '0.1.0'
    GUID                 = 'ad4e53a0-2f63-4f2a-b613-0816b85d3164'
    Author               = 'Dan Cunningham'
    CompanyName          = 'Dan Cunningham'
    Copyright            = 'Copyright (c) 2026 Dan Cunningham. All rights reserved.'
    Description          = 'Declarative Fluent (Windows 11) dialogs for PowerShell, built on Fluence.Wpf.'
    PowerShellVersion    = '5.1'
    CompatiblePSEditions = @('Desktop', 'Core')
    FunctionsToExport    = @(
        'Show-FluenceDialog',
        'New-FluencePrompt',
        'New-FluenceButton',
        'Show-FluenceMessage'
    )
    CmdletsToExport      = @()
    VariablesToExport    = @()
    AliasesToExport      = @()
    FormatsToProcess     = @('Types/Fluence.Format.ps1xml')
    PrivateData          = @{
        PSData = @{
            Tags       = @('GUI', 'WPF', 'Fluent', 'Windows11', 'Dialog', 'PSADT')
            ProjectUri = 'https://github.com/sintaxasn/Fluence.Wpf'
            LicenseUri = 'https://github.com/sintaxasn/Fluence.Wpf/blob/main/LICENSE'
        }
    }
}
