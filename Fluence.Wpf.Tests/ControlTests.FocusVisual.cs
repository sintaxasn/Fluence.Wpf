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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 A1-A4 tests: per-control focus visual style dedup.
    /// Verifies Button, CheckBox, RadioButton, and ToggleButton all use the
    /// shared <c>DefaultControlFocusVisualStyle</c> resource rather than
    /// per-control duplicates.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 A1-A4  Focus visual dedup
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void FocusVisual_DefaultControlFocusVisualStyle_ResolvesInAllThemes()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    _ = MergeGenericDictionary(app);
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);

                    Style? style = app?.TryFindResource("DefaultControlFocusVisualStyle") as Style;
                    Assert.IsNotNull(style,
                        string.Format("DefaultControlFocusVisualStyle must resolve in theme: {0}", theme));
                }
            });
        }

        [TestMethod]
        public void FocusVisual_PerControlKeys_RemovedFromDictionary()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                // These per-control duplicate keys must no longer exist now that
                // all four controls reference DefaultControlFocusVisualStyle.
                Assert.IsNull(app?.TryFindResource("ButtonFocusVisual"),
                    "ButtonFocusVisual per-control key must be removed.");
                Assert.IsNull(app?.TryFindResource("CheckBoxFocusVisual"),
                    "CheckBoxFocusVisual per-control key must be removed.");
                Assert.IsNull(app?.TryFindResource("RadioButtonFocusVisual"),
                    "RadioButtonFocusVisual per-control key must be removed.");
                Assert.IsNull(app?.TryFindResource("ToggleButtonFocusVisual"),
                    "ToggleButtonFocusVisual per-control key must be removed.");
            });
        }

        [TestMethod]
        public void FocusVisual_Button_FocusVisualStyleIsSharedResource()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? sharedStyle = app?.TryFindResource("DefaultControlFocusVisualStyle") as Style;
                Assert.IsNotNull(sharedStyle, "DefaultControlFocusVisualStyle must resolve.");

                Button btn = new();
                Window w = new() { Content = btn, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreSame(sharedStyle, btn.FocusVisualStyle,
                    "Button.FocusVisualStyle must reference the shared DefaultControlFocusVisualStyle.");
                w.Close();
            });
        }

        [TestMethod]
        public void FocusVisual_CheckBox_FocusVisualStyleIsSharedResource()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? sharedStyle = app?.TryFindResource("DefaultControlFocusVisualStyle") as Style;
                Assert.IsNotNull(sharedStyle, "DefaultControlFocusVisualStyle must resolve.");

                CheckBox cb = new() { Content = "Test" };
                Window w = new() { Content = cb, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreSame(sharedStyle, cb.FocusVisualStyle,
                    "CheckBox.FocusVisualStyle must reference the shared DefaultControlFocusVisualStyle.");
                w.Close();
            });
        }

        [TestMethod]
        public void FocusVisual_RadioButton_FocusVisualStyleIsSharedResource()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? sharedStyle = app?.TryFindResource("DefaultControlFocusVisualStyle") as Style;
                Assert.IsNotNull(sharedStyle, "DefaultControlFocusVisualStyle must resolve.");

                RadioButton rb = new() { Content = "Option A" };
                Window w = new() { Content = rb, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreSame(sharedStyle, rb.FocusVisualStyle,
                    "RadioButton.FocusVisualStyle must reference the shared DefaultControlFocusVisualStyle.");
                w.Close();
            });
        }

        [TestMethod]
        public void FocusVisual_ToggleButton_FocusVisualStyleIsSharedResource()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? sharedStyle = app?.TryFindResource("DefaultControlFocusVisualStyle") as Style;
                Assert.IsNotNull(sharedStyle, "DefaultControlFocusVisualStyle must resolve.");

                ToggleButton tb = new() { Content = "Toggle" };
                Window w = new() { Content = tb, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreSame(sharedStyle, tb.FocusVisualStyle,
                    "ToggleButton.FocusVisualStyle must reference the shared DefaultControlFocusVisualStyle.");
                w.Close();
            });
        }
    }
}
