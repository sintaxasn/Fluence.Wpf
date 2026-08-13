# Phase 6: remove unused trailing string parameters from assertion helpers and
# drop the matching final argument at every call site. Preserves UTF-8 BOM + LF.
# Delete after migration.
$utf8Bom = New-Object System.Text.UTF8Encoding($true)
$root = $PSScriptRoot

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

# file (relative) => helper method names whose final parameter is unused
$targets = @{
    'ControlTests.BackgroundParity.cs' = @('AssertBrushColor', 'AssertBrushResolves')
    'ControlTests.DemoParity.cs'       = @('AssertControlHasThemedBorder')
    'ControlTests.DemoSamplePolish.cs' = @('AssertBrushIsTransparent')
    'ControlTests.IconForeground.cs'   = @('AssertIconMatchesText')
    'ControlTests.NavigationView.cs'   = @('AssertContentOffsetEventually', 'AssertPaneToggleVisible')
    'ControlTests.ProgressRing.cs'     = @('AssertPathStroke', 'AssertDependencyPropertyNotAnimated')
    'ControlTests.ToggleButton.cs'     = @('GetSolidColor')
    'DemoMainWindowTests.cs'           = @('AssertIconBrush')
    'ThemeMarkupTests.cs'              = @('AssertProbeBrush')
}

foreach ($rel in $targets.Keys) {
    $path = Join-Path $root $rel
    if (-not (Test-Path $path)) { Write-Host "MISSING: $rel"; continue }
    $t = [System.IO.File]::ReadAllText($path)
    $orig = $t

    foreach ($helper in $targets[$rel]) {
        $pattern = [regex]("\b" + [regex]::Escape($helper) + "\(")
        for ($pass = 0; $pass -lt 20; $pass++) {
            $changed = $false
            $ms = $pattern.Matches($t)
            for ($mi = $ms.Count - 1; $mi -ge 0; $mi--) {
                $m = $ms[$mi]
                $open = $m.Index + $m.Length - 1
                # skip XML doc / comment lines
                $lineStart = $t.LastIndexOf("`n", $m.Index) + 1
                $prefix = $t.Substring($lineStart, $m.Index - $lineStart)
                if ($prefix -match '///|//\s') { continue }
                $call = Get-CallArgs $t $open
                $n = $call.Args.Count
                if ($n -lt 2) { continue }
                $lastStart = $call.Args[$n - 1][0]
                $lastText = $t.Substring($lastStart, $call.Args[$n - 1][1] - $lastStart).Trim()
                $isDefinition = $lastText -match '^(string|ApplicationTheme)\s'
                $isMessageArg = $lastText.StartsWith('"') -or $lastText.StartsWith('$"') -or $lastText.StartsWith('@"') -or
                    $lastText -match '^[a-z]\w*$' -or $lastText -match '^\w+(\s*\+\s*"|\.\w+)'
                if (-not ($isDefinition -or $isMessageArg)) { continue }
                $cut = $lastStart - 1
                while ($cut -gt $open -and $t[$cut] -ne ',') { $cut-- }
                if ($t[$cut] -ne ',') { continue }
                $t = $t.Substring(0, $cut) + ')' + $t.Substring($call.Close + 1)
                $changed = $true
            }
            if (-not $changed) { break }
        }
    }

    if ($t -ne $orig) {
        [System.IO.File]::WriteAllText($path, $t, $utf8Bom)
        Write-Host "fixed: $rel"
    }
}
