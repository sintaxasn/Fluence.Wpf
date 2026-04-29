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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryCategoryPage : UserControl
    {
        public GalleryCategoryPage(string title, string description, IEnumerable<DemoNavigationItem> items)
        {
            InitializeComponent();

            CategoryPageTitle.Text = title;
            CategoryPageDescription.Text = description;

            foreach (var item in items)
            {
                AddCard(item);
            }
        }

        private void AddCard(DemoNavigationItem item)
        {
            var card = new Card
            {
                Margin = new Thickness(0, 0, 12, 12),
                Padding = new Thickness(16),
                IsClickable = true,
                Tag = item.Title,
                Variant = CardVariant.Default
            };
            card.Click += Card_Click;

            var title = new System.Windows.Controls.TextBlock
            {
                Text = item.Title,
                TextWrapping = TextWrapping.Wrap
            };
            title.SetResourceReference(FrameworkElement.StyleProperty, "BodyStrongTextBlockStyle");
            card.Content = title;

            CategoryCardsGrid.Children.Add(card);
        }

        private void Card_Click(object sender, RoutedEventArgs e)
        {
            var card = sender as Card;
            if (card == null)
            {
                return;
            }

            var tag = card.Tag as string;
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }

            var host = Window.GetWindow(this) as MainWindow;
            if (host != null)
            {
                host.NavigateTo(tag);
            }
        }
    }
}
