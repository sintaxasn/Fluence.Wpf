using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fluence.Wpf.Demo.Samples.DataBinding
{
    public partial class ObservableCollectionListView : UserControl
    {
        private readonly ObservableCollection<DataBindingSampleItem> _items = new ObservableCollection<DataBindingSampleItem>();

        public ObservableCollectionListView()
        {
            InitializeComponent();

            BoundListView.ItemsSource = _items;
            AddDemoItem("Fluence.Wpf");
            AddDemoItem("WinUI 3 parity controls");
            AddDemoItem("net472 + net10.0-windows");
            UpdateCount();
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (NewItemBox == null)
            {
                return;
            }

            var text = (NewItemBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            AddDemoItem(text);
            NewItemBox.Text = string.Empty;
            NewItemBox.Focus();
            UpdateCount();
        }

        private void NewItemBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddItem_Click(sender, e);
                e.Handled = true;
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = BoundListView.SelectedItem as DataBindingSampleItem;
            if (selected != null)
            {
                _items.Remove(selected);
                UpdateCount();
            }
        }

        private void AddDemoItem(string name)
        {
            _items.Add(new DataBindingSampleItem
            {
                Name = name,
                AddedAt = DateTime.Now.ToString("HH:mm:ss")
            });
        }

        private void UpdateCount()
        {
            ItemCountLabel.Text = string.Format("{0} item{1}", _items.Count, _items.Count == 1 ? "" : "s");
        }
    }

    public sealed class DataBindingSampleItem
    {
        public string Name { get; set; }

        public string AddedAt { get; set; }
    }
}
