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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryDataPage : UserControl
    {
        private int _addCounter;

        private static readonly string[] SampleNames = { "Liam Torres", "Nora Fischer", "Eli Nakamura", "Priya Kapoor", "Dante Reeves", "Clara Johansson", "Jasper Cheng", "Mila Petrov" };
        private static readonly string[] SampleTitles = { "Frontend Engineer", "QA Specialist", "Solutions Architect", "Product Manager", "Cloud Engineer", "Technical Writer", "SRE Lead", "Mobile Developer" };

        public GalleryDataPage()
        {
            InitializeComponent();
            Loaded += GalleryDataPage_Loaded;
        }

        private void GalleryDataPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GalleryDataPage_Loaded;
            UpdateFontDetectionLabel();
        }

        private void AddListItem_Click(object sender, RoutedEventArgs e)
        {
            if (DemoListView == null)
            {
                return;
            }

            int idx = _addCounter % SampleNames.Length;
            string name = SampleNames[idx];
            string title = SampleTitles[idx];
            _addCounter++;

            DemoListView.Items.Add(new ListViewItem { Content = name });

            if (DemoListViewDetail != null)
            {
                var item = new ListViewItem();
                item.Tag = string.Format("{0}|{1}", name, title);
                item.Content = BuildDetailListItemContent(name, title);
                DemoListViewDetail.Items.Add(item);
            }
        }

        private void RemoveListItem_Click(object sender, RoutedEventArgs e)
        {
            if (DemoListView == null || DemoListView.Items.Count == 0)
            {
                return;
            }

            var lastItem = DemoListView.Items[DemoListView.Items.Count - 1];
            DemoListView.AnimateRemove(lastItem, null);

            if (DemoListViewDetail != null && DemoListViewDetail.Items.Count > 0)
            {
                var detailItem = DemoListViewDetail.Items[DemoListViewDetail.Items.Count - 1];
                DemoListViewDetail.AnimateRemove(detailItem, null);
            }
        }

        private void FontIconDemo_Click(object sender, RoutedEventArgs e)
        {
            if (ClickableFontIcon == null)
            {
                return;
            }

            var animation = new DoubleAnimation
            {
                By = 180,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            ClickableFontIcon.BeginAnimation(FontIcon.RotationProperty, animation);
        }

        private void UpdateFontDetectionLabel()
        {
            if (FontDetectionLabel == null)
            {
                return;
            }

            var w = Window.GetWindow(this);
            FontDetectionLabel.Text = w != null && w.FontFamily != null
                ? string.Format("FontFamily: {0}", w.FontFamily.Source)
                : "FontFamily: (unknown)";
        }

        private static UIElement BuildDetailListItemContent(string header, string subtitle)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var icon = new FontIcon { Glyph = "\uE77B", IconFontSize = 20, VerticalAlignment = VerticalAlignment.Center };
            icon.SetResourceReference(FontIcon.ForegroundProperty, "TextFillColorSecondaryBrush");
            Grid.SetColumn(icon, 0);

            var stack = new System.Windows.Controls.StackPanel();
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = header, FontWeight = FontWeights.SemiBold });
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = subtitle, FontSize = 12 });
            Grid.SetColumn(stack, 1);

            grid.Children.Add(icon);
            grid.Children.Add(stack);
            return grid;
        }
    }
}
