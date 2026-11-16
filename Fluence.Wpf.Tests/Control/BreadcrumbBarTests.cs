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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.BreadcrumbBar"/> /
    /// <see cref="Controls.BreadcrumbBarItem"/> family.
    /// </summary>
    public sealed class BreadcrumbBarTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        // Lightweight subclass that exposes the protected mouse button overrides so we
        // can assert crumb click semantics without relying on a real input device.
        private sealed class BreadcrumbBarItemProbe : Controls.BreadcrumbBarItem
        {
            public void SimulateMouseDown()
            {
                MouseButtonEventArgs args = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = MouseLeftButtonDownEvent,
                    Source = this,
                };
                OnMouseLeftButtonDown(args);
            }

            public void SimulateMouseUp()
            {
                MouseButtonEventArgs args = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = MouseLeftButtonUpEvent,
                    Source = this,
                };
                OnMouseLeftButtonUp(args);
            }
        }

        private sealed class CrumbModel(string label)
        {
            public string Label { get; } = label;
        }

        private sealed class CrumbTemplateSelector(DataTemplate template) : DataTemplateSelector
        {
            public override DataTemplate SelectTemplate(object item, DependencyObject container)
            {
                return template;
            }
        }

        [Fact]
        public Task BreadcrumbBar_DefaultStyle_GeneratesBreadcrumbBarItemContainersAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Style style = Assert.IsType<Style>(app.TryFindResource(typeof(Controls.BreadcrumbBar)));

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                string[] crumbs = ["Home", "Documents", "Design"];
                bar.ItemsSource = crumbs;

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(crumbs.Length, bar.Items.Count);
                    for (int index = 0; index < crumbs.Length; index++)
                    {
                        Controls.BreadcrumbBarItem container =
                            Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(index));
                        Assert.Equal(crumbs[index], container.Content);
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task BreadcrumbBar_LastItem_HidesChevronAndUsesPrimaryTypographyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                string[] crumbs = ["Home", "Documents", "Design"];
                bar.ItemsSource = crumbs;

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SolidColorBrush primaryBrush = Assert.IsType<SolidColorBrush>(app.TryFindResource("TextFillColorPrimaryBrush"));

                    for (int index = 0; index < crumbs.Length - 1; index++)
                    {
                        Controls.BreadcrumbBarItem ancestor =
                            Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(index));
                        Assert.False(ancestor.IsLastItem,
                            string.Format(CultureInfo.InvariantCulture, "The ancestor crumb at index {0} must not report IsLastItem.", index));

                        Controls.FontIcon chevron = Assert.IsType<Controls.FontIcon>(FindVisualChildByName<Controls.FontIcon>(ancestor, "ChevronIcon"), exactMatch: false);
                        Assert.Equal(Visibility.Visible, chevron.Visibility);

                        // WinUI BreadcrumbBarChevronLeftToRight is E974 painted in
                        // BreadcrumbBarNormalForegroundBrush (TextFillColorPrimaryBrush).
                        Assert.Equal("\uE974", chevron.Glyph, StringComparer.Ordinal);
                        SolidColorBrush chevronForeground = Assert.IsType<SolidColorBrush>(chevron.Foreground);
                        Assert.Equal(primaryBrush.Color, chevronForeground.Color);

                        SolidColorBrush ancestorForeground = Assert.IsType<SolidColorBrush>(ancestor.Foreground);
                        Assert.Equal(primaryBrush.Color, ancestorForeground.Color);
                    }

                    Controls.BreadcrumbBarItem last =
                        Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(crumbs.Length - 1));
                    Assert.True(last.IsLastItem, "The last crumb must report IsLastItem=true.");
                    Assert.Equal(FontWeights.SemiBold, last.FontWeight);

                    Controls.FontIcon lastChevron = Assert.IsType<Controls.FontIcon>(FindVisualChildByName<Controls.FontIcon>(last, "ChevronIcon"), exactMatch: false);
                    Assert.Equal(Visibility.Collapsed, lastChevron.Visibility);

                    SolidColorBrush lastForeground = Assert.IsType<SolidColorBrush>(last.Foreground);
                    Assert.Equal(primaryBrush.Color, lastForeground.Color);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task BreadcrumbBar_CrumbClick_RaisesItemClickedWithItemAndIndexAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new()
                {
                    ItemsSource = (string[])["Home", "Documents", "Design"],
                };

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    object? clickedItem = null;
                    int clickedIndex = -1;
                    int raiseCount = 0;
                    bar.ItemClicked += (_, args) =>
                    {
                        clickedItem = args.Item;
                        clickedIndex = args.Index;
                        raiseCount++;
                    };

                    Controls.BreadcrumbBarItem ancestor =
                        Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(1));

                    ancestor.RaiseEvent(new RoutedEventArgs(Controls.BreadcrumbBarItem.ClickEvent, ancestor));
                    Assert.Equal(1, raiseCount);
                    Assert.Equal("Documents", clickedItem);
                    Assert.Equal(1, clickedIndex);

                    Controls.BreadcrumbBarItem last =
                        Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(2));

                    last.RaiseEvent(new RoutedEventArgs(Controls.BreadcrumbBarItem.ClickEvent, last));
                    Assert.Equal(2, raiseCount);
                    Assert.Equal("Design", clickedItem);
                    Assert.Equal(2, clickedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task BreadcrumbBarItem_MouseAndKeyboard_ActivateCrumbAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                BreadcrumbBarItemProbe first = new() { Content = "Home" };
                BreadcrumbBarItemProbe second = new() { Content = "Documents" };
                _ = bar.Items.Add(first);
                _ = bar.Items.Add(second);

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.False(first.IsLastItem, "A directly added ancestor crumb must not report IsLastItem.");
                    Assert.True(second.IsLastItem, "The directly added final crumb must report IsLastItem.");

                    object? clickedItem = null;
                    int clickedIndex = -1;
                    int raiseCount = 0;
                    bar.ItemClicked += (_, args) =>
                    {
                        clickedItem = args.Item;
                        clickedIndex = args.Index;
                        raiseCount++;
                    };

                    first.SimulateMouseDown();
                    Assert.True(first.IsPressed, "IsPressed must flip true after a left-button press.");

                    first.SimulateMouseUp();
                    Assert.False(first.IsPressed, "IsPressed must reset after the left-button release.");
                    Assert.Equal(1, raiseCount);
                    Assert.Same(first, clickedItem);
                    Assert.Equal(0, clickedIndex);

                    _ = second.Focus();
                    PresentationSource source = Assert.IsType<PresentationSource>(PresentationSource.FromVisual(second), exactMatch: false);

                    second.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    Assert.True(second.IsPressed, "Enter key-down on a focused crumb must press it.");

                    second.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyUpEvent,
                    });
                    Assert.False(second.IsPressed, "Enter key-up must release the crumb.");
                    Assert.Equal(2, raiseCount);
                    Assert.Same(second, clickedItem);
                    Assert.Equal(1, clickedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task BreadcrumbBar_ItemsChanges_UpdateLastItemStateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                ObservableCollection<string> crumbs = ["Home", "Documents"];
                bar.ItemsSource = crumbs;

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.BreadcrumbBarItem documents =
                        Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(1));
                    Assert.True(documents.IsLastItem, "The final crumb must start with IsLastItem=true.");

                    crumbs.Add("Design");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                        () => bar.ItemContainerGenerator.ContainerFromIndex(2) is Controls.BreadcrumbBarItem { IsLastItem: true }).ConfigureAwait(true),
                        "Adding a crumb must realize a new last container with IsLastItem=true.");
                    Assert.False(documents.IsLastItem,
                        "The previously last crumb must lose IsLastItem after an append.");

                    Controls.FontIcon documentsChevron = Assert.IsType<Controls.FontIcon>(FindVisualChildByName<Controls.FontIcon>(documents, "ChevronIcon"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, documentsChevron.Visibility);

                    crumbs.RemoveAt(2);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.True(documents.IsLastItem,
                        "Removing the trailing crumb must promote the previous crumb back to IsLastItem=true.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Theory]
        [InlineData(FlowDirection.LeftToRight)]
        [InlineData(FlowDirection.RightToLeft)]
        public Task BreadcrumbBar_NarrowWidth_KeepsCurrentReachableAndRestoresTrailOnResizeAsync(FlowDirection direction)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new()
                {
                    Width = 150,
                    FlowDirection = direction,
                    ItemsSource = (string[])["Root", "Documents", "Projects", "Design", "Current page"],
                };

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Button overflow = Assert.IsType<Button>(bar.Template.FindName("PART_OverflowButton", bar));
                    ScrollViewer viewer = Assert.IsType<ScrollViewer>(bar.Template.FindName("PART_ItemsScrollViewer", bar));
                    Assert.True(overflow.Visibility is Visibility.Visible,
                        string.Format(CultureInfo.InvariantCulture,
                            "Overflow button must be visible. Extent={0}, viewport={1}, viewer width={2}, bar width={3}, state={4}.",
                            viewer.ExtentWidth, viewer.ViewportWidth, viewer.ActualWidth, bar.ActualWidth, overflow.Visibility));
                    Assert.True(overflow.IsEnabled);
                    AutomationPeer overflowPeer = Assert.IsType<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(overflow), exactMatch: false);
                    Assert.False(string.IsNullOrWhiteSpace(overflowPeer.GetName()));
                    Assert.Equal(AutomationControlType.Button, overflowPeer.GetAutomationControlType());
                    Assert.NotNull(overflowPeer.GetPattern(PatternInterface.Invoke));

                    Controls.BreadcrumbBarItem current = Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(4));
                    Assert.True(current.IsLastItem);
                    Rect currentBounds = current.TransformToAncestor(bar).TransformBounds(new Rect(current.RenderSize));
                    Rect viewportBounds = viewer.TransformToAncestor(bar).TransformBounds(new Rect(viewer.RenderSize));
                    Assert.True(currentBounds.Left >= viewportBounds.Left && currentBounds.Right <= viewportBounds.Right + 1,
                        "The current location must remain inside the constrained viewport.");
                    Assert.True(current.Focus(), "The current crumb must be reachable by keyboard focus.");
                    AutomationPeer currentPeer = Assert.IsType<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(current), exactMatch: false);
                    Assert.False(currentPeer.IsOffscreen());
                    Assert.Equal("Current page", currentPeer.GetName(), StringComparer.Ordinal);

                    object? clickedItem = null;
                    int clickedIndex = -1;
                    bool invokeReturned = false;
                    bool handlerObservedInvokeReturn = false;
                    bar.ItemClicked += (_, args) =>
                    {
                        clickedItem = args.Item;
                        clickedIndex = args.Index;
                        handlerObservedInvokeReturn = invokeReturned;
                    };
                    IInvokeProvider currentInvoke = Assert.IsType<IInvokeProvider>(currentPeer.GetPattern(PatternInterface.Invoke), exactMatch: false);
                    currentInvoke.Invoke();
                    invokeReturned = true;
                    Assert.Null(clickedItem);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Current page", clickedItem);
                    Assert.Equal(4, clickedIndex);
                    Assert.True(handlerObservedInvokeReturn, "UIA Invoke must return before the navigation handler runs.");
                    ContextMenu menu = Assert.IsType<ContextMenu>(overflow.ContextMenu, exactMatch: false);
                    menu.IsOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    MenuItem documents = Assert.Single(menu.Items.OfType<MenuItem>(), static item => Equals(item.Header, "Documents"));
                    documents.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, documents));
                    Assert.Equal("Documents", clickedItem);
                    Assert.Equal(1, clickedIndex);
                    menu.IsOpen = false;

                    bar.Width = 500;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(Visibility.Collapsed, overflow.Visibility);
                    for (int index = 0; index < bar.Items.Count; index++)
                    {
                        Controls.BreadcrumbBarItem crumb = Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(index));
                        Rect bounds = crumb.TransformToAncestor(bar).TransformBounds(new Rect(crumb.RenderSize));
                        Assert.True(bounds.Width > 0 && bounds.Left >= 0 && bounds.Right <= bar.ActualWidth + 1,
                            string.Format(CultureInfo.InvariantCulture, "Crumb {0} must return to the visible trail when the width is restored.", index));
                    }

                    bar.Width = 150;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(Visibility.Visible, overflow.Visibility);
                    currentBounds = current.TransformToAncestor(bar).TransformBounds(new Rect(current.RenderSize));
                    viewportBounds = viewer.TransformToAncestor(bar).TransformBounds(new Rect(viewer.RenderSize));
                    Assert.True(currentBounds.Left >= viewportBounds.Left && currentBounds.Right <= viewportBounds.Right + 1,
                        "The current location must remain reachable after shrinking the bar again.");

                    Controls.BreadcrumbBarItem root = Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(0));
                    Assert.True(root.Focus());
                    root.BringIntoView();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    bar.Width = 140;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Rect rootBounds = root.TransformToAncestor(bar).TransformBounds(new Rect(root.RenderSize));
                    viewportBounds = viewer.TransformToAncestor(bar).TransformBounds(new Rect(viewer.RenderSize));
                    Assert.True(rootBounds.Width > 0 && rootBounds.Left >= viewportBounds.Left && rootBounds.Right <= viewportBounds.Right + 1,
                        "Resizing must keep a focused ancestor within the viewport.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task BreadcrumbBar_ReplacingTemplate_ReleasesPreviousOverflowMenuAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new()
                {
                    Width = 150,
                    ItemsSource = (string[])["Root", "Documents", "Current page"],
                };

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Button originalButton = Assert.IsType<Button>(bar.Template.FindName("PART_OverflowButton", bar));
                    ContextMenu originalMenu = Assert.IsType<ContextMenu>(originalButton.ContextMenu, exactMatch: false);
                    bar.Template = new ControlTemplate(typeof(Controls.BreadcrumbBar))
                    {
                        VisualTree = new FrameworkElementFactory(typeof(Border)),
                    };
                    window.UpdateLayout();

                    Assert.Null(originalButton.ContextMenu);
                    Assert.Null(originalMenu.PlacementTarget);
                    Assert.False(originalMenu.IsOpen);
                    Assert.Empty(originalMenu.Items);

                    bar.ClearValue(System.Windows.Controls.Control.TemplateProperty);
                    window.UpdateLayout();
                    Button restoredButton = Assert.IsType<Button>(bar.Template.FindName("PART_OverflowButton", bar));
                    Assert.NotSame(originalButton, restoredButton);
                    Assert.NotNull(restoredButton.ContextMenu);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task BreadcrumbBar_OverflowMenu_UsesModelTemplateAndReadableNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                FrameworkElementFactory text = new(typeof(TextBlock));
                text.SetBinding(TextBlock.TextProperty, new Binding(nameof(CrumbModel.Label)));
                DataTemplate template = new(typeof(CrumbModel)) { VisualTree = text };
                CrumbTemplateSelector selector = new(template);
                CrumbModel[] crumbs = [new("Root"), new("Documents"), new("Current page")];
                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new()
                {
                    Width = 150,
                    ItemsSource = crumbs,
                    ItemTemplateSelector = selector,
                };

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Button overflow = Assert.IsType<Button>(bar.Template.FindName("PART_OverflowButton", bar));
                    Assert.Equal(Visibility.Visible, overflow.Visibility);
                    ContextMenu menu = Assert.IsType<ContextMenu>(overflow.ContextMenu, exactMatch: false);
                    menu.IsOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    MenuItem documents = Assert.Single(menu.Items.OfType<MenuItem>(),
                        static item => item.Header is CrumbModel model && string.Equals(model.Label, "Documents", StringComparison.Ordinal));
                    AutomationPeer peer = Assert.IsType<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(documents), exactMatch: false);
                    Assert.Equal("Documents", peer.GetName(), StringComparer.Ordinal);

                    object? clickedItem = null;
                    int clickedIndex = -1;
                    bar.ItemClicked += (_, args) => { clickedItem = args.Item; clickedIndex = args.Index; };
                    documents.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, documents));
                    Assert.Same(crumbs[1], clickedItem);
                    Assert.Equal(1, clickedIndex);
                    menu.IsOpen = false;
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task BreadcrumbBar_NamedIconContent_ProvidesReadableCrumbAndMenuNamesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.FontIcon icon = new() { Glyph = "\uE10F" };
                AutomationProperties.SetName(icon, "Reports");
                Border content = new() { Child = icon };
                Controls.BreadcrumbBarItem reports = new() { Content = content };
                Controls.BreadcrumbBar bar = new() { Width = 150 };
                _ = bar.Items.Add("Root");
                _ = bar.Items.Add(reports);
                _ = bar.Items.Add("Current page");
                Window window = new() { Width = 500, Height = 200 };

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer crumbPeer = Assert.IsType<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(reports), exactMatch: false);
                    Assert.Equal("Reports", crumbPeer.GetName(), StringComparer.Ordinal);

                    Button overflow = Assert.IsType<Button>(bar.Template.FindName("PART_OverflowButton", bar));
                    Assert.Equal(Visibility.Visible, overflow.Visibility);
                    ContextMenu menu = Assert.IsType<ContextMenu>(overflow.ContextMenu, exactMatch: false);
                    menu.IsOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    MenuItem reportEntry = Assert.IsType<MenuItem>(menu.Items[1], exactMatch: false);
                    Assert.Equal("Reports", reportEntry.Header);
                    AutomationPeer menuPeer = Assert.IsType<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(reportEntry), exactMatch: false);
                    Assert.Equal("Reports", menuPeer.GetName(), StringComparer.Ordinal);
                    menu.IsOpen = false;

                    TextBlock label = new() { Text = "Labelled reports" };
                    AutomationProperties.SetLabeledBy(reports, label);
                    Assert.Equal("Labelled reports", reports.GetAccessibleText(), StringComparer.Ordinal);
                    AutomationProperties.SetName(reports, "Explicit name");
                    Assert.Equal("Explicit name", reports.GetAccessibleText(), StringComparer.Ordinal);
                    AutomationProperties.SetName(reports, string.Empty);
                    AutomationProperties.SetLabeledBy(reports, new UIElement());
                    Assert.Equal("Reports", reports.GetAccessibleText(), StringComparer.Ordinal);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task BreadcrumbBar_ThemeCycle_CrumbBrushesResolveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                string[] brushKeys =
                [
                    "TextFillColorPrimaryBrush",
                    "TextFillColorSecondaryBrush",
                    "TextFillColorDisabledBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                    foreach (string? key in brushKeys)
                    {
                        Assert.NotNull(app.TryFindResource(key));
                    }
                }
            });
        }

        [Fact]
        public Task BreadcrumbBar_AutomationPeer_ReportsGroupClassNameAndNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new()
                {
                    ItemsSource = (string[])["Home", "Documents"],
                };
                AutomationProperties.SetName(bar, "Navigation breadcrumb");

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer peer = Assert.IsType<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(bar), exactMatch: false);
                    _ = Assert.IsType<Automation.BreadcrumbBarAutomationPeer>(peer, exactMatch: false);
                    Assert.Equal("BreadcrumbBar", peer.GetClassName(), StringComparer.Ordinal);
                    Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
                    Assert.Equal("Navigation breadcrumb", peer.GetName(), StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task BreadcrumbBarItem_Pressed_AnimatesContentPlatePressScaleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                BreadcrumbBarItemProbe first = new() { Content = "Home" };
                BreadcrumbBarItemProbe second = new() { Content = "Documents" };
                _ = bar.Items.Add(first);
                _ = bar.Items.Add(second);

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.False(first.IsLastItem, "The pressed crumb must be an ancestor (non-last) item.");

                    ScaleTransform pressScale = Assert.IsType<ScaleTransform>(first.Template.FindName("PressScale", first));
                    Assert.Equal(1.0, pressScale.ScaleX, 0.001);

                    // Press: the Button.xaml press-scale storyboard settles at 0.98.
                    first.SimulateMouseDown();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => pressScale.ScaleX <= 0.98 && pressScale.ScaleY <= 0.98).ConfigureAwait(true),
                        "Pressing a crumb must animate its content plate down to the 0.98 press scale.");

                    // Release: the release storyboard restores 1.0.
                    first.SimulateMouseUp();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => pressScale.ScaleX >= 1.0 && pressScale.ScaleY >= 1.0).ConfigureAwait(true),
                        "Releasing a crumb must animate its content plate back to 1.0 scale.");
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
