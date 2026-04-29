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
using Fluence.Wpf;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Demo.Pages;

namespace Fluence.Wpf.Demo
{
    public partial class MainWindow : FluenceWindow
    {
        internal const string GalleryWindowTitle = "Fluence.Wpf \u2014 Control Gallery";

        // User-intent state for the title-bar Icon / Title pair. The GalleryWindowPage
        // toggles these via SetUserShowIcon / SetUserShowTitle; the actual ShowIcon /
        // ShowTitle DPs can additionally be forced off by the Top-pane + extended-chrome
        // hide rule in ApplyTitleBarContentVisibility.
        private bool _userShowIcon;
        private bool _userShowTitle;
        private ImageSource _userIcon;
        private string _userTitle;
        private DependencyPropertyDescriptor _extendsDpd;
        private DependencyPropertyDescriptor _paneModeDpd;
        private readonly Dictionary<NavigationViewItem, List<NavigationViewItem>> _sectionChildrenByHeader =
            new Dictionary<NavigationViewItem, List<NavigationViewItem>>();
        private readonly Dictionary<NavigationViewItem, NavigationViewItem> _sectionHeaderByChild =
            new Dictionary<NavigationViewItem, NavigationViewItem>();
        private readonly Dictionary<NavigationViewItem, bool> _sectionExpandedByHeader =
            new Dictionary<NavigationViewItem, bool>();
        private NavigationViewItemHeader _controlsHeader;
        private NavigationViewItem _lastSelectedLeafItem;
        private bool _restoringSelection;

        public MainWindow()
        {
            InitializeComponent();
            Title = GalleryWindowTitle;
            SystemThemeWatcher.Watch(this);
            ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica, true);
            PopulateNavigation();
            if (DemoNav != null)
            {
                DemoNav.SelectionChanged += DemoNav_SelectionChanged;
            }

            // Seed user-intent from XAML defaults so the first toggle does not reset to
            // an uninitialised value.
            _userShowIcon = ShowIcon;
            _userShowTitle = ShowTitle;
            _userIcon = Icon;
            _userTitle = Title;

            // Watch FluenceWindow.ExtendsContentIntoTitleBar + NavigationView.PaneDisplayMode
            // so the title-bar Icon / Title / NavSearchBox auto-hide when content is extended
            // into the title bar AND the nav pane is in Top mode (the Top rail already shows
            // its own row of items, so repeating chrome across both is cramped). Restore
            // visibility when either condition flips back.
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
            }

