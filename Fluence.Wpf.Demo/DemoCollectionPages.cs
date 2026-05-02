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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Fluence.Wpf;
using Fluence.Wpf.Demo.Pages;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo
{
    internal static class DemoCollectionPages
    {
        private static readonly char[] SpaceSeparators = { ' ' };

        public static GalleryControlPage Card()
        {
            return CreatePage(
                "Card",
                "Use Card to group related content with a Fluent surface and optional header.",
                new[] { Example("Card variants", "Cards support default, outlined, filled, and subtle visual emphasis.", "Data/CardVariants.xaml", CreateCards) });
        }

        public static GalleryControlPage ListBox()
        {
            return CreatePage(
                "ListBox",
                "Use ListBox for selecting one or more items from a visible list.",
                new[] { Example("List selection", "ListBox supports single and multi-selection with standard item chrome.", "Data/ListBoxItems.xaml", CreateListBoxes) });
        }

        public static GalleryControlPage ListView()
        {
            return CreatePage(
                "ListView",
                "Use ListView for compact single-column collections and richer item layouts.",
                new[]
                {
                    Example("Basic ListView with simple data template", "A simple ListView can use compact text rows for a collection.", "Data/ListViewItems.xaml", CreateBasicListView),
                    Example("ListView with selection support", "WPF ListView supports Single, Multiple, and Extended selection modes.", "Data/ListViewSelection.xaml", CreateListViewSelection),
                    Example("ListView with grouped headers", "Use non-selectable rows to present compact group headers when a full grouped CollectionView is unnecessary.", "Data/ListViewGrouped.xaml", CreateGroupedListView),
                    Example("ListView with filtering", "Filter the visible item set while keeping the same item row template.", "Data/ListViewFiltering.xaml", CreateFilteredListView),
                    Example("ListView for messaging or data logging", "A non-selectable ListView can present chronological messages or log output.", "Data/ListViewMessages.xaml", CreateMessagingListView),
                    Example("ListView with images", "ListView rows can combine image content with text metadata.", "Data/ListViewImages.xaml", CreateImageListView),
                    Example("ListView with context menus", "Attach item-level ContextMenu commands to ListViewItem containers.", "Data/ListViewContextMenu.xaml", CreateContextMenuListView),
                    Example("Empty state", "EmptyContent gives an empty ListView a clear state.", "Data/ListViewEmptyState.xaml", CreateEmptyListView)
                });
        }

        public static GalleryControlPage TreeView()
        {
            return CreatePage(
                "TreeView",
                "Use TreeView for nested data that users expand, collapse, and select.",
                new[]
                {
                    Example("Hierarchy", "TreeViewItem nesting creates compact folder, outline, or category trees.", "Trees/TreeViewHierarchy.xaml", CreateTreeViewHierarchy),
                    Example("Selection", "SelectedItemChanged can update related details as users move through the hierarchy.", "Trees/TreeViewSelection.xaml", CreateTreeViewSelection),
                    Example("Expansion", "Expanded nodes can reveal nested groups while keeping the rest collapsed.", "Trees/TreeViewExpansion.xaml", CreateTreeViewExpansion)
                });
        }

        private static GalleryControlPage CreatePage(string title, string description, DemoExample[] examples)
        {
            return new GalleryControlPage(title, description, examples);
        }

        private static DemoExample Example(string title, string description, string sourcePath, Func<UIElement> createContent)
        {
            return new DemoExample(title, description, sourcePath, createContent);
        }

        private static WrapPanel Wrap(params UIElement[] children)
        {
            var panel = new WrapPanel();
            foreach (var child in children)
            {
                panel.Children.Add(child);
            }

            return panel;
        }

        private static Fluent.StackPanel Stack(params UIElement[] children)
        {
            var panel = new Fluent.StackPanel { Spacing = 8 };
            foreach (var child in children)
            {
                panel.Children.Add(child);
            }

            return panel;
        }

        private static Fluent.StackPanel LabeledBlock(string label, UIElement content)
        {
            var panel = new Fluent.StackPanel { Spacing = 8 };
            panel.Children.Add(new TextBlock { Text = label });
            panel.Children.Add(content);
            return panel;
        }

        private static TextBlock DescriptionText(string text)
        {
            var block = new TextBlock
            {
                MaxWidth = 560,
                Text = text,
                TextWrapping = TextWrapping.Wrap
            };
            block.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            return block;
        }

        private static Fluent.Card DemoCard(string header, CardVariant variant)
        {
            return new Fluent.Card
            {
                Width = 220,
                MinHeight = 118,
                Margin = new Thickness(0, 0, 16, 16),
                Padding = new Thickness(18),
                Header = header,
                Variant = variant,
                Content = new TextBlock { Text = "Grouped content surface.", TextWrapping = TextWrapping.Wrap }
            };
        }

        private static UIElement CreateCards()
        {
            return Wrap(
                DemoCard("Default", CardVariant.Default),
                DemoCard("Outlined", CardVariant.Outlined),
                DemoCard("Filled", CardVariant.Filled),
                DemoCard("Subtle", CardVariant.Subtle));
        }

        private static UIElement CreateListBoxes()
        {
            var panel = new Fluent.StackPanel { Spacing = 18 };
            var colorPreview = new System.Windows.Controls.Border
            {
                Width = 96,
                Height = 48,
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1)
            };
            colorPreview.SetResourceReference(System.Windows.Controls.Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
            var colorLabel = new TextBlock();
            colorLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");

            var colors = new Fluent.ListBox
            {
                Width = 260,
                MinHeight = 156,
                SelectedIndex = 0
            };
            colors.Items.Add(ColorItem("Blue", Brushes.SteelBlue));
            colors.Items.Add(ColorItem("Green", Brushes.SeaGreen));
            colors.Items.Add(ColorItem("Orange", Brushes.DarkOrange));
            colors.SelectionChanged += delegate
            {
                var selected = colors.SelectedItem as Fluent.ListBoxItem;
                colorPreview.Background = selected == null ? Brushes.Transparent : selected.Tag as Brush;
                colorLabel.Text = string.Format(CultureInfo.CurrentCulture, "Selected: {0}", selected == null ? "none" : selected.Content);
            };

            var colorLayout = new Fluent.StackPanel { Spacing = 16, Orientation = Orientation.Horizontal };
            colorLayout.Children.Add(colors);
            var colorDetail = new Fluent.StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
            colorDetail.Children.Add(colorPreview);
            colorDetail.Children.Add(colorLabel);
            colorLayout.Children.Add(colorDetail);
            panel.Children.Add(LabeledBlock("Select a color", colorLayout));

            var fontPreview = new TextBlock
            {
                Text = "The quick brown fox jumps over the lazy dog.",
                TextWrapping = TextWrapping.Wrap
            };
            fontPreview.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            var fontList = new Fluent.ListBox
            {
                Width = 260,
                MinHeight = 156,
                SelectedIndex = 1
            };
            fontList.Items.Add(FontItem("Segoe UI"));
            fontList.Items.Add(FontItem("Segoe UI Semibold"));
            fontList.Items.Add(FontItem("Consolas"));
            fontList.SelectionChanged += delegate
            {
                var selected = fontList.SelectedItem as Fluent.ListBoxItem;
                if (selected != null)
                {
                    fontPreview.FontFamily = new FontFamily(selected.Content as string);
                }
            };

            var fontLayout = new Fluent.StackPanel { Spacing = 16, Orientation = Orientation.Horizontal };
            fontLayout.Children.Add(fontList);
            var previewHost = new Fluent.Card
            {
                Width = 280,
                Padding = new Thickness(16),
                Variant = CardVariant.Subtle,
                Content = fontPreview
            };
            fontLayout.Children.Add(previewHost);
            panel.Children.Add(LabeledBlock("Select a font", fontLayout));
            colorPreview.Background = Brushes.SteelBlue;
            colorLabel.Text = "Selected: Blue";
            fontPreview.FontFamily = new FontFamily("Segoe UI Semibold");
            return panel;
        }

        private static UIElement CreateBasicListView()
        {
            var panel = new Fluent.StackPanel { Spacing = 14 };
            panel.Children.Add(DescriptionText("This mirrors the WinUI Gallery basic ListView sample with simple contact names in compact rows."));

            var list = FramedListView(350, 320);
            var contacts = ContactSamples();
            for (var i = 0; i < contacts.Length; i++)
            {
                list.Items.Add(CreateBasicListViewItem(contacts[i]));
            }

            panel.Children.Add(list);
            return panel;
        }

        private static UIElement CreateListViewSelection()
        {
            var panel = new Fluent.StackPanel { Spacing = 14 };
            panel.Children.Add(DescriptionText("Single selects one item. Multiple shows check-style selection affordances. Extended supports Ctrl and Shift range selection."));

            var list = FramedListView(400, 340);
            var contacts = ContactSamples();
            for (var i = 0; i < contacts.Length; i++)
            {
                list.Items.Add(CreateContactListViewItem(contacts[i]));
            }

            list.SelectedIndex = 0;

            var options = new Fluent.StackPanel { Spacing = 8, Orientation = Orientation.Horizontal };
            var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Text = "SelectionMode" };
            label.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            options.Children.Add(label);

            var modeCombo = new Fluent.ComboBox { Width = 150 };
            modeCombo.Items.Add(new ComboBoxItem { Content = "Single", Tag = SelectionMode.Single });
            modeCombo.Items.Add(new ComboBoxItem { Content = "Multiple", Tag = SelectionMode.Multiple });
            modeCombo.Items.Add(new ComboBoxItem { Content = "Extended", Tag = SelectionMode.Extended });
            modeCombo.SelectedIndex = 0;
            options.Children.Add(modeCombo);

            var status = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            status.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            options.Children.Add(status);

            list.SelectionChanged += delegate { UpdateSelectionStatus(list, status); };
            modeCombo.SelectionChanged += delegate
            {
                var selected = modeCombo.SelectedItem as ComboBoxItem;
                if (selected != null && selected.Tag is SelectionMode)
                {
                    list.SelectionMode = (SelectionMode)selected.Tag;
                    UpdateSelectionStatus(list, status);
                }
            };
            UpdateSelectionStatus(list, status);

            panel.Children.Add(list);
            panel.Children.Add(options);
            return panel;
        }

        private static UIElement CreateGroupedListView()
        {
            var panel = new Fluent.StackPanel { Spacing = 14 };
            panel.Children.Add(DescriptionText("This sample keeps the same ListView row style and inserts non-selectable group headers for a lightweight grouped list."));

            var list = FramedListView(420, 360);
            list.Items.Add(CreateGroupHeaderItem("Contoso"));
            list.Items.Add(CreateContactListViewItem(new ContactInfo("Ana Bowman", "Support Engineer", "Contoso", "Available")));
            list.Items.Add(CreateContactListViewItem(new ContactInfo("Shawn Hughes", "Platform Specialist", "Contoso", "In a meeting")));
            list.Items.Add(CreateGroupHeaderItem("Fabrikam"));
            list.Items.Add(CreateContactListViewItem(new ContactInfo("Oscar Ward", "DevOps Lead", "Fabrikam", "Offline")));
            list.Items.Add(CreateContactListViewItem(new ContactInfo("Madison Butler", "Product Designer", "Fabrikam", "Available")));
            list.Items.Add(CreateGroupHeaderItem("Northwind"));
            list.Items.Add(CreateContactListViewItem(new ContactInfo("Iris Cooper", "Account Manager", "Northwind", "Available")));
            list.SelectedIndex = 1;

            panel.Children.Add(list);
            return panel;
        }

        private static UIElement CreateFilteredListView()
        {
            var panel = new Fluent.StackPanel { Spacing = 14 };
            var list = FramedListView(420, 330);
            var contacts = ContactSamples();

            var filterText = new Fluent.TextBox
            {
                Width = 260,
                PlaceholderText = "Filter by name, role, or company"
            };
            filterText.TextChanged += delegate { PopulateFilteredContacts(list, contacts, filterText.Text); };

            var filterRow = new Fluent.StackPanel { Spacing = 8 };
            filterRow.Children.Add(new TextBlock { Text = "Filter by..." });
            filterRow.Children.Add(filterText);

            panel.Children.Add(filterRow);
            panel.Children.Add(list);
            PopulateFilteredContacts(list, contacts, string.Empty);
            return panel;
        }

        private static UIElement CreateMessagingListView()
        {
            var panel = new Fluent.StackPanel { Spacing = 14 };
            panel.Children.Add(DescriptionText("Set IsItemSelectable to false when the ListView is being used as a readable message or logging surface."));

            var list = FramedListView(460, 260);
            list.IsItemSelectable = false;
            list.Items.Add(CreateMessageListViewItem("10:21", "Package detection started."));
            list.Items.Add(CreateMessageListViewItem("10:22", "Resolved 4 install prerequisites."));
            list.Items.Add(CreateMessageListViewItem("10:24", "Waiting for the user interface to become idle."));
            list.Items.Add(CreateMessageListViewItem("10:25", "Install command completed successfully."));

            panel.Children.Add(list);
            return panel;
        }

        private static UIElement CreateImageListView()
        {
            var list = FramedListView(460, 330);
            list.Items.Add(CreateImageListViewItem("ListView", "Single-column collection presentation.", "ListView.png"));
            list.Items.Add(CreateImageListViewItem("TreeView", "Hierarchical expandable collection.", "TreeView.png"));
            list.Items.Add(CreateImageListViewItem("GridView", "Tile-oriented collection layout in WinUI.", "GridView.png"));
            list.Items.Add(CreateImageListViewItem("ItemsRepeater", "Flexible virtualized items surface in WinUI.", "ItemsRepeater.png"));
            list.SelectedIndex = 0;
            return list;
        }

        private static UIElement CreateContextMenuListView()
        {
            var panel = new Fluent.StackPanel { Spacing = 14 };
            var list = FramedListView(420, 280);
            list.Items.Add(CreateContextMenuListViewItem(list, "Ana Bowman", "Contoso"));
            list.Items.Add(CreateContextMenuListViewItem(list, "Oscar Ward", "Fabrikam"));
            list.Items.Add(CreateContextMenuListViewItem(list, "Priya Shah", "Tailspin"));
            list.SelectedIndex = 0;

            panel.Children.Add(DescriptionText("Right-click an item to open item-scoped commands."));
            panel.Children.Add(list);
            return panel;
        }

        private static Fluent.ListBoxItem ColorItem(string name, Brush brush)
        {
            return new Fluent.ListBoxItem
            {
                Content = name,
                Tag = brush
            };
        }

        private static Fluent.ListBoxItem FontItem(string fontFamily)
        {
            return new Fluent.ListBoxItem { Content = fontFamily };
        }

        private static Fluent.ListView FramedListView(double width, double height)
        {
            var list = new Fluent.ListView
            {
                Width = width,
                Height = height,
                HorizontalAlignment = HorizontalAlignment.Left,
                BorderThickness = new Thickness(1)
            };
            list.SetResourceReference(Control.BorderBrushProperty, "ControlStrongStrokeColorDefaultBrush");
            return list;
        }

        private static ListViewItem CreateBasicListViewItem(ContactInfo contact)
        {
            return new ListViewItem
            {
                Content = new TextBlock
                {
                    Margin = new Thickness(0, 5, 0, 5),
                    Text = contact.Name
                }
            };
        }

        private static ListViewItem CreateContactListViewItem(ContactInfo contact)
        {
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var initials = new System.Windows.Controls.Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.SemiBold,
                    Text = contact.Initials
                }
            };
            initials.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "AccentFillColorSecondaryBrush");
            ((TextBlock)initials.Child).SetResourceReference(TextBlock.ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
            row.Children.Add(initials);

            var text = new Fluent.StackPanel { Spacing = 2, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(new TextBlock { FontWeight = FontWeights.SemiBold, Text = contact.Name });
            var roleText = new TextBlock { FontSize = 12, Text = contact.Role + " - " + contact.Company };
            roleText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            text.Children.Add(roleText);
            Grid.SetColumn(text, 1);
            row.Children.Add(text);

            var statusText = new TextBlock
            {
                Text = contact.Status,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            Grid.SetColumn(statusText, 2);
            row.Children.Add(statusText);

            return new ListViewItem { Content = row };
        }

        private static ListViewItem CreateMessageListViewItem(string time, string message)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var timeText = new TextBlock
            {
                Width = 52,
                VerticalAlignment = VerticalAlignment.Top,
                Text = time
            };
            timeText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            grid.Children.Add(timeText);

            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(messageText, 1);
            grid.Children.Add(messageText);

            return new ListViewItem { Content = grid };
        }

        private static ListViewItem CreateImageListViewItem(string title, string description, string imageFileName)
        {
            var grid = new Grid { Margin = new Thickness(0, 6, 0, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var image = new Image
            {
                Width = 56,
                Height = 56,
                Source = new BitmapImage(new Uri("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/" + imageFileName, UriKind.Absolute)),
                Stretch = Stretch.Uniform
            };
            grid.Children.Add(image);

            var text = new Fluent.StackPanel
            {
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 3
            };
            text.Children.Add(new TextBlock { FontWeight = FontWeights.SemiBold, Text = title });
            var descriptionText = new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap
            };
            descriptionText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            text.Children.Add(descriptionText);
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            return new ListViewItem { Content = grid };
        }

        private static ListViewItem CreateContextMenuListViewItem(Fluent.ListView owner, string name, string company)
        {
            var item = new ListViewItem
            {
                Content = CreateContactListViewItem(new ContactInfo(name, "Contact", company, "Available")).Content
            };

            var menu = new ContextMenu();
            var delete = new MenuItem { Header = "Delete" };
            delete.Click += delegate { owner.Items.Remove(item); };
            menu.Items.Add(delete);
            item.ContextMenu = menu;
            return item;
        }

        private static ListViewItem CreateGroupHeaderItem(string text)
        {
            var header = new TextBlock
            {
                Margin = new Thickness(0, 10, 0, 4),
                FontWeight = FontWeights.SemiBold,
                Text = text
            };
            header.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");

            return new ListViewItem
            {
                Content = header,
                Focusable = false,
                IsHitTestVisible = false
            };
        }

        private static void UpdateSelectionStatus(Fluent.ListView list, TextBlock status)
        {
            status.Text = string.Format(CultureInfo.CurrentCulture, "Selected: {0}", list.SelectedItems.Count);
        }

        private static void PopulateFilteredContacts(Fluent.ListView list, ContactInfo[] contacts, string filter)
        {
            list.Items.Clear();

            for (var i = 0; i < contacts.Length; i++)
            {
                if (ContactMatches(contacts[i], filter))
                {
                    list.Items.Add(CreateContactListViewItem(contacts[i]));
                }
            }

            if (list.Items.Count > 0)
            {
                list.SelectedIndex = 0;
            }
        }

        private static bool ContactMatches(ContactInfo contact, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            return ContainsOrdinalIgnoreCase(contact.Name, filter)
                || ContainsOrdinalIgnoreCase(contact.Role, filter)
                || ContainsOrdinalIgnoreCase(contact.Company, filter);
        }

        private static bool ContainsOrdinalIgnoreCase(string value, string filter)
        {
#if NET10_0_OR_GREATER
            return value.Contains(filter, StringComparison.OrdinalIgnoreCase);
#else
            return value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
#endif
        }

        private static ContactInfo[] ContactSamples()
        {
            return new[]
            {
                new ContactInfo("Ana Bowman", "Support Engineer", "Contoso", "Available"),
                new ContactInfo("Shawn Hughes", "Platform Specialist", "Contoso", "In a meeting"),
                new ContactInfo("Oscar Ward", "DevOps Lead", "Fabrikam", "Offline"),
                new ContactInfo("Madison Butler", "Product Designer", "Fabrikam", "Available"),
                new ContactInfo("Iris Cooper", "Account Manager", "Northwind", "Available"),
                new ContactInfo("Daniel Wells", "Field Technician", "Northwind", "Away"),
                new ContactInfo("Priya Shah", "UX Researcher", "Tailspin", "Available"),
                new ContactInfo("Victor Chen", "Program Manager", "Tailspin", "Busy")
            };
        }

        private static UIElement CreateEmptyListView()
        {
            return new Fluent.ListView
            {
                Name = "EmptyStateListView",
                Height = 160,
                EmptyContent = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = "No items to show."
                }
            };
        }

        private static UIElement CreateTreeViewHierarchy()
        {
            var tree = new Fluent.TreeView { MinHeight = 190 };
            var project = new Fluent.TreeViewItem { Header = "Project", IsExpanded = true };
            var pages = new Fluent.TreeViewItem { Header = "Pages", IsExpanded = true };
            pages.Items.Add(new Fluent.TreeViewItem { Header = "GalleryButtonsPage.xaml" });
            pages.Items.Add(new Fluent.TreeViewItem { Header = "GalleryDataPage.xaml" });
            pages.Items.Add(new Fluent.TreeViewItem { Header = "GalleryTreesPage.xaml" });
            var samples = new Fluent.TreeViewItem { Header = "Samples" };
            samples.Items.Add(new Fluent.TreeViewItem { Header = "Buttons" });
            samples.Items.Add(new Fluent.TreeViewItem { Header = "Trees" });
            project.Items.Add(pages);
            project.Items.Add(samples);
            tree.Items.Add(project);
            tree.Items.Add(new Fluent.TreeViewItem { Header = "Resources" });
            return tree;
        }

        private static UIElement CreateTreeViewExpansion()
        {
            var tree = new Fluent.TreeView { MinHeight = 180 };
            var source = new Fluent.TreeViewItem { Header = "Source", IsExpanded = true };
            source.Items.Add(new Fluent.TreeViewItem { Header = "Controls" });
            source.Items.Add(new Fluent.TreeViewItem { Header = "Themes" });
            var tests = new Fluent.TreeViewItem { Header = "Tests" };
            tests.Items.Add(new Fluent.TreeViewItem { Header = "Control tests" });
            tests.Items.Add(new Fluent.TreeViewItem { Header = "Demo tests" });
            tree.Items.Add(source);
            tree.Items.Add(tests);
            return tree;
        }

        private static UIElement CreateTreeViewSelection()
        {
            var panel = new Fluent.StackPanel { Spacing = 12 };
            var selectedLabel = new TextBlock { Text = "Selected: -" };
            selectedLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            var tree = new Fluent.TreeView { MinHeight = 190 };
            tree.SelectedItemChanged += delegate(object sender, RoutedPropertyChangedEventArgs<object> e)
            {
                var item = e.NewValue as Fluent.TreeViewItem;
                selectedLabel.Text = string.Format(CultureInfo.CurrentCulture, "Selected: {0}", item == null ? "-" : item.Header);
            };

            var inbox = new Fluent.TreeViewItem { Header = "Inbox", IsExpanded = true };
            var priority = new Fluent.TreeViewItem { Header = "Priority", IsExpanded = true };
            priority.Items.Add(new Fluent.TreeViewItem { Header = "Contract review" });
            priority.Items.Add(new Fluent.TreeViewItem { Header = "Customer follow-up" });
            inbox.Items.Add(priority);
            inbox.Items.Add(new Fluent.TreeViewItem { Header = "Later" });
            tree.Items.Add(inbox);
            tree.Items.Add(new Fluent.TreeViewItem { Header = "Archive" });
            panel.Children.Add(tree);
            panel.Children.Add(selectedLabel);
            return panel;
        }

        private sealed class ContactInfo
        {
            public ContactInfo(string name, string role, string company, string status)
            {
                Name = name;
                Role = role;
                Company = company;
                Status = status;
                Initials = GetInitials(name);
            }

            public string Name { get; private set; }

            public string Role { get; private set; }

            public string Company { get; private set; }

            public string Status { get; private set; }

            public string Initials { get; private set; }

            private static string GetInitials(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return string.Empty;
                }

                var parts = name.Split(SpaceSeparators, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                {
                    return char.ToUpperInvariant(parts[0][0]).ToString();
                }

                return char.ToUpperInvariant(parts[0][0]).ToString() +
                    char.ToUpperInvariant(parts[parts.Length - 1][0]).ToString();
            }
        }
    }
}
