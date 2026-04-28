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
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using Fluence.Wpf;
using Fluence.Wpf.Automation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A navigation control with a collapsible pane and content area, similar to WinUI NavigationView.
    /// Uses a single shared selection indicator that animates between items.
    /// </summary>
    [TemplatePart(Name = PartBackButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = PartContentPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PartPaneItemsScrollViewer, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PartPaneToggleButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = PartSelectionIndicator, Type = typeof(FrameworkElement))]
    [TemplateVisualState(GroupName = "BackButtonStates", Name = "BackButtonVisible")]
    [TemplateVisualState(GroupName = "BackButtonStates", Name = "BackButtonCollapsed")]
    public class NavigationView : Selector
    {
        /// <summary>Name of the back button template part.</summary>
        public const string PartBackButton = "PART_BackButton";

        /// <summary>Name of the main content presenter template part.</summary>
        public const string PartContentPresenter = "PART_ContentPresenter";

        /// <summary>Name of the scroll viewer that hosts pane items.</summary>
        public const string PartPaneItemsScrollViewer = "PART_PaneItemsScrollViewer";

        /// <summary>Name of the pane collapse/expand toggle button.</summary>
        public const string PartPaneToggleButton = "PART_PaneToggleButton";

        /// <summary>Name of the shared selection indicator element.</summary>
        public const string PartSelectionIndicator = "PART_SelectionIndicator";

        private System.Windows.Controls.Button _backButton;
        private System.Windows.Controls.Button _paneToggleButton;
        private ScrollViewer _paneScrollViewer;
        private FrameworkElement _selectionIndicator;
        private FrameworkElement _indicatorHost;
        private Storyboard _activeStoryboard;
        private bool _indicatorPositioned;

        /// <summary>Identifies the <see cref="PaneDisplayMode"/> dependency property.</summary>
        public static readonly DependencyProperty PaneDisplayModeProperty = DependencyProperty.Register(
            "PaneDisplayMode",
            typeof(NavigationViewPaneDisplayMode),
            typeof(NavigationView),
            new FrameworkPropertyMetadata(
                NavigationViewPaneDisplayMode.Left,
                FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnPaneDisplayModeChanged));

        /// <summary>Identifies the <see cref="SelectionFollowsFocus"/> dependency property.</summary>
        public static readonly DependencyProperty SelectionFollowsFocusProperty = DependencyProperty.Register(
            "SelectionFollowsFocus",
            typeof(bool),
            typeof(NavigationView),
            new PropertyMetadata(false));

        /// <summary>Identifies the <see cref="IsBackButtonVisible"/> dependency property.</summary>
        public static readonly DependencyProperty IsBackButtonVisibleProperty = DependencyProperty.Register(
            "IsBackButtonVisible",
            typeof(bool),
            typeof(NavigationView),
            new PropertyMetadata(false, OnIsBackButtonVisibleChanged));

        /// <summary>Identifies the <see cref="IsBackEnabled"/> dependency property.</summary>
        public static readonly DependencyProperty IsBackEnabledProperty = DependencyProperty.Register(
            "IsBackEnabled",
            typeof(bool),
            typeof(NavigationView),
            new PropertyMetadata(true));

        /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(null));

        /// <summary>Identifies the <see cref="HeaderTemplate"/> dependency property.</summary>
        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
            "HeaderTemplate",
            typeof(DataTemplate),
            typeof(NavigationView),
            new PropertyMetadata(null));

        /// <summary>Identifies the <see cref="PaneHeader"/> dependency property.</summary>
        public static readonly DependencyProperty PaneHeaderProperty = DependencyProperty.Register(
            "PaneHeader",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(null));

        /// <summary>Identifies the <see cref="PaneFooter"/> dependency property.</summary>
        public static readonly DependencyProperty PaneFooterProperty = DependencyProperty.Register(
            "PaneFooter",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(null));

        /// <summary>Identifies the <see cref="ContentBackground"/> dependency property.</summary>
        public static readonly DependencyProperty ContentBackgroundProperty = DependencyProperty.Register(
            "ContentBackground",
            typeof(Brush),
            typeof(NavigationView),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>Identifies the <see cref="IsPaneOpen"/> dependency property.</summary>
        public static readonly DependencyProperty IsPaneOpenProperty = DependencyProperty.Register(
            "IsPaneOpen",
            typeof(bool),
            typeof(NavigationView),
            new FrameworkPropertyMetadata(true, OnIsPaneOpenChanged));

        /// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(null));

        /// <summary>Identifies the <see cref="SelectedContent"/> dependency property.</summary>
        public static readonly DependencyProperty SelectedContentProperty = DependencyProperty.Register(
            "SelectedContent",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(null));

        static NavigationView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(NavigationView),
                new FrameworkPropertyMetadata(typeof(NavigationView)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationView"/> class.
        /// </summary>
        public NavigationView()
        {
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopAnimation();
            _selectionIndicator = null;
            _indicatorHost = null;
            _indicatorPositioned = false;
        }

        /// <summary>
        /// Occurs when the selected navigation item changes.
        /// </summary>
        /// <remarks>The routed <see cref="Selector.SelectionChanged"/> event also fires.</remarks>
        public event EventHandler<NavigationViewSelectionChangedEventArgs> NavSelectionChanged;

        /// <summary>
        /// Occurs when the back button is invoked.
        /// </summary>
        public event EventHandler<NavigationViewBackRequestedEventArgs> BackRequested;

        /// <summary>
        /// Occurs when the pane is opening (expanded in left mode).
        /// </summary>
        public event EventHandler PaneOpening;

        /// <summary>
        /// Occurs when the pane has closed (collapsed in left mode).
        /// </summary>
        public event EventHandler PaneClosed;

        /// <summary>Gets or sets whether the pane is shown on the left or across the top.</summary>
        public NavigationViewPaneDisplayMode PaneDisplayMode
        {
            get { return (NavigationViewPaneDisplayMode)GetValue(PaneDisplayModeProperty); }
            set { SetValue(PaneDisplayModeProperty, value); }
        }

        /// <summary>Gets or sets whether keyboard focus on an item selects it immediately.</summary>
        public bool SelectionFollowsFocus
        {
            get { return (bool)GetValue(SelectionFollowsFocusProperty); }
            set { SetValue(SelectionFollowsFocusProperty, value); }
        }

        /// <summary>Gets or sets whether the back button is shown.</summary>
        public bool IsBackButtonVisible
        {
            get { return (bool)GetValue(IsBackButtonVisibleProperty); }
            set { SetValue(IsBackButtonVisibleProperty, value); }
        }

        /// <summary>Gets or sets whether the back button can be invoked.</summary>
        public bool IsBackEnabled
        {
            get { return (bool)GetValue(IsBackEnabledProperty); }
            set { SetValue(IsBackEnabledProperty, value); }
        }

        /// <summary>Gets or sets header content displayed beside the navigation chrome.</summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>Gets or sets the DataTemplate used to display the <see cref="Header"/>.</summary>
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <summary>Gets or sets content at the start of the pane chrome (title area).</summary>
        public object PaneHeader
        {
            get { return GetValue(PaneHeaderProperty); }
            set { SetValue(PaneHeaderProperty, value); }
        }

        /// <summary>Gets or sets content at the end of the pane (footer).</summary>
        public object PaneFooter
        {
            get { return GetValue(PaneFooterProperty); }
            set { SetValue(PaneFooterProperty, value); }
        }

        /// <summary>Gets or sets the background brush for the content area.</summary>
        public Brush ContentBackground
        {
            get { return (Brush)GetValue(ContentBackgroundProperty); }
            set { SetValue(ContentBackgroundProperty, value); }
        }

        /// <summary>Gets or sets whether the left pane is expanded.</summary>
        public bool IsPaneOpen
        {
            get { return (bool)GetValue(IsPaneOpenProperty); }
            set { SetValue(IsPaneOpenProperty, value); }
        }

        /// <summary>Gets or sets the content hosted in the main area.</summary>
        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>Gets or sets content bound to the current selection.</summary>
        public object SelectedContent
        {
            get { return GetValue(SelectedContentProperty); }
            set { SetValue(SelectedContentProperty, value); }
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_backButton != null)
            {
                _backButton.Click -= OnBackButtonClick;
            }

            if (_paneToggleButton != null)
            {
                _paneToggleButton.Click -= OnPaneToggleButtonClick;
            }

            base.OnApplyTemplate();

            _backButton = GetTemplateChild(PartBackButton) as System.Windows.Controls.Button;
            if (_backButton != null)
            {
                _backButton.Click += OnBackButtonClick;
            }

            _paneToggleButton = GetTemplateChild(PartPaneToggleButton) as System.Windows.Controls.Button;
            if (_paneToggleButton != null)
            {
                _paneToggleButton.Click += OnPaneToggleButtonClick;
            }

            _paneScrollViewer = GetTemplateChild(PartPaneItemsScrollViewer) as ScrollViewer;
            _selectionIndicator = GetTemplateChild(PartSelectionIndicator) as FrameworkElement;

            if (_selectionIndicator != null)
            {
                _indicatorHost = VisualTreeHelper.GetParent(_selectionIndicator) as FrameworkElement;
            }
            else
            {
                _indicatorHost = null;
            }

            _indicatorPositioned = false;
            StopAnimation();
            UpdateBackButtonState(false);
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            UpdateSelectedContentFromSelection();
            Dispatcher.BeginInvoke(new Action(() => PositionIndicator(true)), DispatcherPriority.Input);

            var handler = NavSelectionChanged;
            if (handler != null)
            {
                handler(this, new NavigationViewSelectionChangedEventArgs(SelectedItem, false));
            }
        }

        /// <inheritdoc />
        protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnPreviewGotKeyboardFocus(e);
            if (!SelectionFollowsFocus)
            {
                return;
            }

            var navItem = FindNavigationViewItem(e.NewFocus as DependencyObject);
            if (navItem == null)
            {
                return;
            }

            var fromContainer = ItemContainerGenerator.ItemFromContainer(navItem);
            if (fromContainer != DependencyProperty.UnsetValue && fromContainer != null)
            {
                if (!object.ReferenceEquals(SelectedItem, fromContainer))
                {
                    SelectedItem = fromContainer;
                }
            }
            else if (!object.ReferenceEquals(SelectedItem, navItem))
            {
                SelectedItem = navItem;
            }
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new NavigationViewAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is NavigationViewItem
                || item is NavigationViewItemHeader
                || item is NavigationViewItemSeparator;
        }

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new NavigationViewItem();
        }

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            var navItem = element as NavigationViewItem;
            if (navItem != null)
            {
                navItem.Selected -= OnNavigationViewItemSelected;
                navItem.Selected += OnNavigationViewItemSelected;
            }
        }

        /// <inheritdoc />
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            var navItem = element as NavigationViewItem;
            if (navItem != null)
            {
                navItem.Selected -= OnNavigationViewItemSelected;
            }

            base.ClearContainerForItemOverride(element, item);
        }

        /// <summary>
        /// Raises <see cref="BackRequested"/> as the back button would. Used by unit tests.
        /// </summary>
        internal void RaiseBackRequestedForTesting()
        {
            OnBackButtonClick(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Returns the shared selection indicator element from the current template, if resolved.
        /// Used by unit tests.
        /// </summary>
        internal FrameworkElement GetSelectionIndicatorForTesting()
        {
            return _selectionIndicator;
        }

        private static void OnIsBackButtonVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((NavigationView)d).UpdateBackButtonState(true);
        }

        /// <summary>
        /// Transitions the back button to the correct <c>BackButtonStates</c> VSM state
        /// based on <see cref="IsBackButtonVisible"/>. Called without transitions on
        /// initial template application; with transitions on runtime changes.
        /// </summary>
        private void UpdateBackButtonState(bool useTransitions)
        {
            string stateName = IsBackButtonVisible ? "BackButtonVisible" : "BackButtonCollapsed";
            VisualStateManager.GoToState(this, stateName, useTransitions);
        }

        private void OnBackButtonClick(object sender, RoutedEventArgs e)
        {
            var handler = BackRequested;
            if (handler != null)
            {
                handler(this, new NavigationViewBackRequestedEventArgs());
            }
        }

        private void OnPaneToggleButtonClick(object sender, RoutedEventArgs e)
        {
            SetCurrentValue(IsPaneOpenProperty, !IsPaneOpen);
        }

        private void OnNavigationViewItemSelected(object sender, RoutedEventArgs e)
        {
            var navItem = sender as NavigationViewItem;
            if (navItem == null)
            {
                return;
            }

            var fromItem = ItemContainerGenerator.ItemFromContainer(navItem);
            if (fromItem != DependencyProperty.UnsetValue && fromItem != null)
            {
                if (!ReferenceEquals(SelectedItem, fromItem))
                {
                    SelectedItem = fromItem;
                }

                return;
            }

            if (!ReferenceEquals(SelectedItem, navItem))
            {
                SelectedItem = navItem;
            }
        }

        private static void OnIsPaneOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var nav = (NavigationView)d;
            var nowOpen = (bool)e.NewValue;
            if (nowOpen)
            {
                var opening = nav.PaneOpening;
                if (opening != null)
                {
                    opening(nav, EventArgs.Empty);
                }
            }
            else
            {
                var closed = nav.PaneClosed;
                if (closed != null)
                {
                    closed(nav, EventArgs.Empty);
                }
            }

            nav._indicatorPositioned = false;
        }

        private static void OnPaneDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var nav = (NavigationView)d;
            var newMode = (NavigationViewPaneDisplayMode)e.NewValue;

            if (newMode == NavigationViewPaneDisplayMode.LeftCompact)
            {
                nav.SetCurrentValue(IsPaneOpenProperty, false);
            }

            nav._indicatorPositioned = false;
            nav.Dispatcher.BeginInvoke(new Action(() => nav.PositionIndicator(false)), DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Positions the shared indicator at the currently selected item.
        /// When <paramref name="animate"/> is true and the indicator was previously positioned,
        /// runs a depart/arrive animation sequence.
        /// </summary>
        private void PositionIndicator(bool animate)
        {
            if (_selectionIndicator == null || _indicatorHost == null)
            {
                return;
            }

            if (!IsLoaded)
            {
                return;
            }

            if (SelectedItem == null)
            {
                HideIndicator();
                return;
            }

            var nvi = ResolveNavigationViewItem(SelectedItem);
            if (nvi == null || !nvi.IsVisible || nvi.ActualHeight == 0)
            {
                HideIndicator();
                return;
            }

            bool topMode = PaneDisplayMode == NavigationViewPaneDisplayMode.Top;
            double targetOffset = CalculateIndicatorOffset(nvi, topMode);

            if (!animate || !_indicatorPositioned)
            {
                SnapIndicator(targetOffset, topMode);
                return;
            }

            double currentOffset = GetCurrentIndicatorOffset(topMode);
            AnimateIndicator(currentOffset, targetOffset, topMode);
        }

        /// <summary>
        /// Calculates the offset for the indicator relative to its host Grid.
        /// Left modes: Y offset to vertically center on the item.
        /// Top mode: X offset to horizontally center on the item.
        /// </summary>
        private double CalculateIndicatorOffset(NavigationViewItem item, bool topMode)
        {
            try
            {
                var transform = item.TransformToAncestor(_indicatorHost);
                var itemPos = transform.Transform(new Point(0, 0));

                if (topMode)
                {
                    return itemPos.X + (item.ActualWidth - _selectionIndicator.Width) / 2.0;
                }

                return itemPos.Y + (item.ActualHeight - _selectionIndicator.Height) / 2.0;
            }
            catch
            {
                return 0;
            }
        }

        private double GetCurrentIndicatorOffset(bool topMode)
        {
            var group = _selectionIndicator.RenderTransform as TransformGroup;
            if (group != null && group.Children.Count >= 2)
            {
                var translate = group.Children[1] as TranslateTransform;
                if (translate != null)
                {
                    return topMode ? translate.X : translate.Y;
                }
            }

            return 0;
        }

        /// <summary>
        /// Immediately places the indicator at the target offset with no animation.
        /// </summary>
        private void SnapIndicator(double targetOffset, bool topMode)
        {
            StopAnimation();
            EnsureMutableTransform();

            var group = (TransformGroup)_selectionIndicator.RenderTransform;
            var scale = (ScaleTransform)group.Children[0];
            var translate = (TranslateTransform)group.Children[1];

            scale.ScaleX = 1.0;
            scale.ScaleY = 1.0;

            if (topMode)
            {
                translate.X = targetOffset;
                translate.Y = 0;
            }
            else
            {
                translate.X = 0;
                translate.Y = targetOffset;
            }

            _selectionIndicator.Opacity = 1.0;
            _indicatorPositioned = true;
        }

        /// <summary>
        /// Runs a two-phase depart/arrive animation: the indicator shrinks at the old
        /// position, then grows at the new position, giving the appearance of sliding.
        /// </summary>
        private void AnimateIndicator(double fromOffset, double toOffset, bool topMode)
        {
            StopAnimation();
            EnsureMutableTransform();

            int direction = toOffset >= fromOffset ? 1 : -1;
            double travel = 3.0;

            var departDuration = new Duration(TimeSpan.FromMilliseconds(167));
            var arriveDuration = new Duration(TimeSpan.FromMilliseconds(250));
            var ease = new QuarticEase { EasingMode = EasingMode.EaseOut };
            var arriveDelay = TimeSpan.FromMilliseconds(83);

            // Use typed property paths targeting the actual transform objects so layout
            // changes that reorder the TransformGroup children cannot silently break animations.
            var group = (TransformGroup)_selectionIndicator.RenderTransform;
            var scale = (ScaleTransform)group.Children[0];
            var translate = (TranslateTransform)group.Children[1];

            DependencyProperty scaleProp = topMode ? ScaleTransform.ScaleXProperty : ScaleTransform.ScaleYProperty;
            DependencyProperty translateProp = topMode ? TranslateTransform.XProperty : TranslateTransform.YProperty;

            var sb = new Storyboard();

            var scaleDown = new DoubleAnimation(1.0, 0.0, departDuration) { EasingFunction = ease };
            Storyboard.SetTarget(scaleDown, scale);
            Storyboard.SetTargetProperty(scaleDown, new PropertyPath(scaleProp));
            sb.Children.Add(scaleDown);

            var slideDepart = new DoubleAnimation(fromOffset, fromOffset + direction * travel, departDuration) { EasingFunction = ease };
            Storyboard.SetTarget(slideDepart, translate);
            Storyboard.SetTargetProperty(slideDepart, new PropertyPath(translateProp));
            sb.Children.Add(slideDepart);

            var fadeOut = new DoubleAnimation(1.0, 0.0, new Duration(TimeSpan.FromMilliseconds(100)))
            {
                BeginTime = TimeSpan.FromMilliseconds(67)
            };
            Storyboard.SetTarget(fadeOut, _selectionIndicator);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(fadeOut);

            var scaleUp = new DoubleAnimation(0.0, 1.0, arriveDuration) { EasingFunction = ease, BeginTime = arriveDelay };
            Storyboard.SetTarget(scaleUp, scale);
            Storyboard.SetTargetProperty(scaleUp, new PropertyPath(scaleProp));
            sb.Children.Add(scaleUp);

            var slideArrive = new DoubleAnimation(toOffset - direction * travel, toOffset, arriveDuration) { EasingFunction = ease, BeginTime = arriveDelay };
            Storyboard.SetTarget(slideArrive, translate);
            Storyboard.SetTargetProperty(slideArrive, new PropertyPath(translateProp));
            sb.Children.Add(slideArrive);

            var fadeIn = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(83)))
            {
                BeginTime = arriveDelay
            };
            Storyboard.SetTarget(fadeIn, _selectionIndicator);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(fadeIn);

            double capturedTarget = toOffset;
            bool capturedTopMode = topMode;
            sb.Completed += delegate
            {
                _activeStoryboard = null;
                SnapIndicator(capturedTarget, capturedTopMode);
            };

            _activeStoryboard = sb;
            _indicatorPositioned = true;
            sb.Begin();
        }

        private void HideIndicator()
        {
            StopAnimation();
            if (_selectionIndicator != null)
            {
                _selectionIndicator.Opacity = 0;
            }

            _indicatorPositioned = false;
        }

        private void StopAnimation()
        {
            if (_activeStoryboard != null)
            {
                _activeStoryboard.Stop();
                _activeStoryboard = null;
            }
        }

        /// <summary>
        /// Replaces frozen XAML-defined transforms with mutable instances.
        /// </summary>
        private void EnsureMutableTransform()
        {
            if (_selectionIndicator == null)
            {
                return;
            }

            _selectionIndicator.BeginAnimation(UIElement.OpacityProperty, null);

            var group = _selectionIndicator.RenderTransform as TransformGroup;
            bool needsReplacement = group == null
                || group.IsFrozen
                || group.Children.Count < 2;

            if (!needsReplacement)
            {
                var s = group.Children[0] as ScaleTransform;
                var t = group.Children[1] as TranslateTransform;
                needsReplacement = s == null || t == null || s.IsFrozen || t.IsFrozen;
            }

            if (needsReplacement)
            {
                var newGroup = new TransformGroup();
                newGroup.Children.Add(new ScaleTransform(1.0, 1.0));
                newGroup.Children.Add(new TranslateTransform(0, 0));
                _selectionIndicator.RenderTransform = newGroup;
                return;
            }

            var scale = (ScaleTransform)group.Children[0];
            var translate = (TranslateTransform)group.Children[1];

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            translate.BeginAnimation(TranslateTransform.XProperty, null);
            translate.BeginAnimation(TranslateTransform.YProperty, null);
        }

        private NavigationViewItem ResolveNavigationViewItem(object item)
        {
            var nvi = item as NavigationViewItem;
            if (nvi != null)
            {
                return nvi;
            }

            return ItemContainerGenerator.ContainerFromItem(item) as NavigationViewItem;
        }

        private void UpdateSelectedContentFromSelection()
        {
            var nvi = SelectedItem as NavigationViewItem;
            if (nvi != null)
            {
                SetCurrentValue(SelectedContentProperty, nvi.PageContent ?? nvi.Content);
                return;
            }

            if (SelectedItem != null)
            {
                var ic = ItemContainerGenerator.ContainerFromItem(SelectedItem);
                var navFromItem = ic as NavigationViewItem;
                if (navFromItem != null)
                {
                    SetCurrentValue(SelectedContentProperty, navFromItem.PageContent ?? navFromItem.Content);
                    return;
                }
            }

            SetCurrentValue(SelectedContentProperty, null);
        }

        private static NavigationViewItem FindNavigationViewItem(DependencyObject focused)
        {
            var current = focused;
            while (current != null)
            {
                var asItem = current as NavigationViewItem;
                if (asItem != null)
                {
                    return asItem;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
