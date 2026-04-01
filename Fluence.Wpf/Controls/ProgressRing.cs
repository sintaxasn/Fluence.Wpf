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
using System.Windows.Shapes;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A circular progress indicator that supports both determinate and indeterminate modes.
    /// </summary>
    /// <remarks>Inspired by WInUI's ProgressRing.</remarks>
    public class ProgressRing : Control
    {
        private Path _arcPath;

        static ProgressRing()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(typeof(ProgressRing)));
        }

        /// <summary>Identifies the <see cref="IsActive"/> dependency property.</summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(true));

        /// <summary>Gets or sets whether the progress ring is active and visible.</summary>
        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        /// <summary>Identifies the <see cref="IsIndeterminate"/> dependency property.</summary>
        public static readonly DependencyProperty IsIndeterminateProperty =
            DependencyProperty.Register(
                nameof(IsIndeterminate),
                typeof(bool),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(true, OnArcPropertyChanged));

        /// <summary>Gets or sets whether the ring operates in indeterminate (spinning) mode.</summary>
        public bool IsIndeterminate
        {
            get { return (bool)GetValue(IsIndeterminateProperty); }
            set { SetValue(IsIndeterminateProperty, value); }
        }

        /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(0.0, OnArcPropertyChanged));

        /// <summary>Gets or sets the current progress value in determinate mode.</summary>
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(0.0, OnArcPropertyChanged));

        /// <summary>Gets or sets the minimum value.</summary>
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(100.0, OnArcPropertyChanged));

        /// <summary>Gets or sets the maximum value.</summary>
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>Identifies the <see cref="StrokeThickness"/> dependency property.</summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(4.0));

        /// <summary>Gets or sets the thickness of the ring stroke.</summary>
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _arcPath = GetTemplateChild("PART_DeterminateArc") as Path;
            UpdateDeterminateArc();
        }

        private static void OnArcPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = (ProgressRing)d;
            ring.UpdateDeterminateArc();
        }

        private void UpdateDeterminateArc()
        {
            if (_arcPath == null || IsIndeterminate)
            {
                return;
            }

            double range = Maximum - Minimum;
            if (range <= 0)
            {
                _arcPath.Data = null;
                return;
            }

            double fraction = Math.Max(0, Math.Min(1, (Value - Minimum) / range));
            if (fraction <= 0)
            {
                _arcPath.Data = null;
                return;
            }

            double size = ActualWidth > 0 ? ActualWidth : Width;
            if (double.IsNaN(size) || size <= 0)
            {
                size = 32;
            }

            double radius = (size - StrokeThickness) / 2.0;
            double centerX = size / 2.0;
            double centerY = size / 2.0;

            double angle = fraction * 360.0;
            if (angle >= 359.99)
            {
                angle = 359.99;
            }

            double angleRad = angle * Math.PI / 180.0;
            double startX = centerX;
            double startY = centerY - radius;
            double endX = centerX + radius * Math.Sin(angleRad);
            double endY = centerY - radius * Math.Cos(angleRad);

            bool isLargeArc = angle > 180;

            var figure = new PathFigure
            {
                StartPoint = new Point(startX, startY),
                IsClosed = false
            };

            figure.Segments.Add(new ArcSegment
            {
                Point = new Point(endX, endY),
                Size = new Size(radius, radius),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise
            });

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            _arcPath.Data = geometry;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateDeterminateArc();
        }
    }
}
