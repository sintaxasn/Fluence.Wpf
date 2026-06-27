# ControlsTour.ps1 - Common Fluence controls (buttons, toggle, checkbox, radios, text box, number
# box, split buttons, a slider, rating, an expander, a list, and icons) inside scrolling cards;
# the toggle drives an InfoBar message from PowerShell. The module handles STA, assembly loading,
# the Application, theming, and the message loop.
# Run: powershell.exe -File ControlsTour.ps1   OR   pwsh -File ControlsTour.ps1

# The -Initialize block keeps the canonical ($Window, $Data) signature shared by every window example;
# this tour holds no cross-click state, so $Data is intentionally unread here.
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Data',
    Justification = 'Initialize blocks keep the canonical ($Window, $Data) signature; this example needs no $Data state.')]
param()

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

$xaml = @'
<ui:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"
    Title="Fluence.Wpf - Controls tour"
    Width="620"
    Height="640"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <ui:SmoothScrollViewer>
        <StackPanel Margin="24">
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Buttons" ui:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <StackPanel Orientation="Horizontal">
                        <ui:Button Content="Standard" Margin="0,0,8,0" />
                        <ui:Button Content="Accent" Appearance="Accent" Margin="0,0,8,0" />
                        <ui:Button Content="Disabled" IsEnabled="False" />
                    </StackPanel>
                </StackPanel>
            </ui:Card>
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Selection" ui:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <ui:ToggleSwitch x:Name="DemoToggle" OnContent="On" OffContent="Off" Margin="0,0,0,8" />
                    <ui:CheckBox Content="I am a checkbox" Margin="0,0,0,8" />
                    <ui:RadioButton Content="Option A" GroupName="Demo" Margin="0,0,0,4" />
                    <ui:RadioButton Content="Option B" GroupName="Demo" />
                </StackPanel>
            </ui:Card>
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Text input" ui:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <ui:TextBox PlaceholderText="Type here" Margin="0,0,0,8" />
                    <ui:NumberBox Header="A number" Minimum="0" Maximum="100" SpinButtonPlacementMode="Compact" />
                </StackPanel>
            </ui:Card>
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Buttons with flyouts" ui:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <StackPanel Orientation="Horizontal">
                        <ui:DropDownButton Content="New" Margin="0,0,8,0">
                            <ui:DropDownButton.Flyout>
                                <StackPanel MinWidth="160" Margin="8">
                                    <ui:Button Content="Document" Appearance="Subtle" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
                                    <ui:Button Content="Folder" Appearance="Subtle" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
                                </StackPanel>
                            </ui:DropDownButton.Flyout>
                        </ui:DropDownButton>
                        <ui:SplitButton Content="Save" Appearance="Accent" Margin="0,0,8,0">
                            <ui:SplitButton.Flyout>
                                <StackPanel MinWidth="160" Margin="8">
                                    <ui:Button Content="Save as..." Appearance="Subtle" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
                                    <ui:Button Content="Export" Appearance="Subtle" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
                                </StackPanel>
                            </ui:SplitButton.Flyout>
                        </ui:SplitButton>
                        <ui:ToggleSplitButton Content="Bulleted list">
                            <ui:ToggleSplitButton.Flyout>
                                <StackPanel MinWidth="160" Margin="8">
                                    <ui:Button Content="Bulleted" Appearance="Subtle" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
                                    <ui:Button Content="Numbered" Appearance="Subtle" HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
                                </StackPanel>
                            </ui:ToggleSplitButton.Flyout>
                        </ui:ToggleSplitButton>
                    </StackPanel>
                </StackPanel>
            </ui:Card>
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Rating and slider" ui:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <ui:RatingControl Caption="Rate the experience" MaxRating="5" Value="3" Margin="0,0,0,8" />
                    <ui:Slider Minimum="0" Maximum="100" Value="40" />
                </StackPanel>
            </ui:Card>
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Expander" ui:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <ui:Expander Header="Advanced options">
                        <TextBlock Text="Secondary settings that start collapsed until needed." TextWrapping="Wrap" Foreground="{DynamicResource TextFillColorSecondaryBrush}" Margin="0,8,0,0" />
                    </ui:Expander>
                </StackPanel>
            </ui:Card>
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="List" ui:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <ui:ListView Height="120">
                        <ListViewItem Content="First item" />
                        <ListViewItem Content="Second item" />
                        <ListViewItem Content="Third item" />
                    </ui:ListView>
                </StackPanel>
            </ui:Card>
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Icons" ui:TextBlockExtensions.Typography="Subtitle" Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <StackPanel Orientation="Horizontal">
                        <ui:FontIcon Glyph="&#xE734;" Margin="0,0,16,0" />
                        <ui:FontIcon Glyph="&#xE713;" Margin="0,0,16,0" />
                        <ui:FontIcon Glyph="&#xE946;" />
                    </StackPanel>
                </StackPanel>
            </ui:Card>
            <ui:InfoBar x:Name="StatusBar" IsOpen="True" IsClosable="False" Severity="Informational" Title="Toggle state" Message="Flip the switch above to update this message from PowerShell." />
        </StackPanel>
    </ui:SmoothScrollViewer>
</ui:FluenceWindow>
'@

Show-FluenceWindow -Xaml $xaml -WatchSystemTheme -Initialize {
    param($Window, $Data)

    $bar = $Window.FindName('StatusBar')
    $toggle = $Window.FindName('DemoToggle')

    $toggle.add_Checked({ $bar.Message = 'The switch is ON (handled in PowerShell).' }.GetNewClosure())
    $toggle.add_Unchecked({ $bar.Message = 'The switch is OFF (handled in PowerShell).' }.GetNewClosure())
}
