# Marks lambdas whose brace-block body contains 'await' as async.
$ErrorActionPreference = 'Stop'
$root = 'C:\Repos\Fluence.Wpf\Fluence.Wpf.Tests'
$files = Get-ChildItem $root -Filter *.cs -Recurse | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
$changed = 0

foreach ($f in $files) {
    $t = [System.IO.File]::ReadAllText($f.FullName)
    $orig = $t
    # match lambda headers: optional 'static', params, '=>' then whitespace then '{'
    $matches2 = [regex]::Matches($t, '(?<head>(static\s+)?(\((\w+\s+\w+(,\s*\w+\s+\w+)*)?\)|\w+)\s*=>\s*)\{')
    # process from last to first so indexes stay valid
    for ($k = $matches2.Count - 1; $k -ge 0; $k--) {
        $m = $matches2[$k]
        if ($t.Substring([Math]::Max(0, $m.Index - 6), 6) -match 'async\s*$') { continue }
        # find matching close brace
        $pos = $m.Index + $m.Length
        $depth = 1
        while ($pos -lt $t.Length -and $depth -gt 0) {
            $c = $t[$pos]
            if ($c -eq '{') { $depth++ } elseif ($c -eq '}') { $depth-- }
            $pos++
        }
        $body = $t.Substring($m.Index + $m.Length, $pos - $m.Index - $m.Length)
        # only direct awaits matter; nested lambdas inside would also match but marking outer async is harmless only if truly needed.
        # crude: strip nested lambda bodies is complex; accept body-level check
        if ($body -match '\bawait\b') {
            # ensure the await is not solely inside a nested async lambda
            $stripped = [regex]::Replace($body, 'async\s+(static\s+)?(\([^)]*\)|\w+)\s*=>\s*\{[^{}]*\}', '')
            if ($stripped -match '\bawait\b') {
                $t = $t.Insert($m.Index, 'async ')
            }
        }
    }
    if ($t -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $t)
        $changed++
    }
}
Write-Output "Files changed: $changed"
