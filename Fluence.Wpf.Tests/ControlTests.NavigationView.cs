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

                    var offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.AreEqual(280.0, offset.X, 1.0,
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

                    var offset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.AreEqual(48.0, offset.X, 1.0,
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

                    var openOffset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.AreEqual(280.0, openOffset.X, 1.0, "Open state: content begins at 280.");

                    nav.IsPaneOpen = false;
                    WaitForAnimationAndDrain(window.Dispatcher, 300);
                    window.UpdateLayout();

                    var closedOffset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.AreEqual(48.0, closedOffset.X, 1.0, "Closed state: content begins at 48 (push, not overlay).");

                    nav.IsPaneOpen = true;
                    WaitForAnimationAndDrain(window.Dispatcher, 300);
                    window.UpdateLayout();

                    var reopenOffset = presenter.TransformToAncestor(nav).Transform(new Point(0, 0));
                    Assert.AreEqual(280.0, reopenOffset.X, 1.0, "Reopen state: content returns to 280.");
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

        // WI-1 F2: NavigationView.ContentBackground must default to SolidBackgroundFillColorBaseBrush
        // (per commit 597aad2 - LayerFillColorDefault is 50% transparent white, composites wrong
        // on WPF-based Mica; the solid #F3F3F3 base matches WinUI 3 Gallery tone in light mode).
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

                    var expected = application.TryFindResource("SolidBackgroundFillColorBaseBrush") as SolidColorBrush;
                    var actual = nav.ContentBackground as SolidColorBrush;

                    Assert.IsNotNull(expected, "SolidBackgroundFillColorBaseBrush must be present in merged resources.");
                    Assert.IsNotNull(actual, "NavigationView.ContentBackground must be a SolidColorBrush.");
                    Assert.AreEqual(expected.Color, actual.Color,
                        "Default ContentBackground must equal SolidBackgroundFillColorBaseBrush (WinUI 3 Gallery content tone).");
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

        [TestMethod]
        public void NavigationView_CompactPane_BackgroundIsTransparent()
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
                        Width = 700,
                        Height = 500,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact
                    };
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);

                    var compactPane = FindVisualChildByName<System.Windows.Controls.Border>(nav, "CompactPane");
                    Assert.IsNotNull(compactPane, "CompactPane Border must exist in LeftCompact template.");

                    // Pane background must be Transparent (or null) so the DWM Mica/Acrylic
                    // backdrop shows through seamlessly — matches the FluenceWindow title bar.
                    bool isTransparent = compactPane.Background == null
                        || (compactPane.Background is SolidColorBrush scb && scb.Color == Colors.Transparent);
                    Assert.IsTrue(isTransparent,
                        "CompactPane Background must be Transparent (or null) to allow Mica/Acrylic backdrop continuity.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary != null)
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        // WI-1 F2: NavigationView.ContentBackground must resolve to SolidBackgroundFillColorBaseBrush
        // (per commit 597aad2). The default style ships the DynamicResource binding; this test
        // guards the contract and proves the brush re-resolves correctly after a theme swap.
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
                    var lightBase = nav.FindResource("SolidBackgroundFillColorBaseBrush") as Brush;
                    Assert.IsNotNull(lightBase, "SolidBackgroundFillColorBaseBrush must resolve under Light theme.");
                    Assert.AreSame(lightBase, nav.ContentBackground,
                        "Light-theme ContentBackground must resolve to SolidBackgroundFillColorBaseBrush via DynamicResource.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    DrainDispatcher(window.Dispatcher);
                    var darkBase = nav.FindResource("SolidBackgroundFillColorBaseBrush") as Brush;
                    Assert.IsNotNull(darkBase, "SolidBackgroundFillColorBaseBrush must resolve under Dark theme.");
                    Assert.AreSame(darkBase, nav.ContentBackground,
                        "Dark-theme ContentBackground must resolve to SolidBackgroundFillColorBaseBrush via DynamicResource.");

                    ThemeTestHelpers.ApplyStandardThemeCycle();
                    DrainDispatcher(window.Dispatcher);
                    var postCycleBase = nav.FindResource("SolidBackgroundFillColorBaseBrush") as Brush;
                    Assert.IsNotNull(postCycleBase, "SolidBackgroundFillColorBaseBrush must resolve after a full theme cycle.");
                    Assert.AreSame(postCycleBase, nav.ContentBackground,
                        "ContentBackground must track the current SolidBackgroundFillColorBaseBrush after a full theme cycle.");
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
    }
}
