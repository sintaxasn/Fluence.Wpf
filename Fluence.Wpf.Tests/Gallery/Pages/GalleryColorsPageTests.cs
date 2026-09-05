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
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    public sealed class GalleryColorsPageTests : IAsyncLifetime
    {
        private static readonly string[] SectionNames =
        [
            "Text",
            "Fill",
            "Stroke",
            "Background",
            "Signal",
            "High Contrast",
        ];

        private Window? _host;
        private GalleryColorsPage? _page;

        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(() =>
            {
                _ = TestApp.EnsureDemoTheme();
                _page = new GalleryColorsPage();
                _host = DemoTestHost.CreateHostWindow(_page);
            }));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(() =>
            {
                if (_host is not null)
                {
                    DemoTestHost.CloseWindow(_host);
                    _host = null;
                }

                _page = null;
            }));
        }

        // This test needs the real shell NavigationView to reach the Colors page by route, so
        // it builds its own MainWindow rather than the page the class shares.
        [Fact]
        public Task GalleryColorsPage_NavigationRoute_LoadsConcretePageAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow window = DemoShellTests.CreateShownMainWindow();
                try
                {
                    window.NavigateTo("colors");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.NavigationView navigationView = Assert.IsType<Controls.NavigationView>(DemoTestHost.FindByName<Controls.NavigationView>(window, "DemoNav"), exactMatch: false);
                    _ = Assert.IsType<GalleryColorsPage>(navigationView.Content, exactMatch: false);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task GalleryColorsPage_ExposesGalleryPageHeaderWithColorsTitleAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                GalleryColorsPage page = _page ?? throw new InvalidOperationException("Page was not initialized.");

                GalleryPageHeader header = Assert.IsType<GalleryPageHeader>(DemoTestHost.FindVisualChildren<GalleryPageHeader>(page).FirstOrDefault(), exactMatch: false);
                Assert.Equal("Colors", header.Title, StringComparer.Ordinal);
                Assert.True(string.IsNullOrWhiteSpace(header.DocsAnchor),
                    "Colors has no matching docs/controls.md section, so the Documentation button should stay hidden.");
            });
        }

        // This test drives the page's color-section tabs, so it builds its own instance rather
        // than mutating the one the class shares.
        [Fact]
        public Task GalleryColorsPage_UsesWinUiGalleryColorStructureAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                GalleryColorsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Controls.SmoothScrollViewer scrollViewer = Assert.IsType<Controls.SmoothScrollViewer>(DemoTestHost.FindVisualChildren<Controls.SmoothScrollViewer>(page).FirstOrDefault(), exactMatch: false);

                    TabControl colorTabs = Assert.IsType<TabControl>(DemoTestHost.FindByName<TabControl>(page, "ColorSectionTabs"), exactMatch: false);
                    Assert.Equal(SectionNames.Length, colorTabs.Items.Count);

                    for (int i = 0; i < SectionNames.Length; i++)
                    {
                        TabItem tabItem = (TabItem)colorTabs.Items[i];
                        Assert.Equal(SectionNames[i], tabItem.Header as string, StringComparer.Ordinal);
                    }

                    List<string> exampleTitles = [.. DemoTestHost.FindVisualChildren<TextBlock>(page)
                        .Where(static text => string.Equals(text.Tag as string, "ColorExampleTitle", StringComparison.Ordinal))
                        .Select(static text => text.Text)];
                    Assert.Equal(["Text", "Accent Text", "Text On Accent"], exampleTitles, StringComparer.Ordinal);

                    Assert.Empty(DemoTestHost.FindVisualChildren<WrapPanel>(page));
                    Assert.Empty(DemoTestHost.FindVisualChildren<DemoSampleControl>(page));

                    int totalTiles = 0;
                    bool sawSystemColorAlias = false;
                    bool sawAccentFill = false;
                    for (int i = 0; i < colorTabs.Items.Count; i++)
                    {
                        SelectTab(colorTabs, i, window.Dispatcher);

                        List<UniformGrid> rows = [.. DemoTestHost.FindVisualChildren<UniformGrid>(page)
                            .Where(static row => string.Equals((row.Parent as FrameworkElement)?.Tag as string, "ColorTokenRow", StringComparison.Ordinal))];
                        Assert.True(rows.Count > 0, "Selected Colors page section should contain token rows.");

                        // WinUI Gallery GalleryTileGridStyle: token rows sit on the SolidBackgroundFillColorBase tile surface.
                        Color surfaceColor = Assert.IsType<SolidColorBrush>(application.TryFindResource("SolidBackgroundFillColorBaseBrush"), exactMatch: false).Color;
                        foreach (UniformGrid row in rows)
                        {
                            Border surface = Assert.IsType<Border>(row.Parent, exactMatch: false);
                            Assert.Equal(surfaceColor, Assert.IsType<SolidColorBrush>(surface.Background, exactMatch: false).Color);
                            Assert.Equal(new Thickness(1), surface.BorderThickness);
                        }

                        foreach (UniformGrid row in rows)
                        {
                            Assert.Equal(row.Children.Count, row.Columns);
                            Assert.True(row.Columns <= 4, "Token rows should stay compact at four columns or fewer.");

                            foreach (string resourceKey in row.Children.Cast<FrameworkElement>().Select(static tile => tile.Tag as string ?? string.Empty))
                            {
                                Assert.False(string.IsNullOrWhiteSpace(resourceKey), "Each token tile should expose its resource key.");
                                totalTiles++;
                                sawSystemColorAlias |= string.Equals(resourceKey, "SystemColorWindowTextColorBrush", StringComparison.Ordinal);
                                sawAccentFill |= string.Equals(resourceKey, "AccentFillColorDefaultBrush", StringComparison.Ordinal);
                            }
                        }
                    }

                    Assert.True(totalTiles >= 90, "Colors page should expose the WinUI-style brush catalogue through token tiles.");
                    Assert.True(sawSystemColorAlias, "High Contrast section should use SystemColor alias resources.");
                    Assert.True(sawAccentFill, "Fill section should include accent fill resources.");
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        // This test drives the page's color-section tabs and cycles the app theme, so it
        // builds its own instance rather than mutating the one the class shares.
        [Fact]
        public Task GalleryColorsPage_DynamicResourceKeys_ResolveAcrossThemesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                GalleryColorsPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    SortedSet<string> resourceKeys = CollectColorTokenResourceKeys(page, window.Dispatcher);
                    Assert.True(resourceKeys.Count >= 90, "Colors page should expose enough token keys to cover the Fluent color families.");

                    ApplicationTheme[] themes =
                    [
                        ApplicationTheme.Light,
                        ApplicationTheme.Dark,
                        ApplicationTheme.HighContrast,
                    ];

                    List<string> unresolved = [];
                    foreach (ApplicationTheme theme in themes)
                    {
                        ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                        ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                        foreach (string resourceKey in resourceKeys.Where(resourceKey => application.TryFindResource(resourceKey) is null))
                        {
                            unresolved.Add(theme + ": " + resourceKey);
                        }
                    }

                    Assert.Empty(unresolved);
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public async Task GalleryColorsPage_SourceAvoidsLegacyControlsAndLiteralForegroundsAsync()
        {
            string pageXaml = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo", "Pages", "GalleryColorsPage.xaml").ConfigureAwait(true);
            string pageCode = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo", "Pages", "GalleryColorsPage.xaml.cs").ConfigureAwait(true);
            string source = pageXaml + Environment.NewLine + pageCode;

            string[] forbidden =
            [
                "ColorGuidance",
                "GalleryBackgroundBrush",
                "Foreground=\"Black\"",
                "Foreground=\"White\"",
                "Foreground=\"#",
                "WrapPanel",
                "SectionSelectorHost",
                "FluenceToggleButton",
            ];

            Assert.DoesNotContain(forbidden, value => source.Contains(value, StringComparison.Ordinal));
        }

        private static SortedSet<string> CollectColorTokenResourceKeys(GalleryColorsPage page, Dispatcher dispatcher)
        {
            SortedSet<string> resourceKeys = new(StringComparer.OrdinalIgnoreCase);
            TabControl colorTabs = Assert.IsType<TabControl>(DemoTestHost.FindByName<TabControl>(page, "ColorSectionTabs"), exactMatch: false);

            for (int index = 0; index < colorTabs.Items.Count; index++)
            {
                SelectTab(colorTabs, index, dispatcher);
                foreach (UniformGrid row in DemoTestHost.FindVisualChildren<UniformGrid>(page)
                    .Where(static row => string.Equals((row.Parent as FrameworkElement)?.Tag as string, "ColorTokenRow", StringComparison.Ordinal)))
                {
                    foreach (UIElement child in row.Children)
                    {
                        if (child is FrameworkElement { Tag: string resourceKey } &&
                            !string.IsNullOrWhiteSpace(resourceKey))
                        {
                            _ = resourceKeys.Add(resourceKey);
                        }
                    }
                }
            }

            return resourceKeys;
        }

        private static void SelectTab(TabControl colorTabs, int index, Dispatcher dispatcher)
        {
            colorTabs.SelectedIndex = index;
            WpfTestSta.DrainDispatcher(dispatcher);
            colorTabs.UpdateLayout();
            WpfTestSta.DrainDispatcher(dispatcher);
        }
    }
}
