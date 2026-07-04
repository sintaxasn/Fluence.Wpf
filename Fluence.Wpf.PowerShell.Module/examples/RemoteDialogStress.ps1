<#
.SYNOPSIS
    Shows the same composed dialog spec many times in a row through the out-of-process host,
    proving that repeated dialog cycles never hang the PowerShell session.
.DESCRIPTION
    The classic in-process failure mode is that a second ShowDialog() call in one PowerShell
    process can hang. Show-FluenceRemoteDialog renders in a separate, auto-managed host process,
    so this loop of self-dismissing dialogs completes cleanly. Each cycle uses a short
    -TimeoutSeconds so no human interaction is needed; the elapsed time per cycle is printed.
.NOTES
    Runs on Windows PowerShell 5.1 and PowerShell 7+. Close-FluenceRemoteHost tears the host down
    at the end (module removal would also do it automatically).
#>

Import-Module "$PSScriptRoot\..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1" -Force

$dialog = New-FluenceDialogSpec -Title 'Remote dialog stress' -Content @(
    New-FluenceSpec TextBlock -Text 'This dialog dismisses itself after one second.'
    New-FluenceSpec TextBox   -Name Desk -PlaceholderText 'Desk number'
) -Buttons (New-FluenceButton -Text 'Continue' -IsDefault), 'Cancel'

$cycles = 15
$overall = [System.Diagnostics.Stopwatch]::StartNew()
try
{
    for ($i = 1; $i -le $cycles; $i++)
    {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $result = Show-FluenceRemoteDialog -Spec $dialog -Theme Auto -Backdrop Mica -TimeoutSeconds 1
        $sw.Stop()
        "Cycle {0,2}: Button={1,-10} elapsed={2,5} ms" -f $i, $result.Button, [int]$sw.ElapsedMilliseconds
    }
}
finally
{
    Close-FluenceRemoteHost
}
$overall.Stop()
"Completed $cycles cycles in $([int]$overall.Elapsed.TotalSeconds) s without hanging."
