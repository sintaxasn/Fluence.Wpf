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

using Fluence.Wpf.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Verifies the wallpaper-tint model fit against the measured WinUI 3 Gallery content-layer
    /// table, the neutral-wallpaper exact-passthrough guarantee, and the live engine integration.
    /// </summary>
    [TestClass]
    public class WallpaperTintHelperTests
    {
        // Wallpaper average colors extracted from F:\Images\WinUI_Light_{White,Red,Orange,Yellow}.png
        // corner patches (20x20 at 10,10; identical across Light/Dark screenshots since the desktop
        // is unaffected by app theme). These are solid Windows desktop-background swatches, not
        // photographic wallpapers.
        private static readonly Color WallWhite = Color.FromRgb(255, 255, 255);
        private static readonly Color WallRed = Color.FromRgb(232, 17, 35);
        private static readonly Color WallOrange = Color.FromRgb(255, 140, 0);
        private static readonly Color WallYellow = Color.FromRgb(255, 255, 0);

        [TestInitialize]
        public void TestInitialize()
        {
            WpfTestSta.Invoke(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                Application.Current.Resources.MergedDictionaries.Clear();
            });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Restore the deterministic neutral override every other fixture relies on (set by
            // WpfTestSta.EnsureApplication); tests below override it locally.
            WallpaperTintHelper.OverrideAverageColor = Color.FromRgb(0xFF, 0xFF, 0xFF);
        }

        // Measured WinUI 3 Gallery content-layer table (exact flat pixel values). Tolerance is
        // +/-6 per channel per the fit spec.
        [TestMethod]
        public void ComputeContentBackground_LightWhite_MatchesMeasuredWithinTolerance()
        {
            AssertContentWithinTolerance(WallWhite, isDark: false, expected: Color.FromRgb(249, 249, 249));
        }

        [TestMethod]
        public void ComputeContentBackground_LightRed_MatchesMeasuredWithinTolerance()
        {
            AssertContentWithinTolerance(WallRed, isDark: false, expected: Color.FromRgb(252, 248, 248));
        }

        [TestMethod]
        public void ComputeContentBackground_LightOrange_MatchesMeasuredWithinTolerance()
        {
            AssertContentWithinTolerance(WallOrange, isDark: false, expected: Color.FromRgb(252, 249, 244));
        }

        [TestMethod]
        public void ComputeContentBackground_LightYellow_MatchesMeasuredWithinTolerance()
        {
            AssertContentWithinTolerance(WallYellow, isDark: false, expected: Color.FromRgb(252, 252, 225));
        }

        [TestMethod]
        public void ComputeContentBackground_DarkWhite_MatchesMeasuredWithinTolerance()
        {
            AssertContentWithinTolerance(WallWhite, isDark: true, expected: Color.FromRgb(39, 39, 39));
        }

        [TestMethod]
        public void ComputeContentBackground_DarkRed_MatchesMeasuredWithinTolerance()
        {
            AssertContentWithinTolerance(WallRed, isDark: true, expected: Color.FromRgb(49, 35, 36));
        }

        [TestMethod]
        public void ComputeContentBackground_DarkOrange_MatchesMeasuredWithinTolerance()
        {
            AssertContentWithinTolerance(WallOrange, isDark: true, expected: Color.FromRgb(42, 39, 35));
        }

        [TestMethod]
        public void ComputeContentBackground_DarkYellow_MatchesMeasuredWithinTolerance()
        {
            AssertContentWithinTolerance(WallYellow, isDark: true, expected: Color.FromRgb(40, 40, 35));
        }

        [TestMethod]
        public void ComputeContentBackground_LightNeutralWhite_IsExactlyF9F9F9()
        {
            Color content = WallpaperTintHelper.ComputeContentBackground(WallWhite, isDark: false);
            Assert.AreEqual(Color.FromArgb(0xFF, 0xF9, 0xF9, 0xF9), content,
                "A white (achromatic) wallpaper must resolve to exactly the neutral fallback #FFF9F9F9.");
        }

        [TestMethod]
        public void ComputeContentBackground_DarkNeutralWhite_IsExactly272727()
        {
            Color content = WallpaperTintHelper.ComputeContentBackground(WallWhite, isDark: true);
            Assert.AreEqual(Color.FromArgb(0xFF, 0x27, 0x27, 0x27), content,
                "A white (achromatic) wallpaper must resolve to exactly the neutral fallback #FF272727.");
        }

        // Any achromatic wallpaper - not only white - must pass through exactly, since the model's
        // chroma-residual delta is zero whenever R=G=B regardless of brightness.
        [TestMethod]
        public void ComputeContentBackground_AnyAchromaticWallpaper_PassesThroughExactly()
        {
            Color midGray = Color.FromRgb(128, 128, 128);
            Color darkGray = Color.FromRgb(40, 40, 40);

            Assert.AreEqual(Color.FromArgb(0xFF, 0xF9, 0xF9, 0xF9), WallpaperTintHelper.ComputeContentBackground(midGray, isDark: false));
            Assert.AreEqual(Color.FromArgb(0xFF, 0xF9, 0xF9, 0xF9), WallpaperTintHelper.ComputeContentBackground(darkGray, isDark: false));
            Assert.AreEqual(Color.FromArgb(0xFF, 0x27, 0x27, 0x27), WallpaperTintHelper.ComputeContentBackground(midGray, isDark: true));
            Assert.AreEqual(Color.FromArgb(0xFF, 0x27, 0x27, 0x27), WallpaperTintHelper.ComputeContentBackground(darkGray, isDark: true));
        }

        [TestMethod]
        public void ComputeContentBackground_IsAlwaysOpaque()
        {
            Color content = WallpaperTintHelper.ComputeContentBackground(WallRed, isDark: false);
            Assert.AreEqual((byte)0xFF, content.A, "NavigationViewContentBackground must always be fully opaque.");
        }

        [TestMethod]
        public void EstimateMicaColor_MatchesMeasuredPaneWithinTolerance()
        {
            // Cross-check the intermediate pane estimate directly against the measured raw-Mica
            // table (separate from the content-layer table above), so a regression in either
            // EstimateMicaColor or PreBlendContent is isolated to the failing test.
            AssertWithinTolerance(WallpaperTintHelper.EstimateMicaColor(WallWhite, isDark: false), Color.FromRgb(243, 243, 243), 6, "Light White pane");
            AssertWithinTolerance(WallpaperTintHelper.EstimateMicaColor(WallRed, isDark: false), Color.FromRgb(249, 240, 241), 6, "Light Red pane");
            AssertWithinTolerance(WallpaperTintHelper.EstimateMicaColor(WallOrange, isDark: false), Color.FromRgb(249, 242, 233), 6, "Light Orange pane");
            AssertWithinTolerance(WallpaperTintHelper.EstimateMicaColor(WallYellow, isDark: false), Color.FromRgb(249, 249, 194), 6, "Light Yellow pane");
            AssertWithinTolerance(WallpaperTintHelper.EstimateMicaColor(WallWhite, isDark: true), Color.FromRgb(32, 32, 32), 6, "Dark White pane");
            AssertWithinTolerance(WallpaperTintHelper.EstimateMicaColor(WallRed, isDark: true), Color.FromRgb(46, 26, 27), 6, "Dark Red pane");
            AssertWithinTolerance(WallpaperTintHelper.EstimateMicaColor(WallOrange, isDark: true), Color.FromRgb(36, 31, 26), 6, "Dark Orange pane");
            AssertWithinTolerance(WallpaperTintHelper.EstimateMicaColor(WallYellow, isDark: true), Color.FromRgb(33, 33, 26), 6, "Dark Yellow pane");
        }

        // Engine integration: a saturated override must shift the published brush away from the
        // neutral fallback, and resetting to neutral must restore it exactly.
        [TestMethod]
        public void Engine_SaturatedOverride_ShiftsPublishedBrush_NeutralOverride_RestoresExactFallback()
        {
            WpfTestSta.Invoke(() =>
            {
                try
                {
                    WallpaperTintHelper.OverrideAverageColor = WallRed;
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);

                    SolidColorBrush? tinted = Application.Current.Resources["NavigationViewContentBackgroundBrush"] as SolidColorBrush;
                    Assert.IsNotNull(tinted, "NavigationViewContentBackgroundBrush should be defined.");
                    Assert.AreNotEqual(Color.FromArgb(0xFF, 0xF9, 0xF9, 0xF9), tinted.Color,
                        "A saturated wallpaper override must shift NavigationViewContentBackground away from the neutral fallback.");

                    WallpaperTintHelper.OverrideAverageColor = Color.FromRgb(0xFF, 0xFF, 0xFF);
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: false);

                    SolidColorBrush? neutral = Application.Current.Resources["NavigationViewContentBackgroundBrush"] as SolidColorBrush;
                    Assert.IsNotNull(neutral, "NavigationViewContentBackgroundBrush should be defined.");
                    Assert.AreEqual(Color.FromArgb(0xFF, 0xF9, 0xF9, 0xF9), neutral.Color,
                        "Resetting to a neutral wallpaper override must restore the exact #F9F9F9 fallback.");
                }
                finally
                {
                    WallpaperTintHelper.OverrideAverageColor = Color.FromRgb(0xFF, 0xFF, 0xFF);
                }
            });
        }

        [TestMethod]
        public void Engine_SaturatedOverride_ShiftsPublishedBrush_DarkTheme()
        {
            WpfTestSta.Invoke(() =>
            {
                try
                {
                    WallpaperTintHelper.OverrideAverageColor = WallRed;
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: false);

                    SolidColorBrush? tinted = Application.Current.Resources["NavigationViewContentBackgroundBrush"] as SolidColorBrush;
                    Assert.IsNotNull(tinted, "NavigationViewContentBackgroundBrush should be defined.");
                    Assert.AreNotEqual(Color.FromArgb(0xFF, 0x27, 0x27, 0x27), tinted.Color,
                        "A saturated wallpaper override must shift NavigationViewContentBackground away from the neutral fallback.");
                }
                finally
                {
                    WallpaperTintHelper.OverrideAverageColor = Color.FromRgb(0xFF, 0xFF, 0xFF);
                }
            });
        }

        [TestMethod]
        public void Engine_HighContrast_NeverTinted()
        {
            WpfTestSta.Invoke(() =>
            {
                try
                {
                    WallpaperTintHelper.OverrideAverageColor = WallRed;
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: false);

                    SolidColorBrush? brush = Application.Current.Resources["NavigationViewContentBackgroundBrush"] as SolidColorBrush;
                    Assert.IsNotNull(brush, "NavigationViewContentBackgroundBrush should be defined in High Contrast.");
                    Assert.AreEqual(SystemColors.WindowColor, brush.Color,
                        "High Contrast must keep binding NavigationViewContentBackground to the live SystemColors.Window, never a wallpaper tint.");
                }
                finally
                {
                    WallpaperTintHelper.OverrideAverageColor = Color.FromRgb(0xFF, 0xFF, 0xFF);
                }
            });
        }

        [TestMethod]
        public void IsWallpaperSettingChange_SpiSetDeskWallpaperWParam_ReturnsTrue()
        {
            IntPtr wParam = new((int)Fluence.Wpf.Native.NativeConstants.SPI_SETDESKWALLPAPER);
            Assert.IsTrue(SystemThemeWatcher.IsWallpaperSettingChange(wParam, IntPtr.Zero));
        }

        [TestMethod]
        public void IsWallpaperSettingChange_WallpaperLParamString_ReturnsTrue()
        {
            IntPtr lParam = System.Runtime.InteropServices.Marshal.StringToHGlobalUni("Wallpaper");
            try
            {
                Assert.IsTrue(SystemThemeWatcher.IsWallpaperSettingChange(IntPtr.Zero, lParam));
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(lParam);
            }
        }

        [TestMethod]
        public void IsWallpaperSettingChange_UnrelatedBroadcast_ReturnsFalse()
        {
            IntPtr lParam = System.Runtime.InteropServices.Marshal.StringToHGlobalUni("intl");
            try
            {
                Assert.IsFalse(SystemThemeWatcher.IsWallpaperSettingChange(IntPtr.Zero, lParam));
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(lParam);
            }
        }

        private static void AssertContentWithinTolerance(Color wallpaper, bool isDark, Color expected)
        {
            Color actual = WallpaperTintHelper.ComputeContentBackground(wallpaper, isDark);
            AssertWithinTolerance(actual, expected, 6, (isDark ? "Dark" : "Light") + " content");
        }

        private static void AssertWithinTolerance(Color actual, Color expected, int tolerance, string label)
        {
            AssertChannelWithinTolerance(actual.R, expected.R, tolerance, label, "R");
            AssertChannelWithinTolerance(actual.G, expected.G, tolerance, label, "G");
            AssertChannelWithinTolerance(actual.B, expected.B, tolerance, label, "B");
        }

        private static void AssertChannelWithinTolerance(byte actual, byte expected, int tolerance, string label, string channel)
        {
            int diff = Math.Abs(actual - expected);
            Assert.IsTrue(diff <= tolerance, string.Format(CultureInfo.InvariantCulture,
                "{0} channel {1}: expected {2} within +/-{3}, got {4} (diff {5}).",
                label, channel, expected, tolerance, actual, diff));
        }
    }
}
