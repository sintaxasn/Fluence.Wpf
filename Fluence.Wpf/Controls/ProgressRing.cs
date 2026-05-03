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
    /// <para>
    /// Indeterminate animation renders a single rounded "caterpillar" arc through
    /// <see cref="ArcSegment"/> and animates private start/sweep angle dependency properties.
    /// This keeps the newer Fluent continuous-arc feel without taking a dependency on WinUI's
    /// animated visual infrastructure.
    /// </para>
    /// <para>
    /// The legacy orbit-dot template settings (<see cref="EllipseDiameter"/> /
    /// <see cref="EllipseOffset"/>) are retained for custom templates and compatibility with
    /// earlier Fluence versions. The default template no longer consumes them.
    /// </para>
    /// <para>
    /// Determinate mode renders a stroked arc through <see cref="ArcSegment"/>; the arc end-angle
    /// tweens for 150 ms when <see cref="Value"/> changes.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PART_IndeterminateArc, Type = typeof(Path))]
    [TemplatePart(Name = PART_DeterminateArc, Type = typeof(Path))]
    public class ProgressRing : Control
    {
        private const string PART_IndeterminateArc = "PART_IndeterminateArc";
        private const string PART_DeterminateArc = "PART_DeterminateArc";
        private const double IndeterminateStartAngleDefault = -720.0;
        private const double IndeterminateMinimumSweepAngle = 0.0;
        private const double IndeterminatePausedStartAngle = 0.0;
        private const double IndeterminatePausedSweepAngle = 50.0;
        private const double FullCircleLimit = 359.99;

        private static readonly Duration IndeterminateAnimationDuration = new Duration(TimeSpan.FromMilliseconds(5000));
        private static readonly Duration DeterminateAnimationDuration = new Duration(TimeSpan.FromMilliseconds(150));
        private static readonly IEasingFunction DeterminateAnimationEasing = new CubicEase { EasingMode = EasingMode.EaseInOut };

        private Path _arcPath;
        private Path _indeterminateArcPath;
        private bool _isIndeterminateAnimationRunning;

        static ProgressRing()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(typeof(ProgressRing)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressRing"/> class.
        /// </summary>
        public ProgressRing()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Public dependency properties
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>Identifies the <see cref="IsActive"/> dependency property.</summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(true, OnIsActiveChanged));

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
                new FrameworkPropertyMetadata(0.0, OnRangePropertyChanged, CoerceRingValue));

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
                new FrameworkPropertyMetadata(0.0, OnMinMaxPropertyChanged));

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
                new FrameworkPropertyMetadata(100.0, OnMinMaxPropertyChanged));

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
                new FrameworkPropertyMetadata(4.0, OnStrokeThicknessChanged));

        /// <summary>Gets or sets the thickness of the progress arc stroke.</summary>
        /// <remarks>Applies to both determinate and indeterminate arc visuals in the default template.</remarks>
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        /// <summary>Identifies the <see cref="ProgressState"/> dependency property.</summary>
        public static readonly DependencyProperty ProgressStateProperty =
            DependencyProperty.Register(
                nameof(ProgressState),
                typeof(ProgressRingState),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(ProgressRingState.Normal, OnProgressStateChanged));

        /// <summary>
        /// Gets or sets the visual state used to color the progress arc.
        /// </summary>
        public ProgressRingState ProgressState
        {
            get { return (ProgressRingState)GetValue(ProgressStateProperty); }
            set { SetValue(ProgressStateProperty, value); }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Read-only template-settings dependency properties
        // (mirror WinUI's ProgressRingTemplateSettings.EllipseDiameter / EllipseOffset)
        // ──────────────────────────────────────────────────────────────────────

        private static readonly DependencyPropertyKey EllipseDiameterPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(EllipseDiameter),
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(0.0));

        /// <summary>Identifies the read-only <see cref="EllipseDiameter"/> dependency property.</summary>
        public static readonly DependencyProperty EllipseDiameterProperty = EllipseDiameterPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the diameter that earlier orbit-dot templates use for each indeterminate-mode dot.
        /// The default Fluence template now uses an arc, but this value is retained for custom
        /// templates and compatibility.
        /// </summary>
        public double EllipseDiameter
        {
            get { return (double)GetValue(EllipseDiameterProperty); }
        }

        private static readonly DependencyPropertyKey EllipseOffsetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(EllipseOffset),
                typeof(Thickness),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(default(Thickness)));

        /// <summary>Identifies the read-only <see cref="EllipseOffset"/> dependency property.</summary>
        public static readonly DependencyProperty EllipseOffsetProperty = EllipseOffsetPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the top-margin offset used by earlier orbit-dot templates to position each dot on
        /// the orbit radius. The default Fluence template now uses an arc, but this value is retained
        /// for custom templates and compatibility.
        /// </summary>
        public Thickness EllipseOffset
        {
            get { return (Thickness)GetValue(EllipseOffsetProperty); }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Determinate animated-fraction (private DP drives the arc geometry)
        // ──────────────────────────────────────────────────────────────────────

        private static readonly DependencyProperty AnimatedFractionProperty =
            DependencyProperty.Register(
                "AnimatedFraction",
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
            ((ProgressRing)d).RenderDeterminateArc((double)e.NewValue);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Indeterminate animated angles (private DPs drive the caterpillar arc)
        // ──────────────────────────────────────────────────────────────────────

        private static readonly DependencyProperty IndeterminateStartAngleProperty =
            DependencyProperty.Register(
                "IndeterminateStartAngle",
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(IndeterminateStartAngleDefault, OnIndeterminateGeometryChanged));

        private double IndeterminateStartAngle
        {
            get { return (double)GetValue(IndeterminateStartAngleProperty); }
            set { SetValue(IndeterminateStartAngleProperty, value); }
        }

        private static readonly DependencyProperty IndeterminateSweepAngleProperty =
            DependencyProperty.Register(
                "IndeterminateSweepAngle",
                typeof(double),
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(IndeterminateMinimumSweepAngle, OnIndeterminateGeometryChanged));

        private double IndeterminateSweepAngle
        {
            get { return (double)GetValue(IndeterminateSweepAngleProperty); }
            set { SetValue(IndeterminateSweepAngleProperty, value); }
        }

        private static void OnIndeterminateGeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ProgressRing)d).RenderIndeterminateArc();
        }

        // ──────────────────────────────────────────────────────────────────────
        // Automation
        // ──────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ProgressRingAutomationPeer(this);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Template wiring
        // ──────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _indeterminateArcPath = GetTemplateChild(PART_IndeterminateArc) as Path;
            _arcPath = GetTemplateChild(PART_DeterminateArc) as Path;

            UpdateTemplateSettings();

            if (IsIndeterminate)
            {
                RenderIndeterminateArc();
            }
            else
            {
                // Force rendering: AnimatedFraction may already equal the target value
                // (set before the template applied), in which case the property-changed
                // callback never fires and the arc would stay blank.
                double fraction = ComputeFraction();
                AnimatedFraction = fraction;
                RenderDeterminateArc(fraction);
            }

            UpdateIndeterminateAnimationState();
        }

        /// <inheritdoc />
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateTemplateSettings();
            if (IsIndeterminate)
            {
                RenderIndeterminateArc();
            }
            else
            {
                RenderDeterminateArc(AnimatedFraction);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Indeterminate / determinate mode switch
        // ──────────────────────────────────────────────────────────────────────

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateIndeterminateAnimationState();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopIndeterminateAnimation();
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ProgressRing)d).UpdateIndeterminateAnimationState();
        }

        private static void OnIsIndeterminateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = (ProgressRing)d;
            if ((bool)e.NewValue)
            {
                // Switching to indeterminate: stop any in-flight value tween (otherwise its
                // Completed callback will re-render the arc geometry we just cleared), then
                // null out determinate arc data.  Code-driven angle animations render the
                // caterpillar arc when the control is active.
                ring.BeginAnimation(AnimatedFractionProperty, null);
                if (ring._arcPath != null)
                {
                    ring._arcPath.Data = null;
                }

                ring.UpdateIndeterminateAnimationState();
            }
            else
            {
                ring.StopIndeterminateAnimation();

                // Switching to determinate: render arc to the current value (no transition tween).
                ring.AnimatedFraction = ring.ComputeFraction();
                ring.RenderDeterminateArc(ring.AnimatedFraction);
            }
        }

        private static void OnProgressStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = (ProgressRing)d;
            ring.UpdateIndeterminateAnimationState();
            if (ring.IsIndeterminate)
            {
                ring.RenderIndeterminateArc();
            }
            else
            {
                ring.RenderDeterminateArc(ring.AnimatedFraction);
            }
        }

        private static object CoerceRingValue(DependencyObject d, object baseValue)
        {
            var ring = (ProgressRing)d;
            double v = (double)baseValue;
            double min = ring.Minimum;
            double max = ring.Maximum;
            if (v < min) return min;
            if (v > max) return max;
            return baseValue;
        }

        private static void OnMinMaxPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Re-coerce Value so it stays within the new bounds, then redraw.
            d.CoerceValue(ValueProperty);
            OnRangePropertyChanged(d, e);
        }

        private static void OnRangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = (ProgressRing)d;
            if (ring.IsIndeterminate)
            {
                return;
            }

            double targetFraction = ring.ComputeFraction();

            // No tween before the template has applied — OnApplyTemplate will render the
            // initial frame synchronously.  Tweening here would race with the layout pass
            // and leave the arc blank when the dispatcher drains mid-animation.
            if (ring._arcPath == null)
            {
                ring.AnimatedFraction = targetFraction;
                return;
            }

            var animation = new DoubleAnimation
            {
                From = ring.AnimatedFraction,
                To = targetFraction,
                Duration = DeterminateAnimationDuration,
                EasingFunction = DeterminateAnimationEasing,
                FillBehavior = FillBehavior.Stop
            };
            // FillBehavior.Stop keeps the private DP from being held by the animation
            // clock after completion; the completion handler commits the final value so
            // the next tween starts from the rendered arc position.
            animation.Completed += (s, args) => ring.AnimatedFraction = targetFraction;
            ring.BeginAnimation(AnimatedFractionProperty, animation);
        }

        private static void OnStrokeThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = (ProgressRing)d;
            if (ring.IsIndeterminate)
            {
                ring.RenderIndeterminateArc();
            }
            else
            {
                ring.RenderDeterminateArc(ring.AnimatedFraction);
            }
        }

        private void UpdateIndeterminateAnimationState()
        {
            if (IsLoaded && IsActive && IsIndeterminate && ProgressState != ProgressRingState.Paused)
            {
                StartIndeterminateAnimation();
            }
            else
            {
                bool shouldRenderPausedFrame =
                    IsLoaded &&
                    IsActive &&
                    IsIndeterminate &&
                    ProgressState == ProgressRingState.Paused;
                StopIndeterminateAnimation();
                if (shouldRenderPausedFrame)
                {
                    IndeterminateStartAngle = IndeterminatePausedStartAngle;
                    IndeterminateSweepAngle = IndeterminatePausedSweepAngle;
                    RenderIndeterminateArc();
                }
            }
        }

        private void StartIndeterminateAnimation()
        {
            if (_indeterminateArcPath == null)
            {
                return;
            }

            if (_isIndeterminateAnimationRunning)
            {
                RenderIndeterminateArc();
                return;
            }

            _isIndeterminateAnimationRunning = true;
            IndeterminateStartAngle = IndeterminateStartAngleDefault;
            IndeterminateSweepAngle = IndeterminateMinimumSweepAngle;

            BeginAnimation(IndeterminateStartAngleProperty, CreateIndeterminateStartAnimation());
            BeginAnimation(IndeterminateSweepAngleProperty, CreateIndeterminateSweepAnimation());
            RenderIndeterminateArc();
        }

        private void StopIndeterminateAnimation()
        {
            BeginAnimation(IndeterminateStartAngleProperty, null);
            BeginAnimation(IndeterminateSweepAngleProperty, null);
            _isIndeterminateAnimationRunning = false;

            IndeterminateStartAngle = IndeterminateStartAngleDefault;
            IndeterminateSweepAngle = IndeterminateMinimumSweepAngle;

            if (_indeterminateArcPath != null)
            {
                _indeterminateArcPath.Data = null;
            }
        }

        private static DoubleAnimationUsingKeyFrames CreateIndeterminateStartAnimation()
        {
            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = IndeterminateAnimationDuration,
                RepeatBehavior = RepeatBehavior.Forever
            };

            AddLinearKeyFrame(animation, -720.0, 0.0);
            AddLinearKeyFrame(animation, -540.0, 0.125);
            AddLinearKeyFrame(animation, -360.0, 0.25);
            AddLinearKeyFrame(animation, -180.0, 0.325);
            AddLinearKeyFrame(animation, 0.0, 0.5);
            AddLinearKeyFrame(animation, 180.0, 0.625);
            AddLinearKeyFrame(animation, 360.0, 0.75);
            AddLinearKeyFrame(animation, 540.0, 0.875);
            AddLinearKeyFrame(animation, 720.0, 1.0);

            return animation;
        }

        private static DoubleAnimationUsingKeyFrames CreateIndeterminateSweepAnimation()
        {
            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = IndeterminateAnimationDuration,
                RepeatBehavior = RepeatBehavior.Forever
            };

            AddLinearKeyFrame(animation, 0.0, 0.0);
            AddLinearKeyFrame(animation, 50.0, 0.125);
            AddLinearKeyFrame(animation, 100.0, 0.25);
            AddLinearKeyFrame(animation, 50.0, 0.325);
            AddLinearKeyFrame(animation, 5.0, 0.5);
            AddLinearKeyFrame(animation, 50.0, 0.625);
            AddLinearKeyFrame(animation, 100.0, 0.75);
            AddLinearKeyFrame(animation, 50.0, 0.875);
            AddLinearKeyFrame(animation, 0.0, 1.0);

            return animation;
        }

        private static void AddLinearKeyFrame(DoubleAnimationUsingKeyFrames animation, double value, double percent)
        {
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(value, KeyTime.FromPercent(percent)));
        }

        private double ComputeFraction()
        {
            double range = Maximum - Minimum;
            if (range <= 0)
            {
                return 0;
            }

            return Math.Max(0, Math.Min(1, (Value - Minimum) / range));
        }

        // ──────────────────────────────────────────────────────────────────────
        // Template-settings computation
        // ──────────────────────────────────────────────────────────────────────

        private void UpdateTemplateSettings()
        {
            double width = ActualWidth;
            if (double.IsNaN(width) || width <= 0)
            {
                width = Width;
            }

            if (double.IsNaN(width) || width <= 0)
            {
                return;
            }

            // diameter = (width × 0.1) + (1 if width ≤ 40 else 0).  Source:
            // microsoft-ui-xaml-main/src/controls/dev/ProgressRing/ProgressRing.cpp::ApplyTemplateSettings.
            double diameter = (width * 0.1) + (width <= 40.0 ? 1.0 : 0.0);
            double anchor = (width * 0.5) - diameter;

            SetValue(EllipseDiameterPropertyKey, diameter);
            SetValue(EllipseOffsetPropertyKey, new Thickness(0, anchor, 0, 0));
        }

        // ──────────────────────────────────────────────────────────────────────
        // Arc rendering
        // ──────────────────────────────────────────────────────────────────────

        private void RenderIndeterminateArc()
        {
            if (_indeterminateArcPath == null)
            {
                return;
            }

            if (!IsActive || !IsIndeterminate)
            {
                _indeterminateArcPath.Data = null;
                return;
            }

            RenderArc(_indeterminateArcPath, IndeterminateStartAngle, IndeterminateSweepAngle, false, 0);
        }

        private void RenderDeterminateArc(double fraction)
        {
            if (_arcPath == null)
            {
                return;
            }

            // Defensive guard: if we've flipped to indeterminate while a tween is in flight,
            // the AnimatedFraction Completed callback can still arrive — drop it.
            if (IsIndeterminate)
            {
                _arcPath.Data = null;
                return;
            }

            if (fraction <= 0)
            {
                _arcPath.Data = null;
                return;
            }

            RenderArc(_arcPath, 0, fraction * 360.0, true, fraction);
        }

        private void RenderArc(Path path, double startAngle, double sweepAngle, bool deferForLayout, double deferredFraction)
        {
            double center;
            double radius;
            bool isLayoutSizeUnavailable;
            if (!TryGetArcMetrics(out center, out radius, out isLayoutSizeUnavailable))
            {
                path.Data = null;
                if (deferForLayout && isLayoutSizeUnavailable)
                {
                    // Defer determinate first render until the initial layout pass provides a size.
                    EventHandler handler = null;
                    handler = delegate { LayoutUpdated -= handler; RenderDeterminateArc(deferredFraction); };
                    LayoutUpdated += handler;
                }

                return;
            }

            double angle = Math.Max(0, Math.Min(sweepAngle, FullCircleLimit));
            if (angle <= 0)
            {
                path.Data = null;
                return;
            }

            var startPoint = GetArcPoint(center, radius, startAngle);
            var endPoint = GetArcPoint(center, radius, startAngle + angle);
            var figure = new PathFigure
            {
                StartPoint = startPoint,
                IsClosed = false
            };

            figure.Segments.Add(new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                IsLargeArc = angle > 180,
                SweepDirection = SweepDirection.Clockwise
            });

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            path.Data = geometry;
        }

        private bool TryGetArcMetrics(out double center, out double radius, out bool isLayoutSizeUnavailable)
        {
            double size = ActualWidth;
            if (double.IsNaN(size) || size <= 0)
            {
                size = Width;
            }

            if (double.IsNaN(size) || size <= 0)
            {
                center = 0;
                radius = 0;
                isLayoutSizeUnavailable = true;
                return false;
            }

            radius = (size - StrokeThickness) / 2.0;
            if (radius <= 0)
            {
                center = 0;
                isLayoutSizeUnavailable = false;
                return false;
            }

            center = size / 2.0;
            isLayoutSizeUnavailable = false;
            return true;
        }

        private static Point GetArcPoint(double center, double radius, double angle)
        {
            double angleRad = angle * Math.PI / 180.0;
            return new Point(
                center + radius * Math.Sin(angleRad),
                center - radius * Math.Cos(angleRad));
        }
    }
}
