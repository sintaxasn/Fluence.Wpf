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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Demo.Pages;

namespace Fluence.Wpf.Demo
{
    public partial class MainWindow : FluenceWindow
    {
        internal const string GalleryWindowTitle = "Fluence.Wpf \u2014 Control Gallery";

        private readonly Dictionary<NavigationViewItem, DemoNavigationItem> _navigationItemByContainer =
            [];
        private readonly Dictionary<NavigationViewItem, object> _pageByContainer =
            [];
        private readonly List<NavigationViewItem> _navigationBackStack =
            [];
        private bool _userShowIcon;
        private bool _userShowTitle;
        private ImageSource? _userIcon;
        private string _userTitle;
        private bool _userNavBackButtonVisible;
        private bool _userNavPaneToggleButtonVisible;
        private bool _lastAppliedExtendedTitleBar;
        private bool _isApplyingTitleBarChrome;
        private bool _syncingPaneModeToggle;
        private bool _isNavigatingBack;
        private bool _isUpdatingExtendedTitleOverlap;
        private NavigationViewPaneDisplayMode _lastNonTopPaneDisplayMode = NavigationViewPaneDisplayMode.Left;
        private bool _lastNonTopIsPaneOpen = true;
        private NavigationViewItem? _currentNavigationItem;
        private Image? _titleBarIconView;
        private DependencyPropertyDescriptor? _extendsDpd;
        private DependencyPropertyDescriptor? _paneModeDpd;
        private DependencyPropertyDescriptor? _paneOpenDpd;
        private DependencyPropertyDescriptor? _backEnabledDpd;
        private DependencyPropertyDescriptor? _backVisibleDpd;
        private DependencyPropertyDescriptor? _paneToggleVisibleDpd;
        private object? _lastAnimatedPageContent;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3366:\"this\" should not be exposed from constructors", Justification = "This is only demo code.")]
        public MainWindow()
        {
            InitializeComponent();

            Title = GalleryWindowTitle;
            SystemThemeWatcher.Watch(this);
            ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica);

            _userShowIcon = ShowIcon;
            _userShowTitle = ShowTitle;
            _userIcon = Icon;
            _userTitle = Title;
            _userNavBackButtonVisible = DemoNav is not null && DemoNav.IsBackButtonVisible;
            _userNavPaneToggleButtonVisible = DemoNav is null || DemoNav.IsPaneToggleButtonVisible;
            CaptureNonTopPaneState();

            DemoNav?.SelectionChanged += DemoNav_SelectionChanged;

            PopulateNavigation();
            WatchTitleBarDependencies();
            ApplyTitleBarContentVisibility();
        }

        protected override void OnClosed(EventArgs e)
        {
            _extendsDpd?.RemoveValueChanged(this, OnTitleBarDependencyChanged);

            if (_paneModeDpd is not null && DemoNav is not null)
            {
                _paneModeDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            if (_paneOpenDpd is not null && DemoNav is not null)
            {
                _paneOpenDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            if (_backEnabledDpd is not null && DemoNav is not null)
            {
                _backEnabledDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            if (_backVisibleDpd is not null && DemoNav is not null)
            {
                _backVisibleDpd.RemoveValueChanged(DemoNav, OnNavigationBackVisibilityChanged);
            }

            if (_paneToggleVisibleDpd is not null && DemoNav is not null)
            {
                _paneToggleVisibleDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            DemoNav?.SelectionChanged -= DemoNav_SelectionChanged;

            base.OnClosed(e);
        }

        private void PopulateNavigation()
        {
            if (DemoNav is null)
            {
                return;
            }

            DemoNav.Items.Clear();
            _navigationItemByContainer.Clear();
            _pageByContainer.Clear();
            _navigationBackStack.Clear();
            _currentNavigationItem = null;

            NavigationViewItem? defaultItem = null;
            foreach (DemoNavigationItem item in DemoNavigationCatalog.Items)
            {
                NavigationViewItem navItem = CreateNavigationItem(item);
                _ = DemoNav.Items.Add(navItem);
                _navigationItemByContainer[navItem] = item;
                if (item.IsDefault)
                {
                    defaultItem = navItem;
                }
            }

            if (defaultItem is null && DemoNav.Items.Count > 0)
            {
                defaultItem = DemoNav.Items[0] as NavigationViewItem;
            }

            NavigateToItem(defaultItem);
            UpdateBackNavigationState();
        }

        private static NavigationViewItem CreateNavigationItem(DemoNavigationItem item)
        {
            return new NavigationViewItem
            {
                Content = item.Title,
                Tag = item.Route + " " + item.Keywords,
                Icon = new FontIcon { Glyph = item.Glyph, IconFontSize = 16 }
            };
        }

        private void DemoNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DemoNav.SelectedItem is not NavigationViewItem selected)
            {
                return;
            }

            if (!ReferenceEquals(_currentNavigationItem, selected))
            {
                if (!_isNavigatingBack && _currentNavigationItem is not null)
                {
                    _navigationBackStack.Add(_currentNavigationItem);
                }

                _currentNavigationItem = selected;
            }

            object? page = EnsurePageContent(selected);
            if (page is not null)
            {
                DemoNav.Content = page;
                AnimatePageInIfChanged(page);
            }

            UpdateBackNavigationState();
        }

        /// <summary>
        /// Selects the pane item whose title, route, or keywords contain the supplied tag.
        /// </summary>
        /// <param name="tag">Search tag such as "buttons", "progress ring", or "window".</param>
        public void NavigateTo(string tag)
        {
            if (DemoNav is null || string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            if (NavSearchBox is not null && !string.IsNullOrWhiteSpace(NavSearchBox.Text))
            {
                NavSearchBox.Text = string.Empty;
            }

            NavigateToItem(FindFirstMatchingItem(tag));
        }

        private void NavigateToItem(NavigationViewItem? item)
        {
            if (item is null || DemoNav is null)
            {
                return;
            }

            if (ReferenceEquals(DemoNav.SelectedItem, item) && EnsurePageContent(item) is object page)
            {
                _currentNavigationItem ??= item;
                DemoNav.Content = page;
                AnimatePageInIfChanged(page);
                UpdateBackNavigationState();
            }
            else
            {
                DemoNav.SelectedItem = item;
            }
        }

        private void UpdateBackNavigationState()
        {
            if (DemoNav is null)
            {
                return;
            }

            bool isBackEnabled = _navigationBackStack.Count > 0;
            if (DemoNav.IsBackEnabled != isBackEnabled)
            {
                DemoNav.IsBackEnabled = isBackEnabled;
            }

            ApplyTitleBarContentVisibility();
        }

        private object? EnsurePageContent(NavigationViewItem item)
        {
            if (item is null)
            {
                return null;
            }

            if (_pageByContainer.TryGetValue(item, out object? page))
            {
                return page;
            }

            if (!_navigationItemByContainer.TryGetValue(item, out DemoNavigationItem? metadata))
            {
                return null;
            }

            page = CreatePageForRoute(metadata.Route);
            _pageByContainer[item] = page;
            return page;
        }

        private static object CreatePageForRoute(string route)
        {
            return (route ?? string.Empty).ToLowerInvariant() switch
            {
                "home" => new GalleryHomePage(),
                "colors" => new GalleryColorsPage(),
                "iconography" => new GalleryGlyphsPage(),
                "typography" => new GalleryTypographyPage(),
                "accessibility" => new GalleryAccessibilityPage(),
                "buttons" => new GalleryButtonsPage(),
                "selection" => new GallerySelectionPage(),
                "inputs" => new GalleryInputsPage(),
                "forms" => new GalleryFormsPage(),
                "data" => new GalleryDataPage(),
                "data binding" => new GalleryDataBindingPage(),
                "trees" => new GalleryTreesPage(),
                "menus" => new GalleryMenusPage(),
                "navigation" => new GalleryNavigationPage(),
                "tabs" => new GalleryTabsPage(),
                "layout" => new GalleryLayoutPage(),
                "status" => new GalleryStatusPage(),
                "window" => new GalleryWindowPage(),
                _ => new GalleryHomePage(),
            };
        }

        private void NavSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyNavSearchFilter();
        }

        private void NavSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            string query = (NavSearchBox?.Text) ?? string.Empty;
            query = query.Trim();
            if (query.Length == 0)
            {
                return;
            }

            NavigationViewItem? match = FindFirstMatchingItem(query);
            if (match is not null)
            {
                NavigateToItem(match);
                e.Handled = true;
            }
        }

        private NavigationViewItem? FindFirstMatchingItem(string query)
        {
            if (DemoNav is null || string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            string trimmed = query.Trim();
            foreach (object obj in DemoNav.Items)
            {
                if (obj is not NavigationViewItem item)
                {
                    continue;
                }

                string title = (item.Content as string) ?? string.Empty;
                _ = _navigationItemByContainer.TryGetValue(item, out DemoNavigationItem? metadata);
                if (string.Equals(title, trimmed, StringComparison.OrdinalIgnoreCase) ||
                    (metadata is not null && string.Equals(metadata.Route, trimmed, StringComparison.OrdinalIgnoreCase)))
                {
                    return item;
                }

                if (ItemMatches(item, metadata, trimmed))
                {
                    return item;
                }
            }

            return null;
        }

        private static bool ItemMatches(NavigationViewItem item, DemoNavigationItem? metadata, string needle)
        {
            string title = (item.Content as string) ?? string.Empty;
            string tag = (item.Tag as string) ?? string.Empty;
            string route = metadata is null ? string.Empty : metadata.Route;
            string keywords = metadata is null ? string.Empty : metadata.Keywords;
            return ContainsOrdinalIgnoreCase(title + " " + tag + " " + route + " " + keywords, needle);
        }

        private static bool ContainsOrdinalIgnoreCase(string value, string needle)
        {
#if NET5_0_OR_GREATER
            return value.Contains(needle, StringComparison.OrdinalIgnoreCase);
#else
            return value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
#endif
        }

        private void ApplyNavSearchFilter()
        {
            if (DemoNav is null || NavSearchBox is null)
            {
                return;
            }

            string query = (NavSearchBox.Text ?? string.Empty).Trim();
            if (query.Length == 0)
            {
                foreach (object obj in DemoNav.Items)
                {
                    if (obj is NavigationViewItem item)
                    {
                        item.Visibility = Visibility.Visible;
                    }
                }

                return;
            }

            foreach (object obj in DemoNav.Items)
            {
                if (obj is not NavigationViewItem item)
                {
                    continue;
                }

                _ = _navigationItemByContainer.TryGetValue(item, out DemoNavigationItem? metadata);
                item.Visibility = ItemMatches(item, metadata, query)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void AnimatePageInIfChanged(object page)
        {
            if (page is null || ReferenceEquals(_lastAnimatedPageContent, page))
            {
                return;
            }

            _lastAnimatedPageContent = page;
            AnimatePageIn(page);
        }

        private static void AnimatePageIn(object page)
        {
            if (page is not UIElement element)
            {
                return;
            }

            element.BeginAnimation(OpacityProperty, null);
            element.RenderTransform = new TranslateTransform(0.0, 20.0);
            element.Opacity = 0.0;

            CubicEase easing = new() { EasingMode = EasingMode.EaseOut };
            DoubleAnimation opacityAnimation = new(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(160)))
            {
                EasingFunction = easing
            };
            opacityAnimation.Completed += delegate
            {
                element.BeginAnimation(OpacityProperty, null);
                element.Opacity = 1.0;
            };
            element.BeginAnimation(OpacityProperty, opacityAnimation);

            if (element.RenderTransform is TranslateTransform transform)
            {
                DoubleAnimation slideAnimation = new(20.0, 0.0, new Duration(TimeSpan.FromMilliseconds(167)))
                {
                    EasingFunction = easing
                };
                slideAnimation.Completed += delegate
                {
                    transform.BeginAnimation(TranslateTransform.YProperty, null);
                    transform.Y = 0.0;
                };
                transform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
            }
        }

        /// <summary>
        /// Records the user's intended title-bar icon visibility before layout rules are applied.
        /// </summary>
        /// <param name="show">Whether the icon should be visible when layout permits it.</param>
        /// <param name="icon">The icon to apply when visible.</param>
        public void SetUserShowIcon(bool show, ImageSource? icon)
        {
            _userShowIcon = show;
            _userIcon = icon;
            ApplyTitleBarContentVisibility();
        }

        private void PaneModeToggle_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_syncingPaneModeToggle || DemoNav is null || PaneModeToggle is null)
            {
                return;
            }

            if (PaneModeToggle.IsChecked == true)
            {
                if (DemoNav.PaneDisplayMode != NavigationViewPaneDisplayMode.Top)
                {
                    CaptureNonTopPaneState();
                    DemoNav.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                }

                return;
            }

            if (DemoNav.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                NavigationViewPaneDisplayMode targetMode = GetLastNonTopPaneDisplayMode();
                bool targetIsPaneOpen = _lastNonTopIsPaneOpen;
                DemoNav.PaneDisplayMode = targetMode;
                DemoNav.IsPaneOpen = targetIsPaneOpen;
            }
        }

        private NavigationViewPaneDisplayMode GetLastNonTopPaneDisplayMode()
        {
            return _lastNonTopPaneDisplayMode == NavigationViewPaneDisplayMode.Top
                ? NavigationViewPaneDisplayMode.Left
                : _lastNonTopPaneDisplayMode;
        }

        /// <summary>
        /// Records the user's intended title-bar title visibility before layout rules are applied.
        /// </summary>
        /// <param name="show">Whether the title should be visible when layout permits it.</param>
        /// <param name="title">The title text to apply when visible.</param>
        public void SetUserShowTitle(bool show, string title)
        {
            _userShowTitle = show;
            _userTitle = title;
            ApplyTitleBarContentVisibility();
        }

        private void WatchTitleBarDependencies()
        {
            _extendsDpd = DependencyPropertyDescriptor.FromProperty(
                ExtendsContentIntoTitleBarProperty, typeof(FluenceWindow));
            _extendsDpd?.AddValueChanged(this, OnTitleBarDependencyChanged);

            if (DemoNav is not null)
            {
                _paneModeDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.PaneDisplayModeProperty, typeof(NavigationView));
                _paneModeDpd?.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);

                _paneOpenDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsPaneOpenProperty, typeof(NavigationView));
                _paneOpenDpd?.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);

                _backEnabledDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsBackEnabledProperty, typeof(NavigationView));
                _backEnabledDpd?.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);

                _backVisibleDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsBackButtonVisibleProperty, typeof(NavigationView));
                _backVisibleDpd?.AddValueChanged(DemoNav, OnNavigationBackVisibilityChanged);

                _paneToggleVisibleDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsPaneToggleButtonVisibleProperty, typeof(NavigationView));
                _paneToggleVisibleDpd?.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }
        }

        private void OnTitleBarDependencyChanged(object? sender, EventArgs e)
        {
            if (sender == DemoNav && !_isApplyingTitleBarChrome)
            {
                CaptureNonTopPaneState();

                if (DemoNav.PaneDisplayMode != NavigationViewPaneDisplayMode.Top)
                {
                    if (ExtendsContentIntoTitleBar)
                    {
                        if (DemoNav.IsPaneToggleButtonVisible)
                        {
                            _userNavPaneToggleButtonVisible = true;
                        }
                    }
                    else
                    {
                        _userNavPaneToggleButtonVisible = DemoNav.IsPaneToggleButtonVisible;
                    }
                }
            }

            ApplyTitleBarContentVisibility();
        }

        private void CaptureNonTopPaneState()
        {
            if (DemoNav is null || DemoNav.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                return;
            }

            _lastNonTopPaneDisplayMode = DemoNav.PaneDisplayMode;
            _lastNonTopIsPaneOpen = DemoNav.IsPaneOpen;
        }

        private void OnNavigationBackVisibilityChanged(object? sender, EventArgs e)
        {
            if (sender == DemoNav && !_isApplyingTitleBarChrome)
            {
                _userNavBackButtonVisible = DemoNav.IsBackButtonVisible;
            }

            ApplyTitleBarContentVisibility();
        }

        private void ApplyTitleBarContentVisibility()
        {
            bool extendedTitleBar = ExtendsContentIntoTitleBar;
            bool shellTitleBarPresent = ShellTitleBar is not null;

            ShowIcon = !shellTitleBarPresent && !extendedTitleBar && _userShowIcon;
            ShowTitle = !shellTitleBarPresent && !extendedTitleBar && _userShowTitle;
            Icon = _userIcon;
            if (_userShowTitle && !string.IsNullOrWhiteSpace(_userTitle))
            {
                Title = _userTitle;
            }

            _ = (NavSearchBox?.Visibility = Visibility.Visible);

            if (DemoNav is not null)
            {
                bool titleBarOwnsBack = shellTitleBarPresent;
                _isApplyingTitleBarChrome = true;
                try
                {
                    if (titleBarOwnsBack)
                    {
                        DemoNav.IsBackButtonVisible = false;
                    }
                    else if (_lastAppliedExtendedTitleBar)
                    {
                        DemoNav.IsBackButtonVisible = _userNavBackButtonVisible;
                    }

                    if (extendedTitleBar)
                    {
                        DemoNav.IsPaneToggleButtonVisible = false;
                    }
                    else if (_lastAppliedExtendedTitleBar)
                    {
                        DemoNav.IsPaneToggleButtonVisible = _userNavPaneToggleButtonVisible;
                    }
                }
                finally
                {
                    _isApplyingTitleBarChrome = false;
                }
            }

            if (ShellTitleBar is not null)
            {
                ShellTitleBar.Title = _userShowTitle ? (_userTitle ?? string.Empty) : string.Empty;
                if (_userShowIcon && _userIcon is not null)
                {
                    ShellTitleBar.Icon = GetTitleBarIconView();
                }
                else
                {
                    ShellTitleBar.ClearValue(Controls.TitleBar.IconProperty);
                }

                ShellTitleBar.IsBackButtonVisible = _userNavBackButtonVisible
                    && DemoNav is not null
                    && DemoNav.IsBackEnabled;
                ShellTitleBar.IsPaneToggleButtonVisible = extendedTitleBar
                    && _userNavPaneToggleButtonVisible
                    && DemoNav is not null
                    && DemoNav.PaneDisplayMode != NavigationViewPaneDisplayMode.Top;
            }

            SyncPaneModeToggle();
            ScheduleExtendedTitleOverlapCheck();
            _lastAppliedExtendedTitleBar = extendedTitleBar;
        }

        private void SyncPaneModeToggle()
        {
            if (DemoNav is null || PaneModeToggle is null)
            {
                return;
            }

            bool shouldBeChecked = DemoNav.PaneDisplayMode == NavigationViewPaneDisplayMode.Top;
            if (PaneModeToggle.IsChecked == shouldBeChecked)
            {
                return;
            }

            _syncingPaneModeToggle = true;
            try
            {
                PaneModeToggle.IsChecked = shouldBeChecked;
            }
            finally
            {
                _syncingPaneModeToggle = false;
            }
        }

        private void TitleBarLayout_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleExtendedTitleOverlapCheck();
        }

        private Image GetTitleBarIconView()
        {
            if (_titleBarIconView is null)
            {
                _titleBarIconView = new Image
                {
                    Width = 20,
                    Height = 20,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(_titleBarIconView, BitmapScalingMode.HighQuality);
            }

            _titleBarIconView.Source = _userIcon;
            return _titleBarIconView;
        }

        private void ShellTitleBar_PaneToggleRequested(object sender, EventArgs e)
        {
            _ = (DemoNav?.IsPaneOpen = !DemoNav.IsPaneOpen);
        }

        private void ShellTitleBar_BackRequested(object sender, EventArgs e)
        {
            if (DemoNav is null || _navigationBackStack.Count == 0)
            {
                UpdateBackNavigationState();
                return;
            }

            NavigationViewItem previousItem = _navigationBackStack[_navigationBackStack.Count - 1];
            _navigationBackStack.RemoveAt(_navigationBackStack.Count - 1);

            _isNavigatingBack = true;
            try
            {
                NavigateToItem(previousItem);
            }
            finally
            {
                _isNavigatingBack = false;
                UpdateBackNavigationState();
            }
        }

        private void ScheduleExtendedTitleOverlapCheck()
        {
            _ = Dispatcher.BeginInvoke(new Action(UpdateExtendedTitleOverlap), DispatcherPriority.Loaded);
        }

        private void UpdateExtendedTitleOverlap()
        {
            if (_isUpdatingExtendedTitleOverlap)
            {
                return;
            }

            _isUpdatingExtendedTitleOverlap = true;
            try
            {
                if (!ExtendsContentIntoTitleBar || ShellTitleBar is null)
                {
                    return;
                }

                string desiredTitle = _userShowTitle ? (_userTitle ?? string.Empty) : string.Empty;
                if (string.IsNullOrWhiteSpace(desiredTitle))
                {
                    ShellTitleBar.Title = string.Empty;
                    return;
                }

                if (!string.Equals(ShellTitleBar.Title, desiredTitle, StringComparison.Ordinal))
                {
                    ShellTitleBar.Title = desiredTitle;
                    _ = ShellTitleBar.ApplyTemplate();
                    ShellTitleBar.UpdateLayout();
                    NavSearchBox?.UpdateLayout();
                }

                System.Windows.Controls.TextBlock? titleText = GetTitleBarTemplatePart<System.Windows.Controls.TextBlock>("PART_TitleText");
                if (titleText is null
                    || NavSearchBox is null
                    || titleText.Visibility != Visibility.Visible
                    || NavSearchBox.Visibility != Visibility.Visible
                    || !titleText.IsVisible
                    || !NavSearchBox.IsVisible)
                {
                    return;
                }

                if (!TryGetVisualX(titleText, this, out double titleLeft)
                    || !TryGetVisualX(NavSearchBox, this, out double searchLeft))
                {
                    return;
                }

                ContentPresenter? titleIcon = GetTitleBarTemplatePart<ContentPresenter>("PART_IconPresenter");
                if (titleIcon is not null
                    && titleIcon.Visibility == Visibility.Visible
                    && titleIcon.IsVisible
                    && TryGetVisualX(titleIcon, this, out double titleIconLeft)
                    && titleIconLeft + titleIcon.ActualWidth > searchLeft - 12.0)
                {
                    titleText.ClearValue(MaxWidthProperty);
                    ShellTitleBar.Title = string.Empty;
                    return;
                }

                double availableTitleWidth = searchLeft - titleLeft - 12.0;
                if (availableTitleWidth < 48.0)
                {
                    titleText.ClearValue(MaxWidthProperty);
                    ShellTitleBar.Title = string.Empty;
                    return;
                }

                titleText.MaxWidth = availableTitleWidth;
                titleText.UpdateLayout();
                if (!TryGetVisualX(titleText, this, out titleLeft))
                {
                    return;
                }

                double titleRight = titleLeft + titleText.ActualWidth;
                if (titleRight > searchLeft - 11.0)
                {
                    ShellTitleBar.Title = string.Empty;
                }
            }
            finally
            {
                _isUpdatingExtendedTitleOverlap = false;
            }
        }

        private static bool TryGetVisualX(FrameworkElement element, Visual ancestor, out double x)
        {
            x = 0.0;
            if (!IsVisualAncestorOf(ancestor, element))
            {
                return false;
            }

            GeneralTransform transform = element.TransformToAncestor(ancestor);
            x = transform.Transform(new Point(0, 0)).X;
            return true;
        }

        private static bool IsVisualAncestorOf(Visual ancestor, DependencyObject descendant)
        {
            DependencyObject? current = descendant;
            while (current is not null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                if (current is not Visual and not System.Windows.Media.Media3D.Visual3D)
                {
                    return false;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static T? FindVisualChildByName<T>(DependencyObject? root, string name)
            where T : FrameworkElement
        {
            if (root is null)
            {
                return null;
            }

            if (root is T current && string.Equals(current.Name, name, StringComparison.Ordinal))
            {
                return current;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                T? match = FindVisualChildByName<T>(VisualTreeHelper.GetChild(root, i), name);
                if (match is not null)
                {
                    return match;
                }
            }

            return null;
        }

        private T? GetTitleBarTemplatePart<T>(string partName)
            where T : FrameworkElement
        {
            if (ShellTitleBar is null)
            {
                return null;
            }

            _ = ShellTitleBar.ApplyTemplate();
            return ShellTitleBar.Template is null ? null : ShellTitleBar.Template.FindName(partName, ShellTitleBar) as T;
        }

    }
}
