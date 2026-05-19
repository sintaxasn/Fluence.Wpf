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

using System.Windows;
using System.Windows.Controls;
using FluenceToggleButton = Fluence.Wpf.Controls.ToggleButton;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryColorsPage : UserControl
    {
        private const string SampleMarkup = "<TextBlock Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\" />";

        public GalleryColorsPage()
        {
            InitializeComponent();
            CopyCodeSampleButton.Tag = SampleMarkup;
            BuildSectionSelector();
            SectionContentHost.Content = CreatePlaceholderSection();
        }

        private void BuildSectionSelector()
        {
            string[] sectionNames =
            [
                "Text",
                "Fill",
                "Stroke",
                "Background",
                "Signal",
                "High Contrast"
            ];

            for (int i = 0; i < sectionNames.Length; i++)
            {
                FluenceToggleButton button = new()
                {
                    Content = sectionNames[i],
                    CornerRadius = new CornerRadius(16),
                    IsChecked = i == 0,
                    MinWidth = 0,
                    Padding = new Thickness(23, 5, 23, 6),
                    Tag = sectionNames[i]
                };
                button.Click += SectionButton_Click;
                _ = SectionSelectorHost.Children.Add(button);
            }
        }

        private static UIElement CreatePlaceholderSection()
        {
            TextBlock textBlock = new()
            {
                Text = "Color token sections are loading.",
                TextWrapping = TextWrapping.Wrap
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            return textBlock;
        }

        private void SectionButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (object child in SectionSelectorHost.Children)
            {
                if (child is FluenceToggleButton button)
                {
                    button.IsChecked = ReferenceEquals(button, sender);
                }
            }
        }

        private void CopyCodeSampleButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(SampleMarkup);
        }
    }
}
