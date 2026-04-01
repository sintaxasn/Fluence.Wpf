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
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class ThemeManagerTests
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
        public void Apply_Light_TextBrushIsDarkOnLight()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);

            var textColor = _app.Resources["TextFillColorPrimary"] as Color?;
            Assert.IsNotNull(textColor, "TextFillColorPrimary should be defined");

            Assert.AreEqual((byte)0xE4, textColor.Value.A, "Alpha should be 0xE4");
            Assert.AreEqual((byte)0x00, textColor.Value.R, "Red should be 0x00");
            Assert.AreEqual((byte)0x00, textColor.Value.G, "Green should be 0x00");
            Assert.AreEqual((byte)0x00, textColor.Value.B, "Blue should be 0x00");
        }

        [TestMethod]
        public void Apply_Dark_TextBrushIsLightOnDark()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);

            var textColor = _app.Resources["TextFillColorPrimary"] as Color?;
            Assert.IsNotNull(textColor, "TextFillColorPrimary should be defined");

            Assert.AreEqual((byte)0xFF, textColor.Value.A, "Alpha should be 0xFF");
            Assert.AreEqual((byte)0xFF, textColor.Value.R, "Red should be 0xFF");
            Assert.AreEqual((byte)0xFF, textColor.Value.G, "Green should be 0xFF");
            Assert.AreEqual((byte)0xFF, textColor.Value.B, "Blue should be 0xFF");
        }

        [TestMethod]
        public void Apply_HighContrast_UsesSystemColors()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, false);

            var brush = _app.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
            Assert.IsNotNull(brush, "TextFillColorPrimaryBrush should be defined");
        }

        [TestMethod]
        public void Apply_FiresChangedExactlyOnce()
        {
            int eventCount = 0;
            EventHandler<ThemeChangedEventArgs> handler = (s, e) => eventCount++;

            ApplicationThemeManager.Changed += handler;
            try
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
                Assert.AreEqual(1, eventCount, "Changed event should fire exactly once");
            }
            finally
            {
                ApplicationThemeManager.Changed -= handler;
            }
        }

        [TestMethod]
        public void TwoRapidApplies_FiresExactlyTwoEvents()
        {
            int eventCount = 0;
            EventHandler<ThemeChangedEventArgs> handler = (s, e) => eventCount++;

            ApplicationThemeManager.Changed += handler;
            try
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
                Assert.AreEqual(2, eventCount, "Changed event should fire exactly twice");
            }
            finally
            {
                ApplicationThemeManager.Changed -= handler;
            }
        }

        [TestMethod]
        public void FiveSwitches_DictionaryCountStable()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
            int initialCount = _app.Resources.MergedDictionaries.Count;

            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);

            int finalCount = _app.Resources.MergedDictionaries.Count;
            Assert.AreEqual(initialCount, finalCount, "Dictionary count should remain stable after multiple switches");
        }
    }
}
