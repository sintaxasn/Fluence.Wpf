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
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryColorsPage : UserControl
    {
        private const string ColorSamplesXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Colors.ColorSamples""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <UserControl.Resources>
        <SolidColorBrush x:Key=""FluenceLogoRedBrush"" Color=""#EF5858"" />
        <SolidColorBrush x:Key=""FluenceLogoPeachBrush"" Color=""#F19263"" />
        <SolidColorBrush x:Key=""FluenceLogoGoldBrush"" Color=""#F6D65A"" />
        <SolidColorBrush x:Key=""FluenceLogoMintBrush"" Color=""#87C29C"" />
        <SolidColorBrush x:Key=""FluenceLogoSkyBrush"" Color=""#60A9E0"" />
        <SolidColorBrush x:Key=""FluenceLogoPeriwinkleBrush"" Color=""#839CD8"" />
        <SolidColorBrush x:Key=""FluenceLogoVioletBrush"" Color=""#8863C8"" />
    </UserControl.Resources>

    <ItemsControl>
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <WrapPanel />
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>

        <Border Width=""148"" Height=""148"" Margin=""0,0,8,8"" Background=""{StaticResource FluenceLogoRedBrush}"" />
        <Border Width=""148"" Height=""148"" Margin=""0,0,8,8"" Background=""{StaticResource FluenceLogoPeachBrush}"" />
        <Border Width=""148"" Height=""148"" Margin=""0,0,8,8"" Background=""{StaticResource FluenceLogoGoldBrush}"" />
        <Border Width=""148"" Height=""148"" Margin=""0,0,8,8"" Background=""{StaticResource FluenceLogoMintBrush}"" />
        <Border Width=""148"" Height=""148"" Margin=""0,0,8,8"" Background=""{StaticResource FluenceLogoSkyBrush}"" />
        <Border Width=""148"" Height=""148"" Margin=""0,0,8,8"" Background=""{StaticResource FluenceLogoPeriwinkleBrush}"" />
        <Border Width=""148"" Height=""148"" Margin=""0,0,8,8"" Background=""{StaticResource FluenceLogoVioletBrush}"" />
    </ItemsControl>
</UserControl>
";

        private const string ColorSamplesCSharpSource = @"/*
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
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ""AS IS""
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
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Colors
{
    public partial class ColorSamples : UserControl
    {
        public ColorSamples()
        {
            InitializeComponent();
        }
    }
}
";

        public GalleryColorsPage()
        {
            InitializeComponent();

            var parent = ColorSamplesContent.Parent as Panel;
            if (parent == null)
            {
                return;
            }

            var index = parent.Children.IndexOf(ColorSamplesContent);
            parent.Children.Remove(ColorSamplesContent);
            parent.Children.Insert(index, new DemoSampleControl
            {
                Title = "Logo palette",
                Description = "Seven swatches sampled from the Fluence.Wpf logo palette.",
                XamlSource = ColorSamplesXamlSource,
                CSharpSource = ColorSamplesCSharpSource,
                SampleContent = ColorSamplesContent
            });
        }
    }
}
