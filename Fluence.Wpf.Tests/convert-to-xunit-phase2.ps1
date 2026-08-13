# Phase 2 MSTest -> xunit.v3 conversion: argument-aware rewrites.
# Preserves UTF-8 BOM + LF. Delete after migration.
$root = $PSScriptRoot
$files = Get-ChildItem -Path $root -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
$utf8Bom = New-Object System.Text.UTF8Encoding($true)

function Get-CallArgs([string]$t, [int]$openParen) {
    # Returns hashtable: Args = list of (start,end) index pairs; Close = index of ')'
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

function Test-StringIsh([string]$s) {
    $s = $s.Trim()
    return $s.StartsWith('"') -or $s.StartsWith('$"') -or $s.StartsWith('@"') -or
        $s.StartsWith('$@"') -or $s.StartsWith('@$"') -or $s.StartsWith('string.Format') -or
        $s.StartsWith('string.Join') -or $s.StartsWith('string.Create')
}

# methods where a trailing string message must be dropped once arg count exceeds N
$dropAfter = @{
    'Equal' = 2; 'NotEqual' = 2; 'Same' = 2; 'NotSame' = 2; 'Equivalent' = 2
    'Throws' = 1; 'Null' = 1; 'NotNull' = 1; 'Single' = 1; 'Empty' = 1; 'NotEmpty' = 1
}
$comparisons = @{
    'IsGreaterThan' = '>'; 'IsGreaterThanOrEqualTo' = '>='
    'IsLessThan' = '<'; 'IsLessThanOrEqualTo' = '<='
}

$callPattern = [regex]'(?<cls>\b(?:Collection|String)?Assert)\.(?<m>\w+)(?<gen><[^<>;=]*>)?\('

foreach ($f in $files) {
    $t = [System.IO.File]::ReadAllText($f.FullName)
    $orig = $t

    $t = $t.Replace('AssertFailedException', 'Xunit.Sdk.XunitException')

    # iterate until stable since replacements shift offsets
    for ($pass = 0; $pass -lt 10; $pass++) {
        $changed = $false
        $matches = $callPattern.Matches($t)
        for ($mi = $matches.Count - 1; $mi -ge 0; $mi--) {
            $m = $matches[$mi]
            $cls = $m.Groups['cls'].Value
            $name = $m.Groups['m'].Value
            $gen = $m.Groups['gen'].Value
            $open = $m.Index + $m.Length - 1
            $call = Get-CallArgs $t $open
            $callArgs = $call.Args
            $close = $call.Close
            $argTexts = @()
            foreach ($a in $callArgs) { $argTexts += $t.Substring($a[0], $a[1] - $a[0]) }
            $n = $argTexts.Count
            $new = $null

            if ($cls -eq 'Assert') {
                if ($comparisons.ContainsKey($name) -and ($n -eq 2 -or $n -eq 3)) {
                    $op = $comparisons[$name]
                    $bound = $argTexts[0].Trim()
                    $value = $argTexts[1].Trim()
                    $msg = if ($n -eq 3) { ', ' + $argTexts[2].Trim() } else { '' }
                    $new = "Assert.True(($value) $op ($bound)$msg)"
                }
                elseif ($name -eq 'IsType' -and [string]::IsNullOrEmpty($gen) -eq $false) {
                    $keep = if ($n -gt 1 -and (Test-StringIsh $argTexts[$n - 1])) { $argTexts[0].Trim() } else { $null }
                    if ($n -eq 1) { $new = "Assert.IsAssignableFrom$gen(" + $argTexts[0].Trim() + ')' }
                    elseif ($keep) { $new = "Assert.IsAssignableFrom$gen($keep)" }
                }
                elseif ($name -eq 'IsType' -and [string]::IsNullOrEmpty($gen)) {
                    # MSTest IsInstanceOfType(value, type[, msg]) -> IsAssignableFrom(type, value)
                    if ($n -ge 2) {
                        $new = 'Assert.IsAssignableFrom(' + $argTexts[1].Trim() + ', ' + $argTexts[0].Trim() + ')'
                    }
                }
                elseif ($name -eq 'Contains') {
                    # converted from CollectionAssert/MSTest order: (container, item[, msg]) -> (item, container)
                    if ($n -ge 2) {
                        $rest = @()
                        for ($k = 2; $k -lt $n; $k++) {
                            if (-not ($k -eq ($n - 1) -and (Test-StringIsh $argTexts[$k]))) { $rest += $argTexts[$k].Trim() }
                        }
                        $tail = if ($rest.Count -gt 0) { ', ' + ($rest -join ', ') } else { '' }
                        $new = 'Assert.Contains(' + $argTexts[1].Trim() + ', ' + $argTexts[0].Trim() + $tail + ')'
                    }
                }
                elseif ($dropAfter.ContainsKey($name)) {
                    $minArgs = $dropAfter[$name]
                    if ($n -gt $minArgs -and (Test-StringIsh $argTexts[$n - 1])) {
                        # drop the final message argument, preserving formatting of the rest
                        $lastStart = $callArgs[$n - 1][0]
                        # trim back over the comma and whitespace preceding the dropped arg
                        $cut = $lastStart - 1
                        while ($cut -gt $open -and $t[$cut] -ne ',') { $cut-- }
                        $new = $t.Substring($m.Index, $cut - $m.Index) + ')'
                    }
                }
            }
            elseif ($cls -eq 'CollectionAssert') {
                switch ($name) {
                    'Equal' { if ($n -ge 2) { $new = 'Assert.Equal(' + $argTexts[0].Trim() + ', ' + $argTexts[1].Trim() + ')' } }
                    'DoesNotContain' { if ($n -ge 2) { $new = 'Assert.DoesNotContain(' + $argTexts[1].Trim() + ', ' + $argTexts[0].Trim() + ')' } }
                }
            }
            elseif ($cls -eq 'StringAssert') {
                if ($name -eq 'Contains' -and $n -ge 2) {
                    # StringAssert.Contains(value, substring[, comparison][, msg])
                    $comparison = ''
                    for ($k = 2; $k -lt $n; $k++) {
                        if ($argTexts[$k].Trim().StartsWith('StringComparison')) { $comparison = ', ' + $argTexts[$k].Trim() }
                    }
                    $new = 'Assert.Contains(' + $argTexts[1].Trim() + ', ' + $argTexts[0].Trim() + $comparison + ')'
                }
            }

            if ($null -ne $new) {
                $t = $t.Substring(0, $m.Index) + $new + $t.Substring($close + 1)
                $changed = $true
            }
        }
        if (-not $changed) { break }
    }

    if ($t -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $t, $utf8Bom)
        Write-Host "fixed: $($f.Name)"
    }
}
