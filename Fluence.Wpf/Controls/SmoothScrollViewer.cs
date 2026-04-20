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
using System.Windows.Media.Animation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A scroll viewer that animates scrolling with easing for a smooth experience.
    /// </summary>
    [TemplatePart(Name = PART_ScrollContentPresenter, Type = typeof(ScrollContentPresenter))]
    [TemplatePart(Name = PART_VerticalScrollBar, Type = typeof(ScrollBar))]
    [TemplatePart(Name = PART_HorizontalScrollBar, Type = typeof(ScrollBar))]
    public class SmoothScrollViewer : ScrollViewer
    {
        private const string PART_ScrollContentPresenter = "PART_ScrollContentPresenter";
        private const string PART_VerticalScrollBar = "PART_VerticalScrollBar";
        private const string PART_HorizontalScrollBar = "PART_HorizontalScrollBar";

        /// <summary>
        /// Identifies the <see cref="ScrollDuration"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScrollDurationProperty =
            DependencyProperty.Register(
                "ScrollDuration",
                typeof(Duration),
                typeof(SmoothScrollViewer),
                new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(250))));

        /// <summary>
        /// Gets or sets the duration of the smooth scroll animation.
        /// </summary>
        public Duration ScrollDuration
        {
            get { return (Duration)GetValue(ScrollDurationProperty); }
            set { SetValue(ScrollDurationProperty, value); }
        }

        private static readonly DependencyProperty CurrentVerticalOffsetProperty =
            DependencyProperty.Register(
                "CurrentVerticalOffset",
                typeof(double),
                typeof(SmoothScrollViewer),
                new PropertyMetadata(0.0, OnCurrentVerticalOffsetChanged));

        private double CurrentVerticalOffset
        {
            get { return (double)GetValue(CurrentVerticalOffsetProperty); }
            set { SetValue(CurrentVerticalOffsetProperty, value); }
        }

        private static void OnCurrentVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SmoothScrollViewer)d).ScrollToVerticalOffset((double)e.NewValue);
        }

        private static readonly DependencyProperty CurrentHorizontalOffsetProperty =
            DependencyProperty.Register(
                "CurrentHorizontalOffset",
                typeof(double),
                typeof(SmoothScrollViewer),
                new PropertyMetadata(0.0, OnCurrentHorizontalOffsetChanged));

        private double CurrentHorizontalOffset
        {
            get { return (double)GetValue(CurrentHorizontalOffsetProperty); }
            set { SetValue(CurrentHorizontalOffsetProperty, value); }
        }

        private static void OnCurrentHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SmoothScrollViewer)d).ScrollToHorizontalOffset((double)e.NewValue);
        }

        private double _targetVerticalOffset;
        private double _targetHorizontalOffset;
        private static readonly CubicEase SharedEase = new CubicEase { EasingMode = EasingMode.EaseOut };

        static SmoothScrollViewer()
        {
            SharedEase.Freeze();
        }

        /// <inheritdoc />
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            double delta = e.Delta * 0.6;
            bool isHorizontal = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (isHorizontal)
            {
                double scrollable = ScrollableWidth;
                _targetHorizontalOffset = Clamp(_targetHorizontalOffset, 0, scrollable);
                double newTarget = Clamp(_targetHorizontalOffset - delta, 0, scrollable);
                if (newTarget == _targetHorizontalOffset)
                    return;
                _targetHorizontalOffset = newTarget;
                AnimateTo(CurrentHorizontalOffsetProperty, _targetHorizontalOffset);
            }
            else
            {
                double scrollable = ScrollableHeight;
                _targetVerticalOffset = Clamp(_targetVerticalOffset, 0, scrollable);
                double newTarget = Clamp(_targetVerticalOffset - delta, 0, scrollable);
                if (newTarget == _targetVerticalOffset)
                    return;
                _targetVerticalOffset = newTarget;
                AnimateTo(CurrentVerticalOffsetProperty, _targetVerticalOffset);
            }

            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnScrollChanged(ScrollChangedEventArgs e)
        {
            base.OnScrollChanged(e);

            if (e.ExtentHeightChange != 0 || e.ViewportHeightChange != 0)
            {
                _targetVerticalOffset = Clamp(VerticalOffset, 0, ScrollableHeight);
            }
            if (e.ExtentWidthChange != 0 || e.ViewportWidthChange != 0)
            {
                _targetHorizontalOffset = Clamp(HorizontalOffset, 0, ScrollableWidth);
            }
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _targetVerticalOffset = VerticalOffset;
            _targetHorizontalOffset = HorizontalOffset;
        }

        private void AnimateTo(DependencyProperty property, double to)
        {
            var animation = new DoubleAnimation
            {
                To = to,
                Duration = ScrollDuration,
                EasingFunction = SharedEase
            };
            animation.Freeze();
            BeginAnimation(property, animation, HandoffBehavior.SnapshotAndReplace);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
