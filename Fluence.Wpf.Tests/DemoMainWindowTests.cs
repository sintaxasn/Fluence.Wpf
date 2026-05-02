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
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using FluenceTextBox = Fluence.Wpf.Controls.TextBox;
using System.Collections.ObjectModel;
using System.Collections;
using System.Windows.Controls.Primitives;
using System.Collections.Generic;
using System.Linq;

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
            Collection<ResourceDictionary> dictionaries = application.Resources.MergedDictionaries;
            ResourceDictionary generic = dictionaries.Count > 0 ? dictionaries[dictionaries.Count - 1] : null;

            ResourceDictionary demoShared = new ResourceDictionary
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
            };
            application.Resources.MergedDictionaries.Add(demoShared);

            return generic;
        }

        private static void Drain(Dispatcher dispatcher)
        {
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
        }

        private static void WaitForAnimationAndDrain(Dispatcher dispatcher, int milliseconds)
        {
            DispatcherFrame frame = new DispatcherFrame();
            DispatcherTimer timer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(milliseconds),
                DispatcherPriority.Normal,
                delegate { frame.Continue = false; },
                dispatcher);
            timer.Start();
            Dispatcher.PushFrame(frame);
            timer.Stop();
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
        }

        private static bool WaitUntil(Dispatcher dispatcher, int milliseconds, Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(milliseconds);

            do
            {
                _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
                if (condition())
                {
                    return true;
                }

                DispatcherFrame frame = new DispatcherFrame();
                DispatcherTimer timer = new DispatcherTimer(
                    TimeSpan.FromMilliseconds(16),
                    DispatcherPriority.Normal,
                    delegate { frame.Continue = false; },
                    dispatcher);
                timer.Start();
                Dispatcher.PushFrame(frame);
                timer.Stop();
            }
            while (DateTime.UtcNow < deadline);

            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
            return condition();
        }

        private static void AssertChevronRotationAnimationStarts(
            Dispatcher dispatcher,
            FontIcon chevron,
            double startRotation,
            double targetRotation,
            string message)
        {
            Assert.IsTrue(
                WaitUntil(dispatcher, 160, () =>
                    chevron.HasAnimatedProperties ||
                    (Math.Abs(chevron.Rotation - startRotation) > 0.01 &&
                     Math.Abs(chevron.Rotation - targetRotation) > 0.01)),
                message);
        }

        private static void AssertChevronRotationSettles(
            Dispatcher dispatcher,
            FontIcon chevron,
            double targetRotation,
            string message)
        {
            _ = WaitUntil(dispatcher, 1000, () => Math.Abs(chevron.Rotation - targetRotation) <= 0.01);
            Assert.AreEqual(targetRotation, chevron.Rotation, 0.01, message);
        }

        private static void AssertVisibilitySettles(
            Dispatcher dispatcher,
            UIElement element,
            Visibility expected,
            string message)
        {
            _ = WaitUntil(dispatcher, 1000, () => element.Visibility == expected);
            Assert.AreEqual(expected, element.Visibility, message);
        }

        private static bool TrySetClipboardTextWithRetry(string text)
        {
            const int RetryCount = 25;
            const int RetryDelayMilliseconds = 20;
            const int ClipboardCannotOpen = unchecked((int)0x800401D0);

            for (int attempt = 0; attempt < RetryCount; attempt++)
            {
                try
                {
                    Clipboard.SetText(text);
                    return true;
                }
                catch (COMException ex)
                {
                    if (ex.HResult != ClipboardCannotOpen)
                    {
                        throw;
                    }

                    if (attempt == RetryCount - 1)
                    {
                        return false;
                    }

                    Thread.Sleep(RetryDelayMilliseconds);
                }
            }

            return false;
        }

        private static bool ClipboardTextEquals(string expected)
        {
            const int ClipboardCannotOpen = unchecked((int)0x800401D0);

            try
            {
                return string.Equals(Clipboard.GetText(), expected, StringComparison.Ordinal);
            }
            catch (COMException ex)
            {
                if (ex.HResult == ClipboardCannotOpen)
                {
                    return false;
                }

                throw;
            }
        }

        private static MainWindow CreateShownMainWindow()
        {
            MainWindow window = new MainWindow
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
            FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
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
            KeyEventArgs args = new KeyEventArgs(
                Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(source),
                0,
                key)
            {
                RoutedEvent = Keyboard.PreviewKeyDownEvent
            };
            source.RaiseEvent(args);
        }

        private static void RaisePreviewMouseLeftButtonDown(FrameworkElement source)
        {
            MouseButtonEventArgs args = new MouseButtonEventArgs(
                Mouse.PrimaryDevice,
                Environment.TickCount,
                MouseButton.Left)
            {
                RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                Source = source
            };
            source.RaiseEvent(args);
        }

        // WI-1 F3: Enter in the search box must select the top visible match.
        [TestMethod]
        public void NavSearch_EnterKey_SelectsTopMatch()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    FluenceTextBox search = GetNavSearchBox(window);
                    NavigationView nav = GetDemoNav(window);

                    _ = search.Focus();
                    search.Text = "button";
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    RaisePreviewKeyDown(search, Key.Enter);
                    Drain(window.Dispatcher);

                    NavigationViewItem selected = nav.SelectedItem as NavigationViewItem;
                    Assert.IsNotNull(selected, "Enter must select a NavigationViewItem.");
                    Assert.AreEqual("Button", selected.Content as string,
                        "Top match for 'button' must be the 'Button' page.");
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
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
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    FluenceTextBox search = GetNavSearchBox(window);
                    NavigationView nav = GetDemoNav(window);

                    object before = nav.SelectedItem;

                    search.Text = string.Empty;
                    Drain(window.Dispatcher);
                    RaisePreviewKeyDown(search, Key.Enter);
                    Drain(window.Dispatcher);

                    Assert.AreSame(before, nav.SelectedItem,
                        "Empty Enter must be a no-op for selection.");
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
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
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    FluenceTextBox search = GetNavSearchBox(window);
                    NavigationView nav = GetDemoNav(window);

                    object before = nav.SelectedItem;
                    search.Text = "zzz-no-such-token-zzz";
                    Drain(window.Dispatcher);

                    RaisePreviewKeyDown(search, Key.Enter);
                    Drain(window.Dispatcher);

                    Assert.AreSame(before, nav.SelectedItem,
                        "Enter with zero matches must not reset or randomise the selection.");
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
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
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    FluenceTextBox search = GetNavSearchBox(window);
                    NavigationView nav = GetDemoNav(window);

                    search.Text = "button";
                    Drain(window.Dispatcher);

                    search.Text = string.Empty;
                    Drain(window.Dispatcher);

                    NavigationViewItem selected = nav.SelectedItem as NavigationViewItem;
                    Assert.IsNotNull(selected, "The search result should remain selected before clearing.");

                    foreach (object obj in nav.Items)
                    {
                        if (obj is NavigationViewItemHeader header)
                        {
                            Assert.AreEqual(Visibility.Visible, header.Visibility,
                                "Empty query must restore navigation headers to Visible.");
                            continue;
                        }

                        if (!(obj is FrameworkElement el))
                        {
                            continue;
                        }

                        if (el is NavigationViewItem item && item.InfoBadge != null)
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
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
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
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);

                    List<string> headers = new List<string>();
                    List<string> parentItems = new List<string>();
                    NavigationViewItem buttonItem = null;
                    foreach (object obj in nav.Items)
                    {
                        if (obj is NavigationViewItemHeader header)
                        {
                            headers.Add(header.Content as string);
                        }

                        if (!(obj is NavigationViewItem item))
                        {
                            continue;
                        }

                        string title = item.Content as string;
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

                    _ = AssertNavigationItemExists(nav, "Color");
                    _ = AssertNavigationItemExists(nav, "Iconography");
                    _ = AssertNavigationItemExists(nav, "Typography");
                    _ = AssertNavigationItemExists(nav, "Screen reader support");
                    _ = AssertNavigationItemExists(nav, "Keyboard support");
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_NavigationPane_OmitsFundamentalsSection()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);

                    foreach (object obj in nav.Items)
                    {
                        if (obj is NavigationViewItemHeader header)
                        {
                            Assert.AreNotEqual("Fundamentals", header.Content as string,
                                "The demo navigation should not expose a Fundamentals section.");
                        }

                        if (obj is NavigationViewItem item)
                        {
                            Assert.AreNotEqual("Fundamentals", item.Content as string,
                                "The demo navigation should not expose a Fundamentals section.");
                        }
                    }
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_NavigationCatalog_PlacesTextInputsUnderTextSection()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);

                    List<string> categories = new List<string>();
                    List<string> basicInputChildren = new List<string>();
                    List<string> textChildren = new List<string>();
                    string currentCategory = string.Empty;

                    foreach (object obj in nav.Items)
                    {
                        if (!(obj is NavigationViewItem item))
                        {
                            continue;
                        }

                        string title = item.Content as string;
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
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_NavigationCatalog_MarksOnlySectionChildrenAsChildItems()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    string currentCategory = string.Empty;

                    foreach (object obj in nav.Items)
                    {
                        if (obj is NavigationViewItemHeader header)
                        {
                            currentCategory = string.Empty;
                            continue;
                        }

                        if (!(obj is NavigationViewItem item))
                        {
                            continue;
                        }

                        if (item.InfoBadge != null)
                        {
                            currentCategory = item.Content as string;
                            Assert.IsFalse(item.IsChildItem,
                                "Section headers should keep top-level indicator positioning: " + item.Content);
                            continue;
                        }

                        if (string.IsNullOrEmpty(currentCategory))
                        {
                            Assert.IsFalse(item.IsChildItem,
                                "Top-level navigation items should not be marked as child items: " + item.Content);
                            continue;
                        }

                        Assert.IsTrue(item.IsChildItem,
                            "Navigation children should be explicitly marked so selected indicators indent with the child row: " + item.Content);
                    }
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_NavigationHeaders_StartCollapsed()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    bool sawCategoryHeader = false;

                    foreach (object obj in nav.Items)
                    {
                        if (!(obj is NavigationViewItem item))
                        {
                            continue;
                        }

                        if (item.InfoBadge is FontIcon icon)
                        {
                            sawCategoryHeader = true;
                            FontIcon chevron = icon;
                            Assert.AreEqual(0.0, chevron.Rotation, 0.01,
                                "Every category chevron should start collapsed.");
                            continue;
                        }

                        if (item.IsChildItem)
                        {
                            Assert.AreEqual(Visibility.Collapsed, item.Visibility,
                                "Category child pages should start collapsed until the header is expanded.");
                        }
                    }

                    Assert.IsTrue(sawCategoryHeader, "The navigation pane should include expandable category headers.");
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_CategoryHeader_OpensOverviewPageWithChildCards()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    NavigationViewItem basicInput = AssertNavigationItemExists(nav, "Basic input");
                    NavigationViewItem button = AssertNavigationItemExists(nav, "Button");

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

                    DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                    Assert.IsNotNull(selectedContent, "Category headers should open an overview page.");
                    Assert.AreEqual("GalleryCategoryPage", selectedContent.GetType().Name,
                        "Category headers should use the category overview page shell.");

                    System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "CategoryPageTitle");
                    Assert.IsNotNull(title, "Category overview should expose CategoryPageTitle.");
                    Assert.AreEqual("Basic input", title.Text);

                    Card buttonCard = null;
                    foreach (Card card in FindAllVisualChildren<Card>(selectedContent))
                    {
                        if (string.Equals(card.Tag as string, "Button", StringComparison.Ordinal))
                        {
                            buttonCard = card;
                            break;
                        }
                    }

                    Assert.IsNotNull(buttonCard, "Category overview should include a clickable card for Button.");
                    Assert.IsTrue(buttonCard.IsClickable, "Category overview cards should be clickable.");
                    Assert.AreEqual(300.0, buttonCard.Width,
                        "Category cards should match the WinUI Gallery overview card width.");
                    Assert.AreEqual(96.0, buttonCard.Height,
                        "Category cards should match the WinUI Gallery overview card height.");

                    Image buttonCardImage = null;
                    foreach (Image image in FindAllVisualChildren<Image>(buttonCard))
                    {
                        buttonCardImage = image;
                        break;
                    }
                    Assert.IsNotNull(buttonCardImage, "Category cards should show the WinUI Gallery control asset.");
                    Assert.AreEqual(32.0, buttonCardImage.Width,
                        "Category card images should match the WinUI Gallery 32px icon slot.");
                    Assert.IsNotNull(buttonCardImage.Source, "Category card images should resolve to a WPF resource.");
                    StringAssert.Contains(buttonCardImage.Source.ToString(), "/Resources/ControlImages/Button.png");

                    ArrayList cardTexts = CollectTextBlockTexts(buttonCard);
                    CollectionAssert.Contains(cardTexts, "Button",
                        "Category cards should show the child page title.");
                    CollectionAssert.Contains(cardTexts, "A control that responds to user input and raises a Click event.",
                        "Category cards should show the WinUI Gallery card subtitle.");

                    buttonCard.RaiseEvent(new RoutedEventArgs(Card.ClickEvent, buttonCard));
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    NavigationViewItem selected = nav.SelectedItem as NavigationViewItem;
                    Assert.IsNotNull(selected, "Clicking a category card should navigate to its child page.");
                    Assert.AreEqual("Button", selected.Content as string);
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_CategoryHeader_AnimatesChevronRotation()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    NavigationViewItem home = AssertNavigationItemExists(nav, "Home");
                    NavigationViewItem basicInput = AssertNavigationItemExists(nav, "Basic input");
                    FontIcon chevron = basicInput.InfoBadge as FontIcon;

                    Assert.IsNotNull(chevron, "Category headers should expose a FontIcon chevron.");
                    Assert.AreEqual("\uE70D", chevron.Glyph,
                        "Category chevrons should use the down glyph and rotate rather than swapping glyphs.");

                    nav.IsPaneOpen = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    nav.IsPaneOpen = false;
                    WaitForAnimationAndDrain(window.Dispatcher, 180);
                    nav.IsPaneOpen = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(0.0, chevron.Rotation, 0.01,
                        "Collapsed category chevron should start at 0 degrees.");

                    nav.SelectedItem = basicInput;
                    AssertChevronRotationAnimationStarts(
                        window.Dispatcher,
                        chevron,
                        0.0,
                        180.0,
                        "Expanding a category should animate the chevron rotation.");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    AssertChevronRotationSettles(window.Dispatcher, chevron, 180.0,
                        "Expanded category chevron should settle at 180 degrees.");

                    Assert.AreEqual(180.0, chevron.Rotation, 0.01,
                        "Category chevron should be expanded before the collapse click.");
                    RaisePreviewMouseLeftButtonDown(basicInput);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    AssertChevronRotationSettles(window.Dispatcher, chevron, 0.0,
                        "Collapsed category chevron should settle back at 0 degrees.");
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_AllControlsItem_OpensAllControlsCategoryPage()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    NavigationViewItem all = AssertNavigationItemExists(nav, "All");

                    nav.SelectedItem = all;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                    Assert.IsNotNull(selectedContent, "All controls should show a real page.");
                    Assert.AreEqual("GalleryCategoryPage", selectedContent.GetType().Name,
                        "All controls should use the same category overview shell as regular control groups.");

                    System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "CategoryPageTitle");
                    Assert.IsNotNull(title, "All controls page should expose CategoryPageTitle.");
                    Assert.AreEqual("All controls", title.Text);

                    ArrayList texts = CollectTextBlockTexts(selectedContent);
                    CollectionAssert.Contains(texts, "Button",
                        "All controls page should list implemented controls, not the home page featured grid.");
                    Assert.IsNull(FindByName<FrameworkElement>(selectedContent, "FeaturedControlsGrid"),
                        "All controls should not route back to the home page.");
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_CategoryCardNavigation_ClearsActiveSearchFilter()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    FluenceTextBox search = GetNavSearchBox(window);
                    NavigationView nav = GetDemoNav(window);
                    NavigationViewItem basicInput = AssertNavigationItemExists(nav, "Basic input");
                    NavigationViewItem button = AssertNavigationItemExists(nav, "Button");

                    nav.SelectedItem = basicInput;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    search.Text = "progress";
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.AreEqual("progress", search.Text, "The test must start with an active search filter.");

                    DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                    Assert.IsNotNull(selectedContent, "Category overview should remain visible while filtering.");

                    Card buttonCard = null;
                    foreach (Card card in FindAllVisualChildren<Card>(selectedContent))
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
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_TabFromSelectedNavigationItem_FocusesSearchThenSelectedPage()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    FluenceTextBox search = GetNavSearchBox(window);
                    NavigationView nav = GetDemoNav(window);
                    NavigationViewItem button = AssertNavigationItemExists(nav, "Button");

                    nav.SelectedItem = button;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.IsTrue(button.Focus(), "The selected navigation item should be focusable.");
                    Drain(window.Dispatcher);
                    Assert.AreSame(button, Keyboard.FocusedElement,
                        "The test must start with keyboard focus on the selected navigation item.");

                    RaisePreviewKeyDown(button, Key.Tab);
                    Drain(window.Dispatcher);
                    Assert.AreSame(search, Keyboard.FocusedElement,
                        "Tab from the selected navigation item should move to search.");

                    RaisePreviewKeyDown(search, Key.Tab);
                    Drain(window.Dispatcher);

                    DependencyObject focused = Keyboard.FocusedElement as DependencyObject;
                    DependencyObject selectedPage = nav.SelectedContent as DependencyObject;
                    Assert.IsNotNull(selectedPage, "The selected navigation item should have page content.");
                    Assert.IsTrue(IsDescendantOrSelf(focused, selectedPage),
                        "Tab from search should move into the selected page content.");
                }
                finally
                {
                    window?.Close();

                    if (dict != null && app.Resources.MergedDictionaries.Contains(dict))
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_PaneCollapse_CollapsesOpenSections()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    NavigationViewItem basicInput = AssertNavigationItemExists(nav, "Basic input");
                    NavigationViewItem button = AssertNavigationItemExists(nav, "Button");

                    nav.SelectedItem = basicInput;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.AreEqual(Visibility.Visible, button.Visibility,
                        "The test section must be expanded before pane collapse.");

                    nav.IsPaneOpen = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    nav.IsPaneOpen = false;
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    AssertVisibilitySettles(window.Dispatcher, button, Visibility.Collapsed,
                        "Pane collapse should collapse open category children so compact mode has no child gaps.");
                    FontIcon chevron = basicInput.InfoBadge as FontIcon;
                    Assert.IsNotNull(chevron, "Category header should keep its chevron glyph.");
                    Assert.AreEqual("\uE70D", chevron.Glyph, "Pane collapse should reset category chevrons to collapsed.");
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_CollapsedParentClick_OpensChildFlyoutWithoutInlineChildren()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    NavigationViewItem basicInput = AssertNavigationItemExists(nav, "Basic input");
                    NavigationViewItem button = AssertNavigationItemExists(nav, "Button");

                    nav.IsPaneOpen = false;
                    WaitForAnimationAndDrain(window.Dispatcher, 180);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    RaisePreviewMouseLeftButtonDown(basicInput);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Popup popup = GetPrivateField<Popup>(window, "_compactSectionPopup");
                    Assert.IsNotNull(popup, "Clicking a collapsed parent section should create the child flyout popup.");
                    Assert.IsTrue(popup.IsOpen, "Clicking a collapsed parent section should open its child flyout.");
                    Assert.AreSame(basicInput, popup.PlacementTarget,
                        "The compact child flyout should be positioned beside the clicked parent item.");
                    Assert.AreEqual(Visibility.Collapsed, button.Visibility,
                        "Inline child items should stay collapsed while compact flyout children are shown.");

                    ArrayList popupTexts = CollectTextBlockTexts(popup.Child);
                    CollectionAssert.Contains(popupTexts, "Button");
                    CollectionAssert.Contains(popupTexts, "DropDownButton");
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_CollapsedChildFlyoutClick_NavigatesButKeepsParentSelected()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    NavigationViewItem basicInput = AssertNavigationItemExists(nav, "Basic input");
                    NavigationViewItem button = AssertNavigationItemExists(nav, "Button");

                    nav.IsPaneOpen = false;
                    WaitForAnimationAndDrain(window.Dispatcher, 180);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    RaisePreviewMouseLeftButtonDown(basicInput);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Popup popup = GetPrivateField<Popup>(window, "_compactSectionPopup");
                    Controls.Button popupButton = FindCompactFlyoutButton(popup.Child, "Button");
                    Assert.IsNotNull(popupButton, "The Basic input flyout should include a Button row.");

                    popupButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, popupButton));
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.IsFalse(popup.IsOpen, "Selecting a compact flyout child should close the flyout.");
                    Assert.AreSame(basicInput, nav.SelectedItem,
                        "The compact rail should keep the parent glyph selected after child navigation.");
                    Assert.AreEqual(Visibility.Collapsed, button.Visibility,
                        "The hidden child item should stay collapsed after compact flyout navigation.");

                    DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                    Assert.IsNotNull(selectedContent, "Compact flyout child navigation should show the child page content.");
                    System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                    Assert.IsNotNull(title, "The Button child page should expose ControlPageTitle.");
                    Assert.AreEqual("Button", title.Text);
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_NavigateTo_CompactChildRoutesThroughParentSelection()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    NavigationViewItem basicInput = AssertNavigationItemExists(nav, "Basic input");
                    NavigationViewItem button = AssertNavigationItemExists(nav, "Button");

                    nav.IsPaneOpen = false;
                    WaitForAnimationAndDrain(window.Dispatcher, 180);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    window.NavigateTo("Button");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreSame(basicInput, nav.SelectedItem,
                        "Programmatic compact navigation to a child should keep the parent rail item selected.");
                    Assert.AreEqual(Visibility.Collapsed, button.Visibility,
                        "Programmatic compact navigation should not reveal inline child rows.");

                    DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                    Assert.IsNotNull(selectedContent, "Compact programmatic navigation should show child page content.");
                    System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                    Assert.IsNotNull(title, "The Button child page should expose ControlPageTitle.");
                    Assert.AreEqual("Button", title.Text);
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void NavSearch_CompactPane_SearchOpensAndFocusedClearRestoresPaneState()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    FluenceTextBox search = GetNavSearchBox(window);

                    nav.IsPaneOpen = false;
                    WaitForAnimationAndDrain(window.Dispatcher, 180);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);
                    Assert.IsFalse(nav.IsPaneOpen, "The test must start from compact closed navigation.");

                    Assert.IsTrue(search.Focus(), "Search box should be focusable in the title bar.");
                    search.Text = "button";
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.IsTrue(nav.IsPaneOpen,
                        "Typing a search query while the pane is closed should open the pane for visible results.");

                    search.Text = string.Empty;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.IsFalse(nav.IsPaneOpen,
                        "Clearing a query that opened the pane should restore the compact closed state while search remains focused.");
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_PageAnimation_DoesNotRestartForSamePageContent()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);

                    window.NavigateTo("Button");
                    WaitForAnimationAndDrain(window.Dispatcher, 360);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    UIElement selectedContent = nav.SelectedContent as UIElement;
                    Assert.IsNotNull(selectedContent, "Navigation should show UIElement page content.");
                    Assert.AreSame(selectedContent, GetPrivateField<object>(window, "_lastAnimatedPageContent"),
                        "The animation tracker should store the last page content it animated.");

                    selectedContent.Opacity = 0.37;
                    window.NavigateTo("Button");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(0.37, selectedContent.Opacity, 0.001,
                        "Navigating to the same page content should not restart the page-in animation.");
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_BasicInputChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    string[] pageTitles = new[]
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

                    foreach (string pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        NavigationViewItem selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Basic input child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);


                        AssertPageUsesInlineSourceSamples(selectedContent, pageTitle);

                    }
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_StatusInfoChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    string[] pageTitles = new[]
                    {
                        "InfoBadge",
                        "InfoBar",
                        "ProgressBar",
                        "ProgressRing",
                        "PersonPicture"
                    };

                    foreach (string pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        NavigationViewItem selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Status and info child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);


                        AssertPageUsesInlineSourceSamples(selectedContent, pageTitle);

                    }
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_CollectionsChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    string[] pageTitles = new[]
                    {
                        "Card",
                        "ListBox",
                        "ListView",
                        "TreeView"
                    };

                    foreach (string pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        NavigationViewItem selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Collections child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);


                        AssertPageUsesInlineSourceSamples(selectedContent, pageTitle);

                    }
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_MenusToolbarChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    string[] pageTitles = new[]
                    {
                        "Menu",
                        "ContextMenu",
                        "ToolTip"
                    };

                    foreach (string pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        NavigationViewItem selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Menus and toolbars child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);


                        AssertPageUsesInlineSourceSamples(selectedContent, pageTitle);

                    }
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_NavigationChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    string[] pageTitles = new[]
                    {
                        "NavigationView",
                        "TabView"
                    };

                    foreach (string pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        NavigationViewItem selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Navigation child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);


                        AssertPageUsesInlineSourceSamples(selectedContent, pageTitle);

                    }
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_LayoutChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    string[] pageTitles = new[]
                    {
                        "Border",
                        "DockPanel",
                        "Expander",
                        "Separator",
                        "StackPanel"
                    };

                    foreach (string pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        NavigationViewItem selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Layout child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);


                        AssertPageUsesInlineSourceSamples(selectedContent, pageTitle);

                    }
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_TextChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    string[] pageTitles = new[]
                    {
                        "TextBlock",
                        "TextBox",
                        "PasswordBox",
                        "NumberBox"
                    };

                    foreach (string pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        NavigationViewItem selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Text child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);


                        AssertPageUsesInlineSourceSamples(selectedContent, pageTitle);

                    }
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_AccessibilityChildren_OpenTopicPages()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    string[] pageTitles = new[]
                    {
                        "Screen reader support",
                        "Keyboard support",
                        "Color contrast"
                    };

                    foreach (string pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        NavigationViewItem selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select an Accessibility child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the topic page shell.");

                        System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);


                        AssertPageUsesInlineSourceSamples(selectedContent, pageTitle);

                    }
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void KeyboardSupportPage_CenterAlignsControlRows()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    window.NavigateTo("Keyboard support");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    DependencyObject selectedContent = GetDemoNav(window).SelectedContent as DependencyObject;
                    Assert.IsNotNull(selectedContent, "Keyboard support should open a page object.");

                    Grid primaryControls = FindByName<Grid>(selectedContent, "KeyboardSupportPrimaryControls");
                    AssertCenteredChildren(primaryControls, "Primary keyboard support controls");
                    Assert.IsTrue(GetMaxGridColumn(primaryControls) <= 3,
                        "Primary keyboard support controls should not exceed four grid columns.");
                    Assert.IsTrue(GetMaxGridRow(primaryControls) >= 1,
                        "Primary keyboard support controls should wrap to a second row instead of clipping horizontally.");
                    AssertCenteredChildren(
                        FindByName<Grid>(selectedContent, "KeyboardSupportExplicitOrderControls"),
                        "Explicit tab order controls");
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MainWindow_WindowingChildren_OpenSingleControlPages()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);
                MainWindow window = null;

                try
                {
                    window = CreateShownMainWindow();
                    NavigationView nav = GetDemoNav(window);
                    string[] pageTitles = new[]
                    {
                        "CaptionButtonChrome",
                        "FluenceWindow",
                        "TitleBar"
                    };

                    foreach (string pageTitle in pageTitles)
                    {
                        window.NavigateTo(pageTitle);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        NavigationViewItem selected = nav.SelectedItem as NavigationViewItem;
                        Assert.IsNotNull(selected, "Navigation should select a Windowing child for " + pageTitle + ".");
                        Assert.AreEqual(pageTitle, selected.Content as string);

                        DependencyObject selectedContent = nav.SelectedContent as DependencyObject;
                        Assert.IsNotNull(selectedContent, pageTitle + " should open a page object.");
                        Assert.AreEqual("GalleryControlPage", selectedContent.GetType().Name,
                            pageTitle + " should use the single-control page shell.");

                        System.Windows.Controls.TextBlock title = FindByName<System.Windows.Controls.TextBlock>(selectedContent, "ControlPageTitle");
                        Assert.IsNotNull(title, pageTitle + " page should expose ControlPageTitle.");
                        Assert.AreEqual(pageTitle, title.Text);


                        AssertPageUsesInlineSourceSamples(selectedContent, pageTitle);

                    }
                }
                finally
                {
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DemoSourceLinks_ResolveLocalAndGitHubUris()
        {
            Type settingsType = typeof(MainWindow).Assembly.GetType("Fluence.Wpf.Demo.DemoSourceLinkSettings");
            Assert.IsNotNull(settingsType, "Demo must expose source-link settings for local and GitHub sample links.");

            MethodInfo localMethod = settingsType.GetMethod("GetLocalSourceUri", BindingFlags.Public | BindingFlags.Static);
            MethodInfo githubMethod = settingsType.GetMethod("GetGitHubSourceUri", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(localMethod, "Local source-link resolver must exist.");
            Assert.IsNotNull(githubMethod, "GitHub source-link resolver must exist.");

            string samplePath = "Buttons/ButtonAppearances.xaml";
            Uri local = localMethod.Invoke(null, new object[] { samplePath }) as Uri;
            Uri github = githubMethod.Invoke(null, new object[] { samplePath }) as Uri;

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
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "ButtonAppearances",
                "ButtonIcons",
                "HyperlinkButtons",
                "DropDownButtons",
                "SplitButtons",
                "ToggleAndRepeatButtons"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Buttons", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Buttons", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceLinks_UseRightAlignedIconPresentation()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    UserControl[] pages = new UserControl[]
                    {
                        new GalleryAccessibilityPage(),
                        new GalleryButtonsPage(),
                        new GalleryDataBindingPage(),
                        new GalleryDataPage(),
                        new GalleryFormsPage(),
                        new GalleryInputsPage(),
                        new GalleryMenusPage(),
                        new GalleryNavigationPage(),
                        new GallerySelectionPage(),
                        new GalleryStatusPage(),
                        new GalleryTabsPage(),
                        new GalleryTreesPage(),
                        new GalleryWindowPage()
                    };

                    foreach (UserControl page in pages)
                    {
                        Grid host = new Grid();
                        _ = host.Children.Add(page);
                        Window window = new Window
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

                            int sourceLinkCount = 0;
                            foreach (FrameworkElement action in FindSourceActionControls(page))
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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DemoSourceActions_ClickTargetsUseGitHubUris()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    UserControl[] pages = new UserControl[]
                    {
                        new GalleryButtonsPage()
                    };

                    foreach (UserControl page in pages)
                    {
                        Grid host = new Grid();
                        _ = host.Children.Add(page);
                        Window window = new Window
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

                            List<string> sourceUris = CollectSourceActionTargetUris(page);
                            Assert.IsTrue(sourceUris.Count > 0, page.GetType().Name + " should expose clickable source targets.");

                            foreach (string sourceUri in sourceUris)
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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void GalleryControlPage_ExamplesUseInlineSourceExpander()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryControlPage page = new GalleryControlPage(
                        "Button",
                        "Use Button for immediate actions.",
                        new[]
                        {
                            new DemoExample(
                                "Default buttons",
                                "Buttons use standard, accent, subtle, and disabled states.",
                                "Selection/ComboBoxSelection.xaml",
                                delegate { return new System.Windows.Controls.TextBlock { Text = "Sample" }; })
                        });

                    Window window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 1040,
                        Height = 720,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = page
                    };

                    try
                    {
                        window.Show();
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        List<FrameworkElement> actions = FindSourceActionControls(page);
                        Assert.AreEqual(0, actions.Count,
                            "Generated control pages should display source inline rather than as top-right source buttons.");

                        DemoSampleControl sample = null;
                        foreach (DemoSampleControl candidate in FindAllVisualChildren<DemoSampleControl>(page))
                        {
                            sample = candidate;
                            break;
                        }

                        Assert.IsNotNull(sample, "Generated control page examples should be hosted by DemoSampleControl.");

                        Card sampleCard = FindByName<Card>(sample, "SampleCard");
                        Assert.IsNotNull(sampleCard, "DemoSampleControl should expose the sample card surface.");
                        Assert.AreEqual(0.0, sampleCard.CornerRadius.BottomLeft,
                            "The sample card should remove its lower-left radius when it is joined to the source expander.");
                        Assert.AreEqual(0.0, sampleCard.CornerRadius.BottomRight,
                            "The sample card should remove its lower-right radius when it is joined to the source expander.");

                        Controls.Expander expander = FindByName<Controls.Expander>(sample, "SourceExpander");
                        Assert.IsNotNull(expander, "DemoSampleControl should expose a source expander.");
                        Assert.IsFalse(expander.IsExpanded, "Source should stay collapsed until the user asks for it.");
                        Assert.AreEqual(0.0, expander.Margin.Top,
                            "The source expander should be flush against the sample card.");
                        Assert.AreEqual(0.0, expander.BorderThickness.Top,
                            "The source expander should not add a second border between the sample card and source area.");
                        Assert.AreEqual(0.0, expander.CornerRadius.TopLeft,
                            "The source expander should remove its upper-left radius when it is joined to the sample card.");
                        Assert.AreEqual(0.0, expander.CornerRadius.TopRight,
                            "The source expander should remove its upper-right radius when it is joined to the sample card.");

                        expander.IsExpanded = true;
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        TabView tabs = FindByName<TabView>(sample, "SourceTabs");
                        Assert.IsNotNull(tabs, "Expanded sample source should be shown in a styled TabView.");
                        Assert.IsFalse(tabs.IsAddTabButtonVisible,
                            "Sample source TabViews should not show an add-tab affordance.");
                        Assert.AreEqual(2, tabs.Items.Count,
                            "Samples with code-behind should display XAML and C# Code-behind tabs.");

                        int sourcePanes = 0;
                        bool sawXaml = false;
                        bool sawCodeBehind = false;
                        bool sawCopyButton = false;
                        bool verifiedCopyButton = false;
                        foreach (object obj in tabs.Items)
                        {
                            TabViewItem tabItem = obj as TabViewItem;
                            Assert.IsNotNull(tabItem, "SourceTabs should contain TabViewItem entries.");
                            Assert.IsFalse(tabItem.IsClosable, "Sample source tabs should not show close buttons.");

                            DependencyObject sourcePane = tabItem.Content as DependencyObject;
                            Assert.IsNotNull(sourcePane, "Each source tab should host a source pane.");

                            RichTextBox viewer = FindByName<RichTextBox>(sourcePane, "SourceTextViewer");
                            Assert.IsNotNull(viewer, "Each source tab should host a read-only formatted source viewer.");
                            Assert.IsTrue(viewer.IsReadOnly, "Source viewers should be read-only.");
                            Assert.IsTrue(HasColoredSourceRuns(viewer),
                                "Source viewers should use colored runs for code formatting.");

                            Controls.Button copyButton = FindByName<Controls.Button>(sourcePane, "CopySourceButton");
                            Assert.IsNotNull(copyButton, "Each source tab should include a copy button.");
                            string sourceText = copyButton.Tag as string;
                            Assert.IsFalse(string.IsNullOrEmpty(sourceText),
                                "Copy buttons should keep the source text they copy in their Tag.");
                            sawCopyButton = true;
                            if (!verifiedCopyButton)
                            {
                                bool clipboardAvailable = TrySetClipboardTextWithRetry("before-copy");
                                copyButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                                if (clipboardAvailable)
                                {
                                    Assert.IsTrue(
                                        WaitUntil(window.Dispatcher, 1000, () => ClipboardTextEquals(sourceText)),
                                        "Copy buttons should copy their tab source text to the clipboard.");
                                }

                                verifiedCopyButton = true;
                            }

                            sourcePanes++;
                            if (sourceText.IndexOf("<ui:ComboBox", StringComparison.Ordinal) >= 0)
                            {
                                sawXaml = true;
                            }

                            if (sourceText.IndexOf("public partial class ComboBoxSelection", StringComparison.Ordinal) >= 0)
                            {
                                sawCodeBehind = true;
                            }
                        }

                        Assert.IsTrue(sourcePanes >= 2, "Expanded source tabs should each host a formatted source pane.");
                        Assert.IsTrue(sawCopyButton, "Expanded source tabs should expose copy buttons.");
                        Assert.IsTrue(verifiedCopyButton, "At least one source copy button should be exercised.");
                        Assert.IsTrue(sawXaml, "The XAML tab should display the sample XAML source.");
                        Assert.IsTrue(sawCodeBehind, "The C# tab should display the sample code-behind source.");
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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DemoSourceActions_UrlIconsUseOnAccentForegroundResource()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    Controls.Button sourceButton = DemoSourceAction.Create("Experimental/XamlOnly.xaml") as Controls.Button;
                    Assert.IsNotNull(sourceButton, "XAML-only samples should create an accent source button.");
                    FontIcon icon = sourceButton.Icon as FontIcon;
                    AssertSourceUrlIconUsesOnAccentForeground(icon);

                    DropDownButton sourceDropdown = DemoSourceAction.Create("Buttons/ButtonAppearances.xaml") as DropDownButton;
                    Assert.IsNotNull(sourceDropdown, "Samples with code-behind should create a source dropdown.");
                    System.Windows.Controls.StackPanel label = sourceDropdown.Content as System.Windows.Controls.StackPanel;
                    Assert.IsNotNull(label, "Source dropdown should use an icon/text label.");
                    Assert.IsTrue(label.Children.Count > 0, "Source dropdown label should include the URL glyph.");
                    AssertSourceUrlIconUsesOnAccentForeground(label.Children[0] as FontIcon);
                }
                finally
                {
                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DemoSourceActions_ForegroundFollowsTextOnAccentThemeResource()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    AssertSourceActionForegroundFollowsTextOnAccent(ApplicationTheme.Light);
                    AssertSourceActionForegroundFollowsTextOnAccent(ApplicationTheme.Dark);
                }
                finally
                {
                    if (dict != null && app.Resources.MergedDictionaries.Contains(dict))
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DemoSourceActions_CodeBehindDropdownUsesAccentHoverPresentation()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    DropDownButton sourceDropdown = DemoSourceAction.Create("Buttons/ButtonAppearances.xaml") as DropDownButton;
                    Assert.IsNotNull(sourceDropdown, "Samples with code-behind should create a source dropdown.");

                    PropertyInfo appearancePropertyInfo = typeof(DropDownButton).GetProperty("Appearance");
                    Assert.IsNotNull(appearancePropertyInfo, "Source dropdowns need an accent appearance state.");
                    Assert.AreEqual(ControlAppearance.Accent, appearancePropertyInfo.GetValue(sourceDropdown, null),
                        "Source dropdowns should use accent appearance.");

                    DependencyProperty appearanceProperty = Controls.ToggleButton.AppearanceProperty;

                    Grid host = new Grid();
                    _ = host.Children.Add(sourceDropdown);
                    Window window = new Window
                    {
                        Left = -20000,
                        Top = -20000,
                        Width = 320,
                        Height = 120,
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

                        AssertTemplateHasAccentMouseOverFill(
                            sourceDropdown.Template,
                            appearanceProperty,
                            "AccentFillColorSecondaryBrush");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    if (dict != null && app.Resources.MergedDictionaries.Contains(dict))
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DemoSourceActions_SamplesWithCodeBehindUseDropdownTargets()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryButtonsPage page = new GalleryButtonsPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        List<DropDownButton> sourceDropdowns = FindSourceDropDownButtons(page);
                        Assert.AreEqual(6, sourceDropdowns.Count,
                            "Buttons page examples all have code-behind and should use source dropdowns.");

                        foreach (DropDownButton dropdown in sourceDropdowns)
                        {
                            Assert.AreEqual(HorizontalAlignment.Right, dropdown.HorizontalAlignment,
                                dropdown.Name + " should be right-aligned.");
                            Assert.IsTrue(ContainsUrlGlyph(dropdown),
                                dropdown.Name + " should display a URL/link icon.");

                            List<string> targetUris = CollectSourceActionTargetUris(dropdown);
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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DemoSourceActions_XamlOnlySamplesUseAccentButton()
        {
            RunOnSta(() =>
            {
                Controls.Button action = DemoSourceAction.Create("Experimental/XamlOnly.xaml") as Controls.Button;
                Assert.IsNotNull(action, "XAML-only samples should use a single source button.");
                Assert.AreEqual(ControlAppearance.Accent, action.Appearance,
                    "XAML-only source buttons should use accent appearance.");
                Assert.AreEqual("Source", action.Content as string);
                Assert.IsTrue(ContainsUrlGlyph(action), "XAML-only source buttons should display a URL/link icon.");

                Uri target = action.Tag as Uri;
                Assert.IsNotNull(target, "XAML-only source buttons should carry their GitHub target URI.");
                Assert.AreEqual(
                    DemoSourceLinkSettings.GetGitHubSourceUri("Experimental/XamlOnly.xaml").AbsoluteUri,
                    target.AbsoluteUri);
            });
        }

        [TestMethod]
        public void DemoSourceSelectionSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "CheckBoxStates",
                "RadioButtonGroups",
                "ToggleSwitchStates",
                "ComboBoxSelection"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Selection", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Selection", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Selection sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Selection sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceInputsSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "TextBoxInput",
                "TextBoxValidation",
                "PasswordBoxInput",
                "NumberBoxInput",
                "SliderInput"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Inputs", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Inputs", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Inputs sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Inputs sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceTextSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "TextBlock"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Text", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Text", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Text sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Text sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceFormsSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "SignInForm",
                "CheckoutForm",
                "SettingsForm"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Forms", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Forms", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Forms sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Forms sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceDataSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplesWithCodeBehind = new[]
            {
                "ListViewItems",
                "ListViewEmptyState",
                "CardVariants"
            };
            string[] xamlOnlySamples = new[]
            {
                "ListViewSelection",
                "ListViewGrouped",
                "ListViewFiltering",
                "ListViewMessages",
                "ListViewImages",
                "ListViewContextMenu"
            };

            foreach (string samplePath in samplesWithCodeBehind)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Data", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Data", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Data sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Data sample code-behind must be copied beside the demo assembly: " + samplePath);
            }

            foreach (string samplePath in xamlOnlySamples)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Data", samplePath + ".xaml");

                Assert.IsTrue(File.Exists(xaml), "XAML-only Data sample must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceTreesSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "TreeViewHierarchy",
                "TreeViewSelection",
                "TreeViewExpansion"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Trees", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Trees", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Trees sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Trees sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceNavigationSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "LeftNavigationView",
                "TopNavigationView",
                "CompactNavigationView"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Navigation", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Navigation", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Navigation sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Navigation sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceTabsSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "TabControlBasics",
                "TabControlPlacement",
                "TabViewDocuments"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Tabs", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Tabs", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Tabs sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Tabs sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceMenusSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "MenuBar",
                "ContextMenuActions",
                "ToolTips",
                "DropDownAndSplitButtonMenus"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Menus", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Menus", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Menus sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Menus sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceStatusSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "ProgressBarValue",
                "ProgressBarIndeterminate",
                "ProgressBarSteps",
                "ProgressRings",
                "InfoBars"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Status", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Status", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Status sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Status sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceColorsSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "ColorSamples",
                "TextAndAccentBrushes",
                "FillAndSurfaceBrushes",
                "StrokeBrushes",
                "SystemAndHighContrastBrushes"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Colors", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Colors", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Colors sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Colors sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceGlyphsSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "IconCatalog",
                "CommonGlyphs",
                "CommandGlyphs",
                "StatusGlyphs"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Glyphs", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Glyphs", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Glyphs sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Glyphs sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceDataBindingSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "ObservableCollectionListView",
                "ListViewSelectionMode",
                "DataTemplateRow"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "DataBinding", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "DataBinding", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "DataBinding sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "DataBinding sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceAccessibilitySamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "FocusAndTabOrder",
                "HighContrastMapping",
                "AutomationProperties",
                "RtlLayout"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Accessibility", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Accessibility", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Accessibility sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Accessibility sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void DemoSourceWindowSamples_CopyToOutput()
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string[] samplePaths = new[]
            {
                "ThemeAndAccent",
                "BackdropAndCaptionButtons",
                "TitleBarChrome",
                "TitleBar"
            };

            foreach (string samplePath in samplePaths)
            {
                string xaml = Path.Combine(outputDirectory, "Samples", "Window", samplePath + ".xaml");
                string codeBehind = Path.Combine(outputDirectory, "Samples", "Window", samplePath + ".xaml.cs");

                Assert.IsTrue(File.Exists(xaml), "Window sample XAML must be copied beside the demo assembly: " + samplePath);
                Assert.IsTrue(File.Exists(codeBehind), "Window sample code-behind must be copied beside the demo assembly: " + samplePath);
            }
        }

        [TestMethod]
        public void ButtonsPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryButtonsPage page = new GalleryButtonsPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "Buttons/ButtonAppearances.xaml",
                            "Buttons/ButtonIcons.xaml",
                            "Buttons/HyperlinkButtons.xaml",
                            "Buttons/DropDownButtons.xaml",
                            "Buttons/SplitButtons.xaml",
                            "Buttons/ToggleAndRepeatButtons.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void InputsPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryInputsPage page = new GalleryInputsPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "Inputs/TextBoxInput.xaml",
                            "Inputs/TextBoxValidation.xaml",
                            "Inputs/PasswordBoxInput.xaml",
                            "Inputs/NumberBoxInput.xaml",
                            "Inputs/SliderInput.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void FormsPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryFormsPage page = new GalleryFormsPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "Forms/SignInForm.xaml",
                            "Forms/CheckoutForm.xaml",
                            "Forms/SettingsForm.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void SelectionPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GallerySelectionPage page = new GallerySelectionPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "Selection/CheckBoxStates.xaml",
                            "Selection/RadioButtonGroups.xaml",
                            "Selection/ToggleSwitchStates.xaml",
                            "Selection/ComboBoxSelection.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DataPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryDataPage page = new GalleryDataPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "Data/ListViewItems.xaml",
                            "Data/ListViewEmptyState.xaml",
                            "Data/CardVariants.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void TreesPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryTreesPage page = new GalleryTreesPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "Trees/TreeViewHierarchy.xaml",
                            "Trees/TreeViewSelection.xaml",
                            "Trees/TreeViewExpansion.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void NavigationPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryNavigationPage page = new GalleryNavigationPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "Navigation/LeftNavigationView.xaml",
                            "Navigation/TopNavigationView.xaml",
                            "Navigation/CompactNavigationView.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void TabsPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryTabsPage page = new GalleryTabsPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "Tabs/TabControlBasics.xaml",
                            "Tabs/TabControlPlacement.xaml",
                            "Tabs/TabViewDocuments.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MenusPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryMenusPage page = new GalleryMenusPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "Menus/MenuBar.xaml",
                            "Menus/ContextMenuActions.xaml",
                            "Menus/ToolTips.xaml",
                            "Menus/DropDownAndSplitButtonMenus.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void StatusPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryStatusPage page = new GalleryStatusPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "Status/ProgressBarValue.xaml",
                            "Status/ProgressBarIndeterminate.xaml",
                            "Status/ProgressBarSteps.xaml",
                            "Status/ProgressRings.xaml",
                            "Status/InfoBars.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DesignPages_UseInlineSourceSampleControls()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    DesignPageCase[] cases = new[]
                    {
                        new DesignPageCase("Color", "Colors/ColorSamples.xaml", delegate { return new GalleryColorsPage(); }),
                        new DesignPageCase("Iconography", "Glyphs/IconCatalog.xaml", delegate { return new GalleryGlyphsPage(); }),
                        new DesignPageCase("Typography", "Typography/TypographyTable.xaml", delegate { return new GalleryTypographyPage(); })
                    };

                    foreach (DesignPageCase testCase in cases)
                    {
                        UserControl page = testCase.CreatePage();
                        Grid host = new Grid();
                        _ = host.Children.Add(page);
                        Window window = new Window
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

                            List<FrameworkElement> actions = FindSourceActionControls(page);
                            Assert.AreEqual(0, actions.Count,
                                testCase.Title + " should use inline source tabs rather than source action buttons.");

                            DemoSampleControl sample = null;
                            foreach (DemoSampleControl candidate in FindAllVisualChildren<DemoSampleControl>(page))
                            {
                                sample = candidate;
                                break;
                            }

                            Assert.IsNotNull(sample, testCase.Title + " should host its visible example in DemoSampleControl.");
                            Assert.AreEqual(testCase.SourcePath, sample.SourcePath,
                                testCase.Title + " should load source from the expected sample file.");

                            Controls.Expander expander = FindByName<Controls.Expander>(sample, "SourceExpander");
                            Assert.IsNotNull(expander, testCase.Title + " should expose a source expander.");
                            expander.IsExpanded = true;
                            Drain(window.Dispatcher);
                            window.UpdateLayout();
                            Drain(window.Dispatcher);

                            TabView tabs = FindByName<TabView>(sample, "SourceTabs");
                            Assert.IsNotNull(tabs, testCase.Title + " should show source in TabView.");
                            Assert.AreEqual(2, tabs.Items.Count,
                                testCase.Title + " should expose XAML and C# source tabs.");
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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void ColorsPage_RemovesIntroductorySourceGridText()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryColorsPage page = new GalleryColorsPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        ArrayList texts = CollectTextBlockTexts(page);
                        CollectionAssert.DoesNotContain(texts,
                            "The brushes below are part of Fluence.Wpf and you can reference them in your app via DynamicResource.");
                        CollectionAssert.DoesNotContain(texts, "Text and accent");
                        CollectionAssert.DoesNotContain(texts, "Fills and surfaces");
                        CollectionAssert.DoesNotContain(texts, "Strokes");
                        CollectionAssert.DoesNotContain(texts, "System colors");
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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void TypographyPage_RendersTypographyStylesAsTable()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryTypographyPage page = new GalleryTypographyPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        Grid table = FindByName<Grid>(page, "TypographyTable");
                        Assert.IsNotNull(table, "The Typography page should present the supported text styles in a table.");

                        ArrayList texts = CollectTextBlockTexts(table);
                        CollectionAssert.Contains(texts, "Example");
                        CollectionAssert.Contains(texts, "Variable Font");
                        CollectionAssert.Contains(texts, "Size/Line height");
                        CollectionAssert.Contains(texts, "Style");

                        CollectionAssert.Contains(texts, "CaptionTextBlockStyle");
                        CollectionAssert.Contains(texts, "BodyTextBlockStyle");
                        CollectionAssert.Contains(texts, "BodyStrongTextBlockStyle");
                        CollectionAssert.Contains(texts, "BodyLargeTextBlockStyle");
                        CollectionAssert.Contains(texts, "SubtitleTextBlockStyle");
                        CollectionAssert.Contains(texts, "TitleTextBlockStyle");
                        CollectionAssert.Contains(texts, "TitleLargeTextBlockStyle");
                        CollectionAssert.Contains(texts, "DisplayTextBlockStyle");
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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void TypographyPage_TableRowsExposeCopyButtonsForStyleKeys()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryTypographyPage page = new GalleryTypographyPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        Grid table = FindByName<Grid>(page, "TypographyTable");
                        Assert.IsNotNull(table, "The Typography page should present the supported text styles in a table.");

                        string[] expected = new[]
                        {
                            "CaptionTextBlockStyle",
                            "BodyTextBlockStyle",
                            "BodyStrongTextBlockStyle",
                            "BodyLargeTextBlockStyle",
                            "SubtitleTextBlockStyle",
                            "TitleTextBlockStyle",
                            "TitleLargeTextBlockStyle",
                            "DisplayTextBlockStyle"
                        };

                        ArrayList actual = new ArrayList();
                        foreach (Controls.Button button in FindAllVisualChildren<Controls.Button>(table))
                        {
                            string styleKey = button.Tag as string;
                            if (!string.IsNullOrEmpty(styleKey) && styleKey.EndsWith("TextBlockStyle", StringComparison.Ordinal))
                            {
                                _ = actual.Add(styleKey);
                            }
                        }

                        CollectionAssert.AreEquivalent(expected, actual,
                            "Each typography table row should expose a copy button tagged with its text style key.");
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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void StatusPage_ProgressRingDemo_UsesDisabledRingAndAlignedLabels()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryStatusPage page = new GalleryStatusPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        ProgressRing disabledRing = FindByName<ProgressRing>(page, "DisabledProgressRing");
                        Assert.IsNotNull(disabledRing, "The third ProgressRing example should be a disabled ring, not an inactive ring.");
                        Assert.IsFalse(disabledRing.IsEnabled, "The disabled ProgressRing example should use IsEnabled=False.");
                        Assert.IsTrue(disabledRing.IsActive, "The disabled ProgressRing should still be active so its disabled visual is visible.");
                        Assert.IsTrue(disabledRing.IsIndeterminate, "The disabled ProgressRing example should use the indeterminate ring visual.");

                        System.Windows.Controls.TextBlock indeterminateLabel = FindByName<System.Windows.Controls.TextBlock>(page, "IndeterminateProgressRingLabel");
                        System.Windows.Controls.TextBlock determinateLabel = FindByName<System.Windows.Controls.TextBlock>(page, "DeterminateProgressRingLabel");
                        System.Windows.Controls.TextBlock disabledLabel = FindByName<System.Windows.Controls.TextBlock>(page, "DisabledProgressRingLabel");
                        Assert.IsNotNull(indeterminateLabel);
                        Assert.IsNotNull(determinateLabel);
                        Assert.IsNotNull(disabledLabel);

                        double indeterminateY = indeterminateLabel.TransformToAncestor(window).Transform(new Point(0, 0)).Y;
                        double determinateY = determinateLabel.TransformToAncestor(window).Transform(new Point(0, 0)).Y;
                        double disabledY = disabledLabel.TransformToAncestor(window).Transform(new Point(0, 0)).Y;

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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void GlyphsPage_RendersEveryAvailableSegoeFluentIcon()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryGlyphsPage page = new GalleryGlyphsPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        System.Windows.Controls.ListView list = FindByName<System.Windows.Controls.ListView>(page, "IconCatalogList");
                        Assert.IsNotNull(list, "The Iconography page should render a single virtualized icon catalog.");

                        int expectedCount = GetSegoeFluentPrivateUseGlyphCount();
                        Assert.IsTrue(expectedCount > 1000, "Segoe Fluent Icons should expose a large private-use glyph set on this machine.");
                        Assert.AreEqual((expectedCount + 3) / 4, list.Items.Count,
                            "The Iconography page should virtualize rows, with four icon cards per grid row.");
                        Assert.IsTrue(list.Items.Count < expectedCount,
                            "The virtualized grid should reduce item containers by grouping glyphs into rows.");
                        Assert.AreEqual(4, GetNestedItemCount(list.Items[0], "Items"),
                            "Each full iconography grid row should render four glyph cards.");
                        Assert.IsTrue(VirtualizingPanel.GetIsVirtualizing(list), "The icon catalog should stay virtualized because it contains more than 1,000 glyphs.");
                        Assert.AreEqual(VirtualizationMode.Recycling, VirtualizingPanel.GetVirtualizationMode(list));

                        ArrayList names = CollectItemPropertyValues(list, "Name");
                        CollectionAssert.Contains(names, "GlobalNavButton");
                        CollectionAssert.Contains(names, "Settings");

                        ArrayList codes = CollectItemPropertyValues(list, "DisplayCode");
                        CollectionAssert.Contains(codes, "U+E700");
                        CollectionAssert.Contains(codes, "U+E713");

                        ArrayList texts = CollectTextBlockTexts(page);
                        CollectionAssert.DoesNotContain(texts, "Common icons");
                        CollectionAssert.DoesNotContain(texts, "Command icons");
                        CollectionAssert.DoesNotContain(texts, "Status icons");
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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void GlyphsPage_DarkTheme_UsesFluentListViewSurface()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                app.Resources.MergedDictionaries.Clear();
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                ResourceDictionary demoShared = new ResourceDictionary
                {
                    Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
                };
                app.Resources.MergedDictionaries.Add(demoShared);

                try
                {
                    GalleryGlyphsPage page = new GalleryGlyphsPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        System.Windows.Controls.ListView list = FindByName<System.Windows.Controls.ListView>(page, "IconCatalogList");
                        Assert.IsNotNull(list, "The Iconography page should render the icon catalog list in dark mode.");
                        Assert.IsInstanceOfType(
                            list,
                            typeof(Controls.ListView),
                            "The Iconography catalog must use the Fluence ListView so its surface and scroll viewer follow dark theme resources.");

                        SolidColorBrush background = list.Background as SolidColorBrush;
                        Assert.IsNotNull(background, "The themed icon catalog surface should resolve a concrete brush in dark mode.");
                        Assert.AreEqual(Colors.Transparent, background.Color,
                            "The dark Iconography catalog should not paint the stock WPF Window background over its Fluent cards.");

                        int realizedIconCount = 0;
                        foreach (FontIcon icon in FindAllVisualChildren<FontIcon>(list))
                        {
                            if (icon.IsVisible)
                            {
                                realizedIconCount++;
                            }
                        }

                        Assert.IsTrue(realizedIconCount > 0, "The dark Iconography catalog should realize visible icon cells.");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    _ = app.Resources.MergedDictionaries.Remove(demoShared);
                }
            });
        }

        [TestMethod]
        public void GlyphsPage_UsesInlineIconCatalogSourceSample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryGlyphsPage page = new GalleryGlyphsPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        List<FrameworkElement> actions = FindSourceActionControls(page);
                        Assert.AreEqual(0, actions.Count,
                            "The Iconography page should expose source through the inline sample expander.");

                        DemoSampleControl sample = null;
                        foreach (DemoSampleControl candidate in FindAllVisualChildren<DemoSampleControl>(page))
                        {
                            sample = candidate;
                            break;
                        }

                        Assert.IsNotNull(sample, "The Iconography page should host its catalog in DemoSampleControl.");
                        Assert.AreEqual("Glyphs/IconCatalog.xaml", sample.SourcePath);
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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void DataBindingPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryDataBindingPage page = new GalleryDataBindingPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "DataBinding/ObservableCollectionListView.xaml",
                            "DataBinding/ListViewSelectionMode.xaml",
                            "DataBinding/DataTemplateRow.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void AccessibilityPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryAccessibilityPage page = new GalleryAccessibilityPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "Accessibility/FocusAndTabOrder.xaml",
                            "Accessibility/HighContrastMapping.xaml",
                            "Accessibility/AutomationProperties.xaml",
                            "Accessibility/RtlLayout.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void WindowPage_ContainsSourceLinksForEachExample()
        {
            RunOnSta(() =>
            {
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryWindowPage page = new GalleryWindowPage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        string[] expected = ExpectedSourceUris(
                            "Window/ThemeAndAccent.xaml",
                            "Window/BackdropAndCaptionButtons.xaml",
                            "Window/TitleBarChrome.xaml"
                        );


                        List<string> actual = CollectSourceActionTargetUris(page);


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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
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
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                MainWindow window = null;
                try
                {
                    window = CreateShownMainWindow();
                    FluenceTextBox search = GetNavSearchBox(window);
                    NavigationView nav = GetDemoNav(window);

                    search.Text = "button";
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    NavigationViewItemHeader anyVisibleEmptyHeader = null;
                    NavigationViewItemHeader currentHeader = null;
                    bool currentHeaderHasVisibleChild = false;

                    foreach (object obj in nav.Items)
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
                    window?.Close();

                    if (dict != null)
                    {
                        _ = app.Resources.MergedDictionaries.Remove(dict);
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
                Application app = EnsureApp();
                ResourceDictionary dict = MergeTheme(app);

                try
                {
                    GalleryHomePage page = new GalleryHomePage();
                    Grid host = new Grid();
                    _ = host.Children.Add(page);
                    Window window = new Window
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

                        FrameworkElement grid = FindByName<FrameworkElement>(page, "FeaturedControlsGrid");
                        Assert.IsNotNull(grid,
                            "GalleryHomePage must contain a FrameworkElement named 'FeaturedControlsGrid' " +
                            "(Paradigm A curated featured controls).");

                        int featuredCards = 0;
                        foreach (Card card in FindAllVisualChildren<Card>(grid))
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
                        _ = app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        private static void AssertPageUsesInlineSourceSamples(DependencyObject root, string pageTitle)
        {
            List<FrameworkElement> actions = FindSourceActionControls(root);
            Assert.AreEqual(0, actions.Count,
                pageTitle + " should expose source through inline sample expanders instead of source action buttons.");

            List<DemoSampleControl> samples = new List<DemoSampleControl>();
            foreach (DemoSampleControl sample in FindAllVisualChildren<DemoSampleControl>(root))
            {
                samples.Add(sample);
            }

            Assert.IsTrue(samples.Count > 0, pageTitle + " should host examples in DemoSampleControl.");
            foreach (DemoSampleControl sample in samples)
            {
                Assert.IsFalse(string.IsNullOrEmpty(sample.SourcePath),
                    pageTitle + " inline source samples should keep a copied source path.");
                Controls.Expander expander = FindByName<Controls.Expander>(sample, "SourceExpander");
                Assert.IsNotNull(expander, pageTitle + " inline source samples should expose a source expander.");
                Assert.IsFalse(expander.IsExpanded,
                    pageTitle + " source expanders should stay collapsed until the user opens them.");
            }
        }

        private static string[] ExpectedSourceUris(params string[] samplePaths)
        {
            List<string> expected = new List<string>();

            foreach (string samplePath in samplePaths)
            {
                expected.Add(DemoSourceLinkSettings.GetGitHubSourceUri(samplePath).AbsoluteUri);

                string codeBehindPath = samplePath.Replace('\\', '/').Trim('/') + ".cs";
                if (SampleFileExists(codeBehindPath))
                {
                    expected.Add(DemoSourceLinkSettings.GetGitHubSourceUri(codeBehindPath).AbsoluteUri);
                }
            }

            return expected.ToArray();
        }

        private static bool SampleFileExists(string samplePath)
        {
            string outputDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
            string localPath = samplePath.Replace('/', Path.DirectorySeparatorChar);
            return File.Exists(Path.Combine(outputDirectory, "Samples", localPath));
        }

        private static ArrayList CollectTextBlockTexts(DependencyObject root)
        {
            ArrayList texts = new ArrayList();

            foreach (System.Windows.Controls.TextBlock textBlock in FindAllVisualChildren<System.Windows.Controls.TextBlock>(root))
            {
                if (!string.IsNullOrEmpty(textBlock.Text))
                {
                    _ = texts.Add(textBlock.Text);
                }
            }

            return texts;
        }

        private static Controls.Button FindCompactFlyoutButton(DependencyObject root, string label)
        {
            foreach (Controls.Button button in FindAllVisualChildren<Controls.Button>(root))
            {
                ArrayList texts = CollectTextBlockTexts(button);
                if (texts.Contains(label))
                {
                    return button;
                }
            }

            return null;
        }

        private static ArrayList CollectItemPropertyValues(ItemsControl control, string propertyName)
        {
            ArrayList values = new ArrayList();

            foreach (object item in control.Items)
            {
                AddItemPropertyValue(values, item, propertyName);

                IEnumerable nestedItems = GetNestedItems(item, "Items");
                if (nestedItems == null)
                {
                    continue;
                }

                foreach (object nestedItem in nestedItems)
                {
                    AddItemPropertyValue(values, nestedItem, propertyName);
                }
            }

            return values;
        }

        private static void AddItemPropertyValue(ArrayList values, object item, string propertyName)
        {
            if (item == null)
            {
                return;
            }

            PropertyInfo property = item.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                return;
            }

            string value = property.GetValue(item, null) as string;
            if (!string.IsNullOrEmpty(value))
            {
                _ = values.Add(value);
            }
        }

        private static int GetNestedItemCount(object item, string propertyName)
        {
            IEnumerable nestedItems = GetNestedItems(item, propertyName);
            if (nestedItems == null)
            {
                return 0;
            }

            int count = 0;
            foreach (object ignored in nestedItems)
            {
                count++;
            }

            return count;
        }

        private static IEnumerable GetNestedItems(object item, string propertyName)
        {
            if (item == null)
            {
                return null;
            }

            PropertyInfo property = item.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return property == null ? null : property.GetValue(item, null) as IEnumerable;
        }

        private static int GetSegoeFluentPrivateUseGlyphCount()
        {
            Typeface typeface = new Typeface(
                new FontFamily("Segoe Fluent Icons"),
                FontStyles.Normal,
                FontWeights.Normal,
                FontStretches.Normal);

            Assert.IsTrue(typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface),
                "Segoe Fluent Icons must be installed for the iconography catalog test.");

            int count = 0;
            foreach (int character in glyphTypeface.CharacterToGlyphMap.Keys)
            {
                if (character >= 0xE000 && character <= 0xF8FF)
                {
                    count++;
                }
            }

            return count;
        }

        private static void AssertSourceUrlIconUsesOnAccentForeground(FontIcon icon)
        {
            Assert.IsNotNull(icon, "Source action should expose a URL glyph.");
            Assert.AreEqual("\uE71B", icon.Glyph, "Source action should use the URL glyph.");

            object localForeground = icon.ReadLocalValue(Control.ForegroundProperty);
            Assert.AreNotSame(DependencyProperty.UnsetValue, localForeground,
                "Source URL glyph should set its own TextOnAccentFillColorPrimaryBrush foreground.");
        }

        private static void AssertSourceActionForegroundFollowsTextOnAccent(ApplicationTheme theme)
        {
            ApplicationThemeManager.Apply(theme, BackdropType.None, true);
            ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

            Controls.Button sourceButton = DemoSourceAction.Create("Experimental/XamlOnly.xaml") as Controls.Button;
            Assert.IsNotNull(sourceButton, "XAML-only samples should create an accent source button.");

            DropDownButton sourceDropdown = DemoSourceAction.Create("Buttons/ButtonAppearances.xaml") as DropDownButton;
            Assert.IsNotNull(sourceDropdown, "Samples with code-behind should create a source dropdown.");

            System.Windows.Controls.StackPanel host = new System.Windows.Controls.StackPanel();
            _ = host.Children.Add(sourceButton);
            _ = host.Children.Add(sourceDropdown);
            Window window = new Window
            {
                Left = -20000,
                Top = -20000,
                Width = 360,
                Height = 160,
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

                Color expected = GetThemeBrushColor("TextOnAccentFillColorPrimaryBrush");
                AssertBrushColor(sourceButton.Foreground, expected,
                    theme + " source button foreground should use text-on-accent.");
                AssertBrushColor(((FontIcon)sourceButton.Icon).Foreground, expected,
                    theme + " source button URL glyph should use text-on-accent.");

                AssertBrushColor(sourceDropdown.Foreground, expected,
                    theme + " source dropdown foreground should use text-on-accent.");

                System.Windows.Controls.StackPanel label = sourceDropdown.Content as System.Windows.Controls.StackPanel;
                Assert.IsNotNull(label, "Source dropdown should use an icon/text label.");
                Assert.IsTrue(label.Children.Count >= 2, "Source dropdown label should include icon and text.");
                AssertBrushColor(((FontIcon)label.Children[0]).Foreground, expected,
                    theme + " source dropdown URL glyph should use text-on-accent.");

                System.Windows.Controls.TextBlock text = label.Children[1] as System.Windows.Controls.TextBlock;
                Assert.IsNotNull(text, "Source dropdown label should include a text label.");
                AssertBrushColor(text.Foreground, expected,
                    theme + " source dropdown text should use text-on-accent.");
            }
            finally
            {
                window.Close();
            }
        }

        private static Color GetThemeBrushColor(string resourceKey)
        {
            SolidColorBrush brush = Application.Current.Resources[resourceKey] as SolidColorBrush;
            Assert.IsNotNull(brush, "Theme resource should be a SolidColorBrush: " + resourceKey);
            return brush.Color;
        }

        private static void AssertBrushColor(Brush brush, Color expected, string message)
        {
            SolidColorBrush solidBrush = brush as SolidColorBrush;
            Assert.IsNotNull(solidBrush, message);
            Assert.AreEqual(expected, solidBrush.Color, message);
        }

        private static void AssertTemplateHasAccentMouseOverFill(
            ControlTemplate template,
            DependencyProperty appearanceProperty,
            string resourceKey)
        {
            Assert.IsNotNull(template, "Source dropdown should have an applied template.");

            foreach (TriggerBase triggerBase in template.Triggers)
            {
                if (!(triggerBase is MultiTrigger multiTrigger))
                {
                    continue;
                }

                if (!HasCondition(multiTrigger, UIElement.IsMouseOverProperty, true) ||
                    !HasCondition(multiTrigger, appearanceProperty, ControlAppearance.Accent))
                {
                    continue;
                }

                foreach (Setter setter in multiTrigger.Setters.OfType<Setter>())
                {
                    if (string.Equals(setter.TargetName, "RestFill", StringComparison.Ordinal) &&
                        setter.Property == System.Windows.Controls.Border.BackgroundProperty &&
                        IsDynamicResource(setter.Value, resourceKey))
                    {
                        return;
                    }
                }
            }

            Assert.Fail("Accent source dropdown hover should use " + resourceKey + ".");
        }

        private static bool HasCondition(MultiTrigger trigger, DependencyProperty property, object value)
        {
            foreach (Condition condition in trigger.Conditions)
            {
                if (condition.Property == property && Equals(condition.Value, value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDynamicResource(object value, string resourceKey)
        {
            return value is DynamicResourceExtension dynamicResource && Equals(dynamicResource.ResourceKey, resourceKey);
        }

        private static List<FrameworkElement> FindSourceActionControls(DependencyObject root)
        {
            List<FrameworkElement> actions = new List<FrameworkElement>();

            foreach (DropDownButton dropdown in FindSourceDropDownButtons(root))
            {
                actions.Add(dropdown);
            }

            foreach (Controls.Button button in FindAllVisualChildren<Controls.Button>(root))
            {
                if (IsSourceActionElement(button))
                {
                    actions.Add(button);
                }
            }

            foreach (HyperlinkButton link in FindAllVisualChildren<HyperlinkButton>(root))
            {
                if (IsSourceActionElement(link))
                {
                    actions.Add(link);
                }
            }

            return actions;
        }

        private static List<DropDownButton> FindSourceDropDownButtons(DependencyObject root)
        {
            List<DropDownButton> dropdowns = new List<DropDownButton>();

            foreach (DropDownButton dropdown in FindAllVisualChildren<DropDownButton>(root))
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

            string tagText = element.Tag as string;
            if (!string.IsNullOrEmpty(tagText) && tagText.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            Uri tagUri = element.Tag as Uri;
            return (tagUri != null && string.Equals(GetContentText(element), "Source", StringComparison.Ordinal)) || string.Equals(GetContentText(element), "Source", StringComparison.Ordinal);
        }

        private static string GetContentText(object value)
        {
            return !(value is ContentControl contentControl) ? null : contentControl.Content as string;
        }

        private static List<string> CollectSourceActionTargetUris(DependencyObject root)
        {
            List<string> uris = new List<string>();
            FrameworkElement rootElement = root as FrameworkElement;
            if (IsSourceActionElement(rootElement))
            {
                AddSourceActionTargetUris(rootElement, uris);
            }

            foreach (FrameworkElement action in FindSourceActionControls(root))
            {
                AddSourceActionTargetUris(action, uris);
            }

            return uris;
        }

        private static void AddSourceActionTargetUris(object value, List<string> uris)
        {
            if (value == null)
            {
                return;
            }

            if (value is HyperlinkButton hyperlink && hyperlink.NavigateUri != null)
            {
                uris.Add(hyperlink.NavigateUri.AbsoluteUri);
            }

            if (value is System.Windows.Controls.Button button)
            {
                Uri uri = button.Tag as Uri;
                if (uri != null)
                {
                    uris.Add(uri.AbsoluteUri);
                }
            }

            if (value is DropDownButton dropdown)
            {
                AddSourceActionTargetUris(dropdown.Flyout, uris);
            }

            if (value is Panel panel)
            {
                foreach (UIElement child in panel.Children)
                {
                    AddSourceActionTargetUris(child, uris);
                }
            }

            if (value is ContentControl contentControl)
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

            if (value is FontIcon fontIcon && string.Equals(fontIcon.Glyph, "\uE71B", StringComparison.Ordinal))
            {
                return true;
            }

            if (value is HyperlinkButton hyperlink && ContainsUrlGlyph(hyperlink.Icon))
            {
                return true;
            }

            if (value is Controls.Button button && ContainsUrlGlyph(button.Icon))
            {
                return true;
            }

            if (value is ContentControl contentControl && ContainsUrlGlyph(contentControl.Content))
            {
                return true;
            }

            if (value is Panel panel)
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

        private sealed class DesignPageCase
        {
            public DesignPageCase(string title, string sourcePath, Func<UserControl> createPage)
            {
                Title = title;
                SourcePath = sourcePath;
                CreatePage = createPage;
            }

            public string Title { get; private set; }

            public string SourcePath { get; private set; }

            public Func<UserControl> CreatePage { get; private set; }
        }

        private static bool HasColoredSourceRuns(RichTextBox sourceViewer)
        {
            foreach (Block block in sourceViewer.Document.Blocks)
            {
                if (!(block is Paragraph paragraph))
                {
                    continue;
                }

                foreach (Inline inline in paragraph.Inlines)
                {
                    if (inline is Run run && !string.IsNullOrEmpty(run.Text) && run.Foreground != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void AssertCenteredChildren(Grid row, string rowName)
        {
            Assert.IsNotNull(row, rowName + " row should exist.");
            Assert.IsTrue(row.Children.Count > 0, rowName + " row should contain controls.");

            foreach (UIElement child in row.Children)
            {
                FrameworkElement element = child as FrameworkElement;
                Assert.IsNotNull(element, rowName + " row children should be framework elements.");
                Assert.AreEqual(VerticalAlignment.Center, element.VerticalAlignment,
                    rowName + " row should vertically center " + element.GetType().Name + ".");
            }
        }

        private static int GetMaxGridColumn(Grid row)
        {
            int max = 0;
            foreach (UIElement child in row.Children)
            {
                max = Math.Max(max, Grid.GetColumn(child));
            }

            return max;
        }

        private static int GetMaxGridRow(Grid row)
        {
            int max = 0;
            foreach (UIElement child in row.Children)
            {
                max = Math.Max(max, Grid.GetRow(child));
            }

            return max;
        }

        private static T FindByName<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (root is T asT && string.Equals(asT.Name, name, StringComparison.Ordinal))
            {
                return asT;
            }

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                T hit = FindByName<T>(VisualTreeHelper.GetChild(root, i), name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }

        private static NavigationViewItem AssertNavigationItemExists(NavigationView nav, string content)
        {
            foreach (object obj in nav.Items)
            {
                if (obj is NavigationViewItem item && string.Equals(item.Content as string, content, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            Assert.Fail("Navigation item should exist: " + content);
            return null;
        }

        private static bool IsDescendantOrSelf(DependencyObject candidate, DependencyObject ancestor)
        {
            if (candidate == null || ancestor == null)
            {
                return false;
            }

            DependencyObject current = candidate;
            while (current != null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = GetVisualOrLogicalParent(current);
            }

            return false;
        }

        private static DependencyObject GetVisualOrLogicalParent(DependencyObject current)
        {
            return current is Visual || current is System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        private static IEnumerable<T> FindAllVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                yield break;
            }

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (T descendant in FindAllVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
