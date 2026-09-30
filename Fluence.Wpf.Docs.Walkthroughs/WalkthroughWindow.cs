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

namespace Fluence.Wpf.Docs.Walkthroughs
{
    internal sealed class WalkthroughWindow : Controls.FluenceWindow
    {
        private readonly StackPanel _content;
        private readonly Controls.ComboBox _scenarioSelector;

        internal (string Slug, string Title)[] Scenarios { get; }

        internal WalkthroughWindow((string Slug, string Title)[] scenarios)
        {
            Scenarios = scenarios;
            Title = "Fluence WPF walkthroughs";
            Width = 900;
            Height = 600;
            MinWidth = 720;
            MinHeight = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SystemBackdropType = WindowBackdropType.Mica;
            ExtendsContentIntoTitleBar = true;
            TitleBar = new Controls.TitleBar { Title = "Fluence WPF walkthroughs", Subtitle = "Documentation sample" };
            SetResourceReference(BackgroundProperty, "ApplicationBackgroundBrush");

            Grid root = new() { Margin = new Thickness(40, 32, 40, 28) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _scenarioSelector = new Controls.ComboBox { Width = 260, HorizontalAlignment = HorizontalAlignment.Right };
            foreach ((_, string title) in scenarios)
            {
                _ = _scenarioSelector.Items.Add(title);
            }

            _scenarioSelector.SelectionChanged += (_, _) =>
            {
                if (_scenarioSelector.SelectedIndex >= 0)
                {
                    ShowScenario(Scenarios[_scenarioSelector.SelectedIndex].Slug);
                }
            };
            Grid.SetRow(_scenarioSelector, 0);
            _ = root.Children.Add(_scenarioSelector);

            _content = new StackPanel { Margin = new Thickness(24), VerticalAlignment = VerticalAlignment.Center };
            Controls.Card surface = new() { Content = _content, Margin = new Thickness(0, 24, 0, 0) };
            Grid.SetRow(surface, 1);
            _ = root.Children.Add(surface);
            Content = root;
            _scenarioSelector.SelectedIndex = 0;
        }

        internal void ShowScenario(string slug)
        {
            int index = Array.FindIndex(Scenarios, entry => string.Equals(entry.Slug, slug, StringComparison.Ordinal));
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slug));
            }

            if (_scenarioSelector.SelectedIndex != index)
            {
                _scenarioSelector.SelectedIndex = index;
            }

            _content.Children.Clear();
            switch (slug)
            {
                case "basic-usage":
                    AddHeading("My first Fluent window", "A Mica window with a text box and an accent button.");
                    Controls.TextBox name = new() { Width = 330, PlaceholderText = "Your name", Text = "Taylor" };
                    Add(name);
                    Controls.TextBlock greeting = new() { Text = "Hello, Taylor!" };
                    AddButton("Say hello", () => greeting.Text = $"Hello, {name.Text}!");
                    Add(greeting);
                    break;
                case "window-and-title-bar":
                    AddHeading("Configure the window", "A custom title bar can host navigation and search.");
                    Controls.TitleBar exampleTitleBar = new()
                    {
                        Title = "Project workspace",
                        Subtitle = "Documents",
                        IsBackButtonVisible = true,
                        IsPaneToggleButtonVisible = true,
                        CustomContent = new Controls.TextBox { Width = 230, PlaceholderText = "Search documents" },
                    };
                    Add(exampleTitleBar);
                    AddLabel("The shell requests Mica and rounded corners where Windows supports them.");
                    AddButton("Open a document", () => AddLabel("Document opened."));
                    break;
                case "theme-and-accent":
                    AddHeading("Theme, accent and backdrop", "Apply light or dark resources and choose an accent.");
                    AddLabel("The controls below use dynamic theme resources.");
                    AddButton("Use blue accent", static () => ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4)));
                    AddButton("Use purple accent", static () => ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x88, 0x57, 0xB7)));
                    AddLabel("Backdrop request: Mica");
                    break;
                case "inputs-and-data":
                    AddHeading("Inputs and data", "Collect values, then display submitted entries.");
                    Add(new Controls.TextBox { Width = 330, PlaceholderText = "Display name", Text = "Taylor" });
                    Add(new Controls.NumberBox { Width = 190, PlaceholderText = "Quantity", Value = 3 });
                    Controls.ListView entries = new() { Height = 120, Width = 420 };
                    _ = entries.Items.Add("Taylor   ·   3 items");
                    _ = entries.Items.Add("Morgan   ·   5 items");
                    Add(entries);
                    break;
                case "navigation-and-tabs":
                    AddHeading("Navigation and tabs", "Use destinations for pages and tabs for parallel work.");
                    Controls.NavigationView navigation = new() { Height = 260, IsPaneOpen = true };
                    _ = navigation.Items.Add(new Controls.NavigationViewItem { Content = "Home" });
                    _ = navigation.Items.Add(new Controls.NavigationViewItem { Content = "Settings" });
                    Controls.TabView tabs = new() { Height = 215, IsAddTabButtonVisible = true };
                    _ = tabs.Items.Add(new Controls.TabViewItem { Header = "Overview", Content = "Overview content" });
                    _ = tabs.Items.Add(new Controls.TabViewItem { Header = "Notes", Content = "Notes content" });
                    navigation.Content = tabs;
                    Add(navigation);
                    break;
                case "dialogs-and-feedback":
                    AddHeading("Dialogs and feedback", "Ask for a decision and show progress or status inline.");
                    AddButton("Ask for confirmation", static () =>
                    {
                        Controls.ContentDialog dialog = new()
                        {
                            Title = "Continue?",
                            Content = "Review this action before proceeding.",
                            PrimaryButtonText = "Continue",
                            CloseButtonText = "Cancel",
                        };
                        _ = dialog.ShowAsync();
                    });
                    Add(new Controls.InfoBar { Title = "Saved", Message = "Your changes are up to date.", Severity = InfoBarSeverity.Success, IsOpen = true });
                    Add(new Controls.ProgressBar { Width = 420, Value = 65 });
                    break;
                case "controls-from-csharp":
                    AddHeading("Create controls in C#", "Construct a window and its content without XAML.");
                    AddLabel("Every element on this page was created in code.");
                    Controls.TextBlock status = new() { Text = "Ready to continue." };
                    Add(status);
                    AddButton("Continue", () => status.Text = "The button was clicked.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slug));
            }
        }

        private void AddHeading(string title, string subtitle)
        {
            Add(new TextBlock { Text = title, FontSize = 30, FontWeight = FontWeights.SemiBold });
            Add(new TextBlock { Text = subtitle, FontSize = 15, Margin = new Thickness(0, 5, 0, 20), Opacity = 0.72 });
        }

        private void AddLabel(string text)
        {
            Add(new TextBlock { Text = text, FontSize = 15 });
        }

        private void AddButton(string label, Action click)
        {
            Controls.Button button = new()
            {
                Content = label,
                Appearance = ControlAppearance.Accent,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            button.Click += (_, _) => click();
            Add(button);
        }

        private void Add(UIElement element)
        {
            if (element is FrameworkElement frameworkElement)
            {
                frameworkElement.Margin = new Thickness(0, 0, 0, 13);
            }

            _ = _content.Children.Add(element);
        }
    }
}
