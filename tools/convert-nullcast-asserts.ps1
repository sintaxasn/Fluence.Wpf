# Transforms `T? x = expr as T;` + `Assert.NotNull(x);` pairs into
# `T x = Assert.IsType<T>(expr);` (or IsAssignableFrom for base/abstract/interface targets).
$baseTypes = @(
    'FrameworkElement', 'UIElement', 'Selector', 'RangeBase', 'ButtonBase',
    'Control', 'DependencyObject', 'Visual', 'Brush', 'Transform',
    'ContentControl', 'ItemsControl', 'Panel', 'TextBoxBase', 'ToggleButton',
    'MenuBase', 'HeaderedContentControl', 'HeaderedItemsControl', 'Freezable',
    'AutomationPeer', 'ImageSource', 'Geometry', 'Effect'
)

$files = Get-ChildItem -Path 'Fluence.Wpf.Tests' -Filter '*.cs' -Recurse
$totalPairs = 0

foreach ($file in $files) {
    $lines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $file.FullName)
    $changed = $false

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $m = [regex]::Match($lines[$i], '^(\s*)([A-Za-z_][A-Za-z0-9_.<>]*)\?\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.+)\s+as\s+([A-Za-z_][A-Za-z0-9_.<>]*)\s*;\s*$')
        if (-not $m.Success) { continue }

        $declType = $m.Groups[2].Value
        $varName  = $m.Groups[3].Value
        $expr     = $m.Groups[4].Value
        $castType = $m.Groups[5].Value
        if ($declType -ne $castType) { continue }

        # Find Assert.NotNull(varName) within next 4 lines
        $notNullIdx = -1
        for ($j = $i + 1; $j -le [Math]::Min($i + 4, $lines.Count - 1); $j++) {
            if ($lines[$j] -match "^\s*(_\s*=\s*)?Assert\.NotNull\($([regex]::Escape($varName))(,.*)?\);\s*$") {
                $notNullIdx = $j
                break
            }
            # stop scan if variable reassigned or block ends
            if ($lines[$j] -match '^\s*}\s*$') { break }
        }
        if ($notNullIdx -lt 0) { continue }

        $shortType = ($castType -split '\.')[-1]
        $isBase = ($baseTypes -contains $shortType) -or ($shortType -cmatch '^I[A-Z]')
        $assert = if ($isBase) { 'Assert.IsAssignableFrom' } else { 'Assert.IsType' }

        # strip trailing null-forgiving/paren noise from expr; expr may end with `!`
        $lines[$i] = "$($m.Groups[1].Value)$castType $varName = $assert<$castType>($expr);"
        $lines.RemoveAt($notNullIdx)
        $changed = $true
        $totalPairs++
    }

    if ($changed) {
        $text = ($lines -join "`n") + "`n"
        $utf8Bom = New-Object System.Text.UTF8Encoding($true)
        [System.IO.File]::WriteAllText($file.FullName, $text, $utf8Bom)
        Write-Host "$($file.Name): updated"
    }
}
Write-Host "Total pairs converted: $totalPairs"
