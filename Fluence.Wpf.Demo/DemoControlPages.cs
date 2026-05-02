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
    internal static class DemoControlPages
    {
        public static GalleryControlPage Button()
        {
            return CreatePage(
                "Button",
                "Use Button for immediate actions.",
                new[]
                {
                    Example("Appearances", "Standard, accent, subtle, and disabled button states.", "Buttons/ButtonAppearances.xaml", CreateButtonAppearances),
                    Example("Icons", "Buttons can place Fluent icons before or after their text.", "Buttons/ButtonIcons.xaml", CreateButtonIcons)
                });
        }

        public static GalleryControlPage DropDownButton()
        {
            return CreatePage(
                "DropDownButton",
                "Use DropDownButton when one command opens a related menu or lightweight flyout.",
                new[] { Example("Flyout content", "The flyout can contain command buttons or arbitrary WPF content.", "Buttons/DropDownButtons.xaml", CreateDropDownButtons) });
        }

        public static GalleryControlPage HyperlinkButton()
        {
            return CreatePage(
                "HyperlinkButton",
                "Use HyperlinkButton for lightweight navigation and related secondary actions.",
                new[] { Example("Navigation links", "Hyperlink buttons can show text, icons, or disabled states.", "Buttons/HyperlinkButtons.xaml", CreateHyperlinkButtons) });
        }

        public static GalleryControlPage RepeatButton()
        {
            return CreatePage(
                "RepeatButton",
                "Use RepeatButton for actions that repeat while pressed.",
                new[] { Example("Repeat actions", "Repeat buttons support standard, accent, and disabled states.", "Buttons/RepeatButtons.xaml", CreateRepeatButtons) });
        }

        public static GalleryControlPage ToggleButton()
        {
            return CreatePage(
                "ToggleButton",
                "Use ToggleButton for a command that can remain on or off.",
                new[] { Example("Toggle states", "Toggle buttons show checked, unchecked, accent, and disabled states.", "Buttons/ToggleButtons.xaml", CreateToggleButtons) });
        }

        public static GalleryControlPage SplitButton()
        {
            return CreatePage(
                "SplitButton",
                "Use SplitButton when a primary command has related secondary choices.",
                new[] { Example("Primary and secondary actions", "The primary side invokes a command; the chevron opens a flyout.", "Buttons/SplitButtons.xaml", CreateSplitButtons) });
        }

        public static GalleryControlPage CheckBox()
        {
            return CreatePage(
                "CheckBox",
                "Use CheckBox for independent binary or tri-state choices.",
                new[] { Example("Check states", "CheckBox supports checked, unchecked, indeterminate, disabled, and description text.", "Selection/CheckBoxStates.xaml", CreateCheckBoxes) });
        }

        public static GalleryControlPage ComboBox()
        {
            return CreatePage(
                "ComboBox",
                "Use ComboBox when users choose one value from a compact list.",
                new[] { Example("Selection", "ComboBox supports placeholders, icon content, disabled state, and standard selection.", "Selection/ComboBoxSelection.xaml", CreateComboBoxes) });
        }

        public static GalleryControlPage RadioButton()
        {
            return CreatePage(
                "RadioButton",
                "Use RadioButton for mutually exclusive choices in a visible group.",
                new[] { Example("Radio groups", "Radio buttons with the same GroupName allow one active choice.", "Selection/RadioButtonGroups.xaml", CreateRadioButtons) });
        }

        public static GalleryControlPage RatingControl()
        {
            return CreatePage(
                "RatingControl",
                "Use RatingControl to capture or display a star rating.",
                new[] { Example("Rating states", "RatingControl supports interactive, read-only, custom maximum, and disabled states.", "Selection/RatingControl.xaml", CreateRatingControls) });
        }

        public static GalleryControlPage Slider()
        {
            return CreatePage(
                "Slider",
                "Use Slider for approximate numeric input across a bounded range.",
                new[] { Example("Slider input", "Sliders support continuous values, tick snapping, vertical layout, and disabled state.", "Inputs/SliderInput.xaml", CreateSliders) });
        }

        public static GalleryControlPage ToggleSwitch()
        {
            return CreatePage(
                "ToggleSwitch",
                "Use ToggleSwitch for on/off settings that take effect immediately.",
                new[] { Example("Switch states", "ToggleSwitch supports on, off, custom labels, and disabled state.", "Selection/ToggleSwitchStates.xaml", CreateToggleSwitches) });
        }

        public static GalleryControlPage TextBox()
        {
            return CreatePage(
                "TextBox",
                "Use TextBox for single-line text entry.",
                new[]
                {
                    Example("Text input", "TextBox supports placeholders, icons, and maximum length.", "Inputs/TextBoxInput.xaml", CreateTextBoxes),
                    Example("Validation", "Validation states can be surfaced inline with the text input.", "Inputs/TextBoxValidation.xaml", CreateTextBoxValidation)
                });
        }

        public static GalleryControlPage PasswordBox()
        {
            return CreatePage(
                "PasswordBox",
                "Use PasswordBox for obscured text entry.",
                new[] { Example("Password input", "PasswordBox supports placeholder text, reveal behavior, and disabled state.", "Inputs/PasswordBoxInput.xaml", CreatePasswordBoxes) });
        }

        public static GalleryControlPage NumberBox()
        {
            return CreatePage(
                "NumberBox",
                "Use NumberBox for numeric input with optional spin buttons and range limits.",
                new[] { Example("Number input", "NumberBox supports min/max coercion, spin buttons, and expression entry.", "Inputs/NumberBoxInput.xaml", CreateNumberBoxes) });
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
            AddChildren(panel, children);
            return panel;
        }

        private static StackPanel Stack(params UIElement[] children)
        {
            var panel = new StackPanel();
            AddChildren(panel, children);
            return panel;
        }

        private static void AddChildren(Panel panel, params UIElement[] children)
        {
            foreach (var child in children)
            {
                panel.Children.Add(child);
            }
        }

        private static Fluent.Button DemoButton(string content)
        {
            return new Fluent.Button
            {
                Margin = new Thickness(0, 0, 8, 8),
                Content = content
            };
        }

        private static UIElement CreateButtonAppearances()
        {
            var accent = DemoButton("Accent");
            accent.Appearance = ControlAppearance.Accent;
            var subtle = DemoButton("Subtle");
            subtle.Appearance = ControlAppearance.Subtle;
            var disabled = DemoButton("Disabled");
            disabled.IsEnabled = false;
            return Wrap(DemoButton("Standard"), accent, subtle, disabled);
        }

        private static UIElement CreateButtonIcons()
        {
            var iconLeft = DemoButton("Icon Left");
            iconLeft.Icon = Icon("\uE774");
            var iconRight = DemoButton("Icon Right");
            iconRight.Icon = Icon("\uE8D6");
            iconRight.IconPlacement = ElementPlacement.Right;
            return Wrap(iconLeft, iconRight);
        }

        private static UIElement CreateDropDownButtons()
        {
            var create = new Fluent.DropDownButton
            {
                Margin = new Thickness(0, 0, 8, 8),
                Content = "New",
                Flyout = CreateFlyout("Document", "Spreadsheet", "Folder")
            };
            var details = new Fluent.DropDownButton
            {
                Margin = new Thickness(0, 0, 8, 8),
                Content = "Details",
                Flyout = TextFlyout("Project status", "Flyout content can be any WPF content.")
            };
            var disabled = new Fluent.DropDownButton
            {
                Margin = new Thickness(0, 0, 8, 8),
                Content = "Disabled",
                IsEnabled = false,
                Flyout = new TextBlock { Margin = new Thickness(12), Text = "Unavailable" }
            };
            return Wrap(create, details, disabled);
        }

        private static UIElement CreateHyperlinkButtons()
        {
            return Wrap(
                new Fluent.HyperlinkButton
                {
                    Margin = new Thickness(0, 0, 8, 8),
                    Content = "Repository",
                    NavigateUri = new Uri("https://github.com/sintaxasn/Fluence.Wpf")
                },
                new Fluent.HyperlinkButton
                {
                    Margin = new Thickness(0, 0, 8, 8),
                    Content = "Releases",
                    NavigateUri = new Uri("https://github.com/sintaxasn/Fluence.Wpf/releases")
                },
                new Fluent.HyperlinkButton
                {
                    Margin = new Thickness(0, 0, 8, 8),
                    Content = "With icon",
                    Icon = Icon("\uE71B"),
                    NavigateUri = new Uri("https://github.com/sintaxasn/Fluence.Wpf")
                },
                new Fluent.HyperlinkButton
                {
                    Margin = new Thickness(0, 0, 8, 8),
                    Content = "Disabled",
                    IsEnabled = false
                });
        }

        private static UIElement CreateRepeatButtons()
        {
            var accent = new Fluent.RepeatButton
            {
                Margin = new Thickness(0, 0, 8, 8),
                Appearance = ControlAppearance.Accent,
                Content = "Accent repeat"
            };
            return Wrap(
                new Fluent.RepeatButton { Margin = new Thickness(0, 0, 8, 8), Content = "Hold to repeat" },
                accent,
                new Fluent.RepeatButton { Margin = new Thickness(0, 0, 8, 8), Content = "Disabled", IsEnabled = false });
        }

        private static UIElement CreateToggleButtons()
        {
            var accent = new Fluent.ToggleButton
            {
                Margin = new Thickness(0, 0, 8, 8),
                Appearance = ControlAppearance.Accent,
                Content = "Pinned",
                IsChecked = true
            };
            return Wrap(
                new Fluent.ToggleButton { Margin = new Thickness(0, 0, 8, 8), Content = "Bold", IsChecked = true },
                new Fluent.ToggleButton { Margin = new Thickness(0, 0, 8, 8), Content = "Italic" },
                accent,
                new Fluent.ToggleButton { Margin = new Thickness(0, 0, 8, 8), Content = "Disabled", IsEnabled = false });
        }

        private static UIElement CreateSplitButtons()
        {
            var accent = new Fluent.SplitButton
            {
                Margin = new Thickness(0, 0, 8, 8),
                Appearance = ControlAppearance.Accent,
                Content = "Publish",
                Flyout = CreateFlyout("Publish draft", "Schedule publish")
            };
            return Wrap(
                new Fluent.SplitButton { Margin = new Thickness(0, 0, 8, 8), Content = "Save", Flyout = CreateFlyout("Save as", "Save a copy", "Export") },
                accent,
                new Fluent.SplitButton { Margin = new Thickness(0, 0, 8, 8), Content = "Disabled", IsEnabled = false, Flyout = new TextBlock { Margin = new Thickness(12), Text = "Unavailable" } });
        }

        private static UIElement CreateCheckBoxes()
        {
            return Stack(
                Wrap(
                    new Fluent.CheckBox { Margin = new Thickness(0, 0, 32, 10), Content = "Checked", IsChecked = true },
                    new Fluent.CheckBox { Margin = new Thickness(0, 0, 32, 10), Content = "Unchecked" },
                    new Fluent.CheckBox { Margin = new Thickness(0, 0, 32, 10), Content = "Indeterminate", IsChecked = null, IsThreeState = true },
                    new Fluent.CheckBox { Margin = new Thickness(0, 0, 32, 10), Content = "Disabled", IsChecked = true, IsEnabled = false }),
                new Fluent.CheckBox { Content = "With description", Description = "Additional detail about this option" });
        }

        private static UIElement CreateComboBoxes()
        {
            var combo = new Fluent.ComboBox
            {
                Name = "SelectionDemoCombo",
                Width = 480,
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Left,
                PlaceholderText = "Choose an option...",
                SelectedIndex = -1
            };
            combo.Items.Add(new ComboBoxItem { Content = "First item" });
            combo.Items.Add(new ComboBoxItem { Content = "Second item" });
            combo.Items.Add(new ComboBoxItem { Content = "Third item" });

            var iconCombo = new Fluent.ComboBox
            {
                Width = 480,
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Left,
                PlaceholderText = "With icon",
                Icon = Icon("\uE721"),
                SelectedIndex = -1
            };
            iconCombo.Items.Add(new ComboBoxItem { Content = "Alpha" });
            iconCombo.Items.Add(new ComboBoxItem { Content = "Beta" });
            iconCombo.Items.Add(new ComboBoxItem { Content = "Gamma" });

            return Stack(
                combo,
                iconCombo,
                new Fluent.ComboBox { Width = 480, HorizontalAlignment = HorizontalAlignment.Left, IsEnabled = false, PlaceholderText = "Disabled" });
        }

        private static UIElement CreateRadioButtons()
        {
            return Stack(
                new Fluent.RadioButton { Margin = new Thickness(0, 0, 0, 8), Content = "Email", GroupName = "Contact", IsChecked = true },
                new Fluent.RadioButton { Margin = new Thickness(0, 0, 0, 8), Content = "Phone", GroupName = "Contact" },
                new Fluent.RadioButton { Margin = new Thickness(0, 0, 0, 8), Content = "Mail", GroupName = "Contact" },
                new Fluent.RadioButton { Content = "Disabled", GroupName = "Contact", IsEnabled = false });
        }

        private static UIElement CreateRatingControls()
        {
            return Stack(
                new Fluent.RatingControl { Margin = new Thickness(0, 0, 0, 12), Value = 3, Caption = "Editable rating" },
                new Fluent.RatingControl { Margin = new Thickness(0, 0, 0, 12), Value = 4, IsReadOnly = true, Caption = "Read-only" },
                new Fluent.RatingControl { Margin = new Thickness(0, 0, 0, 12), Value = 6, MaxRating = 10, Caption = "Ten point rating" },
                new Fluent.RatingControl { Value = 2, IsEnabled = false, Caption = "Disabled" });
        }

        private static UIElement CreateSliders()
        {
            var vertical = new Fluent.Slider
            {
                Height = 140,
                Margin = new Thickness(0, 0, 24, 0),
                Maximum = 100,
                Minimum = 0,
                Orientation = Orientation.Vertical,
                Value = 40
            };
            var disabledVertical = new Fluent.Slider
            {
                Height = 140,
                IsEnabled = false,
                Maximum = 100,
                Minimum = 0,
                Orientation = Orientation.Vertical,
                Value = 25
            };
            return Stack(
                new TextBlock { Margin = new Thickness(0, 0, 0, 6), Text = "Default" },
                new Fluent.Slider { Margin = new Thickness(0, 0, 0, 14), Maximum = 100, Minimum = 0, Value = 35 },
                new TextBlock { Margin = new Thickness(0, 0, 0, 6), Text = "Snapped to ticks" },
                new Fluent.Slider { Margin = new Thickness(0, 0, 0, 14), IsSnapToTickEnabled = true, Maximum = 10, Minimum = 0, TickFrequency = 1, Value = 4 },
                Wrap(vertical, disabledVertical));
        }

        private static UIElement CreateToggleSwitches()
        {
            return Stack(
                new Fluent.ToggleSwitch { Name = "DefaultToggle", Margin = new Thickness(0, 0, 0, 12), OnContent = "On", OffContent = "Off" },
                new Fluent.ToggleSwitch { Margin = new Thickness(0, 0, 0, 12), IsChecked = true, OnContent = "Enabled", OffContent = "Disabled" },
                new Fluent.ToggleSwitch { IsEnabled = false, OffContent = "Disabled" });
        }

        private static UIElement CreateTextBoxes()
        {
            return Stack(
                new Fluent.TextBox { Width = 480, Margin = new Thickness(0, 0, 0, 12), HorizontalAlignment = HorizontalAlignment.Left, PlaceholderText = "Basic text box..." },
                new Fluent.TextBox { Width = 480, Margin = new Thickness(0, 0, 0, 12), HorizontalAlignment = HorizontalAlignment.Left, PlaceholderText = "Search", Icon = Icon("\uE721") },
                new Fluent.TextBox { Name = "CharCountTextBox", Width = 480, HorizontalAlignment = HorizontalAlignment.Left, MaxLength = 40, PlaceholderText = "Limited to 40 characters..." });
        }

        private static UIElement CreateTextBoxValidation()
        {
            return Stack(
                new Fluent.TextBox { Width = 480, Margin = new Thickness(0, 0, 0, 12), HorizontalAlignment = HorizontalAlignment.Left, Text = "name@example.com" },
                new Fluent.TextBox { Width = 480, HorizontalAlignment = HorizontalAlignment.Left, PlaceholderText = "Enter a value..." });
        }

        private static UIElement CreatePasswordBoxes()
        {
            return Stack(
                new Fluent.PasswordBox { Width = 480, Margin = new Thickness(0, 0, 0, 12), HorizontalAlignment = HorizontalAlignment.Left, PlaceholderText = "Password..." },
                new Fluent.PasswordBox { Width = 480, Margin = new Thickness(0, 0, 0, 12), HorizontalAlignment = HorizontalAlignment.Left, Password = "secret", RevealButtonEnabled = true },
                new Fluent.PasswordBox { Width = 480, HorizontalAlignment = HorizontalAlignment.Left, IsEnabled = false, PlaceholderText = "Disabled" });
        }

        private static UIElement CreateNumberBoxes()
        {
            return Stack(
                new Fluent.NumberBox { Width = 240, Margin = new Thickness(0, 0, 0, 12), HorizontalAlignment = HorizontalAlignment.Left, Header = "Quantity", Minimum = 0, Maximum = 100, Value = 42 },
                new Fluent.NumberBox { Width = 240, Margin = new Thickness(0, 0, 0, 12), HorizontalAlignment = HorizontalAlignment.Left, Header = "Expression", AcceptsExpression = true, PlaceholderText = "1 + 2" },
                new Fluent.NumberBox { Width = 240, HorizontalAlignment = HorizontalAlignment.Left, Header = "Disabled", IsEnabled = false, Value = 10 });
        }

        private static StackPanel CreateFlyout(params string[] items)
        {
            var panel = new StackPanel { MinWidth = 180, Margin = new Thickness(4) };
            foreach (var item in items)
            {
                panel.Children.Add(new Fluent.Button
                {
                    Appearance = ControlAppearance.Subtle,
                    Content = item,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                });
            }

            return panel;
        }

        private static StackPanel TextFlyout(string title, string body)
        {
            var panel = new StackPanel { MaxWidth = 260, Margin = new Thickness(12) };
            var titleBlock = new TextBlock { Margin = new Thickness(0, 0, 0, 6), Text = title };
            titleBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            var bodyBlock = new TextBlock { Text = body, TextWrapping = TextWrapping.Wrap };
            bodyBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            panel.Children.Add(titleBlock);
            panel.Children.Add(bodyBlock);
            return panel;
        }
    }
}
