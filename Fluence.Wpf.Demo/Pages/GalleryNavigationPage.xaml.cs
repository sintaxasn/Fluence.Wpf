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
    public partial class GalleryNavigationPage : UserControl
    {
        public GalleryNavigationPage()
        {
            InitializeComponent();
            Loaded += GalleryNavigationPage_Loaded;
        }

        private void GalleryNavigationPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GalleryNavigationPage_Loaded;
            InlineBackEnabledToggle_Changed(null, null);
        }

        private void InlineNavTopDemo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InlineNavTopDemo == null || InlineNavTopLabel == null || InlineNavTopContentHost == null)
            {
                return;
            }

            var item = TryGetSelectedNavigationViewItem(InlineNavTopDemo);
            var selectedLabel = item != null ? item.Content as string : "Home";
            InlineNavTopLabel.Text = string.Format("Top: {0}", selectedLabel);
        }

        private void InlineNavLeftDemo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InlineNavLeftDemo == null || InlineNavLeftLabel == null || InlineNavLeftContentHost == null)
            {
                return;
            }

            var item = TryGetSelectedNavigationViewItem(InlineNavLeftDemo);
            var selectedLabel = item != null ? item.Content as string : "Files";
            InlineNavLeftLabel.Text = string.Format("Left: {0}", selectedLabel);
        }

        private static NavigationViewItem TryGetSelectedNavigationViewItem(NavigationView nav)
        {
            if (nav == null)
            {
                return null;
            }

            var direct = nav.SelectedItem as NavigationViewItem;
            if (direct != null)
            {
                return direct;
            }

            if (nav.SelectedItem == null)
            {
                return null;
            }

            var container = nav.ItemContainerGenerator.ContainerFromItem(nav.SelectedItem) as NavigationViewItem;
            return container;
        }

        private void InlineBackEnabledToggle_Changed(object sender, RoutedEventArgs e)
        {
            var enabled = InlineBackEnabledToggle != null && InlineBackEnabledToggle.IsChecked == true;

            if (InlineNavTopDemo != null)
            {
                InlineNavTopDemo.IsBackEnabled = enabled;
            }

            if (InlineNavLeftDemo != null)
            {
                InlineNavLeftDemo.IsBackEnabled = enabled;
            }

            if (InlineBackStatusLabel != null)
            {
                InlineBackStatusLabel.Text = enabled ? "Back button enabled" : "Back button disabled";
            }
        }
    }
}
