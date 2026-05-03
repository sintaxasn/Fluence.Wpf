/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Fluence.Wpf;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryWindowPage : UserControl
    {

        private const string ThemeAndAccentXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Window.ThemeAndAccent""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""*"" />
            <ColumnDefinition Width=""16"" />
            <ColumnDefinition Width=""*"" />
        </Grid.ColumnDefinitions>
        <StackPanel>
            <TextBlock
                Margin=""0,0,0,8""
                FontWeight=""SemiBold""
                Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                Text=""Theme"" />
            <WrapPanel Margin=""0,0,0,8"">
                <ui:RadioButton
                    x:Name=""ThemeLight""
                    Margin=""0,0,12,8""
                    Checked=""ThemeRadioButton_Checked""
                    Content=""Light""
                    GroupName=""ThemeGroup"" />
                <ui:RadioButton
                    x:Name=""ThemeDark""
                    Margin=""0,0,12,8""
                    Checked=""ThemeRadioButton_Checked""
                    Content=""Dark""
                    GroupName=""ThemeGroup"" />
                <ui:RadioButton
                    x:Name=""ThemeHighContrast""
                    Margin=""0,0,12,8""
                    Checked=""ThemeRadioButton_Checked""
                    Content=""HighContrast""
                    GroupName=""ThemeGroup"" />
                <ui:RadioButton
                    x:Name=""ThemeAuto""
                    Margin=""0,0,12,8""
                    Checked=""ThemeRadioButton_Checked""
                    Content=""Auto""
                    GroupName=""ThemeGroup""
                    IsChecked=""True"" />
            </WrapPanel>
            <StackPanel Margin=""0,0,0,8"" Orientation=""Horizontal"">
                <ui:ToggleSwitch
                    x:Name=""ThemeWatcherToggle""
                    Margin=""0,0,10,0""
                    Checked=""SystemThemeWatcher_Toggled""
                    IsChecked=""True""
                    Unchecked=""SystemThemeWatcher_Toggled"" />
                <TextBlock
                    x:Name=""SystemThemeLabel""
                    VerticalAlignment=""Center""
                    Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                    Text=""Watching: Yes"" />
            </StackPanel>
            <TextBlock
                x:Name=""ThemeStateLabel""
                Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                Text=""Current: Auto"" />
        </StackPanel>

        <StackPanel Grid.Column=""2"">
            <TextBlock
                Margin=""0,0,0,8""
                FontWeight=""SemiBold""
                Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                Text=""Accent color"" />
            <WrapPanel Margin=""0,0,0,8"">
                <ui:Button
                    Margin=""0,0,8,8""
                    Background=""#E81123""
                    Click=""AccentSwatch_Click""
                    Style=""{StaticResource AccentSwatchStyle}""
                    Tag=""#E81123"" />
                <ui:Button
                    Margin=""0,0,8,8""
                    Background=""#DA3B01""
                    Click=""AccentSwatch_Click""
                    Style=""{StaticResource AccentSwatchStyle}""
                    Tag=""#DA3B01"" />
                <ui:Button
                    Margin=""0,0,8,8""
                    Background=""#0078D4""
                    Click=""AccentSwatch_Click""
                    Style=""{StaticResource AccentSwatchStyle}""
                    Tag=""#0078D4"" />
                <ui:Button
                    Margin=""0,0,8,8""
                    Background=""#8A00C4""
                    Click=""AccentSwatch_Click""
                    Style=""{StaticResource AccentSwatchStyle}""
                    Tag=""#8A00C4"" />
            </WrapPanel>
            <ui:Button
                MinWidth=""80""
                HorizontalAlignment=""Left""
                Click=""SystemAccent_Click""
                Content=""System"" />
        </StackPanel>
    </Grid>
</UserControl>
";

        private const string ThemeAndAccentCSharpSource = @"using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluence.Wpf;

namespace Fluence.Wpf.Demo.Pages.Window
{
    public partial class ThemeAndAccent : UserControl
    {
        public ThemeAndAccent()
        {
            InitializeComponent();
        }

        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var rb = sender as RadioButton;
            if (rb == null)
            {
                return;
            }

            ApplicationTheme theme;
            if (ReferenceEquals(rb, ThemeLight))
            {
                theme = ApplicationTheme.Light;
            }
            else if (ReferenceEquals(rb, ThemeDark))
            {
                theme = ApplicationTheme.Dark;
            }
            else if (ReferenceEquals(rb, ThemeHighContrast))
            {
                theme = ApplicationTheme.HighContrast;
            }
            else
            {
                theme = ApplicationTheme.Auto;
            }

            var host = System.Windows.Window.GetWindow(this) as Fluence.Wpf.Controls.FluenceWindow;
            var backdrop = host != null ? host.SystemBackdropType : BackdropType.Mica;
            ApplicationThemeManager.Apply(theme, backdrop, true);
            ThemeStateLabel.Text = string.Format(""Current: {0}"", theme);
        }

        private void SystemThemeWatcher_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var host = System.Windows.Window.GetWindow(this);
            if (host == null)
            {
                return;
            }

            if (ThemeWatcherToggle.IsChecked == true)
            {
                SystemThemeWatcher.Watch(host);
                SystemThemeLabel.Text = ""Watching: Yes"";
            }
            else
            {
                SystemThemeWatcher.UnWatch(host);
                SystemThemeLabel.Text = ""Watching: No"";
            }
        }

        private void AccentSwatch_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;
            var hex = button != null ? button.Tag as string : null;
            if (string.IsNullOrEmpty(hex))
            {
                return;
            }

            try
            {
                var converted = ColorConverter.ConvertFromString(hex);
                if (converted != null)
                {
                    ApplicationAccentColorManager.ApplyCustomAccent((Color)converted);
                }
            }
            catch (FormatException)
            {
            }
        }

        private void SystemAccent_Click(object sender, RoutedEventArgs e)
        {
            ApplicationAccentColorManager.ApplySystemAccent();
        }
    }
}
";
        private const string BackdropAndCaptionButtonsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Window.BackdropAndCaptionButtons""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""Auto"" />
            <ColumnDefinition Width=""32"" />
            <ColumnDefinition Width=""*"" />
        </Grid.ColumnDefinitions>

        <StackPanel>
            <TextBlock
                Margin=""0,0,0,6""
                Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                Text=""Backdrop"" />
            <ui:ComboBox
                x:Name=""BackdropCombo""
                Width=""220""
                HorizontalAlignment=""Left""
                SelectedIndex=""2""
                SelectionChanged=""BackdropCombo_SelectionChanged"">
                <ComboBoxItem Content=""Auto"" />
                <ComboBoxItem Content=""None"" />
                <ComboBoxItem Content=""Mica"" />
                <ComboBoxItem Content=""Acrylic"" />
                <ComboBoxItem Content=""Tabbed"" />
            </ui:ComboBox>
        </StackPanel>

        <StackPanel Grid.Column=""2"">
            <TextBlock
                Margin=""0,0,0,6""
                Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                Text=""Caption buttons"" />
            <Grid Margin=""0,0,0,8"">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width=""Auto"" />
                    <ColumnDefinition Width=""110"" />
                    <ColumnDefinition Width=""14"" />
                    <ColumnDefinition Width=""Auto"" />
                    <ColumnDefinition Width=""110"" />
                </Grid.ColumnDefinitions>
                <Grid.RowDefinitions>
                    <RowDefinition Height=""Auto"" />
                    <RowDefinition Height=""8"" />
                    <RowDefinition Height=""Auto"" />
                </Grid.RowDefinitions>
                <TextBlock
                    Margin=""0,0,8,0""
                    VerticalAlignment=""Center""
                    Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                    Text=""Minimize"" />
                <ui:ComboBox
                    x:Name=""MinimizeVisibilityCombo""
                    Grid.Column=""1""
                    Width=""110""
                    SelectionChanged=""CaptionVisibilityCombo_SelectionChanged"">
                    <ComboBoxItem Content=""Default"" IsSelected=""True"" />
                    <ComboBoxItem Content=""Enable"" />
                    <ComboBoxItem Content=""Disable"" />
                    <ComboBoxItem Content=""Hidden"" />
                    <ComboBoxItem Content=""Collapsed"" />
                </ui:ComboBox>
                <TextBlock
                    Grid.Column=""3""
                    Margin=""0,0,8,0""
                    VerticalAlignment=""Center""
                    Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                    Text=""Maximize"" />
                <ui:ComboBox
                    x:Name=""MaximizeVisibilityCombo""
                    Grid.Column=""4""
                    Width=""110""
                    SelectionChanged=""CaptionVisibilityCombo_SelectionChanged"">
                    <ComboBoxItem Content=""Default"" IsSelected=""True"" />
                    <ComboBoxItem Content=""Enable"" />
                    <ComboBoxItem Content=""Disable"" />
                    <ComboBoxItem Content=""Hidden"" />
                    <ComboBoxItem Content=""Collapsed"" />
                </ui:ComboBox>
                <TextBlock
                    Grid.Row=""2""
                    Margin=""0,0,8,0""
                    VerticalAlignment=""Center""
                    Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                    Text=""Close"" />
                <ui:ComboBox
                    x:Name=""CloseVisibilityCombo""
                    Grid.Row=""2""
                    Grid.Column=""1""
                    Width=""110""
                    SelectionChanged=""CaptionVisibilityCombo_SelectionChanged"">
                    <ComboBoxItem Content=""Default"" IsSelected=""True"" />
                    <ComboBoxItem Content=""Enable"" />
                    <ComboBoxItem Content=""Disable"" />
                    <ComboBoxItem Content=""Hidden"" />
                    <ComboBoxItem Content=""Collapsed"" />
                </ui:ComboBox>
            </Grid>
            <WrapPanel>
                <ui:CheckBox
                    x:Name=""ShowWindowIconToggle""
                    Margin=""0,0,14,0""
                    Checked=""WindowChromeToggle_Changed""
                    Content=""Show Icon""
                    IsChecked=""True""
                    Unchecked=""WindowChromeToggle_Changed"" />
                <ui:CheckBox
                    x:Name=""ShowWindowTitleToggle""
                    Checked=""WindowChromeToggle_Changed""
                    Content=""Show Title""
                    IsChecked=""True""
                    Unchecked=""WindowChromeToggle_Changed"" />
            </WrapPanel>
        </StackPanel>
    </Grid>
