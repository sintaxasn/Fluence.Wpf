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
using FluenceProgressBar = Fluence.Wpf.Controls.ProgressBar;
using WpfBorder = System.Windows.Controls.Border;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void ProgressBar_PausedMode_UsesCautionBrush()
        {
            AssertProgressBarModeBrush(ProgressBarMode.Paused, "SystemFillColorCautionBrush");
        }

        [TestMethod]
        public void ProgressBar_ErrorMode_UsesCriticalBrush()
        {
            AssertProgressBarModeBrush(ProgressBarMode.Error, "SystemFillColorCriticalBrush");
        }

        private static void AssertProgressBarModeBrush(ProgressBarMode mode, string brushKey)
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 50,
                    ProgressMode = mode
                };
                Window w = new() { Content = progressBar, Width = 300, Height = 120 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                WpfBorder? fill = FindVisualChildByName<WpfBorder>(progressBar, "PART_Fill");
                Assert.IsNotNull(fill, "ProgressBar template must expose PART_Fill.");

                SolidColorBrush? expected = app?.TryFindResource(brushKey) as SolidColorBrush;
                Assert.IsNotNull(expected, brushKey + " must resolve.");

                SolidColorBrush? actual = fill.Background as SolidColorBrush;
                Assert.IsNotNull(actual, "PART_Fill.Background should be a SolidColorBrush.");
                Assert.AreEqual(expected.Color, actual.Color, "ProgressBar fill should use the requested state brush.");

                w.Close();
            });
        }
    }
}
