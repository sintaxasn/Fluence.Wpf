<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


# 06-ControlsTour.ps1 - Complete Fluence controls tour in category pages.
# Use the labeled category rail to explore the public controls and common variants.
# The switch and data-grid selection update a shared InfoBar. The grid binds
# PowerShell objects; flyouts, dialogs, tabs, tips, and an in-app acrylic host
# with an explicit moving source and solid fallback show interaction patterns.
# FlyoutPresenter is shown directly; CommandBarFlyout creates its presenter.
# Run: powershell.exe -STA -File 06-ControlsTour.ps1   OR   pwsh -File 06-ControlsTour.ps1

# The -Initialize block keeps the canonical ($Window, $Data) signature shared by every window example.
# It keeps UI state in local closures and does not need caller-provided $Data.
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Data',
    Justification = 'Initialize keeps UI state in local closures and needs no caller-provided Data.')]
param()

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

$xaml = @'
<fluence:FluenceWindow xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:fluence="http://schemas.fluencewpf.com" Title="Fluence.Wpf - Controls tour" Width="1120" Height="900" MinWidth="1000" MinHeight="880" SystemBackdropType="Mica" ExtendsContentIntoTitleBar="False">
    <Grid Margin="24,20,24,20" TextElement.Foreground="{DynamicResource TextFillColorPrimaryBrush}">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="168" />
            <ColumnDefinition Width="16" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        <StackPanel Grid.Row="0" Grid.Column="0" Grid.RowSpan="2" Margin="0,0,0,16">
            <TextBlock Text="Controls tour" fluence:TextBlockExtensions.Typography="BodyStrong" Margin="8,0,0,12" />
            <fluence:ListBox x:Name="CategoryList" SelectedIndex="0" SelectionMode="Single" AutomationProperties.Name="Controls tour categories">
                <fluence:ListBoxItem Content="Inputs" AutomationProperties.Name="Inputs controls" />
                <fluence:ListBoxItem Content="Color" AutomationProperties.Name="Color controls" />
                <fluence:ListBoxItem Content="Selection" AutomationProperties.Name="Selection controls" />
                <fluence:ListBoxItem Content="Commands" AutomationProperties.Name="Commands controls" />
                <fluence:ListBoxItem Content="Data" AutomationProperties.Name="Data controls" />
                <fluence:ListBoxItem Content="Navigation" AutomationProperties.Name="Navigation controls" />
                <fluence:ListBoxItem Content="Status" AutomationProperties.Name="Status controls" />
                <fluence:ListBoxItem Content="Surfaces" AutomationProperties.Name="Surfaces controls" />
                <fluence:ListBoxItem Content="Acrylic" AutomationProperties.Name="Acrylic material" />
                <fluence:ListBoxItem Content="Layout" AutomationProperties.Name="Layout controls" />
            </fluence:ListBox>
        </StackPanel>
        <StackPanel Grid.Row="0" Grid.Column="2" Margin="0,0,0,16">
            <TextBlock x:Name="CategoryTitle" Text="Inputs" fluence:TextBlockExtensions.Typography="Title" />
            <TextBlock x:Name="CategoryPurpose" Text="Enter text, choose values, and try date and time controls." fluence:TextBlockExtensions.Typography="Body" Foreground="{DynamicResource TextFillColorSecondaryBrush}" TextWrapping="Wrap" Margin="0,4,0,0" />
        </StackPanel>
        <Grid x:Name="PageInputs" Grid.Row="1" Grid.Column="2" Visibility="Visible">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Margin="0,0,8,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Text and search" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="Name" fluence:TextBlockExtensions.Typography="Body" />
                        <fluence:TextBox PlaceholderText="Your name" AutomationProperties.Name="Name" />
                        <TextBlock Text="Notes" fluence:TextBlockExtensions.Typography="Body" />
                        <fluence:TextBox PlaceholderText="Add a note" AcceptsReturn="True" TextWrapping="Wrap" MinHeight="60" AutomationProperties.Name="Notes" />
                        <TextBlock Text="Fruit search" fluence:TextBlockExtensions.Typography="Body" />
                        <fluence:AutoSuggestBox x:Name="FruitSuggest" PlaceholderText="Search fruit" AutomationProperties.Name="Fruit suggestions" />
                        <TextBlock Text="Password with reveal and strength" fluence:TextBlockExtensions.Typography="Body" />
                        <PasswordBox
                            Height="48"
                            AutomationProperties.Name="Password with reveal and strength"
                            fluence:PasswordBoxExtensions.PlaceholderText="Password"
                            fluence:PasswordBoxExtensions.RevealButtonEnabled="True"
                            fluence:PasswordBoxExtensions.ShowCapsLockIndicator="True"
                            fluence:PasswordBoxExtensions.ShowPasswordStrength="True" />
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Choices" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="Priority" fluence:TextBlockExtensions.Typography="Body" />
                        <fluence:ComboBox SelectedIndex="0" AutomationProperties.Name="Priority">
                            <ComboBoxItem Content="Normal" />
                            <ComboBoxItem Content="High" />
                        </fluence:ComboBox>
                        <TextBlock Text="Editable choice" fluence:TextBlockExtensions.Typography="Body" Margin="0,4,0,0" />
                        <fluence:ComboBox IsEditable="True" Text="Custom" AutomationProperties.Name="Editable choice">
                            <ComboBoxItem Content="Preset" />
                        </fluence:ComboBox>
                        <fluence:TimePicker Header="24-hour time" ClockIdentifier="24HourClock" MinuteIncrement="15" />
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
            <StackPanel Grid.Column="1" Margin="8,0,0,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Numbers and dates" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:NumberBox Header="Quantity" Minimum="0" Maximum="100" Value="12" SpinButtonPlacementMode="Inline" />
                        <fluence:NumberBox Header="Compact stepper" Value="2" SpinButtonPlacementMode="Compact" />
                        <fluence:DatePicker Header="Due date" />
                        <fluence:TimePicker Header="Start time" />
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
        </Grid>
        <Grid x:Name="PageColor" Grid.Row="1" Grid.Column="2" Visibility="Collapsed">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="320" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Margin="0,0,8,0">
                <fluence:Card Padding="16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Color picker" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:ColorPicker x:Name="TourColorPicker" HorizontalAlignment="Left" AutomationProperties.Name="Color picker with spectrum and channels" />
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
            <StackPanel Grid.Column="1" Margin="8,0,0,0">
                <fluence:Card Padding="16">
                    <fluence:StackPanel Spacing="12">
                        <TextBlock Text="Live color preview" fluence:TextBlockExtensions.Typography="Subtitle" />
                        <Border x:Name="SelectedColorSwatch" Height="224" Background="{DynamicResource AccentFillColorDefaultBrush}" BorderBrush="{DynamicResource CardStrokeColorDefaultBrush}" BorderThickness="1" CornerRadius="{DynamicResource ControlCornerRadius}" AutomationProperties.Name="Selected color swatch" />
                        <TextBlock x:Name="SelectedColorText" Text="Choose a color in the spectrum or channels." fluence:TextBlockExtensions.Typography="BodyStrong" TextWrapping="Wrap" />
                        <TextBlock Text="Edit the spectrum, hue, alpha, or channel values; the swatch and status update together." Foreground="{DynamicResource TextFillColorSecondaryBrush}" TextWrapping="Wrap" />
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
        </Grid>
        <Grid x:Name="PageSelection" Grid.Row="1" Grid.Column="2" Visibility="Collapsed">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Margin="0,0,8,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Toggle controls" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*" />
                                <ColumnDefinition Width="*" />
                            </Grid.ColumnDefinitions>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto" />
                                <RowDefinition Height="Auto" />
                            </Grid.RowDefinitions>
                            <fluence:StackPanel Grid.Row="0" Grid.Column="0" Spacing="8">
                                <TextBlock Text="Switches" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                                <fluence:ToggleSwitch x:Name="DemoToggle" OnContent="On" OffContent="Off" />
                                <fluence:ToggleSwitch IsChecked="True" OnContent="Enabled" OffContent="Disabled" />
                            </fluence:StackPanel>
                            <fluence:StackPanel Grid.Row="0" Grid.Column="1" Spacing="8">
                                <TextBlock Text="Check boxes" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                                <fluence:CheckBox Content="Checked" IsChecked="True" />
                                <fluence:CheckBox Content="Indeterminate" IsThreeState="True" IsChecked="{x:Null}" />
                            </fluence:StackPanel>
                            <fluence:StackPanel Grid.Row="1" Grid.Column="0" Spacing="8" Margin="0,16,0,0">
                                <TextBlock Text="Radio choice" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                                <fluence:RadioButton Content="Option A" GroupName="Demo" IsChecked="True" />
                                <fluence:RadioButton Content="Option B" GroupName="Demo" />
                            </fluence:StackPanel>
                            <fluence:StackPanel Grid.Row="1" Grid.Column="1" Spacing="8" Margin="0,16,0,0">
                                <TextBlock Text="Toggle button" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                                <fluence:ToggleButton Content="Pin item" />
                            </fluence:StackPanel>
                        </Grid>
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Range and rating" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="Horizontal range, 40 of 100" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        <fluence:Slider Minimum="0" Maximum="100" Value="40" TickFrequency="10" IsSnapToTickEnabled="True" AutomationProperties.Name="Horizontal slider" />
                        <TextBlock Text="Vertical range, 30 of 100" Foreground="{DynamicResource TextFillColorSecondaryBrush}" Margin="0,4,0,0" />
                        <fluence:Slider Orientation="Vertical" Height="88" Minimum="0" Maximum="100" Value="30" AutomationProperties.Name="Vertical slider" />
                        <TextBlock Text="Rating choices" Foreground="{DynamicResource TextFillColorSecondaryBrush}" Margin="0,4,0,0" />
                        <fluence:RatingControl Caption="Rate it" MaxRating="5" Value="3" />
                        <fluence:RatingControl Caption="Read only" MaxRating="5" Value="4" IsReadOnly="True" />
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
            <StackPanel Grid.Column="1" Margin="8,0,0,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Lists" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="Select several items" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        <fluence:ListBox Height="104" SelectionMode="Multiple" AutomationProperties.Name="Multiple selection list">
                            <fluence:ListBoxItem Content="Alpha" />
                            <fluence:ListBoxItem Content="Beta" />
                            <fluence:ListBoxItem Content="Gamma" />
                        </fluence:ListBox>
                        <Grid Margin="0,8,0,0">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*" />
                                <ColumnDefinition Width="8" />
                                <ColumnDefinition Width="*" />
                            </Grid.ColumnDefinitions>
                            <fluence:StackPanel Grid.Column="0" Spacing="8">
                                <TextBlock Text="List layout" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                                <fluence:ListView Height="100" ItemsLayout="List" AutomationProperties.Name="List layout">
                                    <ListViewItem Content="Document" />
                                    <ListViewItem Content="Spreadsheet" />
                                </fluence:ListView>
                            </fluence:StackPanel>
                            <fluence:StackPanel Grid.Column="2" Spacing="8">
                                <TextBlock Text="Grid layout" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                                <fluence:ListView Height="100" ItemsLayout="Grid" AutomationProperties.Name="Grid layout">
                                    <ListViewItem Content="Alpha" />
                                    <ListViewItem Content="Beta" />
                                    <ListViewItem Content="Gamma" />
                                    <ListViewItem Content="Delta" />
                                </fluence:ListView>
                            </fluence:StackPanel>
                        </Grid>
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Tree (multi-select)" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:TreeView Height="155" SelectionMode="Multiple" AutomationProperties.Name="Multiple selection tree">
                            <fluence:TreeViewItem Header="Workspace" IsExpanded="True">
                                <fluence:TreeViewItem Header="Documents" IsExpanded="True">
                                    <fluence:TreeViewItem Header="Roadmap" />
                                </fluence:TreeViewItem>
                                <fluence:TreeViewItem Header="Images" />
                            </fluence:TreeViewItem>
                        </fluence:TreeView>
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
        </Grid>
        <Grid x:Name="PageCommands" Grid.Row="1" Grid.Column="2" Visibility="Collapsed">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Margin="0,0,8,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Buttons" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="Standard, accent, subtle, and unavailable" Foreground="{DynamicResource TextFillColorSecondaryBrush}" TextWrapping="Wrap" />
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*" />
                                <ColumnDefinition Width="8" />
                                <ColumnDefinition Width="*" />
                            </Grid.ColumnDefinitions>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto" />
                                <RowDefinition Height="8" />
                                <RowDefinition Height="Auto" />
                            </Grid.RowDefinitions>
                            <fluence:Button Grid.Row="0" Grid.Column="0" Content="Standard" />
                            <fluence:Button x:Name="AccentAction" Grid.Row="0" Grid.Column="2" Content="Accent" Appearance="Accent" />
                            <fluence:Button Grid.Row="2" Grid.Column="0" Content="Subtle" Appearance="Subtle" />
                            <fluence:Button Grid.Row="2" Grid.Column="2" Content="Disabled" IsEnabled="False" />
                        </Grid>
                        <fluence:HyperlinkButton x:Name="HelpLink" Content="Learn more" />
                        <fluence:RepeatButton x:Name="RepeatAction" Content="Hold to repeat" />
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Split buttons" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="Open each arrow for another action." Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        <WrapPanel>
                            <fluence:DropDownButton x:Name="NewDropDown" Content="New" Margin="0,0,8,8">
                                <fluence:DropDownButton.Flyout>
                                    <fluence:Button Content="Document" Margin="8" />
                                </fluence:DropDownButton.Flyout>
                            </fluence:DropDownButton>
                            <fluence:SplitButton x:Name="SaveSplit" Content="Save" Margin="0,0,8,8">
                                <fluence:SplitButton.Flyout>
                                    <fluence:Button Content="Save as" Margin="8" />
                                </fluence:SplitButton.Flyout>
                            </fluence:SplitButton>
                            <fluence:ToggleSplitButton x:Name="BulletsSplit" Content="Bullets" Margin="0,0,8,8">
                                <fluence:ToggleSplitButton.Flyout>
                                    <fluence:Button Content="Numbered" Margin="8" />
                                </fluence:ToggleSplitButton.Flyout>
                            </fluence:ToggleSplitButton>
                        </WrapPanel>
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Flyout and hint" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:Button x:Name="FlyoutButton" Content="Show flyout">
                            <fluence:FlyoutBase.AttachedFlyout>
                                <fluence:Flyout>
                                    <fluence:Flyout.Content>
                                        <StackPanel Margin="8">
                                            <TextBlock Text="Quick details" />
                                            <fluence:Button Content="Acknowledge" />
                                        </StackPanel>
                                    </fluence:Flyout.Content>
                                </fluence:Flyout>
                            </fluence:FlyoutBase.AttachedFlyout>
                        </fluence:Button>
                        <fluence:Button Content="Hover for hint">
                            <fluence:Button.ToolTip>
                                <fluence:ToolTip Content="Helpful context" />
                            </fluence:Button.ToolTip>
                        </fluence:Button>
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
            <StackPanel Grid.Column="1" Margin="8,0,0,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Menus" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="Open File or View. Right-click the note below." Foreground="{DynamicResource TextFillColorSecondaryBrush}" TextWrapping="Wrap" />
                        <fluence:Menu>
                            <fluence:MenuItem Header="_File">
                                <fluence:MenuItem x:Name="MenuOpen" Header="_Open" />
                                <fluence:Separator />
                                <fluence:MenuItem x:Name="MenuSave" Header="_Save" />
                            </fluence:MenuItem>
                            <fluence:MenuItem Header="_View">
                                <fluence:MenuItem x:Name="MenuDetails" Header="Details" IsCheckable="True" />
                            </fluence:MenuItem>
                        </fluence:Menu>
                        <fluence:Border x:Name="ContextNote" Variant="Subtle" Padding="12">
                            <fluence:Border.ContextMenu>
                                <fluence:ContextMenu>
                                    <fluence:MenuItem Header="Copy" />
                                    <fluence:MenuItem Header="Paste" />
                                </fluence:ContextMenu>
                            </fluence:Border.ContextMenu>
                            <TextBlock Text="Right-click this note" />
                        </fluence:Border>
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Command bar" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:Button x:Name="CommandFlyoutButton" Content="Show command bar">
                            <fluence:FlyoutBase.AttachedFlyout>
                                <fluence:CommandBarFlyout>
                                    <fluence:CommandBarFlyout.PrimaryCommands>
                                        <fluence:AppBarButton Label="Copy" />
                                        <fluence:AppBarButton Label="Paste" />
                                    </fluence:CommandBarFlyout.PrimaryCommands>
                                    <fluence:CommandBarFlyout.SecondaryCommands>
                                        <fluence:AppBarButton Label="Rename" />
                                    </fluence:CommandBarFlyout.SecondaryCommands>
                                </fluence:CommandBarFlyout>
                            </fluence:FlyoutBase.AttachedFlyout>
                        </fluence:Button>
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
        </Grid>
        <Grid x:Name="PageData" Grid.Row="1" Grid.Column="2" Visibility="Collapsed">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="3*" />
                <ColumnDefinition Width="2*" />
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Margin="0,0,8,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="PowerShell task data" fluence:TextBlockExtensions.Typography="Subtitle" />
                        <TextBlock Text="Select a row to read its PowerShell object in the status bar." Foreground="{DynamicResource TextFillColorSecondaryBrush}" TextWrapping="Wrap" />
                        <fluence:DataGrid x:Name="TaskGrid" Height="240" AutoGenerateColumns="False" IsReadOnly="True" AutomationProperties.Name="Tasks">
                            <fluence:DataGrid.EmptyContent>
                                <TextBlock Text="Rows load from PowerShell." />
                            </fluence:DataGrid.EmptyContent>
                            <fluence:DataGrid.Columns>
                                <DataGridTextColumn Header="Task" Binding="{Binding Name}" Width="2*" MinWidth="120" />
                                <DataGridTextColumn Header="Owner" Binding="{Binding Owner}" Width="*" MinWidth="80" />
                                <DataGridTextColumn Header="Progress" Binding="{Binding Progress, StringFormat='{}{0}%'}" Width="*" MinWidth="85" />
                                <DataGridCheckBoxColumn Header="Done" Binding="{Binding Done}" Width="Auto" />
                            </fluence:DataGrid.Columns>
                        </fluence:DataGrid>
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Tabs" fluence:TextBlockExtensions.Typography="Subtitle" />
                        <TextBlock Text="Add a tab, switch views, or close an unpinned tab." Foreground="{DynamicResource TextFillColorSecondaryBrush}" TextWrapping="Wrap" />
                        <fluence:TabView x:Name="WorkTabs" Height="120" IsAddTabButtonVisible="True">
                            <fluence:TabViewItem Header="Overview" IsSelected="True">
                                <TextBlock Text="Five tasks in the current plan." Margin="16" />
                            </fluence:TabViewItem>
                            <fluence:TabViewItem Header="Details">
                                <TextBlock Text="Open a task to inspect details." Margin="16" />
                            </fluence:TabViewItem>
                            <fluence:TabViewItem Header="Pinned" IsClosable="False">
                                <TextBlock Text="This tab stays available." Margin="16" />
                            </fluence:TabViewItem>
                        </fluence:TabView>
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
            <StackPanel Grid.Column="1" Margin="8,0,0,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Path and pages" fluence:TextBlockExtensions.Typography="Subtitle" />
                        <TextBlock Text="Bound path" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        <fluence:BreadcrumbBar x:Name="PathCrumbs" />
                        <TextBlock Text="Static path" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        <fluence:BreadcrumbBar AutomationProperties.Name="Static breadcrumb">
                            <fluence:BreadcrumbBarItem Content="Home" />
                            <fluence:BreadcrumbBarItem Content="Samples" />
                        </fluence:BreadcrumbBar>
                        <TextBlock Text="Page navigation" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        <fluence:PipsPager x:Name="DemoPager" NumberOfPages="5" NextButtonVisibility="Visible" PreviousButtonVisibility="Visible" />
                        <TextBlock x:Name="PageNumber" Text="Page 1 of 5" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Views" fluence:TextBlockExtensions.Typography="Subtitle" />
                        <TextBlock Text="Choose a view to slide between result sets." Foreground="{DynamicResource TextFillColorSecondaryBrush}" TextWrapping="Wrap" />
                        <fluence:SelectorBar x:Name="ViewSelector">
                            <fluence:SelectorBarItem Text="Recent" />
                            <fluence:SelectorBarItem Text="Shared" />
                        </fluence:SelectorBar>
                        <fluence:SlideNavigationPresenter x:Name="ViewPresenter" Height="72">
                            <TextBlock Text="Recent items in the current plan" Margin="12" TextWrapping="Wrap" />
                        </fluence:SlideNavigationPresenter>
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
        </Grid>
        <Grid x:Name="PageNavigation" Grid.Row="1" Grid.Column="2" Visibility="Collapsed">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="16" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>
            <fluence:Card Grid.Row="0" Grid.ColumnSpan="3" Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Left navigation" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:NavigationView x:Name="LeftNavigationDemo" Height="300" PaneDisplayMode="Left" AutomationProperties.Name="Left navigation">
                            <fluence:NavigationViewItem Content="Home" IsSelected="True" />
                            <fluence:NavigationViewItemHeader Content="Workspace" />
                            <fluence:NavigationViewItem Content="Files" />
                            <fluence:NavigationViewItemSeparator />
                            <fluence:NavigationViewItem Content="Reports">
                                <fluence:NavigationViewItem.InfoBadge>
                                    <fluence:InfoBadge Value="3" />
                                </fluence:NavigationViewItem.InfoBadge>
                            </fluence:NavigationViewItem>
                            <fluence:NavigationView.Content>
                                <StackPanel Margin="16">
                                    <TextBlock Text="Workspace" fluence:TextBlockExtensions.Typography="BodyStrong" />
                                    <TextBlock x:Name="LeftNavigationContent" Text="Home content" Margin="0,8,0,0" />
                                </StackPanel>
                            </fluence:NavigationView.Content>
                        </fluence:NavigationView>
                    </fluence:StackPanel>
            </fluence:Card>
            <fluence:Card Grid.Row="1" Grid.Column="0" Padding="16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Top navigation" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:NavigationView x:Name="TopNavigationDemo" Height="180" PaneDisplayMode="Top" AutomationProperties.Name="Top navigation">
                            <fluence:NavigationViewItem Content="Home" IsSelected="True" />
                            <fluence:NavigationViewItem Content="Activity" />
                            <fluence:NavigationView.Content>
                                <TextBlock x:Name="TopNavigationContent" Text="Home content" Margin="12" />
                            </fluence:NavigationView.Content>
                        </fluence:NavigationView>
                    </fluence:StackPanel>
            </fluence:Card>
            <fluence:Card Grid.Row="1" Grid.Column="2" Padding="16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Compact navigation" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:NavigationView x:Name="CompactNavigationDemo" Height="200" PaneDisplayMode="LeftCompact" AutomationProperties.Name="Compact navigation">
                            <fluence:NavigationViewItem Content="Home" IsSelected="True">
                                <fluence:NavigationViewItem.Icon>
                                    <fluence:FontIcon Glyph="" />
                                </fluence:NavigationViewItem.Icon>
                            </fluence:NavigationViewItem>
                            <fluence:NavigationViewItem Content="Mail">
                                <fluence:NavigationViewItem.Icon>
                                    <fluence:FontIcon Glyph="" />
                                </fluence:NavigationViewItem.Icon>
                            </fluence:NavigationViewItem>
                            <fluence:NavigationView.Content>
                                <TextBlock x:Name="CompactNavigationContent" Text="Home content" Margin="12" />
                            </fluence:NavigationView.Content>
                        </fluence:NavigationView>
                    </fluence:StackPanel>
            </fluence:Card>
        </Grid>
        <Grid x:Name="PageStatus" Grid.Row="1" Grid.Column="2" Visibility="Collapsed">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Margin="0,0,8,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Progress bars" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="Standard, 60%" />
                        <fluence:ProgressBar Value="60" Height="8" AutomationProperties.Name="Standard progress" />
                        <TextBlock Text="Indeterminate, working" Margin="0,4,0,0" />
                        <fluence:ProgressBar ProgressMode="Indeterminate" Height="8" AutomationProperties.Name="Indeterminate progress" />
                        <TextBlock Text="Paused, 75%" Margin="0,4,0,0" />
                        <fluence:ProgressBar ProgressMode="Paused" Value="75" Height="8" AutomationProperties.Name="Paused progress" />
                        <TextBlock Text="Error, 45%" Margin="0,4,0,0" />
                        <fluence:ProgressBar ProgressMode="Error" Value="45" Height="8" AutomationProperties.Name="Error progress" />
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Rings" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <UniformGrid Columns="4">
                            <fluence:StackPanel Spacing="8">
                                <fluence:ProgressRing Width="50" Height="50" IsActive="True" IsIndeterminate="True" AutomationProperties.Name="Indeterminate ring" />
                                <TextBlock Text="Working" TextAlignment="Center" />
                            </fluence:StackPanel>
                            <fluence:StackPanel Spacing="8">
                                <fluence:ProgressRing Width="50" Height="50" IsActive="True" IsIndeterminate="False" Value="65" AutomationProperties.Name="Standard ring" />
                                <TextBlock Text="65%" TextAlignment="Center" />
                            </fluence:StackPanel>
                            <fluence:StackPanel Spacing="8">
                                <fluence:ProgressRing Width="50" Height="50" IsActive="True" IsIndeterminate="False" Value="75" ProgressState="Paused" AutomationProperties.Name="Paused ring" />
                                <TextBlock Text="Paused" TextAlignment="Center" />
                            </fluence:StackPanel>
                            <fluence:StackPanel Spacing="8">
                                <fluence:ProgressRing Width="50" Height="50" IsActive="True" IsIndeterminate="False" Value="45" ProgressState="Error" AutomationProperties.Name="Error ring" />
                                <TextBlock Text="Error" TextAlignment="Center" />
                            </fluence:StackPanel>
                        </UniformGrid>
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Badges and people" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="Notification badges" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        <WrapPanel>
                            <fluence:InfoBadge Value="7" Margin="0,0,10,8" AutomationProperties.Name="Seven notifications" />
                            <fluence:InfoBadge Margin="0,0,10,8" AutomationProperties.Name="Attention dot" />
                            <fluence:InfoBadge Margin="0,0,10,8" AutomationProperties.Name="Icon notification">
                                <fluence:InfoBadge.IconSource>
                                    <fluence:FontIcon Glyph="&#xE7BA;" />
                                </fluence:InfoBadge.IconSource>
                            </fluence:InfoBadge>
                        </WrapPanel>
                        <TextBlock Text="People and group" Foreground="{DynamicResource TextFillColorSecondaryBrush}" Margin="0,4,0,0" />
                        <WrapPanel>
                            <fluence:PersonPicture Initials="AQ" DisplayName="Avery Quinn" Width="42" Height="42" Margin="0,0,8,8" />
                            <fluence:PersonPicture Initials="JL" DisplayName="Jordan Lee" BadgeNumber="2" Width="42" Height="42" Margin="0,0,8,8" />
                            <fluence:PersonPicture IsGroup="True" Width="42" Height="42" Margin="0,0,8,8" AutomationProperties.Name="Team" />
                            <fluence:PersonPicture DisplayName="Sample profile" Width="42" Height="42" Margin="0,0,8,8">
                                <fluence:PersonPicture.ProfilePicture>
                                    <DrawingImage>
                                        <DrawingImage.Drawing>
                                            <DrawingGroup>
                                                <GeometryDrawing Brush="{DynamicResource AccentFillColorDefaultBrush}" Geometry="M 0,0 L 48,0 48,48 0,48 Z" />
                                                <GeometryDrawing Brush="{DynamicResource TextOnAccentFillColorPrimaryBrush}">
                                                    <GeometryDrawing.Geometry>
                                                        <EllipseGeometry Center="24,17" RadiusX="8" RadiusY="8" />
                                                    </GeometryDrawing.Geometry>
                                                </GeometryDrawing>
                                                <GeometryDrawing Brush="{DynamicResource TextOnAccentFillColorPrimaryBrush}" Geometry="M 8,44 C 8,32 15,28 24,28 C 33,28 40,32 40,44 Z" />
                                            </DrawingGroup>
                                        </DrawingImage.Drawing>
                                    </DrawingImage>
                                </fluence:PersonPicture.ProfilePicture>
                            </fluence:PersonPicture>
                        </WrapPanel>
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
            <StackPanel Grid.Column="1" Margin="8,0,0,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="InfoBar severities" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:InfoBar Title="Information" Message="Routine details." Severity="Informational" IsOpen="True" IsClosable="False" Margin="0,0,0,8" />
                        <fluence:InfoBar Title="Success" Message="Completed." Severity="Success" IsOpen="True" IsClosable="False" Margin="0,0,0,8" />
                        <fluence:InfoBar Title="Warning" Message="Needs attention." Severity="Warning" IsOpen="True" IsClosable="False" Margin="0,0,0,8" />
                        <fluence:InfoBar Title="Error" Message="Try again." Severity="Error" IsOpen="True" IsClosable="False" />
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
        </Grid>
        <Grid x:Name="PageSurfaces" Grid.Row="1" Grid.Column="2" Visibility="Collapsed">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Margin="0,0,8,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Dialog and tip" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="Open a dialog or an anchored teaching tip." Foreground="{DynamicResource TextFillColorSecondaryBrush}" TextWrapping="Wrap" />
                        <fluence:Button x:Name="DialogButton" Content="Show dialog" />
                        <Grid>
                            <fluence:Button x:Name="TipButton" Content="Show teaching tip" />
                            <fluence:TeachingTip x:Name="DemoTeachingTip" Title="Tour tip" Subtitle="Switch categories to explore." CloseButtonContent="Got it" IsLightDismissEnabled="True" />
                        </Grid>
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Expander" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:Expander x:Name="TourSurfacesExpander" Header="Advanced options">
                            <fluence:CheckBox Content="Include archived items" />
                        </fluence:Expander>
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Borders" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:Border Variant="Subtle" Padding="12">
                            <TextBlock Text="Subtle border" />
                        </fluence:Border>
                        <fluence:Border Variant="Card" Padding="12">
                            <TextBlock Text="Card border" />
                        </fluence:Border>
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
            <StackPanel Grid.Column="1" Margin="8,0,0,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Window and title bar" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="The live tour is a FluenceWindow with Mica. The control below is a separate compact TitleBar preview." TextWrapping="Wrap" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        <fluence:TitleBar Title="Sample title" Subtitle="Compact title bar" IsCompact="True" Height="48" AutomationProperties.Name="Compact title bar" />
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Presenters" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:FlyoutPresenter>
                            <TextBlock Text="FlyoutPresenter" Margin="8" />
                        </fluence:FlyoutPresenter>
                        <TextBlock Text="Open the command bar on the Commands page to see its presenter." TextWrapping="Wrap" />
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
        </Grid>
        <Grid x:Name="PageAcrylic" Grid.Row="1" Grid.Column="2" Visibility="Collapsed">
            <Grid.Resources>
                <fluence:AcrylicMaterialRecipe x:Key="TourFallbackRecipe" TintColor="#CCFFFFFF" TintOpacity="1" FallbackColor="{DynamicResource AcrylicBackgroundFillColorDefault}" AlwaysUseFallback="True" />
            </Grid.Resources>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Margin="0,0,8,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="In-app acrylic" fluence:TextBlockExtensions.Typography="Subtitle" />
                        <TextBlock Text="The colored field and acrylic host are separate siblings in this window. Move the field to see the live source." TextWrapping="Wrap" />
                        <fluence:Button x:Name="MoveAcrylicSource" Content="Move source colors" AutomationProperties.Name="Move acrylic source colors" />
                        <fluence:CheckBox x:Name="ForceAcrylicFallback" Content="Use solid fallback" AutomationProperties.Name="Use acrylic solid fallback" />
                        <TextBlock x:Name="AcrylicModeText" Text="Checking material mode..." TextWrapping="Wrap" AutomationProperties.LiveSetting="Polite" />
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16">
                    <TextBlock Text="The host follows the published Default recipe in Light and Dark. High Contrast, disabled transparency, software rendering, and an unavailable source use an opaque fallback." TextWrapping="Wrap" />
                </fluence:Card>
            </StackPanel>
            <StackPanel Grid.Column="1" Margin="8,0,0,0">
                <fluence:Card Padding="16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Same-window source and output" fluence:TextBlockExtensions.Typography="Subtitle" TextWrapping="Wrap" />
                        <Grid Height="250" ClipToBounds="True">
                            <Grid x:Name="AcrylicBackdropSource" Background="#FF243E76">
                                <Rectangle Width="170" Height="250" HorizontalAlignment="Left" Fill="#FFDF534A">
                                    <Rectangle.RenderTransform>
                                        <TranslateTransform x:Name="AcrylicMovingBand" X="0" />
                                    </Rectangle.RenderTransform>
                                </Rectangle>
                                <Rectangle Width="120" Height="250" HorizontalAlignment="Right" Fill="#FF3BB7A1" />
                                <Ellipse Width="100" Height="100" HorizontalAlignment="Center" VerticalAlignment="Bottom" Fill="#FFFFC957" />
                            </Grid>
                            <fluence:AcrylicElement x:Name="TourAcrylicHost" Width="250" Height="150" HorizontalAlignment="Center" VerticalAlignment="Center" Recipe="{DynamicResource AcrylicInAppDefaultRecipe}" AutomationProperties.Name="Live acrylic material">
                                <TextBlock Text="Sharp content over acrylic" HorizontalAlignment="Center" VerticalAlignment="Center" TextWrapping="Wrap" TextAlignment="Center" FontWeight="SemiBold" />
                            </fluence:AcrylicElement>
                        </Grid>
                        <TextBlock Text="The colored field is the explicit backdrop source. The host is not inside that source." TextWrapping="Wrap" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
        </Grid>
        <Grid x:Name="PageLayout" Grid.Row="1" Grid.Column="2" Visibility="Collapsed">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0" Margin="0,0,8,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Layout wrappers" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="DockPanel places the action beside content." Foreground="{DynamicResource TextFillColorSecondaryBrush}" TextWrapping="Wrap" />
                        <fluence:DockPanel>
                            <fluence:Button Content="Left" DockPanel.Dock="Left" Margin="0,0,12,0" />
                            <TextBlock Text="DockPanel content" VerticalAlignment="Center" />
                        </fluence:DockPanel>
                        <fluence:Separator Margin="0,12,0,12" />
                        <TextBlock Text="StackPanel spaces related actions." Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        <fluence:StackPanel Orientation="Horizontal" Spacing="8">
                            <fluence:Button Content="One" />
                            <fluence:Button Content="Two" />
                        </fluence:StackPanel>
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Smooth scrolling" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="Scroll the numbered list." Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        <fluence:SmoothScrollViewer Height="96" VerticalScrollBarVisibility="Visible">
                            <fluence:StackPanel Spacing="8">
                                <TextBlock Text="Line 1" />
                                <TextBlock Text="Line 2" />
                                <TextBlock Text="Line 3" />
                                <TextBlock Text="Line 4" />
                                <TextBlock Text="Line 5" />
                                <TextBlock Text="Line 6" />
                            </fluence:StackPanel>
                        </fluence:SmoothScrollViewer>
                        <TextBlock Text="Horizontal scrollbar (hover to expand)" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        <ScrollBar Orientation="Horizontal" Maximum="100" Value="35" ViewportSize="25" Width="240" Height="24" HorizontalAlignment="Left"
                                   Style="{DynamicResource HorizontalScrollBarStyle}"
                                   fluence:ScrollBarExtensions.IsIndicatorEnabled="True"
                                   fluence:ScrollBarExtensions.IndicatorMode="MouseIndicator"
                                   AutomationProperties.Name="Native themed scrollbar" />
                    </fluence:StackPanel>
                </fluence:Card>
                <TextBlock Text="Card variants" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,12" />
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="16" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>
                    <fluence:Card x:Name="ClickableCard" Grid.Row="0" Grid.Column="0" Variant="Default" IsClickable="True" Padding="16" Margin="0,0,0,12" AutomationProperties.Name="Clickable default card">
                        <TextBlock Text="Default, click to select" TextWrapping="Wrap" />
                    </fluence:Card>
                    <fluence:Card Grid.Row="0" Grid.Column="2" Variant="Outlined" Padding="16" Margin="0,0,0,12" AutomationProperties.Name="Outlined card">
                        <TextBlock Text="Outlined" />
                    </fluence:Card>
                    <fluence:Card Grid.Row="1" Grid.Column="0" Variant="Filled" Padding="16" AutomationProperties.Name="Filled card">
                        <TextBlock Text="Filled" />
                    </fluence:Card>
                    <fluence:Card Grid.Row="1" Grid.Column="2" Variant="Subtle" Padding="16" AutomationProperties.Name="Subtle card">
                        <TextBlock Text="Subtle" />
                    </fluence:Card>
                </Grid>
            </StackPanel>
            <StackPanel Grid.Column="1" Margin="8,0,0,0">
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Icons and image" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <TextBlock Text="Font icons: favorite, settings, and info" Foreground="{DynamicResource TextFillColorSecondaryBrush}" TextWrapping="Wrap" />
                        <WrapPanel>
                            <fluence:FontIcon Glyph="" Margin="0,0,16,0" AutomationProperties.Name="Favorite icon" />
                            <fluence:FontIcon Glyph="" Margin="0,0,16,0" AutomationProperties.Name="Settings icon" />
                            <fluence:FontIcon Glyph="" AutomationProperties.Name="Info icon" />
                        </WrapPanel>
                        <TextBlock Text="Drawing image with theme accent" Foreground="{DynamicResource TextFillColorSecondaryBrush}" Margin="0,8,0,0" />
                        <fluence:Image Width="80" Height="80">
                            <fluence:Image.Source>
                                <DrawingImage>
                                    <DrawingImage.Drawing>
                                        <GeometryDrawing Brush="{DynamicResource AccentFillColorDefaultBrush}" Geometry="M 4,4 L 76,4 76,76 4,76 Z" />
                                    </DrawingImage.Drawing>
                                </DrawingImage>
                            </fluence:Image.Source>
                        </fluence:Image>
                    </fluence:StackPanel>
                </fluence:Card>
                <fluence:Card Padding="16" Margin="0,0,0,16">
                    <fluence:StackPanel Spacing="8">
                        <TextBlock Text="Typography" fluence:TextBlockExtensions.Typography="Subtitle" Margin="0,0,0,4" />
                        <fluence:TextBlock Text="Title - page heading" Typography="Title" />
                        <fluence:TextBlock Text="Subtitle - section heading" Typography="Subtitle" />
                        <fluence:TextBlock Text="BodyStrong - emphasis" Typography="BodyStrong" />
                        <fluence:TextBlock Text="Body - regular reading" Typography="Body" />
                    </fluence:StackPanel>
                </fluence:Card>
            </StackPanel>
        </Grid>
        <Grid Grid.Row="2" Grid.ColumnSpan="3" Margin="0,16,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="Auto" />
            </Grid.ColumnDefinitions>
            <fluence:InfoBar x:Name="StatusBar" Grid.Column="0" IsOpen="True" IsClosable="False" Severity="Informational" Title="Tour status" Message="Select a category or control." AutomationProperties.LiveSetting="Polite" />
            <fluence:Button x:Name="CloseButton" Grid.Column="1" Content="Close" MinWidth="120" IsCancel="True" Margin="16,0,0,0" VerticalAlignment="Center" />
        </Grid>
    </Grid>
