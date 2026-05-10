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
using FluentButton = Fluence.Wpf.Controls.Button;
using FluentMenuItem = Fluence.Wpf.Controls.MenuItem;
using WpfBorder = System.Windows.Controls.Border;
using WpfStackPanel = System.Windows.Controls.StackPanel;

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
                    NavigationViewItem second = new()
                    {
                        Content = "Design",
                        Icon = new FontIcon { Glyph = "\uE790" }
                    };
                    _ = nav.Items.Add(item);
                    _ = nav.Items.Add(second);
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
                    Assert.AreEqual(new Thickness(4, 0, 2, 0), iconPresenter.Margin,
                        "Top navigation item icon presenter should keep the tighter strip while adding 2px more lead-in before the icon.");
                    Assert.AreEqual(new Thickness(2, 0, 2, 0), contentPresenter.Margin,
                        "Top navigation item text presenter should use 2px horizontal spacing.");
                    WpfBorder? outerBorder = FindVisualChildByName<WpfBorder>(item, "OuterBorder");
                    ContentPresenter? infoBadgePresenter = FindVisualChildByName<ContentPresenter>(item, "InfoBadgePresenter");
                    Assert.IsNotNull(outerBorder, "Top navigation item template should expose the outer border.");
                    Assert.IsNotNull(infoBadgePresenter, "Navigation item template should expose the info badge presenter.");
                    Assert.AreEqual(new Thickness(4, 4, 4, 4), outerBorder.Margin,
                        "Top navigation items should keep compact 4px horizontal outer spacing.");
                    Assert.AreEqual(new Thickness(0), outerBorder.Padding,
                        "Top navigation items should not keep left-pane padding.");
                    Assert.AreEqual(Visibility.Collapsed, infoBadgePresenter.Visibility,
                        "Navigation items without an info badge should not reserve trailing badge space.");

                    ColumnDefinition? iconColumn = item.Template.FindName("IconColumn", item) as ColumnDefinition;
                    ColumnDefinition? gapColumn = item.Template.FindName("GapColumn", item) as ColumnDefinition;
                    ColumnDefinition? contentColumn = item.Template.FindName("ContentColumn", item) as ColumnDefinition;
                    Assert.IsNotNull(iconColumn, "Top navigation item template should expose the icon column.");
                    Assert.IsNotNull(gapColumn, "Top navigation item template should expose the icon/text gap column.");
                    Assert.IsNotNull(contentColumn, "Top navigation item template should expose the text content column.");
                    Assert.AreEqual(GridUnitType.Auto, iconColumn.Width.GridUnitType,
                        "Top navigation item icon column should size to its content instead of keeping the left-pane rail width.");
                    Assert.AreEqual(0.0, gapColumn.Width.Value, 0.01,
                        "Top navigation item icon/text gap column should not add extra spacing.");
                    Assert.AreEqual(GridUnitType.Auto, contentColumn.Width.GridUnitType,
                        "Top navigation item text column should size to content so items do not reserve a wide trailing gap.");

                    ContentPresenter? secondIconPresenter = FindVisualChildByName<ContentPresenter>(second, "IconPresenter");
                    Assert.IsNotNull(secondIconPresenter, "Second top navigation item should render its icon presenter.");
                    double textToNextIconGap = GetNavigationElementX(secondIconPresenter, nav) - GetNavigationElementRight(contentPresenter, nav);
                    Assert.AreEqual(18.0, textToNextIconGap, 1.5,
                        "Top navigation item text should leave the requested 2px text margin, 4px item margins, and 4px icon lead-in before the next item icon.");
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
                        Width = 300,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
                        PaneFooter = new WpfStackPanel { Width = 88, Height = 36 }
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

                    FluentButton? overflowButton = FindVisualChildByName<FluentButton>(nav, "PART_TopOverflowButton");
                    Assert.IsNotNull(overflowButton, "Top pane should expose a three-dot overflow button.");
                    Assert.AreEqual(Fluence.Wpf.ControlAppearance.Subtle, overflowButton.Appearance,
                        "Top pane overflow button should use the same subtle Fluence button chrome as other navigation strip buttons.");
                    Assert.AreEqual(Visibility.Visible, overflowButton.Visibility,
                        "Top pane overflow button should become visible when items do not fit.");
                    Grid? topItemsHost = FindVisualChildByName<Grid>(nav, NavigationView.PartTopItemsHost);
                    Assert.IsNotNull(topItemsHost, "Top pane should expose the horizontal item host.");
                    double visibleItemsRight = double.MinValue;
                    foreach (object item in nav.Items)
                    {
                        if (item is NavigationViewItem navItem && navItem.Visibility == Visibility.Visible)
                        {
                            double itemRight = GetNavigationElementRight(navItem, nav);
                            if (itemRight > visibleItemsRight)
                            {
                                visibleItemsRight = itemRight;
                            }
                        }
                    }

                    double overflowButtonGap = GetNavigationElementX(overflowButton, nav) - visibleItemsRight;
                    Assert.AreEqual(4.0, overflowButtonGap, 1.5,
                        "Top pane overflow button should sit after the last visible navigation item using the same 4px strip spacing.");
                    WpfStackPanel? footer = nav.PaneFooter as WpfStackPanel;
                    Assert.IsNotNull(footer, "Test setup should use a right-docked PaneFooter.");
                    Assert.IsTrue(GetNavigationElementRight(overflowButton, nav) <= GetNavigationElementX(footer, nav) + 0.5,
                        "Top pane overflow button should appear before the right-docked PaneFooter instead of docking to the strip edge.");
                    Assert.IsNotNull(overflowButton.ContextMenu, "Top pane overflow button should own a lightweight popup menu.");
                    Assert.IsTrue(overflowButton.ContextMenu.Items.Count > 0,
                        "Top pane overflow menu should contain hidden navigation items.");

                    FluentMenuItem? overflowItem = overflowButton.ContextMenu.Items[overflowButton.ContextMenu.Items.Count - 1] as FluentMenuItem;
                    Assert.IsNotNull(overflowItem, "Overflow entries should be lightweight Fluence MenuItem rows.");
                    Assert.IsNotNull(overflowItem.Icon, "Overflow entries should include the underlying item icon.");
                    Assert.AreEqual("Windowing", overflowItem.Header,
                        "Overflow entries should show the underlying item text.");
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

        private static double GetNavigationElementX(FrameworkElement element, NavigationView ancestor)
        {
            return element.TransformToAncestor(ancestor).Transform(new Point(0, 0)).X;
        }

        private static double GetNavigationElementRight(FrameworkElement element, NavigationView ancestor)
        {
            return GetNavigationElementX(element, ancestor) + element.ActualWidth;
        }
    }
}
