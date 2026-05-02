using System;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Samples.DataBinding
{
    public partial class ListViewSelectionMode : UserControl
    {
        public ListViewSelectionMode()
        {
            InitializeComponent();
        }

        private void SelectionMode_Changed(object sender, RoutedEventArgs e)
        {
            if (SelectionModeListView == null)
            {
                return;
            }

            if (MultipleModeRadio != null && MultipleModeRadio.IsChecked == true)
            {
                SelectionModeListView.SelectionMode = SelectionMode.Multiple;
            }
            else if (ExtendedModeRadio != null && ExtendedModeRadio.IsChecked == true)
            {
                SelectionModeListView.SelectionMode = SelectionMode.Extended;
            }
            else
            {
                SelectionModeListView.SelectionMode = SelectionMode.Single;
            }

            SelectionModeListView.UnselectAll();
            UpdateSelectionLabel();
        }

        private void SelectionModeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectionLabel();
        }

        private void UpdateSelectionLabel()
        {
            if (SelectionCountLabel == null || SelectionModeListView == null)
            {
                return;
            }

            var count = SelectionModeListView.SelectedItems.Count;
            if (count == 0)
            {
                SelectionCountLabel.Text = "Selected: none";
                return;
            }

            SelectionCountLabel.Text = count == 1
                ? string.Format("Selected: {0}", (SelectionModeListView.SelectedItem as ListViewItem)?.Content ?? "?")
                : string.Format("Selected: {0} items", count);
        }
    }
}
