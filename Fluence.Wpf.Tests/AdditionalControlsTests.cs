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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class AdditionalControlsTests
    {
        private static void Drain(Dispatcher d)
        {
            d.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static Application EnsureApp()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static void MergeGeneric(Application app)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            app.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            app.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
            });
        }

        [TestMethod]
        public void NumberBox_DefaultStyle_LoadsParts()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApp();
                MergeGeneric(app);
                var window = new Window();
                var numberBox = new Fluent.NumberBox { Width = 160, Value = 3 };
                try
                {
                    window.Content = numberBox;
                    window.Show();
                    Drain(window.Dispatcher);
                    numberBox.ApplyTemplate();
                    Assert.IsNotNull(numberBox.Template.FindName("PART_TextBox", numberBox));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void NumberBox_Value_Roundtrips()
        {
            WpfTestSta.Invoke(() =>
            {
                var box = new Fluent.NumberBox { Value = 42.5 };
                Assert.AreEqual(42.5, box.Value, 0.001);
            });
        }

        [TestMethod]
        public void Expander_CornerRadius_Default()
        {
            WpfTestSta.Invoke(() =>
            {
                var ex = new Fluent.Expander();
                Assert.AreEqual(new CornerRadius(4), ex.CornerRadius);
            });
        }

        [TestMethod]
        public void Expander_Template_Applies()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApp();
                MergeGeneric(app);
                var window = new Window();
                var ex = new Fluent.Expander { Header = "H", Content = new TextBlock { Text = "C" }, Width = 200 };
                try
                {
                    window.Content = ex;
                    window.Show();
                    Drain(window.Dispatcher);
                    ex.ApplyTemplate();
                    Assert.IsNotNull(ex.Template);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DropDownButton_Template_HasFlyoutPresenterName()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApp();
                MergeGeneric(app);
                var window = new Window();
                var btn = new Fluent.DropDownButton { Content = "Open", Width = 120, Flyout = new TextBlock { Text = "Flyout" } };
                try
                {
                    window.Content = btn;
                    window.Show();
                    Drain(window.Dispatcher);
                    btn.ApplyTemplate();
                    Assert.IsNotNull(btn.Template.FindName("PART_Popup", btn));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void InfoBadge_Value_Roundtrips()
        {
            WpfTestSta.Invoke(() =>
            {
                var badge = new Fluent.InfoBadge { Value = 9 };
                Assert.AreEqual(9, badge.Value);
            });
        }

        [TestMethod]
        public void InfoBadge_Template_Applies()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApp();
                MergeGeneric(app);
                var window = new Window();
                var badge = new Fluent.InfoBadge { Value = 2, Width = 32, Height = 32 };
                try
                {
                    window.Content = badge;
                    window.Show();
                    Drain(window.Dispatcher);
                    badge.ApplyTemplate();
                    Assert.IsNotNull(badge.Template);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ListBox_GetContainerForItemOverride_ReturnsFluentListBoxItem()
        {
            WpfTestSta.Invoke(() =>
            {
                var list = new Fluent.ListBox();
                var m = typeof(Fluent.ListBox).GetMethod(
                    "GetContainerForItemOverride",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(m);
                var container = m.Invoke(list, Array.Empty<object>());
                Assert.IsInstanceOfType(container, typeof(Fluent.ListBoxItem));
            });
        }
    }
}
