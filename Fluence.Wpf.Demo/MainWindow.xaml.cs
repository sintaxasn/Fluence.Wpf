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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        private readonly Dictionary<NavigationViewItem, DemoNavigationItem> _navigationItemByContainer =
            new Dictionary<NavigationViewItem, DemoNavigationItem>();
        private NavigationViewItemHeader _controlsHeader;
        private NavigationViewItem _lastSelectedLeafItem;
        private NavigationViewItem _sectionPointerSelectionToIgnore;
        private Popup _compactSectionPopup;
        private System.Windows.Controls.StackPanel _compactSectionPopupItemsPanel;
        private bool _restoringSelection;

        public MainWindow()
        {
            InitializeComponent();
            PreviewKeyDown += MainWindow_PreviewKeyDown;
            PreviewMouseDown += MainWindow_PreviewMouseDown;
            Title = GalleryWindowTitle;
            SystemThemeWatcher.Watch(this);
            ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica, true);
            PopulateNavigation();
            if (DemoNav != null)
            {
                DemoNav.SelectionChanged += DemoNav_SelectionChanged;
                DemoNav.PaneClosed += DemoNav_PaneClosed;
                DemoNav.PaneOpening += DemoNav_PaneOpening;
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

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || e.Key != Key.Tab)
            {
                return;
            }

            var modifiers = Keyboard.Modifiers;
            if ((modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)) != ModifierKeys.None)
            {
                return;
            }

            var focused = Keyboard.FocusedElement as DependencyObject;
            if (IsFocusWithinSelectedNavigationItem(focused))
            {
                if (FocusSearchBox())
                {
                    e.Handled = true;
                }

                return;
            }

            if (NavSearchBox != null && IsDescendantOrSelf(focused, NavSearchBox) && FocusFirstSelectedPageElement())
            {
                e.Handled = true;
            }
        }

        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_compactSectionPopup == null || !_compactSectionPopup.IsOpen)
            {
                return;
            }

            var source = e.OriginalSource as DependencyObject;
            if (IsDescendantOrSelf(source, _compactSectionPopup.PlacementTarget as DependencyObject))
            {
                return;
            }

            HideCompactSectionPopup();
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
            _navigationItemByContainer.Clear();
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

                        const bool sectionStartsExpanded = false;
                        currentSectionItem = CreateSectionItem(category, sectionStartsExpanded);
                        currentSectionItem.PageContent = CreateCategoryPage(category);
                        currentSectionItem.AddHandler(
                            UIElement.PreviewMouseLeftButtonDownEvent,
                            new MouseButtonEventHandler(SectionItem_PreviewMouseLeftButtonDown),
                            true);
                        DemoNav.Items.Add(currentSectionItem);
                        _sectionChildrenByHeader[currentSectionItem] = new List<NavigationViewItem>();
                        _sectionExpandedByHeader[currentSectionItem] = sectionStartsExpanded;
                    }
                    else
                    {
                        currentSectionItem = null;
                    }
                }

                var navItem = CreatePageItem(item, currentSectionItem == null);
                _navigationItemByContainer[navItem] = item;
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

        private static NavigationViewItem CreateSectionItem(DemoNavigationCategory category, bool isExpanded)
        {
            return new NavigationViewItem
            {
                Content = category.Title,
                Tag = category.Tag,
                Icon = CreateFontIcon(category.Glyph, 20),
                InfoBadge = CreateChevronIcon(isExpanded)
            };
        }

        private static GalleryCategoryPage CreateCategoryPage(DemoNavigationCategory category)
        {
            var children = new List<DemoNavigationItem>();
            foreach (var item in DemoNavigationCatalog.Items)
            {
                if (string.Equals(item.Category, category.Title, StringComparison.Ordinal))
                {
                    children.Add(item);
                }
            }

            return new GalleryCategoryPage(category.Title, GetCategoryDescription(category), children);
        }

        private static string GetCategoryDescription(DemoNavigationCategory category)
        {
            if (string.Equals(category.Title, "Design", StringComparison.Ordinal))
            {
                return "Color, iconography, and typography resources used across Fluence controls.";
            }

            if (string.Equals(category.Title, "Accessibility", StringComparison.Ordinal))
            {
                return "Screen reader, keyboard, and contrast guidance for accessible WPF surfaces.";
            }

            return "Single-control pages and variants in this section.";
        }

        private static NavigationViewItem CreatePageItem(DemoNavigationItem item, bool showIcon)
        {
            return new NavigationViewItem
            {
                Content = item.Title,
                Tag = item.Tag,
                Icon = showIcon ? CreateFontIcon(item.Glyph, 20) : null,
                IsChildItem = !showIcon,
                PageContent = item.CreatePage()
            };
        }

        private static FontIcon CreateFontIcon(string glyph, double size)
        {
            return new FontIcon { Glyph = glyph, IconFontSize = size };
        }

        private static FontIcon CreateChevronIcon(bool expanded)
        {
            var icon = CreateFontIcon("\uE70D", 12);
            icon.Rotation = expanded ? 180.0 : 0.0;
            return icon;
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
                _sectionPointerSelectionToIgnore = selected;
                Dispatcher.BeginInvoke(new Action(ClearSectionPointerSelectionToIgnore), System.Windows.Threading.DispatcherPriority.Background);
                if (!DemoNav.IsPaneOpen)
                {
                    CollapseAllSections(false);
                    ShowCompactSectionPopup(selected);
                    return;
                }

                HideCompactSectionPopup();
                ToggleSection(selected, true);
                return;
            }

            HideCompactSectionPopup();
            if (selected.PageContent != null)
            {
                if (DemoNav.IsPaneOpen)
                {
                    EnsureParentExpanded(selected);
                }

                _lastSelectedLeafItem = selected;
            }
        }

        private void SectionItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var sectionItem = sender as NavigationViewItem;
            if (sectionItem == null || DemoNav == null)
            {
                return;
            }

            if (!DemoNav.IsPaneOpen)
            {
                Activate();
                DemoNav.SelectedItem = sectionItem;
                CollapseAllSections(false);
                ShowCompactSectionPopup(sectionItem);
                e.Handled = true;
                return;
            }

            if (ReferenceEquals(DemoNav.SelectedItem, sectionItem))
            {
                if (ReferenceEquals(_sectionPointerSelectionToIgnore, sectionItem))
                {
                    _sectionPointerSelectionToIgnore = null;
                    return;
                }

                ToggleSection(sectionItem, true);
            }
        }

        private void ClearSectionPointerSelectionToIgnore()
        {
            _sectionPointerSelectionToIgnore = null;
        }

        private void DemoNav_PaneClosed(object sender, EventArgs e)
        {
            CollapseAllSections(true);
        }

        private void DemoNav_PaneOpening(object sender, EventArgs e)
        {
            HideCompactSectionPopup();
        }

        private void ShowCompactSectionPopup(NavigationViewItem sectionItem)
        {
            if (sectionItem == null || DemoNav == null || DemoNav.IsPaneOpen)
            {
                HideCompactSectionPopup();
                return;
            }

            List<NavigationViewItem> children;
            if (!_sectionChildrenByHeader.TryGetValue(sectionItem, out children) || children.Count == 0)
            {
                HideCompactSectionPopup();
                return;
            }

            EnsureCompactSectionPopup();
            _compactSectionPopup.PlacementTarget = sectionItem;
            _compactSectionPopupItemsPanel.Children.Clear();

            // The real child NavigationViewItems stay in DemoNav so selection/content
            // routing remains single-source. The popup creates lightweight buttons that
            // point back to those existing containers through Tag.
            foreach (var child in children)
            {
                _compactSectionPopupItemsPanel.Children.Add(CreateCompactSectionPopupButton(child));
            }

            _compactSectionPopup.IsOpen = true;
        }

        private void HideCompactSectionPopup()
        {
            if (_compactSectionPopup != null)
            {
                _compactSectionPopup.IsOpen = false;
            }
        }

        private void EnsureCompactSectionPopup()
        {
            if (_compactSectionPopup != null)
            {
                return;
            }

            _compactSectionPopupItemsPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = Orientation.Vertical
            };

            var popupSurface = new System.Windows.Controls.Border
            {
                MinWidth = 230.0,
                MaxHeight = 520.0,
                CornerRadius = new CornerRadius(8.0),
                BorderThickness = new Thickness(1.0),
                Padding = new Thickness(8.0),
                Child = _compactSectionPopupItemsPanel
            };
            popupSurface.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            popupSurface.SetResourceReference(System.Windows.Controls.Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");

            _compactSectionPopup = new Popup
            {
                AllowsTransparency = true,
                Placement = PlacementMode.Right,
                HorizontalOffset = 8.0,
                VerticalOffset = -8.0,
                PopupAnimation = PopupAnimation.Fade,
                StaysOpen = true,
                Child = popupSurface
            };
        }

        private Fluence.Wpf.Controls.Button CreateCompactSectionPopupButton(NavigationViewItem child)
        {
            DemoNavigationItem item;
            _navigationItemByContainer.TryGetValue(child, out item);

            var glyph = item == null ? "\uE700" : item.Glyph;
            var label = child.Content as string;
            if (string.IsNullOrEmpty(label))
            {
                label = item == null ? string.Empty : item.Title;
            }

            var icon = CreateFontIcon(glyph, 20.0);
            icon.Width = 40.0;
            icon.HorizontalAlignment = HorizontalAlignment.Left;
            icon.VerticalAlignment = VerticalAlignment.Center;
            icon.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "TextFillColorPrimaryBrush");

            var text = new System.Windows.Controls.TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center
            };
            text.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");

            var content = new System.Windows.Controls.Grid();
            content.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(40.0) });
            content.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            System.Windows.Controls.Grid.SetColumn(icon, 0);
            System.Windows.Controls.Grid.SetColumn(text, 1);
            content.Children.Add(icon);
            content.Children.Add(text);

            var button = new Fluence.Wpf.Controls.Button
            {
                Appearance = ControlAppearance.Subtle,
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(8.0, 9.0, 12.0, 9.0),
                Margin = new Thickness(0.0, 2.0, 0.0, 2.0),
                Tag = child
            };
            button.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "TextFillColorPrimaryBrush");
            button.Click += CompactSectionPopupChild_Click;
            return button;
        }

        private void CompactSectionPopupChild_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Fluence.Wpf.Controls.Button;
            var child = button == null ? null : button.Tag as NavigationViewItem;
            if (child == null)
            {
                return;
            }

            HideCompactSectionPopup();
            NavigateToCompactChild(child);
        }

        private void NavigateToCompactChild(NavigationViewItem child)
        {
            NavigationViewItem sectionItem;
            if (!_sectionHeaderByChild.TryGetValue(child, out sectionItem))
            {
                return;
            }

            _lastSelectedLeafItem = child;
            CollapseAllSections(false);

            _restoringSelection = true;
            try
            {
                // In compact mode the selected rail item is the section header, but the
                // displayed content belongs to the clicked child. Suppress the normal
                // selection handler so it does not immediately toggle the section popup.
                DemoNav.SelectedItem = sectionItem;
            }
            finally
            {
                _restoringSelection = false;
            }

            DemoNav.SelectedContent = child.PageContent ?? child.Content;
        }

        private void ToggleSection(NavigationViewItem sectionItem, bool animate)
        {
            bool expanded;
            if (!_sectionExpandedByHeader.TryGetValue(sectionItem, out expanded))
            {
                return;
            }

            SetSectionExpanded(sectionItem, !expanded, animate);
        }

        private void SetSectionExpanded(NavigationViewItem sectionItem, bool expanded)
        {
            SetSectionExpanded(sectionItem, expanded, false);
        }

        private void SetSectionExpanded(NavigationViewItem sectionItem, bool expanded, bool animate)
        {
            _sectionExpandedByHeader[sectionItem] = expanded;
            SetSectionChevron(sectionItem, expanded, animate);

            List<NavigationViewItem> children;
            if (!_sectionChildrenByHeader.TryGetValue(sectionItem, out children))
            {
                return;
            }

            foreach (var child in children)
            {
                SetChildVisibility(sectionItem, child, expanded, animate);
            }
        }

        private void CollapseAllSections(bool animate)
        {
            foreach (var pair in _sectionChildrenByHeader)
            {
                SetSectionExpanded(pair.Key, false, animate);
            }
        }

        private void SetChildVisibility(NavigationViewItem sectionItem, NavigationViewItem child, bool visible, bool animate)
        {
            child.BeginAnimation(UIElement.OpacityProperty, null);

            if (!animate)
            {
                child.Opacity = 1.0;
                child.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            var duration = new Duration(TimeSpan.FromMilliseconds(visible ? 120 : 80));
            var easing = new CubicEase { EasingMode = visible ? EasingMode.EaseOut : EasingMode.EaseIn };

            if (visible)
            {
                child.Visibility = Visibility.Visible;
                child.Opacity = 0.0;
                child.BeginAnimation(
                    UIElement.OpacityProperty,
                    new DoubleAnimation(1.0, duration) { EasingFunction = easing });
                return;
            }

            if (child.Visibility != Visibility.Visible)
            {
                child.Visibility = Visibility.Collapsed;
                child.Opacity = 1.0;
                return;
            }

            var animation = new DoubleAnimation(0.0, duration) { EasingFunction = easing };
            animation.Completed += delegate
            {
                bool stillExpanded;
                if (_sectionExpandedByHeader.TryGetValue(sectionItem, out stillExpanded) && !stillExpanded)
                {
                    child.Visibility = Visibility.Collapsed;
                }

                child.Opacity = 1.0;
            };
            child.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        private static void SetSectionChevron(NavigationViewItem sectionItem, bool expanded, bool animate)
        {
            var chevron = sectionItem.InfoBadge as FontIcon;
            if (chevron != null)
            {
                const double CollapsedRotation = 0.0;
                const double ExpandedRotation = 180.0;

                var targetRotation = expanded ? ExpandedRotation : CollapsedRotation;
                chevron.Glyph = "\uE70D";
                chevron.BeginAnimation(FontIcon.RotationProperty, null);

                if (!animate)
                {
                    chevron.Rotation = targetRotation;
                    return;
                }

                var animation = new DoubleAnimation(targetRotation, new Duration(TimeSpan.FromMilliseconds(expanded ? 120 : 80)))
                {
                    EasingFunction = new CubicEase { EasingMode = expanded ? EasingMode.EaseOut : EasingMode.EaseIn }
                };
                animation.Completed += delegate
                {
                    chevron.BeginAnimation(FontIcon.RotationProperty, null);
                    chevron.Rotation = targetRotation;
                };
                chevron.BeginAnimation(FontIcon.RotationProperty, animation, HandoffBehavior.SnapshotAndReplace);
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

        private bool IsFocusWithinSelectedNavigationItem(DependencyObject focused)
        {
            if (DemoNav == null)
            {
                return false;
            }

            var selected = DemoNav.SelectedItem as DependencyObject;
            return IsDescendantOrSelf(focused, selected);
        }

        private bool FocusSearchBox()
        {
            if (NavSearchBox == null || NavSearchBox.Visibility != Visibility.Visible || !NavSearchBox.IsEnabled)
            {
                return false;
            }

            return NavSearchBox.Focus();
        }

        private bool FocusFirstSelectedPageElement()
        {
            if (DemoNav == null)
            {
                return false;
            }

            var target = FindFirstFocusableElement(DemoNav.SelectedContent as DependencyObject);
            return target != null && target.Focus();
        }

        private static UIElement FindFirstFocusableElement(DependencyObject root)
        {
            var element = root as UIElement;
            if (element != null && element.Focusable && element.IsEnabled && element.IsVisible)
            {
                return element;
            }

            if (root == null)
            {
                return null;
            }

            var childCount = GetVisualChildCount(root);
            for (var i = 0; i < childCount; i++)
            {
                var match = FindFirstFocusableElement(VisualTreeHelper.GetChild(root, i));
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static int GetVisualChildCount(DependencyObject root)
        {
            if (root is Visual || root is System.Windows.Media.Media3D.Visual3D)
            {
                return VisualTreeHelper.GetChildrenCount(root);
            }

            return 0;
        }

        private static bool IsDescendantOrSelf(DependencyObject candidate, DependencyObject ancestor)
        {
            if (candidate == null || ancestor == null)
            {
                return false;
            }

            var current = candidate;
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
            if (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
            {
                return VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            }

            return LogicalTreeHelper.GetParent(current);
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

            if (NavSearchBox != null && !string.IsNullOrEmpty(NavSearchBox.Text))
            {
                NavSearchBox.Text = string.Empty;
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
            NavigationViewItem fallbackLeaf = null;
            NavigationViewItem fallbackSection = null;
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

                if (!ItemMatches(nvi, needle))
                {
                    continue;
                }

                if (_sectionChildrenByHeader.ContainsKey(nvi))
                {
                    if (fallbackSection == null)
                    {
                        fallbackSection = nvi;
                    }
                }
                else if (fallbackLeaf == null)
                {
                    fallbackLeaf = nvi;
                }
            }

            return fallbackLeaf ?? fallbackSection;
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
                        SetSectionChevron(nvi, true, false);
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
                    SetSectionChevron(item, _sectionExpandedByHeader[item], false);
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
            PreviewMouseDown -= MainWindow_PreviewMouseDown;
            PreviewKeyDown -= MainWindow_PreviewKeyDown;
            if (DemoNav != null)
            {
                DemoNav.SelectionChanged -= DemoNav_SelectionChanged;
                DemoNav.PaneClosed -= DemoNav_PaneClosed;
                DemoNav.PaneOpening -= DemoNav_PaneOpening;
            }
            HideCompactSectionPopup();
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