            ApplyTitleBarContentVisibility();
        }

        private void PopulateNavigation()
        {
            if (DemoNav == null)
            {
                return;
            }

            DemoNav.Items.Clear();
            _sectionChildrenByHeader.Clear();
            _sectionHeaderByChild.Clear();
            _sectionExpandedByHeader.Clear();
            _controlsHeader = null;
            _lastSelectedLeafItem = null;

            var currentCategory = string.Empty;
            NavigationViewItem currentSectionItem = null;
            var controlsHeaderAdded = false;
            foreach (var item in DemoNavigationCatalog.Items)
            {
                if (!string.Equals(currentCategory, item.Category, StringComparison.Ordinal))
                {
                    currentCategory = item.Category;
                    if (!string.IsNullOrEmpty(currentCategory))
                    {
                        DemoNavigationCategory category;
                        if (!DemoNavigationCatalog.TryGetCategory(currentCategory, out category))
                        {
                            currentSectionItem = null;
                            continue;
                        }

                        if (category.IsControlCategory && !controlsHeaderAdded)
                        {
                            _controlsHeader = new NavigationViewItemHeader { Content = "Controls" };
                            DemoNav.Items.Add(_controlsHeader);
                            DemoNav.Items.Add(new NavigationViewItem
                            {
                                Content = "All",
                                Tag = "all controls gallery overview",
                                Icon = CreateFontIcon("\uECA5", 20),
                                PageContent = new GalleryHomePage()
                            });
                            controlsHeaderAdded = true;
                        }

                        currentSectionItem = CreateSectionItem(category);
                        DemoNav.Items.Add(currentSectionItem);
                        _sectionChildrenByHeader[currentSectionItem] = new List<NavigationViewItem>();
                        _sectionExpandedByHeader[currentSectionItem] = category.IsExpandedByDefault;
                    }
                    else
                    {
                        currentSectionItem = null;
                    }
                }

                var navItem = CreatePageItem(item, currentSectionItem == null);
                if (currentSectionItem != null)
                {
                    _sectionChildrenByHeader[currentSectionItem].Add(navItem);
                    _sectionHeaderByChild[navItem] = currentSectionItem;
                    navItem.Visibility = _sectionExpandedByHeader[currentSectionItem]
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }

                DemoNav.Items.Add(navItem);
                if (item.IsDefault)
                {
                    DemoNav.SelectedItem = navItem;
                    _lastSelectedLeafItem = navItem;
                }
            }
        }

        private static NavigationViewItem CreateSectionItem(DemoNavigationCategory category)
        {
            return new NavigationViewItem
            {
                Content = category.Title,
                Tag = category.Tag,
                Icon = CreateFontIcon(category.Glyph, 20),
                InfoBadge = CreateChevronIcon(category.IsExpandedByDefault)
            };
        }

        private static NavigationViewItem CreatePageItem(DemoNavigationItem item, bool showIcon)
        {
            return new NavigationViewItem
            {
                Content = item.Title,
                Tag = item.Tag,
                Icon = showIcon ? CreateFontIcon(item.Glyph, 20) : null,
                PageContent = item.CreatePage()
            };
        }

        private static FontIcon CreateFontIcon(string glyph, double size)
        {
            return new FontIcon { Glyph = glyph, IconFontSize = size };
        }

        private static FontIcon CreateChevronIcon(bool expanded)
        {
            return CreateFontIcon(expanded ? "\uE70E" : "\uE70D", 12);
        }

        private void DemoNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_restoringSelection)
            {
                return;
            }

            var selected = DemoNav.SelectedItem as NavigationViewItem;
            if (selected == null)
            {
                return;
            }

            if (_sectionChildrenByHeader.ContainsKey(selected))
            {
                ToggleSection(selected);
                RestoreLastLeafSelection();
                return;
            }

            if (selected.PageContent != null)
            {
                EnsureParentExpanded(selected);
                _lastSelectedLeafItem = selected;
            }
        }

        private void ToggleSection(NavigationViewItem sectionItem)
        {
            bool expanded;
            if (!_sectionExpandedByHeader.TryGetValue(sectionItem, out expanded))
            {
                return;
            }

            SetSectionExpanded(sectionItem, !expanded);
        }

        private void SetSectionExpanded(NavigationViewItem sectionItem, bool expanded)
        {
            _sectionExpandedByHeader[sectionItem] = expanded;
            SetSectionChevron(sectionItem, expanded);

            List<NavigationViewItem> children;
            if (!_sectionChildrenByHeader.TryGetValue(sectionItem, out children))
            {
                return;
            }

            foreach (var child in children)
            {
                child.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static void SetSectionChevron(NavigationViewItem sectionItem, bool expanded)
        {
            var chevron = sectionItem.InfoBadge as FontIcon;
            if (chevron != null)
            {
                chevron.Glyph = expanded ? "\uE70E" : "\uE70D";
            }
        }

        private void EnsureParentExpanded(NavigationViewItem childItem)
        {
            NavigationViewItem sectionItem;
            if (_sectionHeaderByChild.TryGetValue(childItem, out sectionItem))
            {
                SetSectionExpanded(sectionItem, true);
            }
        }

        private void RestoreLastLeafSelection()
        {
            var target = _lastSelectedLeafItem;
            if (target == null)
            {
                target = FindFirstLeafItem();
            }

            if (target == null)
            {
                return;
            }

            EnsureParentExpanded(target);
            _restoringSelection = true;
            try
            {
                DemoNav.SelectedItem = target;
            }
            finally
            {
                _restoringSelection = false;
            }
        }

        private NavigationViewItem FindFirstLeafItem()
        {
            foreach (var obj in DemoNav.Items)
            {
                var item = obj as NavigationViewItem;
                if (item != null && item.PageContent != null)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Records the user's intent for the title-bar icon and re-evaluates the
        /// Top-pane + extended-chrome hide rule. Call from the gallery's window chrome
        /// toggles so the hide rule stays in charge of actual <see cref="FluenceWindow.ShowIcon"/>.
        /// </summary>
        /// <param name="show">User-requested icon visibility.</param>
        /// <param name="icon">The icon to apply when visible; may be <see langword="null"/>.</param>
        public void SetUserShowIcon(bool show, ImageSource icon)
        {
            _userShowIcon = show;
            _userIcon = icon;
            ApplyTitleBarContentVisibility();
        }

        /// <summary>
        /// Records the user's intent for the title-bar title and re-evaluates the
        /// Top-pane + extended-chrome hide rule. Call from the gallery's window chrome
        /// toggles so the hide rule stays in charge of actual <see cref="FluenceWindow.ShowTitle"/>.
        /// </summary>
        /// <param name="show">User-requested title visibility.</param>
        /// <param name="title">The title text to apply when visible; may be <see langword="null"/>.</param>
        public void SetUserShowTitle(bool show, string title)
        {
            _userShowTitle = show;
            _userTitle = title;
            ApplyTitleBarContentVisibility();
        }

        private void OnTitleBarDependencyChanged(object sender, EventArgs e)
        {
            ApplyTitleBarContentVisibility();
        }

        private void ApplyTitleBarContentVisibility()
        {
            bool hideForTopExtends =
                ExtendsContentIntoTitleBar
                && DemoNav != null
                && DemoNav.PaneDisplayMode == NavigationViewPaneDisplayMode.Top;

            ShowIcon = !hideForTopExtends && _userShowIcon;
            ShowTitle = !hideForTopExtends && _userShowTitle;
            Icon = _userIcon;
            if (_userShowTitle && !string.IsNullOrEmpty(_userTitle))
            {
                Title = _userTitle;
            }

            if (NavSearchBox != null)
            {
                NavSearchBox.Visibility = hideForTopExtends ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// Selects the pane item whose <see cref="FrameworkElement.Tag"/> contains the given token.
        /// The first matching item is used; token matching is case-insensitive and substring-based
        /// against both the item label and its Tag string.
        /// </summary>
        /// <param name="tag">A single token (e.g. "window", "buttons", "colors") that appears in a NavigationViewItem.Tag.</param>
        public void NavigateTo(string tag)
        {
            if (DemoNav == null || string.IsNullOrEmpty(tag))
            {
                return;
            }

            var match = FindFirstMatchingItem(tag);
            if (match != null)
            {
                EnsureParentExpanded(match);
                DemoNav.SelectedItem = match;
            }
        }

        private void NavSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyNavSearchFilter();
        }

        // Enter commits the current filter: the first visible matching item is selected.
        // This closes WI-1 F3 — typing narrows, Enter navigates, empty input is a no-op.
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
                DemoNav.SelectedItem = match;
                e.Handled = true;
            }
        }

        private NavigationViewItem FindFirstMatchingItem(string query)
        {
            if (DemoNav == null || string.IsNullOrEmpty(query))
            {
                return null;
            }

            var needle = query.Trim().ToLowerInvariant();
            NavigationViewItem fallback = null;
            foreach (var obj in DemoNav.Items)
            {
                var nvi = obj as NavigationViewItem;
                if (nvi == null || nvi.PageContent == null)
                {
                    continue;
                }

                var label = (nvi.Content as string) ?? string.Empty;
                if (string.Equals(label, query.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return nvi;
                }

                if (fallback == null && ItemMatches(nvi, needle))
                {
                    fallback = nvi;
                }
            }

            return fallback;
        }

        private static bool ItemMatches(NavigationViewItem nvi, string loweredNeedle)
        {
            var label = (nvi.Content as string) ?? string.Empty;
            var tagText = (nvi.Tag as string) ?? string.Empty;
            var haystack = (label + " " + tagText).ToLowerInvariant();
            return haystack.IndexOf(loweredNeedle, StringComparison.Ordinal) >= 0;
        }

        private void ApplyNavSearchFilter()
        {
            if (DemoNav == null || NavSearchBox == null)
            {
                return;
            }

            var q = (NavSearchBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(q))
            {
                RestoreAllPaneElementsVisible();
                return;
            }

            var ql = q.ToLowerInvariant();
            var anyItemMatch = false;

            foreach (var obj in DemoNav.Items)
            {
                var nvi = obj as NavigationViewItem;
                if (nvi == null)
                {
                    continue;
                }

                if (_sectionChildrenByHeader.ContainsKey(nvi))
                {
                    var sectionMatches = ItemMatches(nvi, ql);
                    var anyChildMatch = false;
                    var children = _sectionChildrenByHeader[nvi];
                    foreach (var child in children)
                    {
                        var childMatches = sectionMatches || ItemMatches(child, ql);
                        child.Visibility = childMatches ? Visibility.Visible : Visibility.Collapsed;
                        if (childMatches)
                        {
                            anyChildMatch = true;
                        }
                    }

                    nvi.Visibility = sectionMatches || anyChildMatch ? Visibility.Visible : Visibility.Collapsed;
                    if (nvi.Visibility == Visibility.Visible)
                    {
                        SetSectionChevron(nvi, true);
                        anyItemMatch = true;
                    }

                    continue;
                }

                if (_sectionHeaderByChild.ContainsKey(nvi))
                {
                    continue;
                }

                var match = ItemMatches(nvi, ql);
                nvi.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
                if (match)
                {
                    anyItemMatch = true;
                }
            }

            // If no item matches, fall back to "restore everything" so the user is never stranded
            // with an empty pane.
            if (!anyItemMatch)
            {
                RestoreAllPaneElementsVisible();
                return;
            }

            UpdateControlsHeaderVisibility();
        }

        private void RestoreAllPaneElementsVisible()
        {
            foreach (var obj in DemoNav.Items)
            {
                var header = obj as NavigationViewItemHeader;
                if (header != null)
                {
                    header.Visibility = Visibility.Visible;
                    continue;
                }

                var item = obj as NavigationViewItem;
                if (item == null)
                {
                    continue;
                }

                if (_sectionChildrenByHeader.ContainsKey(item))
                {
                    item.Visibility = Visibility.Visible;
                    SetSectionChevron(item, _sectionExpandedByHeader[item]);
                    continue;
                }

                NavigationViewItem sectionItem;
                if (_sectionHeaderByChild.TryGetValue(item, out sectionItem))
                {
                    item.Visibility = _sectionExpandedByHeader[sectionItem]
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    continue;
                }

                item.Visibility = Visibility.Visible;
            }
        }

        private void UpdateControlsHeaderVisibility()
        {
            if (_controlsHeader == null)
            {
                return;
            }

            var hasVisibleControlItem = false;
            var afterControlsHeader = false;
            foreach (var obj in DemoNav.Items)
            {
                if (ReferenceEquals(obj, _controlsHeader))
                {
                    afterControlsHeader = true;
                    continue;
                }

                if (!afterControlsHeader)
                {
                    continue;
                }

                var item = obj as NavigationViewItem;
                if (item != null && item.Visibility == Visibility.Visible)
                {
                    hasVisibleControlItem = true;
                    break;
                }
            }

            _controlsHeader.Visibility = hasVisibleControlItem ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DemoNav != null)
            {
                DemoNav.SelectionChanged -= DemoNav_SelectionChanged;
            }
            if (_extendsDpd != null)
            {
                _extendsDpd.RemoveValueChanged(this, OnTitleBarDependencyChanged);
                _extendsDpd = null;
            }
            if (_paneModeDpd != null && DemoNav != null)
            {
                _paneModeDpd.RemoveValueChanged(DemoNav, OnTitleBarDependencyChanged);
                _paneModeDpd = null;
            }
            SystemThemeWatcher.UnWatch(this);
            base.OnClosed(e);
        }
    }
}
