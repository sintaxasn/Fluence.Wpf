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
    /// Indeterminate animation reproduces the WinUI 3 canonical 6-dot orbit (5 visible) authored
    /// in <c>ProgressRingStoryboardAnimationPage.xaml</c> in the microsoft-ui-xaml repository.
    /// All animation is XAML-driven — five staggered <see cref="RotateTransform"/>s drive five
    /// orbiting dots through a 3.47 s repeating storyboard kicked off by a <see cref="MultiTrigger"/>
    /// on (<see cref="IsActive"/>, <see cref="IsIndeterminate"/>).  No code-behind storyboard
    /// management exists; the WPF templating engine starts and stops the animation in response to
    /// dependency-property changes.
    /// </para>
    /// <para>
    /// The dot diameter and the ring's outer edge offset (<see cref="EllipseDiameter"/> /
    /// <see cref="EllipseOffset"/>) match WinUI's <c>ProgressRingTemplateSettings</c>:
    /// <c>diameter = (ActualWidth × 0.1) + (ActualWidth ≤ 40 ? 1 : 0)</c>,
    /// <c>anchor = (ActualWidth × 0.5) − diameter</c>.
    /// </para>
    /// <para>
    /// Determinate mode renders a stroked arc through <see cref="ArcSegment"/>; the arc end-angle
    /// tweens for 150 ms when <see cref="Value"/> changes.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PART_DeterminateArc, Type = typeof(Path))]
    public class ProgressRing : Control
    {
        private const string PART_DeterminateArc = "PART_DeterminateArc";

        private static readonly Duration DeterminateAnimationDuration = new Duration(TimeSpan.FromMilliseconds(150));
        private static readonly IEasingFunction DeterminateAnimationEasing = new CubicEase { EasingMode = EasingMode.EaseInOut };

        private Path _arcPath;

        static ProgressRing()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ProgressRing),
                new FrameworkPropertyMetadata(typeof(ProgressRing)));
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

        /// <summary>Gets or sets the thickness of the determinate-mode arc stroke.</summary>
        /// <remarks>The indeterminate dots are sized via <see cref="EllipseDiameter"/> and ignore this property.</remarks>
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
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
        /// Gets the diameter of each indeterminate-mode orbit dot, computed as
        /// <c>(ActualWidth × 0.1) + (ActualWidth ≤ 40 ? 1 : 0)</c>.  Mirrors
        /// <c>ProgressRingTemplateSettings.EllipseDiameter</c> in the WinUI 3 source.
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
        /// Gets the top-margin offset that pushes each indeterminate-mode dot from the centre of
        /// the layout grid out to the orbit radius.  Mirrors WinUI 3
        /// <c>ProgressRingTemplateSettings.EllipseOffset</c>; only the <c>Top</c> component is non-zero.
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

            _arcPath = GetTemplateChild(PART_DeterminateArc) as Path;

            UpdateTemplateSettings();

            if (!IsIndeterminate)
            {
                // Force rendering: AnimatedFraction may already equal the target value
                // (set before the template applied), in which case the property-changed
                // callback never fires and the arc would stay blank.
                double fraction = ComputeFraction();
                AnimatedFraction = fraction;
                RenderDeterminateArc(fraction);
            }
        }

        /// <inheritdoc />
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateTemplateSettings();
            if (!IsIndeterminate)
            {
                RenderDeterminateArc(AnimatedFraction);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Indeterminate / determinate mode switch
        // ──────────────────────────────────────────────────────────────────────

        private static void OnIsIndeterminateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = (ProgressRing)d;
            if ((bool)e.NewValue)
            {
                // Switching to indeterminate: stop any in-flight value tween (otherwise its
                // Completed callback will re-render the arc geometry we just cleared), then
                // null out arc data.  XAML triggers handle dot animation.
                ring.BeginAnimation(AnimatedFractionProperty, null);
                if (ring._arcPath != null)
                {
                    ring._arcPath.Data = null;
                }
            }
            else
            {
                // Switching to determinate: render arc to the current value (no transition tween).
                ring.AnimatedFraction = ring.ComputeFraction();
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
            animation.Completed += (s, args) => ring.AnimatedFraction = targetFraction;
            ring.BeginAnimation(AnimatedFractionProperty, animation);
        }

        private static void OnStrokeThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ring = (ProgressRing)d;
            if (!ring.IsIndeterminate)
            {
                ring.RenderDeterminateArc(ring.AnimatedFraction);
            }
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
        // Determinate arc rendering
        // ──────────────────────────────────────────────────────────────────────

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

            double size = ActualWidth;
            if (double.IsNaN(size) || size <= 0)
            {
                size = Width;
            }

            if (double.IsNaN(size) || size <= 0)
            {
                // Defer until the first layout pass populates ActualWidth.
                EventHandler handler = null;
                handler = delegate { LayoutUpdated -= handler; RenderDeterminateArc(fraction); };
                LayoutUpdated += handler;
                return;
            }

            double radius = (size - StrokeThickness) / 2.0;
            if (radius <= 0)
            {
                _arcPath.Data = null;
                return;
            }

            double center = size / 2.0;

            double angle = Math.Min(fraction * 360.0, 359.99);
            double angleRad = angle * Math.PI / 180.0;

            var startPoint = new Point(center, center - radius);
            var endPoint = new Point(
                center + radius * Math.Sin(angleRad),
                center - radius * Math.Cos(angleRad));

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
            _arcPath.Data = geometry;
        }
    }
}
