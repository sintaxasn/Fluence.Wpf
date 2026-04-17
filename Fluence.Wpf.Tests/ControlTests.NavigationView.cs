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
    }
}
