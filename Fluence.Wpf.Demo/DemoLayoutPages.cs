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
    internal static class DemoLayoutPages
    {
        public static GalleryControlPage Border()
        {
            return CreatePage(
                "Border",
                "Use Border to draw a Fluent surface around one child element.",
                new[] { Example("Border surface", "Border supports corner radius, padding, stroke, and fill resources.", "Layout/Border.xaml", CreateBorder) });
        }

        public static GalleryControlPage DockPanel()
        {
            return CreatePage(
                "DockPanel",
                "Use DockPanel to dock children to edges while the last child fills the remaining space.",
                new[] { Example("Docked regions", "DockPanel supports edge docking with uniform spacing.", "Layout/DockPanel.xaml", CreateDockPanel) });
        }

        public static GalleryControlPage Expander()
        {
            return CreatePage(
                "Expander",
                "Use Expander to reveal optional details without leaving the page.",
                new[] { Example("Disclosure", "Expander can start opened or collapsed and host arbitrary content.", "Layout/Expander.xaml", CreateExpanders) });
        }

        public static GalleryControlPage Separator()
        {
            return CreatePage(
                "Separator",
                "Use Separator to divide commands, regions, or dense stacked content.",
                new[] { Example("Dividers", "Separator creates a themed horizontal divider between related groups.", "Layout/Separator.xaml", CreateSeparators) });
        }

        public static GalleryControlPage StackPanel()
        {
            return CreatePage(
                "StackPanel",
                "Use StackPanel to arrange children in one direction with optional spacing.",
                new[] { Example("Stacked content", "StackPanel supports vertical and horizontal stacks with consistent spacing.", "Layout/StackPanel.xaml", CreateStackPanels) });
        }

        private static GalleryControlPage CreatePage(string title, string description, DemoExample[] examples)
        {
            return new GalleryControlPage(title, description, examples);
        }

        private static DemoExample Example(string title, string description, string sourcePath, Func<UIElement> createContent)
        {
            return new DemoExample(title, description, sourcePath, createContent);
        }

        private static TextBlock Label(string text)
        {
            return new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Text = text
            };
        }

        private static Fluent.Border Surface(string text)
        {
            var border = new Fluent.Border
            {
                MinWidth = 120,
                MinHeight = 56,
                Padding = new Thickness(14),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Child = Label(text)
            };
            border.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            border.SetResourceReference(System.Windows.Controls.Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
            return border;
        }

        private static UIElement CreateBorder()
        {
            var rounded = Surface("Rounded");
            rounded.Margin = new Thickness(0, 0, 16, 16);
            var subtle = Surface("Subtle fill");
            subtle.Margin = new Thickness(0, 0, 16, 16);
            subtle.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "SubtleFillColorSecondaryBrush");
            var accent = Surface("Accent stroke");
            accent.Margin = new Thickness(0, 0, 16, 16);
            accent.SetResourceReference(System.Windows.Controls.Border.BorderBrushProperty, "AccentFillColorDefaultBrush");
            return new WrapPanel { Children = { rounded, subtle, accent } };
        }

        private static UIElement CreateDockPanel()
        {
            var panel = new Fluent.DockPanel
            {
                Width = 420,
                Height = 220,
                Spacing = 8
            };
            var top = Surface("Top");
            var left = Surface("Left");
            var fill = Surface("Fill");
            System.Windows.Controls.DockPanel.SetDock(top, Dock.Top);
            System.Windows.Controls.DockPanel.SetDock(left, Dock.Left);
            panel.Children.Add(top);
            panel.Children.Add(left);
            panel.Children.Add(fill);
            return panel;
        }

        private static UIElement CreateExpanders()
        {
            var stack = new Fluent.StackPanel { Spacing = 8 };
            stack.Children.Add(new Fluent.Expander { Header = "Open details", IsExpanded = true, Content = new TextBlock { Margin = new Thickness(12), Text = "Expanded content can include any WPF element." } });
            stack.Children.Add(new Fluent.Expander { Header = "Collapsed details", Content = new TextBlock { Margin = new Thickness(12), Text = "This starts collapsed." } });
            return stack;
        }

        private static UIElement CreateSeparators()
        {
            var stack = new Fluent.StackPanel { Spacing = 12 };
            stack.Children.Add(new TextBlock { Text = "First group" });
            stack.Children.Add(new Fluent.Separator());
            stack.Children.Add(new TextBlock { Text = "Second group" });
            return stack;
        }

        private static UIElement CreateStackPanels()
        {
            var vertical = new Fluent.StackPanel { Margin = new Thickness(0, 0, 24, 0), Spacing = 8 };
            vertical.Children.Add(Surface("One"));
            vertical.Children.Add(Surface("Two"));
            vertical.Children.Add(Surface("Three"));

            var horizontal = new Fluent.StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            horizontal.Children.Add(Surface("A"));
            horizontal.Children.Add(Surface("B"));
            horizontal.Children.Add(Surface("C"));

            return new WrapPanel { Children = { vertical, horizontal } };
        }
    }
}
