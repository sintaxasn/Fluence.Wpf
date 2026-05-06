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
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class ControlRenderingTests
    {
        private static void RunOnFreshStaThread(Action action)
        {
            Exception capturedException = null;
            WpfTestSta.Dispatcher.Invoke(new Action(delegate
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    capturedException = exception;
                }
            }));

            if (capturedException != null)
            {
                ExceptionDispatchInfo.Capture(capturedException).Throw();
            }
        }

        private static Application EnsureApplicationSta()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static void MergeThemeAndGeneric(Application app)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            app.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            var demoShared = new ResourceDictionary
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
            };
            app.Resources.MergedDictionaries.Add(demoShared);
        }

        private static void AssertCrispRenderingSetters(FrameworkElement element)
        {
            Assert.IsTrue(element.SnapsToDevicePixels, "SnapsToDevicePixels should be true from default style.");
            Assert.IsTrue(element.UseLayoutRounding, "UseLayoutRounding should be true from default style.");
            Assert.AreEqual(
                TextFormattingMode.Display,
                TextOptions.GetTextFormattingMode(element),
                "TextFormattingMode should be Display.");
            Assert.AreEqual(
                TextRenderingMode.ClearType,
                TextOptions.GetTextRenderingMode(element),
                "TextRenderingMode should be ClearType.");
        }

        [TestMethod]
        public void ThemedButton_HasCrispTextAndLayoutRoundingSetters()
        {
            RunOnFreshStaThread(delegate
            {
                var app = EnsureApplicationSta();
                MergeThemeAndGeneric(app);
                var button = new Fluent.Button();
                _ = new Window { Content = button };
                button.ApplyTemplate();
                AssertCrispRenderingSetters(button);
            });
        }

        [TestMethod]
        public void ThemedTextBox_HasCrispTextAndLayoutRoundingSetters()
        {
            RunOnFreshStaThread(delegate
            {
                var app = EnsureApplicationSta();
                MergeThemeAndGeneric(app);
                var textBox = new Fluent.TextBox();
                _ = new Window { Content = textBox };
                textBox.ApplyTemplate();
                AssertCrispRenderingSetters(textBox);
            });
        }

        [TestMethod]
        public void CrispRendering_PreservedAcrossThemeSwitches()
        {
            RunOnFreshStaThread(delegate
            {
                var app = EnsureApplicationSta();
                MergeThemeAndGeneric(app);

                foreach (var theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);

                    var checkBox = new Fluent.CheckBox();
                    _ = new Window { Content = checkBox };
                    checkBox.ApplyTemplate();
                    AssertCrispRenderingSetters(checkBox);
                }
            });
        }
    }
}
