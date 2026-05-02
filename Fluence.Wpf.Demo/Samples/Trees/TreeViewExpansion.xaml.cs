using System.Windows;
using System.Windows.Controls;

using FluenceTreeViewItem = Fluence.Wpf.Controls.TreeViewItem;

namespace Fluence.Wpf.Demo.Samples.Trees
{
    public partial class TreeViewExpansion : UserControl
    {
        public TreeViewExpansion()
        {
            InitializeComponent();
        }

        private void ExpandAll_Click(object sender, RoutedEventArgs e)
        {
            SetExpanded(ExpansionTreeView.Items, true);
        }

        private void CollapseAll_Click(object sender, RoutedEventArgs e)
        {
            SetExpanded(ExpansionTreeView.Items, false);
        }

        private static void SetExpanded(ItemCollection items, bool expanded)
        {
            foreach (var obj in items)
            {
                var item = obj as FluenceTreeViewItem;
                if (item == null)
                {
                    continue;
                }

                item.IsExpanded = expanded;
                SetExpanded(item.Items, expanded);
            }
        }
    }
}