</fluence:FluenceWindow>
'@

Show-FluenceWindow -Xaml $xaml -WatchSystemTheme -Initialize {
    param($Window, $Data)
    $bar = $Window.FindName('StatusBar')
    $Window.FindName('CloseButton').add_Click({ Close-FluenceWindow -Window $Window }.GetNewClosure())
    $categories = @('Inputs', 'Color', 'Selection', 'Commands', 'Data', 'Navigation', 'Status', 'Surfaces', 'Acrylic', 'Layout')
    $purposes = @(
        'Enter text, choose values, and try date and time controls.'
        'Choose a color and inspect its live value.'
        'Try toggles, ranges, ratings, lists, and a tree.'
        'Use buttons, menus, flyouts, and a command bar.'
        'Explore PowerShell rows, tabs, pages, and views.'
        'Compare left, top, and compact navigation.'
        'Compare progress, notifications, and status messages.'
        'Open dialogs and tips, and inspect reusable surfaces.'
        'Move an explicit same-window source and switch its acrylic host to solid fallback.'
        'Explore layout, icons, images, scrolling, and type.'
    )
    $categoryList = $Window.FindName('CategoryList')
    $categoryList.add_SelectionChanged({
            $index = $categoryList.SelectedIndex
            if ($index -lt 0) { return }
            for ($i = 0; $i -lt $categories.Count; $i++)
            {
                $Window.FindName("Page$($categories[$i])").Visibility = if ($i -eq $index) { [System.Windows.Visibility]::Visible } else { [System.Windows.Visibility]::Collapsed }
            }
            $Window.FindName('CategoryTitle').Text = $categories[$index]
            $Window.FindName('CategoryPurpose').Text = $purposes[$index]
            $bar.Message = "Showing $($categories[$index]) controls."
        }.GetNewClosure())
    $toggle = $Window.FindName('DemoToggle')
    $toggle.add_Checked({ $bar.Message = 'The switch is ON (handled in PowerShell).' }.GetNewClosure())
    $toggle.add_Unchecked({ $bar.Message = 'The switch is OFF (handled in PowerShell).' }.GetNewClosure())
    $grid = $Window.FindName('TaskGrid')
    $grid.ItemsSource = @(
        [pscustomobject]@{ Name = 'Collect inventory'; Owner = 'Avery'; Progress = 100; Done = $true }
        [pscustomobject]@{ Name = 'Patch servers'; Owner = 'Jordan'; Progress = 60; Done = $false }
        [pscustomobject]@{ Name = 'Rotate certificates'; Owner = 'Riley'; Progress = 25; Done = $false }
        [pscustomobject]@{ Name = 'Archive logs'; Owner = 'Avery'; Progress = 100; Done = $true }
        [pscustomobject]@{ Name = 'Review access'; Owner = 'Sam'; Progress = 0; Done = $false }
    )
    $grid.add_SelectionChanged({
            $task = $grid.SelectedItem
            if ($null -ne $task)
            {
                $bar.Message = "Selected '$($task.Name)', owned by $($task.Owner) (read in PowerShell)."
            }
        }.GetNewClosure())
    $Window.FindName('PathCrumbs').ItemsSource = @('Home', 'Projects', 'Controls tour')
    foreach ($sample in @(
            @('LeftNavigationDemo', 'LeftNavigationContent'),
            @('TopNavigationDemo', 'TopNavigationContent'),
            @('CompactNavigationDemo', 'CompactNavigationContent')
        ))
    {
        $navigation = $Window.FindName($sample[0])
        $sampleContent = $Window.FindName($sample[1])
        $navigation.add_ItemInvoked({
                param($navigationView, $invoked)
                $destination = [string]$invoked.InvokedItemContainer.Content
                $sampleContent.Text = "$destination content"
                $bar.Message = "$destination selected in $($navigationView.PaneDisplayMode) navigation."
            }.GetNewClosure())
    }
    $fruit = @('Apple', 'Apricot', 'Banana', 'Cherry', 'Orange')
    $suggest = $Window.FindName('FruitSuggest')
    $suggest.ItemsSource = $fruit
    $suggest.add_TextChanged({
            param($box, $change)
            if ($change.Reason -eq [Fluence.Wpf.AutoSuggestionBoxTextChangeReason]::UserInput)
            {
                $box.ItemsSource = @($fruit | Where-Object { $_.StartsWith($box.Text, [System.StringComparison]::OrdinalIgnoreCase) })
            }
        }.GetNewClosure())
    $suggest.add_QuerySubmitted({
            param($box, $submission)
            $chosen = if ($null -ne $submission.ChosenSuggestion) { $submission.ChosenSuggestion } else { $box.Text }
            $bar.Message = "Searched for $chosen."
        }.GetNewClosure())
    $Window.FindName('TourColorPicker').add_ColorChanged({
            param($picker, $change)
            $color = $picker.Color
            $code = '#{0:X2}{1:X2}{2:X2}{3:X2}' -f $color.A, $color.R, $color.G, $color.B
            $Window.FindName('SelectedColorText').Text = "Selected $code"
            $Window.FindName('SelectedColorSwatch').Background = [System.Windows.Media.SolidColorBrush]::new($color)
            $bar.Message = "Color changed from $($change.OldColor) to $code."
        }.GetNewClosure())
    $initialColor = $Window.FindName('TourColorPicker').Color
    $Window.FindName('SelectedColorSwatch').Background = [System.Windows.Media.SolidColorBrush]::new($initialColor)
    $Window.FindName('SelectedColorText').Text = ('Selected #{0:X2}{1:X2}{2:X2}{3:X2}' -f $initialColor.A, $initialColor.R, $initialColor.G, $initialColor.B)
    $Window.FindName('AccentAction').add_Click({ $bar.Message = 'Accent action selected.' }.GetNewClosure())
    $Window.FindName('ClickableCard').add_Click({ $bar.Message = 'Default Card selected.' }.GetNewClosure())
    $acrylicHost = $Window.FindName('TourAcrylicHost')
    $acrylicHost.BackdropSource = $Window.FindName('AcrylicBackdropSource')
    $acrylicModeText = $Window.FindName('AcrylicModeText')
    $updateAcrylicMode = {
        $acrylicModeText.Text = if ($acrylicHost.IsUsingFallback)
        {
            'Mode: opaque solid fallback.'
        }
        else
        {
            'Mode: live in-app acrylic material.'
        }
    }.GetNewClosure()
    $acrylicStateProperty = [System.ComponentModel.DependencyPropertyDescriptor]::FromProperty([Fluence.Wpf.Controls.AcrylicElement]::IsUsingFallbackProperty, [Fluence.Wpf.Controls.AcrylicElement])
    $acrylicStateHandler = [System.EventHandler]$updateAcrylicMode
    $acrylicStateProperty.AddValueChanged($acrylicHost, $acrylicStateHandler)
    $Window.add_Closed({
            $acrylicStateProperty.RemoveValueChanged($acrylicHost, $acrylicStateHandler)
        }.GetNewClosure())
    & $updateAcrylicMode
    $sourceOffset = 0
    $movingBand = $Window.FindName('AcrylicMovingBand')
    $Window.FindName('MoveAcrylicSource').add_Click({
            $sourceOffset = if ($sourceOffset -eq 0)
            {
                95
            }
            else
            {
                0
            }
            $movingBand.X = $sourceOffset
            $bar.Message = 'The explicit source colors moved behind the acrylic host.'
        }.GetNewClosure())
    $fallbackToggle = $Window.FindName('ForceAcrylicFallback')
    $fallbackToggle.add_Checked({
            $acrylicHost.SetResourceReference([Fluence.Wpf.Controls.AcrylicElement]::RecipeProperty, 'TourFallbackRecipe')
            $bar.Message = 'Acrylic uses its opaque solid fallback.'
            & $updateAcrylicMode
        }.GetNewClosure())
    $fallbackToggle.add_Unchecked({
            $acrylicHost.SetResourceReference([Fluence.Wpf.Controls.AcrylicElement]::RecipeProperty, 'AcrylicInAppDefaultRecipe')
            $bar.Message = 'Acrylic follows the live Default recipe again.'
            & $updateAcrylicMode
        }.GetNewClosure())
    $Window.FindName('NewDropDown').Flyout.add_Click({ $bar.Message = 'New document selected.' }.GetNewClosure())
    $Window.FindName('SaveSplit').add_Click({ $bar.Message = 'Save selected.' }.GetNewClosure())
    $Window.FindName('SaveSplit').Flyout.add_Click({ $bar.Message = 'Save as selected.' }.GetNewClosure())
    $bullets = $Window.FindName('BulletsSplit')
    $bullets.add_IsCheckedChanged({
            param($split, $state)
            $bar.Message = if ($state.IsChecked) { "$($split.Content) enabled." } else { "$($split.Content) disabled." }
        }.GetNewClosure())
    $bullets.Flyout.add_Click({ $bar.Message = 'Numbered list selected.' }.GetNewClosure())
    $Window.FindName('MenuOpen').add_Click({ $bar.Message = 'File menu: Open selected.' }.GetNewClosure())
    $Window.FindName('MenuSave').add_Click({ $bar.Message = 'File menu: Save selected.' }.GetNewClosure())
    $Window.FindName('MenuDetails').add_Click({ $bar.Message = 'View menu: Details toggled.' }.GetNewClosure())
    $contextItems = $Window.FindName('ContextNote').ContextMenu.Items
    $contextItems[0].add_Click({ $bar.Message = 'Context menu: Copy selected.' }.GetNewClosure())
    $contextItems[1].add_Click({ $bar.Message = 'Context menu: Paste selected.' }.GetNewClosure())
    $Window.FindName('HelpLink').add_Click({ $bar.Message = 'HyperlinkButton selected.' }.GetNewClosure())
    $repeatState = @{ Count = 0 }
    $Window.FindName('RepeatAction').add_Click({
            $repeatState.Count++
            $bar.Message = "RepeatButton fired $($repeatState.Count) time(s)."
        }.GetNewClosure())
    $Window.FindName('ViewSelector').add_SelectionChanged({
            $selection = $Window.FindName('ViewSelector').SelectedItem
            if ($null -ne $selection)
            {
                $presenter = $Window.FindName('ViewPresenter')
                $presenter.TransitionEffect = [Fluence.Wpf.SlideNavigationTransitionEffect]::FromRight
                $presenter.Content = [System.Windows.Controls.TextBlock]::new()
                $presenter.Content.Text = "$($selection.Text) items"
                $bar.Message = "Showing $($selection.Text) items."
            }
        }.GetNewClosure())
    $Window.FindName('DemoPager').add_SelectedIndexChanged({
            $pager = $Window.FindName('DemoPager')
            $Window.FindName('PageNumber').Text = "Page $($pager.SelectedPageIndex + 1) of $($pager.NumberOfPages)"
        }.GetNewClosure())
    $flyoutOwner = $Window.FindName('FlyoutButton')
    $flyout = [Fluence.Wpf.Controls.FlyoutBase]::GetAttachedFlyout($flyoutOwner)
    $flyout.Content.Children[1].add_Click({ $bar.Message = 'Flyout acknowledged.' }.GetNewClosure())
    $flyoutOwner.add_Click({
            [Fluence.Wpf.Controls.FlyoutBase]::ShowAttachedFlyout($flyoutOwner)
            $bar.Message = 'Flyout opened.'
        }.GetNewClosure())
    $commandOwner = $Window.FindName('CommandFlyoutButton')
    $commandFlyout = [Fluence.Wpf.Controls.FlyoutBase]::GetAttachedFlyout($commandOwner)
    foreach ($commandButton in @($commandFlyout.PrimaryCommands) + @($commandFlyout.SecondaryCommands))
    {
        $commandButton.add_Click({
                param($action)
                $bar.Message = "Command bar: $($action.Label) selected."
            }.GetNewClosure())
    }
    $commandOwner.add_Click({
            [Fluence.Wpf.Controls.FlyoutBase]::ShowAttachedFlyout($commandOwner)
            $bar.Message = 'Command bar opened.'
        }.GetNewClosure())
    $Window.FindName('TipButton').add_Click({
            $tip = $Window.FindName('DemoTeachingTip')
            $tip.Target = $Window.FindName('TipButton')
            $tip.IsOpen = -not $tip.IsOpen
        }.GetNewClosure())
    $Window.FindName('DialogButton').add_Click({
            $dialogStatusBar = $Window.FindName('StatusBar')
            $dialog = [Fluence.Wpf.Controls.ContentDialog]::new()
            $dialog.Title = 'Save changes?'
            $dialog.Content = 'This ContentDialog was created in PowerShell.'
            $dialog.PrimaryButtonText = 'Save'
            $dialog.SecondaryButtonText = 'Discard'
            $dialog.CloseButtonText = 'Cancel'
            $dialog.add_PrimaryButtonClick({ $dialogStatusBar.Message = 'Save selected.' }.GetNewClosure())
            $dialog.add_SecondaryButtonClick({ $dialogStatusBar.Message = 'Discard selected.' }.GetNewClosure())
            $dialog.add_CloseButtonClick({ $dialogStatusBar.Message = 'Save cancelled.' }.GetNewClosure())
            $null = $dialog.ShowAsync()
        }.GetNewClosure())
    $Window.FindName('WorkTabs').add_AddTabButtonClick({
            $tabs = $Window.FindName('WorkTabs')
            $item = [Fluence.Wpf.Controls.TabViewItem]::new()
            $item.Header = "New tab $($tabs.Items.Count + 1)"
            $item.Content = 'A new tab.'
            $null = $tabs.Items.Add($item)
            $item.IsSelected = $true
        }.GetNewClosure())
    $Window.FindName('WorkTabs').add_TabCloseRequested({
            param($tabControl, $closeArgs)
            $tabControl.Items.Remove($closeArgs.Tab)
        }.GetNewClosure())
}
