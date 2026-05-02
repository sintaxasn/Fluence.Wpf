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
using Fluence.Wpf.Demo.Pages;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo
{
    internal static class DemoMenuPages
    {
        public static GalleryControlPage Menu()
        {
            return CreatePage(
                "Menu",
                "Use Menu for persistent command groups with submenus, keyboard cues, and checked states.",
                new[] { Example("Menu bar", "Menu can host top-level groups, nested commands, separators, and disabled items.", "Menus/MenuBar.xaml", CreateMenuBar) });
        }

        public static GalleryControlPage ContextMenu()
        {
            return CreatePage(
                "ContextMenu",
                "Use ContextMenu for actions that apply to a selected item or region.",
                new[] { Example("Context actions", "Context menus can include icons, separators, disabled items, and nested groups.", "Menus/ContextMenuActions.xaml", CreateContextMenu) });
        }

        public static GalleryControlPage ToolTip()
        {
            return CreatePage(
                "ToolTip",
                "Use ToolTip for short hints that clarify icons, disabled commands, or risky actions.",
                new[] { Example("Helpful hints", "ToolTips can be simple text or structured content.", "Menus/ToolTips.xaml", CreateToolTips) });
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

        private static WrapPanel Wrap(params UIElement[] children)
        {
            var panel = new WrapPanel();
            foreach (var child in children)
            {
                panel.Children.Add(child);
            }

            return panel;
        }

        private static UIElement CreateMenuBar()
        {
            var menu = new Fluent.Menu();
            var file = new Fluent.MenuItem { Header = "_File" };
            file.Items.Add(new Fluent.MenuItem { Header = "_New", Icon = Icon("\uE710") });
            file.Items.Add(new Fluent.MenuItem { Header = "_Open", Icon = Icon("\uE8E5") });
            file.Items.Add(new Separator());
            file.Items.Add(new Fluent.MenuItem { Header = "Print", IsEnabled = false });

            var view = new Fluent.MenuItem { Header = "_View" };
            view.Items.Add(new Fluent.MenuItem { Header = "Navigation pane", IsCheckable = true, IsChecked = true });
            view.Items.Add(new Fluent.MenuItem { Header = "Status bar", IsCheckable = true });

            var help = new Fluent.MenuItem { Header = "_Help" };
            help.Items.Add(new Fluent.MenuItem { Header = "Documentation" });
            help.Items.Add(new Fluent.MenuItem { Header = "About", IsEnabled = false });

            menu.Items.Add(file);
            menu.Items.Add(view);
            menu.Items.Add(help);
            return menu;
        }

        private static UIElement CreateContextMenu()
        {
            var menu = new Fluent.ContextMenu();
            menu.Items.Add(new Fluent.MenuItem { Header = "Edit", Icon = Icon("\uE70F") });
            menu.Items.Add(new Fluent.MenuItem { Header = "Copy", Icon = Icon("\uE8C8") });
            menu.Items.Add(new Fluent.MenuItem { Header = "Delete", Icon = Icon("\uE74D") });
            menu.Items.Add(new Separator());
            var share = new Fluent.MenuItem { Header = "Share" };
            share.Items.Add(new Fluent.MenuItem { Header = "Copy link" });
            share.Items.Add(new Fluent.MenuItem { Header = "Export PDF", IsEnabled = false });
            menu.Items.Add(share);

            return new Fluent.Card
            {
                Padding = new Thickness(18),
                Header = "Right-click target",
                Content = new TextBlock { Text = "Open the context menu on this card.", TextWrapping = TextWrapping.Wrap },
                ContextMenu = menu
            };
        }

        private static UIElement CreateToolTips()
        {
            var save = new Fluent.Button
            {
                Margin = new Thickness(0, 0, 8, 8),
                Content = "Save",
                ToolTip = new Fluent.ToolTip { Content = "Save changes (Ctrl+S)" }
            };
            var delete = new Fluent.Button
            {
                Margin = new Thickness(0, 0, 8, 8),
                Content = "Delete",
                ToolTip = new Fluent.ToolTip { Content = "Delete the selected item" }
            };
            var structuredTip = new StackPanel { MaxWidth = 220 };
            structuredTip.Children.Add(new TextBlock { Text = "Sync status" });
            var detail = new TextBlock { Text = "Last synchronized five minutes ago.", TextWrapping = TextWrapping.Wrap };
            detail.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            structuredTip.Children.Add(detail);

            var sync = new Fluent.Button
            {
                Margin = new Thickness(0, 0, 8, 8),
                Content = "Sync",
                ToolTip = new Fluent.ToolTip { Content = structuredTip }
            };

            return Wrap(save, delete, sync);
        }
    }
}
