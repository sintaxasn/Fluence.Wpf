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
    internal static class DemoNavigationPages
    {
        public static GalleryControlPage NavigationView()
        {
            return CreatePage(
                "NavigationView",
                "Use NavigationView for top-level app destinations with left, top, or compact pane layouts.",
                new[]
                {
                    Example("Left pane", "Left navigation keeps destinations visible beside the active page.", "Navigation/LeftNavigationView.xaml", CreateLeftNavigationView),
                    Example("Top pane", "Top navigation works for smaller destination sets.", "Navigation/TopNavigationView.xaml", CreateTopNavigationView)
                });
        }

        public static GalleryControlPage TabView()
        {
            return CreatePage(
                "TabView",
                "Use TabView for document-style tabs with add and close affordances.",
                new[] { Example("Documents", "TabView hosts closable tabs with optional icons.", "Tabs/TabViewDocuments.xaml", CreateTabView) });
        }

        private static GalleryControlPage CreatePage(string title, string description, DemoExample[] examples)
        {
            return new GalleryControlPage(title, description, examples);
        }

        private static DemoExample Example(string title, string description, string sourcePath, Func<UIElement> createContent)
        {
            return new DemoExample(title, description, sourcePath, createContent);
        }

        private static Fluent.FontIcon Icon(string glyph)
        {
            return new Fluent.FontIcon { Glyph = glyph, IconFontSize = 14 };
        }

        private static TextBlock PageText(string text)
        {
            var block = new TextBlock
            {
                Margin = new Thickness(18),
                Text = text,
                TextWrapping = TextWrapping.Wrap
            };
            block.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            return block;
        }

        private static UIElement CreateLeftNavigationView()
        {
            var nav = new Fluent.NavigationView
            {
                Width = 620,
                Height = 320,
                PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                PaneHeader = new TextBlock { Margin = new Thickness(12, 0, 12, 8), Text = "Workspace" }
            };
            var home = new Fluent.NavigationViewItem
            {
                Content = "Home",
                Icon = Icon("\uE80F"),
                PageContent = PageText("Home overview")
            };
            nav.Items.Add(home);
            nav.Items.Add(new Fluent.NavigationViewItem { Content = "Files", Icon = Icon("\uE8B7"), PageContent = PageText("File browser") });
            nav.Items.Add(new Fluent.NavigationViewItem { Content = "Reports", Icon = Icon("\uE9D2"), PageContent = PageText("Report dashboard") });
            nav.SelectedItem = home;
            return nav;
        }

        private static UIElement CreateTopNavigationView()
        {
            var nav = new Fluent.NavigationView
            {
                Width = 620,
                Height = 240,
                PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                Header = "Project"
            };
            var overview = new Fluent.NavigationViewItem
            {
                Content = "Overview",
                Icon = Icon("\uE80F"),
                PageContent = PageText("Overview content")
            };
            nav.Items.Add(overview);
            nav.Items.Add(new Fluent.NavigationViewItem { Content = "Activity", Icon = Icon("\uE7C1"), PageContent = PageText("Activity content") });
            nav.Items.Add(new Fluent.NavigationViewItem { Content = "Settings", Icon = Icon("\uE713"), PageContent = PageText("Settings content") });
            nav.SelectedItem = overview;
            return nav;
        }

        private static UIElement CreateTabView()
        {
            var tabView = new Fluent.TabView
            {
                Width = 620,
                Height = 260
            };
            tabView.Items.Add(new Fluent.TabViewItem { Header = "Document 1", Icon = Icon("\uE8A5"), Content = PageText("First document content") });
            tabView.Items.Add(new Fluent.TabViewItem { Header = "Document 2", Icon = Icon("\uE8A5"), Content = PageText("Second document content") });
            tabView.Items.Add(new Fluent.TabViewItem { Header = "Read-only", IsClosable = false, Content = PageText("Pinned tab content") });
            tabView.SelectedIndex = 0;
            return tabView;
        }
    }
}
