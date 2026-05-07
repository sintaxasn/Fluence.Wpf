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

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FluencePasswordBox = Fluence.Wpf.Controls.PasswordBox;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryFormsPage : UserControl
    {
        private const string SignInFormXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Forms.SignInForm""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel MaxWidth=""480"" HorizontalAlignment=""Left"">
        <TextBlock Margin=""0,0,0,4"" Text=""Email"" />
        <ui:TextBox
            Margin=""0,0,0,12""
            PlaceholderText=""name@example.com"" />
        <TextBlock Margin=""0,0,0,4"" Text=""Password"" />
        <ui:PasswordBox
            Margin=""0,0,0,12""
            PlaceholderText=""Password""
            RevealButtonEnabled=""True"" />
        <ui:CheckBox
            Margin=""0,0,0,16""
            Content=""Remember me"" />
        <StackPanel Orientation=""Horizontal"">
            <ui:Button
                Margin=""0,0,8,0""
                Appearance=""Accent""
                Content=""Sign in"" />
            <ui:Button Content=""Create account"" />
        </StackPanel>
    </StackPanel>
</UserControl>
";

        private const string SignInFormCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Forms
{
    public partial class SignInForm : UserControl
    {
        public SignInForm()
        {
            InitializeComponent();
        }
    }
}
";
        private const string CheckoutFormXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Forms.CheckoutForm""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel MaxWidth=""560"" HorizontalAlignment=""Left"">
        <TextBlock Margin=""0,0,0,4"" Text=""Contact email"" />
        <ui:TextBox
            Margin=""0,0,0,12""
            PlaceholderText=""name@example.com"" />
        <TextBlock Margin=""0,0,0,4"" Text=""Shipping speed"" />
        <ui:ComboBox
            Margin=""0,0,0,12""
            SelectedIndex=""1"">
            <ComboBoxItem Content=""Standard"" />
            <ComboBoxItem Content=""Priority"" />
            <ComboBoxItem Content=""Overnight"" />
        </ui:ComboBox>
        <WrapPanel Margin=""0,0,0,12"">
            <ui:NumberBox
                Width=""180""
                Margin=""0,0,16,12""
                Header=""Quantity""
                Maximum=""10""
                Minimum=""1""
                SpinButtonPlacementMode=""Compact""
                Value=""2"" />
            <ui:TextBox
                Width=""280""
                Margin=""0,0,0,12""
                PlaceholderText=""Optional"" />
        </WrapPanel>
        <ui:CheckBox
            Margin=""0,0,0,16""
            Content=""This is a gift""
            Description=""Hide prices on the packing slip."" />
        <StackPanel Orientation=""Horizontal"">
            <ui:Button
                Margin=""0,0,8,0""
                Appearance=""Accent""
                Content=""Place order"" />
            <ui:Button Content=""Save for later"" />
        </StackPanel>
    </StackPanel>
</UserControl>
";

        private const string CheckoutFormCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Forms
{
    public partial class CheckoutForm : UserControl
    {
        public CheckoutForm()
        {
            InitializeComponent();
        }
    }
}
";
        private const string SettingsFormXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Forms.SettingsForm""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel MaxWidth=""560"" HorizontalAlignment=""Left"">
        <TextBlock Margin=""0,0,0,4"" Text=""Display name"" />
        <ui:TextBox
            Margin=""0,0,0,12""
            Text=""Avery Stone"" />
        <TextBlock Margin=""0,0,0,4"" Text=""Theme"" />
        <ui:ComboBox
            Margin=""0,0,0,12""
            SelectedIndex=""0"">
            <ComboBoxItem Content=""Use system setting"" />
            <ComboBoxItem Content=""Light"" />
            <ComboBoxItem Content=""Dark"" />
        </ui:ComboBox>
        <ui:ToggleSwitch
            Margin=""0,0,0,16""
            Content=""Email updates""
            IsChecked=""True""
            OffContent=""Off""
            OnContent=""On"" />
        <ui:Button
            HorizontalAlignment=""Left""
            Appearance=""Accent""
            Content=""Save settings"" />
    </StackPanel>
</UserControl>
";

        private const string SettingsFormCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Forms
{
    public partial class SettingsForm : UserControl
    {
        public SettingsForm()
        {
            InitializeComponent();
        }
    }
}
";

        public GalleryFormsPage()
        {
            InitializeComponent();

            _ = DemoSampleControl.ReplaceSourceLink(SignInFormSourceLink, SignInFormXamlSource, SignInFormCSharpSource);
            _ = DemoSampleControl.ReplaceSourceLink(CheckoutFormSourceLink, CheckoutFormXamlSource, CheckoutFormCSharpSource);
            _ = DemoSampleControl.ReplaceSourceLink(SettingsFormSourceLink, SettingsFormXamlSource, SettingsFormCSharpSource);

            Loaded += GalleryFormsPage_Loaded;
        }

        private void GalleryFormsPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GalleryFormsPage_Loaded;
            DependencyPropertyDescriptor
                .FromProperty(FluencePasswordBox.PasswordProperty, typeof(FluencePasswordBox))
                .AddValueChanged(SignInPasswordBox, SignInPassword_Changed);
        }

        private void SignInField_Changed(object? sender, RoutedEventArgs e)
        {
            if (SignInButton is null || SignInEmailBox is null || SignInPasswordBox is null)
            {
                return;
            }

            SignInButton.IsEnabled =
                !string.IsNullOrWhiteSpace(SignInEmailBox.Text) &&
                !string.IsNullOrWhiteSpace(SignInPasswordBox.Password);
        }

        private void SignInPassword_Changed(object? sender, System.EventArgs e)
        {
            SignInField_Changed(sender, new RoutedEventArgs());
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            _ = (SignInStatusBar?.IsOpen = true);
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _ = (SettingsSavedBar?.IsOpen = true);
        }
    }
}
