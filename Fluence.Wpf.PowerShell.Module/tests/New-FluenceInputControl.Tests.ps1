#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Verifies New-FluenceInputControl seeds $State.Result with a correctly-typed live value for an
# untouched control, so a field the user never edits still returns a usable value (not $null or a
# raw/typeless default). The control build runs through Invoke-OnFluenceUi so it executes on an STA
# thread on every host shape: inline on STA (powershell, pwsh) and the module-owned STA runspace on
# an MTA host (pwsh -mta). The prompt is built from primitives inside the UI block so nothing has to
# cross a runspace boundary.

BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force

    $script:SeedFor = {
        param($name, $inputType, $default, $hasDefault)
        & (Get-Module Fluence.Wpf.PowerShell) {
            param($n, $type, $def, $has)
            Invoke-OnFluenceUi -Script {
                param($n2, $type2, $def2, $has2)
                $pp = @{ Name = $n2; Message = 'm'; InputType = $type2 }
                if ($has2) { $pp.DefaultValue = $def2 }
                $prompt = New-FluencePrompt @pp
                $state = @{ Result = @{} }
                $null = New-FluenceInputControl -Prompt $prompt -State $state
                $state.Result[$n2]
            } -ArgumentList @($n, $type, $def, $has)
        } $name $inputType $default $hasDefault
    }
}

Describe 'New-FluenceInputControl result seeding' {

    It 'seeds an untouched Checkbox as a [bool] false, not $null' {
        $v = & $script:SeedFor 'Flag' 'Checkbox' $null $false
        $v | Should -BeOfType ([bool])
        $v | Should -BeFalse
    }

    It 'seeds an untouched Number as the control value 0, not $null' {
        $v = & $script:SeedFor 'Qty' 'Number' $null $false
        $v | Should -Be 0
    }

    It 'seeds a Checkbox from a string default through [bool] coercion' {
        $v = & $script:SeedFor 'Flag' 'Checkbox' 'true' $true
        $v | Should -BeTrue
    }
}
