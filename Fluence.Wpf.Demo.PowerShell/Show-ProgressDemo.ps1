# Run with: powershell.exe -STA -File .\Show-ProgressDemo.ps1

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
    if (-not $PSCommandPath) {  throw 'Run this script from a file with Windows PowerShell 5.1 using: powershell.exe -STA -ExecutionPolicy Bypass -File .\Show-ProgressDemo.ps1' }
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
    Title="Progress Demo - Fluence.Wpf"
    Width="560" Height="500"
    MinWidth="480" MinHeight="420"
    SystemBackdropType="Mica"
    CornerStyle="Round">
    <StackPanel Margin="32" VerticalAlignment="Center">
        <TextBlock
            ui:TextBlockExtensions.Typography="Title"
            Foreground="{DynamicResource TextFillColorPrimaryBrush}"
            Text="Progress &amp; Status" Margin="0,0,0,24" />

        <ui:Card Padding="16" Margin="0,0,0,16" Variant="Default">
            <StackPanel>
                <TextBlock
                    ui:TextBlockExtensions.Typography="Subtitle"
                    Foreground="{DynamicResource TextFillColorPrimaryBrush}"
                    Text="Progress bar" Margin="0,0,0,12" />
                <ui:ProgressBar ProgressMode="Indeterminate" />
            </StackPanel>
        </ui:Card>

        <ui:Card Padding="16" Margin="0,0,0,16" Variant="Default">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto" />
                    <ColumnDefinition Width="16" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>
                <ui:ProgressRing Grid.Column="0"
                    Width="40" Height="40" IsIndeterminate="True" />
                <StackPanel Grid.Column="2" VerticalAlignment="Center">
                    <TextBlock
                        ui:TextBlockExtensions.Typography="Caption"
                        Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                        Text="Progress ring" />
                    <TextBlock
                        ui:TextBlockExtensions.Typography="Body"
                        Foreground="{DynamicResource TextFillColorPrimaryBrush}"
                        Text="Indeterminate activity" Margin="0,4,0,0" />
                </StackPanel>
            </Grid>
        </ui:Card>

        <ui:Card Padding="16" Variant="Default">
            <StackPanel>
                <ui:Button x:Name="ActionBtn"
                    Content="Trigger action"
                    Appearance="Accent"
                    Margin="0,0,0,12" />
                <ui:InfoBar x:Name="StatusBar"
                    IsOpen="True"
                    IsClosable="False"
                    Severity="Informational"
                    Title="Ready"
                    Message="Click the button to trigger an action." />
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

$window.FindName('ActionBtn').add_Click({
    $bar = $window.FindName('StatusBar')
    $bar.Title = 'Action received'
    $bar.Message = "PowerShell handled the click at $(Get-Date -Format 'T')."
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
