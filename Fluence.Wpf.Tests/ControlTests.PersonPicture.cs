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
using System.Windows.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf.Controls;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfGrid = System.Windows.Controls.Grid;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-6 tests: Fluent <see cref="PersonPicture"/>.
    /// Authority: WinUI 3 PersonPicture.xaml + PersonPicture_themeresources.xaml.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-6  PersonPicture
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void PersonPicture_DefaultStyle_Applies()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var pp = new PersonPicture();
                var w = new Window { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Background ellipse must be in visual tree
                var ellipse = FindVisualChild<Ellipse>(pp);
                Assert.IsNotNull(ellipse, "PersonPicture template must contain an Ellipse.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_TemplateParts_Present()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var pp = new PersonPicture();
                var w = new Window { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var initialsText = FindVisualChildByName<WpfTextBlock>(pp, "PART_InitialsText");
                Assert.IsNotNull(initialsText, "PART_InitialsText must be present.");

                var imageEllipse = FindVisualChildByName<Ellipse>(pp, "PART_ImageEllipse");
                Assert.IsNotNull(imageEllipse, "PART_ImageEllipse must be present.");

                var badgeGrid = FindVisualChildByName<WpfGrid>(pp, "PART_BadgeGrid");
                Assert.IsNotNull(badgeGrid, "PART_BadgeGrid must be present.");

                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_NoData_ShowsPlaceholderGlyph()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                // No DisplayName, no Initials, no ProfilePicture
                var pp = new PersonPicture();
                var w = new Window { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var initialsText = FindVisualChildByName<WpfTextBlock>(pp, "PART_InitialsText");
                Assert.IsNotNull(initialsText);
                // Contact glyph U+E77B
                Assert.AreEqual("\uE77B", initialsText.Text,
                    "PersonPicture with no data must show contact glyph U+E77B.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_DisplayName_GeneratesInitials()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var pp = new PersonPicture { DisplayName = "John Doe" };
                var w = new Window { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var initialsText = FindVisualChildByName<WpfTextBlock>(pp, "PART_InitialsText");
                Assert.IsNotNull(initialsText);
                Assert.AreEqual("JD", initialsText.Text,
                    "DisplayName='John Doe' must generate initials 'JD'.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_ExplicitInitials_Override()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var pp = new PersonPicture { DisplayName = "John Doe", Initials = "XY" };
                var w = new Window { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var initialsText = FindVisualChildByName<WpfTextBlock>(pp, "PART_InitialsText");
                Assert.IsNotNull(initialsText);
                Assert.AreEqual("XY", initialsText.Text,
                    "Explicit Initials='XY' must override DisplayName-derived initials.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_IsGroup_ShowsPeopleGlyph()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var pp = new PersonPicture { IsGroup = true };
                var w = new Window { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var initialsText = FindVisualChildByName<WpfTextBlock>(pp, "PART_InitialsText");
                Assert.IsNotNull(initialsText);
                Assert.AreEqual("\uE716", initialsText.Text,
                    "IsGroup=true must show people glyph U+E716 per WinUI 3 PersonPicture.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_BadgeNumber_MakesBadgeVisible()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var pp = new PersonPicture { BadgeNumber = 3 };
                var w = new Window { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var badgeGrid = FindVisualChildByName<WpfGrid>(pp, "PART_BadgeGrid");
                Assert.IsNotNull(badgeGrid);
                Assert.AreEqual(Visibility.Visible, badgeGrid.Visibility,
                    "BadgeNumber > 0 must make PART_BadgeGrid Visible.");

                var badgeText = FindVisualChildByName<WpfTextBlock>(pp, "PART_BadgeText");
                Assert.IsNotNull(badgeText);
                Assert.AreEqual("3", badgeText.Text,
                    "PART_BadgeText must display the BadgeNumber.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_NoBadge_BadgeCollapsed()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var pp = new PersonPicture { BadgeNumber = 0, BadgeGlyph = null };
                var w = new Window { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                var badgeGrid = FindVisualChildByName<WpfGrid>(pp, "PART_BadgeGrid");
                Assert.IsNotNull(badgeGrid);
                Assert.AreEqual(Visibility.Collapsed, badgeGrid.Visibility,
                    "PART_BadgeGrid must be Collapsed when BadgeNumber=0 and BadgeGlyph=null.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_DefaultSize_Is40x40()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var pp = new PersonPicture();
                var w = new Window { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreEqual(40.0, pp.Width,
                    "PersonPicture default Width must be 40 per WinUI 3 PersonPicture spec.");
                Assert.AreEqual(40.0, pp.Height,
                    "PersonPicture default Height must be 40 per WinUI 3 PersonPicture spec.");
                w.Close();
            });
        }

        [TestMethod]
        public void PersonPicture_ThemeCycle_StyleRemainsApplied()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var pp = new PersonPicture { DisplayName = "Alice Smith" };
                var w = new Window { Content = pp, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                var initialsText = FindVisualChildByName<WpfTextBlock>(pp, "PART_InitialsText");
                Assert.IsNotNull(initialsText,
                    "PART_InitialsText must still be present after theme cycle.");
                w.Close();
            });
        }
    }
}
