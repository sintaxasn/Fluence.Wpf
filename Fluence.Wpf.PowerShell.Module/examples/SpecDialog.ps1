<#
.SYNOPSIS
    The typed dialog-spec walkthrough: compose a custom Fluent input dialog from serializable
    spec objects (no XAML, no C#) and show it in-process.
.NOTES
    Runs on Windows PowerShell 5.1 and PowerShell 7+. The same DialogSpec object is designed to
    cross process boundaries in the out-of-process transports.
#>

Import-Module "$PSScriptRoot\..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1" -Force

$dialog = New-FluenceDialogSpec -Title 'Contoso IT' -Content @(
    New-FluenceSpec TextBlock -Text 'Before we upgrade, tell us where you sit.'
    New-FluenceSpec TextBox   -Name Desk -PlaceholderText 'Desk number' -Rules (New-FluenceRule -NotEmpty)
    New-FluenceSpec ComboBox  -Name Site -Items 'Sydney', 'Melbourne', 'Auckland'
    New-FluenceSpec CheckBox  -Name Vpn  -Content 'I use VPN daily'
) -Buttons (New-FluenceButton -Text 'Continue' -IsDefault), 'Defer'

$result = Show-FluenceDialogSpec -Spec $dialog

"Button : $($result.Button)"
"Desk   : $($result.Values.Desk)"
"Site   : $($result.Values.Site)"
"Vpn    : $($result.Values.Vpn)"
