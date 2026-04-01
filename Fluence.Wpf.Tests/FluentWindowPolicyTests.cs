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
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Native;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class WindowPolicyTests
    {
        [TestMethod]
        public void CreateWindowChrome_UsesSharedBackdropDefaults()
        {
            var chrome = WindowPolicy.CreateWindowChrome(32);

            Assert.AreEqual(32d, chrome.CaptionHeight, "CaptionHeight should match the custom title bar height.");
            Assert.AreEqual(new Thickness(-1), chrome.GlassFrameThickness, "Glass frame should be fully extended for DWM materials.");
            Assert.AreEqual(new Thickness(4), chrome.ResizeBorderThickness, "Resize border should match the WinUI pattern.");
            Assert.AreEqual(new CornerRadius(0), chrome.CornerRadius, "Corner radius should be owned by DWM, not WPF.");
            Assert.AreEqual(NonClientFrameEdges.None, chrome.NonClientFrameEdges, "Non-client frame edges should be disabled.");
            Assert.IsFalse(chrome.UseAeroCaptionButtons, "Native caption buttons should be disabled because the template renders custom ones.");
        }

        [TestMethod]
        public void GetCornerPreference_DefaultAndRound_UseFullDwmRound()
        {
            Assert.AreEqual(
                NativeConstants.DWMWCP_ROUND,
                WindowPolicy.GetCornerPreference(CornerPreference.Default));
            Assert.AreEqual(
                NativeConstants.DWMWCP_ROUND,
                WindowPolicy.GetCornerPreference(CornerPreference.Round));
        }

        [TestMethod]
        public void GetCornerPreference_RoundSmall_UsesDwmRoundSmall()
        {
            Assert.AreEqual(
                NativeConstants.DWMWCP_ROUNDSMALL,
                WindowPolicy.GetCornerPreference(CornerPreference.RoundSmall));
        }

        [DataTestMethod]
        [DataRow(WindowState.Normal, ResizeMode.CanResize, 4d)]
        [DataRow(WindowState.Maximized, ResizeMode.CanResize, 0d)]
        [DataRow(WindowState.Normal, ResizeMode.NoResize, 0d)]
        [DataRow(WindowState.Normal, ResizeMode.CanMinimize, 0d)]
        public void GetResizeBorderThickness_ReturnsExpectedThickness(WindowState windowState, ResizeMode resizeMode, double expected)
        {
            var thickness = WindowPolicy.GetResizeBorderThickness(windowState, resizeMode);

            Assert.AreEqual(new Thickness(expected), thickness);
        }

        [TestMethod]
        public void BuildFramePlan_ForActiveAccentBorder_UsesAccentBrushAndDwmBorderColor()
        {
            var accentColor = Color.FromRgb(0x00, 0x78, 0xD4);
            var capabilities = new WindowCapabilities(
                supportsSystemBackdropType: true,
                supportsMicaEffect: false,
                supportsRoundedCorners: true,
                supportsCaptionColor: true,
                supportsBorderColor: true);

            var plan = WindowPolicy.BuildFramePlan(
                WindowState.Normal,
                isActive: true,
                isAccentBorderEnabled: true,
                capabilities,
                accentColor);

            Assert.AreEqual(new Thickness(2), plan.TemplateBorderThickness);
            Assert.AreEqual("SystemAccentColorBrush", plan.TemplateBorderBrushResourceKey);
            Assert.AreEqual(NativeMethods.ColorToAbgr(accentColor), plan.DwmBorderColor);
        }

        [TestMethod]
        public void BuildFramePlan_ForActiveWithoutAccentBorder_UsesSolidCardStrokeAndDefaultDwmBorder()
        {
            var capabilities = new WindowCapabilities(
                supportsSystemBackdropType: true,
                supportsMicaEffect: false,
                supportsRoundedCorners: true,
                supportsCaptionColor: true,
                supportsBorderColor: true);

            var plan = WindowPolicy.BuildFramePlan(
                WindowState.Normal,
                isActive: true,
                isAccentBorderEnabled: false,
                capabilities,
                Color.FromRgb(0x00, 0x78, 0xD4));

            Assert.AreEqual(new Thickness(2), plan.TemplateBorderThickness);
            Assert.AreEqual("CardStrokeColorDefaultSolidBrush", plan.TemplateBorderBrushResourceKey);
            Assert.AreEqual(NativeConstants.DWMWA_COLOR_DEFAULT, plan.DwmBorderColor);
        }

        [TestMethod]
        public void BuildFramePlan_ForInactiveWindow_UsesSubtleBorderAndDefaultDwmBorder()
        {
            var capabilities = new WindowCapabilities(
                supportsSystemBackdropType: true,
                supportsMicaEffect: false,
                supportsRoundedCorners: true,
                supportsCaptionColor: true,
                supportsBorderColor: true);

            var plan = WindowPolicy.BuildFramePlan(
                WindowState.Normal,
                isActive: false,
                isAccentBorderEnabled: true,
                capabilities,
                Color.FromRgb(0x00, 0x78, 0xD4));

            Assert.AreEqual(new Thickness(2), plan.TemplateBorderThickness);
            Assert.AreEqual("CardStrokeColorDefaultSolidBrush", plan.TemplateBorderBrushResourceKey);
            Assert.AreEqual(NativeConstants.DWMWA_COLOR_DEFAULT, plan.DwmBorderColor);
        }

        [TestMethod]
        public void BuildFramePlan_ForMaximizedWindow_RemovesTemplateBorder()
        {
            var capabilities = new WindowCapabilities(
                supportsSystemBackdropType: true,
                supportsMicaEffect: false,
                supportsRoundedCorners: true,
                supportsCaptionColor: true,
                supportsBorderColor: true);

            var plan = WindowPolicy.BuildFramePlan(
                WindowState.Maximized,
                isActive: true,
                isAccentBorderEnabled: true,
                capabilities,
                Color.FromRgb(0x00, 0x78, 0xD4));

            Assert.AreEqual(new Thickness(0), plan.TemplateBorderThickness);
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_AutoPrefersMicaWhenSupported()
        {
            var capabilities = new WindowCapabilities(
                supportsSystemBackdropType: true,
                supportsMicaEffect: false,
                supportsRoundedCorners: true,
                supportsCaptionColor: true);

            var effectiveBackdrop = WindowPolicy.ResolveEffectiveBackdrop(BackdropType.Auto, capabilities);

            Assert.AreEqual(BackdropType.Mica, effectiveBackdrop);
        }

        [TestMethod]
        public void ResolveEffectiveBackdrop_AutoFallsBackToNoneWhenUnsupported()
        {
            var capabilities = new WindowCapabilities(
                supportsSystemBackdropType: false,
                supportsMicaEffect: false,
                supportsRoundedCorners: false,
                supportsCaptionColor: false);

            var effectiveBackdrop = WindowPolicy.ResolveEffectiveBackdrop(BackdropType.Auto, capabilities);

            Assert.AreEqual(BackdropType.None, effectiveBackdrop);
        }

        [TestMethod]
        public void BuildBackdropPlan_ForSystemBackdrop_UsesTransparentBackgroundAndCaptionColorNone()
        {
            var capabilities = new WindowCapabilities(
                supportsSystemBackdropType: true,
                supportsMicaEffect: false,
                supportsRoundedCorners: true,
                supportsCaptionColor: true);

            var plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.Acrylic,
                ApplicationTheme.Dark,
                capabilities,
                Color.FromRgb(0xFA, 0xFA, 0xFA));

            Assert.AreEqual(BackdropType.Acrylic, plan.EffectiveBackdrop);
            Assert.IsTrue(plan.UseTransparentBackground, "System backdrops should let DWM paint the material.");
            Assert.AreEqual(NativeConstants.DWMWA_COLOR_NONE, plan.CaptionColor, "Backdrop windows should clear the native caption fill.");
            Assert.AreEqual(NativeConstants.DWMSBT_TRANSIENTWINDOW, plan.SystemBackdropType.Value, "Acrylic should map to the transient system backdrop.");
            Assert.IsFalse(plan.UseLegacyMicaEffect, "Modern Acrylic should not use the legacy Mica attribute.");
            Assert.IsTrue(plan.UseImmersiveDarkMode, "Dark theme windows should request dark immersive chrome.");
        }

        [TestMethod]
        public void BuildBackdropPlan_ForNone_UsesFallbackBackgroundAndDefaultCaptionColor()
        {
            var fallbackColor = Color.FromRgb(0xFA, 0xFA, 0xFA);
            var capabilities = new WindowCapabilities(
                supportsSystemBackdropType: true,
                supportsMicaEffect: false,
                supportsRoundedCorners: true,
                supportsCaptionColor: true);

            var plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.None,
                ApplicationTheme.Light,
                capabilities,
                fallbackColor);

            Assert.AreEqual(BackdropType.None, plan.EffectiveBackdrop);
            Assert.IsFalse(plan.UseTransparentBackground, "No-backdrop windows should use the fallback background.");
            Assert.AreEqual(fallbackColor, plan.BackgroundColor);
            Assert.AreEqual(NativeConstants.DWMWA_COLOR_DEFAULT, plan.CaptionColor, "No-backdrop windows should restore the default caption fill.");
            Assert.AreEqual(NativeConstants.DWMSBT_NONE, plan.SystemBackdropType.Value, "Removing a backdrop should explicitly disable system backdrop materials.");
            Assert.IsFalse(plan.UseLegacyMicaEffect, "No-backdrop windows should not enable the legacy Mica effect.");
            Assert.IsFalse(plan.UseImmersiveDarkMode, "Light theme windows should not request dark immersive chrome.");
        }

        [TestMethod]
        public void BuildBackdropPlan_ForLegacyMica_UsesMicaEffectInsteadOfSystemBackdropType()
        {
            var capabilities = new WindowCapabilities(
                supportsSystemBackdropType: false,
                supportsMicaEffect: true,
                supportsRoundedCorners: true,
                supportsCaptionColor: true);

            var plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.Mica,
                ApplicationTheme.Dark,
                capabilities,
                Color.FromRgb(0x20, 0x20, 0x20));

            Assert.AreEqual(BackdropType.Mica, plan.EffectiveBackdrop);
            Assert.IsTrue(plan.UseTransparentBackground);
            Assert.IsTrue(plan.UseLegacyMicaEffect, "Pre-22621 Mica should use the legacy attribute.");
            Assert.IsNull(plan.SystemBackdropType, "Legacy Mica should not set the newer system backdrop type.");
        }

        [TestMethod]
        public void BuildBackdropPlan_ForUnsupportedExplicitBackdrop_FallsBackToLegacyMica()
        {
            var capabilities = new WindowCapabilities(
                supportsSystemBackdropType: false,
                supportsMicaEffect: true,
                supportsRoundedCorners: true,
                supportsCaptionColor: true);

            var plan = WindowPolicy.BuildBackdropPlan(
                BackdropType.Acrylic,
                ApplicationTheme.Dark,
                capabilities,
                Color.FromRgb(0x20, 0x20, 0x20));

            Assert.AreEqual(BackdropType.Mica, plan.EffectiveBackdrop, "Unsupported modern backdrops should fall back to the shared legacy Mica path.");
            Assert.IsTrue(plan.UseLegacyMicaEffect);
            Assert.IsNull(plan.SystemBackdropType);
        }
    }
}
