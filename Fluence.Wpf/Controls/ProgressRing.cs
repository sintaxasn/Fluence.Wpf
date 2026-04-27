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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Fluence.Wpf.Automation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A circular progress indicator that supports both determinate and indeterminate modes.
    /// </summary>
    /// <remarks>
    /// Indeterminate animation approximates the WinUI 3 Lottie ProgressRing:
    /// continuous base rotation (360° / 1.4 s) combined with a sinusoidal arc-sweep
    /// oscillation (10% → 75% → 10% of circumference, period 1.6 s).
    /// </remarks>
    [TemplatePart(Name = PART_IndeterminateRing, Type = typeof(System.Windows.Shapes.Ellipse))]
    [TemplatePart(Name = PART_DeterminateArc, Type = typeof(Path))]
    public class ProgressRing : Control
    {
        private const string PART_IndeterminateRing = "PART_IndeterminateRing";
        private const string PART_DeterminateArc = "PART_DeterminateArc";

        // Determinate arc animation settings
        private static readonly Duration AnimationDuration = new Duration(TimeSpan.FromMilliseconds(150));
        private static readonly IEasingFunction AnimationEasing = new CubicEase { EasingMode = EasingMode.EaseInOut };

        // Indeterminate animation timings (WinUI Lottie approximation)
        // Rotation: 360° every 1.4 s ≈ 257°/s (WinUI Lottie ≈ 250°/s).
        private static readonly Duration RotationDuration = new Duration(TimeSpan.FromSeconds(1.4));

        private Path _arcPath;
        private System.Windows.Shapes.Ellipse _indeterminateRing;
        private RotateTransform _indeterminateRotation;

        static ProgressRing()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(typeof(ProgressRing)));
        }

        // ──── Private DP: animated fraction for determinate arc ────

        private static readonly DependencyProperty AnimatedFractionProperty =
            DependencyProperty.Register(
                nameof(AnimatedFraction),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(0.0, OnAnimatedFractionChanged));

        private double AnimatedFraction
        {
            get { return (double)GetValue(AnimatedFractionProperty); }
            set { SetValue(AnimatedFractionProperty, value); }
        }

        private static void OnAnimatedFractionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ProgressRing)d).RenderArc((double)e.NewValue);
        }

        // ──── Private DP: sweep fraction for indeterminate arc ────

        private static readonly DependencyProperty IndeterminateSweepProperty =
            DependencyProperty.Register(
                "IndeterminateSweep",
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(0.0, OnIndeterminateSweepChanged));

        private double IndeterminateSweep
        {
            get { return (double)GetValue(IndeterminateSweepProperty); }
            set { SetValue(IndeterminateSweepProperty, value); }
        }

        private static void OnIndeterminateSweepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = (ProgressRing)d;
            ring.UpdateIndeterminateDash((double)e.NewValue, ring.IndeterminateOffset);
        }

        // ──── Private DP: dash-offset fraction for indeterminate caterpillar ────
        // During contraction this advances at the same rate the sweep shrinks so the
        // leading edge of the arc moves only with rotation (no backward slip).

        private static readonly DependencyProperty IndeterminateOffsetProperty =
            DependencyProperty.Register(
                "IndeterminateOffset",
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(0.0, OnIndeterminateOffsetChanged));

        private double IndeterminateOffset
        {
            get { return (double)GetValue(IndeterminateOffsetProperty); }
            set { SetValue(IndeterminateOffsetProperty, value); }
        }

        private static void OnIndeterminateOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = (ProgressRing)d;
            ring.UpdateIndeterminateDash(ring.IndeterminateSweep, (double)e.NewValue);
        }

        // Cumulative offset accumulates across animation cycles to prevent visual reset jump.
        private double _cumulativeOffset;
        private bool _isIndeterminateRunning;

        // ──── Public DPs ────

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
                new FrameworkPropertyMetadata(true, OnIsIndeterminateChanged));

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

        // ──── Automation ────

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ProgressRingAutomationPeer(this);
        }

        // ──── Template ────

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _arcPath = GetTemplateChild(PART_DeterminateArc) as Path;
            _indeterminateRing = GetTemplateChild(PART_IndeterminateRing) as System.Windows.Shapes.Ellipse;
            _indeterminateRotation = _indeterminateRing != null
                ? _indeterminateRing.RenderTransform as RotateTransform
                : null;

            UpdateIndeterminateState();

            if (_arcPath != null && !IsIndeterminate)
            {
                double range = Maximum - Minimum;
                double fraction = range > 0
                    ? Math.Max(0, Math.Min(1, (Value - Minimum) / range))
                    : 0;
                AnimatedFraction = fraction;
            }
        }

        // ──── Indeterminate animation ────

        private static void OnIsIndeterminateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = (ProgressRing)d;
            ring.UpdateDeterminateArc();
            ring.UpdateIndeterminateState();
        }

        private void UpdateIndeterminateState()
        {
            if (IsIndeterminate)
                StartIndeterminateAnimation();
            else
                StopIndeterminateAnimation();
        }

        private void StartIndeterminateAnimation()
        {
            if (_indeterminateRotation == null)
                return;

            _isIndeterminateRunning = true;
            _cumulativeOffset = 0.0;

            // Continuous rotation: 360° every 1.4 s.
            var rotAnim = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = RotationDuration,
                RepeatBehavior = RepeatBehavior.Forever,
                FillBehavior = FillBehavior.HoldEnd
            };
            _indeterminateRotation.BeginAnimation(RotateTransform.AngleProperty, rotAnim);

            StartNextIndeterminateCycle();
        }

        private void StartNextIndeterminateCycle()
        {
            if (!_isIndeterminateRunning)
                return;

            double startOffset = _cumulativeOffset;
            double endOffset = _cumulativeOffset + 0.65;

            // ── Sweep animation (one cycle, 1.6 s) ───────────────────────────────────────
            // 4-keyframe S-curve: fast expansion → slow apex arrival → ease-in-out contraction.
            // Sweep range: 10 % → 40 % → 75 % → 10 % of circumference.
            var sweepAnim = CreateIndeterminateSweepAnimation();
            BeginAnimation(IndeterminateSweepProperty, sweepAnim);

            // ── Offset animation (one cycle, 1.6 s) ──────────────────────────────────────
            // Offset stays fixed during expansion (0 → 0.8 s) then advances by 0.65 during
            // contraction (0.8 → 1.6 s) with matching ease-in-out easing.  This keeps the
            // leading edge of the arc moving forward at rotation speed only (no backward slip):
            //   d(head)/dt = rotation_rate + d(offset)/dt + d(sweep)/dt
            //              = 1/1.4  +  (-d(sweep)/dt)    + d(sweep)/dt
            //              = 1/1.4   ← constant forward motion regardless of sweep change.
            var offsetAnim = new DoubleAnimationUsingKeyFrames
            {
                FillBehavior = FillBehavior.HoldEnd
            };
            offsetAnim.KeyFrames.Add(new LinearDoubleKeyFrame(
                startOffset,
                KeyTime.FromTimeSpan(TimeSpan.Zero)));
            offsetAnim.KeyFrames.Add(new LinearDoubleKeyFrame(
                startOffset,
                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.8))));
            offsetAnim.KeyFrames.Add(new SplineDoubleKeyFrame(
                endOffset,
                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.6)),
                new KeySpline(0.4, 0.0, 0.6, 1.0)));

            offsetAnim.Completed += delegate
            {
                if (!_isIndeterminateRunning)
                    return;
                _cumulativeOffset = endOffset;
                StartNextIndeterminateCycle();
            };

            BeginAnimation(IndeterminateOffsetProperty, offsetAnim);
        }

        /// <summary>
        /// Creates the 4-keyframe S-curve sweep animation for one indeterminate cycle.
        /// Exposed internal for unit-test access (KeyFrames.Count assertion).
        /// </summary>
        /// <remarks>
        /// Returns a single-cycle (non-repeating) animation so that the caterpillar
        /// offset accumulation via <see cref="StartNextIndeterminateCycle"/> can chain
        /// cycles without resetting the dash offset to zero.
        ///
        /// KeySplines:
        ///   0.0 s → 0.10  initial minimum arc
        ///   0.4 s → 0.40  ease-out fast expansion (0,0,0.2,1)
        ///   0.8 s → 0.75  ease-in slow arrival at apex (0.8,0,1,1)
        ///   1.6 s → 0.10  ease-in-out smooth contraction (0.4,0,0.6,1)
        /// </remarks>
        internal static DoubleAnimationUsingKeyFrames CreateIndeterminateSweepAnimation()
        {
            var sweepAnim = new DoubleAnimationUsingKeyFrames
            {
                FillBehavior = FillBehavior.HoldEnd
            };

            sweepAnim.KeyFrames.Add(new LinearDoubleKeyFrame(
                0.1,
                KeyTime.FromTimeSpan(TimeSpan.Zero)));

            sweepAnim.KeyFrames.Add(new SplineDoubleKeyFrame(
                0.40,
                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.4)),
                new KeySpline(0.0, 0.0, 0.2, 1.0)));

            sweepAnim.KeyFrames.Add(new SplineDoubleKeyFrame(
                0.75,
                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.8)),
                new KeySpline(0.8, 0.0, 1.0, 1.0)));

            sweepAnim.KeyFrames.Add(new SplineDoubleKeyFrame(
                0.1,
                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.6)),
                new KeySpline(0.4, 0.0, 0.6, 1.0)));

            return sweepAnim;
        }

        private void StopIndeterminateAnimation()
        {
            _isIndeterminateRunning = false;
            _cumulativeOffset = 0.0;

            if (_indeterminateRotation != null)
                _indeterminateRotation.BeginAnimation(RotateTransform.AngleProperty, null);

            BeginAnimation(IndeterminateSweepProperty, null);
            BeginAnimation(IndeterminateOffsetProperty, null);

            if (_indeterminateRing != null)
            {
                _indeterminateRing.StrokeDashArray = null;
                _indeterminateRing.StrokeDashOffset = 0;
            }
        }

        private void UpdateIndeterminateDash(double sweepFraction, double offsetFraction)
        {
            if (_indeterminateRing == null)
                return;

            if (sweepFraction <= 0)
            {
                _indeterminateRing.StrokeDashArray = null;
                _indeterminateRing.StrokeDashOffset = 0;
                return;
            }

            double size = ActualWidth > 0 ? ActualWidth : Width;
            if (double.IsNaN(size) || size <= 0)
                return;

            double radius = (size - StrokeThickness) / 2.0;
            if (radius <= 0)
                return;

            double circumference = 2.0 * Math.PI * radius;
            double unitLength = circumference / StrokeThickness;
            double dashLen = sweepFraction * unitLength;
            double gapLen = Math.Max(0.01, (1.0 - sweepFraction) * unitLength);
            _indeterminateRing.StrokeDashArray = new DoubleCollection(new double[] { dashLen, gapLen });
            _indeterminateRing.StrokeDashOffset = offsetFraction * unitLength;
        }

        // ──── Determinate arc ────

        private static void OnArcPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ProgressRing)d).UpdateDeterminateArc();
        }

        private void UpdateDeterminateArc()
        {
            if (_arcPath == null || IsIndeterminate)
            {
                return;
            }

            double range = Maximum - Minimum;
            double targetFraction = range > 0
                ? Math.Max(0, Math.Min(1, (Value - Minimum) / range))
                : 0;

            var animation = new DoubleAnimation
            {
                From = AnimatedFraction,
                To = targetFraction,
                Duration = AnimationDuration,
                EasingFunction = AnimationEasing,
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (s, e) =>
            {
                AnimatedFraction = targetFraction;
            };

            BeginAnimation(AnimatedFractionProperty, animation);
        }

        private void RenderArc(double fraction)
        {
            if (_arcPath == null)
            {
                return;
            }

            if (fraction <= 0)
            {
                _arcPath.Data = null;
                return;
            }

            double size = ActualWidth > 0 ? ActualWidth : Width;
            if (double.IsNaN(size) || size <= 0)
            {
                EventHandler handler = null;
                handler = delegate { LayoutUpdated -= handler; RenderArc(fraction); };
                LayoutUpdated += handler;
                return;
            }

            double radius = (size - StrokeThickness) / 2.0;
            double centerX = size / 2.0;
            double centerY = size / 2.0;

            double angle = Math.Min(fraction * 360.0, 359.99);
            double angleRad = angle * Math.PI / 180.0;
            double startX = centerX;
            double startY = centerY - radius;
            double endX = centerX + radius * Math.Sin(angleRad);
            double endY = centerY - radius * Math.Cos(angleRad);

            var figure = new PathFigure
            {
                StartPoint = new Point(startX, startY),
                IsClosed = false
            };

            figure.Segments.Add(new ArcSegment
            {
                Point = new Point(endX, endY),
                Size = new Size(radius, radius),
                IsLargeArc = angle > 180,
                SweepDirection = SweepDirection.Clockwise
            });

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            _arcPath.Data = geometry;
        }

        /// <inheritdoc />
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            RenderArc(AnimatedFraction);
            if (IsIndeterminate)
                UpdateIndeterminateDash(IndeterminateSweep, IndeterminateOffset);
        }
    }
}
