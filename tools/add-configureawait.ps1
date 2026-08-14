# Appends .ConfigureAwait(true) to awaited invocation expressions lacking ConfigureAwait.
$ErrorActionPreference = 'Stop'
$root = 'C:\Repos\Fluence.Wpf\Fluence.Wpf.Tests'
$files = Get-ChildItem $root -Filter *.cs -Recurse | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
$changedFiles = 0

foreach ($f in $files) {
    $t = [System.IO.File]::ReadAllText($f.FullName)
    $sb = [System.Text.StringBuilder]::new()
    $i = 0
    $changed = $false
    while ($i -lt $t.Length) {
        $m = [regex]::Match($t.Substring($i), '(?<![\w.])await\s+[\w\.]+(<[^>]+>)?\s*\(')
        if (-not $m.Success) { [void]$sb.Append($t.Substring($i)); break }
        $start = $i + $m.Index
        [void]$sb.Append($t.Substring($i, $m.Index + $m.Length))
        # scan balanced parens from just after the opening '('
        $pos = $start + $m.Length
        $depth = 1
        $inStr = $false; $inChar = $false; $inVerbatim = $false; $inLineComment = $false
        while ($pos -lt $t.Length -and $depth -gt 0) {
            $c = $t[$pos]
            if ($inLineComment) { if ($c -eq "`n") { $inLineComment = $false } }
            elseif ($inStr) {
                if ($inVerbatim) { if ($c -eq '"') { if ($pos+1 -lt $t.Length -and $t[$pos+1] -eq '"') { [void]$sb.Append($c); $pos++; $c = $t[$pos] } else { $inStr = $false } } }
                else { if ($c -eq '\') { [void]$sb.Append($c); $pos++; $c = $t[$pos] } elseif ($c -eq '"') { $inStr = $false } }
            }
            elseif ($inChar) { if ($c -eq '\') { [void]$sb.Append($c); $pos++; $c = $t[$pos] } elseif ($c -eq "'") { $inChar = $false } }
            elseif ($c -eq '/' -and $pos+1 -lt $t.Length -and $t[$pos+1] -eq '/') { $inLineComment = $true }
            elseif ($c -eq '"') { $inStr = $true; $inVerbatim = ($pos -gt 0 -and $t[$pos-1] -eq '@') }
            elseif ($c -eq "'") { $inChar = $true }
            elseif ($c -eq '(') { $depth++ }
            elseif ($c -eq ')') { $depth-- }
            [void]$sb.Append($c)
            $pos++
        }
        # $pos now just after closing ')'
        $rest = $t.Substring($pos)
        if ($rest -notmatch '^\s*\.\s*ConfigureAwait' -and $rest -notmatch '^\s*\.') {
            [void]$sb.Append('.ConfigureAwait(true)')
            $changed = $true
        }
        $i = $pos
    }
    if ($changed) {
        [System.IO.File]::WriteAllText($f.FullName, $sb.ToString())
        $changedFiles++
    }
}
Write-Output "Files changed: $changedFiles"
