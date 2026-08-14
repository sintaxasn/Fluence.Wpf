$ErrorActionPreference = 'Stop'
$dir = 'C:\Repos\Fluence.Wpf\Fluence.Wpf.Tests'
foreach ($n in 'DemoColorsPageTests.cs','DemoMainWindowTests.cs','FluenceWindowHardenTests.cs') {
  $p = Join-Path $dir $n
  $t = [System.IO.File]::ReadAllText($p)
  $t = [regex]::Replace($t, '(?s)\r?\n\s*private static string GetRepositoryFilePath\(params string\[\] relativeSegments\)\s*\{.*?\r?\n        \}', '')
  $t = [regex]::Replace($t, '(?s)\r?\n\s*private static string ReadRepositoryFile\(params string\[\] relativeSegments\)\s*\{.*?\r?\n        \}', '')
  $t = $t -replace '(?<![\w.])ReadRepositoryFile\(', 'await DemoTestHost.ReadRepositoryFileAsync(' -replace '(?<![\w.])GetRepositoryFilePath\(', 'DemoTestHost.GetRepositoryFilePath('
  [System.IO.File]::WriteAllText($p, $t)
}
$p = Join-Path $dir 'ControlTests.Button.cs'
$t = [System.IO.File]::ReadAllText($p)
$t = [regex]::Replace($t, '(?s)\r?\n\s*private static string GetRepositoryFilePath\(params string\[\] relativeSegments\)\s*\{.*?\r?\n        \}', '')
$t = $t -replace 'File\.ReadAllText\(GetRepositoryFilePath\(', 'await File.ReadAllTextAsync(DemoTestHost.GetRepositoryFilePath('
[System.IO.File]::WriteAllText($p, $t)
Write-Output 'done'
