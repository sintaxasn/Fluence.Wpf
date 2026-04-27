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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the rewritten <see cref="ProgressRing"/> — XAML-driven 5-dot orbit
    /// indeterminate animation (WinUI canonical) plus code-driven determinate arc.
    /// </summary>
    public partial class ControlTests
    {
        // ──────────────────────────────────────────────────────────────────────
        // Default values + template part
        // ──────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void ProgressRing_Defaults_AreCanonical()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var ring = new ProgressRing();
                Assert.IsTrue(ring.IsActive, "Default IsActive must be true.");
                Assert.IsTrue(ring.IsIndeterminate, "Default IsIndeterminate must be true.");
                Assert.AreEqual(0.0, ring.Value, "Default Value must be 0.");
                Assert.AreEqual(0.0, ring.Minimum, "Default Minimum must be 0.");
                Assert.AreEqual(100.0, ring.Maximum, "Default Maximum must be 100.");
                Assert.AreEqual(4.0, ring.StrokeThickness, "Default StrokeThickness must be 4.");
            });
        }

        [TestMethod]
        public void ProgressRing_Template_ContainsDeterminateArcPart()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var ring = new ProgressRing();
                var w = new Window { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var arc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                Assert.IsNotNull(arc, "ProgressRing template must contain PART_DeterminateArc.");
                w.Close();
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // Indeterminate template — five orbit dots with rotate transforms
        // ──────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void ProgressRing_Indeterminate_TemplateContainsFiveOrbitDots()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var ring = new ProgressRing { IsIndeterminate = true, IsActive = true };
                var w = new Window { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                for (int i = 1; i <= 5; i++)
                {
                    var dot = FindVisualChildByName<Ellipse>(ring, "E" + i);
                    Assert.IsNotNull(dot, "ProgressRing template must contain orbit-dot Ellipse E" + i + ".");
                }

                w.Close();
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // Template settings — diameter + offset match WinUI ProgressRingTemplateSettings
        // diameter = (width × 0.1) + (width ≤ 40 ? 1 : 0)
        // anchor   = (width × 0.5) − diameter
        // ──────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void ProgressRing_TemplateSettings_AtWidth32_MatchWinUiFormula()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var ring = new ProgressRing { Width = 32, Height = 32 };
                var w = new Window { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // 32 × 0.1 + 1 = 4.2 ;  32 × 0.5 − 4.2 = 11.8
                Assert.AreEqual(4.2, ring.EllipseDiameter, 0.001,
                    "EllipseDiameter at Width=32 must be 4.2 ((32×0.1)+1).");
                Assert.AreEqual(11.8, ring.EllipseOffset.Top, 0.001,
                    "EllipseOffset.Top at Width=32 must be 11.8 ((32×0.5)−4.2).");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_TemplateSettings_AtWidth64_DropAdditiveTerm()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var ring = new ProgressRing { Width = 64, Height = 64 };
                var w = new Window { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // 64 × 0.1 + 0 = 6.4 ;  64 × 0.5 − 6.4 = 25.6
                Assert.AreEqual(6.4, ring.EllipseDiameter, 0.001,
                    "EllipseDiameter at Width=64 must be 6.4 (no +1 additive when width > 40).");
                Assert.AreEqual(25.6, ring.EllipseOffset.Top, 0.001,
                    "EllipseOffset.Top at Width=64 must be 25.6.");

                w.Close();
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // Determinate arc geometry
        // ──────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void ProgressRing_Determinate_PathDataIsPopulatedForNonZeroValue()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var ring = new ProgressRing
                {
                    IsIndeterminate = false,
                    Width = 64,
                    Height = 64,
                    Value = 50,
                    Minimum = 0,
                    Maximum = 100
                };
                var w = new Window { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var arc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                Assert.IsNotNull(arc, "PART_DeterminateArc must exist.");
                Assert.IsNotNull(arc.Data,
                    "Determinate arc Path.Data must be populated when Value > 0 and IsIndeterminate=false.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_Determinate_PathDataIsNullWhenValueIsZero()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var ring = new ProgressRing
                {
                    IsIndeterminate = false,
                    Width = 64,
                    Height = 64,
                    Value = 0,
                    Minimum = 0,
                    Maximum = 100
                };
                var w = new Window { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var arc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                Assert.IsNotNull(arc, "PART_DeterminateArc must exist.");
                Assert.IsNull(arc.Data,
                    "Determinate arc Path.Data must be null when Value=0 (no arc to draw).");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_SwitchToIndeterminate_ClearsArcGeometry()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var ring = new ProgressRing
                {
                    IsIndeterminate = false,
                    Width = 64,
                    Height = 64,
                    Value = 75
                };
                var w = new Window { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var arc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                Assert.IsNotNull(arc.Data, "Pre-condition: arc has geometry in determinate mode.");

                ring.IsIndeterminate = true;
                DrainDispatcher(w.Dispatcher);

                Assert.IsNull(arc.Data,
                    "Switching to indeterminate must clear the determinate arc geometry.");

                w.Close();
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // Foreground brush honours theme tokens
        // ──────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void ProgressRing_Foreground_ResolvesToAccentFillColorDefaultBrush()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var ring = new ProgressRing();
                var w = new Window { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var fg = ring.Foreground as SolidColorBrush;
                var expected = app.TryFindResource("AccentFillColorDefaultBrush") as SolidColorBrush;

                Assert.IsNotNull(expected, "AccentFillColorDefaultBrush must resolve.");
                Assert.IsNotNull(fg, "ProgressRing.Foreground must be a SolidColorBrush.");
                Assert.AreEqual(expected.Color, fg.Color,
                    "ProgressRing.Foreground must default to AccentFillColorDefaultBrush.");

                w.Close();
            });
        }

        [TestMethod]
        public void ProgressRing_ThemeCycle_TemplateRemainsApplied()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var ring = new ProgressRing();
                var w = new Window { Content = ring, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                var arc = FindVisualChildByName<Path>(ring, "PART_DeterminateArc");
                Assert.IsNotNull(arc, "PART_DeterminateArc must still exist after theme cycle.");

                w.Close();
            });
        }
    }
}
