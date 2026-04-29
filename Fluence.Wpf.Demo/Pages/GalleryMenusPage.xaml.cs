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
    public partial class GalleryMenusPage : UserControl
    {
        public GalleryMenusPage()
        {
            InitializeComponent();

            MenuBarSourceLink.NavigateUri = DemoSourceLinkSettings.GetSourceUri("Menus/MenuBar.xaml");
            ContextMenuSourceLink.NavigateUri = DemoSourceLinkSettings.GetSourceUri("Menus/ContextMenuActions.xaml");
            ToolTipsSourceLink.NavigateUri = DemoSourceLinkSettings.GetSourceUri("Menus/ToolTips.xaml");
            DropDownAndSplitButtonsSourceLink.NavigateUri = DemoSourceLinkSettings.GetSourceUri("Menus/DropDownAndSplitButtonMenus.xaml");
        }

        private void MenuBar_Click(object sender, RoutedEventArgs e)
        {
            SetTextFromTag(MenuBarResultLabel, "Last menu action", sender);
        }

        private void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            SetTextFromTag(ContextMenuResultLabel, "Last action", sender);
        }

        private void ExportPrimary_Click(object sender, RoutedEventArgs e)
        {
            FlyoutResultLabel.Text = "Last action: Export - Default";
        }

        private void FlyoutAction_Click(object sender, RoutedEventArgs e)
        {
            SetTextFromTag(FlyoutResultLabel, "Last action", sender);
        }

        private static void SetTextFromTag(TextBlock label, string prefix, object sender)
        {
            var element = sender as FrameworkElement;
            var action = element != null ? element.Tag as string : null;
            label.Text = string.Format("{0}: {1}", prefix, string.IsNullOrEmpty(action) ? "None" : action);
        }
    }
}
