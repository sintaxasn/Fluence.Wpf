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
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:color=""clr-namespace:Fluence.Wpf.Demo.Pages.ColorGuidance""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <color:ColorPageExample
            Title=""Text""
            Description=""Semantic text resources update with theme and high contrast mode."">
            <TextBlock
                Style=""{StaticResource DisplayTextBlockStyle}""
                Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                Text=""Aa"" />
        </color:ColorPageExample>

        <WrapPanel Style=""{StaticResource ColorTileWrapPanelStyle}"">
            <color:ColorTile
                Style=""{StaticResource ColorTileStyle}""
                Title=""Text / Primary""
                Description=""Primary content""
                BrushResourceKey=""TextFillColorPrimaryBrush""
                ForegroundResourceKey=""TextFillColorInverseBrush""
                ResourceKey=""TextFillColorPrimaryBrush"" />
            <color:ColorTile
                Style=""{StaticResource ColorTileStyle}""
                Title=""Accent / Default""
                Description=""Primary accent fill""
                BrushResourceKey=""AccentFillColorDefaultBrush""
                ForegroundResourceKey=""TextOnAccentFillColorPrimaryBrush""
                ResourceKey=""AccentFillColorDefaultBrush"" />
            <color:ColorTile
                Style=""{StaticResource ColorTileStyle}""
                Title=""Window Text Color""
                Description=""High contrast foreground""
                BrushResourceKey=""SystemColorWindowTextColorBrush""
                ForegroundResourceKey=""SystemColorWindowColorBrush""
                ResourceKey=""SystemColorWindowTextColorBrush"" />
        </WrapPanel>
    </StackPanel>
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

            if (ColorSamplesContent.Parent is not Panel parent)
            {
                return;
            }

            int index = parent.Children.IndexOf(ColorSamplesContent);
            parent.Children.Remove(ColorSamplesContent);
            parent.Children.Insert(index, new DemoSampleControl
            {
                SampleDescription = "Theme brushes and accent resources available to Fluence controls.",
                XamlSource = ColorSamplesXamlSource,
                CSharpSource = ColorSamplesCSharpSource,
                DemoContent = ColorSamplesContent
            });
        }
    }
}
