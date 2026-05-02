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
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Fluence.Wpf;
using Fluence.Wpf.Demo.Pages;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo
{
    internal static class DemoAccessibilityPages
    {
        private const int KeyboardSupportColumns = 4;

        public static GalleryControlPage ScreenReaderSupport()
        {
            return CreatePage(
                "Screen reader support",
                "Use automation names and labels so assistive technologies can identify controls without visible text.",
                new[] { Example("Automation properties", "Icon-only controls need explicit automation names for screen readers.", "Accessibility/AutomationProperties.xaml", CreateAutomationProperties) });
        }

        public static GalleryControlPage KeyboardSupport()
        {
            return CreatePage(
                "Keyboard support",
                "Use predictable tab order and focus visuals so controls work without a pointing device.",
                new[] { Example("Focus and tab order", "Focusable controls follow layout order unless TabIndex intentionally overrides it.", "Accessibility/FocusAndTabOrder.xaml", CreateFocusAndTabOrder) });
        }

        public static GalleryControlPage ColorContrast()
        {
            return CreatePage(
                "Color contrast",
                "Use canonical theme resources so Fluent brushes remap correctly in High Contrast mode.",
                new[] { Example("High contrast brush mapping", "Theme resources resolve to system colors when High Contrast is active.", "Accessibility/HighContrastMapping.xaml", CreateHighContrastMapping) });
        }

        private static GalleryControlPage CreatePage(string title, string description, DemoExample[] examples)
        {
            return new GalleryControlPage(title, description, examples);
        }

        private static DemoExample Example(string title, string description, string sourcePath, Func<UIElement> createContent)
        {
            return new DemoExample(title, description, sourcePath, createContent);
        }

        private static UIElement CreateAutomationProperties()
        {
            var stack = new Fluent.StackPanel { Spacing = 8 };
            var description = new TextBlock
            {
                Text = "Icon-only buttons all have AutomationProperties.Name so Narrator announces their purpose.",
                TextWrapping = TextWrapping.Wrap
            };
            description.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            stack.Children.Add(description);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal };
            buttons.Children.Add(IconButton("\uE8A5", "New document"));
            buttons.Children.Add(IconButton("\uE8E5", "Open file"));
            buttons.Children.Add(IconButton("\uE74E", "Save"));
            buttons.Children.Add(IconButton("\uE74D", "Delete"));
            buttons.Children.Add(IconButton("\uE72D", "Share"));
            stack.Children.Add(buttons);
            return stack;
        }

        private static UIElement CreateFocusAndTabOrder()
        {
            var stack = new Fluent.StackPanel { Spacing = 12 };
            var controls = CreateCenteredControlRow("KeyboardSupportPrimaryControls");
            AddCenteredControl(controls, NamedButton("Button 1", "First focusable button"));
            AddCenteredControl(controls, NamedButton("Button 2", "Second focusable button"));

            var checkBox = new Fluent.CheckBox { Content = "CheckBox" };
            AutomationProperties.SetName(checkBox, "Focusable checkbox");
            AddCenteredControl(controls, checkBox);

            var toggle = new Fluent.ToggleSwitch();
            AutomationProperties.SetName(toggle, "Focusable toggle");
            AddCenteredControl(controls, toggle);

            var textBox = new Fluent.TextBox { Width = 160, PlaceholderText = "TextBox" };
            AutomationProperties.SetName(textBox, "Focusable text input");
            AddCenteredControl(controls, textBox);

            var comboBox = new Fluent.ComboBox { Width = 140 };
            AutomationProperties.SetName(comboBox, "Focusable combo box");
            comboBox.Items.Add(new ComboBoxItem { Content = "Option A", IsSelected = true });
            comboBox.Items.Add(new ComboBoxItem { Content = "Option B" });
            AddCenteredControl(controls, comboBox);

            var slider = new Fluent.Slider { Width = 140, Minimum = 0, Maximum = 100, Value = 40 };
            AutomationProperties.SetName(slider, "Focusable slider");
            AddCenteredControl(controls, slider);

            var link = new Fluent.HyperlinkButton { Content = "HyperlinkButton" };
            AutomationProperties.SetName(link, "Focusable hyperlink");
            AddCenteredControl(controls, link);
            stack.Children.Add(controls);

            var caption = new TextBlock { Text = "The buttons below have an explicit reverse tab order: 3, 2, 1." };
            caption.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            stack.Children.Add(caption);

            var explicitOrder = CreateCenteredControlRow("KeyboardSupportExplicitOrderControls");
            var third = new Fluent.Button { Content = "Tab order: 3", TabIndex = 3 };
            var second = new Fluent.Button { Content = "Tab order: 2", TabIndex = 2 };
            var first = new Fluent.Button { Content = "Tab order: 1 (first)", TabIndex = 1, Appearance = ControlAppearance.Accent };
            AddCenteredControl(explicitOrder, third);
            AddCenteredControl(explicitOrder, second);
            AddCenteredControl(explicitOrder, first);
            stack.Children.Add(explicitOrder);
            return stack;
        }

        private static Grid CreateCenteredControlRow(string name)
        {
            var grid = new Grid
            {
                Name = name,
                Margin = new Thickness(0, 0, 0, 8)
            };

            for (var column = 0; column < KeyboardSupportColumns; column++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            return grid;
        }

        private static void AddCenteredControl(Grid row, FrameworkElement control)
        {
            var index = row.Children.Count;
            var rowIndex = index / KeyboardSupportColumns;
            while (row.RowDefinitions.Count <= rowIndex)
            {
                row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            var column = index % KeyboardSupportColumns;
            control.Margin = new Thickness(0, 0, 12, 0);
            control.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(control, rowIndex);
            Grid.SetColumn(control, column);
            row.Children.Add(control);
        }

        private static UIElement CreateHighContrastMapping()
        {
            var stack = new StackPanel();
            stack.Children.Add(HcRow("Fluence key", "HC system color", "Live swatch", true, null));

            foreach (var pair in HcPairs)
            {
                stack.Children.Add(HcRow(pair[0], pair[1], null, false, pair[0]));
            }

            return stack;
        }

        private static Fluent.Button IconButton(string glyph, string automationName)
        {
            var button = new Fluent.Button
            {
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(8),
                Content = new Fluent.FontIcon { Glyph = glyph, IconFontSize = 18 }
            };
            AutomationProperties.SetName(button, automationName);
            return button;
        }

        private static Fluent.Button NamedButton(string content, string automationName)
        {
            var button = new Fluent.Button
            {
                Margin = new Thickness(0, 0, 8, 8),
                Content = content
            };
            AutomationProperties.SetName(button, automationName);
            return button;
        }

        private static Grid HcRow(string key, string mapping, string swatchText, bool isHeader, string swatchResourceKey)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            grid.Children.Add(HcText(key, 0, isHeader));
            grid.Children.Add(HcText(mapping, 1, isHeader));

            if (swatchResourceKey == null)
            {
                grid.Children.Add(HcText(swatchText, 2, isHeader));
            }
            else
            {
                var swatch = new System.Windows.Controls.Border
                {
                    Width = 32,
                    Height = 16,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Margin = new Thickness(0, 6, 0, 6)
                };
                swatch.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, swatchResourceKey);
                swatch.SetResourceReference(System.Windows.Controls.Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
                Grid.SetColumn(swatch, 2);
                grid.Children.Add(swatch);
            }

            return grid;
        }

        private static TextBlock HcText(string text, int column, bool isHeader)
        {
            var block = new TextBlock
            {
                Padding = new Thickness(12, 6, 12, 6),
                Text = text,
                TextWrapping = TextWrapping.Wrap
            };
            block.SetResourceReference(TextBlock.ForegroundProperty, isHeader ? "TextFillColorPrimaryBrush" : "TextFillColorSecondaryBrush");
            Grid.SetColumn(block, column);
            return block;
        }

        private static readonly string[][] HcPairs = new string[][]
        {
            new string[] { "TextFillColorPrimaryBrush", "WindowText" },
            new string[] { "TextFillColorSecondaryBrush", "WindowText" },
            new string[] { "TextFillColorTertiaryBrush", "GrayText" },
            new string[] { "TextFillColorDisabledBrush", "GrayText" },
            new string[] { "AccentFillColorDefaultBrush", "Highlight" },
            new string[] { "AccentTextFillColorPrimaryBrush", "HotTrack" },
            new string[] { "ControlFillColorDefaultBrush", "Control" },
            new string[] { "ControlStrokeColorDefaultBrush", "ControlDark" },
            new string[] { "FocusStrokeColorOuterBrush", "Highlight" },
            new string[] { "FocusStrokeColorInnerBrush", "HighlightText" },
            new string[] { "CardBackgroundFillColorDefaultBrush", "Control" },
            new string[] { "SolidBackgroundFillColorBaseBrush", "Window" }
        };
    }
}
