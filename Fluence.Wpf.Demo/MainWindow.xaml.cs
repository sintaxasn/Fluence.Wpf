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
        private bool _userShowIcon;
        private bool _userShowTitle;
        private ImageSource? _userIcon;
        private string _userTitle;
        private bool _userNavBackButtonVisible;
        private bool _userNavPaneToggleButtonVisible;
        private bool _lastAppliedExtendedTitleBar;
        private bool _isApplyingTitleBarChrome;
        private bool _isUpdatingExtendedTitleOverlap;
        private Image? _titleBarIconView;
        private DependencyPropertyDescriptor? _extendsDpd;
        private DependencyPropertyDescriptor? _paneModeDpd;
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

            if (_backEnabledDpd is not null && DemoNav is not null)
            {
                _backEnabledDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            if (_backVisibleDpd is not null && DemoNav is not null)
            {
                _backVisibleDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
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
        }

        private static NavigationViewItem CreateNavigationItem(DemoNavigationItem item)
        {
            return new NavigationViewItem
            {
                Content = item.Title,
                Tag = item.Route + " " + item.Keywords,
                Icon = new FontIcon { Glyph = item.Glyph, IconFontSize = 20 }
            };
        }

        private void DemoNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DemoNav.SelectedItem is not NavigationViewItem selected)
            {
                return;
            }

            object? page = EnsurePageContent(selected);
            if (page is not null)
            {
                DemoNav.Content = page;
                AnimatePageInIfChanged(page);
            }
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
                DemoNav.Content = page;
                AnimatePageInIfChanged(page);
            }
            else
            {
                DemoNav.SelectedItem = item;
            }
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

        private void NavSearchBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {

            // No idea why this is empty.
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

                _backEnabledDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsBackEnabledProperty, typeof(NavigationView));
                _backEnabledDpd?.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);

                _backVisibleDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsBackButtonVisibleProperty, typeof(NavigationView));
                _backVisibleDpd?.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);

                _paneToggleVisibleDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsPaneToggleButtonVisibleProperty, typeof(NavigationView));
                _paneToggleVisibleDpd?.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }
        }

        private void OnTitleBarDependencyChanged(object? sender, EventArgs e)
        {
            if (sender == DemoNav && !_isApplyingTitleBarChrome)
            {
                if (ExtendsContentIntoTitleBar)
                {
                    if (DemoNav.IsBackButtonVisible)
                    {
                        _userNavBackButtonVisible = true;
                    }

                    if (DemoNav.IsPaneToggleButtonVisible)
                    {
                        _userNavPaneToggleButtonVisible = true;
                    }
                }
                else
                {
                    _userNavBackButtonVisible = DemoNav.IsBackButtonVisible;
                    _userNavPaneToggleButtonVisible = DemoNav.IsPaneToggleButtonVisible;
                }
            }

            ApplyTitleBarContentVisibility();
        }

        private void ApplyTitleBarContentVisibility()
        {
            bool extendedTitleBar = ExtendsContentIntoTitleBar;

            ShowIcon = !extendedTitleBar && _userShowIcon;
            ShowTitle = !extendedTitleBar && _userShowTitle;
            Icon = _userIcon;
            if (_userShowTitle && !string.IsNullOrWhiteSpace(_userTitle))
            {
                Title = _userTitle;
            }

            _ = (NavSearchBox?.Visibility = Visibility.Visible);

            if (DemoNav is not null)
            {
                _isApplyingTitleBarChrome = true;
                try
                {
                    if (extendedTitleBar)
                    {
                        DemoNav.IsBackButtonVisible = false;
                        DemoNav.IsPaneToggleButtonVisible = false;
                    }
                    else if (_lastAppliedExtendedTitleBar)
                    {
                        DemoNav.IsBackButtonVisible = _userNavBackButtonVisible;
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
                ShellTitleBar.Title = extendedTitleBar && _userShowTitle ? (_userTitle ?? string.Empty) : string.Empty;
                if (extendedTitleBar && _userShowIcon && _userIcon is not null)
                {
                    ShellTitleBar.Icon = GetTitleBarIconView();
                }
                ShellTitleBar.IsBackButtonVisible = extendedTitleBar
                    && _userNavBackButtonVisible
                    && DemoNav is not null
                    && DemoNav.IsBackEnabled;
                ShellTitleBar.IsPaneToggleButtonVisible = extendedTitleBar
                    && _userNavPaneToggleButtonVisible
                    && DemoNav is not null
                    && DemoNav.PaneDisplayMode != NavigationViewPaneDisplayMode.Top;
            }

            ScheduleExtendedTitleOverlapCheck();
            _lastAppliedExtendedTitleBar = extendedTitleBar;
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
            if (DemoNav is not null && DemoNav.IsBackEnabled)
            {
                NavigateTo("home");
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

                Point titlePoint = titleText.TransformToAncestor(this).Transform(new Point(0, 0));
                Point searchPoint = NavSearchBox.TransformToAncestor(this).Transform(new Point(0, 0));
                double titleRight = titlePoint.X + titleText.ActualWidth;
                double searchLeft = searchPoint.X;
                ShellTitleBar.Title = titleRight + 12.0 > searchLeft ? string.Empty : desiredTitle;
            }
            catch (InvalidOperationException)
            {
                // No idea what we're swallowing here.
            }
            finally
            {
                _isUpdatingExtendedTitleOverlap = false;
            }
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
