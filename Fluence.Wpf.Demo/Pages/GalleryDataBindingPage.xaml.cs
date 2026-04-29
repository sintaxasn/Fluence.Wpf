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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>Gallery page demonstrating ObservableCollection binding and ListView SelectionMode variants.</summary>
    public partial class GalleryDataBindingPage : UserControl
    {
        private readonly ObservableCollection<DemoItem> _items = new ObservableCollection<DemoItem>();
        private readonly ObservableCollection<DemoItem> _templateItems = new ObservableCollection<DemoItem>();

        /// <summary>Initializes a new instance of <see cref="GalleryDataBindingPage"/>.</summary>
        public GalleryDataBindingPage()
        {
            InitializeComponent();

            DemoSourceAction.Replace(ObservableCollectionListViewSourceLink, "DataBinding/ObservableCollectionListView.xaml");
            DemoSourceAction.Replace(ListViewSelectionModeSourceLink, "DataBinding/ListViewSelectionMode.xaml");
            DemoSourceAction.Replace(DataTemplateRowSourceLink, "DataBinding/DataTemplateRow.xaml");

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            BoundListView.ItemsSource = _items;
            DataTemplateListView.ItemsSource = _templateItems;

            // Seed a few items so the list is not empty on first load.
            AddDemoItem("Fluence.Wpf");
            AddDemoItem("WinUI 3 parity controls");
            AddDemoItem("net472 + net10.0-windows");
            AddDataTemplateItem("Release notes");
            AddDataTemplateItem("Design tokens");
            AddDataTemplateItem("Control states");
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
            if (BoundListView == null)
            {
                return;
            }

            var selected = BoundListView.SelectedItem as DemoItem;
            if (selected != null)
            {
                _items.Remove(selected);
                UpdateCount();
            }
        }

        private void AddDemoItem(string name)
        {
            _items.Add(new DemoItem
            {
                Name = name,
                AddedAt = DateTime.Now.ToString("HH:mm:ss")
            });
        }

        private void AddDataTemplateItem(string name)
        {
            _templateItems.Add(new DemoItem
            {
                Name = name,
                AddedAt = DateTime.Now.ToString("HH:mm:ss")
            });
        }

        private void UpdateCount()
        {
            if (ItemCountLabel != null)
            {
                ItemCountLabel.Text = string.Format("{0} item{1}", _items.Count, _items.Count == 1 ? "" : "s");
            }
        }

        private void SelectionMode_Changed(object sender, RoutedEventArgs e)
        {
            if (SelectionModeListView == null)
            {
                return;
            }

            if (MultipleModeRadio?.IsChecked == true)
            {
                SelectionModeListView.SelectionMode = SelectionMode.Multiple;
            }
            else if (ExtendedModeRadio?.IsChecked == true)
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

    /// <summary>Simple view-model item for the bound list demo.</summary>
    public sealed class DemoItem
    {
        /// <summary>Gets or sets the display name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the time the item was added (formatted string).</summary>
        public string AddedAt { get; set; }
    }
}
