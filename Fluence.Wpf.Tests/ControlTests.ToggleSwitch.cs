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
using System.Windows.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 B12 tests: ToggleSwitch knob easing (SplineDoubleKeyFrame / ControlFastOutSlowIn).
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 B12  ToggleSwitch knob easing
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ToggleSwitch_StyleApplies_SwitchThumbFound()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new();
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Ellipse? thumb = FindVisualChildByName<Ellipse>(ts, "SwitchThumb");
                Assert.IsNotNull(thumb, "SwitchThumb Ellipse must exist in ToggleSwitch template.");
                w.Close();
            });
        }

        [TestMethod]
        public void ToggleSwitch_DefaultState_ThumbWidth12()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Ellipse? thumb = FindVisualChildByName<Ellipse>(ts, "SwitchThumb");
                Assert.IsNotNull(thumb, "SwitchThumb must exist.");
                Assert.AreEqual(12.0, thumb.Width, 0.001,
                    "Default knob Width must be 12 (WinUI ToggleSwitch_themeresources.xaml SwitchKnobOff normal state).");
                Assert.AreEqual(12.0, thumb.Height, 0.001,
                    "Default knob Height must be 12.");
                w.Close();
            });
        }

        [TestMethod]
        public void ToggleSwitch_Checked_ThumbTranslateIs20()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new() { IsChecked = true };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Ellipse? thumb = FindVisualChildByName<Ellipse>(ts, "SwitchThumb");
                Assert.IsNotNull(thumb, "SwitchThumb must exist.");

                TranslateTransform? tx = thumb.RenderTransform as TranslateTransform;
                Assert.IsNotNull(tx,
                    "SwitchThumb RenderTransform must be TranslateTransform when IsChecked=True.");
                Assert.AreEqual(20.0, tx.X, 0.5,
                    "Knob X translate must be ~20 when IsChecked=True (WinUI ToggleSwitch_themeresources.xaml checked state).");
                w.Close();
            });
        }

        [TestMethod]
        public void ToggleSwitch_Unchecked_ThumbTranslateIsZero()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Ellipse? thumb = FindVisualChildByName<Ellipse>(ts, "SwitchThumb");
                Assert.IsNotNull(thumb, "SwitchThumb must exist.");

                if (thumb.RenderTransform is TranslateTransform tx)
                {
                    Assert.AreEqual(0.0, tx.X, 0.5,
                        "Knob X translate must be 0 when IsChecked=False.");
                }
                w.Close();
            });
        }
    }
}
