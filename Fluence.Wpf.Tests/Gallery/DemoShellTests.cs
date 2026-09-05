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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.FluentButtonQueries;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Gallery
{
    /// <summary>
    /// The demo gallery shell (<see cref="MainWindow"/>): navigation between pages, the
    /// Settings page's theme, accent, and backdrop controls, caption button defaults, and the
    /// Buttons page's icon-rendering samples.
    /// </summary>
    public sealed class DemoShellTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        [Fact]
        public Task MainWindow_AccentColorButtons_UseButtonControlAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    List<Controls.Button> accentSwatchButtons = [.. FindVisualChildren<Controls.Button>(window).Where(static b => b.Tag is string hex && hex.Length > 0 && hex[0] == '#')];

                    List<string> expectedSwatches =
                    [
                        "#E80000",
                        "#F58809",
                        "#F5E70C",
                        "#2BDE11",
                        "#09C4DE",
                        "#AA04DE",
                        "#FF00E8",
                    ];

                    Assert.Equal(expectedSwatches, accentSwatchButtons.ConvertAll(static b => (string)b.Tag));
                    foreach (Controls.Button swatch in accentSwatchButtons)
                    {
                        _ = Assert.IsType<Controls.Button>(swatch, exactMatch: false);
                    }
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_SettingsSelectors_UseExpectedControlsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "AppThemeComboBox"), exactMatch: false);
                    _ = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "NavigationStyleComboBox"), exactMatch: false);
                    _ = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "BackdropComboBox"), exactMatch: false);
                    _ = Assert.IsType<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "ThemeWatcherToggle"), exactMatch: false);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_AppThemeComboBox_UpdatesStateLabelAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Auto, updateAccent: true);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.ComboBox themeComboBox = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "AppThemeComboBox"), exactMatch: false);
                    TextBlock themeStateLabel = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "ThemeStateLabel"), exactMatch: false);

                    themeComboBox.SelectedIndex = 2;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal("Current: Dark", themeStateLabel.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_DemoButtons_RenderTheirIconsAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Buttons").ConfigureAwait(true);

                    Controls.Button iconLeftButton = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Left"), exactMatch: false);
                    Controls.Button iconRightButton = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Right"), exactMatch: false);

                    AssertButtonShowsGlyph(iconLeftButton, "\uE774");
                    AssertButtonShowsGlyph(iconRightButton, "\uE8D6");
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_StandardDemoButtonIcons_UsePrimaryTextBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Buttons").ConfigureAwait(true);

                    Controls.Button iconLeftButton = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Left"), exactMatch: false);
                    Controls.Button iconRightButton = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Right"), exactMatch: false);
                    SolidColorBrush expectedBrush = Assert.IsType<SolidColorBrush>(application.Resources["TextFillColorPrimaryBrush"]);

                    TextBlock iconLeftGlyph = Assert.IsType<TextBlock>(FindButtonIconTextBlock(iconLeftButton), exactMatch: false);
                    TextBlock iconRightGlyph = Assert.IsType<TextBlock>(FindButtonIconTextBlock(iconRightButton), exactMatch: false);

                    _ = Assert.IsType<SolidColorBrush>(iconLeftGlyph.Foreground, exactMatch: false);
                    _ = Assert.IsType<SolidColorBrush>(iconRightGlyph.Foreground, exactMatch: false);
                    Assert.Equal(expectedBrush.Color, ((SolidColorBrush)iconLeftGlyph.Foreground).Color);
                    Assert.Equal(expectedBrush.Color, ((SolidColorBrush)iconRightGlyph.Foreground).Color);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_TabSelection_ActivatesExpectedContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Buttons").ConfigureAwait(true);
                    Assert.NotNull(FindFluentButtonByContent(window, "Icon Left"));

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Inputs").ConfigureAwait(true);
                    Assert.NotNull(FindVisualChildByName<Controls.TextBox>(window, "CharCountTextBox"));

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Selection").ConfigureAwait(true);
                    Assert.NotNull(FindVisualChildByName<Controls.ToggleSwitch>(window, "WorkToggleSwitch"));

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Selection").ConfigureAwait(true);
                    Assert.NotNull(FindVisualChildByName<Controls.ComboBox>(window, "SelectionDemoCombo"));

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Status").ConfigureAwait(true);
                    Assert.NotNull(FindVisualChildByName<Controls.ProgressBar>(window, "StepProgressBar"));

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Data").ConfigureAwait(true);
                    Assert.NotNull(FindVisualChildByName<Controls.ListView>(window, "EmptyStateListView"));
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_NavigationView_UsesFlatGalleryTaxonomyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(window.FindName("DemoNav"));
                    List<string> pages = [];
                    foreach (object? obj in nav.Items)
                    {
                        if (obj is not Controls.NavigationViewItem item || item.Content is not string content)
                        {
                            continue;
                        }

                        Assert.Null(item.InfoBadge);
                        pages.Add(content);
                    }

                    Assert.Contains("Home", pages, StringComparer.Ordinal);
                    Assert.Contains("Buttons", pages, StringComparer.Ordinal);
                    Assert.Contains("Selection", pages, StringComparer.Ordinal);
                    Assert.Contains("Inputs", pages, StringComparer.Ordinal);
                    Assert.Contains("Typography", pages, StringComparer.Ordinal);
                    Assert.Contains("Icons", pages, StringComparer.Ordinal);
                    Assert.False(pages.Contains("Windowing"), "Windowing controls should move to Settings rather than the main navigation list.");
                    Assert.False(pages.Contains("Button"), "Demo navigation should use grouped pages, not generated per-control pages.");
                    Assert.False(pages.Contains("Fundamentals"), "Demo navigation should not expose the old Fundamentals section.");
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_CaptionButtons_DefaultOverridesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(window.IsMinimizable);
                    Assert.True(window.IsMaximizable);
                    Assert.True(window.IsClosable);

                    Button closeButton = Assert.IsType<Button>(window.Template.FindName("PART_CloseButton", window));
                    Assert.Equal(Visibility.Visible, closeButton.Visibility);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ThemeWatcherToggle_UpdatesLabelAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    window.NavigateTo("settings");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.ToggleSwitch toggle = Assert.IsType<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "ThemeWatcherToggle"), exactMatch: false);
                    TextBlock label = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "SystemThemeLabel"), exactMatch: false);

                    Assert.True(toggle.IsChecked is true, "ThemeWatcherToggle should default to checked.");
                    Assert.Equal("Watching: Yes", label.Text, StringComparer.Ordinal);

                    toggle.IsChecked = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal("Watching: No", label.Text, StringComparer.Ordinal);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_IconLeftButton_IconIsVerticallyCenteredAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Buttons").ConfigureAwait(true);

                    Controls.Button button = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Left"), exactMatch: false);

                    TextBlock glyphTextBlock = Assert.IsType<TextBlock>(FindButtonGlyphTextBlock(button, "\uE774"), exactMatch: false);
                    Point buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
                    Point glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
                    double buttonCenterY = buttonOrigin.Y + (button.ActualHeight / 2.0);
                    double glyphCenterY = glyphOrigin.Y + (glyphTextBlock.ActualHeight / 2.0);

                    Assert.Equal(buttonCenterY, glyphCenterY, 1.0);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_StandardButtonIcons_AreInsideButtonBoundsAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Buttons").ConfigureAwait(true);

                    Controls.Button iconLeftButton = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Left"), exactMatch: false);
                    Controls.Button iconRightButton = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Icon Right"), exactMatch: false);

                    AssertGlyphWithinButtonBounds(window, iconLeftButton, "\uE774");
                    AssertGlyphWithinButtonBounds(window, iconRightButton, "\uE8D6");
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ProgressNumberBox_UpdatesFirstProgressBarAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Status").ConfigureAwait(true);

                    Controls.NumberBox numberBox = Assert.IsType<Controls.NumberBox>(FindVisualChildByName<Controls.NumberBox>(window, "ProgressValueNumberBox"), exactMatch: false);
                    Controls.ProgressBar progressBar = Assert.IsType<Controls.ProgressBar>(FindVisualChildByName<Controls.ProgressBar>(window, "StandardProgressBar"), exactMatch: false);

                    numberBox.Value = 73;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(73d, progressBar.Value, 0.1);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_SelectionDemoCombo_SelectionUpdatesIndexAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Selection").ConfigureAwait(true);

                    Controls.ComboBox combo = Assert.IsType<Controls.ComboBox>(FindVisualChildByName<Controls.ComboBox>(window, "SelectionDemoCombo"), exactMatch: false);
                    Assert.Equal(3, combo.Items.Count);

                    combo.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(1, combo.SelectedIndex);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MainWindow_ComboBoxPage_InitialComboBoxesHaveNoSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Selection").ConfigureAwait(true);
                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(window.FindName("DemoNav"));

                    DependencyObject selectedContent = Assert.IsType<DependencyObject>(nav.Content, exactMatch: false);

                    List<Controls.ComboBox> comboBoxes = [.. FindVisualChildren<Controls.ComboBox>(selectedContent)];
                    Assert.True(comboBoxes.Count >= 2, "ComboBox page should display multiple ComboBox examples.");

                    foreach (Controls.ComboBox comboBox in comboBoxes)
                    {
                        Assert.Equal(-1, comboBox.SelectedIndex);
                    }
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task DemoMainWindow_SelectingNavPage_DoesNotThrowAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Auto, updateAccent: true);
                ApplicationAccentColorManager.ApplySystemAccent();

                MainWindow? window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    window.UpdateLayout();

                    Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(window.FindName("DemoNav"));

                    await SelectMainWindowNavPageAsync(window, window.Dispatcher, "Buttons").ConfigureAwait(true);
                    Assert.NotNull(nav.SelectedItem);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        private static async Task SelectMainWindowNavPageAsync(MainWindow window, Dispatcher dispatcher, string itemContent)
        {
            Controls.NavigationView nav = Assert.IsType<Controls.NavigationView>(window.FindName("DemoNav"));

            window.NavigateTo(itemContent);
            WpfTestSta.DrainDispatcher(dispatcher);
            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Loaded, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ContextIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
            window.UpdateLayout();
            WpfTestSta.DrainDispatcher(dispatcher);

            Controls.NavigationViewItem? selected = nav.SelectedItem as Controls.NavigationViewItem;
            string? selectedLabel = selected is null ? null : selected.Content as string;
            string? selectedTag = selected is null ? null : selected.Tag as string;
            bool matchesRequest = string.Equals(selectedLabel, itemContent, StringComparison.OrdinalIgnoreCase) ||
                (selectedTag?.IndexOf(itemContent, StringComparison.OrdinalIgnoreCase) >= 0);
            if (selected is null || nav.Content is null || !matchesRequest)
            {
                Assert.Fail(string.Format("Navigation item '{0}' should exist.", itemContent));
            }
        }

        private static void AssertButtonShowsGlyph(Controls.Button button, string glyph)
        {
            TextBlock glyphTextBlock = Assert.IsType<TextBlock>(FindButtonGlyphTextBlock(button, glyph), exactMatch: false);
            Assert.True(glyphTextBlock.IsVisible, "Expected button glyph should be visible.");
            Assert.True(glyphTextBlock.ActualWidth > 0, "Expected button glyph should occupy layout space.");
        }

        private static TextBlock? FindButtonIconTextBlock(Controls.Button button)
        {
            return FindVisualChildren<TextBlock>(button).FirstOrDefault(static textBlock => textBlock.FontFamily is FontFamily fontFamily && fontFamily.Source?.IndexOf("Segoe Fluent Icons", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void AssertGlyphWithinButtonBounds(Window window, Controls.Button button, string glyph)
        {
            TextBlock glyphTextBlock = Assert.IsType<TextBlock>(FindButtonGlyphTextBlock(button, glyph), exactMatch: false);

            Assert.True(glyphTextBlock.IsVisible, "Expected button glyph should be visible.");
            Assert.True(glyphTextBlock.ActualWidth > 0, "Expected button glyph should occupy layout space.");

            Point buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
            Point glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
            double buttonRight = buttonOrigin.X + button.ActualWidth;
            double buttonBottom = buttonOrigin.Y + button.ActualHeight;
            double glyphRight = glyphOrigin.X + glyphTextBlock.ActualWidth;
            double glyphBottom = glyphOrigin.Y + glyphTextBlock.ActualHeight;

            Assert.True(glyphOrigin.X >= buttonOrigin.X - 0.5, "Expected button glyph should not render left of the button.");
            Assert.True(glyphOrigin.Y >= buttonOrigin.Y - 0.5, "Expected button glyph should not render above the button.");
            Assert.True(glyphRight <= buttonRight + 0.5, "Expected button glyph should not render right of the button.");
            Assert.True(glyphBottom <= buttonBottom + 0.5, "Expected button glyph should not render below the button.");
        }
    }
}
