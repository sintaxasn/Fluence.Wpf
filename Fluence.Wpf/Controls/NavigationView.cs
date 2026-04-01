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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using Fluence.Wpf;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A navigation control with a collapsible pane and content area, similar to WinUI NavigationView.
    /// </summary>
    /// <remarks>Inspired by WinUI's NavigationView.</remarks>
    [TemplatePart(Name = PartBackButton, Type = typeof(Button))]
    [TemplatePart(Name = PartContentPresenter, Type = typeof(ContentPresenter))]
    public class NavigationView : Selector
    {
        /// <summary>Name of the back button template part.</summary>
        public const string PartBackButton = "PART_BackButton";

        /// <summary>Name of the main content presenter template part.</summary>
        public const string PartContentPresenter = "PART_ContentPresenter";

        private Button _backButton;

        /// <summary>
        /// Identifies the <see cref="PaneDisplayMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaneDisplayModeProperty = DependencyProperty.Register(
            "PaneDisplayMode",
            typeof(NavigationViewPaneDisplayMode),
            typeof(NavigationView),
            new FrameworkPropertyMetadata(
                NavigationViewPaneDisplayMode.Left,
                FrameworkPropertyMetadataOptions.AffectsMeasure));

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
            new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsBackEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsBackEnabledProperty = DependencyProperty.Register(
            "IsBackEnabled",
            typeof(bool),
            typeof(NavigationView),
            new PropertyMetadata(true));

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
            new FrameworkPropertyMetadata(true, OnIsPaneOpenChanged));

        /// <summary>
        /// Identifies the <see cref="Content"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content",
            typeof(object),
            typeof(NavigationView),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="SelectedContent"/> dependency property.
        /// </summary>
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
        /// Occurs when the selected navigation item changes (CLR event with library-specific args).
        /// </summary>
        /// <remarks>The routed <see cref="Selector.SelectionChanged"/> event also fires for compatibility with XAML handlers.</remarks>
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

        /// <summary>
        /// Gets or sets whether the pane is shown on the left or across the top.
        /// </summary>
        public NavigationViewPaneDisplayMode PaneDisplayMode
        {
            get { return (NavigationViewPaneDisplayMode)GetValue(PaneDisplayModeProperty); }
            set { SetValue(PaneDisplayModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether keyboard focus on an item selects it immediately.
        /// </summary>
        public bool SelectionFollowsFocus
        {
            get { return (bool)GetValue(SelectionFollowsFocusProperty); }
            set { SetValue(SelectionFollowsFocusProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the back button is shown.
        /// </summary>
        public bool IsBackButtonVisible
        {
            get { return (bool)GetValue(IsBackButtonVisibleProperty); }
            set { SetValue(IsBackButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the back button can be invoked.
        /// </summary>
        public bool IsBackEnabled
        {
            get { return (bool)GetValue(IsBackEnabledProperty); }
            set { SetValue(IsBackEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets header content displayed beside the navigation chrome.
        /// </summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DataTemplate used to display the <see cref="Header"/>.
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets content at the start of the pane chrome (title area).
        /// </summary>
        public object PaneHeader
        {
            get { return GetValue(PaneHeaderProperty); }
            set { SetValue(PaneHeaderProperty, value); }
        }

        /// <summary>
        /// Gets or sets content at the end of the pane (footer).
        /// </summary>
        public object PaneFooter
        {
            get { return GetValue(PaneFooterProperty); }
            set { SetValue(PaneFooterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the background brush for the content area; if null, the theme default is used.
        /// </summary>
        public Brush ContentBackground
        {
            get { return (Brush)GetValue(ContentBackgroundProperty); }
            set { SetValue(ContentBackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the left pane is expanded. Ignored when <see cref="PaneDisplayMode"/> is <see cref="NavigationViewPaneDisplayMode.Top"/>.
        /// </summary>
        public bool IsPaneOpen
        {
            get { return (bool)GetValue(IsPaneOpenProperty); }
            set { SetValue(IsPaneOpenProperty, value); }
        }

        /// <summary>
        /// Gets or sets the content hosted in the main area (below or beside the pane).
        /// </summary>
        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets content bound to the current selection when items expose content; otherwise use <see cref="Content"/>.
        /// </summary>
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

            base.OnApplyTemplate();

            _backButton = GetTemplateChild(PartBackButton) as Button;
            if (_backButton != null)
            {
                _backButton.Click += OnBackButtonClick;
            }
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            UpdateSelectedContentFromSelection();
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
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is NavigationViewItem;
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

        private void OnBackButtonClick(object sender, RoutedEventArgs e)
        {
            var handler = BackRequested;
            if (handler != null)
            {
                handler(this, new NavigationViewBackRequestedEventArgs());
            }
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

        /// <summary>
        /// Raises <see cref="BackRequested"/> as the back button would. Used by unit tests (see InternalsVisibleTo).
        /// </summary>
        internal void RaiseBackRequestedForTesting()
        {
            OnBackButtonClick(this, new RoutedEventArgs());
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
        }

        private void UpdateSelectedContentFromSelection()
        {
            var nvi = SelectedItem as NavigationViewItem;
            if (nvi != null)
            {
                SetCurrentValue(SelectedContentProperty, nvi.Content);
                return;
            }

            if (SelectedItem != null)
            {
                var ic = ItemContainerGenerator.ContainerFromItem(SelectedItem);
                var navFromItem = ic as NavigationViewItem;
                if (navFromItem != null)
                {
                    SetCurrentValue(SelectedContentProperty, navFromItem.Content);
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
