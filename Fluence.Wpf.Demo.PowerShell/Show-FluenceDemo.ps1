param(
    [switch]$SmokeTest
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Write-Log {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $timestamp = Get-Date -Format 'HH:mm:ss.fff'
    Write-Host "[$timestamp] $Message"
}

$windowsPowerShell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$isWindowsPowerShell51 = $PSVersionTable.PSEdition -eq 'Desktop' -and $PSVersionTable.PSVersion.Major -eq 5
$isSta = [System.Threading.Thread]::CurrentThread.GetApartmentState() -eq [System.Threading.ApartmentState]::STA

if (-not $isWindowsPowerShell51 -or -not $isSta) {
    if (-not $PSCommandPath) {
        throw 'Run this script from a file with Windows PowerShell 5.1 using: powershell.exe -STA -ExecutionPolicy Bypass -File .\Show-FluenceDemo.ps1'
    }

    if (-not (Test-Path -LiteralPath $windowsPowerShell)) {
        throw "Windows PowerShell 5.1 was not found at '$windowsPowerShell'."
    }

    Write-Log 'Relaunching with Windows PowerShell 5.1 in STA mode.'
    $arguments = @('-NoProfile', '-STA', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath)
    if ($SmokeTest) {
        $arguments += '-SmokeTest'
    }

    & $windowsPowerShell @arguments
    exit $LASTEXITCODE
}

Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName System.Xaml

$scriptRoot = Split-Path -Parent $PSCommandPath
$fluenceAssemblyPath = Join-Path $scriptRoot 'Fluence.Wpf.dll'
$xamlPath = Join-Path $scriptRoot 'MainWindow.xaml'

if (-not (Test-Path -LiteralPath $fluenceAssemblyPath)) {
    throw "Missing Fluence.Wpf assembly: '$fluenceAssemblyPath'."
}

if (-not (Test-Path -LiteralPath $xamlPath)) {
    throw "Missing XAML file: '$xamlPath'."
}

$script:fluenceAssemblyPath = $fluenceAssemblyPath
$resolveFluenceAssembly = [System.ResolveEventHandler]{
    param($sender, $eventArgs)

    $requestedName = New-Object System.Reflection.AssemblyName($eventArgs.Name)
    if ($requestedName.Name -eq 'Fluence.Wpf') {
        return [System.Reflection.Assembly]::LoadFrom($script:fluenceAssemblyPath)
    }

    return $null
}

[System.AppDomain]::CurrentDomain.add_AssemblyResolve($resolveFluenceAssembly)

Write-Log "Loading Fluence.Wpf from '$fluenceAssemblyPath'."
$fluenceAssembly = [System.Reflection.Assembly]::LoadFrom($fluenceAssemblyPath)
Write-Log "Loaded $($fluenceAssembly.FullName)."

$application = [System.Windows.Application]::Current
if ($null -eq $application) {
    $application = New-Object System.Windows.Application
    Write-Log 'Created WPF Application instance.'
}

$application.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown

Write-Log 'Applying Fluence.Wpf Auto theme with Mica backdrop.'
[Fluence.Wpf.ApplicationThemeManager]::Apply(
    [Fluence.Wpf.ApplicationTheme]::Auto,
    [Fluence.Wpf.BackdropType]::Mica,
    $true)

$themeChangedHandler = [System.EventHandler[Fluence.Wpf.ThemeChangedEventArgs]]{
    param($sender, $eventArgs)

    Write-Log ("Theme changed: {0}, accent #{1:X2}{2:X2}{3:X2}" -f `
        $eventArgs.Theme, `
        $eventArgs.AccentColor.R, `
        $eventArgs.AccentColor.G, `
        $eventArgs.AccentColor.B)
}

$accentChangedHandler = [System.EventHandler[System.EventArgs]]{
    param($sender, $eventArgs)

    $accent = [Fluence.Wpf.ApplicationAccentColorManager]::SystemAccentColor
    Write-Log ("Accent changed: #{0:X2}{1:X2}{2:X2}" -f $accent.R, $accent.G, $accent.B)
}

[Fluence.Wpf.ApplicationThemeManager]::add_Changed($themeChangedHandler)
[Fluence.Wpf.ApplicationAccentColorManager]::add_AccentColorChanged($accentChangedHandler)

Write-Log "Merged Fluence resource dictionaries: $($application.Resources.MergedDictionaries.Count)."
Write-Log "Loading XAML from '$xamlPath'."

$xmlReaderSettings = New-Object System.Xml.XmlReaderSettings
$xmlReaderSettings.IgnoreComments = $true
$xmlReaderSettings.IgnoreWhitespace = $false

$xmlReader = [System.Xml.XmlReader]::Create($xamlPath, $xmlReaderSettings)
try {
    $window = [System.Windows.Markup.XamlReader]::Load($xmlReader)
}
finally {
    $xmlReader.Close()
}

if (-not ($window -is [System.Windows.Window])) {
    throw "XAML root must load as a System.Windows.Window. Actual type: '$($window.GetType().FullName)'."
}

$statusInfoBar = $window.FindName('StatusInfoBar')
$themeComboBox = $window.FindName('ThemeComboBox')
$accentButton = $window.FindName('AccentButton')
$systemAccentButton = $window.FindName('SystemAccentButton')
$primaryActionButton = $window.FindName('PrimaryActionButton')

$accentColors = @(
    [System.Windows.Media.Color]::FromRgb(0x00, 0x78, 0xD4),
    [System.Windows.Media.Color]::FromRgb(0x10, 0x7C, 0x10),
    [System.Windows.Media.Color]::FromRgb(0xC2, 0x39, 0xB3),
    [System.Windows.Media.Color]::FromRgb(0xD8, 0x3B, 0x01)
)
$script:accentIndex = 0

if ($null -ne $themeComboBox) {
    $themeComboBox.add_SelectionChanged({
        if ($null -eq $themeComboBox.SelectedItem) {
            return
        }

        $themeName = [string]$themeComboBox.SelectedItem.Tag
        $theme = [System.Enum]::Parse([Fluence.Wpf.ApplicationTheme], $themeName)
        [Fluence.Wpf.ApplicationThemeManager]::Apply($theme, [Fluence.Wpf.BackdropType]::Mica, $true)
        Write-Log "Requested theme: $themeName."
    })
}

if ($null -ne $accentButton) {
    $accentButton.add_Click({
        $script:accentIndex = ($script:accentIndex + 1) % $accentColors.Count
        $color = $accentColors[$script:accentIndex]
        [Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent($color)
    })
}

if ($null -ne $systemAccentButton) {
    $systemAccentButton.add_Click({
        [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()
    })
}

if ($null -ne $primaryActionButton) {
    $primaryActionButton.add_Click({
        Write-Log 'Primary action clicked.'
        if ($null -ne $statusInfoBar) {
            $statusInfoBar.Title = 'Action received'
            $statusInfoBar.Message = "PowerShell handled the click at $(Get-Date -Format 'T')."
            $statusInfoBar.IsOpen = $true
        }
    })
}

[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
Write-Log 'System theme watcher enabled.'

$window.add_SourceInitialized({
    Write-Log 'Window source initialized; Win32 theme hook is active.'
})

$window.add_Closed({
    Write-Log 'Window closed. Disabling watcher and stopping dispatcher.'
    [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window)
    [Fluence.Wpf.ApplicationThemeManager]::remove_Changed($themeChangedHandler)
    [Fluence.Wpf.ApplicationAccentColorManager]::remove_AccentColorChanged($accentChangedHandler)
    [System.Windows.Threading.Dispatcher]::CurrentDispatcher.InvokeShutdown()
})

if ($SmokeTest) {
    Write-Log "Smoke test loaded $($window.GetType().FullName) successfully."
    [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window)
    [Fluence.Wpf.ApplicationThemeManager]::remove_Changed($themeChangedHandler)
    [Fluence.Wpf.ApplicationAccentColorManager]::remove_AccentColorChanged($accentChangedHandler)
    exit 0
}

Write-Log 'Showing window with Show(); dispatcher remains active for WPF events.'
$null = $window.Show()
[System.Windows.Threading.Dispatcher]::Run()
Write-Log 'Dispatcher stopped.'
