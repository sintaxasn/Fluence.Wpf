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
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using FluenceExpander = Fluence.Wpf.Controls.Expander;
using FluenceListView = Fluence.Wpf.Controls.ListView;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfButton = System.Windows.Controls.Button;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public sealed class DemoMainWindowTests
    {
        private static readonly DemoPageExpectation[] PageExpectations =
        {
            new DemoPageExpectation("colors", typeof(GalleryColorsPage)),
            new DemoPageExpectation("iconography", typeof(GalleryGlyphsPage)),
            new DemoPageExpectation("typography", typeof(GalleryTypographyPage)),
            new DemoPageExpectation("accessibility", typeof(GalleryAccessibilityPage)),
            new DemoPageExpectation("buttons", typeof(GalleryButtonsPage)),
            new DemoPageExpectation("selection", typeof(GallerySelectionPage)),
            new DemoPageExpectation("inputs", typeof(GalleryInputsPage)),
            new DemoPageExpectation("data binding", typeof(GalleryDataBindingPage)),
            new DemoPageExpectation("data", typeof(GalleryDataPage)),
            new DemoPageExpectation("trees", typeof(GalleryTreesPage)),
            new DemoPageExpectation("menus", typeof(GalleryMenusPage)),
            new DemoPageExpectation("navigation", typeof(GalleryNavigationPage)),
            new DemoPageExpectation("tabs", typeof(GalleryTabsPage)),
            new DemoPageExpectation("layout", typeof(GalleryLayoutPage)),
            new DemoPageExpectation("status", typeof(GalleryStatusPage)),
            new DemoPageExpectation("window", typeof(GalleryWindowPage))
        };

        private static void RunOnSta(Action action)
        {
            Exception captured = null;
            WpfTestSta.Dispatcher.Invoke(new Action(delegate
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

        [TestMethod]
        public void MainWindow_DirectNavigation_LoadsConcretePages()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    foreach (DemoPageExpectation expectation in PageExpectations)
                    {
                        window.NavigateTo(expectation.Tag);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        object content = GetSelectedPageContent(window);
                        Assert.IsNotNull(content, "Navigation must create page content for tag: " + expectation.Tag);
                        Assert.AreEqual(expectation.PageType, content.GetType(), "Tag should load the concrete page directly: " + expectation.Tag);
                        Assert.AreNotEqual("GalleryControlPage", content.GetType().Name, "Generated page shell must not be used.");
                        Assert.AreNotEqual("GalleryCategoryPage", content.GetType().Name, "Category overview shell must not be used.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_InitialSelection_LoadsHomePageContent()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    object content = GetSelectedPageContent(window);
                    Assert.IsNotNull(content, "Initial home navigation must create page content.");
                    Assert.AreEqual(typeof(GalleryHomePage), content.GetType(), "The first selected page should be Home.");

                    NavigationView nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    Assert.AreSame(content, nav.SelectedContent, "NavigationView.SelectedContent should be populated for the initial Home page.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_Search_NavigatesToGroupedConcretePage()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    Fluence.Wpf.Controls.TextBox search = FindByName<Fluence.Wpf.Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(search, "Demo search box must be present.");

                    search.Text = "progress ring";
                    search.RaiseEvent(new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        PresentationSource.FromVisual(window),
                        0,
                        Key.Enter)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent
                    });
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    object content = GetSelectedPageContent(window);
                    Assert.AreEqual(typeof(GalleryStatusPage), content.GetType(), "Search should resolve progress terms to the grouped Status page.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_NavigationCatalog_PutsAccessibilityBeforeWindowing()
        {
            var items = new List<DemoNavigationItem>(DemoNavigationCatalog.Items);
            Assert.IsTrue(items.Count >= 2, "Navigation catalog should contain at least two entries.");
            Assert.AreEqual("Accessibility", items[items.Count - 2].Title,
                "Accessibility should be second-last in the NavigationView list.");
            Assert.AreEqual("Windowing", items[items.Count - 1].Title,
                "Windowing should remain the final NavigationView item.");
        }

        [TestMethod]
        public void MainWindow_TitleBarSearch_CollapsesWhenContentExtendsIntoTitleBar()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    Fluence.Wpf.Controls.TextBox search = FindByName<Fluence.Wpf.Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(search, "Demo search box must be present.");
                    Assert.AreEqual(Visibility.Visible, search.Visibility, "Search should start visible in the normal title bar.");

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(Visibility.Collapsed, search.Visibility,
                        "Search should collapse when content extends into the title bar.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_TitleBarSearch_DoesNotShiftWhenChromeOptionsChange()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    Fluence.Wpf.Controls.TextBox search = FindByName<Fluence.Wpf.Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(search, "Demo search box must be present.");

                    double initialX = GetVisualX(search, window);

                    window.SetUserShowIcon(false, window.Icon);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.AreEqual(initialX, GetVisualX(search, window), 1.0,
                        "Search should not shift when the demo hides the icon.");

                    window.SetUserShowTitle(false, window.Title);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.AreEqual(initialX, GetVisualX(search, window), 1.0,
                        "Search should not shift when the demo hides the title.");

                    window.IsMinimizeButtonVisible = Visibility.Collapsed;
                    window.IsMaximizeButtonVisible = Visibility.Collapsed;
                    window.IsCloseButtonVisible = Visibility.Collapsed;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.AreEqual(initialX, GetVisualX(search, window), 1.0,
                        "Search should not shift when caption buttons are collapsed.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DemoSampleControl_ExpanderUsesInMemorySourceTabs()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                var sample = new DemoSampleControl
                {
                    Title = "Snippet",
                    XamlSource = "<ui:Button Content=\"Save\" />",
                    CSharpSource = "private void Save_Click(object sender, RoutedEventArgs e) { }",
                    SampleContent = new WpfTextBlock { Text = "Visible sample" }
                };

                Window window = CreateHostWindow(sample);
                try
                {
                    FluenceExpander expander = FindByName<FluenceExpander>(sample, "SourceExpander");
                    Assert.IsNotNull(expander, "Inline source expander must exist.");
                    Assert.IsFalse(expander.IsExpanded, "Source starts collapsed.");

                    expander.IsExpanded = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    TabView tabs = FindByName<TabView>(sample, "SourceTabs");
                    Assert.IsNotNull(tabs, "Expanded source creates a TabView.");
                    Assert.AreEqual(2, tabs.Items.Count, "XAML plus C# source should create two tabs.");
                    AssertSourceTab(tabs, "XAML", sample.XamlSource);
                    AssertSourceTab(tabs, "C# Code-behind", sample.CSharpSource);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DemoSampleControl_EmptyCSharpSourceAddsOnlyXamlTab()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                var sample = new DemoSampleControl
                {
                    Title = "Snippet",
                    XamlSource = "<ui:ToggleSwitch IsChecked=\"True\" />"
                };

                Window window = CreateHostWindow(sample);
                try
                {
                    FluenceExpander expander = FindByName<FluenceExpander>(sample, "SourceExpander");
                    expander.IsExpanded = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    TabView tabs = FindByName<TabView>(sample, "SourceTabs");
                    Assert.AreEqual(1, tabs.Items.Count, "XAML-only samples should not show an empty C# tab.");
                    AssertSourceTab(tabs, "XAML", sample.XamlSource);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_NonHomePagesExposeInlineSourceSamples()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    foreach (DemoPageExpectation expectation in PageExpectations)
                    {
                        window.NavigateTo(expectation.Tag);
                        Drain(window.Dispatcher);
                        window.UpdateLayout();
                        Drain(window.Dispatcher);

                        object content = GetSelectedPageContent(window);
                        var root = content as DependencyObject;
                        Assert.IsNotNull(root, "Page content must be visual for tag: " + expectation.Tag);

                        bool found = false;
                        foreach (DemoSampleControl sample in FindAllVisualChildren<DemoSampleControl>(root))
                        {
                            if (!string.IsNullOrWhiteSpace(sample.XamlSource))
                            {
                                found = true;
                                break;
                            }
                        }

                        Assert.IsTrue(found, "Page must expose at least one inline XAML source sample: " + expectation.PageType.Name);
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryAccessibilityPage_KeyboardSamplesUseAlignedRows()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                var page = new GalleryAccessibilityPage();
                Window window = CreateHostWindow(page);
                try
                {
                    Grid primary = FindByName<Grid>(page, "KeyboardSupportPrimaryControls");
                    Assert.IsNotNull(primary, "Accessibility keyboard sample should use a named alignment grid.");
                    Assert.AreEqual(4, primary.ColumnDefinitions.Count,
                        "Primary keyboard sample should have four equal columns.");
                    Assert.AreEqual(2, primary.RowDefinitions.Count,
                        "Primary keyboard sample should have two aligned rows.");
                    Assert.AreEqual(8, primary.Children.Count,
                        "Primary keyboard sample should contain four controls per row.");

                    AssertGridCell(primary, delegate(UIElement child)
                    {
                        var button = child as Fluence.Wpf.Controls.Button;
                        return button != null && string.Equals(button.Content as string, "Button 1", StringComparison.Ordinal);
                    }, 0, 0, "Button 1");
                    AssertGridCell(primary, delegate(UIElement child)
                    {
                        var button = child as Fluence.Wpf.Controls.Button;
                        return button != null && string.Equals(button.Content as string, "Button 2", StringComparison.Ordinal);
                    }, 0, 1, "Button 2");
                    AssertGridCell(primary, delegate(UIElement child)
                    {
                        return child is Fluence.Wpf.Controls.TextBox;
                    }, 0, 2, "TextBox");
                    AssertGridCell(primary, delegate(UIElement child)
                    {
                        return child is Fluence.Wpf.Controls.ComboBox;
                    }, 0, 3, "ComboBox");
                    AssertGridCell(primary, delegate(UIElement child)
                    {
                        return child is Fluence.Wpf.Controls.CheckBox;
                    }, 1, 0, "CheckBox");
                    AssertGridCell(primary, delegate(UIElement child)
                    {
                        return child is ToggleSwitch;
                    }, 1, 1, "ToggleSwitch");
                    AssertGridCell(primary, delegate(UIElement child)
                    {
                        return child is Fluence.Wpf.Controls.Slider;
                    }, 1, 2, "Slider");
                    AssertGridCell(primary, delegate(UIElement child)
                    {
                        return child is HyperlinkButton;
                    }, 1, 3, "HyperlinkButton");

                    Grid tabOrder = FindByName<Grid>(page, "KeyboardSupportExplicitOrderControls");
                    Assert.IsNotNull(tabOrder, "Explicit tab order sample should use an alignment grid.");
                    Assert.AreEqual(3, tabOrder.ColumnDefinitions.Count,
                        "Explicit tab order buttons should line up in equal columns.");
                    Assert.AreEqual(3, tabOrder.Children.Count,
                        "Explicit tab order sample should contain three aligned buttons.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryGlyphsPage_IconCatalogIsScrollableAndVirtualized()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                var page = new GalleryGlyphsPage();
                Window window = CreateHostWindow(page);
                try
                {
                    FluenceListView list = FindByName<FluenceListView>(page, "IconCatalogList");
                    Assert.IsNotNull(list, "Icon catalog list must exist.");
                    Assert.IsTrue(list.Items.Count > 100, "Icon catalog must load enough rows to exercise virtualization.");

                    ScrollViewer viewer = FindVisualChild<ScrollViewer>(list);
                    Assert.IsNotNull(viewer, "Icon catalog list must own a ScrollViewer.");
                    Assert.IsTrue(viewer.ViewportHeight > 0, "Icon catalog needs a bounded viewport height.");
                    Assert.IsTrue(viewer.ExtentHeight > viewer.ViewportHeight, "Icon catalog should have a scrollable extent.");
                    Assert.IsTrue(viewer.ScrollableHeight > 0, "Icon catalog should be scrollable.");

                    int realizedBeforeScroll = CountVisualChildren<ListViewItem>(list);
                    Assert.IsTrue(realizedBeforeScroll > 0, "Initial viewport should realize some row containers.");
                    Assert.IsTrue(realizedBeforeScroll < list.Items.Count / 2, "Initial layout should not realize most icon rows.");
                    Assert.IsNull(list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1), "Last row should stay unrealized before scrolling.");

                    list.ScrollIntoView(list.Items[list.Items.Count - 1]);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.IsNotNull(list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1), "Last row should realize after scrolling into view.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static void AssertSourceTab(TabView tabs, string expectedHeader, string expectedSource)
        {
            foreach (object item in tabs.Items)
            {
                TabViewItem tab = item as TabViewItem;
                if (tab != null && string.Equals(tab.Header as string, expectedHeader, StringComparison.Ordinal))
                {
                    WpfButton copy = FindByName<WpfButton>(tab.Content as DependencyObject, "CopySourceButton");
                    Assert.IsNotNull(copy, "Source tab should expose a copy button: " + expectedHeader);
                    Assert.AreEqual(expectedSource, copy.Tag as string, "Copy button should keep the in-memory source text.");
                    return;
                }
            }

            Assert.Fail("Missing source tab: " + expectedHeader);
        }

        private static void EnsureTheme()
        {
            Application application = WpfTestSta.EnsureApplication();
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

            var demoShared = new ResourceDictionary
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
            };
            application.Resources.MergedDictionaries.Add(demoShared);
        }

        private static MainWindow CreateShownMainWindow()
        {
            var window = new MainWindow
            {
                Left = -20000,
                Top = -20000,
                Width = 1200,
                Height = 900,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false
            };
            window.Show();
            Drain(window.Dispatcher);
            window.UpdateLayout();
            Drain(window.Dispatcher);
            return window;
        }

        private static Window CreateHostWindow(UIElement content)
        {
            var window = new Window
            {
                Left = -20000,
                Top = -20000,
                Width = 1040,
                Height = 720,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false,
                Content = content
            };
            window.Show();
            Drain(window.Dispatcher);
            window.UpdateLayout();
            Drain(window.Dispatcher);
            return window;
        }

        private static object GetSelectedPageContent(MainWindow window)
        {
            NavigationView nav = FindByName<NavigationView>(window, "DemoNav");
            Assert.IsNotNull(nav, "DemoNav must exist.");

            var selected = nav.SelectedItem as NavigationViewItem;
            Assert.IsNotNull(selected, "A NavigationViewItem should be selected.");
            return selected.PageContent;
        }

        private static double GetVisualX(FrameworkElement element, Visual ancestor)
        {
            return element.TransformToAncestor(ancestor).Transform(new Point(0, 0)).X;
        }

        private static void Drain(Dispatcher dispatcher)
        {
            dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        }

        private static void AssertGridCell(Grid grid, Predicate<UIElement> match, int expectedRow, int expectedColumn, string name)
        {
            foreach (UIElement child in grid.Children)
            {
                if (match(child))
                {
                    Assert.AreEqual(expectedRow, Grid.GetRow(child), name + " should be in the expected row.");
                    Assert.AreEqual(expectedColumn, Grid.GetColumn(child), name + " should be in the expected column.");
                    return;
                }
            }

            Assert.Fail("Expected control was not found in the grid: " + name);
        }

        private static T FindByName<T>(DependencyObject root, string name)
            where T : FrameworkElement
        {
            foreach (T item in FindAllVisualChildren<T>(root))
            {
                if (string.Equals(item.Name, name, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private static IEnumerable<T> FindAllVisualChildren<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root == null)
            {
                yield break;
            }

            T current = root as T;
            if (current != null)
            {
                yield return current;
            }

            int visualCount = 0;
            try
            {
                visualCount = VisualTreeHelper.GetChildrenCount(root);
            }
            catch (InvalidOperationException)
            {
                visualCount = 0;
            }

            for (int i = 0; i < visualCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                foreach (T result in FindAllVisualChildren<T>(child))
                {
                    yield return result;
                }
            }

            foreach (object logicalChild in LogicalTreeHelper.GetChildren(root))
            {
                DependencyObject logical = logicalChild as DependencyObject;
                if (logical == null)
                {
                    continue;
                }

                foreach (T result in FindAllVisualChildren<T>(logical))
                {
                    yield return result;
                }
            }
        }

        private static T FindVisualChild<T>(DependencyObject root)
            where T : DependencyObject
        {
            foreach (T item in FindAllVisualChildren<T>(root))
            {
                return item;
            }

            return null;
        }

        private static int CountVisualChildren<T>(DependencyObject root)
            where T : DependencyObject
        {
            int count = 0;
            foreach (T item in FindAllVisualChildren<T>(root))
            {
                count++;
            }

            return count;
        }

        private sealed class DemoPageExpectation
        {
            public DemoPageExpectation(string tag, Type pageType)
            {
                Tag = tag;
                PageType = pageType;
            }

            public string Tag { get; private set; }

            public Type PageType { get; private set; }
        }
    }
}
