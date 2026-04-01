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

        public ListView()
        {
            Loaded += OnListViewLoaded;
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

            var style = TryFindResource("FluentListViewGroupItemStyle") as Style;
            if (style != null)
            {
                GroupStyle.Add(new GroupStyle { ContainerStyle = style });
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ListViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is ListViewItem;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

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
