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
using FluenceTextBox = Fluence.Wpf.Controls.TextBox;
using FluencePasswordBox = Fluence.Wpf.Controls.PasswordBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 C19 tests: TextBox PlaceholderText uses TextFillColorTertiaryBrush;
    /// PasswordBox PlaceholderText uses TextFillColorTertiaryBrush.
    /// Authority: WinUI 3 TextBox_themeresources.xaml (TextBoxPlaceholderTextForeground → TextFillColorTertiaryBrush).
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 C19  TextBox + PasswordBox PlaceholderText brush fix
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void TextBox_PlaceholderTextBlock_UsesTertiaryBrush()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var tb = new FluenceTextBox { PlaceholderText = "Search…", PlaceholderEnabled = true };
                var w = new Window { Content = tb, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var placeholder = FindVisualChildByName<WpfTextBlock>(tb, "PlaceholderTextBlock");
                Assert.IsNotNull(placeholder, "PlaceholderTextBlock must be present in TextBox template.");

                var expected = app.TryFindResource("TextFillColorTertiaryBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "TextFillColorTertiaryBrush resource must resolve.");

                var actual = placeholder.Foreground as SolidColorBrush;
                Assert.IsNotNull(actual, "PlaceholderTextBlock.Foreground must be a SolidColorBrush.");
                Assert.AreEqual(
                    expected.Color,
                    actual.Color,
                    "TextBox PlaceholderTextBlock.Foreground must be TextFillColorTertiaryBrush per WI-3 C19.");
                w.Close();
            });
        }

        [TestMethod]
        public void PasswordBox_PlaceholderTextBlock_UsesTertiaryBrush()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var pb = new FluencePasswordBox { PlaceholderText = "Password" };
                var w = new Window { Content = pb, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var placeholder = FindVisualChildByName<WpfTextBlock>(pb, "PlaceholderTextBlock");
                Assert.IsNotNull(placeholder, "PlaceholderTextBlock must be present in PasswordBox template.");

                var expected = app.TryFindResource("TextFillColorTertiaryBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "TextFillColorTertiaryBrush resource must resolve.");

                var actual = placeholder.Foreground as SolidColorBrush;
                Assert.IsNotNull(actual, "PlaceholderTextBlock.Foreground must be a SolidColorBrush.");
                Assert.AreEqual(
                    expected.Color,
                    actual.Color,
                    "PasswordBox PlaceholderTextBlock.Foreground must be TextFillColorTertiaryBrush per WI-3 C19.");
                w.Close();
            });
        }

        [TestMethod]
        public void TextBox_PlaceholderTextBlock_ThemeCycle_StillTertiaryBrush()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var tb = new FluenceTextBox { PlaceholderText = "Hint", PlaceholderEnabled = true };
                var w = new Window { Content = tb, Width = 300, Height = 60 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                var placeholder = FindVisualChildByName<WpfTextBlock>(tb, "PlaceholderTextBlock");
                Assert.IsNotNull(placeholder, "PlaceholderTextBlock must remain present after theme cycle.");

                var expected = app.TryFindResource("TextFillColorTertiaryBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "TextFillColorTertiaryBrush must resolve after theme cycle.");

                var actual = placeholder.Foreground as SolidColorBrush;
                Assert.IsNotNull(actual);
                Assert.AreEqual(
                    expected.Color,
                    actual.Color,
                    "PlaceholderTextBlock.Foreground must track TextFillColorTertiaryBrush after theme cycle.");
                w.Close();
            });
        }
    }
}