</UserControl>
";

        private const string BackdropAndCaptionButtonsCSharpSource = @"using System;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages.Window
{
    public partial class BackdropAndCaptionButtons : UserControl
    {
        public BackdropAndCaptionButtons()
        {
            InitializeComponent();
        }

        private FluenceWindow HostFluenceWindow
        {
            get { return System.Windows.Window.GetWindow(this) as FluenceWindow; }
        }

        private void BackdropCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var window = HostFluenceWindow;
            if (window == null)
            {
                return;
            }

            BackdropType backdrop;
            switch (BackdropCombo.SelectedIndex)
            {
                case 1:
                    backdrop = BackdropType.None;
                    break;
                case 2:
                    backdrop = BackdropType.Mica;
                    break;
                case 3:
                    backdrop = BackdropType.Acrylic;
                    break;
                case 4:
                    backdrop = BackdropType.Tabbed;
                    break;
                default:
                    backdrop = BackdropType.Auto;
                    break;
            }

            window.SystemBackdropType = backdrop;
            ApplicationThemeManager.Apply(ApplicationThemeManager.CurrentTheme, backdrop, false);
        }

        private void CaptionVisibilityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var window = HostFluenceWindow;
            if (window == null)
            {
                return;
            }

            ApplyCaptionVisibility(MinimizeVisibilityCombo, v => window.IsMinimizeButtonVisible = v, en => window.IsMinimizable = en);
            ApplyCaptionVisibility(MaximizeVisibilityCombo, v => window.IsMaximizeButtonVisible = v, en => window.IsMaximizable = en);
            ApplyCaptionVisibility(CloseVisibilityCombo, v => window.IsCloseButtonVisible = v, en => window.IsClosable = en);
        }

        private void WindowChromeToggle_Changed(object sender, RoutedEventArgs e)
        {
            var window = HostFluenceWindow;
            if (window == null)
            {
                return;
            }

            window.ShowIcon = ShowWindowIconToggle != null && ShowWindowIconToggle.IsChecked == true;
            window.ShowTitle = ShowWindowTitleToggle != null && ShowWindowTitleToggle.IsChecked == true;
        }

        private static void ApplyCaptionVisibility(
            ComboBox combo,
            Action<Visibility> setVisibility,
            Action<bool> setEnabled)
        {
            var item = combo != null ? combo.SelectedItem as ComboBoxItem : null;
            var content = item != null ? item.Content as string : null;

            if (string.Equals(content, ""Hidden"", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Hidden);
                setEnabled(false);
            }
            else if (string.Equals(content, ""Collapsed"", StringComparison.Ordinal) ||
                string.Equals(content, ""Hide"", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Collapsed);
                setEnabled(false);
            }
            else if (string.Equals(content, ""Disable"", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Visible);
                setEnabled(false);
            }
            else
            {
                setVisibility(Visibility.Visible);
                setEnabled(true);
            }
        }
    }
}
";
        private const string TitleBarChromeXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Window.TitleBarChrome""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <StackPanel Margin=""0,0,0,8"" Orientation=""Horizontal"">
            <ui:ToggleSwitch
                x:Name=""ExtendsContentToggle""
                Margin=""0,0,10,0""
                Checked=""TitleBarToggle_Changed""
                IsChecked=""False""
                Unchecked=""TitleBarToggle_Changed"" />
            <TextBlock
                VerticalAlignment=""Center""
                Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                Text=""Extends into title bar"" />
        </StackPanel>
        <StackPanel Margin=""0,0,0,8"" Orientation=""Horizontal"">
            <ui:ToggleSwitch
                x:Name=""HasShadowToggle""
                Margin=""0,0,10,0""
                Checked=""TitleBarToggle_Changed""
                IsChecked=""True""
                Unchecked=""TitleBarToggle_Changed"" />
            <TextBlock
                VerticalAlignment=""Center""
                Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                Text=""Title bar shadow"" />
        </StackPanel>
        <StackPanel Margin=""0,0,0,8"" Orientation=""Horizontal"">
            <TextBlock
                Width=""60""
                Margin=""0,0,8,0""
                VerticalAlignment=""Center""
                Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                Text=""Height"" />
            <ui:Slider
                x:Name=""TitleBarHeightSlider""
                Width=""180""
                Maximum=""64""
                Minimum=""32""
                ValueChanged=""TitleBarHeightSlider_Changed""
                Value=""48"" />
            <TextBlock
                x:Name=""TitleBarHeightLabel""
                Margin=""8,0,0,0""
                VerticalAlignment=""Center""
                Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                Text=""48"" />
        </StackPanel>
    </StackPanel>
