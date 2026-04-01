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
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class DictionaryStabilityTests
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
        public void RepeatedThemeSwitches_NoDictionaryAccumulation()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
            int baselineCount = _app.Resources.MergedDictionaries.Count;

            for (int i = 0; i < 10; i++)
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
            }

            int finalCount = _app.Resources.MergedDictionaries.Count;
            Assert.AreEqual(baselineCount, finalCount,
                string.Format("Dictionary count should remain at {0}, but was {1} after 20 switches",
                    baselineCount, finalCount));
        }

        [TestMethod]
        public void ThemeSlotIsReused()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
            int countAfterFirst = _app.Resources.MergedDictionaries.Count;

            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
            int countAfterSecond = _app.Resources.MergedDictionaries.Count;

            Assert.AreEqual(countAfterFirst, countAfterSecond,
                "Theme dictionary slot should be reused, not added");
        }

        [TestMethod]
        public void AllThemeVariants_SameSlotCount()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
            int lightCount = _app.Resources.MergedDictionaries.Count;

            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
            int darkCount = _app.Resources.MergedDictionaries.Count;

            ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, false);
            int hcCount = _app.Resources.MergedDictionaries.Count;

            Assert.AreEqual(lightCount, darkCount, "Light and Dark should use same slot count");
            Assert.AreEqual(darkCount, hcCount, "Dark and HighContrast should use same slot count");
        }

        [TestMethod]
        public void FirstApply_Loads5Dictionaries()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);

            Assert.AreEqual(5, _app.Resources.MergedDictionaries.Count,
                "Initial Apply should load exactly 5 dictionaries (Colors, Accent, Brushes, Typography, Generic).");
        }

        [TestMethod]
        public void ThemeSwap_OnlyReplacesIndex0And2()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
            var dictionaries = _app.Resources.MergedDictionaries;
            var accentRef = dictionaries[1];
            var typographyRef = dictionaries[3];
            var genericRef = dictionaries[4];

            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);

            Assert.AreSame(accentRef, dictionaries[1], "Accent dictionary at [1] should not be replaced on theme swap.");
            Assert.AreSame(typographyRef, dictionaries[3], "Typography dictionary at [3] should not be replaced on theme swap.");
            Assert.AreSame(genericRef, dictionaries[4], "Generic dictionary at [4] should not be replaced on theme swap.");
        }

        [TestMethod]
        public void BrushesDict_ReloadedOnNonHcSwap()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
            var brushesRef = _app.Resources.MergedDictionaries[2];

            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);
            Assert.AreNotSame(brushesRef, _app.Resources.MergedDictionaries[2],
                "Brushes dictionary should be reloaded after Light->Dark to pick up new Colors.");

            var brushesAfterDark = _app.Resources.MergedDictionaries[2];
            ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, false);
            Assert.AreSame(brushesAfterDark, _app.Resources.MergedDictionaries[2],
                "Brushes dictionary should NOT be reloaded for HighContrast (HC promotes its own brushes).");
        }

        [TestMethod]
        public void AccentUpdate_DoesNotChangeDictionaryCount()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            int countBefore = _app.Resources.MergedDictionaries.Count;

            ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
            int countAfter = _app.Resources.MergedDictionaries.Count;

            Assert.AreEqual(countBefore, countAfter,
                "Applying a custom accent should not change the merged dictionary count.");
        }

        [TestMethod]
        public void AllBrushKeys_Resolve_AfterLightDarkHcCycle()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
            ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, true);

            var keyBrushNames = new[]
            {
                "TextFillColorPrimaryBrush",
                "AccentFillColorDefaultBrush",
                "SubtleFillColorSecondaryBrush",
                "ControlStrokeColorDefaultBrush",
                "CardBackgroundFillColorDefaultBrush"
            };

            foreach (var key in keyBrushNames)
            {
                var brush = _app.Resources[key] as Brush;
                Assert.IsNotNull(brush, string.Format("Brush key '{0}' should resolve to non-null after Light->Dark->HC cycle.", key));
            }
        }

        [TestMethod]
        public void InitialApply_LoadsColorsAccentBrushesTypographyGeneric_InOrder()
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, false);
            var dictionaries = _app.Resources.MergedDictionaries;

            Assert.AreEqual(5, dictionaries.Count);

            var expectedFragments = new[]
            {
                "Theme.Light",
                "Accent",
                "Brushes",
                "Typography",
                "Generic"
            };

            for (int i = 0; i < expectedFragments.Length; i++)
            {
                var source = dictionaries[i].Source;
                Assert.IsNotNull(source, string.Format("Dictionary at [{0}] should have a Source URI.", i));
                Assert.IsTrue(source.OriginalString.IndexOf(expectedFragments[i], StringComparison.OrdinalIgnoreCase) >= 0,
                    string.Format("Dictionary at [{0}] Source should contain '{1}', but was '{2}'.",
                        i, expectedFragments[i], source.OriginalString));
            }
        }
    }
}
