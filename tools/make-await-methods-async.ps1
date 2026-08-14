# Converts methods containing 'await ' into async Task signatures.
$ErrorActionPreference = 'Stop'
$files = Get-ChildItem C:\Repos\Fluence.Wpf\Fluence.Wpf.Tests -Filter *.cs -Recurse |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }

$sigRegex = '^(?<indent>\s*)(?<mods>(public|private|internal|protected)(\s+static)?)\s+(?<ret>void|Task)\s+(?<name>\w+)\s*\((?<args>[^)]*)\)\s*$'
$totalChanged = 0

foreach ($file in $files) {
    $lines = [System.Collections.Generic.List[string]](Get-Content $file.FullName)
    $changed = $false

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $m = [regex]::Match($lines[$i], $sigRegex)
        if (-not $m.Success) { continue }
        if ($lines[$i] -match '\basync\b') { continue }

        # find method body extent via brace depth starting from the '{' after signature
        $depth = 0; $started = $false; $end = -1
        for ($j = $i; $j -lt $lines.Count; $j++) {
            foreach ($ch in $lines[$j].ToCharArray()) {
                if ($ch -eq '{') { $depth++; $started = $true }
                elseif ($ch -eq '}') { $depth-- }
            }
            if ($started -and $depth -le 0) { $end = $j; break }
        }
        if ($end -lt 0) { continue }

        $body = ($lines[($i+1)..$end] -join "`n")
        if ($body -notmatch '\bawait\b') { continue }

        $mods = $m.Groups['mods'].Value
        $indent = $m.Groups['indent'].Value
        $name = $m.Groups['name'].Value
        $args2 = $m.Groups['args'].Value
        $lines[$i] = "$indent$mods async Task $name($args2)"
        $changed = $true
    }

    if ($changed) {
        [System.IO.File]::WriteAllText($file.FullName, ($lines -join "`r`n"))
        $totalChanged++
        Write-Output $file.Name
    }
}
Write-Output "Files changed: $totalChanged"
