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
using System.Windows.Automation;
using System.Windows.Media;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 B14 tests: InfoBar SeverityLevels VSM group + GoToState wiring.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 B14  InfoBar SeverityLevels VSM group
        // ---------------------------------------------------------------------------

        [Fact]
        public Task InfoBar_StyleApplies_RootBorderFoundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Title = "Test" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border root = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(bar, "RootBorder"), exactMatch: false);
                w.Close();
            });
        }

        [Fact]
        public Task InfoBar_TwoLayerIcon_GlyphAndBackgroundPerSeverityAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                // WinUI parity (InfoBar.xaml:108-109 upstream): no SeverityLevels VSM opacity
                // pulse and no 3px indicator bar. The severity read is the two-layer glyph -
                // IconBackground's severity-brush Foreground under StandardIcon's fixed
                // TextFillColorInverseBrush glyph, which changes text per severity.
                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Warning, Title = "Test" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock iconBackground = Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(bar, "IconBackground"), exactMatch: false);
                System.Windows.Controls.TextBlock standardIcon = Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(bar, "StandardIcon"), exactMatch: false);

                SolidColorBrush expectedIconBackground = Assert.IsType<SolidColorBrush>(app.TryFindResource("SystemFillColorCautionBrush"));
                SolidColorBrush expectedIconForeground = Assert.IsType<SolidColorBrush>(app.TryFindResource("TextFillColorInverseBrush"));
                Assert.Equal(expectedIconBackground.Color, ((SolidColorBrush)iconBackground.Foreground).Color);
                Assert.Equal(expectedIconForeground.Color, ((SolidColorBrush)standardIcon.Foreground).Color);
                Assert.Equal("", standardIcon.Text, StringComparer.Ordinal);
                w.Close();
            });
        }

        [Fact]
        public Task InfoBar_CloseButton_UsesFluentSubtlePlateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Title = "Closable" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Button close =
                        Assert.IsType<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(bar, "PART_CloseButton"), exactMatch: false);
                    Assert.Equal(28.0, close.Width, 0.01);
                    Assert.Equal(28.0, close.Height, 0.01);

                    // The subtle plate (TeachingTip / PipsPager pattern): a rounded Border
                    // owned by the button's own template, not the OS default chrome.
                    System.Windows.Controls.Border plate =
                        Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(close, "ButtonPlate"), exactMatch: false);
                    CornerRadius expectedRadius = (CornerRadius)(app.FindResource("ControlCornerRadius")
                        ?? throw new Xunit.Sdk.XunitException("ControlCornerRadius must resolve."));
                    Assert.Equal(expectedRadius, plate.CornerRadius);
                    SolidColorBrush restFill = Assert.IsType<SolidColorBrush>(plate.Background);
                    Assert.Equal(0, restFill.Color.A);

                    // Foreground contract: TextFillColorPrimary at rest, flowing into the glyph.
                    SolidColorBrush primary = (SolidColorBrush)(app.FindResource("TextFillColorPrimaryBrush")
                        ?? throw new Xunit.Sdk.XunitException("TextFillColorPrimaryBrush must resolve."));
                    SolidColorBrush buttonForeground = Assert.IsType<SolidColorBrush>(close.Foreground);
                    Assert.Equal(primary.Color, buttonForeground.Color);

                    FontIcon glyph = Assert.IsType<FontIcon>(FindVisualChildren<FontIcon>(close).FirstOrDefault(), exactMatch: false);
                    Assert.Equal("\uE711", glyph.Glyph, StringComparer.Ordinal);
                    SolidColorBrush glyphForeground = Assert.IsType<SolidColorBrush>(glyph.Foreground);
                    Assert.Equal(primary.Color, glyphForeground.Color);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task InfoBar_DefaultSeverity_IconBackgroundHasForegroundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Informational, Title = "Info" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock iconBackground =
                    Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(bar, "IconBackground"), exactMatch: false);
                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("SystemFillColorAttentionBrush"));
                SolidColorBrush iconBackgroundForeground = Assert.IsType<SolidColorBrush>(iconBackground.Foreground);
                Assert.Equal(expected.Color, iconBackgroundForeground.Color);
                w.Close();
            });
        }

        [Fact]
        public Task InfoBar_InformationalAccentBrushes_TrackAccentColorChangeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Informational, Title = "Info" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock iconBackground = Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(bar, "IconBackground"), exactMatch: false);
                SolidColorBrush initial = Assert.IsType<SolidColorBrush>(iconBackground.Foreground);
                Color initialColor = initial.Color;

                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0xC3, 0x00, 0x52));
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // IconBackground carries the severity brush, which is accent-derived for
                // Informational. StandardIcon's TextFillColorInverseBrush is fixed and does not
                // track the accent color.
                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("SystemFillColorAttentionBrush"));
                SolidColorBrush iconBackgroundBrush = Assert.IsType<SolidColorBrush>(iconBackground.Foreground);
                Assert.Equal(expected.Color, iconBackgroundBrush.Color);
                Assert.NotEqual(initialColor, iconBackgroundBrush.Color);

                w.Close();
            });
        }

        [Fact]
        public Task InfoBar_SeverityChange_IconBackgroundForegroundUpdatesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { IsOpen = true, Severity = InfoBarSeverity.Informational, Title = "Test" };
                Window w = new() { Content = bar, Width = 400, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.TextBlock iconBackground = Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChildByName<System.Windows.Controls.TextBlock>(bar, "IconBackground"), exactMatch: false);
                Color colorBefore = Assert.IsType<SolidColorBrush>(iconBackground.Foreground).Color;

                bar.Severity = InfoBarSeverity.Error;
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("SystemFillColorCriticalBrush"));
                Color colorAfter = Assert.IsType<SolidColorBrush>(iconBackground.Foreground).Color;
                Assert.Equal(expected.Color, colorAfter);
                Assert.NotEqual(colorBefore, colorAfter);
                w.Close();
            });
        }

        [Fact]
        public Task InfoBar_DeclaresPoliteLiveSettingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new() { Title = "Saved", IsOpen = true };
                Window window = new() { Content = bar };
                window.Show();
                _ = bar.ApplyTemplate();
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(bar));
                window.Close();
            });
        }

        [Fact]
        public Task InfoBar_ActionButton_IsNotClippedByRootBorderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                InfoBar bar = new()
                {
                    IsOpen = true,
                    Severity = InfoBarSeverity.Error,
                    Title = "Error",
                    Message = "Retry the operation.",
                    ActionButton = new Button { Content = "Retry" },
                };
                Window w = new() { Content = bar, Width = 520, Height = 120 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border root = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(bar, "RootBorder"), exactMatch: false);
                Assert.False(root.ClipToBounds,
                    "RootBorder should not clip action-button focus visuals or shadow rendering.");

                System.Windows.Controls.ContentPresenter presenter = Assert.IsType<System.Windows.Controls.ContentPresenter>(FindVisualChildByName<System.Windows.Controls.ContentPresenter>(bar, "ActionPresenter"), exactMatch: false);
                Assert.Equal(Visibility.Visible, presenter.Visibility);

                w.Close();
            });
        }
    }
}
