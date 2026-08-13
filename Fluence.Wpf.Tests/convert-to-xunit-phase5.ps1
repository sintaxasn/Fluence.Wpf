# Phase 5: error-driven analyzer fixes (S3415, xUnit2013, MA0002, xUnit1030, IDE0058).
# Preserves UTF-8 BOM + LF. Delete after migration.
param([string]$ErrorFile = 'C:\Repos\Fluence.Wpf\build-errors.txt')

$utf8Bom = New-Object System.Text.UTF8Encoding($true)

function Get-CallArgs([string]$t, [int]$openParen) {
    $depth = 1
    $i = $openParen + 1
    $argStart = $i
    $argList = New-Object System.Collections.ArrayList
    while ($i -lt $t.Length) {
        $c = $t[$i]
        if ($c -eq '"') {
            $j = $i - 1
            $isVerbatim = $false
            while ($j -ge 0 -and ($t[$j] -eq '$' -or $t[$j] -eq '@')) {
                if ($t[$j] -eq '@') { $isVerbatim = $true }
                $j--
            }
            $i++
            if ($isVerbatim) {
                while ($i -lt $t.Length) {
                    if ($t[$i] -eq '"') {
                        if (($i + 1) -lt $t.Length -and $t[$i + 1] -eq '"') { $i += 2; continue }
                        break
                    }
                    $i++
                }
            }
            else {
                while ($i -lt $t.Length -and $t[$i] -ne '"') {
                    if ($t[$i] -eq '\') { $i++ }
                    $i++
                }
            }
        }
        elseif ($c -eq "'") {
            $i++
            while ($i -lt $t.Length -and $t[$i] -ne "'") {
                if ($t[$i] -eq '\') { $i++ }
                $i++
            }
        }
        elseif ($c -eq '(' -or $c -eq '[' -or $c -eq '{') { $depth++ }
        elseif ($c -eq ')' -or $c -eq ']' -or $c -eq '}') {
            $depth--
            if ($depth -eq 0) { break }
        }
        elseif ($c -eq ',' -and $depth -eq 1) {
            [void]$argList.Add(@($argStart, $i))
            $argStart = $i + 1
        }
        $i++
    }
    [void]$argList.Add(@($argStart, $i))
    return @{ Args = $argList; Close = $i }
}

$sites = @{}
foreach ($ln in Get-Content $ErrorFile) {
    if ($ln -match '^(?<file>[A-Z]:[^(]+)\((?<line>\d+),\d+\): error (?<code>S3415|xUnit2013|MA0002|xUnit1030|IDE0058)') {
        $key = $Matches['file']
        if (-not $sites.ContainsKey($key)) { $sites[$key] = New-Object System.Collections.ArrayList }
        [void]$sites[$key].Add(@([int]$Matches['line'], $Matches['code']))
    }
}

$callPattern = [regex]'\bAssert\.(?<m>\w+)(?<gen><[^<>;=]*>)?\('

foreach ($file in $sites.Keys) {
    if (-not (Test-Path $file)) { continue }
    $t = [System.IO.File]::ReadAllText($file)
    $lineStarts = New-Object System.Collections.ArrayList
    [void]$lineStarts.Add(0)
    for ($i = 0; $i -lt $t.Length; $i++) { if ($t[$i] -eq "`n") { [void]$lineStarts.Add($i + 1) } }

    $targets = $sites[$file] | Sort-Object { $_[0] } -Descending
    foreach ($target in $targets) {
        $lineNo = $target[0]
        $code = $target[1]
        $start = $lineStarts[$lineNo - 1]
        $end = if ($lineNo -lt $lineStarts.Count) { $lineStarts[$lineNo] } else { $t.Length }
        $segment = $t.Substring($start, $end - $start)

        if ($code -eq 'xUnit1030') {
            $t = $t.Substring(0, $start) + ($segment -replace '\.ConfigureAwait\((?:false|true|ConfigureAwaitOptions\.\w+)\)', '') + $t.Substring($end)
            continue
        }
        if ($code -eq 'IDE0058') {
            if ($segment -match '^(\s*)(?![_}\/]|_ =|return|if|foreach|while|using|var |int |double |string |bool )([A-Za-z].*;\s*)$') {
                $t = $t.Substring(0, $start) + ($segment -replace '^(\s*)', '$1_ = ') + $t.Substring($end)
            }
            continue
        }

        $ms = $callPattern.Matches($segment)
        if ($ms.Count -eq 0) { continue }
        $m = $ms[0]
        $abs = $start + $m.Index
        $open = $abs + $m.Length - 1
        $call = Get-CallArgs $t $open
        $n = $call.Args.Count
        $name = $m.Groups['m'].Value
        $argText = @()
        foreach ($a in $call.Args) { $argText += $t.Substring($a[0], $a[1] - $a[0]).Trim() }
        $new = $null

        if ($code -eq 'S3415' -and $n -ge 2) {
            $tail = ''
            for ($k = 2; $k -lt $n; $k++) { $tail += ', ' + $argText[$k] }
            $new = "Assert.$name(" + $argText[1] + ', ' + $argText[0] + $tail + ')'
        }
        elseif ($code -eq 'xUnit2013' -and $name -match '^(Equal|NotEqual)$' -and $n -eq 2) {
            $numIdx = if ($argText[0] -match '^[01]$') { 0 } elseif ($argText[1] -match '^[01]$') { 1 } else { -1 }
            if ($numIdx -ge 0) {
                $collExpr = $argText[1 - $numIdx] -replace '\.Count\(\)$', '' -replace '\.(Count|Length)$', ''
                $method = if ($argText[$numIdx] -eq '0') { if ($name -eq 'Equal') { 'Empty' } else { 'NotEmpty' } } else { 'Single' }
                if ($method -eq 'Single') { $new = "_ = Assert.Single($collExpr)" } else { $new = "Assert.$method($collExpr)" }
            }
        }
        elseif ($code -eq 'MA0002' -and $n -eq 2) {
            if ($name -match '^(Equal|NotEqual)$') { $new = "Assert.$name(" + $argText[0] + ', ' + $argText[1] + ', StringComparer.Ordinal)' }
            elseif ($name -match '^(Contains|DoesNotContain)$') { $new = "Assert.$name(" + $argText[0] + ', ' + $argText[1] + ', StringComparison.Ordinal)' }
        }

        if ($null -ne $new) {
            # if the statement already begins with '_ = ', don't double it
            if ($new.StartsWith('_ = ')) {
                $before = $t.Substring($lineStarts[$lineNo - 1], $abs - $lineStarts[$lineNo - 1])
                if ($before -match '_\s*=\s*$') { $new = $new.Substring(4) }
            }
            $t = $t.Substring(0, $abs) + $new + $t.Substring($call.Close + 1)
        }
    }

    [System.IO.File]::WriteAllText($file, $t, $utf8Bom)
    Write-Host "fixed: $(Split-Path $file -Leaf)"
}
