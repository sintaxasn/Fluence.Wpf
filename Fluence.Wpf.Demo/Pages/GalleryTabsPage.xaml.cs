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
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryTabsPage : UserControl
    {
        private int _nextDocumentNumber = 4;

        public GalleryTabsPage()
        {
            InitializeComponent();
        }

        private void DemoTabView_AddTabButtonClick(object sender, RoutedEventArgs e)
        {
            if (DemoTabView == null)
            {
                return;
            }

            int number = _nextDocumentNumber++;
            var icon = new FontIcon { Glyph = "\uE8A5", IconFontSize = 16 };
            var body = new System.Windows.Controls.TextBlock
            {
                Margin = new Thickness(16),
                Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorSecondaryBrush"),
                Text = string.Format("Fresh document {0} content.", number),
                TextWrapping = TextWrapping.Wrap
            };

            var tab = new TabViewItem
            {
                Header = string.Format("Document {0}", number),
                Icon = icon,
                Content = body
            };

            DemoTabView.Items.Add(tab);
            DemoTabView.SelectedItem = tab;
            UpdateStatus();
        }

        private void DemoTabView_TabCloseRequested(object sender, RoutedEventArgs e)
        {
            var args = e as TabViewTabCloseRequestedEventArgs;
            if (args == null || DemoTabView == null || args.Tab == null)
            {
                return;
            }

            DemoTabView.Items.Remove(args.Tab);
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (DemoTabViewStatus == null || DemoTabView == null)
            {
                return;
            }

            DemoTabViewStatus.Text = string.Format("Tabs: {0}", DemoTabView.Items.Count);
        }
    }
}
