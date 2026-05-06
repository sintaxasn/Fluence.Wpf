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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void Button_AccentDisabled_DarkTheme_UsesVisibleDisabledAccentTokens()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                    AssertDisabledAccentButtonUsesDarkTokens();
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_AccentDisabled_DarkThemeWithoutAccentRefresh_UsesVisibleDisabledAccentTokens()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, false);

                    AssertDisabledAccentButtonUsesDarkTokens();
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_ExplicitToolTip_IsNotClearedByTruncationFallback()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                Fluent.ToolTip toolTip = new() { Content = "Save changes" };
                Fluent.Button button = new()
                {
                    Width = 160,
                    Content = "Save",
                    ToolTip = toolTip
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreSame(toolTip, button.ToolTip,
                        "Button truncation fallback must not clear an explicit tooltip supplied by the consumer.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        private static void AssertDisabledAccentButtonUsesDarkTokens()
        {
            Window window = new();
            Fluent.Button button = new()
            {
                Width = 100,
                Appearance = ControlAppearance.Accent,
                Content = "Add",
                IsEnabled = false
            };

            try
            {
                window.Content = button;
                window.Show();
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Border? restFill = button.Template.FindName("RestFill", button) as Border;

                Assert.IsNotNull(restFill, "Button template should expose RestFill.");
                Assert.IsInstanceOfType(restFill.Background, typeof(SolidColorBrush));
                Assert.IsInstanceOfType(button.Foreground, typeof(SolidColorBrush));
                Assert.AreEqual(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF), ((SolidColorBrush)restFill.Background).Color,
                    "Disabled Accent buttons in Dark theme must use the WinUI dark disabled accent fill so the disabled surface stays visible.");
                Assert.AreEqual(Color.FromArgb(0x87, 0xFF, 0xFF, 0xFF), ((SolidColorBrush)button.Foreground).Color,
                    "Disabled Accent button text in Dark theme must use the theme's disabled on-accent text token.");
            }
            finally
            {
                window.Close();
            }
        }
    }
}
