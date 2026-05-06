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
    public partial class GalleryLayoutPage : UserControl
    {
        private const string LayoutXamlSource = @"<ui:Border
    Padding=""14""
    Background=""{DynamicResource CardBackgroundFillColorSecondaryBrush}""
    BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
    BorderThickness=""1""
    CornerRadius=""8"">
    <ui:StackPanel Spacing=""10"">
        <TextBlock Style=""{StaticResource BodyStrongTextBlockStyle}""
                   Text=""Settings group"" />
        <ui:Separator />
        <DockPanel LastChildFill=""True"">
            <ui:Button DockPanel.Dock=""Right""
                       Appearance=""Accent""
                       Content=""Apply"" />
            <TextBlock VerticalAlignment=""Center""
                       Text=""DockPanel keeps the command aligned."" />
        </DockPanel>
    </ui:StackPanel>
</ui:Border>

<ui:Expander Header=""Advanced options"" IsExpanded=""True"">
    <TextBlock Text=""Expander shows secondary settings only when useful.""
               TextWrapping=""Wrap"" />
</ui:Expander>";

        public GalleryLayoutPage()
        {
            InitializeComponent();
            _ = DemoSampleControl.ReplaceSourceLink(LayoutSourceLink, LayoutXamlSource, string.Empty);
        }
    }
}
