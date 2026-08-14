$ErrorActionPreference = 'Stop'
$root = 'C:\Repos\Fluence.Wpf\Fluence.Wpf.Tests'
$total = 0

foreach ($f in Get-ChildItem $root -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }) {
    $lines = [System.Collections.Generic.List[string]][System.IO.File]::ReadAllLines($f.FullName)
    $changed = $false

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $m = [regex]::Match($lines[$i], '^(?<indent>\s+)(?<mods>(?:public|private|internal|protected)(?: static)?) async (?<ret>Task|ValueTask)(?![<\w]) (?<rest>\w+\([^)]*\))\s*$')
        if (-not $m.Success) { continue }
        # find opening brace line
        $ob = $i + 1
        if ($ob -ge $lines.Count -or $lines[$ob].Trim() -ne '{') { continue }
        # first statement line must start the single awaited call (no assignment/result use)
        $s = $ob + 1
        if ($s -ge $lines.Count) { continue }
        $stmt = $lines[$s].TrimStart()
        if ($stmt -notmatch '^await (?<callee>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\(') { continue }
        # find statement end: line ending with ').ConfigureAwait(true);' or ');' tracking brace depth via paren counting
        $depth = 0; $end = -1
        for ($j = $s; $j -lt $lines.Count; $j++) {
            $depth += ([regex]::Matches($lines[$j], '\(')).Count - ([regex]::Matches($lines[$j], '\)')).Count
            if ($depth -le 0 -and $lines[$j] -match ';\s*$') { $end = $j; break }
            if ($lines[$j].Trim() -eq '}' -and $depth -lt 0) { break }
        }
        if ($end -lt 0) { continue }
        # closing method brace must immediately follow
        $cb = $end + 1
        if ($cb -ge $lines.Count -or $lines[$cb].Trim() -ne '}') { continue }
        # method body is exactly this one statement -> elide
        $ret = $m.Groups['ret'].Value
        # strip 'await ' prefix and trailing '.ConfigureAwait(x)'
        $firstLine = $lines[$s] -replace '(?<=^\s*)await ', ''
        $lastLine = $lines[$end] -replace '\.ConfigureAwait\((?:true|false)\);\s*$', ';'
        if ($ret -eq 'ValueTask') {
            if ($s -eq $end) { $firstLine = $lastLine -replace '(?<=^\s*)await ', 'return new ValueTask(' -replace ';\s*$', ');' }
            else { $firstLine = $firstLine -replace '(?<=^\s*)await ', 'return new ValueTask('; $lastLine = $lastLine -replace ';\s*$', ');' }
        }
        else {
            $firstLine = $firstLine -replace '(?<=^\s*)await ', 'return await '  # placeholder, fixed below
            $firstLine = $firstLine -replace 'return await ', 'return '
            if ($s -eq $end) { $firstLine = $firstLine -replace '\.ConfigureAwait\((?:true|false)\);\s*$', ';' }
        }
        $lines[$i] = $lines[$i] -replace ' async (Task|ValueTask) ', ' $1 '
        $lines[$s] = $firstLine
        if ($s -ne $end) { $lines[$end] = $lastLine }
        $changed = $true
        $total++
    }

    if ($changed) {
        [System.IO.File]::WriteAllLines($f.FullName, $lines, (New-Object System.Text.UTF8Encoding($true)))
        Write-Host "$($f.Name)"
    }
}
Write-Host "total elided: $total"
