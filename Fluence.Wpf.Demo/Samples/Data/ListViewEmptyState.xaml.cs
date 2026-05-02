using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Samples.Data
{
    public partial class ListViewEmptyState : UserControl
    {
        private int _addCounter;

        private static readonly string[] SampleNames =
        {
            "Liam Torres",
            "Nora Fischer",
            "Eli Nakamura",
            "Priya Kapoor",
            "Dante Reeves"
        };

        public ListViewEmptyState()
        {
            InitializeComponent();
        }

        private void AddListItem_Click(object sender, RoutedEventArgs e)
        {
            var name = SampleNames[_addCounter % SampleNames.Length];
            _addCounter++;

            EmptyStateListView.Items.Add(new ListViewItem { Content = name });
        }

        private void RemoveListItem_Click(object sender, RoutedEventArgs e)
        {
            if (EmptyStateListView.Items.Count == 0)
            {
                return;
            }

            var lastItem = EmptyStateListView.Items[EmptyStateListView.Items.Count - 1];
            EmptyStateListView.AnimateRemove(lastItem, null);
        }
    }
}
