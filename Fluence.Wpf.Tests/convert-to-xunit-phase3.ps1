# Phase 3 MSTest -> xunit.v3 conversion: compiler-error-driven fixes.
# Reads build-errors.txt (file(line,col): error CODE: ...) and repairs Assert call sites:
#   - Assert.Contains(container, item)      -> swap first two args (MSTest order)
#   - Assert.X(..., messageExpression)      -> drop trailing message argument
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

# min arg counts after removing message
$minArgs = @{
    'Equal' = 2; 'NotEqual' = 2; 'Same' = 2; 'NotSame' = 2; 'Equivalent' = 2
    'Throws' = 1; 'Null' = 1; 'NotNull' = 1; 'Single' = 1; 'Empty' = 1; 'NotEmpty' = 1
    'True' = 1; 'False' = 1; 'IsAssignableFrom' = 1; 'Contains' = 2; 'DoesNotContain' = 2
}

# Collect (file, line) pairs for relevant errors
$sites = @{}
foreach ($ln in Get-Content $ErrorFile) {
    if ($ln -match '^(?<file>[A-Z]:[^(]+)\((?<line>\d+),\d+\): error (?<code>CS1501|CS1503|CS0117|CS1929|CS1662|CS0029)') {
        $key = $Matches['file']
        if (-not $sites.ContainsKey($key)) { $sites[$key] = New-Object System.Collections.ArrayList }
        [void]$sites[$key].Add([int]$Matches['line'])
    }
}

$callPattern = [regex]'\bAssert\.(?<m>\w+)(?<gen><[^<>;=]*>)?\('

foreach ($file in $sites.Keys) {
    if (-not (Test-Path $file)) { continue }
    $t = [System.IO.File]::ReadAllText($file)
    # compute line start offsets
    $lineStarts = New-Object System.Collections.ArrayList
    [void]$lineStarts.Add(0)
    for ($i = 0; $i -lt $t.Length; $i++) { if ($t[$i] -eq "`n") { [void]$lineStarts.Add($i + 1) } }

    $targetLines = $sites[$file] | Sort-Object -Unique -Descending
    foreach ($lineNo in $targetLines) {
        # find the Assert call whose span covers this line: search back up to 8 lines
        $found = $null
        for ($back = 0; $back -le 8; $back++) {
            $searchLine = $lineNo - $back
            if ($searchLine -lt 1) { break }
            $start = $lineStarts[$searchLine - 1]
            $end = if ($searchLine -lt $lineStarts.Count) { $lineStarts[$searchLine] } else { $t.Length }
            $segment = $t.Substring($start, $end - $start)
            $ms = $callPattern.Matches($segment)
            if ($ms.Count -gt 0) {
                # take the last match on the line whose call spans the target line
                for ($k = $ms.Count - 1; $k -ge 0; $k--) {
                    $abs = $start + $ms[$k].Index
                    $open = $abs + $ms[$k].Length - 1
                    $call = Get-CallArgs $t $open
                    $targetOffset = $lineStarts[$lineNo - 1]
                    if ($call.Close -ge $targetOffset) {
                        $found = @{ Match = $ms[$k]; Abs = $abs; Open = $open; Call = $call }
                        break
                    }
                }
            }
            if ($found) { break }
        }
        if (-not $found) { continue }

        $name = $found.Match.Groups['m'].Value
        $call = $found.Call
        $n = $call.Args.Count
        if (-not $minArgs.ContainsKey($name)) { continue }
        $min = $minArgs[$name]

        if ($name -eq 'Contains' -or $name -eq 'DoesNotContain') {
            # swap first two args (MSTest collection order), drop any 3rd non-comparison arg
            $a0 = $t.Substring($call.Args[0][0], $call.Args[0][1] - $call.Args[0][0]).Trim()
            $a1 = $t.Substring($call.Args[1][0], $call.Args[1][1] - $call.Args[1][0]).Trim()
            $tail = ''
            for ($k = 2; $k -lt $n; $k++) {
                $ak = $t.Substring($call.Args[$k][0], $call.Args[$k][1] - $call.Args[$k][0]).Trim()
                if ($ak.StartsWith('StringComparison') -or $ak.StartsWith('StringComparer')) { $tail = ', ' + $ak }
            }
            $new = "Assert.$name($a1, $a0$tail)"
            $t = $t.Substring(0, $found.Abs) + $new + $t.Substring($call.Close + 1)
        }
        elseif ($n -gt $min) {
            # drop the trailing (message) argument, preserving formatting
            $lastStart = $call.Args[$n - 1][0]
            $cut = $lastStart - 1
            while ($cut -gt $found.Open -and $t[$cut] -ne ',') { $cut-- }
            $t = $t.Substring(0, $found.Abs) + $t.Substring($found.Abs, $cut - $found.Abs) + ')' + $t.Substring($call.Close + 1)
        }
    }

    [System.IO.File]::WriteAllText($file, $t, $utf8Bom)
    Write-Host "fixed: $(Split-Path $file -Leaf)"
}
