using System.Windows;
using System.Windows.Controls;

using FluenceTreeViewItem = Fluence.Wpf.Controls.TreeViewItem;

namespace Fluence.Wpf.Demo.Samples.Trees
{
    public partial class TreeViewSelection : UserControl
    {
        public TreeViewSelection()
        {
            InitializeComponent();
        }

        private void SelectionTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = e.NewValue as FluenceTreeViewItem;
            TreeSelectionLabel.Text = item == null
                ? "Selected: -"
                : string.Format("Selected: {0}", BuildPath(item));
        }

        private static string BuildPath(FluenceTreeViewItem item)
        {
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
    }
}
