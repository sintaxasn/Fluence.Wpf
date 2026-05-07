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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            DispatcherFrame frame = new();
            DispatcherTimer timer = new(
                TimeSpan.FromMilliseconds(milliseconds),
                DispatcherPriority.Normal,
                delegate { frame.Continue = false; },
                dispatcher);
            timer.Start();
            Dispatcher.PushFrame(frame);
            timer.Stop();
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        }

        private static bool WaitUntil(Dispatcher dispatcher, int milliseconds, Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(milliseconds);

            do
            {
                _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
                if (condition())
                {
                    return true;
                }

                DispatcherFrame frame = new();
                DispatcherTimer timer = new(
                    TimeSpan.FromMilliseconds(16),
                    DispatcherPriority.Normal,
                    delegate { frame.Continue = false; },
                    dispatcher);
                timer.Start();
                Dispatcher.PushFrame(frame);
                timer.Stop();
            }
            while (DateTime.UtcNow < deadline);

            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
            return condition();
        }

        private static void AssertContentOffsetEventually(
            Window window,
            FrameworkElement nav,
            FrameworkElement presenter,
            double expectedOffset,
            string message)
        {
            _ = WaitUntil(window.Dispatcher, 3000, delegate
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
                bool xIsUnchanged = Math.Abs(translate.X - expectedX) <= 0.5;
                bool yMovedInExpectedDirection = upward ? translate.Y < originalY : translate.Y > originalY;
                return xIsUnchanged && yMovedInExpectedDirection && indicator.Opacity < 1.0;
            });
        }

        [TestMethod]
        public void NavigationView_PaneDisplayMode_Left_RendersVerticalPane()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Two" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.StackPanel? host = GetNavigationViewItemsHostPanel(nav);
                    Assert.IsNotNull(host);
                    Assert.AreEqual(Orientation.Vertical, host.Orientation);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_PaneDisplayMode_Top_RendersHorizontalPane()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Two" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.StackPanel? host = GetNavigationViewItemsHostPanel(nav);
                    Assert.IsNotNull(host);
                    Assert.AreEqual(Orientation.Horizontal, host.Orientation);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_PaneItemsScrollViewer_UsesFluentScrollViewerStyle()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode.Left, true);
                    AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode.LeftCompact, false);
                    AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode.Top, true);
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftCompact_ClosedPaneHidesFooter()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = false,
                        PaneFooter = new System.Windows.Controls.TextBlock { Text = "Footer" }
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Border? footerHost = FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneFooterHost");
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftPaneToggleGlyph_IsOffsetToAlignWithItemGlyphs()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FontIcon? glyph = FindVisualChildByName<FontIcon>(nav, "PaneToggleGlyph");
                    Assert.IsNotNull(glyph, "Left pane template should expose PaneToggleGlyph.");
                    Assert.AreEqual(2.0, glyph.Margin.Left, 0.01,
                        "Pane toggle glyph should be nudged right to align with navigation item glyphs.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftChrome_BackPrecedesPaneToggle()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsBackButtonVisible = true,
                        IsBackEnabled = true
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Button? back = nav.Template.FindName(NavigationView.PartBackButton, nav) as System.Windows.Controls.Button;
                    System.Windows.Controls.Button? paneToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as System.Windows.Controls.Button;
                    Assert.IsNotNull(back, "PART_BackButton must exist in Left template.");
                    Assert.IsNotNull(paneToggle, "PART_PaneToggleButton must exist in Left template.");

                    System.Windows.Controls.StackPanel? chrome = FindVisualChildByName<System.Windows.Controls.StackPanel>(nav, "PaneChrome");
                    Assert.IsNotNull(chrome, "Left template should expose the pane chrome host.");
                    Assert.AreEqual(Orientation.Horizontal, chrome.Orientation,
                        "Left pane chrome should arrange back and pane toggle in a horizontal row.");
                    Assert.AreEqual(48.0, back.ActualWidth, 0.5,
                        "Back button should occupy one 48px navigation rail slot.");
                    Assert.AreEqual(48.0, paneToggle.ActualWidth, 0.5,
                        "Pane toggle should occupy one 48px navigation rail slot.");

                    Point backPoint = back.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Point paneTogglePoint = paneToggle.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.IsTrue(backPoint.X < paneTogglePoint.X, "Back button should be the first glyph, before the pane toggle.");
                    Assert.AreEqual(backPoint.Y, paneTogglePoint.Y, 0.5,
                        "Back button and pane toggle should share the same pane chrome row.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftMode_DefaultFontIconSizeIs20()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    FontIcon icon = new() { Glyph = "\uE80F" };
                    NavigationView nav = new()
                    {
                        Width = 420,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One", Icon = icon });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(20.0, icon.IconFontSize, 0.01,
                        "NavigationView left-mode FontIcon content should default to 20 px.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationViewItem_Template_RendersInfoBadge()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    FontIcon badge = new() { Glyph = "\uE70D", IconFontSize = 12 };
                    NavigationViewItem item = new()
                    {
                        Content = "Section",
                        Icon = new FontIcon { Glyph = "\uE8FD", IconFontSize = 20 },
                        InfoBadge = badge
                    };

                    window.Content = item;
                    window.Width = 240;
                    window.Height = 80;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter? presenter = FindVisualChildByName<ContentPresenter>(item, "InfoBadgePresenter");
                    Assert.IsNotNull(presenter, "NavigationViewItem template must render InfoBadge content.");
                    Assert.AreSame(badge, presenter.Content,
                        "NavigationViewItem InfoBadge presenter must bind to NavigationViewItem.InfoBadge.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_SelectedItem_UpdatesOnItemClick()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        SelectionFollowsFocus = false
                    };
                    NavigationViewItem item0 = new() { Content = "Zero" };
                    NavigationViewItem item1 = new() { Content = "One" };
                    _ = nav.Items.Add(item0);
                    _ = nav.Items.Add(item1);
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_ItemInvoked_FiresBeforeSelectionChanges()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    NavigationViewItem item0 = new() { Content = "Zero" };
                    NavigationViewItem item1 = new() { Content = "One" };
                    _ = nav.Items.Add(item0);
                    _ = nav.Items.Add(item1);
                    nav.SelectedItem = item0;
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    List<string> calls = [];
                    NavigationViewItemInvokedEventArgs? invokedArgs = null;
                    nav.ItemInvoked += delegate (object? sender, NavigationViewItemInvokedEventArgs e)
                    {
                        invokedArgs = e;
                        calls.Add("invoked:" + e.InvokedItemContainer.Content);
                    };
                    nav.SelectionChanged += delegate
                    {
                        calls.Add("selection:" + ((NavigationViewItem)nav.SelectedItem).Content);
                    };

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(item1);
                    Assert.IsNotNull(peer, "NavigationViewItem should create an automation peer.");
                    IInvokeProvider? invokeProvider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    Assert.IsNotNull(invokeProvider, "NavigationViewItem automation peer must expose Invoke.");

                    invokeProvider.Invoke();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsNotNull(invokedArgs, "ItemInvoked should fire when a navigation item is invoked.");
                    Assert.AreSame(item1, invokedArgs.InvokedItemContainer,
                        "ItemInvoked should expose the invoked NavigationViewItem container.");
                    Assert.AreSame(item1, invokedArgs.InvokedItem,
                        "Inline NavigationViewItem invocation should report the item itself as InvokedItem.");
                    Assert.IsFalse(invokedArgs.IsSettingsInvoked,
                        "Regular pane item invocation should not be reported as settings invocation.");
                    CollectionAssert.AreEqual(new[] { "invoked:One", "selection:One" }, calls,
                        "ItemInvoked must be raised before SelectionChanged, matching WinUI NavigationView ordering.");
                    Assert.AreSame(item1, nav.SelectedItem,
                        "Invoking the item should select it after ItemInvoked has been raised.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_SelectionFollowsFocus_True_SelectsOnFocus()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        SelectionFollowsFocus = true
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Zero" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    FrameworkElement? container1 = nav.ItemContainerGenerator.ContainerFromIndex(1) as FrameworkElement;
                    Assert.IsNotNull(container1);
                    _ = Keyboard.Focus(container1);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(1, nav.SelectedIndex);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_SelectionFollowsFocus_False_DoesNotSelectOnFocus()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        SelectionFollowsFocus = false
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Zero" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    FrameworkElement? container1 = nav.ItemContainerGenerator.ContainerFromIndex(1) as FrameworkElement;
                    Assert.IsNotNull(container1);
                    _ = Keyboard.Focus(container1);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(0, nav.SelectedIndex);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_IsBackButtonVisible_False_HidesBackButton()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        IsBackButtonVisible = false,
                        IsBackEnabled = true
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();
                    System.Windows.Controls.Button? back = nav.Template.FindName(NavigationView.PartBackButton, nav) as System.Windows.Controls.Button;
                    Assert.IsNotNull(back);
                    Assert.AreEqual(Visibility.Collapsed, back.Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_IsBackEnabled_False_CollapsesBackButton()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        IsBackButtonVisible = true,
                        IsBackEnabled = false
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();
                    System.Windows.Controls.Button? back = nav.Template.FindName(NavigationView.PartBackButton, nav) as System.Windows.Controls.Button;
                    System.Windows.Controls.Button? paneToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as System.Windows.Controls.Button;
                    Assert.IsNotNull(back);
                    Assert.IsNotNull(paneToggle);
                    Assert.AreEqual(Visibility.Collapsed, back.Visibility,
                        "Disabled back should collapse and stop reserving a 48px glyph slot.");
                    Assert.AreEqual(0.0, paneToggle.TransformToAncestor(nav).Transform(new Point(0, 0)).X, 0.5,
                        "Pane toggle should reflow into the first chrome slot when disabled back collapses.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_IsPaneToggleButtonVisible_False_HidesPaneToggle()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneToggleButtonVisible = false
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();
                    System.Windows.Controls.Button? paneToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as System.Windows.Controls.Button;
                    Assert.IsNotNull(paneToggle);
                    Assert.AreEqual(Visibility.Collapsed, paneToggle.Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_BackRequested_FiresOnBackClick()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        IsBackButtonVisible = true,
                        IsBackEnabled = true
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    bool fired = false;
                    void handler(object? sender, NavigationViewBackRequestedEventArgs e) { fired = true; }
                    nav.BackRequested += handler;
                    _ = nav.ApplyTemplate();
                    nav.RaiseBackRequestedForTesting();
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(fired);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_ThemeSwitch_UpdatesBrushes()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsTrue(application?.Resources.MergedDictionaries.Count > 0);
                    Color lightBase = (Color)application.Resources.MergedDictionaries[0]["SolidBackgroundFillColorBase"];

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    DrainDispatcher(window.Dispatcher);
                    Color darkBase = (Color)application.Resources.MergedDictionaries[0]["SolidBackgroundFillColorBase"];

                    Assert.AreNotEqual(lightBase, darkBase,
                        "Theme color SolidBackgroundFillColorBase should differ between light and dark.");
                    nav.UpdateLayout();
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_SharedIndicator_ExistsInTemplate_AndVisibleWhenSelected()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    NavigationViewItem item0 = new() { Content = "One" };
                    NavigationViewItem item1 = new() { Content = "Two" };
                    _ = nav.Items.Add(item0);
                    _ = nav.Items.Add(item1);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();
                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_PreTemplateSelection_PositionsSharedIndicatorAfterTemplateApplied()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    NavigationViewItem item = new()
                    {
                        Content = "Home",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 }
                    };
                    _ = nav.Items.Add(item);
                    nav.SelectedItem = item;

                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    Assert.AreEqual(1.0, indicator.Opacity, 0.01,
                        "Selection made before template application should show the shared indicator after layout.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftMode_SharedIndicator_TracksHorizontalItemPlacement()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Home",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 }
                    });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Child", IsChildItem = true });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    double iconItemX = GetSelectionIndicatorTranslate(indicator).X;
                    Assert.AreEqual(4.0, iconItemX, 0.5,
                        "Icon item indicator should sit inside the selected item background.");

                    nav.SelectedIndex = 1;
                    WaitForAnimationAndDrain(window.Dispatcher, 600);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    double childItemX = GetSelectionIndicatorTranslate(indicator).X;
                    Assert.AreEqual(48.0, childItemX, 0.5,
                        "Iconless child item indicator should move inward without overlapping the content column.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftMode_SharedIndicator_AnimatesBetweenSelections()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Home",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 }
                    });
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Settings",
                        Icon = new FontIcon { Glyph = "\uE713", IconFontSize = 20 }
                    });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    TranslateTransform translate = GetSelectionIndicatorTranslate(indicator);
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftMode_IndicatorExitsVerticallyBeforeChangingParentChildIndent()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Parent",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 }
                    });
                    _ = nav.Items.Add(new NavigationViewItem
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

                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    TranslateTransform translate = GetSelectionIndicatorTranslate(indicator);
                    double parentX = translate.X;
                    double parentY = translate.Y;

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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftMode_IndicatorExitsUpwardWhenNewSelectionIsAbove()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Parent",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 }
                    });
                    _ = nav.Items.Add(new NavigationViewItem
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

                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    TranslateTransform translate = GetSelectionIndicatorTranslate(indicator);
                    double childX = translate.X;
                    double childY = translate.Y;

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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftMode_TopLevelIconlessItem_DoesNotUseChildIndicatorIndent()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem
                    {
                        Content = "Home",
                        Icon = new FontIcon { Glyph = "\uE80F", IconFontSize = 20 }
                    });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "No icon top-level" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in the NavigationView template.");
                    double iconItemX = GetSelectionIndicatorTranslate(indicator).X;

                    nav.SelectedIndex = 1;
                    WaitForAnimationAndDrain(window.Dispatcher, 600);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    double noIconItemX = GetSelectionIndicatorTranslate(indicator).X;
                    Assert.AreEqual(iconItemX, noIconItemX, 0.5,
                        "A top-level item without an icon should keep the top-level indicator position; child indentation must be explicit.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationViewItem_FocusVisual_StaysInsideItemBounds()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    Style? style = application?.TryFindResource("NavigationViewItemFocusVisual") as Style;
                    Assert.IsNotNull(style, "NavigationViewItemFocusVisual should be present in Generic.xaml.");

                    ControlTemplate? template = null;
                    foreach (SetterBase? setterBase in style.Setters)
                    {
                        if (setterBase is Setter setter && setter.Property == Control.TemplateProperty)
                        {
                            template = setter.Value as ControlTemplate;
                            break;
                        }
                    }

                    Assert.IsNotNull(template, "NavigationViewItemFocusVisual should provide a ControlTemplate.");

                    DependencyObject root = template.LoadContent();
                    Assert.IsNotNull(root, "Focus visual template should load a visual tree.");

                    foreach (System.Windows.Controls.Border border in FindVisualChildren<System.Windows.Controls.Border>(root))
                    {
                        Assert.IsTrue(border.Margin.Left >= 0.0 && border.Margin.Right >= 0.0,
                            "Navigation item focus strokes should stay inside the selected item bounds horizontally.");
                    }
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_SharedIndicator_HidesWhenSelectionCleared()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.AreEqual(1.0, indicator?.Opacity ?? 0.0, 0.01);

                    nav.SelectedItem = null;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(0.0, indicator?.Opacity ?? 1.0, 0.01, "Indicator should hide when selection is cleared.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_TopMode_SharedIndicator_VisibleWhenSelected()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 600,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Alpha" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Beta" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    nav.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "PART_SelectionIndicator should exist in Top pane template.");
                    Assert.AreEqual(1.0, indicator.Opacity, 0.01, "Indicator should be visible in top mode.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S2699:Tests should include assertions", Justification = "Don't care for now.")]
        public void NavigationView_FullThemeCycle_NoExceptions()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ApplicationTheme[] themes =
                    [
                        ApplicationTheme.Light,
                        ApplicationTheme.Dark,
                        ApplicationTheme.HighContrast,
                        ApplicationTheme.Auto
                    ];

                    for (int i = 0; i < themes.Length; i++)
                    {
                        ApplicationThemeManager.Apply(themes[i], BackdropType.None, true);
                        DrainDispatcher(window.Dispatcher);
                        nav.UpdateLayout();
                    }
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_PaneModeSwitch_IndicatorSurvives()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 600,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Two" });
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

                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "Indicator should exist after mode switch.");
                    Assert.AreEqual(1.0, indicator.Opacity, 0.01, "Indicator should remain visible after mode switch.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_PaneCollapse_IndicatorSurvives()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
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

                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationViewItem_DisabledState_ChangesForeground()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    NavigationViewItem item = new() { Content = "Disabled" };
                    _ = nav.Items.Add(item);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Brush enabledForeground = item.Foreground;

                    item.IsEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Brush disabledForeground = item.Foreground;
                    Assert.AreNotEqual(enabledForeground, disabledForeground,
                        "Foreground should change when item is disabled.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_Left_PaneClosedInitially_ContentStartsAt48px_Inline()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = false
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    ContentPresenter? presenter = FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter);
                    Assert.IsNotNull(presenter, "PART_ContentPresenter must exist in Left template.");

                    Point offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.AreEqual(48.0, offset.X, 1.0,
                        "When Left mode starts with IsPaneOpen=false, content must start at the 48px compact rail, not at the expanded pane width.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_Left_ContentStarts42pxBelowWindowTop()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    ContentPresenter? presenter = FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter);
                    Assert.IsNotNull(presenter, "PART_ContentPresenter must exist in Left template.");

                    Point offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.AreEqual(42.0, offset.Y, 1.0,
                        "Left NavigationView content should start 42px below the top of the window.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_Left_HeaderContentUsesAutoHeight()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                        IsPaneOpen = true,
                        Header = new System.Windows.Controls.Border { Width = 100, Height = 20 }
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    ContentPresenter? presenter = FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter);
                    Assert.IsNotNull(presenter, "PART_ContentPresenter must exist in Left template.");

                    Point offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.AreEqual(20.0, offset.Y, 1.0,
                        "Left NavigationView should only reserve the 42px top gap when Header is empty.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
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
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = true
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    // Pane-open enter animation is 167 ms (CubicEase EaseOut). Wait past HoldEnd.
                    WaitForAnimationAndDrain(window.Dispatcher, 300);
                    window.UpdateLayout();

                    ContentPresenter? presenter = FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter);
                    Assert.IsNotNull(presenter, "PART_ContentPresenter must exist in LeftCompact template.");

                    AssertContentOffsetEventually(window, nav, presenter, 280.0,
                        "When IsPaneOpen=true in LeftCompact, content must start inline at pane width 280 (not overlap the pane).");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftCompact_HeaderContentUsesAutoHeight()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = true,
                        Header = new System.Windows.Controls.Border { Width = 100, Height = 20 }
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WaitForAnimationAndDrain(window.Dispatcher, 300);
                    window.UpdateLayout();

                    ContentPresenter? presenter = FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter);
                    Assert.IsNotNull(presenter, "PART_ContentPresenter must exist in LeftCompact template.");

                    Point offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.AreEqual(20.0, offset.Y, 1.0,
                        "LeftCompact NavigationView should only reserve the 42px top gap when Header is empty.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftCompact_PaneClosed_ContentStartsAt48px_Inline()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = false
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    ContentPresenter? presenter = FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter);
                    Assert.IsNotNull(presenter, "PART_ContentPresenter must exist in LeftCompact template.");

                    AssertContentOffsetEventually(window, nav, presenter, 48.0,
                        "When IsPaneOpen=false in LeftCompact, content must start inline at pane width 48 (compact rail).");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftCompact_BackEnabledClosedPane_KeepsPaneToggleVisible()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = true,
                        IsBackButtonVisible = true,
                        IsBackEnabled = true,
                        IsPaneToggleButtonVisible = true
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    nav.IsPaneOpen = false;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    DrainDispatcher(window.Dispatcher);

                    System.Windows.Controls.Button? back = nav.Template.FindName(NavigationView.PartBackButton, nav) as System.Windows.Controls.Button;
                    System.Windows.Controls.Button? paneToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as System.Windows.Controls.Button;
                    ContentPresenter? presenter = FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter);
                    Assert.IsNotNull(back, "PART_BackButton must exist in LeftCompact template.");
                    Assert.IsNotNull(paneToggle, "PART_PaneToggleButton must exist in LeftCompact template.");
                    Assert.IsNotNull(presenter, "PART_ContentPresenter must exist in LeftCompact template.");
                    Assert.AreEqual(Visibility.Visible, back.Visibility,
                        "Back should remain visible while enabled in compact chrome.");
                    Assert.AreEqual(Visibility.Visible, paneToggle.Visibility,
                        "Pane toggle should remain visible to the right of the enabled back button after collapse.");
                    Assert.AreEqual(48.0, paneToggle.TransformToAncestor(nav).Transform(new Point(0, 0)).X, 1.0,
                        "Pane toggle should occupy the second compact chrome slot.");
                    AssertContentOffsetEventually(window, nav, presenter, 96.0,
                        "Closed LeftCompact pane should reserve both 48px chrome slots when back and pane toggle are visible.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_LeftCompact_PaneToggle_ResizesPushingContent()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 800,
                        Height = 480,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                        IsPaneOpen = true
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "One" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    // Pane enter animation is 167 ms (CubicEase). Wait past HoldEnd before sampling layout.
                    WaitForAnimationAndDrain(window.Dispatcher, 300);
                    window.UpdateLayout();

                    ContentPresenter? presenter = FindVisualChildByName<ContentPresenter>(nav, NavigationView.PartContentPresenter);
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
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
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SolidColorBrush? expected = application?.TryFindResource("NavigationViewContentBackgroundBrush") as SolidColorBrush;
                    SolidColorBrush? actual = nav.ContentBackground as SolidColorBrush;

                    Assert.IsNotNull(expected, "NavigationViewContentBackgroundBrush must be present in merged resources.");
                    Assert.IsNotNull(actual, "NavigationView.ContentBackground must be a SolidColorBrush.");
                    Assert.AreEqual(expected.Color, actual.Color,
                        "Default ContentBackground must equal NavigationViewContentBackgroundBrush (semi-transparent Mica tint).");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
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
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    NavigationViewItemHeader header = new() { Content = "Input" };
                    NavigationViewItem item = new() { Content = "Buttons" };
                    _ = nav.Items.Add(header);
                    _ = nav.Items.Add(item);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    NavigationViewItemHeader? renderedHeader = FindVisualChild<NavigationViewItemHeader>(nav);
                    Assert.IsNotNull(renderedHeader, "NavigationViewItemHeader must render inside the pane.");
                    Assert.IsFalse(renderedHeader.Focusable, "Header must not be focusable.");
                    Assert.IsNull(nav.SelectedItem, "Header must not be auto-selected even when placed at index 0.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
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
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new() { Width = 700, Height = 500 };
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationView_IsBackButtonVisible_True_ShowsBackButton()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new() { Width = 700, Height = 500, IsBackButtonVisible = true };
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);

                    System.Windows.Controls.Button? back = nav.Template.FindName(NavigationView.PartBackButton, nav) as System.Windows.Controls.Button;
                    Assert.IsNotNull(back, "PART_BackButton must exist.");
                    Assert.AreEqual(Visibility.Visible, back.Visibility,
                        "PART_BackButton must be Visible when IsBackButtonVisible=True (WI-3 B15 VSM).");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
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
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationView nav = new()
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
                    Assert.IsNotNull(application?.TryFindResource("NavigationViewContentBackgroundBrush"),
                        "NavigationViewContentBackgroundBrush must resolve under Light theme.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsNotNull(nav.ContentBackground,
                        "ContentBackground must resolve under Dark theme.");
                    Assert.IsNotNull(application?.TryFindResource("NavigationViewContentBackgroundBrush"),
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
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
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                // ---- Left pane ----
                Window winLeft = new();
                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    winLeft.Content = nav;
                    winLeft.Show();
                    DrainDispatcher(winLeft.Dispatcher);
                    winLeft.UpdateLayout();

                    System.Windows.Controls.Border? paneBorder = FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneBorder");
                    Assert.IsNotNull(paneBorder, "Left pane must expose Border named 'PaneBorder'.");
                    AssertBrushIsTransparentOrNull(paneBorder.Background,
                        "PaneBorder.Background must be Transparent so DWM backdrop shows through.");
                }
                finally
                {
                    CloseWindowAndDrain(winLeft);
                }

                // ---- LeftCompact pane ----
                Window winCompact = new();
                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    winCompact.Content = nav;
                    winCompact.Show();
                    DrainDispatcher(winCompact.Dispatcher);
                    winCompact.UpdateLayout();

                    System.Windows.Controls.Border? compactPane = FindVisualChildByName<System.Windows.Controls.Border>(nav, "CompactPane");
                    Assert.IsNotNull(compactPane, "LeftCompact pane must expose Border named 'CompactPane'.");
                    AssertBrushIsTransparentOrNull(compactPane.Background,
                        "CompactPane.Background must be Transparent so DWM backdrop shows through.");
                }
                finally
                {
                    CloseWindowAndDrain(winCompact);
                }

                // ---- Top pane ----
                Window winTop = new();
                try
                {
                    NavigationView nav = new()
                    {
                        Width = 600,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });
                    winTop.Content = nav;
                    winTop.Show();
                    DrainDispatcher(winTop.Dispatcher);
                    winTop.UpdateLayout();

                    System.Windows.Controls.Border? paneHeader = FindVisualChildByName<System.Windows.Controls.Border>(nav, "PaneHeaderBorder");
                    Assert.IsNotNull(paneHeader, "Top pane must expose Border named 'PaneHeaderBorder'.");
                    AssertBrushIsTransparentOrNull(paneHeader.Background,
                        "PaneHeaderBorder.Background must be Transparent so DWM backdrop shows through.");
                }
                finally
                {
                    CloseWindowAndDrain(winTop);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        /// <summary>
        /// Asserts that <paramref name="brush"/> is null, Brushes.Transparent, or a
        /// SolidColorBrush whose alpha channel is zero — i.e. effectively transparent.
        /// </summary>
        private static void AssertBrushIsTransparentOrNull(Brush brush, string message)
        {
            if (brush is null)
            {
                return; // null == no background == transparent
            }

            if (brush == Brushes.Transparent)
            {
                return;
            }

            if (brush is SolidColorBrush solid && solid.Color.A == 0)
            {
                return;
            }

            Assert.Fail(message + " Actual: " + brush);
        }

        private static void AssertPaneItemsScrollViewerUsesFluentStyle(NavigationViewPaneDisplayMode mode, bool isPaneOpen)
        {
            Application? application = EnsureApplication();
            Style? expected = application?.TryFindResource("ScrollViewerStyle") as Style;
            Assert.IsNotNull(expected, "ScrollViewerStyle must be present in merged Fluence resources.");

            Window window = new();
            try
            {
                NavigationView nav = new()
                {
                    Width = 640,
                    Height = 420,
                    PaneDisplayMode = mode,
                    IsPaneOpen = isPaneOpen
                };
                _ = nav.Items.Add(new NavigationViewItem { Content = "Item" });

                window.Content = nav;
                window.Show();
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                ScrollViewer? scrollViewer = FindVisualChildByName<ScrollViewer>(nav, NavigationView.PartPaneItemsScrollViewer);
                Assert.IsNotNull(scrollViewer, "NavigationView template must expose PART_PaneItemsScrollViewer.");
                Assert.IsInstanceOfType(scrollViewer, typeof(SmoothScrollViewer),
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
            TransformGroup? group = indicator.RenderTransform as TransformGroup;
            Assert.IsNotNull(group, "Selection indicator must use a TransformGroup.");
            Assert.IsTrue(group.Children.Count >= 2, "Selection indicator TransformGroup must contain scale and translate transforms.");
            TranslateTransform? translate = group.Children[1] as TranslateTransform;
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
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NavigationViewItem item = new()
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

                    System.Windows.Controls.Border? inner = FindVisualChildByName<System.Windows.Controls.Border>(item, "SelectionIndicator");
                    Assert.IsNull(inner,
                        "NavigationViewItem template must not contain a per-item Border named 'SelectionIndicator'. " +
                        "The pane-level PART_SelectionIndicator owns the selection visual.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
