# Handles MULTI-LINE declarations:
#   T? name =
#       expr as T;
#   ...
#   Assert.NotNull(name);
$baseTypes = @(
    'FrameworkElement', 'UIElement', 'Selector', 'RangeBase', 'ButtonBase',
    'Control', 'DependencyObject', 'Visual', 'Brush', 'Transform',
    'ContentControl', 'ItemsControl', 'Panel', 'TextBoxBase', 'ToggleButton',
    'MenuBase', 'HeaderedContentControl', 'HeaderedItemsControl', 'Freezable',
    'AutomationPeer', 'ImageSource', 'Geometry', 'Effect'
)

$files = Get-ChildItem -Path 'Fluence.Wpf.Tests' -Filter '*.cs' -Recurse
$total = 0

foreach ($file in $files) {
    $lines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $file.FullName)
    $changed = $false

    for ($i = 0; $i -lt $lines.Count - 1; $i++) {
        # Line 1: `T? name =` (nothing after =)
        $m1 = [regex]::Match($lines[$i], '^(\s*)([A-Za-z_][\w.<>]*)\?\s+(\w+)\s*=\s*$')
        if (-not $m1.Success) { continue }

        # Line 2 (possibly more): collect until line ending with `as T;`
        $exprLines = @()
        $endIdx = -1
        for ($k = $i + 1; $k -le [Math]::Min($i + 3, $lines.Count - 1); $k++) {
            $exprLines += $lines[$k].Trim()
            if ($lines[$k] -match '\s+as\s+([\w.<>]+)\s*;\s*$') { $endIdx = $k; break }
        }
        if ($endIdx -lt 0) { continue }

        $joined = ($exprLines -join ' ')
        $m2 = [regex]::Match($joined, '^(.*)\s+as\s+([\w.<>]+)\s*;$')
        if (-not $m2.Success) { continue }

        $declType = $m1.Groups[2].Value
        $varName  = $m1.Groups[3].Value
        $indent   = $m1.Groups[1].Value
        $expr     = $m2.Groups[1].Value.Trim()
        $castType = $m2.Groups[2].Value
        if ($declType -ne $castType) { continue }

        # Find Assert.NotNull(varName) within next 12 lines after endIdx
        $notNullIdx = -1
        for ($j = $endIdx + 1; $j -le [Math]::Min($endIdx + 12, $lines.Count - 1); $j++) {
            if ($lines[$j] -match "^\s*(_\s*=\s*)?Assert\.NotNull\($([regex]::Escape($varName))(,.*)?\);\s*$") {
                $notNullIdx = $j
                break
            }
        }
        if ($notNullIdx -lt 0) { continue }

        $shortType = ($castType -split '\.')[-1]
        $isBase = ($baseTypes -contains $shortType) -or ($shortType -cmatch '^I[A-Z]')
        $assert = if ($isBase) { 'Assert.IsAssignableFrom' } else { 'Assert.IsType' }

        # Rewrite: keep the wrapped shape (decl line + indented expr line)
        $exprIndent = ([regex]::Match($lines[$i + 1], '^\s*')).Value
        $lines[$i] = "$indent$castType $varName ="
        # Remove old expr lines, insert single new expr line
        for ($r = $endIdx; $r -gt $i; $r--) { $lines.RemoveAt($r) }
        $lines.Insert($i + 1, "$exprIndent$assert<$castType>($expr);")
        # NotNull index shifted by removed lines
        $shift = ($endIdx - $i) - 1
        $lines.RemoveAt($notNullIdx - $shift)
        $changed = $true
        $total++
    }

    if ($changed) {
        $utf8Bom = New-Object System.Text.UTF8Encoding($true)
        [System.IO.File]::WriteAllText($file.FullName, (($lines -join "`n") + "`n"), $utf8Bom)
        Write-Host "$($file.Name): updated"
    }
}
Write-Host "Multi-line pairs converted: $total"
