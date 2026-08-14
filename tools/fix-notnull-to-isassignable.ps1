# Fix bad `T name = Assert.NotNull(expr);` (void-returning for ref types)
# to `T name = Assert.IsAssignableFrom<T>(expr);`
# Handles single-line and wrapped (decl line + expr line) forms.
$files = Get-ChildItem -Path 'Fluence.Wpf.Tests' -Filter '*.cs' -Recurse
$total = 0

foreach ($file in $files) {
    $lines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $file.FullName)
    $changed = $false

    for ($i = 0; $i -lt $lines.Count; $i++) {
        # single-line
        $m = [regex]::Match($lines[$i], '^(\s*)([A-Za-z_][\w.<>]*)\s+(\w+)\s*=\s*Assert\.NotNull\((.+)\);\s*$')
        if ($m.Success) {
            $lines[$i] = "$($m.Groups[1].Value)$($m.Groups[2].Value) $($m.Groups[3].Value) = Assert.IsAssignableFrom<$($m.Groups[2].Value)>($($m.Groups[4].Value));"
            $changed = $true; $total++
            continue
        }
        # wrapped: `T name =` then `Assert.NotNull(expr);`
        $m1 = [regex]::Match($lines[$i], '^(\s*)([A-Za-z_][\w.<>]*)\s+(\w+)\s*=\s*$')
        if ($m1.Success -and $i + 1 -lt $lines.Count) {
            $m2 = [regex]::Match($lines[$i + 1], '^(\s*)Assert\.NotNull\((.+)\);\s*$')
            if ($m2.Success) {
                $lines[$i + 1] = "$($m2.Groups[1].Value)Assert.IsAssignableFrom<$($m1.Groups[2].Value)>($($m2.Groups[2].Value));"
                $changed = $true; $total++
            }
        }
    }

    if ($changed) {
        $utf8Bom = New-Object System.Text.UTF8Encoding($true)
        [System.IO.File]::WriteAllText($file.FullName, (($lines -join "`n") + "`n"), $utf8Bom)
    }
}
Write-Host "Fixed: $total"
