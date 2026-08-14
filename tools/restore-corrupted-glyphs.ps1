# Restores '?'-corrupted non-ASCII characters by line-matching against a clean git commit.
$ErrorActionPreference = 'Stop'
$commit = 'c0179fa2'
cd C:\Repos\Fluence.Wpf
$files = @(
  'Fluence.Wpf.Tests/ControlTests.InfoBadge.cs',
  'Fluence.Wpf.Tests/ControlTests.ProgressRing.cs',
  'Fluence.Wpf.Tests/ControlTests.TextBox.cs',
  'Fluence.Wpf.Tests/ControlTests.TreeView.cs',
  'Fluence.Wpf.Tests/FluenceWindowHardenTests.cs',
  'Fluence.Wpf.Tests/FluenceWindowTitleBarTests.cs'
)

foreach ($f in $files) {
    $oldLines = (git show "${commit}:$f") -split "`n" | ForEach-Object { $_.TrimEnd("`r") }
    # index old lines by masked form (non-ASCII -> '?')
    $map = @{}
    foreach ($ol in $oldLines) {
        if ($ol -match '[^\x00-\x7F]') {
            $mask = [regex]::Replace($ol, '[^\x00-\x7F]', '?')
            if (-not $map.ContainsKey($mask)) { $map[$mask] = $ol }
        }
    }
    $newLines = [System.IO.File]::ReadAllLines($f)
    $fixed = 0; $unresolved = @()
    for ($i = 0; $i -lt $newLines.Count; $i++) {
        $nl = $newLines[$i]
        if ($nl -notmatch '\?') { continue }
        if ($map.ContainsKey($nl)) { $newLines[$i] = $map[$nl]; $fixed++ }
    }
    [System.IO.File]::WriteAllLines($f, $newLines, (New-Object System.Text.UTF8Encoding($true)))
    # report remaining lines with '?' that likely were corrupted (heuristic: '?' inside string literal adjacent to quote)
    $left = (Select-String -Path $f -Pattern '"\?|\?"' | Measure-Object).Count
    "${f}: fixed=$fixed remainingSuspicious=$left"
}
