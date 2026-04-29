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
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using FluenceTextBox = Fluence.Wpf.Controls.TextBox;

namespace Fluence.Wpf.Tests
{
    // WI-1 behavior tests for the demo shell: nav search (F3), Paradigm A structure, featured grid.
    // These tests exercise the concrete MainWindow and GalleryHomePage rather than the control
    // templates so regressions in demo-level UX surface immediately.
    [TestClass]
    public class DemoMainWindowTests
    {
        private static void RunOnSta(Action action)
        {
            Exception captured = null;
            WpfTestSta.Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    captured = ex;
                }
            }));

            if (captured != null)
            {
                ExceptionDispatchInfo.Capture(captured).Throw();
            }
        }

        private static Application EnsureApp()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static ResourceDictionary MergeTheme(Application application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            var dictionaries = application.Resources.MergedDictionaries;
            var generic = dictionaries.Count > 0 ? dictionaries[dictionaries.Count - 1] : null;

            var demoShared = new ResourceDictionary
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
            };
            application.Resources.MergedDictionaries.Add(demoShared);

            return generic;
        }

        private static void Drain(Dispatcher dispatcher)
        {
            dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
        }

        private static MainWindow CreateShownMainWindow()
        {
            var window = new MainWindow
            {
                Left = -20000,
                Top = -20000,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false
            };
            window.Show();
            Drain(window.Dispatcher);
            window.UpdateLayout();
            Drain(window.Dispatcher);
            return window;
        }

        private static T GetPrivateField<T>(object instance, string name) where T : class
        {
            var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, "Field must exist: " + name);
            return field.GetValue(instance) as T;
        }

        private static FluenceTextBox GetNavSearchBox(MainWindow window)
        {
            return GetPrivateField<FluenceTextBox>(window, "NavSearchBox");
        }

        private static NavigationView GetDemoNav(MainWindow window)
        {
            return GetPrivateField<NavigationView>(window, "DemoNav");
        }

        private static void RaisePreviewKeyDown(FrameworkElement source, Key key)
        {
            var args = new KeyEventArgs(
                Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(source),
                0,
                key)
            {
                RoutedEvent = Keyboard.PreviewKeyDownEvent
            };
            source.RaiseEvent(args);
        }

        // WI-1 F3: Enter in the search box must select the top visible match.
        [TestMethod]
        public void NavSearch_EnterKey_SelectsTopMatch()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    var search = GetNavSearchBox(window);
                    var nav = GetDemoNav(window);

                    search.Focus();
                    search.Text = "button";
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    RaisePreviewKeyDown(search, Key.Enter);
                    Drain(window.Dispatcher);

                    var selected = nav.SelectedItem as NavigationViewItem;
                    Assert.IsNotNull(selected, "Enter must select a NavigationViewItem.");
                    Assert.AreEqual("Button", selected.Content as string,
                        "Top match for 'button' must be the 'Button' page.");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        // WI-1 F3: Empty search box + Enter must not change the selection.
        [TestMethod]
        public void NavSearch_EnterKey_EmptyQuery_DoesNotChangeSelection()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    var search = GetNavSearchBox(window);
                    var nav = GetDemoNav(window);

                    var before = nav.SelectedItem;

                    search.Text = string.Empty;
                    Drain(window.Dispatcher);
                    RaisePreviewKeyDown(search, Key.Enter);
                    Drain(window.Dispatcher);

                    Assert.AreSame(before, nav.SelectedItem,
                        "Empty Enter must be a no-op for selection.");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        // WI-1 F3: Enter with zero matches must not throw and must leave selection untouched.
        [TestMethod]
        public void NavSearch_EnterKey_NoMatches_DoesNotThrowAndKeepsSelection()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    var search = GetNavSearchBox(window);
                    var nav = GetDemoNav(window);

                    var before = nav.SelectedItem;
                    search.Text = "zzz-no-such-token-zzz";
                    Drain(window.Dispatcher);

                    RaisePreviewKeyDown(search, Key.Enter);
                    Drain(window.Dispatcher);

                    Assert.AreSame(before, nav.SelectedItem,
                        "Enter with zero matches must not reset or randomise the selection.");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        // WI-1 F3: Empty query restores the grouped pane to its expanded/collapsed state.
        [TestMethod]
        public void NavSearch_EmptyQuery_RestoresGroupedPaneVisibility()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    var search = GetNavSearchBox(window);
                    var nav = GetDemoNav(window);

                    search.Text = "button";
                    Drain(window.Dispatcher);

                    search.Text = string.Empty;
                    Drain(window.Dispatcher);

                    var selected = nav.SelectedItem as NavigationViewItem;
                    Assert.IsNotNull(selected, "The search result should remain selected before clearing.");

                    foreach (var obj in nav.Items)
                    {
                        var header = obj as NavigationViewItemHeader;
                        if (header != null)
                        {
                            Assert.AreEqual(Visibility.Visible, header.Visibility,
                                "Empty query must restore navigation headers to Visible.");
                            continue;
                        }

                        var el = obj as FrameworkElement;
                        if (el == null)
                        {
                            continue;
                        }

                        var item = el as NavigationViewItem;
                        if (item != null && item.PageContent == null)
                        {
                            Assert.AreEqual(Visibility.Visible, item.Visibility,
                                "Empty query must restore navigation group headings to Visible.");
                        }
                    }

                    Assert.AreEqual(Visibility.Visible, selected.Visibility,
                        "The selected search result should stay visible after clearing the filter.");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        // WinUI Gallery pattern: one Controls header plus expandable navigation groups.
        [TestMethod]
        public void MainWindow_NavigationPane_UsesExpandableWinUIGalleryGroups()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    var nav = GetDemoNav(window);

                    var headers = new System.Collections.Generic.List<string>();
                    var parentItems = new System.Collections.Generic.List<string>();
                    NavigationViewItem buttonItem = null;
                    foreach (var obj in nav.Items)
                    {
                        var header = obj as NavigationViewItemHeader;
                        if (header != null)
                        {
                            headers.Add(header.Content as string);
                        }

                        var item = obj as NavigationViewItem;
                        if (item == null)
                        {
                            continue;
                        }

                        var title = item.Content as string;
                        if (string.Equals(title, "Design", StringComparison.Ordinal)
                            || string.Equals(title, "Accessibility", StringComparison.Ordinal)
                            || string.Equals(title, "Basic input", StringComparison.Ordinal))
                        {
                            parentItems.Add(title);
                            Assert.IsNotNull(item.Icon, "Navigation group headings should have icons.");
                            Assert.IsNotNull(item.InfoBadge, "Navigation group headings should show an expand/collapse glyph.");
                        }

                        if (string.Equals(title, "Button", StringComparison.Ordinal))
                        {
                            buttonItem = item;
                        }
                    }

                    CollectionAssert.AreEqual(new[] { "Controls" }, headers,
                        "The old flat Fundamentals/Basic input/etc. headers should be replaced by a single Controls header.");
                    CollectionAssert.Contains(parentItems, "Design");
                    CollectionAssert.Contains(parentItems, "Accessibility");
                    CollectionAssert.Contains(parentItems, "Basic input");
                    Assert.IsNotNull(buttonItem, "Basic input should contain a Button child page.");
                    Assert.IsNull(buttonItem.Icon, "Child pages should not have icons; only section headings should.");

                    AssertNavigationItemExists(nav, "Color");
                    AssertNavigationItemExists(nav, "Iconography");
                    AssertNavigationItemExists(nav, "Typography");
                    AssertNavigationItemExists(nav, "Screen reader support");
                    AssertNavigationItemExists(nav, "Keyboard support");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_NavigationPane_OmitsFundamentalsSection()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    var nav = GetDemoNav(window);

                    foreach (var obj in nav.Items)
                    {
                        var header = obj as NavigationViewItemHeader;
                        if (header != null)
                        {
                            Assert.AreNotEqual("Fundamentals", header.Content as string,
                                "The demo navigation should not expose a Fundamentals section.");
                        }

                        var item = obj as NavigationViewItem;
                        if (item != null)
                        {
                            Assert.AreNotEqual("Fundamentals", item.Content as string,
                                "The demo navigation should not expose a Fundamentals section.");
                        }
                    }
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_BasicInputChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    var nav = GetDemoNav(window);
                    var pageTitles = new[]
                    {
                        "Button",
                        "DropDownButton",
                        "HyperlinkButton",
                        "RepeatButton",
                        "ToggleButton",
                        "SplitButton",
                        "CheckBox",
                        "ComboBox",
                        "RadioButton",
                        "RatingControl",
                        "Slider",
                        "ToggleSwitch",
                        "TextBox",
                        "PasswordBox",
                        "NumberBox"
                    };

                    foreach (var pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Basic input child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        var selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        var title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);

                        var sourceLinkCount = 0;
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(selectedContent))
                        {
                            if (!string.Equals(link.Content as string, "Source", StringComparison.Ordinal))
                            {
                                continue;
                            }

                            sourceLinkCount++;
                            Assert.AreEqual(HorizontalAlignment.Right, link.HorizontalAlignment,
                                pageTitle + " source link should be right-aligned.");
                            Assert.IsNotNull(link.Icon, pageTitle + " source link should display a URL/link icon.");
                        }

                        Assert.IsTrue(sourceLinkCount > 0, pageTitle + " should expose a source link.");
                    }
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_StatusInfoChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    var nav = GetDemoNav(window);
                    var pageTitles = new[]
                    {
                        "InfoBadge",
                        "InfoBar",
                        "ProgressBar",
                        "ProgressRing",
                        "PersonPicture"
                    };

                    foreach (var pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Status and info child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        var selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        var title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);

                        var sourceLinkCount = 0;
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(selectedContent))
                        {
                            if (!string.Equals(link.Content as string, "Source", StringComparison.Ordinal))
                            {
                                continue;
                            }

                            sourceLinkCount++;
                            Assert.AreEqual(HorizontalAlignment.Right, link.HorizontalAlignment,
                                pageTitle + " source link should be right-aligned.");
                            Assert.IsNotNull(link.Icon, pageTitle + " source link should display a URL/link icon.");
                        }

                        Assert.IsTrue(sourceLinkCount > 0, pageTitle + " should expose a source link.");
                    }
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_CollectionsChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    var nav = GetDemoNav(window);
                    var pageTitles = new[]
                    {
                        "Card",
                        "ListBox",
                        "ListView",
                        "TreeView"
                    };

                    foreach (var pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Collections child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        var selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        var title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);

                        var sourceLinkCount = 0;
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(selectedContent))
                        {
                            if (!string.Equals(link.Content as string, "Source", StringComparison.Ordinal))
                            {
                                continue;
                            }

                            sourceLinkCount++;
                            Assert.AreEqual(HorizontalAlignment.Right, link.HorizontalAlignment,
                                pageTitle + " source link should be right-aligned.");
                            Assert.IsNotNull(link.Icon, pageTitle + " source link should display a URL/link icon.");
                        }

                        Assert.IsTrue(sourceLinkCount > 0, pageTitle + " should expose a source link.");
                    }
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_MenusToolbarChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    var nav = GetDemoNav(window);
                    var pageTitles = new[]
                    {
                        "Menu",
                        "ContextMenu",
                        "ToolTip"
                    };

                    foreach (var pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Menus and toolbars child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        var selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        var title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);

                        var sourceLinkCount = 0;
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(selectedContent))
                        {
                            if (!string.Equals(link.Content as string, "Source", StringComparison.Ordinal))
                            {
                                continue;
                            }

                            sourceLinkCount++;
                            Assert.AreEqual(HorizontalAlignment.Right, link.HorizontalAlignment,
                                pageTitle + " source link should be right-aligned.");
                            Assert.IsNotNull(link.Icon, pageTitle + " source link should display a URL/link icon.");
                        }

                        Assert.IsTrue(sourceLinkCount > 0, pageTitle + " should expose a source link.");
                    }
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_NavigationChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    var nav = GetDemoNav(window);
                    var pageTitles = new[]
                    {
                        "NavigationView",
                        "TabView"
                    };

                    foreach (var pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Navigation child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        var selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        var title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);

                        var sourceLinkCount = 0;
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(selectedContent))
                        {
                            if (!string.Equals(link.Content as string, "Source", StringComparison.Ordinal))
                            {
                                continue;
                            }

                            sourceLinkCount++;
                            Assert.AreEqual(HorizontalAlignment.Right, link.HorizontalAlignment,
                                pageTitle + " source link should be right-aligned.");
                            Assert.IsNotNull(link.Icon, pageTitle + " source link should display a URL/link icon.");
                        }

                        Assert.IsTrue(sourceLinkCount > 0, pageTitle + " should expose a source link.");
                    }
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_LayoutChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    var nav = GetDemoNav(window);
                    var pageTitles = new[]
                    {
                        "Border",
                        "DockPanel",
                        "Expander",
                        "Separator",
                        "StackPanel"
                    };

                    foreach (var pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Layout child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        var selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        var title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);

                        var sourceLinkCount = 0;
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(selectedContent))
                        {
                            if (!string.Equals(link.Content as string, "Source", StringComparison.Ordinal))
                            {
                                continue;
                            }

                            sourceLinkCount++;
                            Assert.AreEqual(HorizontalAlignment.Right, link.HorizontalAlignment,
                                pageTitle + " source link should be right-aligned.");
                            Assert.IsNotNull(link.Icon, pageTitle + " source link should display a URL/link icon.");
                        }

                        Assert.IsTrue(sourceLinkCount > 0, pageTitle + " should expose a source link.");
                    }
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_TextChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    var nav = GetDemoNav(window);
                    var pageTitles = new[]
                    {
                        "TextBlock"
                    };

                    foreach (var pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Text child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        var selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        var title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);

                        var sourceLinkCount = 0;
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(selectedContent))
                        {
                            if (!string.Equals(link.Content as string, "Source", StringComparison.Ordinal))
                            {
                                continue;
                            }

                            sourceLinkCount++;
                            Assert.AreEqual(HorizontalAlignment.Right, link.HorizontalAlignment,
                                pageTitle + " source link should be right-aligned.");
                            Assert.IsNotNull(link.Icon, pageTitle + " source link should display a URL/link icon.");
                        }

                        Assert.IsTrue(sourceLinkCount > 0, pageTitle + " should expose a source link.");
                    }
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DemoSourceLinks_ResolveLocalAndGitHubUris()
        {
            var settingsType = typeof(MainWindow).Assembly.GetType("Fluence.Wpf.Demo.DemoSourceLinkSettings");
            Assert.IsNotNull(settingsType, "Demo must expose source-link settings for local and GitHub sample links.");

            var localMethod = settingsType.GetMethod("GetLocalSourceUri", BindingFlags.Public | BindingFlags.Static);
            var githubMethod = settingsType.GetMethod("GetGitHubSourceUri", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(localMethod, "Local source-link resolver must exist.");
            Assert.IsNotNull(githubMethod, "GitHub source-link resolver must exist.");

            var samplePath = "Buttons/ButtonAppearances.xaml";
            var local = localMethod.Invoke(null, new object[] { samplePath }) as Uri;
            var github = githubMethod.Invoke(null, new object[] { samplePath }) as Uri;

            Assert.IsNotNull(local, "Local resolver must return a URI.");
            Assert.IsNotNull(github, "GitHub resolver must return a URI.");
            Assert.AreEqual("pack://siteoforigin:,,,/Samples/Buttons/ButtonAppearances.xaml", local.AbsoluteUri);
            Assert.AreEqual(
                "https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf.Demo/Samples/Buttons/ButtonAppearances.xaml",
                github.AbsoluteUri);
        }

        [TestMethod]
        public void DemoSourceSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "ButtonAppearances",
                "ButtonIcons",
                "HyperlinkButtons",
                "DropDownButtons",
                "SplitButtons",
                "ToggleAndRepeatButtons"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Buttons", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Buttons", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceLinks_UseRightAlignedIconPresentation()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var pages = new UserControl[]
                    {
                        new GalleryAccessibilityPage(),
                        new GalleryButtonsPage(),
                        new GalleryColorsPage(),
                        new GalleryDataBindingPage(),
                        new GalleryDataPage(),
                        new GalleryFormsPage(),
                        new GalleryGlyphsPage(),
                        new GalleryInputsPage(),
                        new GalleryMenusPage(),
                        new GalleryNavigationPage(),
                        new GallerySelectionPage(),
                        new GalleryStatusPage(),
                        new GalleryTabsPage(),
                        new GalleryTreesPage(),
                        new GalleryWindowPage()
                    };

                    foreach (var page in pages)
                    {
                        var host = new System.Windows.Controls.Grid();
                        host.Children.Add(page);
                        var window = new Window
                        {
                            Left = -20000,
                            Top = -20000,
                            Width = 1040,
                            Height = 720,
                            WindowStartupLocation = WindowStartupLocation.Manual,
                            ShowInTaskbar = false,
                            Content = host
                        };

                        try
                        {
                            window.Show();
                            Drain(window.Dispatcher);
                            window.UpdateLayout();
                            Drain(window.Dispatcher);

                            var sourceLinkCount = 0;
                            foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                            {
                                if (link.Name == null || !link.Name.EndsWith("SourceLink", StringComparison.Ordinal))
                                {
                                    continue;
                                }

                                sourceLinkCount++;
                                Assert.AreEqual("Source", link.Content as string,
                                    "Source-link content should stay textual for screen readers and existing URI tests.");
                                Assert.AreEqual(HorizontalAlignment.Right, link.HorizontalAlignment,
                                    page.GetType().Name + "." + link.Name + " should be right-aligned.");
                                Assert.IsNotNull(link.Icon,
                                    page.GetType().Name + "." + link.Name + " should display a URL/link icon.");
                            }

                            Assert.IsTrue(sourceLinkCount > 0, page.GetType().Name + " should expose source links.");
                        }
                        finally
                        {
                            window.Close();
                        }
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DemoSourceSelectionSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "CheckBoxStates",
                "RadioButtonGroups",
                "ToggleSwitchStates",
                "ComboBoxSelection"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Selection", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Selection", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Selection sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Selection sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceInputsSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "TextBoxInput",
                "TextBoxValidation",
                "PasswordBoxInput",
                "NumberBoxInput",
                "SliderInput"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Inputs", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Inputs", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Inputs sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Inputs sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceTextSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "TextBlock"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Text", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Text", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Text sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Text sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceFormsSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "SignInForm",
                "CheckoutForm",
                "SettingsForm"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Forms", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Forms", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Forms sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Forms sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceDataSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "ListViewItems",
                "ListViewEmptyState",
                "CardVariants"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Data", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Data", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Data sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Data sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceTreesSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "TreeViewHierarchy",
                "TreeViewSelection",
                "TreeViewExpansion"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Trees", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Trees", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Trees sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Trees sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceNavigationSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "LeftNavigationView",
                "TopNavigationView",
                "CompactNavigationView"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Navigation", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Navigation", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Navigation sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Navigation sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceTabsSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "TabControlBasics",
                "TabControlPlacement",
                "TabViewDocuments"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Tabs", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Tabs", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Tabs sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Tabs sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceMenusSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "MenuBar",
                "ContextMenuActions",
                "ToolTips",
                "DropDownAndSplitButtonMenus"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Menus", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Menus", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Menus sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Menus sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceStatusSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "ProgressBarValue",
                "ProgressBarIndeterminate",
                "ProgressBarSteps",
                "ProgressRings",
                "InfoBars"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Status", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Status", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Status sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Status sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceColorsSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "TextAndAccentBrushes",
                "FillAndSurfaceBrushes",
                "StrokeBrushes",
                "SystemAndHighContrastBrushes"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Colors", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Colors", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Colors sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Colors sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceGlyphsSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "CommonGlyphs",
                "CommandGlyphs",
                "StatusGlyphs"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Glyphs", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Glyphs", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Glyphs sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Glyphs sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceDataBindingSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "ObservableCollectionListView",
                "ListViewSelectionMode",
                "DataTemplateRow"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "DataBinding", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "DataBinding", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "DataBinding sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "DataBinding sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceAccessibilitySamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "FocusAndTabOrder",
                "HighContrastMapping",
                "AutomationProperties",
                "RtlLayout"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Accessibility", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Accessibility", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Accessibility sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Accessibility sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceWindowSamples_CopyToOutput()
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var samplePaths = new[]
            {
                "ThemeAndAccent",
                "BackdropAndCaptionButtons",
                "TitleBarChrome"
            };

            foreach (var samplePath in samplePaths)
            {
                var xaml = Path.Combine(outputDirectory, "Samples", "Window", samplePath + ".xaml");
                var codeBehind = Path.Combine(outputDirectory, "Samples", "Window", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Window sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Window sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void ButtonsPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryButtonsPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Buttons/ButtonAppearances.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Buttons/ButtonIcons.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Buttons/HyperlinkButtons.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Buttons/DropDownButtons.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Buttons/SplitButtons.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Buttons/ToggleAndRepeatButtons.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Buttons page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void InputsPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryInputsPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Inputs/TextBoxInput.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Inputs/TextBoxValidation.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Inputs/PasswordBoxInput.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Inputs/NumberBoxInput.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Inputs/SliderInput.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Inputs page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void FormsPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryFormsPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Forms/SignInForm.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Forms/CheckoutForm.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Forms/SettingsForm.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Forms page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void SelectionPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GallerySelectionPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Selection/CheckBoxStates.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Selection/RadioButtonGroups.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Selection/ToggleSwitchStates.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Selection/ComboBoxSelection.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Selection page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DataPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryDataPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Data/ListViewItems.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Data/ListViewEmptyState.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Data/CardVariants.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Data page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void TreesPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryTreesPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Trees/TreeViewHierarchy.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Trees/TreeViewSelection.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Trees/TreeViewExpansion.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Trees page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryNavigationPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Navigation/LeftNavigationView.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Navigation/TopNavigationView.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Navigation/CompactNavigationView.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Navigation page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void TabsPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryTabsPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Tabs/TabControlBasics.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Tabs/TabControlPlacement.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Tabs/TabViewDocuments.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Tabs page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MenusPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryMenusPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Menus/MenuBar.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Menus/ContextMenuActions.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Menus/ToolTips.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Menus/DropDownAndSplitButtonMenus.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Menus page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void StatusPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryStatusPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Status/ProgressBarValue.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Status/ProgressBarIndeterminate.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Status/ProgressBarSteps.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Status/ProgressRings.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Status/InfoBars.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Status page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void ColorsPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryColorsPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Colors/TextAndAccentBrushes.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Colors/FillAndSurfaceBrushes.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Colors/StrokeBrushes.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Colors/SystemAndHighContrastBrushes.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Colors page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void StatusPage_ProgressRingDemo_UsesDisabledRingAndAlignedLabels()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryStatusPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var disabledRing = FindByName<ProgressRing>(page, "DisabledProgressRing");
                        Assert.IsNotNull(disabledRing, "The third ProgressRing example should be a disabled ring, not an inactive ring.");
                        Assert.IsFalse(disabledRing.IsEnabled, "The disabled ProgressRing example should use IsEnabled=False.");
                        Assert.IsTrue(disabledRing.IsActive, "The disabled ProgressRing should still be active so its disabled visual is visible.");
                        Assert.IsTrue(disabledRing.IsIndeterminate, "The disabled ProgressRing example should use the indeterminate ring visual.");

                        var indeterminateLabel = FindByName<System.Windows.Controls.TextBlock>(page, "IndeterminateProgressRingLabel");
                        var determinateLabel = FindByName<System.Windows.Controls.TextBlock>(page, "DeterminateProgressRingLabel");
                        var disabledLabel = FindByName<System.Windows.Controls.TextBlock>(page, "DisabledProgressRingLabel");
                        Assert.IsNotNull(indeterminateLabel);
                        Assert.IsNotNull(determinateLabel);
                        Assert.IsNotNull(disabledLabel);

                        var indeterminateY = indeterminateLabel.TransformToAncestor(window).Transform(new Point(0, 0)).Y;
                        var determinateY = determinateLabel.TransformToAncestor(window).Transform(new Point(0, 0)).Y;
                        var disabledY = disabledLabel.TransformToAncestor(window).Transform(new Point(0, 0)).Y;

                        Assert.AreEqual(indeterminateY, determinateY, 1.0,
                            "Indeterminate and determinate ProgressRing labels should share the same vertical position.");
                        Assert.AreEqual(indeterminateY, disabledY, 1.0,
                            "Indeterminate and disabled ProgressRing labels should share the same vertical position.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void GlyphsPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryGlyphsPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Glyphs/CommonGlyphs.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Glyphs/CommandGlyphs.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Glyphs/StatusGlyphs.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Glyphs page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DataBindingPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryDataBindingPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("DataBinding/ObservableCollectionListView.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("DataBinding/ListViewSelectionMode.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("DataBinding/DataTemplateRow.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Data Binding page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void AccessibilityPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryAccessibilityPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Accessibility/FocusAndTabOrder.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Accessibility/HighContrastMapping.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Accessibility/AutomationProperties.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Accessibility/RtlLayout.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Accessibility page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void WindowPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryWindowPage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var expected = new[]
                        {
                            DemoSourceLinkSettings.GetSourceUri("Window/ThemeAndAccent.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Window/BackdropAndCaptionButtons.xaml").AbsoluteUri,
                            DemoSourceLinkSettings.GetSourceUri("Window/TitleBarChrome.xaml").AbsoluteUri
                        };

                        var actual = new System.Collections.Generic.List<string>();
                        foreach (var link in FindAllVisualChildren<HyperlinkButton>(page))
                        {
                            if (link.NavigateUri != null && link.Content as string == "Source")
                            {
                                actual.Add(link.NavigateUri.AbsoluteUri);
                            }
                        }

                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Window page example must expose a Source link to its sample XAML.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        // WI-1 Paradigm A: filter must hide NavigationViewItemHeaders whose section becomes empty.
        [TestMethod]
        public void NavSearch_Filter_HidesHeaders_WhenAllSectionItemsFilteredOut()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    var search = GetNavSearchBox(window);
                    var nav = GetDemoNav(window);

                    search.Text = "button";
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    NavigationViewItemHeader anyVisibleEmptyHeader = null;
                    NavigationViewItemHeader currentHeader = null;
                    var currentHeaderHasVisibleChild = false;

                    foreach (var obj in nav.Items)
                    {
                        if (obj is NavigationViewItemHeader header)
                        {
                            if (currentHeader != null && currentHeader.Visibility == Visibility.Visible && !currentHeaderHasVisibleChild)
                            {
                                anyVisibleEmptyHeader = currentHeader;
                                break;
                            }

                            currentHeader = header;
                            currentHeaderHasVisibleChild = false;
                        }
                        else if (obj is NavigationViewItem item && item.Visibility == Visibility.Visible)
                        {
                            currentHeaderHasVisibleChild = true;
                        }
                    }

                    if (anyVisibleEmptyHeader == null && currentHeader != null &&
                        currentHeader.Visibility == Visibility.Visible && !currentHeaderHasVisibleChild)
                    {
                        anyVisibleEmptyHeader = currentHeader;
                    }

                    Assert.IsNull(anyVisibleEmptyHeader,
                        "Filter must collapse headers that have no visible items beneath them. " +
                        "Found a stranded header: " + (anyVisibleEmptyHeader != null ? anyVisibleEmptyHeader.Content as string : "<none>"));
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        // WI-1: The Home page must present a curated "Featured controls" grid.
        [TestMethod]
        public void HomePage_ContainsFeaturedControlsGrid()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var page = new GalleryHomePage();
                    var host = new System.Windows.Controls.Grid();
                    host.Children.Add(page);
                    var window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = host
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var grid = FindByName<FrameworkElement>(page, "FeaturedControlsGrid");
                        Assert.IsNotNull(grid,
                            "GalleryHomePage must contain a FrameworkElement named 'FeaturedControlsGrid' " +
                            "(Paradigm A curated featured controls).");

                        var featuredCards = 0;
                        foreach (var card in FindAllVisualChildren<Fluence.Wpf.Controls.Card>(grid))
                        {
                            if (card.IsClickable)
                            {
                                featuredCards++;
                            }
                        }

                        Assert.IsTrue(featuredCards >= 6,
                            "Featured grid must surface at least 6 curated controls. Found: " + featuredCards);
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        private static T FindByName<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            var asT = root as T;
            if (asT != null && string.Equals(asT.Name, name, StringComparison.Ordinal))
            {
                return asT;
            }

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var hit = FindByName<T>(VisualTreeHelper.GetChild(root, i), name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }

        private static NavigationViewItem AssertNavigationItemExists(NavigationView nav, string content)
        {
            foreach (var obj in nav.Items)
            {
                var item = obj as NavigationViewItem;
                if (item != null && string.Equals(item.Content as string, content, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            Assert.Fail("Navigation item should exist: " + content);
            return null;
        }

        private static System.Collections.Generic.IEnumerable<T> FindAllVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                yield break;
            }

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var descendant in FindAllVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
