# Line-based: appends .ConfigureAwait(true) to awaited calls that end on the same or later line,
# using per-line paren balance from the await expression start.
$ErrorActionPreference = 'Stop'
$root = 'C:\Repos\Fluence.Wpf\Fluence.Wpf.Tests'
$files = Get-ChildItem $root -Filter *.cs -Recurse | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
$changedFiles = 0

function Get-Balance([string]$s) {
    $bal = 0; $inStr = $false; $inChar = $false
    for ($i = 0; $i -lt $s.Length; $i++) {
        $c = $s[$i]
        if ($inStr) { if ($c -eq '\') { $i++ } elseif ($c -eq '"') { $inStr = $false }; continue }
        if ($inChar) { if ($c -eq '\') { $i++ } elseif ($c -eq "'") { $inChar = $false }; continue }
        switch ($c) {
            '"' { $inStr = $true }
            "'" { $inChar = $true }
            '(' { $bal++ }
            ')' { $bal-- }
        }
    }
    return $bal
}

foreach ($f in $files) {
    $lines = [System.Collections.Generic.List[string]]([System.IO.File]::ReadAllLines($f.FullName))
    $changed = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $m = [regex]::Match($line, '(?<![\w.])await\s+[\w\.]+(<[^>]+>)?\s*\(')
        if (-not $m.Success) { continue }
        if ($line -match 'ConfigureAwait') { continue }
        # compute balance from expression start
        $tail = $line.Substring($m.Index)
        $bal = Get-Balance $tail
        $endLine = $i
        while ($bal -gt 0 -and $endLine + 1 -lt $lines.Count) {
            $endLine++
            $bal += Get-Balance $lines[$endLine]
        }
        if ($bal -ne 0) { continue }
        $target = $lines[$endLine]
        if ($target -match 'ConfigureAwait') { continue }
        # only when the expression ends the statement: '...);' or '...))' patterns; handle common '...);'
        if ($target -match '\)\s*;\s*$' -and $target -notmatch '\)\s*\.\s*') {
            $lines[$endLine] = $target -replace '\)\s*;\s*$', ').ConfigureAwait(true);'
            $changed = $true
        }
        elseif ($target -match '\)\s*,\s*$') {
            $lines[$endLine] = $target -replace '\)\s*,\s*$', ').ConfigureAwait(true),'
            $changed = $true
        }
        elseif ($target -match '^\s*\)\)\s*;\s*$' -or $target -match '\)\),\s*$') {
            # awaited call nested inside another call ending here: append before final ')'
            $lines[$endLine] = $target -replace '\)(\)\s*[;,]\s*)$', ').ConfigureAwait(true)$1'
            $changed = $true
        }
    }
    if ($changed) {
        [System.IO.File]::WriteAllLines($f.FullName, $lines)
        $changedFiles++
    }
}
Write-Output "Files changed: $changedFiles"
