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

using FluenceTreeViewItem = Fluence.Wpf.Controls.TreeViewItem;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryTreesPage : UserControl
    {
        public GalleryTreesPage()
        {
            InitializeComponent();

            TreeViewHierarchySourceLink.NavigateUri = DemoSourceLinkSettings.GetSourceUri("Trees/TreeViewHierarchy.xaml");
            TreeViewSelectionSourceLink.NavigateUri = DemoSourceLinkSettings.GetSourceUri("Trees/TreeViewSelection.xaml");
            TreeViewExpansionSourceLink.NavigateUri = DemoSourceLinkSettings.GetSourceUri("Trees/TreeViewExpansion.xaml");
        }

        private void SelectionTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TreeSelectionLabel == null)
            {
                return;
            }

            var item = e.NewValue as FluenceTreeViewItem;
            if (item == null)
            {
                TreeSelectionLabel.Text = "Selected: -";
                return;
            }

            var path = BuildPath(item);
            TreeSelectionLabel.Text = string.Format("Selected: {0}", path);
        }

        private static string BuildPath(FluenceTreeViewItem item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            var header = item.Header as string ?? string.Empty;
            var parent = ItemsControl.ItemsControlFromItemContainer(item) as FluenceTreeViewItem;
            if (parent == null)
            {
                return header;
            }

            var parentPath = BuildPath(parent);
            return string.IsNullOrEmpty(parentPath)
                ? header
                : string.Format("{0} / {1}", parentPath, header);
        }

        private void ExpandAll_Click(object sender, RoutedEventArgs e)
        {
            if (ExpansionTreeView == null)
            {
                return;
            }

            SetExpanded(ExpansionTreeView.Items, true);
        }

        private void CollapseAll_Click(object sender, RoutedEventArgs e)
        {
            if (ExpansionTreeView == null)
            {
                return;
            }

            SetExpanded(ExpansionTreeView.Items, false);
        }

        private static void SetExpanded(ItemCollection items, bool expanded)
        {
            foreach (var obj in items)
            {
                var tvi = obj as FluenceTreeViewItem;
                if (tvi == null)
                {
                    continue;
                }

                tvi.IsExpanded = expanded;
                if (tvi.Items.Count > 0)
                {
                    SetExpanded(tvi.Items, expanded);
                }
            }
        }
    }
}