</UserControl>
";

        private const string TitleBarChromeCSharpSource = @"using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages.Window
{
    public partial class TitleBarChrome : UserControl
    {
        public TitleBarChrome()
        {
            InitializeComponent();
        }

        private FluenceWindow HostFluenceWindow
        {
            get { return System.Windows.Window.GetWindow(this) as FluenceWindow; }
        }

        private void TitleBarToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var window = HostFluenceWindow;
            if (window == null)
            {
                return;
            }

            if (ExtendsContentToggle != null)
            {
                var extends = ExtendsContentToggle.IsChecked == true;
                window.ExtendsContentIntoTitleBar = extends;
                window.TitleBarLeftIndent = extends ? 48d : 0d;
            }

            if (HasShadowToggle != null)
            {
                window.HasShadow = HasShadowToggle.IsChecked == true;
            }
        }

        private void TitleBarHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var window = HostFluenceWindow;
            if (window != null)
            {
                window.TitleBarHeight = e.NewValue;
            }

            TitleBarHeightLabel.Text = ((int)e.NewValue).ToString();
        }
    }
}
";

private bool _isSyncingTheme;
        private bool _isSyncingBackdrop;

        public GalleryWindowPage()
        {
            InitializeComponent();

            DemoSampleControl.ReplaceSourceLink(ThemeAndAccentSourceLink, ThemeAndAccentXamlSource, ThemeAndAccentCSharpSource);
            DemoSampleControl.ReplaceSourceLink(BackdropAndCaptionButtonsSourceLink, BackdropAndCaptionButtonsXamlSource, BackdropAndCaptionButtonsCSharpSource);
            DemoSampleControl.ReplaceSourceLink(TitleBarChromeSourceLink, TitleBarChromeXamlSource, TitleBarChromeCSharpSource);

            Loaded += GalleryWindowPage_Loaded;
            Unloaded += GalleryWindowPage_Unloaded;
        }

        private FluenceWindow HostFluenceWindow
        {
            get { return Window.GetWindow(this) as FluenceWindow; }
        }

        private void GalleryWindowPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GalleryWindowPage_Loaded;
            ApplicationThemeManager.Changed += ApplicationThemeManager_Changed;
            SyncThemeRadioButtons();
            SyncBackdropComboFromWindow();
            WindowChromeToggle_Changed(null, null);
            TitleBarToggle_Changed(null, null);
            CaptionVisibilityCombo_SelectionChanged(null, null);
        }

        private void GalleryWindowPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= GalleryWindowPage_Unloaded;
            ApplicationThemeManager.Changed -= ApplicationThemeManager_Changed;
        }

        private void ApplicationThemeManager_Changed(object sender, ThemeChangedEventArgs e)
        {
            SyncThemeRadioButtons();
            SyncBackdropComboFromWindow();
        }

        private void SyncBackdropComboFromWindow()
        {
            var fw = HostFluenceWindow;
            if (fw == null || BackdropCombo == null)
            {
                return;
            }

            _isSyncingBackdrop = true;
            try
            {
                int idx;
                switch (fw.SystemBackdropType)
                {
                    case BackdropType.None:
                        idx = 1;
                        break;
                    case BackdropType.Mica:
                        idx = 2;
                        break;
                    case BackdropType.Acrylic:
                        idx = 3;
                        break;
                    case BackdropType.Tabbed:
                        idx = 4;
                        break;
                    default:
                        idx = 0;
                        break;
                }

                BackdropCombo.SelectedIndex = idx;
            }
            finally
            {
                _isSyncingBackdrop = false;
            }
        }

        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || _isSyncingTheme)
            {
                return;
            }

            var rb = sender as System.Windows.Controls.RadioButton;
            if (rb == null)
            {
                return;
            }

            ApplicationTheme theme;
            if (ReferenceEquals(rb, ThemeLight))
            {
                theme = ApplicationTheme.Light;
            }
            else if (ReferenceEquals(rb, ThemeDark))
            {
                theme = ApplicationTheme.Dark;
            }
            else if (ReferenceEquals(rb, ThemeHighContrast))
            {
                theme = ApplicationTheme.HighContrast;
            }
            else
            {
                theme = ApplicationTheme.Auto;
            }

            var fw = HostFluenceWindow;
            var backdrop = fw != null ? fw.SystemBackdropType : BackdropType.Mica;
            ApplicationThemeManager.Apply(theme, backdrop, true);
            UpdateThemeStateLabel(ApplicationThemeManager.CurrentTheme);
        }

        private void BackdropCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _isSyncingBackdrop)
            {
                return;
            }

            var fw = HostFluenceWindow;
            if (fw == null)
            {
                return;
            }

            BackdropType backdrop;
            switch (BackdropCombo.SelectedIndex)
            {
                case 1:
                    backdrop = BackdropType.None;
                    break;
                case 2:
                    backdrop = BackdropType.Mica;
                    break;
                case 3:
                    backdrop = BackdropType.Acrylic;
                    break;
                case 4:
                    backdrop = BackdropType.Tabbed;
                    break;
                default:
                    backdrop = BackdropType.Auto;
                    break;
            }

            fw.SystemBackdropType = backdrop;
            ApplicationThemeManager.Apply(ApplicationThemeManager.CurrentTheme, backdrop, false);
        }

        private void AccentSwatch_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;
            if (button == null || button.Tag == null)
            {
                return;
            }

            var hex = button.Tag.ToString();
            try
            {
                var converted = ColorConverter.ConvertFromString(hex);
                if (converted != null)
                {
                    ApplicationAccentColorManager.ApplyCustomAccent((Color)converted);
                }
            }
            catch (FormatException)
            {
            }
        }

        private void SystemAccent_Click(object sender, RoutedEventArgs e)
        {
            ApplicationAccentColorManager.ApplySystemAccent();
        }

        private void SystemThemeWatcher_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var host = HostFluenceWindow;
            if (host == null)
            {
                return;
            }

            if (ThemeWatcherToggle.IsChecked == true)
            {
                SystemThemeWatcher.Watch(host);
                SystemThemeLabel.Text = "Watching: Yes";
            }
            else
            {
                SystemThemeWatcher.UnWatch(host);
                SystemThemeLabel.Text = "Watching: No";
            }
        }

        private void CaptionVisibilityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var fw = HostFluenceWindow;
            if (fw == null)
            {
                return;
            }

            ApplyCaptionVisibility(MinimizeVisibilityCombo, v => fw.IsMinimizeButtonVisible = v, en => fw.IsMinimizable = en);
            ApplyCaptionVisibility(MaximizeVisibilityCombo, v => fw.IsMaximizeButtonVisible = v, en => fw.IsMaximizable = en);
            ApplyCaptionVisibility(CloseVisibilityCombo, v => fw.IsCloseButtonVisible = v, en => fw.IsClosable = en);
        }

        private void WindowChromeToggle_Changed(object sender, RoutedEventArgs e)
        {
            var fw = HostFluenceWindow;
            if (fw == null)
            {
                return;
            }

            // Funnel through MainWindow helpers so the Top-pane + extended-chrome hide rule
            // (see MainWindow.ApplyTitleBarContentVisibility) stays authoritative for the
            // actual ShowIcon / ShowTitle DPs.
            var main = fw as MainWindow;

            if (ShowWindowTitleToggle != null)
            {
                bool show = ShowWindowTitleToggle.IsChecked == true;
                string title = show ? MainWindow.GalleryWindowTitle : string.Empty;
                if (main != null)
                {
                    main.SetUserShowTitle(show, title);
                }
                else
                {
                    fw.ShowTitle = show;
                    fw.Title = title;
                }
            }

            if (ShowWindowIconToggle != null)
            {
                bool show = ShowWindowIconToggle.IsChecked == true;
                ImageSource icon = show
                    ? new BitmapImage(new Uri("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/AppIcon.png"))
                    : null;
                if (main != null)
                {
                    main.SetUserShowIcon(show, icon);
                }
                else
                {
                    fw.ShowIcon = show;
                    fw.Icon = icon;
                }
            }
        }

        private void TitleBarToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var fw = HostFluenceWindow;
            if (fw == null)
            {
                return;
            }

            if (ExtendsContentToggle != null)
            {
                bool extends = ExtendsContentToggle.IsChecked == true;
                fw.ExtendsContentIntoTitleBar = extends;
                // When extending into the title bar and a Left / LeftCompact navigation
                // pane is present, push the icon + title past the compact rail so they do
                // not overlap the pane. 48 matches the compact rail width.
                fw.TitleBarLeftIndent = extends ? 48d : 0d;
            }

            if (HasShadowToggle != null)
            {
                fw.HasShadow = HasShadowToggle.IsChecked == true;
            }
        }

        private void TitleBarHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var fw = HostFluenceWindow;
            if (fw != null)
            {
                fw.TitleBarHeight = e.NewValue;
            }

            if (TitleBarHeightLabel != null)
            {
                TitleBarHeightLabel.Text = ((int)e.NewValue).ToString(CultureInfo.CurrentCulture);
            }
        }

        private void SyncThemeRadioButtons()
        {
            if (ThemeLight == null)
            {
                return;
            }

            _isSyncingTheme = true;
            try
            {
                var theme = ApplicationThemeManager.CurrentTheme;
                switch (theme)
                {
                    case ApplicationTheme.Light:
                        ThemeLight.IsChecked = true;
                        break;
                    case ApplicationTheme.Dark:
                        ThemeDark.IsChecked = true;
                        break;
                    case ApplicationTheme.HighContrast:
                        ThemeHighContrast.IsChecked = true;
                        break;
                    default:
                        ThemeAuto.IsChecked = true;
                        break;
                }

                UpdateThemeStateLabel(theme);
            }
            finally
            {
                _isSyncingTheme = false;
            }
        }

        private void UpdateThemeStateLabel(ApplicationTheme theme)
        {
            if (ThemeStateLabel != null)
            {
                ThemeStateLabel.Text = string.Format(CultureInfo.CurrentCulture, "Current: {0}", theme);
            }
        }

        private static void ApplyCaptionVisibility(
            System.Windows.Controls.ComboBox combo,
            Action<Visibility> setVisibility,
            Action<bool> setEnabled)
        {
            var item = combo != null ? combo.SelectedItem as ComboBoxItem : null;
            var content = item != null ? item.Content as string : null;

            if (string.Equals(content, "Hidden", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Hidden);
                setEnabled(false);
            }
            else if (string.Equals(content, "Collapsed", StringComparison.Ordinal) ||
                string.Equals(content, "Hide", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Collapsed);
                setEnabled(false);
            }
            else if (string.Equals(content, "Disable", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Visible);
                setEnabled(false);
            }
            else
            {
                setVisibility(Visibility.Visible);
                setEnabled(true);
            }
        }
    }
}