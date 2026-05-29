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

using Fluence.Wpf.Automation;
using Fluence.Wpf.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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
    [TemplatePart(Name = PartPaneColumn, Type = typeof(ColumnDefinition))]
    [TemplatePart(Name = PartTopItemsHost, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartTopOverflowButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplateVisualState(GroupName = "BackButtonStates", Name = "BackButtonVisible")]
    [TemplateVisualState(GroupName = "BackButtonStates", Name = "BackButtonCollapsed")]
    public class NavigationView : Selector
    {
        /// <summary>
        /// Name of the back button template part.
        /// </summary>
        public const string PartBackButton = "PART_BackButton";

        /// <summary>
        /// Name of the main content presenter template part.
        /// </summary>
        public const string PartContentPresenter = "PART_ContentPresenter";

        /// <summary>
        /// Name of the scroll viewer that hosts pane items.
        /// </summary>
        public const string PartPaneItemsScrollViewer = "PART_PaneItemsScrollViewer";

        /// <summary>
        /// Name of the pane collapse/expand toggle button.
        /// </summary>
        public const string PartPaneToggleButton = "PART_PaneToggleButton";

        /// <summary>
        /// Name of the shared selection indicator element.
        /// </summary>
        public const string PartSelectionIndicator = "PART_SelectionIndicator";

        /// <summary>
        /// Name of the top pane items host template part.
        /// </summary>
        public const string PartTopItemsHost = "PART_TopItemsHost";

        /// <summary>
        /// Name of the top pane overflow button template part.
        /// </summary>
        public const string PartTopOverflowButton = "PART_TopOverflowButton";

        private const string PartPaneColumn = "PaneColumn";
        private const double PaneClosedWidth = 48.0;
        private const double PaneClosedWithBackWidth = 96.0;
        private const double PaneOpenWidth = 320.0;
        private const double PaneAnimationMilliseconds = 167.0;

        private static readonly DependencyProperty IsTopOverflowCollapsedProperty =
            DependencyProperty.RegisterAttached(
                "IsTopOverflowCollapsed",
                typeof(bool),
                typeof(NavigationView),
                new PropertyMetadata(false));

        // Margins and offsets used in indicator and top overflow positioning calculations.
        // The indicator sits just inside the selected item's rounded OuterBorder (Margin 4 + 2px
        // stroke), flush against the inner edge with no padding gap, rather than floating in the
        // pane to the left of the item.
        private const double NavigationItemOuterHorizontalMargin = 9.0;
        private const double NavigationItemChildIndicatorOffset = 44.0;
        private const double TopOverflowReservedEndPadding = 12.0;

        /// <summary>
        /// Identifies the <see cref="PaneDisplayMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaneDisplayModeProperty = DependencyProperty.Register(
            "PaneDisplayMode",
            typeof(NavigationViewPaneDisplayMode),
            typeof(NavigationView),
            new FrameworkPropertyMetadata(
                NavigationViewPaneDisplayMode.Left,
                FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnPaneDisplayModeChanged));

        /// <summary>
        /// Identifies the <see cref="SelectionFollowsFocus"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionFollowsFocusProperty = DependencyProperty.Register(
            "SelectionFollowsFocus",
            typeof(bool),
            typeof(NavigationView),
            new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsBackButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsBackButtonVisibleProperty = DependencyProperty.Register(
            "IsBackButtonVisible",
            typeof(bool),
            typeof(NavigationView),
            new PropertyMetadata(false, OnBackButtonStateChanged));

        /// <summary>
        /// Identifies the <see cref="IsBackEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsBackEnabledProperty = DependencyProperty.Register(
            "IsBackEnabled",
            typeof(bool),
            typeof(NavigationView),
            new PropertyMetadata(true, OnBackButtonStateChanged));

        /// <summary>
        /// Identifies the <see cref="IsPaneToggleButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPaneToggleButtonVisibleProperty = DependencyProperty.Register(
            "IsPaneToggleButtonVisible",
            typeof(bool),
            typeof(NavigationView),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.AffectsMeasure,
                null,
                CoerceIsPaneToggleButtonVisible));

        /// <summary>
        /// Identifies the <see cref="Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HeaderTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
            "HeaderTemplate",
            typeof(DataTemplate),
            typeof(NavigationView),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="PaneHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaneHeaderProperty = DependencyProperty.Register(
            "PaneHeader",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="PaneFooter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaneFooterProperty = DependencyProperty.Register(
            "PaneFooter",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="ContentBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentBackgroundProperty = DependencyProperty.Register(
            "ContentBackground",
            typeof(Brush),
            typeof(NavigationView),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="IsPaneOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPaneOpenProperty = DependencyProperty.Register(
            "IsPaneOpen",
            typeof(bool),
            typeof(NavigationView),
            new FrameworkPropertyMetadata(true, OnIsPaneOpenChanged, CoerceIsPaneOpen));

        /// <summary>
        /// Identifies the <see cref="Content"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(null));

        /// <summary>
        /// Initializes static members of the NavigationView class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the NavigationView control uses its own default
        /// style by associating it with the appropriate style key. This is necessary for custom controls to apply their
        /// styles correctly in XAML-based applications.</remarks>
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
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Occurs when a navigation item is invoked before selection changes.
        /// </summary>
        public event EventHandler<NavigationViewItemInvokedEventArgs>? ItemInvoked;

        /// <summary>
        /// Occurs when the back button is invoked.
        /// </summary>
        public event EventHandler<NavigationViewBackRequestedEventArgs>? BackRequested;

        /// <summary>
        /// Occurs when the pane is opening (expanded in left mode).
        /// </summary>
        public event EventHandler? PaneOpening;

        /// <summary>
        /// Occurs when the pane has closed (collapsed in left mode).
        /// </summary>
        public event EventHandler? PaneClosed;

        /// <summary>
        /// Gets or sets whether the pane is shown on the left or across the top.
        /// </summary>
        public NavigationViewPaneDisplayMode PaneDisplayMode
        {
            get => (NavigationViewPaneDisplayMode)GetValue(PaneDisplayModeProperty);
            set => SetValue(PaneDisplayModeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether keyboard focus on an item selects it immediately.
        /// </summary>
        public bool SelectionFollowsFocus
        {
            get => (bool)GetValue(SelectionFollowsFocusProperty);
            set => SetValue(SelectionFollowsFocusProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the back button is shown.
        /// </summary>
        public bool IsBackButtonVisible
        {
            get => (bool)GetValue(IsBackButtonVisibleProperty);
            set => SetValue(IsBackButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the back button can be invoked.
        /// </summary>
        public bool IsBackEnabled
        {
            get => (bool)GetValue(IsBackEnabledProperty);
            set => SetValue(IsBackEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the pane collapse/expand toggle button is shown in left pane modes.
        /// </summary>
        public bool IsPaneToggleButtonVisible
        {
            get => (bool)GetValue(IsPaneToggleButtonVisibleProperty);
            set => SetValue(IsPaneToggleButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets header content displayed beside the navigation chrome.
        /// </summary>
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the DataTemplate used to display the <see cref="Header"/>.
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get => (DataTemplate)GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets content at the start of the pane chrome (title area).
        /// </summary>
        public object PaneHeader
        {
            get => GetValue(PaneHeaderProperty);
            set => SetValue(PaneHeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets content at the end of the pane (footer).
        /// </summary>
        public object PaneFooter
        {
            get => GetValue(PaneFooterProperty);
            set => SetValue(PaneFooterProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush for the content area.
        /// </summary>
        public Brush ContentBackground
        {
            get => (Brush)GetValue(ContentBackgroundProperty);
            set => SetValue(ContentBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the left pane is expanded.
        /// </summary>
        public bool IsPaneOpen
        {
            get => (bool)GetValue(IsPaneOpenProperty);
            set => SetValue(IsPaneOpenProperty, value);
        }

        /// <summary>
        /// Gets or sets the content hosted in the main area.
        /// </summary>
        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            _backButton?.Click -= OnBackButtonClick;
            _paneToggleButton?.Click -= OnPaneToggleButtonClick;
            _topOverflowButton?.Click -= OnTopOverflowButtonClick;
            StopPaneColumnAnimation();
            base.OnApplyTemplate();
            _backButton = GetTemplateChild(PartBackButton) as System.Windows.Controls.Button;
            _backButton?.Click += OnBackButtonClick;
            _paneToggleButton = GetTemplateChild(PartPaneToggleButton) as System.Windows.Controls.Button;
            _paneToggleButton?.Click += OnPaneToggleButtonClick;
            _topItemsHost = GetTemplateChild(PartTopItemsHost) as FrameworkElement;
            if (GetTemplateChild(PartTopOverflowButton) is System.Windows.Controls.Button topOverflowButton)
            {
                _topOverflowButton = topOverflowButton;
                _topOverflowButton.Click += OnTopOverflowButtonClick;
            }
            else
            {
                _topOverflowButton = null;
            }

            _paneColumn = GetTemplateChild(PartPaneColumn) as ColumnDefinition;
            _selectionIndicator = GetTemplateChild(PartSelectionIndicator) as FrameworkElement;
            _indicatorHost = _selectionIndicator is not null ? VisualTreeHelper.GetParent(_selectionIndicator) as FrameworkElement : null;
            _indicatorPositioned = false;
            StopAnimation();
            CoerceTopPaneProperties();
            UpdateTitleBarExtensionForPaneMode();
            UpdateBackButtonState(false);
            UpdatePaneColumnWidth(false);
            ScheduleTopOverflowUpdate();
            ScheduleIndicatorPosition(false);
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            NavigationViewItem? previousItem = e.RemovedItems.Count > 0
                ? ResolveNavigationViewItem(e.RemovedItems[0])
                : null;
            base.OnSelectionChanged(e);
            _ = Dispatcher.BeginInvoke(new Action(() => PositionIndicator(true, previousItem)), DispatcherPriority.Loaded);
        }

        /// <inheritdoc />
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            ScheduleTopOverflowUpdate();
        }

        /// <inheritdoc />
        protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnPreviewGotKeyboardFocus(e);
            if (!SelectionFollowsFocus)
            {
                return;
            }
            if (FindNavigationViewItem(e.NewFocus as DependencyObject) is not NavigationViewItem navItem)
            {
                return;
            }

            object fromContainer = ItemContainerGenerator.ItemFromContainer(navItem);
            if (fromContainer != DependencyProperty.UnsetValue && fromContainer is not null)
            {
                if (!ReferenceEquals(SelectedItem, fromContainer))
                {
                    SelectedItem = fromContainer;
                }
            }
            else if (!ReferenceEquals(SelectedItem, navItem))
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
            return item is NavigationViewItem or NavigationViewItemHeader or NavigationViewItemSeparator;
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
            if (element is NavigationViewItem navItem)
            {
                navItem.Selected -= OnNavigationViewItemSelected;
                navItem.Selected += OnNavigationViewItemSelected;
                navItem.Loaded -= OnNavigationViewItemLoaded;
                navItem.Loaded += OnNavigationViewItemLoaded;
                navItem.SizeChanged -= OnNavigationViewItemSizeChanged;
                navItem.SizeChanged += OnNavigationViewItemSizeChanged;
                navItem.IsVisibleChanged -= OnNavigationViewItemIsVisibleChanged;
                navItem.IsVisibleChanged += OnNavigationViewItemIsVisibleChanged;
            }
        }

        /// <inheritdoc />
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            if (element is NavigationViewItem navItem)
            {
                navItem.Selected -= OnNavigationViewItemSelected;
                navItem.Loaded -= OnNavigationViewItemLoaded;
                navItem.SizeChanged -= OnNavigationViewItemSizeChanged;
                navItem.IsVisibleChanged -= OnNavigationViewItemIsVisibleChanged;
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
        internal FrameworkElement? GetSelectionIndicatorForTesting()
        {
            return _selectionIndicator;
        }

        internal double GetPaneColumnWidthForTesting()
        {
            return _paneColumn?.Width.Value ?? double.NaN;
        }

        internal Point CalculateDepartPositionForTesting(
            Point fromPosition,
            NavigationViewItem? previousItem,
            bool topMode,
            double direction)
        {
            return CalculateDepartPosition(fromPosition, previousItem, topMode, direction);
        }

        internal void InvokeItem(NavigationViewItem item)
        {
            if (item is null || !item.IsEnabled)
            {
                return;
            }
            object invokedItem = GetDataFromContainer(item);
            ItemInvoked?.Invoke(this, new NavigationViewItemInvokedEventArgs(invokedItem, item, false));
            SelectItemFromContainer(item);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _topOverflowButton?.Click -= OnTopOverflowButtonClick;
            DetachTitleBarWindowWatcher();
            StopAnimation();
            StopPaneColumnAnimation();
            _paneColumn = null;
            _selectionIndicator = null;
            _indicatorHost = null;
            _topItemsHost = null;
            _topOverflowButton = null;
            _indicatorPositioned = false;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachTitleBarWindowWatcher();
            CoerceTopPaneProperties();
            UpdateTitleBarExtensionForPaneMode();
            ScheduleTopOverflowUpdate();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleTopOverflowUpdate();
        }

        private static void OnBackButtonStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NavigationView nav = (NavigationView)d;
            nav.UpdateBackButtonState(true);
            nav.UpdatePaneColumnWidth(false);
        }

        /// <summary>
        /// Transitions the back button to the correct <c>BackButtonStates</c> VSM state
        /// based on <see cref="IsBackButtonVisible"/>. Called without transitions on
        /// initial template application; with transitions on runtime changes.
        /// </summary>
        private void UpdateBackButtonState(bool useTransitions)
        {
            bool isVisible = IsBackButtonVisible && IsBackEnabled;
            string stateName = isVisible ? "BackButtonVisible" : "BackButtonCollapsed";
            _ = VisualStateManager.GoToState(this, stateName, useTransitions);
            if (_backButton is not null)
            {
                _backButton.BeginAnimation(VisibilityProperty, null);
                _backButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnBackButtonClick(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, new NavigationViewBackRequestedEventArgs());
        }

        private void OnPaneToggleButtonClick(object sender, RoutedEventArgs e)
        {
            SetCurrentValue(IsPaneOpenProperty, !IsPaneOpen);
        }

        private void OnNavigationViewItemSelected(object sender, RoutedEventArgs e)
        {
            if (sender is not NavigationViewItem navItem)
            {
                return;
            }
            SelectItemFromContainer(navItem);
        }

        private void OnNavigationViewItemLoaded(object sender, RoutedEventArgs e)
        {
            if (SelectedItem is not null)
            {
                ScheduleIndicatorPosition(false);
            }

            ScheduleTopOverflowUpdate();
        }

        private void OnNavigationViewItemSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleTopOverflowUpdate();
        }

        private void OnNavigationViewItemIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!_updatingTopOverflow && sender is NavigationViewItem navItem)
            {
                navItem.ClearValue(IsTopOverflowCollapsedProperty);
                ScheduleTopOverflowUpdate();
            }
        }

        private static void OnIsPaneOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NavigationView nav = (NavigationView)d;
            bool nowOpen = (bool)e.NewValue;
            if (nowOpen)
            {
                nav.PaneOpening?.Invoke(nav, EventArgs.Empty);
            }
            else
            {
                nav.PaneClosed?.Invoke(nav, EventArgs.Empty);
            }
            nav._indicatorPositioned = false;
            nav.UpdatePaneColumnWidth(true);
            nav.ScheduleTopOverflowUpdate();
            nav.ScheduleIndicatorPosition(false);
        }

        private static void OnPaneDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NavigationView nav = (NavigationView)d;
            NavigationViewPaneDisplayMode newMode = (NavigationViewPaneDisplayMode)e.NewValue;
            if (newMode == NavigationViewPaneDisplayMode.LeftCompact)
            {
                nav.SetCurrentValue(IsPaneOpenProperty, false);
            }
            nav.CoerceTopPaneProperties();
            nav.UpdateTitleBarExtensionForPaneMode();
            nav._indicatorPositioned = false;
            nav.UpdatePaneColumnWidth(false);
            nav.ScheduleTopOverflowUpdate();
            nav.ScheduleIndicatorPosition(false);
        }

        private static object CoerceIsPaneOpen(DependencyObject d, object baseValue)
        {
            NavigationView nav = (NavigationView)d;
            return nav.PaneDisplayMode == NavigationViewPaneDisplayMode.Top || (bool)baseValue;
        }

        private static object CoerceIsPaneToggleButtonVisible(DependencyObject d, object baseValue)
        {
            _ = baseValue;
            NavigationView nav = (NavigationView)d;
            return nav.PaneDisplayMode != NavigationViewPaneDisplayMode.Top;
        }

        private void CoerceTopPaneProperties()
        {
            CoerceValue(IsPaneOpenProperty);
            CoerceValue(IsPaneToggleButtonVisibleProperty);
        }

        private void UpdateTitleBarExtensionForPaneMode()
        {
            if (Window.GetWindow(this) is not FluenceWindow window || _updatingTitleBarExtension)
            {
                return;
            }

            bool? desiredValue = null;
            if (PaneDisplayMode == NavigationViewPaneDisplayMode.Left)
            {
                desiredValue = true;
            }
            else if (PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                desiredValue = false;
            }

            if (desiredValue is null || window.ExtendsContentIntoTitleBar == desiredValue.Value)
            {
                return;
            }

            _updatingTitleBarExtension = true;
            try
            {
                window.SetCurrentValue(FluenceWindow.ExtendsContentIntoTitleBarProperty, desiredValue.Value);
            }
            finally
            {
                _updatingTitleBarExtension = false;
            }
        }

        private void AttachTitleBarWindowWatcher()
        {
            FluenceWindow? window = Window.GetWindow(this) as FluenceWindow;
            if (ReferenceEquals(window, _titleBarExtensionWindow))
            {
                return;
            }

            DetachTitleBarWindowWatcher();
            _titleBarExtensionWindow = window;

            if (_titleBarExtensionWindow is not null)
            {
                _titleBarExtensionDescriptor ??= DependencyPropertyDescriptor.FromProperty(
                    FluenceWindow.ExtendsContentIntoTitleBarProperty,
                    typeof(FluenceWindow));
                _titleBarExtensionDescriptor?.AddValueChanged(_titleBarExtensionWindow, OnTitleBarExtensionChanged);
            }
        }

        private void DetachTitleBarWindowWatcher()
        {
            if (_titleBarExtensionWindow is not null)
            {
                _titleBarExtensionDescriptor?.RemoveValueChanged(_titleBarExtensionWindow, OnTitleBarExtensionChanged);
                _titleBarExtensionWindow = null;
            }
        }

        private void OnTitleBarExtensionChanged(object? sender, EventArgs e)
        {
            UpdateTitleBarExtensionForPaneMode();
        }

        private void UpdatePaneColumnWidth(bool useAnimation)
        {
            if (_paneColumn is null)
            {
                return;
            }

            if (PaneDisplayMode is not NavigationViewPaneDisplayMode.Left and not NavigationViewPaneDisplayMode.LeftCompact)
            {
                StopPaneColumnAnimation();
                return;
            }

            double targetWidth = IsPaneOpen ? PaneOpenWidth : GetClosedPaneWidth();
            if (!useAnimation)
            {
                StopPaneColumnAnimation();
                _paneColumn.Width = new GridLength(targetWidth);
                return;
            }

            double currentWidth = GetCurrentPaneColumnWidth();
            if (Math.Abs(currentWidth - targetWidth) <= 0.1)
            {
                StopPaneColumnAnimation();
                _paneColumn.Width = new GridLength(targetWidth);
                return;
            }

            ColumnDefinition paneColumn = _paneColumn;
            int animationGeneration = ++_paneColumnAnimationGeneration;
            GridLengthAnimation animation = new()
            {
                From = new GridLength(currentWidth),
                To = new GridLength(targetWidth),
                Duration = new Duration(TimeSpan.FromMilliseconds(PaneAnimationMilliseconds)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += delegate
            {
                if (animationGeneration != _paneColumnAnimationGeneration || !ReferenceEquals(paneColumn, _paneColumn))
                {
                    return;
                }

                paneColumn.BeginAnimation(ColumnDefinition.WidthProperty, null);
                paneColumn.Width = new GridLength(targetWidth);
            };

            paneColumn.BeginAnimation(ColumnDefinition.WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }

        private double GetCurrentPaneColumnWidth()
        {
            if (_paneColumn is null)
            {
                return GetClosedPaneWidth();
            }

            GridLength current = _paneColumn.Width;
            return current.GridUnitType == GridUnitType.Pixel
                ? current.Value
                : GetClosedPaneWidth();
        }

        private double GetClosedPaneWidth()
        {
            return IsBackButtonVisible && IsBackEnabled ? PaneClosedWithBackWidth : PaneClosedWidth;
        }

        private void ScheduleIndicatorPosition(bool animate)
        {
            _ = Dispatcher.BeginInvoke(new Action(() => PositionIndicator(animate)), DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Positions the shared indicator at the currently selected item.
        /// When <paramref name="animate"/> is true and the indicator was previously positioned,
        /// runs a depart/arrive animation sequence.
        /// </summary>
        private void PositionIndicator(bool animate)
        {
            PositionIndicator(animate, null);
        }

        private void PositionIndicator(bool animate, NavigationViewItem? previousItem)
        {
            if (_selectionIndicator is null || _indicatorHost is null)
            {
                return;
            }
            if (!IsLoaded)
            {
                return;
            }
            if (SelectedItem is null)
            {
                HideIndicator();
                return;
            }
            if (ResolveNavigationViewItem(SelectedItem) is not NavigationViewItem nvi || !nvi.IsVisible || nvi.ActualHeight is 0)
            {
                HideIndicator();
                return;
            }

            bool topMode = PaneDisplayMode == NavigationViewPaneDisplayMode.Top;
            Point targetPosition = CalculateIndicatorPosition(nvi, topMode);
            if (!animate || !_indicatorPositioned)
            {
                SnapIndicator(targetPosition);
                return;
            }

            Point currentPosition = GetCurrentIndicatorPosition();
            AnimateIndicator(currentPosition, targetPosition, topMode, previousItem, nvi);
        }

        /// <summary>
        /// Calculates the translate position for the indicator relative to its host Grid.
        /// </summary>
        private Point CalculateIndicatorPosition(NavigationViewItem item, bool topMode)
        {
            if (_selectionIndicator is null)
            {
                throw new InvalidOperationException("Selection indicator template part is missing.");
            }
            try
            {
                GeneralTransform transform = item.TransformToAncestor(_indicatorHost);
                Point itemPos = transform.Transform(new Point(0, 0));
                if (topMode)
                {
                    return new Point(itemPos.X + ((item.ActualWidth - _selectionIndicator.Width) / 2.0), 0.0);
                }

                double x = itemPos.X + NavigationItemOuterHorizontalMargin;
                if (ShouldIndentSelectionIndicator(item, topMode))
                {
                    x += NavigationItemChildIndicatorOffset;
                }
                return new Point(x, itemPos.Y + ((item.ActualHeight - _selectionIndicator.Height) / 2.0));
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                return new Point(0, 0);
            }
        }

        private bool ShouldIndentSelectionIndicator(NavigationViewItem item, bool topMode)
        {
            return !topMode && item is not null && item.IsChildItem && (IsPaneOpen || (PaneDisplayMode != NavigationViewPaneDisplayMode.Left && PaneDisplayMode != NavigationViewPaneDisplayMode.LeftCompact));
        }

        private Point GetCurrentIndicatorPosition()
        {
            return _selectionIndicator?.RenderTransform is TransformGroup group && group.Children.Count >= 2 && group.Children[1] is TranslateTransform translate
                ? new Point(translate.X, translate.Y)
                : new Point(0, 0);
        }

        /// <summary>
        /// Immediately places the indicator at the target offset with no animation.
        /// </summary>
        private void SnapIndicator(Point targetPosition)
        {
            if (_selectionIndicator is null)
            {
                throw new InvalidOperationException("Selection indicator template part is missing.");
            }
            StopAnimation(); EnsureMutableTransform();
            TransformGroup group = (TransformGroup)_selectionIndicator.RenderTransform;
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            TranslateTransform translate = (TranslateTransform)group.Children[1];

            scale.ScaleX = 1.0;
            scale.ScaleY = 1.0;
            translate.X = targetPosition.X;
            translate.Y = targetPosition.Y;

            _selectionIndicator.Opacity = 1.0;
            _indicatorPositioned = true;
        }

        private void AnimateIndicator(
            Point fromPosition,
            Point toPosition,
            bool topMode,
            NavigationViewItem? previousItem,
            NavigationViewItem targetItem)
        {
            if (_selectionIndicator is null)
            {
                throw new InvalidOperationException("Selection indicator template part is missing.");
            }
            StopAnimation(); EnsureMutableTransform();
            TransformGroup group = (TransformGroup)_selectionIndicator.RenderTransform;
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            TranslateTransform translate = (TranslateTransform)group.Children[1];
            int animationId = _indicatorAnimationGeneration;
            DependencyProperty axisProperty = topMode ? TranslateTransform.XProperty : TranslateTransform.YProperty;
            DependencyProperty scaleProperty = topMode ? ScaleTransform.ScaleXProperty : ScaleTransform.ScaleYProperty;
            double fromAxis = topMode ? fromPosition.X : fromPosition.Y;
            double toAxis = topMode ? toPosition.X : toPosition.Y;
            double direction = toAxis < fromAxis ? -1.0 : 1.0;

            scale.ScaleX = 1.0;
            scale.ScaleY = 1.0;
            translate.X = fromPosition.X;
            translate.Y = fromPosition.Y;
            _selectionIndicator.Opacity = 1.0;

            Point departPosition = CalculateDepartPosition(fromPosition, previousItem, topMode, direction);
            Point arriveStartPosition = CalculateArriveStartPosition(toPosition, targetItem, topMode, direction);
            double departAxis = topMode ? departPosition.X : departPosition.Y;
            double arriveStartAxis = topMode ? arriveStartPosition.X : arriveStartPosition.Y;
            Duration departDuration = new(TimeSpan.FromMilliseconds(90));
            Duration arriveDuration = new(TimeSpan.FromMilliseconds(140));
            CubicEase departEase = new() { EasingMode = EasingMode.EaseIn };
            CubicEase arriveEase = new() { EasingMode = EasingMode.EaseOut };

            DoubleAnimation departAxisAnimation = new(fromAxis, departAxis, departDuration)
            {
                EasingFunction = departEase,
                FillBehavior = FillBehavior.Stop
            };
            DoubleAnimation departOpacityAnimation = new(1.0, 0.0, departDuration)
            {
                EasingFunction = departEase,
                FillBehavior = FillBehavior.Stop
            };
            DoubleAnimation departScaleAnimation = new(1.0, 0.72, departDuration)
            {
                EasingFunction = departEase,
                FillBehavior = FillBehavior.Stop
            };

            departAxisAnimation.Completed += delegate
            {
                if (animationId != _indicatorAnimationGeneration)
                {
                    return;
                }
                translate.BeginAnimation(axisProperty, null);
                scale.BeginAnimation(scaleProperty, null);
                _selectionIndicator.BeginAnimation(OpacityProperty, null);
                if (topMode)
                {
                    translate.X = arriveStartPosition.X;
                    translate.Y = toPosition.Y;
                    scale.ScaleX = 0.72;
                    scale.ScaleY = 1.0;
                }
                else
                {
                    translate.X = toPosition.X;
                    translate.Y = arriveStartPosition.Y;
                    scale.ScaleX = 1.0;
                    scale.ScaleY = 0.72;
                }
                _selectionIndicator.Opacity = 0.0;

                DoubleAnimation arriveAxisAnimation = new(arriveStartAxis, toAxis, arriveDuration)
                {
                    EasingFunction = arriveEase,
                    FillBehavior = FillBehavior.Stop
                };
                DoubleAnimation arriveOpacityAnimation = new(0.0, 1.0, arriveDuration)
                {
                    EasingFunction = arriveEase,
                    FillBehavior = FillBehavior.Stop
                };
                DoubleAnimation arriveScaleAnimation = new(0.72, 1.0, arriveDuration)
                {
                    EasingFunction = arriveEase,
                    FillBehavior = FillBehavior.Stop
                };

                arriveAxisAnimation.Completed += delegate
                {
                    if (animationId != _indicatorAnimationGeneration)
                    {
                        return;
                    }

                    translate.BeginAnimation(axisProperty, null);
                    scale.BeginAnimation(scaleProperty, null);
                    _selectionIndicator.BeginAnimation(OpacityProperty, null);

                    translate.X = toPosition.X;
                    translate.Y = toPosition.Y;
                    scale.ScaleX = 1.0;
                    scale.ScaleY = 1.0;
                    _selectionIndicator.Opacity = 1.0;
                    _indicatorPositioned = true;
                };
                translate.BeginAnimation(axisProperty, arriveAxisAnimation, HandoffBehavior.SnapshotAndReplace);
                scale.BeginAnimation(scaleProperty, arriveScaleAnimation, HandoffBehavior.SnapshotAndReplace);
                _selectionIndicator.BeginAnimation(OpacityProperty, arriveOpacityAnimation, HandoffBehavior.SnapshotAndReplace);
            };
            _indicatorPositioned = true;
            translate.BeginAnimation(axisProperty, departAxisAnimation, HandoffBehavior.SnapshotAndReplace);
            scale.BeginAnimation(scaleProperty, departScaleAnimation, HandoffBehavior.SnapshotAndReplace);
            _selectionIndicator.BeginAnimation(OpacityProperty, departOpacityAnimation, HandoffBehavior.SnapshotAndReplace);
        }

        private Point CalculateDepartPosition(
            Point fromPosition,
            NavigationViewItem? previousItem,
            bool topMode,
            double direction)
        {
            double length = GetIndicatorLength(topMode);
            if (topMode)
            {
                double x = fromPosition.X + (direction * length);
                if (previousItem is not null && previousItem.IsVisible && previousItem.ActualWidth > 0)
                {
                    try
                    {
                        GeneralTransform transform = previousItem.TransformToAncestor(_indicatorHost);
                        Point itemPos = transform.Transform(new Point(0, 0));
                        x = direction > 0 ? itemPos.X + previousItem.ActualWidth : itemPos.X - length;
                    }
                    catch (Exception ex) when (ex.Message is not null)
                    {
                        return new Point(x, fromPosition.Y);
                    }
                }
                return new Point(x, fromPosition.Y);
            }

            double y = fromPosition.Y + (direction * length);
            if (previousItem is not null && previousItem.IsVisible && previousItem.ActualHeight > 0)
            {
                try
                {
                    GeneralTransform transform = previousItem.TransformToAncestor(_indicatorHost);
                    Point itemPos = transform.Transform(new Point(0, 0));
                    y = direction > 0 ? itemPos.Y + previousItem.ActualHeight : itemPos.Y - length;
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    return new Point(fromPosition.X, y);
                }
            }
            return new Point(fromPosition.X, y);
        }

        private Point CalculateArriveStartPosition(
            Point toPosition,
            NavigationViewItem targetItem,
            bool topMode,
            double direction)
        {
            double length = GetIndicatorLength(topMode);
            if (topMode)
            {
                double x = toPosition.X - (direction * length);
                if (targetItem is not null && targetItem.IsVisible && targetItem.ActualWidth > 0)
                {
                    try
                    {
                        GeneralTransform transform = targetItem.TransformToAncestor(_indicatorHost);
                        Point itemPos = transform.Transform(new Point(0, 0));
                        x = direction > 0 ? itemPos.X - length : itemPos.X + targetItem.ActualWidth;
                    }
                    catch (Exception ex) when (ex.Message is not null)
                    {
                        return new Point(x, toPosition.Y);
                    }
                }

                return new Point(x, toPosition.Y);
            }

            double y = toPosition.Y - (direction * length);
            if (targetItem is not null && targetItem.IsVisible && targetItem.ActualHeight > 0)
            {
                try
                {
                    GeneralTransform transform = targetItem.TransformToAncestor(_indicatorHost);
                    Point itemPos = transform.Transform(new Point(0, 0));
                    y = direction > 0 ? itemPos.Y - length : itemPos.Y + targetItem.ActualHeight;
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    return new Point(toPosition.X, y);
                }
            }
            return new Point(toPosition.X, y);
        }

        private double GetIndicatorLength(bool topMode)
        {
            if (_selectionIndicator is null)
            {
                throw new InvalidOperationException("Selection indicator template part is missing.");
            }
            double actual = topMode ? _selectionIndicator.ActualWidth : _selectionIndicator.ActualHeight;
            if (actual > 0)
            {
                return actual;
            }
            double explicitLength = topMode ? _selectionIndicator.Width : _selectionIndicator.Height;
            return explicitLength > 0 ? explicitLength : 16.0;
        }

        private void HideIndicator()
        {
            StopAnimation();
            _ = _selectionIndicator?.Opacity = 0;
            _indicatorPositioned = false;
        }

        private void StopAnimation()
        {
            _indicatorAnimationGeneration++;
            if (_selectionIndicator is null)
            {
                return;
            }

            _selectionIndicator.BeginAnimation(OpacityProperty, null);
            if (_selectionIndicator.RenderTransform is TransformGroup group && group.Children.Count >= 2)
            {
                if (group.Children[0] is ScaleTransform scale && !scale.IsFrozen)
                {
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }
                if (group.Children[1] is TranslateTransform translate && !translate.IsFrozen)
                {
                    translate.BeginAnimation(TranslateTransform.XProperty, null);
                    translate.BeginAnimation(TranslateTransform.YProperty, null);
                }
            }
        }

        private void StopPaneColumnAnimation()
        {
            _paneColumnAnimationGeneration++;
            _paneColumn?.BeginAnimation(ColumnDefinition.WidthProperty, null);
        }

        /// <summary>
        /// Replaces frozen XAML-defined transforms with mutable instances.
        /// </summary>
        private void EnsureMutableTransform()
        {
            if (_selectionIndicator is null)
            {
                return;
            }

            _selectionIndicator.BeginAnimation(OpacityProperty, null);
            if (_selectionIndicator.RenderTransform as TransformGroup is not TransformGroup group || group.IsFrozen || group.Children.Count < 2 || group.Children[0] is not ScaleTransform s || group.Children[1] is not TranslateTransform t || s.IsFrozen || t.IsFrozen)
            {
                TransformGroup newGroup = new();
                newGroup.Children.Add(new ScaleTransform(1.0, 1.0));
                newGroup.Children.Add(new TranslateTransform(0, 0));
                _selectionIndicator.RenderTransform = newGroup;
                return;
            }
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            TranslateTransform translate = (TranslateTransform)group.Children[1];
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            translate.BeginAnimation(TranslateTransform.XProperty, null);
            translate.BeginAnimation(TranslateTransform.YProperty, null);
        }

        private NavigationViewItem? ResolveNavigationViewItem(object? item)
        {
            return item is not NavigationViewItem nvi
                ? ItemContainerGenerator.ContainerFromItem(item) as NavigationViewItem
                : nvi;
        }

        internal void SelectItemFromContainer(NavigationViewItem navItem)
        {
            if (navItem is null)
            {
                return;
            }
            object data = GetDataFromContainer(navItem);
            if (!ReferenceEquals(SelectedItem, data))
            {
                SelectedItem = data;
            }
        }

        private object GetDataFromContainer(NavigationViewItem navItem)
        {
            object data = ItemContainerGenerator.ItemFromContainer(navItem);
            return (data != DependencyProperty.UnsetValue && data is not null) ? data : navItem;
        }

        private void OnTopOverflowButtonClick(object sender, RoutedEventArgs e)
        {
            if (_topOverflowButton?.ContextMenu is null || _topOverflowButton.ContextMenu.Items.Count == 0)
            {
                return;
            }

            _topOverflowButton.ContextMenu.PlacementTarget = _topOverflowButton;
            _topOverflowButton.ContextMenu.Placement = PlacementMode.Bottom;
            _topOverflowButton.ContextMenu.IsOpen = true;
        }

        private void OnTopOverflowMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem { Tag: NavigationViewItem navItem })
            {
                InvokeItem(navItem);
            }
        }

        private void ScheduleTopOverflowUpdate()
        {
            if (_topOverflowUpdateScheduled)
            {
                return;
            }

            _topOverflowUpdateScheduled = true;
            _ = Dispatcher.BeginInvoke(
                new Action(UpdateTopOverflow),
                DispatcherPriority.Loaded);
        }

        private void UpdateTopOverflow()
        {
            _topOverflowUpdateScheduled = false;

            if (_updatingTopOverflow)
            {
                return;
            }

            _updatingTopOverflow = true;
            try
            {
                List<NavigationViewItem> navItems = GetTopNavigationItems();
                foreach (NavigationViewItem navItem in navItems)
                {
                    if ((bool)navItem.GetValue(IsTopOverflowCollapsedProperty))
                    {
                        navItem.Visibility = Visibility.Visible;
                        navItem.ClearValue(IsTopOverflowCollapsedProperty);
                    }
                }

                if (PaneDisplayMode != NavigationViewPaneDisplayMode.Top || _topOverflowButton is null || _topItemsHost is null)
                {
                    if (_topOverflowButton is not null)
                    {
                        _topOverflowButton.Visibility = Visibility.Collapsed;
                        _topOverflowButton.ContextMenu = null;
                        SetTopOverflowButtonOffset(0.0);
                    }

                    return;
                }

                _topOverflowButton.Visibility = Visibility.Collapsed;
                _topOverflowButton.ContextMenu = null;
                SetTopOverflowButtonOffset(0.0);
                UpdateLayout();

                double availableWidth = _topItemsHost.ActualWidth;
                if (availableWidth <= 0.0)
                {
                    return;
                }

                double totalItemWidth = 0.0;
                foreach (NavigationViewItem navItem in navItems)
                {
                    if (navItem.Visibility == Visibility.Visible)
                    {
                        totalItemWidth += GetElementWidth(navItem);
                    }
                }

                _topOverflowButton.Visibility = Visibility.Visible;
                _topOverflowButton.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                double overflowButtonWidth = GetElementWidth(_topOverflowButton);
                if (totalItemWidth <= availableWidth)
                {
                    _topOverflowButton.Visibility = Visibility.Collapsed;
                    SetTopOverflowButtonOffset(0.0);
                    return;
                }

                double visibleItemsWidthLimit = Math.Max(
                    0.0,
                    availableWidth - overflowButtonWidth - TopOverflowReservedEndPadding);
                double usedWidth = 0.0;
                List<NavigationViewItem> overflowItems = [];

                foreach (NavigationViewItem navItem in navItems)
                {
                    if (navItem.Visibility != Visibility.Visible)
                    {
                        continue;
                    }

                    double itemWidth = GetElementWidth(navItem);
                    if (usedWidth + itemWidth <= visibleItemsWidthLimit)
                    {
                        usedWidth += itemWidth;
                    }
                    else
                    {
                        navItem.SetValue(IsTopOverflowCollapsedProperty, true);
                        navItem.Visibility = Visibility.Collapsed;
                        overflowItems.Add(navItem);
                    }
                }

                double overflowOffset = usedWidth;

                if (overflowItems.Count == 0)
                {
                    _topOverflowButton.Visibility = Visibility.Collapsed;
                    SetTopOverflowButtonOffset(0.0);
                    return;
                }

                SetTopOverflowButtonOffset(overflowOffset);
                _topOverflowButton.ContextMenu = CreateTopOverflowMenu(overflowItems);
            }
            finally
            {
                _updatingTopOverflow = false;
            }
        }

        private List<NavigationViewItem> GetTopNavigationItems()
        {
            List<NavigationViewItem> navItems = [];
            foreach (object item in Items)
            {
                NavigationViewItem? navItem = item as NavigationViewItem
                    ?? ItemContainerGenerator.ContainerFromItem(item) as NavigationViewItem;

                if (navItem is not null)
                {
                    navItems.Add(navItem);
                }
            }

            return navItems;
        }

        private void SetTopOverflowButtonOffset(double x)
        {
            if (_topOverflowButton is null)
            {
                return;
            }

            _topOverflowButton.RenderTransform = null;
            if (x > 0.0)
            {
                _topOverflowButton.RenderTransform = new TranslateTransform(Math.Max(0.0, x), 0.0);
            }
        }

        private System.Windows.Controls.ContextMenu CreateTopOverflowMenu(IReadOnlyList<NavigationViewItem> overflowItems)
        {
            ContextMenu menu = new();
            foreach (NavigationViewItem navItem in overflowItems)
            {
                MenuItem menuItem = new()
                {
                    Header = GetOverflowItemText(navItem),
                    Icon = CreateOverflowIcon(navItem),
                    MinWidth = 280,
                    MinHeight = 44,
                    Tag = navItem
                };
                menuItem.Click += OnTopOverflowMenuItemClick;
                _ = menu.Items.Add(menuItem);
            }

            return menu;
        }

        private static object? CreateOverflowIcon(NavigationViewItem navItem)
        {
            if (navItem.Icon is not FontIcon fontIcon)
            {
                return null;
            }

            FontIcon overflowIcon = new()
            {
                Glyph = fontIcon.Glyph,
                IconFontFamily = fontIcon.IconFontFamily,
                IconFontSize = 16.0,
                MirroredWhenRightToLeft = fontIcon.MirroredWhenRightToLeft
            };
            overflowIcon.SetResourceReference(ForegroundProperty, "TextFillColorSecondaryBrush");

            return overflowIcon;
        }

        private static string GetOverflowItemText(NavigationViewItem navItem)
        {
            string text = navItem.Content as string
                ?? navItem.Content?.ToString()
                ?? navItem.Tag as string
                ?? string.Empty;

            return string.IsNullOrWhiteSpace(text) ? navItem.GetType().Name : text;
        }

        private static double GetElementWidth(FrameworkElement element)
        {
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double desiredWidth = Math.Max(element.DesiredSize.Width, element.MinWidth);
            return desiredWidth > 0.0 ? desiredWidth : element.ActualWidth;
        }

        private static NavigationViewItem? FindNavigationViewItem(DependencyObject? focused)
        {
            DependencyObject? current = focused;
            while (current is not null)
            {
                if (current is NavigationViewItem asItem)
                {
                    return asItem;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// Represents a reference to the back navigation button control.
        /// </summary>
        private System.Windows.Controls.Button? _backButton;

        /// <summary>
        /// Represents the toggle button control used to show or hide a pane within the user interface.
        /// </summary>
        private System.Windows.Controls.Button? _paneToggleButton;

        private FrameworkElement? _topItemsHost;

        private System.Windows.Controls.Button? _topOverflowButton;

        private FluenceWindow? _titleBarExtensionWindow;

        private DependencyPropertyDescriptor? _titleBarExtensionDescriptor;

        private ColumnDefinition? _paneColumn;

        /// <summary>
        /// Represents the visual element used to indicate the current selection within the user interface.
        /// </summary>
        private FrameworkElement? _selectionIndicator;

        /// <summary>
        /// Represents the host element for displaying an indicator within the user interface.
        /// </summary>
        private FrameworkElement? _indicatorHost;

        /// <summary>
        /// Stores the current generation or version of the indicator animation.
        /// </summary>
        /// <remarks>This field is typically used to track changes or updates to the animation state,
        /// allowing the system to determine if a new animation sequence should be started or if the current one remains
        /// valid.</remarks>
        private int _indicatorAnimationGeneration;

        private int _paneColumnAnimationGeneration;

        private bool _topOverflowUpdateScheduled;

        private bool _updatingTopOverflow;

        private bool _updatingTitleBarExtension;

        /// <summary>
        /// Indicates whether the indicator has been positioned.
        /// </summary>
        private bool _indicatorPositioned;
    }
}
