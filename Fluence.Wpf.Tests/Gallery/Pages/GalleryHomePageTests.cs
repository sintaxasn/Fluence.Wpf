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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    public sealed class GalleryHomePageTests : IAsyncLifetime
    {
        private Window? _host;
        private GalleryHomePage? _page;

        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(() =>
            {
                _ = TestApp.EnsureDemoTheme();
                _page = new GalleryHomePage();
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

        [Fact]
        public Task GalleryHomePage_HeroSwapsHeaderLockupWithThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Window window = _host ?? throw new InvalidOperationException("Host window was not initialized.");
                GalleryHomePage page = _page ?? throw new InvalidOperationException("Page was not initialized.");

                System.Windows.Controls.Image image = Assert.IsType<System.Windows.Controls.Image>(DemoTestHost.FindByName<System.Windows.Controls.Image>(page, "BrandHeroImage"), exactMatch: false);

                DrawingImage light = Assert.IsType<DrawingImage>(Application.Current.TryFindResource("FluenceHeaderLightDrawingImage"));
                DrawingImage dark = Assert.IsType<DrawingImage>(Application.Current.TryFindResource("FluenceHeaderDarkDrawingImage"));

                // The hero shows the lockup drawn for the active theme and swaps on
                // theme changes via the page's ThemeDictionary (no code-behind).
                Assert.Same(light, image.Source);

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Same(dark, image.Source);

                // High contrast has no fixed polarity, so the page picks whichever
                // variant reads against the live system window color.
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: true);
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.True(ReferenceEquals(image.Source, light) || ReferenceEquals(image.Source, dark),
                    "High contrast should show one of the two header lockups.");

                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Same(light, image.Source);
            });
        }

        [Fact]
        public async Task GalleryHomePage_UsesHeaderLockupHeroAndGitHubLinkAsync()
        {
            string homePage = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf.Demo", "Pages", "GalleryHomePage.xaml").ConfigureAwait(true);
            Assert.Contains("FluenceHeaderLightDrawingImage", homePage, StringComparison.Ordinal);
            Assert.Contains("https://github.com/sintaxasn/fluence.wpf", homePage, StringComparison.Ordinal);
        }
    }
}
