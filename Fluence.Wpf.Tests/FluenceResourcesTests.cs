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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Phase 4 tests for <see cref="FluenceResources"/>: the declarative
    /// <see cref="ResourceDictionary"/> that seeds all Fluence theme resources via
    /// <see cref="ISupportInitialize.EndInit"/> without requiring a code-behind
    /// <c>Apply</c> call.
    /// </summary>
    [TestClass]
    public class FluenceResourcesTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            WpfTestSta.Invoke(() =>
            {
                _ = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                Application.Current.Resources.MergedDictionaries.Clear();
            });
        }

        [TestMethod]
        public void FluenceResources_EndInit_SeedsCanonicalThemeBrushes_WithoutExplicitApply()
        {
            // Simulates what the XAML parser does when it encounters
            // <fluence:FluenceResources /> in App.xaml MergedDictionaries:
            // it constructs the object, calls BeginInit(), sets properties (none
            // here), then calls EndInit(). EndInit seals the ResourceDictionary
            // and calls ApplicationThemeManager.Apply(Auto). After that, all
            // canonical theme brushes must be resolvable from Application.Current.
            WpfTestSta.Invoke(() =>
            {
                Application? app = WpfTestSta.EnsureApplication();

                // Drive ISupportInitialize explicitly -- the same sequence the XAML parser uses.
                ISupportInitialize init = new FluenceResources();
                init.BeginInit();
                init.EndInit();

                // Canonical text brush -- present in every theme.
                object? textBrush = app?.TryFindResource("TextFillColorPrimaryBrush");
                Assert.IsNotNull(textBrush,
                    "TextFillColorPrimaryBrush must resolve after FluenceResources.EndInit() without any prior explicit Apply().");
                Assert.IsInstanceOfType(textBrush, typeof(SolidColorBrush),
                    "TextFillColorPrimaryBrush must be a SolidColorBrush.");

                // Canonical background brush.
                object? bgBrush = app?.TryFindResource("SolidBackgroundFillColorBaseBrush");
                Assert.IsNotNull(bgBrush,
                    "SolidBackgroundFillColorBaseBrush must resolve after FluenceResources.EndInit().");

                // Accent fill -- requires the accent pipeline.
                object? accentBrush = app?.TryFindResource("AccentFillColorDefaultBrush");
                Assert.IsNotNull(accentBrush,
                    "AccentFillColorDefaultBrush must resolve after FluenceResources.EndInit().");
            });
        }

        [TestMethod]
        public void FluenceResources_EndInit_IsIdempotent_SecondCallDoesNotThrow()
        {
            // A later explicit Apply call (e.g. to pin Mica or Dark theme) must coexist
            // with the declarative FluenceResources. Neither call should throw, and the
            // three-slot invariant must hold after both.
            WpfTestSta.Invoke(() =>
            {
                Application? app = WpfTestSta.EnsureApplication();

                ISupportInitialize init = new FluenceResources();
                init.BeginInit();
                init.EndInit();

                // Simulate what the demo's OnStartup does after FluenceResources has already run.
                ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica);

                object? textBrush = app?.TryFindResource("TextFillColorPrimaryBrush");
                Assert.IsNotNull(textBrush,
                    "TextFillColorPrimaryBrush must still resolve after a second Apply following FluenceResources.EndInit().");

                // The three-slot contract: [0] computed, [1] Typography, [2] Generic.
                Assert.AreEqual(3, app?.Resources.MergedDictionaries.Count,
                    "Exactly three merge slots must be present after FluenceResources + a second Apply; a foreign slot would corrupt DynamicResource resolution.");
            });
        }
    }
}
