$files = Get-ChildItem -Path 'Fluence.Wpf.Tests' -Filter '*.cs' -Recurse
foreach ($f in $files) {
    $lines = Get-Content -LiteralPath $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $m = [regex]::Match($lines[$i], '^\s*([A-Za-z_][\w.<>]*)\?\s+(\w+)\s*=\s*.+\s+as\s+\1\s*;\s*$')
        if (-not $m.Success) { continue }
        $v = $m.Groups[2].Value
        for ($j = $i + 1; $j -le [Math]::Min($i + 12, $lines.Count - 1); $j++) {
            if ($lines[$j] -match ('Assert\.NotNull\(' + [regex]::Escape($v) + '\b')) {
                Write-Host ($f.Name + ':' + ($i + 1) + ' var=' + $v + ' notnull@' + ($j + 1))
                break
            }
        }
    }
}
