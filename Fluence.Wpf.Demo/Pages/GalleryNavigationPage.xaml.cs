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

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryNavigationPage : UserControl
    {
        private int _backRequestCount;

        public GalleryNavigationPage()
        {
            InitializeComponent();

            LeftNavigationViewSourceLink.NavigateUri = DemoSourceLinkSettings.GetSourceUri("Navigation/LeftNavigationView.xaml");
            TopNavigationViewSourceLink.NavigateUri = DemoSourceLinkSettings.GetSourceUri("Navigation/TopNavigationView.xaml");
            CompactNavigationViewSourceLink.NavigateUri = DemoSourceLinkSettings.GetSourceUri("Navigation/CompactNavigationView.xaml");

            Loaded += GalleryNavigationPage_Loaded;
        }

        private void GalleryNavigationPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GalleryNavigationPage_Loaded;

            LeftNavigationDemo.SelectedItem = LeftNavigationHomeItem;
            TopNavigationDemo.SelectedItem = TopNavigationOverviewItem;
            CompactNavigationDemo.SelectedItem = CompactNavigationDashboardItem;
            UpdateBackState();
        }

        private void BackEnabledToggle_Changed(object sender, RoutedEventArgs e)
        {
            UpdateBackState();
        }

        private void CompactNavigationDemo_BackRequested(object sender, Fluence.Wpf.Controls.NavigationViewBackRequestedEventArgs e)
        {
            _backRequestCount++;
            UpdateBackState();
        }

        private void UpdateBackState()
        {
            var isBackEnabled = BackEnabledToggle != null && BackEnabledToggle.IsChecked == true;

            if (CompactNavigationDemo != null)
            {
                CompactNavigationDemo.IsBackEnabled = isBackEnabled;
            }

            if (BackStatusLabel != null)
            {
                BackStatusLabel.Text = isBackEnabled
                    ? string.Format("Back button enabled ({0} requests)", _backRequestCount)
                    : "Back button disabled";
            }
        }
    }
}
