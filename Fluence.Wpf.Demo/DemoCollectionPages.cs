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
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf;
using Fluence.Wpf.Demo.Pages;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo
{
    internal static class DemoCollectionPages
    {
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
                    Example("List items", "ListView supports simple rows and richer item content.", "Data/ListViewItems.xaml", CreateListViews),
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
            var single = new Fluent.ListBox
            {
                Width = 260,
                Margin = new Thickness(0, 0, 24, 0),
                SelectedIndex = 1
            };
            single.Items.Add(new ListBoxItem { Content = "Inbox" });
            single.Items.Add(new ListBoxItem { Content = "Pinned" });
            single.Items.Add(new ListBoxItem { Content = "Archive" });

            var multiple = new Fluent.ListBox
            {
                Width = 260,
                SelectionMode = SelectionMode.Multiple
            };
            multiple.Items.Add(new ListBoxItem { Content = "Security review", IsSelected = true });
            multiple.Items.Add(new ListBoxItem { Content = "Release notes" });
            multiple.Items.Add(new ListBoxItem { Content = "Accessibility pass", IsSelected = true });

            return Wrap(single, multiple);
        }

        private static UIElement CreateListViews()
        {
            var simple = new Fluent.ListView
            {
                Width = 280,
                Height = 170,
                Margin = new Thickness(0, 0, 24, 0)
            };
            simple.Items.Add(new ListViewItem { Content = "Ana Bowman" });
            simple.Items.Add(new ListViewItem { Content = "Shawn Hughes" });
            simple.Items.Add(new ListViewItem { Content = "Oscar Ward" });

            var rich = new Fluent.ListView
            {
                Width = 320,
                Height = 170
            };
            rich.Items.Add(CreateRichListViewItem("Design sync", "Today, 10:00 AM"));
            rich.Items.Add(CreateRichListViewItem("Build validation", "Today, 1:30 PM"));
            rich.Items.Add(CreateRichListViewItem("Release review", "Tomorrow"));

            return Wrap(simple, rich);
        }

        private static ListViewItem CreateRichListViewItem(string title, string subtitle)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = title });
            var detail = new TextBlock { Text = subtitle };
            detail.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            panel.Children.Add(detail);
            return new ListViewItem { Content = panel };
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
    }
}
