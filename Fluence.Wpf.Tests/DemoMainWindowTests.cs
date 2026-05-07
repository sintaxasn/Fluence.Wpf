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
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using FluenceExpander = Fluence.Wpf.Controls.Expander;
using FluenceListView = Fluence.Wpf.Controls.ListView;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfButton = System.Windows.Controls.Button;
using System.Linq;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public sealed class DemoMainWindowTests
    {
        private static readonly DemoPageExpectation[] PageExpectations =
        [
            new("colors", typeof(GalleryColorsPage)),
            new("iconography", typeof(GalleryGlyphsPage)),
            new("typography", typeof(GalleryTypographyPage)),
            new("accessibility", typeof(GalleryAccessibilityPage)),
            new("buttons", typeof(GalleryButtonsPage)),
            new("selection", typeof(GallerySelectionPage)),
            new("inputs", typeof(GalleryInputsPage)),
            new("data binding", typeof(GalleryDataBindingPage)),
            new("data", typeof(GalleryDataPage)),
            new("trees", typeof(GalleryTreesPage)),
            new("menus", typeof(GalleryMenusPage)),
            new("navigation", typeof(GalleryNavigationPage)),
            new("tabs", typeof(GalleryTabsPage)),
            new("layout", typeof(GalleryLayoutPage)),
            new("status", typeof(GalleryStatusPage)),
            new("window", typeof(GalleryWindowPage))
        ];

        private static void RunOnSta(Action action)
        {
            Exception? captured = null;
            WpfTestSta.Dispatcher?.Invoke(new Action(delegate
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

            if (captured is not null)
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

                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    Assert.AreSame(content, nav.Content, "NavigationView.Content should be populated for the initial Home page.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryHomePage_BrandBannerImageSwitchesWithTheme()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryHomePage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Image? image = FindByName<Image>(page, "BrandBannerImage");
                    Assert.IsNotNull(image, "Home page should expose the brand banner image.");
                    Assert.IsInstanceOfType(image.Source, typeof(BitmapImage), "The light banner PNG should load as an image source.");
                    Assert.AreEqual("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/fluence-wpf-banner-light.png", image.Tag as string,
                        "Light theme should use the light banner graphic.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.IsInstanceOfType(image.Source, typeof(BitmapImage), "The dark banner PNG should load as an image source.");
                    Assert.AreEqual("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/fluence-wpf-banner-dark.png", image.Tag as string,
                        "Dark theme should use the dark banner graphic.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/fluence-wpf-banner-light.png", image.Tag as string,
                        "Returning to light theme should restore the light banner graphic.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryHomePage_UsesPngBannerResourcesAndGitHubLink()
        {
            string project = ReadRepositoryFile("Fluence.Wpf.Demo", "Fluence.Wpf.Demo.csproj");
            StringAssert.Contains(project, "<Resource Include=\"Resources\\fluence-wpf-banner-*.png\" />");
            StringAssert.Contains(project, "<Page Remove=\"Resources\\fluence-wpf-banner-*.xaml\" />");

            string homePage = ReadRepositoryFile("Fluence.Wpf.Demo", "Pages", "GalleryHomePage.xaml");
            StringAssert.Contains(homePage, "https://github.com/sintaxasn/fluence.wpf");
        }

        [TestMethod]
        public void DemoProjects_UseSharedFluenceIcoIcon()
        {
            const string iconPath = @"Resources\fluence-wpf-appicon-256.ico";

            AssertProjectUsesIcon("Fluence.Wpf.Demo", "Fluence.Wpf.Demo.csproj", iconPath);
            AssertProjectUsesIcon("Fluence.Wpf.Demo.Mvvm", "Fluence.Wpf.Demo.Mvvm.csproj", iconPath);

            StringAssert.Contains(ReadRepositoryFile("Fluence.Wpf.Demo", "MainWindow.xaml"),
                "Icon=\"Resources/fluence-wpf-appicon-256.ico\"");
            StringAssert.Contains(ReadRepositoryFile("Fluence.Wpf.Demo.Mvvm", "MainWindow.xaml"),
                "Icon=\"Resources/fluence-wpf-appicon-256.ico\"");

            Assert.IsTrue(File.Exists(GetRepositoryFilePath("Fluence.Wpf.Demo", "Resources", "fluence-wpf-appicon-256.ico")),
                "The gallery demo icon should exist.");
            Assert.IsTrue(File.Exists(GetRepositoryFilePath("Fluence.Wpf.Demo.Mvvm", "Resources", "fluence-wpf-appicon-256.ico")),
                "The MVVM demo icon should exist.");
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
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
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
            List<DemoNavigationItem> items = [.. DemoNavigationCatalog.Items];
            Assert.IsTrue(items.Count >= 2, "Navigation catalog should contain at least two entries.");
            Assert.AreEqual("Accessibility", items[items.Count - 2].Title,
                "Accessibility should be second-last in the NavigationView list.");
            Assert.AreEqual("Windowing", items[items.Count - 1].Title,
                "Windowing should remain the final NavigationView item.");
        }

        [TestMethod]
        public void MainWindow_TitleBarSearch_StaysVisibleWhenContentExtendsIntoTitleBar()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(search, "Demo search box must be present.");
                    Assert.AreEqual(Visibility.Visible, search.Visibility, "Search should start visible in the normal title bar.");

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(Visibility.Visible, search.Visibility,
                        "Search should stay visible when content extends into the title bar.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_TitleBarSearch_IsCenteredInWindow()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should use the shared TitleBar control.");
                    Assert.IsNotNull(search, "Demo search box must be present.");
                    Assert.AreEqual(window.ActualWidth / 2.0, GetVisualCenterX(search, window) ?? double.MaxValue, 1.0,
                        "Search should stay horizontally centered in the window.");
                    Assert.AreEqual((GetVisualCenterY(shellTitleBar, window) ?? double.MinValue) + 2.0, GetVisualCenterY(search, window) ?? double.MaxValue, 1.0,
                        "Search should sit 2px below the title-bar vertical center.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_ExtendedTitleBar_UsesHorizontalNavigationChrome()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should use the shared TitleBar control.");

                    WpfButton? titleBarToggle = FindByName<WpfButton>(shellTitleBar, "PART_PaneToggleButton");
                    Assert.IsNotNull(titleBarToggle, "Extended title bar should expose a pane toggle button.");
                    Assert.AreEqual(Visibility.Visible, titleBarToggle.Visibility,
                        "Pane toggle should move into the title bar when content extends into the title bar.");
                    Assert.AreEqual(42.0, titleBarToggle.ActualWidth, 0.5,
                        "Title-bar pane toggle should match the compact title-bar glyph slot.");

                    WpfTextBlock? titleBarGlyph = FindVisualChild<WpfTextBlock>(titleBarToggle);
                    Assert.IsNotNull(titleBarGlyph, "Title-bar pane toggle should render a Segoe Fluent Icons glyph.");
                    Assert.AreEqual(16.0, titleBarGlyph.FontSize, 0.01,
                        "Title-bar pane toggle glyph should match the compact title-bar glyph style.");

                    WpfButton? titleBarBack = FindByName<WpfButton>(shellTitleBar, "PART_BackButton");
                    Assert.IsNotNull(titleBarBack, "Extended title bar should expose a back button slot.");
                    Assert.AreEqual(Visibility.Collapsed, titleBarBack.Visibility,
                        "Back button should collapse in the title bar when no back route is enabled.");

                    NavigationViewItem? firstItem = nav.Items.Count > 0 ? nav.Items[0] as NavigationViewItem : null;
                    Assert.IsNotNull(firstItem, "DemoNav should contain a first navigation item.");
                    FontIcon? itemGlyph = FindVisualChild<FontIcon>(firstItem);
                    Assert.IsNotNull(itemGlyph, "First navigation item should render an icon.");
                    Assert.AreEqual(GetVisualCenterX(itemGlyph, window) ?? double.MaxValue, GetVisualCenterX(titleBarGlyph, window) ?? double.MaxValue, 2.5,
                        "Title-bar pane toggle glyph should align with the NavigationViewItem glyph rail.");

                    ContentPresenter? titleIcon = FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter");
                    Assert.IsNotNull(titleIcon, "Extended title bar icon presenter should exist.");
                    Assert.AreEqual(Visibility.Visible, titleIcon.Visibility,
                        "Extended title bar icon should be visible by default.");
                    Image? titleIconImage = FindVisualChild<Image>(titleIcon);
                    Assert.IsNotNull(titleIconImage, "Extended title bar icon should render an Image.");
                    Assert.AreEqual(20.0, titleIconImage.ActualWidth, 0.5,
                        "Extended title bar icon should match the larger navigation glyph size.");
                    Assert.AreEqual(20.0, titleIconImage.ActualHeight, 0.5,
                        "Extended title bar icon should match the larger navigation glyph size.");
                    Assert.IsTrue(GetVisualX(titleIcon, window) >= GetVisualX(titleBarToggle, window) + titleBarToggle.ActualWidth - 0.5,
                        "Title identity should start after the title-bar navigation slot.");

                    _ = nav.ApplyTemplate();
                    WpfButton? internalToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as WpfButton;
                    Assert.IsNotNull(internalToggle, "Internal NavigationView pane toggle should still exist in the template.");
                    Assert.AreEqual(Visibility.Collapsed, internalToggle.Visibility,
                        "Internal NavigationView pane toggle should be hidden while title-bar chrome owns it.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_ExtendedTitleBar_FirstGlyphTracksBackAvailability()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    nav.IsBackButtonVisible = true;
                    nav.IsBackEnabled = true;

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should use the shared TitleBar control.");
                    WpfButton? titleBarBack = FindByName<WpfButton>(shellTitleBar, "PART_BackButton");
                    WpfButton? titleBarToggle = FindByName<WpfButton>(shellTitleBar, "PART_PaneToggleButton");
                    Assert.IsNotNull(titleBarBack, "Extended title bar should expose a back button.");
                    Assert.IsNotNull(titleBarToggle, "Extended title bar should expose a pane toggle button.");
                    Assert.AreEqual(Visibility.Visible, titleBarBack.Visibility,
                        "Back should be visible in the title bar when back navigation is enabled.");
                    Assert.AreEqual(Visibility.Visible, titleBarToggle.Visibility,
                        "Pane toggle should remain visible after back appears.");
                    Assert.IsTrue((GetVisualX(titleBarBack, window) ?? double.MaxValue) < (GetVisualX(titleBarToggle, window) ?? double.MaxValue),
                        "Back should occupy the first title-bar navigation slot.");
                    Assert.AreEqual(GetVisualY(titleBarBack, window) ?? double.MaxValue, GetVisualY(titleBarToggle, window) ?? double.MaxValue, 1.0,
                        "Back and pane toggle should be arranged in the same title-bar row.");

                    ContentPresenter? titleIcon = FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter");
                    Assert.IsNotNull(titleIcon, "Extended title bar icon should exist.");
                    Assert.AreEqual(Visibility.Visible, titleIcon.Visibility,
                        "Extended title bar icon should be visible while tracking title identity reflow.");
                    double? titleIconWithBackX = GetVisualX(titleIcon, window);

                    nav.IsBackEnabled = false;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(Visibility.Collapsed, titleBarBack.Visibility,
                        "Back must collapse in the title bar when back navigation is disabled.");
                    Assert.AreEqual((titleIconWithBackX ?? double.MaxValue) - 46.0, GetVisualX(titleIcon, window) ?? double.MaxValue, 1.5,
                        "Title identity should shift left by one rail slot when the back glyph collapses.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_ExtendedTitleBar_KeepsNavigationItemsBelowTitleBar()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");

                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(42.0, window.TitleBarHeight, 0.01,
                        "The demo shell should use a compact 42px title bar.");

                    NavigationViewItem? firstItem = nav.Items.Count > 0 ? nav.Items[0] as NavigationViewItem : null;
                    Assert.IsNotNull(firstItem, "DemoNav should contain a first navigation item.");
                    double? itemY = GetVisualY(firstItem, window);
                    Assert.IsTrue(itemY >= window.TitleBarHeight - 0.5,
                        "The first navigation item should be below the extended title bar. itemY=" + itemY + ", titleBarHeight=" + window.TitleBarHeight);
                    Assert.IsTrue(itemY <= window.TitleBarHeight + 14.0,
                        "The first navigation item should not keep the old extra title-bar spacer. itemY=" + itemY + ", titleBarHeight=" + window.TitleBarHeight);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_NonExtendedTitleBar_UsesPaneChromeAboveNavigationItems()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
                    Assert.IsNotNull(nav, "DemoNav must exist.");
                    window.ExtendsContentIntoTitleBar = false;
                    nav.IsPaneToggleButtonVisible = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    _ = nav.ApplyTemplate();
                    nav.IsBackEnabled = true;
                    nav.IsBackButtonVisible = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    WpfButton? internalBack = nav.Template.FindName(NavigationView.PartBackButton, nav) as WpfButton;
                    WpfButton? internalToggle = nav.Template.FindName(NavigationView.PartPaneToggleButton, nav) as WpfButton;
                    Assert.IsNotNull(internalBack, "Internal NavigationView back button should exist.");
                    Assert.IsNotNull(internalToggle, "Internal NavigationView pane toggle should exist.");
                    Assert.AreEqual(Visibility.Visible, internalBack.Visibility,
                        "Non-extended mode should use the NavigationView back button.");
                    Assert.AreEqual(Visibility.Visible, internalToggle.Visibility,
                        "Non-extended mode should use the NavigationView pane toggle.");
                    Assert.IsTrue((GetVisualX(internalBack, window) ?? double.MaxValue) < (GetVisualX(internalToggle, window) ?? double.MaxValue),
                        "Internal back button should be the first glyph in the pane chrome row.");
                    Assert.AreEqual(GetVisualY(internalBack, window) ?? double.MaxValue, GetVisualY(internalToggle, window) ?? double.MaxValue, 1.0,
                        "Internal back and pane toggle should be arranged in a horizontal row.");

                    NavigationViewItem? firstItem = nav.Items.Count > 0 ? nav.Items[0] as NavigationViewItem : null;
                    Assert.IsNotNull(firstItem, "DemoNav should contain a first navigation item.");
                    Assert.IsTrue((GetVisualY(firstItem, window) ?? double.MinValue) > (GetVisualY(internalBack, window) ?? double.MinValue) + internalBack.ActualHeight - 0.5,
                        "Navigation items should start below the non-extended pane chrome row.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_ExtendedTitleBar_HidesTitleTextWhenItOverlapsSearch()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    window.Width = 760;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(true, window.Icon);
                    window.SetUserShowTitle(true, "Fluence.Wpf Control Gallery Extended Title That Should Not Overlap The Search Box");
                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should use the shared TitleBar control.");
                    ContentPresenter? titleIcon = FindByName<ContentPresenter>(shellTitleBar, "PART_IconPresenter");
                    WpfTextBlock? titleText = FindByName<WpfTextBlock>(shellTitleBar, "PART_TitleText");
                    Assert.IsNotNull(titleIcon, "Extended title bar icon should exist.");
                    Assert.IsNotNull(titleText, "Extended title bar title should exist.");
                    Assert.AreEqual(Visibility.Visible, titleIcon.Visibility,
                        "Title icon should remain visible when title text is hidden for search clearance.");
                    Assert.AreEqual(Visibility.Collapsed, titleText.Visibility,
                        "Title text should hide when its bounds would overlap the search box.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void MainWindow_ExtendedTitleBar_RestoresTitleTextWhenSearchHasRoom()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                MainWindow window = CreateShownMainWindow();
                try
                {
                    window.Width = 760;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    window.SetUserShowIcon(true, window.Icon);
                    window.SetUserShowTitle(true, "Fluence.Wpf Control Gallery Extended Title That Should Not Overlap The Search Box");
                    window.ExtendsContentIntoTitleBar = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    TitleBar? shellTitleBar = FindByName<TitleBar>(window, "ShellTitleBar");
                    Assert.IsNotNull(shellTitleBar, "Extended title bar should use the shared TitleBar control.");
                    WpfTextBlock? titleText = FindByName<WpfTextBlock>(shellTitleBar, "PART_TitleText");
                    Assert.IsNotNull(titleText, "Extended title bar title should exist.");
                    Assert.AreEqual(Visibility.Collapsed, titleText.Visibility,
                        "Setup should hide title text while the search collision exists.");

                    window.Width = 1200;
                    window.SetUserShowTitle(true, "Fluence.Wpf");
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Drain(window.Dispatcher);

                    titleText = FindByName<WpfTextBlock>(shellTitleBar, "PART_TitleText");
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(search, "Demo search box must be present.");
                    Assert.AreEqual(Visibility.Visible, titleText?.Visibility,
                        "Title text should return when it can fit without touching the search box.");
                    Assert.AreEqual("Fluence.Wpf", titleText?.Text,
                        "The visible title should use the current user title.");
                    Assert.IsTrue(GetVisualX(titleText, window) + titleText?.ActualWidth + 12.0 <= GetVisualX(search, window),
                        "Visible title text should keep the search clearance gap.");
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
                    Controls.TextBox? search = FindByName<Controls.TextBox>(window, "NavSearchBox");
                    Assert.IsNotNull(search, "Demo search box must be present.");

                    double? initialX = GetVisualX(search, window);

                    window.SetUserShowIcon(false, window.Icon);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.AreEqual(initialX ?? double.MaxValue, GetVisualX(search, window) ?? double.MaxValue, 1.0,
                        "Search should not shift when the demo hides the icon.");

                    window.SetUserShowTitle(false, window.Title);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.AreEqual(initialX ?? double.MaxValue, GetVisualX(search, window) ?? double.MaxValue, 1.0,
                        "Search should not shift when the demo hides the title.");

                    window.IsMinimizeButtonVisible = Visibility.Collapsed;
                    window.IsMaximizeButtonVisible = Visibility.Collapsed;
                    window.IsCloseButtonVisible = Visibility.Collapsed;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.AreEqual(initialX ?? double.MaxValue, GetVisualX(search, window) ?? double.MaxValue, 1.0,
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
                DemoSampleControl sample = new()
                {
                    Title = "Snippet",
                    XamlSource = "<ui:Button Content=\"Save\" />",
                    CSharpSource = "private void Save_Click(object sender, RoutedEventArgs e) { }",
                    SampleContent = new WpfTextBlock { Text = "Visible sample" }
                };

                Window window = CreateHostWindow(sample);
                try
                {
                    FluenceExpander? expander = FindByName<FluenceExpander>(sample, "SourceExpander");
                    Assert.IsNotNull(expander, "Inline source expander must exist.");
                    Assert.IsFalse(expander.IsExpanded, "Source starts collapsed.");

                    expander.IsExpanded = true;
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    TabView? tabs = FindByName<TabView>(sample, "SourceTabs");
                    Assert.IsNotNull(tabs, "Expanded source creates a TabView.");
                    Assert.AreEqual(2, tabs.Items.Count, "XAML plus C# source should create two tabs.");
                    AssertSourceTab(tabs, "XAML", sample.XamlSource);
                    AssertSourceTab(tabs, "C# Code-behind", sample.CSharpSource);

                    Card? sampleCard = FindByName<Card>(sample, "SampleCard");
                    Assert.IsNotNull(sampleCard, "Sample host should expose the sample card.");
                    Assert.AreEqual(new CornerRadius(8, 8, 0, 0), sampleCard.CornerRadius,
                        "Sample card should square off its bottom corners so source attaches.");
                    Assert.AreEqual(new CornerRadius(0, 0, 8, 8), expander.CornerRadius,
                        "Source expander should square off its top corners so it joins the card.");
                    Assert.AreEqual(new Thickness(1, 0, 1, 1), expander.BorderThickness,
                        "Source expander should share the card seam without a duplicate top stroke.");
                    Assert.AreEqual((GetVisualY(sampleCard, window) ?? double.MinValue) + sampleCard.ActualHeight, GetVisualY(expander, window) ?? double.MinValue, 0.5,
                        "Source expander should be attached directly below the sample card.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DemoSampleControl_ReplaceSourceLink_ReplacesOwningCard()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                System.Windows.Controls.StackPanel host = new();
                System.Windows.Controls.StackPanel cardContent = new();
                _ = cardContent.Children.Add(new WpfTextBlock { Text = "Visible sample" });
                WpfButton sourceLink = new()
                {
                    Name = "InlineSourceLink",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Content = "Source"
                };
                _ = cardContent.Children.Add(sourceLink);
                Card card = new()
                {
                    Margin = new Thickness(0, 0, 0, 16),
                    Padding = new Thickness(16),
                    Content = cardContent
                };
                _ = host.Children.Add(card);

                DemoSampleControl sample = DemoSampleControl.ReplaceSourceLink(
                    sourceLink,
                    "<ui:Button Content=\"Save\" />",
                    string.Empty);

                Window window = CreateHostWindow(host);
                try
                {
                    Assert.AreSame(sample, host.Children[0],
                        "Replacing a source link inside a card should replace the owning card, not nest a sample host inside it.");
                    Assert.AreSame(cardContent, sample.SampleContent,
                        "The original card body should become the sample content.");
                    Assert.IsFalse(cardContent.Children.Contains(sourceLink),
                        "The old source-link button should be removed from the sample body.");

                    FluenceExpander? expander = FindByName<FluenceExpander>(sample, "SourceExpander");
                    Card? sampleCard = FindByName<Card>(sample, "SampleCard");
                    Assert.IsNotNull(expander);
                    Assert.IsNotNull(sampleCard);
                    Assert.AreEqual((GetVisualY(sampleCard, window) ?? double.MinValue) + sampleCard.ActualHeight, GetVisualY(expander, window) ?? double.MinValue, 0.5,
                        "Source expander should be attached directly below the replaced card body.");
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
                DemoSampleControl sample = new()
                {
                    Title = "Snippet",
                    XamlSource = "<ui:ToggleSwitch IsChecked=\"True\" />"
                };

                Window window = CreateHostWindow(sample);
                try
                {
                    FluenceExpander? expander = FindByName<FluenceExpander>(sample, "SourceExpander");
                    _ = (expander?.IsExpanded = true);
                    Drain(window.Dispatcher);
                    window.UpdateLayout();

                    TabView? tabs = FindByName<TabView>(sample, "SourceTabs");
                    Assert.AreEqual(1, tabs?.Items.Count, "XAML-only samples should not show an empty C# tab.");
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
                        DependencyObject? root = content as DependencyObject;
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
        public void GalleryWindowPage_UsesCompactThemeAndCaptionLayout()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryWindowPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    System.Windows.Controls.ComboBox? backdrop = FindByName<System.Windows.Controls.ComboBox>(page, "BackdropCombo");
                    UniformGrid? accentRow = FindByName<UniformGrid>(page, "AccentSwatchRow");
                    System.Windows.Controls.ComboBox? minimize = FindByName<System.Windows.Controls.ComboBox>(page, "MinimizeVisibilityCombo");
                    System.Windows.Controls.ComboBox? maximize = FindByName<System.Windows.Controls.ComboBox>(page, "MaximizeVisibilityCombo");
                    System.Windows.Controls.ComboBox? close = FindByName<System.Windows.Controls.ComboBox>(page, "CloseVisibilityCombo");
                    FrameworkElement? showIcon = FindByName<FrameworkElement>(page, "ShowWindowIconToggle");
                    FrameworkElement? showTitle = FindByName<FrameworkElement>(page, "ShowWindowTitleToggle");

                    Assert.IsNotNull(backdrop, "Backdrop picker should live in the theme card.");
                    Assert.IsNotNull(accentRow, "Accent swatches should use a named single-row host.");
                    Assert.IsNotNull(minimize, "Minimize caption picker should exist.");
                    Assert.IsNotNull(maximize, "Maximize caption picker should exist.");
                    Assert.IsNotNull(close, "Close caption picker should exist.");
                    Assert.IsNotNull(showIcon, "Show Icon toggle should exist.");
                    Assert.IsNotNull(showTitle, "Show Title toggle should exist.");
                    Assert.AreEqual(7, accentRow.Children.Count, "The Window page accent picker should expose seven logo accent swatches.");
                    Assert.AreEqual(GetVisualY(accentRow.Children[0] as FrameworkElement, window) ?? double.MaxValue, GetVisualY(accentRow.Children[6] as FrameworkElement, window) ?? double.MaxValue, 1.0,
                        "All accent swatches should fit on one row.");
                    Assert.AreEqual(GetVisualY(minimize, window) ?? double.MaxValue, GetVisualY(maximize, window) ?? double.MaxValue, 1.0,
                        "Minimize and Maximize caption controls should be on the same row.");
                    Assert.AreEqual(GetVisualY(minimize, window) ?? double.MaxValue, GetVisualY(close, window) ?? double.MaxValue, 1.0,
                        "Close should be on the same row as the other caption controls.");
                    Assert.AreEqual(GetVisualY(showIcon, window) ?? double.MaxValue, GetVisualY(showTitle, window) ?? double.MaxValue, 1.0,
                        "Show Icon and Show Title should share their own row.");
                    Assert.IsTrue((GetVisualY(showIcon, window) ?? double.MinValue) > (GetVisualY(minimize, window) ?? double.MinValue),
                        "Show Icon and Show Title should be arranged below the caption-button row.");
                    Assert.IsNull(FindByName<FrameworkElement>(page, "TitleBarChromeSourceLink"),
                        "The TitleBar Chrome section should be removed from the Window page.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void GalleryWindowPage_RainbowAccentSwatches_PreserveLogoColors()
        {
            RunOnSta(delegate
            {
                EnsureTheme();
                GalleryWindowPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    UniformGrid? accentRow = FindByName<UniformGrid>(page, "AccentSwatchRow");
                    Assert.IsNotNull(accentRow, "Accent swatches should use a named single-row host.");

                    string[] expected =
                    [
                        "#E80000",
                        "#F58809",
                        "#F5E70C",
                        "#2BDE11",
                        "#09C4DE",
                        "#AA04DE",
                        "#FF00E8"
                    ];

                    Assert.AreEqual(expected.Length, accentRow.Children.Count,
                        "The Window page accent picker should expose the seven rainbow swatches.");

                    for (int i = 0; i < expected.Length; i++)
                    {
                        FrameworkElement? swatch = accentRow.Children[i] as FrameworkElement;
                        Assert.IsNotNull(swatch, "Each accent swatch should be a FrameworkElement.");
                        Assert.AreEqual(expected[i], swatch.Tag as string,
                            "The Window page swatches should stay in rainbow order.");

                        object converted = ColorConverter.ConvertFromString(expected[i]);
                        Assert.IsInstanceOfType(converted, typeof(Color), "Swatch Tag should be a valid color: " + expected[i]);
                    }
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    ApplicationAccentColorManager.ApplyApplicationAccent();
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
                GalleryAccessibilityPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    Grid? primary = FindByName<Grid>(page, "KeyboardSupportPrimaryControls");
                    Assert.IsNotNull(primary, "Accessibility keyboard sample should use a named alignment grid.");
                    Assert.AreEqual(4, primary.ColumnDefinitions.Count,
                        "Primary keyboard sample should have four equal columns.");
                    Assert.AreEqual(2, primary.RowDefinitions.Count,
                        "Primary keyboard sample should have two aligned rows.");
                    Assert.AreEqual(8, primary.Children.Count,
                        "Primary keyboard sample should contain four controls per row.");

                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is Controls.Button button && string.Equals(button.Content as string, "Button 1", StringComparison.Ordinal);
                    }, 0, 0, "Button 1");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is Controls.Button button && string.Equals(button.Content as string, "Button 2", StringComparison.Ordinal);
                    }, 0, 1, "Button 2");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is Controls.TextBox;
                    }, 0, 2, "TextBox");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is Controls.ComboBox;
                    }, 0, 3, "ComboBox");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is Controls.CheckBox;
                    }, 1, 0, "CheckBox");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is ToggleSwitch;
                    }, 1, 1, "ToggleSwitch");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is Controls.Slider;
                    }, 1, 2, "Slider");
                    AssertGridCell(primary, delegate (UIElement child)
                    {
                        return child is HyperlinkButton;
                    }, 1, 3, "HyperlinkButton");

                    Grid? tabOrder = FindByName<Grid>(page, "KeyboardSupportExplicitOrderControls");
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
                GalleryGlyphsPage page = new();
                Window window = CreateHostWindow(page);
                try
                {
                    FluenceListView? list = FindByName<FluenceListView>(page, "IconCatalogList");
                    Assert.IsNotNull(list, "Icon catalog list must exist.");
                    Assert.IsTrue(list.Items.Count > 100, "Icon catalog must load enough rows to exercise virtualization.");

                    ScrollViewer? viewer = FindVisualChild<ScrollViewer>(list);
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

        private static void AssertSourceTab(TabView? tabs, string expectedHeader, string expectedSource)
        {
            if (tabs is null)
            {
                return;
            }
            foreach (object item in tabs.Items)
            {
                if (item is TabViewItem tab && string.Equals(tab.Header as string, expectedHeader, StringComparison.Ordinal))
                {
                    WpfButton? copy = FindByName<WpfButton>(tab.Content as DependencyObject, "CopySourceButton");
                    Assert.IsNotNull(copy, "Source tab should expose a copy button: " + expectedHeader);
                    Assert.AreEqual(expectedSource, copy.Tag as string, "Copy button should keep the in-memory source text.");
                    return;
                }
            }

            Assert.Fail("Missing source tab: " + expectedHeader);
        }

        private static void EnsureTheme()
        {
            Application? application = WpfTestSta.EnsureApplication();
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application?.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

            ResourceDictionary demoShared = new()
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
            };
            application?.Resources.MergedDictionaries.Add(demoShared);
        }

        private static void AssertProjectUsesIcon(string projectDirectory, string projectFile, string iconPath)
        {
            string project = ReadRepositoryFile(projectDirectory, projectFile);
            StringAssert.Contains(project, "<ApplicationIcon>" + iconPath + "</ApplicationIcon>");
            StringAssert.Contains(project, "<Resource Include=\"" + iconPath + "\" />");
        }

        private static string GetRepositoryFilePath(params string[] relativeSegments)
        {
            string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
            string[] pathParts = new string[relativeSegments.Length + 1];
            pathParts[0] = root;
            Array.Copy(relativeSegments, 0, pathParts, 1, relativeSegments.Length);
            return Path.Combine(pathParts);
        }

        private static string ReadRepositoryFile(params string[] relativeSegments)
        {
            string path = GetRepositoryFilePath(relativeSegments);
            Assert.IsTrue(File.Exists(path), "Repository file must be readable at: " + path);
            return File.ReadAllText(path);
        }

        private static MainWindow CreateShownMainWindow()
        {
            MainWindow window = new()
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
            Window window = new()
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
            NavigationView? nav = FindByName<NavigationView>(window, "DemoNav");
            Assert.IsNotNull(nav, "DemoNav must exist.");

            Assert.IsNotNull(nav.SelectedItem as NavigationViewItem, "A NavigationViewItem should be selected.");
            return nav.Content;
        }

        private static double? GetVisualX(FrameworkElement? element, Visual ancestor)
        {
            return element?.TransformToAncestor(ancestor).Transform(new Point(0, 0)).X;
        }

        private static double? GetVisualY(FrameworkElement? element, Visual ancestor)
        {
            return element?.TransformToAncestor(ancestor).Transform(new Point(0, 0)).Y;
        }

        private static double? GetVisualCenterX(FrameworkElement element, Visual ancestor)
        {
            return GetVisualX(element, ancestor) + (element.ActualWidth / 2.0);
        }

        private static double? GetVisualCenterY(FrameworkElement element, Visual ancestor)
        {
            return GetVisualY(element, ancestor) + (element.ActualHeight / 2.0);
        }

        private static void Drain(Dispatcher dispatcher)
        {
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
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

        private static T? FindByName<T>(DependencyObject? root, string name)
            where T : FrameworkElement
        {
            if (root is FrameworkElement element)
            {
                if (element.FindName(name) is T named)
                {
                    return named;
                }
            }

            foreach (T item in FindAllVisualChildren<T>(root))
            {
                if (string.Equals(item.Name, name, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private static IEnumerable<T> FindAllVisualChildren<T>(DependencyObject? root)
            where T : DependencyObject
        {
            HashSet<DependencyObject> visited = [];
            foreach (T result in FindAllVisualChildren<T>(root, visited))
            {
                yield return result;
            }
        }

        private static IEnumerable<T> FindAllVisualChildren<T>(DependencyObject? root, HashSet<DependencyObject> visited)
            where T : DependencyObject
        {
            if (root is null)
            {
                yield break;
            }

            if (visited.Contains(root))
            {
                yield break;
            }

            _ = visited.Add(root);

            if (root is T current)
            {
                yield return current;
            }

            int visualCount;
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
                foreach (T result in FindAllVisualChildren<T>(child, visited))
                {
                    yield return result;
                }
            }

            foreach (object logicalChild in LogicalTreeHelper.GetChildren(root))
            {
                if (logicalChild is not DependencyObject logical)
                {
                    continue;
                }

                foreach (T result in FindAllVisualChildren<T>(logical, visited))
                {
                    yield return result;
                }
            }
        }

        private static T? FindVisualChild<T>(DependencyObject root)
            where T : DependencyObject
        {
            return FindAllVisualChildren<T>(root).FirstOrDefault();
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

        private sealed class DemoPageExpectation(string tag, Type pageType)
        {
            public string Tag { get; private set; } = tag;

            public Type PageType { get; private set; } = pageType;
        }
    }
}
