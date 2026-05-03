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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf;
using Fluent = Fluence.Wpf.Controls;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        private static void CloseWindowAndDrain(Window window)
        {
            window.Content = null;
            window.UpdateLayout();
            window.Close();
            DrainDispatcher(WpfTestSta.Dispatcher);
        }

        // Pump the dispatcher for `milliseconds` so any in-flight storyboard
        // (e.g. the LeftCompact pane's 167 ms Width animation) reaches its
        // HoldEnd state before the test samples layout values.
        private static void WaitForAnimationAndDrain(Dispatcher dispatcher, int milliseconds)
        {
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(milliseconds),
                DispatcherPriority.Normal,
                delegate { frame.Continue = false; },
                dispatcher);
            timer.Start();
            Dispatcher.PushFrame(frame);
            timer.Stop();
            dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        }

        private static bool WaitUntil(Dispatcher dispatcher, int milliseconds, Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(milliseconds);

            do
            {
                dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
                if (condition())
                {
                    return true;
                }

                var frame = new DispatcherFrame();
                var timer = new DispatcherTimer(
                    TimeSpan.FromMilliseconds(16),
                    DispatcherPriority.Normal,
                    delegate { frame.Continue = false; },
                    dispatcher);
                timer.Start();
                Dispatcher.PushFrame(frame);
                timer.Stop();
            }
            while (DateTime.UtcNow < deadline);

            dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
            return condition();
        }

        private static void AssertContentOffsetEventually(
            Window window,
            FrameworkElement nav,
            FrameworkElement presenter,
            double expectedOffset,
            string message)
        {
            WaitUntil(window.Dispatcher, 3000, delegate
            {
                window.UpdateLayout();
                return Math.Abs(GetContentOffsetX(nav, presenter) - expectedOffset) <= 1.0;
            });

            window.UpdateLayout();
            Assert.AreEqual(expectedOffset, GetContentOffsetX(nav, presenter), 1.0, message);
        }

        private static double GetContentOffsetX(FrameworkElement nav, FrameworkElement presenter)
        {
            return presenter.TransformToAncestor(nav).Transform(new Point(0, 0)).X;
        }

        private static bool WaitForSelectionIndicatorVerticalDepart(
            Dispatcher dispatcher,
            FrameworkElement indicator,
            TranslateTransform translate,
            double expectedX,
            double originalY,
            bool upward)
        {
            return WaitUntil(dispatcher, 250, delegate
            {
                var xIsUnchanged = Math.Abs(translate.X - expectedX) <= 0.5;
                var yMovedInExpectedDirection = upward ? translate.Y < originalY : translate.Y > originalY;
                return xIsUnchanged && yMovedInExpectedDirection && indicator.Opacity < 1.0;
            });
        }

        [TestMethod]
        public void NavigationView_PaneDisplayMode_Left_RendersVerticalPane()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Two" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var host = GetNavigationViewItemsHostPanel(nav);
                    Assert.IsNotNull(host);
                    Assert.AreEqual(Orientation.Vertical, host.Orientation);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_PaneDisplayMode_Top_RendersHorizontalPane()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Two" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var host = GetNavigationViewItemsHostPanel(nav);
                    Assert.IsNotNull(host);
                    Assert.AreEqual(Orientation.Horizontal, host.Orientation);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_PaneItemsScrollViewer_UsesFluentScrollViewerStyle()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);

                try
                {
                    AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode.Left, true);
                    AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode.LeftCompact, false);
                    AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode.Top, true);
                }
                finally
                {
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftCompact_ClosedPaneHidesFooter()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = false,
                        PaneFooter = new System.Windows.Controls.TextBlock { Text = "Footer" }
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var footerHost = FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneFooterHost");
                    Assert.IsNotNull(footerHost, "LeftCompact template should expose PaneFooterHost.");
                    Assert.AreEqual(Visibility.Collapsed, footerHost.Visibility,
                        "LeftCompact footer should be collapsed while the compact pane is closed.");

                    nav.IsPaneOpen = true;
                    WaitForAnimationAndDrain(window.Dispatcher, 220);

                    Assert.AreEqual(Visibility.Visible, footerHost.Visibility,
                        "LeftCompact footer should be visible when the pane opens.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftPaneToggleGlyph_IsOffsetToAlignWithItemGlyphs()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var glyph = FindVisualChildByName<FontIcon>(nav, "PaneToggleGlyph");
                    Assert.IsNotNull(glyph, "Left pane template should expose PaneToggleGlyph.");
                    Assert.AreEqual(2.0, glyph.Margin.Left, 0.01,
                        "Pane toggle glyph should be nudged right to align with navigation item glyphs.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationViewItem_Template_RendersInfoBadge()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var badge = new Fluent.FontIcon { Glyph = "\uE70D", IconFontSize = 12 };
                    var item = new Fluent.NavigationViewItem
                    {
                        Content = "Section",
                        Icon = new Fluent.FontIcon { Glyph = "\uE8FD", IconFontSize = 20 },
                        InfoBadge = badge
                    };

                    window.Content = item;
                    window.Width = 240;
                    window.Height = 80;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var presenter = FindVisualChildByName<ContentPresenter>(item, "InfoBadgePresenter");
                    Assert.IsNotNull(presenter, "NavigationViewItem template must render InfoBadge content.");
                    Assert.AreSame(badge, presenter.Content,
                        "NavigationViewItem InfoBadge presenter must bind to NavigationViewItem.InfoBadge.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_SelectedItem_UpdatesOnItemClick()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        SelectionFollowsFocus = false
                    };
                    var item0 = new Fluent.NavigationViewItem { Content = "Zero" };
                    var item1 = new Fluent.NavigationViewItem { Content = "One" };
                    nav.Items.Add(item0);
                    nav.Items.Add(item1);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(1, nav.SelectedIndex);
                    Assert.AreSame(item1, nav.SelectedItem, "SelectedItem should match the chosen NavigationViewItem.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_SelectionFollowsFocus_True_SelectsOnFocus()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        SelectionFollowsFocus = true
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Zero" });
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    var container1 = nav.ItemContainerGenerator.ContainerFromIndex(1) as FrameworkElement;
                    Assert.IsNotNull(container1);
                    Keyboard.Focus(container1);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(1, nav.SelectedIndex);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_SelectionFollowsFocus_False_DoesNotSelectOnFocus()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        SelectionFollowsFocus = false
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Zero" });
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    var container1 = nav.ItemContainerGenerator.ContainerFromIndex(1) as FrameworkElement;
                    Assert.IsNotNull(container1);
                    Keyboard.Focus(container1);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(0, nav.SelectedIndex);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_IsBackButtonVisible_False_HidesBackButton()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        IsBackButtonVisible = false,
                        IsBackEnabled = true
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.ApplyTemplate();
                    var back = nav.Template.FindName(Fluent.NavigationView.PartBackButton, nav) as System.Windows.Controls.Button;
                    Assert.IsNotNull(back);
                    Assert.AreEqual(Visibility.Collapsed, back.Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_IsBackEnabled_False_DisablesBackButton()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        IsBackButtonVisible = true,
                        IsBackEnabled = false
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.ApplyTemplate();
                    var back = nav.Template.FindName(Fluent.NavigationView.PartBackButton, nav) as System.Windows.Controls.Button;
                    Assert.IsNotNull(back);
                    Assert.IsFalse(back.IsEnabled);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_BackRequested_FiresOnBackClick()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        IsBackButtonVisible = true,
                        IsBackEnabled = true
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var fired = false;
                    EventHandler<NavigationViewBackRequestedEventArgs> handler = delegate
                    {
                        fired = true;
                    };
                    nav.BackRequested += handler;
                    nav.ApplyTemplate();
                    nav.RaiseBackRequestedForTesting();
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(fired);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_ThemeSwitch_UpdatesBrushes()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsTrue(application.Resources.MergedDictionaries.Count > 0);
                    var lightBase = (Color)application.Resources.MergedDictionaries[0]["SolidBackgroundFillColorBase"];

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    DrainDispatcher(window.Dispatcher);
                    var darkBase = (Color)application.Resources.MergedDictionaries[0]["SolidBackgroundFillColorBase"];

                    Assert.AreNotEqual(lightBase, darkBase,
                        "Theme color SolidBackgroundFillColorBase should differ between light and dark.");
                    nav.UpdateLayout();
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_SharedIndicator_ExistsInTemplate_AndVisibleWhenSelected()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    var item0 = new Fluent.NavigationViewItem { Content = "One" };
                    var item1 = new Fluent.NavigationViewItem { Content = "Two" };
                    nav.Items.Add(item0);
                    nav.Items.Add(item1);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.ApplyTemplate();
                    var indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    Assert.AreEqual(0.0, indicator.Opacity, 0.01, "Indicator should be hidden when nothing is selected.");

                    nav.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(1.0, indicator.Opacity, 0.01, "Indicator should be visible when an item is selected.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_PreTemplateSelection_PositionsSharedIndicatorAfterTemplateApplied()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    var item = new Fluent.NavigationViewItem
                    {
                        Content = "Home",
                        Icon = new Fluent.FontIcon { Glyph = "\uE80F", IconFontSize = 20 }
                    };
                    nav.Items.Add(item);
                    nav.SelectedItem = item;

                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    Assert.AreEqual(1.0, indicator.Opacity, 0.01,
                        "Selection made before template application should show the shared indicator after layout.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftMode_SharedIndicator_TracksHorizontalItemPlacement()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem
                    {
                        Content = "Home",
                        Icon = new Fluent.FontIcon { Glyph = "\uE80F", IconFontSize = 20 }
                    });
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Child", IsChildItem = true });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    var iconItemX = GetSelectionIndicatorTranslate(indicator).X;
                    Assert.AreEqual(4.0, iconItemX, 0.5,
                        "Icon item indicator should sit inside the selected item background.");

                    nav.SelectedIndex = 1;
                    WaitForAnimationAndDrain(window.Dispatcher, 600);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var childItemX = GetSelectionIndicatorTranslate(indicator).X;
                    Assert.AreEqual(48.0, childItemX, 0.5,
                        "Iconless child item indicator should move inward without overlapping the content column.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftMode_SharedIndicator_AnimatesBetweenSelections()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem
                    {
                        Content = "Home",
                        Icon = new Fluent.FontIcon { Glyph = "\uE80F", IconFontSize = 20 }
                    });
                    nav.Items.Add(new Fluent.NavigationViewItem
                    {
                        Content = "Settings",
                        Icon = new Fluent.FontIcon { Glyph = "\uE713", IconFontSize = 20 }
                    });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    var translate = GetSelectionIndicatorTranslate(indicator);
                    Assert.IsFalse(translate.HasAnimatedProperties,
                        "Initial selection should snap before later changes animate.");

                    nav.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(translate.HasAnimatedProperties,
                        "Changing selection should animate the shared indicator transform.");
                    WaitForAnimationAndDrain(window.Dispatcher, 600);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftMode_IndicatorExitsVerticallyBeforeChangingParentChildIndent()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem
                    {
                        Content = "Parent",
                        Icon = new Fluent.FontIcon { Glyph = "\uE80F", IconFontSize = 20 }
                    });
                    nav.Items.Add(new Fluent.NavigationViewItem
                    {
                        Content = "Child",
                        IsChildItem = true
                    });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    var translate = GetSelectionIndicatorTranslate(indicator);
                    var parentX = translate.X;
                    var parentY = translate.Y;

                    nav.SelectedIndex = 1;
                    Assert.IsTrue(
                        WaitForSelectionIndicatorVerticalDepart(window.Dispatcher, indicator, translate, parentX, parentY, false),
                        "The selection indicator should move vertically downward and fade out before it moves to the child item's inset X position.");

                    WaitForAnimationAndDrain(window.Dispatcher, 400);
                    Assert.AreEqual(48.0, translate.X, 0.5,
                        "After the depart/arrive animation completes, the child item indicator should sit at the child inset.");
                    Assert.AreEqual(1.0, indicator.Opacity, 0.01,
                        "After the depart/arrive animation completes, the indicator should be visible on the new item.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftMode_IndicatorExitsUpwardWhenNewSelectionIsAbove()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem
                    {
                        Content = "Parent",
                        Icon = new Fluent.FontIcon { Glyph = "\uE80F", IconFontSize = 20 }
                    });
                    nav.Items.Add(new Fluent.NavigationViewItem
                    {
                        Content = "Child",
                        IsChildItem = true
                    });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    var translate = GetSelectionIndicatorTranslate(indicator);
                    var childX = translate.X;
                    var childY = translate.Y;

                    nav.SelectedIndex = 0;
                    Assert.IsTrue(
                        WaitForSelectionIndicatorVerticalDepart(window.Dispatcher, indicator, translate, childX, childY, true),
                        "The selection indicator should move upward and fade out before it moves to the parent item's X position.");

                    WaitForAnimationAndDrain(window.Dispatcher, 400);
                    Assert.AreEqual(4.0, translate.X, 0.5,
                        "After the depart/arrive animation completes, the parent item indicator should sit at the parent inset.");
                    Assert.AreEqual(1.0, indicator.Opacity, 0.01,
                        "After the depart/arrive animation completes, the indicator should be visible on the new item.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftMode_TopLevelIconlessItem_DoesNotUseChildIndicatorIndent()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem
                    {
                        Content = "Home",
                        Icon = new Fluent.FontIcon { Glyph = "\uE80F", IconFontSize = 20 }
                    });
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "No icon top-level" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    var iconItemX = GetSelectionIndicatorTranslate(indicator).X;

                    nav.SelectedIndex = 1;
                    WaitForAnimationAndDrain(window.Dispatcher, 600);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var noIconItemX = GetSelectionIndicatorTranslate(indicator).X;
                    Assert.AreEqual(iconItemX, noIconItemX, 0.5,
                        "A top-level item without an icon should keep the top-level indicator position; child indentation must be explicit.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationViewItem_FocusVisual_StaysInsideItemBounds()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);

                try
                {
                    var style = application.TryFindResource("NavigationViewItemFocusVisual") as Style;
                    Assert.IsNotNull(style, "NavigationViewItemFocusVisual should be present in Generic.xaml.");

                    ControlTemplate template = null;
                    foreach (var setterBase in style.Setters)
                    {
                        var setter = setterBase as Setter;
                        if (setter != null && setter.Property == Control.TemplateProperty)
                        {
                            template = setter.Value as ControlTemplate;
                            break;
                        }
                    }

                    Assert.IsNotNull(template, "NavigationViewItemFocusVisual should provide a ControlTemplate.");

                    var root = template.LoadContent() as DependencyObject;
                    Assert.IsNotNull(root, "Focus visual template should load a visual tree.");

                    foreach (var border in FindVisualChildren<System.Windows.Controls.Border>(root))
                    {
                        Assert.IsTrue(border.Margin.Left >= 0.0 && border.Margin.Right >= 0.0,
                            "Navigation item focus strokes should stay inside the selected item bounds horizontally.");
                    }
                }
                finally
                {
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_SharedIndicator_HidesWhenSelectionCleared()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.AreEqual(1.0, indicator.Opacity, 0.01);

                    nav.SelectedItem = null;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(0.0, indicator.Opacity, 0.01, "Indicator should hide when selection is cleared.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_TopMode_SharedIndicator_VisibleWhenSelected()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 600,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Alpha" });
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Beta" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in Top pane template.");
                    Assert.AreEqual(1.0, indicator.Opacity, 0.01, "Indicator should be visible in top mode.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_FullThemeCycle_NoExceptions()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var themes = new ApplicationTheme[]
                    {
                        ApplicationTheme.Light,
                        ApplicationTheme.Dark,
                        ApplicationTheme.HighContrast,
                        ApplicationTheme.Auto
                    };

                    for (var i = 0; i < themes.Length; i++)
                    {
                        ApplicationThemeManager.Apply(themes[i], BackdropType.None, true);
                        DrainDispatcher(window.Dispatcher);
                        nav.UpdateLayout();
                    }
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_PaneModeSwitch_IndicatorSurvives()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 600,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Two" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "Indicator should exist after mode switch.");
                    Assert.AreEqual(1.0, indicator.Opacity, 0.01, "Indicator should remain visible after mode switch.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_PaneCollapse_IndicatorSurvives()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    nav.IsPaneOpen = false;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "Indicator should exist after pane collapse.");
                    Assert.AreEqual(1.0, indicator.Opacity, 0.01, "Indicator should remain visible after pane collapse.");

                    nav.IsPaneOpen = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(1.0, indicator.Opacity, 0.01, "Indicator should remain visible after pane re-expand.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationViewItem_DisabledState_ChangesForeground()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    var item = new Fluent.NavigationViewItem { Content = "Disabled" };
                    nav.Items.Add(item);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var enabledForeground = item.Foreground;

                    item.IsEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var disabledForeground = item.Foreground;
                    Assert.AreNotEqual(enabledForeground, disabledForeground,
                        "Foreground should change when item is disabled.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_Left_PaneClosedInitially_ContentStartsAt48px_Inline()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = false
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var presenter = FindVisualChildByName<ContentPresenter>(nav, Fluent.NavigationView.PartContentPresenter);
                    Assert.IsNotNull(presenter, "PART_ContentPresenter must exist in Left template.");

                    var offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.AreEqual(48.0, offset.X, 1.0,
                        "When Left mode starts with IsPaneOpen=false, content must start at the 48px compact rail, not at the expanded pane width.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_Left_ContentStarts48pxBelowWindowTop()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var presenter = FindVisualChildByName<ContentPresenter>(nav, Fluent.NavigationView.PartContentPresenter);
                    Assert.IsNotNull(presenter, "PART_ContentPresenter must exist in Left template.");

                    var offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.AreEqual(48.0, offset.Y, 1.0,
                        "Left NavigationView content should start 48px below the top of the window.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        // WI-1 F1: LeftCompact pane must resize inline and push sibling content. Never overlay.
        //
        // Regression guard: the original LeftCompactPaneTemplate drew the pane as an overlay
        // (Panel.ZIndex="1", Width triggered to 280), which caused the pane to cover the content
        // area rather than push it aside. We assert that the pane's visible width changes with
        // IsPaneOpen AND that the content host starts immediately to the right of the pane.
        [TestMethod]
        public void NavigationView_LeftCompact_PaneOpen_ContentStartsAt280px_Inline()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = true
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    // Pane-open enter animation is 167 ms (CubicEase EaseOut). Wait past HoldEnd.
                    WaitForAnimationAndDrain(window.Dispatcher, 300);
                    window.UpdateLayout();

                    var presenter = FindVisualChildByName<ContentPresenter>(nav, Fluent.NavigationView.PartContentPresenter);
                    Assert.IsNotNull(presenter, "PART_ContentPresenter must exist in LeftCompact template.");

                    AssertContentOffsetEventually(window, nav, presenter, 280.0,
                        "When IsPaneOpen=true in LeftCompact, content must start inline at pane width 280 (not overlap the pane).");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftCompact_PaneClosed_ContentStartsAt48px_Inline()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = false
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    var presenter = FindVisualChildByName<ContentPresenter>(nav, Fluent.NavigationView.PartContentPresenter);
                    Assert.IsNotNull(presenter, "PART_ContentPresenter must exist in LeftCompact template.");

                    AssertContentOffsetEventually(window, nav, presenter, 48.0,
                        "When IsPaneOpen=false in LeftCompact, content must start inline at pane width 48 (compact rail).");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftCompact_PaneToggle_ResizesPushingContent()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = true
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    // Pane enter animation is 167 ms (CubicEase). Wait past HoldEnd before sampling layout.
                    WaitForAnimationAndDrain(window.Dispatcher, 300);
                    window.UpdateLayout();

                    var presenter = FindVisualChildByName<ContentPresenter>(nav, Fluent.NavigationView.PartContentPresenter);
                    Assert.IsNotNull(presenter, "PART_ContentPresenter must exist in LeftCompact template.");

                    AssertContentOffsetEventually(window, nav, presenter, 280.0, "Open state: content begins at 280.");

                    nav.IsPaneOpen = false;
                    AssertContentOffsetEventually(window, nav, presenter, 48.0, "Closed state: content begins at 48 (push, not overlay).");

                    nav.IsPaneOpen = true;
                    AssertContentOffsetEventually(window, nav, presenter, 280.0, "Reopen state: content returns to 280.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        // NavigationView.ContentBackground must default to NavigationViewContentBackgroundBrush
        // (semi-transparent tint that allows Mica/Acrylic backdrop to show through the content area).
        [TestMethod]
        public void NavigationView_ContentBackground_DefaultStyle_ResolvesToSolidBackgroundFillColorBase()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var expected = application.TryFindResource("NavigationViewContentBackgroundBrush") as SolidColorBrush;
                    var actual = nav.ContentBackground as SolidColorBrush;

                    Assert.IsNotNull(expected, "NavigationViewContentBackgroundBrush must be present in merged resources.");
                    Assert.IsNotNull(actual, "NavigationView.ContentBackground must be a SolidColorBrush.");
                    Assert.AreEqual(expected.Color, actual.Color,
                        "Default ContentBackground must equal NavigationViewContentBackgroundBrush (semi-transparent Mica tint).");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        // WI-1 F3 supporting guard: NavigationViewItemHeader must be a first-class pane child
        // (placed via Items), styled distinctly from NavigationViewItem, and not selectable.
        [TestMethod]
        public void NavigationView_Header_InPane_IsRendered_NotSelectable()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    var header = new Fluent.NavigationViewItemHeader { Content = "Input" };
                    var item = new Fluent.NavigationViewItem { Content = "Buttons" };
                    nav.Items.Add(header);
                    nav.Items.Add(item);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var renderedHeader = FindVisualChild<Fluent.NavigationViewItemHeader>(nav);
                    Assert.IsNotNull(renderedHeader, "NavigationViewItemHeader must render inside the pane.");
                    Assert.IsFalse(renderedHeader.Focusable, "Header must not be focusable.");
                    Assert.IsNull(nav.SelectedItem, "Header must not be auto-selected even when placed at index 0.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        // ---------------------------------------------------------------------------
        // WI-3 B15  NavigationView pane header LayerFillColorAltBrush + BackButtonStates VSM
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void NavigationView_BackButtonStates_BothStatesAccessible()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView { Width = 700, Height = 500 };
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);

                    // WI-3 B15: BackButtonStates VSM group must expose both states
                    bool okVisible = VisualStateManager.GoToState(nav, "BackButtonVisible", false);
                    bool okCollapsed = VisualStateManager.GoToState(nav, "BackButtonCollapsed", false);

                    Assert.IsTrue(okVisible, "GoToState('BackButtonVisible') must succeed — BackButtonStates VSM group required.");
                    Assert.IsTrue(okCollapsed, "GoToState('BackButtonCollapsed') must succeed.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void NavigationView_IsBackButtonVisible_True_ShowsBackButton()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView { Width = 700, Height = 500, IsBackButtonVisible = true };
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);

                    var back = nav.Template.FindName(Fluent.NavigationView.PartBackButton, nav) as System.Windows.Controls.Button;
                    Assert.IsNotNull(back, "PART_BackButton must exist.");
                    Assert.AreEqual(Visibility.Visible, back.Visibility,
                        "PART_BackButton must be Visible when IsBackButtonVisible=True (WI-3 B15 VSM).");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        // NavigationView_CompactPane_BackgroundIsLayerFillColorAlt REMOVED (WI-3 B15 revert).
        // Replaced by NavigationView_PaneBorders_AreTransparent below.

        // NavigationView.ContentBackground must resolve to NavigationViewContentBackgroundBrush
        // across all themes (semi-transparent tint; color changes per theme file).
        [TestMethod]
        public void NavigationView_ContentBackground_ResolvesToSolidBackgroundFillColorBaseBrush_AcrossThemes()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 640,
                        Height = 400,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsNotNull(nav.ContentBackground,
                        "ContentBackground must resolve under Light theme.");
                    Assert.IsNotNull(application.TryFindResource("NavigationViewContentBackgroundBrush"),
                        "NavigationViewContentBackgroundBrush must resolve under Light theme.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsNotNull(nav.ContentBackground,
                        "ContentBackground must resolve under Dark theme.");
                    Assert.IsNotNull(application.TryFindResource("NavigationViewContentBackgroundBrush"),
                        "NavigationViewContentBackgroundBrush must resolve under Dark theme.");

                    ThemeTestHelpers.ApplyStandardThemeCycle();
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsNotNull(nav.ContentBackground,
                        "ContentBackground must resolve after a full theme cycle.");
                    Assert.IsNotNull(application.TryFindResource("NavigationViewContentBackgroundBrush"),
                        "NavigationViewContentBackgroundBrush must resolve after a full theme cycle.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        // NavigationView_Left_PaneBorder_UsesLayerFillColorAltBrush REMOVED (WI-3 B15 revert).
        // NavigationView_LeftCompact_PaneBorder_UsesLayerFillColorAltBrush REMOVED (WI-3 B15 revert).
        // Both replaced by NavigationView_PaneBorders_AreTransparent below.

        [TestMethod]
        public void NavigationView_PaneBorders_AreTransparent()
        {
            // Regression guard: pane borders (PaneBorder, CompactPane, PaneHeaderBorder) must
            // be Transparent (or null) so the DWM Mica/Acrylic backdrop shows through. The
            // WI-3 B15 commit wrongly set them to LayerFillColorAltBrush, which blocked the
            // backdrop entirely. This test asserts the reverted state is preserved.
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);

                // ---- Left pane ----
                var winLeft = new Window();
                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Item" });
                    winLeft.Content = nav;
                    winLeft.Show();
                    DrainDispatcher(winLeft.Dispatcher);
                    winLeft.UpdateLayout();

                    var paneBorder = FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneBorder");
                    Assert.IsNotNull(paneBorder, "Left pane must expose Border named 'PaneBorder'.");
                    AssertBrushIsTransparentOrNull(paneBorder.Background,
                        "PaneBorder.Background must be Transparent so DWM backdrop shows through.");
                }
                finally
                {
                    CloseWindowAndDrain(winLeft);
                }

                // ---- LeftCompact pane ----
                var winCompact = new Window();
                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Item" });
                    winCompact.Content = nav;
                    winCompact.Show();
                    DrainDispatcher(winCompact.Dispatcher);
                    winCompact.UpdateLayout();

                    var compactPane = FindVisualChildByName<System.Windows.Controls.Border>(nav, "CompactPane");
                    Assert.IsNotNull(compactPane, "LeftCompact pane must expose Border named 'CompactPane'.");
                    AssertBrushIsTransparentOrNull(compactPane.Background,
                        "CompactPane.Background must be Transparent so DWM backdrop shows through.");
                }
                finally
                {
                    CloseWindowAndDrain(winCompact);
                }

                // ---- Top pane ----
                var winTop = new Window();
                try
                {
                    var nav = new Fluent.NavigationView
                    {
                        Width = 600,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top
                    };
                    nav.Items.Add(new Fluent.NavigationViewItem { Content = "Item" });
                    winTop.Content = nav;
                    winTop.Show();
                    DrainDispatcher(winTop.Dispatcher);
                    winTop.UpdateLayout();

                    var paneHeader = FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneHeaderBorder");
                    Assert.IsNotNull(paneHeader, "Top pane must expose Border named 'PaneHeaderBorder'.");
                    AssertBrushIsTransparentOrNull(paneHeader.Background,
                        "PaneHeaderBorder.Background must be Transparent so DWM backdrop shows through.");
                }
                finally
                {
                    CloseWindowAndDrain(winTop);
                    if (genericDictionary != null)
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        /// <summary>
        /// Asserts that <paramref name="brush"/> is null, Brushes.Transparent, or a
        /// SolidColorBrush whose alpha channel is zero — i.e. effectively transparent.
        /// </summary>
        private static void AssertBrushIsTransparentOrNull(System.Windows.Media.Brush brush, string message)
        {
            if (brush == null)
                return; // null == no background == transparent

            if (brush == System.Windows.Media.Brushes.Transparent)
                return;

            var solid = brush as SolidColorBrush;
            if (solid != null && solid.Color.A == 0)
                return;

            Assert.Fail(message + " Actual: " + brush);
        }

        private static void AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode mode, bool isPaneOpen)
        {
            var application = EnsureApplication();
            var expected = application.TryFindResource("ScrollViewerStyle") as Style;
            Assert.IsNotNull(expected, "ScrollViewerStyle must be present in merged Fluence resources.");

            var window = new Window();
            try
            {
                var nav = new Fluent.NavigationView
                {
                    Width = 640,
                    Height = 420,
                    PaneDisplayMode = mode,
                    IsPaneOpen = isPaneOpen
                };
                nav.Items.Add(new Fluent.NavigationViewItem { Content = "Item" });

                window.Content = nav;
                window.Show();
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                var scrollViewer = FindVisualChildByName<ScrollViewer>(nav, Fluent.NavigationView.PartPaneItemsScrollViewer);
                Assert.IsNotNull(scrollViewer, "NavigationView template must expose PART_PaneItemsScrollViewer.");
                Assert.IsInstanceOfType(scrollViewer, typeof(Fluent.SmoothScrollViewer),
                    "NavigationView pane items should use SmoothScrollViewer so the pane scrollbar uses the Fluent scrolling surface.");
                Assert.AreSame(expected, scrollViewer.Style,
                    "NavigationView pane items ScrollViewer must use the Fluence ScrollViewerStyle.");
            }
            finally
            {
                CloseWindowAndDrain(window);
            }
        }

        private static TranslateTransform GetSelectionIndicatorTranslate(FrameworkElement indicator)
        {
            var group = indicator.RenderTransform as TransformGroup;
            Assert.IsNotNull(group, "Selection indicator must use a TransformGroup.");
            Assert.IsTrue(group.Children.Count >= 2, "Selection indicator TransformGroup must contain scale and translate transforms.");
            var translate = group.Children[1] as TranslateTransform;
            Assert.IsNotNull(translate, "Selection indicator transform index 1 must be a TranslateTransform.");
            return translate;
        }

        [TestMethod]
        public void NavigationViewItem_Template_HasNoInnerSelectionIndicator()
        {
            // Regression: per-item Border named "SelectionIndicator" was duplicating the
            // pane-level PART_SelectionIndicator (animated by NavigationView code-behind),
            // producing two visible accent pills on the selected item. The pane-level
            // indicator is canonical (WinUI 3) and is wired in NavigationView.cs; the
            // per-item one must NOT exist in the template.
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var item = new Fluent.NavigationViewItem
                    {
                        Content = "Item",
                        IsSelected = true
                    };
                    window.Content = item;
                    window.Width = 240;
                    window.Height = 80;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var inner = FindVisualChildByName<System.Windows.Controls.Border>(item, "SelectionIndicator");
                    Assert.IsNull(inner,
                        "NavigationViewItem template must not contain a per-item Border named 'SelectionIndicator'. " +
                        "The pane-level PART_SelectionIndicator owns the selection visual.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }
    }
}
