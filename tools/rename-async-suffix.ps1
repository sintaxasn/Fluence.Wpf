# Renames async Task methods to have an Async suffix and updates call sites.
$ErrorActionPreference = 'Stop'
$root = 'C:\Repos\Fluence.Wpf\Fluence.Wpf.Tests'
$files = Get-ChildItem $root -Filter *.cs -Recurse | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }

# 1. Collect names of async Task methods lacking the suffix
$names = [System.Collections.Generic.HashSet[string]]::new()
foreach ($f in $files) {
    $t = [System.IO.File]::ReadAllText($f.FullName)
    foreach ($m in [regex]::Matches($t, '(?m)^\s*(public|private|internal|protected)[\w\s]*\basync\s+(Task|ValueTask)(<[^>]+>)?\s+(\w+)\s*[\(<]')) {
        $n = $m.Groups[4].Value
        if (-not $n.EndsWith('Async') -and $n -ne 'InitializeAsync' -and $n -ne 'DisposeAsync') { $null = $names.Add($n) }
    }
}
Write-Output "Methods to rename: $($names.Count)"

# 2. Rename declarations and call sites (word-boundary followed by ( or <)
foreach ($f in $files) {
    $t = [System.IO.File]::ReadAllText($f.FullName)
    $orig = $t
    foreach ($n in $names) {
        $t = [regex]::Replace($t, "\b$n\b(?=\s*[\(<])", "${n}Async")
        # nameof references
        $t = [regex]::Replace($t, "nameof\($n\)", "nameof(${n}Async)")
    }
    if ($t -ne $orig) { [System.IO.File]::WriteAllText($f.FullName, $t) }
}
Write-Output 'Rename complete'
