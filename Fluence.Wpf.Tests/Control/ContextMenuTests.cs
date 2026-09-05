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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Tests for Fluent <see cref="ContextMenu"/> and <see cref="MenuItem"/>.
    /// </summary>
    public sealed class ContextMenuTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        // ---------------------------------------------------------------------------
        // ContextMenu
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ContextMenu_DefaultStyle_StyleRegisteredAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Style style = Assert.IsType<Style>(app.TryFindResource(typeof(ContextMenu)));
            });
        }

        [Fact]
        public Task ContextMenu_DefaultStyle_BackgroundAndBorderBrushResolveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Assert.NotNull(app.TryFindResource("SolidBackgroundFillColorTertiaryBrush"));
                Assert.NotNull(app.TryFindResource("SurfaceStrokeColorFlyoutBrush"));
            });
        }

        [Fact]
        public Task ContextMenu_DefaultStyle_HasNoHasDropShadowSetterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Style style = Assert.IsType<Style>(app.TryFindResource(typeof(ContextMenu)));

                // HasDropShadow does nothing once a control has a custom Template (verified in
                // the dotnet/wpf sources: it is only consumed by the default ContextMenu style's
                // own trigger, which this template replaces). Elevation instead comes from the
                // real ShadowCaster + FlyoutShadowEffect sibling in the template, asserted by
                // ContextMenuXaml_UsesShadowCasterForElevation below, so the inert setter must
                // not be present.
                bool found = style.Setters.OfType<Setter>().Any(static s => s.Property == System.Windows.Controls.ContextMenu.HasDropShadowProperty);
                Assert.False(found, "ContextMenu style must not set the inert HasDropShadow property; use a ShadowCaster + FlyoutShadowEffect sibling instead.");
            });
        }

        [Fact]
        public async Task ContextMenuXaml_UsesShadowCasterForElevationAsync()
        {
            string xaml = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf", "Themes", "Controls", "ContextMenu.xaml").ConfigureAwait(true);

            Assert.True(
                xaml.Contains("x:Name=\"ShadowCaster\"", StringComparison.Ordinal) &&
                xaml.Contains("x:Name=\"SubMenuShadowCaster\"", StringComparison.Ordinal),
                "ContextMenu's root surface and its submenu popup must each carry a named " +
                "ShadowCaster sibling, since HasDropShadow does not apply the elevation itself.");
            Assert.True(
                xaml.Contains("Effect=\"{DynamicResource FlyoutShadowEffect}\"", StringComparison.Ordinal),
                "The ShadowCaster siblings must carry FlyoutShadowEffect.");
        }

        // ---------------------------------------------------------------------------
        // WI-5A.2 MenuItem
        // ---------------------------------------------------------------------------

        [Fact]
        public Task MenuItem_DefaultStyle_StyleRegisteredAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Style style = Assert.IsType<Style>(app.TryFindResource(typeof(MenuItem)));
            });
        }

        [Fact]
        public Task MenuItem_DefaultStyle_HoverBrushResolvesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Assert.NotNull(app.TryFindResource("SubtleFillColorSecondaryBrush"));
                Assert.NotNull(app.TryFindResource("SubtleFillColorTertiaryBrush"));
            });
        }

        [Fact]
        public Task MenuItem_DefaultStyle_FontSize14Async()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                MenuItem mi = new()
                {
                    Header = "Test",
                    Style = Assert.IsType<Style>(app.TryFindResource(typeof(MenuItem))),
                };
                Assert.Equal(14.0, mi.FontSize, 0.01);
            });
        }

        // ---------------------------------------------------------------------------
        // WI-5A.2 Theme cycle
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ContextMenu_ThemeCycle_BrushesResolveAfterEachSwitchAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                string[] keys =
                [
                    "SolidBackgroundFillColorTertiaryBrush",
                    "SurfaceStrokeColorFlyoutBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                    "DividerStrokeColorDefaultBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    foreach (string? key in keys)
                    {
                        Assert.NotNull(app.TryFindResource(key));
                    }
                }
            });
        }
    }
}
