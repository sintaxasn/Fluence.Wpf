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
using System.Windows.Media;
using Fluence.Wpf;
using Fluence.Wpf.Demo.Pages;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo
{
    internal static class DemoWindowingPages
    {
        public static GalleryControlPage CaptionButtonChrome()
        {
            return CreatePage(
                "CaptionButtonChrome",
                "CaptionButtonChrome is the internal FluenceWindow caption-button presenter; applications use the public FluenceWindow caption properties.",
                new[] { Example("Caption button states", "Change minimize, maximize, and close visibility through FluenceWindow properties.", "Window/BackdropAndCaptionButtons.xaml", CreateCaptionButtonChrome) });
        }

        public static GalleryControlPage FluenceWindow()
        {
            return CreatePage(
                "FluenceWindow",
                "Use FluenceWindow for themed window chrome, backdrop material, title visibility, and accent-aware surfaces.",
                new[] { Example("Theme and accent", "Apply a theme or accent color to the active FluenceWindow.", "Window/ThemeAndAccent.xaml", CreateFluenceWindow) });
        }

        public static GalleryControlPage TitleBar()
        {
            return CreatePage(
                "TitleBar",
                "Use TitleBar when a window needs Fluent title content with an icon, optional back button, and custom content.",
                new[] { Example("Title content", "TitleBar supports title text, icon content, compact mode, and custom content.", "Window/TitleBar.xaml", CreateTitleBar) });
        }

        private static GalleryControlPage CreatePage(string title, string description, DemoExample[] examples)
        {
            return new GalleryControlPage(title, description, examples);
        }

        private static DemoExample Example(string title, string description, string sourcePath, Func<UIElement> createContent)
        {
            return new DemoExample(title, description, sourcePath, createContent);
        }

        private static UIElement CreateCaptionButtonChrome()
        {
            var stack = new Fluent.StackPanel { Spacing = 12 };
            stack.Children.Add(CaptionComboGrid());

            var toggles = new WrapPanel();
            var iconToggle = new Fluent.CheckBox { Margin = new Thickness(0, 0, 14, 0), Content = "Show Icon", IsChecked = true };
            var titleToggle = new Fluent.CheckBox { Content = "Show Title", IsChecked = true };
            iconToggle.Checked += WindowChromeToggleChanged;
            iconToggle.Unchecked += WindowChromeToggleChanged;
            titleToggle.Checked += WindowChromeToggleChanged;
            titleToggle.Unchecked += WindowChromeToggleChanged;
            toggles.Children.Add(iconToggle);
            toggles.Children.Add(titleToggle);
            stack.Children.Add(toggles);
            return stack;
        }

        private static UIElement CreateFluenceWindow()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var themeStack = new Fluent.StackPanel { Spacing = 8 };
            themeStack.Children.Add(SectionLabel("Theme"));
            var themeButtons = new WrapPanel();
            themeButtons.Children.Add(ThemeRadio("ThemeLight", "Light", ApplicationTheme.Light));
            themeButtons.Children.Add(ThemeRadio("ThemeDark", "Dark", ApplicationTheme.Dark));
            themeButtons.Children.Add(ThemeRadio("ThemeHighContrast", "HighContrast", ApplicationTheme.HighContrast));
            themeButtons.Children.Add(ThemeRadio("ThemeAuto", "Auto", ApplicationTheme.Auto));
            themeStack.Children.Add(themeButtons);
            var watcherRow = new StackPanel { Orientation = Orientation.Horizontal };
            var watcherToggle = new Fluent.ToggleSwitch { Name = "ThemeWatcherToggle", Margin = new Thickness(0, 0, 10, 0), IsChecked = true };
            watcherToggle.Checked += ThemeWatcherToggled;
            watcherToggle.Unchecked += ThemeWatcherToggled;
            watcherRow.Children.Add(watcherToggle);
            var watcherLabel = new TextBlock { Name = "SystemThemeLabel", VerticalAlignment = VerticalAlignment.Center, Text = "Watching: Yes" };
            watcherLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            watcherRow.Children.Add(watcherLabel);
            themeStack.Children.Add(watcherRow);
            var stateLabel = new TextBlock { Name = "ThemeStateLabel", Text = "Current: Auto" };
            stateLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            themeStack.Children.Add(stateLabel);
            themeStack.Children.Add(BackdropPicker());
            grid.Children.Add(themeStack);

            var accentStack = new Fluent.StackPanel { Spacing = 8 };
            Grid.SetColumn(accentStack, 2);
            accentStack.Children.Add(SectionLabel("Accent color"));
            var swatches = new WrapPanel();
            swatches.Children.Add(AccentSwatch("#E81123"));
            swatches.Children.Add(AccentSwatch("#DA3B01"));
            swatches.Children.Add(AccentSwatch("#0078D4"));
            swatches.Children.Add(AccentSwatch("#8A00C4"));
            swatches.Children.Add(AccentSwatch("#107C10"));
            swatches.Children.Add(AccentSwatch("#498205"));
            swatches.Children.Add(AccentSwatch("#8764B8"));
            swatches.Children.Add(AccentSwatch("#038387"));
            accentStack.Children.Add(swatches);
            var system = new Fluent.Button { MinWidth = 80, HorizontalAlignment = HorizontalAlignment.Left, Content = "System" };
            system.Click += delegate { ApplicationAccentColorManager.ApplySystemAccent(); };
            accentStack.Children.Add(system);
            grid.Children.Add(accentStack);

            return grid;
        }

        private static UIElement CreateTitleBar()
        {
            var stack = new Fluent.StackPanel { Spacing = 12 };
            stack.Children.Add(TitleBarPreview(false));
            stack.Children.Add(TitleBarPreview(true));
            return stack;
        }

        private static Grid CaptionComboGrid()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(112) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(112) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddCaptionCombo(grid, "Minimize", 0, 0);
            AddCaptionCombo(grid, "Maximize", 0, 3);
            AddCaptionCombo(grid, "Close", 2, 0);
            return grid;
        }

        private static void AddCaptionCombo(Grid grid, string label, int row, int column)
        {
            var text = new TextBlock { Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, Text = label };
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            Grid.SetRow(text, row);
            Grid.SetColumn(text, column);
            grid.Children.Add(text);

            var combo = new Fluent.ComboBox { Width = 112, Tag = label };
            combo.Items.Add(new ComboBoxItem { Content = "Default", IsSelected = true });
            combo.Items.Add(new ComboBoxItem { Content = "Enable" });
            combo.Items.Add(new ComboBoxItem { Content = "Disable" });
            combo.Items.Add(new ComboBoxItem { Content = "Hide" });
            combo.SelectionChanged += CaptionOverrideComboChanged;
            Grid.SetRow(combo, row);
            Grid.SetColumn(combo, column + 1);
            grid.Children.Add(combo);
        }

        private static RadioButton ThemeRadio(string name, string content, ApplicationTheme theme)
        {
            var radio = new Fluent.RadioButton
            {
                Name = name,
                Margin = new Thickness(0, 0, 12, 8),
                Content = content,
                GroupName = "WindowThemeGroup",
                IsChecked = theme == ApplicationTheme.Auto,
                Tag = theme
            };
            radio.Checked += ThemeRadioChecked;
            return radio;
        }

        private static UIElement BackdropPicker()
        {
            var stack = new Fluent.StackPanel { Spacing = 6 };
            stack.Children.Add(SectionLabel("Backdrop"));
            var combo = new Fluent.ComboBox { Name = "BackdropCombo", Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
            combo.Items.Add(new ComboBoxItem { Content = "Auto" });
            combo.Items.Add(new ComboBoxItem { Content = "None" });
            combo.Items.Add(new ComboBoxItem { Content = "Mica" });
            combo.Items.Add(new ComboBoxItem { Content = "Acrylic" });
            combo.Items.Add(new ComboBoxItem { Content = "Tabbed" });
            combo.SelectedIndex = 2;
            combo.SelectionChanged += BackdropSelectionChanged;
            stack.Children.Add(combo);
            return stack;
        }

        private static Fluent.Button AccentSwatch(string hex)
        {
            var button = new Fluent.Button
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 8),
                Content = string.Empty,
                Tag = hex,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex))
            };
            button.Click += AccentSwatchClicked;
            return button;
        }

        private static TextBlock SectionLabel(string text)
        {
            var block = new TextBlock { FontWeight = FontWeights.SemiBold, Text = text };
            block.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            return block;
        }

        private static UIElement TitleBarPreview(bool compact)
        {
            var titleBar = new Fluent.TitleBar
            {
                Title = compact ? "Compact title bar" : "Documents",
                Icon = new Fluent.FontIcon { Glyph = "\uE8A5", IconFontSize = 14 },
                IsBackButtonVisible = true,
                IsCompact = compact,
                CustomContent = new Fluent.TextBox { Width = 180, PlaceholderText = "Search" }
            };

            var surface = new System.Windows.Controls.Border
            {
                Width = 520,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Child = titleBar
            };
            surface.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            surface.SetResourceReference(System.Windows.Controls.Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
            return surface;
        }

        private static void ThemeRadioChecked(object sender, RoutedEventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio == null || !(radio.Tag is ApplicationTheme))
            {
                return;
            }

            var theme = (ApplicationTheme)radio.Tag;
            var window = Window.GetWindow(radio) as Fluent.FluenceWindow;
            var backdrop = window != null ? window.WindowBackdrop : BackdropType.Mica;
            ApplicationThemeManager.Apply(theme, backdrop, true);
            var stateLabel = FindNamedDescendant<TextBlock>(window, "ThemeStateLabel");
            if (stateLabel != null)
            {
                stateLabel.Text = string.Format("Current: {0}", theme);
            }
        }

        private static void BackdropSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null || !combo.IsLoaded)
            {
                return;
            }

            var window = Window.GetWindow(combo) as Fluent.FluenceWindow;
            if (window == null)
            {
                return;
            }

            var backdrop = GetBackdrop(combo.SelectedIndex);
            window.WindowBackdrop = backdrop;
            ApplicationThemeManager.Apply(ApplicationThemeManager.CurrentTheme, backdrop, false);
        }

        private static void AccentSwatchClicked(object sender, RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;
            var hex = button != null ? button.Tag as string : null;
            if (string.IsNullOrEmpty(hex))
            {
                return;
            }

            try
            {
                var converted = ColorConverter.ConvertFromString(hex);
                if (converted != null)
                {
                    ApplicationAccentColorManager.ApplyCustomAccent((Color)converted);
                }
            }
            catch (FormatException)
            {
            }
        }

        private static void ThemeWatcherToggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as Fluent.ToggleSwitch;
            if (toggle == null || !toggle.IsLoaded)
            {
                return;
            }

            var window = Window.GetWindow(toggle);
            if (window == null)
            {
                return;
            }

            var label = FindNamedDescendant<TextBlock>(window, "SystemThemeLabel");
            if (toggle.IsChecked == true)
            {
                SystemThemeWatcher.Watch(window);
                if (label != null)
                {
                    label.Text = "Watching: Yes";
                }
            }
            else
            {
                SystemThemeWatcher.UnWatch(window);
                if (label != null)
                {
                    label.Text = "Watching: No";
                }
            }
        }

        private static void CaptionOverrideComboChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null || !combo.IsLoaded)
            {
                return;
            }

            var window = Window.GetWindow(combo) as Fluent.FluenceWindow;
            if (window == null)
            {
                return;
            }

            var target = combo.Tag as string;
            if (string.Equals(target, "Minimize", StringComparison.Ordinal))
            {
                ApplyCaptionOverride(combo, delegate(Visibility value) { window.MinimizeButtonVisibility = value; }, delegate(bool enabled) { window.IsMinimizable = enabled; });
            }
            else if (string.Equals(target, "Maximize", StringComparison.Ordinal))
            {
                ApplyCaptionOverride(combo, delegate(Visibility value) { window.MaximizeButtonVisibility = value; }, delegate(bool enabled) { window.IsMaximizable = enabled; });
            }
            else if (string.Equals(target, "Close", StringComparison.Ordinal))
            {
                ApplyCaptionOverride(combo, delegate(Visibility value) { window.CloseButtonVisibility = value; }, delegate(bool enabled) { window.IsClosable = enabled; });
            }
        }

        private static void WindowChromeToggleChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox == null)
            {
                return;
            }

            var main = Window.GetWindow(checkBox) as MainWindow;
            if (main == null)
            {
                return;
            }

            if (string.Equals(checkBox.Content as string, "Show Icon", StringComparison.Ordinal))
            {
                main.SetUserShowIcon(checkBox.IsChecked == true, main.Icon);
            }
            else if (string.Equals(checkBox.Content as string, "Show Title", StringComparison.Ordinal))
            {
                main.SetUserShowTitle(checkBox.IsChecked == true, main.Title);
            }
        }

        private static BackdropType GetBackdrop(int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 1:
                    return BackdropType.None;
                case 2:
                    return BackdropType.Mica;
                case 3:
                    return BackdropType.Acrylic;
                case 4:
                    return BackdropType.Tabbed;
                default:
                    return BackdropType.Auto;
            }
        }

        private static void ApplyCaptionOverride(ComboBox combo, Action<Visibility> setVisibility, Action<bool> setEnabled)
        {
            var item = combo.SelectedItem as ComboBoxItem;
            var content = item != null ? item.Content as string : null;
            if (string.Equals(content, "Hide", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Collapsed);
                setEnabled(false);
            }
            else if (string.Equals(content, "Disable", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Visible);
                setEnabled(false);
            }
            else
            {
                setVisibility(Visibility.Visible);
                setEnabled(true);
            }
        }

        private static T FindNamedDescendant<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            if (root == null)
            {
                return null;
            }

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var element = child as T;
                if (element != null && string.Equals(element.Name, name, StringComparison.Ordinal))
                {
                    return element;
                }

                var nested = FindNamedDescendant<T>(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
