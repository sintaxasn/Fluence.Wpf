# Phase 7: repair helper call sites damaged by phase 6, using git HEAD as the
# source of the original argument lists (dropping only the final message arg).
# Preserves UTF-8 BOM + LF. Delete after migration.
$utf8Bom = New-Object System.Text.UTF8Encoding($true)
$repo = 'C:\Repos\Fluence.Wpf'

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

function Get-Calls([string]$t, [string]$helper) {
    # returns ordered list of @{ Index; Open; Close; Args(list of trimmed strings); IsDefinition }
    $result = New-Object System.Collections.ArrayList
    $pattern = [regex]("(?<pre>[\w.]?)\b" + [regex]::Escape($helper) + "\s*\(")
    foreach ($m in $pattern.Matches($t)) {
        $lineStart = $t.LastIndexOf("`n", $m.Index) + 1
        $prefix = $t.Substring($lineStart, $m.Index - $lineStart)
        if ($prefix -match '///|//\s') { continue }
        $open = $m.Index + $m.Length - 1
        $call = Get-CallArgs $t $open
        $isDef = $prefix -match '\b(void|Color|static|private|internal)\b' -and $prefix -match '(void|Color)\s*$'
        $argText = @()
        foreach ($a in $call.Args) { $argText += $t.Substring($a[0], $a[1] - $a[0]).Trim() }
        [void]$result.Add(@{ Index = $m.Index; Open = $open; Close = $call.Close; Args = $argText; IsDefinition = $isDef })
    }
    return $result
}

# file => helpers to repair (drop last arg from ORIGINAL call/definition)
$targets = @{
    'Fluence.Wpf.Tests/ControlTests.NavigationView.cs'   = @('AssertContentOffsetEventually')
    'Fluence.Wpf.Tests/ControlTests.ProgressRing.cs'     = @('AssertPathStroke', 'AssertDependencyPropertyNotAnimated')
    'Fluence.Wpf.Tests/ControlTests.BackgroundParity.cs' = @('AssertBrushColor', 'AssertBrushResolves')
    'Fluence.Wpf.Tests/ControlTests.IconForeground.cs'   = @('AssertIconMatchesText')
    'Fluence.Wpf.Tests/ControlTests.DemoParity.cs'       = @('AssertControlHasThemedBorder')
    'Fluence.Wpf.Tests/ControlTests.DemoSamplePolish.cs' = @('AssertBrushIsTransparent')
    'Fluence.Wpf.Tests/DemoMainWindowTests.cs'           = @('AssertIconBrush')
    'Fluence.Wpf.Tests/ThemeMarkupTests.cs'              = @('AssertProbeBrush')
    'Fluence.Wpf.Tests/ControlTests.ToggleButton.cs'     = @('GetSolidColor')
    'Fluence.Wpf.Tests/ControlTests.ToggleSplitButton.cs' = @('GetSolidColor')
    'Fluence.Wpf.Tests/ControlTests.SplitButton.cs'      = @('GetSolidColor')
    'Fluence.Wpf.Tests/ControlTests.ToggleSwitch.cs'     = @('GetSolidColor')
}

foreach ($rel in $targets.Keys) {
    $path = Join-Path $repo ($rel -replace '/', '\')
    if (-not (Test-Path $path)) { continue }
    $headText = & git -C $repo show ("HEAD:" + $rel) | Out-String
    $t = [System.IO.File]::ReadAllText($path)
    $changedFile = $false

    foreach ($helper in $targets[$rel]) {
        $headCalls = @(Get-Calls $headText $helper | Where-Object { -not $_.IsDefinition })
        if ($headCalls.Count -eq 0) { continue }

        # process current calls repeatedly from a fresh scan (offsets shift after edits)
        for ($pass = 0; $pass -lt 100; $pass++) {
            $curCalls = @(Get-Calls $t $helper | Where-Object { -not $_.IsDefinition })
            if ($curCalls.Count -ne $headCalls.Count) {
                Write-Host "WARN: $rel $helper count mismatch cur=$($curCalls.Count) head=$($headCalls.Count)"
                break
            }
            $edited = $false
            for ($k = $curCalls.Count - 1; $k -ge 0; $k--) {
                $head = $headCalls[$k]
                $cur = $curCalls[$k]
                # desired args: HEAD args minus the trailing message (assume last arg is message)
                $want = $head.Args[0..($head.Args.Count - 2)]
                $wantJoined = ($want -join ', ')
                $curJoined = ($cur.Args -join ', ')
                if ($curJoined -eq $wantJoined) { continue }
                $t = $t.Substring(0, $cur.Open + 1) + $wantJoined + $t.Substring($cur.Close)
                $edited = $true
                $changedFile = $true
                break  # rescan after each edit
            }
            if (-not $edited) { break }
        }
    }

    if ($changedFile) {
        [System.IO.File]::WriteAllText($path, $t, $utf8Bom)
        Write-Host "repaired: $rel"
    }
}
