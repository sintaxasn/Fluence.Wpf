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

using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GallerySelectionPage : UserControl
    {
        private const string CheckBoxStatesXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Selection.CheckBoxStates""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <WrapPanel Margin=""0,0,0,12"">
            <ui:CheckBox
                Margin=""0,0,32,10""
                Content=""Checked""
                IsChecked=""True"" />
            <ui:CheckBox Margin=""0,0,32,10"" Content=""Unchecked"" />
            <ui:CheckBox
                Margin=""0,0,32,10""
                Content=""Indeterminate""
                IsChecked=""{x:Null}""
                IsThreeState=""True"" />
            <ui:CheckBox
                Margin=""0,0,32,10""
                Content=""Disabled""
                IsChecked=""True""
                IsEnabled=""False"" />
        </WrapPanel>
        <ui:CheckBox
            Content=""With description""
            Description=""Additional detail about this option"" />
    </StackPanel>
</UserControl>
";

        private const string CheckBoxStatesCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Selection
{
    public partial class CheckBoxStates : UserControl
    {
        public CheckBoxStates()
        {
            InitializeComponent();
        }
    }
}
";
        private const string RadioButtonGroupsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Selection.RadioButtonGroups""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <TextBlock
            Margin=""0,0,0,8""
            FontWeight=""SemiBold""
            Text=""Basic group"" />
        <StackPanel Margin=""0,0,0,16"" Orientation=""Horizontal"">
            <ui:RadioButton
                Margin=""0,0,16,0""
                Content=""Option A""
                GroupName=""BasicGroup""
                IsChecked=""True"" />
            <ui:RadioButton
                Margin=""0,0,16,0""
                Content=""Option B""
                GroupName=""BasicGroup"" />
            <ui:RadioButton Content=""Option C"" GroupName=""BasicGroup"" />
        </StackPanel>
        <TextBlock
            Margin=""0,0,0,8""
            FontWeight=""SemiBold""
            Text=""With descriptions"" />
        <ui:RadioButton
            Margin=""0,0,0,8""
            Content=""Standard""
            Description=""Uses default application settings""
            GroupName=""DescGroup""
            IsChecked=""True"" />
        <ui:RadioButton
            Margin=""0,0,0,8""
            Content=""Custom""
            Description=""Allows manual configuration""
            GroupName=""DescGroup"" />
        <ui:RadioButton
            Content=""Advanced""
            Description=""Expert-level options""
            GroupName=""DescGroup"" />
    </StackPanel>
</UserControl>
";

        private const string RadioButtonGroupsCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Selection
{
    public partial class RadioButtonGroups : UserControl
    {
        public RadioButtonGroups()
        {
            InitializeComponent();
        }
    }
}
";
        private const string ToggleSwitchStatesXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Selection.ToggleSwitchStates""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <ui:StackPanel Spacing=""18"">
        <ui:StackPanel Spacing=""10"">
            <TextBlock Text=""Simple switch"" />
            <ui:StackPanel Spacing=""12"" Orientation=""Horizontal"">
                <ui:ToggleSwitch VerticalAlignment=""Center"" />
                <TextBlock VerticalAlignment=""Center"" Text=""Notifications"" />
            </ui:StackPanel>
        </ui:StackPanel>
        <ui:StackPanel Spacing=""10"">
            <TextBlock Text=""Custom content"" />
            <ui:StackPanel Spacing=""12"" Orientation=""Horizontal"">
                <ui:ToggleSwitch
                    VerticalAlignment=""Center""
                    IsChecked=""True""
                    OffContent=""Off""
                    OnContent=""On"" />
                <TextBlock VerticalAlignment=""Center"" Text=""Cloud sync"" />
            </ui:StackPanel>
        </ui:StackPanel>
        <ui:StackPanel Spacing=""10"">
            <TextBlock Text=""Disabled"" />
            <ui:StackPanel Spacing=""12"" Orientation=""Horizontal"">
                <ui:ToggleSwitch
                    VerticalAlignment=""Center""
                    IsChecked=""True""
                    IsEnabled=""False"" />
                <TextBlock VerticalAlignment=""Center"" Text=""Disabled (on)"" />
            </ui:StackPanel>
        </ui:StackPanel>
    </ui:StackPanel>
</UserControl>
";

        private const string ToggleSwitchStatesCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Selection
{
    public partial class ToggleSwitchStates : UserControl
    {
        public ToggleSwitchStates()
        {
            InitializeComponent();
        }
    }
}
";
        private const string ComboBoxSelectionXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Selection.ComboBoxSelection""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <ui:ComboBox
            Width=""480""
            Margin=""0,0,0,12""
            HorizontalAlignment=""Left""
            PlaceholderText=""Choose an option...""
            SelectedIndex=""-1"">
            <ComboBoxItem Content=""First item"" />
            <ComboBoxItem Content=""Second item"" />
            <ComboBoxItem Content=""Third item"" />
        </ui:ComboBox>
        <ui:ComboBox
            Width=""480""
            Margin=""0,0,0,12""
            HorizontalAlignment=""Left""
            PlaceholderText=""With icon""
            SelectedIndex=""-1"">
            <ui:ComboBox.Icon>
                <ui:FontIcon Glyph=""&#xE721;"" IconFontSize=""14"" />
            </ui:ComboBox.Icon>
            <ComboBoxItem Content=""Alpha"" />
            <ComboBoxItem Content=""Beta"" />
            <ComboBoxItem Content=""Gamma"" />
        </ui:ComboBox>
        <ui:ComboBox
            Width=""480""
            HorizontalAlignment=""Left""
            IsEnabled=""False""
            PlaceholderText=""Disabled"" />
    </StackPanel>
</UserControl>
";

        private const string ComboBoxSelectionCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Selection
{
    public partial class ComboBoxSelection : UserControl
    {
        public ComboBoxSelection()
        {
            InitializeComponent();
        }
    }
}
";

        public GallerySelectionPage()
        {
            InitializeComponent();

            _ = DemoSampleControl.ReplaceSourceLink(CheckBoxStatesSourceLink, CheckBoxStatesXamlSource, CheckBoxStatesCSharpSource);
            _ = DemoSampleControl.ReplaceSourceLink(RadioButtonGroupsSourceLink, RadioButtonGroupsXamlSource, RadioButtonGroupsCSharpSource);
            _ = DemoSampleControl.ReplaceSourceLink(ToggleSwitchStatesSourceLink, ToggleSwitchStatesXamlSource, ToggleSwitchStatesCSharpSource);
            _ = DemoSampleControl.ReplaceSourceLink(ComboBoxSelectionSourceLink, ComboBoxSelectionXamlSource, ComboBoxSelectionCSharpSource);

            Loaded += GallerySelectionPage_Loaded;
        }

        private void GallerySelectionPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GallerySelectionPage_Loaded;
            DefaultToggle_Changed(null, null);
        }

        private void DefaultToggle_Changed(object? sender, RoutedEventArgs? e)
        {
            if (ToggleStateLabel is null || DefaultToggle is null)
            {
                return;
            }

            ToggleStateLabel.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Default toggle: {0}",
                DefaultToggle.IsChecked == true ? "On" : "Off");
        }

        private void SelectionDemoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboStateLabel is null || SelectionDemoCombo is null)
            {
                return;
            }

            ComboStateLabel.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Selected: {0}",
                SelectionDemoCombo.SelectedItem is ComboBoxItem selectedItem ? selectedItem.Content : "none");
        }
    }
}
