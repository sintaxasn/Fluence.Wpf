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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class AccentColorManagerTests
    {
        private static Application _app;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            if (Application.Current == null)
            {
                _app = new Application();
                _app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }
            else
            {
                _app = Application.Current;
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            _app.Resources.MergedDictionaries.Clear();
        }

        [TestMethod]
        public void ApplySystemAccent_PopulatesRamp()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
            ApplicationAccentColorManager.ApplySystemAccent();

            var defaultBlue = Color.FromRgb(0x00, 0x78, 0xD4);

            Assert.AreNotEqual(default(Color), ApplicationAccentColorManager.SystemAccentColor,
                "SystemAccentColor should not be default");
            Assert.AreNotEqual(default(Color), ApplicationAccentColorManager.SystemAccentColorLight1,
                "SystemAccentColorLight1 should not be default");
            Assert.AreNotEqual(default(Color), ApplicationAccentColorManager.SystemAccentColorLight2,
                "SystemAccentColorLight2 should not be default");
            Assert.AreNotEqual(default(Color), ApplicationAccentColorManager.SystemAccentColorLight3,
                "SystemAccentColorLight3 should not be default");
            Assert.AreNotEqual(default(Color), ApplicationAccentColorManager.SystemAccentColorDark1,
                "SystemAccentColorDark1 should not be default");
            Assert.AreNotEqual(default(Color), ApplicationAccentColorManager.SystemAccentColorDark2,
                "SystemAccentColorDark2 should not be default");
            Assert.AreNotEqual(default(Color), ApplicationAccentColorManager.SystemAccentColorDark3,
                "SystemAccentColorDark3 should not be default");

            var accentResource = _app.Resources["SystemAccentColor"];
            Assert.IsNotNull(accentResource, "SystemAccentColor resource should be set");
        }

        [TestMethod]
        public void ApplyCustomAccent_SetsCorrectBase()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);

            var customColor = Color.FromRgb(0xFF, 0x88, 0x00);
            ApplicationAccentColorManager.ApplyCustomAccent(customColor);

            Assert.AreEqual(customColor, ApplicationAccentColorManager.SystemAccentColor,
                "SystemAccentColor should match custom color");

            Assert.AreNotEqual(customColor, ApplicationAccentColorManager.SystemAccentColorLight1,
                "Light1 should differ from base");
            Assert.AreNotEqual(customColor, ApplicationAccentColorManager.SystemAccentColorDark1,
                "Dark1 should differ from base");

            Assert.AreNotEqual(ApplicationAccentColorManager.SystemAccentColorLight1,
                ApplicationAccentColorManager.SystemAccentColorDark1,
                "Light1 and Dark1 should differ");
        }

        [TestMethod]
        public void ThemeChange_UpdatesAdaptiveAccents()
        {
            var customColor = Color.FromRgb(0x00, 0x78, 0xD4);
            ApplicationAccentColorManager.ApplyCustomAccent(customColor);

            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
            var darkPrimary = ApplicationAccentColorManager.SystemAccentColorPrimary;

            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            var lightPrimary = ApplicationAccentColorManager.SystemAccentColorPrimary;

            Assert.AreNotEqual(darkPrimary, lightPrimary,
                "Primary accent should differ between Dark and Light themes");

            Assert.AreEqual(ApplicationAccentColorManager.SystemAccentColorLight2, darkPrimary,
                "Dark theme Primary should use Light2 variant");
            Assert.AreEqual(ApplicationAccentColorManager.SystemAccentColorDark1, lightPrimary,
                "Light theme Primary should use Dark1 variant");
        }
    }
}
