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

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    public sealed class GalleryPageHeaderTests
    {
        [Fact]
        public Task GalleryPageHeader_DocsButton_TracksDocsAnchorVisibilityAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                GalleryPageHeader header = new() { Title = "Buttons" };
                Window window = DemoTestHost.CreateHostWindow(header);
                try
                {
                    Controls.Button docsButton = Assert.IsType<Controls.Button>(DemoTestHost.FindByName<Controls.Button>(header, "DocsButton"), exactMatch: false);
                    Assert.Equal(Visibility.Collapsed, docsButton.Visibility);

                    header.DocsAnchor = "basic-actions";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Visible, docsButton.Visibility);

                    header.DocsAnchor = string.Empty;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Collapsed, docsButton.Visibility);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GalleryPageHeader_ThemeToggle_DisabledUnderHighContrastEnabledUnderLightAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                GalleryPageHeader header = new() { Title = "Buttons" };
                Window window = DemoTestHost.CreateHostWindow(header);
                try
                {
                    Controls.Button themeToggle = Assert.IsType<Controls.Button>(DemoTestHost.FindByName<Controls.Button>(header, "ThemeToggleButton"), exactMatch: false);
                    Assert.True(themeToggle.IsEnabled, "The theme toggle should be enabled while the resolved theme is Light.");

                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.False(themeToggle.IsEnabled, "The theme toggle should be disabled and inert while the resolved theme is HighContrast.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(themeToggle.IsEnabled, "The theme toggle should re-enable once the resolved theme leaves HighContrast.");
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GalleryPageHeader_ThemeToggle_ClickFromLightResolvesToDarkAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                Assert.Equal(ApplicationTheme.Light, ApplicationThemeManager.ResolvedTheme);

                GalleryPageHeader header = new() { Title = "Buttons" };
                Window window = DemoTestHost.CreateHostWindow(header);
                try
                {
                    Controls.Button themeToggle = Assert.IsType<Controls.Button>(DemoTestHost.FindByName<Controls.Button>(header, "ThemeToggleButton"), exactMatch: false);

                    themeToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, themeToggle));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(ApplicationTheme.Dark, ApplicationThemeManager.ResolvedTheme);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);

                    // The click above resolved the theme to Dark. Restore the neutral Light
                    // state the neighbouring tests in this file expect at their own start, so
                    // this test never leaves Dark applied for whatever runs after it.
                    ApplicationThemeManager.ResetForTesting();
                    ApplicationAccentColorManager.ResetForTesting();
                    _ = DemoTestHost.EnsureDemoTheme();
                }
            });
        }

        [Fact]
        public Task GalleryPageHeader_ThemeToggle_PreservesShellBackdropAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                Application application = DemoTestHost.EnsureDemoTheme();
                Assert.Equal(ApplicationTheme.Light, ApplicationThemeManager.ResolvedTheme);

                // ThemeToggleButton_Click resolves its owner through Application.Current.MainWindow
                // (see GalleryPageHeader.xaml.cs), exactly as App.xaml.cs assigns it at startup
                // (MainWindow = mainWindow;). The backdrop is preserved only when a real MainWindow
                // occupies that property, so this test hosts the header inside one instead of the
                // plain Window the other tests in this file use.
                MainWindow window = new()
                {
                    Left = -20000,
                    Top = -20000,
                    Width = 1200,
                    Height = 900,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    ShowInTaskbar = false,
                };
                application.MainWindow = window;
                window.Show();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                try
                {
                    // Mirror GallerySettingsPage.BackdropComboBox_SelectionChanged: set the shell's
                    // backdrop DP and apply it through the theme manager together.
                    window.SystemBackdropType = BackdropType.Acrylic;
                    ApplicationThemeManager.Apply(ApplicationThemeManager.CurrentTheme, BackdropType.Acrylic);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    window.NavigateTo("buttons");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    GalleryPageHeader header = Assert.Single(DemoTestHost.FindVisualChildren<GalleryPageHeader>(window));
                    Controls.Button themeToggle = Assert.IsType<Controls.Button>(DemoTestHost.FindByName<Controls.Button>(header, "ThemeToggleButton"), exactMatch: false);

                    themeToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, themeToggle));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(ApplicationTheme.Dark, ApplicationThemeManager.ResolvedTheme);
                    Assert.Equal(BackdropType.Acrylic, ApplicationThemeManager.CurrentBackdrop);
                }
                finally
                {
                    window.Close();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    application.MainWindow = null;

                    ApplicationThemeManager.ResetForTesting();
                    ApplicationAccentColorManager.ResetForTesting();
                    _ = DemoTestHost.EnsureDemoTheme();
                }
            });
        }

        [Fact]
        public Task GalleryPageHeader_FavoriteToggle_FlipsGlyphWhenCheckedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                GalleryPageHeader header = new() { Title = "Buttons" };
                Window window = DemoTestHost.CreateHostWindow(header);
                try
                {
                    Controls.ToggleButton favoriteToggle = Assert.IsType<Controls.ToggleButton>(DemoTestHost.FindByName<Controls.ToggleButton>(header, "FavoriteToggleButton"), exactMatch: false);
                    Controls.FontIcon favoriteIcon = Assert.IsType<Controls.FontIcon>(DemoTestHost.FindByName<Controls.FontIcon>(header, "FavoriteIcon"), exactMatch: false);
                    Assert.Equal("\uE734", favoriteIcon.Glyph);

                    favoriteToggle.IsChecked = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("\uE735", favoriteIcon.Glyph);

                    favoriteToggle.IsChecked = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("\uE734", favoriteIcon.Glyph);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }
    }
}
