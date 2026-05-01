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
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design styled list view with animated item states.
    /// </summary>
    public class ListView : System.Windows.Controls.ListView
    {
        private static readonly Duration InsertDuration = new Duration(TimeSpan.FromMilliseconds(250));
        private static readonly Duration RemoveDuration = new Duration(TimeSpan.FromMilliseconds(200));

        static ListView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ListView),
                new FrameworkPropertyMetadata(typeof(ListView)));
        }

        /// <summary>
        /// Identifies the <see cref="ItemAnimationsEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemAnimationsEnabledProperty =
            DependencyProperty.Register(
                nameof(ItemAnimationsEnabled),
                typeof(bool),
                typeof(ListView),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether item animations are enabled.
        /// </summary>
        public bool ItemAnimationsEnabled
        {
            get { return (bool)GetValue(ItemAnimationsEnabledProperty); }
            set { SetValue(ItemAnimationsEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="HoverHighlightEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverHighlightEnabledProperty =
            DependencyProperty.Register(
                nameof(HoverHighlightEnabled),
                typeof(bool),
                typeof(ListView),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether hover highlighting is enabled.
        /// </summary>
        public bool HoverHighlightEnabled
        {
            get { return (bool)GetValue(HoverHighlightEnabledProperty); }
            set { SetValue(HoverHighlightEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ViewState"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewStateProperty =
            DependencyProperty.Register(
                nameof(ViewState),
                typeof(ListViewState),
                typeof(ListView),
                new FrameworkPropertyMetadata(ListViewState.Default));

        /// <summary>
        /// Gets or sets the view state of the list view.
        /// </summary>
        public ListViewState ViewState
        {
            get { return (ListViewState)GetValue(ViewStateProperty); }
            set { SetValue(ViewStateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(ListView),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the list view.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="EmptyContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EmptyContentProperty =
            DependencyProperty.Register(
                nameof(EmptyContent),
                typeof(object),
                typeof(ListView),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Content displayed when the list has no items.
        /// </summary>
        public object EmptyContent
        {
            get { return GetValue(EmptyContentProperty); }
            set { SetValue(EmptyContentProperty, value); }
        }

        /// <summary>
        /// Attached property mirrored from the parent <see cref="ListView"/> so item templates can use
        /// <c>MultiDataTrigger</c> (each condition must use a <c>Binding</c>, not <c>Property</c>, in WPF).
        /// </summary>
        public static readonly DependencyProperty ParentIsItemSelectableProperty =
            DependencyProperty.RegisterAttached(
                "ParentIsItemSelectable",
                typeof(bool),
                typeof(ListView),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Sets the parent list's <see cref="IsItemSelectable"/> value on an item container for template triggers.
        /// </summary>
        public static void SetParentIsItemSelectable(DependencyObject element, bool value)
        {
            element.SetValue(ParentIsItemSelectableProperty, value);
        }

        /// <summary>
        /// Gets whether the parent list allows item selection (for template triggers).
        /// </summary>
        public static bool GetParentIsItemSelectable(DependencyObject element)
        {
            return (bool)element.GetValue(ParentIsItemSelectableProperty);
        }

        /// <summary>
        /// Identifies the <see cref="IsItemSelectable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsItemSelectableProperty =
            DependencyProperty.Register(
                nameof(IsItemSelectable),
                typeof(bool),
                typeof(ListView),
                new FrameworkPropertyMetadata(true, OnIsItemSelectableChanged));

        /// <summary>
        /// Gets or sets whether items can be selected and show hover/selection visuals.
        /// When false, rows are display-only; scrolling and item animations are unchanged.
        /// </summary>
        public bool IsItemSelectable
        {
            get { return (bool)GetValue(IsItemSelectableProperty); }
            set { SetValue(IsItemSelectableProperty, value); }
        }

        private bool _suppressSelectionChange;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListView"/> class and wires the loaded event for default group styling.
        /// </summary>
        public ListView()
        {
            Loaded += OnListViewLoaded;
        }

        private static void OnIsItemSelectableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listView = (ListView)d;
            if (!(bool)e.NewValue)
            {
                listView._suppressSelectionChange = true;
                try
                {
                    listView.UnselectAll();
                }
                finally
                {
                    listView._suppressSelectionChange = false;
                }
            }

            listView.UpdateItemContainersFocusable();
        }

        private void UpdateItemContainersFocusable()
        {
            // Only realized containers exist when virtualization is active. Future
            // containers receive the same mirrored value in PrepareContainerForItemOverride.
            foreach (var item in Items)
            {
                var container = ItemContainerGenerator.ContainerFromItem(item) as DependencyObject;
                if (container != null)
                {
                    SetParentIsItemSelectable(container, IsItemSelectable);
                    if (container is UIElement ui)
                    {
                        ui.Focusable = IsItemSelectable;
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (!IsItemSelectable && !_suppressSelectionChange)
            {
                _suppressSelectionChange = true;
                try
                {
                    UnselectAll();
                }
                finally
                {
                    _suppressSelectionChange = false;
                }

                return;
            }

            base.OnSelectionChanged(e);
        }

        private void OnListViewLoaded(object sender, RoutedEventArgs e)
        {
            EnsureDefaultGroupStyle();
        }

        private void EnsureDefaultGroupStyle()
        {
            if (GroupStyle.Count > 0)
            {
                return;
            }

            var style = TryFindResource("ListViewGroupItemStyle") as Style;
            if (style != null)
            {
                GroupStyle.Add(new GroupStyle { ContainerStyle = style });
            }
        }

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ListViewItem();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is ListViewItem;
        }

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            SetParentIsItemSelectable(element, IsItemSelectable);

            var ui = element as UIElement;
            if (ui != null)
            {
                ui.Focusable = IsItemSelectable;
            }

            if (!ItemAnimationsEnabled || !IsLoaded)
                return;

            var container = element as UIElement;
            if (container == null)
                return;

            container.Opacity = 0;
            container.RenderTransform = new TranslateTransform(0, 12);

            var opacityAnim = new DoubleAnimation(0, 1, InsertDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var slideAnim = new DoubleAnimation(12, 0, InsertDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            container.BeginAnimation(OpacityProperty, opacityAnim);
            container.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }

        /// <summary>
        /// Animates out the item and then calls the provided callback.
        /// </summary>
        public void AnimateRemove(object item, Action onCompleted)
        {
            if (!ItemAnimationsEnabled)
            {
                Items.Remove(item);
                if (onCompleted != null) onCompleted();
                return;
            }

            var container = ItemContainerGenerator.ContainerFromItem(item) as UIElement;
            if (container == null)
            {
                Items.Remove(item);
                if (onCompleted != null) onCompleted();
                return;
            }

            if (container.RenderTransform == null || !(container.RenderTransform is TranslateTransform))
                container.RenderTransform = new TranslateTransform();

            var opacityAnim = new DoubleAnimation(container.Opacity, 0, RemoveDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var slideAnim = new DoubleAnimation(0, -12, RemoveDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            opacityAnim.Completed += (s, e) =>
            {
                Items.Remove(item);
                if (onCompleted != null) onCompleted();
            };

            container.BeginAnimation(OpacityProperty, opacityAnim);
            container.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }
    }
}
