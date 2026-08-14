# Fixpoint: await call sites of local async Task helpers, then make those callers async.
$ErrorActionPreference = 'Stop'
$root = 'C:\Repos\Fluence.Wpf\Fluence.Wpf.Tests'
$files = Get-ChildItem $root -Filter *.cs -Recurse | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }

function Get-AsyncMethodNames {
    $names = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($f in Get-ChildItem $root -Filter *.cs -Recurse | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }) {
        foreach ($m in [regex]::Matches([System.IO.File]::ReadAllText($f.FullName), '(?m)^\s*(public|private|internal|protected)[\w\s]*\basync Task\b\s+(\w+)\s*\(')) {
            $null = $names.Add($m.Groups[2].Value)
        }
    }
    return $names
}

for ($iter = 1; $iter -le 10; $iter++) {
    $names = Get-AsyncMethodNames
    Write-Output "Iteration ${iter}: $($names.Count) async methods known"
    $changedAny = $false

    foreach ($f in $files) {
        $t = [System.IO.File]::ReadAllText($f.FullName)
        $orig = $t
        foreach ($n in $names) {
            # await bare statement-position calls: start of line, optional indentation, name( ... not already awaited/returned/assigned
            $t = [regex]::Replace($t, "(?m)^(?<ind>\s+)(?<!await\s)(?<call>$n\()", '${ind}await ${call}')
            # repair double-await if produced
            $t = $t -replace "await await ", 'await '
            # repair signatures accidentally hit: 'await Name(' on declaration lines never matches because those lines contain modifiers before name
        }
        if ($t -ne $orig) {
            [System.IO.File]::WriteAllText($f.FullName, $t)
            $changedAny = $true
        }
    }

    # make newly-awaiting methods async
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File C:\Repos\Fluence.Wpf\tools\make-await-methods-async.ps1
    if (($out -join "`n") -match 'Files changed: (\d+)' -and [int]$Matches[1] -gt 0) { $changedAny = $true }

    if (-not $changedAny) { Write-Output 'Fixpoint reached'; break }
}
