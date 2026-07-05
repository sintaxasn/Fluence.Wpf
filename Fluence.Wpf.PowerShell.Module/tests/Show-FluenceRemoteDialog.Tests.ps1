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

Describe 'Module unload cleanup' {

    # Removing the module runs the .psm1 OnRemove handler, which must terminate the out-of-process
    # host so a stray child never outlives the session. This starts a real host (hence UI-gated),
    # captures its process id, removes the module, and asserts that specific process is gone. The
    # module is re-imported afterward so the shared BeforeAll state is restored for any later run.
    AfterEach {
        Import-Module $script:ModulePath -Force
    }

    It 'terminates the host process on Remove-Module (OnRemove)' -Tag UI -Skip:($env:FLUENCE_PS_UI -ne '1') {
        Import-Module $script:ModulePath -Force
        $dialog = New-FluenceDialogSpec -Content @(
            New-FluenceSpec TextBlock -Text 'unload'
        ) -Buttons (New-FluenceButton -Text 'OK' -IsDefault)
        $null = Show-FluenceRemoteDialog -Spec $dialog -Theme Light -Backdrop None -TimeoutSeconds 1

        $hostProcesses = @(Get-Process -Name 'Fluence.Wpf.RemoteHost' -ErrorAction SilentlyContinue)
        $hostProcesses.Count | Should -BeGreaterThan 0 -Because 'the host must be running after a Show-FluenceRemoteDialog call'
        $hostIds = $hostProcesses.Id

        Remove-Module Fluence.Wpf.PowerShell -Force

        # OnRemove -> Close-FluenceRemoteHost -> Dispose blocks on Shutdown, so the process should be
        # gone by the time Remove-Module returns; poll briefly to absorb any teardown latency.
        $deadline = (Get-Date).AddSeconds(15)
        do {
            Start-Sleep -Milliseconds 200
            $stillAlive = @(Get-Process -Id $hostIds -ErrorAction SilentlyContinue)
        } while ($stillAlive.Count -gt 0 -and (Get-Date) -lt $deadline)

        $stillAlive.Count | Should -Be 0 -Because 'module removal must terminate the host process'
    }
}
