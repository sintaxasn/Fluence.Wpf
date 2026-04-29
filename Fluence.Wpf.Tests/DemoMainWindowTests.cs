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
                        if (item != null && item.InfoBadge != null)
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
        public void MainWindow_NavigationCatalog_PlacesTextInputsUnderTextSection()
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

                    var categories = new System.Collections.Generic.List<string>();
                    var basicInputChildren = new System.Collections.Generic.List<string>();
                    var textChildren = new System.Collections.Generic.List<string>();
                    var currentCategory = string.Empty;

                    foreach (var obj in nav.Items)
                    {
                        var item = obj as NavigationViewItem;
                        if (item == null)
                        {
                            continue;
                        }

                        var title = item.Content as string;
                        if (item.InfoBadge != null)
                        {
                            currentCategory = title;
                            categories.Add(title);
                            continue;
                        }

                        if (string.Equals(currentCategory, "Basic input", StringComparison.Ordinal))
                        {
                            basicInputChildren.Add(title);
                        }
                        else if (string.Equals(currentCategory, "Text", StringComparison.Ordinal))
                        {
                            textChildren.Add(title);
                        }
                    }

                    Assert.IsTrue(categories.IndexOf("Status and info") >= 0, "Status and info category should exist.");
                    Assert.IsTrue(categories.IndexOf("Text") >= 0, "Text category should exist.");
                    Assert.IsTrue(categories.IndexOf("Status and info") < categories.IndexOf("Text"),
                        "Status and info should appear above Text.");

                    Assert.IsFalse(basicInputChildren.Contains("TextBox"), "TextBox should move out of Basic input.");
                    Assert.IsFalse(basicInputChildren.Contains("PasswordBox"), "PasswordBox should move out of Basic input.");
                    Assert.IsFalse(basicInputChildren.Contains("NumberBox"), "NumberBox should move out of Basic input.");

                    CollectionAssert.Contains(textChildren, "TextBlock");
                    CollectionAssert.Contains(textChildren, "TextBox");
                    CollectionAssert.Contains(textChildren, "PasswordBox");
                    CollectionAssert.Contains(textChildren, "NumberBox");
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
        public void MainWindow_CategoryHeader_OpensOverviewPageWithChildCards()
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
                    var basicInput = AssertNavigationItemExists(nav, "Basic input");
                    var button = AssertNavigationItemExists(nav, "Button");

                    Assert.AreEqual(Visibility.Collapsed, button.Visibility,
                        "Basic input starts collapsed so selecting its header must reveal children.");

                    nav.SelectedItem = basicInput;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreSame(basicInput, nav.SelectedItem,
                        "Selecting a category header should keep the header selected instead of restoring the last leaf.");
                    Assert.AreEqual(Visibility.Visible, button.Visibility,
                        "Selecting a category header should expand its child pages.");

                    var selectedContent = nav.SelectedContent as DependencyObject;
                    Assert.IsNotNull(selectedContent, "Category headers should open an overview page.");
                    Assert.AreEqual("GalleryCategoryPage", selectedContent.GetType().Name,
                        "Category headers should use the category overview page shell.");

                    var title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "CategoryPageTitle");
                    Assert.IsNotNull(title, "Category overview should expose CategoryPageTitle.");
                    Assert.AreEqual("Basic input", title.Text);

                    Card buttonCard = null;
                    foreach (var card in FindAllVisualChildren<Card>(selectedContent))
                    {
                        if (string.Equals(card.Tag as string, "Button", StringComparison.Ordinal))
                        {
                            buttonCard = card;
                            break;
                        }
                    }

                    Assert.IsNotNull(buttonCard, "Category overview should include a clickable card for Button.");
                    Assert.IsTrue(buttonCard.IsClickable, "Category overview cards should be clickable.");

                    buttonCard.RaiseEvent(new RoutedEventArgs(Card.ClickEvent, buttonCard));
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    var selected = nav.SelectedItem as NavigationViewItem;
                    Assert.IsNotNull(selected, "Clicking a category card should navigate to its child page.");
                    Assert.AreEqual("Button", selected.Content as string);
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
        public void MainWindow_CategoryCardNavigation_ClearsActiveSearchFilter()
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
                    var basicInput = AssertNavigationItemExists(nav, "Basic input");
                    var button = AssertNavigationItemExists(nav, "Button");

                    nav.SelectedItem = basicInput;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    search.Text = "progress";
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.AreEqual("progress", search.Text, "The test must start with an active search filter.");

                    var selectedContent = nav.SelectedContent as DependencyObject;
                    Assert.IsNotNull(selectedContent, "Category overview should remain visible while filtering.");

                    Card buttonCard = null;
                    foreach (var card in FindAllVisualChildren<Card>(selectedContent))
                    {
                        if (string.Equals(card.Tag as string, "Button", StringComparison.Ordinal))
                        {
                            buttonCard = card;
                            break;
                        }
                    }

                    Assert.IsNotNull(buttonCard, "Category overview should include a Button card.");
                    buttonCard.RaiseEvent(new RoutedEventArgs(Card.ClickEvent, buttonCard));
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(string.Empty, search.Text,
                        "Navigating from a category card should clear the active search filter.");
                    Assert.AreSame(button, nav.SelectedItem,
                        "Navigating from a category card should select the matching child item.");
                    Assert.AreEqual(Visibility.Visible, button.Visibility,
                        "The selected child item should be visible after category-card navigation.");
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
        public void MainWindow_PaneCollapse_CollapsesOpenSections()
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
                    var basicInput = AssertNavigationItemExists(nav, "Basic input");
                    var button = AssertNavigationItemExists(nav, "Button");

                    nav.SelectedItem = basicInput;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.AreEqual(Visibility.Visible, button.Visibility,
                        "The test section must be expanded before pane collapse.");

                    nav.IsPaneOpen = false;
                    WaitForAnimationAndDrain(window.Dispatcher, 180);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(Visibility.Collapsed, button.Visibility,
                        "Pane collapse should collapse open category children so compact mode has no child gaps.");
                    var chevron = basicInput.InfoBadge as FontIcon;
                    Assert.IsNotNull(chevron, "Category header should keep its chevron glyph.");
                    Assert.AreEqual("\uE70D", chevron.Glyph, "Pane collapse should reset category chevrons to collapsed.");
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
                        "ToggleSwitch"
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


                        AssertPageHasSourceActions(selectedContent, pageTitle);

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


                        AssertPageHasSourceActions(selectedContent, pageTitle);

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


                        AssertPageHasSourceActions(selectedContent, pageTitle);

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


                        AssertPageHasSourceActions(selectedContent, pageTitle);

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


                        AssertPageHasSourceActions(selectedContent, pageTitle);

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


                        AssertPageHasSourceActions(selectedContent, pageTitle);

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
                        "TextBlock",
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
                        Assert.IsNotNull(selected, "Navigation should select a Text child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        var selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        var title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);


                        AssertPageHasSourceActions(selectedContent, pageTitle);

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
        public void MainWindow_AccessibilityChildren_OpenTopicPages()
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
                        "Screen reader support",
                        "Keyboard support",
                        "Color contrast"
                    };

                    foreach (var pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select an Accessibility child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        var selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the topic page shell.");

                        var title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);


                        AssertPageHasSourceActions(selectedContent, pageTitle);

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
        public void MainWindow_WindowingChildren_OpenSingleControlPages()
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
                        "CaptionButtonChrome",
                        "FluenceWindow",
                        "TitleBar"
                    };

                    foreach (var pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        var selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Windowing child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        var selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        var title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);


                        AssertPageHasSourceActions(selectedContent, pageTitle);

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
                            foreach (var action in FindSourceActionControls(page))
                            {
                                sourceLinkCount++;
                                Assert.AreEqual(HorizontalAlignment.Right, action.HorizontalAlignment,
                                    page.GetType().Name + "." + action.Name + " should be right-aligned.");
                                Assert.IsTrue(ContainsUrlGlyph(action),
                                    page.GetType().Name + "." + action.Name + " should display a URL/link icon.");
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
        public void DemoSourceActions_ClickTargetsUseGitHubUris()
        {
            RunOnSta(() =>
            {
                var app = EnsureApp();
                var dict = MergeTheme(app);

                try
                {
                    var pages = new UserControl[]
                    {
                        new GalleryButtonsPage(),
                        new GalleryControlPage(
                            "Button",
                            "Use Button for immediate actions.",
                            new[]
                            {
                                new DemoExample(
                                    "Default buttons",
                                    "Buttons use standard, accent, subtle, and disabled states.",
                                    "Buttons/ButtonAppearances.xaml",
                                    delegate { return new System.Windows.Controls.TextBlock { Text = "Sample" }; })
                            })
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

                            var sourceUris = CollectSourceActionTargetUris(page);
                            Assert.IsTrue(sourceUris.Count > 0, page.GetType().Name + " should expose clickable source targets.");

                            foreach (var sourceUri in sourceUris)
                            {
                                Assert.IsFalse(sourceUri.StartsWith("pack:", StringComparison.OrdinalIgnoreCase),
                                    "Clickable source targets should not attempt to launch pack URIs.");
                                StringAssert.StartsWith(sourceUri,
                                    DemoSourceLinkSettings.RepositoryUrl + "/blob/" + DemoSourceLinkSettings.RepositoryBranch + "/");
                            }
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
        public void DemoSourceActions_SamplesWithCodeBehindUseDropdownTargets()
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

                        var sourceDropdowns = FindSourceDropDownButtons(page);
                        Assert.AreEqual(6, sourceDropdowns.Count,
                            "Buttons page examples all have code-behind and should use source dropdowns.");

                        foreach (var dropdown in sourceDropdowns)
                        {
                            Assert.AreEqual(HorizontalAlignment.Right, dropdown.HorizontalAlignment,
                                dropdown.Name + " should be right-aligned.");
                            Assert.IsTrue(ContainsUrlGlyph(dropdown),
                                dropdown.Name + " should display a URL/link icon.");

                            var targetUris = CollectSourceActionTargetUris(dropdown);
                            Assert.AreEqual(2, targetUris.Count,
                                dropdown.Name + " should expose XAML and C# source targets.");
                            Assert.IsTrue(targetUris[0].EndsWith(".xaml", StringComparison.OrdinalIgnoreCase),
                                dropdown.Name + " should expose the sample XAML first.");
                            Assert.IsTrue(targetUris[1].EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase),
                                dropdown.Name + " should expose the sample code-behind second.");
                        }
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
        public void DemoSourceActions_XamlOnlySamplesUseAccentButton()
        {
            RunOnSta(() =>
            {
                var action = DemoSourceAction.Create("Experimental/XamlOnly.xaml") as Fluence.Wpf.Controls.Button;
                Assert.IsNotNull(action, "XAML-only samples should use a single source button.");
                Assert.AreEqual(ControlAppearance.Accent, action.Appearance,
                    "XAML-only source buttons should use accent appearance.");
                Assert.AreEqual("Source", action.Content as string);
                Assert.IsTrue(ContainsUrlGlyph(action), "XAML-only source buttons should display a URL/link icon.");

                var target = action.Tag as Uri;
                Assert.IsNotNull(target, "XAML-only source buttons should carry their GitHub target URI.");
                Assert.AreEqual(
                    DemoSourceLinkSettings.GetGitHubSourceUri("Experimental/XamlOnly.xaml").AbsoluteUri,
                    target.AbsoluteUri);
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
                "TitleBarChrome",
                "TitleBar"
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

                        var expected = ExpectedSourceUris(
                            "Buttons/ButtonAppearances.xaml",
                            "Buttons/ButtonIcons.xaml",
                            "Buttons/HyperlinkButtons.xaml",
                            "Buttons/DropDownButtons.xaml",
                            "Buttons/SplitButtons.xaml",
                            "Buttons/ToggleAndRepeatButtons.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Buttons page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Inputs/TextBoxInput.xaml",
                            "Inputs/TextBoxValidation.xaml",
                            "Inputs/PasswordBoxInput.xaml",
                            "Inputs/NumberBoxInput.xaml",
                            "Inputs/SliderInput.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Inputs page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Forms/SignInForm.xaml",
                            "Forms/CheckoutForm.xaml",
                            "Forms/SettingsForm.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Forms page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Selection/CheckBoxStates.xaml",
                            "Selection/RadioButtonGroups.xaml",
                            "Selection/ToggleSwitchStates.xaml",
                            "Selection/ComboBoxSelection.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Selection page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Data/ListViewItems.xaml",
                            "Data/ListViewEmptyState.xaml",
                            "Data/CardVariants.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Data page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Trees/TreeViewHierarchy.xaml",
                            "Trees/TreeViewSelection.xaml",
                            "Trees/TreeViewExpansion.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Trees page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Navigation/LeftNavigationView.xaml",
                            "Navigation/TopNavigationView.xaml",
                            "Navigation/CompactNavigationView.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Navigation page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Tabs/TabControlBasics.xaml",
                            "Tabs/TabControlPlacement.xaml",
                            "Tabs/TabViewDocuments.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Tabs page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Menus/MenuBar.xaml",
                            "Menus/ContextMenuActions.xaml",
                            "Menus/ToolTips.xaml",
                            "Menus/DropDownAndSplitButtonMenus.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Menus page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Status/ProgressBarValue.xaml",
                            "Status/ProgressBarIndeterminate.xaml",
                            "Status/ProgressBarSteps.xaml",
                            "Status/ProgressRings.xaml",
                            "Status/InfoBars.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Status page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Colors/TextAndAccentBrushes.xaml",
                            "Colors/FillAndSurfaceBrushes.xaml",
                            "Colors/StrokeBrushes.xaml",
                            "Colors/SystemAndHighContrastBrushes.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Colors page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Glyphs/CommonGlyphs.xaml",
                            "Glyphs/CommandGlyphs.xaml",
                            "Glyphs/StatusGlyphs.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Glyphs page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "DataBinding/ObservableCollectionListView.xaml",
                            "DataBinding/ListViewSelectionMode.xaml",
                            "DataBinding/DataTemplateRow.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Data Binding page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Accessibility/FocusAndTabOrder.xaml",
                            "Accessibility/HighContrastMapping.xaml",
                            "Accessibility/AutomationProperties.xaml",
                            "Accessibility/RtlLayout.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Accessibility page example must expose source targets to its sample files.");
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

                        var expected = ExpectedSourceUris(
                            "Window/ThemeAndAccent.xaml",
                            "Window/BackdropAndCaptionButtons.xaml",
                            "Window/TitleBarChrome.xaml"
                        );


                        var actual = CollectSourceActionTargetUris(page);


                        CollectionAssert.AreEquivalent(
                            expected,
                            actual,
                            "Each Window page example must expose source targets to its sample files.");
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

        private static void AssertPageHasSourceActions(DependencyObject root, string pageTitle)
        {
            var actions = FindSourceActionControls(root);
            Assert.IsTrue(actions.Count > 0, pageTitle + " should expose a source action.");

            foreach (var action in actions)
            {
                Assert.AreEqual(HorizontalAlignment.Right, action.HorizontalAlignment,
                    pageTitle + " source action should be right-aligned.");
                Assert.IsTrue(ContainsUrlGlyph(action),
                    pageTitle + " source action should display a URL/link icon.");
            }

            var targetUris = CollectSourceActionTargetUris(root);
            Assert.IsTrue(targetUris.Count >= actions.Count,
                pageTitle + " source actions should expose clickable source targets.");
        }

        private static string[] ExpectedSourceUris(params string[] samplePaths)
        {
            var expected = new System.Collections.Generic.List<string>();

            foreach (var samplePath in samplePaths)
            {
                expected.Add(DemoSourceLinkSettings.GetGitHubSourceUri(samplePath).AbsoluteUri);

                var codeBehindPath = samplePath.Replace('\\', '/').Trim('/') + ".cs";
                if (SampleFileExists(codeBehindPath))
                {
                    expected.Add(DemoSourceLinkSettings.GetGitHubSourceUri(codeBehindPath).AbsoluteUri);
                }
            }

            return expected.ToArray();
        }

        private static bool SampleFileExists(string samplePath)
        {
            var outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            var localPath = samplePath.Replace('/', Path.DirectorySeparatorChar);
            return File.Exists(Path.Combine(outputDirectory, "Samples", localPath));
        }

        private static System.Collections.Generic.List<FrameworkElement> FindSourceActionControls(DependencyObject root)
        {
            var actions = new System.Collections.Generic.List<FrameworkElement>();

            foreach (var dropdown in FindSourceDropDownButtons(root))
            {
                actions.Add(dropdown);
            }

            foreach (var button in FindAllVisualChildren<Fluence.Wpf.Controls.Button>(root))
            {
                if (IsSourceActionElement(button))
                {
                    actions.Add(button);
                }
            }

            foreach (var link in FindAllVisualChildren<HyperlinkButton>(root))
            {
                if (IsSourceActionElement(link))
                {
                    actions.Add(link);
                }
            }

            return actions;
        }

        private static System.Collections.Generic.List<DropDownButton> FindSourceDropDownButtons(DependencyObject root)
        {
            var dropdowns = new System.Collections.Generic.List<DropDownButton>();

            foreach (var dropdown in FindAllVisualChildren<DropDownButton>(root))
            {
                if (IsSourceActionElement(dropdown))
                {
                    dropdowns.Add(dropdown);
                }
            }

            return dropdowns;
        }

        private static bool IsSourceActionElement(FrameworkElement element)
        {
            if (element == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(element.Name) && element.Name.EndsWith("SourceLink", StringComparison.Ordinal))
            {
                return true;
            }

            var tagText = element.Tag as string;
            if (!string.IsNullOrEmpty(tagText) && tagText.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var tagUri = element.Tag as Uri;
            if (tagUri != null && string.Equals(GetContentText(element), "Source", StringComparison.Ordinal))
            {
                return true;
            }

            return string.Equals(GetContentText(element), "Source", StringComparison.Ordinal);
        }

        private static string GetContentText(object value)
        {
            var contentControl = value as ContentControl;
            if (contentControl == null)
            {
                return null;
            }

            return contentControl.Content as string;
        }

        private static System.Collections.Generic.List<string> CollectSourceActionTargetUris(DependencyObject root)
        {
            var uris = new System.Collections.Generic.List<string>();
            var rootElement = root as FrameworkElement;
            if (IsSourceActionElement(rootElement))
            {
                AddSourceActionTargetUris(rootElement, uris);
            }

            foreach (var action in FindSourceActionControls(root))
            {
                AddSourceActionTargetUris(action, uris);
            }

            return uris;
        }

        private static void AddSourceActionTargetUris(object value, System.Collections.Generic.List<string> uris)
        {
            if (value == null)
            {
                return;
            }

            var hyperlink = value as HyperlinkButton;
            if (hyperlink != null && hyperlink.NavigateUri != null)
            {
                uris.Add(hyperlink.NavigateUri.AbsoluteUri);
            }

            var button = value as System.Windows.Controls.Button;
            if (button != null)
            {
                var uri = button.Tag as Uri;
                if (uri != null)
                {
                    uris.Add(uri.AbsoluteUri);
                }
            }

            var dropdown = value as DropDownButton;
            if (dropdown != null)
            {
                AddSourceActionTargetUris(dropdown.Flyout, uris);
            }

            var panel = value as Panel;
            if (panel != null)
            {
                foreach (UIElement child in panel.Children)
                {
                    AddSourceActionTargetUris(child, uris);
                }
            }

            var contentControl = value as ContentControl;
            if (contentControl != null)
            {
                AddSourceActionTargetUris(contentControl.Content, uris);
            }
        }

        private static bool ContainsUrlGlyph(object value)
        {
            if (value == null)
            {
                return false;
            }

            var fontIcon = value as FontIcon;
            if (fontIcon != null && string.Equals(fontIcon.Glyph, "\uE71B", StringComparison.Ordinal))
            {
                return true;
            }

            var hyperlink = value as HyperlinkButton;
            if (hyperlink != null && ContainsUrlGlyph(hyperlink.Icon))
            {
                return true;
            }

            var button = value as Fluence.Wpf.Controls.Button;
            if (button != null && ContainsUrlGlyph(button.Icon))
            {
                return true;
            }

            var contentControl = value as ContentControl;
            if (contentControl != null && ContainsUrlGlyph(contentControl.Content))
            {
                return true;
            }

            var panel = value as Panel;
            if (panel != null)
            {
                foreach (UIElement child in panel.Children)
                {
                    if (ContainsUrlGlyph(child))
                    {
                        return true;
                    }
                }
            }

            return false;
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
