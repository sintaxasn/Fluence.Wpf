## Run with: powershell.exe -STA -File .\Show-ControlsDemo.ps1
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
    if (-not $PSCommandPath) {  throw 'Run this script from a file with Windows PowerShell 5.1 using: powershell.exe -STA -ExecutionPolicy Bypass -File .\Show-ControlsDemo.ps1' }
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
    Title="Controls Demo - Fluence.Wpf"
    Width="600" Height="690"
    MinWidth="500" MinHeight="690"
    SystemBackdropType="Mica"
    CornerStyle="Round"
    WindowStartupLocation="CenterScreen"
    >
    <ui:SmoothScrollViewer Margin="32"
        HorizontalScrollBarVisibility="Disabled"
        VerticalScrollBarVisibility="Auto">
        <StackPanel>
            <TextBlock
                ui:TextBlockExtensions.Typography="Title"
                Foreground="{DynamicResource TextFillColorPrimaryBrush}"
                Text="Controls" Margin="0,0,0,24" />

            <ui:Card Padding="16" Margin="0,0,0,16" Variant="Default">
                <StackPanel>
                    <TextBlock
                        ui:TextBlockExtensions.Typography="Subtitle"
                        Foreground="{DynamicResource TextFillColorPrimaryBrush}"
                        Text="Buttons" Margin="0,0,0,12" />
                    <StackPanel Orientation="Horizontal">
                        <ui:Button Content="Standard"  Margin="0,0,8,0" />
                        <ui:Button Content="Accent"    Appearance="Accent" Margin="0,0,8,0" />
                        <ui:Button Content="Disabled"  IsEnabled="False" />
                    </StackPanel>
                </StackPanel>
            </ui:Card>

            <ui:Card Padding="16" Margin="0,0,0,16" Variant="Default">
                <StackPanel>
                    <TextBlock
                        ui:TextBlockExtensions.Typography="Subtitle"
                        Foreground="{DynamicResource TextFillColorPrimaryBrush}"
                        Text="Selection" Margin="0,0,0,12" />
                    <ui:ToggleSwitch Content="Notifications"
                        IsChecked="True" OnContent="On" OffContent="Off"
                        Margin="0,0,0,8" />
                    <ui:CheckBox Content="Remember choice"
                        IsChecked="True" Margin="0,0,0,4" />
                    <ui:RadioButton Content="Option A"
                        GroupName="Demo" IsChecked="True" Margin="0,0,0,4" />
                    <ui:RadioButton Content="Option B"
                        GroupName="Demo" />
                </StackPanel>
            </ui:Card>

            <ui:Card Padding="16" Variant="Default">
                <StackPanel>
                    <TextBlock
                        ui:TextBlockExtensions.Typography="Subtitle"
                        Foreground="{DynamicResource TextFillColorPrimaryBrush}"
                        Text="Inputs" Margin="0,0,0,12" />
                    <ui:TextBox PlaceholderText="Type a message"
                        Text="Loaded from PowerShell" Margin="0,0,0,8" />
                    <ui:NumberBox Header="Count"
                        Value="3" Minimum="0" Maximum="10"
                        SpinButtonPlacementMode="Compact" />
                </StackPanel>
            </ui:Card>
        </StackPanel>
    </ui:SmoothScrollViewer>
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

## Set up the system theme watcher to automatically update the window's theme and backdrop when the system theme changes.
[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({
    [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window)
    [System.Windows.Threading.Dispatcher]::CurrentDispatcher.InvokeShutdown()
})

## Display the window and start the WPF message loop.
$null = $window.Show()
[System.Windows.Threading.Dispatcher]::Run()
