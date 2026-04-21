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
using System.IO;
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-2 hardening tests for FluenceWindow: backdrop swap, full HC theme cycle,
    /// close-button DynamicResource fix (Finding B), and TitleBarLeftIndent DP.
    /// </summary>
    [TestClass]
    public class FluenceWindowHardenTests
    {
        private static void RunOnStaThread(Action action)
        {
            Exception captured = null;
            WpfTestSta.Dispatcher.Invoke(new Action(delegate
            {
                try { action(); }
                catch (Exception ex) { captured = ex; }
            }));

            if (captured != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(captured).Throw();
            }
        }

        private static Application EnsureApp()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static void ResetAndApply(ApplicationTheme theme, Application app = null)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            if (app != null)
            {
                app.Resources.MergedDictionaries.Clear();
            }

            ApplicationThemeManager.Apply(theme, BackdropType.None, true);
        }

        // ---------------------------------------------------------------------------
        // 1. TitleBarLeftIndent DP — baseline assertion for snapshot delta
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void TitleBarLeftIndent_DefaultIsZero()
        {
            RunOnStaThread(() =>
            {
                var app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                var w = new FluenceWindow();
                try
                {
                    Assert.AreEqual(0d, w.TitleBarLeftIndent,
                        "TitleBarLeftIndent must default to 0.0 (snapshot baseline).");
                }
                finally { w.Close(); }
            });
        }

        [TestMethod]
        public void TitleBarLeftIndent_RoundTrip()
        {
            RunOnStaThread(() =>
            {
                var app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                var w = new FluenceWindow();
                try
                {
                    w.TitleBarLeftIndent = 48d;
                    Assert.AreEqual(48d, w.TitleBarLeftIndent,
                        "TitleBarLeftIndent DP round-trip must preserve assigned value.");
                    w.TitleBarLeftIndent = 0d;
                    Assert.AreEqual(0d, w.TitleBarLeftIndent);
                }
                finally { w.Close(); }
            });
        }

        // ---------------------------------------------------------------------------
        // 2. WindowBackdrop DP defaults and round-trip
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void WindowBackdrop_Default_IsAuto()
        {
            RunOnStaThread(() =>
            {
                var app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                var w = new FluenceWindow();
                try
                {
                    Assert.AreEqual(BackdropType.Auto, w.WindowBackdrop,
                        "WindowBackdrop must default to BackdropType.Auto.");
                }
                finally { w.Close(); }
            });
        }

        [TestMethod]
        public void WindowBackdrop_CanSetAllValues()
        {
            // Verifies that the DP accepts all four BackdropType values without throwing.
            RunOnStaThread(() =>
            {
                var app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                var w = new FluenceWindow();
                try
                {
                    foreach (var bd in new[] { BackdropType.None, BackdropType.Mica, BackdropType.Acrylic, BackdropType.Tabbed, BackdropType.Auto })
                    {
                        w.WindowBackdrop = bd;
                        Assert.AreEqual(bd, w.WindowBackdrop,
                            "WindowBackdrop DP must accept and reflect: " + bd);
                    }
                }
                finally { w.Close(); }
            });
        }

        // ---------------------------------------------------------------------------
        // 3. Full theme cycle Light → Dark → HighContrast → Light; key brushes resolve
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ThemeCycle_LightDarkHcLight_KeyBrushesResolveAfterEachStep()
        {
            RunOnStaThread(() =>
            {
                var app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);

                var keys = new[]
                {
                    "ApplicationBackgroundBrush",
                    "TextFillColorPrimaryBrush",
                    "TextFillColorSecondaryBrush",
                    "ControlFillColorDefaultBrush",
                    "SystemFillColorCriticalBrush"
                };

                foreach (var theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    foreach (var key in keys)
                    {
                        var resource = app.TryFindResource(key);
                        Assert.IsNotNull(resource,
                            "Resource '" + key + "' must resolve after switching to " + theme);
                    }
                }
            });
        }

        [TestMethod]
        public void ThemeCycle_HighContrast_SystemFillColorCriticalBrush_Resolves()
        {
            // HC theme maps SystemFillColorCriticalBrush to WindowTextColorKey (white on black).
            // If close button used a hardcoded #C42B1C this brush would be ignored in HC mode,
            // breaking accessibility. This test verifies the resource is available for DynamicResource.
            RunOnStaThread(() =>
            {
                var app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, true);
                var brush = app.TryFindResource("SystemFillColorCriticalBrush");
                Assert.IsNotNull(brush,
                    "SystemFillColorCriticalBrush must resolve in HighContrast theme.");
            });
        }

        // ---------------------------------------------------------------------------
        // 4. Finding B: close button must use DynamicResource, not hardcoded hex
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void FluenceWindowXaml_CloseButtonHover_UsesDynamicResource_NotHardcodedHex()
        {
            // Finding B from WI-2 audit: the close button hover/pressed colors must reference
            // {DynamicResource SystemFillColorCriticalBrush} so that High Contrast themes can
            // override the red. Hardcoded #C42B1C renders correctly in Light (same value) but
            // fails HC accessibility requirements.
            var xamlPath = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\Fluence.Wpf\Themes\Controls\FluenceWindow.xaml"));

            Assert.IsTrue(File.Exists(xamlPath),
                "FluenceWindow.xaml must be readable at: " + xamlPath);

            string xaml = File.ReadAllText(xamlPath);

            // Verify hardcoded red hex colors are NOT present in hover/pressed triggers.
            Assert.IsFalse(
                xaml.Contains("#C42B1C"),
                "FluenceWindow.xaml must NOT contain hardcoded hex #C42B1C for close button hover. Use {DynamicResource SystemFillColorCriticalBrush} instead.");

            Assert.IsFalse(
                xaml.Contains("#B4271C"),
                "FluenceWindow.xaml must NOT contain hardcoded hex #B4271C for close button pressed. Use {DynamicResource SystemFillColorCriticalBackgroundBrush} or similar DynamicResource.");

            // Verify the DynamicResource key IS referenced.
            Assert.IsTrue(
                xaml.Contains("SystemFillColorCriticalBrush"),
                "FluenceWindow.xaml must reference SystemFillColorCriticalBrush for close button hover state.");
        }

        // ---------------------------------------------------------------------------
        // 5. WindowPolicy.BuildBackdropPlan — None backdrop returns non-transparent bg
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void BuildBackdropPlan_None_ReturnsOpaqueBackground()
        {
            // Capability with no backdrop support at all.
            var caps = new WindowCapabilities(
                supportsSystemBackdropType: false,
                supportsMicaEffect: false,
                supportsRoundedCorners: false,
                supportsCaptionColor: false,
                supportsBorderColor: false);

            var light = System.Windows.Media.Color.FromRgb(0xFA, 0xFA, 0xFA);
            var plan = WindowPolicy.BuildBackdropPlan(BackdropType.None, ApplicationTheme.Light, caps, light);

            Assert.IsFalse(plan.UseTransparentBackground,
                "BackdropType.None must NOT use transparent background.");
            Assert.AreNotEqual(System.Windows.Media.Colors.Transparent, plan.BackgroundColor,
                "BackdropType.None must return a fallback opaque background color.");
        }

        [TestMethod]
        public void BuildBackdropPlan_Mica_SupportedOs_ReturnsTransparent()
        {
            var caps = new WindowCapabilities(
                supportsSystemBackdropType: true,
                supportsMicaEffect: true,
                supportsRoundedCorners: true,
                supportsCaptionColor: true,
                supportsBorderColor: true);

            var fallback = System.Windows.Media.Color.FromRgb(0xFA, 0xFA, 0xFA);
            var plan = WindowPolicy.BuildBackdropPlan(BackdropType.Mica, ApplicationTheme.Light, caps, fallback);

            Assert.IsTrue(plan.UseTransparentBackground,
                "Mica backdrop on a capable OS must use transparent background.");
            Assert.AreEqual(System.Windows.Media.Colors.Transparent, plan.BackgroundColor,
                "Mica backdrop on a capable OS must set Colors.Transparent as the background color.");
        }

        [TestMethod]
        public void BuildBackdropPlan_Acrylic_FallsBackToMica_WhenMicaEffectButNoSystemBackdrop()
        {
            // Windows 10 21H2: supports DwmSetWindowAttribute(DWMWA_MICA_EFFECT) but NOT
            // DWMWA_SYSTEMBACKDROP_TYPE. Acrylic request must downgrade to Mica.
            var caps = new WindowCapabilities(
                supportsSystemBackdropType: false,
                supportsMicaEffect: true,
                supportsRoundedCorners: false,
                supportsCaptionColor: false);

            var fallback = System.Windows.Media.Color.FromRgb(0x20, 0x20, 0x20);
            var plan = WindowPolicy.BuildBackdropPlan(BackdropType.Acrylic, ApplicationTheme.Dark, caps, fallback);

            // Should fall back to Mica (legacy) and use transparent background.
            Assert.IsTrue(plan.UseTransparentBackground,
                "Acrylic→Mica fallback must still use transparent background.");
            Assert.AreEqual(BackdropType.Mica, plan.EffectiveBackdrop,
                "Acrylic request on Win10 MicaEffect-only OS must downgrade to Mica.");
        }
    }
}
