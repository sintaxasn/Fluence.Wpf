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
            new Dictionary<NavigationViewItem, DemoNavigationItem>();
        private readonly Dictionary<NavigationViewItem, object> _pageByContainer =
            new Dictionary<NavigationViewItem, object>();
        private bool _userShowIcon;
        private bool _userShowTitle;
        private ImageSource _userIcon;
        private string _userTitle;
        private bool _userNavBackButtonVisible;
        private bool _userNavPaneToggleButtonVisible;
        private bool _lastAppliedExtendedTitleBar;
        private bool _isApplyingTitleBarChrome;
        private bool _isUpdatingExtendedTitleOverlap;
        private Image _titleBarIconView;
        private DependencyPropertyDescriptor _extendsDpd;
        private DependencyPropertyDescriptor _paneModeDpd;
        private DependencyPropertyDescriptor _backEnabledDpd;
        private DependencyPropertyDescriptor _backVisibleDpd;
        private DependencyPropertyDescriptor _paneToggleVisibleDpd;
        private object _lastAnimatedPageContent;

        public MainWindow()
        {
            InitializeComponent();

            Title = GalleryWindowTitle;
            SystemThemeWatcher.Watch(this);
            ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica, true);

            _userShowIcon = ShowIcon;
            _userShowTitle = ShowTitle;
            _userIcon = Icon;
            _userTitle = Title;
            _userNavBackButtonVisible = DemoNav != null && DemoNav.IsBackButtonVisible;
            _userNavPaneToggleButtonVisible = DemoNav == null || DemoNav.IsPaneToggleButtonVisible;

            if (DemoNav != null)
            {
                DemoNav.SelectionChanged += DemoNav_SelectionChanged;
            }

            PopulateNavigation();
            WatchTitleBarDependencies();
            ApplyTitleBarContentVisibility();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_extendsDpd != null)
            {
                _extendsDpd.RemoveValueChanged(this, OnTitleBarDependencyChanged);
            }

            if (_paneModeDpd != null && DemoNav != null)
            {
                _paneModeDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            if (_backEnabledDpd != null && DemoNav != null)
            {
                _backEnabledDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            if (_backVisibleDpd != null && DemoNav != null)
            {
                _backVisibleDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            if (_paneToggleVisibleDpd != null && DemoNav != null)
            {
                _paneToggleVisibleDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
            }

            if (DemoNav != null)
            {
                DemoNav.SelectionChanged -= DemoNav_SelectionChanged;
            }

            base.OnClosed(e);
        }

        private void PopulateNavigation()
        {
            if (DemoNav == null)
            {
                return;
            }

            DemoNav.Items.Clear();
            _navigationItemByContainer.Clear();
            _pageByContainer.Clear();

            NavigationViewItem defaultItem = null;
            foreach (DemoNavigationItem item in DemoNavigationCatalog.Items)
            {
                NavigationViewItem navItem = CreateNavigationItem(item);
                DemoNav.Items.Add(navItem);
                _navigationItemByContainer[navItem] = item;
                if (item.IsDefault)
                {
                    defaultItem = navItem;
                }
            }

            if (defaultItem == null && DemoNav.Items.Count > 0)
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
            var selected = DemoNav.SelectedItem as NavigationViewItem;
            if (selected == null)
            {
                return;
            }

            var page = EnsurePageContent(selected);
            if (page != null)
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
            if (DemoNav == null || string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            if (NavSearchBox != null && !string.IsNullOrEmpty(NavSearchBox.Text))
            {
                NavSearchBox.Text = string.Empty;
            }

            NavigateToItem(FindFirstMatchingItem(tag));
        }

        private void NavigateToItem(NavigationViewItem item)
        {
            if (item == null || DemoNav == null)
            {
                return;
            }

            var page = EnsurePageContent(item);
            if (!ReferenceEquals(DemoNav.SelectedItem, item))
            {
                DemoNav.SelectedItem = item;
            }
            else
            {
                DemoNav.Content = page;
                AnimatePageInIfChanged(page);
            }
        }

        private object EnsurePageContent(NavigationViewItem item)
        {
            if (item == null)
            {
                return null;
            }

            object page;
            if (_pageByContainer.TryGetValue(item, out page))
            {
                return page;
            }

            DemoNavigationItem metadata;
            if (!_navigationItemByContainer.TryGetValue(item, out metadata))
            {
                return null;
            }

            page = CreatePageForRoute(metadata.Route);
            _pageByContainer[item] = page;
            return page;
        }

        private static object CreatePageForRoute(string route)
        {
            switch ((route ?? string.Empty).ToLowerInvariant())
            {
                case "home":
                    return new GalleryHomePage();
                case "colors":
                    return new GalleryColorsPage();
                case "iconography":
                    return new GalleryGlyphsPage();
                case "typography":
                    return new GalleryTypographyPage();
                case "accessibility":
                    return new GalleryAccessibilityPage();
                case "buttons":
                    return new GalleryButtonsPage();
                case "selection":
                    return new GallerySelectionPage();
                case "inputs":
                    return new GalleryInputsPage();
                case "forms":
                    return new GalleryFormsPage();
                case "data":
                    return new GalleryDataPage();
                case "data binding":
                    return new GalleryDataBindingPage();
                case "trees":
                    return new GalleryTreesPage();
                case "menus":
                    return new GalleryMenusPage();
                case "navigation":
                    return new GalleryNavigationPage();
                case "tabs":
                    return new GalleryTabsPage();
                case "layout":
                    return new GalleryLayoutPage();
                case "status":
                    return new GalleryStatusPage();
                case "window":
                    return new GalleryWindowPage();
                default:
                    return new GalleryHomePage();
            }
        }

        private void NavSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyNavSearchFilter();
        }

        private void NavSearchBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
        }

        private void NavSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            var query = (NavSearchBox == null ? null : NavSearchBox.Text) ?? string.Empty;
            query = query.Trim();
            if (query.Length == 0)
            {
                return;
            }

            var match = FindFirstMatchingItem(query);
            if (match != null)
            {
                NavigateToItem(match);
                e.Handled = true;
            }
        }

        private NavigationViewItem FindFirstMatchingItem(string query)
        {
            if (DemoNav == null || string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            var trimmed = query.Trim();
            NavigationViewItem fallback = null;
            foreach (object obj in DemoNav.Items)
            {
                var item = obj as NavigationViewItem;
                if (item == null)
                {
                    continue;
                }

                var title = (item.Content as string) ?? string.Empty;
                DemoNavigationItem metadata;
                _navigationItemByContainer.TryGetValue(item, out metadata);
                if (string.Equals(title, trimmed, StringComparison.OrdinalIgnoreCase) ||
                    (metadata != null && string.Equals(metadata.Route, trimmed, StringComparison.OrdinalIgnoreCase)))
                {
                    return item;
                }

                if (fallback == null && ItemMatches(item, metadata, trimmed))
                {
                    fallback = item;
                }
            }

            return fallback;
        }

        private static bool ItemMatches(NavigationViewItem item, DemoNavigationItem metadata, string needle)
        {
            var title = (item.Content as string) ?? string.Empty;
            var tag = (item.Tag as string) ?? string.Empty;
            var route = metadata == null ? string.Empty : metadata.Route;
            var keywords = metadata == null ? string.Empty : metadata.Keywords;
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
            if (DemoNav == null || NavSearchBox == null)
            {
                return;
            }

            var query = (NavSearchBox.Text ?? string.Empty).Trim();
            if (query.Length == 0)
            {
                foreach (object obj in DemoNav.Items)
                {
                    var item = obj as NavigationViewItem;
                    if (item != null)
                    {
                        item.Visibility = Visibility.Visible;
                    }
                }

                return;
            }

            foreach (object obj in DemoNav.Items)
            {
                var item = obj as NavigationViewItem;
                if (item == null)
                {
                    continue;
                }

                DemoNavigationItem metadata;
                _navigationItemByContainer.TryGetValue(item, out metadata);
                item.Visibility = ItemMatches(item, metadata, query)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void AnimatePageInIfChanged(object page)
        {
            if (page == null || ReferenceEquals(_lastAnimatedPageContent, page))
            {
                return;
            }

            _lastAnimatedPageContent = page;
            AnimatePageIn(page);
        }

        private static void AnimatePageIn(object page)
        {
            var element = page as UIElement;
            if (element == null)
            {
                return;
            }

            element.BeginAnimation(UIElement.OpacityProperty, null);
            element.RenderTransform = new TranslateTransform(0.0, 20.0);
            element.Opacity = 0.0;

            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            var opacityAnimation = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(160)))
            {
                EasingFunction = easing
            };
            opacityAnimation.Completed += delegate
            {
                element.BeginAnimation(UIElement.OpacityProperty, null);
                element.Opacity = 1.0;
            };
            element.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);

            var transform = element.RenderTransform as TranslateTransform;
            if (transform != null)
            {
                var slideAnimation = new DoubleAnimation(20.0, 0.0, new Duration(TimeSpan.FromMilliseconds(167)))
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
        public void SetUserShowIcon(bool show, ImageSource icon)
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
                FluenceWindow.ExtendsContentIntoTitleBarProperty, typeof(FluenceWindow));
            if (_extendsDpd != null)
            {
                _extendsDpd.AddValueChanged(this, OnTitleBarDependencyChanged);
            }

            if (DemoNav != null)
            {
                _paneModeDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.PaneDisplayModeProperty, typeof(NavigationView));
                if (_paneModeDpd != null)
                {
                    _paneModeDpd.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);
                }

                _backEnabledDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsBackEnabledProperty, typeof(NavigationView));
                if (_backEnabledDpd != null)
                {
                    _backEnabledDpd.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);
                }

                _backVisibleDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsBackButtonVisibleProperty, typeof(NavigationView));
                if (_backVisibleDpd != null)
                {
                    _backVisibleDpd.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);
                }

                _paneToggleVisibleDpd = DependencyPropertyDescriptor.FromProperty(
                    NavigationView.IsPaneToggleButtonVisibleProperty, typeof(NavigationView));
                if (_paneToggleVisibleDpd != null)
                {
                    _paneToggleVisibleDpd.AddValueChanged(DemoNav, OnTitleBarDependencyChanged);
                }
            }
        }

        private void OnTitleBarDependencyChanged(object sender, EventArgs e)
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
            if (_userShowTitle && !string.IsNullOrEmpty(_userTitle))
            {
                Title = _userTitle;
            }

            if (NavSearchBox != null)
            {
                NavSearchBox.Visibility = Visibility.Visible;
            }

            if (DemoNav != null)
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

            if (ShellTitleBar != null)
            {
                ShellTitleBar.Title = extendedTitleBar && _userShowTitle ? (_userTitle ?? string.Empty) : string.Empty;
                ShellTitleBar.Icon = extendedTitleBar && _userShowIcon && _userIcon != null
                    ? GetTitleBarIconView()
                    : null;
                ShellTitleBar.IsBackButtonVisible = extendedTitleBar
                    && _userNavBackButtonVisible
                    && DemoNav != null
                    && DemoNav.IsBackEnabled;
                ShellTitleBar.IsPaneToggleButtonVisible = extendedTitleBar
                    && _userNavPaneToggleButtonVisible
                    && DemoNav != null
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
            if (_titleBarIconView == null)
            {
                _titleBarIconView = new Image
                {
                    Width = 16,
                    Height = 16,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(_titleBarIconView, BitmapScalingMode.HighQuality);
            }

            _titleBarIconView.Source = _userIcon;
            return _titleBarIconView;
        }

        private void ShellTitleBar_PaneToggleRequested(object sender, EventArgs e)
        {
            if (DemoNav != null)
            {
                DemoNav.IsPaneOpen = !DemoNav.IsPaneOpen;
            }
        }

        private void ShellTitleBar_BackRequested(object sender, EventArgs e)
        {
            if (DemoNav != null && DemoNav.IsBackEnabled)
            {
                NavigateTo("home");
            }
        }

        private void ScheduleExtendedTitleOverlapCheck()
        {
            Dispatcher.BeginInvoke(new Action(UpdateExtendedTitleOverlap), DispatcherPriority.Loaded);
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
                if (!ExtendsContentIntoTitleBar || ShellTitleBar == null)
                {
                    return;
                }

                string desiredTitle = _userShowTitle ? (_userTitle ?? string.Empty) : string.Empty;
                if (string.IsNullOrEmpty(desiredTitle))
                {
                    ShellTitleBar.Title = string.Empty;
                    return;
                }

                if (!string.Equals(ShellTitleBar.Title, desiredTitle, StringComparison.Ordinal))
                {
                    ShellTitleBar.Title = desiredTitle;
                    ShellTitleBar.ApplyTemplate();
                    ShellTitleBar.UpdateLayout();
                    if (NavSearchBox != null)
                    {
                        NavSearchBox.UpdateLayout();
                    }
                }

                System.Windows.Controls.TextBlock titleText = GetTitleBarTemplatePart<System.Windows.Controls.TextBlock>("PART_TitleText");
                if (titleText == null
                    || NavSearchBox == null
                    || titleText.Visibility != Visibility.Visible
                    || NavSearchBox.Visibility != Visibility.Visible
                    || !titleText.IsVisible
                    || !NavSearchBox.IsVisible)
                {
                    return;
                }

                var titlePoint = titleText.TransformToAncestor(this).Transform(new Point(0, 0));
                var searchPoint = NavSearchBox.TransformToAncestor(this).Transform(new Point(0, 0));
                double titleRight = titlePoint.X + titleText.ActualWidth;
                double searchLeft = searchPoint.X;
                if (titleRight + 12.0 > searchLeft)
                {
                    ShellTitleBar.Title = string.Empty;
                }
                else
                {
                    ShellTitleBar.Title = desiredTitle;
                }
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                _isUpdatingExtendedTitleOverlap = false;
            }
        }

        private T GetTitleBarTemplatePart<T>(string partName)
            where T : FrameworkElement
        {
            if (ShellTitleBar == null)
            {
                return null;
            }

            ShellTitleBar.ApplyTemplate();
            return ShellTitleBar.Template == null ? null : ShellTitleBar.Template.FindName(partName, ShellTitleBar) as T;
        }

    }
}
