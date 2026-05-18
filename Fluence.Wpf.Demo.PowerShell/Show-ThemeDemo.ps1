# Run with: powershell.exe -STA -File .\Show-ThemeDemo.ps1

param(
    [switch]$SmokeTest,
    [switch]$RunInProcess
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$windowsPowerShell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$isWindowsPowerShell51 = $PSVersionTable.PSEdition -eq 'Desktop' -and $PSVersionTable.PSVersion.Major -eq 5
$isSta = [System.Threading.Thread]::CurrentThread.GetApartmentState() -eq [System.Threading.ApartmentState]::STA

## If not running in-process, re-launch this script under Windows PowerShell 5.1 in STA mode.
if (-not $RunInProcess) {
    if (-not $PSCommandPath) {  throw 'Run this script from a file with Windows PowerShell 5.1 using: powershell.exe -STA -ExecutionPolicy Bypass -File .\Show-ThemeDemo.ps1' }
    if (-not (Test-Path -LiteralPath $windowsPowerShell)) { throw "Windows PowerShell 5.1 was not found at '$windowsPowerShell'." }
    $arguments = @('-NoProfile', '-STA', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-RunInProcess')
    if ($SmokeTest) {  $arguments += '-SmokeTest' }
    & $windowsPowerShell @arguments
    exit $LASTEXITCODE
}
if (-not $isWindowsPowerShell51 -or -not $isSta) { throw 'The in-process WPF host must run under Windows PowerShell 5.1 in STA mode.' }

## If the Fluence.Wpf library is not built, build it first.
$dllPath = Join-Path $PSScriptRoot '..\Fluence.Wpf\bin\Release\net472\Fluence.Wpf.dll'
if (-not (Test-Path -LiteralPath $dllPath)) {
    Write-Host 'Building Fluence.Wpf library ...' -ForegroundColor Cyan
    $projPath = Join-Path $PSScriptRoot '..\Fluence.Wpf\Fluence.Wpf.csproj'
    dotnet build $projPath -c Release -f net472 --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
}

## Load necessary assemblies and the Fluence.Wpf library.
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName System.Xaml
Add-Type -Path $dllPath

## Set up WPF application...
$app = New-Object System.Windows.Application
$app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown

## Set up the main window, applying the Fluence theme and system backdrop.
[Fluence.Wpf.ApplicationThemeManager]::Apply(
    [Fluence.Wpf.ApplicationTheme]::Auto,
    [Fluence.Wpf.BackdropType]::Mica,
    $true)

## Define the XAML for the main window
$xaml = @'
<ui:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"
    Title="Theme Demo - Fluence.Wpf"
    Width="560" Height="440"
    MinWidth="480" MinHeight="380"
    SystemBackdropType="Mica"
    CornerStyle="Round">
    <StackPanel Margin="32" VerticalAlignment="Center">
        <TextBlock
            ui:TextBlockExtensions.Typography="Title"
            Foreground="{DynamicResource TextFillColorPrimaryBrush}"
            Text="Fluence.Wpf from PowerShell"
            Margin="0,0,0,8" />
        <TextBlock
            ui:TextBlockExtensions.Typography="Body"
            Foreground="{DynamicResource TextFillColorSecondaryBrush}"
            Text="Runtime XAML · Mica backdrop · Live theme switching"
            Margin="0,0,0,32" />

        <ui:Card Padding="16" Margin="0,0,0,16" Variant="Default">
            <StackPanel>
                <TextBlock
                    ui:TextBlockExtensions.Typography="Subtitle"
                    Foreground="{DynamicResource TextFillColorPrimaryBrush}"
                    Text="Theme" Margin="0,0,0,12" />
                <StackPanel Orientation="Horizontal">
                    <ui:Button x:Name="LightBtn" Content="Light" Margin="0,0,8,0" />
                    <ui:Button x:Name="DarkBtn"  Content="Dark"  Margin="0,0,8,0" />
                    <ui:Button x:Name="AutoBtn"  Content="Auto"  Appearance="Accent" />
                </StackPanel>
            </StackPanel>
        </ui:Card>

        <ui:Card Padding="16" Variant="Default">
            <StackPanel>
                <TextBlock
                    ui:TextBlockExtensions.Typography="Subtitle"
                    Foreground="{DynamicResource TextFillColorPrimaryBrush}"
                    Text="Accent" Margin="0,0,0,12" />
                <StackPanel Orientation="Horizontal">
                    <ui:Button x:Name="CycleBtn"  Content="Cycle accent"  Appearance="Accent" Margin="0,0,8,0" />
                    <ui:Button x:Name="SystemBtn" Content="System accent" />
                </StackPanel>
            </StackPanel>
        </ui:Card>
    </StackPanel>
</ui:FluenceWindow>
'@

## Load the XAML and create the main window
$window = [System.Windows.Markup.XamlReader]::Parse($xaml)

## Load the app icon from the assets folder, if it exists, and set it as the window icon.
$iconPath = Join-Path $PSScriptRoot '..\assets\fluence-wpf-appicon-256.ico'
if (Test-Path -LiteralPath $iconPath) {
    $stream = [System.IO.File]::OpenRead($iconPath)
    $decoder = New-Object System.Windows.Media.Imaging.IconBitmapDecoder -ArgumentList `
        $stream, `
        ([System.Windows.Media.Imaging.BitmapCreateOptions]::None), `
        ([System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
    $window.Icon = $decoder.Frames[0]
    $stream.Dispose()
}

$accentColors = @(
    [System.Windows.Media.Color]::FromRgb(0x00, 0x78, 0xD4),
    [System.Windows.Media.Color]::FromRgb(0x10, 0x7C, 0x10),
    [System.Windows.Media.Color]::FromRgb(0xC2, 0x39, 0xB3),
    [System.Windows.Media.Color]::FromRgb(0xD8, 0x3B, 0x01)
)
$script:accentIndex = 0

$window.FindName('LightBtn').add_Click({
    [Fluence.Wpf.ApplicationThemeManager]::Apply(
        [Fluence.Wpf.ApplicationTheme]::Light, [Fluence.Wpf.BackdropType]::Mica, $true)
})
$window.FindName('DarkBtn').add_Click({
    [Fluence.Wpf.ApplicationThemeManager]::Apply(
        [Fluence.Wpf.ApplicationTheme]::Dark, [Fluence.Wpf.BackdropType]::Mica, $true)
})
$window.FindName('AutoBtn').add_Click({
    [Fluence.Wpf.ApplicationThemeManager]::Apply(
        [Fluence.Wpf.ApplicationTheme]::Auto, [Fluence.Wpf.BackdropType]::Mica, $true)
})
$window.FindName('CycleBtn').add_Click({
    $script:accentIndex = ($script:accentIndex + 1) % $accentColors.Count
    [Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent($accentColors[$script:accentIndex])
})
$window.FindName('SystemBtn').add_Click({
    [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()
})

## Set up the system theme watcher to automatically update the window's theme and backdrop when the system theme changes.
[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({
    [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window)
    [System.Windows.Threading.Dispatcher]::CurrentDispatcher.InvokeShutdown()
})

## Display the window and start the WPF message loop.
$null = $window.Show()
[System.Windows.Threading.Dispatcher]::Run()
