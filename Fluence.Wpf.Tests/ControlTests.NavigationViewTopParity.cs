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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void NavigationView_InFluenceWindow_LeftAndTopCoerceTitleBarExtension()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                FluenceWindow window = new()
                {
                    Width = 640,
                    Height = 420,
                    ExtendsContentIntoTitleBar = false
                };

                try
                {
                    NavigationView nav = new()
                    {
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Home" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsTrue(window.ExtendsContentIntoTitleBar,
                        "Left NavigationView pane mode should extend FluenceWindow content into the title bar.");

                    nav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsFalse(window.ExtendsContentIntoTitleBar,
                        "Top NavigationView pane mode should disable FluenceWindow content extension into the title bar.");
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
        public void NavigationView_TopMode_CoercesPaneOpenAndToggleHidden()
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
                        Width = 520,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        IsPaneOpen = false,
                        IsPaneToggleButtonVisible = true
                    };
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Home" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsTrue(nav.IsPaneOpen, "Top mode should always report IsPaneOpen=True.");
                    Assert.IsFalse(nav.IsPaneToggleButtonVisible,
                        "Top mode should always report IsPaneToggleButtonVisible=False.");

                    nav.IsPaneOpen = false;
                    nav.IsPaneToggleButtonVisible = true;
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(nav.IsPaneOpen, "Top mode should coerce runtime IsPaneOpen changes back to true.");
                    Assert.IsFalse(nav.IsPaneToggleButtonVisible,
                        "Top mode should coerce runtime IsPaneToggleButtonVisible changes back to false.");
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
        public void NavigationView_TopMode_KeepsItemIconAndTextVisibleWithoutScrollViewer()
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
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top
                    };
                    NavigationViewItem item = new()
                    {
                        Content = "Home",
                        Icon = new FontIcon { Glyph = "\uE80F" }
                    };
                    _ = nav.Items.Add(item);
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ScrollViewer? topScrollViewer = FindVisualChildByName<ScrollViewer>(nav, NavigationView.PartPaneItemsScrollViewer);
                    Assert.IsNull(topScrollViewer, "Top pane must not expose a scrolling pane-items strip.");

                    ContentPresenter? iconPresenter = FindVisualChildByName<ContentPresenter>(item, "IconPresenter");
                    ContentPresenter? contentPresenter = FindVisualChildByName<ContentPresenter>(item, "ContentPresenter");
                    Assert.IsNotNull(iconPresenter, "Top navigation items should still render their icon presenter.");
                    Assert.IsNotNull(contentPresenter, "Top navigation items should still render their text/content presenter.");
                    Assert.AreEqual(Visibility.Visible, iconPresenter.Visibility,
                        "Top navigation item icon presenter should stay visible.");
                    Assert.AreEqual(Visibility.Visible, contentPresenter.Visibility,
                        "Top navigation item content presenter should stay visible.");
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
        public void NavigationView_TopMode_OverflowMenuInvokesHiddenItem()
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
                        Width = 220,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top
                    };
                    NavigationViewItem first = new() { Content = "Home", Icon = new FontIcon { Glyph = "\uE80F" } };
                    NavigationViewItem last = new() { Content = "Windowing", Icon = new FontIcon { Glyph = "\uE8A7" } };
                    _ = nav.Items.Add(first);
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Design", Icon = new FontIcon { Glyph = "\uE790" } });
                    _ = nav.Items.Add(new NavigationViewItem { Content = "Controls", Icon = new FontIcon { Glyph = "\uECAA" } });
                    _ = nav.Items.Add(last);

                    object? invokedItem = null;
                    nav.ItemInvoked += (_, e) => invokedItem = e.InvokedItem;

                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Button? overflowButton = FindVisualChildByName<System.Windows.Controls.Button>(nav, "PART_TopOverflowButton");
                    Assert.IsNotNull(overflowButton, "Top pane should expose a three-dot overflow button.");
                    Assert.AreEqual(Visibility.Visible, overflowButton.Visibility,
                        "Top pane overflow button should become visible when items do not fit.");
                    Assert.IsNotNull(overflowButton.ContextMenu, "Top pane overflow button should own a lightweight popup menu.");
                    Assert.IsTrue(overflowButton.ContextMenu.Items.Count > 0,
                        "Top pane overflow menu should contain hidden navigation items.");

                    System.Windows.Controls.MenuItem? overflowItem = overflowButton.ContextMenu.Items[overflowButton.ContextMenu.Items.Count - 1] as System.Windows.Controls.MenuItem;
                    Assert.IsNotNull(overflowItem, "Overflow entries should be lightweight MenuItem rows.");
                    overflowItem.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreSame(last, invokedItem,
                        "Clicking an overflow row should invoke the underlying NavigationViewItem without reparenting it.");
                    Assert.AreSame(last, nav.SelectedItem,
                        "Clicking an overflow row should select the underlying NavigationViewItem.");
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
