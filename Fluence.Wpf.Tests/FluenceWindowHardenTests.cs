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
using Fluence.Wpf.Controls;
using System.Windows.Media;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-2 hardening tests for FluenceWindow: backdrop swap, full HC theme cycle,
    /// close-button DynamicResource fix (Finding B).
    /// </summary>
    [TestClass]
    public class FluenceWindowHardenTests
    {
        private static void RunOnStaThread(Action action)
        {
            Exception? captured = null;
            WpfTestSta.Dispatcher?.Invoke(new Action(delegate
            {
                try { action(); }
                catch (Exception ex) { captured = ex; }
            }));

            if (captured is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(captured).Throw();
            }
        }

        private static Application? EnsureApp()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static string GetRepositoryFilePath(params string[] relativeSegments)
        {
            string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
            string[] pathParts = new string[relativeSegments.Length + 1];
            pathParts[0] = root;
            Array.Copy(relativeSegments, 0, pathParts, 1, relativeSegments.Length);
            return Path.Combine(pathParts);
        }

        private static string ReadRepositoryFile(params string[] relativeSegments)
        {
            string path = GetRepositoryFilePath(relativeSegments);
            Assert.IsTrue(File.Exists(path), "Repository file must be readable at: " + path);
            return File.ReadAllText(path);
        }

        private static void ResetAndApply(ApplicationTheme theme, Application? app = null)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            app?.Resources.MergedDictionaries.Clear();

            ApplicationThemeManager.Apply(theme, BackdropType.None, true);
        }

        // ---------------------------------------------------------------------------
        // 1. SystemBackdropType DP defaults and round-trip
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void SystemBackdropType_Default_IsAuto()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                FluenceWindow w = new();
                try
                {
                    Assert.AreEqual(BackdropType.Auto, w.SystemBackdropType,
                        "SystemBackdropType must default to BackdropType.Auto.");
                }
                finally { w.Close(); }
            });
        }

        [TestMethod]
        public void SystemBackdropType_CanSetAllValues()
        {
            // Verifies that the DP accepts all four BackdropType values without throwing.
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);
                FluenceWindow w = new();
                try
                {
                    foreach (BackdropType bd in new[] { BackdropType.None, BackdropType.Mica, BackdropType.Acrylic, BackdropType.Tabbed, BackdropType.Auto })
                    {
                        w.SystemBackdropType = bd;
                        Assert.AreEqual(bd, w.SystemBackdropType,
                            "SystemBackdropType DP must accept and reflect: " + bd);
                    }
                }
                finally { w.Close(); }
            });
        }

        // ---------------------------------------------------------------------------
        // 2. Full theme cycle Light → Dark → HighContrast → Light; key brushes resolve
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ThemeCycle_LightDarkHcLight_KeyBrushesResolveAfterEachStep()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);

                string[] keys =
                [
                    "ApplicationBackgroundBrush",
                    "TextFillColorPrimaryBrush",
                    "TextFillColorSecondaryBrush",
                    "ControlFillColorDefaultBrush",
                    "SystemFillColorCriticalBrush",
                    "WindowCloseButtonBackgroundPointerOverBrush",
                    "WindowCloseButtonBackgroundPressedBrush",
                    "WindowCloseButtonForegroundPointerOverBrush"
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    foreach (string? key in keys)
                    {
                        object? resource = app?.TryFindResource(key);
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
            // Caption close-button chrome uses its own DynamicResource tokens; this guard keeps the
            // general critical brush available for controls that intentionally consume it.
            RunOnStaThread(() =>
            {
                Application? app = EnsureApp();
                ResetAndApply(ApplicationTheme.Light, app);

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, true);
                object? brush = app?.TryFindResource("SystemFillColorCriticalBrush");
                Assert.IsNotNull(brush,
                    "SystemFillColorCriticalBrush must resolve in HighContrast theme.");
            });
        }

        // ---------------------------------------------------------------------------
        // 4. Close button resource-token and template-part regression guards.
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void FluenceWindowXaml_CloseButtonHover_UsesCanonicalCloseButtonBrushTokens()
        {
            string xaml = ReadRepositoryFile("Fluence.Wpf", "Themes", "Controls", "FluenceWindow.xaml");

            StringAssert.Contains(xaml, "WindowCloseButtonBackgroundPointerOverBrush");
            StringAssert.Contains(xaml, "WindowCloseButtonBackgroundPressedBrush");
            StringAssert.Contains(xaml, "WindowCloseButtonForegroundPointerOverBrush");

            Assert.IsFalse(xaml.Contains("WindowCloseFillColorHoverBrush"),
                "FluenceWindow.xaml should consume the canonical close-button background token.");
            Assert.IsFalse(xaml.Contains("WindowCloseFillColorPressedBrush"),
                "FluenceWindow.xaml should consume the canonical close-button pressed token.");
            Assert.IsFalse(xaml.Contains("WindowCloseForegroundHoverBrush"),
                "FluenceWindow.xaml should consume the canonical close-button foreground token.");
            Assert.IsFalse(xaml.Contains("SystemFillColorCriticalBrush"),
                "Caption close-button hover must not use the general critical brush.");
            Assert.IsFalse(xaml.Contains("#C42B1C") || xaml.Contains("#B4271C") || xaml.Contains("#FFFFFF"),
                "Production control templates must not inline close-button hex colors.");
        }

        [TestMethod]
        public void FluenceWindowCloseButtonThemeTokens_AreDefinedForAllManagedThemes()
        {
            AssertCloseButtonThemeTokens("Theme.Light.xaml");
            AssertCloseButtonThemeTokens("Theme.Dark.xaml");
            AssertCloseButtonThemeTokens("Theme.HighContrast.xaml");

            string brushes = ReadRepositoryFile("Fluence.Wpf", "Themes", "Brushes", "Brushes.xaml");
            StringAssert.Contains(brushes, "WindowCloseButtonBackgroundPointerOverBrush");
            StringAssert.Contains(brushes, "WindowCloseButtonBackgroundPressedBrush");
            StringAssert.Contains(brushes, "WindowCloseButtonForegroundPointerOverBrush");
            StringAssert.Contains(brushes, "WindowCloseButtonBackgroundPointerOver");
            StringAssert.Contains(brushes, "WindowCloseButtonBackgroundPressed");
            StringAssert.Contains(brushes, "WindowCloseButtonForegroundPointerOver");
        }

        [TestMethod]
        public void FluenceWindow_DeclaresCaptionButtonTemplateParts()
        {
            object[] attributes = typeof(FluenceWindow).GetCustomAttributes(typeof(TemplatePartAttribute), false);

            AssertTemplatePart(attributes, "PART_MinimizeButton");
            AssertTemplatePart(attributes, "PART_MaximizeButton");
            AssertTemplatePart(attributes, "PART_RestoreButton");
            AssertTemplatePart(attributes, "PART_CloseButton");
        }

        private static void AssertCloseButtonThemeTokens(string themeFileName)
        {
            string theme = ReadRepositoryFile("Fluence.Wpf", "Themes", "Colors", themeFileName);

            StringAssert.Contains(theme, "<Color x:Key=\"WindowCloseButtonBackgroundPointerOver\">#FFC42B1C</Color>");
            StringAssert.Contains(theme, "<Color x:Key=\"WindowCloseButtonBackgroundPressed\">#FFB4271C</Color>");
            StringAssert.Contains(theme, "<Color x:Key=\"WindowCloseButtonForegroundPointerOver\">#FFFFFFFF</Color>");
        }

        private static void AssertTemplatePart(object[] attributes, string name)
        {
            foreach (object attribute in attributes)
            {
                if (attribute is TemplatePartAttribute templatePath && templatePath.Name == name && templatePath.Type == typeof(System.Windows.Controls.Button))
                {
                    return;
                }
            }

            Assert.Fail("FluenceWindow must declare TemplatePart '" + name + "' with type System.Windows.Controls.Button.");
        }

        // ---------------------------------------------------------------------------
        // 5. WindowPolicy.BuildBackdropPlan — None backdrop returns non-transparent bg
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void BuildBackdropPlan_None_ReturnsOpaqueBackground()
        {
            // Capability with no backdrop support at all.
            WindowCapabilities caps = new(
                supportsSystemBackdropType: false,
                supportsMicaEffect: false,
                supportsRoundedCorners: false,
                supportsCaptionColor: false,
                supportsBorderColor: false);

            Color light = Color.FromRgb(0xFA, 0xFA, 0xFA);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(BackdropType.None, ApplicationTheme.Light, caps, light);

            Assert.IsFalse(plan.UseTransparentBackground,
                "BackdropType.None must NOT use transparent background.");
            Assert.AreNotEqual(Colors.Transparent, plan.BackgroundColor,
                "BackdropType.None must return a fallback opaque background color.");
        }

        [TestMethod]
        public void BuildBackdropPlan_Mica_SupportedOs_ReturnsTransparent()
        {
            WindowCapabilities caps = new(
                supportsSystemBackdropType: true,
                supportsMicaEffect: true,
                supportsRoundedCorners: true,
                supportsCaptionColor: true,
                supportsBorderColor: true);

            Color fallback = Color.FromRgb(0xFA, 0xFA, 0xFA);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(BackdropType.Mica, ApplicationTheme.Light, caps, fallback);

            Assert.IsTrue(plan.UseTransparentBackground,
                "Mica backdrop on a capable OS must use transparent background.");
            Assert.AreEqual(Colors.Transparent, plan.BackgroundColor,
                "Mica backdrop on a capable OS must set Colors.Transparent as the background color.");
        }

        [TestMethod]
        public void BuildBackdropPlan_Acrylic_FallsBackToMica_WhenMicaEffectButNoSystemBackdrop()
        {
            // Windows 10 21H2: supports DwmSetWindowAttribute(DWMWA_MICA_EFFECT) but NOT
            // DWMWA_SYSTEMBACKDROP_TYPE. Acrylic request must downgrade to Mica.
            WindowCapabilities caps = new(
                supportsSystemBackdropType: false,
                supportsMicaEffect: true,
                supportsRoundedCorners: false,
                supportsCaptionColor: false);

            Color fallback = Color.FromRgb(0x20, 0x20, 0x20);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(BackdropType.Acrylic, ApplicationTheme.Dark, caps, fallback);

            // Should fall back to Mica (legacy) and use transparent background.
            Assert.IsTrue(plan.UseTransparentBackground,
                "Acrylic→Mica fallback must still use transparent background.");
            Assert.AreEqual(BackdropType.Mica, plan.EffectiveBackdrop,
                "Acrylic request on Win10 MicaEffect-only OS must downgrade to Mica.");
        }
    }
}
