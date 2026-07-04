#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Covers the out-of-process dialog cmdlets. Parameter binding and Close-FluenceRemoteHost
# idempotency are headless-safe and always run. The end-to-end show+timeout-dismiss case launches
# the real host executable and shows a (self-dismissing) window, so it is tagged UI and gated on
# FLUENCE_PS_UI, matching the suite's convention for UI-touching tests. Every test that starts a
# host tears it down in an AfterEach, mirroring the "always dismiss in a finally" discipline.

BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force
}

Describe 'Show-FluenceRemoteDialog' {

    AfterEach {
        Close-FluenceRemoteHost -ErrorAction SilentlyContinue
    }

    It 'rejects a -Spec that is not a DialogSpec, before touching the pipe' {
        { Show-FluenceRemoteDialog -Spec 'not a spec' } | Should -Throw '*Fluence.Wpf.Specs.DialogSpec*'
    }

    It 'shows a spec out-of-process and self-dismisses on timeout' -Tag UI -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $dialog = New-FluenceDialogSpec -Title 'Remote' -Content @(
            New-FluenceSpec TextBlock -Text 'Out-of-process, self-dismissing.'
            New-FluenceSpec TextBox -Name Desk -PlaceholderText 'Desk number'
        ) -Buttons (New-FluenceButton -Text 'OK' -IsDefault)

        $result = Show-FluenceRemoteDialog -Spec $dialog -Theme Light -Backdrop None -TimeoutSeconds 1

        $result.PSTypeNames | Should -Contain 'Fluence.SpecDialogResult'
        $result.Button | Should -Be 'Cancelled'
        $result.Values.ContainsKey('Desk') | Should -BeTrue
    }

    It 'reuses one host process across repeated cycles without hanging' -Tag UI -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $dialog = New-FluenceDialogSpec -Content @(
            New-FluenceSpec TextBlock -Text 'cycle'
        ) -Buttons (New-FluenceButton -Text 'OK' -IsDefault)

        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        foreach ($cycle in 1..5) {
            $result = Show-FluenceRemoteDialog -Spec $dialog -Theme Light -Backdrop None -TimeoutSeconds 1
            $result.Button | Should -Be 'Cancelled'
        }
        $sw.Stop()
        $sw.Elapsed.TotalSeconds | Should -BeLessThan 75
    }
}

Describe 'Close-FluenceRemoteHost' {

    It 'is a no-op when no host was ever started' {
        { Close-FluenceRemoteHost } | Should -Not -Throw
    }

    It 'is idempotent when called twice' {
        { Close-FluenceRemoteHost; Close-FluenceRemoteHost } | Should -Not -Throw
    }
}
