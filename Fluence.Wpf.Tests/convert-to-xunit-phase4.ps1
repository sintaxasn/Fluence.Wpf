# Phase 4: analyzer cleanups from the comparison/IsType rewrites.
# Preserves UTF-8 BOM + LF. Delete after migration.
$root = $PSScriptRoot
$files = Get-ChildItem -Path $root -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
$utf8Bom = New-Object System.Text.UTF8Encoding($true)

# operand safe to unwrap: balanced parens, no top-level low-precedence operators
function Test-SimpleOperand([string]$s) {
    if ($s -match '\?\?|==|!=|&&|\|\||\s\?\s|\sis\s|\sas\s') { return $false }
    $depth = 0
    foreach ($c in $s.ToCharArray()) {
        if ($c -eq '(') { $depth++ }
        elseif ($c -eq ')') { $depth--; if ($depth -lt 0) { return $false } }
    }
    return $depth -eq 0
}

foreach ($f in $files) {
    $t = [System.IO.File]::ReadAllText($f.FullName)
    $orig = $t

    # Assert.True((a) op (b)[, msg]) -> strip redundant parens
    $t = [regex]::Replace($t, 'Assert\.True\(\((?<a>[^()]*(?:\([^()]*(?:\([^()]*\))?[^()]*\))*[^()]*)\) (?<op>[<>]=?) \((?<b>[^()]*(?:\([^()]*(?:\([^()]*\))?[^()]*\))*[^()]*)\)', {
        param($m)
        $a = $m.Groups['a'].Value
        $b = $m.Groups['b'].Value
        $op = $m.Groups['op'].Value
        $left = if (Test-SimpleOperand $a) { $a } else { "($a)" }
        $right = if (Test-SimpleOperand $b) { $b } else { "($b)" }
        "Assert.True($left $op $right"
    })

    # Assert.IsAssignableFrom(typeof(X), y) -> Assert.IsAssignableFrom<X>(y)
    $t = [regex]::Replace($t, 'Assert\.IsAssignableFrom\(typeof\((?<T>[^()]+)\),\s*(?<v>[^();]*(?:\([^()]*(?:\([^()]*\))?[^()]*\))*[^();]*)\)', {
        param($m)
        'Assert.IsAssignableFrom<' + $m.Groups['T'].Value + '>(' + $m.Groups['v'].Value + ')'
    })

    if ($t -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $t, $utf8Bom)
        Write-Host "fixed: $($f.Name)"
    }
}
