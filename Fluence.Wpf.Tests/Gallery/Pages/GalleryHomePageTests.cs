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
    /// <summary>
    /// Covers <see cref="GalleryHomePage"/>.
    /// </summary>
    public sealed class GalleryHomePageTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        [Fact]
        public Task GalleryHomePage_HeroSwapsHeaderLockupWithThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryHomePage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    System.Windows.Controls.Image image = Assert.IsType<System.Windows.Controls.Image>(DemoTestHost.FindByName<System.Windows.Controls.Image>(page, "BrandHeroImage"), exactMatch: false);

                    DrawingImage light = Assert.IsType<DrawingImage>(Application.Current.TryFindResource("FluenceHeaderLightDrawingImage"));
                    DrawingImage dark = Assert.IsType<DrawingImage>(Application.Current.TryFindResource("FluenceHeaderDarkDrawingImage"));

                    // The hero shows the lockup drawn for the active theme and swaps on
                    // theme changes via the page's ThemeDictionary (no code-behind).
                    Assert.Same(light, image.Source);

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(dark, image.Source);

                    // High contrast has no fixed polarity, so the page picks whichever
                    // variant reads against the live system window color.
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(ReferenceEquals(image.Source, light) || ReferenceEquals(image.Source, dark),
                        "High contrast should show one of the two header lockups.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(light, image.Source);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
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
