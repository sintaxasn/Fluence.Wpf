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
    <StackPanel>
        <TextBlock
            Margin=""0,0,0,12""
            FontSize=""32""
            Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
            Text=""Text brushes"" />
        <UniformGrid Margin=""0,0,0,24"" Columns=""4"">
            <Border
                MinHeight=""92""
                Margin=""0,0,4,4""
                Padding=""12""
                Background=""{DynamicResource TextFillColorPrimaryBrush}"">
                <TextBlock
                    Foreground=""{DynamicResource TextFillColorInverseBrush}""
                    Text=""TextFillColorPrimaryBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""92""
                Margin=""4,0,4,4""
                Padding=""12""
                Background=""{DynamicResource TextFillColorSecondaryBrush}"">
                <TextBlock
                    Foreground=""{DynamicResource TextFillColorInverseBrush}""
                    Text=""TextFillColorSecondaryBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""92""
                Margin=""4,0,4,4""
                Padding=""12""
                Background=""{DynamicResource AccentTextFillColorPrimaryBrush}"">
                <TextBlock
                    Foreground=""White""
                    Text=""AccentTextFillColorPrimaryBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""92""
                Margin=""4,0,0,4""
                Padding=""12""
                Background=""{DynamicResource AccentFillColorDefaultBrush}"">
                <TextBlock
                    Foreground=""{DynamicResource TextOnAccentFillColorPrimaryBrush}""
                    Text=""TextOnAccentFillColorPrimaryBrush""
                    TextWrapping=""Wrap"" />
            </Border>
        </UniformGrid>

        <TextBlock
            Margin=""0,0,0,12""
            FontSize=""32""
            Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
            Text=""Fills and surfaces"" />
        <UniformGrid Margin=""0,0,0,24"" Columns=""3"">
            <Border
                MinHeight=""96""
                Margin=""0,0,4,4""
                Padding=""12""
                Background=""{DynamicResource ControlFillColorDefaultBrush}"">
                <TextBlock
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""ControlFillColorDefaultBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""96""
                Margin=""4,0,4,4""
                Padding=""12""
                Background=""{DynamicResource ControlAltFillColorSecondaryBrush}"">
                <TextBlock
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""ControlAltFillColorSecondaryBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""96""
                Margin=""4,0,0,4""
                Padding=""12""
                Background=""{DynamicResource ControlSolidFillColorDefaultBrush}"">
                <TextBlock
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""ControlSolidFillColorDefaultBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""96""
                Margin=""0,4,4,0""
                Padding=""12""
                Background=""{DynamicResource CardBackgroundFillColorDefaultBrush}"">
                <TextBlock
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""CardBackgroundFillColorDefaultBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""96""
                Margin=""4,4,4,0""
                Padding=""12""
                Background=""{DynamicResource LayerFillColorDefaultBrush}"">
                <TextBlock
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""LayerFillColorDefaultBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""96""
                Margin=""4,4,0,0""
                Padding=""12""
                Background=""{DynamicResource SolidBackgroundFillColorBaseBrush}"">
                <TextBlock
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""SolidBackgroundFillColorBaseBrush""
                    TextWrapping=""Wrap"" />
            </Border>
        </UniformGrid>

        <TextBlock
            Margin=""0,0,0,12""
            FontSize=""32""
            Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
            Text=""Strokes"" />
        <UniformGrid Margin=""0,0,0,24"" Columns=""3"">
            <Border
                MinHeight=""96""
                Margin=""0,0,4,0""
                Padding=""12""
                BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
                BorderThickness=""3"">
                <TextBlock
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""CardStrokeColorDefaultBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""96""
                Margin=""4,0,4,0""
                Padding=""12""
                BorderBrush=""{DynamicResource ControlStrokeColorDefaultBrush}""
                BorderThickness=""3"">
                <TextBlock
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""ControlStrokeColorDefaultBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""96""
                Margin=""4,0,0,0""
                Padding=""12""
                BorderBrush=""{DynamicResource FocusStrokeColorOuterBrush}""
                BorderThickness=""3"">
                <TextBlock
                    Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                    Text=""FocusStrokeColorOuterBrush""
                    TextWrapping=""Wrap"" />
            </Border>
        </UniformGrid>

        <TextBlock
            Margin=""0,0,0,12""
            FontSize=""32""
            Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
            Text=""System colors"" />
        <UniformGrid Columns=""3"">
            <Border
                MinHeight=""96""
                Margin=""0,0,4,4""
                Padding=""12""
                Background=""{DynamicResource SystemFillColorSuccessBrush}"">
                <TextBlock
                    Foreground=""White""
                    Text=""SystemFillColorSuccessBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""96""
                Margin=""4,0,4,4""
                Padding=""12""
                Background=""{DynamicResource SystemFillColorCautionBrush}"">
                <TextBlock
                    Foreground=""White""
                    Text=""SystemFillColorCautionBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""96""
                Margin=""4,0,0,4""
                Padding=""12""
                Background=""{DynamicResource SystemFillColorCriticalBrush}"">
                <TextBlock
                    Foreground=""White""
                    Text=""SystemFillColorCriticalBrush""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""96""
                Margin=""0,4,4,0""
                Padding=""12""
                Background=""{DynamicResource {x:Static SystemColors.WindowBrushKey}}"">
                <TextBlock
                    Foreground=""{DynamicResource {x:Static SystemColors.WindowTextBrushKey}}""
                    Text=""SystemColors.WindowBrushKey""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""96""
                Margin=""4,4,4,0""
                Padding=""12""
                Background=""{DynamicResource {x:Static SystemColors.HighlightBrushKey}}"">
                <TextBlock
                    Foreground=""{DynamicResource {x:Static SystemColors.HighlightTextBrushKey}}""
                    Text=""SystemColors.HighlightBrushKey""
                    TextWrapping=""Wrap"" />
            </Border>
            <Border
                MinHeight=""96""
                Margin=""4,4,0,0""
                Padding=""12""
                Background=""{DynamicResource {x:Static SystemColors.ControlBrushKey}}"">
                <TextBlock
                    Foreground=""{DynamicResource {x:Static SystemColors.ControlTextBrushKey}}""
                    Text=""SystemColors.ControlBrushKey""
                    TextWrapping=""Wrap"" />
            </Border>
        </UniformGrid>
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
                Title = "Color resources",
                Description = "Theme brushes and accent resources available to Fluence controls.",
                XamlSource = ColorSamplesXamlSource,
                CSharpSource = ColorSamplesCSharpSource,
                SampleContent = ColorSamplesContent
            });
        }
    }
}