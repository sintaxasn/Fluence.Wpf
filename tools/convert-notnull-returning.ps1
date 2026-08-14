# Converts `T? name = expr;` + `Assert.NotNull(name);` (where expr is NOT an as-cast)
# into `T name = Assert.NotNull(expr);` — xunit.v3 Assert.NotNull returns the value.
# Handles single-line declarations and two-line wrapped declarations.
$files = Get-ChildItem -Path 'Fluence.Wpf.Tests' -Filter '*.cs' -Recurse
$total = 0

foreach ($file in $files) {
    $lines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $file.FullName)
    $changed = $false

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $indent = ''; $declType = ''; $varName = ''; $expr = ''; $endIdx = $i; $wrapped = $false

        $m = [regex]::Match($lines[$i], '^(\s*)([A-Za-z_][\w.<>]*)\?\s+(\w+)\s*=\s*(.+);\s*$')
        if ($m.Success) {
            $indent = $m.Groups[1].Value; $declType = $m.Groups[2].Value
            $varName = $m.Groups[3].Value; $expr = $m.Groups[4].Value.Trim()
        }
        else {
            $m1 = [regex]::Match($lines[$i], '^(\s*)([A-Za-z_][\w.<>]*)\?\s+(\w+)\s*=\s*$')
            if (-not $m1.Success) { continue }
            if ($i + 1 -ge $lines.Count) { continue }
            $m2 = [regex]::Match($lines[$i + 1], '^\s*(.+);\s*$')
            if (-not $m2.Success) { continue }
            $indent = $m1.Groups[1].Value; $declType = $m1.Groups[2].Value
            $varName = $m1.Groups[3].Value; $expr = $m2.Groups[1].Value.Trim()
            $endIdx = $i + 1; $wrapped = $true
        }

        # skip as-casts (handled already), ternaries, null-coalescing, awaits
        if ($expr -match '\bas\s+[\w.<>]+$') { continue }
        if ($expr -match '\?\?|\?\s.*:') { continue }

        # find Assert.NotNull(varName) within 12 lines
        $notNullIdx = -1
        for ($j = $endIdx + 1; $j -le [Math]::Min($endIdx + 12, $lines.Count - 1); $j++) {
            if ($lines[$j] -match "^\s*(_\s*=\s*)?Assert\.NotNull\($([regex]::Escape($varName))(,.*)?\);\s*$") {
                $notNullIdx = $j; break
            }
        }
        if ($notNullIdx -lt 0) { continue }

        if ($wrapped) {
            $exprIndent = ([regex]::Match($lines[$i + 1], '^\s*')).Value
            $lines[$i] = "$indent$declType $varName ="
            $lines[$i + 1] = "$($exprIndent)Assert.NotNull($expr);"
        }
        else {
            $lines[$i] = "$indent$declType $varName = Assert.NotNull($expr);"
        }
        $lines.RemoveAt($notNullIdx)
        $changed = $true
        $total++
    }

    if ($changed) {
        $utf8Bom = New-Object System.Text.UTF8Encoding($true)
        [System.IO.File]::WriteAllText($file.FullName, (($lines -join "`n") + "`n"), $utf8Bom)
        Write-Host "$($file.Name): updated"
    }
}
Write-Host "NotNull-returning pairs converted: $total"
