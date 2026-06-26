# ThemeAndAccent.ps1 - Switch Light/Dark/Auto themes and cycle custom accent colors live. The module
# handles STA, assembly loading, the Application, and the message loop; runtime theming goes through
# Set-FluenceTheme and Set-FluenceAccent, and -WatchSystemTheme follows the OS setting while open.
# Run: powershell.exe -File ThemeAndAccent.ps1   OR   pwsh -File ThemeAndAccent.ps1

# $Data is read inside the add_Click closure (via .GetNewClosure()), which the analyzer cannot trace
# statically; it is the sanctioned cross-click state channel on an MTA UI runspace, not a defect.
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Data',
    Justification = '$Data is read inside the GetNewClosure handler, which the analyzer cannot trace.')]
param()

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

$xaml = @'
<ui:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"
    Title="Fluence.Wpf - Theme and Accent"
    Width="560"
    Height="360"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <StackPanel Margin="24" VerticalAlignment="Center">
        <TextBlock Text="Theme" ui:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <StackPanel Orientation="Horizontal" Margin="0,8,0,16">
            <ui:Button x:Name="LightBtn" Content="Light" Margin="0,0,8,0" />
            <ui:Button x:Name="DarkBtn" Content="Dark" Margin="0,0,8,0" />
            <ui:Button x:Name="AutoBtn" Content="Auto (follow Windows)" />
        </StackPanel>
        <TextBlock Text="Accent" ui:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <StackPanel Orientation="Horizontal" Margin="0,8,0,16">
            <ui:Button x:Name="AccentBtn" Content="Cycle custom accent" Appearance="Accent" Margin="0,0,8,0" />
            <ui:Button x:Name="SystemAccentBtn" Content="Use system accent" />
        </StackPanel>
        <ui:InfoBar x:Name="StatusBar" IsOpen="True" IsClosable="False" Severity="Informational" Title="Tip" Message="Change the Windows theme while this is open - Auto follows it live." />
    </StackPanel>
</ui:FluenceWindow>
'@

Show-FluenceWindow -Xaml $xaml -WatchSystemTheme -Data @{ AccentIndex = 0 } -Initialize {
    param($Window, $Data)

    # A small palette to cycle through; blue, green, red, purple.
    $accents = @(
        [System.Windows.Media.Color]::FromRgb(0x00, 0x78, 0xD4),
        [System.Windows.Media.Color]::FromRgb(0x10, 0x89, 0x3E),
        [System.Windows.Media.Color]::FromRgb(0xC4, 0x2B, 0x1C),
        [System.Windows.Media.Color]::FromRgb(0x74, 0x37, 0xC9)
    )

    $Window.FindName('LightBtn').add_Click({ Set-FluenceTheme -Theme Light }.GetNewClosure())
    $Window.FindName('DarkBtn').add_Click({ Set-FluenceTheme -Theme Dark }.GetNewClosure())
    $Window.FindName('AutoBtn').add_Click({ Set-FluenceTheme -Theme Auto }.GetNewClosure())

    $Window.FindName('AccentBtn').add_Click({
            Set-FluenceAccent -Color $accents[$Data.AccentIndex % $accents.Count]
            $Data.AccentIndex++
        }.GetNewClosure())
    $Window.FindName('SystemAccentBtn').add_Click({ Set-FluenceAccent -System }.GetNewClosure())
}
